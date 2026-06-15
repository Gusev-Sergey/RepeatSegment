@echo off
cd /d C:\ProjectsCSharp\RepeatSegment
echo === FORCE BUILD STARTED %date% %time% === > %TEMP%\rs_build_log.txt
echo Deleting bin and obj... >> %TEMP%\rs_build_log.txt
rmdir /s /q RepeatSegment.App\bin 2>> %TEMP%\rs_build_log.txt
rmdir /s /q RepeatSegment.App\obj 2>> %TEMP%\rs_build_log.txt
echo Building... >> %TEMP%\rs_build_log.txt
dotnet build RepeatSegment.App\RepeatSegment.App.csproj -c Debug --force --verbosity normal >> %TEMP%\rs_build_log.txt 2>&1
echo EXIT CODE: %ERRORLEVEL% >> %TEMP%\rs_build_log.txt
echo Build finished %date% %time% >> %TEMP%\rs_build_log.txt
echo === EXE INFO === >> %TEMP%\rs_build_log.txt
dir RepeatSegment.App\bin\Debug\net8.0-windows\RepeatSegment.App.exe >> %TEMP%\rs_build_log.txt 2>&1
echo === COMPLETE === >> %TEMP%\rs_build_log.txt