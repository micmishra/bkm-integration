using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BKM.Utility;
using BKM.Utility.Domain.Features.Auth.Entities;
using BKM.Utility.Infrastructure.Persistence;
using BKM.Utility.Infrastructure.Features.AppLog.Logging;
using BKM.Utility.Api.Features.ApiResponse.Filters;
using BKM.Utility.Api.Features.ApiResponse.Middleware;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

// ── Register encoding provider for ExcelDataReader (required for legacy .xls files) ──
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// ── Bootstrap logger (captures startup errors before DI is ready) ──────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog: replace built-in logging with Serilog (file + console always on) ──
    builder.Host.UseSerilog((ctx, services, config) =>
        SerilogConfigurator
            .Build(ctx.Configuration, services)
            .CreateLogger());

    // ── BKM.Utility package: registers all features in one call ───────────
    builder.Services.AddBkmUtility(builder.Configuration);

    // ── HttpClient factory (required by AuthService for OAuth2 code exchange) ──
    builder.Services.AddHttpClient();

    // ── Authentication & Authorization ────────────────────────────────────────
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero
        };
    })
    .AddOpenIdConnect("Google", options =>
    {
        options.ClientId     = builder.Configuration["Sso:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Sso:Google:ClientSecret"] ?? "";
        options.Authority    = "https://accounts.google.com";
        options.CallbackPath = "/signin-google";
        options.SaveTokens   = true;
    })
    .AddOpenIdConnect("Microsoft", options =>
    {
        var tenantId = builder.Configuration["Sso:Microsoft:TenantId"] ?? "common";
        options.ClientId     = builder.Configuration["Sso:Microsoft:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Sso:Microsoft:ClientSecret"] ?? "";
        options.Authority    = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.CallbackPath = "/signin-microsoft";
        options.SaveTokens   = true;
    });

    builder.Services.AddAuthorization(options =>
    {
        // RBAC: built-in role policies
        options.AddPolicy("AdminOnly",     p => p.RequireRole("Admin"));
        options.AddPolicy("ModeratorPlus", p => p.RequireRole("Admin", "Moderator"));
        // ABAC: claim-based permission policies
        options.AddPolicy("CanCreatePost",  p => p.RequireClaim("permission", "posts:create"));
        options.AddPolicy("CanManageUsers", p => p.RequireClaim("permission", "users:manage"));
        options.AddPolicy("CanSendEmail",   p => p.RequireClaim("permission", "email:send"));
        options.AddPolicy("CanViewLogs",    p => p.RequireClaim("permission", "logs:view"));
    });

    // ── ASP.NET Core ───────────────────────────────────────────────────────────
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "BKM Utility API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In          = ParameterLocation.Header,
            Description = "Enter: Bearer {token}",
            Name        = "Authorization",
            Type        = SecuritySchemeType.ApiKey,
            Scheme      = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                []
            }
        });
    });

    var app = builder.Build();

    // ── Auto-apply EF Core migrations on startup (one per feature DbContext) ───
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;

        // Each DbContext is independent — migrate in dependency order
        sp.GetRequiredService<CacheDbContext>()        .Database.Migrate(); // cache first: other features depend on it
        sp.GetRequiredService<AuthDbContext>()         .Database.Migrate();
        sp.GetRequiredService<EmailDbContext>()        .Database.Migrate();
        sp.GetRequiredService<FeedDbContext>()         .Database.Migrate();
        sp.GetRequiredService<AppLogDbContext>()       .Database.Migrate();
        sp.GetRequiredService<UrlShortenerDbContext>() .Database.Migrate();
        sp.GetRequiredService<FileIngestionDbContext>().Database.Migrate();

        // ── Seed default roles and admin user (Auth DbContext) ─────────────────
        var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in new[] { "Admin", "Moderator", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole
                {
                    Name        = role,
                    Description = $"Built-in {role} role",
                    CreatedAt   = DateTime.UtcNow
                });
        }

        if (await userManager.FindByEmailAsync("admin@bkm.local") is null)
        {
            var admin = new AppUser
            {
                UserName       = "admin@bkm.local",
                Email          = "admin@bkm.local",
                DisplayName    = "System Admin",
                IsActive       = true,
                CreatedAt      = DateTime.UtcNow,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@12345");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    // ── Middleware pipeline ────────────────────────────────────────────────────
    app.UseGlobalExceptionHandler();       // ← FIRST: catches all unhandled exceptions
    app.UseSerilogRequestLogging(opts =>   // ← logs every HTTP request automatically
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();   // ← must come before UseAuthorization
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BKM.Utility failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
