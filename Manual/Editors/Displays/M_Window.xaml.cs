using Manual.API;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Manual.Editors.Displays;

/// <summary>
/// The content of a M_Window
/// </summary>
public partial class W_WindowContent : UserControl
{
    ActionHistory.DoAction asignWindow = new();

    private M_Window _window;
    public M_Window window
    {
        get => _window;
        set
        {
            _window = value;
            asignWindow.DoOnce();
        }
    }

    public W_WindowContent()
    {
        asignWindow.action = OnWindowAsigned;
    }
    void OnWindowAsigned()
    {
        window.Closed += Window_Closed;
    }

    public virtual void Window_Closed(object? sender, EventArgs e)
    {
        if(AppModel.mainW != null)
        AppModel.mainW.FadeIn();

        if (AppModel.mainW != null)
           AppModel.mainW.IsEnabled = true;
    }


    /// <summary>
    /// close the window with animation
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public virtual void Close_Click(object sender, RoutedEventArgs e)
    {
        var op = AnimationLibrary.Opacity(window, 0, 0.3);
        op.OnFinalized(window.Close);
        op.Begin();

        var m = AnimationLibrary.Translate(window, 0, 20, 0.3, 0, 0);
        m.Begin();

      
    }

    public virtual void OnWindowOpening()
    {

    }


}



/// <summary>
///              BUTTONS _0X AND BASIC WINDOW INTERACTION
/// </summary>
public partial class M_Window : Window
{

    public enum TabButtonsType
    {
        _OX,
        _X,
        OX,
        X,
        None,
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
            case TabButtonsType.OX:
                WMin.Visibility = Visibility.Collapsed;
                WMax.Visibility = Visibility.Visible;
                WClose.Visibility = Visibility.Visible;
                break;
            case TabButtonsType.X:
                WMin.Visibility = Visibility.Collapsed;
                WMax.Visibility = Visibility.Collapsed;
                WClose.Visibility = Visibility.Visible;
                break;
            case TabButtonsType.None:
                WMin.Visibility = Visibility.Collapsed;
                WMax.Visibility = Visibility.Collapsed;
                WClose.Visibility = Visibility.Collapsed;
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
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void WClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
        if(CustomContent != null)
          ((W_WindowContent)CustomContent).Close_Click(sender, e);

        if(Application.Current.MainWindow == this)
            Application.Current.Shutdown();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Storyboard opacityAnimation = (Storyboard)FindResource("StartAnimation");
        opacityAnimation.Begin(this);
    }

    private void onDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        double xadjust = Width + e.HorizontalChange;
        double yadjust = Height + e.VerticalChange;

        if (xadjust > MinWidth)
            Width = xadjust;
        if (yadjust > MinHeight)
            Height = yadjust;




        //var cont = CustomContent as W_WindowContent;
        //if (cont != null)
        //{
        //    xadjust = cont.Width + e.HorizontalChange;
        //    yadjust = cont.Height + e.VerticalChange;

        //    if (xadjust > MinWidth)
        //        cont.Width = xadjust;
        //    if (yadjust > MinHeight)
        //        cont.Height = yadjust;
        //}
    }
}



/// <summary>
/// Lógica de interacción para M_Window.xaml
/// </summary>
public partial class M_Window : Window
{
    public static readonly DependencyProperty CustomContentProperty = DependencyProperty.Register(
        nameof(CustomContent),
        typeof(object),
        typeof(M_Window),
        new PropertyMetadata(null));

    public bool fadeMain;

    public object CustomContent
    {
        get { return GetValue(CustomContentProperty); }
        set { SetValue(CustomContentProperty, value); }
    }


