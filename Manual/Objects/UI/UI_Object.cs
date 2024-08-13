using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Markup;
using Manual.API;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

using PointF = System.Drawing.PointF;
using SkiaSharp;
using System.Windows.Controls;
using System.Windows.Media;

namespace Manual.Objects.UI;

public class UICanvasElement : UserControl
{
    public static readonly DependencyProperty RealThickProperty = DependencyProperty.Register(
      "RealThick",
      typeof(double),
      typeof(BoundingBoxView),
      new FrameworkPropertyMetadata(
          defaultValue: 10.0)); // El valor por defecto para RealThick);

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


    public UICanvasElement()
    {
        Loaded += BoundingBoxView_Loaded;
        Unloaded += BoundingBoxView_Unloaded;
    }

    private void BoundingBoxView_Unloaded(object sender, RoutedEventArgs e)
    {
        canvas.OnMatrixChanged -= Canvas_OnMatrixChanged;
        canvas = null;
    }

    private void BoundingBoxView_Loaded(object sender, RoutedEventArgs e)
    {
        canvas = AppModel.FindAncestor<CanvasArea>(this);
        canvas.OnMatrixChanged += Canvas_OnMatrixChanged;
        Canvas_OnMatrixChanged(canvas.CanvasTransform);
    }
    internal CanvasArea canvas;
    private void Canvas_OnMatrixChanged(Matrix value)
    {
        if (canvas != null)
        {
            var v = Math.Abs(1 / canvas.CanvasTransform.M11);
            RealThick = v;
            OnThickChanged(RealThick);
        }

    }

    public virtual void OnThickChanged(double thick)
    {
        
    }


}






public partial class UI_Object : CanvasObject, IRecipient<NotifyMessage>
{
    [ObservableProperty] [property: JsonIgnore] double realThicko = 20;
    [ObservableProperty] [property: JsonIgnore] double thick = 1;


    public UI_Object()
    {
        this.Index = 1_000;
        ImageScale = new PixelPoint(512, 512);

        WeakReferenceMessenger.Default.Register(this);
    }


    public void Receive(NotifyMessage message)
    {
        RealThicko = Thick * (double)message.Value;
    }

}



public partial class Camera2D : UI_Object, IAnimable
{
    [JsonIgnore] public Shot ShotParent;

    [JsonIgnore] public Camera2DView ui_camera;

    [ObservableProperty] [property: JsonIgnore] bool isRenderMode = true;
    [ObservableProperty] [property: JsonIgnore] WriteableBitmap render;


    [ObservableProperty] string name = "MainCamera";

    [ObservableProperty] float backgroundOpacity = 0.33f;

    [JsonIgnore] public SKCanvas RenderCanvas;

    public AnimationBehaviour _Animation { get; set; }

    public Camera2D()
    {
        this.Index = 999;
    }
    public Camera2D(int width, int height)
    {
        ImageWidth = width;
        ImageHeight = height;
        this.Index = 999;
      
    }

}

public partial class Selector : UI_Object
{
    public Selector()
    {

    }
}


public partial class Transformer : UI_Object
{
    [ObservableProperty] bool isEnabled = true;
    [ObservableProperty] CanvasObject? target;
    [ObservableProperty] bool keepAspectRatio = true;
    [ObservableProperty] bool canRotate = false;

    public Transformer()
    {
        OnCanvasPropertiesChanged += Transformer_OnCanvasPropertiesChanged;
    }
    public Transformer(CanvasObject target)
    {
        Target = target;
        OnCanvasPropertiesChanged += Transformer_OnCanvasPropertiesChanged;
    }

    private void Transformer_OnCanvasPropertiesChanged()
    {
        if (Target != null)
        {
            Target.ImageWidth = this.ImageWidth;
            Target.ImageHeight = this.ImageHeight;

            Target.Position = this.Position;
        }
    }

    public TransformMode Mode = Transformer.TransformMode.Center;
    public enum TransformMode
    {
        Center,
        Corner,
    }


    public static void V_FlipHorizontal(LayerBase layer)
    {
        ActionHistory.StartAction(layer, nameof(layer.Image));
        var img = RendGPU.FlipHorizontal(layer.Image);
        layer.Image = img;
        ActionHistory.FinalizeAction();
    }
    public static void V_FlipVertical(LayerBase layer)
    {
        ActionHistory.StartAction(layer, nameof(layer.Image));
        var img = RendGPU.FlipVertical(layer.Image);
        layer.Image = img;
        ActionHistory.FinalizeAction();
    }

    public static void ApplyScale(LayerBase layer)
    {
        if (layer.Image == null)
        {
            return;
        }


        //CHANGE IMAGE
        ActionHistory.StartAction(layer, nameof(layer.Image));


        // Obtener el tamaño original del SKBitmap
        int originalWidth = layer.Image.Width;
        int originalHeight = layer.Image.Height;

        // Calcular el nuevo tamaño basado en la escala normalizada
        int newWidth = (int)(originalWidth * layer.NormalizedScale.X);
        int newHeight = (int)(originalHeight * layer.NormalizedScale.Y);

        // Crear un nuevo SKBitmap escalado
        SKBitmap newImage = new SKBitmap(newWidth, newHeight);
        using (SKCanvas canvas = new SKCanvas(newImage))
        {
            canvas.DrawBitmap(layer.Image, new SKRect(0, 0, newWidth, newHeight));
        }

        // Asignar el nuevo SKBitmap a layer.Image
        layer.Image.Dispose(); // Liberar el bitmap antiguo si es necesario
     

     
        layer.Image = newImage;
        ActionHistory.FinalizeAction();


        //CHANGE SCALE
        ActionHistory.StartAction(layer, nameof(layer.Scale));
        layer.Scale = new Point(100, 100);
        ActionHistory.FinalizeAction();

        Output.Log("Scale Applied");

    }


}

