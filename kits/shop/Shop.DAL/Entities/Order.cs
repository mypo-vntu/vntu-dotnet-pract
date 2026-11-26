namespace Shop.DAL.Entities;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    public List<OrderItem> Items { get; set; } = [];
}
