param([string]$BaseUrl,[string]$Email,[string]$Password,[string]$RecipientEmail,[string]$RecipientPhone,[int]$RecipientUserId)
$ErrorActionPreference='Stop'
if(-not $BaseUrl){ $BaseUrl='http://localhost:5185' }
if(-not $Email){ $Email='zied.soltani11@gmail.com' }
if(-not $Password){ $Password='123456' }
if(-not $RecipientEmail){ $RecipientEmail='khalilouerghemmi@gmail.com' }
if(-not $RecipientPhone){ $RecipientPhone='+21699414008' }
if(-not $RecipientUserId){ $RecipientUserId=55 }

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $session
$token=''; if($loginPage.Content -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"'){ $token=$Matches[1] }

$body=@{ Email=$Email; Password=$Password; RememberMe='true' }
if($token){ $body['__RequestVerificationToken']=$token }
$loginResp = Invoke-WebRequest "$BaseUrl/Account/Login" -Method POST -WebSession $session -ContentType 'application/x-www-form-urlencoded' -Body $body -MaximumRedirection 0 -ErrorAction SilentlyContinue
"Login HTTP: $($loginResp.StatusCode)"

$payload = @{
  title='Automated Test Alert'
  message='E2E: email+whatsapp+desktop to user 55'
  emails=@($RecipientEmail)
  phones=@($RecipientPhone)
  userIds=@(55)
  platforms=@{ Email=$true; WhatsApp=$true; Desktop=$true }
  alertTypeId=1
} | ConvertTo-Json -Depth 5

$sendResp = Invoke-WebRequest "$BaseUrl/AlertsCrud/Send" -Method POST -WebSession $session -ContentType 'application/json' -Body $payload
"Send HTTP: $($sendResp.StatusCode)"
$sendResp.Content

