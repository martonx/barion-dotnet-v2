﻿namespace BarionClientLibrary.Tests.RetryPolicies;

public class ExponentialRetryTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.NoContent, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.NotImplemented, false)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, false)]
    [InlineData(HttpStatusCode.Redirect, false)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    public void ShouldRetry_ShouldReturnFalse_OnNonRetriableStatusCodes(HttpStatusCode httpStatusCode, bool expectedRetryResult)
    {
        var retry = new ExponentialRetry();

        Assert.Equal(expectedRetryResult, retry.ShouldRetry(0, httpStatusCode, out _));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void ShouldRetry_ShouldReturn_IfRetryCountIsTooHigh(uint currentRetryCount, bool expectedResult)
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.Equal(expectedResult, retry.ShouldRetry(currentRetryCount, HttpStatusCode.RequestTimeout, out _));
    }

    [Theory]
    [InlineData(WebExceptionStatus.Timeout, true)]
    [InlineData(WebExceptionStatus.ConnectionClosed, true)]
    [InlineData(WebExceptionStatus.SendFailure, true)]
    [InlineData(WebExceptionStatus.UnknownError, false)]
    public void ShouldRetry_ShouldReturn_OnWebExceptions(WebExceptionStatus status, bool expectedResult)
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.Equal(expectedResult, retry.ShouldRetry(0, new WebException("", status), out _));
    }

    [Theory]
    [InlineData(WebExceptionStatus.Timeout, true)]
    [InlineData(WebExceptionStatus.ConnectionClosed, true)]
    [InlineData(WebExceptionStatus.SendFailure, true)]
    [InlineData(WebExceptionStatus.UnknownError, false)]
    public void ShouldRetry_ShouldReturn_OnHttpRequestExceptions(WebExceptionStatus status, bool expectedResult)
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.Equal(expectedResult, retry.ShouldRetry(0, new HttpRequestException("", new WebException("", status)), out _));
    }

    [Theory]
    [InlineData((int)SocketError.ConnectionRefused, true)]
    [InlineData((int)SocketError.TimedOut, true)]
    [InlineData((int)SocketError.AccessDenied, false)]
    public void ShouldRetry_ShouldReturn_OnSocketException(int errorCode, bool expectedResult)
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.Equal(expectedResult, retry.ShouldRetry(0, new SocketException(errorCode), out _));
    }

    [Theory]
    [InlineData((int)SocketError.ConnectionRefused, true)]
    [InlineData((int)SocketError.TimedOut, true)]
    [InlineData((int)SocketError.AccessDenied, false)]
    public void ShouldRetry_ShouldReturn_OnSocketException_InHttpRequestException(int errorCode, bool expectedResult)
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.Equal(expectedResult, retry.ShouldRetry(0, new HttpRequestException("", new SocketException(errorCode)), out _));
    }

    [Fact]
    public void ShouldRetry_ShouldReturn_OnTimeoutException()
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.True(retry.ShouldRetry(0, new TimeoutException(), out _));
    }

    [Fact]
    public void ShouldRetry_ShouldReturn_OnIOException()
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.True(retry.ShouldRetry(0, new IOException(), out _));
    }

    [Fact]
    public void ShouldRetry_ShouldReturn_OnJsonException()
    {
        var retry = new ExponentialRetry(default(TimeSpan), 3);

        Assert.False(retry.ShouldRetry(0, new JsonSerializationException(), out _));
    }

    [Theory]
    [InlineData(0, 3, 3)]
    [InlineData(1, 6.2, 7.8)]
    [InlineData(2, 12.6, 17.4)]
    public void ShouldRetry_ShouldReturn_ExponentiallyIncreasingRetryInterval(uint currentRetryCount, double min, double max)
    {
        var retry = new ExponentialRetry(TimeSpan.FromSeconds(4), 3);

        retry.ShouldRetry(currentRetryCount, HttpStatusCode.RequestTimeout, out var retryInterval);

        Assert.InRange(retryInterval.TotalSeconds, min, max);
    }

    [Fact]
    public void ShouldThrowException_OnNegativeDeltaBackoff()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialRetry(TimeSpan.FromSeconds(-4), 3));
    }
}
