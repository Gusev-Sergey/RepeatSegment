using System.Windows;

namespace RepeatSegment.App;

public partial class FirstRunWindow : Window
{
    public string? SelectedLanguage { get; private set; }

    public FirstRunWindow()
    {
        InitializeComponent();
    }

    private void BtnEn_Click(object s, RoutedEventArgs e)
    {
        SelectedLanguage = "en";
        DialogResult = true;
        Close();
    }

    private void BtnRu_Click(object s, RoutedEventArgs e)
    {
        SelectedLanguage = "ru";
        DialogResult = true;
        Close();
    }
}
