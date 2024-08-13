using Manual.Core;
using Manual.MUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para M_Checkbox.xaml
/// </summary>
public partial class M_CheckBox : UserControl, IManualElement
{
    public IManualElement Clone()
    {

        var clone = new M_CheckBox(Header);
     
        var bind = AppModel.CloneBinding(this, IsCheckedProperty);
        clone.InitializeBind(bind);

        return clone;
    }

    public void InitializeBind(Binding bind)
    {
        if (bind is null)
            return;

        SetBinding(IsCheckedProperty, bind);
    }


    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
    nameof(Header),
    typeof(string),
    typeof(M_CheckBox), // Reemplaza 'TuControl' con el nombre real de tu clase
    new PropertyMetadata(string.Empty)); // Puedes cambiar el valor predeterminado si lo deseas

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
    nameof(IsChecked),
    typeof(bool),
    typeof(M_CheckBox), // Reemplaza 'TuControl' con el nombre real de tu clase
    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    public M_CheckBox()
    {
        ContentStringFormat = "{}{0}";

        InitializeComponent();
    }

    public M_CheckBox(string header)
    {
        ContentStringFormat = "{}{0}";

        InitializeComponent();
        Header = header;
    }
    public M_CheckBox(string header, string binding)
    {
        ContentStringFormat = "{}{0}";

        InitializeComponent();
        Header = header;
        SetBinding(M_CheckBox.IsCheckedProperty, binding);

    }
}
