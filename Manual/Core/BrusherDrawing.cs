using CefSharp.DevTools.LayerTree;
using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Objects;
using Manual.Objects.UI;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;

using static System.Windows.Media.Imaging.WriteableBitmapExtensions;

using PointF = System.Drawing.PointF;

namespace Manual.Core;




public static partial class RenderizableExtension
{
    public static WriteableBitmap ChangeMaskBlackAndWhite(this WriteableBitmap image)
    {
        
        for (int x = 0; x < image.PixelWidth; x++)
        {
            for (int y = 0; y < image.PixelHeight; y++)
            {
                Color originalColor = image.GetPixel(x, y);
                if (originalColor.A != 0)
                {
                    // Cambiar el color del píxel
                    image.SetPixel(x, y, Colors.White);
                }
                else
                {
                    image.SetPixel(x, y, Colors.Black);
                }
            }
        }

        return image;
    }
    /// <summary>
    /// support all hex variations: #RGB #ARGB #RRGGBB #AARRGGBB
    /// </summary>
    /// <param name="hexValue"></param>
    /// <returns></returns>
    public static Color ToColor(this string hexValue)
    {
        if (!string.IsNullOrEmpty(hexValue) && hexValue[0] == '#')
        {
            try
            {
                byte a = 255; // Alpha predeterminado (opacidad completa)
                byte r, g, b;

                // Expande el formato abreviado a su forma completa
                if (hexValue.Length == 4) // Formato #RGB
                {
                    r = Convert.ToByte(string.Concat(hexValue[1], hexValue[1]), 16);
                    g = Convert.ToByte(string.Concat(hexValue[2], hexValue[2]), 16);
                    b = Convert.ToByte(string.Concat(hexValue[3], hexValue[3]), 16);
                }
                else if (hexValue.Length == 5) // Formato #ARGB
                {
                    a = Convert.ToByte(string.Concat(hexValue[1], hexValue[1]), 16);
                    r = Convert.ToByte(string.Concat(hexValue[2], hexValue[2]), 16);
                    g = Convert.ToByte(string.Concat(hexValue[3], hexValue[3]), 16);
                    b = Convert.ToByte(string.Concat(hexValue[4], hexValue[4]), 16);
                }
                else if (hexValue.Length == 7) // Formato #RRGGBB
                {
                    r = Convert.ToByte(hexValue.Substring(1, 2), 16);
                    g = Convert.ToByte(hexValue.Substring(3, 2), 16);
                    b = Convert.ToByte(hexValue.Substring(5, 2), 16);
                }
                else if (hexValue.Length == 9) // Formato #AARRGGBB
                {
                    a = Convert.ToByte(hexValue.Substring(1, 2), 16);
                    r = Convert.ToByte(hexValue.Substring(3, 2), 16);
                    g = Convert.ToByte(hexValue.Substring(5, 2), 16);
                    b = Convert.ToByte(hexValue.Substring(7, 2), 16);
                }
                else
                {
                    throw new ArgumentException("El valor hexadecimal no tiene un formato válido.");
                }

                return Color.FromArgb(a, r, g, b);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error de conversión aquí
                Console.WriteLine("Error al convertir el valor hexadecimal a Color: " + ex.Message);
            }
        }

        // Valor por defecto en caso de error o valor no válido
        return Colors.Transparent;
    }

    public static SolidColorBrush ToSolidColorBrush(this string hexValue)
    {
        return new SolidColorBrush(hexValue.ToColor());
    }

    public static Color ColorHex(string hexValue)
    {
       return hexValue.ToColor();
    }

  
    public static string? ToHex(this Color? color)
    {
        if (color is Color c)
            return ToHex(c);
        else
            return null;
    }
    public static string ToHex(this Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static bool IsHexadecimal(string input)
    {
        // Una simple verificación para determinar si la cadena parece ser un valor hexadecimal de color.
        return input.StartsWith("#") && (input.Length == 7 || input.Length == 9);
    }


    public static WriteableBitmap ChangeBrushColor(this WriteableBitmap image, Color newColor)
    {

        WriteableBitmap col = BitmapFactory.New(image.PixelWidth, image.PixelHeight);
        col.Clear(newColor);

        Rect maskRect = new Rect(0, 0, col.PixelWidth, col.PixelHeight);
        Rect layerRect = new Rect(0, 0, col.PixelWidth, col.PixelHeight);

        col.Blit(maskRect, image, layerRect, BlendMode.Mask);
        return col;
    }


    public static Color GetAverageColor(this WriteableBitmap bitmap, double samplingDensity = 1.0)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);

