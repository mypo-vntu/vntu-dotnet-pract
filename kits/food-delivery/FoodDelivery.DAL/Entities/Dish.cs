namespace FoodDelivery.DAL.Entities;

public class Dish
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int WeightGrams { get; set; }
    public bool IsAvailable { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = [];
}
