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
using System.Windows.Data;
using Manual.Objects.UI;
using System.Windows.Input;
using Manual;
using ManualToolkit.Generic;
using Manual.Objects;
using ManualToolkit.Specific;
using InkPlatform.UserInterface;


namespace Plugins;

[Export(typeof(IPlugin))]
public class InspectorTool : IPlugin
{
    public void Initialize()
    {
        Tool.Register(new T_Inspector());
        Tool.Register(new T_Text());
    }

}

public class T_Inspector : Tool
{

    Cursor inspectorCursor;
    public T_Inspector()
    {
        //prop.Test = 42;

        //SHORTCUTS
        HotKey hotKey = new(Key.V, ChangeToolToThis, "Inspector Tool");
        HotKey hotKey2 = new(Key.S, ChangeToolScaling, "Scaling");


        // TOOL PROPERTIES
        name = "Inspector";
        iconPath = $"{App.LocalPath}Resources/Scripts/Inspector/icon.png";
        inspectorCursor = null;

        var stackPanelContext = new M_StackPanel("SelectedShot.SelectedLayer");
        //  AddField(stackPanelContext);


        //NEW

        //var numberBox = new M_NumberBox("X", "PositionX", 100, 1);
        ////   SetBind(numberBox, "PositionX");

        //var section = new M_Section("header",
        //      numberBox,
        //       new M_SliderBox("Y", "PositionY", 3, 1080, 1, 8, true)
        //       );
        //stackPanelContext.Add(section);





        //------------------------------ OLD
        //CONTEXT
        body.DataContext = project;
        //M_StackPanel stackPanelContext = new("SelectedShot.SelectedLayer");
        //  t.body.Children.Add(stackPanelContext);
        add(stackPanelContext, body);

        bool transforms = false;
        if (transforms)
        {
            //TRANSFORM
            var spacing = 0.3;

            //POSITION
            var gPos = new M_Grid();
            gPos.Column(new M_NumberBox("", "PositionX"));
            gPos.Column(new M_NumberBox("", "PositionY"));
            gPos.Column(new M_NumberBox("", "PositionZ"));

            var gPos2 = new M_Grid();
            gPos2.Column(new M_Label("Position"), spacing);
            gPos2.Column(gPos);


            //ROTATION
            var gRot = new M_Grid();
            gRot.Column(new M_NumberBox("", "RotationX"));
            gRot.Column(new M_NumberBox("", "RotationY"));
            gRot.Column(new M_NumberBox("", "RotationZ"));

            var gRot2 = new M_Grid();
            gRot2.Column(new M_Label("Rotation"), spacing);
            gRot2.Column(gRot);

            //SCALE

            var gScale = new M_Grid();
            gScale.Column(new M_NumberBox("", "ScaleX"));
            gScale.Column(new M_NumberBox("", "ScaleY"));
            gScale.Column(new M_NumberBox("", "ScaleZ"));

            var gScale2 = new M_Grid();
            gScale2.Column(new M_Label("Scale"), spacing);
            gScale2.Column(gScale);

            section(stackPanelContext, "Transform", gPos2, gRot2, gScale2);
        }



        Button b = new();
        b.Click += (args, sender) =>
        {
            Output.Log(prop.Test);
        };


        M_Expander inspector_exp_extra = new(stackPanelContext, "Extra Info");
        inspector_exp_extra.AddRange(
        [
              new M_Grid("Image", new M_TextBox(){ Opacity = 0}, "Image"),

            new M_Label("Image Size", true),
            new M_Grid("Width", "ImageWidth"){IsEnabled = false},
            new M_Grid("Height", "ImageHeight"){IsEnabled = false},
            new Separator(),

            new M_Label("Anchor Point", true),
            new M_Grid("X", "AnchorPointNormalizedX", jumps: 2000, jump: 0.01),
            new M_Grid("Y", "AnchorPointNormalizedY", jumps: 2000, jump: 0.01),
            new Separator(),


             new Separator(),
             new M_Label("Parent", true),
             new M_ImageBox("Parent"),
            // new M_Grid("testing", "Test"),
        ]);

        M_Expander inspector_exp = new(stackPanelContext, "Layer");
        inspector_exp.AddRange(
        [
          
            new M_Label("Position", true),
            new M_Grid("X", "PositionX"),
            new M_Grid("Y", "PositionY"),
            new Separator(),

              new M_Label("Rotation", true),
              new M_Grid("degrees", "RealRotation", 100, 1),
              new Separator(),

              //new M_Label("Size", true),
              //new M_Grid("Width", "Width"),
              //new M_Grid("Height", "Height"),
              //new Separator(),

              new M_Label("Size", true),
              new M_Grid("Width", "Width"),
              new M_Grid("Height", "Height"),
              new Separator(),

             new M_Grid("Z Index", "Index"),
              // new M_Grid("Opacity", "Opacity"),
             new M_Grid("", new M_SliderBox("Opacity", "Opacity", 0, 100, 50, 1, true), "Opacity"),


            // new M_Grid("FPS", "_Animation.FPS", 100, 1),

        ]);



        if(Output.DEBUGGING_MODE)
          SetAdminProps(stackPanelContext);


        //EFFECTS
        StackPanel stackPanel = new StackPanel();
        stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

        ItemsControl items = new();
        stackPanel.Children.Add(items);

        SetBind(items, "Effects");
        add(stackPanel, stackPanelContext);


        EffectsMenu addEffect = new();
        add(addEffect, stackPanelContext);

    }


