namespace Hotel.DAL.Entities;

/// <summary>Бронювання може містити кілька номерів (сім'я, група).</summary>
public class Booking
{
    public int Id { get; set; }

    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public DateTime CreatedAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Booked;

    public List<BookingRoom> Rooms { get; set; } = [];
}
