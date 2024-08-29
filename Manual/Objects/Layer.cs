using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using FFmpeg.AutoGen;
using Manual.API;
using Manual.Core;
using Manual.Core.Graphics;
using Manual.Core.Nodes;
using Manual.Editors;
using Manual.Objects;
using Manual.Objects.UI;
using ManualToolkit.Generic;
using ManualToolkit.Windows;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using SharpCompress.Common;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using static Manual.API.ManualAPI;
using static Manual.Core.RenderizableExtension;

using PointF = System.Drawing.PointF;

namespace Manual.Objects;

/// <summary>
/// standard layer for drawing or pixel based images
/// </summary>
public class Layer : LayerBase
{
    public Layer()
    {

    }
    public Layer(byte[] image)
    {
        //   _Animation = new AnimationBehaviour(this);

       // Effects.CollectionChanged += EFfectsCollectionChanged;
        var img = image.ToWriteableBitmap();
        SetImage(img);
        ImageOriginalWr = img;
        SetAnchorPointToCenter();

        Name = Namer.SetName("Layer", layers);

    }
    public Layer(WriteableBitmap image) // register layer  MAIN
    {
     //   _Animation = new AnimationBehaviour(this);

       // Effects.CollectionChanged += EFfectsCollectionChanged;

        SetImage(image);
        ImageOriginalWr = image;
        SetAnchorPointToCenter();

        Name = Namer.SetName("Layer", layers);

    }
    public Layer(SKBitmap image) // register layer  MAIN
    {
        //   _Animation = new AnimationBehaviour(this);

        // Effects.CollectionChanged += EFfectsCollectionChanged;

        SetImage(image);
        //ImageOriginal = image;
        SetAnchorPointToCenter();

        Name = Namer.SetName("Layer", layers);

    }



    /// <summary>
    /// blank alpha layer
    /// </summary>
    /// <returns></returns>
    public static Layer New()
    {
        Layer layer = Layer.New(BlankLayer());
        layer.Position = MainCamera.Position;
        layer.AnchorPoint = MainCamera.AnchorPoint;

        return layer;
    }
    public static Layer New(string imagePath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
        var layer = Layer.New(Renderizainador.ImageFromFile(imagePath));
        layer.Name = Namer.SetName(fileNameWithoutExtension, layers);

        return layer;
    }
    /// <summary>
    /// creates a new layer based on image, and add it in to the selected shot
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static Layer New(WriteableBitmap image)
    {
        Layer layer = new(image);
        AddLayerBase(layer);
     
        return layer;
    }



}


//------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ LAYER BASE
public partial class LayerBase : CanvasObject, INamable, IAnimable, ISelectable, ICloneableBehaviour, IBloqueable
{
    public virtual void OnClone<T>(T source)
    {
        var s = source as LayerBase;
        Name = Namer.SetName(s.Name, layers);
        _Animation = s._Animation.Clone();      
    }
  
    /// <summary>
    /// only image and position, nothing more
    /// </summary>
    /// <returns></returns>
    public Layer CloneFast()
    {
        var layer = new Layer(this.Image);
        layer.CopyDimensions(this);
        return layer;
    }

    //DEPRECATED
    public LayerBase CloneOld()
    {
        return new LayerBase
        {
            Name = ShotParent != null ? Namer.SetName(Name, ShotParent.layers) : this.Name,
            _Animation = this._Animation.Clone(),
            ImageWr = this.ImageWr,
            ImageData = this.ImageData,
            Effects = this.Effects,
            ImageRenderWr = this.ImageRenderWr,
            ImageOriginalWr = this.ImageOriginalWr,
            BlendMode = this.BlendMode,
            ShotParent = this.ShotParent,
            AttachedImageGeneration = this.AttachedImageGeneration,

            //canvasObject
            Visible = this.Visible,
            Opacity = this.Opacity,
            Index = this.Index,
            Parent = this.Parent,
            Rotation = this.Rotation,
            RotationDegree = this.RotationDegree,
            RealRotation = this.RealRotation,
            AnchorPoint = this.AnchorPoint,
            Position = this.Position,
            // Width = this.Width,
            // Height = this.Height,

        };
    }


    [JsonIgnore] public virtual BitmapSource Thumbnail
    {
        get
        {
            if (Image != null)
                return Image.ToWriteableBitmap();
            else
                return null;
        }
        set
        {

        }
    }
    /// <summary>
    /// hide transparent pattern in LayerView
    /// </summary>
    [ObservableProperty] bool isCustomThumbnail = false;

    [ObservableProperty] BitmapSource icon;
    [ObservableProperty] BitmapSource specialIcon;

    [ObservableProperty] string type = "Layer";
    [JsonIgnore] public Shot ShotParent { get; set; }
    [JsonIgnore] public int LayerIndex => ShotParent.layers.IndexOf(this);
    public AnimationBehaviour _Animation { get;
        set; }

    [ObservableProperty] private string _name = "layer";
    [ObservableProperty] bool enabled = true;

    [JsonIgnore] public bool Block
    {
        get => Enabled;
        set => Enabled = value;
    }

    [ObservableProperty] [property: JsonIgnore] bool isSelected = false;

    [ObservableProperty] Color colorTag = Colors.Transparent;


    private WriteableBitmap _imageWr;
    //---------------------------------------------------------------- IMAGE
    [JsonIgnore]
    public WriteableBitmap ImageWr
    {
        get
        {
            if (Image != null)
                return Image.ToWriteableBitmap();//_imageWr;
            else
                return null;
        }
        set
        {
            if (ImageWr == null)
                ImageOriginalWr = value;

            if (_imageWr != value)
            {
                SetImage(value);
                OnPropertyChanged(nameof(ImageWr));
                OnPropertyChanged(nameof(Thumbnail));
            }
        }
    }

    [JsonIgnore] public PreviewableValue<SKBitmap> PreviewValue { get; private set; }
    void InitPreviewValue()
    {
        PreviewValue =
        new(
            get: getPreview,
            set: setPreview
        );

    }
    void setPreview(SKBitmap value)
    {
        if(_Animation.GetKeyframeStatusAt(ShotParent.Animation.CurrentFrame) is Keyframe k)
        {
            k.PreviewValue.previewValue = value;
            k.PreviewValue.EndPreview(true);
        }
        else
           Image = value;
    }
    SKBitmap getPreview()
    {
        return Image;
    }


    private SKBitmap _image;
    [JsonIgnore]
    public SKBitmap Image
    {
        get => _image;
        set
        {
            var oldValue = _image;

            if (_image != value)
            {
                SetImage(value);
                OnPropertyChanged(nameof(Image));
                if (ShotParent != null && !ShotParent.IsRenderMode) OnPropertyChanged(nameof(ImageWr));
            }

            if (oldValue == null)
                OnPropertyChanged(nameof(Thumbnail));

        }
    }


    public byte[] ImageData //serialize
    {
        get
        {
            if (Image != null)
                return Image.ToByte();
            else
                return null;
        }
        set
        {
            if (value != null)
            {
                var bitmap = value.ToSKBitmap();//.ToWriteableBitmap();
                SetImage(bitmap);
                //ImageOriginal = bitmap;
            }
        }
    }




