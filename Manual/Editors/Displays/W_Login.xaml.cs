using Manual.Core;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
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
/// Lógica de interacción para W_Login.xaml
/// </summary>
public partial class W_Login : W_WindowContent
{
    public static W_Login OpenedLogin;

    public W_Login()
    {
        InitializeComponent();

        DataContext = AppModel.userManager;

        OpenedLogin = this;

        if (AppModel.IsForceLogin)
            closeBtn.Visibility = Visibility.Visible;
    }



    private void Button_Click(object sender, RoutedEventArgs e)
    {
        LogInWeb();
    }

    public static string url = $"{Constants.WebURL}/?fromLauncher=yes";
    public void LogInWeb()
    {
        WebManager.OPEN(url);
    }


    public override void Close_Click(object sender, RoutedEventArgs e)
    {
        // base.Close_Click(sender, e);
        // OnClose();

        if(AppModel.IsForceLogin)
           Application.Current.Shutdown();
    }


    public override void Window_Closed(object? sender, EventArgs e)
    {
        base.Window_Closed(sender, e);
        OnClose();
    }


    void OnClose()
    {
        OpenedLogin = null;
    }

}
