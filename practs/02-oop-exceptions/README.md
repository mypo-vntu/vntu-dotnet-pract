# Практична робота №2. ООП та обробка виключень у консольному застосунку

## Мета роботи

Отримати практичні навички роботи з класами в C# з використанням принципів ООП (інкапсуляція, успадкування, поліморфізм, абстракція), роботи з інтерфейсами та коректної обробки виключень (`Exeption`).

## Загальні завдання (обов'язкові для будь-якого варіанта)

- Створити консольний проєкт (.NET 10, Console App).
- Спроєктувати ієрархію класів відповідно до варіанта: базовий (можливо, абстрактний) клас та щонайменше 2 похідні класи з перевизначенням (`override`) щонайменше одного методу.
- Реалізувати інкапсуляцію: приватні поля, публічні властивості (у т.ч. з валідацією значення у `set` — наприклад, заборона від'ємної ціни чи порожнього імені).
- Реалізувати щонайменше один інтерфейс `IComparable<T>`.
- Реалізувати колекцію об'єктів базового типу, що фактично зберігає похідні об'єкти (демонстрація поліморфізму) — обробка колекції через `foreach` з викликом віртуального методу. **Виняток щодо вводу:** колекцію дозволяється заповнити наперед у коді (масив-константа або кілька викликів конструкторів у `Main`), а не вводити всі об'єкти з клавіатури; при цьому в меню має бути пункт, що додає новий об'єкт до колекції через введення з клавіатури з валідацією (`TryParse` для числових полів). Для зберігання колекції рекомендовано використовувати `List<T>`.
- Реалізувати власний клас винятку (успадкований від `Exception`) та використати блок `try {} catch {}` для обробки цих виключень.
- Реалізувати вивід результатів у зручному для читання вигляді.
- Передбачити просте меню (цикл із вибором пункту), що дозволяє повторювати дії без перезапуску програми, аж до вибору пункту «Вихід».

## Теоретичні відомості

Нижче — засоби мови C#, які знадобляться в цій роботі (докладніше про кожен — за посиланням на MS Learn).
**P.S.** Дана робота передбачає, що ви вже ознайомлені з базовими принципами ООП, тому теоретичні відомості не вникають в деталі, а лише поверхнево пояснюють синтаксис мови C#.

### Класи, властивості та конструктори

**Оголошення класу (`class`)** — базова синтаксична конструкція для опису власного типу: поля, властивості, конструктори, методи. Кожен варіант цієї роботи будується навколо ієрархії з кількох таких класів (базовий + похідні).

```csharp
public class Shape
{
    public string Name { get; }
}
```
[learn.microsoft.com/dotnet/csharp/fundamentals/types/classes](https://learn.microsoft.com/dotnet/csharp/fundamentals/types/classes)

**Властивості (`get`/`set`) з валідацією** — керований доступ до приватних полів; у блоці `set` можна перевірити значення перед присвоєнням і за потреби викинути виняток (знадобиться для властивостей на кшталт ціни, зарплати, ваги чи потужності — залежно від варіанта):

```csharp
private double _price;

public double Price
{
    get => _price;
    set => _price = value >= 0
        ? value
        : throw new ArgumentException("Ціна не може бути від'ємною.");
}
```
[learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/properties](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/properties)

**Конструктори та їх перевантаження** — спеціальний метод ініціалізації об'єкта; кілька конструкторів з різною сигнатурою (overload) дають кілька способів створення об'єкта (наприклад, з значенням за замовчуванням для необов'язкового поля):

```csharp
public class Employee
{
    public string FullName { get; }
    public double BaseSalary { get; }

    public Employee(string fullName, double baseSalary)
    {
        FullName = fullName;
        BaseSalary = baseSalary;
    }

    public Employee(string fullName) : this(fullName, baseSalary: 10000)
    {
    }
}
```
[learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/constructors](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/constructors)

### Успадкування та поліморфізм

**`: BaseClass` та успадкування** — похідний клас переймає члени базового та може додавати власні або перевизначати наявні; саме так у кожному варіанті будується ієрархія «базовий клас → 2 похідні»:

```csharp
public class Circle : Shape
{
}
```
[learn.microsoft.com/dotnet/csharp/fundamentals/object-oriented/inheritance](https://learn.microsoft.com/dotnet/csharp/fundamentals/object-oriented/inheritance)

**`virtual`/`override`** — `virtual` у базовому класі дозволяє похідним класам перевизначити метод через `override`; саме так у C# реалізується поліморфізм під час виконання — виклик через базовий тип (`Shape shape = new Circle(...)`) фактично викликає реалізацію похідного класу:

```csharp
public virtual double GetArea() => 0;
// у Circle: public override double GetArea() => Math.PI * Radius * Radius;
```
[learn.microsoft.com/dotnet/csharp/language-reference/keywords/override](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/override)

**`abstract` (клас/метод)** — абстрактний клас не можна створити напряму (лише через похідні); абстрактний метод не має реалізації в базовому класі — її зобов'язаний надати кожен похідний клас. Доречний, коли в базовому класі немає жодної розумної «реалізації за замовчуванням» (наприклад, площа фігури без знання її типу):

```csharp
public abstract class Shape
{
    public abstract double GetArea();
}
```
[learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract)

**`interface`, зокрема `IComparable<T>`** — інтерфейс задає контракт із сигнатурами членів без реалізації; клас може реалізовувати кілька інтерфейсів одночасно (на відміну від успадкування, де базовий клас лише один). `IComparable<T>` — стандартний інтерфейс .NET для визначення «природного» порядку сортування об'єктів власного типу через метод `CompareTo`; використовується, зокрема, у `List<T>.Sort()`:

```csharp
public class Shape : IComparable<Shape>
{
    public double GetArea() => 0;

    // Позитивне число — this "більший" за other; від'ємне — "менший"; нуль — рівні
    public int CompareTo(Shape? other) => GetArea().CompareTo(other?.GetArea() ?? 0);
}
```

Після реалізації `IComparable<T>` колекцію можна відсортувати одним викликом — `List<T>.Sort()` (сортує на місці, за зростанням `CompareTo`); для сортування у зворотному порядку без зміни `CompareTo` можна передати делегат-компаратор: `shapes.Sort((a, b) => b.CompareTo(a))`:

```csharp
List<Shape> shapes = new() { new Circle(5), new Rectangle(2, 3) };
shapes.Sort(); // за зростанням площі — використовує CompareTo кожного елемента
```
[learn.microsoft.com/dotnet/csharp/language-reference/keywords/interface](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/interface) · [learn.microsoft.com/dotnet/api/system.icomparable-1](https://learn.microsoft.com/dotnet/api/system.icomparable-1)

### Обробка виключень

**`try`/`catch`/`finally`** — блок обробки виключних ситуацій: `try` — код, що може викинути виняток; `catch` — обробка конкретного типу винятку (можна кілька гілок `catch` для різних типів, від конкретнішого до загальнішого); `finally` — код, що виконується завжди, незалежно від того, стався виняток чи ні (наприклад, для закриття ресурсу чи виведення підсумкового повідомлення):

```csharp
try
{
    double area = shape.GetArea();
}
catch (NegativeValueException ex)
{
    Console.WriteLine($"Помилка вхідних даних: {ex.Message}");
}
finally
{
    Console.WriteLine("Обробку завершено.");
}
```
[learn.microsoft.com/dotnet/csharp/language-reference/statements/exception-handling-statements](https://learn.microsoft.com/dotnet/csharp/language-reference/statements/exception-handling-statements)

**`throw`** — явне «викидання» винятку в місці виявлення помилкової ситуації (наприклад, усередині `set` властивості чи конструктора, коли отримане значення недопустиме):

```csharp
if (radius <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(radius), "Радіус має бути додатним.");
}
```
[learn.microsoft.com/dotnet/csharp/language-reference/keywords/throw](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/throw)

**Власні класи винятків** — успадкування від `Exception` для створення предметно-специфічного типу помилки (наприклад, `InvalidPackageException`, `InsufficientFundsException` — залежно від варіанта). Офіційна рекомендація — назва завершується на `...Exception`, і клас містить три стандартні конструктори (без параметрів, з повідомленням, з повідомленням та внутрішнім винятком):

```csharp
public class NegativeValueException : Exception
{
    public NegativeValueException() { }
    public NegativeValueException(string message) : base(message) { }
    public NegativeValueException(string message, Exception inner) : base(message, inner) { }
}
```
[learn.microsoft.com/dotnet/standard/exceptions/how-to-create-user-defined-exceptions](https://learn.microsoft.com/dotnet/standard/exceptions/how-to-create-user-defined-exceptions)

### Колекції

**`List<T>`** — узагальнена колекція зі змінним розміром; у цій роботі зберігає об'єкти базового типу ієрархії (`List<Shape>`, `List<Employee>` тощо), при цьому фактично містить похідні об'єкти — саме на цьому й будується демонстрація поліморфізму через `foreach`:

```csharp
List<Shape> shapes = new() { new Circle(5), new Rectangle(4, 6) };
shapes.Add(new Circle(3));
```
[learn.microsoft.com/dotnet/api/system.collections.generic.list-1](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)

## Приклади коду (для орієнтиру — не є готовим розв'язком варіанта)

Наведені фрагменти ілюструють технічні прийоми, потрібні для роботи; конкретну ієрархію й логіку свого варіанта студент розробляє самостійно.

### 1. Базовий (абстрактний) клас, властивість з валідацією, перевантажені конструктори

```csharp
public abstract class Shape
{
    private string _name;

    public string Name
    {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Назва фігури не може бути порожньою.")
            : value;
    }

    protected Shape(string name)
    {
        Name = name;
    }

    // Абстрактний метод — кожен похідний клас зобов'язаний його реалізувати
    public abstract double GetArea();

    // Віртуальний метод з реалізацією за замовчуванням, яку можна (не обов'язково) перевизначити
    public virtual void PrintInfo()
    {
        Console.WriteLine($"{Name}: площа = {GetArea():F2}");
    }
}
```

### 2. Похідний клас, `override`, власний конструктор з перевантаженням

```csharp
public class Circle : Shape
{
    public double Radius { get; }

    public Circle(double radius) : base("Коло")
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Радіус має бути додатним.");
        }

        Radius = radius;
    }

    public override double GetArea() => Math.PI * Radius * Radius;

    public override void PrintInfo()
    {
        base.PrintInfo(); // виклик базової реалізації перед доповненням
        Console.WriteLine($"  Радіус: {Radius}");
    }
}
```

### 3. Власний клас винятку (три стандартні конструктори)

```csharp
public class NegativeValueException : Exception
{
    public NegativeValueException() { }

    public NegativeValueException(string message) : base(message) { }

    public NegativeValueException(string message, Exception inner) : base(message, inner) { }
}
```

### 4. Обробка кількох сценаріїв винятків (`try`/`catch`/`finally`)

```csharp
static void ProcessShape(Shape shape)
{
    try
    {
        double area = shape.GetArea();
        Console.WriteLine($"Площа: {area:F2}");
    }
    catch (NegativeValueException ex)
    {
        Console.WriteLine($"Помилка вхідних даних: {ex.Message}");
    }
    catch (Exception ex) // "запасний" обробник для непередбачених винятків
    {
        Console.WriteLine($"Непередбачена помилка: {ex.Message}");
    }
    finally
    {
        Console.WriteLine("Обробку фігури завершено.");
    }
}
```

### 5. Поліморфна колекція, заповнена наперед у коді, з можливістю розширення

```csharp
List<Shape> shapes = new()
{
    new Circle(5),
    new Rectangle(4, 6)
};

// Викликається з пункту меню "Додати фігуру" — тут демонструється
// введення з клавіатури з валідацією через TryParse
static void AddCircle(List<Shape> shapes)
{
    double radius = ReadDouble("Радіус нового кола: ", min: 0.01);
    shapes.Add(new Circle(radius));
}

// Демонстрація поліморфізму: виклик virtual/override-методу через базовий тип
foreach (Shape shape in shapes)
{
    shape.PrintInfo();
}
```

### 6. Повний скелет програми — як усе з'єднати разом

Попередні приклади показували окремі прийоми; тут вони зібрані в один робочий (компільований) файл `Program.cs`, щоб було видно, як ієрархія класів, власний виняток і меню стикуються між собою. Меню тут навмисно нейтральне (лише додавання й перегляд) — це **не** розв'язок жодного варіанта (немає сортування, підсумкових розрахунків тощо з вимог варіантів), а каркас, який треба наповнити операціями свого варіанта. Написано без top-level statements — рекомендовано не вмикати цю опцію при створенні проєкту.

```csharp
abstract class Shape
{
    public string Name { get; }

    protected Shape(string name) => Name = name;

    public abstract double GetArea();

    public virtual void PrintInfo() => Console.WriteLine($"{Name}: площа = {GetArea():F2}");
}

class Circle : Shape
{
    public double Radius { get; }

    public Circle(double radius) : base("Коло")
    {
        if (radius <= 0)
        {
            throw new NegativeValueException("Радіус має бути додатним.");
        }

        Radius = radius;
    }

    public override double GetArea() => Math.PI * Radius * Radius;
}

class NegativeValueException : Exception
{
    public NegativeValueException() { }
    public NegativeValueException(string message) : base(message) { }
    public NegativeValueException(string message, Exception inner) : base(message, inner) { }
}

class Program
{
    static void Main()
    {
        // === 1. Колекція об'єктів базового типу, заповнена наперед у коді ===
        List<Shape> shapes = new() { new Circle(5) };

        // === 2. Головний цикл меню ===
        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine("\n=== Меню ===");
            Console.WriteLine("0. Вихід");
            Console.WriteLine("1. Додати коло");
            Console.WriteLine("2. Показати всі фігури");

            int choice = ReadInt("Ваш вибір: ", 0, 2);

            isRunning = choice switch
            {
                1 => HandleAddCircle(shapes),
                2 => HandleShowAll(shapes),
                0 => false,
                _ => true // сюди потрапити неможливо: ReadInt уже обмежив ввід діапазоном 0-2
            };
        }

        Console.WriteLine("Роботу завершено.");
    }

    // === 3. Методи для кожного пункту меню ===

    static bool HandleAddCircle(List<Shape> shapes)
    {
        // Введення з клавіатури з валідацією (TryParse), обробка власного винятку
        try
        {
            double radius = ReadDouble("Радіус нового кола: ");
            shapes.Add(new Circle(radius));
        }
        catch (NegativeValueException ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }

        return true;
    }

    static bool HandleShowAll(List<Shape> shapes)
    {
        if (shapes.Count == 0)
        {
            Console.WriteLine("Немає даних.");
            return true;
        }

        // Демонстрація поліморфізму: виклик virtual/override-методу через базовий тип
        foreach (Shape shape in shapes)
        {
            shape.PrintInfo();
        }

        return true;
    }

    // === 4. Допоміжні методи введення ===

    static double ReadDouble(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();

            if (double.TryParse(input, out double value))
            {
                return value;
            }

            Console.WriteLine("Некоректне значення. Введіть число.");
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

Зверніть увагу на структуру: `Main` лише координує — читає вибір і викликає `Handle`-методи; кожен `Handle`-метод відповідає за один пункт меню, а перевірку допустимості значення винесено у властивості/конструктори похідних класів (`Circle`) — саме там і виникає власний виняток, а не в `Main`.

## Порядок виконання

1. Реалізувати базовий клас (з властивостями з валідацією) та похідні класи з перевизначенням поведінки.
2. Реалізувати інтерфейс(и) та їх використання поліморфно через колекцію.
3. Реалізувати власний тип винятку та продемонструвати його виникнення й обробку поряд з обробкою хоча б одного вбудованого винятку.
4. Написати консольне меню для демонстрації роботи всіх реалізованих сценаріїв, включно з пунктом додавання нового об'єкта до колекції — за зразком розділу «Приклади коду» вище.
5. Протестувати граничні та помилкові випадки (див. «Граничний випадок для обробки» у своєму варіанті), зафіксувати їх у звіті.

## Варіанти завдань

Кожен варіант описаний з конкретною структурою ієрархії класів, мінімальним переліком пунктів меню та граничним випадком.

### Варіант 1. Геометричні фігури

**Ієрархія:** абстрактний клас `Shape` (властивість `Name`) → похідні `Circle` (`Radius`), `Rectangle` (`Width`, `Height`).

**Обов'язкові перевизначення:**

- `GetArea()` (`abstract` у `Shape`): `Circle` — `Math.PI * Radius * Radius`; `Rectangle` — `Width * Height`.
- `GetPerimeter()` (`virtual` у `Shape`, базова реалізація не обов'язкова — можна теж зробити `abstract`): `Circle` — `2 * Math.PI * Radius`; `Rectangle` — `2 * (Width + Height)`.

**Мінімальне меню:**

0. Вихід.
1. Додати фігуру — обрати тип (коло / прямокутник) і ввести її розміри з валідацією; недопустимий розмір (0 або від'ємний) обробляється через виняток, а не аварійне завершення програми.
2. Показати всі фігури колекції (назва, площа, периметр).
3. Відсортувати фігури за площею та показати результат.
4. Показати сумарну площу та периметр колекції.

**Рекомендації щодо реалізації:**

- для сортування (пункт меню 3) варто реалізувати `IComparable<Shape>`, порівнюючи за `GetArea()` (`GetArea().CompareTo(other.GetArea())`) — природна, однозначна величина для впорядкування фігур будь-якого типу; тоді досить викликати `shapes.Sort()`;
- сумарну площу й пошук максимуму зручно обчислити одним проходом `foreach` по колекції;
- за бажанням можна додати пункт меню «Показати фігури певного типу» (фільтрація через `is`/патерн-матчинг).

**Граничний випадок для обробки:** спроба створити фігуру з недодатним розміром сторони/радіуса — конструктор має викинути виняток (`ArgumentOutOfRangeException` або власний), який меню обробляє повідомленням, не завершуючи програму.

### Варіант 2. Способи доставки замовлень

**Ієрархія:** абстрактний клас `DeliveryMethod` (`Name`, `RatePerKg`) → похідні `StandardDelivery` (без надбавок), `ExpressDelivery` (`SpeedMultiplier`, `Surcharge`).

**Обов'язкові перевизначення:**

- `CalculateCost(double weightKg)` (`abstract` у `DeliveryMethod`): `StandardDelivery` — `weightKg * RatePerKg`; `ExpressDelivery` — `weightKg * RatePerKg * SpeedMultiplier + Surcharge`.

**Мінімальне меню:**

0. Вихід.
1. Додати спосіб доставки до довідника — обрати тип (стандартна / експрес) і ввести його параметри (ставка за кг, надбавки) з валідацією.
2. Розрахувати вартість доставки — обрати спосіб зі списку, ввести вагу посилки з валідацією; вивести розраховану вартість.
3. Показати всі додані способи доставки довідника (назва, параметри).
4. Відсортувати сподоби доставки від найдешевшого до найдорожчого (див. рекомендації).

**Рекомендації щодо реалізації:**

- `IComparable<T>` вимагає порівняння без додаткових параметрів, а `CalculateCost` приймає вагу — тому для природного впорядкування варто додати властивість-довідник, наприклад `ReferenceCost => CalculateCost(1)` (вартість для контрольної ваги 1 кг), і реалізувати `IComparable<DeliveryMethod>` саме за нею; так сортування «від найдешевшого способу» має сенс без ручного введення ваги;

**Граничний випадок для обробки:** вага посилки, що дорівнює нулю, — вартість має коректно порахуватись (для `ExpressDelivery` це буде саме надбавка `Surcharge`, для `StandardDelivery` — 0), а не викидати помилку; помилкою вважається лише від'ємна вага.

### Варіант 3. Співробітники компанії

**Ієрархія:** базовий клас `Employee` (`FullName`, `BaseSalary`) → похідні `Manager` (`SubordinatesCount`), `Developer` (`Level`: Junior/Middle/Senior).

**Обов'язкові перевизначення:**

- `CalculateSalary()` (`virtual` у `Employee`, базова реалізація повертає `BaseSalary`): `Manager` — `BaseSalary + SubordinatesCount * BonusPerSubordinate`; `Developer` — `BaseSalary + LevelBonus` (фіксована надбавка залежно від рівня, наприклад Junior — 0, Middle — 5000, Senior — 10000).

**Мінімальне меню:**

0. Вихід.
1. Додати співробітника — обрати посаду й ввести дані (`BaseSalary` та специфічні для посади поля) з валідацією.
2. Показати список співробітників з розрахованою зарплатою (`CalculateSalary()`).
3. Показати сумарний фонд заробітної плати по всій колекції.
4. Показати співробітників відсортованих по їх заробітній платі (від найбільшої до найменшої).

**Рекомендації щодо реалізації:**

- властивість `BaseSalary` варто валідувати саме в `set` (заборона від'ємного або нульового значення), щоб некоректне значення не потрапило в об'єкт жодним шляхом;
- варто реалізувати `IComparable<Employee>`, порівнюючи за результатом `CalculateSalary()`, а не за `BaseSalary`, — так сортування й пошук максимуму (пункт 4) відображають фактичний дохід, а не базову ставку, і заразом перевіряють коректність перевизначених формул;
- за бажанням можна додати пункт «Показати середню зарплату по посаді» — групування через `foreach` з лічильниками або `LINQ` (тема наступної роботи, тут не обов'язково).

**Граничний випадок для обробки:** спроба встановити `BaseSalary` через властивість у від'ємне значення (наприклад, після завантаження некоректних даних) — властивість має викинути виняток, а не мовчки зберегти некоректне значення.

### Варіант 4. Банківські рахунки

**Ієрархія:** базовий клас `BankAccount` (`Owner`, `Balance`) → похідні `SavingsAccount` (`MinimumBalance` — мінімальний залишок, нижче якого не можна знімати кошти), `CheckingAccount` (`OverdraftLimit` — ліміт овердрафту).

**Обов'язкові перевизначення:**

- `Withdraw(double amount)` (`virtual` у `BankAccount`, базова реалізація забороняє зняття, що перевищує `Balance`): `SavingsAccount` — додатково забороняє зняття, після якого `Balance` опуститься нижче `MinimumBalance`; `CheckingAccount` — дозволяє зняття понад нульовий баланс у межах `OverdraftLimit` (тобто `Balance` може стати від'ємним, але не нижче `-OverdraftLimit`).

**Мінімальне меню:**

0. Вихід.
1. Додати рахунок — обрати тип (ощадний / поточний), ввести власника, початковий баланс та специфічний для типу параметр (`MinimumBalance` або `OverdraftLimit`) з валідацією.
2. Поповнити рахунок — обрати рахунок зі списку та внести суму.
3. Зняти кошти з рахунка — обрати рахунок і суму; перевищення дозволеної межі обробляється власним винятком, межа при цьому різна для ощадного й поточного рахунку.
4. Показати список усіх рахунків з поточним балансом за спаданням (від найбільшого до найменшого).

**Рекомендації щодо реалізації:**

- власний виняток `InsufficientFundsException` зручно викидати саме в `Withdraw` — у базовій реалізації при `amount > Balance`, а в кожному перевизначенні — при порушенні власної межі (`MinimumBalance` чи `-OverdraftLimit`);
- `Deposit(double amount)` можна залишити спільним для обох типів у базовому класі — перевизначення тут не потрібне;
- варто реалізувати `IComparable<BankAccount>`, порівнюючи за `Balance`, — природне впорядкування рахунків від найменшого залишку до найбільшого;
- за бажанням можна додати метод `ApplyInterest()` для `SavingsAccount`, що нараховує відсотки на поточний баланс, і пункт меню для переказу коштів між двома рахунками.

**Граничний випадок для обробки:** зняття, після якого баланс точно дорівнює дозволеній межі (`MinimumBalance` для ощадного рахунку, `-OverdraftLimit` для поточного), — операція має пройти успішно, а не викидати виняток; помилкою вважається лише перевищення цієї межі.

## Вимоги до звіту

Звіт з практичної роботи оформлюється у вигляді документа (PDF/DOCX) та має містити:

- тему, мету та номер варіанта завдання;
- короткий опис реалізованого функціоналу з лістингом;
- посилання на репозиторій GitHub з кодом проєкту;
- висновки (2–4 речення) щодо отриманих результатів.

## Контрольні питання

1. У чому різниця між абстрактним класом та інтерфейсом? Коли що застосовувати?
2. Навіщо потрібна інкапсуляція і чому властивість з валідацією в `set` краще за публічне поле?
3. Навіщо потрібне перевантаження конструкторів, якщо можна обійтися одним конструктором з параметрами за замовчуванням?
4. Як реалізується поліморфізм у C# на рівні виконання (пізнє зв'язування)? Наведіть приклад із власної ієрархії.
5. У чому різниця між `abstract`-методом і `virtual`-методом з реалізацією за замовчуванням?
6. Для чого призначений інтерфейс `IComparable<T>` і як його реалізація впливає на роботу `List<T>.Sort()`?
7. Що виконується у блоці `finally` і в яких випадках це критично важливо?
8. У чому різниця між обробкою винятку в `catch (SpecificException)` і `catch (Exception)`? Чому порядок гілок `catch` має значення?
9. Навіщо створювати власні класи винятків, якщо є вбудовані (`ArgumentException`, `InvalidOperationException` тощо)?
10. Чому власний клас винятку прийнято постачати трьома стандартними конструкторами?
11. Що станеться, якщо колекція базового типу (`List<Shape>`) міститиме похідний об'єкт, а виклик методу не позначено `virtual`/`override`? Чим це відрізняється від коректної поліморфної поведінки?
12. Чому колекцію об'єктів ієрархії варто типізувати базовим класом (`List<Shape>`), а не окремими списками для кожного похідного типу?
