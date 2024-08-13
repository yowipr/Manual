using MANUAL2222.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using MANUAL2222.MUI;
using static MANUAL2222.API.MANUAL2222API;
using MANUAL2222.Core;
using System.Windows;
using MANUAL2222.Editors;
using System.Windows.Media;
using MANUAL2222.Objects.UI;
using MANUAL2222.Objects;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace Plugins;

[Export(typeof(IPlugin))]
public class ControlNetPlugin : IPlugin
{
    public void Initialize()
    {
        Tool.ByName("Text2Image").AddBodyScript(ControlNetEx.instance);
    }

}

public class ControlNetEx : ExtensibleTool
{
    public static ControlNetEx instance = new();
    [JsonIgnore] public M_StackPanel body { get; set; }
    [JsonIgnore] public M_StackPanel cnetBody { get; set; }
 
    public ControlNetEx()
    {
        
    }

    public override M_StackPanel Body()
    {
        M_StackPanel c_selectedPreset = new("SelectedPreset");

        /*  //  presetProp.steps = 1;
          //   presetProp.controlnet_input_images = new List<string>();
          //  presetProp.controlnet_processor_res = 512;
          //   presetProp.controlnet_threshold_a = 64;
          //    presetProp.controlnet_threshold_b = 64;

          //   presetProp.controlnet_input_images.Add("");


          //  dynamic moduleList = GET(url_module_list);
          //  List<string> moduleListx = moduleList.module_list as List<string>;
          //   M_ComboBox modulebox = new();
          //  modulebox.ItemsSource = moduleListx;

       //   presetProp.controlnet_model = "none";
       //   presetProp.controlnet_module = "none";*/


        M_StackPanel c_controlnet = new("Scripts[0]"); //controlnet
        add(c_controlnet, c_selectedPreset);


        Button addbtn = new();
        addbtn.Content = "Add";
        addbtn.Click += (sender, args) => { AddArg(); };
      //  add(addbtn, c_selectedPreset);

        ContentPresenter argsPresenter = new();
        argsPresenter.SetBinding(ContentPresenter.ContentProperty, "uiArgs");
       // add(argsPresenter, c_controlnet);


        M_Section controlNet_sec = new(c_controlnet, "ControlNet", true, "Enabled");
        controlNet_sec.AddRange(new UIElement[]
      {
          argsPresenter,
          addbtn,
      });

        AddArg();
        /*
        M_ComboBox modelbox = new();
        M_ComboBox modulebox = new(); 
        M_ImageBox img2img = new M_ImageBox("TargetLayer");
        M_Section controlNet_arg = new(c, "Unit 0", true, "Enabled");
        controlNet_arg.AddRange(new UIElement[]
        {
             img2img,
             modulebox,
             modelbox,

            new Separator(),

            new M_Label("Weight", true),
            new M_SliderBox("", "weight", 0, 1, 150, 0.05, true),

            new Separator(),
        }) ;
     
        GET_SetItems(modelbox, url_model_list, "model_list");
        modelbox.SetBinding(M_ComboBox.SelectedItemProperty, "model");

        GET_SetItems(modulebox, url_module_list, "module_list");
        modulebox.SetBinding(M_ComboBox.SelectedItemProperty, "module");
        */

        /*      ////-------------------------------------------------------------------------------\\

              //presetProp.urltest = "/controlnet/model_list";

              //Button btn = new Button();
              //btn.Click += Btn_Click;
              //btn.Content = "GET";

              //Button btn2 = new Button();
              //btn2.Click += Btn2_Click;
              //btn2.Content = "POST";


              //M_Expander img2img_exp = new(b, "test");
              //img2img_exp.AddRange(new UIElement[]
              //{

              //  //  new M_Label("Rezize Mode", true),
              // //   new M_SliderBox("", "rezize_mode", 0, 5, 1, 1, true),
              //   // new M_Grid("Value", "rezize_mode", 10, 1, true, 1, 30),
              //  //  new M_Grid("Rezize Mode", "steps", 10, 1, true, 1),



              // //   new M_Label("Control Weight", true),
              ////    new M_SliderBox("", "denoising_strengtmh", 0, 1, 2000, 0.01, true),
              // //   new M_TextBox("a", "", "urltest"),
              //    new Separator(),
              //    btn,
              //    btn2,
              //});

        */

        body = c_selectedPreset;
        return body;
    }

    /*  private async void Btn_Click(object sender, RoutedEventArgs e)
      {
          dynamic modellist = await GET(presetProp.urltest);
          string a = modellist.ToString();
          DEBUG_TEST(a);
      }
      private async void Btn2_Click(object sender, RoutedEventArgs e)
      {
          dynamic modellist = await POST(presetProp.urltest);
          string a = modellist.ToString();
          DEBUG_TEST(a);
      }*/

    public override void AfterBodyCreated()
    {
        PromptPreset.RegisterScript("controlnet", () => new ControlNet());
    }

    public void AddArg()
    {
        ControlNet cnet = SelectedPreset.GetScript("controlnet") as ControlNet;

        if(cnet != null)
        cnet.AddArg();
        else
        {
            Output.Log("cnet is null");
        }
    }
}

public class ControlNet : PromptPresetScript
{
    [JsonIgnore] public M_StackPanel uiArgs { get; set; } = new();

