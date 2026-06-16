using System.Windows;

namespace RepeatSegment.App;

public partial class ManualWindow : Window
{
    public ManualWindow(MainWindow mainWindow)
    {
        Owner = mainWindow;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Inject brushes from MainWindow for theme support
        foreach (var key in mainWindow.Resources.Keys)
        {
            if (mainWindow.Resources[key] is System.Windows.Media.SolidColorBrush brush)
                Resources[key] = new System.Windows.Media.SolidColorBrush(brush.Color);
        }
        if (Resources["WindowBackgroundBrush"] is System.Windows.Media.SolidColorBrush bg)
            Background = bg;

        InitializeComponent();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
