using System.Windows;

namespace ATMegaPestaV1.Views;

/// <summary>
/// Perguntado quando as tentativas de leitura por ISP se esgotam. Um "Sim"
/// (<c>DialogResult == true</c>) encaminha para o módulo de alta tensão.
/// </summary>
public partial class AltaTensaoDialog : Window
{
    public AltaTensaoDialog()
    {
        InitializeComponent();
    }

    private void BtnSim_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnNao_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
