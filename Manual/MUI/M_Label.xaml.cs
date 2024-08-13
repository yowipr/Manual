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
/// Lógica de interacción para M_Label.xaml
/// </summary>
public partial class M_Label : Label, IManualElement
{

    public void InitializeBind(Binding bind)
    {
        SetBinding(Label.ContentProperty, bind);
    }

    public IManualElement Clone()
    {
        return new M_Label(this.Content.ToString());
    }


    public M_Label()
    {
        InitializeComponent();
    }
    public M_Label(string labelText, bool isSubtitle = false)
    {
        InitializeComponent();
        Content = labelText;
        if (isSubtitle)
        {
            Margin = new Thickness(5,0,5,-5);
            AlignLeft();
        }
    }
    public void AlignLeft()
    {
        HorizontalAlignment = HorizontalAlignment.Left;
    }
    public void AlignCenter()
    {
        HorizontalAlignment = HorizontalAlignment.Center;
    }
    public void AlignRight()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
    }

}
