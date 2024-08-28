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
using Manual.Objects.UI;
using Manual.Objects;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Manual;
using SkiaSharp;

using PointF = System.Drawing.PointF;
using System.Diagnostics;
using SkiaSharp.Views.WPF;
using SkiaSharp.Views.Desktop;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core.Nodes;
using CefSharp.DevTools.CSS;

namespace Plugins;

[Export(typeof(IPlugin))]
public class BrushTool : IPlugin
{
    public void Initialize()
    {
        Tool.Register(new T_Brush());
        Tool.Register(new T_Eraser());
        Tool.Register(new T_Dropper());

        Tool.Register(new T_Fill());
        Tool.Register(new T_Distort());
        Tool.Register(new T_Lasso());

        Tool.Register(new T_Shape());
    }

}


public partial class T_Brush : Tool
{
    public bool customPencil = false;
    Brusher pencilBrush = new();


    [ObservableProperty] bool isGenerativeBrush = false;
    [ObservableProperty] bool isSketchMode = false;
    GhostLayer GenBrushLayer;

    public static Cursor drawingCursor;
    public T_Brush()
    {
      //  targetPresetOnSelected = false;



        TargetLayer = SelectedLayer;
        TargetBrush = pencilBrush;

        SetTimedImage();
        //SHORTCUTS
        HotKey hotKey = new(Key.B, ChangeToolToThis, "Brush Tool");


        // TOOL PROPERTIES
        name = "Brusher";
        iconPath = $"{App.LocalPath}Resources/Scripts/Brush/icon.png";
        cursorPath = $"{App.LocalPath}Resources/Scripts/Brush/cursor_draw.cur";
        drawingCursor = new Cursor(cursorPath);

        InstantiateBrushUI(body);


        //section("Generative Brush", "IsGenerativeBrush", [
        //    //new M_Button("Apply", GenApply)
        //    new M_Label("draw with AI"),

        //]);

        _disabledGenBtn();
        genBtn.Height = 30;
        genBtn.Click += (s, e) => { switchGenBrush(); };

        var clearBtn = new M_Button("Clear Draw", ClearDraw);

        var sketchbox = new M_CheckBox("Sketch Mode", "IsSketchMode");
        section("Generative", [
           genBtn,
           sketchbox,
           clearBtn
            ]);

    
    }
    M_Button genBtn = new();

    void ClearDraw()
    {
        DisableGenBrush();
        GenBrushLayer = null;
        EnableGenBrush();
    }

    void switchGenBrush()
    {
        IsGenerativeBrush = !IsGenerativeBrush;
    }
    void _disabledGenBtn()
    {
        genBtn.Height = 30;

        genBtn.Background = new SolidColorBrush(Colors.Transparent);
        genBtn.BorderBrush = AppModel.GetResource<SolidColorBrush>("fg");
        genBtn.BorderThickness = new Thickness(1);

        genBtn.Content = "✎ Generative Brush";
        genBtn.ToolTip = "Click to enable Generative Brush";

    }
    void _enabledGenBtn()
    {
        genBtn.Background = new SolidColorBrush(ManualColors.Violet);
   //     genBtn.BorderBrush = null;
    //    genBtn.BorderThickness = new Thickness(0);

        genBtn.Content = "✎ Generative Brush";
        genBtn.ToolTip = "Click to disable Generative Brush";
    }


    partial void OnIsSketchModeChanged(bool value)
    {
        if (IsGenerativeBrush)
            AsignWorkflow();
    }
    void AsignWorkflow()
    {
        if (IsSketchMode)
            SetTargetPreset("generative draw sketch");
        else
            SetTargetPreset("generative draw");

        SelectTargetPreset();
    }

    //------------------------------------------------------------------------------------- GENERATIVE BRUSH
    partial void OnIsGenerativeBrushChanged(bool value)
    {
        if (value)
            EnableGenBrush();
        else
            DisableGenBrush();
    }

    PromptPreset genP;
    void EnableGenBrush()
    {
        PreviewableValue<object>.OnPreviewChanged += PreviewableValue_OnPreviewChanged;
        PreviewableValue<SKBitmap>.OnPreviewChanged += PreviewableValueSK_OnPreviewChanged;

        _enabledGenBtn();
        targetAsSelected = false;


        if (GenBrushLayer == null)
        {
            GenBrushLayer = api.layer("GenDraw") as GhostLayer;
            GenBrushLayer ??= new GhostLayer() { Name = "GenDraw" };
        }
        
        Add_GhostLayer(GenBrushLayer);
        GenBrushLayer._Animation.StartOffset = 0;
        GenBrushLayer.AddEffect(new E_Blur() { Strength = 0 });

        TargetLayer = GenBrushLayer;


        AsignWorkflow();

        prompt = GenerationManager.Instance.SelectedPrompt;


        UpdateCanvas_BottomSection(SetBottomSection);
    }

