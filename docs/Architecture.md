## Architecture AlertSystem (Refonte Multi-domaine)

### Vision
Système d’alertes générique multi‑domaine (Stock, GRH, BNQ, …), avec un modèle unifié des destinataires et un Worker générique qui poll des URLs configurées en base, génère et envoie des alertes (Email/WhatsApp), et les rend visibles par défaut dans le dashboard (desktop implicite).

### Couches et responsabilités
- Entities/DataLayer: entités EF Core, `ApplicationDbContext`, mappings.
- Repository/Service: logique applicative, envoi, audit, CRUD.
- API: endpoints publics, middleware clé API, confirm.
- WEB: authentification, Dashboard, CRUD Admin (SignalR).
- Worker: `AlertPoller`, `AlertSenderWorker`, services auxiliaires (résolution destinataires, templates).

### Modèle de données
- `def_App(AppId, Description)` — domaines/applications.
- `def_TypeAlerte(TypeAlertId, AppId, Description, TemplateKey?, TemplateBody?)` — config des types.
- `def_Utilisateur(UtilisateurId, Username, Email, Password, AppId, WhatsAppNumber)` — identité unifiée.
- `def_Alerte(DefAlerteId, DefTypeAlerte, URL, IsActive, ListDestinatairesId)` — sources à poller et destinataires cibles.
- `Alerte` — événements à envoyer, avec:
  - `AppId`, `TypeEnvoieId`, `StatutId`, `EtatId`, `PlateformeEnvoieId`, `Destinataire`
  - `TitreAlerte`, `DescriptionAlerte`, `DateCreationAlerte`, `DateLecture`
  - `ProcessedByWorker`, `AttemptCount`, `AlertGroupId`

### Flux principal
1) Admin crée des lignes `def_Alerte` (URL + type + destinataires).
2) Worker `AlertPoller` lit `def_Alerte` actives:
   - Fetch JSON depuis `URL`.
   - Détermine le template via `def_TypeAlerte`.
   - Construit `TitreAlerte`/`DescriptionAlerte`.
   - Résout destinataires (emails/WhatsApp) à partir de `def_Utilisateur`.
   - Insère lignes `Alerte` (une par destinataire et plateforme).
3) `AlertSenderWorker` consomme `Alerte`:
   - Envoie Email/WhatsApp selon `PlateformeEnvoieId` et `Destinataire`.
   - Ajoute lien de confirmation `/confirm?t=...`.
   - Met à jour `StatutId`, `AttemptCount`, `ProcessedByWorker`.
4) Dashboard affiche par défaut toutes les alertes reçues (desktop implicite).

### API
- `/api/v1/stock-alerts` — sert le JSON `stock_alerts` (feed local de dev).
- `/api/v1/alerts` — récupération/filtrage d’alertes.
- `/confirm` — confirmation publique (token).
- Clés API via `ApiClients`. Exemptions: `/api/v1/stock-alerts`, `/confirm`.

### Sécurité
- Middleware d’API Key; gestion des cas manquants/inactifs/expirés.
- Tokens de confirmation signés avec secret.

### UI Admin (temps réel)
- CRUD `DefApp`, `DefTypeAlerte`, `DefAlerte`, `DefUtilisateur`.
- Recherche instantanée + pagination async (`GET .../list?q=&page=&pageSize=`).
- SignalR: broadcast `AdminConfigUpdated` (Create/Edit/Delete).

### Choix techniques
- EF Core pour la majorité; ADO.NET ciblé dans `AlertSenderWorker` pour éviter `DbContext` concurrent.
- Services DI clairs par domaine (polling, templating, résolution destinataires).
- Nettoyage du legacy (tests/seed/webpush/stats).

### Maintenabilité
- Respect SRP/DRY/SOLID.
- Configuration minimale dans `appsettings.*` (templates email/WhatsApp, endpoints worker).
- Logs précis (envoi, token, erreurs HTTP/SQL) pour faciliter le debug.


