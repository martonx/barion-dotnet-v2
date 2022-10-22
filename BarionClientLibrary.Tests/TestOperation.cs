using System.Runtime.Serialization;

namespace BarionClientLibrary.Tests;

internal class TestOperation : BarionOperation
{
    public override HttpMethod Method => MethodReturns;

    public HttpMethod MethodReturns { get; set; }

    public override Uri RelativeUri => RelativeUriReturns;

    public Uri RelativeUriReturns { get; set; }

    public override Type ResultType => ResultTypeReturns;

    public Type ResultTypeReturns { get; set; }

    public CultureInfo TestCultureInfo { get; set; }
}

internal class TestOperationWithEnum : TestOperation
{
    public ConsoleColor Color { get; set; }
}