    void DisableGenBrush()
    {
        PreviewableValue<object>.OnPreviewChanged -= PreviewableValue_OnPreviewChanged;
        PreviewableValue<SKBitmap>.OnPreviewChanged -= PreviewableValueSK_OnPreviewChanged;

        _disabledGenBtn();

        TargetPreset = null;

        if(GenBrushLayer.Enabled)
          Remove_GhostLayer(GenBrushLayer);


        targetAsSelected = true;

        Shot.UpdateCurrentRender();
        UpdateCanvas_BottomSection(null);

    }


    M_StackPanel SetBottomSection()
    {
        var sp = new M_StackPanel();
        sp.DataContext = GenerationManager.Instance;
        var strength = new M_SliderBox("Strength", 0, 1, 100, 0.05, true) { Width=140, IsUndo = false};
        var binding = new Binding("SelectedPreset.Prompt")
        {
            Source = GenerationManager.Instance
        };
        strength.SetBinding(M_SliderBox.DataContextProperty, binding);
        sp.Add(strength);

        strength.PreviewMouseLeftButtonDown += (s, e) => 
        {
            var blur = GenBrushLayer.FindEffect<E_Blur>("Blur");
            if(blur != null)
                blur.Strength = _setBlurStrength(strength.Value);
            isStrengthChanging = true;

            Shot.UpdateCurrentRender();
        };
        strength.PreviewMouseLeftButtonUp += (s, e) =>
        {
            isStrengthChanging = false;

            var blur = GenBrushLayer.FindEffect<E_Blur>("Blur");
            if (blur != null)
                blur.Strength = 0;

            Shot.UpdateCurrentRender();
        };
        strength.ValueChanged += Strength_ValueChanged;
        return sp;
    }

    bool isStrengthChanging = false;
    float _setBlurStrength(double Strength)
    {
        return Convert.ToSingle(Strength * 20);
    }
    private void Strength_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (!isStrengthChanging) return;

