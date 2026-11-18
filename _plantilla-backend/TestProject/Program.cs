using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PlantillaBackend.Application.UseCases.Auth;
using PlantillaBackend.Application.UseCases.PreguntasSeguridad;
using PlantillaBackend.Application.UseCases.Profile;
using PlantillaBackend.Application.UseCases.Users;
using PlantillaBackend.Data;
using PlantillaBackend.Data.Persistence;
using PlantillaBackend.Domain.Abstractions;
using PlantillaBackend.Domain.Entities;
using PlantillaBackend.WebApi;
using PlantillaBackend.WebApi.Middleware;
using PlantillaBackend.WebApi.Services;

// Soporte para timestamp without time zone en PostgreSQL con Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// =========================================================================
// 0. Carga automática de Variables de Entorno desde archivo .env (si existe)
// =========================================================================
var envCandidates = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "Backend", "TestProject", ".env")
};

foreach (var envFile in envCandidates)
{
    if (File.Exists(envFile))
    {
        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value[1..^1];
                }
                Environment.SetEnvironmentVariable(key, value);
            }
        }
        break;
    }
}

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. Controladores y Serialización JSON
// =========================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddAuthorization();
builder.Services.Configure<PasswordResetOptions>(
    builder.Configuration.GetSection(PasswordResetOptions.SectionName));

// =========================================================================
// 2. Contexto de Usuario Actual y Persistencia Data (EF Core + Repositorios)
// =========================================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("PostgresConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión a la base de datos.");

// Registro modular de persistencia (ApplicationDbContext + Repositorios de Dominio)
builder.Services.AddDataPersistence(connectionString);

// =========================================================================
// 3. Casos de Uso del Dominio PlantillaBackend (Capa Application)
// =========================================================================
// Servicios de Seguridad, Autenticación y Perfil
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IPreguntaSeguridadService, PreguntaSeguridadService>();
builder.Services.AddScoped<IFuncionarioPreguntaSeguridadService, FuncionarioPreguntaSeguridadService>();

// =========================================================================
// 4. Configuración de CORS
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// =========================================================================
// 5. Documentación Interactiva Swagger y Scalar (Capa WebApi)
// =========================================================================
builder.Services.AddDocumentationServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var yaExiste = await dbContext.Funcionarios.AnyAsync(
        funcionario =>
            funcionario.Snacionalidad == "V" &&
            funcionario.NcedulaIdent == 12345678);

    if (!yaExiste)
    {
        var rolAdministrador = await dbContext.Roles.SingleOrDefaultAsync(
            rol => rol.Sdescripcion == "Administrador" && rol.Benabled);

        if (rolAdministrador is null)
        {
            throw new InvalidOperationException(
                "No existe el rol activo 'Administrador'. Ejecute init.sql antes de iniciar la aplicación.");
        }

        var funcionario = new Funcionario
        {
            Snacionalidad = "V",
            NcedulaIdent = 12345678,
            SprimerNombre = "Administrador",
            SprimerApellido = "Sistema",
            RolId = rolAdministrador.IdRol,
            Benabled = true,
            NusuarioCreacion = 1,
            DfechaCreacion = DateTime.UtcNow
        };

        var passwordHasher = new PasswordHasher<Funcionario>();
        funcionario.SpasswordHash = passwordHasher.HashPassword(funcionario, "Admin123!");

        dbContext.Funcionarios.Add(funcionario);
        await dbContext.SaveChangesAsync();
    }
}

// =========================================================================
// 6. Pipeline HTTP, Middleware y Subruta (PathBase)
// =========================================================================
// Soporte para proxy inverso Nginx (Headers X-Forwarded-*)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

// Subruta base para toda la API y blindaje contra accesos fuera del prefijo
var pathBase = builder.Configuration["PathBase"] ?? "/PlantillaBackend/api";
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);

    // Blindaje: Rechazar peticiones que no pasen por la subruta base configurada
    app.Use(async (context, next) =>
    {
        if (string.IsNullOrEmpty(context.Request.PathBase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await next();
    });
}

// Middlewares de WebApi
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors("AllowAll");
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<ExhortacionPreguntasSeguridadMiddleware>();
app.UseAuthorization();

// Documentación interactiva Swagger y Scalar activa en WebApi
app.UseDocumentation(pathBase);

app.MapControllers();

app.Run();
