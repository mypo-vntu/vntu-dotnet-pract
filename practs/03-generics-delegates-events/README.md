# Практична робота №3. Узагальнення, делегати, події та лямбда-вирази

## Мета роботи

Навчитися писати власні узагальнені (generic) класи та методи з обмеженнями типів, передавати поведінку як параметр і змінювати її під час роботи програми через делегати (`Func`/`Action`/`Predicate`) і лямбда-вирази, а також організовувати сповіщення між об'єктами через події (`event`) за стандартним шаблоном .NET.

## Загальні завдання (обов'язкові для будь-якого варіанта)

- Створити консольний проєкт (.NET 10, Console App).
- Реалізувати узагальнений клас, заданий варіантом, з обмеженням типу-параметра (`where T : ...`).
- Реалізувати в узагальненому класі методи, що приймають делегат як параметр, зокрема метод `SetXxx` для необов'язкового правила варіанта: якщо правило задано, клас використовує його, якщо ні (`null`) — працює за замовчуванням. Користувач обирає правило через меню.
- Реалізувати подію за стандартним шаблоном .NET: власний клас-нащадок `EventArgs` зі змістовними даними, `event EventHandler<TEventArgs>`, захищений метод `OnXxx` з викликом `?.Invoke`. Подія має щонайменше 2 підписники; хоча б один з них — іменований метод, хоча б один — лямбда-вираз.
- Використати лямбда-вирази замість іменованих методів щонайменше в 3 місцях коду (правила варіанта, обробники подій, дії для `ForEach` тощо).
- **Виняток щодо вводу:** початкові дані дозволяється заповнити наперед у коді (кілька викликів `Add` у `Main`), а не вводити все з клавіатури. При цьому в меню має бути пункт, що вводить нові дані з клавіатури з валідацією (`TryParse` для числових полів).
- Реалізувати вивід результатів у зручному для читання вигляді.
- Передбачити просте меню (цикл із вибором пункту), що дозволяє повторювати дії без перезапуску програми, аж до вибору пункту «Вихід».

## Теоретичні відомості

Нижче — засоби мови C#, які знадобляться в цій роботі (докладніше про кожен — за посиланням на MS Learn). Матеріал розгорнуто викладено в лекції 5; тут лише те, що потрібно для варіантів.

### Узагальнення (generics)

**Узагальнений клас (`class Name<T>`)** — клас, у якому тип даних є параметром: код пишеться один раз, а конкретний тип підставляється під час створення об'єкта (`new Box<decimal>(99.90m)`). Компілятор перевіряє типи так само суворо, як і для звичайного класу, а для value type (`int`, `decimal`) не відбувається boxing. Параметрів-типів може бути кілька, за конвенцією їх імена починаються з `T`:

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

**Обмеження типу-параметра (`where T : ...`)** — без обмежень про `T` компілятор знає лише те, що це `object`, тому викликати в нього `Price` чи `CompareTo` неможливо. Обмеження повідомляє, що вміє тип `T`, і відкриває доступ до відповідних членів. Найуживаніші обмеження:

| Обмеження | Значення |
|---|---|
| `where T : class` | лише reference type |
| `where T : notnull` | будь-який тип, що не допускає `null` |
| `where T : IInterface` | тип реалізує інтерфейс, отже має його члени (усі варіанти цієї роботи) |
| `where T : new()` | тип має публічний конструктор без параметрів (пишеться останнім) |

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

**Делегат як необов'язкове правило** — делегат можна зберегти в полі класу, щоб змінювати поведінку об'єкта під час роботи програми, не змінюючи код класу. Якщо правило необов'язкове, поле оголошують nullable: `null` означає, що правило не задано. Оператор `?.Invoke` викликає делегат лише тоді, коли він не `null`, інакше повертає `null`, а оператор `??` у такому разі підставляє значення за замовчуванням (потрібно в усіх варіантах):

```csharp
private Func<decimal, decimal>? _rule;

public void SetRule(Func<decimal, decimal>? rule) => _rule = rule;

public decimal Apply(decimal value) => _rule?.Invoke(value) ?? value;
```
[learn.microsoft.com/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-) · [learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator)

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

