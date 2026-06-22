# Skill: .NET Publish & Distribution

## Purpose
Configure `dotnet publish` for WPF applications — framework-dependent vs self-contained, WiX integration, size optimization.

## Key Rules

### 1. Publish Profiles

#### Framework-Dependent (for installer distribution)
```powershell
dotnet publish Project.csproj -c Release -r win-x64 -o Publish/Release
```
**.csproj requirement**:
```xml
<SelfContained>false</SelfContained>
```
Without this, `-r win-x64` always produces self-contained (870+ MB). Framework-dependent `.msi` ≈ 6-8 MB.

#### Self-Contained (for single-exe portable)
```powershell
dotnet publish Project.csproj -c Release -r win-x64 --self-contained -o Publish/Portable
```
Output: 870+ MB. No .NET Runtime needed on target PC. Use for USB sticks/portable apps.

### 2. WPF-Specific Limitations
- **WPF does NOT support trimming**: `PublishTrimmed=true` → `NETSDK1168` error.
- **Native DLLs**: `e_sqlite3.dll`, `libmp3lame.*.dll`, `WebView2Loader.dll` must be published with `-r win-x64`.
- **WebView2**: `Microsoft.Web.WebView2.*.dll` + `runtimes/win-x64/native/WebView2Loader.dll`.

### 3. Publish Output Structure
```
Publish/Release/
  App.dll, App.exe, App.deps.json, App.runtimeconfig.json
  *.dll (dependencies)
  Icons/ (all PNG + ICO)
  runtimes/win-x64/native/WebView2Loader.dll
```
Note: `lang/` folder is NOT in publish output for framework-dependent. Source from project folder.

### 4. WiX Integration
```xml
<!-- All DLLs + EXE -->
<ComponentGroup Id="AppComponents" Directory="INSTALLFOLDER">
  <Component Guid="*"><File Source="..\Publish\Release\App.dll" KeyPath="yes"/></Component>
  <Component Guid="*"><File Source="..\Publish\Release\App.exe" KeyPath="yes"/></Component>
  <!-- ... enumerate ALL DLLs ... -->
  <Component Guid="*"><File Source="..\Publish\Release\Icons\app.ico" KeyPath="yes"/></Component>
</ComponentGroup>
```
- **Never use wildcards** in WiX — `<Files Include="**\*">` is unstable.
- **Language files** from project source: `..\Project\lang\*.json`.
- **Config template** from project source: `..\Project\config.template.ini`.

### 5. Size Comparison
| Mode | .msi Size | .NET Runtime | Target |
|------|-----------|-------------|--------|
| Framework-dependent | ~6-8 MB | Required (user installs) | Installer distribution |
| Self-contained | ~870+ MB | Included | Portable / USB |

### 6. Prerequisites for Framework-Dependent
User needs:
1. [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)
2. WebView2 Runtime (preinstalled on Windows 11)

If missing, Windows AppHost shows "Download and install" dialog.

### 7. Common Errors
| Error | Cause | Fix |
|-------|-------|-----|
| NETSDK1168 | `PublishTrimmed=true` with WPF | Remove trimming |
| Self-contained despite `--self-contained false` | No `<SelfContained>false</SelfContained>` in csproj | Add to csproj |
| MSI size 876 KB (not MB) | WiX didn't harvest files | Clean bin/obj, rebuild |
| Duplicate WebView2Loader | DLL in root + runtimes/ | Include only one |
| Missing lang/ folder | Framework-dependent doesn't copy lang/ | Source from project |
