using Manual.Core.Nodes.ComfyUI;
//using CefSharp.DevTools.LayerTree;
using Manual.Objects;
using Manual.Objects.UI;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.IO;
//using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media;


//using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PointF = System.Drawing.PointF;

namespace Manual.Core;





public enum Quality
{
    Full,
    Medium,
    Low,
    Very_Low,
}

public class RenderAreaOptions
{
    public Quality Quality = Quality.Low;
    public bool EnableEffects = true;
    public bool EnableOpacity = true;
    public bool EnableVisibility = true;
    public bool EnablePreviews = true;

    public SKSurface? Surface;
    public SKCanvas? Canvas;
    public Action<SKPaint, LayerBase>? Paint;

    public Dictionary<LayerBase, SKBlendMode> BlendMode = new();

    public Dictionary<LayerBase, List<Effect>> Effects = new();
}


public static partial class RendGPU
{
    /// <summary>
    /// Serializa un SKBitmap a PNG con la máxima calidad posible.
    /// </summary>
    /// <param name="bitmap">El bitmap a serializar.</param>
    /// <returns>Un array de bytes que representa el bitmap en formato PNG.</returns>
    public static byte[] ToByte(this SKBitmap bitmap)
    {
        using (var image = SKImage.FromBitmap(bitmap))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) // 100 indica la máxima calidad
        {
            return data.ToArray();
        }
    }
    public static SKBitmap ToSKBitmap(this byte[] pngBytes)
    {
        using (var stream = new MemoryStream(pngBytes))
        {
            return SKBitmap.Decode(stream);
        }
    }

    public static SKBitmap RenderArea(BoundingBox area, LayerBase layer, RenderAreaOptions? options = null)
    {
        return RenderArea(area, [layer], options);
    }
    public static SKBitmap RenderArea(CanvasObject area, LayerBase layer, RenderAreaOptions? options = null)
    {
        return RenderArea(area, [layer], options);
    }

    public static SKBitmap RenderArea(LayerBase source, LayerBase dest, RenderAreaOptions? options = null)
    {
        return RenderArea(source, [source, dest], options);
    }

    public static SKBitmap RenderFrame(Shot shot, RenderAreaOptions? options = null)
    {
        return RenderArea(shot.MainCamera, shot.LayersR, options);
    }

    //------------------------------------------------------------------------------------------------------------------------------------------------------- MAIN RENDER AREA
    public static SKBitmap RenderArea(CanvasObject areaCanvas, IEnumerable<LayerBase> layers, RenderAreaOptions? options = null)
    {
        options ??= new();

        var qualityFactor = GetQualityFactor(options.Quality);
        // Asegúrate de que el factor de calidad esté entre 0 y 1
        qualityFactor = Math.Clamp(qualityFactor, 0.1f, 1f);
        var skQuality = SKFilterQuality.High; //options.Quality.ToSKQuality();

        var finalImageInfo = new SKImageInfo((int)(areaCanvas.ImageWidth), (int)(areaCanvas.ImageHeight));
        var finalImage = new SKBitmap(finalImageInfo);

        if (options.Canvas == null)
            options.Canvas = new SKCanvas(finalImage);

        using (var canvas = options.Canvas)
        {
            canvas.Clear(SKColors.Transparent);

            for (int i = 0; i < layers.Count(); i++)
            {
                var layer = layers.ElementAt(i);


                if ((options.EnableVisibility && (!layer._Animation.IsActuallyVisible || !layer.Visible)) 
                    || layer.Opacity == 0 || layer.Image == null || layer is UILayerBase || (layer.IsAlphaMask && layer.ShotParent.EnableEffects)) continue;


                //----------- SKPAINT
                SKPaint paint;
                byte opacity = options.EnableOpacity ? (byte)(layer.RealOpacity * 255) : (byte)255;
                paint = new SKPaint
                {
                    Color = SKColors.White.WithAlpha(opacity),
                    FilterQuality = skQuality,

                    IsAntialias = true,
                };
                //BLEND MODE
                if (options.BlendMode.Any())
                    paint.BlendMode = options.BlendMode[layer];
                else
                    paint.BlendMode = layer.BlendMode.ToSkiaBlendMode();


                //------------------------------------------------------  EFFECTS
                if (options.EnableEffects)
                {
                    SKImageFilter? filters;
                    if (options.Effects.Any() && options.Effects.TryGetValue(layer, out var effects))
                        filters = layer.GetEffects(effects);
                    else
                        filters = layer.GetEffects();


                    if (filters != null)
                        paint.ImageFilter = filters;
                }



                if (options.Paint != null)
                    options.Paint(paint, layer);


                SKBitmap layerImageRender;

                //------------------------------------------------------------------------------------ ADJUSTMENT LAYER
                if (layer is AdjustmentLayer adj)
                {
                    layerImageRender = layer.Image.Copy();
                    using (var layerCanvas = new SKCanvas(layerImageRender))
                    {
                        using var _p = new SKPaint() { BlendMode = SKBlendMode.SrcIn };
                        //  layerCanvas.Clear(SKColors.Transparent);
                        if (options.Surface != null)
                            layerCanvas.DrawImage(options.Surface.Snapshot(), -layer.PositionX, -layer.PositionY, _p);
                        else
                            layerCanvas.DrawBitmap(finalImage, -layer.PositionX, -layer.PositionY, _p);

                    }

                }

                //------ TEXT
                else if (layer is TextLayer textL)
                {
                    using var paintt = new SKPaint
                    {
                        Color = textL.BrushColor.ToSKColor(),
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextSize = textL.Size,
                        Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                        TextAlign = SKTextAlign.Center,

                    };

                    layerImageRender = SolidColor(SKColors.Transparent, layer.ImageWidth, layer.ImageHeight);
                    using var canvast = new SKCanvas(layerImageRender);
                    //canvast.Clear(SKColors.Transparent);

                    string text = textL.Text;
                    SKRect textBounds = new SKRect();
                    paint.MeasureText(text, ref textBounds);

                    float x = (canvast.DeviceClipBounds.Width - textBounds.Width) / 2;
                    float y = (canvast.DeviceClipBounds.Height + textBounds.Height) / 2;

                    canvast.DrawText(text, x, y, paintt);




                }


                //---------------------------NORMAL LAYER
                else
                {
                    if (!options.EnablePreviews && layer.PreviewValue.IsPreviewMode)
                        layerImageRender = layer.PreviewValue.originalValue;
                    else
                        layerImageRender = layer.Image;
                }


                //------------------------------------------------------------------------------------MASK
                bool expanded = false;
                int nextIndex = i + 1;
                if (layer.ShotParent != null && layer.ShotParent.EnableEffects && nextIndex <= layers.Count() - 1 && layers.ElementAtOrDefault(nextIndex) is LayerBase fm && fm.IsAlphaMask)
                {
                    expanded = true;

                    layer.ImageRender = ExpandLayer(layer, areaCanvas, finalImageInfo, skQuality: skQuality);
                    layerImageRender = layer.ImageRender;
                 
                    SKBitmap compositeBitmap = new SKBitmap(layerImageRender.Width, layerImageRender.Height);
                    using (var tempCanvas = new SKCanvas(compositeBitmap))
                    {
                        tempCanvas.Clear(SKColors.Transparent);
                        tempCanvas.DrawBitmap(layerImageRender, 0f, 0f);  // Dibuja la imagen base


                        while (nextIndex <= layers.Count() - 1 && layers.ElementAtOrDefault(nextIndex) is LayerBase maskL && maskL.IsAlphaMask)
                        {
                            if (maskL != null)
                            {
                                if (maskL.Visible == false)
                                {
                                    nextIndex++;
                                    continue;
                                }

                                // normalize to camera
                                var maskEffects = maskL.GetEffects();
                                var fullMask = ExpandLayer(maskL, areaCanvas, finalImageInfo, maskEffects, skQuality);
                                maskL.ImageRender = fullMask;
                                var blendMode = maskL.BlendMode.ToSkiaBlendMode();
                                // Aplicar la máscara

                                var maskRender = Mask(layerImageRender, fullMask, SKBlendMode.SrcOver, skQuality);

                                using (var maskShader = SKShader.CreateBitmap(maskRender, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp))
                                {
                                    using (var paint2 = new SKPaint())
                                    {
                                        paint2.BlendMode = blendMode;
                                        paint2.Shader = maskShader;
                                        var opacity2 = (byte)(maskL.RealOpacity * 255);
                                        paint2.Color = SKColors.White.WithAlpha(opacity2);
                                        paint2.FilterQuality = skQuality;
                                        paint2.IsAntialias = true;

                                        tempCanvas.DrawRect(new SKRect(0f, 0f, layerImageRender.Width, layerImageRender.Height), paint2);
                                    }
                                }

                                nextIndex++; // Mover al siguiente índice para buscar otra posible máscara 'IsAlphaMask'
                            }
                            else
                            {
                                break; // Salir del bucle si el siguiente elemento no es una capa válida o no es una máscara
                            }
                        }

                        canvas.DrawBitmap(compositeBitmap, 0f, 0f, paint);
                    }
                }




                //------------------------------------------------------------------------final draw

                if (!expanded)
                {
                    var newPos = layer.RelativeToPosition(areaCanvas);
                    var matrix = RelativeMatrix(areaCanvas, layer);
                    canvas.SetMatrix(matrix);

                  //  if(layerImageRender.Height > 0)
                 //   canvas.DrawBitmap(layerImageRender, (float)newPos.X, (float)newPos.Y, paint);
                    canvas.DrawBitmap(layerImageRender, 0, 0, paint);
                }


                canvas.ResetMatrix();
                paint.Dispose();
            }
        }


        return finalImage;


    }


    static SKBitmap ExpandLayer(LayerBase layer, CanvasObject areaCanvas, SKImageInfo finalImageInfo, SKImageFilter? filters = null, SKFilterQuality skQuality = SKFilterQuality.High)
    {
        // Aquí ajustas el tamaño de la imagen de la capa antes de dibujarla
        //    var finalImageInfo = new SKImageInfo((int)(areaCanvas.RealWidth * qualityFactor), (int)(areaCanvas.RealHeight * qualityFactor));

        var finalImage2 = new SKBitmap(finalImageInfo);
        using (var canvas2 = new SKCanvas(finalImage2))
        {
            var matrix = RelativeMatrix(areaCanvas, layer);
            canvas2.SetMatrix(matrix);

            var resizedImage = layer.Image;//ResizeImage(layer.Image.ToSKBitmap(), qualityFactor, skQuality);//****
            var paint2 = new SKPaint
            {
                FilterQuality = skQuality,
                IsAntialias = true,
            };

            if (filters != null && layer.ShotParent.EnableEffects)
                paint2.ImageFilter = filters;

            var newPos = layer.RelativeToPosition(areaCanvas);
            canvas2.DrawBitmap(resizedImage, (float)newPos.X, (float)newPos.Y, paint2);

            return finalImage2;
        }
    }


    public static SKBitmap Mask(SKBitmap sourceBitmap, SKBitmap maskBitmap, SKBlendMode blendMode = SKBlendMode.SrcOver, SKFilterQuality skQuality = SKFilterQuality.High)
    {
        // Crear un nuevo bitmap del mismo tamaño que el bitmap de origen
        var resultBitmap = new SKBitmap(sourceBitmap.Width, sourceBitmap.Height);

        // Crear un canvas para dibujar el resultado
        using (var canvas = new SKCanvas(resultBitmap))
        {
            //canvas.Clear(SKColors.Transparent);  // Limpiar el canvas

            // Crear un shader de imagen para el bitmap de origen
            using (var sourceShader = SKShader.CreateBitmap(sourceBitmap, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp))
            // Crear un shader de imagen para la máscara
            using (var maskShader = SKShader.CreateBitmap(maskBitmap, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp))
            {
                // Configurar el SKPaint para usar el bitmap como máscara
                using (var paint = new SKPaint())
                {
                    paint.FilterQuality = skQuality;
                    // Configurar el paint para usar el shader de la máscara
                    paint.Shader = maskShader;
                    // Dibujar el bitmap de máscara en el canvas, que configura el canal alpha
                    canvas.DrawRect(new SKRect(0, 0, maskBitmap.Width, maskBitmap.Height), paint);

                    // Configurar el paint para dibujar el bitmap de origen usando el shader de origen
                    paint.Shader = sourceShader;
                    paint.BlendMode = SKBlendMode.DstIn;  // Usar el canal alpha establecido por la máscara

                    // Dibujar el bitmap de origen sobre la máscara;
                    canvas.DrawRect(new SKRect(0, 0, sourceBitmap.Width, sourceBitmap.Height), paint);
                }
            }
        }

        return resultBitmap;
    }

    static SKMatrix RelativeMatrix(CanvasObject areaCanvas, CanvasObject layer)
    {
        //POSITION
        var matrix = SKMatrix.CreateTranslation(0, 0);

        var newW = layer.NormalizedWidth / areaCanvas.NormalizedWidth;
        var newH = layer.NormalizedHeight / areaCanvas.NormalizedHeight;

        //ROTATION
        if (layer.RealRotation != 0)
        {
            float centerX = layer.AnchorPointX / newW; //WOOO WHERE THE PARTY GOOO
            float centerY = layer.AnchorPointY / newH; // NANANANA NA NANA NA NA


            SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(layer.RealRotation, centerX, centerY);
            matrix = matrix.PostConcat(rotationMatrix);
        }

        // POSITION
       var newPos = layer.RelativeToPosition(areaCanvas);
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation((float)newPos.X, (float)newPos.Y));

        //SCALE
        //if (layer.Scale.X != 100 || layer.Scale.Y != 100)
            matrix = matrix.PostConcat(SKMatrix.CreateScale(newW, newH));

        return matrix;
    }



    public static SKBitmap Inpaint(LayerBase layer, CanvasObject area, SKBitmap areaInp, OutputBlendMode blendMode = OutputBlendMode.Front)
    {
        var r = new Layer(areaInp);
        r.CopyDimensions(area);


        var blend = blendMode switch
        {
            OutputBlendMode.Front => SKBlendMode.SrcOver,
            OutputBlendMode.Behind => SKBlendMode.DstOver,
            OutputBlendMode.Replace => SKBlendMode.Src,
            OutputBlendMode.OnTop => SKBlendMode.SrcATop,
            _ => SKBlendMode.SrcOver // Esto maneja todos los casos no especificados
        };


        var options = new RenderAreaOptions();
        options.EnableOpacity = false;
        options.EnableVisibility = false;
        options.EnablePreviews = false;
        options.BlendMode = new Dictionary<LayerBase, SKBlendMode>
                {
                    { layer, SKBlendMode.SrcOver },
                    { r, blend }
                };

        //SKBitmap result;
        //if (layerMask != null)
        //{
        //    options.BlendMode[layerMask] = SKBlendMode.SrcATop;
        //    result = RenderArea(layer, [layer], options);
        //}
        

        var result = RenderArea(layer, r, options);
        return result;
    }

    public static SKBitmap Inpaint(LayerBase layer, CanvasObject area, SKBitmap areaInp, Keyframe targetKey, OutputBlendMode blendMode = OutputBlendMode.Front)
    {
        var kimg = targetKey.Value as SKBitmap;
        LayerBase frezedLayer;
        if (kimg != null)
            frezedLayer = new Layer(kimg);
        else
            frezedLayer = layer;

        return Inpaint(frezedLayer, area, areaInp, blendMode);
    }



    public static SKImage ScaleBy(this SKBitmap originalBitmap, float scale)
    {
        // Convertir SKBitmap a SKImage
        using (var originalImage = SKImage.FromBitmap(originalBitmap))
        {
            // Calcular las nuevas dimensiones
            int newWidth = (int)(originalImage.Width * scale);
            int newHeight = (int)(originalImage.Height * scale);

            // Crear un nuevo SKImage con las dimensiones escaladas
            using (var surface = SKSurface.Create(new SKImageInfo(newWidth, newHeight)))
            {
                var canvas = surface.Canvas;
                canvas.Scale(scale, scale);
                canvas.DrawImage(originalImage, 0, 0);
                return surface.Snapshot();
            }
        }
    }
    public static SKBitmap ScaleTo(this SKBitmap originalBitmap, int newWidth, int newHeight)
    {
        // Crear un nuevo bitmap con las dimensiones específicas
        SKImageInfo imageInfo = new SKImageInfo(newWidth, newHeight);
        SKBitmap resizedBitmap = new SKBitmap(imageInfo);

        // Redimensionar el bitmap original al nuevo tamaño
        originalBitmap.ScalePixels(resizedBitmap, SKFilterQuality.High); // Puedes ajustar la calidad del filtro según necesidad

        return resizedBitmap;
    }

    public static SKBitmap ScaleToWidth(this SKBitmap bitmap, int width)
    {
        // Calcula la nueva altura manteniendo la relación de aspecto
        int newHeight = (int)(width * bitmap.Height / (float)bitmap.Width);

        // Crea una nueva bitmap escalada
        var scaledBitmap = bitmap.Resize(new SKImageInfo(width, newHeight), SKFilterQuality.High);

        return scaledBitmap;
    }

    public static SKBitmap ScaleToHeight(this SKBitmap bitmap, int height)
    {
        // Calcula la nueva anchura manteniendo la relación de aspecto
        int newWidth = (int)(height * bitmap.Width / (float)bitmap.Height);

        // Crea una nueva bitmap escalada
        var scaledBitmap = bitmap.Resize(new SKImageInfo(newWidth, height), SKFilterQuality.High);

        return scaledBitmap;
    }












    public static void Clear(this SKBitmap bitmap, SKColor color)
    {
        bitmap.Erase(color);
    }

    public static SKBitmap SolidColor(SKColor color, int width, int height)
    {
        SKBitmap bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        return bitmap;
    }

    //------------------------------------------------------------------------------------------------------------------- EFFECTS
    public static SKBitmap Blur(SKBitmap skBitmap, float blurAmount)
    {
         // Crear un nuevo bitmap para el resultado
        var blurredBitmap = new SKBitmap(skBitmap.Width, skBitmap.Height);

        // Crear un canvas para dibujar la imagen desenfocada
        using (var canvas = new SKCanvas(blurredBitmap))
        {
            // Limpiar el canvas
            canvas.Clear(SKColors.Transparent);

            // Aplicar el efecto de desenfoque
            using (var paint = new SKPaint())
            {
                paint.ImageFilter = SKImageFilter.CreateBlur(blurAmount, blurAmount, SKShaderTileMode.Repeat);
                canvas.DrawBitmap(skBitmap, 0, 0, paint);
            }
        }

        return blurredBitmap;
    }

    public static SKImageFilter Sharpen(float sharpness)
    {
        // Asegúrate de que el valor de sharpness sea positivo y razonable
        sharpness = Math.Max(sharpness, 0);

        // Calcula los valores del kernel basados en el parámetro de sharpness
        float centerValue = 1 + (4 * sharpness);
        float adjacentValue = -1 * sharpness;

        var kernel = new float[]
        {
        0,        adjacentValue, 0,
        adjacentValue, centerValue, adjacentValue,
        0,        adjacentValue, 0
        };

        // Configura los parámetros del filtro de convolución
        var kernelSize = new SKSizeI(3, 3);
        var kernelOffset = new SKPointI(1, 1); // Centro del kernel

        return SKImageFilter.CreateMatrixConvolution(
            kernelSize,
            kernel,
            1.0f, // Ganancia
            0.0f, // Bias (no se añade ningún offset a los valores)
            kernelOffset,
            SKShaderTileMode.Clamp, // Manejo de los bordes
            false, // No convolucionar con alfa
            null   // Sin filtro de imagen de entrada
        );
    }


    public static SKImageFilter ColorAdjust(float R, float G, float B)
    {
        // Crea una matriz de color que ajusta los canales RGB individualmente
        float[] matrix = {
            R, 0, 0, 0, 0,    // Rojo
            0, G, 0, 0, 0,  // Verde
            0, 0, B, 0, 0,   // Azul
            0, 0, 0, 1, 0   // Alpha
        };

        // Crea un filtro de color usando la matriz
        SKColorFilter colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        SKImageFilter imageFilter = SKImageFilter.CreateColorFilter(colorFilter);

        return imageFilter;
    }

    //EEEEE
    public static SKImageFilter HSLAdjust(float hue, float saturation, float lightness)
    {
        // Normalizar el hue para que esté en el rango [0, 1]
        hue /= 360f;

        // Convertir HSL a RGB
        var color = HSLToRGB(hue, saturation, lightness);

        // Usar los valores RGB convertidos para ajustar la imagen
        return ColorAdjust(color.r, color.g, color.b);
    }
    public static (float r, float g, float b) HSLToRGB(float h, float s, float l)
    {
        float r, g, b;

        if (s == 0)
        {
            r = g = b = l; // Achromatic, same as grayscale
        }
        else
        {
            Func<float, float, float, float> hue2rgb = (v1, v2, vh) =>
            {
                if (vh < 0) vh += 1;
                if (vh > 1) vh -= 1;
                if ((6 * vh) < 1) return (v1 + (v2 - v1) * 6 * vh);
                if ((2 * vh) < 1) return v2;
                if ((3 * vh) < 2) return (v1 + (v2 - v1) * ((2.0f / 3) - vh) * 6);
                return v1;
            };

            float v2 = (l < 0.5) ? (l * (1 + s)) : ((l + s) - (l * s));
            float v1 = 2 * l - v2;

            r = hue2rgb(v1, v2, h + (1.0f / 3));
            g = hue2rgb(v1, v2, h);
            b = hue2rgb(v1, v2, h - (1.0f / 3));
        }

        return (r, g, b);
    }


    public static SKImageFilter HSVAdjust(float hue, float saturation, float value)
    {
        var color = HSVToRGB(hue, saturation, value);

        // Usar los valores RGB convertidos para ajustar la imagen
        return ColorAdjust(color.r, color.g, color.b);
    }

    public static (float r, float g, float b) HSVToRGB(float h, float s, float v)
    {
        h = (h + 180) % 360; // Normalizar H a [0, 360]
        s = (s + 100) / 100; // Normalizar S a [0, 2]
        v = (v + 100) / 100; // Normalizar V a [0, 2]

        float r, g, b;

        int i = (int)(h / 60) % 6;
        float f = h / 60 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        switch (i)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
            default: r = g = b = 0; break;
        }
        return (r, g, b);
    }
 



    //---------------------------------------------------------------------------------------- DRAWING


    public static SKBitmap DrawStoke(this LayerBase layer, PointF initialPosition, PointF finalPosition, Brusher brush)
    {
      
        var initial = PixelPoint.DividePointF(initialPosition, new PointF(layer.NormalizedWidth, layer.NormalizedHeight));
        var final = PixelPoint.DividePointF(finalPosition, new PointF(layer.NormalizedWidth, layer.NormalizedHeight));
   
        using (var canvas = new SKCanvas(layer.ImagePreview))
        {

            var paint = new SKPaint
            {
                IsAntialias = brush.IsAntialias,
                FilterQuality = SKFilterQuality.Low,
                //StrokeCap = SKStrokeCap.Round,
                Style = SKPaintStyle.Fill
            };
            paint.Color = brush.ColorBrush.ToSKColor();
            //if (brush.Opacity.EnablePenPressure)
            //{
            //    var op = (byte)((brush.Opacity.RealValue / 100) * 255);
            //    paint.Color = paint.Color.WithAlpha(op);
            //}


            float distance = MathF.Sqrt(MathF.Pow(final.X - initial.X, 2) + MathF.Pow(final.Y - initial.Y, 2));
            // Número de puntos intermedios basado en el tamaño del paso
            int steps = distance == 0 ? 1 : (int)MathF.Ceiling(distance / (brush.Size.RealValue / 5f));


            for (int i = 0; i <= steps; i++)
            {
                float t = steps != 0 ? i / (float)steps : 1;

                float lerpX = initial.X + (final.X - initial.X) * t;
                float lerpY = initial.Y + (final.Y - initial.Y) * t;

                float initSize = brush.Size.initialRealValue;
                float realSize = brush.Size.RealValue;
                float size = initSize + (realSize - initSize) * t;

                // Dibujar un círculo
                canvas.DrawCircle(lerpX, lerpY, size / 2, paint);

            }

        }

        return layer.ImagePreview;
    }



    public static SKBitmap DrawDistort(this SKBitmap sourceImage, SKPoint initialPosition, SKPoint finalPosition, Brusher brush)
    {
        float brushRadius = brush.Size.Value;
        // Crea un nuevo bitmap para el resultado
        SKBitmap resultBitmap = sourceImage.Copy();//new SKBitmap(sourceImage.Width, sourceImage.Height);

        SKPoint delta =  initialPosition - finalPosition;

     //  var sourceImageCopy = resultBitmap;//sourceImage.Copy();

        Parallel.For(0, sourceImage.Height, y =>
        //for (int y = 0; y < sourceImage.Height; y++)
        {
            for (int x = 0; x < sourceImage.Width; x++)
            {
                // Calcula la distancia desde el punto inicial hasta este píxel
                float distance = SKPoint.Distance(new SKPoint(x, y), initialPosition);

                // Calcula la nueva posición del píxel basada en el factor de deformación
                float factor = (distance < brushRadius) ? (brushRadius - distance) / brushRadius : 0;
                if (factor == 0)
                    continue;

                float newX = x + delta.X * factor;
                float newY = y + delta.Y * factor;

                // Aplicar interpolación bilineal para calcular el color del píxel
                SKColor color = GetBilinearInterpolatedColor(sourceImage, newX, newY);
                resultBitmap.SetPixel(x, y, color);
            }
        });


        using var canvas = new SKCanvas(sourceImage);
        using var paint = new SKPaint()
        {
            BlendMode = SKBlendMode.Src,
        };
        canvas.DrawBitmap(resultBitmap, 0, 0, paint);

        return sourceImage;
    }

    private static SKColor GetBilinearInterpolatedColor(SKBitmap bitmap, float x, float y)
    {
        int x1 = Math.Clamp((int)x, 0, bitmap.Width - 1);
        int y1 = Math.Clamp((int)y, 0, bitmap.Height - 1);
        int x2 = Math.Clamp(x1 + 1, 0, bitmap.Width - 1);
        int y2 = Math.Clamp(y1 + 1, 0, bitmap.Height - 1);

        // Obten los colores de los cuatro puntos más cercanos
        SKColor c11 = bitmap.GetPixel(x1, y1);
        SKColor c21 = bitmap.GetPixel(x2, y1);
        SKColor c12 = bitmap.GetPixel(x1, y2);
        SKColor c22 = bitmap.GetPixel(x2, y2);

        // Calcular las fracciones de x y y
        float fx = x - x1;
        float fy = y - y1;

        // Interpolar en x entre las filas
        SKColor top = InterpolateColor(c11, c21, fx);
        SKColor bottom = InterpolateColor(c12, c22, fx);

        // Interpolar en y entre las columnas interpoladas
        return InterpolateColor(top, bottom, fy);
    }

    private static SKColor InterpolateColor(SKColor c1, SKColor c2, float fraction)
    {
        // Aplicar una función exponencial a la fracción
        float expFraction = fraction; //EaseInOutExpo(fraction);

        byte r = (byte)(c1.Red + (c2.Red - c1.Red) * expFraction);
        byte g = (byte)(c1.Green + (c2.Green - c1.Green) * expFraction);
        byte b = (byte)(c1.Blue + (c2.Blue - c1.Blue) * expFraction);
        byte a = (byte)(c1.Alpha + (c2.Alpha - c1.Alpha) * expFraction);

        return new SKColor(r, g, b, a);
    }


    // Función de interpolación exponencial "ease-in-out"
    private static float EaseInOutExpo(float x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        return (float)(Math.Log(1 + (Math.E - 1) * x) / Math.Log(Math.E));
    }




    public static void FloodFill(SKBitmap bitmap, SKPoint startPoint, SKColor fillColor, int threshold)
    {
        // Color del punto inicial
        SKColor targetColor = bitmap.GetPixel((int)startPoint.X, (int)startPoint.Y);
        if (targetColor.Equals(fillColor)) return;  // Si el color de relleno es el mismo, no hacer nada.

        Stack<SKPoint> pixels = new Stack<SKPoint>();
        pixels.Push(startPoint);

        bool[,] visited = new bool[bitmap.Width, bitmap.Height];

        while (pixels.Count > 0)
        {
            SKPoint point = pixels.Pop();
            int x = (int)point.X;
            int y = (int)point.Y;

            if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height || visited[x, y])
                continue;

            visited[x, y] = true; // Marcar como visitado

            SKColor currentColor = bitmap.GetPixel(x, y);

            if (currentColor == fillColor) continue; // No procesar si el píxel ya es del color de relleno

            if (ColorMatch(currentColor, targetColor, threshold) && currentColor != fillColor)
            {
                bitmap.SetPixel(x, y, fillColor);
                pixels.Push(new SKPoint(x - 1, y)); // izquierda
                pixels.Push(new SKPoint(x + 1, y)); // derecha
                pixels.Push(new SKPoint(x, y - 1)); // arriba
                pixels.Push(new SKPoint(x, y + 1)); // abajo
            }
        }

    }


    private static bool ColorMatch(SKColor c1, SKColor c2, int threshold)
    {
        return Math.Abs(c1.Red - c2.Red) <= threshold &&
               Math.Abs(c1.Green - c2.Green) <= threshold &&
               Math.Abs(c1.Blue - c2.Blue) <= threshold;
    }









    //-------------------------------------------------------------------------------------------------------------------------- EFFECTS

    public static SKBitmap ConvertToGrayscale(SKBitmap original)
    {
        var grayBitmap = new SKBitmap(original.Width, original.Height, SKColorType.Gray8, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(grayBitmap))
        {
            canvas.Clear(SKColors.White);
            var paint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0.2126f, 0.7152f, 0.0722f, 0, 0,
                0,       0,       0,       1, 0
                }),
            };
            canvas.DrawBitmap(original, 0, 0, paint);
        }
        return grayBitmap;
    }
    public static SKBitmap ApplyEdgeDetection(SKBitmap grayBitmap)
    {
        var width = grayBitmap.Width;
        var height = grayBitmap.Height;
        var edgeBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        int[,] gx = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] gy = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        Parallel.For(1, height - 1, (y) =>
        {
            for (int x = 1; x < width - 1; x++)
            {
                float pixelX = ApplyKernel(grayBitmap, gx, x, y);
                float pixelY = ApplyKernel(grayBitmap, gy, x, y);

                var magnitude = (int)Math.Sqrt(pixelX * pixelX + pixelY * pixelY);
                magnitude = Math.Clamp(magnitude, 0, 255);

                edgeBitmap.SetPixel(x, y, new SKColor((byte)magnitude, (byte)magnitude, (byte)magnitude));
            }
        });

        return edgeBitmap;
    }

    private static float ApplyKernel(SKBitmap bitmap, int[,] kernel, int x, int y)
    {
        float px = 0;
        for (int ky = -1; ky <= 1; ky++)
        {
            for (int kx = -1; kx <= 1; kx++)
            {
                var color = bitmap.GetPixel(x + kx, y + ky);
                px += color.Red * kernel[kx + 1, ky + 1];
            }
        }
        return px;
    }



    public static SKImageFilter Lineart(float kernelScale = 1)
    {
        int kernelSize = 3;
        // Un ejemplo simple de un kernel de Laplace que detecta bordes
        float[] kernel = new float[]
        {
        0,  1, 0,
        1, -4, 1,
        0,  1, 0
        };
        float[] kernel2 = new float[]
      {
        -1, -1, -1,
        -1,  8, -1,
        -1, -1, -1
      };
        // Crear el filtro de convolución
        var filter = SKImageFilter.CreateMatrixConvolution(
            new SKSizeI(kernelSize, kernelSize),
            kernel,
            kernelScale,
            0,
            SKPointI.Empty,
            SKShaderTileMode.Clamp,
            false
        );
        return filter;
    }



    public static SKImageFilter InvertColors()
    {
        float[] colorMatrix = {
            -1,  0,  0, 0, 255,
             0, -1,  0, 0, 255,
             0,  0, -1, 0, 255,
             0,  0,  0, 1,   0
        };
        var colorf = SKColorFilter.CreateColorMatrix(colorMatrix);
        return SKImageFilter.CreateColorFilter(colorf);
    }



    public static SKBitmap ApplyPatternToImage(LayerBase layer, SKBitmap pattern)
    {

        SKBitmap sourceImage = layer.Image;

        var resultBitmap = new SKBitmap(sourceImage.Width, sourceImage.Height);
        using (var canvas = new SKCanvas(resultBitmap))
        {
            canvas.DrawBitmap(sourceImage, layer.PositionX, layer.PositionY);
            var shader = SKShader.CreateBitmap(pattern, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);

            var opacity = (byte)(layer.RealOpacity * 255);
            var paint = new SKPaint
            {
                Shader = shader,
                BlendMode = SKBlendMode.SrcATop, //SKBlendMode.DstIn,
                Color = SKColors.White.WithAlpha(opacity),
            };

            // Dibuja el patrón encima de la imagen
            canvas.DrawRect(layer.PositionX, layer.PositionY, sourceImage.Width, sourceImage.Height, paint);
        }
        return resultBitmap;
    }

    public static SKBitmap CreateCheckeredPattern(int squareSize, int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            //  canvas.Clear(SKColors.Transparent); // Fondo transparente
            canvas.Clear(SKColors.White.WithAlpha(64)); 

            var paint = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(125) // Semi-transparent gray
            };

            for (int y = 0; y < height; y += squareSize)
            {
                for (int x = 0; x < width; x += squareSize)
                {
                    if ((x / squareSize + y / squareSize) % 2 == 0)
                    {
                        canvas.DrawRect(x, y, squareSize, squareSize, paint);
                    }
                }
            }
        }
        return bitmap;
    }


    public static SKBitmap CreateScreentonePattern(int dotSize, int spacing, int width, int height, bool antialias = true)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent); // Fondo transparente

            var paint = new SKPaint
            {
                Color = SKColors.Black, // Color de los puntos
                IsAntialias = false,     // Suavizar los bordes de los puntos
            };

            // Dibuja una matriz de puntos
            for (int y = 0; y < height; y += spacing)
            {
                for (int x = 0; x < width; x += spacing)
                {
                    canvas.DrawCircle(x, y, dotSize, paint);
                }
            }
        }
        return bitmap;
    }

    public static SKBitmap CreateDynamicScreentonePattern(SKBitmap sourceImage, int maxDotSize, int spacing, bool antialias = true)
    {

        int width = sourceImage.Width;
        int height = sourceImage.Height;
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent); // Fondo transparente

            var paint = new SKPaint
            {
                Color = SKColors.Black, // Color de los puntos
                IsAntialias = antialias,     // Suavizar los bordes de los puntos
            };

            // Dibuja una matriz de puntos ajustando el tamaño según la transparencia
            for (int y = 0; y < height; y += spacing)
            {
                for (int x = 0; x < width; x += spacing)
                {
                    var alpha = sourceImage.GetPixel(x, y).Alpha / 255f; // Normaliza el alpha (0-1)
                    float dotSize = maxDotSize * alpha; // Ajusta el tamaño del dot según el alpha

                    if (dotSize > 0) // Solo dibuja si el dotSize es mayor que 0
                    {
                        canvas.DrawCircle(x, y, dotSize / 2, paint); // Usar dotSize/2 porque el tamaño es el diámetro
                    }
                }
            }
        }
        return bitmap;
    }


    public static SKImageFilter CreateOutlineFilter(int outlineWidth, SKColor outlineColor)
    {
        // Crear un filtro de desplazamiento para mover la imagen y crear un contorno
        var displacementFilter = SKImageFilter.CreateOffset(outlineWidth, outlineWidth, null);

        // Crear un filtro de color para cambiar el color de la imagen desplazada
        var colorFilter = SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(outlineColor, SKBlendMode.SrcIn), displacementFilter);

        // Combinar el filtro de color con un desenfoque para suavizar el contorno
        var blurFilter = SKImageFilter.CreateBlur(0, 0, colorFilter);

        return blurFilter;
    }



    public static SKBitmap CreateGradient(int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        // Definir los colores del degradado
        SKColor[] colors = new SKColor[]
        {
            SKColor.Parse("#30B2FE"), // Azul
            SKColor.Parse("#409DFF"), // Cian
            SKColor.Parse("#BE6DFC"), // Magenta
            SKColor.Parse("#E768C3"), // Amarillo
            SKColor.Parse("#FB7267"), // Rojo
            SKColor.Parse("#FD9926"), // Naranja
        };

        // Definir las posiciones del degradado
        float[] colorPositions = new float[]
        {
            0.0f,
            0.2f,
            0.4f,
            0.6f,
            0.8f,
            1.0f
        };
  
        // Crear el shader de degradado lineal
        SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),                // Punto de inicio del degradado
            new SKPoint(width, 0),     // Punto de fin del degradado
            colors,                           // Colores del degradado
            colorPositions,                   // Posiciones de los colores
            SKShaderTileMode.Clamp            // Modo de tiling
        );

        // Crear el SKPaint con el shader
        using (SKPaint paint = new SKPaint())
        {
            paint.Shader = shader;

            // Dibujar un rectángulo con el degradado
            canvas.DrawRect(new SKRect(0, 0, width, height), paint);
        }

        return bitmap;
    }


    
    public static SKBitmap FlipHorizontal(SKBitmap bitmap)
    {
        SKBitmap flippedBitmap = new SKBitmap(bitmap.Width, bitmap.Height);
        using (SKCanvas canvas = new SKCanvas(flippedBitmap))
        {
            // Crear una matriz para el flip horizontal
            SKMatrix flipMatrix = SKMatrix.MakeScale(-1, 1);
            // Mover la imagen a la posición correcta después de la inversión horizontal
            flipMatrix.TransX = bitmap.Width;

            // Dibujar el bitmap original con la matriz de flip horizontal
            canvas.SetMatrix(flipMatrix);
            canvas.DrawBitmap(bitmap, 0, 0);
        }
        return flippedBitmap;
    }
    public static SKBitmap FlipVertical(SKBitmap bitmap)
    {
        SKBitmap flippedBitmap = new SKBitmap(bitmap.Width, bitmap.Height);
        using (SKCanvas canvas = new SKCanvas(flippedBitmap))
        {
            // Crear una matriz para el flip vertical
            SKMatrix flipMatrix = SKMatrix.MakeScale(1, -1);
            // Mover la imagen a la posición correcta después de la inversión vertical
            flipMatrix.TransY = bitmap.Height;

            // Dibujar el bitmap original con la matriz de flip vertical
            canvas.SetMatrix(flipMatrix);
            canvas.DrawBitmap(bitmap, 0, 0);
        }
        return flippedBitmap;
    }




    //------------------------------------------------------------------------------------------------------ ANIMATIONS


    public static void AnimateFloat(Action<float> updateAction, float from, float to, float duration, Action onCompleted = null, int fps = 24, bool updateRender = true)
    {
        DispatcherTimer timer = new DispatcherTimer();
        float elapsed = 0;
        float interval = 1.0f / fps;
        int totalFrames = (int)(duration * fps);

        timer.Interval = TimeSpan.FromSeconds(interval);
        timer.Tick += (sender, e) =>
        {
            elapsed += interval;
            float t = elapsed / duration;

            if (t >= 1)
            {
                t = 1;
                timer.Stop();
                onCompleted?.Invoke();
            }

            float value = EaseInOut(from, to, t);
            updateAction(value);

            if(updateRender)
             Shot.UpdateCurrentRender();
        };

        timer.Start();
    }

    private static float EaseInOut(float start, float end, float t)
    {
        t = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        return start + (end - start) * t;
    }




    public static SKBitmap CreateInvertedColorEffect(SKBitmap bitmap)
    {
        string shaderCode = @"
    in fragmentProcessor image;

    half4 main(float2 coord) {
        // Obtener el color original del pixel
        half4 originalColor = sample(image, coord);
        // Invertir los colores
        half4 invertedColor = half4(1.0 - originalColor.rgb, originalColor.a);
        return invertedColor;
    }
    ";

        // Compilar el shader
        using var effect = SKRuntimeEffect.Create(shaderCode, out var error);
        if (effect == null)
        {
            throw new InvalidOperationException($"Error compiling shader: {error}");
        }
        // Shader values
        var textureShader = bitmap.Copy().ToShader();
        var children = new SKRuntimeEffectChildren(effect)
        {
            ["image"] = textureShader,
        };

        // create actual shader
        using var shader = effect.ToShader(true, new SKRuntimeEffectUniforms(effect), children);


        // Crear un nuevo bitmap y un canvas para dibujar el resultado
        var resultBitmap = new SKBitmap(bitmap.Width, bitmap.Height);
        using var canvas = new SKCanvas(resultBitmap);
        using var paint = new SKPaint { Shader = shader };

        // Dibujar el bitmap invertido
        canvas.Clear(SKColors.Transparent);
        canvas.DrawRect(new SKRect(0, 0, bitmap.Width, bitmap.Height), paint);

        return resultBitmap;
    }

    public static SKBitmap CreateFishEyeEffect2(SKBitmap bitmap, SKPoint center)
    {
        using var bitmapImage = SKImage.FromBitmap(bitmap);

        // shader
        var src = @"
            in fragmentProcessor color_map;

            uniform float2 uCenter;
            uniform float uRadius;

            half4 main(float2 p) {
                float2 relCoord = (p - uCenter) / uRadius;
                float r = length(relCoord);
                if (r > 0.0) {
                    float theta = atan(relCoord.y / relCoord.x);
                    r = pow(r, 0.75);
                    relCoord = r * float2(cos(theta), sin(theta));
                }
                relCoord = relCoord * uRadius + uCenter;
                relCoord = clamp(relCoord, float2(0.0, 0.0), float2(1.0, 1.0));  // Clamping coordinates

                // Ensure color is returned in all cases
                return sample(color_map, relCoord);
            }";
        // Compilar el shader
        using var effect = SKRuntimeEffect.Create(src, out var error);
        if (effect == null)
        {
            throw new InvalidOperationException($"Error compiling shader: {error}");
        }

        // Crear un shader con el efecto

        // Configurar los uniformes
        var radius = (float)Math.Min(bitmap.Width, bitmap.Height) / 2;
        var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["uCenter"] = new float[] { center.X, center.Y },
            ["uRadius"] = radius,
        };


        // Shader values
        var textureShader = bitmapImage.ToShader();
        var children = new SKRuntimeEffectChildren(effect)
        {
            ["color_map"] = textureShader,
        };

        // create actual shader
        using var shader = effect.ToShader(true, uniforms, children);

        // Draw as normal
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(SKRect.Create(0, 0, bitmap.Width, bitmap.Height), paint);


        return bitmap;
    }

    public static SKBitmap CreateFishEyeEffect(SKBitmap bitmap, SKPoint center)
    {

        // El código del shader que realiza la distorsión de ojo de pez
        string shaderCode = @"
        uniform shader uTexture;
        uniform float2 uCenter;
        uniform float uRadius;

        half4 main(float2 coord) {
            float2 relCoord = (coord - uCenter) / uRadius;
            float r = length(relCoord);
            if (r > 0.0) {
                float theta = atan(relCoord.y / relCoord.x);
                r = pow(r, 0.75);
                relCoord = r * float2(cos(theta), sin(theta));
            }
            relCoord = relCoord * uRadius + uCenter;

            return sample(uTexture, relCoord);
        }

    ";

        // Compilar el shader
        using var effect = SKRuntimeEffect.Create(shaderCode, out var error);
        if (effect == null)
        {
            throw new InvalidOperationException($"Error compiling shader: {error}");
        }

        // Crear un shader con el efecto

        //// Configurar los uniformes
        var uniforms = new SKRuntimeEffectUniforms(effect)
        {
            ["uCenter"] = new float[]{ center.X, center.Y },
            ["uRadius"] = 30.0f,
        };


        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();
        // Aplicar el shader al paint
        paint.Shader = effect.ToShader(true, uniforms);

        // Dibujar algo usando este paint
        canvas.DrawRect(new SKRect(0, 0, 512, 512), paint);

        // Aplicar el shader a un nuevo bitmap usando un canvas
        var resultBitmap = new SKBitmap(bitmap.Width, bitmap.Height);

        canvas.Clear(SKColors.Transparent);

        canvas.DrawRect(new SKRect(0, 0, bitmap.Width, bitmap.Height), paint);

        return resultBitmap;

    }


    public static SKBitmap Test(this SKBitmap bitmap, SKPoint initial)
    {
        var result = bitmap.Copy();
        using SKCanvas canvas = new SKCanvas(result);
        var src = "half4 main(float2 coord) { return half4(1, 0, 0, 1); }";

        using (var effect = SKRuntimeEffect.Create(src, out var errorText))
        {
            using (var shader = effect.ToShader(true, new SKRuntimeEffectUniforms(effect), new SKRuntimeEffectChildren(effect)))
            {
                var paint = new SKPaint
                {
                    Shader = shader
                };

                canvas.DrawRect(new SKRect(0, 0, 100, 100), paint);
            }
        }
        return result;

    }



}




















