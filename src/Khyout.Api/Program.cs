using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Khyout.Api.Authorization;
using Khyout.Api.Middleware;
using Khyout.Api.Security;
using Khyout.Application;
using Khyout.Application.Abstractions;
using Khyout.Infrastructure;
using Khyout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddEndpointsApiExplorer();

// Swagger (Development). Bearer tokens are supplied through client tooling; the
// Swagger security-requirement UI will be revisited when Swashbuckle/OpenApi 2.x
// APIs settle (Phase 4 polish).
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Khyout API", Version = "v1" });
});

// Authentication: JWT bearer; claims are sub / name / role / companyId.
// Options are configured through DI so the final (post-build) configuration is
// used — important for WebApplicationFactory-based test hosts.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        // Keep original claim names (sub / role / companyId) — no inbound claim mapping.
        options.MapInboundClaims = false;

        var jwtSection = configuration.GetSection("Auth:Jwt");
        var signingKey = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Auth:Jwt:SigningKey is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "khyout",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "khyout",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, VerifiedCompanyHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BuyerOnly", p => p.RequireClaim("role", "Buyer"));
    options.AddPolicy("SupplierOnly", p => p.RequireClaim("role", "Supplier"));
    options.AddPolicy("SupplierOrAdmin", p => p.RequireClaim("role", "Supplier", "Admin"));
    options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
    options.AddPolicy("VerifiedBuyer", p =>
    {
        p.RequireClaim("role", "Buyer");
        p.AddRequirements(new VerifiedCompanyRequirement());
    });
    options.AddPolicy("VerifiedSupplier", p =>
    {
        p.RequireClaim("role", "Supplier");
        p.AddRequirements(new VerifiedCompanyRequirement());
    });
});

// Rate limiting for OTP-issuing endpoints (per client IP).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("otp", context =>
    {
        var limitConfig = context.RequestServices.GetRequiredService<IConfiguration>()["RateLimiting:OtpPermitLimit"];
        var permitLimit = int.TryParse(limitConfig, out var parsed) ? parsed : 10;

        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = permitLimit,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Fail fast on missing auth configuration (checked after the host is built, so
// test-host overrides have been applied).
if (string.IsNullOrWhiteSpace(app.Configuration["Auth:Jwt:SigningKey"]))
{
    throw new InvalidOperationException(
        "Auth:Jwt:SigningKey is not configured. Set it via appsettings.Development.json or environment variables.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Json(new { status = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable));

app.Run();

/// <summary>Exposed for integration-test hosting (WebApplicationFactory).</summary>
public partial class Program { }
