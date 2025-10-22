param(
    [Parameter(Mandatory=$true)]
    [string]$ApiClientId,
    [string]$Server = "(localdb)\\MSSQLLocalDB",
    [string]$Database = "AlertSystemDB"
)

function New-RandomApiKey {
    # 64 hex chars (32 bytes) => strong enough for demo
    return ([Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N'))
}

function Get-Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $hash = $sha.ComputeHash($bytes)
    ($hash | ForEach-Object { $_.ToString('x2') }) -join ''
}

# Generate new key
$newKey = New-RandomApiKey
$newHash = Get-Sha256Hex $newKey

$sql = @"
UPDATE ApiClients 
SET ApiKeyHash = N'$newHash', CreatedAt = GETUTCDATE()
WHERE ApiClientId = $ApiClientId;

SELECT ApiClientId, Name, CreatedAt FROM ApiClients WHERE ApiClientId = $ApiClientId;
"@

Write-Host "Rotating API key for client ID: $ApiClientId" -ForegroundColor Yellow
sqlcmd -S $Server -d $Database -Q $sql | Out-Host

Write-Host "" -ForegroundColor Cyan
Write-Host "NEW PLAINTEXT API KEY (copy and keep safe):" -ForegroundColor Yellow
Write-Host $newKey -ForegroundColor Green
Write-Host "" -ForegroundColor Cyan
Write-Host "IMPORTANT: Update your applications with the new key immediately!" -ForegroundColor Red
