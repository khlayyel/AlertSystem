param(
  [string[]]$Phones,
  [string]$Message = "Test AlertSystem WA message",
  [ValidateSet('text','template','smart')][string]$Mode = 'smart',
  [string]$TemplateName = '',
  [string]$TemplateLang = 'en_US'
)
${ErrorActionPreference} = 'Stop'
if (-not $Phones) { $Phones = @() }

function Load-DotEnv($path) {
  if (-not (Test-Path $path)) { Write-Host "[WARN] .env not found at $path" -ForegroundColor Yellow; return }
  Get-Content -Raw -Path $path -Encoding UTF8 | ForEach-Object {
    $_ -split "`n" | ForEach-Object {
      $line = $_.Trim()
      if ([string]::IsNullOrWhiteSpace($line)) { return }
      if ($line.StartsWith('#')) { return }
      $idx = $line.IndexOf('=')
      if ($idx -lt 1) { return }
      $key = $line.Substring(0, $idx).Trim()
      $val = $line.Substring($idx+1).Trim()
      # remove surrounding quotes
      if ($val.StartsWith('"') -and $val.EndsWith('"')) { $val = $val.Substring(1, $val.Length-2) }
      if ($val.StartsWith("'") -and $val.EndsWith("'")) { $val = $val.Substring(1, $val.Length-2) }
      if (-not [string]::IsNullOrWhiteSpace($key)) {
        Set-Item -Path "Env:$key" -Value $val | Out-Null
      }
    }
  }
  Write-Host "[OK] .env loaded from $path" -ForegroundColor Green
}

function Normalize-Phone([string]$p) {
  if (-not $p) { return $null }
  $s = ($p -replace "[^0-9+]", '')
  if ($s.StartsWith('+')) { return $s }
  if ($s.StartsWith('216') -and $s.Length -eq 11) { return "+$s" }
  if ($s.Length -eq 8) { return "+216$s" }
  if ($s.StartsWith('0') -and $s.Length -ge 9) { return "+216$($s.Substring(1))" }
  if (-not $s.StartsWith('+')) { return "+$s" }
  return $s
}

function Send-WhatsappText([string]$to, [string]$message) {
  $num = Normalize-Phone $to
  if (-not $num) { Write-Host "[SKIP] Invalid phone '$to'" -ForegroundColor Yellow; return }

  $token = $env:WHATSAPP__ACCESSTOKEN
  $phoneId = $env:WHATSAPP__PHONENUMBERID
  $apiVer = if ($env:WHATSAPP__APIVERSION) { $env:WHATSAPP__APIVERSION } else { 'v22.0' }
  $baseUrl = if ($env:WHATSAPP__METAGRAPHBASEURL) { $env:WHATSAPP__METAGRAPHBASEURL.TrimEnd('/') } else { 'https://graph.facebook.com' }

  if (-not $token -or -not $phoneId) {
    throw "Missing WhatsApp credentials in environment (WHATSAPP__ACCESSTOKEN / WHATSAPP__PHONENUMBERID)."
  }

  $uri = "$baseUrl/$apiVer/$phoneId/messages"
  $headers = @{ Authorization = "Bearer $token" }
  $body = @{ messaging_product = 'whatsapp'; to = $num; type = 'text'; text = @{ preview_url = $false; body = $message } } | ConvertTo-Json -Depth 5

  Write-Host "POST $uri -> $num" -ForegroundColor Cyan
  try {
    $resp = Invoke-WebRequest -Method POST -Uri $uri -Headers $headers -ContentType 'application/json' -Body $body -UseBasicParsing -TimeoutSec 60
    Write-Host ("Status: {0}" -f $resp.StatusCode) -ForegroundColor Green
    if ($resp.Content) { Write-Host $resp.Content }
  }
  catch {
    if ($_.Exception.Response) {
      $resp = $_.Exception.Response
      $status = [int]$resp.StatusCode
      $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
      $content = $reader.ReadToEnd()
      Write-Host ("Status: {0}" -f $status) -ForegroundColor Red
      Write-Host $content
    } else {
      Write-Error $_
    }
  }
}