        var blur = GenBrushLayer.FindEffect<E_Blur>("Blur");
        if (blur != null)
            blur.Strength = _setBlurStrength(e.NewValue);
    }

    bool generated = false;
    void GenApply()
    {

        if (ManualAPI.SelectedLayer.PreviewValue.IsPreviewMode)
        {
            ManualAPI.SelectedLayer.PreviewValue.EndPreview(true);
            GenBrushLayer.Image.Erase(SKColors.Transparent);
        }
    }

    private void GenerationManager_OnGenerated(GeneratedImage img)
    {
        if (IsGenerativeBrush)
        {
            GenBrushLayer.Opacity = 0;
            generated = true;
            Shot.UpdateCurrentRender();
        }
    }
    Prompt prompt;






    //------------------------------------------------------------------------------------------------------------------------------- NORMAL BRUSH
    public static void InstantiateBrushUI(StackPanel Body)
    {
        Body.DataContext = project;

        M_StackPanel stackPanelContext = new("SelectedBrush");
        add(stackPanelContext, Body);


        CheckBox ck2 = new();
        SetBind(ck2, "Size.EnablePenPressure");
        ck2.Content = "Pen Pressure";

        CheckBox ck = new();
        SetBind(ck, "Size.EnableVelocity");
        ck.Content = "Velocity";

        CheckBox ck3 = new();
        SetBind(ck3, "IsAntialias");
        ck3.Content = "Antialiasing";


        CheckBox ck4 = new();
        SetBind(ck4, "Opacity.EnablePenPressure");
        ck4.Content = "Opacity Pen Pressure";

        var blendModeBox = new ComboBox() { ItemsSource = AppModel.GetEnumNames(typeof(LayerBlendMode)) };
        blendModeBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("BlendMode"));

        // Render //
        M_Expander render_exp = new(stackPanelContext, "Brush");
        render_exp.AddRange([

            new M_Label("Brush", true),
            new M_SliderBox("Size", "Size.Value", 1, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },

            ck2,

            new Separator(),
            new M_SliderBox("Opacity", "Opacity.Value", 1, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },

           // ck4,



            new M_SliderBox("Pressure Min", "Size.PenPressureMinimum", 0, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },

          //  new M_SliderBox("Pressure Multiply", "Size.PenPressureMultiply", 1, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },



            ck,

            new M_SliderBox("Velocity Min", "Size.VelocityMinimum", 0, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },
            // test,
            new Separator(),

            ck3,
          
            new M_Label("Blend Mode", true),
            blendModeBox,


            new M_SliderBox("Feather", "Feather", 0, 100, 100, 1, true) { IsUndo = false, IsUpdateRender = false },

        ]);

    }

    void SetTimedImage()
    {
        if(TargetLayer != null && timedImage != Animation.GetTimedVariable(TargetLayer, nameof(TargetLayer.Image)) )
              timedImage = Animation.GetTimedVariable(TargetLayer, nameof(TargetLayer.Image));
    }


    public override void OnToolSelected()
    {
        base.OnToolSelected();

        GenerationManager.OnGenerated += GenerationManager_OnGenerated;

        if (!customPencil)
            SelectedBrush = pencilBrush;

        SetTimedImage();

        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp += Shortcuts_CanvasMouseUp;

        Shortcuts.CanvasKeyUp += Shortcuts_KeyUp;
        Shortcuts.CanvasKeyDown += Shortcuts_KeyDown;

        Shortcuts.CanvasMouseEnter += Shortcuts_CanvasMouseEnter;
        Shortcuts.CanvasMouseLeave += Shortcuts_CanvasMouseLeave;
        Shot.LayerChanged += Shot_LayerChanged;


        if (TargetLayer != null && TargetLayer.Enabled)
            cursor = drawingCursor;
        else
            cursor = Cursors.No;


        // brush ui
        if (IsCursorFormType(SelectedBrush))
        {
            ui_brushSize.BrushSize = SelectedBrush.Size.Value;
            SelectedShot.Add_UI_Object(ui_brushSize);
        }
        else
        {
            SelectedShot.Remove_UI_Object(ui_brushSize);
        }


        if(IsGenerativeBrush)
           AsignWorkflow();

        UpdateCursor();
    }



    public override void OnToolDeselected()
    {
        base.OnToolDeselected();

        if(!IsGenerativeBrush)
            targetAsSelected = true;

        GenerationManager.OnGenerated -= GenerationManager_OnGenerated;

        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp -= Shortcuts_CanvasMouseUp;


        Shortcuts.CanvasKeyUp -= Shortcuts_KeyUp;
        Shortcuts.CanvasKeyDown -= Shortcuts_KeyDown;

        Shortcuts.CanvasMouseEnter -= Shortcuts_CanvasMouseEnter;
        Shortcuts.CanvasMouseLeave -= Shortcuts_CanvasMouseLeave;
        Shot.LayerChanged -= Shot_LayerChanged;

        if (IsCursorFormType(SelectedBrush))
            SelectedShot.Remove_UI_Object(ui_brushSize);



    }

    private void PreviewableValueSK_OnPreviewChanged(PreviewableValue<SKBitmap> value)
    {
        OnPreviewApplied(value);
    }
    private void PreviewableValue_OnPreviewChanged(PreviewableValue<object> value)
    {
        if (value.previewValue is SKBitmap)
        {
            OnPreviewApplied(value);
        }

    }
    void OnPreviewApplied<T>(PreviewableValue<T> value)
    {
        if (IsGenerativeBrush)
        {
            if (value.IsFinished && !value.IsPreviewMode) //appplying
            {
                if (value.IsApplied)
                {
                    ClearLayer();
                }
                else
                {
                    GenBrushLayer.Opacity = 100;
                }
            }
        }
    }

    bool IsCursorFormType(Brusher brusher)
    {
        if (brusher.isCustomCursor)
            return false;
        else if ((brusher.Type == "Pencil" && Settings.instance.PencilForm != PencilCursor.Dot) ||
               (brusher.Type == "Eraser" && Settings.instance.EraserForm != PencilCursor.Dot))
            return true;
        else if (brusher.Type != "Pencil" && brusher.Type != "Eraser") //default
            return true;
        else
            return false;
    }

    private void Shortcuts_CanvasMouseLeave(object sender, MouseEventArgs e)
    {
        SelectedShot.Remove_UI_Object(ui_brushSize);
    }

    private void Shot_LayerChanged(LayerBase layer)
    {
        if(targetAsSelected)
            TargetLayer = layer;

        UpdateCursor();
    }

    private void Shortcuts_CanvasMouseEnter(object sender, MouseEventArgs e)
    {
        UpdateCursor();
    }

    void UpdateCursor()
    {


       if ((TargetLayer != null && TargetLayer.Enabled) || targetAsSelected)
            cursor = drawingCursor;
        else
            cursor = Cursors.No;

        if (SelectedBrush == null) return;

        ui_brushSize.BrushSize = SelectedBrush.Size.Value;
        if (IsCursorFormType(SelectedBrush))
            SelectedShot.Add_UI_Object(ui_brushSize);

    }




    PointF initial;


    TimedVariable timedImage;

    bool changeSize;
    float initialSize;
    Point fixedInitialSize; // center of the circle
    Point fixedInitialPos;

    UI_Brush ui_brushSize = new();

    public LayerBase? TargetLayer = null;
    public Brusher TargetBrush = null;
    public bool targetAsSelected = true;

    public bool enableDraw = true;

    float ui_rotBrush = 0;
    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {

            TargetBrush = SelectedBrush;

            if (targetAsSelected)
            {
                if(!IsGenerativeBrush)
                    TargetLayer = SelectedLayer;
                else
                    TargetLayer = GenBrushLayer;

            }      
            if (TargetLayer == null || !TargetLayer.Enabled) return;


            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                fixedInitialPos = MousePixelPosition;
                initialSize = TargetBrush.Size.Value;

                float angleDegrees = ui_rotBrush; // El ángulo en grados, por ejemplo, 90 grados para el punto superior
                float radius = initialSize / 2; // El radio del círculo

                //añadir initialsize par que aparezca en el borde del círculo en lugar del centro
                fixedInitialSize = Degrees.GetCircleCenterFromPointOnEdge(MousePixelPosition, radius, angleDegrees);

                ui_brushSize.Position = fixedInitialSize;
                ui_brushSize.BrushSize = initialSize;
                ui_brushSize.IsHeader = true;
                SelectedShot.Add_UI_Object(ui_brushSize);

                changeSize = true;
                enableDraw = false;
                return;
            }



            //PREVIEW MODE
            if (SelectedLayer.PreviewValue.IsPreviewMode && IsGenerativeBrush)
            {
                SelectedLayer.PreviewValue.EndPreview(true);
                generated = false;
                GenBrushLayer.SetSolidColor(SKColors.Transparent);
                GenBrushLayer.Opacity = 100;
            }
            else if (IsGenerativeBrush)
            {
                if(TargetLayer.Opacity != 100)
                   TargetLayer.Opacity = 100;

                if (generated)
                {
                    GenBrushLayer.Opacity = 100;
                    if (ManualAPI.SelectedLayer.PreviewValue.IsPreviewMode)
                        ManualAPI.SelectedLayer.PreviewValue.SwitchPreview(false);

                    generated = false;
                    Shot.UpdateCurrentRender();
                }
            }


            ActionHistory.StartAction(TargetLayer, nameof(TargetLayer.Image), $"{TargetLayer.GetType().Name} {TargetLayer.Name}: Brush Tool, {TargetBrush}, {initial}");



            // on drawing
            TargetLayer.StartEdit();


            //if (Animation.AutoKeyframe)
            //{
            //    SetTimedImage();

            //    if (timedImage != null && !timedImage.HasKeyframe())
            //        TargetLayer.Image.Erase(SKColors.Transparent);//.Clear(Colors.Transparent);
            //}




            initial = MousePositionF.RelativeTo(new PointF(TargetLayer.PositionGlobalX, TargetLayer.PositionGlobalY));

            TargetBrush.SetInitial();        
            Draw(0);     
            
            enableDraw = true;
        }
    }




    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (enableDraw && IsCursorFormType(SelectedBrush))
        {
            if (Shortcuts.IsPanning)
                ui_brushSize.Visible = false;
            else
                ui_brushSize.Visible = true;

            PointF position = MousePositionF;
            ui_brushSize.IsHeader = false;
            ui_brushSize.PositionX = position.X;
            ui_brushSize.PositionY = position.Y;
            ui_brushSize.BrushSize = SelectedBrush.Size.Value;
        }


        if (Shortcuts.Dragging)
        {
            if (enableDraw)
                Draw();
            else if (changeSize && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                ChangeSize();
        }

       // test.Text = Shortcuts.MouseVelocity.ToString();
    }

    void ChangeSize()
    {
        ui_brushSize.IsHeader = true;

        PixelPoint position = MousePixelPosition;
        var mouseDistance = PixelPoint.DistanceLength(fixedInitialSize, position);
        var radius = PixelPoint.DistanceLength(fixedInitialPos, fixedInitialSize);
        int newSize = (int)( radius + (mouseDistance * 2) );

       var radius2 = (int)(PixelPoint.DistanceLength(ui_brushSize.Position, position) * 2);
        ui_brushSize.BrushSize = radius2;
        TargetBrush.Size.Value = radius2;
    }




    //---------------------------------------------------------------------------------------------- DRAWING SYSTEM

    private List<PointF> mousePositions = new List<PointF>();

    private bool _awaitingRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitingRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        
    }

    void Draw(int phase = 1)
    {
        if (TargetLayer == null || !TargetLayer.Enabled || TargetBrush == null) return;


        PointF position = MousePositionF.RelativeTo(TargetLayer.PositionGlobal);

    
        if (_awaitingRender)
            return;

        if(phase == 1)
             SelectedBrush.OnDraw?.Invoke(TargetLayer, initial, position);
        else
            SelectedBrush.OnMouseDown?.Invoke(TargetLayer, initial, position);

        initial = position;
        TargetBrush.SetInitial();

        //wait for updating
        _awaitingRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

        Shot.UpdateCurrentRender();
    }



    private void Shortcuts_CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
    
        if (TargetLayer == null || !TargetLayer.Enabled) return;

        if (!changeSize)
        {
            SelectedBrush.OnMouseUp?.Invoke(TargetLayer, initial, initial);
            TargetLayer.EndEdit();
            ActionHistory.FinalizeAction();
        }
        else
        {
            ui_brushSize.IsHeader = false;
            if (!IsCursorFormType(SelectedBrush))
                Remove_UI_Object(ui_brushSize);


            enableDraw = true;
        }
    }




    private void Shortcuts_KeyUp(object sender, KeyEventArgs e) 
    {
        if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt && TargetBrush != null)  // aplly size
        {
            changeSize = false;
            TargetBrush.Size.Value = ui_brushSize.BrushSize;
            ui_rotBrush = (int)Degrees.GetAngleFromPoints(ui_brushSize.Position, MousePixelPosition);
            if (!IsCursorFormType(SelectedBrush))
                Remove_UI_Object(ui_brushSize);

            cursor = drawingCursor;
            
        }
    }

    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
        if (IsGenerativeBrush)
        {
            if (e.Key == Key.Return && !Shortcuts.IsShiftPressed && !Shortcuts.IsCtrlPressed)
            {
                //GenApply();
            }
        }

        if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)  // aplly size
        {
            cursor = null;
        }
        else if (e.Key == Key.Back)
        {
            ClearLayer();
        }

    }
    void ClearLayer()
    {

        if (TargetLayer != null)
        {
            TargetLayer.StartEdit();
            TargetLayer.SetSolidColor(SKColors.Transparent);
            TargetLayer.EndEdit();
        }
    }

}