**Замикання (closure)** — лямбда може використовувати локальні змінні зовнішнього методу. Вона захоплює **саму змінну, а не її значення** на момент створення: якщо змінну згодом змінити, лямбда побачить нове значення. Це зручно, коли правило залежить від значення, яке ввів користувач:

```csharp
int minAge = 18;
Func<int, bool> isAllowed = age => age >= minAge;

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

**`EventArgs` та `EventHandler<TEventArgs>`** — за конвенцією .NET обробник події має сигнатуру `(object? sender, TEventArgs e)`: `sender` — хто спричинив подію, `e` — дані про те, що сталося. Дані передаються власним класом-нащадком `EventArgs`, а тип події — вбудований делегат `EventHandler<TEventArgs>`. Клас `EventArgs` теж може бути узагальненим, якщо подія несе елемент типу `T` (варіанти 2–4):

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

Узагальнений клас варіанта зберігає дані у знайомому з ПР №1–2 `List<T>`. Крім нього, знадобиться:

**`Dictionary<TKey, TValue>`** — колекція пар «ключ → значення» зі швидким пошуком за ключем; ключ не може бути `null` і має бути унікальним (варіант 1 — промокоди, варіант 4 — рядки звіту):

```csharp
var stock = new Dictionary<string, int> { ["A-100"] = 12 };

if (stock.TryGetValue("A-100", out int quantity))
{
    Console.WriteLine(quantity); // 12
}
```
[learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2)

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

`Func<T, bool>` — для умови, `Action<T>` — для дії над кожним елементом, `Func<T, TResult>` — для обчислення значення з елемента. Метод не знає, *яку саме* умову чи дію йому передадуть, — тому один метод обслуговує будь-яку кількість сценаріїв:

```csharp
public List<T> FindAll(Func<T, bool> match)
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
Func<Note, bool> isShort = n => n.Text.Length <= maxLength;

Console.WriteLine(notes.FindAll(isShort).Count);

maxLength = 5; // нове значення введено користувачем
Console.WriteLine(notes.FindAll(isShort).Count); // результат уже інший
```

### 4. Необов'язкове правило, яке обирає користувач

Клас зберігає делегат у полі. Якщо правило задано, клас використовує його, якщо ні — працює за замовчуванням. Доступні правила зберігаються окремо, наприклад у словнику, тому нове правило не потребує змін у класі:

```csharp
public class ObservableList<T> where T : class, IHasTitle
{
    private Func<T, string>? _formatter; // null — правило не задано

    public void SetFormatter(Func<T, string>? formatter) => _formatter = formatter;

    public string Format(T item) => _formatter?.Invoke(item) ?? item.Title;
}

// Правила, які користувач обирає через меню
var formats = new Dictionary<string, Func<Note, string>>
{
    ["full"] = n => $"{n.Title}: {n.Text}",
    ["upper"] = n => n.Title.ToUpper(),
};

if (formats.TryGetValue(userChoice, out var format))
{
    notes.SetFormatter(format);
}

notes.SetFormatter(null); // повернення до поведінки за замовчуванням
```

### 5. Подія за стандартним шаблоном .NET

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

### 6. Підписка кількох обробників і відписка

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

### 7. Порівняння іменованого методу та лямбда-виразу (для звіту)

```csharp
// Іменований метод: зручний, коли логіка складна або потрібна в кількох місцях
static bool IsShoppingNote(Note n) => n.Title.StartsWith("Покуп");
List<Note> result1 = notes.FindAll(IsShoppingNote);

