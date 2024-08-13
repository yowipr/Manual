using Manual.API;
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
/// Lógica de interacción para RectangleSelectorView.xaml
/// </summary>
public partial class RectangleSelectorView : UserControl
{
    public RectangleSelectorView()
    {
        InitializeComponent();
    }


    bool _isSelecting = false;
    public bool isSelecting
    {
        get => _isSelecting;
        set
        {
            if (_isSelecting != value)
            {
                if (value)
                    Visibility = Visibility.Visible;
                else
                    Visibility = Visibility.Collapsed;

                _isSelecting = value;
            }
        }
    }

    public Point initialSelectorPos;
    Point initialMousePositionCanvas;
    public void StartSelect(MatrixTransform _transform)
    {

        var canvas = Parent as Canvas;

        Point mousePosition = _transform.Inverse.Transform(Mouse.GetPosition(canvas));
        initialMousePositionCanvas = mousePosition;

        var x = mousePosition.X * _transform.Matrix.M11; //CanvasTransform.M11;
        var y = mousePosition.Y * _transform.Matrix.M22; //CanvasTransform.M22;

        Canvas.SetLeft(this, x);
        Canvas.SetTop(this, y);

        initialSelectorPos = new Point(x, y);

        Width = 1;
        Height = 1;

        isSelecting = true;

        CaptureMouse();
    }

    public Rect DoSelect(MatrixTransform _transform)
    {
        
        var canvas = Parent as Canvas;

        Point mousePosition = _transform.Inverse.Transform(Mouse.GetPosition(canvas));
        Vector delta = Point.Subtract(mousePosition, initialMousePositionCanvas);

        var x = mousePosition.X * _transform.Matrix.M11; //CanvasTransform.M11;
        var y = mousePosition.Y * _transform.Matrix.M22; //CanvasTransform.M22;

        var _sx = Math.Abs(delta.X);
        var _sy = Math.Abs(delta.Y);
        var scaleX = _sx > 1 ? _sx : 1;
        var scaleY = _sy > 1 ? _sy : 1;


        if (x > initialSelectorPos.X)
        {
            Width = scaleX;
        }
        else
        {
            Canvas.SetLeft(this, x);
            Width = scaleX;
        }


        if (y > initialSelectorPos.Y)
        {
            Height = scaleY;
        }
        else
        {
            Canvas.SetTop(this, y);
            Height = scaleY;
        }
        var pos = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        var size = new Size(Width, Height);

        return new Rect(pos, size);

    }

    Rect rect;

    public Rect EndSelect(MatrixTransform _transform)
    {
        var result = DoSelect(_transform);

        Canvas.SetTop(this, 0);
        Canvas.SetLeft(this, 0);
        Width = 1;
        Height = 1;



        if (isSelecting == true)
        {
            isSelecting = false;
            return result;
        }

        ReleaseMouseCapture();

        return new Rect(new Point(0, 0), new Size(0, 0));
    }

}
