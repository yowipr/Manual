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
using System.Windows.Media.Media3D;
using System.Reflection.Metadata;
using Manual;

namespace Plugins;

[Export(typeof(IPlugin))]
public class MovementTool : IPlugin
{
    public void Initialize()
    {
        Tool.Register(new T_Pan());
        Tool.Register(new T_Zoom());

        //DISABLED tools: rot, eraser, distort, text

        // Tool.Register(T_Rot.Instance);

        // Tool.Register(new T_Eraser());
        // Tool.Register(new T_Distort());
        //  Tool.Register(new T_Text());

    }

}

public class T_Zoom : Tool
{
    public T_Zoom()
    {
        name = "Zoom";
        iconPath = $"{App.LocalPath}Resources/Scripts/Zoom/icon.png";
        cursor = Cursors.ScrollWE;
    }

    public override void OnToolSelected()
    {
        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp += Shortcuts_CanvasMouseUp;

    }
    public override void OnToolDeselected()
    {
        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp -= Shortcuts_CanvasMouseUp;

    }
    bool dragging;
    Point initialMousePosition;
    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
      //  initialMousePosition = Shortcuts.MousePosition;
        initialMousePosition = Shortcuts.GetMousePos(e);
        dragging = true;
    }
    private void Shortcuts_CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        ActionHistory.IsOnAction = false;
        dragging = false;
    }
    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            DoZoom(e);
        }
    }

    bool _awaitRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }
    private Point _previousMousePosition;
    private DateTime _previousMousePositionTime;
    void DoZoom(MouseEventArgs e)
    {
        if (_awaitRender)
            return;

        Point currentMousePosition = Shortcuts.GetMousePos(e);


        // Calcular la velocidad del mouse
        double timeDiff = (DateTime.Now - _previousMousePositionTime).TotalMilliseconds;
        double distance = currentMousePosition.X - _previousMousePosition.X;
        double speed = distance / timeDiff;



        int zoomOffset = 20;

        // Calcular el factor de escala basado en la velocidad del mouse
        double scaleFactor = 1.0;

        double absSpeed = Math.Abs(speed);

        if (absSpeed > 0)
        {
            if (speed > 0)
                scaleFactor += absSpeed / zoomOffset; // ajustar el divisor según tu preferencia               
            else
                scaleFactor -= absSpeed / zoomOffset;
        }

        Matrix scaleMatrix = Shortcuts.CurrentCanvas.CanvasTransform;

        var mf = Math.Abs(scaleMatrix.M11 * scaleFactor);

        //limit scale minsize minscale
        //zoom out         zoom in
        if (mf > 0.02 && mf < 100)
        {
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, initialMousePosition.X, initialMousePosition.Y);
            Shortcuts.CurrentCanvas.CanvasTransform = scaleMatrix;
        }

        // Almacenar la posición actual del mouse para el próximo evento MouseMove
        _previousMousePosition = currentMousePosition;
        _previousMousePositionTime = DateTime.Now;


        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }




}




public class T_Pan : Tool
{
 //   public static T_Pan Instance = new T_Pan();
    HotKey zoomKey = new(KeyComb.Shift, "Zoom at Pan");
    public T_Pan()
    {
        //SHORTCUTS
       //   HotKey hotKey = new(KeyComb.None, Key.Space, ChangeTool, ResetTool, "Pan Tool");


        // TOOL PROPERTIES
        name = "Pan";
        iconPath = $"{App.LocalPath}Resources/Scripts/Pan/icon.png";
        cursor = Cursors.Hand;

    }

    void ChangeTool()
    {
        if (SelectedTool != this)
        {
            //  body = SelectedTool.body;
            //    body.DataContext = SelectedTool.body.DataContext;

          //  elements = SelectedTool.elements;

            SelectedTool = this;
        }
    }
    void ResetTool()
    {
        SelectedTool = project.toolManager.OldTool;
    }

    public override void OnToolSelected()
    {

        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;
        Shortcuts.CanvasMouseUp += Shortcuts_CanvasMouseUp;

        Shortcuts.KeyDown += Shortcuts_KeyDown;
        Shortcuts.KeyUp += Shortcuts_KeyUp;

        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        {
            ZoomMode = true;
            cursor = Cursors.ScrollWE;
        }
    }

