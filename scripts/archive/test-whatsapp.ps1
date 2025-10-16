# Test WhatsApp Direct Send
$body = @{
    phoneNumber = "21699414008"
    message = "Test direct WhatsApp to 99414008 - " + (Get-Date)
} | ConvertTo-Json

Write-Host "Sending WhatsApp test to 21699414008..."
Write-Host "Body: $body"

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:5143/api/v1/whatsapptest/send' -Method POST -ContentType 'application/json' -Body $body
    Write-Host "✅ Success: $response"
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}
