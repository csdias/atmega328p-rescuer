using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ATMegaPestaV1.Services;
using Microsoft.Win32;

namespace ATMegaPestaV1.Views;

public partial class TesteFuncionalidadesView : UserControl
{
    private static Brush BrushTema(string key) =>
        (Brush?)Application.Current.Resources[key] ?? Brushes.Transparent;

    private static Brush CorVerde    => BrushTema("BrushPinSucesso");
    private static Brush CorVermelha => BrushTema("BrushPinErro");
    private static Brush CorAmarela  => BrushTema("BrushPinAviso");
    private static Brush CorCinza    => BrushTema("BrushPinInactivo");

    private readonly IBusManager _busManager;
    private readonly IUsbAspService _usbAspService;

    /// <summary>Falhas de leitura por ISP toleradas antes de propor a alta tensão.</summary>
    private const int MaxTentativasLeitura = 3;

    private int _tentativasLeitura;

    /// <summary>
    /// A leitura das configurações passou por inteiro: chip identificado <em>e</em> fuses
    /// descodificados. É o que destranca o passo seguinte — daí não bastar o valor de
    /// retorno do <see cref="TentarLerAsync"/>, que dá true também quando o chip responde
    /// mas os fuses não saem.
    /// </summary>
    private bool _configuracoesLidas;

    /// <summary>
    /// O que a última leitura completa identificou. Serve à ficha da cópia de segurança,
    /// que tem de dizer a que chip é que a cópia pertence.
    /// </summary>
    private McuInfo? _mcuLido;

    private string? _assinaturaLida;

    /// <summary>
    /// Texto de repouso do cartão da verificação de integridade, tal como está no XAML.
    /// Guardado à construção para o cartão poder voltar a ele sem o duplicar aqui.
    /// </summary>
    private readonly string _descricaoVerificacaoIntegridade;

    private record TesteItem(
        string Nome,
        TextBlock Icone,
        TextBlock Tempo,
        Border Badge,
        string[] Pinos,
        Border? Tag = null);

    private record ResultadoTeste(string Nome, bool Passou, long TempoMs);

    private List<TesteItem> _testes = [];
    private readonly List<ResultadoTeste> _resultados = [];

    private static readonly Dictionary<string, (string Titulo, string Conteudo)> Ajudas = new()
    {
        ["GpioEscrita"] = (
            "GPIO Escrita",
            "A escrita GPIO consiste em configurar um pino do ATmega328P como saída (DDRx = 1) " +
            "e definir o seu estado lógico através do registo PORTx.\n\n" +
            "O teste verifica:\n" +
            "• O pino comuta correctamente entre HIGH (~5V) e LOW (~0V)\n" +
            "• A tensão de saída está dentro dos limites esperados\n" +
            "• Não existem curto-circuitos entre pinos adjacentes\n" +
            "• A corrente de saída está dentro das especificações\n\n" +
            "O tempo apresentado corresponde à duração total do teste, incluindo " +
            "a comunicação série e a validação dos resultados."
        ),
        ["GpioLeitura"] = (
            "GPIO Leitura",
            "A leitura GPIO consiste em configurar um pino do ATmega328P como entrada " +
            "(DDRx = 0) e ler o seu estado lógico através do registo PINx.\n\n" +
            "Como se realiza o teste:\n" +
            "1. O USBAsp carrega o firmware de teste no ATmega328P via ISP\n" +
            "2. O firmware configura os pinos como entradas\n" +
            "3. Aplica-se um estado conhecido externamente ao pino\n" +
            "4. O microcontrolador lê o registo PINx e reporta via UART\n" +
            "5. O CH340 envia os resultados para o PC\n\n" +
            "O que se verifica:\n" +
            "• Continuidade eléctrica\n" +
            "• Funcionalidade do registo PINx\n" +
            "• Pull-ups internos\n" +
            "• Ausência de curto-circuitos\n" +
            "• Integridade da PCB"
        ),
        ["Spi"] = (
            "Verificação do SPI",
            "O SPI (Serial Peripheral Interface) é um protocolo de comunicação síncrono " +
            "que utiliza 4 linhas: MOSI, MISO, SCK e SS.\n\n" +
            "O teste verifica:\n" +
            "• Comunicação bidirecional entre o ATmega328P e um periférico SPI\n" +
            "• Integridade dos dados transmitidos e recebidos\n" +
            "• Velocidade de relógio (SCK) dentro dos parâmetros\n" +
            "• Selecção correcta do periférico via linha SS"
        ),
        ["I2c"] = (
            "Verificação do I²C",
            "O I²C (Inter-Integrated Circuit) é um protocolo de comunicação série " +
            "que utiliza 2 linhas: SDA e SCL.\n\n" +
            "O teste verifica:\n" +
            "• Scan de endereços — detecção de dispositivos no barramento\n" +
            "• Escrita e leitura de registos\n" +
            "• Condições de START, STOP e ACK/NACK\n" +
            "• Resistências de pull-up nas linhas SDA e SCL"
        ),
        ["Adc"] = (
            "Verificação do ADC",
            "O ADC (Analogue-to-Digital Converter) do ATmega328P converte sinais " +
            "analógicos (0V a 5V) em valores digitais de 10 bits (0 a 1023).\n\n" +
            "O teste verifica:\n" +
            "• Leitura de tensão de referência conhecida\n" +
            "• Linearidade dos valores\n" +
            "• Ruído entre leituras consecutivas\n" +
            "• Selecção correcta do canal ADC\n" +
            "• Tensão de referência interna"
        )
    };

