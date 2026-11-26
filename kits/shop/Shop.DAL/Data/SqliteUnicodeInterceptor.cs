using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop.DAL.Data;

/// <summary>
/// Вбудовані функції SQLite lower() і upper() змінюють регістр лише для латиниці.
/// Цей перехоплювач під час відкриття з'єднання замінює їх на версії .NET,
/// тому запит на кшталт p.Name.ToLower().Contains(search.ToLower()) працює і для кирилиці.
/// </summary>
public sealed class SqliteUnicodeInterceptor : DbConnectionInterceptor
{
    public static readonly SqliteUnicodeInterceptor Instance = new();

    private SqliteUnicodeInterceptor()
    {
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Register(connection);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
        {
            sqlite.CreateFunction("lower", (string? value) => value?.ToLowerInvariant(), isDeterministic: true);
            sqlite.CreateFunction("upper", (string? value) => value?.ToUpperInvariant(), isDeterministic: true);
        }
    }
}
