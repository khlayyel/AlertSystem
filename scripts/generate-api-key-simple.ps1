param(
    [string]$Name = "Test Hotel",
    [string]$Server = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BELVEDERE_17_10_2025"
)

function New-RandomApiKey {
    # Generate 64 hex chars (32 bytes) - strong enough for production
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

# Generate API key
$key = New-RandomApiKey
$hash = Get-Sha256Hex $key

Write-Host "API Key Generation for: $Name" -ForegroundColor Green
Write-Host ""

# Try to insert into database
$sql = @"
INSERT INTO ApiClients(Name, ApiKeyHash, IsActive, RateLimitPerMinute, CreatedAt)
VALUES (N'$Name', N'$hash', 1, 600, GETUTCDATE());

SELECT TOP 1 ApiClientId, Name, CreatedAt, IsActive, RateLimitPerMinute 
FROM ApiClients 
WHERE ApiKeyHash = N'$hash';
"@

Write-Host "Inserting into database..." -ForegroundColor Yellow
try {
    $result = sqlcmd -S $Server -d $Database -Q $sql -h -1 -W
    if ($result) {
        Write-Host "SUCCESS: API Client created!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Client Information:" -ForegroundColor Cyan
        Write-Host $result -ForegroundColor White
    }
} catch {
    Write-Host "WARNING: Could not connect to database" -ForegroundColor Yellow
    Write-Host "The API key was generated but not stored in database" -ForegroundColor Yellow
    Write-Host "You can still use it for testing via Swagger UI" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "YOUR API KEY (save this securely):" -ForegroundColor Yellow
Write-Host $key -ForegroundColor Green
Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "Header: X-API-KEY: $key" -ForegroundColor White
Write-Host ""
Write-Host "Test the key:" -ForegroundColor Cyan
Write-Host "curl -H ""X-API-KEY: $key"" http://localhost:5002/api/v1/keys/validate" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANT: This key is shown only once!" -ForegroundColor Red
Write-Host "Save it in a secure location." -ForegroundColor Red

