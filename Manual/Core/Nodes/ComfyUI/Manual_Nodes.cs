using LangChain.Base;
using Manual.API;
using Manual.Editors.Displays;
using Manual.MUI;
using Manual.Objects;
using Manual.Objects.UI;
using MS.WindowsAPICodePack.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Silk.NET.Core.Native;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Manual.Core.Nodes.ComfyUI;

 static partial class ManualNodes
{
    internal static void RegisterAllNodes()
    {
      
        //MANUAL NODES
         RegisterNode(

         ("M_Layer", () => new NodeLayer()),
         ("M_Output", () => new NodeOutput()),
         ("M_RenderArea", () => new NodeRenderArea()),
         ("M_Keyframes", () => new NodeKeyframes())

         );
    }

    internal static void RegisterUtils(GenerationManager gen)
    {
        //ACTIONS
     //   gen.RegisterNode("Prompt Input", "PromptInput", () => new NodePromptInput(), "Manual/Special");
     //   gen.RegisterNode("Prompt Output", "PromptOutput", () => new NodePromptOutput(), "Manual/Special");


        //UTILS
      //  gen.RegisterNode("Wrapper Node", "WrapperNode", ()=> new WrapperNode(), "Manual/Special");

        gen.RegisterNode("Primitive", "PrimitiveNode", () => new PrimitiveNode(), "ComfyUI/Utils");
        gen.RegisterNode("Reroute", "Reroute", () => new RerouteNode(), "ComfyUI/Utils");
    }
}
//----------------------------------------------------------------------------------------------------------------------------------- MANUAL NODES

//#################################################################################################################################################################################
public class NodeLayer : ManualNode
{
    NodeOption Result, ResultMask, LayerRef, Normalize, Isolate, CropBox;
    NodeOption CustomKF, KFInt;
  
    public NodeLayer()
    {
        Name = "Layer";
        NameType = "M_Layer";

        //output has to have the same name of the input in python but with uppercase
        //input: "layer"  output: "Layer"
        Result = AddOutput("Layer", FieldTypes.LAYER);
        Result.AddType(FieldTypes.LAYER, Result_LAYER_S, "layer");
        Result.AddType(FieldTypes.IMAGE, Result_IMAGE, "layer current image");
        Result.AddType(FieldTypes.STRING, Result_STRING, "the layer name");

        ResultMask = AddOutput("Alpha", FieldTypes.MASK);
        Result.AddType(FieldTypes.MASK, Result_IMAGE, "layer current image");

        LayerRef = AddField("LayerRef", FieldTypes.LAYER, new Item("Selected"), new M_ComboBox_Layers());
        CropBox = AddField("Crop to Bounding Box", FieldTypes.BOOLEAN, true, new M_CheckBox("Crop to Bounding Box"));

        Isolate = AddField("Isolate", FieldTypes.BOOLEAN, true, new M_CheckBox("Isolate") { ToolTip = "ignores Opacity and Visibility of the Layer (only if crop BoundingBox is enabled)" });
        CropBox.ValueChanged += CropBoxValueChanged;

        Normalize = AddField("Normalize", FieldTypes.BOOLEAN, false, new M_CheckBox("Normalize"));

    
        EnablePreview = true;
        LayerRef.ValueChanged += PREVIEW_CHANGED;

        CustomKF = AddField("Custom Keyframe", FieldTypes.BOOLEAN, false, new M_CheckBox("Custom Keyframe"));
        CustomKF.ValueChanged += CustomKF_Changed;

        KFInt = AddField("Keyframe", FieldTypes.INT, 1, new M_NumberBox() { Header = "Keyframe"});
        KFInt.ValueChanged += PREVIEW_CHANGED;
    }

    void CropBoxValueChanged(object? value)
    {
        Isolate.Enabled = (bool)value;
    }

    void CustomKF_Changed(object? value)
    {
        KFInt.Enabled = (bool)value;       
    }

    public void SetLayer(LayerBase layer)
    {
        LayerRef.FieldValue = layer;
        KFInt.FieldValue = layer._Animation.CurrentFrame;
    }
    public void SetSelected()
    {
        LayerRef.FieldValue = new Item("Selected");
        KFInt.FieldValue = ManualAPI.SelectedLayer._Animation.CurrentFrame;
    }

