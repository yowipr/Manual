using Manual.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Manual.MUI;
using static Manual.API.ManualAPI;
using Manual.Core;
using System.Windows;
using Manual.Editors;
using System.Windows.Media;
using Manual.Objects.UI;
using Manual.Objects;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis;
using Manual.Core.Nodes;
using Manual;
using System.Windows.Threading;
using System.Drawing;
using System.Threading;
using CefSharp.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenTK.Graphics.OpenGL;

namespace Plugins;

[Export(typeof(IPlugin))]
public class Text2ImageTool : IPlugin
{
   
    public void Initialize()
    {
        Tool.Register(new T_ImageGenerator());
        Tool.Register(new T_Inpainting());

        //PLUS
        //DISABLED RELEASE: 
       // Editor.RegisterEditor("UndoRedo History", () => new ED_UndoRedoHistory());
    }

}

public partial class T_ImageGenerator : Tool
{
    M_Section boxSection;
    public T_ImageGenerator()
    {
        //SHORTCUTS
        HotKey hotKey = new(Key.O, ChangeToolToThis, "Text2Image Tool");

        Box = OutputNode.GetBoundingBoxs();
  
        // TOOL PROPERTIES
        name = "Image Generator";
        iconPath = $"{App.LocalPath}Resources/Scripts/1Text2Image/icon.png";

        var resetBtn = new M_Button() { Content = "⟳ Reset Box Position", ToolTip = "Reset Bounding Box Position"};
        resetBtn.Click += (s, e) =>
        {
             BoundingBox.Reset();
        };
        boxSection = section("Bounding Box", [
           resetBtn,
           new M_SliderBox("ResolutionX", "ImageWidth", 1, 2048, 1, 8, true),
           new M_SliderBox("ResolutionY", "ImageHeight", 1, 2048, 1, 8, true)
        ]);

        //DISABLED: old
        //var genManagerD = new M_StackPanel(project.generationManager);
        //genManagerD.Add(new M_ComboBox("PromptPresets", "SelectedPreset"));
        //AddField(genManagerD);

        //var selctPresetD = new M_StackPanel("SelectedPreset");
        //genManagerD.Add(selctPresetD);

        ////var output = M_Section.VisualizeNode("Output", "Result");
        //var output = M_Section.VisualizeNodeOutput();
        //selctPresetD.Add(output);



        //PROMPT
        var sp = new M_StackPanel(GenerationManager.Instance);
        add(sp, body);


        var spp = new M_StackPanel(GenerationManager.Instance);

        var grid2 = new M_Grid();
        grid2.Column(new M_ComboBox("Prompts", "SelectedPreset.Prompt"), width: 3);
        grid2.Column(new M_CheckBox("⟳", "SelectedPreset.IsUpdateDriversPrompt") { ToolTip = "Update Drivers Prompt Automatically" });

        spp.Add(grid2);

   
        var grid = new M_Grid();
        grid.Column(new M_TextBox("Name"), width: 3);
        grid.Column(new M_Button("➕", GenerationManager.Instance.DuplicatePrompt) { Background = null, ToolTip = "Add new duplicated Prompt and asign" });
        grid.Column(new M_Button("🗑️", GenerationManager.Instance.DeletePrompt) { Background = null, ToolTip = "Delete Prompt and delete" });


        var sp2 = new M_StackPanel("SelectedPreset.Prompt");
        add(sp2, sp);

        // PRESET
        var p0 = section(sp2, "Preset",

           spp,
           grid
            );

        // PROMPT
        var p1 = section(sp2, "Prompt",

            new M_PromptBox("Positive Prompt", "", "PositivePrompt", false),
            new M_PromptBox("Negative Prompt", "", "NegativePrompt", false)

            );
        p1.IsExpanded = false;


        // ALT PROMPT
        var p1b = section(sp2, "Alt Prompt",

            new M_PromptBox("Alt Positive Prompt", "", "AltPositivePrompt", false),
            new M_PromptBox("Alt Negative Prompt", "", "AltNegativePrompt", false)

            );
        p1b.IsExpanded = false;

        // IMAGE
        var p2 = section(sp2, "Image",
          new M_ComboBox_Layers("ReferenceLayer") { Header = "Reference"},
          new M_SliderBox("Strength", 0, 1, jump: 0.01, isLimited: true),

          new M_SliderBox("Width", 64, 1024, jump: 64, isLimited: true),
          new M_SliderBox("Height", 64, 1024, jump: 64, isLimited: true)
          );


        // PARAMETERS
        var p3 = section(sp2, "Parameters",
           // params
           new M_NumberBox("Steps"),
           new M_NumberBox("CFG"),
           new M_NumberBox("Seed"),
             new M_NumberBox("ClipSkip")
           );

        // MODEL
        var p4 = section(sp2, "Model",
         new M_TextBox("Model"),
         new M_TextBox("Sampler"),
         new M_TextBox("Scheduler")
         );

        // LORAS
        var p5 = section(sp2, "LoRAs",
         new M_TextBox("Lora0"),
         new M_SliderBox("Lora0Strength", 0, 1, jump: 0.01, isLimited: true),

         new M_TextBox("Lora1"),
         new M_SliderBox("Lora1Strength", 0, 1, jump: 0.01, isLimited: true)

         );

    }
    void NewPrompt()
    {
      
    }
    void DuplicatePrompt()
    {
      
    }
    void RemovePrompt()
    {
       
    }



