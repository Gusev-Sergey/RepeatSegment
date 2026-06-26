$ErrorActionPreference = "Stop"
$base = "C:\ProjectsCSharp\RepeatSegment"
$publishDir = "$base\Publish\Release"
$setupBin = "$base\Setup\bin"
$setupObj = "$base\Setup\obj"

# Clean
Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $setupBin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $setupObj -ErrorAction SilentlyContinue

# Publish self-contained
Write-Host "=== Publishing self-contained ==="
dotnet publish "$base\RepeatSegment.App\RepeatSegment.App.csproj" -c Release -r win-x64 --self-contained -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

# Generate Product.wxs with ALL files
Write-Host "=== Generating Product.wxs ==="

$head = @'
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">

  <Package Name="RepeatSegment" Manufacturer="AstrorumArbor"
           Version="1.0.0.0" UpgradeCode="A1B2C3D4-E5F6-7890-ABCD-EF1234567890"
           Scope="perMachine" Compressed="yes">
    <MajorUpgrade Schedule="afterInstallValidate" AllowSameVersionUpgrades="yes"
                  DowngradeErrorMessage="A newer version is already installed." />
    <Media Id="1" Cabinet="RepeatSegment.cab" EmbedCab="yes" />

    <Feature Id="Main" Title="RepeatSegment" Level="1">
      <ComponentGroupRef Id="AppComponents" />
      <ComponentRef Id="ConfigTemplateComponent" />
      <ComponentRef Id="ShortcutComponent" />
    </Feature>

    <Property Id="ARPCONTACT" Value="astrorum_arbor@outlook.com" />
    <Property Id="ARPCOMMENTS" Value="Smart audio segment repeater. Developed with AI: VS Code + Zoo Code + DeepSeek API." />
    <Icon Id="AppIcon" SourceFile="..\RepeatSegment.App\Icons\app.ico" />
  </Package>

  <Fragment>
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="INSTALLFOLDER" Name="RepeatSegment" />
    </StandardDirectory>
    <StandardDirectory Id="ProgramMenuFolder">
      <Directory Id="StartMenuDir" Name="RepeatSegment" />
    </StandardDirectory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="AppComponents" Directory="INSTALLFOLDER">
'@

$foot = @'
    </ComponentGroup>
  </Fragment>

  <Fragment>
    <Component Id="ConfigTemplateComponent" Directory="INSTALLFOLDER" Guid="*">
      <File Id="ConfigTemplate" Source="..\RepeatSegment.App\config.template.ini" Name="config.template.ini" KeyPath="yes" />
    </Component>
  </Fragment>

  <Fragment>
    <Component Id="ShortcutComponent" Directory="StartMenuDir" Guid="*">
      <RegistryValue Root="HKCU" Key="Software\RepeatSegment" Name="installed" Value="1" Type="integer" KeyPath="yes" />
      <Shortcut Id="StartMenuShortcut" Name="RepeatSegment" Target="[INSTALLFOLDER]RepeatSegment.App.exe"
                WorkingDirectory="INSTALLFOLDER" Icon="AppIcon" Advertise="no" />
      <Shortcut Id="DesktopShortcut" Directory="DesktopFolder" Name="RepeatSegment"
                Target="[INSTALLFOLDER]RepeatSegment.App.exe"
                WorkingDirectory="INSTALLFOLDER" Icon="AppIcon" Advertise="no" />
      <RemoveFolder Id="RemoveStartMenu" Directory="StartMenuDir" On="uninstall" />
    </Component>
  </Fragment>

</Wix>
'@

$body = ""
Get-ChildItem -Path $publishDir -Recurse -File | ForEach-Object {
    $src = $_.FullName -replace '\\','\\'
    $g = [Guid]::NewGuid().ToString().ToUpper()
    $body += "      <Component Guid=`"$g`"><File Source=`"$src`" KeyPath=`"yes`" /></Component>`n"
}

# Also add language files
foreach ($lang in @("en","ru","de","fr","es")) {
    $src = "$base\RepeatSegment.App\lang\$lang.json" -replace '\\','\\'
    $g = [Guid]::NewGuid().ToString().ToUpper()
    $body += "      <Component Guid=`"$g`"><File Source=`"$src`" KeyPath=`"yes`" /></Component>`n"
}

# Also add config template  
$g = [Guid]::NewGuid().ToString().ToUpper()
$src = "$base\RepeatSegment.App\config.template.ini" -replace '\\','\\'
$body += "      <Component Guid=`"$g`"><File Source=`"$src`" Name=`"config.template.ini`" KeyPath=`"yes`" /></Component>`n"

$head + $body + $foot | Out-File -FilePath "$base\Setup\Product.wxs" -Encoding UTF8
Write-Host "Product.wxs generated. Lines: $((Get-Content $base\Setup\Product.wxs | Measure-Object -Line).Lines)"

# Build WiX
Write-Host "=== Building WiX ==="
dotnet build "$base\Setup\Setup.wixproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "WiX build failed" }

$msi = Get-ChildItem "$base\Setup\bin\Release\*.msi" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$sizeMB = [math]::Round($msi.Length / 1MB, 1)
Write-Host "=== DONE: $($msi.Name) — $sizeMB MB ==="
