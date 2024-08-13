using Manual.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Manual.MUI;
using static Manual.API.ManualAPI;
using Manual.Core;
using System.Windows;
using Manual.Editors;
using System.Windows.Media;
using System.Resources;
using System.Windows.Input;
using Manual.Objects;
using Manual.Objects.UI;
using System.Windows.Media.Imaging;
using Manual;

namespace Plugins;

[Export(typeof(IPlugin))]
public class SelectorTool : IPlugin
{
    public void Initialize()
    {
        Tool.Register(T_Selector.Instance);
        Tool.Register(T_Transformer.Instance);
    }
}


public class T_Selector : Tool
{
    public static T_Selector Instance = new T_Selector();

    public Selector selector = new();
    public T_Selector()
    {
      
        // TOOL PROPERTIES
        name = "Selector"; 
        iconPath = $"{App.LocalPath}Resources/Scripts/Lazo/icon.png";
        body.DataContext = project;


        // UI 
        M_StackPanel stackPanelContext = new("SelectedShot");
        add(stackPanelContext, body);


        // main camera
        M_StackPanel stackPanelContext_Camera = new("SelectorArea");
        add(stackPanelContext_Camera, stackPanelContext);

        M_Expander selector_exp = new(stackPanelContext_Camera, "Select");
        selector_exp.AddRange([
        
            new M_Label("Position", true),
            new M_Grid("X", "PositionX", 100, 1),
            new M_Grid("Y", "PositionY", 100, 1),

             new Separator(),

            new M_Label("Resolution", true),
            new M_SliderBox("X", "Width", 1, 2048, 1, 8, true),
            new M_SliderBox("Y", "Height", 1, 2048, 1, 8, true),

            new Separator(),
      ]);
    }
    public override void OnToolSelected()
    {
        //EVENTS
        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp += Shortcuts_CanvasMouseUp;
        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;

    }
    public override void OnToolDeselected()
    {
        //EVENTS
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp -= Shortcuts_CanvasMouseUp;
        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove; //TODO: al recargar los plugins, se bugea porque esto no se desuscribe
    }




    private Point StartPoint;
  
    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {       
        selector.Position = MousePosition;
        // selector.Scale = new PixelPoint(512, 512);
        selector.Scale = PixelPoint.Zero;
        SelectedShot.Add_UI_Object(selector);
        
        // Output.Log($" selector.Position: X:{selector.Position.X}  Y:{selector.Position.Y} \n" +
        //               $"MousePosition: X:{MousePosition.X}  Y:{MousePosition.Y}");

    }
    private void Shortcuts_CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (selector.Scale == PixelPoint.Zero)
        {
            SelectedShot.Remove_UI_Object(selector);
            SelectedShot.SelectedArea = null;
        }
        else
        {
            SelectedShot.SelectedArea = selector;
        }
      
    }
    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
       if (Shortcuts.Dragging)
        {
            selector.Scale = PixelPoint.Distance(StartPoint, MousePosition);
        }
    }

}

public class T_Transformer : Tool
{
    public static T_Transformer Instance = new T_Transformer();

    public Transformer transformer = new();
    public T_Transformer()
    {
        transformer.Visible = false;
        SelectedShot.Add_UI_Object(transformer);


        //SHORTCUTS
        HotKey hotKey = new(KeyComb.Ctrl, Key.T, ChangeToolToThis, "Transform Tool");

        // TOOL PROPERTIES
        name = "Transformer";
        iconPath = $"{App.LocalPath}Resources/Scripts/Move/icon.png";
        body.DataContext = project;


        // UI 
        M_StackPanel stackPanelContext = new("SelectedShot");
        add(stackPanelContext, body);


        // main camera
        M_StackPanel stackPanelContext_Camera = new("TransformerArea");
        add(stackPanelContext_Camera, stackPanelContext);


        CheckBox check = new CheckBox();
        check.SetBinding(CheckBox.IsCheckedProperty, "KeepAspectRatio");
        check.Content = "Keep Aspect Ratio";

        M_Expander selector_exp = new(stackPanelContext_Camera, "Transform");
        selector_exp.AddRange(
        [
             new M_Label("AnchorPoint", true),
            new M_Grid("X", "AnchorPointX", 100, 1),
            new M_Grid("Y", "AnchorPointY", 100, 1),

            new Separator(),

            new M_Label("Position", true),
            new M_Grid("X", "PositionX", 100, 1),
            new M_Grid("Y", "PositionY", 100, 1),

             new Separator(),

            new M_Label("Rotation", true),
            new M_Grid("º", "RealRotation", 100, 1),

             new Separator(),

            new M_Label("Resolution", true),
            new M_SliderBox("X", "Width", 1, 2048, 1, 8, true),
            new M_SliderBox("Y", "Height", 1, 2048, 1, 8, true),

            new Separator(),


            check
        ]);
    }
    public override void OnToolSelected()
    {
        Shortcuts.KeyDown += Shortcuts_KeyDown;
        Shot.LayerChanged += SelectObject;

        transformer.Visible = true;
        transformer.IsEnabled = true;
        SelectObject(SelectedLayer);
    }


    public override void OnToolDeselected()
    {
        Shortcuts.KeyDown -= Shortcuts_KeyDown;
        Shot.LayerChanged -= SelectObject;
    }
    public override void OnNewToolSelected()
    {
       if(SelectedTool.name != "Pan" && SelectedTool.name != "Rot")
        {
            ApplyTransform();

            transformer.Visible = false;
            transformer.IsEnabled = false;
            SelectedShot.TransformerArea = null;
        }
    }

    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
      if(e.Key == Key.Enter)
        {
            ApplyTransform();
            ChangeToolToOld();
        }
    }


    void ApplyTransform() //TODO: por alguna razón, esto se laguea mucho
    {
      //  if(transformer.Target is LayerBase layer)
        //  layer.ImageWr = layer.ImageWr.ScaleTo(transformer.ImageWidth, transformer.ImageHeight);
    }

    public void SelectObject(LayerBase layer)
    {
        if (layer != null)
        {
            transformer.CopyDimensions(layer);
            transformer.AnchorPoint = layer.AnchorPoint;
            SelectedShot.TransformerArea = transformer;

            transformer.Target = layer;
        }
    }
}
