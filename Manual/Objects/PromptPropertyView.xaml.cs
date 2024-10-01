using Manual.Core.Nodes;
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

namespace Manual.Objects;

/// <summary>
/// Lógica de interacción para PromptPropertyView.xaml
/// </summary>
public partial class PromptPropertyView : UserControl
{
    public PromptPropertyView()
    {
        InitializeComponent();

        namerText.OnNameChanging = NameChanging;

        Loaded += PromptPropertyView_Loaded;
    }

    private void PromptPropertyView_Loaded(object sender, RoutedEventArgs e)
    {
        var ep = DataContext as PromptProperty;
        if( ep != null && !ep.isInitialized)
        {
            namerText.EditName();
            ep.isInitialized = true;
        }
    }

    string NameChanging(string newName)
    {    
        var property = DataContext as PromptProperty;
        var prompt = property.Prompt;

        newName = newName.Replace(" ", "_");
        if (property.Name != newName)
        {
            var nam = Core.Namer.SetName(newName, prompt.Properties.Values);
            return nam;
        }
        else
            return newName;
    }

    private void Dots_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;

        // Verificar si el botón tiene un ContextMenu asignado
        if (button.ContextMenu != null)
        {
            // Asignar el ContextMenu al botón y abrirlo con clic izquierdo
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var m = sender as MenuItem;
        var header = (string)m.Header;
        var property = m.DataContext as PromptProperty;
        var prompt = property.Prompt;

        if(header == "Rename")
        {
            namerText.EditName();
           // Core.Output.Log($"{property.Value}");
        }
        else if (header == "Edit Element")
        {
            property.Element = new(MUI.ElementType.NumberBox);
        }
        else if (header == "Move Up")
        {
            prompt.MovePropertyUp(property);
        }
        else if (header == "Move Down")
        {
            prompt.MovePropertyDown(property);
        }
        else if(header == "Remove")
        {
            prompt.RemoveProperty(property);
        }

    }



}
