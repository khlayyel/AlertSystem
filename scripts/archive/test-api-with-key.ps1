# Script PowerShell pour tester l'API d'envoi automatique avec clé API

Write-Host "=== TEST API ENVOI AUTOMATIQUE AVEC CLÉ ===" -ForegroundColor Green
Write-Host ""

# Configuration
$baseUrl = "http://localhost:5000"
$testUrl = "$baseUrl/api/v1/alerts/send-by-id/194"
$apiKey = "test-auto-send-key-123"  # Clé de test valide

Write-Host "Test de connexion à l'application..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri $baseUrl -Method GET -TimeoutSec 5
    Write-Host "✅ Application accessible" -ForegroundColor Green
} catch {
    Write-Host "❌ Application non accessible sur $baseUrl" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Appel de l'API d'envoi automatique avec clé API..." -ForegroundColor Yellow
Write-Host "URL: $testUrl" -ForegroundColor Cyan
Write-Host "API Key: $apiKey" -ForegroundColor Cyan

# Créer les headers avec la clé API
$headers = @{
    "X-Api-Key" = $apiKey
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri $testUrl -Method POST -Headers $headers -TimeoutSec 30
    
    Write-Host "✅ Appel API réussi !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Réponse:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    Write-Host ""
    Write-Host "Résumé:" -ForegroundColor Yellow
    Write-Host "- Alerte ID: $($response.alerteId)"
    Write-Host "- Titre: $($response.titre)"
    Write-Host "- Destinataires: $($response.totalDestinataires)"
    Write-Host "- Envoyés: $($response.totalEnvoyes)" -ForegroundColor Green
    Write-Host "- Erreurs: $($response.totalErreurs)" -ForegroundColor $(if($response.totalErreurs -gt 0) { "Red" } else { "Green" })
    
    Write-Host ""
    Write-Host "=== DÉTAILS PAR DESTINATAIRE ===" -ForegroundColor Yellow
    foreach ($detail in $response.details) {
        Write-Host "👤 $($detail.user.fullName) ($($detail.user.email))" -ForegroundColor Cyan
        foreach ($result in $detail.results) {
            $color = if ($result -like "*✅*") { "Green" } elseif ($result -like "*❌*") { "Red" } else { "Yellow" }
            Write-Host "   $result" -ForegroundColor $color
        }
    }
    
} catch {
    Write-Host "❌ Erreur lors de l'appel API" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Code de statut: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq "Unauthorized") {
            Write-Host ""
            Write-Host "💡 Suggestions pour résoudre l'erreur 401:" -ForegroundColor Yellow
            Write-Host "1. Vérifiez que la clé API '$apiKey' est valide"
            Write-Host "2. Créez une nouvelle clé API de test"
            Write-Host "3. Ou désactivez temporairement l'authentification pour les tests"
        }
    }
}

Write-Host ""
Write-Host "=== ALTERNATIVE: TEST SANS AUTHENTIFICATION ===" -ForegroundColor Yellow
Write-Host "Si l'authentification pose problème, vous pouvez:"
Write-Host "1. Tester directement via l'interface web: http://localhost:5000/Home/HistoriqueTest"
Write-Host "2. Insérer une nouvelle alerte via SQL pour déclencher le trigger"
Write-Host "3. Créer une clé API valide dans la base de données"
