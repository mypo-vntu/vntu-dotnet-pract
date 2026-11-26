namespace CarService.DAL.Entities;

/// <summary>Послуга автосервісу.</summary>
public class Service
{
    public int Id { get; set; }

    public int ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    /// <summary>Код послуги ("EN-001"), унікальний. Природний ключ.</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Нормогодини на виконання.</summary>
    public decimal NormHours { get; set; }

    public List<WorkOrderLine> Lines { get; set; } = [];
}
