using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace RepeatSegment.App;

public class WaveformGraph : SKElement
{
    private float[]? _samplesSmall;
    private int _sampleRateSmall = 1000;
    private double _durationSeconds;
    private double _positionSeconds;
    private List<(double T1, double T2)> _fragments = new();
    private int _currentCounter;
    private bool _repeatSegment;
    private bool _playGoMode;

    private const double WindowWidthSec = 25.0;

    private static readonly SKColor BgColor = new(0xE8, 0xE8, 0xE8);
    private static readonly SKColor BgColorDark = new(0x25, 0x25, 0x25);
    private static readonly SKColor WaveColor = new(0x3A, 0x7B, 0xD5);
    private static readonly SKColor WaveColorDark = new(0x5A, 0x9B, 0xE6);
    private static readonly SKColor CursorColor = new(0xE0, 0x40, 0x40);
    private static readonly SKColor SegmentLineColor = new(0xFF, 0x8C, 0x00);
    private static readonly SKColor SegmentLineDark = new(0xFF, 0x8C, 0x00);
    private static readonly SKColor HighlightFill = new(0x40, 0x3A, 0x7B, 0xD5);
    private static readonly SKColor HighlightFillDark = new(0x40, 0x5A, 0x9B, 0xE6);
    private static readonly SKColor UserSegmentColor = new(0x40, 0xE0, 0x40, 0x60); // light green 38% opaque

    private bool _isDarkTheme;

    private bool _isDragging;
    private double _dragStartSec;
    private double _dragEndSec;
    private double _dragVisLeft, _dragVisRight, _dragW;
    private (double T1, double T2)? _userSegment;

    public event Action<double, double>? SegmentSelected;

    public float[]? SamplesSmall { get => _samplesSmall; set { _samplesSmall = value; InvalidateVisual(); } }
    public int SampleRateSmall { get => _sampleRateSmall; set { _sampleRateSmall = value; InvalidateVisual(); } }
    public double DurationSeconds { get => _durationSeconds; set { _durationSeconds = value; InvalidateVisual(); } }
    public double PositionSeconds { get => _positionSeconds; set { _positionSeconds = value; InvalidateVisual(); } }
    public List<(double T1, double T2)> Fragments { get => _fragments; set { _fragments = value ?? new(); InvalidateVisual(); } }
    public int CurrentCounter { get => _currentCounter; set { _currentCounter = value; InvalidateVisual(); } }
    public bool RepeatSegment { get => _repeatSegment; set { _repeatSegment = value; InvalidateVisual(); } }
    public bool PlayGoMode { get => _playGoMode; set { _playGoMode = value; InvalidateVisual(); } }
    public bool IsDarkTheme { get => _isDarkTheme; set { _isDarkTheme = value; InvalidateVisual(); } }

    public WaveformGraph() { this.RenderTransform = null; }

    private const float RulerHeight = 22f;

    private (double visLeft, double visRight, double visWidth) GetVisibleWindow()
    {
        double duration = _durationSeconds > 0 ? _durationSeconds : 1.0;
        double pos = Math.Clamp(_positionSeconds, 0, duration);
        double halfWin = WindowWidthSec / 2.0;
        double visLeft = pos - halfWin;
        double visRight = pos + halfWin;
        if (visLeft < 0) { visRight -= visLeft; visLeft = 0; }
        if (visRight > duration) { visLeft -= (visRight - duration); visRight = duration; if (visLeft < 0) visLeft = 0; }
        return (visLeft, visRight, visRight - visLeft);
    }

