using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Editors;
using Manual.Objects;
using Manual.Objects.UI;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using MS.WindowsAPICodePack.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using static System.Windows.Media.Imaging.WriteableBitmapExtensions;

using PointF = System.Drawing.PointF;


namespace Manual.Core;

public static class Renderizainador
{


   public static WriteableBitmap ImageFromFile(string filePath)
    {
        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            BitmapFrame frame = decoder.Frames[0]; // Obtener el primer frame del archivo
            // Convertir el formato del bitmap a PBGRA32
            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);

            WriteableBitmap writeableBitmap = new WriteableBitmap(convertedBitmap);

            return writeableBitmap;
        }
    }



    public static WriteableBitmap SolidColor(Color color, int width, int height)
    {
        WriteableBitmap image = BitmapFactory.New(width, height);
        image.Clear(color);
        return image;
    }

    public static double Distance(Point point1, Point point2)
    {
        double dx = point2.X - point1.X;
        double dy = point2.Y - point1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }


    public static Color BlendColor(this Color color, Color backColor, double amount)
    {
        byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
        byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
        byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
        return Color.FromRgb(r, g, b);
    }

}











public interface ICloneableBehaviour
{
    void OnClone<T>(T source);
}
public static partial class RenderizableExtension //--------------------------------------------- render extension
{

    public static T Clone<T>(this T source) where T : ICloneableBehaviour
    {
        // Si el objeto fuente es null, devuelve el valor por defecto para ese tipo
        if (source == null)
        {
            return default(T);
        }
       // var t = source.GetType();


        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects, // Importante para manejar herencia
                                                         // Incluye aquí cualquier otro ajuste necesario de tus JsonSerializerSettings
        };

        var serialized = JsonConvert.SerializeObject(source, settings);
        var cloned = JsonConvert.DeserializeObject<T>(serialized, settings);

        if (cloned is IId i)
            i.Id = Guid.NewGuid();

      //  var tc = cloned.GetType();

        cloned.OnClone(source);

