using System.Windows;

namespace ATMegaPestaV1.Views;

/// <summary>
/// A title, a body, and a way out.
///
/// With <paramref name="actionText"/> it grows a second button and becomes a question:
/// <c>DialogResult == true</c> means the action was chosen, anything else means the user
/// closed it — by the button, by Esc, or by the cross. Refusing is the default of a
/// dialog like this, so every way out that is not the action means no.
/// </summary>
public partial class MessageDialog : Window
{
    public MessageDialog(string title, string content, string? actionText = null)
    {
        InitializeComponent();

        TxtTitle.Text = title;
        TxtContent.Text = content;

        if (actionText is null)
            return;

        BtnAction.Content = actionText;
        BtnAction.Visibility = Visibility.Visible;

        // With something to decide, closing stops being the obvious move: the action takes
        // the default and Fechar keeps Esc.
        BtnAction.IsDefault = true;
        BtnClose.IsDefault = false;
    }

    // Fechar has no handler: IsCancel already closes the dialog and sets DialogResult.
    private void BtnAction_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
