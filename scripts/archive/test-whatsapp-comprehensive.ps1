# Comprehensive WhatsApp Testing Script
param(
    [string]$PhoneNumber = "21699414008",
    [string]$Message = "Test comprehensive WhatsApp - " + (Get-Date)
)

Write-Host "=== Comprehensive WhatsApp Testing ===" -ForegroundColor Cyan
Write-Host "Phone Number: $PhoneNumber" -ForegroundColor Yellow
Write-Host "Message: $Message" -ForegroundColor Yellow
Write-Host ""

# Test 1: Configuration Check
Write-Host "1. Checking WhatsApp Configuration..." -ForegroundColor Green
try {
    $config = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/config' -Method GET
    Write-Host "✅ Configuration OK:" -ForegroundColor Green
    Write-Host "   - Access Token: $($config.hasAccessToken)" -ForegroundColor White
    Write-Host "   - Phone Number ID: $($config.phoneNumberId)" -ForegroundColor White
    Write-Host "   - API Version: $($config.apiVersion)" -ForegroundColor White
    Write-Host "   - Base URL: $($config.baseUrl)" -ForegroundColor White
} catch {
    Write-Host "❌ Configuration Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Direct API Test (Most Comprehensive)
Write-Host "2. Testing Direct API (Free-form + Template)..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = $Message
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/test-direct-api' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.freeFormResult.success) {
        Write-Host "✅ Free-form message SUCCESS!" -ForegroundColor Green
        Write-Host "   Response: $($result.freeFormResult.response)" -ForegroundColor White
    } else {
        Write-Host "❌ Free-form message FAILED" -ForegroundColor Red
        Write-Host "   Status: $($result.freeFormResult.statusCode)" -ForegroundColor Yellow
        Write-Host "   Response: $($result.freeFormResult.response)" -ForegroundColor Yellow
        
        if ($result.freeFormResult.errorDetails) {
            Write-Host "   Error Code: $($result.freeFormResult.errorDetails.code)" -ForegroundColor Red
            Write-Host "   Error Message: $($result.freeFormResult.errorDetails.message)" -ForegroundColor Red
            Write-Host "   Explanation: $($result.freeFormResult.errorDetails.explanation)" -ForegroundColor Cyan
        }
    }
    
    Write-Host ""
    
    if ($result.templateResult) {
        if ($result.templateResult.success) {
            Write-Host "✅ Template message SUCCESS!" -ForegroundColor Green
            Write-Host "   Response: $($result.templateResult.response)" -ForegroundColor White
        } else {
            Write-Host "❌ Template message FAILED" -ForegroundColor Red
            Write-Host "   Status: $($result.templateResult.statusCode)" -ForegroundColor Yellow
            Write-Host "   Response: $($result.templateResult.response)" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "💡 Recommendation: $($result.recommendation.message)" -ForegroundColor Cyan
    if ($result.recommendation.solution) {
        Write-Host "🔧 Solution: $($result.recommendation.solution)" -ForegroundColor Yellow
    }
    if ($result.recommendation.solutions) {
        Write-Host "🔧 Solutions:" -ForegroundColor Yellow
        foreach ($solution in $result.recommendation.solutions) {
            Write-Host "   - $solution" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host "❌ Direct API Test Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Service Layer Test
Write-Host "3. Testing Service Layer..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = $Message
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/test-free-form' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.success) {
        Write-Host "✅ Service layer SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "❌ Service layer FAILED" -ForegroundColor Red
    }
    Write-Host "   Message: $($result.message)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Service Layer Test Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Template Test
Write-Host "4. Testing Template Message..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = "Template test"
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapp-debug/test-template' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.success) {
        Write-Host "✅ Template message SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "❌ Template message FAILED" -ForegroundColor Red
    }
    Write-Host "   Message: $($result.message)" -ForegroundColor White
    Write-Host "   Template: $($result.template)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Template Test Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Original Test Endpoint
Write-Host "5. Testing Original Endpoint..." -ForegroundColor Green
try {
    $body = @{
        phoneNumber = $PhoneNumber
        message = $Message
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapptest/send' -Method POST -ContentType 'application/json' -Body $body
    
    if ($result.success) {
        Write-Host "✅ Original endpoint SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "❌ Original endpoint FAILED" -ForegroundColor Red
    }
    Write-Host "   Message: $($result.message)" -ForegroundColor White
    
} catch {
    Write-Host "❌ Original Endpoint Test Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Testing Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "📱 IMPORTANT NOTES:" -ForegroundColor Yellow
Write-Host "1. If free-form messages fail but templates work:" -ForegroundColor White
Write-Host "   → Send a message from +$PhoneNumber to your WhatsApp Business number" -ForegroundColor Cyan
Write-Host "   → Then test again within 24 hours" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. If both fail:" -ForegroundColor White
Write-Host "   → Check your WhatsApp Business API configuration" -ForegroundColor Cyan
Write-Host "   → Verify your access token and phone number ID" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Check application logs for detailed error information" -ForegroundColor White
