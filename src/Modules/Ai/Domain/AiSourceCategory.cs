namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Explicit provenance source category for AI responses and evidence.
/// </summary>
public enum AiSourceCategory
{
    CompanyData = 1,
    CompanyPolicy = 2,
    ProductKnowledge = 3,
    PayrollTrace = 4,
    ExternalAi = 5
}
