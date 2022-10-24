namespace BarionClientLibrary;

/// <summary>
/// Provides a base class for executing Barion operations.
/// </summary>
public class BarionClient : IDisposable
{
    private HttpClient _httpClient;
    private BarionSettings _settings;
    private IRetryPolicy _retryPolicy;
    private TimeSpan _timeout;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
    private static readonly TimeSpan InfiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
    private static readonly JsonSerializerOptions deserializerOptions =
        new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(), new CultureInfoJsonConverter() }, AllowTrailingCommas = true };
    private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new TypeJsonConverter(), new CultureInfoJsonConverter() }
    };

    /// <summary>
    /// Initializes a new instance of the BarionClientLibrary.BarionClient class.
    /// </summary>
    /// <param name="settings">Barion specific settings.</param>
    public BarionClient(BarionSettings settings) : this(settings is null
        ? null
        : Options.Create(new BarionSettings
            {
                BaseUrl = settings.BaseUrl,
                Payee= settings.Payee,
                POSKey = settings.POSKey,
            }), new HttpClient()) {}

    /// <summary>
    /// Initializes a new instance of the BarionClientLibrary.BarionClient class.
    /// </summary>
    /// <param name="options">Barion specific settings.</param>
    /// <param name="httpClient">HttpClient instance to use for sending HTTP requests.</param>
    public BarionClient(IOptions<BarionSettings> options, HttpClient httpClient)
    {
        if (httpClient == null)
            throw new ArgumentNullException(nameof(httpClient));

        _httpClient = httpClient;

        if (options is null)
            throw new ArgumentNullException(nameof(_settings));

        _settings = options.Value;

        if (_settings.BaseUrl == null)
            throw new ArgumentNullException(nameof(_settings.BaseUrl));

        if (!_settings.BaseUrl.IsAbsoluteUri)
            throw new ArgumentException($"BaseUrl must be an absolute Uri. Actual value: {_settings.BaseUrl}", nameof(_settings.BaseUrl));


        _retryPolicy = new ExponentialRetry();

        _timeout = DefaultTimeout;
    }

    /// <summary>
    /// Gets or sets the retry policy to use on transient failures.
    /// </summary>
    public IRetryPolicy RetryPolicy
    {
        get
        {
            return _retryPolicy;
        }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _retryPolicy = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of milliseconds to wait before the request times out.
    /// </summary>
    public TimeSpan Timeout
    {
        get
        {
            return _timeout;
        }
        set
        {
            if (value != InfiniteTimeout && (value <= TimeSpan.Zero || value > MaxTimeout))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _timeout = value;
        }
    }

    /// <summary>
    /// Executes a Barion operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the Barion operation.</typeparam>
    /// <param name="operation">The Barion operation to execute.</param>
    /// <returns>Returns System.Threading.Tasks.Task`1.The task object representing the asynchronous operation.</returns>
    public async Task<TResult> ExecuteAsync<TResult>(BarionOperation operation)
        where TResult : BarionOperationResult
    {
        if (typeof(TResult) != operation.ResultType)
            throw new InvalidOperationException("TResult should be equal to the ResultType of the operation.");

        return await ExecuteAsync(operation) as TResult;
    }

    /// <summary>
    /// Executes a Barion operation.
    /// </summary>
    /// <param name="operation">The Barion operation to execute.</param>
    /// <returns>Returns System.Threading.Tasks.Task`1.The task object representing the asynchronous operation.</returns>
    public async Task<BarionOperationResult> ExecuteAsync(BarionOperation operation)
    {
        return await ExecuteAsync(operation, default(CancellationToken));
    }

    /// <summary>
    /// Executes a Barion operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the Barion operation.</typeparam>
    /// <param name="operation">The Barion operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Returns System.Threading.Tasks.Task`1.The task object representing the asynchronous operation.</returns>
    public async Task<TResult> ExecuteAsync<TResult>(BarionOperation operation, CancellationToken cancellationToken)
        where TResult : BarionOperationResult
    {
        if (typeof(TResult) != operation.ResultType)
            throw new InvalidOperationException("TResult should be equal to the ResultType of the operation.");

        return await ExecuteAsync(operation, cancellationToken) as TResult;
    }

    /// <summary>
    /// Executes a Barion operation.
    /// </summary>
    /// <param name="operation">The Barion operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>Returns System.Threading.Tasks.Task`1.The task object representing the asynchronous operation.</returns>
    public async Task<BarionOperationResult> ExecuteAsync(BarionOperation operation, CancellationToken cancellationToken)
    {
        CheckDisposed();
        ValidateOperation(operation);

        operation.POSKey = _settings.POSKey;

        return await SendWithRetry(operation, cancellationToken);
    }

    private async Task<BarionOperationResult> SendWithRetry(BarionOperation operation, CancellationToken cancellationToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        SetTimeout(linkedCts);

        var shouldRetry = false;
        uint currentRetryCount = 0;
        var retryInterval = TimeSpan.Zero;
        BarionOperationResult result = null;

        do
        {
            var message = PrepareHttpRequestMessage(operation);

            try
            {
                var responseMessage = await _httpClient.SendAsync(message, linkedCts.Token);

                result = await CreateResultFromResponseMessage(responseMessage, operation);

                if (!result.IsOperationSuccessful)
                    shouldRetry = _retryPolicy.CreateInstance().ShouldRetry(currentRetryCount, responseMessage.StatusCode, out retryInterval);
            }
            catch (Exception ex)
            {
                shouldRetry = _retryPolicy.CreateInstance().ShouldRetry(currentRetryCount, ex, out retryInterval);

                if (!shouldRetry)
                    throw;
            }

            if (shouldRetry)
            {
                await Task.Delay(retryInterval);
                currentRetryCount++;
            }
        } while (shouldRetry && !linkedCts.IsCancellationRequested);

        return result;
    }

    private HttpRequestMessage PrepareHttpRequestMessage(BarionOperation operation)
    {
        var message = new HttpRequestMessage(operation.Method, new Uri(_settings.BaseUrl, operation.RelativeUri));

        if (operation.Method == HttpMethod.Post || operation.Method == HttpMethod.Put)
        {
            //this <object> trick must for serialize properties of derived classes: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-6-0
            var body = JsonSerializer.Serialize<object>(operation, serializerOptions);
            message.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return message;
    }

    private async Task<BarionOperationResult> CreateResultFromResponseMessage(HttpResponseMessage responseMessage, BarionOperation operation)
    {
        var response = await responseMessage.Content.ReadAsStringAsync();
        var operationResult = JsonSerializer.Deserialize(response, operation.ResultType, deserializerOptions) as BarionOperationResult;

        if (operationResult == null)
            return CreateFailedOperationResult(operation.ResultType, "Deserialized result was null");

        if (!responseMessage.IsSuccessStatusCode && operationResult.Errors == null)
            return CreateFailedOperationResult(operation.ResultType, responseMessage.StatusCode.ToString(), responseMessage.ReasonPhrase, response);

        operationResult.IsOperationSuccessful = responseMessage.IsSuccessStatusCode && (operationResult.Errors == null || !operationResult.Errors.Any());

        return operationResult;
    }

    private void SetTimeout(CancellationTokenSource cancellationTokenSource)
    {
        if (_timeout != InfiniteTimeout)
        {
            cancellationTokenSource.CancelAfter(_timeout);
        }
    }

    private void ValidateOperation(BarionOperation operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        if (operation.RelativeUri == null)
            throw new ArgumentNullException(nameof(operation.RelativeUri));

        if (operation.RelativeUri.IsAbsoluteUri)
            throw new ArgumentException("operation.RelativeUri should be a relative Uri.", nameof(operation.RelativeUri));

        if (operation.ResultType == null)
            throw new ArgumentNullException(nameof(operation.ResultType));

        if (!operation.ResultType.GetTypeInfo().IsSubclassOf(typeof(BarionOperationResult)))
            throw new ArgumentException("ResultType should be a subclass of BarionOperationResult.", nameof(operation.ResultType));

        if (operation.Method == null)
            throw new ArgumentNullException(nameof(operation.Method));
    }

    private BarionOperationResult CreateFailedOperationResult(Type resultType, string errorCode, string title = null, string description = null)
    {
        var result = Activator.CreateInstance(resultType) as BarionOperationResult;
        result.IsOperationSuccessful = false;
        result.Errors = new[] 
        {
            new Error
            {
                ErrorCode = errorCode,
                Title = title,
                Description = description
            }
        };

        return result;
    }

    #region IDisposable members

    private volatile bool _disposed;

    /// <summary>
    /// Releases the unmanaged resources and disposes of the managed resources used by the BarionClient.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BarionClient()
    {
        Dispose(false);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the BarionClient and optionally disposes of the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
        }
    }

    #endregion

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().ToString());
        }
    }
}
