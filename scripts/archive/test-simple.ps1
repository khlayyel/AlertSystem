# Simple test to debug the API issue
$baseUrl = "http://localhost:5143"

Write-Host "=== Simple API Debug Test ===" -ForegroundColor Green

# Test 1: Check seed status
Write-Host "1. Checking database status..." -ForegroundColor Yellow
try {
    $status = Invoke-RestMethod -Uri "$baseUrl/api/v1/seed/status" -Method GET
    Write-Host "✅ Database status retrieved:" -ForegroundColor Green
    $status | Format-List
} catch {
    Write-Host "❌ Failed to get database status: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Try to create API client with detailed error info
Write-Host "2. Testing API client creation..." -ForegroundColor Yellow
$clientData = @{
    name = "Debug Test Client"
    rateLimitPerMinute = 50
} | ConvertTo-Json

Write-Host "Request body: $clientData" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/clients" -Method POST -Body $clientData -ContentType "application/json"
    Write-Host "✅ API Client created successfully!" -ForegroundColor Green
    $response | Format-List
} catch {
    Write-Host "❌ Failed to create API client" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.GetType().Name)" -ForegroundColor Red
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Debug test completed ===" -ForegroundColor Green