public static partial class RendGPU
{

    static void Shader(SKCanvas canvas)
    {
        string src = @"
in fragmentProcessor color_map;

uniform float scale;
uniform half exp;
uniform float3 in_colors0;

void main(float2 p, inout half4 color) {
    half4 texColor = sample(color_map, p);
    if (length(abs(in_colors0 - pow(texColor.rgb, half3(exp)))) < scale)
        discard;
    color = texColor;
}";

        // Crear el efecto runtime
        SKRuntimeEffect effect = SKRuntimeEffect.Create(src, out var errorText);
        if (effect == null)
        {
            Console.WriteLine("Error compiling shader: " + errorText);
            return;
        }

        // Usar el efecto
        using (var paint = new SKPaint())
        {
            // Configurar los uniformes
            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms.Add("scale", 0.5f);
            uniforms.Add("exp", 2.0f);
            uniforms.Add("in_colors0", 1.0f);

 
            // Aplicar el shader al paint
            paint.Shader = effect.ToShader(true, uniforms);

            // Dibujar algo usando este paint
            canvas.DrawRect(new SKRect(0, 0, 512, 512), paint);
        }

    }


    public static SKFilterQuality ToSKQuality(this Quality quality)
    {
        switch (quality)
        {
            case Quality.Full: return SKFilterQuality.High;
            case Quality.Medium: return SKFilterQuality.Medium;
            case Quality.Low: return SKFilterQuality.Low;
            case Quality.Very_Low: return SKFilterQuality.None;
            default: return SKFilterQuality.Medium;
        }
    }
    static float GetQualityFactor(Quality quality)
    {
        switch (quality)
        {
            case Quality.Full: return 1.0f;
            case Quality.Medium: return 0.5f;
            case Quality.Low: return 0.25f;
            case Quality.Very_Low: return 0.125f;
            default: return 0.5f;
        }
    }
    public static SKBlendMode ToSkiaBlendMode(this LayerBlendMode mode)
    {
        switch (mode)
        {
            case LayerBlendMode.Normal:
                return SKBlendMode.SrcOver;
            case LayerBlendMode.Darken:
                return SKBlendMode.Darken;
            case LayerBlendMode.Multiply:
                return SKBlendMode.Multiply;
            case LayerBlendMode.Color_Burn:
                return SKBlendMode.ColorBurn;
          //  case LayerBlendMode.Linear_Burn:
                // No hay un equivalente directo en SkiaSharp
            //    break;

            case LayerBlendMode.Add:
                return SKBlendMode.Plus;
            case LayerBlendMode.Lighten:
                return SKBlendMode.Lighten;
            case LayerBlendMode.Screen:
                return SKBlendMode.Screen;
            case LayerBlendMode.Color_Dodge:
                return SKBlendMode.ColorDodge;
            case LayerBlendMode.Linear_Dodge:
                return SKBlendMode.Plus;
            case LayerBlendMode.Hard_Light:
                return SKBlendMode.HardLight;
            case LayerBlendMode.Soft_Light:
                return SKBlendMode.SoftLight;
          //  case LayerBlendMode.Lighter_Color:
                // No hay un equivalente directo en SkiaSharp
            //    break;

            case LayerBlendMode.Overlay:
                return SKBlendMode.Overlay;
         //   case LayerBlendMode.Hard_Mix:
                // No hay un equivalente directo en SkiaSharp
           //     break;

            case LayerBlendMode.Difference:
                return SKBlendMode.Difference;
            case LayerBlendMode.Exclusion:
                return SKBlendMode.Exclusion;
         //   case LayerBlendMode.Subtract:
        //       return SKBlendMode.Exc;
         //   case LayerBlendMode.Divide:
         //       return SKBlendMode.Divide;

            case LayerBlendMode.Hue:
                return SKBlendMode.Hue;
            case LayerBlendMode.Saturation:
                return SKBlendMode.Saturation;
            case LayerBlendMode.Color:
                return SKBlendMode.Color;




            default:
                return SKBlendMode.SrcOver;
        }

        // En caso de que quieras manejar de manera específica los modos que no tienen equivalente,
        // podrías lanzar una excepción o manejarlo de otra manera aquí.
        throw new NotSupportedException($"El modo de fusión {mode} no tiene un equivalente en SkiaSharp.");
    }


    




