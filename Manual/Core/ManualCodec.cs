using Manual.API;
using Manual.Objects;
using Manual.Objects.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Media.Imaging.WriteableBitmapExtensions;
using static Manual.Core.Renderizainador;
using static Manual.Core.RenderizableExtension;
using static Manual.API.ManualAPI;

using FFMediaToolkit;
using FFMediaToolkit.Encoding;
using FFMediaToolkit.Graphics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.Editors.Displays;
using static Manual.Core.RenderMethod;
using FFmpeg.AutoGen;
using System.Diagnostics;
using System.ComponentModel;
using FFMediaToolkit.Decoding;
using System.Windows.Controls;
using System.Xml.Linq;
using FFMediaToolkit.Audio;
using System.Media;
using System.Windows.Input;
using System.Drawing;
using Newtonsoft.Json;
using SharpCompress.Common;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Manual.Core.Nodes.ComfyUI;
using static Manual.Editors.TimelineView;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Manual.Core.Nodes;

namespace Manual.Core;

public static partial class ManualCodec     //------------------------------------------------------------------- DECODE, IMPORT
{
    public static bool IsFormat(this string filePath, string extension)
    {
        string realExtension = Path.GetExtension(filePath);

        if (!extension.StartsWith("."))
            extension = "." + extension;

        return string.Equals(realExtension, extension, StringComparison.OrdinalIgnoreCase);
    }


    // -----------------------IMPORTS
    public static void ImportMp4(string filePath)
    {
        AddLayerBase(new VideoLayer(filePath)); 
    }

    public static PixelFormat ToMediaPixelFormat(this ImagePixelFormat imagePixelFormat)
    {
        switch (imagePixelFormat)
        {
            case ImagePixelFormat.Bgr24:
                return PixelFormats.Bgr24;
            case ImagePixelFormat.Bgra32:
                return PixelFormats.Bgra32;
            case ImagePixelFormat.Rgb24:
                return PixelFormats.Rgb24;
            case ImagePixelFormat.Rgba32:
                return PixelFormats.Rgba64; // this is wrong but lol
            case ImagePixelFormat.Argb32:
                return PixelFormats.Pbgra32;
            case ImagePixelFormat.Uyvy422:
                return PixelFormats.Default;
            case ImagePixelFormat.Yuv444:
                return PixelFormats.Default;
            case ImagePixelFormat.Yuv422:
                return PixelFormats.Default;
            case ImagePixelFormat.Yuv420:
                return PixelFormats.Default;
            case ImagePixelFormat.Gray16:
                return PixelFormats.Gray16;
            case ImagePixelFormat.Gray8:
                return PixelFormats.Gray8;
            default:
                throw new ArgumentOutOfRangeException(nameof(imagePixelFormat), "Formato de píxel no válido.");
        }
    }

