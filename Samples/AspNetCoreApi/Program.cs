using BarionClientLibrary;
using Microsoft.Extensions.Options;

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

app.MapGet("/startpayment", (BarionClient client) =>
{
    return "Hello world";
})
.WithName("StartPayment")
.WithOpenApi();

app.MapGet("/callback/{paymentId}", (Guid paymentId, BarionClient client) =>
{
    return new { paymentId };
})
.WithOpenApi();

app.Run();
