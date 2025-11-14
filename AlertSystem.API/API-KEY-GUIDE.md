# 🔑 Guide Complet - Génération de Clés API AlertSystem

## 🚀 **Méthode 1 : Via Swagger UI (Recommandée)**

### Étapes détaillées :
1. **Démarrez l'API** :
   ```bash
   dotnet run --project AlertSystem.API
   ```

2. **Ouvrez Swagger UI** :
   - Allez à : http://localhost:5050/
   - L'interface Swagger s'affiche automatiquement

3. **Créez un client API** :
   - Trouvez la section **"Clients"** dans Swagger
   - Cliquez sur **POST /api/v1/clients**
   - Cliquez sur **"Try it out"**
   - Modifiez le JSON d'exemple :
     ```json
     {
       "name": "Mon Hotel",
       "rateLimitPerMinute": 600
     }
     ```
   - Cliquez sur **"Execute"**

4. **Récupérez votre clé** :
   - Dans la réponse, copiez la valeur de `apiKey`
   - **IMPORTANT** : Cette clé ne sera affichée qu'une seule fois !

### Exemple de réponse :
```json
{
  "apiClientId": 1,
  "name": "Mon Hotel",
  "apiKey": "a1b2c3d4e5f6...",
  "isActive": true,
  "rateLimitPerMinute": 600,
  "createdAt": "2025-01-24T10:30:00Z"
}
```

---

## 🔧 **Méthode 2 : Via PowerShell**

### Si l'API fonctionne :
```powershell
# Créer le corps de la requête
$body = @{
    name = "Mon Hotel"
    rateLimitPerMinute = 600
} | ConvertTo-Json

# Envoyer la requête
$response = Invoke-RestMethod -Uri "http://localhost:5050/api/v1/clients" -Method POST -Body $body -ContentType "application/json"

# Afficher la clé API
Write-Host "Votre clé API : $($response.apiKey)" -ForegroundColor Green
```

### Si l'API ne fonctionne pas :
```powershell
# Utilisez le script PowerShell
.\scripts\generate-api-key-simple.ps1 -Name "Mon Hotel"
```

---

## 🧪 **Méthode 3 : Clé Temporaire pour Tests**

### Clé de test (non persistante) :
```
X-API-KEY: c3d721829c9f418dbed23a025a43eece2ae7584a84cd404783290a919674bf16
```

⚠️ **ATTENTION** : Cette clé n'est pas enregistrée en base de données et ne fonctionnera que pour les tests locaux.

---

## 🔐 **Utilisation de la Clé API**

### Dans Swagger UI :
1. Cliquez sur le bouton **"Authorize"** en haut à droite
2. Entrez votre clé dans le champ **"X-API-KEY"**
3. Cliquez sur **"Authorize"**
4. Testez vos endpoints !

### Dans curl :
```bash
curl -H "X-API-KEY: votre-clé-ici" \
     -H "Content-Type: application/json" \
     http://localhost:5050/api/v1/alerts
```

### Dans PowerShell :
```powershell
$headers = @{
    "X-API-KEY" = "votre-clé-ici"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri "http://localhost:5050/api/v1/alerts" -Headers $headers
```

---

## 🧪 **Test de votre Clé API**

### Via Swagger UI :
1. Allez à **GET /api/v1/keys/validate**
2. Cliquez sur **"Try it out"**
3. Cliquez sur **"Execute"**
4. Vérifiez que vous obtenez une réponse `200 OK`

### Via curl :
```bash
curl -H "X-API-KEY: votre-clé-ici" \
     http://localhost:5050/api/v1/keys/validate
```

### Via PowerShell :
```powershell
$headers = @{ "X-API-KEY" = "votre-clé-ici" }
Invoke-RestMethod -Uri "http://localhost:5050/api/v1/keys/validate" -Headers $headers
```

---

## 📊 **Gestion des Clés API**

### Lister vos clients :
```bash
curl -H "X-API-KEY: votre-clé-ici" \
     http://localhost:5050/api/v1/clients
```

### Désactiver une clé :
```bash
curl -X PATCH \
     -H "X-API-KEY: votre-clé-ici" \
     -H "Content-Type: application/json" \
     http://localhost:5050/api/v1/clients/1/deactivate
```

### Modifier la limite de taux :
```bash
curl -X PATCH \
     -H "X-API-KEY: votre-clé-ici" \
     -H "Content-Type: application/json" \
     -d '{"rateLimitPerMinute": 1200}' \
     http://localhost:5050/api/v1/clients/1/rate-limit
```

---

## ⚠️ **Sécurité et Bonnes Pratiques**

### ✅ **À FAIRE** :
- **Sauvegardez** votre clé API dans un endroit sûr
- **Utilisez HTTPS** en production
- **Limitez** la limite de taux selon vos besoins
- **Désactivez** les clés non utilisées
- **Testez** votre clé après génération

### ❌ **À ÉVITER** :
- **Ne partagez jamais** votre clé API
- **Ne commitez pas** la clé dans le code
- **N'utilisez pas** la même clé pour tous les environnements
- **Ne laissez pas** des clés actives inutilisées

---

## 🔧 **Dépannage**

### Problème : "401 Unauthorized"
- Vérifiez que votre clé API est correcte
- Vérifiez que l'en-tête `X-API-KEY` est bien envoyé
- Vérifiez que la clé est active en base de données

### Problème : "403 Forbidden"
- Vérifiez que votre clé n'est pas désactivée
- Vérifiez que vous n'avez pas dépassé la limite de taux
- Vérifiez que la clé existe en base de données

### Problème : "500 Internal Server Error"
- Vérifiez que l'API est bien démarrée
- Vérifiez les logs de l'API
- Vérifiez la connexion à la base de données

---

## 📝 **Exemple Complet**

### Créer une alerte avec votre clé :
```bash
curl -X POST \
     -H "X-API-KEY: votre-clé-ici" \
     -H "Content-Type: application/json" \
     -d '{
       "title": "Test Alert",
       "message": "Message de test",
       "alertType": "acquittementNécessaire",
       "expedType": "Service",
       "appId": 1,
       "recipients": [
         {"recipientId": "test@example.com"}
       ]
     }' \
     http://localhost:5050/api/v1/alerts
```

---

**🎉 Votre clé API est maintenant prête à être utilisée !**
