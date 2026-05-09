namespace Auth.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    protected BaseAuditableEntity() { }

    protected BaseAuditableEntity(Guid id) : base(id) { }
}
