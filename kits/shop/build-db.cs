// Створює kits/shop/app.db з моделі Shop.DAL і заповнює тестовими даними.
// Запуск з кореня репозиторію:  dotnet run kits/shop/build-db.cs
// Інша дата відліку:            dotnet run kits/shop/build-db.cs -- 2027-02-01
#:project Shop.DAL/Shop.DAL.csproj
#:package Microsoft.Extensions.Identity.Core@10.0.15
#:package Bogus@35.6.5
#:property PublishAot=false

using System.Runtime.CompilerServices;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.DAL.Data;
using Shop.DAL.Entities;

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

// Каталог: категорія -> (префікс SKU, назви товарів)
var catalog = new (string Name, string Sku, string[] Items)[]
{
    ("Смартфони", "PH", ["Смартфон", "Кнопковий телефон"]),
    ("Ноутбуки", "LT", ["Ноутбук", "Ультрабук", "Ігровий ноутбук"]),
    ("Навушники", "AU", ["Бездротові навушники", "Накладні навушники"]),
    ("Аксесуари", "AC", ["Зарядний пристрій", "Кабель USB-C", "Павербанк", "Чохол"]),
    ("Побутова техніка", "HM", ["Електрочайник", "Блендер", "Пилосос", "Кавоварка"]),
};
var products = new List<Product>();
foreach (var (name, prefix, items) in catalog)
{
    var category = new Category { Name = name, Description = $"Товари категорії «{name}»" };
    for (var i = 1; i <= 5; i++)
    {
        products.Add(new Product
        {
            Category = category,
            Sku = $"{prefix}-{i:000}",
            Name = $"{f.PickRandom(items)} {f.Random.Replace("??-##").ToUpperInvariant()}",
            Price = Math.Round(f.Random.Decimal(200, 40000), 0),
            Stock = f.Random.Bool(0.15f) ? 0 : f.Random.Int(1, 50),
            IsActive = f.Random.Bool(0.9f),
        });
    }
}
db.Categories.Add(new Category { Name = "Подарункові сертифікати", Description = "Поки без товарів" });
db.Products.AddRange(products);

// Клієнти: перші два прив'язані до customer@ і customer2@
var customers = new Faker<Customer>("uk")
    .RuleFor(c => c.FullName, x => x.Name.FullName())
    .RuleFor(c => c.Phone, x => x.Phone.PhoneNumber("+38067#######"))
    .RuleFor(c => c.DeliveryAddress, x => $"{x.Address.City()}, {x.Address.StreetAddress()}")
    .Generate(8);
customers[0].User = customerUsers[0];
customers[1].User = customerUsers[1];
db.Customers.AddRange(customers);

// Замовлення: 1–3 різні активні товари, усі статуси; одне порожнє New для демонстрації правила
var active = products.Where(p => p.IsActive).ToList();
var orders = new Faker<Order>("uk")
    .RuleFor(o => o.Customer, x => x.PickRandom(customers))
    .RuleFor(o => o.CreatedAt, x => now.AddDays(-x.Random.Int(0, 30)).AddMinutes(-x.Random.Int(0, 600)))
    .RuleFor(o => o.Status, x => x.PickRandom<OrderStatus>())
    .RuleFor(o => o.Items, x => x.PickRandom(active, x.Random.Int(1, 3))
        .Select(p => new OrderItem { Product = p, Quantity = x.Random.Int(1, 3), UnitPrice = p.Price }).ToList())
    .Generate(15);
orders.Add(new Order { Customer = customers[0], CreatedAt = now, Status = OrderStatus.New });
db.Orders.AddRange(orders);

db.SaveChanges();
Console.WriteLine($"OK: {dbPath}");
Console.WriteLine($"Товарів {db.Products.Count()}, клієнтів {db.Customers.Count()}, замовлень {db.Orders.Count()}, позицій {db.OrderItems.Count()}, користувачів {db.Users.Count()}");

static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
