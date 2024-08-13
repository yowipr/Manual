using Manual.Core;
using Manual.Editors;
using ManualToolkit.Generic;
using Plugins;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Manual.Objects;


public abstract class CanvasMatrix : Canvas
{
    public abstract Matrix CanvasTransform { get; set; }

    public bool IsDotPattern = true;
    protected override void OnRender(DrawingContext dc)
    {
    
        base.OnRender(dc);
    }

  

}

/// <summary>
/// Lógica de interacción para CanvasAreaControl.xaml
/// </summary>
public partial class CanvasAreaControl : CanvasMatrix
{

    // Registra la propiedad de dependencia
    public static readonly DependencyProperty ClickPanProperty = DependencyProperty.Register(
        nameof(ClickPan),
        typeof(bool),
        typeof(CanvasAreaControl),
        new PropertyMetadata(false) // Valor predeterminado es false
    );

    // Propiedad pública para acceder a la propiedad de dependencia
    public bool ClickPan
    {
        get { return (bool)GetValue(ClickPanProperty); }
        set { SetValue(ClickPanProperty, value); }
    }


    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<IPositionable>),
            typeof(CanvasAreaControl),
            new PropertyMetadata(null));

    public IEnumerable<IPositionable> ItemsSource
    {
        get { return (IEnumerable<IPositionable>)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }


    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CanvasAreaControl instance)
            instance.UpdateCanvasTransform();
    }

    public static readonly DependencyProperty CanvasTransformProperty =
      DependencyProperty.Register(
      nameof(CanvasTransform),
      typeof(Matrix),
      typeof(CanvasAreaControl),
      new FrameworkPropertyMetadata(Matrix.Identity,
      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
      OnValueChanged,
      CoerceValue,
      true,
      UpdateSourceTrigger.PropertyChanged));

    public readonly MatrixTransform _transform = new MatrixTransform();

    public override Matrix CanvasTransform
    {
        get { return (Matrix)GetValue(CanvasTransformProperty); }
        set
        {
            SetValue(CanvasTransformProperty, value);
            UpdateCanvasTransform();
        }
    }


    public bool CachedElements = false;

    public double MinZoom = 0.1;
    public double MaxZoom = 1.5;

    public void UpdateCanvasTransform()
    {
        _transform.Matrix = CanvasTransform;

        foreach (UIElement child in Children)
        {

            if (child is FrameworkElement frameworkElement)
            {

                if (frameworkElement.Tag is string tag)
                {

                    //UIType ignores X and Y
                    //UITypeX ignores Y
                    //UITypeY ignores X
                    if (tag == "UIType")
                    {
                        // Ignorar el elemento con el Tag "UIType"
                        continue;
                    }
                    else if (tag == "UITypeX")
                    {
                        TranslateTransform translateTransform = new TranslateTransform(_transform.Matrix.OffsetX, 0);
                        child.RenderTransform = translateTransform;
                        continue;
                    }
                    else if (tag == "UITypeY")
                    {
                        TranslateTransform translateTransform = new TranslateTransform(0, _transform.Matrix.OffsetY);
                        child.RenderTransform = translateTransform;
                        continue;
                    }

                    //if (CanvasTransform.M22 > 0.6)
                    //{
                    //    child.CacheMode = null;
                    //}

                  //  AppModel.mainW.SetMessage(CanvasTransform.M22.ToString());
                }
            }

            UpdateGrid2D();
            child.RenderTransform = _transform;
        }


    }

    void UpdateGrid2D()
    {
        if (Background is not SolidColorBrush && Background.Transform != _transform)
            Background.Transform = _transform;
    }



    public CanvasAreaControl()
    {
        InitializeComponent();
        Focusable = true;
        CanvasTransform = new Matrix(1, 0, 0, 1, 19, 97);

        Unloaded += CanvasAreaControl_Unloaded;

    }

    private void CanvasAreaControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (panTimer != null)
        {
            panTimer.Stop();
            panTimer.Tick -= PanTimer_Tick;
            panTimer = null;
        }
    }

    private void Canvas_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGrid2D();
    }




    public bool isPan = false;
    bool isPanned = false;
    bool isPanning = false;
    public bool dragging = false;
    private void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            isPan = true;
            Cursor = Cursors.Hand;
        }
    }
    private void Canvas_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            Cursor = null;
            isPan = false;
        }
    }

    Point initialMousePosition;
    Point initialMousePositionCanvas;
    Matrix initialCanvasTransform;

    double initialScaleX = 1;
    double initialScaleY = 1;
    private void canvas_MouseEnter(object sender, MouseEventArgs e)
    {
        Focus();
    }




    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
 
        Focus();
        if (e.ChangedButton == MouseButton.Left)
        {
            Point mousePosition = e.GetPosition(this);
            var hit = HitTest(e);
            if (isPan || (ClickPan && hit == this))
            {
              
                initialMousePositionCanvas = _transform.Inverse.Transform(Mouse.GetPosition(this));
                initialMousePosition = mousePosition;
                initialCanvasTransform = CanvasTransform;

                //   initialScaleX = CanvasScaleX;
                //  initialScaleY = timelineContext.CanvasScaleY;

               // e.Handled = true; TODO: esto era por algo

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                {
                    selector.StartSelect(_transform);
                }
                else
                {
                    isPanning = true;
                    isPanned = true;


                    CanvasAreaControl.SetLowQuality(this);

                  //  if (CanvasTransform.M22 < LowSize || Shortcuts.IsAltPressed)
                  //      AppModel.SetLowQuality(this);

                }

                //  initialCanvasPosition = new Point(timelineContext.GlobalCanvasTransformX, timelineContext.GlobalCanvasTransformY);
                return;
            }
            else
            {
                dragging = true;


                panTimer = new DispatcherTimer();
                panTimer.Interval = TimeSpan.FromMilliseconds(16); // Aproximadamente 60 fps
                panTimer.Tick += PanTimer_Tick;

                panTimer.Start();
            }


        }
    }

    double LowSize = 0.5;
    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        isPanning = false;

        if (CanvasTransform.M22 > LowSize)
            CanvasAreaControl.SetNormalQuality(this);


        if (!isPan)
            isPanned = false;

        dragging = false;

        if (selector.isSelecting)
        {
            var rect = selector.EndSelect(_transform);
            OnEndSelect?.Invoke(rect);
        }
        if (panTimer != null)
        {
            panTimer.Stop();
            panTimer.Tick -= PanTimer_Tick;
            panTimer = null;
        }
    }
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (isPanning)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    DoZoom();
                else
                    DoPan();

                return;
            }
            else if (selector.isSelecting)
            {
                selector.DoSelect(_transform);
            }
            else
            {
                PanBorder();
            }
           
        }

    }
    private DispatcherTimer panTimer;
    private void PanTimer_Tick(object sender, EventArgs e)
    {
        if (!dragging) return;
        PanBorder();
    }
    void PanBorder()
    {
     
        var position = Mouse.GetPosition(this);
        var canvasWidth = this.ActualWidth;
        var canvasHeight = this.ActualHeight;

        double panSpeed = 2.0; // Ajusta la velocidad del pan según sea necesario

        // Desplazar si el mouse está cerca de los bordes
        if (position.X < 20)
        {
            // Mouse cerca del borde izquierdo
            PanCanvas(panSpeed, 0);
        }
        else if (position.X > canvasWidth - 20)
        {
            // Mouse cerca del borde derecho
            PanCanvas(-panSpeed, 0);
        }

        if (position.Y < 20)
        {
            // Mouse cerca del borde superior
            PanCanvas(0, panSpeed);
        }
        else if (position.Y > canvasHeight - 20)
        {
            // Mouse cerca del borde inferior
            PanCanvas(0, -panSpeed);
        }
    }
    private void PanCanvas(double offsetX, double offsetY)
    {
        var translate = new TranslateTransform(offsetX, offsetY);
        CanvasTransform = translate.Value * CanvasTransform;

    }

    public Action<Rect> OnEndSelect;

    
    public static void SetLowQuality(Canvas target)
    {
        AppModel.SetLowQuality(target);

        foreach (UIElement child in target.Children)
        {
            child.CacheMode ??= new BitmapCache();
        }

    }
    public static void SetNormalQuality(Canvas target)
    {
        AppModel.SetNormalQuality(target);

        foreach (UIElement child in target.Children)
        {
            if (child.CacheMode != null)
                child.CacheMode = null;
        }

    }

    bool _awaitRender = false;
    void DoPan()
    {

        if (_awaitRender)
            return;

        Point mousePosition = _transform.Inverse.Transform(Mouse.GetPosition(this));
        Vector delta = Point.Subtract(mousePosition, initialMousePositionCanvas);
        var translate = new TranslateTransform(delta.X, delta.Y);
        CanvasTransform = translate.Value * CanvasTransform;


        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    private Point _previousMousePosition;
    private DateTime _previousMousePositionTime;
    void DoZoom()
    {
        if (_awaitRender)
            return;

        Point currentMousePosition = Mouse.GetPosition(this);


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

        Matrix scaleMatrix = CanvasTransform;

        var mf = scaleMatrix.M11 * scaleFactor;
        //limit scale minsize minscale
        //zoom out         zoom in

        if (mf > MinZoom &&  mf < MaxZoom && scaleFactor > 0)
        {
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, initialMousePosition.X, initialMousePosition.Y);
            CanvasTransform = scaleMatrix;
        }

        // Almacenar la posición actual del mouse para el próximo evento MouseMove
        _previousMousePosition = currentMousePosition;
        _previousMousePositionTime = DateTime.Now;


        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

    }

    public Point mousePosition(Point mousepos)
    {
        return _transform.Inverse.Transform(mousepos);
    }
    public Point mousePosition()
    {
        return _transform.Inverse.Transform(Mouse.GetPosition(this));
    }
    public Point elementPosition(FrameworkElement element)
    {
        try
        {
            // var transform = element.TransformToAncestor(this);
            var transform = element.TransformToVisual(this);
            var point = transform.Transform(new Point(0, 0));
            return _transform.Inverse.Transform(point);
        }
        catch
        {
            return new Point(0, 0);
        }
    }
    public Point elementPositionCenter(FrameworkElement element)
    {
        var position = elementPosition(element);

        // Calcular el centro del Ellipse
        double centerX = position.X + element.ActualWidth / 2;
        double centerY = position.Y + element.ActualHeight / 2;

        // Establecer el centro como startPoint
        return new Point(centerX, centerY);
    }






    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        Matrix scaleMatrix = CanvasTransform;

        //OFFSET
        var offset = 0.1;

        var scaleFactor = 1 + offset;
        if (e.Delta > 0)
        {
            // La rueda del ratón se movió hacia arriba
        }
        else if (e.Delta < 0)
        {
            // La rueda del ratón se movió hacia abajo
            scaleFactor = 1 - offset;
        }


        scaleMatrix.ScaleAt(scaleFactor, scaleFactor, initialMousePosition.X, initialMousePosition.Y);
        CanvasTransform = scaleMatrix;
    }





    //---------------------------------------------------------------------------------------------------------------------- EVENTS

    public delegate void CanvasMouseEvent(Point mousePosition, bool isDragging);
    public event CanvasMouseEvent CanvasMouseMove;


    public FrameworkElement? HitTestOld(MouseButtonEventArgs e)
    {
        Point mouseUpPoint = e.GetPosition(this);
        HitTestResult result = VisualTreeHelper.HitTest(this, mouseUpPoint);

        if (result != null && result.VisualHit is FrameworkElement hitElement)
            return hitElement;
        else
            return null;
    }


    public FrameworkElement? HitTest(MouseButtonEventArgs e)
    {
        Point mouseUpPoint = e.GetPosition(this);
        return HitTest(mouseUpPoint);
    }


    public FrameworkElement? HitTest(Point mousepos)
    {
        return HitTest(this, mousepos);
    }

    public static FrameworkElement? HitTest(FrameworkElement element, Point mousepos)
    {
        FrameworkElement hitElement = null;

        HitTestResultCallback resultCallback = (result) =>
        {
            if (result.VisualHit is FrameworkElement element)
            {
                if (element.Tag is string tag && tag == "IgnoreHit" || !AppModel.IsElementActuallyVisible(element))
                    return HitTestResultBehavior.Continue;

                hitElement = element;
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        };

        VisualTreeHelper.HitTest(element, null, resultCallback, new PointHitTestParameters(mousepos));

        return hitElement;
    }

    public FrameworkElement? HitTest2(MouseButtonEventArgs e)
    {
        Point mouseUpPoint = e.GetPosition(this);
        List<HitTestResult> hits = new List<HitTestResult>();

        VisualTreeHelper.HitTest(
            this,
            null,
            result =>
            {
                hits.Add(result);
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(mouseUpPoint)
        );

        // Recorre los resultados en orden inverso (los más cercanos al usuario primero)
        for (int i = hits.Count - 1; i >= 0; i--)
        {
            if (hits[i].VisualHit is FrameworkElement element && !(element is Canvas))
            {
                return element;
            }
        }

        return null;
    }













    //-------------------------------------------------------------------------- CANVAS FOCUS



}











public class DottedCanvas : Canvas
{
    public static readonly DependencyProperty CanvasMatrixProperty =
        DependencyProperty.Register(
            nameof(CanvasMatrix),
            typeof(Matrix),
            typeof(DottedCanvas),
            new FrameworkPropertyMetadata(Matrix.Identity, FrameworkPropertyMetadataOptions.AffectsRender));

    public Matrix CanvasMatrix
    {
        get { return (Matrix)GetValue(CanvasMatrixProperty); }
        set { SetValue(CanvasMatrixProperty, value); }
    }


    protected override void OnRender(DrawingContext dc)
    {
        RenderDots(this, CanvasMatrix, dc);
    }


    public static void RenderDots(Canvas canvas, Matrix CanvasMatrix, DrawingContext dc)
    {

        var canvasScaleX = CanvasMatrix.M11;
        var canvasScaleY = CanvasMatrix.M22;
        var globalCanvasTransformX = CanvasMatrix.OffsetX;
        var globalCanvasTransformY = CanvasMatrix.OffsetY;

        // Determinar la frecuencia de los puntos en función del zoom
        int frameStep = DetermineSteps(canvasScaleX);

        // Calcular el inicio y el final basado en el desplazamiento y el tamaño del canvas
        double startX = Math.Ceiling(-globalCanvasTransformX / canvasScaleX / frameStep) * frameStep;
        double endX = Math.Floor((canvas.ActualWidth - globalCanvasTransformX) / canvasScaleX / frameStep) * frameStep;
        double startY = Math.Ceiling(-globalCanvasTransformY / canvasScaleY / frameStep) * frameStep;
        double endY = Math.Floor((canvas.ActualHeight - globalCanvasTransformY) / canvasScaleY / frameStep) * frameStep;

        double pointSize = 3;

        for (double x = startX; x <= endX; x += frameStep)
        {
            for (double y = startY; y <= endY; y += frameStep)
            {
                double xPosition = x * canvasScaleX + globalCanvasTransformX;
                double yPosition = y * canvasScaleY + globalCanvasTransformY;

                // Determinar si el punto es principal o secundario
                bool isPrimaryPoint = ((int)(x / frameStep) + (int)(y / frameStep)) % 2 == 0;

                byte g = 80;
                var brush = new SolidColorBrush(isPrimaryPoint ? Color.FromArgb(255, g, g, g) : Color.FromArgb(255, g, g, g));

                // Asegúrate de que el punto se dibuje dentro de los límites del canvas
                if (xPosition >= 0 && xPosition <= canvas.ActualWidth && yPosition >= 0 && yPosition <= canvas.ActualHeight)
                {
                    dc.DrawEllipse(brush, null, new Point(xPosition, yPosition), pointSize, pointSize);
                }
            }
        }
    }

    protected static int DetermineSteps(double canvasScaleX)
    {
        return GetSteps(canvasScaleX, 64, 2);
    }

    public static int GetSteps(double canvasScaleX, int start, int jumps)
    {
        if (double.IsInfinity(canvasScaleX))
            return start;

        int baseStep = 1;
        double scaleLog = Math.Log(canvasScaleX / start, jumps);
        int exponent = (int)Math.Floor(scaleLog);
        return baseStep << Math.Abs(exponent);
    }



}




