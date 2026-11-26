namespace FoodDelivery.DAL.Entities;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>Мінімальна сума замовлення, щоб ресторан почав готувати.</summary>
    public decimal MinOrderAmount { get; set; }
    public bool IsOpen { get; set; } = true;

    public List<Dish> Dishes { get; set; } = [];
}
