using System.Windows;

namespace ATMegaPestaV1.Views;

public partial class AjudaDialog : Window
{
    public AjudaDialog(string titulo, string conteudo)
    {
        InitializeComponent();
        TxtTitulo.Text = titulo;
        TxtConteudo.Text = conteudo;
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
