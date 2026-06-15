@echo off
cd /d C:\ProjectsCSharp\RepeatSegment
echo === Cleaning bin and obj ===
rmdir /s /q "RepeatSegment.App\bin" 2>nul
rmdir /s /q "RepeatSegment.App\obj" 2>nul
echo === Building ===
dotnet build "RepeatSegment.App\RepeatSegment.App.csproj" -c Debug 2>&1
echo === Exit Code: %ERRORLEVEL% ===
echo === Checking EXE ===
dir "RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe" 2>nul
echo === DONE ===
pause