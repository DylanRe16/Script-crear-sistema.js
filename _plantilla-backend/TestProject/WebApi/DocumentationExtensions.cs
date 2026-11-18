using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace PlantillaBackend.WebApi;

/// <summary>
/// Configuración modular activa de Swagger y Scalar para la documentación interactiva de la API.
/// </summary>
public static class DocumentationExtensions
{
    /// <summary>
    /// Registra los servicios de OpenAPI 3.1.0 y SwaggerGen con autenticación JWT Bearer.
    /// </summary>
    public static IServiceCollection AddDocumentationServices(this IServiceCollection services)
    {
        // 1. Especificación nativa OpenAPI 3.1.0 de ASP.NET Core
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "PlantillaBackend API - Autenticación y Perfil";
                document.Info.Version = "v1 (OpenAPI 3.1.0)";
                document.Info.Description = "API de autenticación, perfil y preguntas de seguridad bajo Clean Architecture.";
                return Task.CompletedTask;
            });
        });

        services.AddEndpointsApiExplorer();

        // 2. Swagger / Swashbuckle con esquema de seguridad Bearer JWT y comentarios XML
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PlantillaBackend API - Autenticación y Perfil",
                Version = "v1",
                Description = "API de autenticación, perfil y preguntas de seguridad bajo Clean Architecture."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el token JWT obtenido en /auth/login en el formato: Bearer {token}"
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Configura el pipeline interactivo de Scalar y Swagger UI.
    /// </summary>
    public static WebApplication UseDocumentation(this WebApplication app, string pathBase)
    {
        // 1. Endpoint nativo OpenAPI 3.1.0 (/openapi/v1.json)
        app.MapOpenApi();

        // 2. Interfaz interactiva Scalar (optimizada y moderna para OpenAPI 3.1.0 en /scalar/v1)
        app.MapScalarApiReference(options =>
        {
            options.Title = "PlantillaBackend API - Pruebas Interactivas (OpenAPI 3.1.0)";
            options.Theme = ScalarTheme.Purple;
        });

        // 3. Swagger UI clásico apuntando al documento OpenAPI 3.1.0
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{pathBase}/openapi/v1.json", "PlantillaBackend API (OpenAPI 3.1.0)");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "PlantillaBackend API - Swagger UI (OpenAPI 3.1.0)";
        });

        // Redirección de la raíz del prefijo (/PlantillaBackend/api) hacia Scalar API Reference
        app.MapGet("/", (HttpContext context) => Results.Redirect($"{context.Request.PathBase}/scalar/v1"))
           .ExcludeFromDescription();

        return app;
    }
}
