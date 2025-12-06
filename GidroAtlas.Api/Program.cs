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
