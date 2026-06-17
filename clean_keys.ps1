# Replace API keys script for git filter-branch
param([string]$File = "RepeatSegment.App/config.template.ini")

if (-not (Test-Path $File)) { exit 0 }

$content = Get-Content $File -Raw
$changed = $false

if ($content -match '5d343a133e014d3c866928299bc267f0') {
    $content = $content -replace '5d343a133e014d3c866928299bc267f0', 'YOUR_ASSEMBLYAI_KEY'
    $changed = $true
}
if ($content -match '5f8efa436c8b19dc254bf10187621eb3dc988ac5') {
    $content = $content -replace '5f8efa436c8b19dc254bf10187621eb3dc988ac5', 'YOUR_DEEPGRAM_KEY'
    $changed = $true
}

if ($changed) {
    [System.IO.File]::WriteAllText($File, $content)
    Write-Host "Keys replaced in $File"
}
