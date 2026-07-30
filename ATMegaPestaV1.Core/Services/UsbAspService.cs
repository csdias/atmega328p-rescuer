using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Implementação real do UsbAspService via avrdude.exe.
/// </summary>
public class UsbAspService : IUsbAspService
{
    // -v para o avrdude imprimir o nome/assinatura do dispositivo (na verbosidade
    // por defeito o avrdude 8.1 não o faz).
    private const string AvrdudeArgs = "-c usbasp -p m328p -v";

    // Leitura dos fuses e do lock byte para stdout em hexadecimal (o texto
    // informativo vai para stderr). Só leitura — nunca se escrevem fuses.
    private const string FusesArgs =
        "-c usbasp -p m328p -U lfuse:r:-:h -U hfuse:r:-:h -U efuse:r:-:h -U lock:r:-:h";

    public Task<AvrdudeResult> DetectarAssinaturaAsync(CancellationToken ct = default) =>
        Task.Run(() => CorrerAvrdude(AvrdudeArgs), ct);

    public Task<AvrdudeResult> RetryIspAsync(CancellationToken ct = default) =>
        Task.Run(() => CorrerAvrdude(AvrdudeArgs), ct);

    public Task<FusesResult> LerFusesAsync(CancellationToken ct = default) =>
        Task.Run(LerFuses, ct);

    public Task<CopiaResult> GuardarCopiaAsync(string caminhoFlash, string caminhoEeprom,
                                               CancellationToken ct = default) =>
        Task.Run(() => GuardarCopia(caminhoFlash, caminhoEeprom), ct);

    /// <summary>
    /// Uma só invocação para as duas memórias e os fuses: o avrdude faz um "program
    /// enable" por execução, e execuções separadas dariam ao chip várias oportunidades
    /// de não responder para uma cópia que só vale inteira. As memórias vão para
    /// ficheiros, os fuses para stdout — que fica assim só com eles.
    /// </summary>
    private static CopiaResult GuardarCopia(string caminhoFlash, string caminhoEeprom)
    {
        var ficheiros = new[] { caminhoFlash, caminhoEeprom };

        try
        {
            var (exitCode, stdout, stderr) = ExecutarAvrdude(
                $"-c usbasp -p m328p -U flash:r:\"{caminhoFlash}\":i -U eeprom:r:\"{caminhoEeprom}\":i " +
                "-U lfuse:r:-:h -U hfuse:r:-:h -U efuse:r:-:h -U lock:r:-:h");

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            var fuses = InterpretarFuses(exitCode, stdout, output);

            // Uma cópia sem fuses repõe as memórias mas não o chip: voltaria a correr
            // com outro relógio ou com o ISP fechado. Por isso conta como incompleta.
            //
            // Ficheiros com conteúdo, não ficheiros existentes: o avrdude cria a saída
            // antes de falar com o chip, e uma leitura falhada deixa-a com zero bytes.
            var completa = exitCode == 0
                           && fuses.Success
                           && ficheiros.All(f => new FileInfo(f) is { Exists: true, Length: > 0 });

            return new CopiaResult(completa, output, ficheiros, fuses);
        }
        catch (Exception ex)
        {
            var erro = $"Erro ao executar avrdude: {ex.Message}";
            return new CopiaResult(false, erro, ficheiros,
                new FusesResult(false, null, null, null, null, erro));
        }
    }

    private static AvrdudeResult CorrerAvrdude(string args)
    {
        try
        {
            var (exitCode, stdout, stderr) = ExecutarAvrdude(args);

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            // O avrdude devolve código 0 apenas quando o chip responde e a
            // assinatura é lida/validada — critério robusto, ao contrário de
            // procurar strings na saída.
            return new AvrdudeResult(exitCode == 0, output,
                ExtrairAssinatura(output), ExtrairDispositivo(output));
        }
        catch (Exception ex)
        {
            return new AvrdudeResult(false, $"Erro ao executar avrdude: {ex.Message}");
        }
    }

    private static FusesResult LerFuses()
    {
        try
        {
            var (exitCode, stdout, stderr) = ExecutarAvrdude(FusesArgs);

            var output = (stdout + stderr).Trim();
            if (output.Length == 0)
                output = "Sem resposta do avrdude.";

            return InterpretarFuses(exitCode, stdout, output);
        }
        catch (Exception ex)
        {
            return new FusesResult(false, null, null, null, null, $"Erro ao executar avrdude: {ex.Message}");
        }
    }

    /// <summary>
    /// Extrai os fuses do stdout de um avrdude que os leu para "-" em hexadecimal: um
    /// valor por leitura, na ordem pedida — lfuse, hfuse, efuse, lock. O texto
    /// informativo do avrdude vai para stderr, pelo que o stdout traz só estes valores.
    /// </summary>
    private static FusesResult InterpretarFuses(int exitCode, string stdout, string output)
    {
        var valores = Regex.Matches(stdout, @"0x[0-9a-fA-F]{2}")
            .Select(m => m.Value.ToUpperInvariant())
            .ToList();

        if (exitCode == 0 && valores.Count >= 3)
            return new FusesResult(true, valores[0], valores[1], valores[2],
                valores.Count >= 4 ? valores[3] : null, output);

        return new FusesResult(false, null, null, null, null, output);
    }

    /// <summary>Extrai "1E 95 0F" de "Device signature = 1E 95 0F (ATmega328P, ...)".</summary>
    private static string? ExtrairAssinatura(string output)
    {
        var m = Regex.Match(output, @"Device signature\s*=\s*(?:0x)?([0-9A-Fa-f]{2}(?:\s+[0-9A-Fa-f]{2})*|[0-9A-Fa-f]{6})");
        return m.Success ? m.Groups[1].Value.Trim().ToUpperInvariant() : null;
    }

    /// <summary>Extrai "ATmega328P" da linha "AVR part : ATmega328P".</summary>
    private static string? ExtrairDispositivo(string output)
    {
        var m = Regex.Match(output, @"AVR part\s*:\s*(\S+)");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static (int exitCode, string stdout, string stderr) ExecutarAvrdude(string args)
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
