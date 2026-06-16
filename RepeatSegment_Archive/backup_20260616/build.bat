@echo off
cd /d C:\ProjectsCSharp\RepeatSegment
echo === Cleaning ===
dotnet clean RepeatSegment.App\RepeatSegment.App.csproj --verbosity quiet
echo === Building ===
dotnet build RepeatSegment.App\RepeatSegment.App.csproj -c Debug --force --verbosity quiet
echo === Exit Code: %ERRORLEVEL% ===
echo === Checking exe ===
dir RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe
echo === Done ===