    private double PixelToSeconds(double pixelX, double canvasW, double visLeft, double visWidth)
        => visLeft + (pixelX / canvasW) * visWidth;

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (_samplesSmall == null || _fragments == null) return;
        var pos = e.GetPosition(this);
        var (visLeft, visRight, visWidth) = GetVisibleWindow();
        if (visWidth <= 0) return;
        _dragStartSec = PixelToSeconds(pos.X, ActualWidth, visLeft, visWidth);
        _dragEndSec = _dragStartSec;
        _dragVisLeft = visLeft;
        _dragVisRight = visRight;
        _dragW = ActualWidth;
        _isDragging = true;
        this.CaptureMouse();
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging) return;
        var pos = e.GetPosition(this);
        double visWidth = _dragVisRight - _dragVisLeft;
        if (visWidth <= 0) return;
        _dragEndSec = PixelToSeconds(pos.X, _dragW, _dragVisLeft, visWidth);
        _dragEndSec = Math.Clamp(_dragEndSec, 0, _durationSeconds);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isDragging) return;
        _isDragging = false;
        this.ReleaseMouseCapture();

        double t1 = Math.Min(_dragStartSec, _dragEndSec);
        double t2 = Math.Max(_dragStartSec, _dragEndSec);
        if (t2 - t1 >= 0.5)
        {
            _userSegment = (t1, t2);
            SegmentSelected?.Invoke(t1, t2);
        }
        _dragStartSec = 0;
        _dragEndSec = 0;
        InvalidateVisual();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        float w = info.Width;
        float h = info.Height;
        if (w <= 0 || h <= 0) return;
        canvas.Clear(_isDarkTheme ? BgColorDark : BgColor);
        if (_samplesSmall == null || _samplesSmall.Length == 0) return;

        double duration = _durationSeconds > 0 ? _durationSeconds : 1.0;
        double pos = Math.Clamp(_positionSeconds, 0, duration);

        float waveAreaH = h - RulerHeight;
        if (waveAreaH < 10) waveAreaH = h;

        double halfWin = WindowWidthSec / 2.0;
        double visLeft = pos - halfWin;
        double visRight = pos + halfWin;
        if (visLeft < 0) { visRight -= visLeft; visLeft = 0; }
        if (visRight > duration) { visLeft -= (visRight - duration); visRight = duration; if (visLeft < 0) visLeft = 0; }
        double visWidth = visRight - visLeft;
        if (visWidth <= 0) return;

        DrawSegmentHighlight(canvas, w, waveAreaH, visLeft, visRight);
        DrawSegmentLines(canvas, w, waveAreaH, visLeft, visRight);
        DrawUserSegment(canvas, w, waveAreaH, visLeft, visRight);
        DrawWaveform(canvas, w, waveAreaH, visLeft, visRight);
        DrawCursor(canvas, w, waveAreaH, pos, visLeft, visRight);
        DrawTimeRuler(canvas, w, h, visLeft, visRight);
    }

    private void DrawUserSegment(SKCanvas canvas, float w, float h, double visLeft, double visRight)
    {
        (double t1, double t2) = _isDragging
            ? (Math.Min(_dragStartSec, _dragEndSec), Math.Max(_dragStartSec, _dragEndSec))
            : _userSegment ?? (0, 0);

        if (!_isDragging && _userSegment == null) return;
        if (t2 - t1 < 0.25) return;

        double visWidth = visRight - visLeft;
        if (visWidth <= 0) return;

        double x1 = (t1 - visLeft) / visWidth * w;
        double x2 = (t2 - visLeft) / visWidth * w;
        x1 = Math.Max(0, Math.Min(w, x1));
        x2 = Math.Max(0, Math.Min(w, x2));
        if (x2 - x1 < 1) return;

        canvas.DrawRect((float)x1, 0, (float)(x2 - x1), h,
            new SKPaint { Color = UserSegmentColor, Style = SKPaintStyle.Fill });
    }

    private void DrawSegmentHighlight(SKCanvas canvas, float w, float h, double visLeft, double visRight)
    {
        if (_fragments.Count == 0 || _currentCounter >= _fragments.Count) return;
        if (!_repeatSegment) return;
        var (t1, t2) = _fragments[_currentCounter];
        if (t2 <= visLeft || t1 >= visRight) return;
        double x1 = (t1 - visLeft) / (visRight - visLeft) * w;
        double x2 = (t2 - visLeft) / (visRight - visLeft) * w;
        x1 = Math.Max(0, Math.Min(w, x1));
        x2 = Math.Max(0, Math.Min(w, x2));
        canvas.DrawRect((float)x1, 0, (float)(x2 - x1), h,
            new SKPaint { Color = _isDarkTheme ? HighlightFillDark : HighlightFill, Style = SKPaintStyle.Fill });
    }

    private void DrawSegmentLines(SKCanvas canvas, float w, float h, double visLeft, double visRight)
    {
        if (_fragments.Count == 0) return;
        var paint = new SKPaint
        {
            Color = _isDarkTheme ? SegmentLineDark : SegmentLineColor,
            StrokeWidth = 4.0f,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 8, 4 }, 0),
        };
        foreach (var (t1, t2) in _fragments)
        {
            if (t1 >= visLeft && t1 <= visRight) canvas.DrawLine((float)((t1 - visLeft) / (visRight - visLeft) * w), 0, (float)((t1 - visLeft) / (visRight - visLeft) * w), h, paint);
            if (t2 >= visLeft && t2 <= visRight) canvas.DrawLine((float)((t2 - visLeft) / (visRight - visLeft) * w), 0, (float)((t2 - visLeft) / (visRight - visLeft) * w), h, paint);
        }
    }

    private void DrawWaveform(SKCanvas canvas, float w, float h, double visLeft, double visRight)
    {
        if (_samplesSmall == null || _samplesSmall.Length == 0) return;
        int sr = _sampleRateSmall > 0 ? _sampleRateSmall : 1000;
        int startIdx = (int)(visLeft * sr);
        int endIdx = (int)(visRight * sr);
        startIdx = Math.Max(0, startIdx);
        endIdx = Math.Min(_samplesSmall.Length, endIdx);
        int count = endIdx - startIdx;
        if (count <= 1) return;

        var paint = new SKPaint { Color = _isDarkTheme ? WaveColorDark : WaveColor, StrokeWidth = 1, Style = SKPaintStyle.Stroke, IsAntialias = true };
        float midY = h / 2f;
        float scaleY = h / 2f * 0.85f;
        var path = new SKPath();
        bool first = true;
        int maxPoints = (int)w * 4;
        int step = Math.Max(1, count / maxPoints);
        for (int i = 0; i < count; i += step)
        {
            float val = _samplesSmall[startIdx + i];
            float x = (float)i / count * w;
            float y = midY - val * scaleY;
            y = Math.Clamp(y, 0, h);
            if (first) { path.MoveTo(x, y); first = false; }
            else { path.LineTo(x, y); }
        }
        canvas.DrawPath(path, paint);
    }

    private void DrawCursor(SKCanvas canvas, float w, float h, double pos, double visLeft, double visRight)
    {
        double visWidth = visRight - visLeft;
        if (visWidth <= 0) return;
        float cursorX = (float)((pos - visLeft) / visWidth * w);
        cursorX = Math.Clamp(cursorX, 0, w);
        canvas.DrawLine(cursorX, 0, cursorX, h,
            new SKPaint { Color = CursorColor, StrokeWidth = 2, Style = SKPaintStyle.Stroke });
    }

    private void DrawTimeRuler(SKCanvas canvas, float w, float h, double visLeft, double visRight)
    {
        double visWidth = visRight - visLeft;
        if (visWidth <= 0) return;
        float rulerTop = h - RulerHeight + 2;
        float rulerMid = h - 10;
        float rulerBottom = h - 4;

        var paint = new SKPaint { Color = _isDarkTheme ? new SKColor(0xCC, 0xCC, 0xCC) : new SKColor(0x55, 0x55, 0x55), StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke, IsAntialias = true };
        var textPaint = new SKPaint { Color = _isDarkTheme ? new SKColor(0xEE, 0xEE, 0xEE) : new SKColor(0x22, 0x22, 0x22), TextSize = 16, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Segoe UI") };

        double firstTick = Math.Ceiling(visLeft / 5.0) * 5.0;
        for (double t = firstTick; t <= visRight; t += 5.0)
        {
            float x = (float)((t - visLeft) / visWidth * w);
            canvas.DrawLine(x, rulerTop, x, rulerBottom, paint);
            int mins = (int)(t / 60);
            int secs = (int)(t % 60);
            canvas.DrawText($"{mins}:{secs:D2}", x + 2, h - 1, textPaint);
        }

        var subPaint = new SKPaint { Color = _isDarkTheme ? new SKColor(0x88, 0x88, 0x88) : new SKColor(0x99, 0x99, 0x99), StrokeWidth = 1f, Style = SKPaintStyle.Stroke };
        double firstSub = Math.Ceiling(visLeft);
        for (double t = firstSub; t <= visRight; t += 1.0)
        {
            if (Math.Abs(t % 5.0) < 0.01) continue;
            float x = (float)((t - visLeft) / visWidth * w);
            canvas.DrawLine(x, rulerTop + 6, x, rulerMid, subPaint);
        }
    }
}
