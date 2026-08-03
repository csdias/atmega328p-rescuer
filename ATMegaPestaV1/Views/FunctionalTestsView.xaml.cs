using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ATMegaPestaV1.Backups;
using ATMegaPestaV1.Services;
using Microsoft.Win32;

namespace ATMegaPestaV1.Views;

public partial class FunctionalTestsView : UserControl
{
    private static Brush ThemeBrush(string key) =>
        (Brush?)Application.Current.Resources[key] ?? Brushes.Transparent;

    private static Brush GreenColour => ThemeBrush("BrushPinSuccess");
    private static Brush RedColour   => ThemeBrush("BrushPinError");
    private static Brush AmberColour => ThemeBrush("BrushPinWarning");
    private static Brush GreyColour  => ThemeBrush("BrushPinIdle");

    private readonly IBusManager _busManager;
    private readonly IUsbAspService _usbAspService;

    /// <summary>ISP read failures tolerated before proposing high voltage.</summary>
    private const int MaxReadAttempts = 3;

    private int _readAttempts;

    /// <summary>
    /// The settings read went through whole: chip identified <em>and</em> fuses decoded.
    /// This is what unlocks the next step — hence the return value of
    /// <see cref="TryReadAsync"/> not being enough, since that also gives true when the
    /// chip answers but the fuses do not come out.
    /// </summary>
    private bool _settingsRead;

    /// <summary>
    /// What the last complete read identified. Serves the backup sheet, which has to say
    /// which chip the backup belongs to.
    /// </summary>
    private McuInfo? _readMcu;

    private string? _readSignature;

    /// <summary>
    /// Resting text of the integrity check card, exactly as it is in the XAML. Kept at
    /// construction so the card can go back to it without duplicating it here.
    /// </summary>
    private readonly string _integrityCheckDescription;

    private record TestItem(
        string Name,
        TextBlock Icon,
        TextBlock Time,
        Border Badge,
        string[] Pins,
        Border? Tag = null);

    private record TestResult(string Name, bool Passed, long TimeMs);

    private List<TestItem> _tests = [];
    private readonly List<TestResult> _results = [];

    // The help topics stay in Portuguese: this is what the student reads on screen.
    private static readonly Dictionary<string, (string Title, string Content)> HelpTopics = new()
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

    public FunctionalTestsView(IBusManager busManager, IUsbAspService usbAspService)
    {
        InitializeComponent();

        _busManager = busManager;
        _usbAspService = usbAspService;
        _integrityCheckDescription = TxtIntegrityCheck.Text;

        _tests =
        [
            new("UART",          IcoUart, TxtUartTime, BadgeUart, ["D0","D1"], TagUart),
            new("GPIO Escrita",  IcoGpioWrite, TxtGpioWriteTime, BadgeGpioWrite,
                ["D0","D1","D2","D3","D4","D5","D6","D7","D8","D9","D10","D11","D12","D13"]),
            new("GPIO Leitura",  IcoGpioRead, TxtGpioReadTime, BadgeGpioRead,
                ["D0","D1","D2","D3","D4","D5","D6","D7","D8","D9","D10","D11","D12","D13"]),
            new("SPI",           IcoSpi,  TxtSpiTime,  BadgeSpi,  ["D10","D11","D12","D13"], TagSpi),
            new("I²C",           IcoI2c,  TxtI2cTime,  BadgeI2c,  ["A4","A5"],               TagI2c),
            new("ADC",           IcoAdc,  TxtAdcTime,  BadgeAdc,  ["A0","A1","A2","A3","A4","A5"]),
            new("PWM",           IcoPwm,  TxtPwmTime,  BadgePwm,  ["D3","D5","D6","D9","D10","D11"], TagPwm),
        ];
    }

    private void BtnStartCheck_Click(object sender, RoutedEventArgs e)
    {
        StartCheckPanel.Visibility = Visibility.Collapsed;
        InsertChipPanel.Visibility = Visibility.Visible;
        StartArrowAnimation();
    }

    private void BtnInsertChipCancel_Click(object sender, RoutedEventArgs e)
    {
        InsertChipPanel.Visibility = Visibility.Collapsed;
        StartCheckPanel.Visibility = Visibility.Visible;
    }

    private async void BtnInsertChipConfirm_Click(object sender, RoutedEventArgs e)
    {
        InsertChipPanel.Visibility = Visibility.Collapsed;
        PanelMaster.Visibility = Visibility.Visible;
        await LoadSettingsAsync();
    }

    private void StartArrowAnimation()
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

    // ── Current settings ────────────────────────────────────────────────────