public class T_Dropper : Tool
{
    Cursor dropperCursor = new Cursor($"{App.LocalPath}Resources/Scripts/Brush/cursor_dropper.cur");
    public T_Dropper()
    {
        name = "Dropper";
        iconPath = $"{App.LocalPath}Resources/Scripts/Brush/icon2.png";
        cursor = dropperCursor;

        HotKey hotKey = new(KeyComb.None, Key.I, ChangeToolToThis, "Dropper Tool");

    }
 
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);

    [DllImport("gdi32.dll")]
    static extern uint GetPixel(IntPtr hDC, int nXPos, int nYPos);

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    void SetColor()
    {
        SelectedBrush.ColorBrush = GetColorScreen();
    }
    Color GetColorScreen()
    {
        // Obtener la posición del cursor en coordenadas de pantalla
        System.Drawing.Point cursor = new System.Drawing.Point();
        GetCursorPos(ref cursor);

        // Obtener el color del píxel bajo el cursor
        IntPtr desktopDC = GetDC(IntPtr.Zero);
        uint pixelColor = GetPixel(desktopDC, cursor.X, cursor.Y);
        ReleaseDC(IntPtr.Zero, desktopDC);

        // Descomponer el color del píxel en componentes ARGB
        //  byte a = (byte)((pixelColor & 0xFF000000) >> 24);
        byte b = (byte)((pixelColor & 0x00FF0000) >> 16);
        byte g = (byte)((pixelColor & 0x0000FF00) >> 8);
        byte r = (byte)(pixelColor & 0x000000FF);

        // Usar el color como necesites
       return Color.FromArgb(255, r, g, b);
    }


    bool isDragging = false;

    private void Shortcuts_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            isDragging = true;
            SelectedBrush.isUpdate = false;
        }
    }
    private void Shortcuts_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging)
        {
            SetColor();
        }
    }
    bool releasedMouse = false;
    private void Shortcuts_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            isDragging = false;
            ResetTool();
        }
    }


    public override void OnToolSelected()
    {
        isDragging = false;
        Mouse.OverrideCursor = dropperCursor;

        Shortcuts.MouseDown += Shortcuts_MouseDown;
        Shortcuts.MouseMove += Shortcuts_MouseMove;
        Shortcuts.MouseUp += Shortcuts_MouseUp;

    }



    public override void OnToolDeselected()
    {
        Mouse.OverrideCursor = null;

        Shortcuts.MouseDown += Shortcuts_MouseDown;
        Shortcuts.MouseMove -= Shortcuts_MouseMove;
        Shortcuts.MouseUp -= Shortcuts_MouseUp;

        isDragging = false;

        var color = GetColorScreen();
        SelectedBrush.ColorBrush = color;
        SelectedBrush.isUpdate = true;

        //SelectedBrush.UpdateBrush(color);
     
    }

    void ResetTool()
    {
        Mouse.OverrideCursor = null;
        SelectedTool = project.toolManager.OldTool;
    }



}




