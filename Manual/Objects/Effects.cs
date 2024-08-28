using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Core;
using Manual.MUI;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using static InkPlatform.UserInterface.BoxLayout;
using static Manual.API.ManualAPI;

namespace Manual.Objects;
public partial class Effect : ObservableObject, ISelectable, IAnimable
{
    private int _shaderProgram;



    [ObservableProperty] bool isSelected;


    [JsonIgnore] public LayerBase Target;

    [ObservableProperty] string name = "Effect";
    [ObservableProperty] bool enabled = true;
    [JsonIgnore] public StackPanel Body { get; set; } = new();


    public AnimationBehaviour _Animation { get; set; }


    public Effect()
    {
        if (this.Name != "Effect")
        {
            //  ID = this.GetHashCode();
            RegisterEffect(this.Name, () => (Effect)Activator.CreateInstance(this.GetType()));
        }
    }

    [JsonIgnore]
    public static Dictionary<string, Func<Effect>> RegisteredEffects = new Dictionary<string, Func<Effect>>
    {
        { "Blur", () => new E_Blur() },
        { "Sharpen", () => new E_Sharpen() },

        { "Invert Color", () => new E_InvertColor() },
        { "Noise", () => new E_Noise() },
        //{ "Alpha Mask", () => new E_AlphaMask() },
        { "Colorize", () => new E_Colorize() },
        { "Channels", () => new E_Channels() },
        //{ "Hue/Saturation/Lights", () => new E_HueSaturationLights() },
        { "Saturation", () => new  E_Saturation() },

        { "Lineart", () => new E_Lineart() },
        { "Transparency Pattern", () => new E_TransparencyPattern() },
        { "Screentone Pattern", () => new  E_ScreentonePattern() },

        { "Outline", () => new  E_Outline() },
        { "Dilate", () => new  E_Dilate() },

        { "Outer Shadow", () => new  E_OuterShadow() },
        //{ "Inner Shadow", () => new  E_InnerShadow() },
        { "Diffuse Shadow", () => new  E_DiffuseShadow() },
     

   

    };

    public static void RegisterEffect(string name, Func<Effect> newEffect)
    {
        RegisteredEffects[name] = newEffect;
    }



    public virtual WriteableBitmap RenderWr(WriteableBitmap renderImage)
    {
        return renderImage;
    }


    public virtual SKBitmap Render(SKBitmap renderImage)
    {
        return renderImage;
    }

    public virtual SKImageFilter RenderFilter()
    {
        return null;
    }

    partial void OnEnabledChanged(bool value)
    {
        if (Target != null)
        {
            if (value)
            {
                OnEnable();
                Target.UpdateEffects();
            }
            else
            {
                OnDisable();
                Target.UpdateEffects();
            }
            Target.ShotParent.Animation.RemoveTrackBuffer(Target);
        }
    }
    public void ui(FrameworkElement element)
    {
        add(element, Body);
    }
    public virtual void OnEnable()
    {

    }
    public virtual void OnDisable()
    {

    }



    public void Apply()
    {
        Target.ApplyEffect(this);
    }
    public void Remove()
    {
        Target.RemoveEffect(this);
    }

    public static WriteableBitmap ApplyEffectsToCpu(LayerBase layerEffects, WriteableBitmap targetImage) => layerEffects.ApplyEffectsToImageWr(targetImage);
    public static SKBitmap ApplyEffectsTo(LayerBase layerEffects, SKBitmap targetImage) => layerEffects.ApplyEffectsToImage(targetImage);

    internal void MoveUp()
    {
        if (Target == null) return;

        var Effects = Target.Effects;
        int index = Effects.IndexOf(this);
        if (index > 0) // Asegurarse de que no es el primer elemento
        {
            Effects.Move(index, index - 1);
        }
    }

    internal void MoveDown()
    {
        if (Target == null) return;

        var Effects = Target.Effects;
        int index = Effects.IndexOf(this);
        if (index < Effects.Count - 1) // Asegurarse de que no es el último elemento
        {
            Effects.Move(index, index + 1);
        }
    }




    

}