    public TesteFuncionalidadesView(IBusManager busManager, IUsbAspService usbAspService)
    {
        InitializeComponent();

        _busManager = busManager;
        _usbAspService = usbAspService;
        _descricaoVerificacaoIntegridade = TxtVerificacaoIntegridade.Text;

        _testes =
        [
            new("UART",          IcoUart, TxtUartTempo, BadgeUart, ["D0","D1"], TagUart),
            new("GPIO Escrita",  IcoGpioEscrita, TxtGpioEscritaTempo, BadgeGpioEscrita,
                ["D0","D1","D2","D3","D4","D5","D6","D7","D8","D9","D10","D11","D12","D13"]),
            new("GPIO Leitura",  IcoGpioLeitura, TxtGpioLeituraTempo, BadgeGpioLeitura,
                ["D0","D1","D2","D3","D4","D5","D6","D7","D8","D9","D10","D11","D12","D13"]),
            new("SPI",           IcoSpi,  TxtSpiTempo,  BadgeSpi,  ["D10","D11","D12","D13"], TagSpi),
            new("I²C",           IcoI2c,  TxtI2cTempo,  BadgeI2c,  ["A4","A5"],               TagI2c),
            new("ADC",           IcoAdc,  TxtAdcTempo,  BadgeAdc,  ["A0","A1","A2","A3","A4","A5"]),
            new("PWM",           IcoPwm,  TxtPwmTempo,  BadgePwm,  ["D3","D5","D6","D9","D10","D11"], TagPwm),
        ];
    }

    private void BtnIniciarVerificacao_Click(object sender, RoutedEventArgs e)
    {
        PanelIniciarVerificacao.Visibility = Visibility.Collapsed;
        PanelInserirChip.Visibility = Visibility.Visible;
        IniciarAnimacaoSeta();
    }

    private void BtnInserirChipCancelar_Click(object sender, RoutedEventArgs e)
    {
        PanelInserirChip.Visibility = Visibility.Collapsed;
        PanelIniciarVerificacao.Visibility = Visibility.Visible;
    }

    private async void BtnInserirChipConfirmar_Click(object sender, RoutedEventArgs e)
    {
        PanelInserirChip.Visibility = Visibility.Collapsed;
        PanelMaster.Visibility = Visibility.Visible;
        await CarregarConfiguracoesAsync();
    }

