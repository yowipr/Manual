using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Reflection;
using System.Collections.Specialized;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics.Eventing.Reader;
using System.Linq.Expressions;
using System.Windows.Controls;
using Manual.Editors;
using System.Windows.Media.Media3D;
using static System.Net.WebRequestMethods;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection.Emit;
using Manual.Objects.UI;
using Manual.API;
using Newtonsoft.Json;
using static Python.Runtime.TypeSpec;
using ManualToolkit.Generic;
using System.Threading;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Manual.Editors.Displays;

using Manual.Core.Graphics;


namespace Manual.Core;

public interface ISavable
{
    public bool IsSaved { get; set; }
    public string? SavedPath { get; set; }
}


[Serializable]
public partial class Shot : ObservableObject, INamable, ISavable
{
    //DISABLED RELEASE: shot - para new
    public static ShotTemplate defaultTemplate = new(1024, 1024);


    //DEBUG
    [ObservableProperty][property: JsonIgnore] private bool _debugMode = false;
    [ObservableProperty][property: JsonIgnore] private string _testLog = "none";



    public bool SavedOnProject = false;
    [ObservableProperty] string? savedPath = null;
    [ObservableProperty] [property: JsonIgnore] bool isSaved = false;

    [ObservableProperty] byte[] thumbnail;
    public void UpdateThumbnail()
    {
        if(MainCamera != null)
        Thumbnail = Animation.FrameStart < Animation.CurrentFrame && Animation.CurrentFrame < Animation.FrameEnd
        ? ManualCodec.RenderFrame(this.Animation.CurrentFrame, this).ToWriteableBitmap().ToByte() : ManualCodec.RenderFrame(this.Animation.FrameStart, this).ToWriteableBitmap().ToByte();
    }
    

    [JsonIgnore] public string Version;



    //--------------- SHOT PROPERTIES -------------\\
    [JsonProperty] string version { get => Settings.instance.Version; set => Version = value; }

