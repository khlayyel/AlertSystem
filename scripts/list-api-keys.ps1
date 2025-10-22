param(
    [string]$Server = "(localdb)\\MSSQLLocalDB",
    [string]$Database = "AlertSystemDB"
)

$sql = @"
SELECT 
    ApiClientId,
    Name,
    IsActive,
    CreatedAt,
    RateLimitPerMinute,
    CASE 
        WHEN IsActive = 1 THEN 'Active'
        ELSE 'Inactive'
    END as Status
FROM ApiClients
ORDER BY CreatedAt DESC;
"@

Write-Host "Listing all API clients:" -ForegroundColor Yellow
sqlcmd -S $Server -d $Database -Q $sql | Out-Host
