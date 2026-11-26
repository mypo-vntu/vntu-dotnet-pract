// Створює kits/food-delivery/app.db з моделі FoodDelivery.DAL і заповнює тестовими даними.
// Запуск з кореня репозиторію:  dotnet run kits/food-delivery/build-db.cs
// Інша дата відліку:            dotnet run kits/food-delivery/build-db.cs -- 2027-02-01
#:project FoodDelivery.DAL/FoodDelivery.DAL.csproj
#:package Microsoft.Extensions.Identity.Core@10.0.15
#:package Bogus@35.6.5
#:property PublishAot=false

using System.Runtime.CompilerServices;
using Bogus;
using FoodDelivery.DAL.Data;
using FoodDelivery.DAL.Entities;
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

// Каталог: ресторан -> (мінімальна сума, страви)
var catalog = new (string Name, decimal Min, string[] Dishes)[]
{
    ("Піцерія Маргарита", 300m, ["Піца Маргарита", "Піца Пепероні", "Піца Чотири сири", "Кальцоне", "Салат Цезар"]),
    ("Суші Сакура", 400m, ["Рол Філадельфія", "Рол Каліфорнія", "Суші з лососем", "Місо-суп", "Едамаме"]),
    ("Бургерна Гриль", 250m, ["Класичний бургер", "Чизбургер", "Бургер з беконом", "Картопля фрі", "Нагетси"]),
    ("Домашня кухня", 200m, ["Борщ", "Вареники з картоплею", "Деруни зі сметаною", "Котлета по-київськи", "Голубці"]),
};
var dishes = new List<Dish>();
var restaurants = new List<Restaurant>();
foreach (var (name, min, items) in catalog)
{
    var restaurant = new Restaurant { Name = name, Address = $"Вінниця, {f.Address.StreetAddress()}", MinOrderAmount = min };
    restaurants.Add(restaurant);
    dishes.AddRange(items.Select(d => new Dish
    {
        Restaurant = restaurant,
        Name = d,
        Price = f.Random.Int(9, 45) * 10m,
        WeightGrams = f.Random.Int(5, 40) * 10,
        IsAvailable = f.Random.Bool(0.85f),
    }));
}
db.Restaurants.Add(new Restaurant { Name = "Кав'ярня Ранок", Address = "Вінниця, вул. Соборна, 1", MinOrderAmount = 150m, IsOpen = false });
db.Dishes.AddRange(dishes);

// Клієнти: перші два прив'язані до customer@ і customer2@
var customers = new Faker<Customer>("uk")
    .RuleFor(c => c.FullName, x => x.Name.FullName())
    .RuleFor(c => c.Phone, x => x.Phone.PhoneNumber("+38067#######"))
    .RuleFor(c => c.DefaultAddress, x => $"Вінниця, {x.Address.StreetAddress()}")
    .Generate(8);
customers[0].User = customerUsers[0];
customers[1].User = customerUsers[1];
db.Customers.AddRange(customers);

// Замовлення: усі страви з одного ресторану. Далі за New проходять лише замовлення
// із сумою не меншою за мінімальну, тому дані не порушують правил ПР №8.
var orders = new List<Order>();
for (var i = 0; i < 16; i++)
{
    var restaurant = f.PickRandom(restaurants);
    var customer = f.PickRandom(customers);
    var items = f.PickRandom(dishes.Where(d => d.Restaurant == restaurant && d.IsAvailable), f.Random.Int(1, 3))
        .Select(d => new OrderItem { Dish = d, Quantity = f.Random.Int(1, 3), UnitPrice = d.Price })
        .ToList();
    var total = items.Sum(x => x.UnitPrice * x.Quantity);
    var status = total < restaurant.MinOrderAmount
        ? f.PickRandom(OrderStatus.New, OrderStatus.Cancelled)
        : f.PickRandom<OrderStatus>();
    orders.Add(new Order
    {
        Customer = customer,
        DeliveryAddress = customer.DefaultAddress,
        CreatedAt = now.AddDays(-f.Random.Int(0, 20)).AddMinutes(-f.Random.Int(0, 600)),
        Status = status,
        Items = items,
    });
}
db.Orders.AddRange(orders);

db.SaveChanges();
Console.WriteLine($"OK: {dbPath}");
Console.WriteLine($"Страв {db.Dishes.Count()}, клієнтів {db.Customers.Count()}, замовлень {db.Orders.Count()}, позицій {db.OrderItems.Count()}, користувачів {db.Users.Count()}");

static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
