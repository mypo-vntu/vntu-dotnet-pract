namespace CarService.DAL.Entities;

/// <summary>New → InProgress → Done → Paid; New або InProgress → Cancelled.</summary>
public enum WorkOrderStatus
{
    New,
    InProgress,
    Done,
    Paid,
    Cancelled
}
