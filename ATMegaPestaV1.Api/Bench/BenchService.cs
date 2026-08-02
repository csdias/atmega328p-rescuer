using ATMegaPestaV1.Backups;
using ATMegaPestaV1.Diagnostics;
using ATMegaPestaV1.Services;
using Microsoft.AspNetCore.SignalR;

namespace ATMegaPestaV1.Api.Bench;

/// <summary>
/// The bench as the API sees it. Does what the WPF code-behind did — scans the USB, routes
/// the bus, reads the target chip, saves the backup — and reports the same things it did.
///
/// It is a singleton and serialises everything behind a semaphore because the bench is a
/// single physical resource: one serial port, one USBAsp, one bus. Two HTTP requests
/// switching the bus at the same time would leave the target connected to two masters, and
/// avrdude running twice in parallel fails both times. Whoever arrives mid-operation waits
/// their turn.
///
/// The state it keeps between requests is the same the window kept between clicks: the
/// port where the CH340 showed up, the attempts spent, and what the last read identified
/// (which the backup sheet needs).
/// </summary>
public class BenchService
{
    /// <summary>ISP read failures tolerated before proposing high voltage.</summary>
    private const int MaxReadAttempts = 3;

    private readonly IServiceFactory _services;
    private readonly IDeviceDetector _detector;
    private readonly IHubContext<BenchHub> _hub;
    private readonly ILogger<BenchService> _log;

    private readonly int _maxAttempts;
    private readonly bool _verifySignature;
    private readonly string _backupFolder;

    private readonly SemaphoreSlim _access = new(1, 1);

    private int _detectionAttempt;
    private bool _detectionDone;
    private string? _comPort;

    private int _readAttempts;
    private McuInfo? _readMcu;
    private string? _readSignature;

    public BenchService(IConfiguration config, IHubContext<BenchHub> hub, ILogger<BenchService> log)
    {
        _hub = hub;
        _log = log;

        _services = ServiceFactory.From(config);
        _detector = _services.CreateDetector();

        var max = config.GetValue<int>("MaxAttempts");
        _maxAttempts = max > 0 ? max : 3;

        // Benches with older firmware do not answer option 1 — hence the option to turn the
        // check off, as in the WPF.
        _verifySignature = config.GetValue("VerifySignature", true);

        _backupFolder = ResolveBackupFolder(config);
    }

    public ConfigResponse Config => new(
        _maxAttempts, _verifySignature, MaxReadAttempts, _backupFolder);

    public CatalogResponse Catalog => new(
        TestCatalog.Pins, TestCatalog.All, IntegrityState.NotImplemented);

    /// <summary>
    /// Where the backups live. The browser does not pick folders on the server, so the
    /// folder is API configuration — each backup gets its own subfolder stamped with the
    /// time.
    /// </summary>
    private static string ResolveBackupFolder(IConfiguration config)
    {
        var configured = config.GetValue<string>("BackupFolder");

        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ATMegaPesta", "Copias");
    }

    // ── Device verification ─────────────────────────────────────────────────

    /// <summary>
    /// Scans the USB and asks the bench for its signature. Each call is one attempt, like
    /// each click of the WPF button — except once the bench is already complete, where a new
    /// call only reconfirms what is plugged in (the front end may reload the page) and
    /// does not spend attempts.
    /// </summary>
    public async Task<DetectionResponse> DetectAsync(CancellationToken ct)
    {
        await _access.WaitAsync(ct);
        try
        {
            // Attempts spent: the front end has already shown the shutdown and there is
            // nothing to re-evaluate until someone restarts the cycle.
            if (!_detectionDone && _detectionAttempt >= _maxAttempts)
                return Exhausted();

            if (!_detectionDone)
                _detectionAttempt++;

            await ProgressAsync("A verificar dispositivos...", LedState.Idle);
            var devices = await _detector.DetectAsync(ct);

            var ch340 = devices.Ch340Port is { } port
                ? new Indicator(LedState.Ok, $"Conectado na porta {port}")
                : new Indicator(LedState.Error, "Não detectado — Ligue o conversor USB-Serial CH340");

            var usbAsp = devices.UsbAspConnected
                ? new Indicator(LedState.Ok, "Conectado")
                : new Indicator(LedState.Error, "Não detectado — Ligue o programador USBAsp");

            var (signature, signatureOk) =
                await VerifySignatureAsync(devices.Ch340Port, ct);

            if (devices.Complete && signatureOk)
            {
                _detectionDone = true;
                _comPort = devices.Ch340Port;

                return new DetectionResponse(ch340, usbAsp, signature, _comPort,
                    CanProceed: true, _detectionAttempt, _maxAttempts, Exhausted: false,
                    "Bancada pronta.", LedState.Ok);
            }

            if (_detectionAttempt >= _maxAttempts)
                return Exhausted(ch340, usbAsp, signature);

            return new DetectionResponse(ch340, usbAsp, signature, devices.Ch340Port,
                CanProceed: false, _detectionAttempt, _maxAttempts, Exhausted: false,
                "Dispositivo(s) em falta. Por favor, ligue o(s) dispositivo(s) e tente novamente.",
                LedState.Warning);
        }
        finally
        {
            _access.Release();
        }
    }

