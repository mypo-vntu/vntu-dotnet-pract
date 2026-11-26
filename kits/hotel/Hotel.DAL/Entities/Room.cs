namespace Hotel.DAL.Entities;

public class Room
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    /// <summary>Номер кімнати ("101"), унікальний. Природний ключ.</summary>
    public string Number { get; set; } = string.Empty;
    public int Floor { get; set; }
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }

    public List<BookingRoom> BookingRooms { get; set; } = [];
}
