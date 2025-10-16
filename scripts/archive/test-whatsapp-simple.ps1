# Simple WhatsApp Test
param(
    [string]$PhoneNumber = "21699414008",
    [string]$Message = "Test WhatsApp - " + (Get-Date)
)

Write-Host "=== WhatsApp Comprehensive Test ===" -ForegroundColor Cyan
Write-Host "Testing phone: $PhoneNumber" -ForegroundColor Yellow
Write-Host ""

# Test 1: Configuration
Write-Host "1. Checking configuration..." -ForegroundColor Green
try {
    $config = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/config' -Method GET
    Write-Host "   Access Token: $($config.hasAccessToken)" -ForegroundColor White
    Write-Host "   Phone ID: $($config.phoneNumberId)" -ForegroundColor White
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Direct API Test
Write-Host "2. Testing direct API..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = $Message
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/test-direct-api' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.freeFormResult.success) {
        Write-Host "   ✅ Free-form SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Free-form FAILED" -ForegroundColor Red
        if ($result.freeFormResult.errorDetails) {
            Write-Host "   Error: $($result.freeFormResult.errorDetails.explanation)" -ForegroundColor Yellow
        }
    }
    
    if ($result.templateResult -and $result.templateResult.success) {
        Write-Host "   ✅ Template SUCCESS!" -ForegroundColor Green
    } elseif ($result.templateResult) {
        Write-Host "   ❌ Template FAILED" -ForegroundColor Red
    }
    
    Write-Host "   💡 $($result.recommendation.message)" -ForegroundColor Cyan
    
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Original endpoint
Write-Host "3. Testing original endpoint..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = $Message
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapptest/send' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.success) {
        Write-Host "   ✅ Original endpoint SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Original endpoint FAILED" -ForegroundColor Red
    }
    
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== SOLUTION ===" -ForegroundColor Yellow
Write-Host "If messages fail to deliver:" -ForegroundColor White
Write-Host "1. Send a message FROM +$PhoneNumber TO your WhatsApp Business number" -ForegroundColor Cyan
Write-Host "2. Then test again within 24 hours" -ForegroundColor Cyan
Write-Host "3. This opens the messaging window for free-form messages" -ForegroundColor Cyan
