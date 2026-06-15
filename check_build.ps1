$exePath = "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe"
$dllPath = "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.dll"
$pdbPath = "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.pdb"

$log = ""

if (Test-Path $exePath) {
    $exe = Get-Item $exePath
    $log += "EXE: $($exe.FullName) | Size: $($exe.Length) | Modified: $($exe.LastWriteTime)`n"
} else {
    $log += "EXE: NOT FOUND`n"
}

if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    $log += "DLL: $($dll.FullName) | Size: $($dll.Length) | Modified: $($dll.LastWriteTime)`n"
} else {
    $log += "DLL: NOT FOUND`n"
}

if (Test-Path $pdbPath) {
    $pdb = Get-Item $pdbPath
    $log += "PDB: $($pdb.FullName) | Size: $($pdb.Length) | Modified: $($pdb.LastWriteTime)`n"
} else {
    $log += "PDB: NOT FOUND`n"
}

$log += "`nAll EXE files in bin:`n"
Get-ChildItem -Path "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin" -Recurse -Filter "*.exe" | ForEach-Object {
    $log += "  $($_.FullName) | Size: $($_.Length) | Modified: $($_.LastWriteTime)`n"
}

$log | Out-File -FilePath "C:\ProjectsCSharp\RepeatSegment\check_result.txt" -Encoding UTF8
Write-Output $log