        return cloned;
    }



    //PRIVATE
    private static byte[] WriteableToByte(WriteableBitmap writeableBitmap)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));

            encoder.Save(stream);
            return stream.ToArray();
        }
    }
    private static WriteableBitmap ByteToWriteable(byte[] imageData)
    {
        using (MemoryStream stream = new MemoryStream(imageData))
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            return new WriteableBitmap(bitmapImage);
        }
    }

    // PUBLIC
    public static void RenderArea(this WriteableBitmap writeableBitmap, CanvasObject area, LayerBase layer, bool renderAlpha = false) //------------------------------------- RenderArea Original Core
    {

        PointF layerRelative = layer.RelativeTo(area);


        // Crea un grupo de transformaciones que incluirá traslación y escala.
        TransformGroup transformGroup = new TransformGroup();

        // Añade la traslación al grupo de transformaciones.
        var newX = layerRelative.X / layer.NormalizedScale.X;
        var newY = layerRelative.Y / layer.NormalizedScale.Y;


        var newScaleX = layer.RealScale.X / area.RealScale.X; // / area.Width;
        var newScaleY = layer.RealScale.Y / area.RealScale.Y; // / area.Height;

        var nsX = (float)area.ImageScale.X / layer.ImageScale.X;
        newScaleX *= nsX;
        var nsY = (float)area.ImageScale.Y / layer.ImageScale.Y;
        newScaleY *= nsY;


        TranslateTransform translateTransform = new TranslateTransform
        {
            X = layerRelative.X / layer.NormalizedScale.X,
            Y = layerRelative.Y / layer.NormalizedScale.Y,
        };
        transformGroup.Children.Add(translateTransform);


        // Añade la escala al grupo de transformaciones.
        ScaleTransform scaleTransform = new ScaleTransform
        {
            ScaleX = newScaleX,//layer.NormalizedScale.X,
            ScaleY = newScaleY,//layer.NormalizedScale.Y,
        };
        transformGroup.Children.Add(scaleTransform);


        if(layer.RealRotation != 0)
        {
            RotateTransform rotateTransform = new RotateTransform
            {
                Angle = layer.RealRotation, // El ángulo de rotación
                CenterX = newX + layer.AnchorPoint.X, // El centro de la rotación, ajusta según sea necesario
                CenterY = newY + layer.AnchorPoint.Y,
            };
            transformGroup.Children.Add(rotateTransform);
        }

        var r = BitmapFactory.ConvertToPbgra32Format(layer.ImageWr); // layer == result gen


        if (renderAlpha) // ALPHA PIXELS
        {
            writeableBitmap.BlitRender(r, false, layer.RealOpacity, transformGroup);
            return;
        }


        var wclone = BitmapFactory.New(area.RealScale.X.ToInt32(), area.RealScale.Y.ToInt32());

        wclone.BlitRender(r, false, layer.RealOpacity, transformGroup);


        var rec = new Rect(0, 0, wclone.Width, wclone.Height);

        var blendMode = BlendMode.Alpha;
        writeableBitmap.Blit(rec, wclone, rec, blendMode);

 
    }



    public static WriteableBitmap ToWriteableBitmap(this byte[] imageData)
    {
        return ByteToWriteable(imageData);
    }
    public static byte[] ToByte(this WriteableBitmap writeableBitmap)
    {
        return WriteableToByte(writeableBitmap);
    }


    /// <summary>
    /// copy Position and Size
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="uiObject"></param>
    public static void CopyDimensions(this CanvasObject uiObject2, CanvasObject uiObject, bool copyPosition = true)
    {
        if (copyPosition)
        {
            uiObject2.Position = uiObject.Position;

        }
        uiObject2.AnchorPointNormalized = uiObject.AnchorPointNormalized;
        // uiObject2.ImageScale = uiObject.ImageScale;

        //uiObject2.Width = ((float)uiObject.ImageScale.X / uiObject2.ImageScale.X) * 100;
        //uiObject2.Height = ((float)uiObject.ImageScale.Y / uiObject2.ImageScale.Y) * 100;

        uiObject2.Width *= Convert.ToSingle(uiObject.RealScale.X / uiObject2.RealScale.X);
        uiObject2.Height *= Convert.ToSingle(uiObject.RealScale.Y / uiObject2.RealScale.Y);


        uiObject2.RealRotation = uiObject.RealRotation;
    }

    public static bool IsPngImage(this string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase);
    }

    public static ObservableCollection<byte[]> ToByte(this ObservableCollection<WriteableBitmap> bitmaps)
    {
        ObservableCollection<byte[]> byteCollections = new ObservableCollection<byte[]>();
        foreach (var bitmap in bitmaps)
        {
            byte[] bytes = WriteableToByte(bitmap);
            byteCollections.Add(bytes);
        }
        return byteCollections;
    }
    public static ObservableCollection<WriteableBitmap> ToWriteableBitmap(this ObservableCollection<byte[]> bytes)
    {
        ObservableCollection<WriteableBitmap> byteCollections = new ObservableCollection<WriteableBitmap>();
        foreach (var Byte in bytes)
        {
            WriteableBitmap bitmap = ByteToWriteable(Byte);
            byteCollections.Add(bitmap);
        }
        return byteCollections;
    }

    public static List<T> Reorder<T>(ObservableCollection<T> collection)
    {
        List<T> list = new List<T>(collection);
        list.Reverse();
        return list;
    }

    public static Layer InterestAreaLayer(this Layer layer, bool clear = false)
    {
        PointF relativePos = new(0, 0);
        layer.ImageWr = layer.ImageWr.CropInterest(out relativePos);
        layer.PositionGlobal = PixelPoint.Add(layer.PositionGlobal, relativePos);

        if (clear)
            layer.ImageWr.Clear(Colors.Transparent);

        return layer;
    }
    public static CanvasObject InterestArea(this LayerBase layer)
    {
        PointF relativePos = new(0, 0);
        layer.ImageWr = layer.ImageWr.CropInterest(out relativePos);
        layer.PositionGlobal = PixelPoint.Add(layer.PositionGlobal, relativePos);


        CanvasObject obj = new();
        obj.CopyDimensions(layer);
        return obj;
    }

    // ---------------- LayerBase
    public static void Insert(this LayerBase layer)
    {
        layer.Insert(ManualAPI.SelectedLayer);
    }
    public static void Insert(this LayerBase layer, LayerBase BelowOf)
    {
        ManualAPI.layers.Insert(ManualAPI.layers.IndexOf(BelowOf), layer);
    }


    public static WriteableBitmap CropInterest(this WriteableBitmap bitmap, out PointF position)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        // Recorte desde arriba
        int top = 0;
        for (int y = 0; y < height; y++)
        {
            bool hasNonAlphaPixel = false;
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                if (pixelColor.A != 0) // Verifica si el píxel tiene un valor de alfa diferente de cero
                {
                    hasNonAlphaPixel = true;
                    break;
                }
            }
            if (hasNonAlphaPixel)
                break;
            top++;
        }


        // Recorte desde abajo
        int bottom = height - 1;
        for (int y = height - 1; y >= 0; y--)
        {
            bool hasNonAlphaPixel = false;
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                if (pixelColor.A != 0) // Verifica si el píxel tiene un valor de alfa diferente de cero
                {
                    hasNonAlphaPixel = true;
                    break;
                }
            }
            if (hasNonAlphaPixel)
                break;
            bottom--;
        }

        // Recorte desde la izquierda
        int left = 0;
        for (int x = 0; x < width; x++)
        {
            bool hasNonAlphaPixel = false;
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                if (pixelColor.A != 0) // Verifica si el píxel tiene un valor de alfa diferente de cero
                {
                    hasNonAlphaPixel = true;
                    break;
                }
            }
            if (hasNonAlphaPixel)
                break;
            left++;
        }

        // Recorte desde la derecha
        int right = width - 1;
        for (int x = width - 1; x >= 0; x--)
        {
            bool hasNonAlphaPixel = false;
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                if (pixelColor.A != 0) // Verifica si el píxel tiene un valor de alfa diferente de cero
                {
                    hasNonAlphaPixel = true;
                    break;
                }
            }
            if (hasNonAlphaPixel)
                break;
            right--;
        }

        // Calcula el ancho y alto del recorte
        int cropWidth = right - left + 1;
        int cropHeight = bottom - top + 1;

        // Realiza el recorte utilizando el método Crop de WriteableBitmapEx
        var croppedBitmap = bitmap.Crop(left, top, cropWidth, cropHeight);
        position = new PointF(left, top);

        return croppedBitmap;
    }
    /// <summary>
    /// alpha clamp 0, 1
    /// </summary>
    /// <param name="writeableBitmap"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    public static WriteableBitmap SetAlpha(this WriteableBitmap writeableBitmap, float alpha = 1)
    {
        alpha = float.Clamp(alpha, 0, 1);

        byte byteAlpha = (byte)(alpha * 255);
        // Establece el valor de transparencia en cada píxel del WriteableBitmap
        int width = writeableBitmap.PixelWidth;
        int height = writeableBitmap.PixelHeight;
        int bytesPerPixel = (writeableBitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] pixelData = new byte[height * stride];

        writeableBitmap.CopyPixels(pixelData, stride, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelOffset = y * stride + x * bytesPerPixel;
                pixelData[pixelOffset + 3] = byteAlpha; // Establece la opacidad a 255 (sin transparencia)
            }
        }
        writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        return writeableBitmap;
    }
  

    //PIXELPOINTS
    public static Point ToPoint(this PixelPoint point)
    {
        return new Point(point.X, point.Y);
    }

    public static Vector ToVector(this PixelPoint point)
    {
        return new Vector(point.X, point.Y);
    }
    public static Size ToSize(this PixelPoint point)
    {
        return new Size(point.X, point.Y);
    }


    public static PixelPoint ToPixelPoint(this Point point)
    {
        return new PixelPoint(point.X, point.Y);
    }
    public static PixelPoint ToPixelPoint(this Vector vector)
    {
        return new PixelPoint(vector.X, vector.Y);
    }
    public static Point ToPoint(this Vector vector)
    {
        return new Point(vector.X, vector.Y);
    }
    public static PixelPoint PixelPointScale(this WriteableBitmap bitmap)
    {
        return new PixelPoint(bitmap.PixelWidth, bitmap.PixelHeight);
    }

    public static PixelPoint RelativeToPixel(this PixelPoint subArea, PixelPoint area)
    {
        return PixelPoint.Distance(area, subArea);
    }
    public static PixelPoint RelativeToPixel(this PixelPoint subArea, Point area)
    {
        return PixelPoint.Distance(area, subArea);
    }
    public static PixelPoint RelativeToPixel(this Point subArea, Point area)
    {
        return PixelPoint.Distance(area, subArea);
    }
    public static PointF RelativeTo(this PointF subArea, PointF area)
    {
        return Distance(area, subArea);
    }


    public static Point RelativeTo(this Point subArea, Point area)
    {
        return Distance(area, subArea);
    }

    public static PointF RelativeTo(this CanvasObject canvasObject, CanvasObject area)
    {
        return PixelPoint.Distance(area.PositionGlobal, canvasObject.PositionGlobal);
    }
    /// <summary>
    /// this take cares of NormalizedScale
    /// </summary>
    /// <param name="canvasObject"></param>
    /// <param name="area"></param>
    /// <returns></returns>
    public static Point RelativeToPosition(this CanvasObject canvasObject, CanvasObject area)
    {
        var p = PixelPoint.Distance(area.PositionGlobal, canvasObject.PositionGlobal);
        return new Point(p.X / canvasObject.NormalizedScale.X, p.Y / canvasObject.NormalizedScale.Y);
    }
    public static Point Distance(Point startPoint, Point endPoint)
    {
        double difX = endPoint.X - startPoint.X;
        double difY = endPoint.Y - startPoint.Y;


        return new Point(difX, difY);
    }

    public static PointF Distance(PointF startPoint, PointF endPoint)
    {
        float difX = endPoint.X - startPoint.X;
        float difY = endPoint.Y - startPoint.Y;


        return new PointF(difX, difY);
    }


    public static Point RelativeToScale(this CanvasObject canvasObject, CanvasObject area)
    {
        return new Point(area.RealScale.X - canvasObject.RealScale.X, area.RealScale.Y - canvasObject.RealScale.Y);
    }
    public static Point RelativeToScaleNormalized(this CanvasObject canvasObject, CanvasObject area)
    {
        return new Point((area.Scale.X - canvasObject.Scale.X) / 100, (area.Scale.Y - canvasObject.Scale.Y) / 100);
    }

    /// <summary>
    /// Returns a Point with X and Y as double, from 0 to 1,so 0.5 is the center.
    /// If the point is 256x and basedOn is 512x, then x normalized is 0.5x
    /// </summary>
    /// <param name="pixelPoint"></param>
    /// <param name="basedOn"></param>
    /// <returns></returns>
    public static Point Normalize(this PixelPoint pixelPoint, PixelPoint basedOn)
    {
        return new Point((double)pixelPoint.X / basedOn.X, (double)pixelPoint.Y / basedOn.Y);
    }
    public static Point Normalize(this PixelPoint pixelPoint, Point basedOn)
    {
        return new Point(pixelPoint.X / basedOn.X, pixelPoint.Y / basedOn.Y);
    }
    public static Point Normalize(this Point pixelPoint, PixelPoint basedOn)
    {
        return new Point(pixelPoint.X / basedOn.X, pixelPoint.Y / basedOn.Y);
    }
    public static Point Normalize(this Point point, Point basedOn)
    {
        return new Point(point.X / basedOn.X, point.Y / basedOn.Y);
    }

    public static Point ToPoint(this PointF point)
    {
        return new Point(point.X, point.Y);
    }


    /// <summary>
    /// normalize as a distance point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Point Normalize(this Point point)
    {
        // Calcular la longitud del vector (distancia desde (0,0) hasta el punto)
        double length = Math.Sqrt(point.X * point.X + point.Y * point.Y);

        if (length == 0)
        {
            // El punto es (0,0), así que devolvemos el mismo ya que no tiene dirección
            return point;
        }

        // Calcular el ángulo en radianes
        double angle = Math.Atan2(point.Y, point.X);

        // Calcular las coordenadas normalizadas usando el ángulo
        // Estas son las coordenadas en la circunferencia de un círculo unitario
        double normalizedX = Math.Cos(angle);
        double normalizedY = Math.Sin(angle);

        return new Point(normalizedX, normalizedY);
    }

    public static string ToString(this WriteableBitmap bitmap)
    {
        return $"Image({bitmap.PixelWidth}, {bitmap.PixelHeight})";
    }


    /// <summary>
    /// round position in pixel scale
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Point ToPointPixel(this Point point)
    {
        return new PixelPoint(point).ToPoint();
    }

    public static double GetDiagonal(this CanvasObject canvasObject)
    {
        double diagonal = Math.Sqrt(Math.Pow(canvasObject.ImageWidth, 2) + Math.Pow(canvasObject.ImageHeight, 2));
        return diagonal;
    }



}

