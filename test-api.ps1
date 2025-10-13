# AlertSystem API Test Script
# This script demonstrates how to use the AlertSystem API

$baseUrl = "http://localhost:5143"

# Bypass SSL certificate validation for localhost testing
if (-not ([System.Management.Automation.PSTypeName]'ServerCertificateValidationCallback').Type) {
    $certCallback = @"
        using System;
        using System.Net;
        using System.Net.Security;
        using System.Security.Cryptography.X509Certificates;
        public class ServerCertificateValidationCallback
        {
            public static void Ignore()
            {
                if(ServicePointManager.ServerCertificateValidationCallback ==null)
                {
                    ServicePointManager.ServerCertificateValidationCallback += 
                        delegate
                        (
                            Object obj, 
                            X509Certificate certificate, 
                            X509Chain chain, 
                            SslPolicyErrors errors
                        )
                        {
                            return true;
                        };
                }
            }
        }
"@
    Add-Type $certCallback
}
[ServerCertificateValidationCallback]::Ignore()

Write-Host "=== AlertSystem API Test Script ===" -ForegroundColor Green
Write-Host ""

# Test 0: Check if server is running
Write-Host "0. Checking server connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl" -Method GET -UseBasicParsing
    Write-Host "✅ Server is running on $baseUrl" -ForegroundColor Green
} catch {
    Write-Host "❌ Server is not accessible: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the application is running with 'dotnet run'" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test 1: Create an API client
Write-Host "1. Creating API client..." -ForegroundColor Yellow
$clientData = @{
    name = "Test Client"
    rateLimitPerMinute = 100
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/clients" -Method POST -Body $clientData -ContentType "application/json" -ErrorAction Stop
    Write-Host "✅ API Client created successfully!" -ForegroundColor Green
    Write-Host "Client ID: $($response.clientId)" -ForegroundColor Cyan
    Write-Host "API Key: $($response.apiKey)" -ForegroundColor Cyan
    $apiKey = $response.apiKey
    $clientId = $response.clientId
} catch {
    Write-Host "❌ Failed to create API client" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try to get more detailed error information
    if ($_.Exception.Response) {
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "Response Body: $errorBody" -ForegroundColor Red
        } catch {
            Write-Host "Could not read error response body" -ForegroundColor Red
        }
    }
    
    Write-Host "This might be a database seeding issue. Try running the seed-database.sql script first." -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test 2: Validate the API key
Write-Host "2. Testing API key validation..." -ForegroundColor Yellow
$headers = @{
    "X-Api-Key" = $apiKey
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/keys/validate" -Method GET -Headers $headers
    Write-Host "✅ API Key validation successful!" -ForegroundColor Green
    Write-Host "Valid: $($response.valid)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ API key validation failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Create an alert
Write-Host "3. Creating an alert..." -ForegroundColor Yellow
$alertData = @{
    title = "Test Alert from PowerShell"
    message = "This is a test alert created via the API to demonstrate the multi-channel notification system."
    alertType = "acquittementNonNécessaire"
    expedType = "Service"
    appId = 1
    recipients = @(
        @{ externalRecipientId = "test@example.com" }
        @{ externalRecipientId = "+21612345678" }
        @{ externalRecipientId = "device-test-123" }
    )
} | ConvertTo-Json -Depth 3

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/alerts" -Method POST -Body $alertData -Headers $headers
    Write-Host "✅ Alert created successfully!" -ForegroundColor Green
    Write-Host "Alert ID: $($response.alertId)" -ForegroundColor Cyan
    Write-Host "Recipients created: $($response.recipientsCreated)" -ForegroundColor Cyan
    Write-Host "Notifications sent: $($response.notificationsSent)" -ForegroundColor Cyan
    $alertId = $response.alertId
} catch {
    Write-Host "❌ Failed to create alert: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Query alerts
Write-Host "4. Querying alerts..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/alerts?page=1&size=5" -Method GET -Headers $headers
    Write-Host "✅ Alert query successful!" -ForegroundColor Green
    Write-Host "Total alerts: $($response.total)" -ForegroundColor Cyan
    Write-Host "Retrieved: $($response.items.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Failed to query alerts: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get specific alert
if ($alertId) {
    Write-Host "5. Getting alert details..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/alerts/$alertId" -Method GET -Headers $headers
        Write-Host "✅ Alert details retrieved!" -ForegroundColor Green
        Write-Host "Title: $($response.title)" -ForegroundColor Cyan
        Write-Host "Recipients: $($response.recipients.Count)" -ForegroundColor Cyan
    } catch {
        Write-Host "❌ Failed to get alert details: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Test completed ===" -ForegroundColor Green
Write-Host "API Key for dashboard: $apiKey" -ForegroundColor Yellow
Write-Host "You can now use this API key in the dashboard to send alerts!" -ForegroundColor Yellow
