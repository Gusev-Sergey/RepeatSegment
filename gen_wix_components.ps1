$ErrorActionPreference = "Stop"
$publishDir = "C:\ProjectsCSharp\RepeatSegment\Publish\Release"
$outputFile = "C:\ProjectsCSharp\RepeatSegment\Setup\AppComponents.wxs"
$baseDir = $publishDir -replace '\\$',''

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<Include>')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <ComponentGroup Id="AppComponents" Directory="INSTALLFOLDER">')

Get-ChildItem -Path $publishDir -Recurse -File | ForEach-Object {
    $fullPath = $_.FullName
    $relPath = $fullPath.Substring($baseDir.Length + 1)
    $g = [Guid]::NewGuid().ToString().ToUpper()
    $src = $fullPath -replace '\\','\\'
    $cId = "c" + [Guid]::NewGuid().ToString("N").Substring(0,12)
    $fId = "f" + [Guid]::NewGuid().ToString("N").Substring(0,12)
    
    [void]$sb.AppendLine("      <Component Id=`"$cId`" Guid=`"$g`">")
    [void]$sb.AppendLine("        <File Id=`"$fId`" Source=`"$src`" KeyPath=`"yes`" />")
    [void]$sb.AppendLine("      </Component>")
}

[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('</Include>')

$sb.ToString() | Out-File -FilePath $outputFile -Encoding UTF8 -NoNewline
$lines = (Get-Content $outputFile | Measure-Object -Line).Lines
Write-Host "Done. $lines lines written to $outputFile"
