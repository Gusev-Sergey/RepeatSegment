$ErrorActionPreference = "Stop"
Set-Location C:\ProjectsCSharp\RepeatSegment

$logFile = "C:\ProjectsCSharp\RepeatSegment\rebuild_result.txt"

"=== START $(Get-Date) ===" | Out-File $logFile

# Kill any running instance
Get-Process -Name "RepeatSegment*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Force delete bin and obj
"=== Removing bin/obj ===" | Out-File $logFile -Append
Remove-Item -Recurse -Force "RepeatSegment.App\bin" -ErrorAction SilentlyContinue 2>&1 | Out-File $logFile -Append
Remove-Item -Recurse -Force "RepeatSegment.App\obj" -ErrorAction SilentlyContinue 2>&1 | Out-File $logFile -Append
Start-Sleep -Seconds 1

"bin exists: $(Test-Path 'RepeatSegment.App\bin')" | Out-File $logFile -Append
"obj exists: $(Test-Path 'RepeatSegment.App\obj')" | Out-File $logFile -Append

# Build
"=== dotnet build ===" | Out-File $logFile -Append
$result = dotnet build "RepeatSegment.App\RepeatSegment.App.csproj" -c Debug --no-incremental 2>&1
$result | Out-File $logFile -Append
"EXIT CODE: $LASTEXITCODE" | Out-File $logFile -Append

# Check exe
"=== EXE ===" | Out-File $logFile -Append
$exe = Get-Item "RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe" -ErrorAction SilentlyContinue
if ($exe) {
    "EXE: $($exe.FullName)" | Out-File $logFile -Append
    "Date: $($exe.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" | Out-File $logFile -Append
    "Size: $($exe.Length)" | Out-File $logFile -Append
} else {
    "EXE NOT FOUND" | Out-File $logFile -Append
}

"=== DONE ===" | Out-File $logFile -Append
Write-Host "Done. Check rebuild_result.txt"