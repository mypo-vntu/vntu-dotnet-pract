// Створює kits/car-service/app.db з моделі CarService.DAL і заповнює тестовими даними.
// Запуск з кореня репозиторію:  dotnet run kits/car-service/build-db.cs
// Інша дата відліку:            dotnet run kits/car-service/build-db.cs -- 2027-02-01
#:project CarService.DAL/CarService.DAL.csproj
#:package Microsoft.Extensions.Identity.Core@10.0.15
#:package Bogus@35.6.5
#:property PublishAot=false

using System.Runtime.CompilerServices;
using Bogus;
using CarService.DAL.Data;
using CarService.DAL.Entities;
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

// Каталог: категорія -> (префікс коду, послуги)
var catalog = new (string Name, string Code, string[] Items)[]
{
    ("Двигун", "EN", ["Заміна мастила", "Заміна ременя ГРМ", "Діагностика двигуна", "Заміна свічок", "Промивка інжектора"]),
    ("Ходова частина", "SU", ["Розвал-сходження", "Заміна амортизаторів", "Заміна сайлентблоків", "Діагностика ходової"]),
    ("Гальма", "BR", ["Заміна гальмівних колодок", "Заміна гальмівних дисків", "Заміна гальмівної рідини"]),
    ("Шиномонтаж", "TI", ["Сезонна заміна шин", "Балансування коліс", "Ремонт проколу"]),
    ("Електрика", "EL", ["Комп'ютерна діагностика", "Заміна акумулятора", "Ремонт генератора"]),
};
var services = new List<Service>();
foreach (var (name, code, items) in catalog)
{
    var category = new ServiceCategory { Name = name, Description = $"Роботи категорії «{name}»" };
    for (var i = 0; i < items.Length; i++)
    {
        var hours = f.Random.Int(1, 12) * 0.5m;
        services.Add(new Service
        {
            ServiceCategory = category,
            Code = $"{code}-{i + 1:000}",
            Name = items[i],
            NormHours = hours,
            Price = hours * 800m + f.Random.Int(0, 10) * 50m,
        });
    }
}
db.ServiceCategories.Add(new ServiceCategory { Name = "Кузовні роботи", Description = "Поки без послуг" });
db.Services.AddRange(services);

// Клієнти: перші два прив'язані до customer@ і customer2@
var clients = new Faker<Client>("uk")
    .RuleFor(c => c.FullName, x => x.Name.FullName())
    .RuleFor(c => c.Phone, x => x.Phone.PhoneNumber("+38067#######"))
    .Generate(8);
clients[0].User = customerUsers[0];
clients[1].User = customerUsers[1];
db.Clients.AddRange(clients);

// Наряди: одне авто на клієнта. Активним (New/InProgress) може бути лише останній наряд авто,
// тому на один держномер ніколи немає двох активних нарядів.
var cars = new[] { "Toyota Corolla", "Skoda Octavia", "Renault Logan", "Volkswagen Golf", "Hyundai Tucson", "Kia Ceed", "Ford Focus", "Mazda 6" };
var letters = "АВЕІКМНОРСТХ".ToCharArray();
string Plate() => $"{f.PickRandom(letters)}{f.PickRandom(letters)}{f.Random.Int(1000, 9999)}{f.PickRandom(letters)}{f.PickRandom(letters)}";

var workOrders = new List<WorkOrder>();
for (var c = 0; c < clients.Count; c++)
{
    var plate = Plate();
    var count = f.Random.Int(1, 3);
    for (var i = 0; i < count; i++)
    {
        var status = i == count - 1 ? f.PickRandom<WorkOrderStatus>() : f.PickRandom(WorkOrderStatus.Paid, WorkOrderStatus.Cancelled);
        var lines = status == WorkOrderStatus.New && f.Random.Bool(0.4f) ? 0 : f.Random.Int(1, 3);
        workOrders.Add(new WorkOrder
        {
            Client = clients[c],
            CarModel = cars[c],
            PlateNumber = plate,
            CreatedAt = now.AddDays(-(count - i) * 12 + f.Random.Int(0, 5)).AddHours(-f.Random.Int(0, 8)),
            Status = status,
            Lines = f.PickRandom(services, lines)
                .Select(s => new WorkOrderLine { Service = s, Price = s.Price, Comment = f.Random.Bool(0.3f) ? "Погоджено з клієнтом" : null })
                .ToList(),
        });
    }
}
db.WorkOrders.AddRange(workOrders);

db.SaveChanges();
Console.WriteLine($"OK: {dbPath}");
Console.WriteLine($"Послуг {db.Services.Count()}, клієнтів {db.Clients.Count()}, нарядів {db.WorkOrders.Count()}, робіт {db.WorkOrderLines.Count()}, користувачів {db.Users.Count()}");

static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