    // ----------------- EFFECTS ----------------- \\
    public SelectionCollection<Effect> SelectedEffects { get; set; } = [];
    public ObservableCollection<Effect> Effects { get; set; } = new();
    [JsonIgnore] public WriteableBitmap ImageRenderWr { get; set; }
    [JsonIgnore] public WriteableBitmap ImageOriginalWr { get; set; }



    [JsonIgnore] public SKBitmap ImageOriginal { get; set; }
    [JsonIgnore] public SKBitmap? ImageRender { get; set; }
    [JsonIgnore] public SKBitmap? ImagePreview { get; set; }

    [JsonIgnore] public bool IsOnPreview = false;
    public void StartEdit()
    {
        ImagePreview = new SKBitmap(Image.Info);
        ImagePreview.Erase(SKColors.Transparent);


        ImageOriginal = Image;

        Image = Image.Copy();

        IsOnPreview = true;

    }

    public void EndEdit()
    {
        ImageOriginal = Image;
        IsOnPreview = false;

    }


    public void InvalidateRender()
    {
        ImageRender = null;
    }

    public void EFfectsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (Effect item in e.NewItems)
            {
                item.Target = this;
            }
        }

        UpdateEffects();
    }

    public void UpdateEffects()
    {
        //EnableEffects();
        ShotParent?.Animation.RemoveTrackBuffer(this);
        ShotParent?.UpdateRender();
    }

    internal Effect? FindEffect(string effectName)
    {
        var effect = Effects.FirstOrDefault(e => e.Name == effectName);
        return effect;
    }
    internal T? FindEffect<T>(string effectName) where T : Effect
    {
        var effect = Effects.FirstOrDefault(e => e.Name == effectName);
        return (T)effect;
    }

    public WriteableBitmap ApplyEffectsToImageWr(WriteableBitmap image)
    {
        var render = image.Clone(); // Asume que Image es un WriteableBitmap
        foreach (Effect effect in this.Effects)
        {
            if (effect.Enabled)
                render = effect.RenderWr(render);
        }
        return render;
    }

    public SKBitmap ApplyEffectsToImage(SKBitmap image)
    {
        var render = image; // Asume que Image es un WriteableBitmap
        foreach (Effect effect in this.Effects)
        {
            if (effect.Enabled)
                render = effect.Render(render);
        }
        return render;
    }



    public WriteableBitmap GetImageRenderedWr()
    {
        if(this is UILayerBase)
        {
            var r = BitmapFactory.New(2, 2);
            ImageRenderWr = r;
            return r;
        }

        // Itera sobre los efectos y aplica cada uno al ImageRender
        var render = ApplyEffectsToImageWr(this.ImageWr);
        ImageRenderWr = render;
        return render;
    }

    public SKImageFilter? GetEffects()
    {
        return GetEffects(Effects);
    }
    public SKImageFilter? GetEffects(IEnumerable<Effect> effects)
    {
        SKImageFilter? combinedFilter = null;  // Comienza con un filtro nulo

        // Iterar sobre todos los efectos habilitados
        foreach (Effect effect in effects)
        {
            if (effect.Enabled)  // Solo considera los efectos habilitados
            {
                var newFilter = effect.RenderFilter();  // Obtiene el filtro del efecto actual
                if (combinedFilter == null)
                {
                    combinedFilter = newFilter;  // Si no hay filtro combinado aún, usa el nuevo filtro
                }
                else if (newFilter != null)
                {
                    // Componer el filtro combinado actual con el nuevo filtro
                    combinedFilter = SKImageFilter.CreateCompose(newFilter, combinedFilter);
                }
            }
        }

        return combinedFilter;  // Devuelve el filtro combinado resultante
    }



    public void RemoveEffect(string name)
    {
        Effect effectToRemove = Effects.FirstOrDefault(effect => effect.Name == name);
        if (effectToRemove != null)
        {
            Effects.Remove(effectToRemove);
        }
        //   UpdateEffects();
    }
    public void AddEffect(Effect effect)
    {
        Effects.Add(effect);
        //  UpdateEffects();
    }
    public void RemoveEffect(Effect effect)
    {
        Effects.Remove(effect);
        //  UpdateEffects();
    }


    public void ApplyEffect(Effect effect)
    {
        var originals = Effects;
        Effects = [effect];
        Image = RendGPU.RenderArea(this, [this]);
        Effects = originals;

        if(effect != Effects.First())
            Output.Log("Applied effect was not first, result may not be as expected");

        if (Effects.Contains(effect))
            Effects.Remove(effect);
    }



    //------------------------------------------------------------------------- BLEND MODE
    [ObservableProperty] LayerBlendMode blendMode = LayerBlendMode.Normal;

    public void SetBlendMode(LayerBlendMode blendMode)
    {
        ActionHistory.StartAction(this, "BlendMode");
        BlendMode = blendMode;
        ActionHistory.FinalizeAction();
    }




    //------------------------------ MASK
    [ObservableProperty] bool isAlphaMask = false;
    partial void OnIsAlphaMaskChanged(bool value) => UpdateShotRender(nameof(IsAlphaMask));

    [ObservableProperty] bool isBlockTransparency = false;
    partial void OnIsBlockTransparencyChanged(bool value) => UpdateShotRender(nameof(IsBlockTransparency));


    void UpdateShotRender(string propertyName) 
    {
        if (ShotParent != null)
        {
            ShotParent.UpdateRender();

            if (ShotParent.Animation == Animation)
                ShotParent.Animation.NotifyActionStartChanging(this, propertyName);
          
        }
    }


    public LayerBase()
    {
        InitPreviewValue();

        ImageWr = BitmapFactory.New(2, 2);
       _Animation = new AnimationBehaviour(this);

        Effects.CollectionChanged += EFfectsCollectionChanged;
    }


    public virtual void SetImage(WriteableBitmap image)
    {
        if (image != null)
        {
             SetImage(image.ToSKBitmap());
        }

        if (image == null)
            image = BitmapFactory.New(2, 2);

        _imageWr = image;
        //ImageOriginal = image;

        if (ImageScale != new PixelPoint(image.PixelWidth, image.PixelHeight))
            ImageScale = image.PixelPointScale();



        //  Position = new PixelPoint(50, 25);
    }
    public virtual void SetImage(SKBitmap image)
    {
        if (image == null)
        {
            image = null;
            return;
        }

        _image = image;

        if (ImageScale != new PixelPoint(image.Width, image.Height))
            ImageScale = new PixelPoint(image.Width, image.Height);


   //    if(_isInitializedMesh)
   //         UpdateTexture();

        InvalidateRender();

    }


    public void SetBlank()
    {
        ImageOriginalWr = BlankLayer();
        SetImage(ImageOriginalWr);
    }

    public static WriteableBitmap BlankLayer()
    {
        return BlankLayer(Colors.Transparent, MainCamera.ImageWidth, MainCamera.ImageHeight);
    }
    public static WriteableBitmap BlankLayer(Color color)
    {
        return BlankLayer(color, MainCamera.ImageWidth, MainCamera.ImageHeight);
    }
    public static WriteableBitmap BlankLayer(Color color, int width, int height)
    {
        return Renderizainador.SolidColor(color, width, height);
    }
    
    
    
    public LayerBase(WriteableBitmap image, int index)
    {
        SetImage(image);
        ManualAPI.SelectedShot.layers.Insert(index, this);

    }





    public void InsertKeyframe(string propertyName)
    {
        Animation.InsertKeyframeConditioned(this, propertyName);
    }



    public static void Move(LayerBase Old, LayerBase New)
    {
        int oldIndex = layers.IndexOf(Old);//Old.Index;
        int newIndex = layers.IndexOf(New);//New.Index;
        if (oldIndex != newIndex)
        {
            Old.ShotParent.layers.Move(oldIndex, newIndex);
           // Old._Animation.TrackIndex = New._Animation.TrackIndex;
        }
    }
    public void MoveTo(LayerBase layer)
    {
        Move(this, layer);
    }

    public void MoveOnTop()
    {
        MoveTo(layers.First());
    }

    public void MoveOnTop(LayerBase layer)
    {
        int oldIndex = layers.IndexOf(this);//Old.Index;
        int newIndex = layers.IndexOf(layer);//New.Index;
        if (oldIndex > newIndex)
        {
            ShotParent.layers.Move(oldIndex, newIndex);
            this._Animation.TrackIndex = layer._Animation.TrackIndex;
        }
    }

    public void MoveUp()
    {
        int currentIndex = layers.IndexOf(this);
        if (currentIndex > 0)
        {
            layers.Move(currentIndex, currentIndex - 1);
        }

    }
    public void MoveDown()
    {
        int currentIndex = layers.IndexOf(this);
        if (currentIndex < layers.Count - 1)
        {
            layers.Move(currentIndex, currentIndex + 1);
        }

    }
    public void MoveUp(int steps)
    {
        MoveSteps(steps);
    }
    public void MoveDown(int steps)
    {
        MoveSteps(-steps);
    }
    public void MoveSteps(int steps)
    {
        int currentIndex = layers.IndexOf(this);
        int newIndex = currentIndex + steps;
        if (newIndex >= 0 && newIndex < layers.Count)
        {
            layers.Move(currentIndex, newIndex);
        }
    }


    public void MoveToSelected()
    {
        int oldIndex = this.Index;
        int newIndex = ManualAPI.SelectedLayer.Index;

        if (oldIndex < newIndex)
            newIndex--;

        if (oldIndex != newIndex && layers.Count > newIndex)
            ManualAPI.SelectedShot.layers.Move(oldIndex, newIndex);
    }


    public int GetIndex()
    {
        return ManualAPI.layers.IndexOf(this);
    }
    public bool ExistInLayers()
    {
        return ManualAPI.layers.Contains(this);
    }

    public override string ToString()
    {
        return $"{this.GetType()} ({Name})";
    }


    public void SetSolidColor(SKColor color)
    {
        //ImageWr = Renderizainador.SolidColor(color, ImageWidth, ImageHeight);
        //Image = RendGPU.SolidColor(SKColors.Transparent, ImageWidth, ImageHeight);
        Image.Erase(color);
        ShotParent.UpdateRender();
    }





    //--------------------------------------------------------------------------- BAKE KEYFRAMES
    private GeneratedImage _attachedImageGeneration;

    [JsonIgnore]
    public GeneratedImage AttachedImageGeneration
    {
        get => _attachedImageGeneration;
        set
        {
            if (_attachedImageGeneration != value)
            {
                _attachedImageGeneration = value;
                OnPropertyChanged(nameof(AttachedImageGeneration));
            }
        }
    }


    //---------------------- OLD
    [JsonIgnore] public List<Keyframe> BakeKeyframes { get; private set; } = new();

    private Keyframe currentBakingKeyframe;

    [JsonIgnore]
    public Keyframe CurrentBakingKeyframe
    {
        get => currentBakingKeyframe;
        set 
        {
            if(currentBakingKeyframe != null)
            currentBakingKeyframe.IsBaking = false;

            if(value != null)
            value.IsBaking = true;

            currentBakingKeyframe = value;
        }
    }
    public void AddBakeKeyframe(Keyframe keyframe)
    {
        if (!BakeKeyframes.Contains(keyframe))
        {
            BakeKeyframes.Add(keyframe);
            keyframe.IsBakeKeyframe = true;
        }
    }
    public void AddBakeKeyframe(IEnumerable<Keyframe> keyframes)
    {
        foreach (var keyframe in keyframes)
        {
            AddBakeKeyframe(keyframe);
        }
    }
    public void RemoveBakeKeyframe(Keyframe keyframe)
    {
        BakeKeyframes.Remove(keyframe);
        keyframe.IsBakeKeyframe = false;
    }
    public void RemoveBakeKeyframe(IEnumerable<Keyframe> keyframes)
    {
        foreach (var keyframe in keyframes)
        {
            BakeKeyframes.Remove(keyframe);
            keyframe.IsBakeKeyframe = false;
        }
    }




    //----------------------------------------- EVENTS
    public virtual void OnProjectLoaded(Project project)
    {
       
    }



}
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------- CANVAS OBJECT
public enum LayerBlendMode
{
    Normal,
    Darken,
    Multiply,
    Color_Burn,
  //  Linear_Burn,

