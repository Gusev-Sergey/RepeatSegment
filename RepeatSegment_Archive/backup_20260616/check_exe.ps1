$exe = Get-Item "C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe"
$info = "EXE: $($exe.FullName)`nDate: $($exe.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))`nSize: $($exe.Length) bytes"
$info | Out-File -FilePath "C:\ProjectsCSharp\RepeatSegment\exe_info.txt" -Encoding utf8