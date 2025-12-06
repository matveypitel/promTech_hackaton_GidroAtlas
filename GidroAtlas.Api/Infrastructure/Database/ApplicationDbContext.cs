using GidroAtlas.Api.Entities;
using GidroAtlas.Api.Infrastructure.Database.Converters;
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
    public DbSet<WaterObjectEmbedding> WaterObjectEmbeddings { get; set; } = null!;
    public DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

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
                .HasConversion(new EnumDisplayNameConverter<Role>());

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
                .HasConversion(new EnumDisplayNameConverter<ResourceType>());

            entity.Property(e => e.WaterType)
                .HasColumnName("water_type")
                .IsRequired()
                .HasConversion(new EnumDisplayNameConverter<WaterType>());

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

        // WaterObjectEmbedding configuration for pgvector
        modelBuilder.Entity<WaterObjectEmbedding>(entity =>
        {
            entity.ToTable("water_object_embeddings");

            entity.HasKey(e => new { e.WaterObjectId, e.ChunkIndex });

            entity.Property(e => e.WaterObjectId)
                .HasColumnName("water_object_id");

            entity.Property(e => e.ChunkIndex)
                .HasColumnName("chunk_index");

            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired();

            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .IsRequired()
                .HasColumnType("vector(768)"); // nomic-embed-text produces 768-dim vectors

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasOne(e => e.WaterObject)
                .WithMany()
                .HasForeignKey(e => e.WaterObjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create IVFFlat index for efficient similarity search
            entity.HasIndex(e => e.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
        });

        // DocumentEmbedding configuration for pgvector (standalone documents like PDFs)
        modelBuilder.Entity<DocumentEmbedding>(entity =>
        {
            entity.ToTable("document_embeddings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.DocumentName)
                .HasColumnName("document_name")
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(500);

            entity.Property(e => e.ChunkIndex)
                .HasColumnName("chunk_index");

            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired();

            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .IsRequired()
                .HasColumnType("vector(768)");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Create IVFFlat index for efficient similarity search
            entity.HasIndex(e => e.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");

            // Index for filtering by document name
            entity.HasIndex(e => e.DocumentName);
            
            // Index for filtering by content type
            entity.HasIndex(e => e.ContentType);
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
            // === ОЗЁРА ===
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345001"),
                Name = "Озеро Балхаш",
                Region = "Алматинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh, // западная часть пресная, восточная солёная
                HasFauna = true,
                PassportDate = new DateTime(2023, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 46.54f,
                Longitude = 74.88f,
                PdfUrl = "/documents/balkhash.pdf",
                Priority = 5
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345002"),
                Name = "Аральское море (Северный Арал)",
                Region = "Кызылординская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.NonFresh,
                HasFauna = false,
                PassportDate = new DateTime(2020, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 1,
                Latitude = 46.78f,
                Longitude = 61.04f,
                PdfUrl = "/documents/aral.pdf",
                Priority = 18
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345003"),
                Name = "Озеро Зайсан",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2023, 7, 12, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 5,
                Latitude = 48.00f,
                Longitude = 84.00f,
                PdfUrl = "/documents/zaysan.pdf",
                Priority = 2
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345004"),
                Name = "Озеро Алаколь",
                Region = "Алматинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.NonFresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 6, 20, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 46.15f,
                Longitude = 81.70f,
                PdfUrl = "/documents/alakol.pdf",
                Priority = 5
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345005"),
                Name = "Озеро Тенгиз",
                Region = "Акмолинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.NonFresh,
                HasFauna = true,
                PassportDate = new DateTime(2021, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 50.40f,
                Longitude = 68.90f,
                PdfUrl = "/documents/tengiz.pdf",
                Priority = 10
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345006"),
                Name = "Озеро Маркаколь",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2023, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 5,
                Latitude = 48.77f,
                Longitude = 85.75f,
                PdfUrl = "/documents/markakol.pdf",
                Priority = 1
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345007"),
                Name = "Большое Алматинское озеро",
                Region = "Алматинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 5,
                Latitude = 43.05f,
                Longitude = 76.98f,
                PdfUrl = "/documents/bao.pdf",
                Priority = 0
            },
            new WaterObject
            {
                Id = Guid.Parse("a1b2c3d4-1234-5678-9abc-def012345008"),
                Name = "Озеро Боровое (Бурабай)",
                Region = "Акмолинская область",
                ResourceType = ResourceType.Lake,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 5,
                Latitude = 53.07f,
                Longitude = 70.28f,
                PdfUrl = "/documents/borovoe.pdf",
                Priority = 1
            },
            
            // === ВОДОХРАНИЛИЩА ===
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456001"),
                Name = "Капшагайское водохранилище",
                Region = "Алматинская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 43.90f,
                Longitude = 77.18f,
                PdfUrl = "/documents/kapshagay.pdf",
                Priority = 8
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456002"),
                Name = "Бухтарминское водохранилище",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 49.17f,
                Longitude = 84.07f,
                PdfUrl = "/documents/bukhtarma.pdf",
                Priority = 5
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456003"),
                Name = "Шардаринское водохранилище",
                Region = "Туркестанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2021, 9, 30, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 41.23f,
                Longitude = 67.97f,
                PdfUrl = "/documents/shardara.pdf",
                Priority = 10
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456004"),
                Name = "Шульбинское водохранилище",
                Region = "Восточно-Казахстанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2020, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 2,
                Latitude = 50.07f,
                Longitude = 81.45f,
                PdfUrl = "/documents/shulbinsk.pdf",
                Priority = 14
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456005"),
                Name = "Коксарайское водохранилище",
                Region = "Туркестанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2023, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 43.95f,
                Longitude = 66.93f,
                PdfUrl = "/documents/koksaray.pdf",
                Priority = 4
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456006"),
                Name = "Сергеевское водохранилище",
                Region = "Северо-Казахстанская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2019, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 2,
                Latitude = 53.88f,
                Longitude = 67.42f,
                PdfUrl = "/documents/sergeevka.pdf",
                Priority = 17
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456007"),
                Name = "Вячеславское водохранилище",
                Region = "Акмолинская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2021, 11, 8, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 51.18f,
                Longitude = 71.43f,
                PdfUrl = "/documents/vyacheslavka.pdf",
                Priority = 9
            },
            new WaterObject
            {
                Id = Guid.Parse("b2c3d4e5-2345-6789-abcd-ef0123456008"),
                Name = "Куртинское водохранилище",
                Region = "Алматинская область",
                ResourceType = ResourceType.Reservoir,
                WaterType = WaterType.Fresh,
                HasFauna = true,
                PassportDate = new DateTime(2022, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 3,
                Latitude = 43.72f,
                Longitude = 76.57f,
                PdfUrl = "/documents/kurty.pdf",
                Priority = 8
            },
            
            // === КАНАЛЫ ===
            new WaterObject
            {
                Id = Guid.Parse("c3d4e5f6-3456-789a-bcde-f01234567001"),
                Name = "Канал Иртыш-Караганда",
                Region = "Карагандинская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2021, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 2,
                Latitude = 49.80f,
                Longitude = 73.10f,
                PdfUrl = "/documents/irtysh-karaganda.pdf",
                Priority = 12
            },
            new WaterObject
            {
                Id = Guid.Parse("c3d4e5f6-3456-789a-bcde-f01234567002"),
                Name = "Большой Алматинский канал",
                Region = "Алматинская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2022, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 4,
                Latitude = 43.35f,
                Longitude = 77.05f,
                PdfUrl = "/documents/bak.pdf",
                Priority = 5
            },
            new WaterObject
            {
                Id = Guid.Parse("c3d4e5f6-3456-789a-bcde-f01234567003"),
                Name = "Арысь-Туркестанский канал",
                Region = "Туркестанская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2020, 6, 25, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 2,
                Latitude = 42.50f,
                Longitude = 68.25f,
                PdfUrl = "/documents/arys-turkestan.pdf",
                Priority = 14
            },
            new WaterObject
            {
                Id = Guid.Parse("c3d4e5f6-3456-789a-bcde-f01234567004"),
                Name = "Кызылординский магистральный канал",
                Region = "Кызылординская область",
                ResourceType = ResourceType.Canal,
                WaterType = WaterType.Fresh,
                HasFauna = false,
                PassportDate = new DateTime(2018, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                TechnicalCondition = 1,
                Latitude = 44.85f,
                Longitude = 65.50f,
                PdfUrl = "/documents/kyzylorda-canal.pdf",
                Priority = 21
            }
        );
    }
}
