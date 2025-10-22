param(
    [Parameter(Mandatory=$true)]
    [string]$ApiClientId,
    [string]$Server = "(localdb)\\MSSQLLocalDB",
    [string]$Database = "AlertSystemDB"
)

$sql = @"
UPDATE ApiClients 
SET IsActive = 0
WHERE ApiClientId = $ApiClientId;

SELECT ApiClientId, Name, IsActive, CreatedAt FROM ApiClients WHERE ApiClientId = $ApiClientId;
"@

Write-Host "Deactivating API key for client ID: $ApiClientId" -ForegroundColor Yellow
sqlcmd -S $Server -d $Database -Q $sql | Out-Host

Write-Host "" -ForegroundColor Cyan
Write-Host "API key deactivated successfully!" -ForegroundColor Green
