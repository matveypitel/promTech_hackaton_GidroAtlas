using GidroAtlas.Api.Abstractions;
using GidroAtlas.Api.Handlers;
using GidroAtlas.Api.Infrastructure.Auth;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.AI.Chat;
using GidroAtlas.Api.Infrastructure.AI.Ollama;
using GidroAtlas.Api.Infrastructure.AI.Rag;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Api.Infrastructure.Documents;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;
using GidroAtlas.Api.Infrastructure.ML;
using GidroAtlas.Api.Options;
using GidroAtlas.Api.Services;
using GidroAtlas.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(AppConstants.ConnectionStringName),
        npgsqlOptions => npgsqlOptions.UseVector()));

// JWT configuration
var jwtSettings = builder.Configuration.GetSection(AppConstants.Jwt.SectionName);
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings[AppConstants.Jwt.SecretKey] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings[AppConstants.Jwt.Issuer],
        ValidateAudience = true,
        ValidAudience = jwtSettings[AppConstants.Jwt.Audience],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWaterObjectService, WaterObjectService>();
builder.Services.AddSingleton<PredictionService>();

// Ollama configuration for RAG chat
var ollamaSettings = builder.Configuration.GetSection("Ollama");
builder.Services.Configure<OllamaSettings>(ollamaSettings);

// Register HttpClient for Ollama services
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddHttpClient<ILlmService, OllamaLlmService>();

// Register RAG and Chat services
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddScoped<IDocumentIndexingService, DocumentIndexingService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Background service for auto-indexing water objects
builder.Services.AddHostedService<IndexingBackgroundService>();

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, GuestAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.GuestOnly, policy => 
    {
        policy.Requirements.Add(new GuestRequirement());
    })
    .AddPolicy(AuthPolicies.ExpertOnly, policy => policy.RequireRole(Roles.Expert));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Configure CORS
var corsSettings = builder.Configuration.GetSection(AppConstants.Cors.SectionName);
var allowedOrigins = corsSettings.GetSection(AppConstants.Cors.AllowedOrigins).Get<string[]>() 
    ?? throw new InvalidOperationException("CORS AllowedOrigins is not configured");

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppConstants.CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
    
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GidroAtlas API",
        Version = "v1",
        Description = "API for water resources monitoring system"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token in format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    var sharedXmlFile = "GidroAtlas.Shared.xml";
    var sharedXmlPath = Path.Combine(AppContext.BaseDirectory, sharedXmlFile);
    options.IncludeXmlComments(sharedXmlPath);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Seed HydroTechnicalStructures if none exist
    if (!dbContext.WaterObjects.Any(w => w.ResourceType == GidroAtlas.Shared.Enums.ResourceType.HydroTechnicalStructure))
    {
        var structures = new List<GidroAtlas.Api.Entities.WaterObject>();
        var random = new Random();

        var realStructures = new[]
        {
            new { Name = "Бухтарминская ГЭС", Region = "Восточно-Казахстанская область", Lat = 49.62f, Lng = 83.52f },
            new { Name = "Усть-Каменогорская ГЭС", Region = "Восточно-Казахстанская область", Lat = 49.97f, Lng = 82.68f },
            new { Name = "Шульбинская ГЭС", Region = "Восточно-Казахстанская область", Lat = 50.39f, Lng = 81.09f },
            new { Name = "Капчагайская ГЭС", Region = "Алматинская область", Lat = 43.91f, Lng = 77.08f },
            new { Name = "Шардаринская ГЭС", Region = "Туркестанская область", Lat = 41.25f, Lng = 67.96f },
            new { Name = "Мойнакская ГЭС", Region = "Алматинская область", Lat = 43.19f, Lng = 78.96f },
            new { Name = "Сергеевский гидроузел", Region = "Северо-Казахстанская область", Lat = 53.70f, Lng = 67.28f },
            new { Name = "Вячеславское водохранилище (Плотина)", Region = "Акмолинская область", Lat = 51.08f, Lng = 71.98f },
            new { Name = "Бартогайское водохранилище (Плотина)", Region = "Алматинская область", Lat = 43.36f, Lng = 78.50f },
            new { Name = "Тасоткельское водохранилище (Плотина)", Region = "Жамбылская область", Lat = 43.60f, Lng = 73.68f }
        };

        foreach (var item in realStructures)
        {
            var condition = random.Next(1, 6); // Randomize condition for demo variety
            
            structures.Add(new GidroAtlas.Api.Entities.WaterObject
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                Region = item.Region,
                ResourceType = GidroAtlas.Shared.Enums.ResourceType.HydroTechnicalStructure,
                WaterType = GidroAtlas.Shared.Enums.WaterType.Fresh,
                HasFauna = false,
                PassportDate = DateTime.UtcNow.AddYears(-random.Next(5, 40)),
                TechnicalCondition = condition,
                Latitude = item.Lat,
                Longitude = item.Lng,
                PdfUrl = "#",
                Priority = condition >= 4 ? 5 : (condition >= 2 ? 3 : 1)
            });
        }
        
        dbContext.WaterObjects.AddRange(structures);
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRouting();

// Enable CORS
app.UseCors(AppConstants.CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