    private async void BtnDetectConfig_Click(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    /// <summary>
    /// Orchestrates one read attempt and what follows it: after
    /// <see cref="MaxReadAttempts"/> failures in a row, ISP is spent as a way in and the
    /// only way out is high-voltage programming.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        var identified = await TryReadAsync();

        // The next step opens on the whole read, not just on the chip being identified:
        // checking the pins without knowing what state the fuses are in is measuring
        // without knowing against what.
        if (_settingsRead)
            ShowNextStep();

        if (identified)
        {
            _readAttempts = 0;
            return;
        }

        _readAttempts++;
        UpdateFailurePanel();

        if (_readAttempts >= MaxReadAttempts)
            EscalateToHighVoltage();
    }

    /// <summary>
    /// Switches the bus over to the USBAsp and reads the target chip's identification and
    /// fuses. Strictly a read — nothing in this app writes fuses. At the end it always
    /// isolates the bus: ISP is not left connected to the target. Returns true only if
    /// the chip was identified.
    /// </summary>
    private async Task<bool> TryReadAsync()
    {
        BtnDetectConfig.IsEnabled = false;
        BtnRetry.IsEnabled = false;
        ClearSettings();

        // A new read invalidates the previous one: the next step closes again until this
        // one passes. Without it, whoever swapped the part in the ZIF would carry on
        // seeing the check button unlocked by another chip's read.
        _settingsRead = false;
        _readMcu = null;
        _readSignature = null;
        HideNextStep();

        var busActive = false;
        var readOk = false;

        try
        {
            // 1a — route the ISP bus to the USBAsp (menu option 2).
            SetConfigState("A activar o USBAsp no barramento...", AmberColour);
            var busRes = await _busManager.SelectUsbAspAsync();

            if (busRes is not null && busRes.StartsWith("Erro:"))
            {
                SetConfigState(busRes, RedColour);
                SetMcuState("Sem barramento", "BadgeFail");
                ShowReadFailure();
                return false;
            }

            busActive = true;

            // 1b — identify the target chip over ISP.
            SetConfigState("Barramento no USBAsp — a ler o chip-alvo...", AmberColour);
            var detect = await _usbAspService.DetectSignatureAsync();
            AppendLog(detect.Output);

            if (!detect.Success)
            {
                // avrdude's raw output does not come in here: to whoever is at the bench it
                // is noise, and the failure panel already says what to do. It still goes
                // to the log through AppendLog, above.
                SetConfigState("O chip-alvo não respondeu ao ISP.", RedColour);
                SetMcuState("Sem resposta", "BadgeFail");
                ShowReadFailure();
                return false;
            }

            ShowReadData();
            readOk = true;

            var mcu = FuseDecoder.Identify(detect.Device);
            TxtMcuName.Text      = mcu.Name;
            TxtMcuSignature.Text = detect.Signature ?? "—";
            TxtMcuFlash.Text     = DecodedFuses.FormatBytes(mcu.FlashBytes);
            TxtMcuEeprom.Text    = DecodedFuses.FormatBytes(mcu.EepromBytes);
            TxtMcuSram.Text      = DecodedFuses.FormatBytes(mcu.SramBytes);
            SetMcuState("Detetado", "BadgePass");

            // 1c — read and decode the fuses.
            var fuses = await _usbAspService.ReadFusesAsync();
            var decoded = FuseDecoder.Decode(fuses, mcu);

            if (decoded is null)
            {
                // The chip answered — the way in is not in question, so this does not
                // count as a read failure nor consume attempts.
                SetConfigState($"{mcu.Name} detetado, mas a leitura dos fuses falhou.", AmberColour);
                // The read is what failed, and this card is where it is reported: opening
                // it is the point.
                ShowAvrdudeOutput(fuses.Output, expand: true);
                return true;
            }

            FillFuses(fuses, decoded, mcu);
            _settingsRead = true;
            _readMcu = mcu;
            _readSignature = detect.Signature;

            // Dense summary: it is the only thing visible with the accordion closed.
            SetConfigState(
                $"{mcu.Name} · {detect.Signature} · " +
                $"L {fuses.Low}  H {fuses.High}  E {fuses.Extended}  LB {fuses.Lock ?? "n/d"}",
                GreenColour);
        }
        finally
        {
            // The read is over — the link to the target is cut. The bus goes back to Hi-Z
            // (menu option 4), which is the firmware's startup state. This runs even when
            // something fails halfway, so ISP is not left connected.
            if (busActive && !await IsolateBusAsync())
            {
                TxtConfigState.Text += "  ·  ATENÇÃO: falha ao isolar o barramento";
                LedConfig.Fill = AmberColour;
                TxtConfigState.Foreground = AmberColour;
            }

            BtnDetectConfig.IsEnabled = true;
            BtnRetry.IsEnabled = true;
        }

        return readOk;
    }