    void PREVIEW_CHANGED(object? value)
    {
        var layer = GetLayer(LayerRef);
        if (layer == null) return;

        if ((bool)CustomKF.FieldValue == true)
            PreviewImage = layer._Animation.GetImageStatusAt(Convert.ToInt32(KFInt.FieldValue));
        else
            PreviewImage = layer.Image;
   }


    public static LayerBase GetLayer(NodeBase nodeLayer)
    {
        return GetLayer(nodeLayer.field("LayerRef"));
    }
    public static LayerBase GetLayer(NodeOption layerRef)
    {
        if (layerRef.IsInputOrField() && layerRef.ConnectionInput != null)
            return GetLayer(layerRef.ConnectionInput);

        if (layerRef.FieldValue == null || layerRef.FieldValue.ToString() == "Selected")
            return ManualAPI.SelectedLayer;
        else if (layerRef.FieldValue is string name)
            return api.layer(name);
        else
            return (LayerBase)layerRef.FieldValue;
    }


    byte[] layer_image_kf(LayerBase layer)
    {
        return layer_image_kf_sk(layer).ToByte();
    }
    SKBitmap layer_image_kf_sk(LayerBase layer)
    {
        if ((bool)CustomKF.FieldValue == true && layer._Animation.GetTimedVariable("Image") is TimedVariable timed)
        {
            return layer._Animation.GetImageStatusAt(Convert.ToInt32(KFInt.FieldValue));
        }
        else if (layer.PreviewValue.IsPreviewMode)
            return layer.PreviewValue.originalValue;
        else
            return layer.Image;

    }

    string Result_LAYER_S()
    {
        var l = Result_LAYER();
        var s = ManualClipboard.Serialize(l);
        return s;
    }

   public LayerBase Result_LAYER()
    {
        var realLayer = NodeLayer.GetLayer(LayerRef);
        if(realLayer == null)
        {
            AttachedPreset.ExceptionNode(this);
            throw new Exception("\"Layer not found {LayerRef.FieldValue}");
        }

        LayerBase layer = realLayer.CloneFast();


        if ((bool)Isolate.FieldValue == true)
        {
            layer.Opacity = 100;
            layer.Visible = true;
        }

       // if ((bool)CustomKF.FieldValue == true)
            layer.Image = layer_image_kf_sk(realLayer);

        if ((bool)Normalize.FieldValue == true)
        {
            var box = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;
            layer.CopyDimensions(box);

            layer.Image = layer_image_kf_sk(realLayer);

        }


        if ((bool)CropBox.FieldValue == true)
        {
            var isolate = !(bool)Isolate.FieldValue;

            BoundingBox box = ManualAPI.SelectedShot.layers.FirstOrDefault(l => l.GetType() == typeof(BoundingBox)) as BoundingBox;
            var options = new RenderAreaOptions()
            {
                EnableEffects = false,
                EnableOpacity = isolate,
                EnableVisibility = isolate,
            };
            var renderArea = RendGPU.RenderArea(box, layer, options);
            //var result = renderArea.ScaleTo(box.ResolutionX, box.ResolutionY);
            layer.Image = renderArea;
        }


     //   Core.Output.Log(layer.Image);
        return layer;
    }

    byte[] Result_IMAGE()
    {
        var layer = Result_LAYER();

        var img = layer.Image.ToByte();

       // Core.Output.Log(layer.Image, layer.Name);
        return img;
    } 

    string Result_STRING()
    {
        return ((LayerBase)LayerRef.FieldValue).Name;
    }


    public override ComfyNodeAPI TO_API(ComfyNodeAPI data)
    {
        if (Result.IsConnected() && Result.ConnectedNode().First() is NodeOutput)
        {
            data.inputs["layer"] = null;
            return data;
        }

        var a = base.TO_API(data);
        if (a.Get("layer") == null)
        {
            var idata = InputData.New(FieldTypes.MASK, Result_IMAGE());
            a.Set("layer", idata);
        }


        return a;
    }

}

//#################################################################################################################################################################################
public enum OutputBlendMode
{
    Front,
    Behind,
    Replace,
    OnTop,
}
public class NodeOutput : ManualNodeOutput
{

