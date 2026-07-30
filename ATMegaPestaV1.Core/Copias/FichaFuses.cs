using System.Text;
using ATMegaPestaV1.Services;

namespace ATMegaPestaV1.Copias;

/// <summary>
/// A ficha que acompanha uma cópia de segurança: os fuse bits em hexadecimal, o que eles
/// significam, e o comando que os repõe.
///
/// Vive no Core, e não no front end, porque o comando de reposição não pode divergir
/// entre eles — um valor errado nessa linha fecha o ISP e o chip só volta por alta tensão.
/// </summary>
public static class FichaFuses
{
    /// <summary>
    /// Constrói o texto da ficha. Os quatro bytes por si só não dizem nada a quem abrir a
    /// pasta meses depois — a descodificação é o que torna a ficha útil.
    /// </summary>
    public static string Construir(McuInfo mcu, string? assinatura, CopiaResult copia, DateTime agora)
    {
        var dec = FuseDecoder.Descodificar(copia.Fuses, mcu);
        var f = copia.Fuses;

        var sb = new StringBuilder();
        sb.AppendLine($"# Cópia de segurança do chip-alvo — {agora:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("# Escrita pelo ATMegaPesta V1. Só leitura: esta aplicação nunca grava fuses.");
        sb.AppendLine();
        sb.AppendLine($"MCU              = {mcu.Nome}");
        sb.AppendLine($"Assinatura       = {assinatura ?? "n/d"}");
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
            sb.AppendLine($"#   {Path.GetFileName(ficheiro)}");

        sb.AppendLine();
        sb.AppendLine("# Reposição dos fuses — por sua conta, não por esta aplicação:");
        sb.AppendLine($"#   avrdude -c usbasp -p m328p " +
                      $"-U lfuse:w:{f.Low}:m -U hfuse:w:{f.High}:m -U efuse:w:{f.Extended}:m");
        sb.AppendLine("# Um valor errado aqui fecha o ISP e o chip só volta por alta tensão.");
        sb.AppendLine("# O lock byte não se repõe por escrita: só um chip erase o devolve a 0xFF.");

        return sb.ToString();
    }
}