    Add,
    Lighten,
    Screen,
    Color_Dodge,
    Linear_Dodge,
   // Lighter_Color,

    Overlay,
    Soft_Light,
    Hard_Light,
  //  Hard_Mix,

    Difference,
    Exclusion,
 //   Subtract, //no están en skia
  //  Divide,

    Hue,
    Saturation,
    Color,


}



public interface IPositionable
{
    public float PositionGlobalX { get; set; }
    public float PositionGlobalY { get; set; }
}
/// <summary>
/// the objects that can be seen in canvas
/// </summary>
public partial class CanvasObject : CanvasObject3D, IPositionable, IId, IDisposable
{
    // P2_global = P1_global + P2_local
    // P2_local = P2_global - P1_global
    //bing

    // P2_global = P1_local* + P2_local
    // P2_local = P2_global - P1_local*
    // p2local = p2global, whitout parent

    [NotifyPropertyChangedFor(nameof(AnchorPointNormalized))]
    [NotifyPropertyChangedFor(nameof(PositionGlobalX))]
    [ObservableProperty] [property: JsonIgnore] private float _anchorPointX;

    [NotifyPropertyChangedFor(nameof(AnchorPointNormalized))]
    [NotifyPropertyChangedFor(nameof(PositionGlobalY))]
    [ObservableProperty] [property: JsonIgnore] private float _anchorPointY;
    partial void OnAnchorPointXChanged(float value)
    {
        UpdateChilds();
        _anchorPointNormalizedX = value / RealWidth;
        OnPropertyChanged(nameof(AnchorPointNormalizedX));

    }
    partial void OnAnchorPointYChanged(float value)
    {
        UpdateChilds();
        _anchorPointNormalizedY = value / RealHeight;
        OnPropertyChanged(nameof(AnchorPointNormalizedY));
    }



