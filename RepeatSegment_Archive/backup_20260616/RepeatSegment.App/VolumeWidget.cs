using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RepeatSegment.App;

/// <summary>
/// Graphical volume control widget — 5 vertical bars + triangular indicator.
/// Mirrors Python's VolumeWidget with dynamic bar heights.
/// </summary>
public class VolumeWidget : UserControl
{
    private readonly Rectangle[] _bars = new Rectangle[5];
    private readonly Polygon _indicator;
    private double _volume = 0.7;
    private bool _isDragging;

    private const int BarW = 20;
    private const int Gap = 12;
    private const int NumBars = 5;
    private const int TriSize = 14;

    public event EventHandler<double>? VolumeChanged;

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            UpdateBars();
            VolumeChanged?.Invoke(this, _volume);
        }
    }

    public System.Drawing.Color BarColor { get; set; } = System.Drawing.Color.FromArgb(0x3A, 0x7B, 0xD5);
    public System.Drawing.Color BgBarColor { get; set; } = System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD);

    public VolumeWidget()
    {
        Width = 160;
        Height = 80;
        MinWidth = 160;
        MinHeight = 64;
        MaxHeight = 80;
        ClipToBounds = false;

        var canvas = new Canvas();
        Content = canvas;

        // ── Create 5 bars ──
        for (int i = 0; i < NumBars; i++)
        {
            var bar = new Rectangle
            {
                Width = BarW,
                RadiusX = 3,
                RadiusY = 3,
                Fill = Brushes.Gray,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 1
            };
            _bars[i] = bar;
            canvas.Children.Add(bar);
        }

        // ── Triangular indicator ──
        _indicator = new Polygon
        {
            Fill = new SolidColorBrush(Color.FromRgb(58, 123, 213)),
            Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            StrokeThickness = 1
        };
        canvas.Children.Add(_indicator);

        // ── Size changed → reposition ──
        SizeChanged += (s, e) => UpdateBars();

        // ── Mouse events ──
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;

        UpdateBars();
    }

    /// <summary>Reposition bars and indicator based on current volume and size.</summary>
    private void UpdateBars()
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 1 || h < 1) return;

        double barsW = NumBars * BarW + (NumBars - 1) * Gap;
        double startX = (w - barsW) / 2;
        double barHMax = h - 28;
        double baseY = h - 12;

        int activeBars = (int)Math.Round(_volume * NumBars);

        for (int i = 0; i < NumBars; i++)
        {
            double x = startX + i * (BarW + Gap);
            // Dynamic height: proportional to (i+1)/NumBars * volume
            double barH = barHMax * (i + 1.0) / NumBars * _volume;
            if (barH < 2) barH = 0;

            Canvas.SetLeft(_bars[i], x);
            Canvas.SetTop(_bars[i], baseY - barH);
            _bars[i].Height = barH;
            _bars[i].Width = BarW;

            _bars[i].Fill = i < activeBars
                ? new SolidColorBrush(Color.FromRgb(58, 123, 213))
                : new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }

        // ── Triangular indicator ──
        double triCX = startX + barsW * _volume;
        triCX = Math.Max(startX, Math.Min(startX + barsW, triCX));
        double triY = baseY + 6;

        _indicator.Points = new PointCollection
        {
            new Point(triCX, triY - TriSize),
            new Point(triCX - TriSize, triY),
            new Point(triCX + TriSize, triY)
        };
    }

    private double PosToVolume(double x)
    {
        double w = ActualWidth;
        double barsW = NumBars * BarW + (NumBars - 1) * Gap;
        double startX = (w - barsW) / 2;
        double val = (x - startX) / barsW;
        return Math.Clamp(val, 0, 1);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        CaptureMouse();
        Volume = PosToVolume(e.GetPosition(this).X);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
            Volume = PosToVolume(e.GetPosition(this).X);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }
}