    internal NodeOption Result, LayerTarget, BlendMode, Mask;
    public NodeOutput()
    {
        Name = "Output";
        NameType = "M_Output";
        IsOutput = true;
        EnablePreview = true;

        Result = AddInput("Result", FieldTypes.IMAGE);
        Mask = AddInput("Mask", FieldTypes.LAYER);

        LayerTarget = AddField("Target", FieldTypes.LAYER, new Item("Selected"), new M_ComboBox_Layers());

        BlendMode = AddField("Blend Mode", FieldTypes.STRING, OutputBlendMode.Front.ToString(), new M_ComboBoxEnum(typeof(OutputBlendMode)));

    }


    public override ComfyNodeAPI TO_API(ComfyNodeAPI data)
    {
        data.inputs["result"] = Result.ToComyData();
        return data;
    }

    internal override void OnApiLoaded(ApiList a)
    {
        base.OnApiLoaded(a);
        a.Nodes[$"{IdNode}"].inputs.Remove("mask");
    }


    PreviewLayer previewLayer;
    public override void ON_START(GeneratedImage genimg)
    {

        if (Mask.IsConnected())
        {
            genimg.TargetMask = NodeLayer.GetLayer(Mask.ConnectionInput.AttachedNode);//.Result_LAYER();

            if (AttachedPreset.FindNode("Dilate Mask") is NodeBase n)
                genimg.MaskDilate = Convert.ToInt32(n.field("dilation").FieldValue);

            //if (genimg.TargetMask != null)
            //{
            //    previewMask = new Layer(genimg.TargetMask.Image);
            //    previewMask.CopyDimensions(genimg.BoundingBox);
            //    previewMask.IsAlphaMask = true;
            //    previewMask.ShotParent = genimg.TargetLayer.ShotParent;
            //}
            //else
            //    previewMask = null;

        }


        genimg.TargetLayer = NodeLayer.GetLayer(LayerTarget);

        if (!genimg.BakeKeyframes.Any())
        {
            var timed = genimg.TargetLayer._Animation.GetTimedVariable("Image");
            if (timed != null)
            {
                if (timed.GetKeyframe(genimg.LocalFrame) is Keyframe kk)
                {
                    genimg.BakeKeyframes.Add(kk);
                    kk.PreviewValue.StartPreview(genimg.TargetLayer.Image);
                }
                else
                {
                    var k = Keyframe.InsertBake(timed, genimg.LocalFrame);
                    k.IsBaking = true;
                    genimg.BakeKeyframes.Add(k);
                    k.PreviewValue.StartPreview(genimg.TargetLayer.Image);
                }
              
            }
            else
            {
                genimg.TargetLayer.PreviewValue.StartPreview(null);
            }
        }


        previewLayer = new PreviewLayer(genimg);
        previewLayer.Start();

    }


    public override void ON_OUTPUT(GeneratedImage genimg)
    {

        var firstFrame = genimg.LocalFrame;

        OutputBlendMode blendMode;
        if (BlendMode.FieldValue is string s)
            blendMode = AppModel.ParseEnum<OutputBlendMode>((string)BlendMode.FieldValue);
        else
            blendMode = (OutputBlendMode)BlendMode.FieldValue;

        //GENERATION SINGLE
        if (genimg.Results.Length == 1 && !ManualAPI.Animation.AutoKeyframe && genimg.TargetLayer._Animation.GetTimedVariable("Image") == null) //first time
        {

            // targetLayer.SetImage(results[0]);
           //  var img = Renderizainador.InpaintLayer(genimg.TargetLayer, genimg.BoundingBox, genimg.Results[0]);
            var img = RendGPU.Inpaint(genimg.TargetLayer, genimg.BoundingBox, genimg.Results[0], blendMode);
            PreviewImage = img; 
          //  genimg.TargetLayer.Image = img;
          genimg.TargetLayer.PreviewValue.FinishedPreview(img);

        //    Core.Output.Log(img);


        }


        //ANIMATION
        else
        {
            // REFS AND BAKE
            if (genimg.BakeKeyframes.Count <= 1)
            {
                var f = firstFrame;
                genimg.BakeKeyframes.Clear();
                foreach (var result in genimg.Results)
                {
                    var keyframe = Keyframe.Insert(genimg.TargetLayer, "Image", f);
                    genimg.BakeKeyframes.Add(keyframe);
                    f = f + 2;
                    
                }
            }

            Parallel.ForEach(genimg.BakeKeyframes, (keyframe, state, index) =>
            {
                if (keyframe.Block)
                    return;  // equivalent to 'continue' in a regular foreach

                keyframe.IsBaking = true;
                var imageResult = RendGPU.Inpaint(genimg.TargetLayer, genimg.BoundingBox, genimg.Results[(int)index], keyframe, blendMode);
                keyframe.PreviewValue.FinishedPreview(imageResult);
               // keyframe.Set(imageResult);
                keyframe.IsBaking = false;


            });

            AppModel.Invoke(() =>
            {
                genimg.TargetLayer._Animation.SetValue(ManualAPI.Animation.CurrentFrame);
                if (!ManualAPI.SelectedKeyframes.Any())
                    ManualAPI.SelectedKeyframe = genimg.BakeKeyframes[0];

            });


        }

    }




