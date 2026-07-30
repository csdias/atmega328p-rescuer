using System.Windows.Controls;

namespace ATMegaPestaV1.Views;

public partial class UnderConstructionView : UserControl
{
    public UnderConstructionView(string titulo)
    {
        InitializeComponent();
        TxtTitulo.Text = titulo;
    }
}
