using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarService.DAL.Data;

/// <summary>
/// SQLite не зберігає DateTimeKind. Усі дати в базі — UTC, тому під час читання
/// їм повертається Kind = Utc, і в JSON вони виходять із суфіксом Z.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
