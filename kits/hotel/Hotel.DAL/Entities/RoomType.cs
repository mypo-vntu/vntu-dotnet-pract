namespace Hotel.DAL.Entities;

public class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<Room> Rooms { get; set; } = [];
}
