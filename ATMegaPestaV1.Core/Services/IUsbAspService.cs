namespace ATMegaPestaV1.Services;

/// <summary>
/// Result of an avrdude ISP operation.
/// <paramref name="Signature"/> and <paramref name="Device"/> come from avrdude's output
/// when the chip answers (e.g. "1E 95 0F" and "ATmega328P").
/// </summary>
public record AvrdudeResult(bool Success, string Output, string? Signature = null, string? Device = null);

/// <summary>
/// Result of reading the fuse bits (lfuse/hfuse/efuse) and the lock byte.
/// Values come in hexadecimal (e.g. "0xFF"), or null if the read failed.
/// </summary>
public record FusesResult(bool Success, string? Low, string? High, string? Extended, string? Lock, string Output);

/// <summary>
/// Result of a backup of the target chip.
/// </summary>
/// <param name="Files">Paths written, so the user can be told where they ended up.</param>
/// <param name="Fuses">
/// The fuse bits as they stood at backup time. Without them the backup does not restore
/// the chip: the memories are back in place but running off a different clock, a
/// different brown-out level, or with ISP disabled.
/// </param>
public record BackupResult(bool Success, string Output, IReadOnlyList<string> Files, FusesResult Fuses);

/// <summary>
/// Wrapper over avrdude for talking to the target chip through the USBAsp.
/// </summary>
public interface IUsbAspService
{
    /// <summary>
    /// Reads Flash, EEPROM and the fuse bits off the target chip. The memories go to
    /// Intel HEX files; the fuses come back in the result so the caller can record them.
    /// Still read-only: this is what gets saved before the integrity check writes over
    /// whatever the student has on the chip.
    /// </summary>
    Task<BackupResult> SaveBackupAsync(string flashPath, string eepromPath,
                                       CancellationToken ct = default);

    /// <summary>
    /// Attempts to reach the target chip: avrdude -c usbasp -p m328p.
    /// Returns the full output and whether it succeeded (signature read).
    /// </summary>
    Task<AvrdudeResult> DetectSignatureAsync(CancellationToken ct = default);

    /// <summary>
    /// Retries ISP after clock injection: avrdude -c usbasp -p m328p.
    /// </summary>
    Task<AvrdudeResult> RetryIspAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the target chip's fuse bits (lfuse, hfuse, efuse) through avrdude.
    /// </summary>
    Task<FusesResult> ReadFusesAsync(CancellationToken ct = default);
}
