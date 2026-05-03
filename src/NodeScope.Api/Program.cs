using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Features;
using NodeScope.Api.Configuration;
using NodeScope.Api.Development;
using NodeScope.Api.Security;
using NodeScope.Application;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Configuration;
using NodeScope.Infrastructure;
using NodeScope.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(jsonOptions =>
    {
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swaggerGenOptions =>
{
    swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NodeScope IT/OT API",
        Description =
            "Platform API for analysing and validating OT/IT technical datasets orchestrated via CQRS + MediatR.",
        Version = "v1",
    });

    var bearerSchemeId = JwtBearerDefaults.AuthenticationScheme;

    swaggerGenOptions.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
    {
        Description = "Paste a JWT Bearer token prefixed with Bearer: `Bearer eyJhbGciOi...`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = bearerSchemeId,
        BearerFormat = "JWT",
    });

    swaggerGenOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = bearerSchemeId },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtBootstrap = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");
jwtBootstrap.EnsureConfigured();

builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

var connectionString =
    builder.Configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

var processingBootstrap =
    builder.Configuration.GetSection(ProcessingSettings.SectionName).Get<ProcessingSettings>() ?? new ProcessingSettings();

builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = processingBootstrap.MaxUploadBytes);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = processingBootstrap.MaxUploadBytes);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = processingBootstrap.MaxUploadBytes;
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddScoped<INodeScopeDbContext>(static provider => provider.GetRequiredService<AppDbContext>());

builder.Services.AddNodeScopeInfrastructureServices();
builder.Services.AddApplication();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBootstrap.SigningKey)),
            ValidateAudience = true,
            ValidAudience = jwtBootstrap.Audience,
            ValidateIssuer = true,
            ValidIssuer = jwtBootstrap.Issuer,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var exceptionFeature = context.HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error is not ValidationException validationException)
        {
            return;
        }

        context.ProblemDetails.Title = "Validation failed.";
        context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
        context.ProblemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        context.ProblemDetails.Extensions["errors"] =
            validationException.Errors
                .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName)
                    ? string.Empty
                    : error.PropertyName.Trim())
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Select(error => error.ErrorMessage).Distinct().ToArray());
    };
});

builder.Services.AddCors(policyBuilder =>
{
    policyBuilder.AddPolicy(
        "AngularSpaClients",
        policies =>
            policies.WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://127.0.0.1:4200",
                    "https://127.0.0.1:4200")
                .AllowAnyHeader()
                .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "NodeScope IT/OT API v1");
        ui.DocumentTitle = "NodeScope Swagger UI";
    });
}

app.UseExceptionHandler();
app.UseCors("AngularSpaClients");

// In Development the SPA targets http://localhost:5003. Redirecting to HTTPS switches origin/port and
// often breaks the Angular client (blocked preflight / untrusted dev cert) with HttpErrorResponse status 0.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync().ConfigureAwait(false);
}

await DevDataSeeder.SeedDevelopmentUserAsync(app).ConfigureAwait(false);

app.Run();
