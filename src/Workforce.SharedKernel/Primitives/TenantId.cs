namespace Workforce.SharedKernel.Primitives;

public readonly record struct TenantId
{
    public Guid Value { get; }

    public TenantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(value));
            
        Value = value;
    }

    public static TenantId New() => new(Guid.NewGuid());
    
    public override string ToString() => Value.ToString();
}