    //debug
    void SetAdminProps(M_StackPanel stackPanelContext)
    {
       M_Expander inspector_exp2 = new(stackPanelContext, "Extra Info");
        inspector_exp2.AddRange(
        [

            new M_Label("Anchor Point", true),
            new M_Grid("X", "AnchorPointX"),
            new M_Grid("Y", "AnchorPointY"),
            new Separator(),

            new M_Label("PositionGlobal", true),
            new M_Grid("X", "PositionGlobalX"),
            new M_Grid("Y", "PositionGlobalY"),
            new Separator(),
            new Separator(),


            new M_Label("Anchor Point Nornalized", true),
            new M_Grid("X", "AnchorPointNormalizedX", jumps: 2000, jump: 0.01),
            new M_Grid("Y", "AnchorPointNormalizedY", jumps: 2000, jump: 0.01),
            new Separator(),

            new M_Label("RealSize", true),
            new M_Grid("Width", "RealWidth"),
            new M_Grid("Height", "RealHeight"),
            new Separator(),

        ]);
        inspector_exp2.IsExpanded = false;
    }
   

    bool toOldTool = false;
    void ChangeToolScaling()
    {
        if (SelectedTool != this) 
        {
            ChangeToolToThis();
            toOldTool = true;

            isScaling = true;
            isDragging = true;

            if (SelectedLayer != null)
            {
                initSize = SelectedLayer.Scale;
                initRealSize = SelectedLayer.RealScale;
                initMousePos = Shortcuts.MousePosition;

                ActionHistory.StartAction(SelectedLayer, nameof(SelectedLayer.Scale));
            }
        }

    }


    public override void OnToolSelected()
    {
        _awaitingRender = false;

        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseUp += Shortcuts_CanvasMouseUp;

        Shortcuts.KeyDown += Shortcuts_KeyDown;

        Shortcuts.CanvasMouseEnter += Shortcuts_CanvasMouseEnter;

        if (SelectedLayer != null && SelectedLayer.Enabled)
             cursor = inspectorCursor;
        else
            cursor = Cursors.No;
    }

    private void Shortcuts_CanvasMouseEnter(object sender, MouseEventArgs e)
    {
        if (SelectedLayer.Enabled)
            cursor = inspectorCursor;
        else
            cursor = Cursors.No;
    }

