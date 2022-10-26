namespace BarionClientLibrary.EndToEndTests;

public class PaymentTests : IDisposable
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
            BaseUrl = new Uri(config["Barion:BaseUrl"]),
            POSKey = Guid.Parse(config["Barion:POSKey"]),
            Payee = config["Barion:Payee"]
        };
        barionClient = new BarionClient(settings);

        var options = new ChromeOptions();
        //options.AddArguments("--headless");
        driver = new ChromeDriver(options);
        driver.Manage().Window.Maximize();
    }

    public void Dispose()
    {
        driver.Close();
        driver.Quit();
    }

    [Fact]
    public void Test1()
    {
        var paymentResult = Operations.StartPayment(barionClient, settings, PaymentType.Immediate);
        driver.Url = paymentResult.GatewayUrl;
        var cardNumberElement = driver.FindElement(By.Id("CardNumber"));
        cardNumberElement.SendKeys("444488888888");
        cardNumberElement.SendKeys(Keys.End);
        cardNumberElement.SendKeys("5");
        cardNumberElement.SendKeys(Keys.End);
        cardNumberElement.SendKeys("5");
        cardNumberElement.SendKeys(Keys.End);
        cardNumberElement.SendKeys("5");
        cardNumberElement.SendKeys(Keys.End);
        cardNumberElement.SendKeys("9");
        var cardExpirationElement = driver.FindElement(By.Id("CardExpiration"));
        cardExpirationElement.SendKeys("1026");
        var cardCvcElement = driver.FindElement(By.Id("CardCVC"));
        cardCvcElement.SendKeys("100");
        var cardHolderNameElement = driver.FindElement(By.Id("CardHolderName"));
        cardHolderNameElement.SendKeys("Teszt Elek");
        var emailAddressElement = driver.FindElement(By.Id("EmailAddress"));
        emailAddressElement.SendKeys("tesztelek@gmail.com");
        var paymentButtonElement = driver.FindElement(By.Id("StartGuestPayment"));
        paymentButtonElement.Click();

        bool paymentWasSuccessful;
        do
        {
            Thread.Sleep(500);
            var successfulPaymentElement = driver.FindElement(By.Id("SuccessfulPayment"));
            if (successfulPaymentElement.Displayed)
            {
                paymentWasSuccessful = true;
                break;
            }
        } while (true);

        Assert.True(paymentWasSuccessful);
    }
}