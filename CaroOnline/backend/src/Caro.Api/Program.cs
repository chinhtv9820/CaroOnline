using System.Linq;
using System.Text;
using Caro.Infrastructure;
using Caro.Services;
using Caro.Api.Hubs;
using Caro.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Log configuration sources for debugging
var env = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {env}");
Console.WriteLine($"Configuration sources: {string.Join(", ", configuration.Sources.Select(s => s.GetType().Name))}");

// DbContext
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: Connection string 'DefaultConnection' not found!");
    Console.WriteLine($"Available connection strings: {string.Join(", ", configuration.GetSection("ConnectionStrings").GetChildren().Select(c => c.Key))}");
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
Console.WriteLine($"Connection string loaded: Server={connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Server="))?.Substring(7)}");
builder.Services.AddDbContext<CaroDbContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging() // Enable detailed error logging
           .EnableDetailedErrors()); // Enable detailed error messages

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton<PresenceService>();
builder.Services.AddSingleton<ChallengeService>();
builder.Services.AddSingleton<GameTimerService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = new List<string>
        {
            "http://localhost",
            "http://localhost:80",
            "http://localhost:5173",
            "http://127.0.0.1",
            "http://127.0.0.1:80",
            "http://127.0.0.1:5173"
        };
        
        // Thêm origins từ config (cho phép nhiều IP)
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            origins.AddRange(allowedOrigins);
        }
        
        // Luôn cho phép mọi origin trong development để dễ test từ nhiều IP
        // Kiểm tra nhiều cách để detect development mode
        var isDevelopment = builder.Environment.IsDevelopment() || 
                           builder.Environment.EnvironmentName == "Development" ||
                           builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
        
        Console.WriteLine($"[CORS Config] Environment: {builder.Environment.EnvironmentName}");
        Console.WriteLine($"[CORS Config] IsDevelopment: {builder.Environment.IsDevelopment()}");
        Console.WriteLine($"[CORS Config] ASPNETCORE_ENVIRONMENT: {builder.Configuration["ASPNETCORE_ENVIRONMENT"]}");
        Console.WriteLine($"[CORS Config] Final isDevelopment: {isDevelopment}");
        
        // Tạm thời luôn cho phép mọi origin để test (có thể bỏ sau khi fix xong)
        // Cho phép bất kỳ origin nào (vẫn cần credentials)
        policy.SetIsOriginAllowed(origin => {
            Console.WriteLine($"[CORS] Allowing origin: {origin}");
            return true;
        })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR with JWT
    });
});

// Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Auth
var jwtKey = configuration["Jwt:Key"] ?? "super_secret_dev_key_change_me";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "Caro";
var jwtAudience = configuration["Jwt:Audience"] ?? "CaroClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // SignalR support for JWT via querystring access_token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].ToString();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/game"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<GameHub>("/hub/game");

// Configure URLs
var port = configuration["Server:Port"] ?? "8080";
// Luôn bind 0.0.0.0 để accept connections từ mọi IP (cho development và testing)
// Trong production, nên dùng reverse proxy (nginx, IIS) thay vì bind trực tiếp
var host = "0.0.0.0";
app.Urls.Add($"http://{host}:{port}");
Console.WriteLine($"Server listening on http://{host}:{port}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Access from other devices: http://[YOUR_IP]:{port}");

app.Run();



