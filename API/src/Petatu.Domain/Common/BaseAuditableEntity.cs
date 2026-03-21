namespace Petatu.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset CreationDate { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public DateTimeOffset? LastModifiedDate { get; set; }
    
    public string? LastModifiedBy { get; set; }
    
    public string IsDeletedBy { get; set; }
    
    public DateTimeOffset? DeletionDate { get; set; }
}