    private DetectionResponse Exhausted(
        Indicator? ch340 = null, Indicator? usbAsp = null, Indicator? signature = null) =>
        new(ch340 ?? new Indicator(LedState.Error, "Não detectado"),
            usbAsp ?? new Indicator(LedState.Error, "Não detectado"),
            signature ?? new Indicator(LedState.Idle, "A aguardar detecção do CH340..."),
            null, CanProceed: false, _detectionAttempt, _maxAttempts, Exhausted: true,
            "Não foi possível detectar todos os dispositivos após várias tentativas.\n" +
            "Por favor, contacte a assistência técnica.",
            LedState.Error);

    /// <summary>
    /// Asks the bench for its signature (option 1 of the firmware menu). With no CH340 there
    /// is nowhere to ask, and with the check turned off it is not asked.
    /// </summary>
    private async Task<(Indicator, bool)> VerifySignatureAsync(string? port, CancellationToken ct)
    {
        if (!_verifySignature)
            return (new Indicator(LedState.Idle, "Verificação de assinatura ignorada"), true);

        if (port is null)
            return (new Indicator(LedState.Idle, "A aguardar detecção do CH340..."), false);

        await ProgressAsync("A verificar a assinatura do equipamento...", LedState.Idle);
        var result = await _services.CreateBusManager(port).VerifySignatureAsync(ct);

        if (result.Valid)
            return (new Indicator(LedState.Ok,
                $"Equipamento identificado: {result.Signature} na porta {port}"), true);

        var detail = result.Response is { Length: > 0 } response
            ? $"Assinatura inesperada em {port} — recebido: {FirstLine(response)}"
            : $"Sem resposta do equipamento em {port} — Verifique a ligação";

        return (new Indicator(LedState.Error, detail), false);
    }

    private static string FirstLine(string text) => text.Split('\n')[0].Trim();

    /// <summary>
    /// Puts the cycle back to the start: attempts at zero and the previous read discarded.
    /// It is what you do when swapping the part in the ZIF, and what the WPF managed by
    /// relaunching the application.
    /// </summary>
    public async Task ResetAsync(CancellationToken ct)
    {
        await _access.WaitAsync(ct);
        try
        {
            _detectionAttempt = 0;
            _detectionDone = false;
            _comPort = null;
            _readAttempts = 0;
            _readMcu = null;
            _readSignature = null;
        }
        finally
        {
            _access.Release();
        }
    }

    // ── Reading the target chip's settings ──────────────────────────────────

    /// <summary>
    /// Routes the bus to the USBAsp, reads the target chip's identification and fuses, and
    /// isolates the bus again. Strictly a read — nothing in this API writes fuses.
    ///
    /// After <see cref="MaxReadAttempts"/> failures in a row, ISP is spent as a way in:
    /// the response comes back with <c>Exhausted</c> and the front end proposes
    /// high-voltage programming.
    /// </summary>
    public async Task<ReadResponse> ReadSettingsAsync(CancellationToken ct)
    {
        await _access.WaitAsync(ct);
        try
        {
            // A new read invalidates the previous one: whoever swapped the part in the ZIF
            // should not carry on seeing the check unlocked by another chip's read.
            _readMcu = null;
            _readSignature = null;

            var read = await TryReadAsync(ct);

            if (read.Identified)
            {
                _readAttempts = 0;
                return read with { Attempt = 0, MaxAttempts = MaxReadAttempts };
            }

            _readAttempts++;
            var exhausted = _readAttempts >= MaxReadAttempts;

            return read with
            {
                Instruction = exhausted
                    ? "O chip-alvo continuou sem responder ao ISP nas três tentativas."
                    : "Verifique se o microcontrolador está corretamente inserido no ZIF socket — " +
                      "alavanca baixada e pino 1 no canto marcado — e tente detetar novamente.",
                Attempts = exhausted
                    ? $"{MaxReadAttempts} de {MaxReadAttempts} tentativas sem sucesso."
                    : $"Tentativa {_readAttempts} de {MaxReadAttempts}",
                Attempt = _readAttempts,
                MaxAttempts = MaxReadAttempts,
                Exhausted = exhausted
            };
        }
        finally
        {
            _access.Release();
        }
    }

