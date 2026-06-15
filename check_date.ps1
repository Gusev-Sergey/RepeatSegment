$exe = Get-Item 'C:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe'
"EXE: $($exe.Name)" | Out-File "$env:TEMP\rs_check.txt"
"Date: $($exe.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" | Out-File "$env:TEMP\rs_check.txt" -Append
"Size: $($exe.Length) bytes" | Out-File "$env:TEMP\rs_check.txt" -Append