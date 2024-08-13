using MLauncher.Sections;
using MLauncher.Core;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Reflection;
using System.IO;
using ManualToolkit.Windows;
using ManualToolkit.Generic;
using ManualToolkit.Specific;

namespace MLauncher;

/// <summary>
///              BUTTONS _0X AND BASIC WINDOW INTERACTION
/// </summary>
public partial class MainWindow : Window
{

    public enum TabButtonsType
    {
        _OX,
        _X,
        X
    }

    public static readonly DependencyProperty TabButtonsProperty =
        DependencyProperty.Register(nameof(TabButtons), typeof(TabButtonsType), typeof(MainWindow), new PropertyMetadata(TabButtonsType.X));

    public TabButtonsType TabButtons
    {
        get { return (TabButtonsType)GetValue(TabButtonsProperty); }
        set { SetValue(TabButtonsProperty, value); SetButtons(value); }
    }
    void SetButtons(TabButtonsType buttonType)
    {
        switch (buttonType)
        {
            case TabButtonsType._OX:
                WMin.Visibility = Visibility.Visible;
                WMax.Visibility = Visibility.Visible;
                WClose.Visibility = Visibility.Visible;
                break;
            case TabButtonsType._X:
                WMin.Visibility = Visibility.Visible;
                WMax.Visibility = Visibility.Collapsed;
                WClose.Visibility = Visibility.Visible;
                break;
            case TabButtonsType.X:
                WMin.Visibility = Visibility.Collapsed;
                WMax.Visibility = Visibility.Collapsed;
                WClose.Visibility = Visibility.Visible;
                break;
            default:
                break;
        }
    }



    private void WMin_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WMax_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WMax.Content = "▢";
        }
        else
        {
            WindowState = WindowState.Maximized;
            WMax.Content = "❏";
        }
    }

    private void WClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
        Application.Current.Shutdown();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Storyboard opacityAnimation = (Storyboard)FindResource("StartAnimation");
        opacityAnimation.Begin(this);

    }


}


/// <summary>
/// Lógica de interacción para MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public MainWindow()
    {
        // DESCOMENTA ESTO PARA SIMULAR QUE LO ABRES POR PRIMERA VEZ pd: old supongo
        //  Properties.Settings.Default.IsFirstRun = true;


        if (Properties.Settings.Default.IsFirstRun)
        {
            // Es la primera ejecución, realiza las tareas de configuración inicial.

            Properties.Settings.Default.IsFirstRun = false;
            Properties.Settings.Default.Save();
        }



        InitializeComponent();

        if (WindowState == WindowState.Maximized)
        {
            WMax.Content = "❏";
        }
        else
        {
            WMax.Content = "▢";
        }

        //DATACONTEXT
        topDownloading.DataContext = AppModel.launch;


        //  FirstStartup(); ya se utilizó una vez.
        AppModel.mainW = this;
        AppModel.GoToLoading();
        Launch.LoadSession();

        AppModel.launch.CheckUpdates();

        Focus();
        Topmost = true;
        Topmost = false;

        CenterWindowOnScreen();

        TaskBar.SetWindow(this);
        TaskBar.OnProgressChanged += TaskBar_OnProgressChanged;

        versionLabel.Content = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }

    private void TaskBar_OnProgressChanged(int progress, string message)
    {
        AppModel.launch.Progress = progress;
        AppModel.launch.MessageLog = message;
    }

    private void CenterWindowOnScreen()
    {
        double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
        double windowWidth = this.Width;
        double windowHeight = this.Height;
        this.Left = (screenWidth / 2) - (windowWidth / 2);
        this.Top = (screenHeight / 2) - (windowHeight / 2);
    }

}
