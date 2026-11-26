namespace CarService.DAL.Entities;

/// <summary>Робота в наряді. Price — ціна послуги на момент додавання.</summary>
public class WorkOrderLine
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public decimal Price { get; set; }
    public string? Comment { get; set; }
}
