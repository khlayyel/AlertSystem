param(
    [string]$Name = "Test Hotel",
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BELVEDERE_17_10_2025"
)

function New-RandomApiKey {
    # GÃ©nÃ¨re une clÃ© API de 64 caractÃ¨res hexadÃ©cimaux (32 bytes)
    return ([Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N'))
}

function Get-Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $hash = $sha.ComputeHash($bytes)
    $sb = New-Object System.Text.StringBuilder
    foreach ($b in $hash) {
        $sb.Append($b.ToString('x2'))
    }
    return $sb.ToString()
}

function Test-DatabaseConnection {
    param([string]$Server, [string]$Database)
    
    try {
        $testQuery = "SELECT 1"
        $result = sqlcmd -S $Server -d $Database -Q $testQuery -h -1
        return $result -eq "1"
    }
    catch {
        return $false
    }
}

# GÃ©nÃ©ration de la clÃ© API
$key = New-RandomApiKey
$hash = Get-Sha256Hex $key

Write-Host "ðŸ”‘ GÃ©nÃ©ration de clÃ© API pour: $Name" -ForegroundColor Green
Write-Host ""

# Test de connexion Ã  la base de donnÃ©es
Write-Host "ðŸ” Test de connexion Ã  la base de donnÃ©es..." -ForegroundColor Yellow
if (Test-DatabaseConnection -Server $Server -Database $Database) {
    Write-Host "âœ… Connexion Ã  la base rÃ©ussie" -ForegroundColor Green
    
    # Insertion en base de donnÃ©es
    $sql = @"
INSERT INTO ApiClients(Name, ApiKeyHash, IsActive, RateLimitPerMinute, CreatedAt)
VALUES (N'$Name', N'$hash', 1, 600, GETUTCDATE());

SELECT TOP 1 ApiClientId, Name, CreatedAt, IsActive, RateLimitPerMinute 
FROM ApiClients 
WHERE ApiKeyHash = N'$hash';
"@

    Write-Host "ðŸ“ Insertion en base de donnÃ©es..." -ForegroundColor Yellow
    $result = sqlcmd -S $Server -d $Database -Q $sql -h -1 -W
    
    if ($result) {
        Write-Host "âœ… Client API crÃ©Ã© avec succÃ¨s !" -ForegroundColor Green
        Write-Host ""
        Write-Host "ðŸ“Š Informations du client :" -ForegroundColor Cyan
        Write-Host $result -ForegroundColor White
    } else {
        Write-Host "âŒ Erreur lors de l'insertion" -ForegroundColor Red
    }
} else {
    Write-Host "âŒ Impossible de se connecter Ã  la base de donnÃ©es" -ForegroundColor Red
    Write-Host "ðŸ’¡ Essayez de dÃ©marrer SQL Server LocalDB ou vÃ©rifiez la chaÃ®ne de connexion" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "ðŸ”‘ VOTRE CLÃ‰ API (Ã  conserver prÃ©cieusement) :" -ForegroundColor Yellow
Write-Host $key -ForegroundColor Green
Write-Host ""
Write-Host "ðŸ“‹ Utilisation :" -ForegroundColor Cyan
Write-Host "Header: X-API-KEY: $key" -ForegroundColor White
Write-Host ""
Write-Host "ðŸ§ª Test de la clÃ© :" -ForegroundColor Cyan
Write-Host "curl -H ""X-API-KEY: $key"" http://localhost:5002/api/v1/keys/validate" -ForegroundColor White
Write-Host ""
Write-Host "âš ï¸  IMPORTANT : Cette clÃ© ne sera affichÃ©e qu'une seule fois !" -ForegroundColor Red
Write-Host "   Sauvegardez-la dans un endroit sÃ»r." -ForegroundColor Red

