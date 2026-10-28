# Практична робота №4. LINQ to Objects та асинхронне програмування

> ⚠️ **WIP** — матеріал ще в розробці, зміст може змінюватись.

## Мета роботи

Навчитися асинхронно отримувати дані з кількох джерел (`async`/`await`, `Task`, `Task.WhenAll`), коректно обробляти збої під час завантаження, а потім аналізувати отримані колекції запитами LINQ to Objects: фільтрувати, сортувати, групувати, агрегувати, з'єднувати колекції та проєктувати результат у потрібну форму. Саме так виглядає робота з даними у веб-застосунках Модуля 2: дочекатися відповіді від джерела, не блокуючи потік, і обробити результат запитом.

## Загальні завдання (обов'язкові для будь-якого варіанта)

- Створити консольний проєкт (.NET 10, Console App), метод `Main` оголосити як `static async Task Main()`.
- Описати дані варіанта типами `record`.
- Реалізувати щонайменше 3 асинхронні методи-«джерела» виду `Task<List<T>> LoadXxxAsync()`, які імітують звернення до віддаленого сервера затримкою `await Task.Delay(...)` (1–2 с, у кожного джерела своя) і повертають записи, заповнені наперед у коді. Сумарно з усіх джерел — 20–30 записів.
- Реалізувати два пункти меню завантаження: **одночасне** (`Task.WhenAll`) і **послідовне** (`await` по черзі), і для кожного вивести час виконання, виміряний `Stopwatch`.
- Передбачити збій одного з джерел: перемикач у меню, після якого джерело кидає виняток. Виняток перехоплюється `try/catch`, програма повідомляє, яке джерело недоступне, і продовжує роботу з даними решти джерел.
- Реалізувати щонайменше 6 різних LINQ-запитів, які разом охоплюють: `Where`, `OrderBy`/`OrderByDescending` + `ThenBy`, `Select` з проєкцією в анонімний тип, `GroupBy` з агрегатними функціями (`Count`, `Sum`, `Average`, `Min`, `Max`), `Join` з другою колекцією варіанта, `Any` або `All`. Щонайменше один запит записати в query-синтаксисі (`from ... where ... select`).
- Хоча б один запит має використовувати параметр, введений з клавіатури з валідацією через `TryParse` (або непорожній рядок).
- Зберегти підсумковий звіт у CSV-файл асинхронно (`File.WriteAllLinesAsync`).
- Передбачити просте меню (цикл із вибором пункту), що дозволяє повторювати дії без перезапуску програми, аж до вибору пункту «Вихід».
- Не використовувати `.Result`, `.Wait()`, `Thread.Sleep` та `async void` (див. «Типові помилки»).

## Теоретичні відомості

Нижче — засоби C# і .NET, які знадобляться в цій роботі (докладніше про кожен — за посиланням на MS Learn). Асинхронне програмування розгорнуто викладено в лекції 6; тут лише те, що потрібно для варіантів.

### Записи (record)

**`record`** — скорочений спосіб оголосити клас для зберігання даних. Один рядок створює конструктор, властивості лише для читання, змістовний `ToString()` і порівняння за значенням (два записи з однаковими полями вважаються рівними). Для колекцій, які аналізують запитами LINQ, це найзручніший вибір (потрібно в усіх варіантах):

```csharp
public record Book(string Branch, string Title, string Author, string Genre, int Year);

var book = new Book("Центральна", "1984", "Джордж Орвелл", "Антиутопія", 1949);
Console.WriteLine(book.Title); // 1984
Console.WriteLine(book);       // Book { Branch = Центральна, Title = 1984, ... }
```
[learn.microsoft.com/dotnet/csharp/fundamentals/types/records](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/records)

### Асинхронне програмування

**Навіщо асинхронність** — значна частина роботи програм полягає не в обчисленнях, а в очікуванні: відповіді сервера, читання файлу, результату запиту до бази даних (**I/O-bound** операції). Синхронний код увесь цей час тримає потік зайнятим. Асинхронний код на час очікування звільняє потік, і той може виконувати іншу роботу. Асинхронність не робить окрему операцію швидшою. Вона дозволяє не марнувати час на очікування і чекати на кілька операцій одночасно.

[learn.microsoft.com/dotnet/csharp/asynchronous-programming](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)

**`Task` і `Task<T>`** — об'єкт, що представляє операцію, яка може ще не завершитися («квиток у гардеробі»: отримали зараз, результат заберете пізніше). `Task` — операція без результату, `Task<T>` — операція, що поверне значення типу `T`. За конвенцією імена асинхронних методів закінчуються на `Async`: так названі й методи .NET (`File.ReadAllTextAsync`, `HttpClient.GetStringAsync`), і методи EF Core в Модулі 2 (`ToListAsync`, `SaveChangesAsync`).

**`async` і `await`** — `async` у сигнатурі дозволяє використовувати `await` у тілі методу. `await` означає «дочекатися завершення задачі, звільнивши потік на час очікування» і «розпаковує» результат: `await` над `Task<List<Book>>` дає `List<Book>`. Асинхронний код читається так само, як звичайний послідовний:

```csharp
static async Task<List<Book>> LoadCentralAsync()
{
    await Task.Delay(2000); // імітація очікування відповіді сервера
    return new List<Book>
    {
        new("Центральна", "1984", "Джордж Орвелл", "Антиутопія", 1949),
    };
}

List<Book> books = await LoadCentralAsync();
```
[learn.microsoft.com/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model)

**`Task.Delay` проти `Thread.Sleep`** — обидва «чекають» заданий час, але `Thread.Sleep` блокує потік, а `await Task.Delay` його відпускає. У цій роботі `Task.Delay` замінює справжній мережевий запит, тому `Thread.Sleep` використовувати не можна.