public class T_BrushCustom : Tool
{
    public LayerBase TargetLayer;

    public Brusher brush = new();
    public T_BrushCustom()
    {
        brush.Type = "Custom";
        brush.OnMouseDown = OnMouseDown;
        brush.OnMouseUp = OnMouseUp;
        brush.OnDraw = OnDraw;

        cursor = T_Brush.drawingCursor;
    }
    public override void OnToolSelected()
    {
        var BrushInstance = Tool.ByName("Brusher") as T_Brush;

        SelectedBrush = brush;
        //call a tool in a tool
        BrushInstance.customPencil = true;

        if (TargetLayer != null)
        {
            BrushInstance.TargetLayer = TargetLayer;
            BrushInstance.targetAsSelected = false;
        }

        BrushInstance.targetPresetOnSelected = false;
        BrushInstance.OnToolSelected();
        base.OnToolSelected();
    }

    public override void OnToolDeselected()
    {
        var BrushInstance = Tool.ByName("Brusher") as T_Brush;

        if (TargetLayer != null)
        {
            BrushInstance.TargetLayer = null;
            BrushInstance.targetAsSelected = true;
        }

        BrushInstance.targetPresetOnSelected = true;
        BrushInstance.customPencil = false;
        BrushInstance.OnToolDeselected();
    }
    public virtual void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {

    }
    public virtual void OnDraw(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
      
    }
    public virtual void OnMouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {

    }

