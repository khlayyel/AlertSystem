# Script PowerShell pour tester l'API d'envoi automatique avec clÃ© API

Write-Host "=== TEST API ENVOI AUTOMATIQUE AVEC CLÃ‰ ===" -ForegroundColor Green
Write-Host ""

# Configuration
$baseUrl = "http://localhost:5000"
$testUrl = "$baseUrl/api/v1/alerts/send-by-id/194"
$apiKey = "test-auto-send-key-123"  # ClÃ© de test valide

Write-Host "Test de connexion Ã  l'application..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri $baseUrl -Method GET -TimeoutSec 5
    Write-Host "âœ… Application accessible" -ForegroundColor Green
} catch {
    Write-Host "âŒ Application non accessible sur $baseUrl" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Appel de l'API d'envoi automatique avec clÃ© API..." -ForegroundColor Yellow
Write-Host "URL: $testUrl" -ForegroundColor Cyan
Write-Host "API Key: $apiKey" -ForegroundColor Cyan

# CrÃ©er les headers avec la clÃ© API
$headers = @{
    "X-Api-Key" = $apiKey
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri $testUrl -Method POST -Headers $headers -TimeoutSec 30
    
    Write-Host "âœ… Appel API rÃ©ussi !" -ForegroundColor Green
    Write-Host ""
    Write-Host "RÃ©ponse:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    Write-Host ""
    Write-Host "RÃ©sumÃ©:" -ForegroundColor Yellow
    Write-Host "- Alerte ID: $($response.alerteId)"
    Write-Host "- Titre: $($response.titre)"
    Write-Host "- Destinataires: $($response.totalDestinataires)"
    Write-Host "- EnvoyÃ©s: $($response.totalEnvoyes)" -ForegroundColor Green
    Write-Host "- Erreurs: $($response.totalErreurs)" -ForegroundColor $(if($response.totalErreurs -gt 0) { "Red" } else { "Green" })
    
    Write-Host ""
    Write-Host "=== DÃ‰TAILS PAR DESTINATAIRE ===" -ForegroundColor Yellow
    foreach ($detail in $response.details) {
        Write-Host "ðŸ‘¤ $($detail.user.fullName) ($($detail.user.email))" -ForegroundColor Cyan
        foreach ($result in $detail.results) {
            $color = if ($result -like "*âœ…*") { "Green" } elseif ($result -like "*âŒ*") { "Red" } else { "Yellow" }
            Write-Host "   $result" -ForegroundColor $color
        }
    }
    
} catch {
    Write-Host "âŒ Erreur lors de l'appel API" -ForegroundColor Red
    Write-Host "Erreur: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Code de statut: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq "Unauthorized") {
            Write-Host ""
            Write-Host "ðŸ’¡ Suggestions pour rÃ©soudre l'erreur 401:" -ForegroundColor Yellow
            Write-Host "1. VÃ©rifiez que la clÃ© API '$apiKey' est valide"
            Write-Host "2. CrÃ©ez une nouvelle clÃ© API de test"
            Write-Host "3. Ou dÃ©sactivez temporairement l'authentification pour les tests"
        }
    }
}

Write-Host ""
Write-Host "=== ALTERNATIVE: TEST SANS AUTHENTIFICATION ===" -ForegroundColor Yellow
Write-Host "Si l'authentification pose problÃ¨me, vous pouvez:"
Write-Host "1. Tester directement via l'interface web: http://localhost:5000/Home/HistoriqueTest"
Write-Host "2. InsÃ©rer une nouvelle alerte via SQL pour dÃ©clencher le trigger"
Write-Host "3. CrÃ©er une clÃ© API valide dans la base de donnÃ©es"