public partial class E_Blur : Effect
{
    [ObservableProperty] float strength = 4;

    public E_Blur()
    {
        Name = "Blur";
        ui( new M_SliderBox("Strength", "Strength", 0, 100, 100, 1, true) );
    }


    public override SKBitmap Render(SKBitmap renderImage)
    {
        var result = RendGPU.Blur(renderImage, Strength);
        return result;
    }

    public override SKImageFilter RenderFilter()
    {
        if (Strength != 0)
            return SKImageFilter.CreateBlur(Strength, Strength, SKShaderTileMode.Repeat);
        else
            return null;
    }

}

public partial class E_Noise : Effect
{
    [ObservableProperty] float strength = 0.2f;

    public E_Noise()
    {
        Name = "Noise";


        ui(new M_SliderBox("Strength", "Strength", 0, 10, 50, 0.1, true));
    }

    public override SKImageFilter RenderFilter()
    {
        return RendGPU.Sharpen(Strength * 10);
    }


}


public partial class E_Sharpen : Effect
{
    [ObservableProperty] float strength = 0.2f;

    public E_Sharpen()
    {
        Name = "Sharpen";

        ui(new M_SliderBox("Strength", "Strength", 0, 10, 50, 0.1, true));
    }


    public override SKImageFilter RenderFilter()
    {
        var filter = RendGPU.Sharpen(Strength);
        return filter;
    }
}


public partial class E_Colorize : Effect
{
    [ObservableProperty] Color colorValue = Colors.White;

    public E_Colorize()
    {
        Name = "Colorize";

        var palette = new M_ColorPicker();
        palette.DataContext = this;
        palette.SetBinding(M_ColorPicker.SelectedColorProperty, "ColorValue");
        ui(palette);
    }

    public override SKImageFilter RenderFilter()
    {
        var color = SKColorFilter.CreateBlendMode(ColorValue.ToSKColor(), SKBlendMode.SrcIn);

      //  var col = SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(BrushColor.ToSKColor(), SKBlendMode.SrcIn));
        return SKImageFilter.CreateColorFilter(color);
    }
}

public partial class E_Channels : Effect
{
    public enum ChannelsPreset
    {
        None,
        Red,
        Green,
        Blue,
        Black,
        Saturated
    }

    [ObservableProperty]
    private float red = 1.0f;

    [ObservableProperty]
    private float green = 1.0f;

    [ObservableProperty]
    private float blue = 1.0f;

    [ObservableProperty]
    private float alpha = 1.0f;

    [ObservableProperty]
    private ChannelsPreset preset = ChannelsPreset.None;

    partial void OnPresetChanged(ChannelsPreset value)
    {
        switch (value)
        {
            case ChannelsPreset.None:
                Red = 1.0f;
                Green = 1.0f;
                Blue = 1.0f;
                break;
            case ChannelsPreset.Red:
                Red = 1.0f;
                Green = 0.0f;
                Blue = 0.0f;
                break;
            case ChannelsPreset.Green:
                Red = 0.0f;
                Green = 1.0f;
                Blue = 0.0f;
                break;
            case ChannelsPreset.Blue:
                Red = 0.0f;
                Green = 0.0f;
                Blue = 1.0f;
                break;
            case ChannelsPreset.Black:
                Red = 0.0f;
                Green = 0.0f;
                Blue = 0.0f;
                break;
            case ChannelsPreset.Saturated:
                Red = 2.0f;
                Green = 2.0f;
                Blue = 2.0f;
                break;
        }

        // Si tienes una referencia al shader program, puedes actualizar los uniforms aquí
        //UpdateShaderUniforms();
        Shot.UpdateCurrentRender();
    }
    public E_Channels()
    {
        Name = "Channels";

        var cbox = new M_ComboBoxEnum(typeof(ChannelsPreset), "Preset");
        cbox.SelectedItemChanged += Cbox_SelectedItemChanged;
        ui(cbox);

        // Crear interfaz de usuario con deslizadores para cada canal de color
        ui(new M_SliderBox("Red", 0, 1, 50, 0.01f, true));
        ui(new M_SliderBox("Green", 0, 1, 50, 0.01f, true));
        ui(new M_SliderBox("Blue", 0, 1, 50, 0.01f, true));

      //  ui(new M_SliderBox("Alpha", 0, 1, 50, 0.01f, true));
    }