    [ObservableProperty] BoundingBox box;
    public override void OnToolSelected()
    {
        Box = OutputNode.GetBoundingBoxs();
        if(Box is null)
        {
            Box = BoundingBox.Add();
        }

        boxSection.DataContext = Box;
        Box.Opacity = 100f;
        Box.Enabled = true;

        Shortcuts.MouseDown += Shortcuts_MouseDown;
    }

    private void Shortcuts_MouseDown(object sender, MouseEventArgs e)
    {
        Box = OutputNode.GetBoundingBoxs();
        if (Box != null)
        {
            Box.Enabled = true;
        }
        boxSection.DataContext = Box;
    }

    public override void OnToolDeselected()
    {
        Box = OutputNode.GetBoundingBoxs();
        if (Box != null)
        {
            Box.Opacity = 33f;
            Box.Enabled = false;
        }
        boxSection.DataContext = Box;
        Shortcuts.MouseDown -= Shortcuts_MouseDown;
    }



}


public class T_Inpainting : T_BrushCustom
{

    PromptPreset ipreset;

    public T_Inpainting()
    {
       

        //SHORTCUTS
        HotKey hotKey = new(KeyComb.None, Key.M, ChangeToolToThis, "Inpainting Tool");
        brush.isCustomCursor = false;

        //brush
        SetBrush();

        // TOOL PROPERTIES
        name = "Inpainting";
        iconPath = $"{App.LocalPath}Resources/Scripts/Inpainting/icon.png";
        cursorPath = $"{App.LocalPath}Resources/Scripts/Brush/cursor_draw.cur";

        InstantiateBrushUI();
    }


    class BrushInpaint : Brusher
    {
        public override System.Windows.Media.Color ColorBrush
        {
            get => _colorBrush;//base.ColorBrush;
            set => _colorBrush = value;
        }
    }

    void SetBrush()
    {
        brush = new BrushInpaint();
        brush.Size.EnablePenPressure = false;
        brush.Type = "Inpaint";
        brush.Size.Value = 50;
        brush.Size.EnableVelocity = false;
        brush.ColorBrush = Colors.White;

        brush.OnMouseDown = OnMouseDown;
        brush.OnMouseUp = OnMouseUp;

    }

    public override void OnToolSelected()
    {

        var mask0 = FindLayer("Mask") as GhostLayer;
        TargetLayer = mask0;


        //PROMPT
        prompt = GenerationManager.Instance.SelectedPrompt;
        if (prompt != null)
        {
            prompt.PropertyChanged += Prompt_PropertyChanged;
            SetTargetPreset("inpainting");
        }
        else
        {
            GenerationManager.OnRegisteredInvoke(() => 
            {
                SelectedPreset.Prompt.PropertyChanged += Prompt_PropertyChanged;
                SetTargetPreset("inpainting");
                prompt = SelectedPreset.Prompt;
            }          
          );
            
        }

        GenerationManager.OnGenerating += GenerationManager_OnGenerating;


        if (TargetLayer == null) //&& !layers.Contains(TargetLayer))
        {
            var mask = new GhostLayer();
            AddLayerBase(mask, false);
            mask.MoveOnTop();
           // mask.Opacity = 33f;
            mask.Name = Namer.SetName("Mask", layers);
            mask.AddEffect(new E_TransparencyPattern());

            TargetLayer = mask;

            //   var nodeop = SelectedPreset.FindNode(typeof(PrincipledLatentNode)).FindField("Mask");
            //   nodeop.FieldValue = mask;
        }


        ////PRESET
        //if (!SelectedPreset.Pinned)
        //{
        //    ipreset ??= FindPreset("inpainting");
        //    if (ipreset == null)
        //        InstantiatePreset();

        //    ManualAPI.SelectedPreset = ipreset;
        //}


        if (prompt != null)
        {
            TargetLayer.Opacity = prompt.Strength * 100;
            TargetLayer.Visible = true;
        }

        base.OnToolSelected();

    }