    // ImageData -> BitmapSource (unsafe)
    public static BitmapSource ToBitmapSafe(this ImageData bitmapData)
    {
        var bitmapSource = BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96,  PixelFormats.Bgr24, null, bitmapData.Data.ToArray(), bitmapData.Stride);
        return new FormatConvertedBitmap(bitmapSource, PixelFormats.Pbgra32, null, 0);
    }
    public static unsafe BitmapSource ToBitmapGeneral(this ImageData bitmapData) // con PixelFormats.Bgr32 no funcionaba
    {
        fixed (byte* ptr = bitmapData.Data)
        {
            var bitmapSource = BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96, bitmapData.PixelFormat.ToMediaPixelFormat(), null, new IntPtr(ptr), bitmapData.Data.Length, bitmapData.Stride);
            return new FormatConvertedBitmap(bitmapSource, PixelFormats.Pbgra32, null, 0);
        }
    }

    public static unsafe BitmapSource ToBitmap(this ImageData bitmapData) // con PixelFormats.Bgr32 no funcionaba
    {
        fixed (byte* ptr = bitmapData.Data)
        {
            var bitmapSource = BitmapSource.Create(bitmapData.ImageSize.Width, bitmapData.ImageSize.Height, 96, 96, PixelFormats.Bgr24, null, new IntPtr(ptr), bitmapData.Data.Length, bitmapData.Stride);
            return new FormatConvertedBitmap(bitmapSource, PixelFormats.Pbgra32, null, 0);
        }
    }



    public static SKBitmap? GetVideoFrame(string filePath, int frame, Shot shot) //WORKING PERRAS
    {
        if (shot == null) return null;

        using var mediaFile = MediaFile.Open(filePath);

        var videoStream = mediaFile.Video;
        var frameTime = shot.Animation.GetTimeAt(frame);
      
        if (!videoStream.TryGetFrame(frameTime, out ImageData videoFrame))
            return null;

        return videoFrame.ToSKBitmap();
    }
    public static List<SKBitmap>? GetVideoFrames(string filePath, int frameStart, int frameEnd, Shot shot)
    {
        if (shot == null) return null;

        using var mediaFile = MediaFile.Open(filePath);

        var videoStream = mediaFile.Video;


        // Use a thread-safe collection for parallel processing
        var frames = new ConcurrentBag<SKBitmap>();

        Parallel.For(frameStart, frameEnd, i =>
        {
            var frameTime = shot.Animation.GetTimeAt(i);
            if (videoStream.TryGetFrame(frameTime, out ImageData videoFrame))
            {
                var v = videoFrame.ToSKBitmap();
                frames.Add(v);
            }
        });

        // Convert ConcurrentBag to List
        return frames.ToList();
    }
    public static SKBitmap ToSKBitmap(this ImageData videoFrame)
    {
        var result = videoFrame.ToBitmap();
        return result.ToSKBitmap();
    }



    public static void GetAudio(string filePath, int frame)
    {
        var mediaFile = MediaFile.Open(filePath);
        var audioStream = mediaFile.Audio;
        var frameTime = Animation.GetTimeAt(frame);
        
        if (!audioStream.TryGetFrame(frameTime, out AudioData audioFrame))
            return;



        SoundPlayer player = new SoundPlayer(filePath);
        player.Load();
        player.Play();

        // ???

    }


    public static void ImportImageSequence()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Multiselect = true;
        openFileDialog.Filter = "Imágenes PNG (*.png)|*.png|Imágenes GIF (*.gif)|*.gif|Todos los archivos de imagen (*.png;*.gif)|*.png;*.gif";


        if (openFileDialog.ShowDialog() == true)
        {
            List<string> imagePaths = new List<string>();

            foreach (string filePath in openFileDialog.FileNames)
            {
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    imagePaths.Add(filePath);
                }
                else if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    List<SKBitmap> gifFrames = ImportGif(filePath);
                    var imageSequence2 = new ImageSequence(gifFrames);
                    ManualAPI.AddLayerBase(imageSequence2);
                    return; // Salir del método después de importar el GIF
                }
            }

            if (imagePaths.Count == 1)
            {
                string folderPath = Path.GetDirectoryName(imagePaths[0]);
                string[] filesInFolder = Directory.GetFiles(folderPath, "*.png");
                imagePaths = new List<string>(filesInFolder);
            }
            var imageSequence = new ImageSequence(imagePaths);
            ManualAPI.AddLayerBase(imageSequence);
        }
    }





    //---------------------------------- DECODE, IMPORT
    public static SKBitmap ImageFromFile(string filePath)
    {
        var img = SKBitmap.Decode(filePath);
        return img;
    }
    public static WriteableBitmap GetClipboardImage()
    {
        var clipboardImage = Clipboard.GetImage();
        var writeableBitmap = new WriteableBitmap(clipboardImage).SetAlpha();
        return writeableBitmap;
    }


    public static List<SKBitmap> ImportGif(string path)
    {
        List<SKBitmap> frames = [];
        BitmapDecoder decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
        for (int i = 0; i < decoder.Frames.Count; i++)
        {
            BitmapFrame frame = decoder.Frames[i];
            var f = frame.ToSKBitmap();
            frames.Add(f);
        }
        return frames;
    }




}











public static partial class ManualCodec // ------------------------------------------------------------------------------------------------------- ENCODE, EXPORT, RENDER
{