    private void Cbox_SelectedItemChanged(object value)
    {
        var newValue = AppModel.ParseEnum<ChannelsPreset>(value.ToString());
        Preset = newValue;
    }

    public override SKImageFilter RenderFilter()
    {
        // Ajusta el color basado en los valores de los deslizadores para R, G, y B
        var filter = RendGPU.ColorAdjust(Red, Green, Blue);
        return filter;
    }


}


public partial class E_HueSaturationLights : Effect
{


    public override SKImageFilter RenderFilter()
    {
        // Asumiendo que RendGPU tiene un método para ajustar HSL
        var filter = RendGPU.HSVAdjust(Hue, Saturation, Lights);
        return filter;
    }


}





public partial class E_Lineart : Effect
{

    [ObservableProperty] float scale = 1;
    public E_Lineart()
    {
        Name = "Lineart";

        ui( new M_SliderBox("Scale", "Scale", 1, 100, 100, 1, true) );

    }

    public override SKImageFilter RenderFilter()
    {
        var lineart = RendGPU.Lineart(Scale);
        var invert = RendGPU.InvertColors();

   //    var compo = SKImageFilter.CreateCompose(lineart, invert);
        return lineart;
    }

}


public partial class E_TransparencyPattern : Effect
{
    [ObservableProperty] int size = 10;
    public E_TransparencyPattern()
    {
        Name = "Transparency Pattern";
        var strengthbox = new M_SliderBox("Size", "Size", 1, 100, 100, 1, true);
        add(strengthbox, Body);
    }

    public override SKImageFilter RenderFilter()
    {
        var pattern = RendGPU.CreateCheckeredPattern(Size, Target.Image.Width, Target.Image.Height);
        var image = RendGPU.ApplyPatternToImage(Target, pattern);

       var imgF = SKImageFilter.CreateImage(SKImage.FromBitmap(image));
        return SKImageFilter.CreateBlendMode(SKBlendMode.DstIn, imgF);

    }

}


public partial class E_ScreentonePattern : Effect
{
    [ObservableProperty] int size = 2;

    [ObservableProperty] int spacing = 4;

    [ObservableProperty] bool isDynamicSize = true;

    [ObservableProperty] bool isAntialias = false;
    public E_ScreentonePattern()
    {
        Name = "Screentone Pattern";
        var strengthbox = new M_SliderBox("Size", "Size", 1, 100, 100, 1, true);
        add(strengthbox, Body);

        var strengthbox2 = new M_SliderBox("Spacing", "Spacing", 4, 100, 100, 1, true);
        add(strengthbox2, Body);

        var ckb = new CheckBox() { Content = "Dynamic Size"};
        ckb.SetBinding(CheckBox.IsCheckedProperty, "IsDynamicSize");
        add(ckb, Body);

        var ckb2 = new CheckBox() { Content = "Antialias" };
        ckb2.SetBinding(CheckBox.IsCheckedProperty, "IsAntialias");
        add(ckb2, Body);


    }

    public override SKImageFilter RenderFilter()
    {       

        SKBitmap pattern;
        if (IsDynamicSize)
            pattern = RendGPU.CreateDynamicScreentonePattern(Target.Image, Size, Spacing, IsAntialias); 
        else
            pattern = RendGPU.CreateScreentonePattern(Size, Spacing, Target.Image.Width, Target.Image.Height, IsAntialias);

        var image = RendGPU.ApplyPatternToImage(Target, pattern);

        // return SKImageFilter.CreateImage(SKImage.FromBitmap(image));

        var img = SKImageFilter.CreateImage(SKImage.FromBitmap(pattern));
     
        var blendMode = SKImageFilter.CreateBlendMode(SKBlendMode.SrcIn, img);
  
        return blendMode;
    }

}




