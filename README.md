# Документація до дисципліни «Розробка проєктів засобами платформи .NET»

Спеціальність 121, 3 курс

## Структура репозиторію

| № | Тема | Модуль | Посилання |
|---|---|---|---|
| 0 | Налаштування робочого середовища (.NET 10 SDK, IDE, Git, GitHub) | — (вступна) | [00-setup](./00-setup) |
| 1 | Базові конструкції C# | 1 | [01-csharp-basics](./01-csharp-basics) |
| 2 | ООП та обробка виключень | 1 | [02-oop-exceptions](./02-oop-exceptions) |
| 3 | Узагальнення, делегати, події та лямбда-вирази | 1 | [03-generics-delegates-events](./03-generics-delegates-events) |
| 4 | LINQ to Objects | 1 | [04-linq-to-objects](./04-linq-to-objects) |
| 5 | Основи ASP.NET Core, EF Core та SQLite: перший CRUD Web API | 2 | [05-aspnetcore-efcore-sqlite-crud](./05-aspnetcore-efcore-sqlite-crud) |
| 6 | Проєктування та розробка REST API | 2 | [06-rest-api](./06-rest-api) |
| 7 | Автентифікація та авторизація (JWT) | 2 | [07-auth-jwt](./07-auth-jwt) |
| 8 | Архітектурні патерни та тестування | 2 | [08-architecture-testing](./08-architecture-testing) |

## Як користуватись

- **ПР №1–4** (Модуль 1) — окремі самостійні консольні застосунки C#/.NET, кожна робота в окремому проєкті.
- **ПР №5–8** (Модуль 2) — єдиний наскрізний мікропроєкт (Web API на ASP.NET Core + EF Core + SQLite), що нарощується від роботи до роботи в межах одного проєкту/репозиторію. Домен обирається один раз у ПР №5 (за таблицею варіантів) і використовується без змін у ПР №6–8.
- Почніть з [00-setup](./00-setup) — гайд по налаштуванню робочого середовища для виконання практичних робіт, який вклчає у себе встановлення dotnet-sdk, IDE (Visual Studio/Rider/VS Code) та поради по роботі з Git

## Технологічний стек

.NET 10 SDK · C# · ASP.NET Core Web API · Entity Framework Core (SQLite) · JWT (JwtBearer) · OpenAPI (Microsoft.AspNetCore.OpenApi) + Scalar UI · xUnit + Moq · (опційно) MediatR/CQRS