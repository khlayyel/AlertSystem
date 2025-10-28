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
                Description = @"
# AlertSystem API - Documentation Complète

## 🚀 Vue d'ensemble
API complète pour le système de gestion d'alertes AlertSystem, permettant l'envoi de notifications multi-canaux (Email, WhatsApp, Desktop Push) avec gestion des confirmations et rappels automatiques.

## 🔐 Authentification
La plupart des endpoints nécessitent une clé API dans l'en-tête `X-API-KEY`. 
- **Endpoints exemptés** : `/api/v1/test/*`, `/api/v1/seed/*`, `/api/v1/clients` (POST), `/api/v1/keys/test`
- **Obtenir une clé** : Contactez l'administrateur ou utilisez `/api/v1/clients` pour créer un nouveau client

## 📡 Endpoints Principaux

### 🚨 Alertes (`/api/v1/alerts`)
- **POST** : Créer et envoyer une alerte
- **GET** : Lister les alertes avec filtres
- **GET /{id}** : Récupérer une alerte spécifique
- **POST /{id}/read** : Marquer comme lue

### 🔑 Gestion des Clés (`/api/v1/keys`)
- **GET /validate** : Valider une clé API
- **POST /test** : Tester une clé API

### 👥 Clients API (`/api/v1/clients`)
- **POST** : Créer un nouveau client
- **GET** : Lister les clients
- **GET /{id}** : Détails d'un client
- **PATCH /{id}/activate** : Activer un client
- **PATCH /{id}/deactivate** : Désactiver un client

### 🧪 Tests (`/api/v1/test`)
- **GET /ping** : Test de connectivité
- **GET /health** : État de santé
- **GET /database** : Test de base de données

### 📱 WhatsApp (`/api/v1/whatsapptest`, `/api/v1/whatsapp-debug`)
- Tests et débogage des messages WhatsApp
- Configuration et codes d'erreur

### 🌐 WebPush (`/api/v1/webpush`)
- Gestion des notifications push desktop
- Abonnement/désabonnement des utilisateurs

## 📊 Codes de Statut
- **200** : Succès complet
- **201** : Ressource créée
- **207** : Succès partiel (certaines notifications échouées)
- **400** : Données invalides
- **401** : Clé API manquante/invalide
- **403** : Clé API inactive ou limite dépassée
- **404** : Ressource non trouvée
- **500** : Erreur serveur

## 🔧 Exemples d'Utilisation

### Créer une alerte :
```bash
curl -X POST ""http://localhost:5002/api/v1/alerts"" \
  -H ""Content-Type: application/json"" \
  -H ""X-API-KEY: votre-clé-api"" \
  -d '{
    ""title"": ""Alerte Système"",
    ""message"": ""Défaillance détectée"",
    ""alertType"": ""acquittementNécessaire"",
    ""expedType"": ""Service"",
    ""appId"": 1,
    ""recipients"": [
      {""recipientId"": ""admin@hotel.com""},
      {""recipientId"": ""+1234567890""}
    ]
  }'
```

### Valider une clé API :
```bash
curl -X GET ""http://localhost:5002/api/v1/keys/validate"" \
  -H ""X-API-KEY: votre-clé-api""
```
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

            // Personnaliser les schémas
            options.SchemaFilter<SwaggerSchemaFilter>();
            options.OperationFilter<SwaggerOperationFilter>();
        }
    }

    /// <summary>
    /// Filtre pour personnaliser les schémas Swagger
    /// </summary>
    public class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(DateTime))
            {
                schema.Format = "date-time";
                schema.Description = "Date et heure au format ISO 8601";
            }
            else if (context.Type == typeof(DateTime?))
            {
                schema.Format = "date-time";
                schema.Description = "Date et heure au format ISO 8601 (optionnel)";
            }
        }
    }

    /// <summary>
    /// Filtre pour personnaliser les opérations Swagger
    /// </summary>
    public class SwaggerOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Ajouter des exemples de réponses
            if (context.MethodInfo.Name == "Create")
            {
                operation.Responses.Add("207", new OpenApiResponse
                {
                    Description = "Succès partiel - Certaines notifications ont échoué",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Example = new Microsoft.OpenApi.Any.OpenApiObject
                            {
                                ["alertId"] = new Microsoft.OpenApi.Any.OpenApiInteger(123),
                                ["overallSuccess"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                                ["results"] = new Microsoft.OpenApi.Any.OpenApiArray
                                {
                                    new Microsoft.OpenApi.Any.OpenApiObject
                                    {
                                        ["type"] = new Microsoft.OpenApi.Any.OpenApiString("email"),
                                        ["recipient"] = new Microsoft.OpenApi.Any.OpenApiString("user@example.com"),
                                        ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                                        ["error"] = new Microsoft.OpenApi.Any.OpenApiNull()
                                    },
                                    new Microsoft.OpenApi.Any.OpenApiObject
                                    {
                                        ["type"] = new Microsoft.OpenApi.Any.OpenApiString("whatsapp"),
                                        ["recipient"] = new Microsoft.OpenApi.Any.OpenApiString("+1234567890"),
                                        ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                                        ["error"] = new Microsoft.OpenApi.Any.OpenApiString("Template not approved")
                                    }
                                }
                            }
                        }
                    }
                });
            }

            // Ajouter des descriptions pour les paramètres
            foreach (var parameter in operation.Parameters)
            {
                switch (parameter.Name.ToLower())
                {
                    case "page":
                        parameter.Description = "Numéro de page (commence à 1)";
                        break;
                    case "size":
                        parameter.Description = "Nombre d'éléments par page (max 100)";
                        break;
                    case "sort":
                        parameter.Description = "Champ de tri (title, type, status, dateCreation)";
                        break;
                    case "order":
                        parameter.Description = "Ordre de tri (asc, desc)";
                        break;
                }
            }
        }
    }
}
