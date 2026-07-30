using System.Windows;

namespace ATMegaPestaV1.Views;

/// <summary>What the user answered to the transfer warning.</summary>
public enum TransferChoice
{
    /// <summary>Do not proceed. The value for whoever closes the window by the cross or by Esc.</summary>
    Cancel,

    /// <summary>Save Flash and EEPROM to file before proceeding.</summary>
    ProceedWithBackup,

    /// <summary>Proceed and accept losing what is on the chip.</summary>
    ProceedWithoutBackup
}

/// <summary>
/// Warning shown before the integrity check. It exists because the check is not a read:
/// the target chip has to receive the verifier application, which replaces whatever the
/// student has on it. This is the last chance to keep a backup.
/// </summary>
public partial class VerifierTransferDialog : Window
{
    /// <summary>
    /// The answer. Stays at <see cref="TransferChoice.Cancel"/> if the window is closed
    /// without a choice — refusing is always the default value of a warning like this.
    /// </summary>
    public TransferChoice Choice { get; private set; } = TransferChoice.Cancel;

    public VerifierTransferDialog()
    {
        InitializeComponent();
    }

    private void BtnWithBackup_Click(object sender, RoutedEventArgs e) =>
        Answer(TransferChoice.ProceedWithBackup);

    private void BtnWithoutBackup_Click(object sender, RoutedEventArgs e) =>
        Answer(TransferChoice.ProceedWithoutBackup);

    private void BtnCancel_Click(object sender, RoutedEventArgs e) =>
        Answer(TransferChoice.Cancel);

    private void Answer(TransferChoice choice)
    {
        Choice = choice;
        DialogResult = choice != TransferChoice.Cancel;
    }
}
