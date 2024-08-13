using Manual.API;
using Manual.Core;
using Manual.Editors.Displays;
using Plugins;
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

namespace Manual.Objects.UI;

/// <summary>
/// Lógica de interacción para Transformer.xaml
/// </summary>
public partial class BoundingBoxView : UICanvasElement
{

    BoundingBox transformer
    {
        get
        {
            if (DataContext is BoundingBox a)
                return a;
            else
                return null;
        }
    }

    AnimateUI anim;
    public BoundingBoxView()
    {
        InitializeComponent();

    //    Shortcuts.CanvasMouseDown += PointMouseDown;
        Shortcuts.CanvasMouseMove += PointMouseMove;
        Shortcuts.CanvasMouseUp += PointMouseUp;


        anim = new AnimateUI(sizeStats, focusValue: 0.56, unFocusValue: 0, subscribeTo: this);



        
    }

  

    bool isDragging = false;
    FrameworkElement? currentPoint;
    Point initialTargetPos;
    PixelPoint initialPos;
    PixelPoint initialScale;
    private void PointMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (transformer != null && transformer.Enabled && ManualAPI.SelectedTool is T_ImageGenerator)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            if (hitTestResult != null && hitTestResult.VisualHit is FrameworkElement fe)
            {
                initialPos = Shortcuts.InitialMousePixelPosition;
                if(transformer != null)
                initialScale = transformer.ImageScale;


                if (fe.Name == "inside")
                {
                    currentPoint = fe;
                    isDragging = true;
                }
                else if (AppModel.FindAncestorContainsInName("point", fe) is Grid pointGrid)
                {
                    currentPoint = pointGrid;
                    isDragging = true;
                }

            }
        }



    }
    private void PointMouseUp(object sender, MouseButtonEventArgs e)
    {
        isDragging = false;
    }
    private void PointMouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging && Shortcuts.Dragging && transformer != null )
        {
            PixelPoint mousePos = e.GetPosition(this).ToPixelPoint();
            TransformCenter(mousePos);
        }
    }

    void TransformCorner(PixelPoint mousePos)
    {
       //
    }

    void TransformCenter(PixelPoint mousePos)
    {
        bool snap = ManualAPI.SelectedShot.Snap || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

        // Corners
        var trans = transformer;
        int width = 0;
        int height = 0;
        if (currentPoint == point_TopLeft)
        {
            width = initialScale.X - mousePos.X;
            height = initialScale.Y - mousePos.Y;
        }
        else if (currentPoint == point_TopRight)
        {
            width = mousePos.X;
            height = initialScale.Y - mousePos.Y;
        }
        else if (currentPoint == point_BottomRight)
        {
            width = mousePos.X;
            height = mousePos.Y;
        }
        else if (currentPoint == point_BottomLeft)
        {
            width = initialScale.X - mousePos.X;
            height = mousePos.Y;
        }

        // Sides
        else if (currentPoint == point_Left)
        {
            width = initialScale.X - mousePos.X;
        }
        else if (currentPoint == point_Top)
        {
           height = initialScale.Y - mousePos.Y;
        }
        else if (currentPoint == point_Right)
        {
            width = mousePos.X;
        }
        else if (currentPoint == point_Bottom)
        {
            height = mousePos.Y;
        }

 
        //inside
        else if (currentPoint == inside)
        {
            if (snap)
                trans.Position = ManualAPI.SelectedShot.SnapToJump((Point)(initialPos - Shortcuts.MouseDownDistance()));
            else
                trans.Position = ((Point)(initialPos - Shortcuts.MouseDownDistance())).ToPointPixel();

            return;
        }



        if (snap)
        {
            var snapped = ManualAPI.SelectedShot.SnapToJump(new Point(width, height));
            width = (int)snapped.X;
            height = (int)snapped.Y;

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (width > 0)
                {
                    width = (int)snapped.X;
                    height = width;
                }
            }
        }
        else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            if (width > 0)
            {
                height = width;
            }
        }





        //set scale
        if (width > 0)
            trans.ImageWidth = width;

        if (height > 0)
            trans.ImageHeight = height;


    }

    PixelPoint CalculateTransform(PixelPoint initialObj, PixelPoint initialMousePos, PixelPoint mousePos)
    {
        return initialObj - (initialMousePos - mousePos);
    }

    private void point_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var point = sender as FrameworkElement;

        if (!Shortcuts.IsPanning && point.DataContext is BoundingBox box && box.Enabled == true && ManualAPI.SelectedTool is T_ImageGenerator)
        {
            var trans = transformer;
            if (trans != null)
                initialScale = trans.ImageScale;

            initialPos = trans.Position.ToPixelPoint();

            currentPoint = point;
            isDragging = true;
        }


    }
}



