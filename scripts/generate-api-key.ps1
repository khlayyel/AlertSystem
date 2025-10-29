param(
    [string]$Name = "Test Hotel",
    [string]$Server = "(localdb)\\MSSQLLocalDB",
    [string]$Database = "BELVEDERE_17_10_2025"
)

function New-RandomApiKey {
    # 64 hex chars (32 bytes) => strong enough for demo
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

$key = New-RandomApiKey
$hash = Get-Sha256Hex $key

$sql = @"
INSERT INTO ApiClients(Name, ApiKeyHash, IsActive, RateLimitPerMinute)
VALUES (N'$Name', N'$hash', 1, 600);
SELECT TOP 1 ApiClientId, Name, CreatedAt FROM ApiClients WHERE ApiKeyHash = N'$hash';
"@

sqlcmd -S $Server -d $Database -Q $sql | Out-Host

Write-Host "" -ForegroundColor Cyan
Write-Host "PLAINTEXT API KEY (copy and keep safe):" -ForegroundColor Yellow
Write-Host $key -ForegroundColor Green