// Той самий критерій лямбда-виразом — логіка там, де вона використовується
List<Note> result2 = notes.FindAll(n => n.Title.StartsWith("Покуп"));
```

### 8. Повний скелет програми — як усе з'єднати разом

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

    private Func<T, string>? _formatter; // null — правило не задано

    public event EventHandler<ItemAddedEventArgs<T>>? ItemAdded;

    public int Count => _items.Count;

    public void SetFormatter(Func<T, string>? formatter) => _formatter = formatter;

    public string Format(T item) => _formatter?.Invoke(item) ?? item.Title;

    public void Add(T item)
    {
        _items.Add(item);
        OnItemAdded(new ItemAddedEventArgs<T>(item, _items.Count));
    }

    public List<T> FindAll(Func<T, bool> match)
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
            Console.WriteLine("4. Обрати формат виводу");

            int choice = ReadInt("Ваш вибір: ", 0, 4);

            isRunning = choice switch
            {
                1 => HandleAdd(notes),
                2 => HandleShowAll(notes),
                3 => HandleFind(notes),
                4 => HandleChooseFormat(notes),
                0 => false,
                _ => true // сюди потрапити неможливо: ReadInt уже обмежив ввід діапазоном 0-4
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
        notes.ForEach(n => Console.WriteLine($"- {notes.Format(n)}"));
        return true;
    }

    static bool HandleFind(ObservableList<Note> notes)
    {
        string word = ReadNonEmpty("Слово для пошуку: ");

        // Func<T, bool> — лямбда захоплює локальну змінну word (замикання)
        List<Note> found = notes.FindAll(n => n.Title.Contains(word, StringComparison.OrdinalIgnoreCase));

        Console.WriteLine(found.Count == 0 ? "Нічого не знайдено." : $"Знайдено: {found.Count}");

        foreach (Note n in found)
        {
            Console.WriteLine($"- {n.Title}");
        }

        return true;
    }

    static bool HandleChooseFormat(ObservableList<Note> notes)
    {
        Console.WriteLine("0 - лише назва, 1 - назва й текст, 2 - назва великими літерами");

        // Func<T, string> — користувач обирає правило, клас ObservableList<T> не змінюється
        Func<Note, string>? format = ReadInt("Формат: ", 0, 2) switch
        {
            1 => n => $"{n.Title}: {n.Text}",
            2 => n => n.Title.ToUpper(),
            _ => null // правило не задано: працює формат за замовчуванням
        };

        notes.SetFormatter(format);
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

Зверніть увагу на розподіл відповідальності: узагальнений клас `ObservableList<T>` нічого не знає ні про консоль, ні про нотатки — він лише зберігає елементи, виконує передані йому делегати і повідомляє про зміни подією. Яке правило обрати, що виводити і як реагувати на подію, вирішує `Program` через лямбди та обробники. Саме так варто розділити код і у своєму варіанті.

## Порядок виконання

1. Створити інтерфейс(и) та класи предметної області свого варіанта, заповнити дані наперед у коді.
2. Реалізувати узагальнений клас варіанта з обмеженням типу та методи з параметрами-делегатами (приклади коду №1–2).
3. Додати необов'язкове правило варіанта: поле-делегат, метод `SetXxx` і поведінку за замовчуванням (приклад коду №4).
4. Реалізувати клас `EventArgs`, подію та метод `OnXxx` у класі-видавці, підписати щонайменше два обробники (приклади коду №5–6).
5. Написати консольне меню за зразком прикладу коду №8, включно з пунктами введення даних з клавіатури й вибору правила.
6. Перевірити роботу всіх пунктів меню, зробити скріншоти для звіту.

## Варіанти завдань

Усі варіанти побудовані за однією логікою. Узагальнений клас зберігає дані й має одне **необов'язкове правило** — делегат, який користувач обирає через меню під час роботи програми. Якщо правило задано, клас використовує його, якщо ні — працює за замовчуванням (див. приклад коду №4). Подія повідомляє решту програми про важливу зміну, а клас при цьому не знає, хто і як на неї реагує.

### Варіант 1. Кошик інтернет-магазину

Сума кошика змінюється, коли покупець додає чи прибирає товар або вводить промокод. Магазин одразу показує нову суму й те, скільки лишилося до безкоштовної доставки.

**Структура:** інтерфейс `IPriced` (`Name`, `Price`), клас `Product`, що його реалізує, і узагальнений клас `Cart<T> where T : IPriced` з методами:

- `Add(T item)` — додати товар;
- `bool Remove(string name)` — прибрати товар за назвою;
- `decimal GetTotal()` — порахувати суму до сплати з урахуванням знижки;
- `ForEach(Action<T> action)` — виконати дію для кожного товару;
- `SetDiscount(Func<decimal, decimal>? discount)` — задати або скасувати знижку.

**Необов'язкове правило:** знижка `Func<decimal, decimal>` отримує суму товарів і повертає суму до сплати. Якщо знижку не задано (`null`), сума не змінюється. Промокоди зберігаються в `Program` у словнику `Dictionary<string, Func<decimal, decimal>>`:

| Промокод | Лямбда-вираз | Що робить |
|---|---|---|
| `SALE10` | `total => total * 0.9m` | знижка 10% |
| `MINUS100` | `total => Math.Max(total - 100, 0)` | мінус 100 грн |
| `BIG20` | `total => total >= 2000 ? total * 0.8m : total` | знижка 20% на суму від 2000 грн |

**Подія:** `TotalChanged` з `TotalChangedEventArgs` (стара і нова сума) спричиняється в `Add`, `Remove` і `SetDiscount`. Підписники: (1) вивід нової суми; (2) вивід повідомлення про безкоштовну доставку (від 1000 грн).

**Мінімальне меню:**

0. Вихід.
1. Додати товар — ввести назву й ціну (`decimal.TryParse`).
2. Прибрати товар — ввести назву.
3. Показати кошик — товари й сума до сплати.
4. Ввести промокод — знайти правило у словнику й передати в `SetDiscount`; для невідомого коду вивести повідомлення.
5. Скасувати промокод — викликати `SetDiscount(null)`.

**Приклад роботи:**

```text
> Додати: Навушники, 800 грн
Сума: 800 грн. До безкоштовної доставки: 200 грн