public partial class E_Outline : Effect
{
    [ObservableProperty] int size = 10;
    [ObservableProperty] Color brushColor = Colors.Black;
    public E_Outline()
    {
        Name = "Outline";
        var strengthbox = new M_SliderBox("Size", "Size", 0, 100, 100, 1, true);
        ui(strengthbox);

        var palette = new M_ColorPicker();
        palette.SetBinding(M_ColorPicker.SelectedColorProperty, "BrushColor");
        ui(palette);
    }

    public override SKImageFilter RenderFilter()
    {
        return RendGPU.CreateOutlineFilter(Size, BrushColor.ToSKColor());

    }

}



public partial class E_AlphaMask : Effect
{
    [ObservableProperty][property: JsonIgnore] LayerBase layerMask;

    [JsonIgnore] M_ImageBox imgBox = new();

    public E_AlphaMask()
    {
        Name = "Alpha Mask";
        ui(imgBox);

        SetBind(imgBox, "LayerMask");
    }


}


public partial class E_Saturation : Effect
{
    [ObservableProperty] float k1 = 1;
    [ObservableProperty] float k2 = 0;
    [ObservableProperty] float k3 = 0;
    [ObservableProperty] float k4 = 0;


    [ObservableProperty] Color brushColor = Colors.Green;
    public E_Saturation()
    {
        Name = "Saturation";

        ui(new M_SliderBox("K1", "K1", 0, 10, 50, 0.1, true) );
        //ui(new M_SliderBox("K2", "K2", 0, 10, 50, 0.1, true));
        //ui(new M_SliderBox("K3", "K3", 0, 10, 50, 0.1, true));
        //ui(new M_SliderBox("K4", "K4", 0, 10, 50, 0.1, true));



        var palette = new M_ColorPicker();
        palette.SetBinding(M_ColorPicker.SelectedColorProperty, "BrushColor");
        ui(palette);
    }

    public override SKImageFilter RenderFilter()
    {

        var img = SKImageFilter.CreateImage(SKImage.FromBitmap(Target.Image));

        // Crea el filtro aritmético
        var filter = SKImageFilter.CreateArithmetic(
            k1: K1,
            k2: K2,
            k3: K3,
            k4: K4,
            enforcePMColor: true,
            background: img,
            foreground: img          
        );

        return filter;
    }
}


public partial class E_Dilate : Effect
{
    [ObservableProperty] float size = 1;
    [ObservableProperty] Color brushColor = Colors.Green;
    public E_Dilate()
    {
        Name = "Dilate";
        var strengthbox = new M_SliderBox("Size", "Size", -50, 50, 100, 1, true);
        ui(strengthbox);
    }

    public override SKImageFilter RenderFilter()
    {
        if (Size >= 0)
        {
            var filter = SKImageFilter.CreateDilate(Size, Size);
            return filter;
        }
        else
        {
            var filter = SKImageFilter.CreateErode(-Size, -Size);
            return filter;
        }

    }



}



public partial class E_OuterShadow : Effect
{
    [ObservableProperty] float positionX = 1;
    [ObservableProperty] float positionY = 1;

    [ObservableProperty] float width = 1;
    [ObservableProperty] float height = 1;

    [ObservableProperty] Color brushColor = Colors.Black;
    public E_OuterShadow()
    {
        Name = "Outer Shadow";

        ui(new M_NumberBox("Posotion X", "PositionX", 100, 1));
        ui(new M_NumberBox("Posotion Y", "PositionY", 100, 1));

        ui(new M_SliderBox("Width", "Width", 0, 10, 50, 0.1, true));
        ui(new M_SliderBox("Height", "Height", 0, 10, 50, 0.1, true));

        ui(new M_ColorPicker("BrushColor"));
    }
    public override SKImageFilter RenderFilter()
    {
        var filter = SKImageFilter.CreateDropShadow(PositionX, PositionY, Width, Height, BrushColor.ToSKColor());
        return filter;
    }
}