**Асинхронний `Main`** — щоб використовувати `await` у `Main`, метод оголошують як `static async Task Main()`. Методи пунктів меню, що виконують асинхронні дії, теж стають асинхронними й повертають `Task<bool>` замість `bool`. Асинхронність «поширюється» вгору ланцюжком викликів (*async all the way*).

[learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/main-command-line](https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/main-command-line)

**`Task.WhenAll`: кілька операцій одночасно** — якщо операції не залежать одна від одної, їх запускають усі разом, а потім чекають завершення всіх. Загальний час дорівнює часу найдовшої операції, а не сумі. Різницю зручно показати класом `Stopwatch` (простір імен `System.Diagnostics`):

```csharp
var sw = Stopwatch.StartNew();

// Послідовно: 2 + 1,5 + 1 ≈ 4,5 с
List<Book> a = await LoadCentralAsync();
List<Book> b = await LoadSchoolAsync();
List<Book> c = await LoadUniversityAsync();

Console.WriteLine($"Послідовно: {sw.ElapsedMilliseconds} мс");
sw.Restart();

// Одночасно: усі три задачі стартують одразу, чекаємо найдовшу ≈ 2 с
List<Book>[] parts = await Task.WhenAll(LoadCentralAsync(), LoadSchoolAsync(), LoadUniversityAsync());

Console.WriteLine($"Одночасно: {sw.ElapsedMilliseconds} мс");
```

`Task.WhenAll` приймає і окремі задачі, і колекцію `List<Task<T>>`, а повертає масив результатів у тому самому порядку, в якому передано задачі (знадобиться у варіанті 3, де одне джерело викликається для кількох серверів).

