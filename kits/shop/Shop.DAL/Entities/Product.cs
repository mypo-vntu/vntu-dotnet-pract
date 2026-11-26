namespace Shop.DAL.Entities;

public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Артикул, унікальний. Природний ключ товару.</summary>
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }

    /// <summary>Залишок на складі.</summary>
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = [];
}