    public static System.Drawing.Bitmap ToBitmap(this WriteableBitmap writeBmp)
    {
        System.Drawing.Bitmap bmp;
        using (MemoryStream outStream = new MemoryStream())
        {
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
            enc.Save(outStream);
            bmp = new System.Drawing.Bitmap(outStream);
        }
        return bmp;
    }


    //sin probar aún
    public static WriteableBitmap CreateTriangleBitmap(int width, int height)
    {
        var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
        using (var surface = SKSurface.Create(info))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var paint = new SKPaint
            {
                Color = SKColors.Blue,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var path = new SKPath();
            path.MoveTo(width / 2f, 0);
            path.LineTo(width, height);
            path.LineTo(0, height);
            path.Close();

            canvas.DrawPath(path, paint);

            // Asegúrate de llamar a Flush para asegurar que todas las operaciones de dibujo sean aplicadas
            canvas.Flush();

            // Creamos WriteableBitmap para WPF
            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);
            bitmap.Lock();

            // Obtenemos los datos del pixmap
            using (var pixmap = surface.PeekPixels())
            {
                // Utiliza la copia directa de memoria para transferir los datos de SkiaSharp a WriteableBitmap
                SKImageInfo dstInfo = new SKImageInfo(bitmap.PixelWidth, bitmap.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                pixmap.ReadPixels(dstInfo, bitmap.BackBuffer, bitmap.BackBufferStride);
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            return bitmap;
        }
    }
    public static SKBitmap ChromaKey(SKBitmap sourceBitmap, SKColor keyColor, int tolerance = 30)
    {
        var width = sourceBitmap.Width;
        var height = sourceBitmap.Height;

        // Crea un nuevo bitmap para el resultado
        var resultBitmap = new SKBitmap(width, height);

        // Recorre cada píxel en la imagen
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixelColor = sourceBitmap.GetPixel(x, y);

                // Calcula la diferencia de color
                int diff = Math.Abs(pixelColor.Red - keyColor.Red) +
                           Math.Abs(pixelColor.Green - keyColor.Green) +
                           Math.Abs(pixelColor.Blue - keyColor.Blue);

                if (diff < tolerance)
                {
                    // Hacer el píxel transparente si está dentro del rango de tolerancia del color clave
                    resultBitmap.SetPixel(x, y, SKColors.Transparent);
                }
                else
                {
                    // Copia el píxel original
                    resultBitmap.SetPixel(x, y, pixelColor);
                }
            }
        }