public struct PixelPoint
{


    public int X { get; set; }
    public int Y { get; set; }

    public PixelPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Aproximation in pixels of position relative to canvas Matrix
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public PixelPoint(double x, double y)
    {
        X = (int)Math.Round(x);
        Y = (int)Math.Round(y);
    }

    public PixelPoint(Point pointBase)
    {
        X = (int)Math.Round(pointBase.X);
        Y = (int)Math.Round(pointBase.Y);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is PixelPoint))
            return false;

        PixelPoint other = (PixelPoint)obj;
        return this == other;
    }
    public override int GetHashCode()
    {
        return (int)this.X.GetHashCode() ^ (int)this.Y.GetHashCode();
    }

    /// <summary>
    /// point (0,0).
    /// or usually when an object is tiny than a pixel
    /// </summary>
    public static PixelPoint Zero
    {
        get { return new PixelPoint(0, 0); }
    }
    public static Point ZeroPoint
    {
        get { return new Point(0, 0); }
    }
    public static bool operator ==(PixelPoint p1, PixelPoint p2)
    {
        return p1.X == p2.X && p1.Y == p2.Y;
    }
    public static bool operator !=(PixelPoint p1, PixelPoint p2)
    {
        return !(p1 == p2);
    }
    public static PixelPoint operator +(PixelPoint p1, PixelPoint p2)
    {
        return new PixelPoint(p1.X + p2.X, p1.Y + p2.Y);
    }
    public static PixelPoint operator +(PixelPoint p1, System.Windows.Point p2)
    {
        return new PixelPoint(p1.X + p2.X, p1.Y + p2.Y);
    }
    public static PixelPoint operator +(System.Windows.Point p1, PixelPoint p2)
    {
        return new PixelPoint(p1.X + p2.X, p1.Y + p2.Y);
    }
    public static PixelPoint operator -(PixelPoint p1, PixelPoint p2)
    {
        return new PixelPoint(p1.X - p2.X, p1.Y - p2.Y);
    }

    public static PixelPoint operator /(PixelPoint p1, Vector v)
    {
        return new PixelPoint(p1.X / v.X, p1.Y / v.Y);
    }
    public static PixelPoint operator /(PixelPoint p1, Point v)
    {
        return new PixelPoint(p1.X / v.X, p1.Y / v.Y);
    }



    public static PixelPoint Multiply(PixelPoint p1, Point p2)
    {
        return new PixelPoint(p1.X * p2.X, p1.Y * p2.Y);
    }
    public static PixelPoint Multiply(Point p1, Point p2)
    {
        return new PixelPoint(p1.X * p2.X, p1.Y * p2.Y);
    }

    public static PixelPoint Multiply(PixelPoint p1, int p2)
    {
        return new PixelPoint(p1.X * p2, p1.Y * p2);
    }
    public static PixelPoint Divide(PixelPoint p1, Point p2)
    {
        return new PixelPoint(p1.X / p2.X, p1.Y / p2.Y);
    }
    public static Point DividePoint(Point p1, Point p2)
    {
        return new Point(p1.X / p2.X, p1.Y / p2.Y);
    }

    public static System.Drawing.PointF DividePointF(System.Drawing.PointF p1, System.Drawing.PointF p2)
    {
        return new System.Drawing.PointF(p1.X / p2.X, p1.Y / p2.Y);
    }

    public static PointF Distance(PointF startPoint, PointF endPoint)
    {
        float difX = endPoint.X - startPoint.X;
        float difY = endPoint.Y - startPoint.Y;

        return new PointF(difX, difY);
    }

    public static PixelPoint Divide(Point p1, Point p2)
    {
        return new PixelPoint(p1.X / p2.X, p1.Y / p2.Y);
    }
    public static PixelPoint Divide(Size p1, Point p2)
    {
        return new PixelPoint(p1.Width / p2.X, p1.Height / p2.Y);
    }

    public static PixelPoint Divide(Point p1, Vector p2)
    {
        return new PixelPoint(p1.X / p2.X, p1.Y / p2.Y);
    }
    public static PixelPoint Divide(Vector p1, Point p2)
    {
        return new PixelPoint(p1.X / p2.X, p1.Y / p2.Y);
    }

    public static PixelPoint Distance(PixelPoint startPoint, PixelPoint endPoint)
    {
        double difX = endPoint.X - startPoint.X;
        double difY = endPoint.Y - startPoint.Y;

        return new PixelPoint(difX, difY);
    }
    public static PixelPoint Distance(Point startPoint, Point endPoint)
    {
        double difX = endPoint.X - startPoint.X;
        double difY = endPoint.Y - startPoint.Y;


        return new PixelPoint(difX, difY);
    }


    public static PixelPoint Distance(PixelPoint startPoint,Point endPoint)
    {
        double difX = endPoint.X - startPoint.X;
        double difY = endPoint.Y - startPoint.Y;


        return new PixelPoint(difX, difY);
    }
    public static PixelPoint Distance(Point startPoint, PixelPoint endPoint)
    {
        double difX = endPoint.X - startPoint.X;
        double difY = endPoint.Y - startPoint.Y;


        return new PixelPoint(difX, difY);
    }
    public static Point Add(Point point1, Point point2)
    {
        return new Point(point1.X + point2.X, point1.Y + point2.Y);
    }

    public static PointF Add(PointF point1, PointF point2)
    {
        return new PointF(point1.X + point2.X, point1.Y + point2.Y);
    }

    public static Point Minus(Point point1, Point point2)
    {
        return new Point(point1.X - point2.X, point1.Y - point2.Y);
    }

    public override string ToString()
    {
        return $"({X}px, {Y}px)";
    }

    public static explicit operator int(PixelPoint point)
    {
        int combinedValue = point.X;
        return combinedValue;
    }

    public static implicit operator Point(PixelPoint v)
    {
        return new Point(v.X, v.Y);
    }

    public static double DistanceLength(Point startPoint, Point endPoint)
    {
        Vector distance = endPoint - startPoint;
        return distance.Length;
    }



}


