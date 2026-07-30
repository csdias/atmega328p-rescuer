using System.Text;
using ATMegaPestaV1.Services;

namespace ATMegaPestaV1.Backups;

/// <summary>
/// The sheet that goes with a backup: the fuse bits in hexadecimal, what they mean, and
/// the command that puts them back.
///
/// It lives in the Core, and not in the front end, because the restore command must not
/// diverge between them — a wrong value on that line closes ISP and the chip only comes
/// back through high voltage.
///
/// The sheet's text stays in Portuguese: it is what the student reads when they open the
/// backup folder.
/// </summary>
public static class FuseSheet
{
    /// <summary>
    /// Builds the sheet's text. The four bytes on their own say nothing to whoever opens
    /// the folder months later — the decoding is what makes the sheet useful.
    /// </summary>
    public static string Build(McuInfo mcu, string? signature, BackupResult backup, DateTime now)
    {
        var dec = FuseDecoder.Decode(backup.Fuses, mcu);
        var f = backup.Fuses;

        var sb = new StringBuilder();
        sb.AppendLine($"# Cópia de segurança do chip-alvo — {now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("# Escrita pelo ATMegaPesta V1. Só leitura: esta aplicação nunca grava fuses.");
        sb.AppendLine();
        sb.AppendLine($"MCU              = {mcu.Name}");
        sb.AppendLine($"Assinatura       = {signature ?? "n/d"}");
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
            sb.AppendLine($"Relógio          = {dec.Clock}");
            sb.AppendLine($"CKDIV8           = {(dec.Ckdiv8Enabled ? "activo — clock ÷8" : "desligado")}");
            sb.AppendLine($"Brown-out        = {dec.BrownOut}");
            sb.AppendLine($"ISP (SPIEN)      = {(dec.SpiEnabled ? "habilitado" : "desactivado")}");
            sb.AppendLine($"RESET (RSTDISBL) = {(dec.ResetEnabled ? "activo" : "desactivado")}");
            sb.AppendLine($"EEPROM em erase  = {(dec.EepromPreserved ? "preservada" : "apagada")}");
            sb.AppendLine($"Bootloader       = {dec.HighDescription}");
            sb.AppendLine($"Bloqueio         = {dec.LockLevel}");
        }

        sb.AppendLine();
        sb.AppendLine("# Memórias guardadas nesta pasta");
        foreach (var file in backup.Files)
            sb.AppendLine($"#   {Path.GetFileName(file)}");

        sb.AppendLine();
        sb.AppendLine("# Reposição dos fuses — por sua conta, não por esta aplicação:");
        sb.AppendLine($"#   avrdude -c usbasp -p m328p " +
                      $"-U lfuse:w:{f.Low}:m -U hfuse:w:{f.High}:m -U efuse:w:{f.Extended}:m");
        sb.AppendLine("# Um valor errado aqui fecha o ISP e o chip só volta por alta tensão.");
        sb.AppendLine("# O lock byte não se repõe por escrita: só um chip erase o devolve a 0xFF.");

        return sb.ToString();
    }
}
