# Data kit для ПР №5–8

Готові дані для наскрізного мікропроєкту. Кожен kit містить проєкт DAL (сутності й `AppDbContext`) та заповнену базу SQLite `app.db`. Схему бази студенти не створюють і не змінюють.

| Варіант | Папка | Проєкт DAL |
|---|---|---|
| 1. Інтернет-магазин | `shop/` | `Shop.DAL` |
| 2. Готель | `hotel/` | `Hotel.DAL` |
| 3. Автосервіс | `car-service/` | `CarService.DAL` |
| 4. Доставка їжі | `food-delivery/` | `FoodDelivery.DAL` |

## Підключення (ПР №5)

1. Додати проєкт `*.DAL` у своє рішення і посилання на нього з API-проєкту. Пакети EF Core підтягнуться разом із посиланням.
2. Скопіювати `app.db` у папку API-проєкту.
3. Додати рядок підключення в `appsettings.json`: `"ConnectionStrings": { "Default": "Data Source=app.db" }`.
4. Зареєструвати контекст у `Program.cs`: `builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));`

Якщо дані зіпсовано, досить скопіювати `app.db` заново.

## Користувачі в базі

Однакові в усіх варіантах. Паролі навчальні, для реальних систем так не можна.

| Email | Пароль | Роль | Профіль |
|---|---|---|---|
| `admin@example.com` | `Admin123!` | `Admin` | — |
| `staff@example.com` | `Staff123!` | `Staff` | — |
| `customer@example.com` | `Customer123!` | `Customer` | профіль з `Id = 1` |
| `customer2@example.com` | `Customer123!` | `Customer` | профіль з `Id = 2` |

Два клієнти потрібні, щоб перевірити в ПР №7, що `Customer` не бачить чужих записів.

## Що вже налаштовано в `AppDbContext`

- **Пошук кирилицею.** Вбудовані `lower()`/`upper()` у SQLite працюють лише з латиницею. `SqliteUnicodeInterceptor` замінює їх, тому `x.Name.ToLower().Contains(search.ToLower())` знаходить і «Ноутбук», і «ноутбук».
- **`decimal` зберігається як `REAL`.** Інакше EF порівнює й сумує `decimal` через власні функції, які падають на системі з українськими регіональними налаштуваннями (кома як десятковий роздільник).
- **Дати в UTC.** Усі `DateTime` повертаються з `Kind = Utc`. Дати заїзду й виїзду в готелі — `DateOnly`.
- **Видалення.** Групу з елементами або елемент, що використовується в операціях, видалити не можна: база поверне помилку. Позиції видаляються разом з операцією.
- **Статуси** зберігаються рядками (`"New"`, `"Confirmed"` …), їх видно в DB Browser for SQLite.

## Для викладача: перегенерація баз

Бази створюються скриптами `build-db.cs` (.NET 10, запуск без `.csproj`) з даними від [Bogus](https://github.com/bchavez/Bogus) із фіксованим seed. Не редагуйте `app.db` вручну: змініть сутності або скрипт і перегенеруйте.

```bash
dotnet run kits/shop/build-db.cs
dotnet run kits/hotel/build-db.cs
dotnet run kits/car-service/build-db.cs
dotnet run kits/food-delivery/build-db.cs
```

Дати в даних рахуються від дня запуску (бронювання в минулому й майбутньому, замовлення за останній місяць). Перед початком семестру перегенеруйте бази з потрібною датою: `dotnet run kits/hotel/build-db.cs -- 2027-02-01`.

Під час публікації сайту CI пакує кожен kit у `kits/<варіант>.zip` без `build-db.cs`. Посилання для студентів: `https://mypo-vntu.github.io/vntu-dotnet-pract/kits/<варіант>.zip`.