    //UNUSED: but useful
    public static void SaveShotWithThumbnail(Shot shot, string filename, WriteableBitmap thumbnail)
    {
        // Convertir la miniatura a un arreglo de bytes (por ejemplo, en formato PNG)
        byte[] thumbnailBytes;
        using (var ms = new MemoryStream())
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));
            encoder.Save(ms);
            thumbnailBytes = ms.ToArray();
        }

        // Imagina que tienes una función para convertir tu objeto 'Shot' y la miniatura a bytes
        byte[] shotBytes = ConvertShotAndThumbnailToBytes(shot, thumbnailBytes);

        // Guarda los bytes en el archivo
        File.WriteAllBytes(filename, shotBytes);
    }

    public static byte[] ConvertShotAndThumbnailToBytes(Shot shot, byte[] thumbnailBytes)
    {
        // Primero, serializa el objeto Shot a JSON
        string json = project.SaveShotJson(shot);//JsonConvert.SerializeObject(shot);

        // Convierte el JSON a bytes
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Imagina un formato simple donde guardas la longitud del JSON, los bytes del JSON, 
        // seguido directamente por los bytes de la imagen de la miniatura.

        // Calcula la longitud total del archivo: longitud del JSON (como int) + longitud del JSON + longitud de la imagen
        int totalLength = sizeof(int) + jsonBytes.Length + thumbnailBytes.Length;
        byte[] result = new byte[totalLength];

        // Copia la longitud del JSON al inicio
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(jsonBytes.Length), 0, result, 0, sizeof(int));

        // Copia el JSON
        System.Buffer.BlockCopy(jsonBytes, 0, result, sizeof(int), jsonBytes.Length);

        // Copia la imagen de la miniatura
        System.Buffer.BlockCopy(thumbnailBytes, 0, result, sizeof(int) + jsonBytes.Length, thumbnailBytes.Length);

        return result;
    }

    static JsonSerializerSettings shotSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto // Incluye información de tipo solo donde sea necesario
        };
    }
    public static (Shot, BitmapImage) LoadShotAndThumbnail(string filename)
    {

        // Leer todo el contenido del archivo como bytes
        byte[] fileContent = File.ReadAllBytes(filename);

        // Leer la longitud del JSON desde el inicio del archivo
        int jsonLength = BitConverter.ToInt32(fileContent, 0);

        // Leer y convertir el JSON a string
        string json = System.Text.Encoding.UTF8.GetString(fileContent, sizeof(int), jsonLength);

        // Deserializar el JSON a una instancia de Shot
        var settings = shotSettings(); // Asegúrate de tener esta función definida como antes
        Shot shot = JsonConvert.DeserializeObject<Shot>(json, settings);

        // Leer los bytes de la miniatura que siguen después del JSON
        byte[] thumbnailBytes = new byte[fileContent.Length - sizeof(int) - jsonLength];
        System.Buffer.BlockCopy(fileContent, sizeof(int) + jsonLength, thumbnailBytes, 0, thumbnailBytes.Length);

        // Convertir los bytes de la miniatura a BitmapImage para su uso en WPF
        BitmapImage thumbnailImage = ConvertBytesToBitmapImage(thumbnailBytes);

        return (shot, thumbnailImage);
    }

    public static BitmapImage ConvertBytesToBitmapImage(byte[] bytes)
    {
        using (var ms = new MemoryStream(bytes))
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze(); // Importante para usar en el hilo de la UI si se trabaja con WPF
            return image;
        }
    }






    public static void SaveImage(WriteableBitmap bitmap, string filePath)
    {
        var jsonPrompt = Prompt.PromptJson(SelectedPreset.Prompt);

        // Crear metadatos y añadir el JSON como tEXt chunk
        BitmapMetadata metadata = new BitmapMetadata("png");
        metadata.SetQuery("/tEXt/{str=prompt}", jsonPrompt);

        // Crear un nuevo frame con los metadatos
        BitmapFrame frame = BitmapFrame.Create(bitmap);
        BitmapFrame newFrame = BitmapFrame.Create(frame, frame.Thumbnail, metadata, frame.ColorContexts);

        // Guardar el WriteableBitmap en el archivo PNG seleccionado con metadatos
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(newFrame);
            encoder.Save(stream);
        }

    }
    public static void SaveImageWithWorkflow(WriteableBitmap bitmap, string filePath)
    {
        var workflow = Comfy.LoadWorkflow(ManualAPI.SelectedPreset);
        var jsonWorkflow = Comfy.WorkflowJson(workflow, Formatting.None);


        // Crear metadatos y añadir el JSON como tEXt chunk
        BitmapMetadata metadata = new BitmapMetadata("png");
        metadata.SetQuery("/tEXt/{str=workflow}", jsonWorkflow);

        // Crear un nuevo frame con los metadatos
        BitmapFrame frame = BitmapFrame.Create(bitmap);
        BitmapFrame newFrame = BitmapFrame.Create(frame, frame.Thumbnail, metadata, frame.ColorContexts);

        // Guardar el WriteableBitmap en el archivo PNG seleccionado con metadatos
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(newFrame);
            encoder.Save(stream);
        }

    }

    public static void SaveImage(byte[] imageBytes, string filePath)
    {
        using (var ms = new MemoryStream(imageBytes))
        {
            using (var image = System.Drawing.Image.FromStream(ms))
            {
                image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }

 


    public static SKBitmap RenderFrame()
    {
        var result = RendGPU.RenderFrame(SelectedShot);
        return result;
       // return RenderFrame(CurrentFrame);
    }


    public static SKBitmap RenderFrame(int frame)
    {
        return RenderFrame(frame, SelectedShot);
    }
    public static SKBitmap RenderFrame(int frame, Shot shot)
    {
      //  var oldFrame = shot.Animation.CurrentFrame;
     //   shot.Animation.isPropertyChanged = false;

        shot.Animation.CurrentFrame = frame;
    
        Camera2D mainCamera = shot.MainCamera;
        MvvmHelpers.ObservableRangeCollection<LayerBase> layers = shot.LayersR;

        var options = new RenderAreaOptions()
        {
            EnableEffects = shot.EnableEffects,
        };
        var result = RendGPU.RenderArea(mainCamera, layers, options);

      //  shot.Animation.CurrentFrame = oldFrame;
     //   shot.Animation.isPropertyChanged = true;

        return result;
    }

    #region WORKER
    private static readonly BackgroundWorker renderWorker = new BackgroundWorker();
    static bool firstRender = true;


    private static void RenderWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
       if(e.Cancelled == true)
        {
            Render_Manager.IsRendering = false;
            M_MessageBox.Show("Render Canceled");
        }
    }

    public static void CancelRender()
    {
        if (Render_Manager.IsRendering)
        {
            renderWorker.CancelAsync();
        }
        
    }
    private static void RenderWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        Render_Manager.Progress = e.ProgressPercentage / 100;
    }


    #endregion



    //--------------------------------------------------------------------------------------------------------------------------- RENDER ANIMATION
    public static async void RenderAnimation()
    {
        //si preguntas dónde está lo de FFMediaToolkit: IRenderFormat es de manual, está generalizado para renderizar en diferentes formatos como gif y etc, busca el class RenderMethod mp4 abajo


        StartProgressBar();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        Render_Manager.IsRendering = true;

        RenderSettings renderSettings = SelectedRenderSettings;
        int oldCurrentFrame = CurrentFrame;

        await Task.Run(async () =>
        {

            IRenderFormat renderFormat = GetRenderFormat(renderSettings.Format);
            string path = Path.ChangeExtension(Render_Manager.OutputFullPath, renderFormat.Extension);

            int frameSteps = Animation.FrameRate / renderSettings.FrameRate;


            using (renderFormat.UsingStream(path))
            {
                for (int i = Animation.FrameStart; i <= Animation.FrameEnd; i += Animation.FrameSteps)
                {

               //     await Render_Manager.UIWindow.Dispatcher.InvokeAsync(() =>
               //     {

                    var imgFrame = RenderFrame(i);
                    float progress = (float)(i - Animation.FrameStart) / (Animation.FrameEnd - Animation.FrameStart);

    
                    renderFormat.AddFrame(imgFrame, i);

                    Application.Current.Dispatcher.Invoke(() =>
                    { 
                        SetProgressBar(progress);
                        LoadingRender(progress, imgFrame);
                    });

                }

                renderFormat.Finish();

            }
            stopwatch.Stop();


        });

        Render_Manager.IsRendering = false;

        CurrentFrame = oldCurrentFrame;
        LoadingRender(0);
        StopProgressBar();
        NotificationTaskBar();


        var finalTime = AppModel.TimeFormat(stopwatch.Elapsed);
        M_MessageBox.Show($"Render Finished in {finalTime}");

    }




    static void LoadingRender(float progress) //0 to 1
    {
        Render_Manager.Progress = progress;
    }
    static void LoadingRender(float progress, SKBitmap frame) //0 to 1
    {
        Render_Manager.PreviewFrame = frame;
        Render_Manager.Progress = progress;
    }

}

