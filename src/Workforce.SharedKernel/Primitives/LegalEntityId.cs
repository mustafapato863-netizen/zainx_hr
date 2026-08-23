namespace Workforce.SharedKernel.Primitives;

public readonly record struct LegalEntityId
{
    public Guid Value { get; }

    public LegalEntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("LegalEntityId cannot be empty.", nameof(value));
            
        Value = value;
    }

    public static LegalEntityId New() => new(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
}
