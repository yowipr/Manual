using Manual.API;
using Manual.Core;
using Manual.Core.Nodes;
using Manual.Core.Nodes.ComfyUI;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ManualToolkit.Generic;
using Manual.Objects;
using CommunityToolkit.Mvvm.ComponentModel;


namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para LatentNodesView.xaml
/// </summary>
public partial class LatentNodesView : UserControl
{
    public LatentNodesView()
    {
        InitializeComponent();

        searchBox.CloseDoAction = CloseSearch;
        canvas.OnEndSelect = EndSelect;

    }

    void EndSelect(Rect selection)
    {
        List<LatentNode> nodesIntersect = new();
        foreach (var node in ManualAPI.SelectedPreset.LatentNodes)
        {
            var nodeRect = new Rect(node.Position, node.Scale);
            if (selection.IntersectsWith(nodeRect))
            {
                nodesIntersect.Add(node);
            }
        }

        bool clear = !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        ManualAPI.SelectedPreset.SelectedNodes.Select(nodesIntersect, clear);
       
    }



    public void OpenSearchMenuBox()
    {
        if (ManualAPI.SelectedPreset != null)
        {
            searchPopup.IsOpen = true;
            ManualAPI.SelectedPreset.newNodePosition = canvas.mousePosition();
            searchBox.Open();
        }
    }
    private void CloseSearch()
    {
        searchPopup.IsOpen = false;
    }
    private void searchPopup_Closed(object sender, EventArgs e)
    {
        ManualAPI.SelectedPreset.newNodeConnect = null;
    }

    private void canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (canvas.HitTest(e) is Canvas)
        {

            if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed && GenerationManager.Instance.SelectedPreset != null && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                GenerationManager.Instance.SelectedPreset.SelectedNodes.Clear();

            else if (e.ClickCount == 2)
            {
                OpenSearchMenuBox();
                e.Handled = true;

            }

        }
    }


    private void canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete) // Key.Back es para la tecla Supr en algunos teclados
            GenerationManager.Instance.SelectedPreset.DeleteSelectedNodes();
        else if (e.Key == Key.F3)
            OpenSearchMenuBox();
        else if (e.Key == Key.Escape)
            CloseSearch();
    }

    private async void Canvas_Drop(object sender, DragEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        //IMPORT WORKFLOW OR PROMPT
        await GenerationManager.ImportDropWorkflow(e, canvas);
        Mouse.OverrideCursor = null;
       
    }



    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        GenerationManager.Generate();

    }

    // ADD menuitems
    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is MenuItemNode menuItemNode && menuItemNode.Factory != null)
        {
            // Realizar acción, como instanciar un nodo
            LatentNode latentNode = menuItemNode.Factory();
            // ... hacer algo con latentNode ... 
            if(latentNode != null)
               GenerationManager.Instance.SelectedPreset.AddNode(latentNode);

        }
    }

    private void Templates_OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is MenuItemNode menuItemNode)
        {
            menuItemNode.DoAction();
        }
    }



    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;

        if (header == "Refresh")
        {
            GenerationManager.RefreshPromptPreset();
        }

        //testing
        else if (header == "Test")
        {

        }

        else if (header == "Automatic Drivers")
        {
            ManualAPI.SelectedPreset.AutomaticDrivers();
        }
        else if (header == "Delete All Drivers")
        {
            ManualAPI.SelectedPreset.ClearDrivers();
        }
    }





    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (Shortcuts.IsTextBoxFocus)
            return;

        if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.C)
        {
            var nodes = Node.Copy(ManualAPI.SelectedPreset.SelectedNodes);
            ManualClipboard.Copy(nodes);
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.V)
        {
            var pasted = ManualClipboard.Paste();
            if (pasted is List<Node> nodes)
            {
                var newSelect = Node.PasteNodes(nodes, ManualAPI.SelectedPreset);
                ManualAPI.SelectedPreset.SelectedNodes.Select(newSelect);
            }

            
        }
    }

    private void RightClickMenu(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;

        var nodes = ManualAPI.SelectedPreset.SelectedNodes;
        if (header == "Wrap Nodes")
        {
            WrapperNode.AddWrapper(ManualAPI.SelectedPreset, nodes);
        }
        else if (header == "Connect By Pass")
        {
            if (nodes.Count == 2)
            {
                nodes[0].ConnectByPass(nodes[1]);
            }
            else
                Core.Output.Log("Select only 2 nodes to connect by pass");

        }
        if (header == "Delete Node")
        {
            nodes.Delete();
            return;
        }


        //COLOR
        var parent = s.Parent as MenuItem;
        if (parent == null) return;

        string parentheader = (string)parent.Header;

        if (parentheader == "Color")
        {
            NodeColors.ChangeNodesColor(nodes, NodeColors.ColorByName(header));
            return;
        }

    }


    private void searchPopup_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            searchPopup.IsOpen = false;
        }
    }
}

public partial class ED_LatentNodes : Editor, ICanvasMatrix
{
    public bool loaded { get; set; }
    //TODO: pa luego
    [ObservableProperty] [property: JsonIgnore] PromptPreset selectedPreset;
    partial void OnSelectedPresetChanged(PromptPreset value)
    {
        GenerationManager.Instance.SelectedPreset = value;
    }
    public ED_LatentNodes()
    {
        //SelectedPreset ??= GenerationManager.Instance.SelectedPreset;

        ////ensure
        //GenerationManager.OnRegisteredInvoke(() =>
        //{
        //    SelectedPreset ??= GenerationManager.Instance.SelectedPreset;
        //});
    }

    public double GridWidth { get; set; }
    public double GridHeight { get; set; }
    public Matrix CanvasMatrix
    {
        get
        {
            if (ManualAPI.SelectedPreset != null)
                return ManualAPI.SelectedPreset.CanvasMatrix;
            else
                return Matrix.Identity; ;
        }
        set
        {
            if(ManualAPI.SelectedPreset != null)
                ManualAPI.SelectedPreset.CanvasMatrix = value;
        }
    }

    public void DefaultPosition()
    {
       
    }
}