    // ── Read failure: counting, panel and escalation ────────────────────────

    private void ShowReadFailure()
    {
        ReadDataPanel.Visibility = Visibility.Collapsed;
        ReadFailurePanel.Visibility = Visibility.Visible;

        // The failure screen has one subject only. avrdude's output is reserved for the
        // partial failures, where it is the only clue about what went wrong.
        ConfigErrorPanel.Visibility = Visibility.Collapsed;

        // The panel brings its own button; two "Detect" buttons on the same card would
        // only split the attention.
        BtnDetectConfig.Visibility = Visibility.Collapsed;

        // A message inside a closed accordion is not much use.
        ToggleConfigDetails.IsChecked = true;
    }

    private void ShowReadData()
    {
        ReadFailurePanel.Visibility = Visibility.Collapsed;
        ReadDataPanel.Visibility = Visibility.Visible;
        BtnDetectConfig.Visibility = Visibility.Visible;
    }

    private void UpdateFailurePanel()
    {
        var exhausted = _readAttempts >= MaxReadAttempts;

        TxtFailureInstruction.Text = exhausted
            ? "O chip-alvo continuou sem responder ao ISP nas três tentativas."
            : "Verifique se o microcontrolador está corretamente inserido no ZIF socket — " +
              "alavanca baixada e pino 1 no canto marcado — e tente detetar novamente.";

        TxtFailureAttempts.Text = exhausted
            ? $"{MaxReadAttempts} de {MaxReadAttempts} tentativas sem sucesso."
            : $"Tentativa {_readAttempts} de {MaxReadAttempts}";

        BtnRetry.IsEnabled = !exhausted;
    }

    /// <summary>
    /// With the ISP attempts spent, proposes high-voltage programming. Runs after the bus
    /// has already been isolated — a modal dialog is not opened with ISP still connected
    /// to the target.
    /// </summary>
    private void EscalateToHighVoltage()
    {
        var window = Window.GetWindow(this);

        if (new HighVoltageDialog { Owner = window }.ShowDialog() == true)
        {
            (window as MainWindow)?.OpenHighVoltageProgramming();
            return;
        }

        if (new ShutdownDialog { Owner = window }.ShowDialog() == true)
        {
            Application.Current.Shutdown();
            return;
        }

        // They cancelled the exit. Giving them a fresh cycle avoids leaving them on a
        // screen where no button does anything.
        _readAttempts = 0;
        UpdateFailurePanel();
        TxtFailureAttempts.Text = "Pode tentar novamente ou navegar pelo menu lateral.";
    }

