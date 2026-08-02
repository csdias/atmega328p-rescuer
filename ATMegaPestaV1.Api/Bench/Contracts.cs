using ATMegaPestaV1.Diagnostics;

namespace ATMegaPestaV1.Api.Bench;

/// <summary>
/// State of an indicator, in the vocabulary the WPF LEDs already used. Serialises as text
/// ("ok", "warning", ...) so the front end does not depend on the order of the members.
/// </summary>
public enum LedState
{
    Idle,
    Ok,
    Warning,
    Error
}

/// <summary>An indicator: the colour and the line of text that goes with it.</summary>
public record Indicator(LedState State, string Detail);

/// <summary>Bench parameters the front end needs to know at startup.</summary>
public record ConfigResponse(
    int MaxAttempts,
    bool VerifySignature,
    int MaxReadAttempts,
    string BackupFolder);

/// <summary>
/// Result of a USB scan, with the signature already verified.
/// </summary>
/// <param name="CanProceed">Bench complete and equipment identified.</param>
/// <param name="Exhausted">
/// Attempts spent without success — the front end shows the shutdown, as the WPF does.
/// </param>
public record DetectionResponse(
    Indicator Ch340,
    Indicator UsbAsp,
    Indicator Signature,
    string? ComPort,
    bool CanProceed,
    int Attempt,
    int MaxAttempts,
    bool Exhausted,
    string Message,
    LedState Severity);

/// <summary>The four bytes, as avrdude returned them.</summary>
public record FusesDto(string? Low, string? High, string? Extended, string? Lock);

/// <summary>The same bytes in human language.</summary>
public record DecodedFusesDto(
    string Clock,
    bool Ckdiv8Enabled,
    string BrownOut,
    bool SpiEnabled,
    bool ResetEnabled,
    bool EepromPreserved,
    bool BootRstEnabled,
    string LockLevel,
    bool ReadEnabled,
    string LowDescription,
    string HighDescription,
    string ExtendedDescription,
    string LockDescription);

/// <summary>Fixed characteristics of the identified chip, already formatted.</summary>
public record McuDto(
    string Name,
    int FlashBytes,
    int EepromBytes,
    int SramBytes,
    string Flash,
    string Eeprom,
    string Sram);

/// <summary>
/// The Flash map. The application's slice is what is <em>left</em> after the bootloader is
/// reserved — capacity, not occupancy: the Flash contents are not read.
/// </summary>
public record FlashMapDto(
    int TotalBytes,
    int BootloaderBytes,
    int ApplicationBytes,
    string Bootloader,
    string Application,
    bool BootloaderReserved);

/// <summary>
/// Result of a read of the target chip's settings.
/// </summary>
/// <param name="Identified">The chip answered over ISP.</param>
/// <param name="SettingsRead">
/// Chip identified <em>and</em> fuses decoded. It is this — and not
/// <paramref name="Identified"/> — that unlocks the integrity check.
/// </param>
/// <param name="Exhausted">ISP attempts spent: the front end proposes high voltage.</param>
/// <param name="BusIsolated">
/// The bus went back to Hi-Z. False is a warning to show, not an internal detail.
/// </param>
public record ReadResponse(
    bool Identified,
    bool SettingsRead,
    McuDto? Mcu,
    string? Signature,
    FusesDto? Fuses,
    DecodedFusesDto? Decoded,
    FlashMapDto? FlashMap,
    string State,
    LedState Severity,
    string? Instruction,
    string? Attempts,
    int Attempt,
    int MaxAttempts,
    bool Exhausted,
    bool BusIsolated,
    string? AvrdudeOutput);

/// <summary>One file in the backup and where it can be downloaded from.</summary>
public record BackupFile(string Name, string Url, long Bytes);

/// <summary>
/// Result of a backup. The folder is on the server side — the browser does not pick
/// folders — and the files are downloaded through the returned URLs.
/// </summary>
public record BackupResponse(
    bool Success,
    string Timestamp,
    string Folder,
    IReadOnlyList<BackupFile> Files,
    FusesDto? Fuses,
    string Message,
    LedState Severity,
    string? AvrdudeOutput,
    bool BusIsolated);

/// <summary>What whoever asked for the check decided about the backup before going on.</summary>
public enum TransferChoice
{
    ProceedWithBackup,
    ProceedWithoutBackup
}

public record IntegrityRequest(TransferChoice Choice);

/// <summary>State of a test in the results list.</summary>
public enum TestState
{
    Pending,
    Passed,
    Failed
}

public record TestResultDto(string Name, TestState State, string Time);

/// <summary>
/// Result of the integrity check.
///
/// <paramref name="Results"/> always comes back pending and <paramref name="ProgressPct"/>
/// at zero: see <see cref="IntegrityState.NotImplemented"/>. The bus is switched to the
/// master and isolated at the end, because that does actually happen — what does not
/// happen is the measurement.
/// </summary>
public record IntegrityResponse(
    bool BusSwitched,
    bool BusIsolated,
    BackupResponse? Backup,
    IReadOnlyList<TestResultDto> Results,
    int ProgressPct,
    string Message,
    LedState Severity);

/// <summary>Which tests exist, which pins they touch, and the warning that they are not run.</summary>
public record CatalogResponse(
    IReadOnlyList<string> Pins,
    IReadOnlyList<CatalogTest> Tests,
    string Warning);
