using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Warnings;

public class ExpirationWarning : FullAuditedEntity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid PantryItemId { get; private set; }
    public string WarningType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ExpirationDate { get; private set; }

    protected ExpirationWarning() { }

    public ExpirationWarning(
        Guid id,
        Guid userId,
        Guid pantryItemId,
        string warningType,
        DateTime? expirationDate)
        : base(id)
    {
        UserId = userId;
        PantryItemId = pantryItemId;
        WarningType = warningType;
        ExpirationDate = expirationDate;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate(DateTime? newExpirationDate)
    {
        IsActive = true;
        ExpirationDate = newExpirationDate;
    }

    public void UpdateExpirationDate(DateTime? newExpirationDate)
    {
        ExpirationDate = newExpirationDate;
    }
}