    [ObservableProperty] string _name = "shot";
    //   [ObservableProperty] int _id = 0;
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty] [property: JsonIgnore] float progress = 0;
    [ObservableProperty][property: JsonIgnore] bool isGenerating = false;

    internal void EnsureSelections()
    {
        if (MainCamera == null && cameras.Any())
            MainCamera = cameras[0];

        if (SelectedLayer == null && layers.Any())
            SelectedLayer = layers.Last();
    }

    [RelayCommand]
    [property: JsonIgnore]
    void CloseShot()
    {
        AppModel.project.CloseShot(this);
    }


    // Format

    public Shot()
    {
        Animation = new(this);

        InitializeLayers();
    }
    public Shot(bool isSquare)
    {
        IsSaved = true;
        Animation = new(this);
     
        InitializeLayers();


        if (!isSquare)
            return;

        //Camera2D mainCam = new();
        //cameras.Add(mainCam);
        //SetMainCamera(mainCam);


        //Layer layer = new Layer();
        //layer.ShotParent = this;
        //layer.Name = "Square";
        ////layer.Enabled = false;

        //int w = mainCam.ImageWidth;
        //int h = mainCam.ImageHeight;

        //layer.ImageScale = new PixelPoint(w, h);

        //// layer.ImageWr = Layer.BlankLayer(Colors.LightGray, w, h);
        //layer.Image = RendGPU.SolidColor(SKColors.LightGray, w, h);
        //layers.Add(layer);

        //canvasPos = new Point(layer.ImageScale.X / 2, layer.ImageScale.Y / 2);

        //var box = new BoundingBox();
        //box.ImageScale = layer.ImageScale;
        //layers.Insert(0, box);
        //box._Animation.TrackIndex = 1;

        //SelectedLayer = layer;


        
    }

    public static Shot New(int width, int height) => New(width, height, Colors.LightGray);
    
    public static Shot New(int width, int height, Color color) => New(width, height, color, TimeSpan.FromSeconds(2));
    
    public static Shot New(int width, int height, Color color, TimeSpan duration)
    {
        var shot = new Shot();
        shot.Animation.Duration = duration;

        Camera2D mainCam = new(width, height);
        shot.cameras.Add(mainCam);
        shot.SetMainCamera(mainCam);


        Layer layer = new Layer();
        layer.ShotParent = shot;
        layer.Name = "Square";


        int w = mainCam.ImageWidth;
        int h = mainCam.ImageHeight;

        layer.ImageScale = new PixelPoint(w, h);
        layer.Image = RendGPU.SolidColor(color.ToSKColor(), w, h);
        shot.layers.Add(layer);

        shot.canvasPos = new Point(layer.ImageScale.X / 2, layer.ImageScale.Y / 2);

        var box = new BoundingBox();
        box.ImageScale = layer.ImageScale;
        shot.layers.Insert(0, box);
        box._Animation.TrackIndex = 1;

        shot.SelectedLayer = layer;

        return shot;
    }



    public void AddSolidColorLayer()
    {
        var l = new Layer();
        l.ImageWr = Layer.BlankLayer(Colors.LightGray, MainCamera.ImageWidth, MainCamera.ImageHeight);
        ManualAPI.AddLayerBase(l);
        l.Name = Namer.SetName("Square", layers);

    }

    private void InitializeLayers()
    {
        SelectedLayerTypes = new Dictionary<string, bool>
        {
          { "Layer", true },
          { "GhostLayer", false },
        };

        layers.CollectionChanged += Layers_CollectionChanged;

        _selectedLayers = new(layers);
    }

    public ObservableCollection<TimedVariable> LinkedTimedVariables { get; set; } = new();
   
    //--------------- LAYERS -------------\\
    [JsonIgnore] public MvvmHelpers.ObservableRangeCollection<LayerBase> LayersR { get; set; } = new(); // for render and canvas

    /// <summary>
    /// layers for LayerView.xaml, separated by type, like ghostlayers off on
    /// </summary>
    [JsonIgnore] public MvvmHelpers.ObservableRangeCollection<LayerBase> LayersFromView { get; set; } = new();

    /// <summary>
    /// layers view, the other above is for render and models
    /// </summary>
    public MvvmHelpers.ObservableRangeCollection<LayerBase> layers { get; set; } = new(); //onlylayers



    SelectionCollection<LayerBase> _selectedLayers;
    [JsonIgnore] public SelectionCollection<LayerBase> SelectedLayers 
    { 
        get => _selectedLayers;
        set 
        {
            _selectedLayers.Clear();
            _selectedLayers = value;
        } 
    }



    [ObservableProperty] [property: JsonIgnore] private LayerBase _selectedLayer = null;
    partial void OnSelectedLayerChanged(LayerBase value)
    {
        //if (!changedOnce)
        //{
        //    SelectedLayer = AppModel.LoadSelectedInCollection(layers, value, nameof(value.Index)) ?? SelectedLayer;
        //    changedOnce = true;
        //}

        if(value != null)
         value.IsSelected = true;

        SelectedLayers.SelectedItem = value;

        LayerChanged?.Invoke(value);

    }
    


    //------------- Layer Filter -------------\\
    public bool EnableFilterType = true;
    private Dictionary<string, bool> selectedLayerTypes;
    [JsonIgnore] public Dictionary<string, bool> SelectedLayerTypes
    {
        get { return selectedLayerTypes; }
        set
        {
            selectedLayerTypes = value;
            FilterLayers();
            OnPropertyChanged(nameof(SelectedLayerTypes));
        }
    }




    [RelayCommand]
    [property: JsonIgnore]
    private void FilterLayers()
    {
        AppModel.Invoke(() => {
        // ---------- Layers From Render for Canvas

        var r = RenderizableExtension.Reorder(layers);
        LayersR.ReplaceRange(r);

        // ---------- Layers in LayerView

        if (EnableFilterType)
        {
            var filteredLayers = layers.Where(layer =>
            {
                foreach (var kvp in SelectedLayerTypes)
                {
                    if(kvp.Key == layer.Type)
                    {
                        return kvp.Value; // Si el tipo está seleccionado y coincide con el tipo de capa, se muestra
                    }
                }
                return true; // Si no se cumple ninguna condición, no se muestra
            });

            LayersFromView.ReplaceRange(filteredLayers);
        }
        else
        {
            var filteredLayers = layers;
            LayersFromView.ReplaceRange(filteredLayers);
        }
        });

    }

    public void ChangeLayerTypeFilter(string type, bool isVisible)
    {
        SelectedLayerTypes[type] = isVisible;
        FilterLayers();
    }


    public void SeeAllLayerTypeFilter( bool isVisible)
    {
        //foreach (var kvp in SelectedLayerTypes)
        //{
        //    ChangeLayerTypeFilter(kvp.Key, isVisible);
        //}
        EnableFilterType = !isVisible;
        FilterLayers();
    }



    private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) //---------------------------------------------------------- LAYERS COLLECTION CHANGED
    {      

        if(e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (LayerBase layer in e.NewItems)
            {
                layer.ShotParent = this;

                layer._Animation.AttachedLayer = layer;
                layer._Animation.AsignLinkedTimedVariables();



                if (Project.IsLoading) // serializing
                {

                    //TIMED VARIABLES
                    foreach (var tm in layer._Animation.TimedVariables)
                    {
                        
                        //KEYFRAMES
                        foreach (var k in tm.Keyframes)
                            k.propertyInfo = layer.GetType().GetProperty(tm.PropertyName);

                    }
                }

               // }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (LayerBase layer in e.OldItems)
            {
                if (Settings.instance.IsRender3D && layer._isInitializedMesh)
                    layer.Internal_DisposeMesh();

                var ab = LinkedTimedVariables.FirstOrDefault(tv => tv.AttachedAnimationBehaviours.Contains(layer._Animation));

                if (ab != null)
                {
                    if (ab.AttachedAnimationBehaviours.Count == 1)
                        LinkedTimedVariables.Remove(ab);

                    ab.AttachedAnimationBehaviours.Remove(layer._Animation);

                }
                layer.Dispose();

            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Move)
        {
            UpdateRender();
        }

       // UpdateLayersIndex();
        FilterLayers();
    }
    public void UpdateLayersIndex()
    {
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            layers[i].Index = i;
        }
      
    }

    public delegate void LayerEvent(LayerBase layer);
    public static event LayerEvent LayerChanged;
    public static void InvokeSelectedLayerChanged()
    {
        LayerChanged?.Invoke(ManualAPI.SelectedLayer);
    }
    public Matrix CanvasMatrix; //  TODO: quefuncione estilo parent
    // --------- initialize --------- \\
    private bool changedOnce = false;


    [RelayCommand]
    [property: JsonIgnore]
    public void AddLayer()
    {
        Layer.New();
    }
    public void AddLayer(WriteableBitmap image)
    {
       Layer.New(image);
    }
    public void AddLayer(LayerBase layer)
    {
        ManualAPI.AddLayerBase(layer);
    }




    [RelayCommand]
    [property: JsonIgnore]
    public void AddFolder()
    {
        Folder folder = new();
        ManualAPI.AddLayerBase(folder);
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void AddAdjustmentLayer()
    {
        AdjustmentLayer layer = new();
        FillLayerToCamera(layer, SKColors.White);
        ManualAPI.AddLayerBase(layer);
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void AddText()
    {
        TextLayer layer = new();
        FillLayerToCamera(layer, SKColors.Transparent);
        ManualAPI.AddLayerBase(layer);
    }




    void FillLayerToCamera(LayerBase layer, SKColor color)
    {
        layer.Image = RendGPU.SolidColor(color, MainCamera.ImageWidth, MainCamera.ImageHeight);
        layer.Position = MainCamera.Position;
        layer.AnchorPoint = MainCamera.AnchorPoint;
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void RemoveLayer()
    {
        RemoveLayer(SelectedLayer);
    }
    public void RemoveLayer(LayerBase layer, bool notifyAction = true)
    {

        if (SelectedLayer == null || !layers.Contains(layer))
            return;

        Animation.RemoveTrackBuffer(layer);

        int index = layers.IndexOf(layer);
        layers.RemoveAt(index);

        var oldS = SelectedLayer;

        if (oldS == layer)
        {
            if (layers.Count != 0)
            {
                if (index == layers.Count)
                {
                    SelectedLayer = layers[index - 1];
                }
                else if (layers[index] != null)
                {
                    SelectedLayer = layers[index];
                }
            }
            else
            {
                SelectedLayer = null;
            }
        }

        if (notifyAction)
        {
            ActionHistory.CollectionActionSelected(layers, layer, isAdd: false,
                 this, nameof(SelectedLayer), oldS, $"Remove {layer}");
        }

    }


    [RelayCommand]
    [property: JsonIgnore]
    void IsAlphaMask()
    {
        ManualAPI.SelectedLayers.ForEach(layer => layer.IsAlphaMask = !layer.IsAlphaMask);
    }


    [RelayCommand]
    [property: JsonIgnore]
    void IsBlockTransparency()
    {
        ManualAPI.SelectedLayers.ForEach(layer => layer.IsBlockTransparency = !layer.IsBlockTransparency);
    }

    [RelayCommand]
    [property: JsonIgnore]
    void IsEnabled()
    {
        ManualAPI.SelectedLayers.ForEach(layer => layer.Enabled = !layer.Enabled);
    }





    [RelayCommand]
    [property: JsonIgnore]
    void Maskering()
    {

        int index = layers.IndexOf(SelectedLayer);
        if (index != -1 && index < layers.Count - 1)
        {
            Layer layerBelow = layers[index + 1] as Layer;
            // Aquí puedes hacer lo que necesites con la capa que está por debajo
            E_AlphaMask maskE = new();
            maskE.LayerMask = SelectedLayer;
            layerBelow.AddEffect(maskE);


        }
    }

    [RelayCommand]
    [property: JsonIgnore]
    void Testing()
    {
        // SelectedLayer.CropInterest();
        //  SelectedLayer.Extend(50, 100, 150, 200);

        //  var selected = SelectedLayer;

        //   CanvasObject rect = selected.GetBoundingBox();
        //  var l = rect.ToLayer(Colors.Red);
        //  ManualAPI.AddLayerBase(l);


        //SelectedLayer.Crop(ManualAPI.FindLayer("Square"));
    }

    public static byte[] SetSolidColor(int w, int h, Color color)
    {
        byte[] pixels = new byte[w * h * 4];

        byte blue = color.B;
        byte green = color.G;
        byte red = color.R;
        byte alpha = color.A;

        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = blue;
            pixels[i + 1] = green;
            pixels[i + 2] = red;
            pixels[i + 3] = alpha;
        }

        using (var stream = new MemoryStream())
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(
                w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4)));
            encoder.Save(stream);
            return stream.ToArray();
        }
    }




    //--------------- UI OBJECTS -------------\\
    public ObservableCollection<UI_Object> uiObjects { get; set; } = new(); //TODO: T_Selector marca error al serializar
     public ObservableCollection<Camera2D> cameras { get; set; } = new();
    [ObservableProperty] [property: JsonIgnore] private Camera2D _mainCamera = null;


    [ObservableProperty] [property: JsonIgnore] private Selector _selectedArea = null;
    [ObservableProperty] [property: JsonIgnore] private Transformer _transformerArea = null; 
    [JsonIgnore] public Lasso Lasso = new();


    /// <summary>
    /// Add an ui object inside the canvas
    /// </summary>
    /// <param name="obj"></param>
    public void Add_UI_Object(UI_Object obj)
    {
        if(!uiObjects.Contains(obj))
              uiObjects.Insert(0, obj);
    }
    public void Remove_UI_Object(UI_Object obj)
    {
        if(uiObjects.Contains(obj))
          uiObjects.Remove(obj);
    }


    public void SetMainCamera(Camera2D camera)
    {
        MainCamera = camera;
        camera.ShotParent = this;
    }

    //serialize I think.
    partial void OnMainCameraChanged(Camera2D value)
    {
        MainCamera = cameras[0];
        MainCamera.ShotParent = this;
    }


    //--------------- CANVAS -------------\\
  //  public ObservableCollection<byte[]> generatedImagesData { get; set; } = new ObservableCollection<byte[]>();
    [JsonIgnore] public ObservableCollection<WriteableBitmap> generatedImages { get; set; } = new ObservableCollection<WriteableBitmap>();
    public ObservableCollection<byte[]> generatedImageData //serialize
    {
        get
        {
            return generatedImages.ToByte();
        }
        set
        {
            generatedImages = value.ToWriteableBitmap();
        }
    }




    [ObservableProperty] private int zoomHistory = 95;
    public Point canvasPos { get; set; }


    [JsonIgnore] public ActionHistory actionHistory = new();


    AnimationManager _animation;
    public AnimationManager Animation {
        get => _animation;
        set
        {
            value.AttachedShot = this;
            _animation = value;
        }
    }




    //---------------BUTTONS ON TOP CANVAS -------------\\

    [RelayCommand]
    [property: JsonIgnore]
    void OpenAssetStore()
    {
        AppModel.OpenAssetStore();
    }

    [RelayCommand]
    [property: JsonIgnore]
    void AddShot()
    {
        AppModel.project.AddShot();
    }

    [RelayCommand]
    [property: JsonIgnore]
    void OpenAddShot()
    {
        AppModel.project.OpenAddShot();
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void Undo()
    {
        ActionHistory.Undo();
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void Redo()
    {
        ActionHistory.Redo();
    }

    [RelayCommand]
    [property: JsonIgnore]
    void OpenPreferences()
    {
        AppModel.Edit_Preferences();
    }

    public override string ToString()
    {
        return $"Shot: {Name}, layers count: {layers.Count}";
    }

    [ObservableProperty] bool snap = false;
    [ObservableProperty] int snapJumps = 64;


    public float SnapToJump(float position, int jumps)
    {
        if (jumps <= 0)
        {
            throw new ArgumentException("SnapJumps debe ser mayor que cero.");
        }

        // Redondea la posición al múltiplo más cercano de jumps
        return (float)Math.Round(position / jumps) * jumps;
    }
    public double SnapToJump(double position, int jumps)
    {
        if (jumps <= 0)
        {
            throw new ArgumentException("SnapJumps debe ser mayor que cero.");
        }

        // Redondea la posición al múltiplo más cercano de jumps
        return Math.Round(position / jumps) * jumps;
    }
    public float SnapToJump(float position)
    {
        return (float)SnapToJump(position, SnapJumps);
    }
    public double SnapToJump(double position)
    {
        return SnapToJump(position, SnapJumps);
    }
    public Point SnapToJump(Point position)
    {
        double x = SnapToJump(position.X);
        double y = SnapToJump(position.Y);
        return new Point(x, y);
    }
    public PixelPoint SnapToJump(PixelPoint position)
    {
        double x = SnapToJump(position.X);
        double y = SnapToJump(position.Y);
        return new PixelPoint(x, y);
    }



    [RelayCommand]
    [property: JsonIgnore]
    void FlipCanvasHorizontal()
    {
        if(Shortcuts.CurrentCanvas != null)
        Shortcuts.CurrentCanvas.FlipHorizontal();
    }


    [ObservableProperty] bool onion = false;
    [JsonIgnore] OnionSkin onionSkin;
    partial void OnOnionChanged(bool value)
    {
        if (value)
        {
            if (onionSkin is null)
                onionSkin = new();

            onionSkin.Enable();
        }
        else
        {
            if(onionSkin is not null)
               onionSkin.Disable();
        }
    }


    //----------------------------------------------------------------------------------------------------------------------------- RENDERING

    [ObservableProperty] [property: JsonIgnore] bool isRenderMode = true;
    [ObservableProperty] bool showUIElements = true;
    [ObservableProperty] bool enableEffects = true;
    partial void OnEnableEffectsChanged(bool value)
    {
        UpdateRender();
    }

    partial void OnIsRenderModeChanged(bool value)
    {
        MainCamera.IsRenderMode = value;
        if (value == true)
        {
            Parallel.ForEach(layers, layer =>
            layer.SetImage(layer.ImageWr?.ToSKBitmap())
            );
            
            UpdateRender();
        }
        else
        {
            Parallel.ForEach(layers, layer =>
            layer.ImageWr = layer.Image?.ToWriteableBitmap()
            );
        }
    }
    [ObservableProperty] [property: JsonIgnore] WriteableBitmap render;
    partial void OnRenderChanged(WriteableBitmap value)
    {
        MainCamera.Render = value;
    }

  
    [ObservableProperty] Quality quality = Quality.Full;
    partial void OnQualityChanged(Quality value) => UpdateRender();
    public static void UpdateCurrentRender() => ManualAPI.SelectedShot.UpdateRender();

    public static event Event OnUpdateRender;

    public async void UpdateRender()
    {
        if (IsRenderMode && Application.Current.Dispatcher.CheckAccess())
        {

            if (!Settings.instance.IsRender3D)
               MainCamera?.ui_camera?.skRender?.InvalidateVisual();
            else
              Rend3D.UpdateRender();

            OnUpdateRender?.Invoke();
        }

    }





    //---------------------------------------------------------------------------------------------------- CONTEXT MENU LAYERS
    [RelayCommand]
    [property: JsonIgnore]
    void CombineLayers()
    {
        if (SelectedLayers.Any())
        {
           var oldL = SelectedLayers.ToList();
           var newLayer = CombineLayers(SelectedLayers);

           // AddLayer(newLayer);
          //  oldL.ForEach(l => RemoveLayer(l));

            oldL.Skip(1).ToList().ForEach(l => RemoveLayer(l));
        }
    }
    public LayerBase CombineLayers(IEnumerable<LayerBase> Layers)
    {
        var firstL = Layers.First();
        var img = RendGPU.RenderArea(firstL, Layers);
        firstL.Image = img;

        return firstL;
    }
    public LayerBase CombineLayersOld(IEnumerable<LayerBase> Layers)
    {
        var box = BoundingBox.GetBoundingBox(layers);
        var img = RendGPU.RenderArea(box, Layers);
        var firstL = Layers.First();

        var newLayer = new Layer(img);
        newLayer.CopyDimensions(box);
        newLayer.Name = firstL.Name;

        return newLayer;
    }


    [RelayCommand]
    [property: JsonIgnore]
    void CopyImage()
    {
        ManualClipboard.Copy(SelectedLayer.Image);
    }


    #region ----------------------------------------------------------------------- SERIALIZE

    [JsonProperty]
    int mainCameraId
    {
        get => cameras.IndexOf(MainCamera);
        set => MainCamera = cameras[value];      
    }

    [JsonProperty] Guid SelectedLayerId
    {
        get => SelectedLayer.Id;
        set => SelectedLayer = layers.FirstOrDefault(l => l.Id == value);
    }

    [JsonProperty]
    List<Guid> SelectedLayersId
    {
        get => SelectedLayers.Select(a => a.Id).ToList();
        set
        {
            foreach (var itemId in value)
            {
                var i = layers.FirstOrDefault(l => l.Id == itemId);
                if(i != null)
                    SelectedLayers.Add(i);
            }     
        }
    }
    #endregion -----------------------------------------------------------------------




    public static BitmapImage LoadThumbnail(string filename)
    {
        // Leer todo el contenido del archivo como bytes
        byte[] fileContent = System.IO.File.ReadAllBytes(filename);

        // Leer la longitud del JSON desde el inicio del archivo
        int jsonLength = BitConverter.ToInt32(fileContent, 0);

        // Calcular el inicio de los bytes de la miniatura
        int thumbnailStartIndex = sizeof(int) + jsonLength;

        // Leer los bytes de la miniatura
        byte[] thumbnailBytes = new byte[fileContent.Length - thumbnailStartIndex];
        System.Buffer.BlockCopy(fileContent, thumbnailStartIndex, thumbnailBytes, 0, thumbnailBytes.Length);

        // Convertir los bytes de la miniatura a BitmapImage
        BitmapImage thumbnailImage = ManualCodec.ConvertBytesToBitmapImage(thumbnailBytes);

        return thumbnailImage;
    }


    internal void ClearCache()
    {
        Animation.ClearCache();

        foreach (var layer in layers)
            layer._Animation.ClearCache();
    }
}





public partial class ShotBuilder : ObservableObject
{
    public static ShotBuilder instance => AppModel.project.ShotBuilder;

    [JsonIgnore] public ObservableCollection<ShotTemplate> Templates { get; set; } =
        [
            new ShotTemplate(512, 512),
            new ShotTemplate(768, 768),
            new ShotTemplate(1024, 1024),
            new ShotTemplate(1920, 1080),
       ];

    public ObservableCollection<ShotTemplate> ProjectShotTemplates { get; set; } = [];

    /// <summary>
    /// on select, clones the template to RealSelectedTemplate, be careful
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] ShotTemplate selectedTemplate;

    [ObservableProperty] ShotTemplate realSelectedTemplate;

    partial void OnSelectedTemplateChanged(ShotTemplate? oldValue, ShotTemplate newValue)
    {
        if (oldValue != null)
            oldValue.IsSelected = false;

        if (newValue != null) {
            newValue.IsSelected = true;
            RealSelectedTemplate = ManualClipboard.Clone(newValue);
        }
    }

    public ShotBuilder()
    {
            
    }
    public ShotBuilder(bool selectFirst)
    {
        if(selectFirst)
            SelectedTemplate = Templates.First();      
    }

    public void Open()
    {
        M_Window.NewShow(new Manual.Editors.Displays.W_ShotBuilderView(), "New Shot");
    }

    public void CreateSelected()
    {
        RealSelectedTemplate.Create();
    }

}

public partial class ShotTemplate : ObservableObject, ISelectable
{
    [ObservableProperty] [property: JsonIgnore] bool isSelected = false;

    [ObservableProperty] string name = "Shot";

    [ObservableProperty] int width = 512;
    [ObservableProperty] int height = 512;
    [ObservableProperty] Color backgroundColor = Colors.LightGray;

    [ObservableProperty] TimeSpan duration = TimeSpan.FromSeconds(2);

    public ShotTemplate(int width, int height)
    {
        Width = width;
        Height = height;
    }


    public void Create()
    {
        Shot shot = Shot.New(Width, Height, BackgroundColor, Duration);
        shot.Name = Namer.SetName(Name, ManualAPI.project.ShotsCollection);
        ManualAPI.project.ShotsCollection.Add(shot);

        ManualAPI.project.OpenShot(shot);
    }

    [RelayCommand]
    [property: JsonIgnore]
    void SwitchResolution()
    {
        var oldH = Height;
        Height = Width;
        Width = oldH;
    }




}