    string url_model_list = "/controlnet/model_list";
    string url_module_list = "/controlnet/module_list";

    [JsonIgnore] public List<ControlNetArgs> Args { get; set; } = new();

    public List<ControlNetArgs> args 
    {  get     {     return Args.Where(arg => arg.Enabled == true).ToList();   }   }


    public ControlNet()
    {
        Enabled = false;
        //  args.Add( new ControlNetArgs() );

    }
    public ControlNet(ControlNetArgs args)
    {
        AddArg(args);
    }


    public void AddArg()
    {
        AddArg(new ControlNetArgs());
    }

    public void AddArg(ControlNetArgs arg)
    {
        try
        {
            Args.Add(arg);
            int argIndex = Args.IndexOf(arg);

            //UI
            M_StackPanel c = new($"Args[{argIndex}]");
            add(c, uiArgs);
            arg.attachedUi = c;

            M_ComboBox modelbox = new();
            M_ComboBox modulebox = new();
            M_ImageBox img2img = new M_ImageBox("TargetLayer");
            M_Section controlNet_arg = new(c, $"Unit {argIndex}", true, "Enabled");
            controlNet_arg.AddRange(new UIElement[]
            {
             img2img,
             modulebox,
             modelbox,

            new Separator(),

            new M_Label("Weight", true),
            new M_SliderBox("", "weight", 0, 1, 150, 0.05, true),

            new Separator(),
            });
            arg.attachedSection = controlNet_arg;

            GET_SetItems(modelbox, url_model_list, "model_list");
            modelbox.SetBinding(M_ComboBox.SelectedItemProperty, "model");
        //    modelbox.Items.Insert(0, "None");
          //  modelbox.SelectedItem = modelbox.Items[0];

            GET_SetItems(modulebox, url_module_list, "module_list");
            modulebox.SetBinding(M_ComboBox.SelectedItemProperty, "module");




            var cmenu = new ContextMenu();
            var itemMenu = new MenuItem() { Header = "Delete" };

            itemMenu.Click += (sender, args) =>
            {
                FrameworkElement clickedElement = ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget as FrameworkElement;
                if (clickedElement != null)
                {
                    RemoveArg(Args.FirstOrDefault(a => a.attachedSection == clickedElement)); 
                }      
              //  Output.Log(clickedElement.ToString());
            };

            cmenu.Items.Add(itemMenu);
            controlNet_arg.ContextMenu = cmenu;
        }
        catch (Exception ex)
        {
            Output.Log(ex.Message);
        }

    }
    public void RemoveArg(ControlNetArgs arg)
    {
        if (Args.Count == 1)
            return;


        Args.Remove(arg);

        //UI
        uiArgs.Children.Remove(arg.attachedUi);
        UpdateBindings();
    }


    void UpdateBindings()
    {
        foreach ( var arg in Args)
        {
            var index = Args.IndexOf(arg);
            arg.attachedUi.SetBinding(FrameworkElement.DataContextProperty, $"Args[{index}]" );
            arg.attachedSection.Header = $"Unit {index}";
        }
    }



    //public static ControlNet ControlNetToScript()
    //{
    //    var controlNet = new ControlNet();
    //    SelectedPreset.alwayson_scripts.controlnet = controlNet;

    //    return controlNet;
    //}
}


public partial class ControlNetArgs : ObservableObject
{
    //UI
    [JsonIgnore] public M_StackPanel attachedUi { get; set; } = new();
    [JsonIgnore] public M_Section attachedSection { get; set; } = new();



    [JsonIgnore] public bool Enabled { get; set; } = false;

    public string module { get; set; } = "none";
    public string model { get; set; } = "None";

    public decimal weight { get; set; } = 1.0m;

    [JsonIgnore] public LayerBase TargetLayer { get; set; } = null;
    public byte[] input_image
    {
        get
        {
            LayerBase target;
            if (TargetLayer != null)
            {
                target = TargetLayer;
            }
            else
            {
                target = ImageGenerator.Instance.CurrentImageProcessing.targetLayer;
            }

            TimedVariable timedVariable = Animation.GetTimedVariable(target, "Image");
            if (ImageGenerator.Instance.CurrentImageProcessing.BakeKeyframes.Count == 0 || timedVariable == null)
            {
                return Renderizainador.RenderArea(SelectedShot.GenerateArea, target).ToByte();
            }
            else
            {

                int frame = ImageGenerator.Instance.CurrentBakeKeyframe.Frame;
                WriteableBitmap bitmap = (WriteableBitmap)timedVariable.GetConstantKeyframeAt(frame).Value;

                LayerBase l = new();
                l.CopyDimensions(target);
                l.Image = bitmap;
                WriteableBitmap imageBitmap = Renderizainador.RenderArea(ImageGenerator.Instance.CurrentImageProcessing.area, l);
                imageBitmap = imageBitmap.FromScale(ImageGenerator.Instance.CurrentImageProcessing.area.Scale.X, ImageGenerator.Instance.CurrentImageProcessing.area.Scale.Y);


                return imageBitmap.ToByte();
            }
 


        }
    }

    public bool lowvram { get; set; } = true;

    public int threshold_a { get; set; } = 0;
    public int threshold_b { get; set; } = 0;

   // public bool pixel_perfect = true;
    public ControlNetArgs()
    {

    }
 
}