        byte[] pixels = new byte[height * stride];
        bitmap.CopyPixels(pixels, stride, 0);

        return GetAverageColor(pixels, width, height, stride, samplingDensity);
    }

    public static Color GetAverageColor(this byte[] pixels, double samplingDensity = 1.0)
    {
        int stride = (int)Math.Sqrt(pixels.Length / 4) * 4; // Asumiendo formato BGRA
        int height = pixels.Length / stride;
        int width = stride / 4;

        return pixels.ToWriteableBitmap().GetAverageColor(samplingDensity);//GetAverageColor(pixels, width, height, stride, samplingDensity);
    }

    private static Color GetAverageColor(byte[] pixels, int width, int height, int stride, double samplingDensity)
    {
        if (samplingDensity <= 0) samplingDensity = 0.01;  // Asegurarse de que no sea 0 o negativo
        if (samplingDensity > 1) samplingDensity = 1;     // No puede ser mayor que 1

        long totalR = 0, totalG = 0, totalB = 0;
        long sampledPixelCount = 0;

        int step = (int)(1 / samplingDensity);  // Calcula cada cuántos píxeles tomar una muestra
        if (step < 1) step = 1;  // Asegurarse de que al menos cada píxel sea considerado una vez

        for (int y = 0; y < height; y += step)
        {
            for (int x = 0; x < width; x += step)
            {
                int index = y * stride + x * 4; // Asumiendo formato BGRA
                byte alpha = pixels[index + 3]; // Alpha está en el cuarto byte
                if (alpha > 10)  // Considerar solo píxeles con alfa mayor a 10
                {
                    totalB += pixels[index];
                    totalG += pixels[index + 1];
                    totalR += pixels[index + 2];
                    sampledPixelCount++;
                }
            }
        }

        if (sampledPixelCount == 0) return Colors.DarkGray; // Devuelve gris oscuro si todos los píxeles son transparentes

        byte averageR = (byte)(totalR / sampledPixelCount);
        byte averageG = (byte)(totalG / sampledPixelCount);
        byte averageB = (byte)(totalB / sampledPixelCount);

        return Color.FromRgb(averageR, averageG, averageB);
    }




}







public enum PencilCursor
{
    Dot,
    BrushForm
}

public partial class Brusher : ObservableObject
{
    public bool isCustomCursor = false;
    [ObservableProperty] string name = "brush";


    internal Color _colorBrush = Colors.Black;
    public virtual Color ColorBrush
    {
        get => AppModel.project.SelectedColor;// _colorBrush;
        set
        {
            if (_colorBrush != value)
            {
               // _colorBrush = value;
                AppModel.project.SelectedColor = value;
               // OnPropertyChanged(nameof(ColorBrush));
            }
        }
    }

    public string Type = "Pencil";

    [ObservableProperty] bool isEraser = false;
    
    public bool isUpdate { get; set; } = true;
   

    [ObservableProperty] DynamicVariable size = new(6);
    [ObservableProperty] DynamicVariable opacity = new(100);

    [ObservableProperty] float feather = 0;

    [ObservableProperty] LayerBlendMode blendMode = LayerBlendMode.Normal;


    private WriteableBitmap _imageBrush;
   [JsonIgnore] public WriteableBitmap ImageBrush
    {
        get { return _imageBrush; }
        set
        {
            if (_imageBrush != value)
            {
                _imageBrush = value;
                OnPropertyChanged();
            }
        }
    }


    [ObservableProperty] bool isAntialias = true;

    public Brusher()
    {
        Opacity.EnablePenPressure = false;

        OnDraw = MouseDown;
        OnMouseDown = Draw;
        OnMouseUp = MouseUp;
    }

    public void SetInitial()
    {
        Size.initialRealValue = Size.RealValue;
    }


