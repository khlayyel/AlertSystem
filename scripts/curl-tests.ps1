$ErrorActionPreference = 'Stop'

$base = 'http://localhost:5186'
$apiKey = 'REPLACE_WITH_TEST_KEY'

Write-Host 'Testing API key validation...'
curl.exe -s -H "X-Api-Key: $apiKey" "$base/api/v1/keys/validate" | Write-Output

Write-Host 'Creating manual send via WEB endpoint (if exposed)...'
curl.exe -s -H "Content-Type: application/json" -X POST "http://localhost:5185/AlertsCrud/Send" --data '{
  "title":"Test API",
  "message":"Hello from curl",
  "emails":["khalil.ouerghemmi@gmail.com"],
  "phones":["21699414008"],
  "platforms": {"email": true, "whatsApp": true, "desktop": false}
}' | Write-Output



