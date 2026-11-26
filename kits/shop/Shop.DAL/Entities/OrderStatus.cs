namespace Shop.DAL.Entities;

/// <summary>New → Confirmed → Shipped; New або Confirmed → Cancelled.</summary>
public enum OrderStatus
{
    New,
    Confirmed,
    Shipped,
    Cancelled
}