    [ObservableProperty] private float _anchorPointNormalizedX = 0.5f;
    [ObservableProperty] private float _anchorPointNormalizedY = 0.5f;
    partial void OnAnchorPointNormalizedXChanged(float value) { AnchorPointX = (RealWidth * value).ToInt32(); }
    partial void OnAnchorPointNormalizedYChanged(float value) { AnchorPointY = (RealHeight * value).ToInt32(); }


    [JsonIgnore] public Point AnchorPointNormalized
    {
        get => new Point(AnchorPointNormalizedX, AnchorPointNormalizedY);
        set
        {
            AnchorPointNormalizedX = (float)value.X;
            AnchorPointNormalizedY = (float)value.Y;
        }
    }

    [JsonIgnore] public Point AnchorPoint
    {
        get => new Point(AnchorPointX, AnchorPointY);
        set
        {
            AnchorPointX = (float)value.X;
            AnchorPointY = (float)value.Y;
        }
    }
    //  public Point TransformOrigin => AnchorPointNormalized;


    [JsonIgnore]
    public float PositionGlobalX
    {
        get
        {
            if (Parent is null)
                return PositionX - AnchorPointX;
            else
                return PositionX - AnchorPointX + Parent.PositionGlobalX;
        }
        set
        {
            if (Parent is null)
                PositionX = value + AnchorPointX;
            else
                PositionX = value + AnchorPointX + Parent.PositionGlobalX;

        }
    }

    [JsonIgnore]
    public float PositionGlobalY
    {
        get
        {
            if (Parent is null)
                return PositionY - AnchorPointY;
            else
                return PositionY - AnchorPointY + Parent.PositionGlobalY;
        }
        set
        {
            if (Parent is null)
                PositionY = value + AnchorPointY;
            else
                PositionY = value + AnchorPointY + Parent.PositionGlobalY;
        }
    }

    //local
    [NotifyPropertyChangedFor(nameof(PositionGlobalX))]
    [ObservableProperty][property: JsonIgnore] private float _positionX = 0;
    [NotifyPropertyChangedFor(nameof(PositionGlobalY))]
    [ObservableProperty][property: JsonIgnore] private float _positionY = 0;


    //[JsonIgnore]
    //public Point PositionGlobal
    //{
    //    get => new Point(PositionGlobalX, PositionGlobalY);
    //    set
    //    {
    //        PositionGlobalX = (float)value.X;
    //        PositionGlobalY = (float)value.Y;
    //    }
    //}

    [JsonIgnore]
    public PointF PositionGlobal
    {
        get => new PointF(PositionGlobalX, PositionGlobalY);
        set
        {
            PositionGlobalX = value.X;
            PositionGlobalY = value.Y;
        }
    }

    public Point Position
    {
        get => new Point(PositionX, PositionY);
        set
        {
            PositionX = (float)value.X;
            PositionY = (float)value.Y;
        }
    }

    //[JsonIgnore]
    //public Point PositionLocal
    //{
    //    get => PixelPoint.Divide(PositionGlobal, NormalizedScale);
    //}
    [JsonIgnore]
    public PointF PositionLocal
    {
        get => PixelPoint.DividePointF(PositionGlobal, NormalizedScale);
    }


    //the normalized scale
    //[JsonIgnore]
    //public Point NormalizedScale
    //{
    //    get => new Point(NormalizedWidth, NormalizedHeight);
    //    set
    //    {
    //        Width = (float)value.X * 100;
    //        Height = (float)value.Y * 100;
    //    }
    //}

    //the normalized scale
    [JsonIgnore]
    public PointF NormalizedScale
    {
        get => new PointF(NormalizedWidth, NormalizedHeight);
        set
        {
            Width = value.X * 100;
            Height = value.Y * 100;
        }
    }


    /// <summary>
    /// percentage from 0 to 100
    /// </summary>
    [JsonIgnore] public Point Scale
    {
        get => new Point(Width, Height);
        set
        {
            Width = (float)value.X;
            Height = (float)value.Y;
        }
    }


    [JsonIgnore] public PixelPoint ImageScale
    {
        get => new PixelPoint(ImageWidth, ImageHeight);
        set
        {
            ImageWidth = value.X;
            ImageHeight = value.Y;
        }
    }
    [JsonIgnore]
    public Point RealScale
    {
        get => new Point(RealWidth, RealHeight);
        set
        {
            //RealWidth = value.X;
            // RealHeight = value.Y;

            Width = Convert.ToSingle((value.X / ImageWidth) * 100);
            Height = Convert.ToSingle((value.Y / ImageHeight) * 100);
        }
    }



    //SCALE
    [NotifyPropertyChangedFor(nameof(PositionGlobalX))]
    [ObservableProperty] [property: JsonIgnore] private float _realWidth = 512;
    [NotifyPropertyChangedFor(nameof(PositionGlobalY))]
    [ObservableProperty] [property: JsonIgnore] private float _realHeight = 512;


    [NotifyPropertyChangedFor(nameof(PositionGlobalX))]
    [NotifyPropertyChangedFor(nameof(RealWidth))]
    [ObservableProperty]  private int _imageWidth = 512;

    [NotifyPropertyChangedFor(nameof(PositionGlobalY))]
    [NotifyPropertyChangedFor(nameof(RealHeight))]
    [ObservableProperty] private int _imageHeight = 512;



    partial void OnImageWidthChanged(int value) 
    {
        RealWidth = ImageWidth * NormalizedWidth;
        AnchorPointX = RealWidth * AnchorPointNormalizedX;
        UpdateChilds();
    }
    partial void OnImageHeightChanged(int value) 
    {
        RealHeight = ImageHeight * NormalizedHeight;
        AnchorPointY = RealHeight * AnchorPointNormalizedY; 
        UpdateChilds(); 
    }


     //PERCENTAGE
     [ObservableProperty] private float _width = 100;
     [ObservableProperty] private float _height = 100;

    [JsonIgnore] public float NormalizedWidth => Width / 100;
    [JsonIgnore] public float NormalizedHeight => Height / 100;
    partial void OnWidthChanged(float value)
    {
        RealWidth = ImageWidth * NormalizedWidth;
        AnchorPointX = RealWidth * AnchorPointNormalizedX;
        UpdateChilds();
    }
    partial void OnHeightChanged(float value)
    {
        RealHeight = ImageHeight * NormalizedHeight;
        AnchorPointY = RealHeight * AnchorPointNormalizedY;
        UpdateChilds();
    }


    [ObservableProperty] [property: JsonIgnore] private int _twists = 0;
    [ObservableProperty] [property: JsonIgnore] private float _rotation = 0;
    [ObservableProperty] private float _realRotation = 0;






    //-------------------------------------------------- PARENT CHILDREN SYSTEM
    private CanvasObject? _parent;

    [JsonIgnore]
    public CanvasObject? Parent
    {
        get => _parent;
        set
        {         
            if (_parent != value && value != this)
            {
                OnParentChanging(value);
                _parent = value;
                OnParentChanged(value);
                OnPropertyChanged(nameof(Parent));
            }
        }
    }

    [JsonIgnore] public List<CanvasObject> Childrens { get; set; } = new();
    public delegate void CanvasObjectChanged();
    public event CanvasObjectChanged OnCanvasPropertiesChanged;



