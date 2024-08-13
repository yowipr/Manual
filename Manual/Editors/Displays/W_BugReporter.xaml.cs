using Manual.API;
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
/// Lógica de interacción para W_BugReporter.xaml
/// </summary>
public partial class W_BugReporter : W_WindowContent
{
    public W_BugReporter()
    {
        InitializeComponent();

       // window.Closed += Window_Closed1;
    }

    private void Window_Closed1(object? sender, EventArgs e)
    {
      //  window.Closed -= Window_Closed1;
      //  if (AppModel.mainW == null)
        //    Application.Current.Shutdown();          
    }

    public W_BugReporter(Exception ex)
    {
        InitializeComponent();
        Ex = ex;
        string message = $"{ex.Message}\n{ex.Source}";
        exTxt.Text = message;
        Core.Output.Error(ex.Message, ex.Source);

     //   if (Core.Output.DEBUG_BUILD())
       //     Core.Output.LogEx(ex);
    }
    Exception Ex;
    private void SendReport(object sender, RoutedEventArgs e)
    {
        var report = new BugReport(Ex, txt.Text);
        BugReporter.Send(report);
        Close_Click(sender, e);
    }
    public override void Close_Click(object sender, RoutedEventArgs e)
    {
        base.Close_Click(sender, e);
    }
}
