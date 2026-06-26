$ErrorActionPreference = "Stop"
$publish = "C:\ProjectsCSharp\RepeatSegment\Publish\Release"
$outFile = "C:\ProjectsCSharp\RepeatSegment\Setup\Product.wxs"
$base = "C:\ProjectsCSharp\RepeatSegment"

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Package Name="RepeatSegment" Manufacturer="AstrorumArbor"')
[void]$sb.AppendLine('           Version="1.0.0.0" UpgradeCode="A1B2C3D4-E5F6-7890-ABCD-EF1234567890"')
[void]$sb.AppendLine('           Scope="perMachine" Compressed="yes">')
[void]$sb.AppendLine('    <MajorUpgrade Schedule="afterInstallValidate" AllowSameVersionUpgrades="yes"')
[void]$sb.AppendLine('                  DowngradeErrorMessage="A newer version is already installed." />')
[void]$sb.AppendLine('    <Media Id="1" Cabinet="RepeatSegment.cab" EmbedCab="yes" />')
[void]$sb.AppendLine('    <Feature Id="Main" Title="RepeatSegment" Level="1">')
[void]$sb.AppendLine('      <ComponentGroupRef Id="AppComponents" />')
[void]$sb.AppendLine('      <ComponentRef Id="ShortcutComponent" />')
[void]$sb.AppendLine('    </Feature>')
[void]$sb.AppendLine('    <Property Id="ARPCONTACT" Value="astrorum_arbor@outlook.com" />')
[void]$sb.AppendLine('    <Property Id="ARPCOMMENTS" Value="Smart audio segment repeater." />')
[void]$sb.AppendLine('    <Icon Id="AppIcon" SourceFile="..\RepeatSegment.App\Icons\app.ico" />')
[void]$sb.AppendLine('  </Package>')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <StandardDirectory Id="ProgramFiles6432Folder">')
[void]$sb.AppendLine('      <Directory Id="INSTALLFOLDER" Name="RepeatSegment" />')
[void]$sb.AppendLine('    </StandardDirectory>')
[void]$sb.AppendLine('    <StandardDirectory Id="ProgramMenuFolder">')
[void]$sb.AppendLine('      <Directory Id="StartMenuDir" Name="RepeatSegment" />')
[void]$sb.AppendLine('    </StandardDirectory>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <ComponentGroup Id="AppComponents" Directory="INSTALLFOLDER">')

$count = 0
$usedIds = @{}

function New-Id($prefix) {
    $id = $prefix + [Guid]::NewGuid().ToString("N").Substring(0, 16).ToUpper()
    while ($usedIds.ContainsKey($id)) {
        $id = $prefix + [Guid]::NewGuid().ToString("N").Substring(0, 16).ToUpper()
    }
    $usedIds[$id] = $true
    return $id
}

Get-ChildItem -Path $publish -Recurse -File | ForEach-Object {
    $src = $_.FullName
    $relPath = $src.Substring($publish.Length + 1)
    $cId = New-Id "c"
    $fId = New-Id "f"
    $g = [Guid]::NewGuid().ToString().ToUpper()
    [void]$sb.AppendLine("      <Component Id=`"$cId`" Guid=`"$g`"><File Id=`"$fId`" Source=`"$src`" KeyPath=`"yes`" /></Component>")
    $count++
}

foreach ($lc in @('en','ru','de','fr','es')) {
    $src = "$base\RepeatSegment.App\lang\$lc.json"
    $cId = New-Id "c"
    $fId = New-Id "f"
    $g = [Guid]::NewGuid().ToString().ToUpper()
    [void]$sb.AppendLine("      <Component Id=`"$cId`" Guid=`"$g`"><File Id=`"$fId`" Source=`"$src`" KeyPath=`"yes`" /></Component>")
    $count++
}

$cfg = "$base\RepeatSegment.App\config.template.ini"
$cId = New-Id "c"
$fId = New-Id "f"
$g = [Guid]::NewGuid().ToString().ToUpper()
[void]$sb.AppendLine("      <Component Id=`"$cId`" Guid=`"$g`"><File Id=`"$fId`" Source=`"$cfg`" Name=`"config.template.ini`" KeyPath=`"yes`" /></Component>")

[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <Component Id="ShortcutComponent" Directory="StartMenuDir" Guid="*">')
[void]$sb.AppendLine('      <RegistryValue Root="HKCU" Key="Software\RepeatSegment" Name="installed" Value="1" Type="integer" KeyPath="yes" />')
[void]$sb.AppendLine('      <Shortcut Id="StartMenuShortcut" Name="RepeatSegment" Target="[INSTALLFOLDER]RepeatSegment.App.exe" WorkingDirectory="INSTALLFOLDER" Icon="AppIcon" Advertise="no" />')
[void]$sb.AppendLine('      <Shortcut Id="DesktopShortcut" Directory="DesktopFolder" Name="RepeatSegment" Target="[INSTALLFOLDER]RepeatSegment.App.exe" WorkingDirectory="INSTALLFOLDER" Icon="AppIcon" Advertise="no" />')
[void]$sb.AppendLine('      <RemoveFolder Id="RemoveStartMenu" Directory="StartMenuDir" On="uninstall" />')
[void]$sb.AppendLine('    </Component>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('</Wix>')

[System.IO.File]::WriteAllText($outFile, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Product.wxs: $count components written"
