# AlertSystem - Refonte Multi-domaine

Ce dépôt contient une refonte complète d'AlertSystem pour supporter un modèle d’alertes multi‑domaine, avec unifier les destinataires (Email/WhatsApp) et un worker de polling générique piloté par la table `def_Alerte`.

## Projets
- AlertSystem.API: API REST (alerts, ingestion, feed `stock_alerts`, clés API, confirm).
- AlertSystem.WEB: UI (dashboard, CRUD Admin temps réel).
- AlertSystem.Worker: Poller générique et sender des alertes (Email/WhatsApp).
- AlertSystem.Service, Repository, DataLayer, Entities: couches applicatives.

## Schéma de données (principaux)
- def_App(AppId, Description)
- def_TypeAlerte(TypeAlertId, AppId, Description, TemplateKey?, TemplateBody?)
- def_Utilisateur(UtilisateurId, Username, Email, Password, AppId, WhatsAppNumber)
- def_Alerte(DefAlerteId, DefTypeAlerte, URL, IsActive, ListDestinatairesId json[])
- Alerte(AlertRecordId, AppId, TypeEnvoieId, StatutId, EtatId, PlateformeEnvoieId, Destinataire, TitreAlerte, DescriptionAlerte, DateCreationAlerte, DateLecture, ProcessedByWorker, AttemptCount, AlertGroupId)

## Points clés
- Un seul champ `Destinataire` et `PlateformeEnvoieId` (1=Email, 2=WhatsApp). Le “desktop” est implicite via le dashboard.
- Polling générique: le Worker lit `def_Alerte`, consomme les URLs, génère titre/description selon `def_TypeAlerte`, résout les destinataires via `def_Utilisateur`.
- Middleware d’API Key qui exempte `/api/v1/stock-alerts` et `/confirm`.
- Endpoints legacy de test/seed supprimés (pas masqués).
- UI Admin avec recherche instantanée, pagination asynchrone, mises à jour temps réel (SignalR).

## Lancer en Dev
1) dotnet build
2) API
   - `cd AlertSystem.API`
   - `set ASPNETCORE_ENVIRONMENT=Development`
   - `dotnet run --urls http://localhost:5050`
3) WEB
   - `cd AlertSystem.WEB`
   - `set ASPNETCORE_ENVIRONMENT=Development`
   - `dotnet run --urls http://localhost:5000`
4) Worker
   - `cd AlertSystem.Worker`
   - `set ASPNETCORE_ENVIRONMENT=Development`
   - `dotnet run`

Assurez-vous que la chaîne de connexion pointe vers `AlertDB` et que l’API écoute l’URL configurée dans le Worker (`Polling:Alerts:Endpoint` si utilisé).

## Tests rapides
- GET `http://localhost:5050/swagger` (uniquement endpoints nécessaires).
- GET `http://localhost:5050/api/v1/stock-alerts` (structure `stock_alerts`).
- Dashboard WEB (http://localhost:5000), authentification via `def_Utilisateur`.
- Confirmation `/confirm?t=...`.

## Sécurité
- Clés API gérées via `ApiClients`.
- Liens de confirmation signés (token) pointant vers `/confirm`.

## Maintenance
- Respect SOLID/DRY/SRP.
- Services injectés via DI, EF Core pour accès base, ADO.NET ciblé dans le Worker pour éviter la concurrence `DbContext`.