function Send-WhatsappTemplate([string]$to, [string]$templateName, [string]$lang) {
  $num = Normalize-Phone $to
  if (-not $num) { Write-Host "[SKIP] Invalid phone '$to'" -ForegroundColor Yellow; return }

  $token = $env:WHATSAPP__ACCESSTOKEN
  $phoneId = $env:WHATSAPP__PHONENUMBERID
  $apiVer = if ($env:WHATSAPP__APIVERSION) { $env:WHATSAPP__APIVERSION } else { 'v22.0' }
  $baseUrl = if ($env:WHATSAPP__METAGRAPHBASEURL) { $env:WHATSAPP__METAGRAPHBASEURL.TrimEnd('/') } else { 'https://graph.facebook.com' }

  if (-not $token -or -not $phoneId) { throw "Missing WhatsApp credentials in environment (WHATSAPP__ACCESSTOKEN / WHATSAPP__PHONENUMBERID)." }

  $uri = "$baseUrl/$apiVer/$phoneId/messages"
  $headers = @{ Authorization = "Bearer $token" }
  $bodyObj = @{ messaging_product = 'whatsapp'; to = $num; type = 'template'; template = @{ name = $templateName; language = @{ code = $lang } } }
  $body = $bodyObj | ConvertTo-Json -Depth 6

  Write-Host "POST $uri (template=$templateName) -> $num" -ForegroundColor Cyan
  try {
    $resp = Invoke-WebRequest -Method POST -Uri $uri -Headers $headers -ContentType 'application/json' -Body $body -UseBasicParsing -TimeoutSec 60
    Write-Host ("Status: {0}" -f $resp.StatusCode) -ForegroundColor Green
    if ($resp.Content) { Write-Host $resp.Content }
  }
  catch {
    if ($_.Exception.Response) {
      $resp = $_.Exception.Response
      $status = [int]$resp.StatusCode
      $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
      $content = $reader.ReadToEnd()
      Write-Host ("Status: {0}" -f $status) -ForegroundColor Red
      Write-Host $content
    } else { Write-Error $_ }
  }
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$envPath = Join-Path $root '.env'
Load-DotEnv $envPath

# Quick diagnostics
Write-Host "WHATSAPP__PHONENUMBERID=$($env:WHATSAPP__PHONENUMBERID)" -ForegroundColor DarkGray
Write-Host "WHATSAPP__ACCESSTOKEN present? $([bool]$env:WHATSAPP__ACCESSTOKEN)" -ForegroundColor DarkGray

if (-not $Phones -or $Phones.Count -eq 0) {
  $Phones = @('99414008','21699414008','+21699414008','21494064')
}

if ($Mode -eq 'text') {
  Write-Host "Sending WhatsApp text to: $($Phones -join ', ')" -ForegroundColor White
  foreach ($p in $Phones) { Send-WhatsappText -to $p -message $Message }
}
elseif ($Mode -eq 'template') {
  Write-Host "Sending WhatsApp template '$TemplateName' ($TemplateLang) to: $($Phones -join ', ')" -ForegroundColor White
  foreach ($p in $Phones) { Send-WhatsappTemplate -to $p -templateName $TemplateName -lang $TemplateLang }
}
else {
  # smart: template-first with dynamic variables; fallback to hello_world, then free-form
  $defName = if ($env:WHATSAPP__DEFAULTTEMPLATENAME) { $env:WHATSAPP__DEFAULTTEMPLATENAME } else { $TemplateName }
  $defLang = if ($env:WHATSAPP__DEFAULTTEMPLATELANG) { $env:WHATSAPP__DEFAULTTEMPLATELANG } else { $TemplateLang }
  Write-Host "SMART mode: template-first (name='$defName' lang='$defLang'), fallback to hello_world then text" -ForegroundColor White
  foreach ($p in $Phones) {
    $ok = $false
    if ($defName) {
      $title,$body = $Message.Split("`n",2)
      if (-not $body) { $title = 'Alert'; $body = $Message }
      # try dynamic template name (env-provided) first
      Send-WhatsappTemplate -to $p -templateName $defName -lang $defLang
      $ok = $?
    }
    if (-not $ok) {
      Send-WhatsappTemplate -to $p -templateName 'hello_world' -lang $defLang
      $ok = $?
    }
    if (-not $ok) {
      Send-WhatsappText -to $p -message $Message
    }
  }
}


