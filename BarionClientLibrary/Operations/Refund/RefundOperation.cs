﻿namespace BarionClientLibrary.Operations.Refund;

/// <summary>
/// Represents a refund operation.
/// </summary>
/// <remarks>
/// View the full documentation of the operation here: https://doksi.barion.com/Payment-Refund-v2
/// </remarks>
public class RefundOperation : BarionOperation
{
    public Guid PaymentId { get; set; }
    public TransactionToRefund[] TransactionsToRefund { get; set; }

    public override Uri RelativeUri => new Uri("/v2/Payment/Refund", UriKind.Relative);
    public override HttpMethod Method => HttpMethod.Post;
    public override Type ResultType => typeof(RefundOperationResult);
}
