namespace FoodDelivery.DAL.Entities;

/// <summary>Позиція замовлення. UnitPrice — ціна страви на момент додавання.</summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
