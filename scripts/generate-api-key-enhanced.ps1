param(
    [string]$Name = "Test Hotel",
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "AlertSystemDB"
)

function New-RandomApiKey {
    # Génère une clé API de 64 caractères hexadécimaux (32 bytes)
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

# Génération de la clé API
$key = New-RandomApiKey
$hash = Get-Sha256Hex $key

Write-Host "🔑 Génération de clé API pour: $Name" -ForegroundColor Green
Write-Host ""

# Test de connexion à la base de données
Write-Host "🔍 Test de connexion à la base de données..." -ForegroundColor Yellow
if (Test-DatabaseConnection -Server $Server -Database $Database) {
    Write-Host "✅ Connexion à la base réussie" -ForegroundColor Green
    
    # Insertion en base de données
    $sql = @"
INSERT INTO ApiClients(Name, ApiKeyHash, IsActive, RateLimitPerMinute, CreatedAt)
VALUES (N'$Name', N'$hash', 1, 600, GETUTCDATE());

SELECT TOP 1 ApiClientId, Name, CreatedAt, IsActive, RateLimitPerMinute 
FROM ApiClients 
WHERE ApiKeyHash = N'$hash';
"@

    Write-Host "📝 Insertion en base de données..." -ForegroundColor Yellow
    $result = sqlcmd -S $Server -d $Database -Q $sql -h -1 -W
    
    if ($result) {
        Write-Host "✅ Client API créé avec succès !" -ForegroundColor Green
        Write-Host ""
        Write-Host "📊 Informations du client :" -ForegroundColor Cyan
        Write-Host $result -ForegroundColor White
    } else {
        Write-Host "❌ Erreur lors de l'insertion" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Impossible de se connecter à la base de données" -ForegroundColor Red
    Write-Host "💡 Essayez de démarrer SQL Server LocalDB ou vérifiez la chaîne de connexion" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🔑 VOTRE CLÉ API (à conserver précieusement) :" -ForegroundColor Yellow
Write-Host $key -ForegroundColor Green
Write-Host ""
Write-Host "📋 Utilisation :" -ForegroundColor Cyan
Write-Host "Header: X-API-KEY: $key" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Test de la clé :" -ForegroundColor Cyan
Write-Host "curl -H ""X-API-KEY: $key"" http://localhost:5002/api/v1/keys/validate" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  IMPORTANT : Cette clé ne sera affichée qu'une seule fois !" -ForegroundColor Red
Write-Host "   Sauvegardez-la dans un endroit sûr." -ForegroundColor Red
