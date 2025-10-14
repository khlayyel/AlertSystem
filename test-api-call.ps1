# Script PowerShell pour tester l'API d'envoi automatique

Write-Host "=== TEST API ENVOI AUTOMATIQUE ===" -ForegroundColor Green
Write-Host ""

# Tester d'abord si l'application répond
$baseUrl = "http://localhost:5000"
$testUrl = "$baseUrl/api/v1/alerts/send-by-id/194"

Write-Host "Test de connexion à l'application..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri $baseUrl -Method GET -TimeoutSec 5
    Write-Host "✅ Application accessible" -ForegroundColor Green
} catch {
    Write-Host "❌ Application non accessible sur $baseUrl" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Solutions possibles:" -ForegroundColor Yellow
    Write-Host "1. Vérifiez que l'application AlertSystem est démarrée"
    Write-Host "2. Vérifiez le port dans les logs de l'application"
    Write-Host "3. Essayez http://localhost:5001 (HTTPS)"
    exit 1
}

Write-Host ""
Write-Host "Appel de l'API d'envoi automatique..." -ForegroundColor Yellow
Write-Host "URL: $testUrl" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri $testUrl -Method POST -ContentType "application/json" -TimeoutSec 30
    
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
    
} catch {
    Write-Host "❌ Erreur lors de l'appel API" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Code de statut: $statusCode" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== VÉRIFICATIONS RECOMMANDÉES ===" -ForegroundColor Yellow
Write-Host "1. Vérifiez vos emails (Gmail, etc.)"
Write-Host "2. Vérifiez vos messages WhatsApp"
Write-Host "3. Consultez l'interface: http://localhost:5000/Home/HistoriqueTest"
Write-Host "4. Vérifiez les logs de l'application AlertSystem"