    Layer previewMask;
    public override void ON_PREVIEW(GeneratedImage genimg)
    {
        genimg.TargetLayer = genimg.TargetLayer;


        SKBitmap result;
        if (genimg.TargetMask != null)
        {
            var resultLayer = new Layer(genimg.PreviewImage);
            resultLayer.CopyDimensions(genimg.BoundingBox);
            resultLayer.Name = "resultLayer";

            //OPTIONS
            var options = new RenderAreaOptions();
            options.EnableOpacity = false;
            options.EnableVisibility = false;
            options.EnablePreviews = false;
            options.BlendMode = new Dictionary<LayerBase, SKBlendMode>
                {
                    { genimg.TargetMask, SKBlendMode.SrcOver },
                    { resultLayer, SKBlendMode.SrcIn }
                };

            if(genimg.MaskDilate > 0)
            options.Paint = (paint, l) =>
            {
                if(l == genimg.TargetMask)
                {
                    var strength = genimg.MaskDilate / 2;
                    var dilate = SKImageFilter.CreateDilate(strength, strength);
                    var blur = SKImageFilter.CreateBlur(strength, strength, SKShaderTileMode.Repeat);
                    paint.ImageFilter = SKImageFilter.CreateCompose(dilate, blur);
                }

            };

            var rr = RendGPU.RenderArea(genimg.TargetMask, resultLayer, options);
            var r = RendGPU.Inpaint(genimg.TargetLayer, genimg.BoundingBox, rr);

            result = r;
        }
        else
            result = RendGPU.Inpaint(genimg.TargetLayer, genimg.BoundingBox, genimg.PreviewImage);



        if (genimg.TargetLayer._Animation.GetTimedVariable("Image") is TimedVariable timed)
            genimg.BakeKeyframes[0].PreviewValue.StartPreview(result);
        else
           genimg.TargetLayer.PreviewValue.StartPreview(result);

        PreviewImage = result;

        previewLayer.Preview();

        if(!Settings.instance.EnablePreviewAnimation)
           Shot.UpdateCurrentRender();
    }

  
}


//#################################################################################################################################################################################


public class NodeRenderArea : ManualNode
{
    NodeOption Source, Dest, Result;
    public NodeRenderArea()
    {
        Name = "Render Area";
        NameType = "M_RenderArea";

        Result = AddOutput("Layer", FieldTypes.LAYER);
        Result.AddType(FieldTypes.IMAGE, Result_IMAGE, "layer current image");

        Source = AddInput("Source Layer", FieldTypes.LAYER);
        Dest = AddInput("Dest Layer", FieldTypes.LAYER);
    }


    byte[] Result_IMAGE()
    {
        //TODO: mala idea hacer esto
        var layerSrc = ((NodeLayer)Source.ConnectionInput.AttachedNode).Result_LAYER();
        var layerDest = ((NodeLayer)Dest.ConnectionInput.AttachedNode).Result_LAYER();

        var render = RendGPU.RenderArea(layerSrc, layerDest);
        Core.Output.Log(render);
        Core.Output.Log(layerSrc.Image);
        Core.Output.Log(layerDest.Image);
        return render.ToByte();
    }

}





//#################################################################################################################################################################################

public class NodeKeyframes : ManualNode
{
    NodeOption Result, LayerRef;
    public NodeKeyframes()
    {
        Name = "Keyframes";
        NameType = "M_Keyframes";

        Result = AddOutput("Keyframes", FieldTypes.IMAGE);
        Result.AddType(FieldTypes.IMAGE, Result_LAYER_S, "layer current image");

        LayerRef = AddField("Layer", FieldTypes.LAYER, new Item("Selected"), new M_ComboBox_Layers());
    }

    string Result_LAYER_S()
    {
        return "";
    }
}
    
    
    
    
    //#################################################################################################################################################################################
