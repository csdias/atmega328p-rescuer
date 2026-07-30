using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Real UsbAspService implementation over avrdude.exe.
/// </summary>
public class UsbAspService : IUsbAspService
{
    // -v so avrdude prints the device name/signature (at the default verbosity
    // avrdude 8.1 does not).
    private const string AvrdudeArgs = "-c usbasp -p m328p -v";

    // Reads the fuses and the lock byte to stdout in hexadecimal (the informational
    // text goes to stderr). Read-only — fuses are never written.
    private const string FusesArgs =
        "-c usbasp -p m328p -U lfuse:r:-:h -U hfuse:r:-:h -U efuse:r:-:h -U lock:r:-:h";

    public Task<AvrdudeResult> DetectSignatureAsync(CancellationToken ct = default) =>
        Task.Run(() => RunAvrdude(AvrdudeArgs), ct);

    public Task<AvrdudeResult> RetryIspAsync(CancellationToken ct = default) =>
        Task.Run(() => RunAvrdude(AvrdudeArgs), ct);

    public Task<FusesResult> ReadFusesAsync(CancellationToken ct = default) =>
        Task.Run(ReadFuses, ct);

    public Task<BackupResult> SaveBackupAsync(string flashPath, string eepromPath,
                                              CancellationToken ct = default) =>
        Task.Run(() => SaveBackup(flashPath, eepromPath), ct);

    /// <summary>
    /// A single invocation for both memories and the fuses: avrdude does one "program
    /// enable" per run, and separate runs would give the chip several chances to not
    /// answer for a backup that is only worth anything whole. The memories go to files,
    /// the fuses to stdout — which is then left holding only them.
    /// </summary>
    private static BackupResult SaveBackup(string flashPath, string eepromPath)
    {
        var files = new[] { flashPath, eepromPath };

        try
        {
            var (exitCode, stdout, stderr) = ExecuteAvrdude(
                $"-c usbasp -p m328p -U flash:r:\"{flashPath}\":i -U eeprom:r:\"{eepromPath}\":i " +
                "-U lfuse:r:-:h -U hfuse:r:-:h -U efuse:r:-:h -U lock:r:-:h");

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            var fuses = ParseFuses(exitCode, stdout, output);

            // A backup without fuses restores the memories but not the chip: it would
            // come back running off a different clock or with ISP disabled. So it counts
            // as incomplete.
            //
            // Files with content, not files that exist: avrdude creates the output before
            // it talks to the chip, and a failed read leaves it at zero bytes.
            var complete = exitCode == 0
                           && fuses.Success
                           && files.All(f => new FileInfo(f) is { Exists: true, Length: > 0 });

            return new BackupResult(complete, output, files, fuses);
        }
        catch (Exception ex)
        {
            var error = $"Erro ao executar avrdude: {ex.Message}";
            return new BackupResult(false, error, files,
                new FusesResult(false, null, null, null, null, error));
        }
    }

    private static AvrdudeResult RunAvrdude(string args)
    {
        try
        {
            var (exitCode, stdout, stderr) = ExecuteAvrdude(args);

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            // avrdude returns code 0 only when the chip answers and the signature is
            // read/validated — a robust criterion, unlike hunting for strings in the
            // output.
            return new AvrdudeResult(exitCode == 0, output,
                ExtractSignature(output), ExtractDevice(output));
        }
        catch (Exception ex)
        {
            return new AvrdudeResult(false, $"Erro ao executar avrdude: {ex.Message}");
        }
    }

    private static FusesResult ReadFuses()
    {
        try
        {
            var (exitCode, stdout, stderr) = ExecuteAvrdude(FusesArgs);

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            return ParseFuses(exitCode, stdout, output);
        }
        catch (Exception ex)
        {
            return new FusesResult(false, null, null, null, null, $"Erro ao executar avrdude: {ex.Message}");
        }
    }

    /// <summary>
    /// Pulls the fuses out of the stdout of an avrdude that read them to "-" in
    /// hexadecimal: one value per read, in the order asked for — lfuse, hfuse, efuse,
    /// lock. avrdude's informational text goes to stderr, so stdout carries only these
    /// values.
    /// </summary>
    private static FusesResult ParseFuses(int exitCode, string stdout, string output)
    {
        var values = Regex.Matches(stdout, @"0x[0-9a-fA-F]{2}")
            .Select(m => m.Value.ToUpperInvariant())
            .ToList();

        if (exitCode == 0 && values.Count >= 3)
            return new FusesResult(true, values[0], values[1], values[2],
                values.Count >= 4 ? values[3] : null, output);

        return new FusesResult(false, null, null, null, null, output);
    }

    /// <summary>Pulls "1E 95 0F" out of "Device signature = 1E 95 0F (ATmega328P, ...)".</summary>
    private static string? ExtractSignature(string output)
    {
        var m = Regex.Match(output, @"Device signature\s*=\s*(?:0x)?([0-9A-Fa-f]{2}(?:\s+[0-9A-Fa-f]{2})*|[0-9A-Fa-f]{6})");
        return m.Success ? m.Groups[1].Value.Trim().ToUpperInvariant() : null;
    }

    /// <summary>Pulls "ATmega328P" out of the "AVR part : ATmega328P" line.</summary>
    private static string? ExtractDevice(string output)
    {
        var m = Regex.Match(output, @"AVR part\s*:\s*(\S+)");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static (int exitCode, string stdout, string stderr) ExecuteAvrdude(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "avrdude.exe",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("não foi possível iniciar o avrdude.");

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        return (proc.ExitCode, stdout, stderr);
    }
}
