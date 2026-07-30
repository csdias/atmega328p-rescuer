using System.Windows;
using System.Windows.Threading;

namespace ATMegaPestaV1.Views;

/// <summary>
/// Counts five seconds down to shutting the application down, giving the user a chance to
/// go back. <c>DialogResult == true</c> means "shut down"; cancelling or closing the
/// window returns false and the application stays open.
/// </summary>
public partial class ShutdownDialog : Window
{
    private const int SecondsUntilShutdown = 5;

    private readonly DispatcherTimer _timer;
    private int _remaining = SecondsUntilShutdown;

    public ShutdownDialog()
    {
        InitializeComponent();

        UpdateCountdown();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remaining--;

        if (_remaining > 0)
        {
            UpdateCountdown();
            return;
        }

        _timer.Stop();
        DialogResult = true;
    }

    private void UpdateCountdown()
    {
        TxtCountdown.Text = _remaining == 1
            ? "A encerrar dentro de 1 segundo..."
            : $"A encerrar dentro de {_remaining} segundos...";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = false;
    }

    /// <summary>
    /// Closing by the X counts as cancelling — but the timer has to stop either way, or
    /// it carries on firing against a window that is already closed.
    /// </summary>
    private void Window_Closed(object sender, EventArgs e)
    {
        _timer.Stop();
    }
}