public enum RenderFormatType
{
    mp4,
    png,
    jpeg,
    gif,
    // avi,
    // mkv,
    // mp3,
    //  wav,

}
public class RenderMethod
{

    public static IRenderFormat GetRenderFormat(RenderFormatType format)
    {
        switch (format)
        {
            case RenderFormatType.mp4:
                return new mp4();
            case RenderFormatType.png:
                return new png();
            //case RenderFormatType.jpeg:
            //    return new jpeg();
            case RenderFormatType.gif:
                return new gif();
            //case RenderFormatType.avi:
            //    return new avi();
            //case RenderFormatType.mkv:
            //    return new mkv();
            //case RenderFormatType.mp3:
            //    return new mp3();
            //case RenderFormatType.wav:
            //    return new wav();
            default:
                throw new ArgumentException("Unsupported render format");
        }
    }

  


    //------------------------------------------------------------------------------------------------------------------------------------ FORMATS
    public class mp4 : IRenderFormat
    {
        VideoEncoderSettings settings = new VideoEncoderSettings(
            width: SelectedRenderSettings.ResolutionX,
            height: SelectedRenderSettings.ResolutionY,
            framerate: Animation.FrameRate,
            codec: VideoCodec.H264);

        public mp4()
        {
            // settings.EncoderPreset = EncoderPreset.Fast;
            settings.CRF = SelectedRenderSettings.CFR;
            settings.EncoderPreset = EncoderPreset.VerySlow;
        }

     
        public  string Extension { get; set; } = ".mp4";

