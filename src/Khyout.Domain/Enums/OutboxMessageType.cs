namespace Khyout.Domain.Enums;

public enum OutboxMessageType
{
    RfqCreated = 1,
    QuotationSubmitted = 2,
    QuotationAccepted = 3,
    QuotationRejected = 4,
    QuotationExpired = 5,
    SampleRequested = 6,
    SampleStatusChanged = 7
}
