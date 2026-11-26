namespace CarService.DAL.Entities;

/// <summary>Наряд-замовлення на ремонт одного авто.</summary>
public class WorkOrder
{
    public int Id { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string CarModel { get; set; } = string.Empty;

    /// <summary>Держномер у форматі "АВ1234ВК".</summary>
    public string PlateNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.New;

    public List<WorkOrderLine> Lines { get; set; } = [];
}
