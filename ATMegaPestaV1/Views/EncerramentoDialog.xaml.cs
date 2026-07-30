using System.Windows;
using System.Windows.Threading;

namespace ATMegaPestaV1.Views;

/// <summary>
/// Conta cinco segundos até encerrar a aplicação, dando ao utilizador a hipótese
/// de voltar atrás. <c>DialogResult == true</c> significa "encerrar"; cancelar ou
/// fechar a janela devolve false e a aplicação continua aberta.
/// </summary>
public partial class EncerramentoDialog : Window
{
    private const int SegundosAteEncerrar = 5;

    private readonly DispatcherTimer _timer;
    private int _restantes = SegundosAteEncerrar;

    public EncerramentoDialog()
    {
        InitializeComponent();

        ActualizarContagem();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _restantes--;

        if (_restantes > 0)
        {
            ActualizarContagem();
            return;
        }

        _timer.Stop();
        DialogResult = true;
    }

    private void ActualizarContagem()
    {
        TxtContagem.Text = _restantes == 1
            ? "A encerrar dentro de 1 segundo..."
            : $"A encerrar dentro de {_restantes} segundos...";
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = false;
    }

    /// <summary>
    /// Fechar pelo X conta como cancelar — mas o timer tem de parar de qualquer
    /// forma, senão continua a disparar sobre uma janela já fechada.
    /// </summary>
    private void Window_Closed(object sender, EventArgs e)
    {
        _timer.Stop();
    }
}
