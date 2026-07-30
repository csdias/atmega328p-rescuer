using System.Windows;

namespace ATMegaPestaV1.Views;

/// <summary>O que o utilizador respondeu ao aviso da transferência.</summary>
public enum EscolhaTransferencia
{
    /// <summary>Não prosseguir. É o valor de quem fecha a janela pela cruz ou pelo Esc.</summary>
    Cancelar,

    /// <summary>Guardar a Flash e a EEPROM em ficheiro antes de prosseguir.</summary>
    ProsseguirComCopia,

    /// <summary>Prosseguir e aceitar a perda do que está no chip.</summary>
    ProsseguirSemCopia
}

/// <summary>
/// Aviso apresentado antes da verificação de integridade. Existe porque a verificação
/// não é uma leitura: o chip-alvo tem de receber a aplicação verificadora, o que
/// substitui o que o aluno lá tem. É a última oportunidade de guardar uma cópia.
/// </summary>
public partial class TransferenciaVerificadorDialog : Window
{
    /// <summary>
    /// A resposta. Fica em <see cref="EscolhaTransferencia.Cancelar"/> se a janela for
    /// fechada sem escolha — recusar é sempre o valor por omissão de um aviso destes.
    /// </summary>
    public EscolhaTransferencia Escolha { get; private set; } = EscolhaTransferencia.Cancelar;

    public TransferenciaVerificadorDialog()
    {
        InitializeComponent();
    }

    private void BtnComCopia_Click(object sender, RoutedEventArgs e) =>
        Responder(EscolhaTransferencia.ProsseguirComCopia);

    private void BtnSemCopia_Click(object sender, RoutedEventArgs e) =>
        Responder(EscolhaTransferencia.ProsseguirSemCopia);

    private void BtnCancelar_Click(object sender, RoutedEventArgs e) =>
        Responder(EscolhaTransferencia.Cancelar);

    private void Responder(EscolhaTransferencia escolha)
    {
        Escolha = escolha;
        DialogResult = escolha != EscolhaTransferencia.Cancelar;
    }
}
