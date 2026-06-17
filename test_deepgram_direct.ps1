# Direct Deepgram API test — using curl.exe (no type constraints needed)
$ErrorActionPreference = "Continue"
$cfgPath = "RepeatSegment.App\bin\Debug\net8.0-windows\config.ini"
$lines = Get-Content $cfgPath -Encoding UTF8
$apiKey = ""
foreach ($line in $lines) {
    if ($line -match "^deepgram_api_key\s*=\s*(.+)$") {
        $apiKey = $Matches[1].Trim()
        break
    }
}
if ([string]::IsNullOrEmpty($apiKey)) { Write-Host "ERROR: API key not found" -ForegroundColor Red; exit 1 }
Write-Host "Key: $($apiKey.Substring(0,8))... (len=$($apiKey.Length))" -ForegroundColor Cyan

# Generate a tiny valid WAV (1 sec silence, 16kHz mono 16-bit) — all bytes, no PowerShell types
$sampleRate = 16000; $duration = 1; $numSamples = $sampleRate * $duration
$dataSize = $numSamples * 2
$wavSize = 36 + $dataSize
$bytes = [System.Collections.Generic.List[byte]]::new()
# RIFF
$bytes.AddRange([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
$bytes.AddRange([System.BitConverter]::GetBytes([int]$wavSize))
$bytes.AddRange([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
# fmt
$bytes.AddRange([System.Text.Encoding]::ASCII.GetBytes("fmt "))
$bytes.AddRange([System.BitConverter]::GetBytes([int]16))   # chunk size
$bytes.AddRange([System.BitConverter]::GetBytes([System.Int16]1))  # PCM
$bytes.AddRange([System.BitConverter]::GetBytes([System.Int16]1))  # mono
$bytes.AddRange([System.BitConverter]::GetBytes([int]$sampleRate))
$bytes.AddRange([System.BitConverter]::GetBytes([int]($sampleRate * 2)))
$bytes.AddRange([System.BitConverter]::GetBytes([System.Int16]2))  # block align
$bytes.AddRange([System.BitConverter]::GetBytes([System.Int16]16)) # bits
# data
$bytes.AddRange([System.Text.Encoding]::ASCII.GetBytes("data"))
$bytes.AddRange([System.BitConverter]::GetBytes([int]$dataSize))
# silence samples (zero)
$silence = [byte[]]::new($dataSize)
$bytes.AddRange($silence)
$wavPath = "test_dg.wav"
[System.IO.File]::WriteAllBytes($wavPath, $bytes.ToArray())
Write-Host "WAV created: $wavPath ($([System.IO.FileInfo]::new($wavPath).Length) bytes)"

# Call via curl
Write-Host "Calling Deepgram..." -ForegroundColor Yellow
$curlArgs = @("-s", "-w", "%{http_code}", "-X", "POST",
    "https://api.deepgram.com/v1/listen?model=nova-2&smart_format=true&language=en",
    "-H", "Authorization: Token $apiKey",
    "-H", "Content-Type: audio/wav",
    "--data-binary", "@$wavPath",
    "--connect-timeout", "10", "--max-time", "25")
$output = & curl.exe $curlArgs 2>&1
Write-Host "Raw output (first 500 chars): $($output.Substring(0, [Math]::Min(500, $output.Length)))" -ForegroundColor Gray

# Parse: last 3 chars are HTTP status code
$len = $output.Length
$httpCode = ""
if ($len -ge 3) {
    $httpCode = $output.Substring($len - 3)
    if ($httpCode -match "^\d{3}$") {
        $body = $output.Substring(0, $len - 3)
        Write-Host "HTTP Status: $httpCode" -ForegroundColor $(if ($httpCode -eq "200") { "Green" } else { "Red" })
        Write-Host "Body: $body" -ForegroundColor Gray
        $body | Out-File -FilePath "test_dg_result.txt" -Encoding UTF8
        if ($httpCode -eq "200") { Write-Host "SUCCESS" -ForegroundColor Green }
    } else {
        Write-Host "Could not parse HTTP code from: $output" -ForegroundColor Red
    }
}
Remove-Item $wavPath -ErrorAction SilentlyContinue