public struct Degrees
{
    private int twists;
    private float degree;
    private float realDegree;

    public int Twists
    {
        get { return twists; }
        set
        {
            twists = value;
            CalculateRealDegrees();
        }
    }

    public float Degree
    {
        get { return degree; }
        set
        {
            if(value < 0 || 360 < value)
            {
                RealDegree = value + (twists * 360);
            } 
            else 
            { 
                this.degree = value;
                CalculateRealDegrees();
            }
         
        }
    }

    public float RealDegree
    {
        get { return realDegree; }
         set
        {
            realDegree = value;
            CalculateTwistsValue();
        }
    }

    private void CalculateRealDegrees()
    {
        realDegree = degree + 360 * twists;
    }
    private void CalculateTwistsValue()
    {
        twists = (int)Math.Floor(realDegree / 360);
        degree = realDegree - (twists * 360);

    }

    public Degrees (float degree, int twists)
    {      
        Twists = twists;
        Degree = degree;
    }
    public Degrees (float degree)
    {
        RealDegree = degree;
    }

    /// <summary>
    /// "Name, Twists, Value, RealDegrees"
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Degrees, Twists: {Twists}, Value: {Degree}, RealDegrees: {RealDegree}";
    }

    public static Degrees operator +(Degrees degree1, Degrees degree2)
    {
        return new Degrees(degree1.RealDegree + degree2.RealDegree);
    }
    public static Degrees operator +(Degrees degree1, float degree2)
    {
        return new Degrees(degree1.RealDegree + degree2);
    }
    public static Degrees operator +(Degrees degree1, int degree2)
    {
        return new Degrees(degree1.RealDegree + degree2);
    }


    public static Point GetPointOnCircleEdge(Point circleCenter, double radius, double angleDegrees)
    {
        // Convertir el ángulo a radianes
        double angleRadians = angleDegrees * Math.PI / 180.0;

        // Calcular la posición del borde del círculo basado en el ángulo dado
        double x = circleCenter.X + radius * Math.Cos(angleRadians);
        double y = circleCenter.Y + radius * Math.Sin(angleRadians);

        return new Point(x, y);
    }
    public static Point GetCircleCenterFromPointOnEdge(Point pointOnEdge, double radius, double angleDegrees)
    {
        // Convertir el ángulo a radianes
        double angleRadians = angleDegrees * Math.PI / 180.0;

        // Calcular la posición del centro del círculo basado en el ángulo dado
        // Se mueve en la dirección opuesta, por eso se usan signos negativos
        double x = pointOnEdge.X - radius * Math.Cos(angleRadians);
        double y = pointOnEdge.Y + radius * Math.Sin(angleRadians);

        return new Point(x, y);
    }

    public static double GetAngleFromPoints(Point center, Point pointOnEdge)
    {
        // Calcular el vector desde el centro hasta el punto en el borde
        double deltaX = pointOnEdge.X - center.X;
        double deltaY = center.Y - pointOnEdge.Y; // Invertir el eje Y para coincidir con la orientación de la pantalla

        // Calcular el ángulo en radianes y convertirlo a grados
        double angleRadians = Math.Atan2(deltaY, deltaX);
        double angleDegrees = angleRadians * (180.0 / Math.PI);

        // Normalizar el ángulo para que esté entre 0 y 360 grados
        angleDegrees = (angleDegrees + 360) % 360;

        return angleDegrees;
    }



}

