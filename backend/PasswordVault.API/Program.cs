using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PasswordVault.API.Middleware;
using PasswordVault.Application.Interfaces;
using PasswordVault.Application.Services;
using PasswordVault.Domain.Interfaces;
using PasswordVault.Infrastructure.Data;
using PasswordVault.Infrastructure.Data.Repositories;
using PasswordVault.Infrastructure.Logging;
using PasswordVault.Infrastructure.Security;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.RateLimiting;

// ─── Bootstrap logger ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/passwordvault-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/passwordvault-.log", rollingInterval: RollingInterval.Day));

    // ─── Configuration ─────────────────────────────────────────────────────
    builder.Services.Configure<SecuritySettings>(
        builder.Configuration.GetSection("Security"));

    // ─── Database ──────────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(3)));

    // ─── Repositories & Unit of Work ───────────────────────────────────────
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // ─── Infrastructure Services ───────────────────────────────────────────
    builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
    builder.Services.AddSingleton<ITokenService, JwtTokenService>();
    builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    builder.Services.AddScoped<IAuditService, AuditService>();

    // ─── Application Services ──────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAccountService, AccountService>();

    // ─── Authentication / JWT ──────────────────────────────────────────────
    var jwtSecret = builder.Configuration["Security:JwtSecret"]
        ?? throw new InvalidOperationException("Security:JwtSecret not configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Security:JwtIssuer"],
                ValidAudience            = builder.Configuration["Security:JwtAudience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew                = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // ─── CORS ──────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts =>
        opts.AddPolicy("Angular", p => p
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    // ─── Rate Limiting ─────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        opts.AddFixedWindowLimiter("auth", o =>
        {
            o.Window          = TimeSpan.FromMinutes(15);
            o.PermitLimit     = 20;
            o.QueueLimit      = 0;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ─── Controllers & Swagger ─────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title   = "Password Vault API",
            Version = "v1",
            Description = "Secure end-to-end encrypted password manager API"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT token"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ─── Health Checks ─────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: ["db", "sql"]);

    // ─── App Pipeline ──────────────────────────────────────────────────────
    var app = builder.Build();

    // Run EF migrations on startup (all environments in Docker)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var maxRetries = 10;
        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                Log.Information("Applying EF migrations (attempt {Attempt}/{Max})...", i, maxRetries);
                await db.Database.MigrateAsync();
                Log.Information("EF migrations applied successfully.");
                break;
            }
            catch (Exception ex) when (i < maxRetries)
            {
                Log.Warning(ex, "Migration attempt {Attempt} failed. Retrying in 5s...", i);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Password Vault API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseHttpsRedirection();

    // Security headers
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"]       = "nosniff";
        ctx.Response.Headers["X-Frame-Options"]              = "DENY";
        ctx.Response.Headers["X-XSS-Protection"]             = "1; mode=block";
        ctx.Response.Headers["Referrer-Policy"]              = "no-referrer";
        ctx.Response.Headers["Permissions-Policy"]           = "camera=(), microphone=(), geolocation=()";
        ctx.Response.Headers["Strict-Transport-Security"]    = "max-age=31536000; includeSubDomains";
        ctx.Response.Headers["Content-Security-Policy"]      = "default-src 'self'";
        await next();
    });

    app.UseCors("Angular");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