/// <summary>
/// it's like UI_Object but can be see in layer collection
/// </summary>
public partial class UILayerBase : LayerBase, IRecipient<NotifyMessage>
{
    [ObservableProperty] double realThick = 20;
    [ObservableProperty] double thick = 1;

    public UILayerBase()
    {
        this.Index = 1_000;
        ImageScale = new PixelPoint(512, 512);

        WeakReferenceMessenger.Default.Register(this);
    }


    public void Receive(NotifyMessage message)
    {
        RealThick = Thick * (double)message.Value;
    }

    public override void SetImage(WriteableBitmap image)
    {
        return; //do nothing
        //base.SetImage(image);
    }
}


public partial class BoundingBox : UILayerBase
{
    [ObservableProperty] int resolutionX = 512;
    [ObservableProperty] int resolutionY = 512;

    public BoundingBox()
    {
        Name = "Bounding Box";
        Type = "GhostLayer";
        Enabled = false;
    }

    public static BoundingBox Add(Shot shot)
    {
        BoundingBox box = new();
        ManualAPI.AddLayerBase(box, shot);
        box.SetResolution(shot.MainCamera.ImageWidth, shot.MainCamera.ImageHeight);
        return box;
    }
    public static BoundingBox Add()
    {
        return Add(ManualAPI.SelectedShot);
    }
    public void SetResolution(int width, int height)
    {
        ResolutionX = width;
        ResolutionY = height;
    }

    public static BoundingBox GetBoundingBox(CanvasObject obj, double inflate = 0)
    {
        var rect = GetBoundingBoxRect([obj], inflate);
        var box = new BoundingBox();
        box.RealScale = new Point(rect.Width, rect.Height);
        box.PositionGlobal = new PointF((float)rect.X, (float)rect.Y);
        return box;
    }
    public static BoundingBox GetBoundingBox(IEnumerable<CanvasObject> objects, double inflate = 0)
    {
        var rect = GetBoundingBoxRect(objects, inflate);
        var box = new BoundingBox();
        box.RealScale = new Point(rect.Width, rect.Height);
        box.PositionGlobal = new PointF((float)rect.X, (float)rect.Y);
        return box;
    }
    public static Rect GetBoundingBoxRect(IEnumerable<CanvasObject> objects, double inflate = 0)
    {
        // Inicializa el rectángulo envolvente con el primer objeto para tener una base de comparación.
        var first = objects.First();
        Rect boundingBox = new Rect(first.PositionGlobal.ToPoint(), new Size(first.RealScale.X, first.RealScale.Y));

        // Itera sobre todos los objetos para expandir el boundingBox según sea necesario.
        foreach (var obj in objects.Skip(1)) // Usa Skip(1) para omitir el primer objeto ya que ya está incluido.
        {
            Rect objRect = new Rect(obj.PositionGlobal.ToPoint(), new Size(obj.RealScale.X, obj.RealScale.Y));
            boundingBox.Union(objRect); // Expande el boundingBox para incluir este objeto.
        }

        // Aquí puedes ajustar el boundingBox según sea necesario, por ejemplo, agregando un desplazamiento.
        if (inflate != 0)
            boundingBox.Inflate(inflate, inflate);

        return boundingBox;
    }

    internal static void Reset()
    {
        var b = ManualAPI.SelectedShot.layers.FirstOrDefault(l => l is BoundingBox) as BoundingBox;
        b.ImageWidth = ManualAPI.SelectedShot.MainCamera.ImageWidth;
        b.ImageHeight = ManualAPI.SelectedShot.MainCamera.ImageHeight;
        b.Position = new Point(0, 0);
    }
}


public partial class UI_Brush : UI_Object
{
    [ObservableProperty] bool isHeader;

    float _brushSize = 3;
    public float BrushSize
    {
        get => _brushSize;
        set
        {
            if (value >= 1)
            {
                ImageScale = new(value, value);
                _brushSize = value;
            }
        }
    }

    public UI_Brush()
    {
        Rotation = 315;
    }
}




//---------------------------------------------------------------------------------------- CONVERTERS ----\\||

public sealed class Int32Extension : MarkupExtension
{
    public Int32Extension(int value) { this.Value = value; }
    public int Value { get; set; }
    public override Object ProvideValue(IServiceProvider sp) { return Value; }
};

public class ConstantMultiplierConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double realThick && parameter is int constantValue)
        {
            return realThick * constantValue;
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException(); 
    }
}


public class ConstantMultiplierThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double realThick && parameter is int constantValue)
        {
            return new Thickness(realThick * constantValue);
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