public struct Percentage
{
    private double value;

    public double Value
    {
        get { return value; }
        set { this.value = Math.Clamp(value, 0, 1); }
    }

    public double ValueAsPercentage => value * 100;

    public Percentage(double value)
    {
        this.value = Math.Clamp(value, 0, 1);
    }

    public Percentage(double value, bool isPercentage)
    {
        if (isPercentage)
        {
            this.value = Math.Clamp(value / 100, 0, 1);
        }
        else
        {
            this.value = Math.Clamp(value, 0, 1);
        }
    }

    public static implicit operator Percentage(double value)
    {
        return new Percentage(value);
    }

    public static implicit operator double(Percentage percentage)
    {
        return percentage.Value;
    }

    public override string ToString()
    {
        return ValueAsPercentage.ToString("0.##") + "%";
    }
}



public class ColorTag
{
    public ColorTag(string name, Color? color, float size = 1)
    {
        Name = name;
        Color = color;
        Size = size;
    }
    public ColorTag()
    {
            
    }

    public string Name { get; set; } 
    public Color? Color { get; set; } 
    public float Size { get; set; }

    public override string ToString()
    {
        return $"{Name} {Color}";
    }

    // Sobrecarga del operador ==
    public static bool operator ==(ColorTag type, string name)
    {
        // Compara el nombre del objeto Type con la cadena
        if (ReferenceEquals(type, null))
            return name == null;

        return type.Name == name;
    }

