using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using Manual.Core.Nodes;
using CommunityToolkit.Mvvm.Input;
namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para ToolProperties.xaml
/// </summary>
public partial class ToolProperties : UserControl
{
    public ToolProperties()
    {
        InitializeComponent();

        toolManagerContext.DataContext = AppModel.project.toolManager;
        generationManagerContext.DataContext = GenerationManager.Instance;

        currentToolContext.DataContextChanged += CurrentToolContext_DataContextChanged;
     
      //  AsignCheckpointContext();
    }

    //DISABLED

    //void AsignCheckpointContext()
    //{
    //    gridContext.DataContextChanged += GridContext_DataContextChanged;
    //    gridContext.AddContext(
    //        new NodeContext(n => n.NameType == "CheckpointLoaderSimple", f => f.Name == "ckpt_name"),
    //        new NodeContext("Principled Latent", "Model")
    //        );;
    //}

    //private void GridContext_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    //{
    // if(e.NewValue is NodeOption nodeop)
    //    {
    //        var element = nodeop.FieldElement.Invoke();
    //        ckptBox.Content = element;
    //    }   
    //}

    private void CurrentToolContext_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) // instantiate dool
    {
        if (e.NewValue is Tool tool)
        {
            contentBody.Content = tool.body;//.InitializeBody();
        }
    }

    private async void preset_Drop(object sender, DragEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        await GenerationManager.ImportDropWorkflow(e);
        Mouse.OverrideCursor = null;
    }



    private void ContextMenu_Click(object sender, RoutedEventArgs e)
    {
        MenuItem clickedItem = sender as MenuItem;
        string header = clickedItem.Header.ToString();
        if(header == "Open in Latent Nodes Editor")
        {
            Mouse.OverrideCursor = Cursors.Wait;
            EditorsSpace.instance.NewEditorWindow(Core.Editors.Latent_Nodes_Editor);
            Mouse.OverrideCursor = null;
        }
        else if(header == "Duplicate")
        {
            GenerationManager.Instance.DuplicatePrompt();
        }
        else if(header == "Refresh")
        {
            GenerationManager.RefreshPromptPreset();
        }
        else if(header == "Delete")
        {
            GenerationManager.Instance.DeletePrompt();
        }
    }

    private void Templates_OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is MenuItemNode menuItemNode)
        {
            menuItemNode.DoAction();
        }
    }


    private void PromptPreset_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;
        if (button != null && button.ContextMenu != null)
        {
            // Open the context menu with the left mouse button click
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }
}

public partial class ED_ToolProperties : Editor
{
    [ObservableProperty] bool enablePalette = true;
    
    public ED_ToolProperties()
    {

    }
    [RelayCommand]
    void SwitchPalette()
    {
        EnablePalette = !EnablePalette;
    }
}