    private void Shortcuts_CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        ActionHistory.IsOnAction = false;
        dragging = false;
    }

    public override void OnToolDeselected()
    {
        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;

        Shortcuts.KeyDown -= Shortcuts_KeyDown;
        Shortcuts.KeyUp -= Shortcuts_KeyUp;

        cursor = Cursors.Hand;
        ZoomMode = false;
        ActionHistory.IsOnAction = false;
    }
    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
       if(e.Key == Key.RightAlt)
        {
           // MessageBox.Show("hola");
           cursor = Cursors.ScrollWE;
           ZoomMode = true;
        }
        ActionHistory.IsOnAction = false;
    }
    private void Shortcuts_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.RightAlt)
        {
            cursor = Cursors.Hand;
            ZoomMode = false;
        }
    }


    bool ZoomMode = false;
    bool dragging = false;
    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        Shortcuts.Dragging = false;

        initialMousePosition = Shortcuts.GetMousePos(e);
        _initialMousePosition = Shortcuts.MousePosition;

         initialCanvasMatrix = Shortcuts.CurrentCanvas.RenderTransform.Value;
        dragging = true;
    }

    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            // if (ZoomMode || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            if (ZoomMode || zoomKey.IsSubKeysPressed())
            {
                ZoomMode = true;
                if (cursor != Cursors.ScrollWE)
                    cursor = Cursors.ScrollWE;

                DoZoom(e);
            }
            else
            {
                DoPan(e);
            }
        }
    }

    Matrix initialCanvasMatrix;
    Point initialMousePosition;
    Point _initialMousePosition;
    //  double initialScale;
    void DoPan(MouseEventArgs e)
    {

        //  Point currentMousePosition = Shortcuts.ActualMousePosition;

        //Vector delta = Point.Subtract(currentMousePosition, initialMousePosition);
        //delta.X += initialCanvasMatrix.OffsetX;
        //delta.Y += initialCanvasMatrix.OffsetY;

        //var translate = new TranslateTransform(delta.X, delta.Y);

        //Shortcuts.CurrentCanvas.CanvasTransform = translate.Value;



        Point mousePosition = Shortcuts.MousePosition;
        Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
        var translate = new TranslateTransform(delta.X, delta.Y);
        Shortcuts.CurrentCanvas.CanvasTransform = translate.Value * Shortcuts.CurrentCanvas.CanvasTransform;

    }


    private Point _previousMousePosition;
    private DateTime _previousMousePositionTime;
    void DoZoom(MouseEventArgs e)
    {
        Point currentMousePosition = Shortcuts.GetMousePos(e);


        // Calcular la velocidad del mouse
        double timeDiff = (DateTime.Now - _previousMousePositionTime).TotalMilliseconds;
        double distance = currentMousePosition.X - _previousMousePosition.X;
        double speed = distance / timeDiff;



        int zoomOffset = 20;

        // Calcular el factor de escala basado en la velocidad del mouse
        double scaleFactor = 1.0;

        double absSpeed = Math.Abs(speed);

        if (absSpeed > 0)
        {
            if (speed > 0)
                scaleFactor += absSpeed / zoomOffset; // ajustar el divisor según tu preferencia               
            else
                scaleFactor -= absSpeed / zoomOffset;
        }

        Matrix scaleMatrix = Shortcuts.CurrentCanvas.CanvasTransform;
        scaleMatrix.ScaleAt(scaleFactor, scaleFactor, initialMousePosition.X, initialMousePosition.Y);
        Shortcuts.CurrentCanvas.CanvasTransform = scaleMatrix;

        // Almacenar la posición actual del mouse para el próximo evento MouseMove
        _previousMousePosition = currentMousePosition;
        _previousMousePositionTime = DateTime.Now;

    }

}

public class T_Rot : Tool
{
    public static T_Rot Instance = new T_Rot();

    M_NumberBox rotationbox = new();
    public T_Rot()
    {
        //SHORTCUTS
        HotKey hotKey = new(KeyComb.None, Key.K, ChangeTool, "Rotation Tool");


        // TOOL PROPERTIES
        name = "Rotation";
        iconPath = $"{App.LocalPath}Resources/Scripts/Pan/icon2.png";
        cursor = Cursors.ArrowCD;

        add(rotationbox, body);

    }
    void ChangeTool()
    {
        SelectedTool = this;
    }

    public override void OnToolSelected()
    {
        Shortcuts.CanvasMouseMove += Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown += Shortcuts_CanvasMouseDown;

        Shortcuts.KeyDown += Shortcuts_KeyDown;

    }

    public override void OnToolDeselected()
    {
        Shortcuts.CanvasMouseMove -= Shortcuts_CanvasMouseMove;
        Shortcuts.CanvasMouseDown -= Shortcuts_CanvasMouseDown;

        Shortcuts.KeyDown -= Shortcuts_KeyDown;
    }



    private void Shortcuts_CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Shortcuts.CurrentCanvas.RenderTransform is RotateTransform rotateTransform)
        {
            initialRotation = rotateTransform.Angle;
            initialMousePosition = MousePosition;
        }

    }

    double initialRotation = 0;
    Point initialMousePosition;
    private void Shortcuts_CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (Shortcuts.Dragging)
        {
            double actualAngle = 0;
            if (Shortcuts.CurrentCanvas.RenderTransform is RotateTransform rotateTransform2)
            {
               actualAngle = rotateTransform2.Angle; 
            }
            
            double angle = MousePosition.X - initialMousePosition.X;
            angle = rotationbox.Value;
            RotateCanvas(angle);

           // actualAngle += angle;
          //  RotateTransform rotateTransform = new RotateTransform(actualAngle);
          //  Shortcuts.CurrentCanvas.RenderTransform = rotateTransform;
        }
    }

    private void Shortcuts_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.RightAlt && Shortcuts.Dragging)
        {
            Shortcuts.CurrentCanvas.RenderTransform = new RotateTransform(0);
        }
    }


    static void RotateCanvas(double angle)
    {
        // Obtener la matriz actual
        Matrix transformMatrix = Shortcuts.CurrentCanvas.CanvasTransform;

        // Define el punto alrededor del cual quieres rotar el Canvas.
        // Esto podría ser el centro del Canvas o cualquier otro punto.
        double centerX = 0;//Shortcuts.CurrentCanvas.Width / 2;
        double centerY = 0;//Shortcuts.CurrentCanvas.Height / 2;

        // Rotar la matriz
        transformMatrix.RotateAtPrepend(angle, centerX, centerY);

        // Aplicar la matriz modificada
        Shortcuts.CurrentCanvas.CanvasTransform = transformMatrix;
    }



}



