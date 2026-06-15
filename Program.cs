using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using Pm.Data;
using Pm.Services;
using Pm.Helper;
using Pm.Middleware;
using Pm.DTOs;
using Pm.Validators;
using Pm.DTOs.Auth;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
}




// ✅ FORCE application timezone to UTC
TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "UTC");

// ===== Add Controllers =====
builder.Services.AddControllers(options =>
{
    // ✅ Register ResponseWrapperFilter globally
    options.Filters.Add<ResponseWrapperFilter>();

})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

    // ✅ OPTIONAL: Ignore null values for cleaner response
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PM MKN API",
        Version = "v1",
        Description = "API PM & Documentation"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Gunakan format: Bearer {token}",
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ===== Database Context =====
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Priority: Environment Variable > appsettings.json
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("Connection string tidak ditemukan.");

    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
            mySqlOptions.CommandTimeout(180);
            mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }
    );

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.WebHost.UseUrls("http://*:5116");

// ===== JWT Authentication =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey tidak ditemukan.");


var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? jwtSettings["Issuer"];

var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? jwtSettings["Audience"];

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Di development (localhost), matikan HTTPS requirement agar SignalR bisa connect via ws://
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)
        ),

        ValidateIssuer = true,
        ValidIssuer = issuer,        // ✅ INI YANG DIGANTI
        ValidateAudience = true,
        ValidAudience = audience,    // ✅ INI YANG DIGANTI

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // If the request is for our hub...
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ===== Authorization =====
builder.Services.AddAuthorization(options =>
{
    options.AddCustomAuthorizationPolicies();
});

// ===== Services =====
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<ICallRecordService, CallRecordService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IInspeksiTemuanKpcService, InspeksiTemuanKpcService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();


// ===== Validators =====
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();

// ===== Signal NEC ===== 
builder.Services.AddScoped<INecSignalService, NecSignalService>();
builder.Services.AddScoped<IInternalLinkService, InternalLinkService>();
// ===== SWR Radio ===== 
builder.Services.AddScoped<ISwrSignalService, SwrSignalService>();

// ===== Letter Numbering System =====
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ILetterNumberService, LetterNumberService>();

// ===== Gatepass & Quotation =====
builder.Services.AddScoped<IGatepassService, GatepassService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();

// ===== KPI Monitoring =====
builder.Services.AddScoped<IKpiDocumentService, KpiDocumentService>();

// ===== Division Master Data =====
builder.Services.AddScoped<IDivisionService, DivisionService>();

// ===== Radio Management =====
builder.Services.AddScoped<Pm.Services.Radio.IRadioService, Pm.Services.Radio.RadioService>();
builder.Services.AddScoped<Pm.Services.Media.IImageBase64Validator, Pm.Services.Media.ImageBase64Validator>();
builder.Services.AddScoped<Pm.Services.RadioRepairJob.IRadioRepairJobService, Pm.Services.RadioRepairJob.RadioRepairJobService>();
builder.Services.AddScoped<Pm.Services.RepairJobCustomStatus.IRepairJobCustomStatusService, Pm.Services.RepairJobCustomStatus.RepairJobCustomStatusService>();
builder.Services.AddScoped<Pm.Services.RadioHandover.IRadioHandoverService, Pm.Services.RadioHandover.RadioHandoverService>();
builder.Services.AddScoped<Pm.Services.WarehousePartBorrow.IWarehousePartBorrowService, Pm.Services.WarehousePartBorrow.WarehousePartBorrowService>();
builder.Services.AddScoped<Pm.Services.WarehousePartBorrow.IWarehousePartCatalogService, Pm.Services.WarehousePartBorrow.WarehousePartCatalogService>();
builder.Services.AddScoped<Pm.Services.IWorkshopTechnicianService, Pm.Services.WorkshopTechnicianService>();

// PM Schedule
builder.Services.AddScoped<Pm.Services.PmSchedule.IPmSiteService, Pm.Services.PmSchedule.PmSiteService>();
builder.Services.AddScoped<Pm.Services.PmSchedule.IPmScheduleService, Pm.Services.PmSchedule.PmScheduleService>();

// ===== CCTV KPC =====
builder.Services.AddScoped<Pm.Services.CctvKpc.ICctvKpcService, Pm.Services.CctvKpc.CctvKpcService>();

// ===== Notification =====
builder.Services.AddScoped<Pm.Services.Notification.INotificationService, Pm.Services.Notification.NotificationService>();
builder.Services.AddHostedService<Pm.Services.Notification.NotificationCleanupService>();

// ===== Cloudinary =====
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));

// ===== External Integrations =====
builder.Services.AddHttpClient<ISihepiIntegrationService, SihepiIntegrationService>();

builder.Services.AddHttpContextAccessor();

// ===== Permission Claims (DB-based, bukan dari JWT token) =====
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, PermissionClaimsTransformer>();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;

            var uri = new Uri(origin);
            var host = uri.Host;

            // Exact allowed origins
            var exactAllowed = new[]
            {
                "pm.mknops.web.id",
                "pmfrontend.vercel.app",
                "pmdocmkn-web.vercel.app",
                "v0.dev",
                "localhost",
            };

            if (exactAllowed.Contains(host)) return true;

            // Wildcard: *.vercel.app
            if (host.EndsWith(".vercel.app")) return true;

            // Wildcard: *.vusercontent.net (v0.dev preview URLs)
            if (host.EndsWith(".vusercontent.net")) return true;

            // Wildcard: *.mknops.web.id
            if (host.EndsWith(".mknops.web.id")) return true;

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});




// Enable detailed model binding errors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning("❌ Model validation failed: {@Errors}",
            context.ModelState);

        return new BadRequestObjectResult(context.ModelState);
    };
});

// ===== Logging =====
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824;
});

// ===== SignalR =====
builder.Services.AddSignalR();

var app = builder.Build();

// ===== Middleware =====
// Swagger aktif di semua environment untuk keperluan testing internal
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PM MKN API V1"));

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ErrorHandlingMiddleware>();

// Jangan redirect HTTPS di development — menyebabkan SignalR WebSocket gagal di localhost
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ===== SEEDING (Development Only) =====
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Pastikan tabel sudah ada → JIKA BELUM, JALANKAN MIGRASI DULU!
        // Jika Anda belum buat migrasi, ganti dengan:
        // await context.Database.EnsureCreatedAsync();
        // TAPI LEBIH BAIK PAKAI MIGRASI

        await context.SeedInitialDataAsync(logger);
        logger.LogInformation("✅ Seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Seeding failed.");
    }
}

app.UseRequestLogging();


app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ===== Map SignalR Hub =====
app.MapHub<Pm.Hubs.NotificationHub>("/hubs/notification");

// ===== Debug endpoint — test broadcast SignalR (development only) =====
if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/signalr-broadcast/{entity}", async (
        string entity,
        Pm.Services.Notification.INotificationService notifService) =>
    {
        await notifService.BroadcastRefreshDataAsync(entity);
        return Results.Ok(new { message = $"Broadcast '{entity}' sent to all clients", at = DateTime.UtcNow });
    });
}

app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
app.Logger.LogInformation("DB Connection String: {Conn}", builder.Configuration.GetConnectionString("DefaultConnection"));

app.Run();