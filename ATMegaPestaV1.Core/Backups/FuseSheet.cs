using System.Text;
using ATMegaPestaV1.Services;

namespace ATMegaPestaV1.Backups;

/// <summary>
/// The sheet that goes with a backup: the fuse bits in hexadecimal and what they mean.
/// It is a record of how the chip was, nothing more.
///
/// It deliberately does <em>not</em> carry a restore command. Putting an
/// <c>avrdude -U lfuse:w:…</c> line in a text file next to the .hex invites someone to
/// paste it by hand, and a wrong value there closes ISP for good — the chip then only
/// comes back through high-voltage programming. Restoring is the application's job,
/// where the values come from this same reading instead of being retyped.
///
/// It lives in the Core, and not in a front end, because the WPF and the API write the
/// same sheet and it must not drift between them.
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

        return sb.ToString();
    }
}