> Додати: Чохол, 300 грн
Сума: 1100 грн. Доставка безкоштовна!

> Промокод: SALE10
Сума: 990 грн. До безкоштовної доставки: 10 грн
```

**Рекомендації щодо реалізації:**

- у `Add`, `Remove` і `SetDiscount` запам'ятовуйте стару суму до зміни, а подію спричиняйте після неї;
- новий промокод — це один рядок у словнику: клас `Cart<T>` при цьому не змінюється;
- за бажанням: промокод із власним відсотком, де лямбда захоплює введене число (замикання, див. приклад коду №3), і другий тип `Cart<Ticket>` для квитків.

### Варіант 2. Музичний плейлист

Кнопка «Наступний трек» у плеєрі працює по-різному залежно від режиму: по порядку, з повтором або випадково. Під час відтворення плеєр показує поточний трек і веде історію прослуховування.

**Структура:** інтерфейс `IPlayable` (`Title`, `DurationSeconds`), клас `Song`, що його реалізує і має поле `Artist`, та узагальнений клас `Playlist<T> where T : IPlayable` з методами:

- `Add(T track)` — додати трек;
- `Next()` — перейти до наступного треку;
- `int GetTotalDuration()` — порахувати загальну тривалість;
- `ForEach(Action<T> action)` — виконати дію для кожного треку;
- `SetNextTrackRule(Func<int, int, int>? rule)` — задати або скасувати режим відтворення.

**Необов'язкове правило:** режим `Func<int, int, int>` отримує позицію поточного треку й кількість треків, а повертає позицію наступного. Якщо режим не задано (`null`), треки йдуть по порядку: `(current + 1) % count`.

| Режим | Лямбда-вираз |
|---|---|
| Повтор треку | `(current, count) => current` |
| Випадковий | `(current, count) => Random.Shared.Next(count)` |
| У зворотному порядку | `(current, count) => (current - 1 + count) % count` |

**Подія:** `TrackStarted` з `TrackStartedEventArgs<T>` (трек і його позиція) спричиняється в `Next`. Підписники: (1) вивід «Зараз грає: ...»; (2) запис треку в історію прослуховування.

**Мінімальне меню:**

0. Вихід.
1. Додати трек — ввести назву, виконавця й тривалість у секундах (`int.TryParse`).
2. Показати плейлист — треки, позначка поточного й загальна тривалість.
3. Наступний трек.
4. Обрати режим — по порядку (`SetNextTrackRule(null)`), повтор, випадковий або зворотний.
5. Показати історію прослуховування.

**Приклад роботи:**

```text
> Наступний трек
Зараз грає: Трек 2

> Режим: повтор
> Наступний трек
Зараз грає: Трек 2