    // Sobrecarga del operador !=
    public static bool operator !=(ColorTag type, string name)
    {
        return !(type.Name == name);
    }

    // Sobreescribe Equals para garantizar consistencia con la sobrecarga del operador ==
    public override bool Equals(object obj)
    {
        if (obj is string)
            return this == (string)obj;
        if (obj is ColorTag tag)
            return this.Name == tag.Name;

        return false;
    }

    public static ColorTag FromKeyframe(string keyframeType)
    {
        return AnimationManager.GetKeyframeType(keyframeType);
    }

}


public static class ManualColors
{
    public static Color? NoColor => null;

    public static Color Red => (Color)ColorConverter.ConvertFromString("#533");
    public static Color Brown => (Color)ColorConverter.ConvertFromString("#593930");
    public static Color Green => (Color)ColorConverter.ConvertFromString("#353");
    public static Color Blue => (Color)ColorConverter.ConvertFromString("#335");
    public static Color PaleBlue => (Color)ColorConverter.ConvertFromString("#3f5159");
    public static Color Cyan => (Color)ColorConverter.ConvertFromString("#355");
    public static Color Violet => (Color)ColorConverter.ConvertFromString("#436");
    public static Color Purple => (Color)ColorConverter.ConvertFromString("#535");
    public static Color Pink => (Color)ColorConverter.ConvertFromString("#644065");
    public static Color Yellow => (Color)ColorConverter.ConvertFromString("#653");
    public static Color Orange => (Color)ColorConverter.ConvertFromString("#7E544C");
    public static Color Black => Colors.Black;
    public static Color Gray => Colors.Gray;
    public static Color White => (Color)ColorConverter.ConvertFromString("#CCC");
}
