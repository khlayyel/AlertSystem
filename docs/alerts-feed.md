# Flux d'alertes (Alert Feed) - Format et Tests

## Format JSON attendu (GET /api/v1/alerts-feed)
```json
{
  "alerts": [
    {
      "domaineId": 1,                // 1=Stock, 2=GRH, 3=BNQ, 4=Réservation, ...
      "title": "Alerte Stock",      // titre libre
      "description": "Article A1 épuisé.",
      "alertTypeId": 2,              // 1=Information, 2=Obligatoire
      "destinataires": [             // liste d'emails et/ou numéros (E.164) par ligne
        "khalilouerghemmi@gmail.com",
        "+21699414008"
      ]
    }
  ]
}
```

- Une ligne est créée par destinataire et par plateforme:
  - email -> PlateformeEnvoieId = 1
  - numéro (sans @) -> PlateformeEnvoieId = 2 (WhatsApp)
- Chaque alerte reçoit un `AlertGroupId` commun et des lignes indépendantes pour la livraison.
- Champs DB par ligne: `DomaineId`, `TypeId`, `TitreAlerte`, `DescriptionAlerte`, `StatutId=1`, `EtatId=1`, `PlateformeEnvoieId`, `Destinataire`, `ProcessedByWorker=0`, `AttemptCount=0`.

## Test rapide
1. API: lancer `AlertSystem.API`.
2. Optionnel: créer `AlertSystem.API/App_Data/alerts.json` avec le format ci-dessus; sinon un échantillon est renvoyé.
3. Vérifier le feed: `GET http://localhost:5002/api/v1/alerts-feed`.
4. Worker: lancer `AlertSystem.Worker`.
   - Le worker lit `Polling:Alerts:Endpoint` et insère les alertes dans `dbo.Alerte`.
   - `AlertSenderWorker` envoie email/WhatsApp/desktop et met à jour `StatutId`, `AttemptCount`, `ProcessedByWorker`.
5. Confirmation: le mail/WhatsApp comporte un lien `/confirm?t=...&id={AlertRecordId}` qui marque 1→2 (Lu) ou 3→4 (Obligatoire Confirmée).

## Configuration Worker
- `appsettings.json` / `appsettings.Development.json`:
```json
"Polling": {
  "Alerts": {
    "Enabled": true,
    "IntervalSeconds": 10,
    "Endpoint": "http://localhost:5002/api/v1/alerts-feed"
  }
}
```