> Режим: по порядку
> Наступний трек
Зараз грає: Трек 3
```

**Рекомендації щодо реалізації:**

- заповніть плейлист кількома треками наперед у коді, щоб усі режими можна було показати одразу;
- новий режим — це ще одна лямбда: метод `Next` при цьому не змінюється;
- за бажанням: третій підписник із лічильником прослуховувань кожного треку (`Dictionary<string, int>`) і другий тип `Playlist<Podcast>`.

### Варіант 3. Відстеження посилок

Служба доставки веде статуси посилок і розраховує вартість за обраним тарифом. Коли статус змінюється, одержувач отримує SMS, а зміна потрапляє в історію.

**Структура:** перелік `ShipmentStatus` (`Created`, `InTransit`, `Arrived`, `Received`), інтерфейс `IShipment` (`TrackingNumber`, `WeightKg` типу `decimal`, `Status`), клас `Parcel`, що його реалізує і має поле `Recipient`, та узагальнений клас `Tracker<T> where T : IShipment` з методами:

- `Add(T item)` — додати посилку;
- `bool AdvanceStatus(string trackingNumber)` — перевести посилку на наступний статус;
- `decimal GetCost(T item)` — порахувати вартість доставки;
- `ForEach(Action<T> action)` — виконати дію для кожної посилки;
- `SetTariff(Func<T, decimal>? tariff)` — задати або скасувати тариф.

**Необов'язкове правило:** тариф `Func<T, decimal>` отримує посилку й повертає вартість доставки. Якщо тариф не задано (`null`), діє стандартний розрахунок: `WeightKg * 40`.

| Тариф | Лямбда-вираз |
|---|---|
| Фіксований | `p => 70m` |
| Експрес | `p => 100 + p.WeightKg * 60` |
| Акційний | `p => p.WeightKg * 40 * 0.8m` |

**Подія:** `StatusChanged` з `StatusChangedEventArgs<T>` (посилка, старий і новий статус) спричиняється в `AdvanceStatus`. Підписники: (1) «SMS одержувачу» — вивід повідомлення в консоль; (2) запис зміни в історію.

**Мінімальне меню:**

0. Вихід.
1. Додати посилку — ввести номер, одержувача й вагу (`decimal.TryParse`).
2. Показати посилки — номер, статус і вартість за поточним тарифом.
3. Оновити статус посилки — ввести номер; для вже отриманої посилки вивести повідомлення.
4. Обрати тариф — стандартний (`SetTariff(null)`), фіксований, експрес або акційний.
5. Показати історію статусів.

**Приклад роботи:**

```text
> Посилки (тариф: стандартний)
UA-1001: Created, 2 кг, 80 грн

> Тариф: експрес
UA-1001: Created, 2 кг, 220 грн

> Оновити статус: UA-1001
SMS для Олени: посилка UA-1001 у дорозі
```

**Рекомендації щодо реалізації:**

- наступний статус зручно отримати приведенням: `(ShipmentStatus)((int)item.Status + 1)`, якщо посилка ще не в статусі `Received`;
- обмеження `IShipment` дає класу доступ до `WeightKg` і `Status`, тому `Tracker<T>` працює з будь-яким типом відправлення;
- за бажанням: у разі статусу `Arrived` додавати до SMS вартість доставки й випадковий код отримання (`Random.Shared.Next(1000, 10000)`) і другий тип `Tracker<Letter>`.

### Варіант 4. Облік витрат

Застосунок для обліку витрат показує, куди йдуть гроші: за категоріями, магазинами чи місяцями. Коли витрати перевищують бюджет, застосунок попереджає про це й позначає покупки, зроблені понад бюджет.

**Структура:** інтерфейс `IHasAmount` (`Amount`), клас `Expense`, що його реалізує і має поля `Category`, `Shop`, `Date`, та узагальнений клас `ExpenseLog<T> where T : IHasAmount` (бюджет задається в конструкторі) з методами:

- `Add(T item)` — додати запис;
- `decimal GetTotal()` — порахувати загальну суму;
- `Dictionary<string, decimal> GetReport()` — побудувати звіт (назва групи → сума);
- `ForEach(Action<T> action)` — виконати дію для кожного запису;
- `SetGrouping(Func<T, string>? grouping)` — задати або скасувати групування.

**Необов'язкове правило:** групування `Func<T, string>` отримує запис і повертає назву його групи. Якщо групування не задано (`null`), усі записи потрапляють в один рядок звіту — «Усього».

| Групування | Лямбда-вираз |
|---|---|
| За категорією | `e => e.Category` |
| За магазином | `e => e.Shop` |
| За місяцем | `e => e.Date.ToString("yyyy-MM")` |

**Подія:** `BudgetExceeded` з `BudgetExceededEventArgs<T>` (запис, загальна сума, бюджет) спричиняється в `Add`, якщо після додавання сума перевищує бюджет. Підписники: (1) вивід попередження; (2) додавання запису до списку покупок понад бюджет.

**Мінімальне меню:**

0. Вихід.
1. Додати витрату — ввести категорію, магазин і суму (`decimal.TryParse`); дата — поточна.
2. Показати всі витрати.
3. Показати звіт за поточним групуванням.
4. Обрати групування — без групування (`SetGrouping(null)`), за категорією, магазином або місяцем.
5. Показати покупки понад бюджет.

**Приклад роботи:**

```text
> Звіт (без групування)
Усього: 1500 грн

