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
    private readonly IServiceFactory _servicos;
    private readonly IDeviceDetector _detector;
    private readonly int _maxTentativas;
    private readonly bool _verificarAssinatura;
    private int _tentativaActual;
    private bool _verificacaoConcluida;
    private bool _aguardarTecla;
    private string? _portaCom;
    private DispatcherTimer? _relogio;

    private static readonly Brush CorVerde = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
    private static readonly Brush CorVermelha = new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));

    public MainWindow()
    {
        // Antes do InitializeComponent: o XAML marca o MenuTestes com IsChecked="True",
        // o que dispara MenuItem_Checked ainda dentro da construção da janela. O que
        // esse handler precisar tem de já cá estar.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _maxTentativas = config.GetValue<int>("MaxTentativas");
        if (_maxTentativas <= 0)
            _maxTentativas = 3;

        // A assinatura é a opção 1 do menu do firmware (Prog_Tester V1.2). Equipamentos
        // com firmware antigo não respondem — daí a possibilidade de desligar a verificação.
        _verificarAssinatura = config.GetValue("VerificarAssinatura", true);

        // A construção dos serviços fica toda aqui: daqui para baixo esta janela só
        // conhece interfaces.
        _servicos = ServiceFactory.APartirDe(config);
        _detector = _servicos.CriarDetector();

        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await ExecutarVerificacaoAsync();
    }

    private async void BtnVerificar_Click(object sender, RoutedEventArgs e)
    {
        await ExecutarVerificacaoAsync();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_aguardarTecla)
            Application.Current.Shutdown();
    }

    private bool _verificacaoEmCurso;

    private async Task ExecutarVerificacaoAsync()
    {
        if (_verificacaoConcluida || _aguardarTecla || _verificacaoEmCurso)
            return;

        _verificacaoEmCurso = true;
        BtnVerificar.IsEnabled = false;
        _tentativaActual++;

        TxtMensagem.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
        TxtMensagem.Text = "A verificar dispositivos...";

        var dispositivos = await _detector.DetectarAsync();

        ActualizarEstadoCH340(dispositivos.PortaCH340);
        ActualizarEstadoUSBAsp(dispositivos.UsbAspLigado);

        var signatureValida = await VerificarAssinaturaAsync(dispositivos.PortaCH340);

        if (dispositivos.Completa && signatureValida)
        {
            _verificacaoConcluida = true;
            _portaCom = dispositivos.PortaCH340;
            MostrarPainelPrincipal();
            _verificacaoEmCurso = false;
            return;
        }

        if (_tentativaActual >= _maxTentativas)
        {
            _aguardarTecla = true;
            TxtMensagem.Foreground = CorVermelha;
            TxtMensagem.Text = "Não foi possível detectar todos os dispositivos após várias tentativas.\n" +
                               "Por favor, contacte a assistência técnica.";
            TxtTentativas.Text = "";
            TxtRodape.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
            TxtRodape.Text = "Pressione qualquer tecla para terminar a aplicação.";
            Focus();
            _verificacaoEmCurso = false;
            return;
        }

        var tentativasRestantes = _maxTentativas - _tentativaActual;
        TxtTentativas.Text = $"Tentativa {_tentativaActual} de {_maxTentativas}";
        TxtMensagem.Foreground = new SolidColorBrush(Color.FromRgb(0xFA, 0xB3, 0x87));
        TxtMensagem.Text = "Dispositivo(s) em falta. Por favor, ligue o(s) dispositivo(s) e pressione o botão novamente.";
        BtnVerificar.Content = $"Tentar Novamente ({tentativasRestantes} restante{(tentativasRestantes != 1 ? "s" : "")})";
        BtnVerificar.IsEnabled = true;
        _verificacaoEmCurso = false;
    }

    private void MostrarPainelPrincipal()
    {
        PanelVerificacao.Visibility = Visibility.Collapsed;
        PanelPrincipal.Visibility = Visibility.Visible;

        ActualizarRelogio();
        _relogio = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _relogio.Tick += (_, _) => ActualizarRelogio();
        _relogio.Start();

        NavegarPara("Testes");
    }

    private void ActualizarRelogio()
    {
        TxtHeaderDataHora.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
    }

    /// <summary>
    /// Abre o módulo de alta tensão a partir de outro ecrã. Marcar o item do menu
    /// (em vez de chamar NavegarPara directamente) mantém o menu lateral em
    /// sincronia com o que está a ser mostrado.
    /// </summary>
    public void AbrirProgramacaoAltaTensao()
    {
        MenuProgramacao.IsChecked = true;
    }

    private void MenuItem_Checked(object sender, RoutedEventArgs e)
    {
        if (ConteudoPrincipal is null)
            return;

        // O IsChecked="True" do MenuTestes dispara isto durante o InitializeComponent,
        // com o ecrã de verificação ainda por correr: sem porta série detectada, a vista
        // que daí saísse ficava agarrada a um barramento que não existe. Quem manda
        // navegar para os testes é o MostrarPainelPrincipal, depois da verificação passar.
        if (!_verificacaoConcluida)
            return;

        if (sender == MenuTestes)
            NavegarPara("Testes");
        else if (sender == MenuProgramacao)
            NavegarPara("Programacao");
        else if (sender == MenuConfiguracoes)
            NavegarPara("Configuracoes");
    }

    private void NavegarPara(string modulo)
    {
        var busManager = _servicos.CriarBusManager(_portaCom ?? "");
        var usbAspService = _servicos.CriarUsbAspService();

        ConteudoPrincipal.Content = modulo switch
        {
            "Testes" => new TesteFuncionalidadesView(busManager, usbAspService),
            "Programacao" => new UnderConstructionView("Programação de Alta Tensão"),
            "Configuracoes" => new UnderConstructionView("Configurações"),
            _ => null
        };
    }

    private void ActualizarEstadoCH340(string? porta)
    {
        if (porta is not null)
        {
            LedCH340.Fill = CorVerde;
            TxtCH340Detail.Text = $"Conectado na porta {porta}";
        }
        else
        {
            LedCH340.Fill = CorVermelha;
            TxtCH340Detail.Text = "Não detectado — Ligue o conversor USB-Serial CH340";
        }
    }

    private void ActualizarEstadoUSBAsp(bool encontrado)
    {
        if (encontrado)
        {
            LedUSBAsp.Fill = CorVerde;
            TxtUSBaspDetail.Text = "Conectado";
        }
        else
        {
            LedUSBAsp.Fill = CorVermelha;
            TxtUSBaspDetail.Text = "Não detectado — Ligue o programador USBAsp";
        }
    }

    /// <summary>
    /// Pede a assinatura ao equipamento (opção 1 do menu) e actualiza o LED respectivo.
    /// </summary>
    private async Task<bool> VerificarAssinaturaAsync(string? porta)
    {
        if (!_verificarAssinatura)
        {
            ActualizarEstadoSignatureIgnorada();
            return true;
        }

        if (porta is null)
        {
            ActualizarEstadoSignature(null, null);
            return false;
        }

        TxtMensagem.Text = "A verificar a assinatura do equipamento...";

        var resultado = await _servicos.CriarBusManager(porta).VerificarAssinaturaAsync();
        ActualizarEstadoSignature(porta, resultado);
        return resultado.Valida;
    }

    private void ActualizarEstadoSignature(string? porta, AssinaturaResult? resultado)
    {
        if (porta is null || resultado is null)
        {
            LedSignature.Fill = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
            TxtSignatureDetail.Text = "A aguardar detecção do CH340...";
            return;
        }

        if (resultado.Valida)
        {
            LedSignature.Fill = CorVerde;
            TxtSignatureDetail.Text = $"Equipamento identificado: {resultado.Assinatura} na porta {porta}";
        }
        else
        {
            LedSignature.Fill = CorVermelha;
            TxtSignatureDetail.Text = resultado.Resposta is { Length: > 0 } resposta
                ? $"Assinatura inesperada em {porta} — recebido: {PrimeiraLinha(resposta)}"
                : $"Sem resposta do equipamento em {porta} — Verifique a ligação";
        }
    }

    private static string PrimeiraLinha(string texto) =>
        texto.Split('\n')[0].Trim();

    private void ActualizarEstadoSignatureIgnorada()
    {
        LedSignature.Fill = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
        TxtSignatureDetail.Text = "Verificação de assinatura ignorada";
    }
}
