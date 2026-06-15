$exe = Get-Item "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe" -ErrorAction SilentlyContinue
if ($exe) {
    $msg = "EXE date: " + $exe.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + " | Size: " + $exe.Length
} else {
    $msg = "EXE NOT FOUND"
}
$msg | Out-File -FilePath "C:\ProjectsCSharp\RepeatSegment\exe_date_result.txt" -Encoding ascii -Force
Write-Host $msg