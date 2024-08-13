using LangChain.Base;
using Manual.API;
using Manual.Core;
using ManualToolkit.Generic;
using SkiaSharp;
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
/// Lógica de interacción para KeyframeView.xaml
/// </summary>
public partial class KeyframeView : UserControl
{

    public KeyframeView()
    {
        InitializeComponent();
        Focusable = true;

    }
    Point startPoint;
    Dock handler = Dock.Left;
    bool dragging = false;
    Point initialMousePosition;
    private void Handle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if(sender == LeftHandle)
        {
            handler = Dock.Left;
            startPoint = ((Keyframe)DataContext).LeftHandle;
            LeftHandle.CaptureMouse();
        }
        else if(sender == RightHandle)
        {
            handler = Dock.Right;
            startPoint = ((Keyframe)DataContext).RightHandle;
            RightHandle.CaptureMouse();

         
        }
        initialMousePosition = Mouse.GetPosition(this);
        dragging = true;

        e.Handled = true;
    }

    private void Handle_MouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            var mousePos = Mouse.GetPosition(this);
            var k = (Keyframe)DataContext;
            if(handler == Dock.Left)
            {
                k.LeftHandle = startPoint.Add(deltaMousePoint(e));
            }
            else if (handler == Dock.Right)
            {
                k.RightHandle = startPoint.Add(deltaMousePoint(e));
            }

            if(ManualAPI.Animation.IsPlaying)
              k.AttachedTimedVariable.UpdateGraph();

        }
    }

    private void Handle_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        dragging = false;

        RightHandle.ReleaseMouseCapture();
        LeftHandle.ReleaseMouseCapture();
    }

    private PixelPoint deltaMousePoint(MouseEventArgs e)
    {
        Point mousePosition = e.GetPosition(this);
        Vector delta = Point.Subtract(mousePosition, initialMousePosition);

        return PixelPoint.Divide(delta, ManualAPI.SelectedLayer._Animation.CanvasScale);
    }

}
