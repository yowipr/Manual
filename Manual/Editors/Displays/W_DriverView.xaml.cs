using Manual.Core;
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

namespace Manual.Editors.Displays;

/// <summary>
/// Lógica de interacción para W_DriverView.xaml
/// </summary>
public partial class W_DriverView : W_WindowContent 
{
    public W_DriverView(Driver driver)
    {
        InitializeComponent();
        DataContext = driver;
    }

    private void W_WindowContent_Loaded(object sender, RoutedEventArgs e)
    {
        window.Width = 256;
        window.Height = 100;
        window.PositionWindowAtMouse();

        if (((Driver)DataContext).target is INamable n)
            window.Title = $"Driver Property {n.Name}";
        else
           window.Title = $"Driver Property {((Driver)DataContext)?.targetPropertyName}";

        textbox.Focus();
        textbox.SelectAll();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Apply();
    }
    public Action onApplying;
    void Apply()
    {
        onApplying?.Invoke();

        var driver = ((Driver)DataContext);
        driver.Initialize();

        window.Close();

    }

    private void textbox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            var driver = ((Driver)DataContext);
            driver.ExpressionCode = textbox.Text;

            Apply();
            e.Handled = true;
        }
    }
}