    private async Task<ReadResponse> TryReadAsync(CancellationToken ct)
    {
        var busActive = false;

        try
        {
            // 1a — route the ISP bus to the USBAsp (menu option 2).
            await ProgressAsync("A activar o USBAsp no barramento...", LedState.Warning);
            var busRes = await _services.CreateBusManager(_comPort ?? "").SelectUsbAspAsync(ct);

            // With no bus nothing ever got connected to the target — nothing to isolate.
            if (IsBusError(busRes))
                return Failure(busRes!, busIsolated: true);

            busActive = true;

            // 1b — identify the target chip over ISP.
            await ProgressAsync("Barramento no USBAsp — a ler o chip-alvo...", LedState.Warning);
            var usbAsp = _services.CreateUsbAspService();
            var detect = await usbAsp.DetectSignatureAsync(ct);

            if (!detect.Success)
            {
                // avrdude's raw output does not go into the message: to whoever is at the
                // bench it is noise. It travels separately, for anyone who wants the log.
                var isolated = await IsolateAsync(ct);
                busActive = false;

                return Failure("O chip-alvo não respondeu ao ISP.", isolated, detect.Output);
            }

            var mcu = FuseDecoder.Identify(detect.Device);

            // 1c — read and decode the fuses.
            await ProgressAsync($"{mcu.Name} detetado — a ler os fuse bits...", LedState.Warning);
            var fuses = await usbAsp.ReadFusesAsync(ct);
            var decoded = FuseDecoder.Decode(fuses, mcu);

            var finalIsolated = await IsolateAsync(ct);
            busActive = false;

            if (decoded is null)
            {
                // The chip answered — the way in is not in question, so this does not
                // count as a read failure nor consume attempts.
                return new ReadResponse(
                    Identified: true, SettingsRead: false, Map(mcu),
                    detect.Signature, Map(fuses), null, null,
                    $"{mcu.Name} detetado, mas a leitura dos fuses falhou.", LedState.Warning,
                    null, null, 0, MaxReadAttempts, Exhausted: false, finalIsolated, fuses.Output);
            }

            _readMcu = mcu;
            _readSignature = detect.Signature;

            // Dense summary: it is the only thing visible with the accordion closed.
            var summary = $"{mcu.Name} · {detect.Signature} · " +
                          $"L {fuses.Low}  H {fuses.High}  E {fuses.Extended}  LB {fuses.Lock ?? "n/d"}";

            return new ReadResponse(
                Identified: true, SettingsRead: true, Map(mcu),
                detect.Signature, Map(fuses), Map(decoded),
                MapFlash(mcu, decoded),
                summary, LedState.Ok, null, null,
                0, MaxReadAttempts, Exhausted: false, finalIsolated, null);
        }
        finally
        {
            // Runs when something blows up halfway: ISP is not left connected to the target.
            if (busActive)
                await IsolateAsync(ct);
        }
    }

    private static ReadResponse Failure(string state, bool busIsolated,
                                        string? avrdudeOutput = null) =>
        new(Identified: false, SettingsRead: false, null, null, null, null, null,
            state, LedState.Error, null, null, 0, MaxReadAttempts,
            Exhausted: false, busIsolated, avrdudeOutput);

    // ── Backup ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the target chip's Flash, EEPROM and fuse sheet into a subfolder stamped with
    /// the time. Routes the bus to the USBAsp and isolates it again, like any ISP access.
    /// </summary>
    public async Task<BackupResponse> SaveBackupAsync(CancellationToken ct)
    {
        await _access.WaitAsync(ct);
        try
        {
            return await SaveBackupInternalAsync(ct);
        }
        finally
        {
            _access.Release();
        }
    }

