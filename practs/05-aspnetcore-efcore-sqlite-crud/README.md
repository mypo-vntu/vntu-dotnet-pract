# Практична робота №5. Основи ASP.NET Core

## Мета роботи

Навчитися створювати Web API з використанням ASP.NET Core, налаштовувати конвеєр обробки запитів і контейнер залежностей, працювати з базою даних через EF Core та реалізовувати повний набір операцій CRUD для двох зв'язаних сутностей використовуючи при цьому базові правила побудови REST API. Перевіряти роботу API через інтерактивну документацію Scalar.

Це перша з чотирьох робіт наскрізного мікропроєкту (ПР №5–8). Варіант, обраний тут, не змінюється до ПР №8, а проєкт з кожною роботою розширюється.

## Загальні завдання (обов'язкові для будь-якого варіанта)

- Створити проєкт ASP.NET Core Web API (.NET 10) з контролерами й OpenAPI, підключити інтерактивну документацію Scalar.
- Завантажити data kit свого варіанта і підключити його: проєкт `*.DAL` додати в рішення, файл `app.db` скопіювати в API-проєкт, зареєструвати `AppDbContext` у контейнері залежностей.
- Описати клас `Program` з методом `Main` (без top-level statements), як у попередніх роботах.
- Для групи й елемента каталогу свого варіанта створити базові DTO (`XxxDto` для відповіді, `SaveXxxDto` для запиту) і мапінг Entity → DTO вручну. Контролери не повертають і не приймають Entity EF напряму.
- Реалізувати два контролери з повним CRUD (отримати список, отримати за id, створити, змінити, видалити) і вкладений маршрут «елементи групи».
- Повертати коректні Http Status Codes: 200, 201 із заголовком `Location`, 204, 404.
- При роботі з базою даних використовувати асинхронні методи.
- Підключити `AddProblemDetails` і `UseExceptionHandler`, щоб необроблена помилка повертала JSON без стека викликів.
- Перевірити всі ендпоінти через Scalar.

Рекомендований підхід у ПР №5–8 — використовувати контролери, а не Minimal API.

## Теоретичні відомості

Нижче — засоби ASP.NET Core та EF Core, які знадобляться для виконання цієї роботи (детальніше про кожен — за посиланням на MS Learn). Приклади наведено на нейтральному домені «Бібліотека»: автори й книги.

### HTTP і Web API

**Web API** — застосунок, що приймає HTTP-запити й повертає дані (зазвичай JSON), а не HTML-сторінки. Клієнтом може бути вебсторінка, мобільний застосунок чи інший сервер. Клієнт і сервер не мають спільної пам'яті й не викликають методи одне одного напряму: усе спілкування відбувається через обмін текстовими повідомленнями за протоколом HTTP.

**HTTP-запит** складається з чотирьох частин:

- **метод (Method)** — що клієнт хоче зробити: `GET` (отримати), `POST` (створити), `PUT` (замінити), `DELETE` (видалити);
- **адреса (URL)** — з чим: шлях до ресурсу (`/api/authors/5`) і, за потреби, query parameters після `?` (`/api/books?year=1949`);
- **заголовки (Headers)** — службова інформація: формат тіла (`Content-Type: application/json`), бажаний формат відповіді, токен автентифікації (ПР №7);
- **тіло (Body)** — дані, що передаються на сервер, наприклад JSON нового автора. У `GET` і `DELETE` тіла зазвичай немає.

**HTTP-відповідь** складається з **коду статусу** (`200 OK`, `404 Not Found` тощо), заголовків і тіла з даними. Ось як виглядає створення автора:

```http
POST /api/authors HTTP/1.1
Host: localhost:7001
Content-Type: application/json

{"fullName": "Іван Франко", "country": "Україна"}
```

```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: https://localhost:7001/api/authors/12

{"id": 12, "fullName": "Іван Франко", "country": "Україна"}
```

Перший рядок запиту — метод і шлях, далі заголовки, порожній рядок і тіло. Перший рядок відповіді — код статусу, далі заголовки, порожній рядок і тіло.

**Ресурс** — сутність, з якою працює API, з власною адресою: `/api/authors` — колекція авторів, `/api/authors/5` — один автор, `/api/authors/5/books` — книги цього автора. За угодами REST адреса називає ресурс іменником у множині, а дію задає HTTP-метод: замість `/api/getAuthors` чи `/api/deleteAuthor?id=5` пишуть `GET /api/authors` і `DELETE /api/authors/5`.

