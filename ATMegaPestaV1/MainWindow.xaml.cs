using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ATMegaPestaV1.Services;
using ATMegaPestaV1.Views;
using Microsoft.Extensions.Configuration;

namespace ATMegaPestaV1;

public partial class MainWindow : Window
{
    private readonly IServiceFactory _services;
    private readonly IDeviceDetector _detector;
    private readonly int _maxAttempts;
    private readonly bool _verifySignature;
    private int _currentAttempt;
    private bool _verificationDone;
    private bool _awaitingKey;
    private string? _comPort;
    private DispatcherTimer? _clock;

    private static readonly Brush GreenColour = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
    private static readonly Brush RedColour = new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));

    public MainWindow()
    {
        // Before InitializeComponent: the XAML marks MenuTests with IsChecked="True",
        // which fires MenuItem_Checked while the window is still being constructed.
        // Whatever that handler needs has to already be here.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _maxAttempts = config.GetValue<int>("MaxAttempts");
        if (_maxAttempts <= 0)
            _maxAttempts = 3;

        // The signature is option 1 of the firmware menu (Prog_Tester V1.2). Benches with
        // older firmware do not answer — hence the option to turn the check off.
        _verifySignature = config.GetValue("VerifySignature", true);

        // Service construction all lives here: from here down this window only knows
        // interfaces.
        _services = ServiceFactory.From(config);
        _detector = _services.CreateDetector();

        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RunVerificationAsync();
    }

    private async void BtnVerify_Click(object sender, RoutedEventArgs e)
    {
        await RunVerificationAsync();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_awaitingKey)
            Application.Current.Shutdown();
    }

    private bool _verificationRunning;

    private async Task RunVerificationAsync()
    {
        if (_verificationDone || _awaitingKey || _verificationRunning)
            return;

        _verificationRunning = true;
        BtnVerify.IsEnabled = false;
        _currentAttempt++;

        TxtMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
        TxtMessage.Text = "A verificar dispositivos...";

        var devices = await _detector.DetectAsync();

        UpdateCh340State(devices.Ch340Port);
        UpdateUsbAspState(devices.UsbAspConnected);

        var signatureValid = await VerifySignatureAsync(devices.Ch340Port);

        if (devices.Complete && signatureValid)
        {
            _verificationDone = true;
            _comPort = devices.Ch340Port;
            ShowMainPanel();
            _verificationRunning = false;
            return;
        }

        if (_currentAttempt >= _maxAttempts)
        {
            _awaitingKey = true;
            TxtMessage.Foreground = RedColour;
            TxtMessage.Text = "Não foi possível detectar todos os dispositivos após várias tentativas.\n" +
                              "Por favor, contacte a assistência técnica.";
            TxtAttempts.Text = "";
            TxtFooter.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
            TxtFooter.Text = "Pressione qualquer tecla para terminar a aplicação.";
            Focus();
            _verificationRunning = false;
            return;
        }

        var attemptsLeft = _maxAttempts - _currentAttempt;
        TxtAttempts.Text = $"Tentativa {_currentAttempt} de {_maxAttempts}";
        TxtMessage.Foreground = new SolidColorBrush(Color.FromRgb(0xFA, 0xB3, 0x87));
        TxtMessage.Text = "Dispositivo(s) em falta. Por favor, ligue o(s) dispositivo(s) e pressione o botão novamente.";
        BtnVerify.Content = $"Tentar Novamente ({attemptsLeft} restante{(attemptsLeft != 1 ? "s" : "")})";
        BtnVerify.IsEnabled = true;
        _verificationRunning = false;
    }

    private void ShowMainPanel()
    {
        VerificationPanel.Visibility = Visibility.Collapsed;
        MainPanel.Visibility = Visibility.Visible;

        UpdateClock();
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();

        NavigateTo("Tests");
    }

    private void UpdateClock()
    {
        TxtHeaderDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
    }

    /// <summary>
    /// Opens the high-voltage module from another screen. Checking the menu item (rather
    /// than calling NavigateTo directly) keeps the side menu in sync with what is being
    /// shown.
    /// </summary>
    public void OpenHighVoltageProgramming()
    {
        MenuProgramming.IsChecked = true;
    }

    private void MenuItem_Checked(object sender, RoutedEventArgs e)
    {
        if (MainContent is null)
            return;

        // MenuTests' IsChecked="True" fires this during InitializeComponent, with the
        // verification screen still to run: with no serial port detected, the view that
        // came out of it would be stuck to a bus that does not exist. What orders the
        // navigation to the tests is ShowMainPanel, after verification passes.
        if (!_verificationDone)
            return;

        if (sender == MenuTests)
            NavigateTo("Tests");
        else if (sender == MenuProgramming)
            NavigateTo("Programming");
        else if (sender == MenuSettings)
            NavigateTo("Settings");
    }

    private void NavigateTo(string module)
    {
        var busManager = _services.CreateBusManager(_comPort ?? "");
        var usbAspService = _services.CreateUsbAspService();

        MainContent.Content = module switch
        {
            "Tests" => new FunctionalTestsView(busManager, usbAspService),
            "Programming" => new UnderConstructionView("Programação de Alta Tensão"),
            "Settings" => new UnderConstructionView("Configurações"),
            _ => null
        };
    }

    private void UpdateCh340State(string? port)
    {
        if (port is not null)
        {
            LedCH340.Fill = GreenColour;
            TxtCH340Detail.Text = $"Conectado na porta {port}";
        }
        else
        {
            LedCH340.Fill = RedColour;
            TxtCH340Detail.Text = "Não detectado — Ligue o conversor USB-Serial CH340";
        }
    }

    private void UpdateUsbAspState(bool found)
    {
        if (found)
        {
            LedUSBAsp.Fill = GreenColour;
            TxtUSBaspDetail.Text = "Conectado";
        }
        else
        {
            LedUSBAsp.Fill = RedColour;
            TxtUSBaspDetail.Text = "Não detectado — Ligue o programador USBAsp";
        }
    }

    /// <summary>
    /// Asks the bench for its signature (menu option 1) and updates the matching LED.
    /// </summary>
    private async Task<bool> VerifySignatureAsync(string? port)
    {
        if (!_verifySignature)
        {
            UpdateSignatureStateSkipped();
            return true;
        }

        if (port is null)
        {
            UpdateSignatureState(null, null);
            return false;
        }

        TxtMessage.Text = "A verificar a assinatura do equipamento...";

        var result = await _services.CreateBusManager(port).VerifySignatureAsync();
        UpdateSignatureState(port, result);
        return result.Valid;
    }

    private void UpdateSignatureState(string? port, SignatureResult? result)
    {
        if (port is null || result is null)
        {
            LedSignature.Fill = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
            TxtSignatureDetail.Text = "A aguardar detecção do CH340...";
            return;
        }

        if (result.Valid)
        {
            LedSignature.Fill = GreenColour;
            TxtSignatureDetail.Text = $"Equipamento identificado: {result.Signature} na porta {port}";
        }
        else
        {
            LedSignature.Fill = RedColour;
            TxtSignatureDetail.Text = result.Response is { Length: > 0 } response
                ? $"Assinatura inesperada em {port} — recebido: {FirstLine(response)}"
                : $"Sem resposta do equipamento em {port} — Verifique a ligação";
        }
    }

    private static string FirstLine(string text) =>
        text.Split('\n')[0].Trim();

    private void UpdateSignatureStateSkipped()
    {
        LedSignature.Fill = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
        TxtSignatureDetail.Text = "Verificação de assinatura ignorada";
    }
}
