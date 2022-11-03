using BarionClientLibrary;
using BarionClientLibrary.Operations.Common;
using BarionClientLibrary.Operations.Enums;
using BarionClientLibrary.Operations.PaymentState;
using BarionClientLibrary.Operations.StartPayment;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<BarionClient>();
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddJsonFile("appsettings.json", false, true);
});
builder.Services.Configure<BarionSettings>(builder.Configuration.GetSection("Barion"));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/startpayment", async (BarionClient client, IOptions<BarionSettings> options) =>
{
    var startPaymentOperation = new StartPaymentOperation
    {
        GuestCheckOut = true,
        PaymentRequestId = "P1",
        OrderNumber = "1_0",
        Currency = Currency.HUF,
        CallbackUrl = "http://index.hu",
        Locale = CultureInfo.CurrentCulture,
        RedirectUrl = "http://index.hu"
    };

    var transaction = new PaymentTransaction
    {
        Payee = options.Value.Payee,
        POSTransactionId = "T1",
        Total = new decimal(1000),
        Comment = "comment"
    };

    var item = new Item
    {
        Name = "Test",
        Description = "Test",
        ItemTotal = new decimal(1000),
        Quantity = 1,
        Unit = "piece",
        UnitPrice = new decimal(1000),
        SKU = "SKU"
    };

    transaction.Items = new[] { item };
    startPaymentOperation.Transactions = new[] { transaction };
    var result = await client.ExecuteAsync<StartPaymentOperationResult>(startPaymentOperation);

    return result;
})
.WithName("StartPayment")
.WithOpenApi();

app.MapGet("/callback/{paymentId}", async (Guid paymentId, BarionClient client) =>
{
    var getPaymentStateOperation = new GetPaymentStateOperation { PaymentId = paymentId };
    var result = await client.ExecuteAsync<GetPaymentStateOperationResult>(getPaymentStateOperation);

    return result.Status;
})
.WithOpenApi();

app.Run();
