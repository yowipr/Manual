using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using System.Diagnostics;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MLauncher.Core;
using ManualToolkit.Windows;
using System.IO;

namespace MLauncher.Sections;

/// <summary>
/// Lógica de interacción para Login.xaml
/// </summary>
public partial class LoginView : UserControl
{

    public LoginView()
    {
        InitializeComponent();
    }

    private void Button_ClickLogin(object sender, RoutedEventArgs e)
    {
        loginBtn.Visibility = Visibility.Collapsed;
        loginLoading.Visibility = Visibility.Visible;
        btnLink.Visibility = Visibility.Visible;
        AppModel.login.LogInWeb();
    }

    private void Button_ClickGoLaunch(object sender, RoutedEventArgs e)
    {
        AppModel.GoToLaunch();
    }

    private void Button_Click_Link(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(AppModel.login.url);
        copiedText.Visibility = Visibility.Visible;
    }
}
