namespace ATMegaPestaV1.Services;

/// <summary>
/// Resultado de uma operação avrdude ISP.
/// <paramref name="Assinatura"/> e <paramref name="Dispositivo"/> vêm do output do
/// avrdude quando o chip responde (ex.: "1E 95 0F" e "ATmega328P").
/// </summary>
public record AvrdudeResult(bool Success, string Output, string? Assinatura = null, string? Dispositivo = null);

/// <summary>
/// Resultado da leitura dos fuse bits (lfuse/hfuse/efuse) e do lock byte.
/// Os valores vêm em hexadecimal (ex.: "0xFF") ou null se a leitura falhou.
/// </summary>
public record FusesResult(bool Success, string? Low, string? High, string? Extended, string? Lock, string Output);

/// <summary>
/// Resultado de uma cópia de segurança do chip-alvo.
/// </summary>
/// <param name="Ficheiros">Caminhos escritos, para se poder dizer ao utilizador onde ficaram.</param>
/// <param name="Fuses">
/// Os fuse bits como estavam no momento da cópia. Sem eles a cópia não repõe o chip:
/// as memórias voltam a lá estar mas a correr com outro relógio, outro brown-out, ou
/// com o ISP fechado.
/// </param>
public record CopiaResult(bool Success, string Output, IReadOnlyList<string> Ficheiros, FusesResult Fuses);

/// <summary>
/// Wrapper sobre o avrdude para comunicar com o chip-alvo via USBAsp.
/// </summary>
public interface IUsbAspService
{
    /// <summary>
    /// Lê a Flash, a EEPROM e os fuse bits do chip-alvo. As memórias vão para ficheiros
    /// Intel HEX; os fuses voltam no resultado, para quem chama os poder registar.
    /// Continua a ser só leitura: é o que se guarda antes de a verificação de integridade
    /// escrever sobre o que o aluno tem no chip.
    /// </summary>
    Task<CopiaResult> GuardarCopiaAsync(string caminhoFlash, string caminhoEeprom,
                                        CancellationToken ct = default);

    /// <summary>
    /// Tenta aceder ao chip-alvo: avrdude -c usbasp -p m328p.
    /// Devolve o output completo e se foi bem-sucedido (signature lida).
    /// </summary>
    Task<AvrdudeResult> DetectarAssinaturaAsync(CancellationToken ct = default);

    /// <summary>
    /// Re-tenta o ISP após clock injection: avrdude -c usbasp -p m328p.
    /// </summary>
    Task<AvrdudeResult> RetryIspAsync(CancellationToken ct = default);

    /// <summary>
    /// Lê os fuse bits do chip-alvo (lfuse, hfuse, efuse) via avrdude.
    /// </summary>
    Task<FusesResult> LerFusesAsync(CancellationToken ct = default);
}