    void OnParentChanging(CanvasObject? value)
    {
        if (Parent is not null)
            Parent.OnCanvasPropertiesChanged -= ParentChanged;

        if (value is not null)
        {
            value.OnCanvasPropertiesChanged += ParentChanged;
        }
        else
        {
              ModifyBasedOnParent(Parent, false);
        }
    }
     void OnParentChanged(CanvasObject? value)
    {
        ModifyBasedOnParent(value, true); 
        ParentChanged();
    }
   void ModifyBasedOnParent(CanvasObject? value, bool adding)
    {
        if (value is null)
            return;


        int factor = adding == true ? -1 : 1;

        PositionX += (float)value.RealPosition().X * factor;
        PositionY += (float)value.RealPosition().Y * factor;

    }


    Point RealPosition()// anchor point & local position
    {
        if(Parent is not null)
       return new Point(PositionX - AnchorPointX + Parent.PositionGlobalX,
                        PositionY - AnchorPointY + Parent.PositionGlobalY);
        else
            return new Point(PositionX - AnchorPointX,
                       PositionY - AnchorPointY);
    } 
    
    protected virtual void ParentChanged()
    {
        OnPropertyChanged(nameof(PositionGlobalX));
        OnPropertyChanged(nameof(PositionGlobalY));

        UpdateChilds();
    }
    public void UpdateChilds()
    {
        OnPositionchanged();
        OnCanvasPropertiesChanged?.Invoke();
    }
    public virtual void OnPositionchanged()
    {
    }

    partial void OnPositionXChanged(float value)   {  UpdateChilds();  }
    partial void OnPositionYChanged(float value) { UpdateChilds(); }





    //----------------------------------
    [JsonIgnore]
    public Degrees RotationDegree
    {
        get
        {
            return new Degrees(RealRotation);
        }
        set
        {
            Twists = value.Twists;
            Rotation = value.Degree;
            RealRotation = value.RealDegree;
        }

    }

    [NotifyPropertyChangedFor(nameof(RealOpacity))]


    /// <summary>
    /// percentage, from 0 to 100
    /// </summary>
    [ObservableProperty] private float _opacity = 100;

    /// <summary>
    /// from 0 to 1
    /// </summary>
    [JsonIgnore] public float RealOpacity => Opacity / 100;
    partial void OnOpacityChanged(float value)
    {
        OpacityChanged(value);
    }
    public virtual void OpacityChanged(float value)
    {
        //do something
    }

    public CanvasObject()
    {
        SetAnchorPointToCenter();
    }
    public void SetAnchorPointToCenter()
    {
        AnchorPointNormalized = new Point(0.5, 0.5);
        OnAnchorPointNormalizedXChanged(0.5f);
        OnAnchorPointNormalizedYChanged(0.5f);
        AnchorPointNormalized = new Point(0.5, 0.5);
    }
    ~CanvasObject()
    {
        if (Parent != null)
        {
            Parent.OnCanvasPropertiesChanged -= ParentChanged;
        }
    }



    /// <summary>
    /// Z axis for 3D maybe
    /// </summary>
    [ObservableProperty] private int _index = -1;
    [ObservableProperty] private bool _visible = true;
    partial void OnVisibleChanged(bool value)
    {
        Shot.UpdateCurrentRender();

        if (this is LayerBase a)
            a.ShotParent?.Animation.NotifyActionStartChanging(a, nameof(Visible));
    }
    

    public (float width, float height) GetBoundingBoxScale()
    {
        float angle = RealRotation * (float)Math.PI / 180;
        float sin = (float)Math.Sin(angle);
        float cos = (float)Math.Cos(angle);
        float x1 = -ImageWidth / 2;
        float y1 = -ImageHeight / 2;
        float x2 = ImageWidth / 2;
        float y2 = -ImageHeight / 2;
        float x3 = ImageWidth / 2;
        float y3 = ImageHeight / 2;
        float x4 = -ImageWidth / 2;
        float y4 = ImageHeight / 2;
        float x1r = x1 * cos - y1 * sin;
        float y1r = x1 * sin + y1 * cos;
        float x2r = x2 * cos - y2 * sin;
        float y2r = x2 * sin + y2 * cos;
        float x3r = x3 * cos - y3 * sin;
        float y3r = x3 * sin + y3 * cos;
        float x4r = x4 * cos - y4 * sin;
        float y4r = x4 * sin + y4 * cos;
        float minX = Math.Min(Math.Min(x1r, x2r), Math.Min(x3r, x4r));
        float maxX = Math.Max(Math.Max(x1r, x2r), Math.Max(x3r, x4r));
        float minY = Math.Min(Math.Min(y1r, y2r), Math.Min(y3r, y4r));
        float maxY = Math.Max(Math.Max(y1r, y2r), Math.Max(y3r, y4r));
        return (maxX - minX, maxY - minY);
    }
    public CanvasObject GetBoundingBox()
    {
        float angle = RealRotation * (float)Math.PI / 180;
        float sin = (float)Math.Sin(angle);
        float cos = (float)Math.Cos(angle);
        float x1 = -ImageWidth / 2;
        float y1 = -ImageHeight / 2;
        float x2 = ImageWidth / 2;
        float y2 = -ImageHeight / 2;
        float x3 = ImageWidth / 2;
        float y3 = ImageHeight / 2;
        float x4 = -ImageWidth / 2;
        float y4 = ImageHeight / 2;
        float x1r = x1 * cos - y1 * sin;
        float y1r = x1 * sin + y1 * cos;
        float x2r = x2 * cos - y2 * sin;
        float y2r = x2 * sin + y2 * cos;
        float x3r = x3 * cos - y3 * sin;
        float y3r = x3 * sin + y3 * cos;
        float x4r = x4 * cos - y4 * sin;
        float y4r = x4 * sin + y4 * cos;
        float minX = Math.Min(Math.Min(x1r, x2r), Math.Min(x3r, x4r));
        float maxX = Math.Max(Math.Max(x1r, x2r), Math.Max(x3r, x4r));
        float minY = Math.Min(Math.Min(y1r, y2r), Math.Min(y3r, y4r));
        float maxY = Math.Max(Math.Max(y1r, y2r), Math.Max(y3r, y4r));


        var result = new CanvasObject();
        result.ImageWidth = (maxX - minX).ToInt32();
        result.ImageHeight = (maxY - minY).ToInt32();

        result.SetAnchorPointToCenter();
        result.PositionX = this.PositionX;
        result.PositionY = this.PositionY;


        return result;
    }

    public virtual void Dispose()
    {
        
    }





    #region ----------------------------------------------------------------------- SERIALIZE
    public Guid Id { get; set; } = Guid.NewGuid();

    #endregion -----------------------------------------------------------------------
}


//--------------------------------------------------------------------------------------------------------------------------------------------------------------------- LAYERBASE TYPES

public class Folder : LayerBase
{
    public ObservableCollection<LayerBase> Items { get; private set; } = new();
    public Folder()
    {
        Name = Namer.SetName("Folder", layers);
    }

    public void Add(LayerBase layer)
    {
        layer.Parent = this;
        Items.Add(layer);
    }


}

