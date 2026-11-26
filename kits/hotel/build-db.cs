// Створює kits/hotel/app.db з моделі Hotel.DAL і заповнює тестовими даними.
// Запуск з кореня репозиторію:  dotnet run kits/hotel/build-db.cs
// Інша дата відліку:            dotnet run kits/hotel/build-db.cs -- 2027-02-01
#:project Hotel.DAL/Hotel.DAL.csproj
#:package Microsoft.Extensions.Identity.Core@10.0.15
#:package Bogus@35.6.5
#:property PublishAot=false

using System.Runtime.CompilerServices;
using Bogus;
using Hotel.DAL.Data;
using Hotel.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

Randomizer.Seed = new Random(42);
var f = new Faker("uk");
var today = args.Length > 0 ? DateOnly.Parse(args[0]) : DateOnly.FromDateTime(DateTime.UtcNow);
var now = today.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
var dbPath = Path.Combine(ScriptDir(), "app.db");

using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options);
db.Database.EnsureDeleted();
db.Database.EnsureCreated();

// Ролі й користувачі
var roles = new[] { RoleNames.Admin, RoleNames.Staff, RoleNames.Customer }.ToDictionary(n => n, n => new Role { Name = n });
var hasher = new PasswordHasher<User>();
User NewUser(string email, string password, string role)
{
    var u = new User { Email = email, Role = roles[role], CreatedAt = now.AddDays(-60) };
    u.PasswordHash = hasher.HashPassword(u, password);
    return u;
}
var customerUsers = new[] { NewUser("customer@example.com", "Customer123!", RoleNames.Customer), NewUser("customer2@example.com", "Customer123!", RoleNames.Customer) };
db.Users.AddRange(NewUser("admin@example.com", "Admin123!", RoleNames.Admin), NewUser("staff@example.com", "Staff123!", RoleNames.Staff));
db.Users.AddRange(customerUsers);

// Каталог: тип номера -> (базова ціна, місткість)
var types = new (string Name, decimal Price, int Capacity)[]
{
    ("Стандарт", 1400m, 2), ("Покращений", 1900m, 2), ("Сімейний", 2600m, 4), ("Люкс", 4200m, 3),
};
var rooms = new List<Room>();
foreach (var (name, price, capacity) in types)
{
    var type = new RoomType { Name = name, Description = $"Номери категорії «{name}»" };
    for (var i = 0; i < 4; i++)
    {
        var floor = f.Random.Int(1, 5);
        rooms.Add(new Room
        {
            RoomType = type,
            Floor = floor,
            Number = $"{floor}{rooms.Count + 1:00}",
            PricePerNight = price + f.Random.Int(0, 4) * 100m,
            Capacity = capacity,
        });
    }
}
db.RoomTypes.Add(new RoomType { Name = "Апартаменти", Description = "Поки без номерів" });
db.Rooms.AddRange(rooms);

// Гості: перші два прив'язані до customer@ і customer2@
var guests = new Faker<Guest>("uk")
    .RuleFor(g => g.FullName, x => x.Name.FullName())
    .RuleFor(g => g.Phone, x => x.Phone.PhoneNumber("+38067#######"))
    .Generate(8);
guests[0].User = customerUsers[0];
guests[1].User = customerUsers[1];
db.Guests.AddRange(guests);

// Бронювання: 1–2 номери, без перетину активних бронювань одного номера.
// Статус випливає з дат відносно сьогодні; частина бронювань скасована.
var bookings = new List<Booking>();
bool Overlaps(Room room, DateOnly from, DateOnly to) => bookings.Any(b =>
    b.Status != BookingStatus.Cancelled && b.Rooms.Any(r => r.Room == room) && b.CheckIn < to && from < b.CheckOut);

while (bookings.Count < 18)
{
    var checkIn = today.AddDays(f.Random.Int(-40, 50));
    var checkOut = checkIn.AddDays(f.Random.Int(1, 6));
    var picked = f.PickRandom(rooms, f.Random.Bool(0.25f) ? 2 : 1).ToList();
    var cancelled = f.Random.Bool(0.15f);
    if (!cancelled && picked.Any(r => Overlaps(r, checkIn, checkOut)))
    {
        continue;
    }

    var status = cancelled ? BookingStatus.Cancelled
        : checkOut <= today ? BookingStatus.CheckedOut
        : checkIn <= today ? BookingStatus.CheckedIn
        : BookingStatus.Booked;
    bookings.Add(new Booking
    {
        Guest = f.PickRandom(guests),
        CheckIn = checkIn,
        CheckOut = checkOut,
        CreatedAt = checkIn.AddDays(-f.Random.Int(3, 30)).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
        Status = status,
        Rooms = picked.Select(r => new BookingRoom { Room = r, GuestsCount = f.Random.Int(1, r.Capacity), PricePerNight = r.PricePerNight }).ToList(),
    });
}
db.Bookings.AddRange(bookings);

db.SaveChanges();
Console.WriteLine($"OK: {dbPath}");
Console.WriteLine($"Номерів {db.Rooms.Count()}, гостей {db.Guests.Count()}, бронювань {db.Bookings.Count()}, номерів у бронюваннях {db.BookingRooms.Count()}, користувачів {db.Users.Count()}");

static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