[developer.mozilla.org/uk/docs/Web/HTTP/Overview](https://developer.mozilla.org/uk/docs/Web/HTTP/Overview) · [learn.microsoft.com/azure/architecture/best-practices/api-design](https://learn.microsoft.com/azure/architecture/best-practices/api-design)

### API-методи (ендпоінти) в ASP.NET Core

**Ендпоінт (API-метод)** — пара «HTTP-метод + маршрут», за якою сервер виконує певний код: `GET /api/authors/{id}` — отримати автора, `POST /api/authors` — створити автора. Для клієнта API — це і є набір ендпоінтів; саме їх показує Scalar.

**Як ендпоінт оголошується в ASP.NET Core.** У підході з контролерами ендпоінт — це звичайний публічний метод класу-контролера, який називають **дією (action)**. Атрибути над методом зв'язують його з HTTP: `[HttpGet]`, `[HttpPost]` тощо задають HTTP-метод, рядок в атрибуті — продовження маршруту. Решту робить фреймворк:

```csharp
[ApiController]
[Route("api/authors")]                              // спільна частина маршруту всіх дій контролера
public class AuthorsController : ControllerBase
{
    [HttpGet("{id:int}")]                           // GET /api/authors/5
    public async Task<ActionResult<AuthorDto>> GetById(int id, CancellationToken ct)
    {
        // id = 5 узято з маршруту
        // повернений AuthorDto фреймворк перетворить на JSON у тілі відповіді з кодом 200
    }
}
```

Що відбувається з запитом `GET /api/authors/5`:

1. **Маршрутизація** — ASP.NET Core порівнює метод і шлях запиту з атрибутами всіх дій і знаходить `AuthorsController.GetById`.
2. **Прив'язка параметрів (model binding)** — значення для параметрів методу беруться із запиту: `id` — з маршруту, DTO — з JSON у тілі, прості параметри без місця в маршруті — з рядка запиту. `CancellationToken` фреймворк підставляє сам.
3. **Виконання** — контролер створюється (разом із залежностями з контейнера, наприклад `AppDbContext`), і викликається метод.
4. **Формування відповіді** — повернене значення перетворюється на HTTP-відповідь: об'єкт → `200` з JSON, `NotFound()` → `404`, `CreatedAtAction(...)` → `201` із заголовком `Location`.

Отже, програміст пише звичайний метод C#, а перетворення HTTP-запиту на параметри і результату на HTTP-відповідь бере на себе ASP.NET Core.

**Minimal API** — альтернативний спосіб оголосити той самий ендпоінт без контролера, лямбдою в `Program.cs`: `app.MapGet("/api/authors/{id:int}", async (int id, AppDbContext db) => ...)`. У цьому курсі основний підхід — контролери.

[learn.microsoft.com/aspnet/core/web-api](https://learn.microsoft.com/aspnet/core/web-api/) · [learn.microsoft.com/aspnet/core/fundamentals/apis](https://learn.microsoft.com/aspnet/core/fundamentals/apis)

### Структура застосунку ASP.NET Core

**`WebApplication`** — серце застосунку. Робота з ним має два етапи. Спочатку `WebApplication.CreateBuilder(args)` створює будівельника: через `builder.Services` реєструються служби, через `builder.Configuration` читаються налаштування. Потім `builder.Build()` створює застосунок `app`, на якому налаштовується конвеєр обробки запитів і маршрути, а `app.Run()` запускає вебсервер:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();     // 1. Реєстрація служб

        var app = builder.Build();
        app.MapControllers();                  // 2. Конвеєр і маршрути

        app.Run();                             // 3. Запуск
    }
}
```
[learn.microsoft.com/aspnet/core/fundamentals](https://learn.microsoft.com/aspnet/core/fundamentals/)

### Конвеєр middleware

**Middleware** — компонент, через який проходить кожен запит. Компоненти утворюють ланцюжок (конвеєр): кожен може щось зробити із запитом, передати його далі наступному компоненту і щось зробити з відповіддю на зворотному шляху. Наприклад, `UseHttpsRedirection` перенаправляє запити з HTTP на HTTPS, `UseExceptionHandler` перехоплює помилки, а `MapControllers` в кінці ланцюжка передає запит відповідній дії контролера. Порядок викликів `app.UseXxx()` у `Program.cs` і є порядком конвеєра, тому він важливий: обробник помилок має стояти на початку, щоб перехоплювати помилки всіх наступних компонентів.

[learn.microsoft.com/aspnet/core/fundamentals/middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)

### Dependency Injection

**Dependency Injection** — патерн, що передбачає використання спеціального контейнера для "вирішення залежностей". Такий контейнер сам відповідає за створення всіх необхідних для конструктора параметрів. Клас лише оголошує, що йому потрібно, параметрами конструктора, а контейнер сам передає готові екземпляри. Так контролер отримує `AppDbContext`. Сервіс спочатку реєструють у `builder.Services`, потім вона доступна будь-якому класу, який створює контейнер.

**Service Life Cycle** визначає, як довго живе створений екземпляр:

- **`AddSingleton`** — один екземпляр на весь час роботи застосунку (наприклад, кеш або налаштування);
- **`AddScoped`** — один екземпляр на один HTTP-запит; усі класи в межах запиту отримують той самий об'єкт;
- **`AddTransient`** — новий екземпляр щоразу, коли його запитують.

`AddDbContext` реєструє контекст як Scoped: кожен запит працює зі своїм контекстом, а зміни одного запиту не змішуються зі змінами іншого. `DbContext` не потокобезпечний, тому Singleton для нього неприпустимий.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

public class AuthorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthorsController(AppDbContext db) // контейнер передасть контекст поточного запиту
    {
        _db = db;
    }
}
```
[learn.microsoft.com/aspnet/core/fundamentals/dependency-injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)

