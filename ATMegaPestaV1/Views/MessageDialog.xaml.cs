using System.Windows;

namespace ATMegaPestaV1.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string title, string content)
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