[learn.microsoft.com/dotnet/api/system.threading.tasks.task.whenall](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.whenall) · [learn.microsoft.com/dotnet/api/system.diagnostics.stopwatch](https://learn.microsoft.com/dotnet/api/system.diagnostics.stopwatch)

**Винятки в асинхронному коді** — виняток, що виник усередині асинхронного методу, зберігається в `Task` і кидається повторно в момент `await`. Тому звичайний `try/catch` навколо `await` працює так само, як у синхронному коді. Якщо ж упала одна із задач, переданих у `Task.WhenAll`, то `await Task.WhenAll(...)` кине виняток, і результатів решти задач у вигляді масиву ви не отримаєте. Тому кожне джерело зручно загорнути в окремий метод, який перехоплює помилку саме цього джерела і повертає порожній список із позначкою про збій (приклад коду №2):

```csharp
try
{
    List<Book> books = await LoadCentralAsync();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Джерело недоступне: {ex.Message}");
}
```
[learn.microsoft.com/dotnet/csharp/asynchronous-programming/asynchronous-programming-scenarios](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/asynchronous-programming-scenarios)

**Асинхронна робота з файлами** — `File.WriteAllLinesAsync`, `File.WriteAllTextAsync`, `File.ReadAllLinesAsync` роблять те саме, що й знайомі синхронні методи, але не блокують потік під час запису чи читання диска:

```csharp
await File.WriteAllLinesAsync("report.csv", new[] { "Genre;Count", "Поезія;2" });
```
[learn.microsoft.com/dotnet/api/system.io.file.writealllinesasync](https://learn.microsoft.com/dotnet/api/system.io.file.writealllinesasync)

**Типові помилки:**

| Помилка | Чому це погано | Як правильно |
|---|---|---|
| `async void Load()` | метод неможливо `await`-ити, виняток із нього не перехопити | `async Task LoadAsync()`; `async void` — лише для обробників подій |
| `LoadAsync().Result`, `LoadAsync().Wait()` | потік блокується до завершення, асинхронність втрачено; у десктопних застосунках — ризик взаємного блокування (deadlock) | `await LoadAsync()` |
| виклик `LoadAsync();` без `await` | метод продовжить роботу, не дочекавшись результату, а виняток загубиться (компілятор попереджає CS4014) | `await LoadAsync();` |
| `await` у циклі для незалежних операцій | час додається: 3 × 2 с = 6 с | запустити всі задачі і `await Task.WhenAll(...)` |

**Скасування: `CancellationToken` (за бажанням)** — довгу операцію можна зупинити, наприклад, за таймаутом. `CancellationTokenSource` подає команду «скасувати», а `CancellationToken` передається в асинхронні методи, щоб вони могли на неї відреагувати. Скасована операція кидає `OperationCanceledException`. В обов'язкову частину роботи не входить, але знадобиться в ПР №5–8, де фреймворк передає токен у методи контролера:

```csharp
static async Task<List<Book>> LoadCentralAsync(CancellationToken ct)
{
    await Task.Delay(2000, ct); // токен передаємо далі
    return new List<Book>();
}

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));

try
{
    List<Book> books = await LoadCentralAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Джерело не відповіло за 1,5 с");
}
```
[learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads)

### LINQ to Objects

**LINQ (Language Integrated Query)** — набір методів розширення для `IEnumerable<T>` (простір імен `System.Linq`, у .NET 10 підключений автоматично), які дозволяють описати, *що* потрібно отримати з колекції, замість того щоб писати цикли вручну. Кожен оператор LINQ — це узагальнений метод, що приймає делегат `Func<...>`, і зазвичай йому передають лямбда-вираз (ПР №3). Ті самі оператори в Модулі 2 працюватимуть із базою даних через EF Core.

[learn.microsoft.com/dotnet/csharp/linq](https://learn.microsoft.com/dotnet/csharp/linq/)

**Метод-синтаксис і query-синтаксис** — запит можна записати ланцюжком викликів методів або SQL-подібним виразом. Компілятор перетворює query-синтаксис на ті самі виклики методів, тому результат однаковий. Метод-синтаксис уживаніший і підтримує всі оператори (`Take`, `Any`, `MaxBy` в query-синтаксисі не мають ключових слів), а query-синтаксис часто читабельніший для `join` і складного групування:

```csharp
// Метод-синтаксис
var recent = books.Where(b => b.Year > 2000).OrderBy(b => b.Year).Select(b => b.Title);

// Той самий запит у query-синтаксисі
var recentQuery =
    from b in books
    where b.Year > 2000
    orderby b.Year
    select b.Title;
```
[learn.microsoft.com/dotnet/csharp/linq/get-started/query-syntax-and-method-syntax-in-linq](https://learn.microsoft.com/dotnet/csharp/linq/get-started/query-syntax-and-method-syntax-in-linq)

**Відкладене та негайне виконання** — запит LINQ не виконується в момент оголошення. Змінна `recent` вище зберігає лише *опис* запиту, а обчислення відбувається під час перебору (`foreach`) і повторюється при кожному переборі. Методи `ToList()`, `ToArray()`, `Count()`, `Sum()`, `First()` тощо виконують запит негайно і повертають готовий результат. Якщо колекцію змінили між оголошенням і перебором, запит із відкладеним виконанням побачить зміни:

```csharp
var genres = books.Select(b => b.Genre);      // ще нічого не обчислено
List<string> snapshot = genres.ToList();      // виконано зараз, результат зафіксовано

books.Add(new Book("Шкільна", "Кобзар", "Тарас Шевченко", "Поезія", 1840));

Console.WriteLine(genres.Count());   // враховує нову книгу
Console.WriteLine(snapshot.Count);   // не враховує
```
[learn.microsoft.com/dotnet/standard/linq/deferred-execution-lazy-evaluation](https://learn.microsoft.com/dotnet/standard/linq/deferred-execution-lazy-evaluation)

**Фільтрація, сортування, проєкція:**

| Оператор | Призначення |
|---|---|
| `Where(x => умова)` | залишити елементи, що задовольняють умову |
| `OrderBy` / `OrderByDescending` | сортування за ключем за зростанням / спаданням |
| `ThenBy` / `ThenByDescending` | додатковий ключ для елементів з однаковим першим ключем (після `OrderBy`, а не другий `OrderBy`) |
| `Select(x => ...)` | перетворити кожен елемент, часто в анонімний тип `new { x.A, x.B }` лише з потрібними полями |
| `Take(n)` / `Skip(n)` | взяти перші `n` / пропустити перші `n` елементів (наприклад, «топ-5») |
| `SelectMany(x => колекція)` | «розгорнути» колекцію колекцій в одну послідовність, наприклад об'єднати списки з кількох джерел |

**Анонімні типи (`new { ... }`)** генеруються компілятором «на льоту»: зручно для проєкції, коли потрібні лише кілька полів або обчислені значення. Змінну такого типу оголошують через `var`.

[learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/filtering-data](https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/filtering-data) · [learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/sorting-data](https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/sorting-data) · [learn.microsoft.com/dotnet/csharp/fundamentals/types/anonymous-types](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/anonymous-types)

**Групування `GroupBy` та агрегатні функції** — `GroupBy` повертає послідовність груп `IGrouping<TKey, TElement>`: у кожної групи є ключ `Key`, а сама група — послідовність її елементів, тому до неї можна застосувати агрегатні функції `Count`, `Sum`, `Average`, `Min`, `Max`. Ключем може бути й анонімний тип з кількох полів: `GroupBy(x => new { x.Group, x.Subject })` (варіант 2). Методи `MinBy`/`MaxBy` повертають не саме мінімальне значення, а *елемент* з мінімальним ключем, наприклад найдешевшу пропозицію цілком (варіант 1):

```csharp
var byGenre = books
    .GroupBy(b => b.Genre)
    .Select(g => new { Genre = g.Key, Count = g.Count(), Oldest = g.MinBy(b => b.Year) });
```

Важливо: **`Average`, `Min`, `Max` на порожній послідовності чисел кидають `InvalidOperationException`** (варіанти 2 і 3), а `MinBy`/`MaxBy` на порожній послідовності записів повертають `null` (варіант 1). Якщо після фільтрації послідовність може виявитися порожньою, перевірте це заздалегідь через `Any()`.

[learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/grouping-data](https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/grouping-data) · [learn.microsoft.com/dotnet/api/system.linq.enumerable.maxby](https://learn.microsoft.com/dotnet/api/system.linq.enumerable.maxby)

**З'єднання `Join` і `GroupJoin`** — `Join` поєднує дві колекції за спільним ключем (аналог SQL `INNER JOIN`): у результат потрапляють лише ті пари, для яких знайшлася відповідність. Елементи без пари **мовчки зникають**. Якщо їх треба зберегти (наприклад, «замовлення, посилку якого не знайдено», варіант 4), використовують `GroupJoin`: для кожного елемента першої колекції він повертає групу відповідних елементів другої, можливо порожню (аналог `LEFT JOIN`):

```csharp
// Join: книга + країна автора; книги невідомих авторів не потраплять у результат
var withCountry = books.Join(
    authors,
    b => b.Author,        // ключ з першої колекції
    a => a.Name,          // ключ з другої колекції
    (b, a) => new { b.Title, a.Country });

// GroupJoin: кожен автор + його книги (у автора без книг група порожня, але він не зникає)
var authorsWithBooks = authors.GroupJoin(
    books,
    a => a.Name,
    b => b.Author,
    (a, bookGroup) => new { a.Name, Count = bookGroup.Count() });
```
[learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/join-operations](https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/join-operations)

**Квантори `Any` та `All`** — `Any(умова)` перевіряє, чи є хоча б один елемент, що задовольняє умову; `Any()` без умови — чи послідовність непорожня. `All(умова)` перевіряє, чи задовольняють умову всі елементи. **`All` на порожній послідовності завжди повертає `true`** («немає жодного елемента, що порушує умову»), тому вивід «усі в нормі» для порожньої вибірки вводить в оману (варіант 3).

[learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/quantifier-operations](https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/quantifier-operations)

## Приклади коду (для орієнтиру — не є готовим розв'язком варіанта)

Наведені фрагменти ілюструють технічні прийоми, потрібні для роботи, на нейтральному прикладі (каталоги кількох філій бібліотеки). Записи, джерела й запити свого варіанта студент розробляє самостійно.

### 1. Запис і асинхронне джерело даних

Джерело імітує відповідь сервера: чекає, потім повертає дані, заповнені в коді. Статичне поле `_centralIsDown` — «перемикач збою», яким керує пункт меню:

```csharp
public record Book(string Branch, string Title, string Author, string Genre, int Year);

static bool _centralIsDown = false;

static async Task<List<Book>> LoadCentralAsync()
{
    await Task.Delay(2000);

    if (_centralIsDown)
    {
        throw new InvalidOperationException("сервер не відповідає");
    }

    return new List<Book>
    {
        new("Центральна", "Маруся Чурай", "Ліна Костенко", "Поезія", 1979),
        new("Центральна", "1984", "Джордж Орвелл", "Антиутопія", 1949),
        new("Центральна", "Інтернат", "Сергій Жадан", "Роман", 2017),
    };
}
```

### 2. Обробка збою окремого джерела

Узагальнений метод-обгортка (ПР №3) приймає джерело як делегат `Func<Task<List<T>>>`, перехоплює його помилку і повертає результат разом зі статусом. Завдяки цьому збій одного джерела не зупиняє `Task.WhenAll` для решти:

```csharp
public record LoadResult<T>(string Source, List<T> Items, string? Error);

static async Task<LoadResult<T>> TryLoadAsync<T>(string source, Func<Task<List<T>>> load)
{
    try
    {
        List<T> items = await load();
        return new LoadResult<T>(source, items, null);
    }
    catch (InvalidOperationException ex)
    {
        return new LoadResult<T>(source, new List<T>(), ex.Message);
    }
}
```

### 3. Одночасне та послідовне завантаження зі `Stopwatch`

```csharp
var sw = Stopwatch.StartNew();
LoadResult<Book>[] results;

if (parallel)
{
    // Усі три запити запускаються одразу, чекаємо на найдовший
    results = await Task.WhenAll(
        TryLoadAsync("Центральна", LoadCentralAsync),
        TryLoadAsync("Шкільна", LoadSchoolAsync),
        TryLoadAsync("Університетська", LoadUniversityAsync));
}
else
{
    // Кожен наступний запит стартує лише після завершення попереднього
    results = new[]
    {
        await TryLoadAsync("Центральна", LoadCentralAsync),
        await TryLoadAsync("Шкільна", LoadSchoolAsync),
        await TryLoadAsync("Університетська", LoadUniversityAsync),
    };
}

sw.Stop();

// SelectMany «склеює» списки з усіх джерел в один
List<Book> books = results.SelectMany(r => r.Items).ToList();

Console.WriteLine($"Усього {books.Count} книг за {sw.ElapsedMilliseconds} мс.");
```

Якщо одне джерело викликається для різних параметрів (наприклад, `LoadLogsAsync(string server)` у варіанті 3), задачі зручно зібрати в список і передати в `Task.WhenAll` цілим списком:

```csharp
var tasks = new List<Task<LoadResult<Book>>>();

foreach (string branch in new[] { "Центральна", "Шкільна" })
{
    tasks.Add(TryLoadAsync(branch, () => LoadBranchAsync(branch)));
}

LoadResult<Book>[] results = await Task.WhenAll(tasks);
```

### 4. Фільтрація, сортування та проєкція

```csharp
var top = books
    .Where(b => b.Year >= 1950)
    .OrderBy(b => b.Genre)
    .ThenByDescending(b => b.Year)
    .Take(5)
    .Select(b => new { b.Title, b.Genre, b.Year }); // проєкція в анонімний тип

foreach (var b in top)
{
    Console.WriteLine($"{b.Genre}: «{b.Title}» ({b.Year})");
}
```

### 5. Групування та агрегування: метод- і query-синтаксис

```csharp
// Метод-синтаксис
var stats = books
    .GroupBy(b => b.Genre)
    .Select(g => new { Genre = g.Key, Count = g.Count(), Newest = g.Max(b => b.Year) })
    .OrderByDescending(s => s.Count)
    .ThenBy(s => s.Genre);

// Той самий запит у query-синтаксисі
var statsQuery =
    from b in books
    group b by b.Genre into g
    orderby g.Count() descending, g.Key
    select new { Genre = g.Key, Count = g.Count(), Newest = g.Max(b => b.Year) };
```

### 6. `Join`, `Any` та `All`

```csharp
public record AuthorInfo(string Name, string Country);

List<AuthorInfo> authors = new()
{
    new("Ліна Костенко", "Україна"),
    new("Джордж Орвелл", "Велика Британія"),
};

// Query-синтаксис з join: країна автора береться з другої колекції
var ukrainian =
    from b in books
    join a in authors on b.Author equals a.Name
    where a.Country == "Україна"
    select new { b.Title, b.Branch };

bool hasOld = books.Any(b => b.Year < 1900);

string genre = "Роман"; // наприклад, введено з клавіатури
bool allModern = books.Where(b => b.Genre == genre).All(b => b.Year > 2000);
// Увага: якщо жанру немає, вибірка порожня і All поверне true
```

### 7. Асинхронне збереження звіту

```csharp
IEnumerable<string> lines = books
    .GroupBy(b => b.Branch)
    .Select(g => $"{g.Key};{g.Count()}");

await File.WriteAllLinesAsync("report.csv", new[] { "Branch;Books" }.Concat(lines));
Console.WriteLine("Звіт збережено у файл report.csv");
```

### 8. Повний скелет програми — як усе з'єднати разом

Попередні приклади показували окремі прийоми. Тут вони зібрані в один робочий (компільований) файл `Program.cs`, щоб було видно, як асинхронні джерела, обробка збою, запити LINQ і меню стикуються між собою. Сценарій навмисно нейтральний (філії бібліотеки) — це **не** розв'язок жодного варіанта, а каркас, який треба наповнити записами, джерелами й запитами свого варіанта. Написано без top-level statements — рекомендовано не вмикати цю опцію при створенні проєкту.

```csharp
using System.Diagnostics;

record Book(string Branch, string Title, string Author, string Genre, int Year);

record AuthorInfo(string Name, string Country);

record LoadResult<T>(string Source, List<T> Items, string? Error);

class Program
{
    // === 1. Стан програми: завантажені дані та «перемикач збою» ===
    static List<Book> _books = new();
    static bool _centralIsDown = false;

    // Друга колекція для Join — заповнена в коді, не завантажується
    static readonly List<AuthorInfo> _authors = new()
    {
        new("Ліна Костенко", "Україна"),
        new("Сергій Жадан", "Україна"),
        new("Джордж Орвелл", "Велика Британія"),
        new("Рей Бредбері", "США"),
    };

    static async Task Main()
    {
        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine("\n=== Меню ===");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("1. Завантажити дані (одночасно)");
            Console.WriteLine("2. Завантажити дані (послідовно)");
            Console.WriteLine("3. Статистика за жанрами");
            Console.WriteLine("4. Книги, видані після року");
            Console.WriteLine("5. Зберегти звіт у файл");
            Console.WriteLine($"6. Збій центральної бібліотеки: {(_centralIsDown ? "увімкнено" : "вимкнено")}");

            int choice = ReadInt("Ваш вибір: ", 0, 6);

            isRunning = choice switch
            {
                1 => await HandleLoadAsync(parallel: true),
                2 => await HandleLoadAsync(parallel: false),
                3 => HandleStatsByGenre(),
                4 => HandleBooksAfterYear(),
                5 => await HandleSaveReportAsync(),
                6 => HandleToggleFailure(),
                0 => false,
                _ => true // сюди потрапити неможливо: ReadInt уже обмежив ввід діапазоном 0-6
            };
        }
    }

    // === 2. Асинхронні «джерела даних» (імітація звернення до сервера) ===

    static async Task<List<Book>> LoadCentralAsync()
    {
        await Task.Delay(2000);

        if (_centralIsDown)
        {
            throw new InvalidOperationException("сервер не відповідає");
        }

        return new List<Book>
        {
            new("Центральна", "Маруся Чурай", "Ліна Костенко", "Поезія", 1979),
            new("Центральна", "1984", "Джордж Орвелл", "Антиутопія", 1949),
            new("Центральна", "Інтернат", "Сергій Жадан", "Роман", 2017),
        };
    }

    static async Task<List<Book>> LoadSchoolAsync()
    {
        await Task.Delay(1500);

        return new List<Book>
        {
            new("Шкільна", "451° за Фаренгейтом", "Рей Бредбері", "Антиутопія", 1953),
            new("Шкільна", "Маруся Чурай", "Ліна Костенко", "Поезія", 1979),
        };
    }

    static async Task<List<Book>> LoadUniversityAsync()
    {
        await Task.Delay(1000);

        return new List<Book>
        {
            new("Університетська", "Ворошиловград", "Сергій Жадан", "Роман", 2010),
            new("Університетська", "Скотоферма", "Джордж Орвелл", "Сатира", 1945),
        };
    }

    // Обгортка: помилка одного джерела не зупиняє завантаження решти
    static async Task<LoadResult<T>> TryLoadAsync<T>(string source, Func<Task<List<T>>> load)
    {
        try
        {
            List<T> items = await load();
            return new LoadResult<T>(source, items, null);
        }
        catch (InvalidOperationException ex)
        {
            return new LoadResult<T>(source, new List<T>(), ex.Message);
        }
    }

    // === 3. Методи для кожного пункту меню ===

    static async Task<bool> HandleLoadAsync(bool parallel)
    {
        Console.WriteLine("Завантаження...");
        var sw = Stopwatch.StartNew();
        LoadResult<Book>[] results;

        if (parallel)
        {
            results = await Task.WhenAll(
                TryLoadAsync("Центральна", LoadCentralAsync),
                TryLoadAsync("Шкільна", LoadSchoolAsync),
                TryLoadAsync("Університетська", LoadUniversityAsync));
        }
        else
        {
            results = new[]
            {
                await TryLoadAsync("Центральна", LoadCentralAsync),
                await TryLoadAsync("Шкільна", LoadSchoolAsync),
                await TryLoadAsync("Університетська", LoadUniversityAsync),
            };
        }

        sw.Stop();

        _books = results.SelectMany(r => r.Items).ToList();

        foreach (LoadResult<Book> r in results)
        {
            Console.WriteLine(r.Error is null
                ? $"  {r.Source}: отримано записів — {r.Items.Count}"
                : $"  {r.Source}: недоступне ({r.Error})");
        }

        Console.WriteLine($"Усього записів: {_books.Count}, час: {sw.ElapsedMilliseconds} мс.");
        return true;
    }

    static bool HandleStatsByGenre()
    {
        if (!HasData())
        {
            return true;
        }

        var stats = _books
            .GroupBy(b => b.Genre)
            .Select(g => new { Genre = g.Key, Count = g.Count(), Newest = g.Max(b => b.Year) })
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Genre);

        foreach (var s in stats)
        {
            Console.WriteLine($"{s.Genre}: {s.Count} шт., найновіша — {s.Newest} р.");
        }

        return true;
    }

    static bool HandleBooksAfterYear()
    {
        if (!HasData())
        {
            return true;
        }

        int year = ReadInt("Рік: ", 0, DateTime.Now.Year);

        // Query-синтаксис з join: дані про країну автора беруться з другої колекції
        var found =
            (from b in _books
             join a in _authors on b.Author equals a.Name
             where b.Year > year
             orderby b.Year
             select new { b.Title, b.Author, a.Country, b.Year, b.Branch })
            .ToList();

        if (!found.Any())
        {
            Console.WriteLine("Книг не знайдено.");
            return true;
        }

        foreach (var f in found)
        {
            Console.WriteLine($"{f.Year} — «{f.Title}», {f.Author} ({f.Country}), філія: {f.Branch}");
        }

        return true;
    }

    static async Task<bool> HandleSaveReportAsync()
    {
        if (!HasData())
        {
            return true;
        }

        IEnumerable<string> lines = _books
            .GroupBy(b => b.Branch)
            .Select(g => $"{g.Key};{g.Count()}");

        await File.WriteAllLinesAsync("report.csv", new[] { "Branch;Books" }.Concat(lines));
        Console.WriteLine("Звіт збережено у файл report.csv");
        return true;
    }

    static bool HandleToggleFailure()
    {
        _centralIsDown = !_centralIsDown;
        return true;
    }

    // === 4. Допоміжні методи ===

    static bool HasData()
    {
        if (_books.Count > 0)
        {
            return true;
        }

        Console.WriteLine("Немає даних. Спочатку завантажте їх (пункт 1 або 2).");
        return false;
    }

    static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"Некоректне значення. Введіть ціле число від {min} до {max}.");
        }
    }
}
```

Зверніть увагу на два моменти. Перший — розподіл відповідальності: джерела лише «повертають дані», `TryLoadAsync` відповідає за збої, а методи пунктів меню лише будують запити й виводять результат. Другий — асинхронними стали тільки ті методи, що справді чекають (`HandleLoadAsync`, `HandleSaveReportAsync`). Запити LINQ до вже завантаженого списку виконуються в пам'яті, тому `async` їм не потрібен.

## Порядок виконання

1. Оголосити записи (`record`) свого варіанта і заповнити другу колекцію (для `Join`) у коді.
2. Реалізувати асинхронні джерела з різними затримками та перемикачем збою (приклад коду №1).
3. Реалізувати обгортку для обробки збою і два способи завантаження: одночасне та послідовне зі `Stopwatch` (приклади коду №2–3). Порівняти час.
4. Реалізувати запити LINQ свого варіанта, щонайменше один — у query-синтаксисі (приклади коду №4–6).
5. Реалізувати асинхронне збереження звіту (приклад коду №7).
6. Зібрати все в меню за зразком прикладу коду №8, перевірити роботу всіх пунктів, зокрема зі збоєм джерела та з граничним випадком варіанта, зробити скріншоти для звіту.

## Варіанти завдань

Кожен варіант моделює типову задачу: дані розкидані по кількох віддалених системах, кожна відповідає зі своєю затримкою і може бути недоступною. Програма збирає дані з усіх систем одночасно, а потім будує по них зведені звіти запитами LINQ.

### Варіант 1. Порівняння цін в інтернет-магазинах

Сервіси порівняння цін опитують кілька магазинів і показують, де товар дешевший. Магазини відповідають з різною швидкістю, тому опитувати їх по черзі довго.

**Дані:**

- `record Offer(string Shop, string Product, string Category, decimal Price, bool InStock)` — пропозиція магазину;
- `record WishItem(string Product, decimal MaxPrice)` — список бажань покупця (4–5 позицій, заповнюється в коді): товар і ціна, яку покупець готовий заплатити.

**Джерела:** 3 магазини з вигаданими назвами (наприклад, «ТехноМаркет», «ЦифраПлюс», «Електросвіт»), кожен повертає 7–10 пропозицій. Одні й ті самі 6–8 товарів у 3 категоріях («Ноутбуки», «Смартфони», «Аксесуари») мають бути в кількох магазинах за різною ціною; частини товарів немає в наявності (`InStock = false`).

**Мінімальне меню:**

0. Вихід.
1. Завантажити пропозиції (одночасно).
2. Завантажити пропозиції (послідовно).
3. Найдешевша пропозиція кожного товару — лише серед наявних, з назвою магазину.
4. Статистика за категоріями — кількість пропозицій, мінімальна, середня і максимальна ціна.
5. Пошук у ціновому діапазоні — ввести мінімальну і максимальну ціну (`decimal.TryParse`); вивести наявні пропозиції, відсортовані за ціною, а за однакової ціни — за назвою магазину.
6. Список бажань — для кожної позиції пропозиції, що не перевищують бажаної ціни (`Join` за назвою товару).
7. Зберегти звіт (найдешевша пропозиція кожного товару) у CSV-файл.
8. Увімкнути / вимкнути збій одного з магазинів.

**Рекомендації щодо реалізації:**

- у пункті 3 зручно згрупувати пропозиції за товаром, а в кожній групі взяти `MinBy(o => o.Price)` серед наявних;
- пункт 6 природно записати в query-синтаксисі: `from w in wishes join o in offers on w.Product equals o.Product where ...`;
- у пункті 5 варто перевірити, що мінімальна ціна не більша за максимальну;
- за бажанням: пункт «Товари, наявні в усіх магазинах, що відповіли» (кількість різних магазинів у групі товару дорівнює кількості джерел без помилки);
- за бажанням: таймаут завантаження через `CancellationTokenSource` (див. теоретичні відомості) — магазин, що не встиг відповісти за 1,5 с, вважається недоступним.

**Граничний випадок для обробки:** товар, якого немає в наявності в жодному магазині, що відповів. У пункті 3 він має бути у звіті з позначкою «немає в наявності», а не зникнути чи спричинити `NullReferenceException`: `MinBy` на порожній після фільтрації послідовності повертає `null`.

### Варіант 2. Зведена відомість успішності

Оцінки студентів зберігаються в системах різних кафедр, а список студентів — у деканаті. Щоб скласти рейтинг, деканат збирає оцінки з усіх кафедр і зводить їх в одну відомість.

**Дані:**

- `record Student(string Name, string Group)` — список деканату (8–10 студентів у 2–3 групах, заповнюється в коді);
- `record Grade(string StudentName, string Subject, int Score)` — оцінка (0–100), отримана від кафедри.

**Джерела:** 3 кафедри (наприклад, «Вища математика», «Програмування», «Англійська мова»), кожна повертає оцінки зі свого предмета для 6–10 студентів. Не всі кафедри мають оцінки для всіх студентів.

**Мінімальне меню:**

0. Вихід.
1. Завантажити оцінки (одночасно).
2. Завантажити оцінки (послідовно).
3. Рейтинг студентів — середній бал за всіма предметами, за спаданням, за однакового балу — за прізвищем.
4. Середній бал з кожного предмета в кожній групі (`Join` оцінок зі списком студентів, групування за групою і предметом).
5. Боржники — студенти, які мають хоча б одну оцінку нижче 60 (`Any`), з переліком предметів.
6. Студенти групи з середнім балом не нижче порогу — ввести назву групи і поріг (`int.TryParse`, 0–100).
7. Зберегти рейтинг у CSV-файл.
8. Увімкнути / вимкнути збій однієї з кафедр.

**Рекомендації щодо реалізації:**

- для пункту 4 ключ групування зручно зробити анонімним типом: `GroupBy(x => new { x.Group, x.Subject })`;
- рейтинг (пункт 3) варто будувати від списку студентів, а не від оцінок, інакше студент без оцінок просто не потрапить у результат;
- якщо введеної групи немає серед даних, слід вивести «Групу не знайдено»;
- за бажанням: розподіл студентів за шкалою («відмінно» 90+, «добре» 75–89, «задовільно» 60–74, «незадовільно» <60) — групування за обчислюваним ключем;
- за бажанням: таймаут завантаження через `CancellationTokenSource`.

**Граничний випадок для обробки:** студент зі списку деканату, для якого не надійшло жодної оцінки (наприклад, через збій кафедри). Він має бути в рейтингу з позначкою «немає оцінок», а не спричинити виняток: `Average` на порожній послідовності кидає `InvalidOperationException`.

### Варіант 3. Моніторинг вебсерверів

Застосунок працює на кількох серверах, і кожен веде власний журнал запитів. Щоб знайти повільні та проблемні місця, журнали збирають з усіх серверів і аналізують разом.

**Дані:**

- `record LogEntry(string Server, DateTime Time, string Level, string Endpoint, int ResponseMs)` — запис журналу; рівень: `INFO`, `WARN` або `ERROR`; ендпоінти на кшталт `/api/orders`, `/api/products`, `/api/login`;
- `record ServerInfo(string Name, string Region, int SlaMs)` — довідник серверів (заповнюється в коді): назва, регіон і допустимий час відповіді за угодою про рівень обслуговування (SLA).

**Джерела:** один метод `LoadLogsAsync(string server)`, викликаний для 4 серверів (`web-01` … `web-04`) зі своєю затримкою для кожного. Сервери повертають по 6–10 записів, а один (наприклад, `web-04`, резервний) повертає порожній журнал: за цей період запитів на нього не було.

**Мінімальне меню:**

0. Вихід.
1. Завантажити журнали (одночасно) — задачі для всіх серверів зібрати в список і передати в `Task.WhenAll`.
2. Завантажити журнали (послідовно).
3. Статистика по серверах — кількість записів кожного рівня, середній і максимальний час відповіді.
4. Порушення SLA — запити, час яких перевищує `SlaMs` свого сервера (`Join` журналу з довідником), відсортовані за сервером, потім за спаданням часу.
5. Помилки за ендпоінтом — ввести ендпоінт (непорожній рядок), вивести записи рівня `ERROR` для нього в хронологічному порядку.
6. Перевірка сервера — ввести назву сервера і з'ясувати через `All`, чи всі його запити вклалися в SLA.
7. Зберегти статистику по серверах у CSV-файл.
8. Увімкнути / вимкнути збій одного із серверів.

**Рекомендації щодо реалізації:**

- час запису зручно задавати відносно поточного: `DateTime.Now.AddMinutes(-15)`;
- пункт 4 природно записати в query-синтаксисі з `join ... on ... equals ...`;
- для пункту 3 сервери варто брати з довідника, а не з журналу, щоб сервер без запитів теж був у статистиці;
- за бажанням: топ-5 найповільніших запитів (`OrderByDescending` + `Take`);
- за бажанням: таймаут завантаження через `CancellationTokenSource`.

**Граничний випадок для обробки:** сервер без жодного запису в журналі (`web-04` або сервер зі збоєм). Перевірка в пункті 6 не повинна повідомляти «усі запити в нормі», адже `All` на порожній послідовності повертає `true`; треба вивести «немає даних для перевірки». Статистика в пункті 3 для такого сервера має показувати «немає запитів», а не кидати виняток з `Average`/`Max`. У звіті до роботи поясніть, чому `All` на порожній послідовності повертає `true`.

### Варіант 4. Відстеження посилок

Інтернет-магазин відправляє замовлення кількома службами доставки, і в кожної служби своя система відстеження. Щоб відповісти клієнту, де його замовлення, магазин опитує всі служби й зіставляє номери відправлень зі своїми замовленнями.

**Дані:**

- `record Parcel(string TrackingNumber, string Carrier, string City, string Status, double WeightKg, int DaysInTransit)` — посилка; статус: «В дорозі», «У відділенні» або «Отримано»;
- `record Order(string OrderId, string Customer, string TrackingNumber)` — замовлення магазину (10–12 записів, заповнюється в коді); 1–2 замовлення мають номер відправлення, якого немає в жодній службі.

**Джерела:** 3 служби доставки з вигаданими назвами (наприклад, «ШвидкоПошта», «ЕкспресДоставка», «Посилка24»), кожна повертає 6–10 посилок.

**Мінімальне меню:**

0. Вихід.
1. Завантажити дані служб доставки (одночасно).
2. Завантажити дані служб доставки (послідовно).
3. Статистика за службами — кількість посилок кожного статусу і сумарна вага.
4. Середній термін доставки отриманих посилок для кожної служби, від найшвидшої до найповільнішої.
5. Затримані посилки — ввести N (`int.TryParse`, більше 0), вивести посилки зі статусом «В дорозі» довше N днів, відсортовані за спаданням днів, потім за службою.
6. Замовлення клієнта — ввести ім'я клієнта, вивести його замовлення зі статусом і містом посилки (з'єднання замовлень з посилками за номером відправлення).
7. Зберегти звіт про всі замовлення та їхні статуси у CSV-файл.
8. Увімкнути / вимкнути збій однієї зі служб.

**Рекомендації щодо реалізації:**

- для пункту 4 спершу відфільтрувати посилки зі статусом «Отримано», потім групувати;
- пункт 5 зручно записати в query-синтаксисі (`where ... orderby ... descending, ...`);
- якщо клієнта не знайдено, слід вивести «Замовлень не знайдено»;
- за бажанням: місто, у яке йде найбільше посилок (`GroupBy` + `MaxBy`);
- за бажанням: таймаут завантаження через `CancellationTokenSource`.

**Граничний випадок для обробки:** замовлення, номер відправлення якого не знайдено в жодній службі (помилка в номері або служба недоступна). У пункті 6 і у звіті воно має бути зі статусом «статус невідомий», а не зникнути: звичайний `Join` відкидає елементи без пари, тому тут потрібен `GroupJoin` (див. теоретичні відомості).

## Вимоги до звіту

Звіт з практичної роботи оформлюється у вигляді документа (PDF/DOCX) та має містити:

- титульний аркуш;
- тему, мету та номер варіанта завдання;
- короткий опис реалізованого функціоналу та лістинг;
- скріншоти одночасного та послідовного завантаження з виміряним часом і пояснення, чому час відрізняється;
- скріншот роботи програми під час збою одного з джерел;
- скріншот обробки граничного випадку свого варіанта;
- перелік реалізованих запитів LINQ з коротким поясненням призначення кожного; для запиту в query-синтаксисі — його еквівалент у метод-синтаксисі;
- посилання на репозиторій GitHub;
- висновки (2–4 речення) щодо отриманих результатів.

## Контрольні питання

1. Чим I/O-bound операції відрізняються від CPU-bound? Для яких з них асинхронність дає найбільший виграш?
2. Що відбувається з потоком, коли метод доходить до `await` незавершеної задачі?
3. Чому `Thread.Sleep` і `Task.Delay` поводяться по-різному, хоча обидва «чекають»?
4. Чому три `await` поспіль виконуються довше, ніж `Task.WhenAll` для тих самих задач? Коли послідовний `await` все ж правильний?
5. Як перехопити виняток, що виник в асинхронному методі? Що станеться з `await Task.WhenAll(...)`, якщо одна із задач завершилася з винятком?
6. Чому `async void`, `.Result` і `.Wait()` вважаються поганою практикою?
7. У чому різниця між відкладеним і негайним виконанням запиту LINQ? Які методи спричиняють негайне виконання?
8. Чим метод-синтаксис відрізняється від query-синтаксису? На що компілятор перетворює query-синтаксис?
9. Що повертає `GroupBy`? Як отримати ключ і елементи групи?
10. У чому різниця між `Join` і `GroupJoin`? Як не втратити елементи першої колекції, для яких немає пари?
11. Що повертають `Any` і `All` для порожньої послідовності? Які агрегатні функції кидають виняток на порожній послідовності?
12. Чим `Select` відрізняється від `SelectMany`? Для чого `SelectMany` використано у вашій роботі?
