# Практична робота №3. Узагальнення, делегати, події та лямбда-вирази

> ⚠️ **WIP** — матеріал ще в розробці, зміст може змінюватись.

## Мета роботи

Навчитися писати власні узагальнені (generic) класи та методи з обмеженнями типів, передавати поведінку як параметр через делегати (`Func`/`Action`/`Predicate`) і лямбда-вирази, а також організовувати сповіщення між об'єктами через події (`event`) за стандартним шаблоном .NET.

## Загальні завдання (обов'язкові для будь-якого варіанта)

- Створити консольний проєкт (.NET 10, Console App).
- Реалізувати узагальнений клас, заданий варіантом, з обмеженням типу-параметра (`where T : ...`).
- Реалізувати щонайменше один узагальнений метод, що приймає делегат (`Func<...>`, `Action<...>` або `Predicate<T>`) як параметр.
- Реалізувати подію за стандартним шаблоном .NET: власний клас-нащадок `EventArgs` зі змістовними даними, `event EventHandler<TEventArgs>`, захищений метод `OnXxx` з викликом `?.Invoke`. Подія має щонайменше 2 підписники; хоча б один з них — іменований метод, хоча б один — лямбда-вираз.
- Використати лямбда-вирази замість іменованих методів щонайменше в 3 місцях коду (критерії пошуку, селектори, обробники подій тощо).
- **Виняток щодо вводу:** початкові дані дозволяється заповнити наперед у коді (кілька викликів `Add`/`Enqueue` у `Main`), а не вводити все з клавіатури. При цьому в меню має бути пункт, що вводить нові дані з клавіатури з валідацією (`TryParse` для числових полів), і саме ця дія спричиняє подію варіанта.
- Реалізувати вивід результатів у зручному для читання вигляді.
- Передбачити просте меню (цикл із вибором пункту), що дозволяє повторювати дії без перезапуску програми, аж до вибору пункту «Вихід».

## Теоретичні відомості

Нижче — засоби мови C#, які знадобляться в цій роботі (докладніше про кожен — за посиланням на MS Learn). Матеріал розгорнуто викладено в лекції 5; тут лише те, що потрібно для варіантів.

### Узагальнення (generics)

**Узагальнений клас (`class Name<T>`)** — клас, у якому тип даних є параметром: код пишеться один раз, а конкретний тип підставляється під час створення об'єкта (`new Box<decimal>(99.90m)`). Компілятор перевіряє типи так само суворо, як і для звичайного класу, а для value type (`int`, `decimal`) не відбувається boxing. Параметрів-типів може бути кілька, за конвенцією їх імена починаються з `T` (знадобиться в усіх варіантах; у варіанті 1 — два параметри, `TKey` і `TValue`):

```csharp
public class Box<T>
{
    private readonly T _value;

    public Box(T value) => _value = value;

    public T Get() => _value;
}

var price = new Box<decimal>(99.90m);
var name = new Box<string>("Кава");
```
[learn.microsoft.com/dotnet/csharp/fundamentals/types/generics](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/generics)

**Узагальнений метод (`void Name<T>(...)`)** — метод зі своїм параметром-типом; може бути і в звичайному, і в узагальненому класі. Зазвичай тип-аргумент не вказують явно: компілятор виводить його з аргументів (type inference):

```csharp
public static void Swap<T>(ref T a, ref T b)
{
    (a, b) = (b, a);
}

int x = 1, y = 2;
Swap(ref x, ref y); // T = int виведено автоматично
```
[learn.microsoft.com/dotnet/csharp/programming-guide/generics/generic-methods](https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/generic-methods)

**Обмеження типу-параметра (`where T : ...`)** — без обмежень про `T` компілятор знає лише те, що це `object`, тому викликати в нього `Quantity` чи `CompareTo` неможливо. Обмеження повідомляє, що вміє тип `T`, і відкриває доступ до відповідних членів. Найуживаніші обмеження:

| Обмеження | Значення | Де знадобиться |
|---|---|---|
| `where T : class` | лише reference type | варіанти 3, 4 |
| `where T : notnull` | будь-який тип, що не допускає `null` | варіант 1 (ключ словника) |
| `where T : IInterface` | тип реалізує інтерфейс, отже має його члени | варіант 2 |
| `where T : new()` | тип має публічний конструктор без параметрів (пишеться останнім) | — |

Обмеження можна комбінувати через кому:

```csharp
public interface IHasTitle
{
    string Title { get; }
}

public class Catalog<T> where T : class, IHasTitle
{
    private readonly List<T> _items = new();

    public void Add(T item) => _items.Add(item);

    // Title доступний лише завдяки обмеженню IHasTitle
    public T? FindByTitle(string title) => _items.Find(x => x.Title == title);
}
```
[learn.microsoft.com/dotnet/csharp/language-reference/keywords/where-generic-type-constraint](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/where-generic-type-constraint)

### Делегати

**`delegate`** — тип, що описує сигнатуру методу; змінна такого типу зберігає посилання на будь-який метод з відповідною сигнатурою і дозволяє передати *поведінку* як параметр. Метод фіксує алгоритм, а змінну частину отримує ззовні — саме так влаштовані `List<T>.Sort`, `List<T>.Find` і всі оператори LINQ (наступна робота):

```csharp
public delegate decimal PriceRule(decimal price);

static decimal Discount10(decimal price) => price * 0.9m;

PriceRule rule = Discount10;
Console.WriteLine(rule(200m)); // 180
```
[learn.microsoft.com/dotnet/csharp/programming-guide/delegates](https://learn.microsoft.com/dotnet/csharp/programming-guide/delegates)

**Вбудовані делегати `Func`, `Action`, `Predicate`** — у сучасному коді власні типи делегатів оголошують рідко, бо BCL уже має узагальнені делегати майже на будь-яку сигнатуру (потрібні в усіх варіантах):

| Делегат | Сигнатура | Призначення | Приклад |
|---|---|---|---|
| `Action<T1, ...>` | 0–16 параметрів, повертає `void` | виконати дію | `Action<string> print = Console.WriteLine;` |
| `Func<T1, ..., TResult>` | 0–16 параметрів, **останній** тип — результат | обчислити значення | `Func<decimal, decimal> addVat = p => p * 1.2m;` |
| `Predicate<T>` | один параметр, повертає `bool` | перевірити умову | `Predicate<int> isEven = n => n % 2 == 0;` |

`Predicate<T>` за змістом тотожний `Func<T, bool>`, але це різні типи: старі методи `List<T>` (`Find`, `FindAll`, `RemoveAll`) приймають `Predicate<T>`, а LINQ — `Func<T, bool>`.

[learn.microsoft.com/dotnet/api/system.func-2](https://learn.microsoft.com/dotnet/api/system.func-2) · [learn.microsoft.com/dotnet/api/system.action-1](https://learn.microsoft.com/dotnet/api/system.action-1) · [learn.microsoft.com/dotnet/api/system.predicate-1](https://learn.microsoft.com/dotnet/api/system.predicate-1)

**Багатоадресні (multicast) делегати** — делегат може зберігати список методів; `+=` додає метод до списку виклику, `-=` прибирає. Під час виклику методи виконуються по черзі. Якщо делегат повертає значення, результатом буде значення лише останнього методу, тому multicast зазвичай використовують з `void`. На багатоадресності побудовані події:

```csharp
Action<string> log = Console.WriteLine;
log += msg => File.AppendAllText("log.txt", msg + Environment.NewLine);

log("Замовлення створено"); // виконаються обидва методи
```
[learn.microsoft.com/dotnet/csharp/programming-guide/delegates/how-to-combine-delegates-multicast-delegates](https://learn.microsoft.com/dotnet/csharp/programming-guide/delegates/how-to-combine-delegates-multicast-delegates)

### Лямбда-вирази та замикання

**Лямбда-вираз (`x => x * 2`)** — анонімний метод, записаний прямо в місці використання; компілятор перетворює його на екземпляр делегата потрібного типу. Після `=>` може бути один вираз (без `return`) або блок у фігурних дужках (з `return`, якщо є результат). Лямбда зручна для коротких одноразових умов; складну або повторно використовувану логіку краще залишити окремим іменованим методом:

```csharp
Func<int, int, int> add = (a, b) => a + b;   // кілька параметрів
Action hello = () => Console.WriteLine("Hi"); // без параметрів
Func<decimal, decimal, decimal> safeDiv = (a, b) =>
{
    if (b == 0)
    {
        throw new DivideByZeroException();
    }

    return a / b;
};
```
[learn.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions)

**Замикання (closure)** — лямбда може використовувати локальні змінні зовнішнього методу. Вона захоплює **саму змінну, а не її значення** на момент створення: якщо змінну згодом змінити, лямбда побачить нове значення. На цьому побудовані пункти меню у варіантах 1, 3 і 4, де умову, передану в метод, налаштовує користувач:

```csharp
int minAge = 18;
Predicate<int> isAllowed = age => age >= minAge;

minAge = 21;
Console.WriteLine(isAllowed(19)); // False — використано поточне значення 21
```

Типова пастка — захоплення змінної циклу `for`: у циклі існує **одна** змінна `i`, тому всі лямбди бачать її кінцеве значення. Виправлення — локальна копія в тілі циклу (у `foreach` змінна створюється заново на кожній ітерації, тож там проблеми немає):

```csharp
var actions = new List<Action>();

for (int i = 0; i < 3; i++)
{
    actions.Add(() => Console.Write(i + " ")); // виведе: 3 3 3
}

for (int i = 0; i < 3; i++)
{
    int copy = i;
    actions.Add(() => Console.Write(copy + " ")); // виведе: 0 1 2
}
```
[learn.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions#capture-of-outer-variables-and-variable-scope-in-lambda-expressions](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions#capture-of-outer-variables-and-variable-scope-in-lambda-expressions)

### Події

**`event`** — механізм, яким об'єкт-видавець сповіщає підписників про те, що щось сталося, не знаючи, хто вони (патерн «Спостерігач»). Технічно подія — обгортка над multicast-делегатом. Від публічного поля-делегата її відрізняє інкапсуляція: ззовні класу дозволено лише `+=` і `-=`, а викликати подію чи стерти всіх підписників може тільки клас-власник:

```csharp
public class Thermostat
{
    public Action<double>? TemperatureChangedField;            // поле: будь-хто може викликати чи обнулити
    public event EventHandler<TemperatureEventArgs>? TemperatureChanged; // подія: ззовні лише += і -=
}

thermostat.TemperatureChangedField = null;      // компілюється — підписників стерто
thermostat.TemperatureChanged = null;           // помилка компіляції
thermostat.TemperatureChanged?.Invoke(...);     // помилка компіляції поза класом
```
[learn.microsoft.com/dotnet/csharp/programming-guide/events](https://learn.microsoft.com/dotnet/csharp/programming-guide/events)

**`EventArgs` та `EventHandler<TEventArgs>`** — за конвенцією .NET обробник події має сигнатуру `(object? sender, TEventArgs e)`: `sender` — хто спричинив подію, `e` — дані про те, що сталося. Дані передаються власним класом-нащадком `EventArgs`, а тип події — вбудований делегат `EventHandler<TEventArgs>`. Клас `EventArgs` теж може бути узагальненим, якщо подія належить узагальненому класу (варіанти 1 і 4):

```csharp
public class TemperatureEventArgs : EventArgs
{
    public double OldValue { get; }
    public double NewValue { get; }

    public TemperatureEventArgs(double oldValue, double newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
```
[learn.microsoft.com/dotnet/standard/events](https://learn.microsoft.com/dotnet/standard/events) · [learn.microsoft.com/dotnet/api/system.eventhandler-1](https://learn.microsoft.com/dotnet/api/system.eventhandler-1)

**Метод `OnXxx` і безпечний виклик `?.Invoke`** — клас-видавець не викликає подію напряму в кожному місці, а робить це через захищений віртуальний метод `OnXxx` (нащадки можуть його перевизначити). Подія без жодного підписника дорівнює `null`, тому виклик іде через `?.Invoke` — інакше за відсутності підписників буде `NullReferenceException`:

```csharp
protected virtual void OnTemperatureChanged(TemperatureEventArgs e)
{
    TemperatureChanged?.Invoke(this, e);
}
```

**Відписка (`-=`)** — підписник, якому сповіщення більше не потрібні, відписується через `-=`. Відписатися від лямбди, записаної прямо в `-=`, неможливо: кожен запис лямбди створює *новий* делегат, тож нічого не буде прибрано. Щоб мати змогу відписатися, лямбду зберігають у змінну або використовують іменований метод. Варто пам'ятати й про те, що видавець тримає посилання на підписника, тому підписка довгоживучого видавця на короткоживучий об'єкт без відписки заважає GC звільнити пам'ять.

[learn.microsoft.com/dotnet/csharp/programming-guide/events/how-to-subscribe-to-and-unsubscribe-from-events](https://learn.microsoft.com/dotnet/csharp/programming-guide/events/how-to-subscribe-to-and-unsubscribe-from-events)

### Колекції

Узагальнений клас варіанта зберігає дані у вбудованих узагальнених колекціях. Крім знайомого з ПР №1–2 `List<T>`, знадобляться:

**`Dictionary<TKey, TValue>`** — колекція пар «ключ → значення» зі швидким пошуком за ключем; ключ не може бути `null` і має бути унікальним (варіант 1 — значення кешу):

```csharp
var stock = new Dictionary<string, int> { ["A-100"] = 12 };

if (stock.TryGetValue("A-100", out int quantity))
{
    Console.WriteLine(quantity); // 12
}
```
[learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2)

**`Queue<T>`** — черга за принципом FIFO («першим прийшов — першим обслужений»): `Enqueue` додає елемент у кінець, `Dequeue` забирає з початку, `TryDequeue` робить те саме без винятку на порожній черзі (варіант 4):

```csharp
var queue = new Queue<string>();
queue.Enqueue("перше");
queue.Enqueue("друге");

if (queue.TryDequeue(out string? next))
{
    Console.WriteLine(next); // перше
}
```
[learn.microsoft.com/dotnet/api/system.collections.generic.queue-1](https://learn.microsoft.com/dotnet/api/system.collections.generic.queue-1)

## Приклади коду (для орієнтиру — не є готовим розв'язком варіанта)

Наведені фрагменти ілюструють технічні прийоми, потрібні для роботи, на нейтральному прикладі (список нотаток); конкретні класи й логіку свого варіанта студент розробляє самостійно.

### 1. Узагальнений клас з обмеженням типу

```csharp
public interface IHasTitle
{
    string Title { get; }
}

public class Note : IHasTitle
{
    public string Title { get; }
    public string Text { get; }

    public Note(string title, string text)
    {
        Title = title;
        Text = text;
    }
}

public class ObservableList<T> where T : class, IHasTitle
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;

    public void Add(T item) => _items.Add(item);
}
```

### 2. Узагальнені методи з параметром-делегатом

`Predicate<T>` — для умови, `Action<T>` — для дії над кожним елементом, `Func<T, TResult>` — для обчислення значення з елемента. Метод не знає, *яку саме* умову чи дію йому передадуть, — тому один метод обслуговує будь-яку кількість сценаріїв:

```csharp
public List<T> FindAll(Predicate<T> match)
{
    var result = new List<T>();

    foreach (T item in _items)
    {
        if (match(item))
        {
            result.Add(item);
        }
    }

    return result;
}

public void ForEach(Action<T> action)
{
    foreach (T item in _items)
    {
        action(item);
    }
}

public int SumBy(Func<T, int> selector)
{
    int sum = 0;

    foreach (T item in _items)
    {
        sum += selector(item);
    }

    return sum;
}

// Виклики з різними лямбда-виразами:
List<Note> shopping = notes.FindAll(n => n.Title.StartsWith("Покуп"));
notes.ForEach(n => Console.WriteLine(n.Title));
int totalLength = notes.SumBy(n => n.Text.Length);
```

### 3. Замикання: умова, яку налаштовує користувач

Лямбда захоплює змінну `maxLength`, тому після зміни змінної (наприклад, з пункту меню) той самий делегат працює з новим значенням — його не треба створювати заново:

```csharp
int maxLength = 20;
Predicate<Note> isShort = n => n.Text.Length <= maxLength;

Console.WriteLine(notes.FindAll(isShort).Count);

maxLength = 5; // нове значення введено користувачем
Console.WriteLine(notes.FindAll(isShort).Count); // результат уже інший
```

### 4. Подія за стандартним шаблоном .NET

```csharp
// 1. Дані події — узагальнений клас-нащадок EventArgs
public class ItemAddedEventArgs<T> : EventArgs
{
    public T Item { get; }
    public int TotalCount { get; }

    public ItemAddedEventArgs(T item, int totalCount)
    {
        Item = item;
        TotalCount = totalCount;
    }
}

// 2. Видавець: оголошує подію і спричиняє її через OnItemAdded
public class ObservableList<T> where T : class, IHasTitle
{
    private readonly List<T> _items = new();

    public event EventHandler<ItemAddedEventArgs<T>>? ItemAdded;

    public void Add(T item)
    {
        _items.Add(item);
        OnItemAdded(new ItemAddedEventArgs<T>(item, _items.Count));
    }

    protected virtual void OnItemAdded(ItemAddedEventArgs<T> e) => ItemAdded?.Invoke(this, e);
}
```

### 5. Підписка кількох обробників і відписка

```csharp
var notes = new ObservableList<Note>();
int addedCount = 0;

// Підписник 1 — іменований метод
notes.ItemAdded += LogItemAdded;

// Підписник 2 — лямбда, збережена в змінну, щоб потім можна було відписатися
EventHandler<ItemAddedEventArgs<Note>> counter = (sender, e) => addedCount++;
notes.ItemAdded += counter;

notes.Add(new Note("Покупки", "Хліб")); // спрацюють обидва обробники

notes.ItemAdded -= counter;             // відписка: лічильник більше не реагує
notes.Add(new Note("Ідеї", "..."));     // спрацює лише LogItemAdded

static void LogItemAdded(object? sender, ItemAddedEventArgs<Note> e)
{
    Console.WriteLine($"Додано «{e.Item.Title}», усього: {e.TotalCount}");
}
```

### 6. Порівняння іменованого методу та лямбда-виразу (для звіту)

```csharp
// Іменований метод: зручний, коли логіка складна або потрібна в кількох місцях
static bool IsShoppingNote(Note n) => n.Title.StartsWith("Покуп");
List<Note> result1 = notes.FindAll(IsShoppingNote);

// Той самий критерій лямбда-виразом — логіка там, де вона використовується
List<Note> result2 = notes.FindAll(n => n.Title.StartsWith("Покуп"));
```

### 7. Повний скелет програми — як усе з'єднати разом

Попередні приклади показували окремі прийоми; тут вони зібрані в один робочий (компільований) файл `Program.cs`, щоб було видно, як узагальнений клас, делегати, подія та меню стикуються між собою. Сценарій навмисно нейтральний (список нотаток) — це **не** розв'язок жодного варіанта, а каркас, який треба наповнити класами й операціями свого варіанта. Написано без top-level statements — рекомендовано не вмикати цю опцію при створенні проєкту.

```csharp
interface IHasTitle
{
    string Title { get; }
}

class Note : IHasTitle
{
    public string Title { get; }
    public string Text { get; }

    public Note(string title, string text)
    {
        Title = title;
        Text = text;
    }
}

class ItemAddedEventArgs<T> : EventArgs
{
    public T Item { get; }
    public int TotalCount { get; }

    public ItemAddedEventArgs(T item, int totalCount)
    {
        Item = item;
        TotalCount = totalCount;
    }
}

class ObservableList<T> where T : class, IHasTitle
{
    private readonly List<T> _items = new();

    public event EventHandler<ItemAddedEventArgs<T>>? ItemAdded;

    public int Count => _items.Count;

    public void Add(T item)
    {
        _items.Add(item);
        OnItemAdded(new ItemAddedEventArgs<T>(item, _items.Count));
    }

    public List<T> FindAll(Predicate<T> match)
    {
        var result = new List<T>();

        foreach (T item in _items)
        {
            if (match(item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    public void ForEach(Action<T> action)
    {
        foreach (T item in _items)
        {
            action(item);
        }
    }

    protected virtual void OnItemAdded(ItemAddedEventArgs<T> e) => ItemAdded?.Invoke(this, e);
}

class Program
{
    static void Main()
    {
        // === 1. Узагальнена колекція, заповнена наперед у коді ===
        var notes = new ObservableList<Note>();
        notes.Add(new Note("Покупки", "Хліб, молоко"));
        notes.Add(new Note("Навчання", "Дочитати лекцію 5"));

        // === 2. Підписники на подію (після початкового заповнення, щоб не засмічувати вивід) ===
        int addedThisSession = 0;

        notes.ItemAdded += LogItemAdded;                       // підписник 1 — іменований метод
        notes.ItemAdded += (sender, e) => addedThisSession++;  // підписник 2 — лямбда із замиканням

        // === 3. Головний цикл меню ===
        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine("\n=== Меню ===");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("1. Додати нотатку");
            Console.WriteLine("2. Показати всі нотатки");
            Console.WriteLine("3. Знайти нотатки за словом у назві");

            int choice = ReadInt("Ваш вибір: ", 0, 3);

            isRunning = choice switch
            {
                1 => HandleAdd(notes),
                2 => HandleShowAll(notes),
                3 => HandleFind(notes),
                0 => false,
                _ => true // сюди потрапити неможливо: ReadInt уже обмежив ввід діапазоном 0-3
            };
        }

        Console.WriteLine($"Роботу завершено. Додано за сеанс: {addedThisSession}.");
    }

    // === 4. Обробник події — іменований метод ===

    static void LogItemAdded(object? sender, ItemAddedEventArgs<Note> e)
    {
        Console.WriteLine($"[подія] Додано «{e.Item.Title}», усього нотаток: {e.TotalCount}.");
    }

    // === 5. Методи для кожного пункту меню ===

    static bool HandleAdd(ObservableList<Note> notes)
    {
        string title = ReadNonEmpty("Назва: ");
        string text = ReadNonEmpty("Текст: ");
        notes.Add(new Note(title, text)); // саме тут спричиняється подія ItemAdded
        return true;
    }

    static bool HandleShowAll(ObservableList<Note> notes)
    {
        if (notes.Count == 0)
        {
            Console.WriteLine("Немає даних.");
            return true;
        }

        // Action<T> — лямбда замість окремого методу виведення
        notes.ForEach(n => Console.WriteLine($"- {n.Title}: {n.Text}"));
        return true;
    }

    static bool HandleFind(ObservableList<Note> notes)
    {
        string word = ReadNonEmpty("Слово для пошуку: ");

        // Predicate<T> — лямбда захоплює локальну змінну word (замикання)
        List<Note> found = notes.FindAll(n => n.Title.Contains(word, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine(found.Count == 0 ? "Нічого не знайдено." : $"Знайдено: {found.Count}");

        foreach (Note n in found)
        {
            Console.WriteLine($"- {n.Title}");
        }

        return true;
    }

    // === 6. Допоміжні методи введення ===

    static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine("Значення не може бути порожнім.");
        }
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

Зверніть увагу на розподіл відповідальності: узагальнений клас `ObservableList<T>` нічого не знає ні про консоль, ні про нотатки — він лише зберігає елементи, виконує передані йому делегати і повідомляє про зміни подією. Що виводити і як реагувати на подію, вирішує `Program` через лямбди та обробники. Саме так варто розділити код і у своєму варіанті.

## Порядок виконання

1. Створити інтерфейс(и) та класи предметної області свого варіанта, заповнити дані наперед у коді.
2. Реалізувати узагальнений клас варіанта з обмеженням типу та узагальнені методи з параметрами-делегатами (приклади коду №1–2).
3. Реалізувати клас `EventArgs`, подію та метод `OnXxx` у класі-видавці, підписати щонайменше два обробники (приклади коду №4–5).
4. Написати консольне меню за зразком прикладу коду №7, включно з пунктом введення даних з клавіатури, що спричиняє подію варіанта.
5. Перевірити роботу всіх пунктів меню, зробити скріншоти для звіту.

## Варіанти завдань

Кожен варіант моделює задачу, яка в реальних застосунках розв'язується саме цими засобами: узагальнений клас — це повторно використовуваний «інструмент», делегати налаштовують його поведінку під конкретну задачу, а подія повідомляє решту програми про важливі зміни.

### Варіант 1. Кеш курсів валют

Звернення до зовнішнього сервісу (наприклад, API банку) повільне, тому застосунки тримають уже отримані значення в пам'яті — у кеші. Пам'ять не безмежна, тож кеш має обмежену місткість: коли місця немає, найстаріший запис видаляється.

**Структура:** узагальнений клас `Cache<TKey, TValue> where TKey : notnull`, місткість задається в конструкторі. Значення зберігаються в `Dictionary<TKey, TValue>`, порядок додавання ключів — у `List<TKey>`. Методи:

- `TValue GetOrAdd(TKey key, Func<TKey, TValue> load)` — якщо ключ є в кеші, повертає збережене значення; якщо немає, викликає `load(key)`, зберігає результат і повертає його. Якщо кеш заповнений, перед додаванням видаляє найстаріший запис;
- `void ForEach(Action<TKey, TValue> action)` — виконує дію для кожного запису.

«Сервер банку» імітує звичайний метод `decimal LoadRate(string code)`: виводить повідомлення «Запит до сервера банку...» і повертає курс із заповненого в коді словника (USD, EUR, PLN, GBP тощо). Саме його передають у `GetOrAdd`.

**Подія:** `ItemRemoved` з `ItemRemovedEventArgs<TKey, TValue>` (ключ і значення видаленого запису) — спричиняється, коли запис видалено через нестачу місця. Підписники: (1) повідомлення в консоль («Видалено з кешу: EUR = 44.10»); (2) лічильник видалених записів.

**Мінімальне меню:**

0. Вихід.
1. Отримати курс валюти — ввести код валюти (наприклад, `USD`); вивести курс і позначку «з кешу» або «завантажено». Додавання нового запису в заповнений кеш спричиняє подію `ItemRemoved`.
2. Показати вміст кешу.
3. Показати валюти з курсом, вищим за поріг — ввести поріг (`decimal.TryParse`) і вивести відповідні записи через `ForEach` з лямбдою.

**Рекомендації щодо реалізації:**

- місткість варто взяти малою (3 записи), щоб видалення було легко показати;
- обмеження `where TKey : notnull` потрібне, бо ключ `Dictionary` не може бути `null`;
- у пункті 3 лямбда захоплює введений користувачем поріг (замикання, див. приклад коду №3);
- за бажанням: другий екземпляр, наприклад `Cache<int, string>` — кеш імен користувачів за ідентифікатором з лямбдою `id => $"Користувач #{id}"`.

### Варіант 2. Складський облік

У магазині товар не повинен закінчитися несподівано. Для кожної позиції задають мінімальний залишок: коли після продажу залишок опускається до цієї межі, система попереджає комірника і формує заявку постачальнику.

**Структура:** інтерфейс `IStockItem` (`Name`, `Quantity`, `MinQuantity`) і клас товару, що його реалізує (наприклад, `Product` з додатковим полем `Price`). Узагальнений клас `Inventory<T> where T : class, IStockItem` з методами:

- `Add(T item)` — додати позицію;
- `Sell(string name, int quantity)` — продати товар (зменшити залишок);
- `List<T> Find(Predicate<T> match)` — знайти позиції за довільною умовою.

**Подія:** `LowStock` з `LowStockEventArgs` (назва товару, поточний залишок, мінімальний залишок) — спричиняється в `Sell`, якщо після продажу `Quantity <= MinQuantity`. Підписники: (1) попередження в консоль; (2) «заявка постачальнику» — додає назву товару до списку заявок.

**Мінімальне меню:**

0. Вихід.
1. Показати залишки.
2. Продати товар — ввести назву і кількість (`int.TryParse`, більше 0); операція може спричинити подію `LowStock`.
3. Знайти товари із залишком, меншим за N — ввести N і передати в `Find` лямбду.
4. Показати заявки постачальнику.

**Рекомендації щодо реалізації:**

- обмеження `IStockItem` потрібне класу складу, бо він звертається до `Quantity` і `MinQuantity`: без обмеження ці властивості недоступні, і код не скомпілюється;
- якщо товару з такою назвою немає або на складі менше, ніж просять, достатньо вивести повідомлення й не змінювати залишок;
- за бажанням: пункт меню «Знайти товари дорожчі за N» — той самий `Find` з іншою лямбдою.

### Варіант 3. Перевірка даних реєстраційної форми

Перш ніж зберегти дані з форми реєстрації, застосунок перевіряє їх набором правил і показує користувачу всі помилки одразу. Правила зручно задавати делегатами: набір правил можна змінювати, не переписуючи клас, що їх перевіряє. За цим принципом працює бібліотека FluentValidation, яку можна буде використати в ПР №6.

**Структура:** клас `UserRegistration` (`Login`, `Email`, `Age`) і узагальнений клас `Validator<T> where T : class`, що зберігає список правил (пара «умова + текст помилки»). Методи:

- `void AddRule(Func<T, bool> isValid, string errorMessage)` — додати правило;
- `List<string> Validate(T item)` — перевірити всі правила і повернути список помилок (порожній, якщо дані коректні).

**Подія:** `ValidationFailed` з `ValidationFailedEventArgs` (логін користувача і список помилок) — спричиняється в `Validate`, якщо є хоча б одна помилка. Підписники: (1) вивід помилок у консоль; (2) лічильник невдалих спроб реєстрації.

**Мінімальне меню:**

0. Вихід.
1. Зареєструвати користувача — ввести логін, email і вік (`int.TryParse`). Якщо помилок немає, користувача додано до списку; інакше спричиняється подія `ValidationFailed`.
2. Показати зареєстрованих користувачів.
3. Змінити мінімальний вік реєстрації — ввести нове значення.

**Рекомендації щодо реалізації:**

- приклади правил: логін від 3 символів; email містить `@`; вік не менший за мінімальний;
- пункт 3 показує замикання: правило `u => u.Age >= minAge` захоплює змінну `minAge`, тож достатньо змінити змінну — правило одразу працюватиме з новим значенням (див. приклад коду №3);
- за бажанням: другий екземпляр `Validator<RoomBooking>` для бронювання номера в готелі (наприклад, правило «дата виїзду пізніша за дату заїзду»).

### Варіант 4. Черга розсилки повідомлень

Застосунки надсилають email через зовнішні сервіси, які час від часу недоступні. Тому повідомлення ставлять у чергу й надсилають по одному; ті, що не вдалося надіслати, відкладають у список недоставлених, щоб спробувати пізніше.

**Структура:** клас `EmailMessage` (`To`, `Subject`) і узагальнений клас `SendQueue<T> where T : class`. Усередині — `Queue<T>` для черги і `List<T>` для недоставлених. Методи:

- `void Enqueue(T item)` — поставити в чергу;
- `void ProcessAll(Func<T, bool> send)` — по черзі передати кожен елемент у `send`: `true` — надіслано, `false` — елемент переходить до недоставлених;
- `void RetryFailed()` — повернути всі недоставлені елементи в чергу.

«Поштовий сервіс» імітує метод `bool Send(EmailMessage message)`: виводить «Надсилання на ...» і повертає `false` для адрес з домену, що «не працює» (наприклад, `@down.example`), і `true` для решти.

**Події:** `Sent` і `Failed` з `MessageEventArgs<T>` (повідомлення і час). Підписники: (1) журнал у консоль; (2) лічильник надісланих і недоставлених повідомлень.

**Мінімальне меню:**

0. Вихід.
1. Додати повідомлення в чергу — ввести адресу й тему (непорожні рядки).
2. Показати чергу.
3. Надіслати всі повідомлення з черги.
4. Показати недоставлені повідомлення.
5. Повторити надсилання недоставлених — викликати `RetryFailed`, а потім знову надіслати чергу.

**Рекомендації щодо реалізації:**

- обробник `send` передається ззовні, тому клас черги не знає, що саме він надсилає — email, SMS чи щось інше;
- щоб у пункті 5 повідомлення доставилися, зручно мати змінну `bool serverIsDown`, яку захоплює лямбда-обробник, і пункт меню, що її перемикає (замикання, див. приклад коду №3);
- за бажанням (лекція 6): асинхронний обробник `Func<T, Task<bool>>` з імітацією мережевої затримки `await Task.Delay(300)`.

## Вимоги до звіту

Звіт з практичної роботи оформлюється у вигляді документа (PDF/DOCX) та має містити:

- титульний аркуш;
- тему, мету та номер варіанта завдання;
- короткий опис реалізованого функціоналу та лістинг;
- фрагмент коду «іменований метод vs лямбда-вираз» з поясненням, який варіант доречніший у цьому місці і чому (див. приклад коду №6);
- посилання на репозиторій GitHub;
- висновки (2–4 речення) щодо отриманих результатів.

## Контрольні питання

1. Які проблеми колекцій на основі `object` (`ArrayList`) усувають узагальнення?
2. Чим подія (`event`) відрізняється від звичайного публічного поля-делегата? Які операції з подією доступні ззовні класу-власника?
3. У чому суть замикання (closure) у лямбда-виразі? Що виведе цикл `for`, у якому лямбди захоплюють змінну циклу, і як це виправити?
4. Чим відрізняються делегати `Func`, `Action` та `Predicate`? Наведіть приклад кожного зі свого варіанта.
5. Навіщо потрібні обмеження типу-параметра (`where T : ...`)? Що станеться, якщо прибрати обмеження з узагальненого класу вашого варіанта?
6. Як компілятор визначає тип-аргумент під час виклику узагальненого методу без явного зазначення `<T>`?
7. Що таке делегат? Як передача делегата як параметра дозволяє одному методу обслуговувати різні сценарії?
8. Що таке багатоадресний (multicast) делегат? Що поверне виклик multicast-делегата `Func<int>` з трьома методами?
9. Яку сигнатуру має обробник події за стандартним шаблоном .NET і що передається в параметрах `sender` та `e`?
10. Навіщо подію спричиняють через окремий метод `OnXxx` і чому виклик іде через `?.Invoke`?
11. Чому `-=` з лямбда-виразом, записаним прямо в операторі, не відписує обробник? Як відписати обробник-лямбду?
12. Коли краще використати іменований метод, а коли лямбда-вираз? Наведіть приклад зі свого коду.