    private async void BtnRetry_Click(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    /// <summary>
    /// Returns the bus to Hi-Z. Isolating is routine tidying up: when it goes well it is
    /// not announced. When it fails it returns false — leaving the target connected
    /// without the user knowing would be worse than the error — and the caller warns on
    /// its own card.
    /// </summary>
    private async Task<bool> IsolateBusAsync()
    {
        var isolateRes = await _busManager.IsolateBusAsync();
        return isolateRes is null || !isolateRes.StartsWith("Erro:");
    }

    private void SetConfigState(string text, Brush colour)
    {
        LedConfig.Fill = colour;
        TxtConfigState.Text = text;
        TxtConfigState.Foreground = colour;
    }

    private void SetMcuState(string text, string badgeStyle)
    {
        TxtMcuState.Text = text;
        BadgeMcuState.Style = (Style)FindResource(badgeStyle);
    }

    /// <summary>
    /// Puts avrdude's raw output in the settings card.
    ///
    /// <paramref name="expand"/> has no default on purpose. Whether the accordion should
    /// open is the caller's business: when the read is what failed, the card is the
    /// subject and opening it is the point; when the failure belongs to a later step that
    /// reports itself elsewhere, forcing it open throws the whole settings panel back on
    /// screen over what the student is actually being told.
    /// </summary>
    private void ShowAvrdudeOutput(string text, bool expand)
    {
        TxtConfigOutput.Text = text.Length > 600 ? text[^600..] : text;
        ConfigErrorPanel.Visibility = Visibility.Visible;

        if (expand)
            ToggleConfigDetails.IsChecked = true;
    }

    private void ClearSettings()
    {
        ConfigErrorPanel.Visibility = Visibility.Collapsed;

        TxtMcuName.Text      = "—";
        TxtMcuSignature.Text = "—";
        TxtMcuFlash.Text     = "—";
        TxtMcuEeprom.Text    = "—";
        TxtMcuSram.Text      = "—";
        SetMcuState("A aguardar", "BadgePending");

        TxtFlashTotal.Text     = "—";
        TxtFlashAppBytes.Text  = "—";
        TxtFlashAppPct.Text    = "—";
        TxtFlashBootBytes.Text = "—";
        TxtFlashBootPct.Text   = "—";
        TxtBootLegend.Text     = "Bootloader reservado";
        BootLegendPanel.Visibility = Visibility.Visible;
        TxtFlashBootBytes.Visibility = Visibility.Visible;
        TxtFlashBootPct.Visibility   = Visibility.Visible;
        ChipFlashApp.Background = GreyColour;

        foreach (var t in new[] { TxtFuseL, TxtFuseH, TxtFuseE, TxtFuseLB,
                                  TxtFuseLDesc, TxtFuseHDesc, TxtFuseEDesc, TxtFuseLBDesc })
            t.Text = "—";

        ColFlashApp.Width  = new GridLength(1, GridUnitType.Star);
        ColFlashBoot.Width = new GridLength(0, GridUnitType.Star);
        FlashAppBar.Background = GreyColour;

        foreach (var led in new[] { LedIndIsp, LedIndReset, LedIndClock,
                                    LedIndBod, LedIndBootloader, LedIndEeprom })
            led.Fill = GreyColour;

        // The XAML's labels ("ISP habilitado", "Pino RESET activo", ...) are only the
        // panel's drawing. Before there is a read they assert nothing about the part.
        foreach (var txt in new[] { TxtIndIsp, TxtIndReset, TxtIndClock,
                                    TxtIndBod, TxtIndBootloader, TxtIndEeprom })
            txt.Text = "—";
    }

    private void FillFuses(FusesResult fuses, DecodedFuses dec, McuInfo mcu)
    {
        TxtFuseL.Text  = fuses.Low ?? "—";
        TxtFuseH.Text  = fuses.High ?? "—";
        TxtFuseE.Text  = fuses.Extended ?? "—";
        TxtFuseLB.Text = fuses.Lock ?? "n/d";

        TxtFuseLDesc.Text  = dec.LowDescription;
        TxtFuseHDesc.Text  = dec.HighDescription;
        TxtFuseEDesc.Text  = dec.ExtendedDescription;
        TxtFuseLBDesc.Text = dec.LockDescription;

        FillFlashMap(dec, mcu);

        SetIndicator(LedIndIsp, TxtIndIsp, dec.SpiEnabled,
            "ISP habilitado (SPIEN)", "ISP desactivado (SPIEN)");

        SetIndicator(LedIndReset, TxtIndReset, dec.ResetEnabled,
            "Pino RESET activo", "RESET desactivado (RSTDISBL)");

        SetIndicator(LedIndClock, TxtIndClock, !dec.Ckdiv8Enabled,
            "Clock a full speed", "CKDIV8 activo — clock ÷8");

        SetIndicator(LedIndBod, TxtIndBod, dec.BrownOut != "desactivado",
            $"Brown-out a {dec.BrownOut}", "Brown-out desactivado");

        // Bootloader and EEPROM are design choices, not defects: when they are off the
        // indicator goes grey, not amber.
        SetIndicator(LedIndBootloader, TxtIndBootloader, dec.BootRstEnabled,
            $"Bootloader de {DecodedFuses.FormatBytes(dec.BootloaderBytes)}",
            "Sem bootloader", neutralWhenFalse: true);

        SetIndicator(LedIndEeprom, TxtIndEeprom, dec.EepromPreserved,
            "EEPROM preservada em erase", "EEPROM apagada em chip erase",
            neutralWhenFalse: true);
    }

    /// <summary>
    /// Draws the Flash map bar and its legend.
    /// The green slice is what is <em>left</em> for the application after the bootloader
    /// is reserved — capacity, not occupancy: the Flash contents are not read.
    /// </summary>
    private void FillFlashMap(DecodedFuses dec, McuInfo mcu)
    {
        var boot = dec.BootRstEnabled ? dec.BootloaderBytes : 0;
        var app = dec.ApplicationFlash;

        ColFlashApp.Width  = new GridLength(app, GridUnitType.Star);
        ColFlashBoot.Width = new GridLength(boot, GridUnitType.Star);
        FlashAppBar.Background = GreenColour;
        ChipFlashApp.Background = GreenColour;

        // The bootloader's percentage is the complement of the application's: rounding
        // the two separately gave 93.8 % + 6.3 % = 100.1 %, which jumps out at you.
        var appPct = mcu.FlashBytes > 0
            ? Math.Round(100.0 * app / mcu.FlashBytes, 1)
            : 0;

        TxtFlashTotal.Text    = $"total {mcu.FlashBytes} B";
        TxtFlashAppBytes.Text = $"{app} B";
        TxtFlashAppPct.Text   = $"{appPct:0.0} %";

        if (boot > 0)
        {
            BootLegendPanel.Visibility = Visibility.Visible;
            TxtFlashBootBytes.Visibility = Visibility.Visible;
            TxtFlashBootPct.Visibility   = Visibility.Visible;
            TxtBootLegend.Text     = $"Bootloader reservado ({DecodedFuses.FormatBytes(boot)})";
            TxtFlashBootBytes.Text = $"{boot} B";
            TxtFlashBootPct.Text   = $"{100 - appPct:0.0} %";
        }
        else
        {
            // With no BOOTRST there is no orange slice on the bar — its legend would be noise.
            BootLegendPanel.Visibility = Visibility.Collapsed;
            TxtFlashBootBytes.Visibility = Visibility.Collapsed;
            TxtFlashBootPct.Visibility   = Visibility.Collapsed;
        }
    }

    private void SetIndicator(Ellipse led, TextBlock txt, bool active,
                              string activeText, string inactiveText,
                              bool neutralWhenFalse = false)
    {
        txt.Text = active ? activeText : inactiveText;

        if (active)
            led.Fill = GreenColour;
        else
            led.Fill = neutralWhenFalse ? GreyColour : AmberColour;
    }

    // ── Next step — integrity check ─────────────────────────────────────────

    /// <summary>
    /// Unlocks the next step. Called only with the settings read complete: it is that
    /// read which says which part is in the ZIF and in what state.
    /// </summary>
    private void ShowNextStep()
    {
        IntegrityCheckCard.Visibility = Visibility.Visible;
    }

    private void HideNextStep()
    {
        IntegrityCheckCard.Visibility = Visibility.Collapsed;

        // The right-hand panel follows the card: with no next step there are no results
        // to show, and the column goes back to taking up no space.
        IntegrityCheckColumn.Width = new GridLength(0);
        IntegrityCheckResultsPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Puts the ATmega2560 on the bus and runs the integrity check — what used to be
    /// steps 2 and 3. Switching the bus is not worth anything on its own: it exists so
    /// the master can exercise the target's pins, and so it is the same gesture.
    /// </summary>
    private async void BtnIntegrityCheck_Click(object sender, RoutedEventArgs e)
    {
        // The warning comes before the bus is touched: the check is not a read, and
        // whoever presses "Start" does not yet know they are going to lose what is on
        // the chip.
        var warning = new VerifierTransferDialog { Owner = Window.GetWindow(this) };
        warning.ShowDialog();

        if (warning.Choice == TransferChoice.Cancel)
            return;

        BtnIntegrityCheck.IsEnabled = false;

        // The settings have already been read and the subject has moved on: closing the
        // accordion gives the screen to the results instead of splitting it between two
        // steps. The dense summary stays visible in the card's header.
        ToggleConfigDetails.IsChecked = false;

        try
        {
            // A backup asked for and not obtained stops the check: proceeding would mean
            // writing over the chip exactly after the user said they wanted to keep it.
            if (warning.Choice == TransferChoice.ProceedWithBackup)
            {
                var outcome = await SaveChipBackupAsync();

                // null means the student cancelled — they know, no need to be told.
                if (outcome is null)
                    return;

                // Safe to open a modal here: SaveChipBackupAsync isolates the bus in its
                // own finally, so ISP is no longer connected to the target.
                new MessageDialog(outcome.Title, outcome.Message)
                {
                    Owner = Window.GetWindow(this)
                }.ShowDialog();

                if (!outcome.Ok)
                    return;
            }

            SetIntegrityCheckState("A comutar o barramento para o ATmega2560...", AmberColour);
            var megaRes = await _busManager.SwitchToMegaAsync();

            if (megaRes is not null && megaRes.StartsWith("Erro:"))
            {
                SetIntegrityCheckState(megaRes, RedColour);
                return;
            }

            ShowIntegrityCheckPanel();
            await RunTests(_tests);

            // The card goes back to its resting text: it is the right-hand panel that
            // reports what happened, and the bus is isolated next — there is no state
            // from this step that survives to be announced.
            RestIntegrityCheck();
        }
        finally
        {
            // Nothing was measured, so there is no reason to leave the master driving the
            // bus: it goes back to Hi-Z, the firmware's startup state. When the tests
            // exist, this isolation moves to after them.
            if (!await IsolateBusAsync())
                SetIntegrityCheckState(
                    TxtIntegrityCheck.Text + "  ·  ATENÇÃO: falha ao isolar o barramento",
                    AmberColour);

            BtnIntegrityCheck.IsEnabled = true;
        }
    }

    private void SetIntegrityCheckState(string text, Brush colour)
    {
        LedIntegrityCheck.Fill = colour;
        TxtIntegrityCheck.Text = text;
        TxtIntegrityCheck.Foreground = colour;
    }

    /// <summary>
    /// How a backup ended, for the caller to report once the bus is already isolated.
    /// <c>null</c> from <see cref="SaveChipBackupAsync"/> means the student cancelled —
    /// they know what they did, so nothing is shown.
    /// </summary>
    private record BackupOutcome(bool Ok, string Title, string Message);

    /// <summary>
    /// Wait pointer for as long as the bench is busy. Application-wide on purpose: while
    /// avrdude has the chip, nothing else in this window would answer either.
    ///
    /// Disposable rather than a pair of calls so the pointer comes back even if something
    /// throws on the way out — an hourglass left stuck reads as a frozen application, which
    /// is worse than no hourglass at all.
    ///
    /// Restores whatever was there before instead of assuming null, so nesting is safe.
    /// </summary>
    private sealed class WaitCursor : IDisposable
    {
        private readonly Cursor? _previous = Mouse.OverrideCursor;

        public WaitCursor() => Mouse.OverrideCursor = Cursors.Wait;

        public void Dispose() => Mouse.OverrideCursor = _previous;
    }

    /// <summary>
    /// Asks for a file name and saves the target chip's Flash and EEPROM as Intel HEX,
    /// plus the fuse sheet. Routes the bus to the USBAsp and isolates it again, like any
    /// ISP access in this app — so by the time this returns, ISP is no longer connected
    /// and the caller may open a modal.
    /// </summary>
    private async Task<BackupOutcome?> SaveChipBackupAsync()
    {
        // The student names the backup; the three files derive from that one name. The
        // name is what identifies whose chip this was months later — a timestamp alone
        // does not, once several parts have been through the bench.
        var choice = new SaveFileDialog
        {
            Title = "Nome para a cópia do microcontrolador",
            FileName = $"ATmega328P_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = ".hex",
            Filter = "Cópia do microcontrolador|*.hex",
            // The picked name is a base, never written as-is: Windows would ask about
            // overwriting a file that is not one of the three we produce.
            OverwritePrompt = false
        };

        if (choice.ShowDialog() != true)
        {
            SetIntegrityCheckState(
                "Cópia cancelada — a verificação não avançou e o chip fica como está.",
                AmberColour);
            return null;
        }

        // System.IO.Path spelled out: this file's System.Windows.Shapes.Path (the LEDs'
        // Ellipse comes from there) makes the short name ambiguous.
        var folder = System.IO.Path.GetDirectoryName(choice.FileName) ?? "";
        var name   = System.IO.Path.GetFileNameWithoutExtension(choice.FileName);

        var flash  = System.IO.Path.Combine(folder, $"{name}_flash.hex");
        var eeprom = System.IO.Path.Combine(folder, $"{name}_eeprom.hex");
        var sheet  = System.IO.Path.Combine(folder, $"{name}_fuses.txt");

        // Overwriting is checked here because the dialog could not: it never saw these
        // three names. Silently replacing an earlier backup would defeat the point of
        // making one.
        var existing = new[] { flash, eeprom, sheet }.Where(File.Exists).ToList();

        if (existing.Count > 0)
        {
            var answer = MessageBox.Show(
                "Já existem ficheiros com este nome:\n\n" +
                string.Join("\n", existing.Select(System.IO.Path.GetFileName)) +
                "\n\nSubstituir?",
                "Cópia do microcontrolador",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
            {
                SetIntegrityCheckState(
                    "Cópia cancelada — a verificação não avançou e o chip fica como está.",
                    AmberColour);
                return null;
            }
        }

        // The name and the overwrite question are asked once; only the ISP work repeats.
        for (var attempt = 1; ; attempt++)
        {
            var outcome = await TryBackupOnceAsync(flash, eeprom, sheet, folder);

            if (outcome.Ok)
                return outcome;

            if (attempt >= MaxBackupAttempts)
                return outcome with { Message = "Por favor, contacte o administrador do sistema." };

            // Safe to ask: TryBackupOnceAsync isolates the bus before returning.
            var again = new MessageDialog(
                outcome.Title,
                $"Tentativa {attempt} de {MaxBackupAttempts}.",
                actionText: "Tentar novamente")
            {
                Owner = Window.GetWindow(this)
            };

            if (again.ShowDialog() != true)
                return null;   // gave up — the card already says what happened
        }
    }

    /// <summary>How many goes at the backup before sending the student to the administrator.</summary>
    private const int MaxBackupAttempts = 3;

    /// <summary>
    /// One attempt at the backup. Routes the bus to the USBAsp and isolates it again, like
    /// any ISP access in this app — so by the time this returns, ISP is no longer connected
    /// and a modal may be opened over it.
    /// </summary>
    private async Task<BackupOutcome> TryBackupOnceAsync(
        string flash, string eeprom, string sheet, string folder)
    {
        SetIntegrityCheckState(
            "A guardar a Flash, a EEPROM e os fuses do chip-alvo...", AmberColour);

        var busActive = false;

        // From here to the end of the method the bench is talking to the chip: seconds of
        // avrdude with the screen still. Scoped to the attempt itself, so the retry
        // question is not asked under a wait pointer.
        using var busy = new WaitCursor();

        try
        {
            // Routing the bus to the USBAsp.
            var busRes = await _busManager.SelectUsbAspAsync();

            if (busRes is not null && busRes.StartsWith("Erro:"))
                return Failed(busRes);

            busActive = true;

            // Reading Flash and EEPROM off the chip. avrdude writes the two .hex itself, so
            // a disk that refuses them — full, read-only, path rejected — fails right here
            // along with a chip that will not answer. The service also checks the files
            // came out existing and non-empty, which catches an avrdude that claims success
            // over nothing.
            var backup = await _usbAspService.SaveBackupAsync(flash, eeprom);
            AppendLog(backup.Output);

            if (!backup.Success)
            {
                // The output is filed away for whoever wants it, but the accordion stays
                // as the integrity step left it — closed. A dialog is about to say what
                // happened, and re-opening the settings would put the whole panel back on
                // screen behind it.
                ShowAvrdudeOutput(backup.Output, expand: false);
                return Failed("A saída do avrdude está no cartão das configurações.");
            }

            // Writing the fuse sheet. This one is ours — File.WriteAllText, and the only
            // disk write the application makes on its own.
            try
            {
                WriteFuseSheet(sheet, backup);
            }
            catch (Exception ex)
            {
                return Failed(ex.Message);
            }

            var f = backup.Fuses;
            SetIntegrityCheckState(
                $"Cópia guardada em {folder} — Flash, EEPROM e fuses " +
                $"(L {f.Low}  H {f.High}  E {f.Extended}  LB {f.Lock ?? "n/d"})",
                GreenColour);

            // Only what was written and where. The fuse values are already on the card's
            // status line and inside the sheet itself — repeating them here a third time
            // adds nothing to read.
            return new BackupOutcome(true, "Cópia guardada",
                $"Pasta: {folder}\n\n" +
                $"    {System.IO.Path.GetFileName(flash)}\n" +
                $"    {System.IO.Path.GetFileName(eeprom)}\n" +
                $"    {System.IO.Path.GetFileName(sheet)}");
        }
        catch (Exception ex)
        {
            // Whatever got past the checks above. The three named paths cover reading the
            // chip and both writes, but "handled" has to mean handled: a path the file
            // system rejects, a device pulled mid-read, anything unforeseen. Without this
            // the exception would leave through an async void handler and take the whole
            // application with it, which at a bench with a chip in the socket is the worst
            // way to fail.
            return Failed(ex.Message);
        }
        finally
        {
            if (busActive && !await IsolateBusAsync())
                SetIntegrityCheckState(
                    TxtIntegrityCheck.Text + "  ·  ATENÇÃO: falha ao isolar o barramento",
                    AmberColour);
        }

        // One sentence for every way a backup can fail — reaching the chip or writing to
        // the disk. Telling the two apart would only ask the student to diagnose something
        // they cannot act on differently; the detail goes in the body, for whoever wants it.
        BackupOutcome Failed(string detail)
        {
            SetIntegrityCheckState("Não foi possível efetuar o backup.", RedColour);
            return new BackupOutcome(false, "Não foi possível efetuar o backup.", detail);
        }
    }

    /// <summary>
    /// Writes the backup's sheet. The text is built in the Core because the API writes the
    /// same sheet, and the two must not drift apart.
    /// </summary>
    private void WriteFuseSheet(string path, BackupResult backup)
    {
        var mcu = _readMcu ?? FuseDecoder.Default;
        var sheet = FuseSheet.Build(mcu, _readSignature, backup, DateTime.Now);

        File.WriteAllText(path, sheet, Encoding.UTF8);
    }

    /// <summary>Returns the card to its resting state: the description and the idle LED.</summary>
    private void RestIntegrityCheck()
    {
        LedIntegrityCheck.Fill = GreyColour;
        TxtIntegrityCheck.Text = _integrityCheckDescription;
        TxtIntegrityCheck.Foreground = ThemeBrush("BrushTextNormal");
    }

    /// <summary>
    /// Reveals the integrity check panel. Should only be called when the integrity check
    /// starts — until then the column has zero width.
    /// </summary>
    private void ShowIntegrityCheckPanel()
    {
        IntegrityCheckColumn.Width = new GridLength(360);
        IntegrityCheckResultsPanel.Visibility = Visibility.Visible;
    }

    private void AppendLog(string text)
    {
        TxtLastResult.Text = text.Length > 200 ? text[^200..] : text;
    }


    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string key && HelpTopics.TryGetValue(key, out var help))
        {
            var dialog = new MessageDialog(help.Title, help.Content)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
    }

    private async void BtnStartAll_Click(object sender, RoutedEventArgs e)
    {
        await RunTests(_tests);
    }

    private async void BtnRetestFailures_Click(object sender, RoutedEventArgs e)
    {
        var failedNames = _results
            .Where(r => !r.Passed)
            .Select(r => r.Name)
            .ToHashSet();

        var failures = _tests.Where(t => failedNames.Contains(t.Name)).ToList();

        if (failures.Count > 0)
            await RunTests(failures);
    }

    // With no physical execution there is nothing to stop; the button is hidden.
    private void BtnStop_Click(object sender, RoutedEventArgs e) { }

    private void BtnExportLog_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
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

        var passed = 0;
        var failed = 0;

        foreach (var r in _results)
        {
            var state = r.Passed ? "PASSOU" : "FALHOU";
            sb.AppendLine($"  {r.Name,-25} {state,-10} {r.TimeMs,6} ms");
            if (r.Passed) passed++;
            else failed++;
        }

        sb.AppendLine();
        sb.AppendLine(new string('-', 50));
        sb.AppendLine($"  Total: {_results.Count}   Passou: {passed}   Falhou: {failed}");

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

        TxtLastResult.Text = $"Log exportado para:\n{dialog.FileName}";
    }

    /// <summary>
    /// The integrity check has no real execution. The Prog_Tester V1.2 firmware only
    /// exposes Serial2 and SPI diagnostics — for GPIO, I²C, ADC and PWM there is no
    /// command at all, and without a measurement there is no result. What used to be here
    /// was a PASS/FAIL draw, indistinguishable at the bench from a real measurement;
    /// leaving the tests unrun is less information but information that is right.
    /// </summary>
    private Task RunTests(List<TestItem> tests)
    {
        _results.RemoveAll(r => tests.Any(t => t.Name == r.Name));

        foreach (var test in tests)
        {
            test.Icon.Text = "—";
            test.Icon.Foreground = GreyColour;
            test.Time.Text = "n/d";
            test.Badge.Style = (Style)FindResource("BadgePending");
        }

        TxtLastResult.Text =
            "Verificação de integridade por implementar — o firmware não expõe estes testes. " +
            "Nada foi medido.";

        BtnStartAll.IsEnabled = false;
        BtnRetestFailures.Visibility = Visibility.Collapsed;
        BtnExportLog.IsEnabled = _results.Count > 0;
        BtnStop.Visibility = Visibility.Collapsed;

        UpdateProgressBar();
        return Task.CompletedTask;
    }

    private void UpdateProgressBar()
    {
        var total = _tests.Count;
        if (total == 0) return;

        var pct = _results.Count * 100 / total;
        TxtProgressPct.Text = $"{pct}%";

        // The bar takes the parent's width — uses ActualWidth at runtime
        var parentWidth = ((Border)ProgressFill.Parent).ActualWidth;
        ProgressFill.Width = parentWidth * pct / 100;
    }

    private void SetPinColor(string name, Brush colour)
    {
        var border = name switch
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

        border.Background = colour;
        if (border.Child is TextBlock txt)
            txt.Foreground = ThemeBrush("BrushErrorText");
    }

}
