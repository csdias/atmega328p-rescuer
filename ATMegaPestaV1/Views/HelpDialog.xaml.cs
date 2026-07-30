using System.Windows;

namespace ATMegaPestaV1.Views;

public partial class HelpDialog : Window
{
    public HelpDialog(string title, string content)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtContent.Text = content;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
