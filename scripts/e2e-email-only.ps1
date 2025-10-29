param([string]$BaseUrl)
$ErrorActionPreference='Stop'
if(-not $BaseUrl){ $BaseUrl='http://localhost:5185' }
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
# login
$loginPage = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $session
$token=''; if($loginPage.Content -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"'){ $token=$Matches[1] }
$body=@{ Email='zied.soltani11@gmail.com'; Password='123456'; RememberMe='true' }
if($token){ $body['__RequestVerificationToken']=$token }
Invoke-WebRequest "$BaseUrl/Account/Login" -Method POST -WebSession $session -ContentType 'application/x-www-form-urlencoded' -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
# email-only payload
$payload = @{ title='Email-only Test'; message='SMTP focused test'; emails=@('khalilouerghemmi@gmail.com'); phones=@(); userIds=@(); platforms=@{ Email=$true; WhatsApp=$false; Desktop=$false }; alertTypeId=1 } | ConvertTo-Json -Depth 5
try {
  $resp = Invoke-WebRequest "$BaseUrl/AlertsCrud/Send" -Method POST -WebSession $session -ContentType 'application/json' -Body $payload -ErrorAction Stop
  "Send HTTP: $($resp.StatusCode)"; $resp.Content
} catch {
  $err = $_.Exception; $resp = $err.Response
  if($resp -and $resp.GetResponseStream){ $reader = New-Object System.IO.StreamReader($resp.GetResponseStream()); $text = $reader.ReadToEnd(); "Send failed: $($err.Message)"; "Body: $text" } else { "Send failed: $($err.Message)" }
}