### Конфігурація і середовища

**`appsettings.json`** — файл налаштувань застосунку: рядки підключення, рівні журналювання, власні параметри. Читається через `builder.Configuration`; для `Connection Strings` є окремий метод `GetConnectionString("Default")`, що бере значення з розділу `ConnectionStrings`.

**Середовище (Environment)** (`Development`, `Production`) задається змінною `ASPNETCORE_ENVIRONMENT`. Під час запуску з IDE вона береться з `Properties/launchSettings.json` і дорівнює `Development`. Значення з `appsettings.Development.json` доповнюють і перекривають `appsettings.json`, а в коді середовище перевіряють через `app.Environment.IsDevelopment()`. Так, наприклад, документацію API показують лише для `DEV Environment`.

**SQL-запити в журналі** — щоб бачити, який SQL генерує EF Core, у розділ `Logging:LogLevel` додають категорію `"Microsoft.EntityFrameworkCore.Database.Command": "Information"`. Це найпростіший спосіб зрозуміти, що саме відбувається з базою після виклику методу контролера.

**Options pattern** *(за бажанням)* — розділ конфігурації можна прив'язати до класу (`builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"))`) і отримувати його в конструкторі як `IOptions<ApiSettings>`. У ПР №7 так налаштовуватимуться параметри JWT.

[learn.microsoft.com/aspnet/core/fundamentals/configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)

### Контролери та маршрутизація

**Контролер** — клас, що успадковує `ControllerBase` і групує методи (дії) одного ресурсу. Атрибут **`[ApiController]`** вмикає поведінку, потрібну для API: автоматичну відповідь 400 на некоректне тіло запиту, автоматичне визначення джерела параметрів і відповіді про помилки у форматі `ProblemDetails`.

**Атрибутна маршрутизація** — адреса задається атрибутами. `[Route("api/authors")]` на класі задає спільний префікс, а `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` на методах — HTTP-метод і продовження шляху. **Обмеження маршруту** `{id:int}` пропускає лише цілі числа: запит `/api/authors/abc` не дійде до методу і поверне 404.

**Джерела параметрів** — `[ApiController]` визначає їх сам: прості типи, що є в шаблоні маршруту, беруться з маршруту, решта простих — з рядка запиту, складні типи (DTO) — з тіла запиту. Явно джерело вказують атрибутами `[FromRoute]`, `[FromQuery]`, `[FromBody]`, `[FromHeader]`:

