namespace BarionClientLibrary.EndToEndTests;

public class PaymentTests
{
    private readonly BarionSettings settings;
    private readonly BarionClient barionClient;
    private IWebDriver driver;

    public PaymentTests()
    {
        var config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json").Build();

        settings = new BarionSettings
        {
            BaseUrl = new Uri(config["Barion:BaseAddress"]),
            POSKey = Guid.Parse(config["Barion:POSKey"]),
            Payee = config["Barion:Payee"]
        };
        barionClient = new BarionClient(settings);

        var options = new ChromeOptions();
        //options.AddArguments("--headless");
        driver = new ChromeDriver(options);
        driver.Manage().Window.Maximize();
    }

    [Fact]
    public void Test1()
    {
        var paymentResult = Operations.StartPayment(barionClient, settings, PaymentType.Immediate);
        driver.Url = paymentResult.GatewayUrl;

        Thread.Sleep(2000);
    }
}