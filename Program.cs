using System.Text;
using AI_Resume_Analyzer_API.Domain.Entities;
using AI_Resume_Analyzer_API.Infrastructure.AI;
using AI_Resume_Analyzer_API.Infrastructure.Database;
using AI_Resume_Analyzer_API.Infrastructure.Middleware;
using AI_Resume_Analyzer_API.Infrastructure.Parsers;
using AI_Resume_Analyzer_API.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://chennappan0923.github.io")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure DbContext with fallback to InMemory database if SQL Server is offline or unconfigured
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool useInMemory = string.IsNullOrWhiteSpace(connectionString) || 
                   connectionString.Contains("YOUR_") ||
                   connectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase);

if (!useInMemory)
{
    try
    {
        // Quickly test if local SQL Server is running (timeout after 2 seconds)
        var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 2
        };
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(connBuilder.ConnectionString);
        conn.Open();
    }
    catch
    {
        useInMemory = true;
    }
}

if (useInMemory)
{
    builder.Services.AddDbContext<ResumeAnalyzerDbContext>(options =>
        options.UseInMemoryDatabase("ResumeAnalyzerDb"));
}
else
{
    builder.Services.AddDbContext<ResumeAnalyzerDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKeyForResumeAnalyzerSolutionSecretKey123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ResumeAnalyzerApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ResumeAnalyzerUi";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Register custom dependencies
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<PdfParserService>();
builder.Services.AddSingleton<DocxParserService>();
builder.Services.AddHttpClient<GeminiAIService>();
builder.Services.AddHttpClient<OllamaAIService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddScoped<IAIService, FallbackAIService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Resume Analyzer API", Version = "v1" });
    
    // Add Security Definition for JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created and seed hardcoded user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ResumeAnalyzerDbContext>();
        context.Database.EnsureCreated();

        // Seed demo/admin user
        if (!context.Users.Any(u => u.Email == "admin@example.com"))
        {
            context.Users.Add(new User
            {
                FullName = "Administrator",
                Email = "admin@example.com",
                PasswordHash = PasswordHasher.HashPassword("Admin123!")
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing/seeding the database fallback.");
    }
}

app.Run();