    //NO BORRAR, SÍ SE USAN, SE HEREDAN DE M_MessageBox
    public M_Window()
    {
        InitializeComponent();
    }
    public M_Window(object content)
    {
        InitializeComponent();

        Owner = AppModel.mainW;
        CustomContent = content;
    }
    public M_Window(object content, string title)
    {
        InitializeComponent();

        Owner = AppModel.mainW;
        titleWindow.Text = title;
        CustomContent = content;
    }
    public M_Window(UserControl content, string title)
    {
        InitializeComponent();

        Owner = AppModel.mainW;
        titleWindow.Text = title;
        CustomContent = content;
    }
    public M_Window(object content, string title, TabButtonsType buttons)
    {
        InitializeComponent();

        Owner = AppModel.mainW;
        Title = title;
        TabButtons = buttons;
        CustomContent = content;

        if(content is FrameworkElement fe)
        {
            Width = fe.Width + 15;
            Height = fe.Height + 25 + 15;
        }
   
    }

    public void CenterToScreen()
    {
        // Establece la ubicación de inicio de la ventana en Manual para usar valores personalizados
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        //// Obtiene las dimensiones de la pantalla principal
        //double screenWidth = SystemParameters.PrimaryScreenWidth;
        //double screenHeight = SystemParameters.PrimaryScreenHeight;
        //double windowWidth = this.Width;
        //double windowHeight = this.Height;

        //// Calcula la posición X e Y para centrar la ventana
        //this.Left = (screenWidth / 2) - (windowWidth / 2);
        //this.Top = (screenHeight / 2) - (windowHeight / 2);
    }

    public void SetCenterToScreen()
    {
        // Establece la ubicación de inicio de la ventana en Manual para usar valores personalizados
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // Obtiene las dimensiones de la pantalla principal
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        double windowWidth = this.Width;
        double windowHeight = this.Height;

        // Calcula la posición X e Y para centrar la ventana
        this.Left = (screenWidth / 2) - (windowWidth / 2);
        this.Top = (screenHeight / 2) - (windowHeight / 2);
    }

    public static void Show(W_WindowContent windowDisplay)
    {
        Show(windowDisplay, "", TabButtonsType.X);
    }
    public static void Show(W_WindowContent windowDisplay, string title)
    {
        Show(windowDisplay, title, TabButtonsType.X);
    }
    public static void Show(W_WindowContent windowDisplay, string title, TabButtonsType buttons)
    {
        var w = new M_Window(windowDisplay, title, buttons);
        windowDisplay.window = w;
        windowDisplay.OnWindowOpening();

        w.Opacity = 0;
        w.ShowDialog();
        var op = AnimationLibrary.Opacity(w, 1, 0.3);
        op.Begin();
        var m = AnimationLibrary.Translate(w, 0, 0, 0.3, 0, 20);
        m.Begin();
    }

    /// <summary>
    /// uses Show instead of ShowDialog
    /// </summary>
    /// <param name="windowDisplay"></param>
    /// <param name="title"></param>
    /// <param name="buttons"></param>
    public static M_Window NewShow(W_WindowContent windowDisplay, string title, TabButtonsType buttons, bool showInTaskbar = true)
    {
        var w = new M_Window(windowDisplay, title, buttons);
        w.CenterToScreen();
        w.fadeMain = false;
        w.ShowInTaskbar = showInTaskbar;
        windowDisplay.window = w;
        windowDisplay.OnWindowOpening();

        w.Opacity = 0;
        w.Show();
        var op = AnimationLibrary.Opacity(w, 1, 0.3);
        op.Begin();
        var m = AnimationLibrary.Translate(w, 0, 0, 0.3, 0, 20);
        m.Begin();

        return w;
    }

  
    public static M_Window NewShow(W_WindowContent windowDisplay, string title = "")
    {
        return M_Window.NewShow(windowDisplay, title, M_Window.TabButtonsType.X, false);
    }


    public void PositionWindowAtMouse()
    {
        // Obtiene la posición del mouse respecto a la pantalla
        var mousePosition = GetMousePositionScreen();

        // Mueve la ventana a la posición del mouse
        this.Left = mousePosition.X - (this.Width / 2);  // Centrar horizontalmente
        this.Top = mousePosition.Y - (this.Height);  // Centrar verticalmente
    }

    public static Point GetMousePositionScreen()
    {
        var point = Mouse.GetPosition(AppModel.mainW);
        return AppModel.mainW.PointToScreen(point);
    }







}