    PromptPreset oldPreset;
    Prompt prompt;
    public override void OnToolDeselected()
    {
        base.OnToolDeselected();

        TargetLayer.Visible = false;

        prompt.PropertyChanged -= Prompt_PropertyChanged;
        GenerationManager.OnGenerating -= GenerationManager_OnGenerating;

    }


    void InstantiatePreset()
    {
        ipreset = PromptPreset.FromTemplate("inpainting");
        ipreset.Block = false;

        if(ipreset != null)
            GenerationManager.Instance.AddPreset(ipreset, false);
    }



    CancellationTokenSource cts;
    public override void OnMouseDown(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        cts?.Cancel();
        TargetLayer.Opacity = 100;
        //AnimateOpacityTarget(100);
        base.OnMouseDown(layer, initialPosition, finalPosition);
    }
    public override void OnMouseUp(LayerBase layer, PointF initialPosition, PointF finalPosition)
    {
        cts = new CancellationTokenSource();

        // TargetLayer.Opacity = prompt.Strength * 100;
        // Shot.UpdateCurrentRender();
        ActionHistory.DoAction.Do(() => {
            AnimateOpacityTarget(prompt.Strength * 100);
        }, 0.5, cts.Token);

        base.OnMouseUp(layer, initialPosition, finalPosition);
    }

    void AnimateOpacityTarget(float targetOpacity)
    {
        RendGPU.AnimateFloat(
            updateAction: value => TargetLayer.Opacity = value,
            from: TargetLayer.Opacity,
            to: targetOpacity,
            duration: 0.5f);

    }


    //on selected only
    private void Prompt_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Strength" && !SelectedLayer.PreviewValue.IsPreview)
        {
            TargetLayer.Opacity = prompt.Strength * 100;
            Shot.UpdateCurrentRender();
        }
    }

    private void GenerationManager_OnGenerating()
    {
        //if (GenerationManager.Instance.CurrentGeneratingImage is GeneratedImage genimg)
        //{
        //    if (genimg.Progress == 0)
        //        TargetLayer.Opacity = 1;
        //    else
        //        TargetLayer.Opacity = (1 - genimg.Progress) * prompt.Strength * 100;


        //   // Output.Log($"opacity {TargetLayer.Opacity}, genimg {genimg.Progress}");
        //}
    }



}






public class ED_UndoRedoHistory : ED_Control
{
    public ED_UndoRedoHistory()
    {
        name = "UndoRedo History";
        body = Body();

        ActionHistory.OnActionChange += ActionHistory_OnActionChange;
    }

    private void ActionHistory_OnActionChange(IUndoableAction actin)
    {
        // RRR.Source = Renderizainador.RenderFrame();
        listH.ItemsSource = ActionHistory.instance.actions;
        //  listRH.ItemsSource = ActionHistory.instance.RealHistory;
        textIndex.Text = ActionHistory.instance.currentIndex.ToString();
        listH.Items.Refresh();
     //   listRH.Items.Refresh();
    }

//    ListView listRH;
    ListView listH;
    TextBlock textIndex;
    StackPanel Body()
    {
        var b = new StackPanel();

      //  listRH = new ListView() { DisplayMemberPath = "Name", Opacity = 0.56, Background= new SolidColorBrush(Colors.Black), Foreground= new SolidColorBrush(Colors.White) };
        listH = new ListView() { DisplayMemberPath = "Name", Opacity= 1, Background = new SolidColorBrush(Colors.Black), Foreground = new SolidColorBrush(Colors.White) };
        textIndex = new TextBlock();

        //   b.Children.Add(listRH);
        b.Children.Add(textIndex);

        b.Children.Add(listH);


        return b;
    }


}