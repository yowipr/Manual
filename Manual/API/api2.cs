using Manual.Core;
using Manual.Core.Nodes;
using Manual.Objects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using static Manual.API.ManualAPI;
using System.Windows.Data;
using Manual.Core.Nodes.ComfyUI;
namespace Manual.API;

public static class api
{
    public static void log(object obj, string title = "")
    {
        Core.Output.Log(obj, title);
    }


    //---------------------------------------------------------- NODES
    public static Prompt prompt(string name)
    {
        return GenerationManager.Instance.Prompts.FirstOrDefault(p => p.Name == name);
    }

    public static PromptPreset preset(string name)
    {
        return FindPreset(name);
    }

    public static NodeBase add_node(string name)
    {
        var node = Node.New(name);

        if(node != null)
        SelectedPreset.AddNode(node);

        return node;
    }
    public static NodeBase node(string name)
    {
        return SelectedPreset.FindNode(name);
    }

    public static NodeBase node(this PromptPreset preset, string name)
    {
        return preset.FindNode(name);
    }
    public static NodeBase node_type(this PromptPreset preset, string nameType)
    {
        return preset.LatentNodes.FirstOrDefault(n => n.NameType == nameType);
    }
    public static NodeBase node_type(this PromptPreset preset, string[] nameTypes)
    {
        return preset.LatentNodes.FirstOrDefault(n => nameTypes.Contains(n.NameType));
    }
    public static NodeBase node(this PromptPreset preset, int idNode)
    {
        return preset.LatentNodes.FirstOrDefault(n => n.IdNode == idNode);
    }

    public static NodeOption field(this NodeBase node, string name)
    {
        return node.FindField(name);
    }



    //------------------------------------------------------ LAYERS


    public static LayerBase layer(string name)
    {
        return FindLayer(name);
    }
    public static LayerBase layer(Guid id)
    {
        return FindLayer(id);
    }
    public static Shot shot(string name)
    {
        return project.ShotsCollection.FirstOrDefault(s => s.Name == name);
    }
    public static Shot shot(Guid id)
    {
        return project.ShotsCollection.FirstOrDefault(s => s.Id == id);
    }




    //-------------------------------------------------------- UI
    public static Grid columns(FrameworkElement[] elements)
    {
        // Crear un nuevo Grid
        Grid grid = new Grid();
     //   grid.Width = double.NaN;
    //    grid.HorizontalAlignment = HorizontalAlignment.Stretch;

        // Configurar las filas del Grid
        foreach (var element in elements)
        {
            // Crea una nueva fila con altura automática
            ColumnDefinition colDef = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            grid.ColumnDefinitions.Add(colDef);

            // Añadir el elemento al Grid
            Grid.SetColumn(element, grid.ColumnDefinitions.Count - 1);
            element.VerticalAlignment = VerticalAlignment.Center;
            element.Margin = new Thickness(3, 0, 3, 0);
            grid.Children.Add(element);
        }

        return grid;
    }


    public static FrameworkElement setHide(this FrameworkElement element, string boolBinding)
    {
        element.SetBinding(FrameworkElement.IsEnabledProperty, new Binding(boolBinding));
        return element;
    }


}
