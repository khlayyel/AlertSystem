# Test email sending directly
param(
    [string]$ToEmail = "test@example.com",
    [string]$Subject = "Test Email",
    [string]$Message = "This is a test email"
)

# Load environment variables
function Load-DotEnv {
    $envFile = ".env"
    if (Test-Path $envFile) {
        Get-Content $envFile | ForEach-Object {
            if ($_ -and $_ -match "^([^#][^=]+)=(.*)$") {
                $name = $matches[1].Trim()
                $value = $matches[2].Trim()
                if ($name -and $value) {
                    [Environment]::SetEnvironmentVariable($name, $value, "Process")
                }
            }
        }
    }
}

Load-DotEnv

# Get SMTP settings
$smtpHost = $env:SMTP__HOST
$smtpPort = [int]$env:SMTP__PORT
$smtpUser = $env:SMTP__USER
$smtpPass = $env:SMTP__PASS
$smtpFrom = $env:SMTP__FROM
$smtpFromName = $env:SMTP__FROMNAME
$useStartTls = $env:SMTP__USESTARTTLS -eq "true"

Write-Host "Testing email to: $ToEmail"
Write-Host "SMTP Host: $smtpHost"
Write-Host "SMTP Port: $smtpPort"
Write-Host "SMTP User: $smtpUser"
Write-Host "SMTP From: $smtpFrom"
Write-Host "Use StartTLS: $useStartTls"

try {
    # Send email using .NET SMTP
    $smtpClient = New-Object System.Net.Mail.SmtpClient($smtpHost, $smtpPort)
    
    if ($smtpPort -eq 465) {
        $smtpClient.EnableSsl = $true
    } elseif ($useStartTls) {
        $smtpClient.EnableSsl = $true
    }
    
    $smtpClient.Credentials = New-Object System.Net.NetworkCredential($smtpUser, $smtpPass)
    
    $mailMessage = New-Object System.Net.Mail.MailMessage($smtpFrom, $ToEmail, $Subject, $Message)
    
    $smtpClient.Send($mailMessage)
    
    Write-Host "✅ Email sent successfully to $ToEmail" -ForegroundColor Green
}
catch {
    Write-Host "❌ Email failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Full error: $($_.Exception)" -ForegroundColor Red
}
finally {
    if ($smtpClient) { $smtpClient.Dispose() }
    if ($mailMessage) { $mailMessage.Dispose() }
}