    public override void OnToolDeselected()
    {
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseUp -= Shortcuts_CanvasMouseUp;

        Shortcuts.KeyDown -= Shortcuts_KeyDown;

        Shortcuts.CanvasMouseEnter -= Shortcuts_CanvasMouseEnter;
    }

    bool isRotation = false;
    bool isScaling = false;

    Point initSize;
    Point initMousePos;
    Point initRealSize;

    float initRot;
    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
        if (_awaitingRender) return;


        if (Shortcuts.CurrentCanvas != null)
        {
            //SCALING
            if (Shortcuts.IsKeyDown(e, Key.S))
            {
                if (isScaling)
                {
                    CancelAction();
                }
                else
                {
                    isScaling = true;
                    isDragging = true;

                    if (SelectedLayer != null)
                    {
                        cursor = Cursors.ScrollWE;

                        initSize = SelectedLayer.Scale;
                        initRealSize = SelectedLayer.RealScale;
                        initMousePos = Shortcuts.MousePosition;

                        ActionHistory.StartAction(SelectedLayer, nameof(SelectedLayer.Scale));
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                RealCancelAction();
            }

            //ROTATION
            if (Shortcuts.IsKeyDown(e, Key.R))
            {
                if (isRotation)
                {
                    CancelAction();
                }
                else
                {
                    isRotation = true;
                    isDragging = true;

                    if (SelectedLayer != null)
                    {
                        cursor = Cursors.ScrollWE;

                        initRot = SelectedLayer.RealRotation;
                        initMousePos = Shortcuts.MousePosition;

                        ActionHistory.StartAction(SelectedLayer, nameof(SelectedLayer.RealRotation));
                    }
                }
            }

            //POSITION
            else if (Shortcuts.IsCapsLock())
            {
                if (e.Key == Key.Left)
                    SelectedLayers.ForEach(l => l.PositionX--);
                else if (e.Key == Key.Up)
                    SelectedLayers.ForEach(l => l.PositionY++);
                else if (e.Key == Key.Right)
                    SelectedLayers.ForEach(l => l.PositionX++);
                else if (e.Key == Key.Down)
                    SelectedLayers.ForEach(l => l.PositionY--);

                Shot.UpdateCurrentRender();

            }
        }

    }

    void RealCancelAction()
    {
        isDragging = false;

        if (isScaling)
        {
            SelectedLayer.Scale = initSize;
            isScaling = false;
        }
        else if (isRotation)
        {
            SelectedLayer.RealRotation = initRot;
            isRotation = false;
        }

        ActionHistory.CancelAction();
    }
    void CancelAction()
    {
        if (!ActionHistory.IsOnAction) return;

        RealCancelAction();
    }

    Vector draggingDelta;
    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Shortcuts.IsCtrlPressed)
        {
            //no funca porque no ve la elementui de los layers

            //var hit = Shortcuts.CurrentCanvas.HitTest(e, element => element.DataContext is LayerBase l && l is not UILayerBase &&  l._Animation.IsActuallyVisible);
            //if(hit?.DataContext is LayerBase l)
            //{
            //    SelectedLayer = l;
            //}
            
        }

        if (isScaling || isRotation)
        {
            return;
        }


