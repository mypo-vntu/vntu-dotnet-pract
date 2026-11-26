namespace CarService.DAL.Entities;

/// <summary>Профіль клієнта. Може бути без облікового запису (UserId = null).</summary>
public class Client
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public List<WorkOrder> WorkOrders { get; set; } = [];
}