    public PointF GetMousePos(CanvasObject relativeTo)
    {
        return MousePositionF.RelativeTo(new PointF(relativeTo.PositionGlobalX, relativeTo.PositionGlobalY));
    }


    public void InstantiateBrushUI()
    {
        T_Brush.InstantiateBrushUI(body);
    }
}

public class T_Eraser : T_BrushCustom
{
    public T_Eraser()
    {
        name = "Eraser";
        iconPath = $"{App.LocalPath}Resources/Scripts/Eraser/icon.png";
        cursorPath = $"{App.LocalPath}Resources/Scripts/Brush/cursor_draw.cur";

        HotKey hotKey = new(Key.N, ChangeToolToThis, "Eraser Tool");

        InstantiateBrushUI();

        SetBrush();
    }

    void SetBrush()
    {
        brush = new();
        brush.Size.EnablePenPressure = false;
        brush.Type = "Eraser";
        brush.Size.Value = 30;
        brush.Size.EnableVelocity = false;
        //brush.ColorBrush = Colors.White;

        brush.IsEraser = true;
    }




}





public class T_Distort : T_BrushCustom
{
    public T_Distort()
    {
        name = "Distort";
        iconPath = $"{App.LocalPath}Resources/Scripts/Pan/icon3.png";
        brush.Type = "Distort";
        brush.Size.Value = 40;

        Props.Realtime = false;

        //SHORTCUTS
        HotKey hotKey = new(Key.J, ChangeToolToThis, "Distort Tool");

        body.DataContext = project;
        M_StackPanel stackPanelContext = new("SelectedBrush");
        add(stackPanelContext, body);


        var ckb = new CheckBox() { Content = "Deform"};
        ckb.DataContext = this;
        ckb.SetBinding(CheckBox.IsCheckedProperty, "Props.Realtime");

        M_Expander render_exp = new(stackPanelContext, "Brush");
        render_exp.AddRange([

            new M_Label("Brush", true),
            new M_SliderBox("Size", "Size.Value", 1, 2028, 100, 1, true) { IsUndo = false, IsUpdateRender = false },
            new Separator(),
           ckb,
        ]);

    }


    PointF initial;
    public override void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        initial = GetMousePos(layer);
    }
    public override void OnMouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if (!((bool)Props.Realtime))
        {
            Mouse.OverrideCursor = Cursors.Wait;
            layer.Image.DrawDistort(initial.ToSKPoint(), finalPosition.ToSKPoint(), brush);
            Mouse.OverrideCursor = null;
        }
    }

    public override void OnDraw(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if ((bool)Props.Realtime)
        {
            layer.Image.DrawDistort(initialPosition.ToSKPoint(), finalPosition.ToSKPoint(), brush);
        }
    }


}



