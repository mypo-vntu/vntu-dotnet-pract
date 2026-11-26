namespace FoodDelivery.DAL.Entities;

/// <summary>New → Cooking → OnTheWay → Delivered; New → Cancelled.</summary>
public enum OrderStatus
{
    New,
    Cooking,
    OnTheWay,
    Delivered,
    Cancelled
}
