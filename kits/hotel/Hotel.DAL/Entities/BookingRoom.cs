namespace Hotel.DAL.Entities;

/// <summary>Номер у бронюванні. PricePerNight — ціна на момент бронювання.</summary>
public class BookingRoom
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int GuestsCount { get; set; }
    public decimal PricePerNight { get; set; }
}
