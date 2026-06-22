# Skill: WiX Installer

## Purpose
Build `.msi` installers for .NET WPF applications using WiX Toolset v4.

## Key Rules

### 1. Self-Contained vs Framework-Dependent
- **Never use `--self-contained`** for distribution to other PCs. It bundles .NET Runtime (~800 MB extra).
- Add `<SelfContained>false</SelfContained>` to `.csproj`. Without this, `dotnet publish -r win-x64` always produces self-contained.
- Framework-dependent `.msi` ≈ 6-8 MB; self-contained ≈ 870+ MB.

### 2. Publish Command
```powershell
dotnet publish ProjectFolder\Project.csproj -c Release -r win-x64 -o Publish\Release
```
- `-r win-x64` is required for native DLLs (e_sqlite3, libmp3lame, WebView2Loader).
- WPF does NOT support trimming (`PublishTrimmed=true` → error).

### 3. WiX Product.wxs Structure
- **Always enumerate files explicitly**: `<Component Guid="*"><File Source="..\Publish\Release\Some.dll" KeyPath="yes"/></Component>`
- **Avoid wildcards**: `<Files Include="**\*">` is unstable in WiX v4 — may miss files or create duplicates.
- **Language files**: source from project `lang/` folder (not `Publish/Release/lang/` — not present).
- **Config template**: `<File Source="..\Project\config.template.ini" Name="config.template.ini"/>` — no real API keys.
- **Shortcuts**: `<Shortcut Id="..." Name="App" Target="[INSTALLFOLDER]App.exe" WorkingDirectory="INSTALLFOLDER" Icon="AppIcon"/>`

### 4. Build Command
```powershell
dotnet build Setup\Setup.wixproj -c Release
```
- Output: `Setup\bin\Release\App-Installer.msi`
- **Clean before rebuild** when file list changes: delete `Setup\bin\` and `Setup\obj\`.

### 5. Common Pitfalls
- Duplicate `WebView2Loader.dll`: appears both in root and `runtimes\win-x64\native\`. Include only one.
- WiX caches old files — clean `obj/` and `bin/` between publish changes.
- `#` in WiX paths (e.g., `obj\#App.cab`) is normal.
- MSI size: check with `dir` — 876,544 bytes is 876 KB (empty/wrong), should be 6,000,000+.

## Package Metadata
```xml
<Package Name="AppName" Manufacturer="Developer"
         Version="1.0.0.0" UpgradeCode="GUID-HERE"
         Scope="perMachine" Compressed="yes">
  <Property Id="ARPCONTACT" Value="email@example.com"/>
  <Property Id="ARPCOMMENTS" Value="Description"/>
  <Icon Id="AppIcon" SourceFile="..\Project\Icons\app.ico"/>
```

## Test After Build
- Install on clean VM/PC without .NET SDK.
- Verify shortcuts (Start Menu + Desktop).
- Run app — if .NET Runtime missing, Windows shows download dialog.
