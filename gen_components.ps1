$publishDir = 'C:\ProjectsCSharp\RepeatSegment\Publish\Release'
$outFile = 'C:\ProjectsCSharp\RepeatSegment\Setup\components.inc'

$lines = @()
Get-ChildItem -Path $publishDir -Recurse -File | ForEach-Object {
    $src = $_.FullName -replace '\\','\\'
    $g = [Guid]::NewGuid().ToString().ToUpper()
    $lines += "      <Component Guid=`"$g`"><File Source=`"$src`" KeyPath=`"yes`" /></Component>"
}

$lines -join "`n" | Out-File -FilePath $outFile -Encoding UTF8
Write-Host "Generated $($lines.Count) components"
