using Microsoft.EntityFrameworkCore;
using CarService.DAL.Entities;

namespace CarService.DAL.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderLine> WorkOrderLines => Set<WorkOrderLine>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(SqliteUnicodeInterceptor.Instance);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

        // decimal зберігається як REAL: інакше EF порівнює й сумує decimal через власні функції,
        // які ламаються на системах з комою як десятковим роздільником (uk-UA).
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Каталог
        modelBuilder.Entity<ServiceCategory>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Service>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(10);
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.ServiceCategory)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Клієнти й наряди
        modelBuilder.Entity<Client>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<Client>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.Property(x => x.CarModel).HasMaxLength(100);
            e.Property(x => x.PlateNumber).HasMaxLength(10);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.PlateNumber);
            e.HasOne(x => x.Client)
                .WithMany(x => x.WorkOrders)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkOrderLine>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(500);
            e.HasIndex(x => new { x.WorkOrderId, x.ServiceId }).IsUnique();
            e.HasOne(x => x.WorkOrder)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Service)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureAuth(modelBuilder);
    }

    private static void ConfigureAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(20);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
