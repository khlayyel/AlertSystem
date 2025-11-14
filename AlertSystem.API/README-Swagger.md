# 📡 AlertSystem API - Documentation Swagger

## 🚀 Démarrage Rapide

### 1. Lancer l'API avec Swagger
```bash
# Depuis la racine du projet AlertSystem
dotnet run --project AlertSystem.API
```

### 2. Accéder à Swagger UI
Une fois l'API démarrée, ouvrez votre navigateur et allez à :
- **Swagger UI** : http://localhost:5050/
- **JSON Swagger** : http://localhost:5050/swagger/v1/swagger.json

## 🔧 Configuration Swagger

### Fonctionnalités Activées
✅ **Swagger UI** à la racine (`/`)  
✅ **Authentification par clé API** (`X-API-KEY`)  
✅ **Documentation complète** avec exemples  
✅ **Commentaires XML** intégrés  
✅ **Organisation par contrôleurs**  
✅ **Exemples de réponses**  
✅ **Codes d'erreur documentés**  

### Profils de Lancement
- **http** : Lance sur http://localhost:5050 avec navigateur
- **https** : Lance sur https://localhost:7002 avec navigateur  
- **swagger** : Lance directement sur l'interface Swagger

## 📋 Endpoints Documentés

### 🚨 **AlertsController** (`/api/v1/alerts`)
- `GET /{id}` - Récupérer une alerte spécifique
- `GET /` - Lister les alertes avec filtres
- `POST /` - Créer et envoyer une alerte
- `POST /{id}/read` - Marquer comme lue

### 🔑 **KeysController** (`/api/v1/keys`)
- `GET /validate` - Valider une clé API
- `POST /test` - Tester une clé API

### 👥 **ClientsController** (`/api/v1/clients`)
- `POST /` - Créer un nouveau client API
- `GET /` - Lister les clients
- `GET /{id}` - Détails d'un client
- `PATCH /{id}/activate` - Activer un client
- `PATCH /{id}/deactivate` - Désactiver un client
- `PATCH /{id}/rate-limit` - Modifier la limite de taux
- `DELETE /{id}` - Supprimer un client

### 🧪 **TestController** (`/api/v1/test`)
- `GET /ping` - Test de connectivité
- `GET /health` - État de santé
- `GET /database` - Test de base de données

### 📱 **WhatsAppTestController** (`/api/v1/whatsapptest`)
- `POST /send` - Tester l'envoi WhatsApp
- `GET /config` - Configuration WhatsApp

### 🔧 **WhatsAppDebugController** (`/api/v1/whatsapp-debug`)
- `GET /config` - Configuration pour débogage
- `POST /test-free-form` - Test messages libres
- `POST /test-template` - Test templates
- `POST /test-direct-api` - Test API directe
- `GET /error-codes` - Codes d'erreur WhatsApp

### 🌐 **WebPushController** (`/api/v1/webpush`)
- `GET /vapid-public-key` - Clé publique VAPID
- `POST /subscribe` - Abonnement notifications
- `POST /unsubscribe` - Désabonnement
- `POST /test-notification` - Notification de test
- `GET /user/{userId}/devices` - Appareils utilisateur

### 🌱 **SeedController** (`/api/v1/seed`)
- `POST /database` - Initialiser la base
- `GET /status` - État de la base

### 📊 **HistoriqueAlerteController** (`/api/v1/historiquealerte`)
- `GET /` - Historique des alertes
- `GET /stats` - Statistiques de lecture
- `POST /mark-read/{destId}` - Marquer comme lu
- `GET /by-alert/{alerteId}` - Par alerte

## 🔐 Authentification

### Clé API Requise
La plupart des endpoints nécessitent l'en-tête :
```
X-API-KEY: votre-clé-api-ici
```

### Endpoints Exemptés
Ces endpoints ne nécessitent **PAS** de clé API :
- `/api/v1/test/*` - Tests de connectivité
- `/api/v1/seed/*` - Initialisation base
- `/api/v1/clients` (POST) - Création client
- `/api/v1/keys/test` - Test de clé
- `/api/v1/webpush/*` - Notifications push

## 🧪 Tests dans Swagger UI

### 1. Tester la Connectivité
```
GET /api/v1/test/ping
```

### 2. Créer un Client API
```
POST /api/v1/clients
{
  "name": "Test Client",
  "rateLimitPerMinute": 600
}
```

### 3. Valider la Clé API
```
GET /api/v1/keys/validate
Header: X-API-KEY: votre-clé-générée
```

### 4. Créer une Alerte
```
POST /api/v1/alerts
Header: X-API-KEY: votre-clé-générée
{
  "title": "Test Alert",
  "message": "Message de test",
  "alertType": "acquittementNécessaire",
  "expedType": "Service",
  "appId": 1,
  "recipients": [
    {"recipientId": "test@example.com"},
    {"recipientId": "+1234567890"}
  ]
}
```

## 📊 Codes de Statut

| Code | Description |
|------|-------------|
| 200 | Succès complet |
| 201 | Ressource créée |
| 207 | Succès partiel (certaines notifications échouées) |
| 400 | Données invalides |
| 401 | Clé API manquante/invalide |
| 403 | Clé API inactive ou limite dépassée |
| 404 | Ressource non trouvée |
| 500 | Erreur serveur |

## 🔧 Configuration Avancée

### Personnalisation Swagger
Le fichier `SwaggerConfiguration.cs` contient :
- Description détaillée de l'API
- Configuration de l'authentification
- Exemples de réponses
- Filtres personnalisés
- Organisation par contrôleurs

### Commentaires XML
Les commentaires XML sont générés automatiquement et intégrés dans Swagger grâce à :
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

## 🚀 Déploiement

### Production
Pour la production, désactivez Swagger en modifiant `Program.cs` :
```csharp
if (app.Environment.IsDevelopment())
{
    // Configuration Swagger uniquement en développement
}
```

### Docker
L'API est prête pour le déploiement Docker avec Swagger configuré pour l'environnement de développement.

## 📝 Notes Importantes

1. **Swagger UI** est accessible à la racine (`/`) pour faciliter l'accès
2. **Authentification** est configurée avec le bouton "Authorize" dans Swagger UI
3. **Exemples** sont fournis pour tous les endpoints principaux
4. **Codes d'erreur** sont documentés avec explications
5. **Tests** peuvent être effectués directement depuis l'interface

## 🎯 Prochaines Étapes

1. **Tester tous les endpoints** via Swagger UI
2. **Générer des clés API** pour les clients externes
3. **Documenter les cas d'usage** spécifiques
4. **Configurer l'authentification** pour la production

---

**AlertSystem API** est maintenant entièrement documenté avec Swagger ! 🎉