        MediaOutput? videoFile;
        public IDisposable UsingStream(string fullPath) 
        {
            videoFile = MediaBuilder.CreateContainer(fullPath).WithVideo(settings).Create();
            return videoFile;
        }

        public void AddFrame(SKBitmap frame, int currentFrame)
        {
            // Convertir el WriteableBitmap a un ImageData
            // var imageData = ImageData.FromPointer(frame.BackBuffer, ImagePixelFormat.Bgra32, frame.PixelWidth, frame.PixelHeight);

            var imageData = ImageData.FromPointer(frame.GetPixels(), ImagePixelFormat.Bgra32, new System.Drawing.Size(frame.Width, frame.Height));

            // Escribir el frame en el archivo de video
            videoFile.Video.AddFrame(imageData);
        }

        public void Finish()
        {
           
        }
    }




    public class gif : IRenderFormat
    {
        GifBitmapEncoder encoder = new GifBitmapEncoder();
        FileStream? stream;
        public string Extension { get; set; } = ".gif";
        public void AddFrame(SKBitmap frame, int currentFrame)
        {
            encoder.Frames.Add(BitmapFrame.Create(frame.ToWriteableBitmap()));
        }

        public void Finish()
        {
            encoder.Save(stream);
        }

        public IDisposable UsingStream(string fullPath)
        {
            stream = new FileStream(fullPath, FileMode.Create);
            return stream;
        }
    }


    public class png : IRenderFormat
    {
        public string Extension { get; set; } = ".png";

        public void AddFrame(SKBitmap frame, int currentFrame)
        {
            string fileName = Path.GetFileNameWithoutExtension(Render_Manager.OutputName);
            fileName = $"{fileName}_{currentFrame}{Extension}";

            string filePath = Path.Combine(Render_Manager.OutputPath, fileName);

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame.ToWriteableBitmap()));
                encoder.Save(stream);
            }
        }

        public void Finish()
        {
            // No se requiere ninguna acción adicional para finalizar la secuencia de imágenes PNG
        }

        public IDisposable UsingStream(string fullPath)
        {
            return new EmptyDisposable(); // Una implementación simple de IDisposable que no realiza ninguna acción
        }

    }


    public class jpeg : IRenderFormat
    {
        public string Extension { get; set; } = ".jpeg";

        public void AddFrame(SKBitmap frame, int currentFrame)
        {
            string fileName = Path.GetFileNameWithoutExtension(Render_Manager.OutputName);
            fileName = $"{fileName}_{currentFrame}{Extension}";

            string filePath = Path.Combine(Render_Manager.OutputPath, fileName);

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame.ToWriteableBitmap()));
                encoder.Save(stream);
            }
        }

        public void Finish()
        {
            // No se requiere ninguna acción adicional para finalizar la secuencia de imágenes JPEG
        }

        public IDisposable UsingStream(string fullPath)
        {
            return new EmptyDisposable(); // Una implementación simple de IDisposable que no realiza ninguna acción
        }
    }












}