    public delegate void DrawHandler(LayerBase layer, PointF initialPosition, PointF finalPosition);
    public DrawHandler OnDraw;
    public DrawHandler OnMouseDown;
    public DrawHandler OnMouseUp;

    public void MouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        layer.DrawStoke(initialPosition, finalPosition, this);
        ApplyDraw(layer);
    }
    public void Draw(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        layer.DrawStoke(initialPosition, finalPosition, this);
        ApplyDraw(layer);
    }
    public void MouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        //ApplyDraw(layer);
    }

    public virtual void ApplyDraw(LayerBase layer)
    {
        using (var baseCanvas = new SKCanvas(layer.Image))
        {
            baseCanvas.Clear(SKColors.Transparent);
            baseCanvas.DrawBitmap(layer.ImageOriginal, 0, 0);  // Dibuja la imagen original

            var op = (byte)((Opacity.Value / 100) * 255);
            var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.Low,
                BlendMode = BlendMode.ToSkiaBlendMode(), // Ajusta según sea necesario para la combinación final
                Color = SKColors.White.WithAlpha(op),
                IsAntialias = true,
            }; 

            if(AppModel.project.IsColorEraser || IsEraser)
                paint.BlendMode = SKBlendMode.DstOut;
            else if (layer.IsBlockTransparency)
                paint.BlendMode = SKBlendMode.SrcATop;

            //LASSO
            if (layer.ShotParent.Lasso.HasSelection())
            {
                var clipPath = Lasso.CreateClipPath(layer.ShotParent.Lasso.Points);
                clipPath.Offset(-layer.PositionX, -layer.PositionY);
                baseCanvas.ClipPath(clipPath, antialias: true);
            }

            if (Feather > 0)
                paint.ImageFilter = SKImageFilter.CreateBlur(Feather, Feather, SKShaderTileMode.Repeat);

            baseCanvas.DrawBitmap(layer.ImagePreview, 0, 0, paint);
        }
    }

}


public partial class DynamicVariable : ObservableObject
{
   [ObservableProperty] bool enablePenPressure = true;
    partial void OnEnablePenPressureChanged(bool value)
    {
        //PenPressure.Enabled = value;
    }
    [ObservableProperty] bool enableVelocity  = false;

    //percentage
    [ObservableProperty] int penPressureMinimum = 0;
    [ObservableProperty] float penPressureMultiply = 1f;

    [ObservableProperty] int velocityMinimum = 50;
  
    
    [ObservableProperty] float value;
    [JsonIgnore] public float RealValue
    {
        get
        {
            float baseValue = Value;
            float velocity = 1;
            float pressure = 1;


            if (EnablePenPressure && PenPressure.Enabled)
            {
                float realPres = PenPressure.CurrentPenInfo.Pressure;

                pressure =  realPres / (PenPressure.MaxPressure / PenPressureMultiply);

                var offset = PenPressureMinimum / 100f;
                float newPressure = offset + (1.0f - offset) * pressure;
                pressure = newPressure;
            }

            if (EnableVelocity)
            {
                 velocity = Shortcuts.MouseVelocity;
                 velocity = Math.Clamp(velocity, 0, 1);

                 velocity = Math.Max(velocity, VelocityMinimum / 100f);
            }

        //    AppModel.mainW.SetMessage($"velocity: {velocity} pressure: {pressure}");
            return baseValue * pressure / velocity;

        }
    }


    [JsonIgnore] public float initialRealValue { get; set; }



    Point previousMousePosition;
    DateTime previousTimestamp;
    double GetVelocity()
    {
        Point currentMousePosition = Mouse.GetPosition(AppModel.mainW);
        DateTime currentTimestamp = DateTime.Now;

        double distance = Renderizainador.Distance(previousMousePosition, currentMousePosition);
        TimeSpan timeElapsed = currentTimestamp - previousTimestamp;

        var velocity = distance / timeElapsed.TotalMilliseconds;

        previousMousePosition = currentMousePosition;
        previousTimestamp = currentTimestamp;

        return velocity;
    }


    public DynamicVariable()
    {
            
    }
    public DynamicVariable(float initialValue)
    {
        Value = initialValue;
    }

}


