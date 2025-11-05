using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace AlertSystem.API
{
    /// <summary>
    /// Configuration personnalisée pour Swagger
    /// </summary>
    public class SwaggerConfiguration
    {
        /// <summary>
        /// Configure Swagger avec les paramètres personnalisés
        /// </summary>
        public static void ConfigureSwagger(SwaggerGenOptions options)
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AlertSystem API",
                Version = "v1",
                Description = @"# AlertSystem API

Endpoints exposés dans cette documentation:
- Alertes: `/api/v1/alerts` (CRUD + update-state)
- Gestion des clés clients: `/api/v1/clients`

Toutes les requêtes protégées doivent inclure l'en-tête `X-API-KEY`.
",
                Contact = new OpenApiContact
                {
                    Name = "AlertSystem Team",
                    Email = "support@alertsystem.com",
                    Url = new Uri("https://github.com/alertsystem")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://alertsystem.com/terms")
            });

            // Configuration de l'authentification par clé API
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-KEY",
                Description = "Clé API requise pour l'authentification.\n\n" +
                    "Pour obtenir une clé API :\n" +
                    "1. Contactez l'administrateur système\n" +
                    "2. Ou créez un compte via : POST /api/v1/clients\n\n" +
                    "IMPORTANT : La boîte de dialogue Authorize n'effectue PAS de validation en temps réel.\n" +
                    "C'est le comportement normal de Swagger UI. La clé API est stockée côté client et sera validée\n" +
                    "par le serveur uniquement lors de l'appel à un endpoint API.\n\n" +
                    "Si vous entrez une clé invalide :\n" +
                    "- Le dialogue Authorize l'accepte (comportement normal)\n" +
                    "- Lors de l'appel à un endpoint, vous recevrez une erreur 401 Unauthorized\n" +
                    "- Vérifiez vos logs backend pour voir le message Invalid API key\n\n" +
                    "Pour tester votre clé : Utilisez l'endpoint GET /api/v1/keys/validate",
                Scheme = "ApiKeyScheme"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        },
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });

            // Inclure les commentaires XML
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Organiser par contrôleur
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            options.DocInclusionPredicate((name, api) => true);

            // Pas d'extensions/sections de test
        }
    }
}
