using CefSharp.DevTools.DOM;
using Manual.API;
using Manual.Core;
using Manual.Objects.UI;
using ManualToolkit.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Plugins;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// Lógica de interacción para LassoView.xaml
/// </summary>
public partial class LassoView : UICanvasElement
{
    public LassoView()
    {
        InitializeComponent();

    }

    public override void OnThickChanged(double thick)
    {
        var offset = 1.8;
        polyLine.StrokeThickness = thick * offset;
        polyLine2.StrokeThickness = thick * offset;
    }



    private void UICanvasElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!Shortcuts.IsPanning)
        {
            if (DataContext == BindingOperations.DisconnectedSource) return;

            var lasso = (Lasso)DataContext;
            if (!lasso.Enabled) return;

            lasso.Mode = LassoMode.Dragging;
            initial = Shortcuts.MousePosition;

            initialPoints.Clear();

            foreach (var item in lasso.Points)
                initialPoints.Add(item);
            

            CaptureMouse();
            dragging = true;
            e.Handled = true;
        }
    }
    bool dragging;
    Point initial;
    List<Point> initialPoints = [];
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (dragging) // Asegura que el botón izquierdo está presionado
        {
            if (DataContext == BindingOperations.DisconnectedSource) return;
            var mousePosition = Shortcuts.MousePosition; // Obtiene la posición del ratón relativa al Canvas
            var lasso = ((Lasso)DataContext);

            if (lasso.Points.Count == initialPoints.Count)
            {
                for (int i = 0; i < lasso.Points.Count; i++)
                    lasso.Points[i] = PixelPoint.Minus(initialPoints[i], PixelPoint.Minus(initial, mousePosition));

                lasso.UpdateUIPoints();
            }
            e.Handled = true;
        }

    }

    private void UICanvasElement_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (dragging)
        {
            var lasso = ((Lasso)DataContext);
            lasso.Mode = LassoMode.Nothing;

            ReleaseMouseCapture();
            dragging = false;
            e.Handled = true;
        }
    }

    private void UICanvasElement_MouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext == BindingOperations.DisconnectedSource) return;

        var lasso = ((Lasso)DataContext);
        if(lasso.Mode == LassoMode.Nothing && !Shortcuts.IsPanning && lasso.Enabled)
        {
            //Cursor = Cursors.ScrollAll;
        }
    }

    private void UICanvasElement_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext == BindingOperations.DisconnectedSource) return;

        var lasso = ((Lasso)DataContext);
        if (lasso.Mode == LassoMode.Nothing && !Shortcuts.IsPanning && lasso.Enabled)
        {
            //Cursor = null;
        }
    }
}


public enum LassoMode
{
    Nothing,
    Dragging,
    Add,
    Remove
}
public class Lasso : UI_Object
{
    public bool Enabled;

    public LassoMode Mode = LassoMode.Nothing;
    private ObservableCollection<Point> _points = new ObservableCollection<Point>();
    public ObservableCollection<Point> Points
    {
        get => _points;
        set
        {
            _points = value;
            OnPropertyChanged(nameof(Points));
        }
    }

    public Lasso()
    {
        Width = 200;
        Height = 200;
    }

    public void UpdateUIPoints()
    {
        OnPropertyChanged(nameof(Points));
    }
    public void Add(Point point)
    {
        Points.Add(point);
        OnPropertyChanged(nameof(Points));
    }
    public void Clear()
    {
        Points.Clear();
        OnPropertyChanged(nameof(Points));
    }

    public bool HasSelection()
    {
        return Points.Any();
    }

    public LayerBase ClipLayer(LayerBase layer)
    {
        var bounds = new Layer();
        bounds.Image = layer.Image;
        bounds.CopyDimensions(layer);
        bounds.ShotParent = layer.ShotParent;

        var clipPath = CreateClipPath(Points);
        var clipped = layer.Image;

        clipPath.Offset(-bounds.PositionX, -bounds.PositionY);

        //  var clipped = RendGPU.RenderArea(bounds, layer);
        var result = new SKBitmap(layer.ImageWidth, layer.ImageHeight); //RendGPU.RenderArea(layer.ShotParent.MainCamera, layer);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.Transparent);  // Asegura un fondo transparente para áreas no recortadas
            canvas.ClipPath(clipPath, antialias: true);
            canvas.DrawBitmap(clipped, 0, 0);
        }


        bounds.Image = result;
        return bounds;
    }

  
    public static SKPath CreateClipPath(IEnumerable<Point> points)
    {
        var layer = ManualAPI.SelectedLayer;
        var lasso = ManualAPI.SelectedShot.Lasso;
        var width = (layer.RealWidth - lasso.RealWidth) / 2;
        var height = (layer.RealHeight - lasso.RealHeight) / 2;

        var path = new SKPath();
        if (points.Any())
        {
            var start = points.First();
            path.MoveTo((float)start.X + width, (float)start.Y + height);

            foreach (var point in points.Skip(1))
            {
                path.LineTo((float)point.X + width, (float)point.Y + height);
            }

            path.Close();
        }
        return path;
    }






}