public class T_Fill : T_BrushCustom
{
    Cursor customCursor = new Cursor($"{App.LocalPath}Resources/Scripts/Fill/fill.cur");
    public T_Fill()
    {
        name = "Fill";
        iconPath = $"{App.LocalPath}Resources/Scripts/Fill/icon.png";
        brush.Type = "Fill";
        brush.isCustomCursor = true;
        cursor = customCursor;

        Props.Threshold = 40;

        HotKey hotKey = new(Key.G, ChangeToolToThis, "Fill Tool");

        body.DataContext = project;
        M_StackPanel stackPanelContext = new(this);
        add(stackPanelContext, body);


        M_Expander render_exp = new(stackPanelContext, "Fill");
        render_exp.AddRange([           
            new M_SliderBox("Threshold", "Props.Threshold", 0, 100, 50, 1, true) { IsUndo = false, IsUpdateRender = false },
        ]);

    }

    public override void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        var threshold = (int)Props.Threshold;
        RendGPU.FloodFill(layer.Image, initialPosition.ToSKPoint(), brush.ColorBrush.ToSKColor(), threshold);
        Mouse.OverrideCursor = null;

    }

}




public class T_Lasso : T_BrushCustom
{
    Cursor customCursor = new Cursor($"{App.LocalPath}Resources/Scripts/Selector/lasso.cur");
    Lasso lasso => SelectedShot.Lasso;
    public T_Lasso()
    {
        name = "Lasso";
        iconPath = $"{App.LocalPath}Resources/Scripts/Selector/icon.png";
        brush.Type = "Lasso";
        brush.isCustomCursor = true;
        cursor = customCursor;

        HotKey hotKey = new(Key.L, ChangeToolToThis, "Lasso Tool");

        var btn = new M_Button("Clear Lasso");
        btn.Click += (s, e) => SelectedShot.Lasso.Clear();

        section("Lasso",
            btn);

    }

    public override void OnToolSelected()
    {
        Shortcuts.KeyDown += Shortcuts_KeyDown;

        base.OnToolSelected();
        lasso.Enabled = true;
    }


    public override void OnToolDeselected()
    {
        Shortcuts.KeyDown -= Shortcuts_KeyDown;

        base.OnToolDeselected();
        lasso.Enabled = false;
    }


    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Remove_UI_Object(lasso);
            lasso.Clear();
        }
    }


    bool selecting = false;
    bool added = false;
    PointF initial;
    public override void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if (lasso.Mode != LassoMode.Dragging)
        {
            Remove_UI_Object(lasso);
            lasso.Clear();
            selecting = true;
            added = false;
            initial = GetPoint();

            lasso.Mode = LassoMode.Add;
           // lasso.Add(initial.ToPoint());
        }
    }
    public override void OnDraw(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if (lasso.Mode != LassoMode.Dragging)
        {
            if (selecting)
            {
               // if (!added && SKPoint.Distance(initial.ToSKPoint(), finalPosition.ToSKPoint()) < 1)
              //      return;

                if (lasso.Mode == LassoMode.Add)
                {
                    Add_UI_Object(lasso);
                    added = true;
                }

                lasso.Add(GetPoint().ToPoint());
            }

          
        }

    }
    public override void OnMouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if (lasso.Mode != LassoMode.Dragging)
        {
            if(lasso.Points.Count == 0)
            {
                lasso.Clear();
            }
            else
            {
                selecting = false;
                lasso.Add(initial.ToPoint());
            }   

            lasso.Mode = LassoMode.Nothing;

        }
    }
  

    PointF GetPoint()
    {
        var pos = GetMousePos(lasso);
        
        return pos;
    }


}









public partial class T_Shape : T_BrushCustom
{

    [ObservableProperty] int thickness = 7;
    [ObservableProperty] Color color = Colors.Black;
    [ObservableProperty] Color color2 = Colors.White;

    [ObservableProperty] bool fill = true;
    public enum ShapeType
    {
        Rectangle,
        Line
    }
    [ObservableProperty] ShapeType selectedShape = ShapeType.Rectangle;
    public T_Shape()
    {
        
        name = "Shape";
        //iconPath = $"{App.LocalPath}Resources/Scripts/Selector/icon.png";
        brush.Type = "Shape";
        brush.isCustomCursor = true;
        cursor = Cursors.Cross;

        HotKey hotKey = new(Key.Q, ChangeToolToThis, "Shape Tool");


        add(new M_ComboBoxEnum(typeof(ShapeType), "SelectedShape") { OnSelectedChanged = SelectedChanged}, body);
        add(new M_SliderBox("Thickness", 0, 100, 100, 1), body);
        add(new M_ColorPicker("Color"), body);

        add(new M_CheckBox("Fill", "Fill"), body);
        add(new M_ColorPicker("Color2"), body);
    }

    void SelectedChanged(object value)
    {
        SelectedShape = AppModel.ParseEnum<ShapeType>(value.ToString());  
    }

    public override void OnToolSelected()
    {
        base.OnToolSelected();
    }


    public override void OnToolDeselected()
    {
        base.OnToolDeselected();
    }