    private void IniciarAnimacaoSeta()
    {
        var anim = new DoubleAnimation(0, 8, TimeSpan.FromSeconds(0.7))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        ((TranslateTransform)ArrowIndicator.RenderTransform).BeginAnimation(TranslateTransform.XProperty, anim);
        var fade = new DoubleAnimation(1, 0.2, TimeSpan.FromSeconds(0.7))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        ArrowIndicator.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    // ── Configurações atuais ────────────────────────────────────────────────

    private async void BtnDetectarConfig_Click(object sender, RoutedEventArgs e)
    {
        await CarregarConfiguracoesAsync();
    }

    /// <summary>
    /// Orquestra uma tentativa de leitura e o que se segue: ao fim de
    /// <see cref="MaxTentativasLeitura"/> falhas seguidas, o ISP está esgotado como
    /// via de acesso e a única saída é a programação de alta tensão.
    /// </summary>
    private async Task CarregarConfiguracoesAsync()
    {
        var identificado = await TentarLerAsync();

        // O passo seguinte abre-se com a leitura inteira, não só com o chip
        // identificado: verificar os pinos sem saber em que estado estão os fuses
        // é medir sem saber contra o quê.
        if (_configuracoesLidas)
            MostrarProximoPasso();

        if (identificado)
        {
            _tentativasLeitura = 0;
            return;
        }

        _tentativasLeitura++;
        ActualizarPainelFalha();

        if (_tentativasLeitura >= MaxTentativasLeitura)
            EscalarParaAltaTensao();
    }

    /// <summary>
    /// Comuta o barramento para o USBAsp e lê a identificação e os fuses do
    /// chip-alvo. Estritamente leitura — nada nesta app grava fuses.
    /// No fim isola sempre o barramento: não se deixa o ISP ligado ao alvo.
    /// Devolve true apenas se o chip foi identificado.
    /// </summary>
    private async Task<bool> TentarLerAsync()
    {
        BtnDetectarConfig.IsEnabled = false;
        BtnTentarNovamente.IsEnabled = false;
        LimparConfiguracoes();

        // Uma leitura nova invalida a anterior: o passo seguinte volta a fechar-se até
        // esta passar. Sem isto, quem trocasse a peça no ZIF continuava a ver o botão
        // de verificar destrancado pela leitura de outro chip.
        _configuracoesLidas = false;
        _mcuLido = null;
        _assinaturaLida = null;
        EsconderProximoPasso();

        var barramentoActivo = false;
        var leituraOk = false;

        try
        {
            // 1a — encaminhar o barramento ISP para o USBAsp (opção 2 do menu).
            SetConfigEstado("A activar o USBAsp no barramento...", CorAmarela);
            var resBus = await _busManager.SelectUsbAspAsync();

            if (resBus is not null && resBus.StartsWith("Erro:"))
            {
                SetConfigEstado(resBus, CorVermelha);
                SetMcuEstado("Sem barramento", "BadgeFail");
                MostrarFalhaLeitura();
                return false;
            }

            barramentoActivo = true;

            // 1b — identificar o chip-alvo via ISP.
            SetConfigEstado("Barramento no USBAsp — a ler o chip-alvo...", CorAmarela);
            var detect = await _usbAspService.DetectarAssinaturaAsync();
            AppendLog(detect.Output);

            if (!detect.Success)
            {
                // A saída crua do avrdude não entra aqui: para quem está à bancada é
                // ruído, e o painel de falha já diz o que fazer. Continua a ir para o
                // log via AppendLog, acima.
                SetConfigEstado("O chip-alvo não respondeu ao ISP.", CorVermelha);
                SetMcuEstado("Sem resposta", "BadgeFail");
                MostrarFalhaLeitura();
                return false;
            }

            MostrarDadosLeitura();
            leituraOk = true;

            var mcu = FuseDecoder.Identificar(detect.Dispositivo);
            TxtMcuNome.Text       = mcu.Nome;
            TxtMcuAssinatura.Text = detect.Assinatura ?? "—";
            TxtMcuFlash.Text      = FusesDecodificados.FormatarBytes(mcu.FlashBytes);
            TxtMcuEeprom.Text     = FusesDecodificados.FormatarBytes(mcu.EepromBytes);
            TxtMcuSram.Text       = FusesDecodificados.FormatarBytes(mcu.SramBytes);
            SetMcuEstado("Detetado", "BadgePass");

            // 1c — ler e descodificar os fuses.
            var fuses = await _usbAspService.LerFusesAsync();
            var decodificado = FuseDecoder.Descodificar(fuses, mcu);

            if (decodificado is null)
            {
                // O chip respondeu — a via de acesso não está em causa, por isso
                // isto não conta como falha de leitura nem consome tentativas.
                SetConfigEstado($"{mcu.Nome} detetado, mas a leitura dos fuses falhou.", CorAmarela);
                MostrarSaidaAvrdude(fuses.Output);
                return true;
            }

            PreencherFuses(fuses, decodificado, mcu);
            _configuracoesLidas = true;
            _mcuLido = mcu;
            _assinaturaLida = detect.Assinatura;

            // Resumo denso: é a única coisa visível com o acordeão fechado.
            SetConfigEstado(
                $"{mcu.Nome} · {detect.Assinatura} · " +
                $"L {fuses.Low}  H {fuses.High}  E {fuses.Extended}  LB {fuses.Lock ?? "n/d"}",
                CorVerde);
        }
        finally
        {
            // A leitura acabou — corta-se a ligação ao alvo. O barramento volta a
            // Hi-Z (opção 4 do menu), que é o estado de arranque do firmware.
            // Corre mesmo quando algo falha a meio, para não deixar o ISP ligado.
            if (barramentoActivo && !await IsolarBarramentoAsync())
            {
                TxtConfigEstado.Text += "  ·  ATENÇÃO: falha ao isolar o barramento";
                LedConfig.Fill = CorAmarela;
                TxtConfigEstado.Foreground = CorAmarela;
            }

            BtnDetectarConfig.IsEnabled = true;
            BtnTentarNovamente.IsEnabled = true;
        }

        return leituraOk;
    }

    // ── Falha de leitura: contagem, painel e escalada ───────────────────────

    private void MostrarFalhaLeitura()
    {
        PainelDadosLeitura.Visibility = Visibility.Collapsed;
        PainelFalhaLeitura.Visibility = Visibility.Visible;

        // O ecrã de falha tem um assunto só. A saída do avrdude fica reservada para
        // as falhas parciais, onde é a única pista do que correu mal.
        PainelConfigErro.Visibility = Visibility.Collapsed;

        // O painel traz o seu próprio botão; dois "Detetar" no mesmo cartão só
        // dividiriam a atenção.
        BtnDetectarConfig.Visibility = Visibility.Collapsed;

        // De pouco serve a mensagem dentro de um acordeão fechado.
        ToggleConfigDetalhes.IsChecked = true;
    }

    private void MostrarDadosLeitura()
    {
        PainelFalhaLeitura.Visibility = Visibility.Collapsed;
        PainelDadosLeitura.Visibility = Visibility.Visible;
        BtnDetectarConfig.Visibility = Visibility.Visible;
    }

    private void ActualizarPainelFalha()
    {
        var esgotou = _tentativasLeitura >= MaxTentativasLeitura;

        TxtFalhaInstrucao.Text = esgotou
            ? "O chip-alvo continuou sem responder ao ISP nas três tentativas."
            : "Verifique se o microcontrolador está corretamente inserido no ZIF socket — " +
              "alavanca baixada e pino 1 no canto marcado — e tente detetar novamente.";

        TxtFalhaTentativas.Text = esgotou
            ? $"{MaxTentativasLeitura} de {MaxTentativasLeitura} tentativas sem sucesso."
            : $"Tentativa {_tentativasLeitura} de {MaxTentativasLeitura}";

        BtnTentarNovamente.IsEnabled = !esgotou;
    }

    /// <summary>
    /// Esgotadas as tentativas por ISP, propõe a programação de alta tensão.
    /// Corre depois de o barramento já ter sido isolado — não se abre um diálogo
    /// modal com o ISP ainda ligado ao alvo.
    /// </summary>
    private void EscalarParaAltaTensao()
    {
        var janela = Window.GetWindow(this);

        if (new AltaTensaoDialog { Owner = janela }.ShowDialog() == true)
        {
            (janela as MainWindow)?.AbrirProgramacaoAltaTensao();
            return;
        }

        if (new EncerramentoDialog { Owner = janela }.ShowDialog() == true)
        {
            Application.Current.Shutdown();
            return;
        }

        // Cancelou a saída. Devolver-lhe um ciclo novo evita deixá-lo num ecrã
        // onde nenhum botão faz nada.
        _tentativasLeitura = 0;
        ActualizarPainelFalha();
        TxtFalhaTentativas.Text = "Pode tentar novamente ou navegar pelo menu lateral.";
    }

    private async void BtnTentarNovamente_Click(object sender, RoutedEventArgs e)
    {
        await CarregarConfiguracoesAsync();
    }

    /// <summary>
    /// Devolve o barramento a Hi-Z. Isolar é arrumação de rotina: quando corre bem não se
    /// anuncia. Quando falha devolve false — deixar o alvo ligado sem o utilizador saber
    /// seria pior que o erro — e quem chamou avisa no seu próprio cartão.
    /// </summary>
    private async Task<bool> IsolarBarramentoAsync()
    {
        var resIsolar = await _busManager.IsolarBarramentoAsync();
        return resIsolar is null || !resIsolar.StartsWith("Erro:");
    }

    private void SetConfigEstado(string texto, Brush cor)
    {
        LedConfig.Fill = cor;
        TxtConfigEstado.Text = texto;
        TxtConfigEstado.Foreground = cor;
    }

    private void SetMcuEstado(string texto, string estiloBadge)
    {
        TxtMcuEstado.Text = texto;
        BadgeMcuEstado.Style = (Style)FindResource(estiloBadge);
    }

    private void MostrarSaidaAvrdude(string texto)
    {
        TxtConfigSaida.Text = texto.Length > 600 ? texto[^600..] : texto;
        PainelConfigErro.Visibility = Visibility.Visible;

        // De pouco serve mostrar o erro dentro de um acordeão fechado.
        ToggleConfigDetalhes.IsChecked = true;
    }

    private void LimparConfiguracoes()
    {
        PainelConfigErro.Visibility = Visibility.Collapsed;

        TxtMcuNome.Text       = "—";
        TxtMcuAssinatura.Text = "—";
        TxtMcuFlash.Text      = "—";
        TxtMcuEeprom.Text     = "—";
        TxtMcuSram.Text       = "—";
        SetMcuEstado("A aguardar", "BadgePendente");

        TxtFlashTotal.Text     = "—";
        TxtFlashAppBytes.Text  = "—";
        TxtFlashAppPct.Text    = "—";
        TxtFlashBootBytes.Text = "—";
        TxtFlashBootPct.Text   = "—";
        TxtLegendaBoot.Text    = "Bootloader reservado";
        PainelLegendaBoot.Visibility = Visibility.Visible;
        TxtFlashBootBytes.Visibility = Visibility.Visible;
        TxtFlashBootPct.Visibility   = Visibility.Visible;
        ChipFlashApp.Background = CorCinza;

        foreach (var t in new[] { TxtFuseL, TxtFuseH, TxtFuseE, TxtFuseLB,
                                  TxtFuseLDesc, TxtFuseHDesc, TxtFuseEDesc, TxtFuseLBDesc })
            t.Text = "—";

        ColFlashApp.Width  = new GridLength(1, GridUnitType.Star);
        ColFlashBoot.Width = new GridLength(0, GridUnitType.Star);
        BarraFlashApp.Background = CorCinza;

        foreach (var led in new[] { LedIndIsp, LedIndReset, LedIndClock,
                                    LedIndBod, LedIndBootloader, LedIndEeprom })
            led.Fill = CorCinza;

        // Os rótulos do XAML ("ISP habilitado", "Pino RESET activo", ...) são só o
        // desenho do painel. Antes de haver leitura não afirmam nada sobre a peça.
        foreach (var txt in new[] { TxtIndIsp, TxtIndReset, TxtIndClock,
                                    TxtIndBod, TxtIndBootloader, TxtIndEeprom })
            txt.Text = "—";
    }

    private void PreencherFuses(FusesResult fuses, FusesDecodificados dec, McuInfo mcu)
    {
        TxtFuseL.Text  = fuses.Low ?? "—";
        TxtFuseH.Text  = fuses.High ?? "—";
        TxtFuseE.Text  = fuses.Extended ?? "—";
        TxtFuseLB.Text = fuses.Lock ?? "n/d";

        TxtFuseLDesc.Text  = dec.DescricaoLow;
        TxtFuseHDesc.Text  = dec.DescricaoHigh;
        TxtFuseEDesc.Text  = dec.DescricaoExtended;
        TxtFuseLBDesc.Text = dec.DescricaoLock;

        PreencherMapaFlash(dec, mcu);

        SetIndicador(LedIndIsp, TxtIndIsp, dec.SpiActivo,
            "ISP habilitado (SPIEN)", "ISP desactivado (SPIEN)");

        SetIndicador(LedIndReset, TxtIndReset, dec.ResetActivo,
            "Pino RESET activo", "RESET desactivado (RSTDISBL)");

        SetIndicador(LedIndClock, TxtIndClock, !dec.Ckdiv8Activo,
            "Clock a full speed", "CKDIV8 activo — clock ÷8");

        SetIndicador(LedIndBod, TxtIndBod, dec.BrownOut != "desactivado",
            $"Brown-out a {dec.BrownOut}", "Brown-out desactivado");

        // Bootloader e EEPROM são escolhas de projecto, não defeitos: quando estão
        // desligados o indicador fica cinzento, não amarelo.
        SetIndicador(LedIndBootloader, TxtIndBootloader, dec.BootRstActivo,
            $"Bootloader de {FusesDecodificados.FormatarBytes(dec.BootloaderBytes)}",
            "Sem bootloader", neutroQuandoFalso: true);

        SetIndicador(LedIndEeprom, TxtIndEeprom, dec.EepromPreservada,
            "EEPROM preservada em erase", "EEPROM apagada em chip erase",
            neutroQuandoFalso: true);
    }

    /// <summary>
    /// Desenha a barra do mapa da Flash e a respectiva legenda.
    /// A fatia verde é o que <em>sobra</em> para a aplicação depois de reservado o
    /// bootloader — capacidade, não ocupação: o conteúdo da Flash não é lido.
    /// </summary>
    private void PreencherMapaFlash(FusesDecodificados dec, McuInfo mcu)
    {
        var boot = dec.BootRstActivo ? dec.BootloaderBytes : 0;
        var app = dec.FlashAplicacao;

        ColFlashApp.Width  = new GridLength(app, GridUnitType.Star);
        ColFlashBoot.Width = new GridLength(boot, GridUnitType.Star);
        BarraFlashApp.Background = CorVerde;
        ChipFlashApp.Background  = CorVerde;

        // A percentagem do bootloader é o complemento da da aplicação: arredondar as
        // duas em separado dava 93,8 % + 6,3 % = 100,1 %, que salta à vista.
        var pctApp = mcu.FlashBytes > 0
            ? Math.Round(100.0 * app / mcu.FlashBytes, 1)
            : 0;

        TxtFlashTotal.Text     = $"total {mcu.FlashBytes} B";
        TxtFlashAppBytes.Text  = $"{app} B";
        TxtFlashAppPct.Text    = $"{pctApp:0.0} %";

        if (boot > 0)
        {
            PainelLegendaBoot.Visibility = Visibility.Visible;
            TxtFlashBootBytes.Visibility = Visibility.Visible;
            TxtFlashBootPct.Visibility   = Visibility.Visible;
            TxtLegendaBoot.Text          = $"Bootloader reservado ({FusesDecodificados.FormatarBytes(boot)})";
            TxtFlashBootBytes.Text       = $"{boot} B";
            TxtFlashBootPct.Text         = $"{100 - pctApp:0.0} %";
        }
        else
        {
            // Sem BOOTRST não há fatia laranja na barra — a legenda dela seria ruído.
            PainelLegendaBoot.Visibility = Visibility.Collapsed;
            TxtFlashBootBytes.Visibility = Visibility.Collapsed;
            TxtFlashBootPct.Visibility   = Visibility.Collapsed;
        }
    }

    private void SetIndicador(Ellipse led, TextBlock txt, bool activo,
                              string textoActivo, string textoInactivo,
                              bool neutroQuandoFalso = false)
    {
        txt.Text = activo ? textoActivo : textoInactivo;

        if (activo)
            led.Fill = CorVerde;
        else
            led.Fill = neutroQuandoFalso ? CorCinza : CorAmarela;
    }

    // ── Passo seguinte — Verificação de Integridade ─────────────────────────

    /// <summary>
    /// Destranca o passo seguinte. Chamado só com a leitura das configurações
    /// completa: é ela que diz que peça está no ZIF e em que estado.
    /// </summary>
    private void MostrarProximoPasso()
    {
        CartaoVerificacaoIntegridade.Visibility = Visibility.Visible;
    }

    private void EsconderProximoPasso()
    {
        CartaoVerificacaoIntegridade.Visibility = Visibility.Collapsed;

        // O painel direito acompanha o cartão: sem passo seguinte não há resultados
        // para mostrar, e a coluna volta a não ocupar espaço.
        ColunaVerificacaoIntegridade.Width = new GridLength(0);
        PainelResultadosVerificacaoIntegridade.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Activa o ATmega2560 no barramento e corre a verificação de integridade — o que eram os
    /// passos 2 e 3. Comutar o barramento não vale por si: existe para o master poder
    /// exercitar os pinos do alvo, e por isso é o mesmo gesto.
    /// </summary>
    private async void BtnVerificacaoIntegridade_Click(object sender, RoutedEventArgs e)
    {
        // O aviso vem antes de se tocar no barramento: a verificação não é uma leitura,
        // e quem carrega em "Iniciar" ainda não sabe que vai perder o que tem no chip.
        var aviso = new TransferenciaVerificadorDialog { Owner = Window.GetWindow(this) };
        aviso.ShowDialog();

        if (aviso.Escolha == EscolhaTransferencia.Cancelar)
            return;

        BtnVerificacaoIntegridade.IsEnabled = false;

        // As configurações já foram lidas e o assunto passou a ser outro: fechar o
        // acordeão dá o ecrã aos resultados em vez de o dividir entre dois passos.
        // O resumo denso continua visível no cabeçalho do cartão.
        ToggleConfigDetalhes.IsChecked = false;

        try
        {
            // Cópia pedida e não obtida trava a verificação: prosseguir seria escrever
            // sobre o chip exactamente depois de o utilizador dizer que queria guardá-lo.
            if (aviso.Escolha == EscolhaTransferencia.ProsseguirComCopia &&
                !await GuardarCopiaChipAsync())
                return;

            SetVerificacaoIntegridadeEstado("A comutar o barramento para o ATmega2560...", CorAmarela);
            var resMega = await _busManager.SwitchToMegaAsync();

            if (resMega is not null && resMega.StartsWith("Erro:"))
            {
                SetVerificacaoIntegridadeEstado(resMega, CorVermelha);
                return;
            }

            MostrarPainelVerificacaoIntegridade();
            await ExecutarTestes(_testes);

            // O cartão volta ao seu texto de repouso: é o painel da direita que reporta
            // o que aconteceu, e o barramento é isolado a seguir — não há estado deste
            // passo que sobreviva para anunciar.
            RepousarVerificacaoIntegridade();
        }
        finally
        {
            // Nada foi medido, logo não há razão para deixar o master a conduzir o
            // barramento: devolve-se a Hi-Z, o estado de arranque do firmware. Quando
            // os testes existirem, este isolamento passa para depois deles.
            if (!await IsolarBarramentoAsync())
                SetVerificacaoIntegridadeEstado(
                    TxtVerificacaoIntegridade.Text + "  ·  ATENÇÃO: falha ao isolar o barramento",
                    CorAmarela);

            BtnVerificacaoIntegridade.IsEnabled = true;
        }
    }

    private void SetVerificacaoIntegridadeEstado(string texto, Brush cor)
    {
        LedVerificacaoIntegridade.Fill = cor;
        TxtVerificacaoIntegridade.Text = texto;
        TxtVerificacaoIntegridade.Foreground = cor;
    }

    /// <summary>
    /// Pede a pasta e guarda a Flash e a EEPROM do chip-alvo em Intel HEX. Encaminha o
    /// barramento para o USBAsp e volta a isolá-lo, como qualquer acesso ISP desta app.
    /// Devolve false se o utilizador desistir da pasta ou se a cópia não sair inteira.
    /// </summary>
    private async Task<bool> GuardarCopiaChipAsync()
    {
        var escolhaPasta = new OpenFolderDialog
        {
            Title = "Onde guardar a cópia do microcontrolador"
        };

        if (escolhaPasta.ShowDialog() != true)
        {
            SetVerificacaoIntegridadeEstado(
                "Cópia cancelada — a verificação não avançou e o chip fica como está.",
                CorAmarela);
            return false;
        }

        // Carimbo na hora: quem traz várias peças à bancada não fica com cópias a
        // sobrepor-se umas às outras sem se perceber qual é qual.
        var carimbo = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // System.IO.Path por extenso: o System.Windows.Shapes.Path deste ficheiro
        // (o Ellipse dos LEDs vem de lá) torna o nome curto ambíguo.
        var flash  = System.IO.Path.Combine(escolhaPasta.FolderName, $"ATmega328P_{carimbo}_flash.hex");
        var eeprom = System.IO.Path.Combine(escolhaPasta.FolderName, $"ATmega328P_{carimbo}_eeprom.hex");
        var ficha  = System.IO.Path.Combine(escolhaPasta.FolderName, $"ATmega328P_{carimbo}_fuses.txt");

        SetVerificacaoIntegridadeEstado(
            "A guardar a Flash, a EEPROM e os fuses do chip-alvo...", CorAmarela);

        var barramentoActivo = false;

        try
        {
            var resBus = await _busManager.SelectUsbAspAsync();

            if (resBus is not null && resBus.StartsWith("Erro:"))
            {
                SetVerificacaoIntegridadeEstado(resBus, CorVermelha);
                return false;
            }

            barramentoActivo = true;

            var copia = await _usbAspService.GuardarCopiaAsync(flash, eeprom);
            AppendLog(copia.Output);

            if (!copia.Success)
            {
                SetVerificacaoIntegridadeEstado(
                    "A cópia falhou — a verificação não avançou e o chip fica como está.",
                    CorVermelha);
                MostrarSaidaAvrdude(copia.Output);
                return false;
            }

            // A ficha dos fuses é escrita aqui, e não no serviço: os bytes vêm do
            // avrdude, mas o que eles significam é o FuseDecoder que sabe.
            try
            {
                EscreverFichaFuses(ficha, copia);
            }
            catch (Exception ex)
            {
                SetVerificacaoIntegridadeEstado(
                    $"As memórias foram guardadas mas a ficha dos fuses falhou: {ex.Message}",
                    CorVermelha);
                return false;
            }

            var f = copia.Fuses;
            SetVerificacaoIntegridadeEstado(
                $"Cópia guardada em {escolhaPasta.FolderName} — Flash, EEPROM e fuses " +
                $"(L {f.Low}  H {f.High}  E {f.Extended}  LB {f.Lock ?? "n/d"})",
                CorVerde);
            return true;
        }
        finally
        {
            if (barramentoActivo && !await IsolarBarramentoAsync())
                SetVerificacaoIntegridadeEstado(
                    TxtVerificacaoIntegridade.Text + "  ·  ATENÇÃO: falha ao isolar o barramento",
                    CorAmarela);
        }
    }

    /// <summary>
    /// Escreve a ficha da cópia: os fuse bits em hexadecimal, o que eles significam, e as
    /// memórias que ficaram guardadas ao lado. Os quatro bytes por si só não dizem nada a
    /// quem abrir a pasta meses depois — a descodificação é o que torna a ficha útil.
    /// </summary>
    private void EscreverFichaFuses(string caminho, CopiaResult copia)
    {
        var mcu = _mcuLido ?? FuseDecoder.PorDefeito;
        var dec = FuseDecoder.Descodificar(copia.Fuses, mcu);
        var f = copia.Fuses;

        var sb = new StringBuilder();
        sb.AppendLine($"# Cópia de segurança do chip-alvo — {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("# Escrita pelo ATMegaPesta V1. Só leitura: esta aplicação nunca grava fuses.");
        sb.AppendLine();
        sb.AppendLine($"MCU              = {mcu.Nome}");
        sb.AppendLine($"Assinatura       = {_assinaturaLida ?? "n/d"}");
        sb.AppendLine();
        sb.AppendLine("# Fuse bits como estavam antes da transferência do verificador");
        sb.AppendLine($"lfuse            = {f.Low}");
        sb.AppendLine($"hfuse            = {f.High}");
        sb.AppendLine($"efuse            = {f.Extended}");
        sb.AppendLine($"lock             = {f.Lock ?? "n/d"}");

        if (dec is not null)
        {
            sb.AppendLine();
            sb.AppendLine("# O que estes bytes significam");
            sb.AppendLine($"Relógio          = {dec.Relogio}");
            sb.AppendLine($"CKDIV8           = {(dec.Ckdiv8Activo ? "activo — clock ÷8" : "desligado")}");
            sb.AppendLine($"Brown-out        = {dec.BrownOut}");
            sb.AppendLine($"ISP (SPIEN)      = {(dec.SpiActivo ? "habilitado" : "desactivado")}");
            sb.AppendLine($"RESET (RSTDISBL) = {(dec.ResetActivo ? "activo" : "desactivado")}");
            sb.AppendLine($"EEPROM em erase  = {(dec.EepromPreservada ? "preservada" : "apagada")}");
            sb.AppendLine($"Bootloader       = {dec.DescricaoHigh}");
            sb.AppendLine($"Bloqueio         = {dec.Bloqueio}");
        }

        sb.AppendLine();
        sb.AppendLine("# Memórias guardadas nesta pasta");
        foreach (var ficheiro in copia.Ficheiros)
            sb.AppendLine($"#   {System.IO.Path.GetFileName(ficheiro)}");

        sb.AppendLine();
        sb.AppendLine("# Reposição dos fuses — por sua conta, não por esta aplicação:");
        sb.AppendLine($"#   avrdude -c usbasp -p m328p " +
                      $"-U lfuse:w:{f.Low}:m -U hfuse:w:{f.High}:m -U efuse:w:{f.Extended}:m");
        sb.AppendLine("# Um valor errado aqui fecha o ISP e o chip só volta por alta tensão.");
        sb.AppendLine("# O lock byte não se repõe por escrita: só um chip erase o devolve a 0xFF.");

        File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>Devolve o cartão ao estado de repouso: a descrição e o LED inactivo.</summary>
    private void RepousarVerificacaoIntegridade()
    {
        LedVerificacaoIntegridade.Fill = CorCinza;
        TxtVerificacaoIntegridade.Text = _descricaoVerificacaoIntegridade;
        TxtVerificacaoIntegridade.Foreground = BrushTema("BrushTextoNormal");
    }

    /// <summary>
    /// Revela o painel da verificação de integridade. Só deve ser chamado quando a
    /// verificação de integridade arrancar — até lá a coluna tem largura zero.
    /// </summary>
    private void MostrarPainelVerificacaoIntegridade()
    {
        ColunaVerificacaoIntegridade.Width = new GridLength(360);
        PainelResultadosVerificacaoIntegridade.Visibility = Visibility.Visible;
    }

    private void AppendLog(string texto)
    {
        TxtUltimoResultado.Text = texto.Length > 200 ? texto[^200..] : texto;
    }


    private void BtnAjuda_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string chave && Ajudas.TryGetValue(chave, out var ajuda))
        {
            var dialog = new AjudaDialog(ajuda.Titulo, ajuda.Conteudo)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
    }

    private async void BtnIniciarTodos_Click(object sender, RoutedEventArgs e)
    {
        await ExecutarTestes(_testes);
    }

    private async void BtnRetestarFalhas_Click(object sender, RoutedEventArgs e)
    {
        var nomesFalhados = _resultados
            .Where(r => !r.Passou)
            .Select(r => r.Nome)
            .ToHashSet();

        var falhas = _testes.Where(t => nomesFalhados.Contains(t.Nome)).ToList();

        if (falhas.Count > 0)
            await ExecutarTestes(falhas);
    }

    // Sem execução física não há nada para interromper; o botão está escondido.
    private void BtnInterromper_Click(object sender, RoutedEventArgs e) { }

    private void BtnExportarLog_Click(object sender, RoutedEventArgs e)
    {
        if (_resultados.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Ficheiro de texto (*.txt)|*.txt|CSV (*.csv)|*.csv",
            FileName = $"TestLog_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog() != true)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"ATmega328P Pesta V1 — Relatório de Testes");
        sb.AppendLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine(new string('-', 50));
        sb.AppendLine();

        var passou = 0;
        var falhou = 0;

        foreach (var r in _resultados)
        {
            var estado = r.Passou ? "PASSOU" : "FALHOU";
            sb.AppendLine($"  {r.Nome,-25} {estado,-10} {r.TempoMs,6} ms");
            if (r.Passou) passou++;
            else falhou++;
        }

        sb.AppendLine();
        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"  Total: {_resultados.Count}   Passou: {passou}   Falhou: {falhou}");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

        TxtUltimoResultado.Text = $"Log exportado para:\n{dialog.FileName}";
    }

    /// <summary>
    /// A verificação de integridade não tem execução real. O firmware Prog_Tester V1.2 só expõe
    /// diagnósticos de Serial2 e de SPI — para GPIO, I²C, ADC e PWM não há comando nenhum,
    /// e sem medição não há resultado. O que aqui estava era um sorteio de PASS/FAIL,
    /// indistinguível à bancada de uma medição verdadeira; deixar os testes por executar é
    /// menos informação mas informação certa.
    /// </summary>
    private Task ExecutarTestes(List<TesteItem> testes)
    {
        _resultados.RemoveAll(r => testes.Any(t => t.Nome == r.Nome));

        foreach (var teste in testes)
        {
            teste.Icone.Text = "—";
            teste.Icone.Foreground = CorCinza;
            teste.Tempo.Text = "n/d";
            teste.Badge.Style = (Style)FindResource("BadgePendente");
        }

        TxtUltimoResultado.Text =
            "Verificação de integridade por implementar — o firmware não expõe estes testes. " +
            "Nada foi medido.";

        BtnIniciarTodos.IsEnabled = false;
        BtnRetestarFalhas.Visibility = Visibility.Collapsed;
        BtnExportarLog.IsEnabled = _resultados.Count > 0;
        BtnInterromper.Visibility = Visibility.Collapsed;

        ActualizarBarra();
        return Task.CompletedTask;
    }

    private void ActualizarBarra()
    {
        var total = _testes.Count;
        if (total == 0) return;

        var pct = _resultados.Count * 100 / total;
        TxtProgressoPct.Text = $"{pct}%";

        // Barra tem largura do pai — usa ActualWidth em runtime
        var larguraPai = ((Border)BarraProgresso.Parent).ActualWidth;
        BarraProgresso.Width = larguraPai * pct / 100;
    }

    private void SetPinColor(string nome, Brush cor)
    {
        var border = nome switch
        {
            "D0"  => PinD0,  "D1"  => PinD1,  "D2"  => PinD2,  "D3"  => PinD3,
            "D4"  => PinD4,  "D5"  => PinD5,  "D6"  => PinD6,  "D7"  => PinD7,
            "D8"  => PinD8,  "D9"  => PinD9,  "D10" => PinD10, "D11" => PinD11,
            "D12" => PinD12, "D13" => PinD13,
            "A0"  => PinA0,  "A1"  => PinA1,  "A2"  => PinA2,  "A3"  => PinA3,
            "A4"  => PinA4,  "A5"  => PinA5,  "RST" => PinRST,
            _ => null
        };
        if (border is null) return;

        border.Background = cor;
        if (border.Child is TextBlock txt)
            txt.Foreground = BrushTema("BrushErroText");
    }

}