public interface IRenderFormat
{
    public string Extension { get; set; }
    public IDisposable UsingStream(string fullPath)
    {
        return new EmptyDisposable();
    }
    public void AddFrame(SKBitmap frame, int currentFrame);
    public void Finish();

}


public class EmptyDisposable : IDisposable
{
    public void Dispose()
    {
        // No se realiza ninguna acción en la disposición
    }
}

//------------------------------------------------------------------------------------------------------------------------------------------- RENDER SETTINGS
[Cloneable.Cloneable]
public partial class RenderSettings : ObservableObject, INamable
{
    public string Name { get; set; } = "unknown settings";

    private RenderFormatType format = RenderFormatType.mp4;
    public RenderFormatType Format
    {
        get
        {
            return format;
        } 
        set
        {
            format = value;
            Render_Manager.OutputName = Path.ChangeExtension(Render_Manager.OutputName, $".{value}" );
        }
    }

    //Video
    public VideoCodec VideoCodec { get; set; } = VideoCodec.H264;

    public int ResolutionX { get; set; }
    public int ResolutionY { get; set; }

    public int FrameRate { get; set; } = 24;

    public int ConstantRateFactor { get; set; } = 23;

    //Audio
    public bool EnableAudio { get; set; } = false;

    public AudioCodec AudioCodec { get; set; } = AudioCodec.AAC;

    public int AudioBitrate { get; set; } = 128000;

    public int AudioSampleRate { get; set; } = 44100;


    public int CutFrameStart { get; set; }
    public int CutFrameEnd { get; set; }

    public int CFR { get; set; } = 6;
    public RenderSettings()
    {
        ResolutionX = MainCamera.ImageWidth;
        ResolutionY = MainCamera.ImageHeight;
        
    }

}
//----------------------------------------------------------------------------------------------------------------------------- RENDER MANAGER
public partial class RenderManager : ObservableObject
{
    [ObservableProperty] SKBitmap previewFrame;
    [ObservableProperty] float progress = 0;

    public ObservableCollection<RenderSettings> RenderSettings { get; set; } = new();
    [ObservableProperty] RenderSettings selectedRenderSettings;
    [JsonIgnore] public Shot ShotTarget { get; set; }

    [NotifyPropertyChangedFor(nameof(OutputFullPath))]
    [ObservableProperty] string outputName;

    [NotifyPropertyChangedFor(nameof(OutputFullPath))]
    [ObservableProperty] string outputPath;
    public string OutputFullPath => Path.Combine(OutputPath, OutputName);

   [JsonIgnore] public Window UIWindow { get; internal set; }

    public RenderManager()
    {
        OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        OutputName = "RenderResult.mp4";
    }
    public RenderSettings NewSettings()
    {
        var rs = new RenderSettings();
        rs.Name = Namer.SetName(rs.Name, RenderSettings);
        RenderSettings.Add(rs);
        SelectedRenderSettings = rs;
        return rs;
    }
    public void AddSettings(RenderSettings renderSettings)
    {
        RenderSettings.Add(renderSettings);
        SelectedRenderSettings = renderSettings;
    }


  // [RelayCommand]
    public void RenderAnimation()
    {
        try
        {
             ManualCodec.RenderAnimation();
        }
        catch(Exception ex)
        {
            M_MessageBox.Show(ex.Message, "Render Exception Error");
            StopProgressBar();
        }
    }

    [RelayCommand]
    public void DuplicateSelectedSettings()
    {
        AddSettings(SelectedRenderSettings.Clone());
    }

    public void SetPath(string fullPath)
    {
        OutputPath = Path.GetDirectoryName(fullPath);
        OutputName = Path.GetFileName(fullPath);
    }


    [RelayCommand]
    public void CancelRender()
    {
        ManualCodec.CancelRender();
    }
    [ObservableProperty] bool isRendering = false;
}

