using System.Windows;

namespace ATMegaPestaV1.Views;

/// <summary>
/// Asked when the ISP read attempts run out. A "Yes" (<c>DialogResult == true</c>) routes
/// through to the high-voltage module.
/// </summary>
public partial class HighVoltageDialog : Window
{
    public HighVoltageDialog()
    {
        InitializeComponent();
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