```csharp
[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    [HttpGet("{id:int}")]                       // GET /api/authors/5
    public async Task<ActionResult<AuthorDto>> GetById(int id, CancellationToken ct) { ... }

    [HttpGet("{id:int}/books")]                 // GET /api/authors/5/books
    public async Task<ActionResult<List<BookDto>>> GetBooks([FromRoute] int id, CancellationToken ct) { ... }

    [HttpPost]                                  // POST /api/authors, DTO — з тіла запиту
    public async Task<ActionResult<AuthorDto>> Create(SaveAuthorDto dto, CancellationToken ct) { ... }
}
```
[learn.microsoft.com/aspnet/core/web-api](https://learn.microsoft.com/aspnet/core/web-api/) · [learn.microsoft.com/aspnet/core/mvc/controllers/routing](https://learn.microsoft.com/aspnet/core/mvc/controllers/routing)

### Результати виконання API (ActionResult) і коди статусу

**`ActionResult<T>`** — тип результату, що дозволяє повернути або дані типу `T` (буде 200 OK з JSON), або інший результат: `NotFound()`, `NoContent()` тощо. Коди статусу для CRUD:

| Операція | Метод і маршрут | Успіх | Помилка |
|---|---|---|---|
| Список | `GET /api/authors` | `200 OK` + масив | — |
| Один запис | `GET /api/authors/5` | `200 OK` + об'єкт | `404 Not Found` |
| Створення | `POST /api/authors` | `201 Created` + об'єкт + заголовок `Location` | `400 Bad Request` |
| Зміна | `PUT /api/authors/5` | `204 No Content` | `404`, `400` |
| Видалення | `DELETE /api/authors/5` | `204 No Content` | `404` |

**`CreatedAtAction`** — формує відповідь 201: тіло з новим записом і заголовок `Location` з адресою, за якою цей запис можна отримати, наприклад `Location: https://localhost:7001/api/authors/12`. Першим аргументом передається назва методу, що повертає один запис, другим — значення параметрів його маршруту: `CreatedAtAction(nameof(GetById), new { id = author.Id }, dto)`.

[learn.microsoft.com/aspnet/core/web-api/action-return-types](https://learn.microsoft.com/aspnet/core/web-api/action-return-types)

### EF Core і готова база даних

**EF Core** — ORM: працює з таблицями бази як із колекціями об'єктів C#. Схему бази й дані в цій роботі не створюють: вони вже є в data kit. Kit містить класи сутностей (`Entities/`) і контекст `AppDbContext` (`Data/`), налаштований під SQLite. Змінювати kit не потрібно.

**`DbContext` і `DbSet<T>`** — контекст представляє сеанс роботи з базою, а кожна властивість `DbSet<T>` — таблицю: `_db.Authors`, `_db.Books`. Запит LINQ до `DbSet` (LINQ to Entities) EF Core перекладає на SQL і виконує на сервері бази. Запит, як і LINQ to Objects у ПР №4, відкладений: SQL виконується лише під час виклику `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync` тощо.

**Основні асинхронні методи:**

- `ToListAsync(ct)` — виконати запит і отримати список;
- `FindAsync([id], ct)` — знайти запис за первинним ключем (або `null`);
- `FirstOrDefaultAsync(x => ..., ct)` — перший запис за умовою (або `null`);
- `AnyAsync(x => ..., ct)` — чи існує хоча б один запис;
- `Add`, `Remove` — позначити запис для додавання чи видалення (без звернення до бази);
- `SaveChangesAsync(ct)` — записати всі позначені зміни в базу однією транзакцією.

**Відстеження змін** — сутність, отриману з контексту, EF Core відстежує. Щоб змінити запис, досить отримати його, присвоїти нові значення властивостям і викликати `SaveChangesAsync`: EF Core сам згенерує `UPDATE` лише для змінених стовпців.

```csharp
var author = await _db.Authors.FindAsync([id], ct);
if (author is null)
{
    return NotFound();
}

author.FullName = dto.FullName;   // зміна відстежується
await _db.SaveChangesAsync(ct);   // UPDATE Authors SET FullName = ... WHERE Id = ...
return NoContent();
```

**Асинхронність і `CancellationToken`** — запит до бази є I/O-операцією, тому, як і в ПР №4, використовуються асинхронні методи: поки база відповідає, потік обслуговує інші запити. Параметр `CancellationToken ct` у методі контролера ASP.NET Core заповнює сам; якщо клієнт закрив з'єднання, токен скасовується, і EF Core перериває запит до бази.

[learn.microsoft.com/ef/core](https://learn.microsoft.com/ef/core/) · [learn.microsoft.com/ef/core/querying](https://learn.microsoft.com/ef/core/querying/) · [learn.microsoft.com/ef/core/saving/basic](https://learn.microsoft.com/ef/core/saving/basic)

### DTO

**DTO (Data Transfer Object)** — простий клас або `record`, що описує, які дані API віддає чи приймає. Повертати сутності EF напряму не можна з трьох причин:

- **цикли під час серіалізації** — автор має список книг, кожна книга посилається на автора; серіалізатор JSON «ходить по колу» і завершується помилкою;
- **зайві дані** — разом із сутністю клієнт отримає все, що в ній є, зокрема службові поля;
- **надмірне прийняття (over-posting)** — якщо метод приймає сутність, клієнт може передати в тілі будь-яке її поле, наприклад `Id` чи зовнішній ключ, який змінювати не можна.

У цій роботі DTO **базові**: вони повторюють поля сутності без навігаційних властивостей. DTO відповіді містить `Id`, DTO запиту — ні, бо ідентифікатор призначає база. У ПР №6 їх замінять DTO під конкретні ендпоінти.

```csharp
public record AuthorDto(int Id, string FullName, string? Country);
public record SaveAuthorDto(string FullName, string? Country);
```

**Мапінг** — перетворення сутності на DTO. Його пишуть вручну методами розширення, щоб код контролерів лишався коротким. Метод `ToDto()` можна викликати й у `Select` запиту до бази: EF Core виконає SQL, а метод застосує до отриманих рядків.

[learn.microsoft.com/aspnet/core/tutorials/first-web-api#prevent-over-posting](https://learn.microsoft.com/aspnet/core/tutorials/first-web-api#prevent-over-posting)

### Обробка помилок: ProblemDetails

**ProblemDetails** (RFC 9457) — стандартний JSON-формат опису помилки HTTP-API з полями `type`, `title`, `status`, `detail`. Завдяки йому клієнт обробляє всі помилки однаково.

**`AddProblemDetails` + `UseExceptionHandler`** — перший виклик реєструє служби формування ProblemDetails, другий додає на початок конвеєра middleware, що перехоплює необроблений виняток, записує його в журнал і повертає клієнту `500` у форматі ProblemDetails, без стека викликів і внутрішніх деталей. Відповіді `NotFound()` з контролера з `[ApiController]` теж отримують тіло ProblemDetails.

```csharp
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```

У цій роботі деякі помилки ще не обробляються свідомо: створення елемента з неіснуючою групою чи видалення групи, в якій є елементи, порушує зовнішній ключ і дає `500`. Перевірки й правильні коди для таких ситуацій додає ПР №6.

[learn.microsoft.com/aspnet/core/fundamentals/error-handling-api](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api)

### OpenAPI і Scalar

**OpenAPI** — стандартний машиночитаний опис API: які є ендпоінти, які параметри вони приймають і що повертають. У .NET 10 опис генерує вбудований пакет `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` + `MapOpenApi`), і шаблон проєкту вже містить ці виклики. Опис доступний за адресою `/openapi/v1.json`.

**Scalar** — інтерактивна сторінка документації за цим описом: список ендпоінтів, моделі DTO і кнопка надсилання запиту прямо з браузера. Пакет `Scalar.AspNetCore`, виклик `app.MapScalarApiReference()`, адреса `/scalar`.

[learn.microsoft.com/aspnet/core/fundamentals/openapi/overview](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview) · [github.com/scalar/scalar](https://github.com/scalar/scalar)

### Типові помилки

- **`no such table: ...`** — застосунок не знайшов `app.db` і SQLite створив новий порожній файл. Відносний шлях `Data Source=app.db` відраховується від папки API-проєкту: файл має лежати поруч із `Program.cs`.
- **Пропущений `await`** — метод повертає `Task` замість даних або завершується раніше, ніж збережено зміни. Компілятор попереджає про це (CS4014), попередження не можна ігнорувати.
- **Повернення сутності замість DTO** — помилка циклічного посилання під час серіалізації або зайві дані у відповіді.
- **`return Ok()` після створення** — для `POST` очікується `201` із заголовком `Location`, тобто `CreatedAtAction`.
- **Синхронні методи EF Core** (`ToList`, `SaveChanges`) у контролері — блокують потік на час звернення до бази.

## Приклади коду (для орієнтиру — не є готовим розв'язком варіанта)

Фрагменти показують технічні прийоми на нейтральному домені «Бібліотека»: група каталогу — `Author` (автор), елемент — `Book` (книга, `AuthorId` → `Author`). Структура kit бібліотеки така сама, як у kit варіантів, тому код переноситься на свій варіант заміною назв.

```csharp
// Сутності з kit бібліотеки (Library.DAL/Entities) — для довідки, змінювати не потрібно
public class Author
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public List<Book> Books { get; set; } = [];
}

public class Book
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public int TotalCopies { get; set; }
}
```

### 1. Підключення data kit і `Program.cs`

Після підключення kit рішення має такий вигляд:

```
Library.sln
├── Library.Api/                 ← ваш проєкт Web API
│   ├── Controllers/
│   ├── Dtos/
│   ├── app.db                   ← скопійовано з kit
│   ├── appsettings.json
│   └── Program.cs
└── Library.DAL/                 ← проєкт з kit, не змінюється
    ├── Data/AppDbContext.cs
    └── Entities/
```

Рядок підключення в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=app.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Реєстрація служб і конвеєр у `Program.cs`. Порядок у конвеєрі: спочатку обробник помилок, потім решта middleware, у кінці — маршрути контролерів.

```csharp
using Library.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Library.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}
```

### 2. Базові DTO і мапінг

DTO відповіді повторює поля сутності без навігаційних властивостей; у книги замість об'єкта `Author` є лише `AuthorId`. DTO запиту не має `Id`.

```csharp
using Library.DAL.Entities;

namespace Library.Api.Dtos;

public record AuthorDto(int Id, string FullName, string? Country);
public record SaveAuthorDto(string FullName, string? Country);

public record BookDto(int Id, int AuthorId, string Isbn, string Title, int Year, int TotalCopies);
public record SaveBookDto(int AuthorId, string Isbn, string Title, int Year, int TotalCopies);

public static class MappingExtensions
{
    public static AuthorDto ToDto(this Author author) =>
        new(author.Id, author.FullName, author.Country);

    public static BookDto ToDto(this Book book) =>
        new(book.Id, book.AuthorId, book.Isbn, book.Title, book.Year, book.TotalCopies);
}
```

### 3. Читання: список, запис за id, вкладений маршрут

Вкладений маршрут `GET /api/authors/{id}/books` має розрізняти дві ситуації: автора не існує (`404`) і автор існує, але книг у нього немає (`200` з порожнім масивом).

```csharp
[HttpGet]
public async Task<ActionResult<List<AuthorDto>>> GetAll(CancellationToken ct)
{
    return await _db.Authors
        .OrderBy(a => a.FullName)
        .Select(a => a.ToDto())
        .ToListAsync(ct);
}

[HttpGet("{id:int}")]
public async Task<ActionResult<AuthorDto>> GetById(int id, CancellationToken ct)
{
    var author = await _db.Authors.FindAsync([id], ct);
    return author is null ? NotFound() : author.ToDto();
}

[HttpGet("{id:int}/books")]
public async Task<ActionResult<List<BookDto>>> GetBooks(int id, CancellationToken ct)
{
    if (!await _db.Authors.AnyAsync(a => a.Id == id, ct))
    {
        return NotFound();
    }

    return await _db.Books
        .Where(b => b.AuthorId == id)
        .OrderBy(b => b.Title)
        .Select(b => b.ToDto())
        .ToListAsync(ct);
}
```

### 4. Запис: створення, зміна, видалення

`POST` створює сутність із DTO запиту, зберігає її (після `SaveChangesAsync` у неї з'являється `Id`) і повертає `201`. `PUT` і `DELETE` спочатку шукають запис і повертають `404`, якщо його немає.

```csharp
[HttpPost]
public async Task<ActionResult<AuthorDto>> Create(SaveAuthorDto dto, CancellationToken ct)
{
    var author = new Author { FullName = dto.FullName, Country = dto.Country };
    _db.Authors.Add(author);
    await _db.SaveChangesAsync(ct);

    return CreatedAtAction(nameof(GetById), new { id = author.Id }, author.ToDto());
}

[HttpPut("{id:int}")]
public async Task<IActionResult> Update(int id, SaveAuthorDto dto, CancellationToken ct)
{
    var author = await _db.Authors.FindAsync([id], ct);
    if (author is null)
    {
        return NotFound();
    }

    author.FullName = dto.FullName;
    author.Country = dto.Country;
    await _db.SaveChangesAsync(ct);
    return NoContent();
}

[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id, CancellationToken ct)
{
    var author = await _db.Authors.FindAsync([id], ct);
    if (author is null)
    {
        return NotFound();
    }

    _db.Authors.Remove(author);
    await _db.SaveChangesAsync(ct);
    return NoContent();
}
```

Для методів без даних у відповіді (`PUT`, `DELETE`) тип результату — `IActionResult`, бо повертати `T` вони не можуть.

### 5. Повний скелет — як усе з'єднати разом

Повний контролер книг. Разом із `Program.cs` (приклад №1) і DTO (приклад №2) він утворює робочий проєкт. Контролер авторів збирається з прикладів №3 і №4 так само.

```csharp
using Library.Api.Dtos;
using Library.DAL.Data;
using Library.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly AppDbContext _db;

    public BooksController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/books
    [HttpGet]
    public async Task<ActionResult<List<BookDto>>> GetAll(CancellationToken ct)
    {
        return await _db.Books
            .OrderBy(b => b.Title)
            .Select(b => b.ToDto())
            .ToListAsync(ct);
    }

    // GET /api/books/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken ct)
    {
        var book = await _db.Books.FindAsync([id], ct);
        return book is null ? NotFound() : book.ToDto();
    }

    // POST /api/books
    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(SaveBookDto dto, CancellationToken ct)
    {
        var book = new Book
        {
            AuthorId = dto.AuthorId,
            Isbn = dto.Isbn,
            Title = dto.Title,
            Year = dto.Year,
            TotalCopies = dto.TotalCopies,
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct); // неіснуючий AuthorId → помилка БД → 500 (виправляється в ПР №6)

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book.ToDto());
    }

    // PUT /api/books/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveBookDto dto, CancellationToken ct)
    {
        var book = await _db.Books.FindAsync([id], ct);
        if (book is null)
        {
            return NotFound();
        }

        book.AuthorId = dto.AuthorId;
        book.Isbn = dto.Isbn;
        book.Title = dto.Title;
        book.Year = dto.Year;
        book.TotalCopies = dto.TotalCopies;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/books/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var book = await _db.Books.FindAsync([id], ct);
        if (book is null)
        {
            return NotFound();
        }

        _db.Books.Remove(book);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
```

## Порядок виконання

1. Створити рішення і проєкт ASP.NET Core Web API (.NET 10): у Visual Studio — шаблон **ASP.NET Core Web API** з позначками **Use controllers** і **Enable OpenAPI support**, у Rider — шаблон **ASP.NET Core Web API** з типом **Controllers**. Видалити згенерований приклад `WeatherForecast`. Переписати `Program.cs` у вигляді класу `Program` з методом `Main`.
2. Встановити NuGet-пакет `Scalar.AspNetCore`, підключити `MapScalarApiReference`, запустити проєкт і відкрити `/scalar`.
3. Завантажити data kit свого варіанта (посилання у варіанті), додати проєкт `*.DAL` у рішення (**Add → Existing Project**) і посилання на нього з API-проєкту (**Add → Project Reference**). Скопіювати `app.db` у папку API-проєкту.
4. Додати рядок підключення в `appsettings.json` і зареєструвати `AppDbContext` (приклад коду №1). Відкрити `app.db` у DB Browser for SQLite і переглянути таблиці групи й елемента свого варіанта.
5. Підключити `AddProblemDetails` і `UseExceptionHandler` (приклад коду №1).
6. Описати DTO і методи мапінгу для групи й елемента (приклад коду №2).
7. Реалізувати контролер групи: читання і вкладений маршрут (приклад коду №3), потім запис (приклад коду №4).
8. Реалізувати контролер елемента за зразком прикладу коду №5.
9. Перевірити кожен ендпоінт у Scalar, зокрема граничний випадок свого варіанта і відповіді `404`. Переглянути в журналі SQL, який генерує EF Core.
10. Закомітити проєкт у Git. Файл `app.db` можна комітити: він невеликий, і з ним проєкт запускається одразу після клонування.

## Варіанти завдань

Номер варіанта, як і в ПР №1–4, визначається порядковим номером у списку групи. Варіант не змінюється до ПР №8.

У всіх варіантах data kit має однакову будову: група каталогу → елемент каталогу (у цій роботі), профіль клієнта, операція з позиціями (ПР №6), ролі й користувачі (ПР №7). У кожному kit є група без елементів — на ній зручно перевіряти вкладений маршрут.

### Варіант 1. Інтернет-магазин

Каталог інтернет-магазину: категорії товарів і самі товари з артикулом, ціною й залишком на складі. Через API адміністратор магазину веде каталог, а вітрина магазину його читає.

**Дані:** data kit [shop.zip](https://mypo-vntu.github.io/vntu-dotnet-pract/kits/shop.zip), проєкт `Shop.DAL`.

- `Category` — `Id`, `Name` (унікальна), `Description?`;
- `Product` — `Id`, `CategoryId` → `Category`, `Sku` (артикул, унікальний), `Name`, `Description?`, `Price`, `Stock` (залишок), `IsActive`.

**Мінімальний перелік ендпоінтів:**

- `GET /api/categories`, `GET /api/categories/{id}`, `POST /api/categories`, `PUT /api/categories/{id}`, `DELETE /api/categories/{id}`;
- `GET /api/categories/{id}/products` — товари категорії;
- `GET /api/products`, `GET /api/products/{id}`, `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}`.

DTO: `CategoryDto`, `SaveCategoryDto`, `ProductDto`, `SaveProductDto`.

**Рекомендації щодо реалізації:**

- список товарів зручно сортувати за назвою, а товари категорії — за ціною;
- у журналі варто подивитись, який SQL генерує `GET /api/categories/{id}/products`, і порівняти його з LINQ-запитом;
- за бажанням: `GET /api/products/by-sku/{sku}` — пошук за артикулом поруч із пошуком за id; власна служба DI (наприклад, форматування ціни) з поясненням вибраного часу життя.

**Граничний випадок для обробки:** створення або зміна товару з артикулом, який уже має інший товар. Унікальний індекс у базі не дасть зберегти такий запис, і без перевірки клієнт отримає `500`. Перед збереженням треба перевірити `AnyAsync` і повернути `409 Conflict` з поясненням (`Conflict("Товар з артикулом ... уже існує")`).

### Варіант 2. Готель

Номерний фонд готелю: типи номерів і самі номери з поверхом, ціною за ніч і місткістю. Через API адміністратор готелю веде номерний фонд, а сайт готелю показує його гостям.

**Дані:** data kit [hotel.zip](https://mypo-vntu.github.io/vntu-dotnet-pract/kits/hotel.zip), проєкт `Hotel.DAL`.

- `RoomType` — `Id`, `Name` (унікальна), `Description?`;
- `Room` — `Id`, `RoomTypeId` → `RoomType`, `Number` (номер кімнати, унікальний, наприклад `"204"`), `Floor`, `PricePerNight`, `Capacity` (місткість).

**Мінімальний перелік ендпоінтів:**

- `GET /api/room-types`, `GET /api/room-types/{id}`, `POST /api/room-types`, `PUT /api/room-types/{id}`, `DELETE /api/room-types/{id}`;
- `GET /api/room-types/{id}/rooms` — номери цього типу;
- `GET /api/rooms`, `GET /api/rooms/{id}`, `POST /api/rooms`, `PUT /api/rooms/{id}`, `DELETE /api/rooms/{id}`.

DTO: `RoomTypeDto`, `SaveRoomTypeDto`, `RoomDto`, `SaveRoomDto`.

**Рекомендації щодо реалізації:**

- список номерів зручно сортувати за поверхом, а в межах поверху — за номером кімнати (`OrderBy` + `ThenBy`);
- маршрут у кілька слів записується через дефіс (`room-types`), а клас контролера — `RoomTypesController`;
- за бажанням: `GET /api/rooms/by-number/{number}` — пошук за номером кімнати; власна служба DI з поясненням вибраного часу життя.

**Граничний випадок для обробки:** створення або зміна номера з номером кімнати, який уже зайнятий іншим записом. Без перевірки унікальний індекс у базі дасть `500`. Перед збереженням треба перевірити `AnyAsync` (під час зміни — без урахування самого номера, що змінюється) і повернути `409 Conflict` з поясненням.

### Варіант 3. Автосервіс

Прайс-лист автосервісу: категорії робіт і послуги з кодом, ціною й нормогодинами. Через API адміністратор веде прайс-лист, а клієнти бачать перелік послуг і ціни.

**Дані:** data kit [car-service.zip](https://mypo-vntu.github.io/vntu-dotnet-pract/kits/car-service.zip), проєкт `CarService.DAL`.

- `ServiceCategory` — `Id`, `Name` (унікальна), `Description?`;
- `Service` — `Id`, `ServiceCategoryId` → `ServiceCategory`, `Code` (код послуги, унікальний, наприклад `"EN-001"`), `Name`, `Price`, `NormHours` (нормогодини).

**Мінімальний перелік ендпоінтів:**

- `GET /api/service-categories`, `GET /api/service-categories/{id}`, `POST /api/service-categories`, `PUT /api/service-categories/{id}`, `DELETE /api/service-categories/{id}`;
- `GET /api/service-categories/{id}/services` — послуги категорії;
- `GET /api/services`, `GET /api/services/{id}`, `POST /api/services`, `PUT /api/services/{id}`, `DELETE /api/services/{id}`.

DTO: `ServiceCategoryDto`, `SaveServiceCategoryDto`, `ServiceDto`, `SaveServiceDto`.

**Рекомендації щодо реалізації:**

- клас сутності називається `Service`, тому контролер — `ServicesController`; плутанини зі «службами» DI варто уникати в назвах власних класів;
- послуги категорії зручно сортувати за кодом;
- за бажанням: `GET /api/services/by-code/{code}` — пошук за кодом послуги; власна служба DI з поясненням вибраного часу життя.

**Граничний випадок для обробки:** створення або зміна послуги з кодом, який уже має інша послуга. Без перевірки унікальний індекс у базі дасть `500`. Перед збереженням треба перевірити `AnyAsync` і повернути `409 Conflict` з поясненням.

### Варіант 4. Доставка їжі

Меню сервісу доставки: ресторани-партнери та їхні страви з ціною, вагою й ознакою доступності. Через API адміністратор сервісу веде меню, а застосунок доставки показує його клієнтам.

**Дані:** data kit [food-delivery.zip](https://mypo-vntu.github.io/vntu-dotnet-pract/kits/food-delivery.zip), проєкт `FoodDelivery.DAL`.

- `Restaurant` — `Id`, `Name` (унікальна), `Address`, `MinOrderAmount` (мінімальна сума замовлення), `IsOpen`;
- `Dish` — `Id`, `RestaurantId` → `Restaurant`, `Name` (унікальна в межах ресторану), `Description?`, `Price`, `WeightGrams`, `IsAvailable`.

**Мінімальний перелік ендпоінтів:**

- `GET /api/restaurants`, `GET /api/restaurants/{id}`, `POST /api/restaurants`, `PUT /api/restaurants/{id}`, `DELETE /api/restaurants/{id}`;
- `GET /api/restaurants/{id}/dishes` — меню ресторану;
- `GET /api/dishes`, `GET /api/dishes/{id}`, `POST /api/dishes`, `PUT /api/dishes/{id}`, `DELETE /api/dishes/{id}`.

DTO: `RestaurantDto`, `SaveRestaurantDto`, `DishDto`, `SaveDishDto`.

**Рекомендації щодо реалізації:**

- меню ресторану зручно сортувати за ціною;
- у kit є закритий ресторан (`IsOpen = false`) без страв — на ньому зручно перевірити вкладений маршрут;
- за бажанням: `GET /api/restaurants/open` — лише відкриті ресторани (маршрут без параметра має стояти так, щоб не конфліктувати з `{id:int}`); власна служба DI з поясненням вибраного часу життя.

**Граничний випадок для обробки:** створення або зміна страви з назвою, яка вже є в меню **того самого** ресторану (в іншому ресторані така назва дозволена). Без перевірки унікальний індекс у базі дасть `500`. Перед збереженням треба перевірити `AnyAsync` за парою «ресторан + назва» і повернути `409 Conflict` з поясненням.

## Вимоги до звіту

Звіт з практичної роботи оформлюється у вигляді документа (PDF/DOCX) та має містити:

- титульний аркуш;
- тему, мету та номер варіанта завдання;
- короткий опис реалізованого функціоналу та лістинг (`Program.cs`, DTO, контролери);
- таблицю ендпоінтів (метод, маршрут, коди відповіді);
- знімки екрана Scalar: успішне створення (`201` із заголовком `Location`), відповідь `404`, граничний випадок варіанта;
- посилання на репозиторій GitHub;
- висновки (2–4 речення) щодо отриманих результатів.

## Контрольні питання

1. З яких частин складаються HTTP-запит і HTTP-відповідь? Що таке ресурс і ендпоінт?
2. Як оголошується ендпоінт у контролері ASP.NET Core? Що робить фреймворк між отриманням запиту й викликом методу дії?
3. З яких етапів складається запуск застосунку ASP.NET Core? Що відбувається на кожному з них у `Program.cs`?
4. Що таке middleware? Чому порядок викликів `app.UseXxx()` має значення? Де у конвеєрі має стояти `UseExceptionHandler` і чому?
5. Чим відрізняються часи життя служб Singleton, Scoped і Transient? Чому `DbContext` реєструється як Scoped?
6. Як контролер отримує `AppDbContext`, якщо ніде не створює його через `new`?
7. Як працюють `appsettings.json` і `appsettings.Development.json` разом? Як визначається поточне середовище?
8. Що дає атрибут `[ApiController]`? Звідки беруться значення параметрів методу контролера?
9. Що робить обмеження маршруту `{id:int}`? Що поверне запит `/api/books/abc`?
10. Які коди статусу повертають операції CRUD? Що містить заголовок `Location` і як його формує `CreatedAtAction`?
11. Чому методи контролера не повинні приймати й повертати сутності EF напряму? Що таке over-posting?
12. Коли виконується SQL-запит, побудований через LINQ до `DbSet`? Як змінити запис у базі без явного `UPDATE`?
13. Навіщо методу контролера параметр `CancellationToken` і хто передає в нього значення?
14. Що таке ProblemDetails? Що отримає клієнт, якщо в методі контролера виникне необроблений виняток?