    private async Task<BackupResponse> SaveBackupInternalAsync(CancellationToken ct)
    {
        // Timestamped on the spot: whoever brings several parts to the bench does not end up
        // with backups overlapping each other with no way to tell which is which.
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var folder = Path.Combine(_backupFolder, timestamp);
        Directory.CreateDirectory(folder);

        var flash  = Path.Combine(folder, $"ATmega328P_{timestamp}_flash.hex");
        var eeprom = Path.Combine(folder, $"ATmega328P_{timestamp}_eeprom.hex");
        var sheet  = Path.Combine(folder, $"ATmega328P_{timestamp}_fuses.txt");

        await ProgressAsync("A guardar a Flash, a EEPROM e os fuses do chip-alvo...", LedState.Warning);

        var busActive = false;

        try
        {
            var busRes = await _services.CreateBusManager(_comPort ?? "").SelectUsbAspAsync(ct);

            if (IsBusError(busRes))
                return new BackupResponse(false, timestamp, folder, [], null, busRes!,
                    LedState.Error, null, BusIsolated: true);

            busActive = true;

            var backup = await _services.CreateUsbAspService().SaveBackupAsync(flash, eeprom, ct);

            if (!backup.Success)
            {
                var failureIsolated = await IsolateAsync(ct);
                busActive = false;

                return new BackupResponse(false, timestamp, folder, [], Map(backup.Fuses),
                    "A cópia falhou — a verificação não avançou e o chip fica como está.",
                    LedState.Error, backup.Output, failureIsolated);
            }

            // The sheet is written here, and not in the service: the bytes come from
            // avrdude, but what they mean is what the FuseDecoder knows.
            try
            {
                var text = FuseSheet.Build(
                    _readMcu ?? FuseDecoder.Default, _readSignature, backup, DateTime.Now);

                await File.WriteAllTextAsync(sheet, text, System.Text.Encoding.UTF8, ct);
            }
            catch (Exception ex)
            {
                var sheetIsolated = await IsolateAsync(ct);
                busActive = false;

                return new BackupResponse(false, timestamp, folder, List(timestamp, flash, eeprom),
                    Map(backup.Fuses),
                    $"As memórias foram guardadas mas a ficha dos fuses falhou: {ex.Message}",
                    LedState.Error, backup.Output, sheetIsolated);
            }

            var isolated = await IsolateAsync(ct);
            busActive = false;

            var f = backup.Fuses;
            return new BackupResponse(true, timestamp, folder,
                List(timestamp, flash, eeprom, sheet), Map(f),
                $"Cópia guardada em {folder} — Flash, EEPROM e fuses " +
                $"(L {f.Low}  H {f.High}  E {f.Extended}  LB {f.Lock ?? "n/d"})",
                LedState.Ok, backup.Output, isolated);
        }
        finally
        {
            if (busActive)
                await IsolateAsync(ct);
        }
    }

    private static IReadOnlyList<BackupFile> List(string timestamp, params string[] paths) =>
        [.. paths
            .Select(p => new FileInfo(p))
            .Where(fi => fi.Exists)
            .Select(fi => new BackupFile(
                fi.Name, $"/api/target/backups/{timestamp}/{Uri.EscapeDataString(fi.Name)}", fi.Length))];

    /// <summary>
    /// Opens a file from a backup for download. The name is validated against what the
    /// backup actually wrote — a path coming from the client is not joined to a folder
    /// without more.
    /// </summary>
    public FileInfo? LocateBackupFile(string timestamp, string name)
    {
        // Only the timestamp this API generates: digits and one underscore, no separators.
        if (!System.Text.RegularExpressions.Regex.IsMatch(timestamp, @"^\d{8}_\d{6}$"))
            return null;

        if (name != Path.GetFileName(name))
            return null;

        var folder = Path.Combine(_backupFolder, timestamp);
        var file = new FileInfo(Path.Combine(folder, name));

        // Once resolved, it has to still be inside the backups folder.
        if (!file.FullName.StartsWith(Path.GetFullPath(_backupFolder), StringComparison.OrdinalIgnoreCase))
            return null;

        return file.Exists ? file : null;
    }

    // ── Integrity check ─────────────────────────────────────────────────────