public class OnionSkin : Folder
{
    public OnionSkin()
    {
        Name = "Onion Skin";
        _Animation = new AnimationBehaviour(this, SetFrame);
        Opacity = 16;
        Type = "GhostLayer";

        previous.Name = "OnionSkin Previous";
        next.Name = "OnionSkin Next";

        //  previous.AddEffect(new E_Colorize() {ColorValue = Colors.Blue} );
        // next.AddEffect(new E_Colorize() {ColorValue = Colors.Green} );

        previous.AddEffect(new E_HueSaturationLights() { Hue = 30 }); //blue
        next.AddEffect(new E_HueSaturationLights() { Hue = -30 }); //green
    }

    public void Enable()
    {
        Enabled = true;

        AddLayerBase(this, false, false);
        AddLayerBase(next, false, false);
        AddLayerBase(previous, false, false);

        _Animation.GlobalFrameStart = SelectedLayer._Animation.GlobalFrameStart;
        next._Animation.GlobalFrameStart = SelectedLayer._Animation.GlobalFrameStart;
        previous._Animation.GlobalFrameStart = SelectedLayer._Animation.GlobalFrameStart;

    }
    public void Disable()
    {
        Enabled = false;

        layers.Remove(this);

        layers.Remove(next);
        layers.Remove(previous);

        Shot.UpdateCurrentRender();
    }

    GhostLayer next = new();
    GhostLayer previous = new();
    void SetFrame(int frame)
    {
        if (Enabled)
        {
            var currentKey = SelectedLayer._Animation.GetKeyframeStatusAt(CurrentFrame);

            if (currentKey != null)
            {
                var previousKey = Animation.GetKeyframePrevious(currentKey);
                SetOnionLayer(previous, previousKey);

                var nextKey = Animation.GetKeyframeNext(currentKey);
                SetOnionLayer(next, nextKey);
            }

        }

    }
    void SetOnionLayer(GhostLayer onionLayer, Keyframe? key)
    {
        if (key != null && key.IsValueType(typeof(SKBitmap)))
        {
            onionLayer.Image = (SKBitmap)key?.Value;
            onionLayer.Position = SelectedLayer._Animation.GetPositionAt(key.Frame);
        }
    }



    public override void OpacityChanged(float value)
    {
        previous.Opacity = value;
        next.Opacity = value;
    }

}


/// <summary>
/// This layer is controlled by the code behind, not by the user.
/// So it can be added or removed without notice.
/// </summary>
public class GhostLayer : LayerBase
{
    public GhostLayer()
    {
        SetBlank();
        ColorTag = "#78787d".ToColor();
        Name = Namer.SetName("Ghost Layer", layers);
        Type = "GhostLayer";
    }


}


public class ImageSequence : LayerBase
{
    public ObservableCollection<string> ImagePaths = new();

    public ImageSequence()
    {
         
    }
    public ImageSequence(IEnumerable<WriteableBitmap> images)
    {
        Name = Namer.SetName("Image Sequence", layers);
        ImageWr = images.First();

        int frame = 0;
        foreach (WriteableBitmap img in images)
        {
            Keyframe.Insert(this, nameof(Image), frame, img.ToSKBitmap());
            frame += _Animation.FrameSteps;
        }
    }
    public ImageSequence(IEnumerable<SKBitmap> images)
    {
        Name = Namer.SetName("Image Sequence", layers);
        Image = images.First();

        int frame = 0;
        foreach (SKBitmap img in images)
        {
            Keyframe.Insert(this, nameof(Image), frame, img);
            frame += _Animation.FrameSteps;
        }
    }


    public ImageSequence(IEnumerable<string> imagePaths)
    {
        ImagePaths = new(imagePaths);
        Name = GetImageSequenceName(ImagePaths[0]);

        Image = ManualCodec.ImageFromFile(ImagePaths[0]);
        SetSequence(coincideFrames: true);
    }
    int firstFrame = 0;
    void SetSequence(bool coincideFrames = false)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        if (coincideFrames == false)
        {
            int frame = 0;
            foreach (string imagePath in ImagePaths)
            {
                InsertFrame(imagePath, frame);
                frame += _Animation.FrameSteps;
            }
        }
        else
        {
            firstFrame = GetFirstFrame(ImagePaths);
            foreach (string imagePath in ImagePaths)
            {
                int frame = GetImageSequenceFrame(imagePath);
                frame -= firstFrame;
                InsertFrame(imagePath, frame);
            }
        }

        _Animation.FrameEnd = GetLastFrame(ImagePaths);
        Mouse.OverrideCursor = null;
    }

    void InsertFrame(string imagePath, int frame)
    {
        var img = ManualCodec.ImageFromFile(imagePath);
        Keyframe.Insert(this, nameof(Image), frame, img);
    }


    public int GetFirstFrame(IEnumerable<string> imagePaths)
    {
        int? firstFrameNumber = null;

        foreach (var path in imagePaths)
        {
            int frameNumber = GetImageSequenceFrame(path);
            if (firstFrameNumber == null || frameNumber < firstFrameNumber)
            {
                firstFrameNumber = frameNumber;
            }
        }

        return firstFrameNumber ?? 0;
    }
    public  int GetLastFrame(IEnumerable<string> imagePaths)
    {
        int? firstFrameNumber = null;

        foreach (var path in imagePaths)
        {
            int frameNumber = GetImageSequenceFrame(path);
            if (firstFrameNumber == null || frameNumber > firstFrameNumber)
            {
                firstFrameNumber = frameNumber;
            }
        }

        return firstFrameNumber ?? _Animation.FrameEnd; 
    }



    public static int GetImageSequenceFrame(string filePath)
    {
        var (_, Frame) = GetImageSequenceInfo(filePath);
        return Frame;
    }
    public static string GetImageSequenceName(string filePath)
    {
        var (BaseName, _) = GetImageSequenceInfo(filePath);
        return BaseName;
    }
    public static (string BaseName, int FrameNumber) GetImageSequenceInfo(string filePath)
    {
        var regex = new Regex(@"(?<BaseName>\D+)?(?<FrameNumber>\d+)(\.\w+)$");
        var fileName = Path.GetFileName(filePath);
        var match = regex.Match(fileName);

        if (match.Success)
        {
            var baseName = match.Groups["BaseName"].Value;
            var frameNumber = int.Parse(match.Groups["FrameNumber"].Value);

            if (baseName.EndsWith("_")) // is blender exported "ava_007.png"
                baseName = baseName.TrimEnd('_');
            
            // Si baseName está vacío, usa el nombre del directorio como baseName
            if (string.IsNullOrEmpty(baseName)) // "007.png"
                baseName = FileManager.GetFileFolderName(filePath);
            

            return (baseName, frameNumber);
        }
        else
        {
            return (null, 0); // not a sequence
        }
    }


}



public class VideoLayer : LayerBase // ._.XD
{
    public string FilePath { get; set; }
    public VideoLayer()
    {
            
    }
    public VideoLayer(string filePath)
    {
        FilePath = filePath;
        _Animation = new AnimationBehaviour.VideoBased(this);
        Name = Path.GetFileNameWithoutExtension(filePath);
    }