> Групування: за категорією
> Звіт
Їжа: 900 грн
Транспорт: 600 грн

> Додати: Їжа, АТБ, 600 грн (бюджет: 2000 грн)
Увага! Витрачено 2100 грн із бюджету 2000 грн
```

**Рекомендації щодо реалізації:**

- у `GetReport` достатньо одного проходу `foreach`: визначити назву групи й додати суму запису до відповідного значення словника;
- новий розріз звіту — це ще одна лямбда: метод `GetReport` при цьому не змінюється. Схожим чином працює `GroupBy` з LINQ, який розглядається в наступній роботі;
- за бажанням: менший бюджет на окрему категорію й другий тип `ExpenseLog<Subscription>` для регулярних платежів.

## Вимоги до звіту

Звіт з практичної роботи оформлюється у вигляді документа (PDF/DOCX) та має містити:

- титульний аркуш;
- тему, мету та номер варіанта завдання;
- короткий опис реалізованого функціоналу та лістинг;
- фрагмент коду «іменований метод vs лямбда-вираз» з поясненням, який варіант доречніший у цьому місці і чому (див. приклад коду №7);
- посилання на репозиторій GitHub;
- висновки (2–4 речення) щодо отриманих результатів.

## Контрольні питання

1. Які проблеми колекцій на основі `object` (`ArrayList`) усувають узагальнення?
2. Чим подія (`event`) відрізняється від звичайного публічного поля-делегата? Які операції з подією доступні ззовні класу-власника?
3. У чому суть замикання (closure) у лямбда-виразі? Що виведе цикл `for`, у якому лямбди захоплюють змінну циклу, і як це виправити?
4. Чим відрізняються делегати `Func`, `Action` та `Predicate`? Наведіть приклад кожного.
5. Навіщо потрібні обмеження типу-параметра (`where T : ...`)? Що станеться, якщо прибрати обмеження з узагальненого класу вашого варіанта?
6. Як компілятор визначає тип-аргумент під час виклику узагальненого методу без явного зазначення `<T>`?
7. Що таке делегат? Як передача делегата як параметра дозволяє одному методу обслуговувати різні сценарії?
8. Що таке багатоадресний (multicast) делегат? Що поверне виклик multicast-делегата `Func<int>` з трьома методами?
9. Яку сигнатуру має обробник події за стандартним шаблоном .NET і що передається в параметрах `sender` та `e`?
10. Навіщо подію спричиняють через окремий метод `OnXxx` і чому виклик іде через `?.Invoke`?
11. Чому `-=` з лямбда-виразом, записаним прямо в операторі, не відписує обробник? Як відписати обробник-лямбду?
12. Коли краще використати іменований метод, а коли лямбда-вираз? Наведіть приклад зі свого коду.
13. Навіщо зберігати делегат у полі класу? Що робить вираз `_rule?.Invoke(value) ?? value`, якщо правило не задано?