        return resultBitmap;
    }




    public static SKPoint ToSKPoint(this PixelPoint point)
    {
        return new SKPoint(point.X, point.Y);
    }





}





public class FloatAnimator
{
    private Timer timer;

    private Action<float> updateAction;
    private float from;
    private float to;
    private float duration;
    private Action onCompleted;
    private float elapsed;
    private float interval;
    private int fps;
    private bool updateRender;
    private bool loop;
    private bool reverse;

    public FloatAnimator(Action<float> updateAction, float from, float to, float duration, Action onCompleted = null, int fps = 24, bool updateRender = true, bool loop = false)
    {
        this.updateAction = updateAction;
        this.from = from;
        this.to = to;
        this.duration = duration;
        this.onCompleted = onCompleted;
        this.fps = fps;
        this.updateRender = updateRender;
        this.loop = loop;
        // this.interval = 1.0f / fps;
        this.interval = 1000.0f / fps; // Intervalo en milisegundos

        timer = new Timer(interval);
        timer.Elapsed += OnTick;
    }

    bool finalized = false;

    private bool _awaitingRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitingRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;

    }
    private void OnTick(object? sender, EventArgs e)
    {
        if (finalized)
            return;

        elapsed += interval / 1000.0f; // Convertir el intervalo a segundos
        float t = elapsed / duration;

        //   elapsed += interval;
        //    float t = elapsed / duration;

    
        if (t >= 1)
        {
            if (loop)
            {
                reverse = !reverse;
                elapsed = 0;
                t = 1;
                return;
            }
            else
            {
                timer.Stop();
                t = 1;
            }
           
        }
        else if (t <= 0)
        {
            t = 0;

            if(loop)
               return;
        }
     //   t = Math.Max(Math.Min(t, 0), 1);

        if (reverse)
        {
            t = 1 - t;
        }

        AppModel.Invoke(() =>
        {
            if (_awaitingRender)
                return;

            float value = EaseInOut(from, to, t);
            updateAction(value);

            if (t == 1)
            {
                finalized = true;
                onCompleted?.Invoke();
            }

            if (updateRender && !ActionHistory.IsOnAction && !Shortcuts.IsPanning)
            {
                // Replace this with your actual render update method
                Shot.UpdateCurrentRender();

            }
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        });
    }



    public void Start()
    {
        elapsed = 0;
        reverse = false;
        timer.Start();
    }

    public void Cancel(bool jumpToEnd = false)
    {
        timer.Stop();
        if (jumpToEnd)
        {
            updateAction(to);
        }
    }

    private float EaseInOut(float start, float end, float t)
    {
        t = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        return start + (end - start) * t;
    }
}

