
using Manual.Core;
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
using System.IO;
using Microsoft.Win32;
using System.Net;
using Manual.Editors;
using System.ComponentModel;
using System.Windows.Media.Media3D;
using System.Windows.Input.StylusPlugIns;
using WintabDN;
using System.Runtime.InteropServices;
using Manual.API;
using System.Xml.Serialization;
using static Manual.API.ManualAPI;
using ManualToolkit.Generic;
using SkiaSharp;
using System.Reflection;
using System.Windows.Controls.Primitives;
using ManualToolkit.Windows;
using Manual.Core.Nodes.ComfyUI;
using Manual.Core.Graphics;

namespace Manual.Objects;


public partial class CanvasArea : CanvasMatrix
{
    public static readonly DependencyProperty RealThickProperty = DependencyProperty.Register(
    "RealThick",
    typeof(double),
    typeof(CanvasArea),
    new FrameworkPropertyMetadata(
        defaultValue: 1.0,
        flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        propertyChangedCallback: OnRealThickChanged
    ));

    public double RealThick
    {
        get { return (double)GetValue(RealThickProperty); }
        set { SetValue(RealThickProperty, value); }
    }
    private static void OnRealThickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CanvasArea)d;
        // Aquí puedes añadir lógica adicional si necesitas reaccionar al cambio de RealThick.
        // Por ejemplo, puedes necesitar actualizar otros componentes UI o valores dependientes.
        // Console.WriteLine("RealThick ha cambiado a: " + e.NewValue);
    }





    public bool NewMethod = true;


    public event EventMatrix OnMatrixChanged;

    public static readonly DependencyProperty CanvasTransformProperty =
     DependencyProperty.Register(
        nameof(CanvasTransform),
         typeof(Matrix),
         typeof(CanvasArea),
         new FrameworkPropertyMetadata(Matrix.Identity,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    // double MinScale = 0.0543644;
    //   double MaxScale = 100.0;
    public override Matrix CanvasTransform
    {
        get { return (Matrix)GetValue(CanvasTransformProperty); }
        set
        {
            //limit scale
            // value.M11 = Math.Min(Math.Max(value.M11, MinScale), MaxScale);
            //  value.M22 = Math.Min(Math.Max(value.M22, MinScale), MaxScale);
            //   AppModel.mainW.l_textblock.Text = $"M11: {value.M11}, M22: {value.M22}";

            SetValue(CanvasTransformProperty, value);

            Dispatcher.BeginInvoke(() =>
           {
           _transform.Matrix = CanvasTransform;
           UpdateGrid2D();
           foreach (UIElement child in this.Children)
           {
               child.RenderTransform = _transform;
           }


           SetQualityCanvas();


           }, System.Windows.Threading.DispatcherPriority.Render);
        
            RealThick = 1 / value.M11;
            OnMatrixChanged?.Invoke(value);

        }
    }


    void SetQualityCanvas()
    {
        if (CanvasTransform.M22 > 2)
        {
            if (RenderOptions.GetBitmapScalingMode(this) == BitmapScalingMode.HighQuality)
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        }
        else
        {
            if (RenderOptions.GetBitmapScalingMode(this) == BitmapScalingMode.NearestNeighbor)
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        }
    }

    void UpdateGrid2D()
    {
        if (Background is not SolidColorBrush && Background.Transform != _transform)
            Background.Transform = _transform;
    }

    private void Canvas_Loaded(object sender, RoutedEventArgs e)
    {
        // ui_object
        _transform.Matrix = CanvasTransform;//transform.Matrix;
        foreach (UIElement child in this.Children)
        {
            child.RenderTransform = _transform;
        }

        // thick
        var view = AppModel.FindAncestor<CanvasView>(this);
        ED_CanvasView model = view.DataContext as ED_CanvasView;
        model.CanvasMatrix = view.canvas.CanvasTransform;
        //model.UpdateThick();


        // grid
        Settings.InvokeOnCurrentBgGrid();
        UpdateGrid2D();

    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

    }



    [XmlIgnore] public readonly MatrixTransform _transform = new MatrixTransform();
    public float Zoomfactor { get; set; } = 1.1f;
    public Action? cancelAnimation { get; internal set; } = null;

    public CanvasArea()
    {
       Settings.CurrentBgGridChanged += Settings_CurrentBgGridChanged;


        InitializeComponent();

        Focusable = true;


        if (!NewMethod)
        {
            MouseEnter += myCanvas_MouseEnter;
            MouseLeave += myCanvas_MouseLeave;

            MouseWheel += CanvasArea_MouseWheel; //TODO: mouse wheel, no tengo forma de comprobar esto XD
        }
        Unloaded += CanvasArea_Unloaded;

        // --------------------------------------- CTOR ----------------------------------------------------\\

    }

    private void CanvasArea_Unloaded(object sender, RoutedEventArgs e)
    {
        Settings.CurrentBgGridChanged -= Settings_CurrentBgGridChanged;

    }

    public void OpenContextMenu(string contextMenuKeyName)
    {
        var contextMenu = this.FindResource(contextMenuKeyName) as ContextMenu;
        OpenContextMenu(contextMenu);
    }
    void OpenContextMenu(ContextMenu contextMenu)
    {
        // Obtener la posición actual del mouse
        var mousePosition = Mouse.GetPosition(this);

        // Configurar el ContextMenu para que se abra en la posición del mouse
        contextMenu.PlacementTarget = this; // El elemento de destino (puede ser la ventana o el canvas)
        contextMenu.Placement = PlacementMode.RelativePoint;
        contextMenu.HorizontalOffset = mousePosition.X - (contextMenu.ActualWidth / 2);
        contextMenu.VerticalOffset = mousePosition.Y - 10;

        // Abrir el ContextMenu
        contextMenu.IsOpen = true;
    }

    private void InsertKeyframe_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = (string)s.Header;

        if (header == "Image")
        {
            SelectedLayer.InsertKeyframe(nameof(SelectedLayer.Image));
        }
        if (header == "Blank Image")
        {
            SelectedLayer.StartEdit();
            SelectedLayer.Image.Erase(SKColors.Transparent);
            SelectedLayer.EndEdit();
            SelectedLayer.InsertKeyframe(nameof(SelectedLayer.Image));
            Shot.UpdateCurrentRender();
        }
        if (header == "Position")
        {
            SelectedLayer.InsertKeyframe(nameof(SelectedLayer.Position));
        }
        if (header == "Scale")
        {
            SelectedLayer.InsertKeyframe(nameof(SelectedLayer.Scale));
        }

    }




    public void Settings_CurrentBgGridChanged(string message)
    {
        if (Resources.Contains(message))
        {
            Background = (Brush)Resources[message];
            UpdateGrid2D();
        }
        else // None
        {
            Background = Brushes.Transparent;
        }
    }


    //ZOOM
    private void CanvasArea_MouseWheel(object sender, MouseWheelEventArgs e)
    {

        float scaleFactor = Zoomfactor;
        if (e.Delta < 0)
        {
            scaleFactor = 1f / scaleFactor;

        }


        Point mousePosition = e.GetPosition(this);

        Matrix scaleMatrix = _transform.Matrix;
        scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePosition.X, mousePosition.Y);
        _transform.Matrix = scaleMatrix;


        foreach (UIElement child in this.Children)
        {
            double x = Canvas.GetLeft(child);
            double y = Canvas.GetTop(child);

            double sx = x * scaleFactor;
            double sy = y * scaleFactor;

            Canvas.SetLeft(child, sx);
            Canvas.SetTop(child, sy);



            child.RenderTransform = _transform;


        }

        if (Settings.instance.IsRender3D)
        {
            var camera = Shortcuts.ViewportCamera;
            camera?.DoZoom((float)scaleFactor);

            Shot.UpdateCurrentRender();
        }
    }




    //------------------- API ----------------------\\
    bool playingMode = true;

    public void InvokeKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
           // CanvasAreaControl.SetNormalQuality(this);
           // SetQualityCanvas();

            if (playingMode)
            {
                //Shortcuts.PlayKey.action();
            }
            playingMode = false;

            dragging = false;
            ZoomMode = false;
            Shortcuts.IsPanning = false;
            Shortcuts.UpdateCursor();
        }

        else if (e.Key == Key.RightAlt && Shortcuts.IsPanning)
        {
            Cursor = Cursors.Hand;
            ZoomMode = false;
        }


    }

    public void InvokeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            if (!Shortcuts.IsPanning)
                playingMode = true;


            Shortcuts.IsPanning = true;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                Cursor = Cursors.ScrollWE;
                ZoomMode = true;
            }
            else
            {
                Cursor = Cursors.Hand;
            }

            ActionHistory.IsOnAction = false;


        }
    }


    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);

        if(!Shortcuts.IsTextBoxFocus)
           Focus();

        Shortcuts.InvokeCanvasMouseEnter(this, e);
    }
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        Shortcuts.IsPanning = false;
        ZoomMode = false;

        // playingMode = true;


        Shortcuts.InvokeCanvasMouseLeave(this, e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        Focus();
        playingMode = false;

        if (Shortcuts.CurrentCanvas != this)
        {
            AppModel.project.editorsSpace.CanvasView = AppModel.FindAncestor<CanvasView>(this);
            Shortcuts.CurrentCanvas = this;
        }

        if (Shortcuts.IsPanning)
        {

            Shortcuts.Dragging = false;

            initialMousePosition = Shortcuts.GetMousePos(e);
            _initialMousePosition = Shortcuts.MousePosition;

            dragging = true;
        }
        else
            Shortcuts.InvokeCanvasMouseDown(this, e);

        this.CaptureMouse();
        
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (Shortcuts.IsPanning)
        {  

            ActionHistory.IsOnAction = false;
            dragging = false;
            AppModel.mainW.InvalidateGlow();
        }
        else
            Shortcuts.InvokeCanvasMouseUp(this, e);

       this.ReleaseMouseCapture();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (dragging)
        {
            //CanvasAreaControl.SetLowQuality(this);

            // if (ZoomMode || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            if (ZoomMode || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                ZoomMode = true;
                Cursor = Cursors.ScrollWE;

                DoZoom(e);
            }
            else if (Keyboard.IsKeyDown(Key.OemPeriod) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                DoOrbit();
            }
            else
            {
                DoPan(e);
            }
        }

        else
        {
            Shortcuts.InvokeCanvasMouseMove(this, e);
        }
    }




    //-------------------------------------------------------------------------------------------------------------------------------------- PAN AND ZOOM
    bool ZoomMode = false;
    bool dragging = false;

    Point initialMousePosition;
    Point _initialMousePosition;
    //  double initialScale;


    bool _awaitRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }


    void DoOrbit()
    {
        Point mousePosition = Shortcuts.MousePosition;
        Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
        var translate = new TranslateTransform(delta.X, delta.Y);

        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

        Shortcuts.CurrentCanvas.CanvasTransform = translate.Value * Shortcuts.CurrentCanvas.CanvasTransform;
        if (Settings.instance.IsRender3D)
        {
            // var camera = AppModel.project.Shot3D.MainCamera;
            var camera = Shortcuts.ViewportCamera;
            camera?.DoOrbit((float)delta.X, -(float)delta.Y);

            Shot.UpdateCurrentRender();
        }
    }

    void DoPan(MouseEventArgs e)
    {
        if (_awaitRender)
            return;

        Point mousePosition = Shortcuts.MousePosition;
        Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
        var translate = new TranslateTransform(delta.X, delta.Y);

        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

        Shortcuts.CurrentCanvas.CanvasTransform = translate.Value * Shortcuts.CurrentCanvas.CanvasTransform;
        if (Settings.instance.IsRender3D)
        {
            // var camera = AppModel.project.Shot3D.MainCamera;
            var camera = Shortcuts.ViewportCamera;
            camera?.DoPan(-(float)delta.X, (float)delta.Y);

            Shot.UpdateCurrentRender();
        }

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
            if (Settings.instance.IsRender3D)
            {
                var camera = ((ED_CanvasView)Shortcuts.canvasView?.DataContext)?.ViewportCamera;
                camera?.DoZoom((float)distance);

                Shot.UpdateCurrentRender();
            }
            else
            {
                scaleMatrix.ScaleAt(scaleFactor, scaleFactor, initialMousePosition.X, initialMousePosition.Y);
                Shortcuts.CurrentCanvas.CanvasTransform = scaleMatrix;
            }
        }

        // Almacenar la posición actual del mouse para el próximo evento MouseMove
        _previousMousePosition = currentMousePosition;
        _previousMousePositionTime = DateTime.Now;


        _awaitRender = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }




    public void FlipHorizontal()
    {
        // Obtener la matriz actual
        Matrix transformMatrix = CanvasTransform;

        var puntoDeReferenciaX = 0;
        var puntoDeReferenciaY = 0;
        // Invertir la escala en el eje X
        transformMatrix.ScaleAtPrepend(-1, 1, puntoDeReferenciaX, puntoDeReferenciaY);

        // Aplicar la matriz modificada
        CanvasTransform = transformMatrix;
    }













    private async void Canvas_Drop(object sender, DragEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        await ManageDrop(e);
        Mouse.OverrideCursor = null;
    }

    async Task ManageDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files[0].IsFormat("mp4"))
            {
                ManualCodec.ImportMp4(files[0]);
                return;
            }
            else if (files[0].IsFormat("shot"))
            {
                project.ImportShot(files[0]);
                return;
            }

            if (Directory.Exists(files[0])) // drop folder as image sequence
            {
                var folderFiles = Directory.GetFiles(files[0]);
                ImageSequence imgs = new(folderFiles);
                imgs.Name = FileManager.GetFolderName(files[0]);
                AddLayerBase(imgs);

                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) // import image sequence instead of individual layers
            {
                ImageSequence imgs = new(files);
                AddLayerBase(imgs);
            }
            else // normal behaviour
            {
                foreach (var file in files)
                {
                    if (file.IsPngImage())
                    {
                        Layer l = Layer.New(file); //add a layer automatically
                    }


                }

            }

        }
        else if (e.Data.GetDataPresent(DataFormats.StringFormat)) // IMAGE FROM WEB
        {
            // Manejar imágenes arrastradas desde un navegador
            string url = (string)e.Data.GetData(DataFormats.StringFormat);
            var imageUrl = await WebManager.ImageUrlFromWeb(url);
            if (imageUrl != null)
            {
                var image = await WebManager.ImageFromWeb(imageUrl);
                if (image != null)
                    LayerFromWeb(image, imageUrl);
            }
        }
    }



    void LayerFromWeb(byte[] imageBytes, string url)
    {
        var name = Path.GetFileNameWithoutExtension(url);

        Layer l = Layer.New(imageBytes.ToWriteableBitmap());
        l.Name = Namer.SetName(name, layers);
    }

    // Change the cursor 
    private void myCanvas_MouseEnter(object sender, MouseEventArgs e)
    {
        // SetCursor();
    }

    private void myCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        // Restaurar el cursor predeterminado
        Mouse.OverrideCursor = null;
    }



    //----------------------------------------------------- RIGHT CLICK LAYER -----------------------------------------\\
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {

        MenuItem menuItem = sender as MenuItem;
        if (menuItem.Header.ToString() == "Save Image As...")
        {

            var imageBytes = SelectedLayer.ImageData;
            if (imageBytes != null && imageBytes.Length > 0)
            {
                var dialog = new SaveFileDialog();
                dialog.Filter = "Imagen PNG (*.png)|*.png";
                if (dialog.ShowDialog() == true)
                {
                    using (var fileStream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        fileStream.Write(imageBytes, 0, imageBytes.Length);
                    }
                }
            }
        }
        else if (menuItem.Header.ToString() == "Copy Image")
        {
            var imageBytes = SelectedLayer.ImageData;
            if (imageBytes != null && imageBytes.Length > 0)
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    Clipboard.SetImage(bitmap);
                }
            }

        }
        else if (menuItem.Header.ToString() == "Send To Img2Img")
        {
            //  GenerationManager.Instance.SelectedPreset.TargetImage = _model.SelectedLayer.ImageData;  
        }
    }

    public Point mousePosition()
    {
        return _transform.Inverse.Transform(Mouse.GetPosition(this));
    }
    public Point elementPosition(FrameworkElement element)
    {
        var transform = element.TransformToAncestor(this);
        var point = transform.Transform(new Point(0, 0));
        return _transform.Inverse.Transform(point);
    }
    public Point elementPositionCenter(FrameworkElement element)
    {
        var position = elementPosition(element);

        // Calcular el centro del Ellipse
        double centerX = position.X + element.Width / 2;
        double centerY = position.Y + element.Height / 2;

        // Establecer el centro como startPoint
        return new Point(centerX, centerY);
    }


    public object? HitTestContext(MouseButtonEventArgs e)
    {
        Point mouseUpPoint = e.GetPosition(this);
        var h = HitTest(mouseUpPoint);
        return h.DataContext;
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

    public FrameworkElement? HitTest(MouseButtonEventArgs e, Func<FrameworkElement, bool> condition)
    {
        Point mousepos = e.GetPosition(this);

        FrameworkElement hitElement = null;

        HitTestResultCallback resultCallback = (result) =>
        {
            if (result.VisualHit is FrameworkElement element && element != this)
            {
                if (element.Tag is string tag && tag == "IgnoreHit" || !AppModel.IsElementActuallyVisible(element))
                    return HitTestResultBehavior.Continue;

                hitElement = element;
                if(condition(element))
                    return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        };

        VisualTreeHelper.HitTest(this, null, resultCallback, new PointHitTestParameters(mousepos));

        return hitElement;
    }



}