    public ImageSequence ToImageSequence()
    {
        List<SKBitmap> frames = new();
        using (var mediaFile = MediaFile.Open(FilePath))
        {
            var videoStream = mediaFile.Video;
            for (int i = 0; i < _Animation.FrameDuration; i++)
            {       
                var frame = ManualCodec.GetVideoFrame(FilePath, i, ShotParent);
                frames.Add(frame);
            }
           
        }

        var im = new ImageSequence(frames);     
        return im;
    }



    public void ConvertToKeyframes()
    {
        var img = this.ToImageSequence();

        bool selected = ShotParent.SelectedLayer == this;

        ManualAPI.ReplaceLayer(this, img);

        if (selected)
            img.ShotParent.SelectedLayer = img;
        
    }

}


public partial class AudioLayer : LayerBase
{
    public string? FilePath { get; set; }
    public AudioLayer()
    {
            
    }
    public AudioLayer(string filePath)
    {
        FilePath = filePath;
        _Animation = new AnimationBehaviour.AudioBased(this);
        Name = Path.GetFileNameWithoutExtension(filePath);
    }


}




public partial class ShotLayer : LayerBase
{
    public string? FilePath { get; set; }

    Shot _shotRef;
    [JsonIgnore] public Shot ShotRef 
    {
        get => _shotRef;
        set
        {
            if (_shotRef != null)
                _shotRef.Animation.OnFrameBuffering -= Animation_OnFrameBuffering;

            _shotRef = value;

            if(value != null)
                _shotRef.Animation.OnFrameBuffering += Animation_OnFrameBuffering;
        } 
    }
    public override void Dispose()
    {
        base.Dispose();

        if (_shotRef != null)
            _shotRef.Animation.OnFrameBuffering -= Animation_OnFrameBuffering;
    }
    public void Animation_OnFrameBuffering(int value)
    {
        if(!ShotParent.Animation.IsPlaying)
            ShotParent.Animation.RemoveTrackBuffer(this);
    }

    Guid? _shotRefId;
    [JsonProperty]
    Guid ShotRefId
    {
        get => ShotRef.Id;
        set => _shotRefId = value;
    }

    public ShotLayer()
    {
            
    }
    public ShotLayer(Shot shot)
    {
        ShotRef = shot;
        _Animation = new AnimationBehaviour.ShotBased(this);
        Name = shot.Name;

        _Animation.FrameStart =ShotRef.Animation.FrameStart;
        _Animation.FrameEnd = ShotRef.Animation.FrameEnd;

        _Animation.StartOffset = CurrentFrame;

    }

    
    public override void OnProjectLoaded(Project project)
    {
        SetShotRef(project);  
    }

    void SetShotRef(Project project)
    {
        ShotRef = project.ShotsCollection.FirstOrDefault(s => s.Id == _shotRefId);
        _shotRefId = null;
    }

    public override void OnClone<T>(T source)
    {
        base.OnClone(source);
        SetShotRef(project);
    }

}


public partial class TextLayer : LayerBase
{
    public override BitmapSource Thumbnail { get; set; } = AppModel.LoadIconFromToolkit("Objects/text.png");

    [ObservableProperty] string text = "Lorem Ipsum";
    [ObservableProperty] Color brushColor = Colors.Black;
    [ObservableProperty] float size = 40;
    public TextLayer()
    {
        IsCustomThumbnail = true;
        Name = "Text";
    }

}



public partial class AdjustmentLayer : LayerBase
{
   // public override BitmapSource Thumbnail { get; set; } = AppModel.LoadIconFromToolkit("Objects/adjustment-layer.png");

    public AdjustmentLayer()
    {
        Name = "Adjustment Layer";

        IsCustomThumbnail = true;
        SpecialIcon = AppModel.LoadIconFromToolkit("Objects/adjustment-layer.png");
    }
}





public partial class PreviewLayer : GhostLayer
{
    SKBitmap original;
    SKBitmap result;

    SKBitmap preview;

    GeneratedImage targetGen;

    FloatAnimator animator;

    int fpsAnim = 30;
    LayerBase lmask;

    bool isMask = false;
    public PreviewLayer(GeneratedImage genimg)
    {
        if (genimg == null)
            return;

        this.targetGen = genimg;
        this.targetGen.PropertyChanged += TargetGen_PropertyChanged;
        Name = "Preview";
        BlendMode = LayerBlendMode.Linear_Dodge;

        isMask = genimg.TargetMask != null;
        lmask = isMask ? genimg.TargetMask! : genimg.BoundingBox;

        original = lmask.Image;
     


        var dilate = genimg.MaskDilate;
        var effects = this.GetEffects([
            new E_Dilate() {Size=dilate},
            new E_Blur() {Strength=(dilate / 2)},
            new E_Colorize(){ColorValue = Colors.Black}
        ]);



        if (lmask is not BoundingBox bbox)
        { 
            // Crear el SKPaint con un SKMaskFilter para desenfoque
            SKPaint paint = new SKPaint
            {
                ImageFilter = effects,
                IsAntialias = true,
            };

            // Crear el SKBitmap para preview
            preview = new SKBitmap(original.Width, original.Height);
            using (SKCanvas canvas = new SKCanvas(preview))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(original, 0, 0, paint);
            }
        }
        else
        {
            preview = new SKBitmap(genimg.TargetLayer.Image.Width, genimg.TargetLayer.Image.Height);
            using (SKCanvas canvas = new SKCanvas(preview))
            {
                canvas.Clear(SKColors.White);
            }
        }


        //set initial status
        Opacity = 0;
        Image = preview.Copy();
        this.CopyDimensions(lmask);