    SKPoint initialPoint;
    SKBitmap original;
    public override void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        initialPoint = initialPosition.ToSKPoint();
        original = layer.Image.Copy();
    }
    public override void OnDraw(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        if (SelectedShape == ShapeType.Rectangle)
            layer.Image = DrawRectangle(original, initialPoint, finalPosition.ToSKPoint());
        else if (SelectedShape == ShapeType.Line)
        {
            if (Shortcuts.IsAltPressed || SelectedShot.Snap)
                finalPosition = CalculateSnap(new PointF(initialPoint.X, initialPoint.Y), finalPosition);           

            layer.Image = DrawLine(original, initialPoint, finalPosition.ToSKPoint());
        }
    }
    public override void OnMouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        original = null;
    }


    PointF CalculateSnap(PointF initialPosition, PointF finalPosition)
    {
        // Calcular las diferencias
        float deltaX = finalPosition.X - initialPosition.X;
        float deltaY = finalPosition.Y - initialPosition.Y;

        // Umbral para snapear
        float snapThreshold = 10.0f;

        if (Math.Abs(deltaX) < snapThreshold)
        {
            // Snapeo a línea vertical
            finalPosition.X = initialPosition.X;
        }
        else if (Math.Abs(deltaY) < snapThreshold)
        {
            // Snapeo a línea horizontal
            finalPosition.Y = initialPosition.Y;
        }
        else if (Math.Abs(deltaX - deltaY) < snapThreshold)
        {
            // Snapeo a 45 grados (diagonal ascendente)
            finalPosition.Y = initialPosition.Y + deltaX;
        }
        else if (Math.Abs(deltaX + deltaY) < snapThreshold)
        {
            // Snapeo a 45 grados (diagonal descendente)
            finalPosition.Y = initialPosition.Y - deltaX;
        }
        return finalPosition;
    }

    public SKBitmap DrawRectangle(SKBitmap originalBitmap, SKPoint startPoint, SKPoint endPoint)
    {
        // Crear un nuevo SKBitmap con el mismo tamaño que el original
        SKBitmap newBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);

        // Crear un SKCanvas para el nuevo bitmap
        using (var canvas = new SKCanvas(newBitmap))
        {
            // Copiar el contenido del bitmap original al nuevo
            canvas.DrawBitmap(originalBitmap, 0, 0);

            // Crear un rectángulo basado en los puntos del mouse
            var rect = new SKRect(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Max(startPoint.X, endPoint.X),
                Math.Max(startPoint.Y, endPoint.Y)
            );

            if (Fill)
            {
                // Dibujar el relleno del rectángulo
                using (var fillPaint = new SKPaint())
                {
                    fillPaint.Color = Color2.ToSKColor();          // Color de relleno (rojo)
                    fillPaint.Style = SKPaintStyle.Fill;      // Rellenar el rectángulo
                    fillPaint.IsAntialias = true;
                    fillPaint.FilterQuality = SKFilterQuality.High;

                    canvas.DrawRect(rect, fillPaint);
                }
            }

            // Dibujar el borde del rectángulo
            using (var strokePaint = new SKPaint())
            {
                strokePaint.Color = Color.ToSKColor();    // Color del borde
                strokePaint.Style = SKPaintStyle.Stroke;  // Dibujar solo el borde
                strokePaint.StrokeWidth = Thickness;      // Grosor del borde
                strokePaint.IsAntialias = true;
                strokePaint.FilterQuality = SKFilterQuality.High;

                canvas.DrawRect(rect, strokePaint);
            }
        }

        // Retornar el nuevo bitmap con el rectángulo dibujado
        return newBitmap;
    }



    public SKBitmap DrawLine(SKBitmap originalBitmap, SKPoint startPoint, SKPoint endPoint)
    {
        // Crear un nuevo SKBitmap con el mismo tamaño que el original
        SKBitmap newBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);

        // Crear un SKCanvas para el nuevo bitmap
        using (var canvas = new SKCanvas(newBitmap))
        {
            // Copiar el contenido del bitmap original al nuevo
            canvas.DrawBitmap(originalBitmap, 0, 0);

            // Configurar la pintura
            using (var paint = new SKPaint())
            {
                paint.Color = Color.ToSKColor();        // Color de la línea
                paint.Style = SKPaintStyle.Stroke;      // Dibujar solo el borde
                paint.StrokeWidth = Thickness;          // Grosor de la línea
                paint.IsAntialias = true;
                paint.FilterQuality = SKFilterQuality.High;

                // Dibujar la línea entre los puntos de inicio y final
                canvas.DrawLine(startPoint, endPoint, paint);
            }
        }

        // Retornar el nuevo bitmap con la línea dibujada
        return newBitmap;
    }



}

