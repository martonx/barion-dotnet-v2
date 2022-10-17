var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapGet("/startpayment", () =>
{
    return "Hello world";
})
.WithName("StartPayment")
.WithOpenApi();

app.MapGet("/callback", (Guid PaymentId) =>
{
    return new { PaymentId };
})
.WithOpenApi();

app.Run();
