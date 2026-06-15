@echo off
cd /d C:\ProjectsCSharp\RepeatSegment
echo === CLEANING === > build_log.txt 2>&1
rmdir /s /q RepeatSegment.App\bin 2>&1 >> build_log.txt
rmdir /s /q RepeatSegment.App\obj 2>&1 >> build_log.txt
echo === DOTNET BUILD === >> build_log.txt 2>&1
dotnet build RepeatSegment.App\RepeatSegment.App.csproj -c Debug >> build_log.txt 2>&1
echo EXIT CODE: %ERRORLEVEL% >> build_log.txt 2>&1
echo === EXE CHECK === >> build_log.txt 2>&1
dir RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe >> build_log.txt 2>&1
echo === DONE === >> build_log.txt 2>&1