namespace CarService.DAL.Entities;

public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<Service> Services { get; set; } = [];
}