        SetGradient();
    }

    private void TargetGen_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(GeneratedImage.IsError))
        {
            if(targetGen.IsError)
               End();
        }
        else if (e.PropertyName == nameof(GeneratedImage.IsGenerated))
        {
            if (targetGen.IsGenerated)
                End();
        }
    }

    public void Start()
    {
        if (!Settings.instance.EnablePreviewAnimation) 
        {
            lmask.Opacity = 0;
            return; 
        }

        Add_GhostLayer(this);
        this.MoveOnTop(lmask);

       animator = new FloatAnimator(UpdateOpacity0,
       from: 0,
       to: 20,
       duration: 0.5f,
       Opacity0_End, fps: fpsAnim, updateRender: true);
       animator.Start();
    }

    void UpdateOpacity0(float value)
    {
        this.Opacity = value;
        ApplyGradient(value / 100);
    }
    

    void Opacity0_End()
    {
        animator?.Cancel();

        animator = new FloatAnimator(UpdateOpacity,
        from: 0,
        to: 1,
        duration: 2.0f,
        null, fps: fpsAnim, updateRender: true, loop: true);

        animator.Start();

    }




    public void Preview()
    {
        if (!Settings.instance.EnablePreviewAnimation) return;

        if (isMask && GenerationManager.Instance.CurrentGeneratingImage is GeneratedImage genimg)
        {
            if (genimg.Progress == 0)
                lmask.Opacity = 100;
            else
                lmask.Opacity = (1 - genimg.Progress) * genimg.Preset.Prompt.Strength * 100;
        }
    }


    //----------------------------------------------------------------------- END PREVIEW
    public void End()
    {
        if (targetGen == null) return;

        lmask.Opacity = 0;

        if (!Settings.instance.EnableAnimations)
        {
            Remove_GhostLayer(this);
            _end_final();
        };

        animator?.Cancel();
        animator = new FloatAnimator(UpdateOpacityFinal_End,
          from: this.Opacity,
          to: 0.0f,
          duration: 0.1f,
          _end0, fps: fpsAnim, updateRender: true);
        animator.Start();


        // GRADIENT EXPANSION
        void _end0()
        {
            animator?.Cancel();
            animator = new FloatAnimator(UpdateOpacityFinal,
              from: 0,
              to: 1,
              duration: 3f,
              _end_final, fps: fpsAnim, updateRender: true);
            animator.Start();

        }
        void _end_final()
        {
            Remove_GhostLayer(this);
            Shot.UpdateCurrentRender();
        }
    }
    void UpdateOpacityFinal_2(float value)
    {
        Opacity = value;
    }

    bool finalizing_2 = false;
    void UpdateOpacityFinal(float value)
    {
        float min = 0.10f;
        float max = 0.90f;
        if (value <= min)
            Opacity = GeneralExtension.Lerp(value / min, 0, 50);
        else if (value >= max && value <= 1.0f)
            // Opacity = GeneralExtension.Lerp((value - max) / min, 50, 0);
            Opacity = GeneralExtension.Lerp((value - max) / (1.0f - max), 50, 0);
        else
            Opacity = 50;

       // Opacity = 50;
        ApplyExpansionGradient(value);

        //if (value > 0.5 && !finalizing_2)
        //{
        //    finalizing_2 = true;

        //    var an2 = new FloatAnimator(UpdateOpacityFinal_2,
        //      from: 50,
        //      to: 0,
        //      duration: 0.5f,
        //      null, fps: fpsAnim, updateRender: true);

        //    animator.Start();
        //}


    }

    void UpdateOpacityFinal_End(float value)
    {
        Opacity = value;
    }



    void UpdateOpacity(float value)
    {
        ApplyGradient(value);
        this.Opacity = value.Lerp(20, 40);
    }

    public override void Dispose()
    {
        base.Dispose();
        End();
    }


    SKColor[] colors;
    float[] colorPositions;
    void SetGradient()
    {

        // Definir los colores del degradado
        if (Settings.instance.EnableColorfulPreview)
        {
            colors = new SKColor[]
            {
            SKColor.Parse("#30B2FE"), // Azul
            SKColor.Parse("#409DFF"), // Cian
            SKColor.Parse("#BE6DFC"), // Magenta
            SKColor.Parse("#E768C3"), // Amarillo
            SKColor.Parse("#FB7267"), // Rojo
            SKColor.Parse("#FD9926"), // Naranja
            SKColor.Parse("#30B2FE"), // Azul
            };
            var offset = colors.Count() - 1;
            // Definir las posiciones del degradado
            colorPositions = new float[]
            {
            0.0f,
            1.0f / offset, //0.6
            2.0f / offset,
            3.0f / offset,
            4.0f / offset,
            5.0f / offset,
            1.0f
            };
        }

        else
        {
            colors = new SKColor[]
            {
                 SKColor.Parse("#BE6DFC"), // Magenta
            };

            colorPositions = new float[]
            {
                  0.0f
            };
        }


    }

    void ApplyGradient(float offset)
    {
        // Calcular los puntos de inicio y fin del degradado con el desplazamiento
        float width = preview.Width;
        float startX = offset * width;
        float endX = startX + width;

        // Crear el shader de degradado lineal
        SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(startX, 0),       // Punto de inicio del degradado
            new SKPoint(endX, 0),         // Punto de fin del degradado
            colors,                       // Colores del degradado
            colorPositions,               // Posiciones de los colores
            SKShaderTileMode.Repeat       // Modo de tiling para que se repita
        );

        float realOpacity = 0.75f;
        byte opacity = (byte)(realOpacity * 255);
        // Crear el SKPaint con el shader
        SKPaint paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(opacity),
            IsAntialias = true,
            Shader = shader,
            BlendMode = SKBlendMode.SrcATop,
            
        };

        // Dibujar el degradado en el bitmap preview
        using (SKCanvas canvas = new SKCanvas(Image))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(preview, 0, 0);
            canvas.DrawRect(new SKRect(0, 0, Image.Width, Image.Height), paint);
        }
    }






    void ApplyExpansionGradient(float offset)
    {
        //  EXPANSION GRADIENT

        // Calcular el centro y el radio del gradiente radial
        float width = preview.Width;
        float height = preview.Height;
        float centerX = width / 2;
        float centerY = height / 2;
        float radius = Math.Max(width, height) * (offset * 4);

        // Definir los colores del gradiente
        var radialCol = SKColor.Parse("#BE6DFC"); // Magenta
        SKColor[] radialColors = new SKColor[]
        {
        radialCol.WithAlpha(0),             // Centro transparente
        radialCol,      // Azul
        SKColors.White,                  // Blanco puro
        radialCol,       // Azul
        radialCol.WithAlpha(0),             // Borde transparente
        };

        // Definir las posiciones del gradiente
        float[] radialColorPositions = new float[]
        {
        0.0f,   //transparent
        0.3f,   //blue
        0.6f,   //white
        0.8f,   //blue
        0.9f    //transparent
        };

        // Crear el shader de gradiente radial
        SKShader shader = SKShader.CreateRadialGradient(
            new SKPoint(centerX, centerY), // Centro del gradiente
            radius,                        // Radio del gradiente
            radialColors,                  // Colores del gradiente
            radialColorPositions,          // Posiciones de los colores
            SKShaderTileMode.Clamp         // Modo de tiling para que se mantenga en el borde
        );

        float realOpacity = 0.75f;
        byte opacity = (byte)(realOpacity * 255);

        // Crear el SKPaint con el shader
        SKPaint paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(opacity),
           // IsAntialias = true,
            Shader = shader,
            BlendMode = SKBlendMode.SrcIn,
        };



        //COLORFUL GRADIENT

        //float startX = offset * width;
        //float endX = startX + width;

        //// Crear el shader de degradado lineal
        //SKShader shader2 = SKShader.CreateLinearGradient(
        //    new SKPoint(startX, 0),       // Punto de inicio del degradado
        //    new SKPoint(endX, 0),         // Punto de fin del degradado
        //    colors,                       // Colores del degradado
        //    colorPositions,               // Posiciones de los colores
        //    SKShaderTileMode.Repeat       // Modo de tiling para que se repita
        //);
        //SKPaint paint2 = new SKPaint
        //{
        //    Color = SKColors.White,
        //   // IsAntialias = true,
        //    Shader = shader2,
        //    BlendMode = SKBlendMode.SrcATop,
        //};
        // Dibujar el gradiente en el bitmap preview
        using (SKCanvas canvas = new SKCanvas(Image))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(preview, 0, 0); //mask

            canvas.DrawRect(new SKRect(0, 0, Image.Width, Image.Height), paint); //expansion

        }
    }



}