    /// <summary>
    /// Switches the bus over to the ATmega2560 and returns the test list — pending.
    ///
    /// The switching and the isolation do actually happen; it is the measurement that does
    /// not exist. The Prog_Tester V1.2 firmware only exposes Serial2 and SPI diagnostics,
    /// and a made-up PASS/FAIL would be indistinguishable at the bench from a real
    /// measurement.
    /// </summary>
    public async Task<IntegrityResponse> RunIntegrityAsync(
        IntegrityRequest request, CancellationToken ct)
    {
        await _access.WaitAsync(ct);
        try
        {
            BackupResponse? backup = null;

            // A backup asked for and not obtained stops the check: proceeding would mean
            // writing over the chip exactly after someone said they wanted to keep it.
            if (request.Choice == TransferChoice.ProceedWithBackup)
            {
                backup = await SaveBackupInternalAsync(ct);

                if (!backup.Success)
                    return new IntegrityResponse(false, backup.BusIsolated, backup,
                        Pending(), 0, backup.Message, LedState.Error);
            }

            await ProgressAsync("A comutar o barramento para o ATmega2560...", LedState.Warning);
            var megaRes = await _services.CreateBusManager(_comPort ?? "").SwitchToMegaAsync(ct);

            if (IsBusError(megaRes))
                return new IntegrityResponse(false, await IsolateAsync(ct), backup,
                    Pending(), 0, megaRes!, LedState.Error);

            await ProgressAsync(IntegrityState.NotImplemented, LedState.Warning);

            // Nothing was measured, so there is no reason to leave the master driving the
            // bus: it goes back to Hi-Z, the firmware's startup state.
            var isolated = await IsolateAsync(ct);

            var message = isolated
                ? IntegrityState.NotImplemented
                : IntegrityState.NotImplemented + "  ·  ATENÇÃO: falha ao isolar o barramento";

            return new IntegrityResponse(BusSwitched: true, isolated, backup,
                Pending(), 0, message, LedState.Warning);
        }
        finally
        {
            _access.Release();
        }
    }

    private static IReadOnlyList<TestResultDto> Pending() =>
        [.. TestCatalog.All.Select(t => new TestResultDto(t.Name, TestState.Pending, "n/d"))];

    // ── Bus ─────────────────────────────────────────────────────────────────

    private static bool IsBusError(string? response) =>
        response is not null && response.StartsWith("Erro:");

    /// <summary>
    /// Returns the bus to Hi-Z. Isolating is routine tidying up: when it goes well it is
    /// not announced. When it fails it returns false — leaving the target connected with
    /// nobody knowing would be worse than the error — and the caller warns in its own
    /// response.
    /// </summary>
    private async Task<bool> IsolateAsync(CancellationToken ct)
    {
        try
        {
            var response = await _services.CreateBusManager(_comPort ?? "").IsolateBusAsync(ct);
            var isolated = !IsBusError(response);

            if (!isolated)
                _log.LogWarning("Falha ao isolar o barramento: {Response}", response);

            return isolated;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Excepção ao isolar o barramento");
            return false;
        }
    }

    private Task ProgressAsync(string text, LedState severity) =>
        _hub.Clients.All.SendAsync(BenchHub.ProgressEvent, new Progress(text, severity));

    // ── Mapping to the contracts ────────────────────────────────────────────

    private static McuDto Map(McuInfo mcu) => new(
        mcu.Name, mcu.FlashBytes, mcu.EepromBytes, mcu.SramBytes,
        DecodedFuses.FormatBytes(mcu.FlashBytes),
        DecodedFuses.FormatBytes(mcu.EepromBytes),
        DecodedFuses.FormatBytes(mcu.SramBytes));

    private static FusesDto Map(FusesResult f) => new(f.Low, f.High, f.Extended, f.Lock);

    private static DecodedFusesDto Map(DecodedFuses d) => new(
        d.Clock, d.Ckdiv8Enabled, d.BrownOut, d.SpiEnabled, d.ResetEnabled,
        d.EepromPreserved, d.BootRstEnabled, d.LockLevel, d.ReadEnabled,
        d.LowDescription, d.HighDescription, d.ExtendedDescription, d.LockDescription);

    private static FlashMapDto MapFlash(McuInfo mcu, DecodedFuses d) => new(
        mcu.FlashBytes,
        d.BootRstEnabled ? d.BootloaderBytes : 0,
        d.ApplicationFlash,
        DecodedFuses.FormatBytes(d.BootloaderBytes),
        DecodedFuses.FormatBytes(d.ApplicationFlash),
        d.BootRstEnabled);
}
