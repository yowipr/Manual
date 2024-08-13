using Manual.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
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
/// Lógica de interacción para W_Editor.xaml
/// </summary>
public partial class W_Editor : W_WindowContent
{
    public W_Editor()
    {
        InitializeComponent();
       
        Loaded += W_Editor_Loaded;

    }

    private void W_Editor_Loaded(object sender, RoutedEventArgs e)
    {

        var ed = (WorkspaceFloatingEditor)DataContext;
        window.Width = ed.Width;
        window.Height = ed.Height;
        window.Left = ed.Left;
        window.Top = ed.Top;

        window.SizeChanged += Window_SizeChanged;
        window.LocationChanged += Window_LocationChanged;
        window.WClose.Click += WClose_Click;
    }



    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        var ed = (WorkspaceFloatingEditor)DataContext;
        if (window != null && ed != null)
        {
            var mainWindowLeft = AppModel.mainW.ActualWidth;
            var mainWindowTop = AppModel.mainW.ActualHeight;

            // Calcula los valores relativos en función del tamaño nuevo de W_Editor y el tamaño actual de MainWindow.
            ed.RelativeLeft = window.Left / mainWindowLeft;
            ed.RelativeTop = window.Top / mainWindowTop;

        }
    }


    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var ed = (WorkspaceFloatingEditor)DataContext;
        if (window != null && ed != null && AppModel.mainW != null)
        {
            // Obtiene las dimensiones actuales de MainWindow.
            var mainWindowWidth = AppModel.mainW.ActualWidth;
            var mainWindowHeight = AppModel.mainW.ActualHeight;

            // Calcula los valores relativos en función del tamaño nuevo de W_Editor y el tamaño actual de MainWindow.
            ed.RelativeWidth = e.NewSize.Width / mainWindowWidth;
            ed.RelativeHeight = e.NewSize.Height / mainWindowHeight;
        }
    }

    public void UpdateSize()
    {
        //var ed = (WorkspaceFloatingEditor)DataContext;
        //window.Width = ed.Width;
        //window.Height = ed.Height;
    }
    public void UpdateLocation()
    {
        //var ed = (WorkspaceFloatingEditor)DataContext;
        //window.Top = ed.Top;
        //window.Left = ed.Left;
    }


    private void WClose_Click(object sender, RoutedEventArgs e)
    {
        var ed = (WorkspaceFloatingEditor)DataContext;
        var ated = ed.AttachedSpace as WorkspaceSingle;

        ated.RemoveFloatEditor(ed);
        base.Close_Click(sender, e);
    }




}
