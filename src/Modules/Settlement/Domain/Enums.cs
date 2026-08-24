namespace Workforce.Modules.Settlement.Domain;

public enum SettlementStatus
{
    Draft = 1,
    Approved = 2,
    Processing = 3,
    Exported = 4,
    Reconciled = 5
}

public enum PaymentInstructionStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3
}

public enum ExportFormat
{
    NeutralCsv = 1,
    Iso20022Xml = 2,
    WpsStandard = 3
}
