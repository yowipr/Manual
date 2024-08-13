using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manual.API;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para Tools.xaml
/// </summary>
public partial class Tools : UserControl//, IRecipient<NotifyMessage>
{

    public Tools()
    {
        InitializeComponent();

        DataContext = AppModel.project.toolManager;

        //   DataContext = AppModel.project.regionSpace.ed_tools;

       // WeakReferenceMessenger.Default.Register(this);
    }

      RadioButton oldRd;
  
    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
      //  AppModel.project.toolManager.CurrentToolSpace.current = false;
      
        RadioButton rd = sender as RadioButton;
        Tool t = rd.DataContext as Tool;

        AppModel.project.toolManager.CurrentToolSpace = t;

        // ChangeTool();
    }



    //public void Receive(NotifyMessage message)
    //{
    //    Tool t = message.Value as Tool;
    //    ChangeTool();
    //    MessageBox.Show("hola");
    //}



    //void ChangeTool(Tool tool, RadioButton rd)
    //{
    //    AppModel.project.toolManager.CurrentToolSpace = tool;

    //    if (oldRd != null && rd != oldRd)
    //        oldRd.IsChecked = false;

    //    oldRd = rd;
    //}



    //private void ChangeTool()
    //{
      
    //}


}


public partial class ED_Tools : Editor
{

    public ED_Tools()
    {
    
    }
}


public class CurrentToolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Verificar si los valores coinciden y determinar si el RadioButton debe estar seleccionado
        if (values.Length == 2 && values[0] is Tool tool && values[1] is Tool currentTool)
        {
            return tool == currentTool;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
       // return null;
    }
}