public partial class E_InnerShadow : Effect
{

    [ObservableProperty] float positionX = 10;
    [ObservableProperty] float positionY = 10;

    [ObservableProperty] Color brushColor = Colors.Black;
    public E_InnerShadow()
    {
        Name = "Inner Shadow";

        ui(new M_NumberBox("Posotion X", "PositionX", 100, 1));
        ui(new M_NumberBox("Posotion Y", "PositionY", 100, 1));

    //    ui(new M_SliderBox("Opacity", "Opacity", 0, 10, 50, 0.1, true));

        ui(new M_ColorPicker("BrushColor"));
    }

    public override SKImageFilter RenderFilter()
    {
        // Crear un filtro de desplazamiento para mover la imagen
        var offsetFilter = SKImageFilter.CreateOffset(PositionX, PositionY);

        // Crear un filtro de color para cambiar el color a un gris oscuro
        var colorFilter = SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(BrushColor.ToSKColor().WithAlpha(255), SKBlendMode.SrcIn));

        // Combinar ambos filtros
        var shadowFilter = SKImageFilter.CreateCompose(colorFilter, offsetFilter);

        var b = SKImageFilter.CreateBlendMode(SKBlendMode.SrcOut, shadowFilter);


        var col = SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(BrushColor.ToSKColor(), SKBlendMode.SrcIn));


        var b2 = SKImageFilter.CreateBlendMode(SKBlendMode.SrcIn, b, col);

        return b2;  SKImageFilter.CreateBlendMode(SKBlendMode.SrcOver, b2);
    }
}





public partial class E_DiffuseShadow : Effect
{
    [ObservableProperty] float positionX = 1;
    [ObservableProperty] float positionY = 1;
    [ObservableProperty] float positionZ = 1;

    [ObservableProperty] float scale = 1;
    [ObservableProperty] float diffusion = 0.5f;

    [ObservableProperty] Color brushColor = Colors.Black;
    public E_DiffuseShadow()
    {
        Name = "Diffuse";

        ui(new M_NumberBox("Posotion X", "PositionX", 100, 1));
        ui(new M_NumberBox("Posotion Y", "PositionY", 100, 1));
        ui(new M_NumberBox("Posotion Z", "PositionZ", 100, 1));

        ui(new M_SliderBox("Scale", "Scale", 0, 10, 50, 0.5, true));
        ui(new M_SliderBox("Diffusion", "Diffusion", 0, 10, 50, 0.1, true));

        ui(new M_ColorPicker("BrushColor"));
    }
    public override SKImageFilter RenderFilter()
    {
        var filter = SKImageFilter.CreateDistantLitDiffuse(
        direction: new SKPoint3(PositionX, PositionY, PositionZ),   // Dirección de la luz
        lightColor: BrushColor.ToSKColor(),          // Color de la luz blanca
        surfaceScale: Scale,                  // Escala de la superficie
        kd: Diffusion                             // Coeficiente de difusión
         );


        var filter2 = SKImageFilter.CreateSpotLitSpecular(
      location: new SKPoint3(PositionX, PositionY, PositionZ),  // Posición de la luz
      target: new SKPoint3(200, 200, 0),      // Punto focal de la luz
      specularExponent: 20,                   // Exponente especular
      surfaceScale: Scale,
      ks: Diffusion, 
      shininess: 1,
      cutoffAngle: 45,                        // Ángulo de corte
      lightColor: BrushColor.ToSKColor()             // Color de la luz
  );

        var filter3 = SKImageFilter.CreateBlendMode(SKBlendMode.DstATop, filter2);

        return filter3;
    }
}