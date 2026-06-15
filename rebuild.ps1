$ErrorActionPreference = "Continue"
Set-Location C:\ProjectsCSharp\RepeatSegment

Write-Host "Cleaning..."
dotnet clean RepeatSegment.App\RepeatSegment.App.csproj --verbosity quiet 2>&1 | Out-Null

Write-Host "Removing bin/obj..."
Remove-Item -Recurse -Force RepeatSegment.App\bin, RepeatSegment.App\obj -ErrorAction SilentlyContinue

Write-Host "Building..."
$output = & dotnet build RepeatSegment.App\RepeatSegment.App.csproj -c Debug --verbosity normal 2>&1
$output | Out-String | Set-Content -Path "$env:TEMP\rs_build_output.txt" -Encoding UTF8

Write-Host "Build exit code: $LASTEXITCODE"

$exe = Get-Item "RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe" -ErrorAction SilentlyContinue
if ($exe) {
    $date = $exe.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
    $size = $exe.Length
    "EXE: $date, $size bytes" | Set-Content -Path "$env:TEMP\rs_exe_info.txt" -Encoding UTF8
    Write-Host "EXE date: $date, size: $size"
} else {
    "EXE NOT FOUND" | Set-Content -Path "$env:TEMP\rs_exe_info.txt" -Encoding UTF8
    Write-Host "EXE NOT FOUND!"
}