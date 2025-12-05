using GidroAtlas.Api.Entities;
using GidroAtlas.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GidroAtlas.Api.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<WaterObject> WaterObjects { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Login)
                .HasColumnName("login")
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>();

            entity.HasIndex(e => e.Login).IsUnique();
        });

        modelBuilder.Entity<WaterObject>(entity =>
        {
            entity.ToTable("water_objects");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Region)
                .HasColumnName("region")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ResourceType)
                .HasColumnName("resource_type")
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.WaterType)
                .HasColumnName("water_type")
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.HasFauna)
                .HasColumnName("fauna")
                .IsRequired();

            entity.Property(e => e.PassportDate)
                .HasColumnName("passport_date")
                .IsRequired();

            entity.Property(e => e.TechnicalCondition)
                .HasColumnName("technical_condition")
                .IsRequired();

            entity.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            entity.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .IsRequired();

            entity.Property(e => e.PdfUrl)
                .HasColumnName("pdf_url")
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Priority)
                .HasColumnName("priority")
                .IsRequired();
        });

        SeedUsers(modelBuilder);
        SeedWaterObjects(modelBuilder);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        // Все пароли: password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
        
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Login = "guest",
                PasswordHash = passwordHash,
                Role = Role.Guest
            },
            new User
            {
                Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                Login = "expert",
                PasswordHash = passwordHash,
                Role = Role.Expert
            },
            new User
            {
                Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                Login = "admin",
                PasswordHash = passwordHash,
                Role = Role.Expert
            }
        );
    }

    private static void SeedWaterObjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WaterObject>().HasData(
            new WaterObject
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Озеро Балхаш",
                Region = "Алматинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2023, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 46.5f,
                Longitude = 74.8f,
                PdfUrl = "/documents/balkhash.pdf",
                Priority = 8
            },
            new WaterObject
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Капшагайское водохранилище",
                Region = "Алматинская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 43.87f,
                Longitude = 77.08f,
                PdfUrl = "/documents/kapshagay.pdf",
                Priority = 7
            },
            new WaterObject
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Иртыш-Караганда канал",
                Region = "Карагандинская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2021, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 2,
                Latitude = 49.8f,
                Longitude = 73.1f,
                PdfUrl = "/documents/irtysh-karaganda.pdf",
                Priority = 9
            },
            new WaterObject
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Аральское море",
                Region = "Кызылординская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.NonFresh,
                HasFauna = false,
                PassportDate = new DateTime(2023, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 1,
                Latitude = 45.0f,
                Longitude = 59.5f,
                PdfUrl = "/documents/aral.pdf",
                Priority = 10
            },
            new WaterObject
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Бухтарминское водохранилище",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 49.2f,
                Longitude = 84.3f,
                PdfUrl = "/documents/bukhtarma.pdf",
                Priority = 5
            },
            new WaterObject
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Озеро Зайсан",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2023, 7, 12, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 5,
                Latitude = 48.0f,
                Longitude = 84.0f,
                PdfUrl = "/documents/zaysan.pdf",
                Priority = 3
            },
            new WaterObject
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Шардаринское водохранилище",
                Region = "Туркестанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2021, 9, 30, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 41.2f,
                Longitude = 67.9f,
                PdfUrl = "/documents/shardara.pdf",
                Priority = 6
            },
            new WaterObject
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Name = "Большой Алматинский канал",
                Region = "Алматинская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2022, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 43.25f,
                Longitude = 76.95f,
                PdfUrl = "/documents/bak.pdf",
                Priority = 4
            }
        );
    }
}
