# Skill: WPF Adaptive Layout

## Purpose
Create WPF windows and controls that adapt to any screen resolution.

## Key Rules

### 1. Window Sizing — Two-Level System
**XAML** (designer defaults):
```xml
<Window Width="520" Height="640" MinWidth="440" MinHeight="420"
        SizeToContent="Manual">
```

**Code-behind** (runtime override):
```csharp
double sw = SystemParameters.WorkArea.Width;
double sh = SystemParameters.WorkArea.Height;
Width = Math.Min(sw * 0.40, 520);       // proportion, cap
MinWidth = Math.Max(400, sw * 0.28);    // minimum threshold
MinHeight = Math.Max(380, sh * 0.30);
```

**Formula**: `Math.Min(WorkArea * proportion, max_pixels)` + `Math.Max(min_pixels, WorkArea * min_proportion)`.

### 2. Standard Proportions for Child Windows
| Window Type | Width | MinWidth |
|-------------|-------|----------|
| API Keys / Settings | `sw * 0.40` (max 520) | `sw * 0.28` (min 400) |
| Translation Settings | `sw * 0.38` (max 500) | `sw * 0.26` (min 360) |
| About | `sw * 0.38` (max 500) | `sw * 0.26` (min 360) |
| General Settings | `sw * 0.42` (max 540) | `sw * 0.28` (min 380) |
| Manual / User Guide | `sw * 0.65` (max 800) | `sw * 0.32` (min 420) |

### 3. Text Handling — Prevent Overflow
- **Global style**: `<Style TargetType="TextBlock"><Setter Property="TextWrapping" Value="Wrap"/></Style>`
- **Long descriptions**: add `MaxWidth` to prevent window stretching:
  ```xml
  <TextBlock Text="..." TextWrapping="Wrap" MaxWidth="480"/>
  ```
- **Never use `SizeToContent="WidthAndHeight"`** — it stretches the window to fit single-line text without wrapping.

### 4. ScrollViewer for Overflow Content
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <!-- content that might not fit -->
    </StackPanel>
</ScrollViewer>
```

### 5. MainWindow Proportions
```csharp
Width = sw * 0.85;
Height = sh * 0.48;
MinWidth = sw * 0.45;
MinHeight = Math.Max(380, sh * 0.35);
```

### 6. GrowWindowForTranslation Pattern
```csharp
private double _baseWindowH;
private void GrowWindowForTranslation()
{
    if (_baseWindowH <= 0) _baseWindowH = ActualHeight;  // fix base ONCE
    double grow = Math.Max(150, sh * 0.22);
    double maxH = sh * 0.85;
    double newH = _baseWindowH + grow;
    if (newH > maxH) newH = maxH;
    if (newH > ActualHeight) Height = newH;
}
```
**Critical**: use `_baseWindowH` (captured once), NOT `ActualHeight` (already modified by previous growth).

### 7. Button Sizing
```csharp
double bs = Math.Max(48, sw * 0.06);       // button size
double isize = bs * 0.85;                   // icon inside button
```

### 8. What NOT to do
- ❌ Hardcoded pixels: `Width=500`, `Height=360`, `MinHeight=180`
- ❌ `SizeToContent="WidthAndHeight"` for windows with long text
- ❌ `ActualHeight + N` in grow functions (cumulative growth)
- ❌ `TranslatePoint` for window sizing (coordinate system issues)
