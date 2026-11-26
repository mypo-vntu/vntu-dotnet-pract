namespace Hotel.DAL.Entities;

/// <summary>Профіль гостя. Може бути без облікового запису (UserId = null).</summary>
public class Guest
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public List<Booking> Bookings { get; set; } = [];
}