        if (SelectedLayer != null && SelectedLayer.Enabled)
        {
            isDragging = true;   
            draggingDelta = SelectedLayer.Position - Shortcuts.MousePosition;

            ActionHistory.StartAction(SelectedLayer, nameof(SelectedLayer.Position));
        }
    }

    bool isDragging = false;


    //------------------------------------------------------------------------------------------------------- MOUSE MOVE
    private bool _awaitingRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitingRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }
 //   int count = 0;
    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
      
        if (_awaitingRender || !SelectedLayer.Enabled && !isDragging) return;




        if (SelectedLayer == null) return;

        if (isScaling)
        {
            var delta = initMousePos - Shortcuts.MousePosition;
           // var newP = new Point(initSize.X - delta.X, initSize.Y - delta.X);


            var scaleFactor = 1 - (delta.X / initRealSize.X); // Proporción de cambio basado en el ancho real.

            // Calcula la nueva escala aplicando el scaleFactor a initSize
            var newScaleX = Math.Max(0, initSize.X * scaleFactor); // Asegura que la escala no sea negativa
            var newScaleY = Math.Max(0, initSize.Y * scaleFactor); // Mantiene la relación de aspecto

            // Establece la nueva escala
            if (newScaleX > 0.5 && newScaleY > 0.5)
            {
                if(SelectedShot.Snap || Shortcuts.IsAltPressed)
                   SelectedLayer.Scale = new Point(SelectedShot.SnapToJump(newScaleX, 10), SelectedShot.SnapToJump(newScaleY, 10));
                else
                    SelectedLayer.Scale = new Point(newScaleX, newScaleY);
            }

            //    if (newP.X > 0.5 && newP.Y > 0.5)
            //       SelectedLayer.Scale = newP;


            Shot.UpdateCurrentRender();
        }
        else if (isRotation)
        {
            var delta = initMousePos - Shortcuts.MousePosition;

            float rotFactor = ((float)delta.X * 0.3f); // Proporción de cambio basado en el ancho real.
            var newRot = initRot + rotFactor;

            if(SelectedShot.Snap || Shortcuts.IsAltPressed)
                SelectedLayer.RealRotation = SelectedShot.SnapToJump(newRot, 45);
            else
                SelectedLayer.RealRotation = newRot;

            Shot.UpdateCurrentRender();
        }

        else if (Shortcuts.Dragging)
        {
            Point mousePos = Shortcuts.MousePosition;
            Point newPos = (mousePos + draggingDelta);

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                newPos = newPos.ToPixelPoint();

            // Si el ajuste está activado, redondea newPos a los saltos más cercanos
            if (SelectedShot.Snap || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                newPos = SelectedShot.SnapToJump(newPos);
            }


            SelectedLayer.Position = newPos;


            //wait for updating
            _awaitingRender = true;
            CompositionTarget.Rendering += CompositionTarget_Rendering;


            Shot.UpdateCurrentRender();

          //  Output.Log(count++);

        }

    }


    private void Shortcuts_CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (SelectedLayer == null || !SelectedLayer.Enabled) return;

 
            if (isScaling || isRotation)
            {
                isScaling = false;
                isRotation = false;

                if (SelectedLayer != null)
                {
                    ActionHistory.FinalizeAction();

                    if (toOldTool)
                    {
                        toOldTool = false;
                        ChangeToolToOld();
                    }
                }
            }
            else
            ActionHistory.FinalizeAction();

        cursor = null;
        isDragging = false;
    }



}






public class T_Text : Tool
{
    public T_Text()
    {
        name = "Text";
        iconPath = $"{App.LocalPath}Resources/Scripts/Text/icon.png";

        HotKey hotKey = new(KeyComb.None, Key.T, ChangeToolToThis, "Text Tool");


        var sbody = new M_StackPanel("SelectedShot.SelectedLayer");
        body.DataContext = project;
        add(sbody, body);

        section(sbody, "Text", [
            new M_Grid("Text", new M_TextBox("Text"), "Text"),
            new M_NumberBox("Size", 1, 1, true, 1, 468),

            new M_ColorPicker("BrushColor"),

        ]);



    }

    public override void OnToolDeselected()
    {
        Shortcuts.MouseDown -= Shortcuts_MouseDown;
    }

    public override void OnToolSelected()
    {
        cursor = Cursors.IBeam;

        Shortcuts.MouseDown += Shortcuts_MouseDown;

    }

    private void Shortcuts_MouseDown(object sender, MouseEventArgs e)
    {
    //    if (isTexted == false)
     //       SelectedShot.AddText();

    }


}