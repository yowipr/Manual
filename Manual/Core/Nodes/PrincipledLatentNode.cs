using Json.Net;
using Manual.API;
using Manual.MUI;
using Manual.Objects;
using Manual.Objects.UI;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Microsoft.Xaml.Behaviors;
using MS.WindowsAPICodePack.Internal;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Manual.Core.Nodes;

public partial class PrincipledLatentNode : LatentNode
{
    NodeOption ResultOutput;
    NodeOption Prompt, PromptNegative, ResolutionX, ResolutionY, Steps, CFGScale, Seed, Control;
    NodeOption Reference, DenoisingStrength, Mask, MaskBlur, MaskedContent;
    NodeOption UseRunpod;


    bool enableMasking = false;
    LayerBase maskLayer = null;
    public PrincipledLatentNode()
    {
        Name = "Principled Latent";
      
        ResultOutput = AddOutput("Generation", FieldTypes.LAYER, GenerateImageValue);

        AddField("Model", FieldTypes.STRING, "Model", typeof(M_ComboBox));

        Reference = AddInput("Reference", FieldTypes.LAYER);
        DenoisingStrength = AddField("Denoising Strength", FieldTypes.FLOAT, 0.75f, M_SliderBox.StrengthSlider() );

        Prompt = AddInput("Prompt Positive", FieldTypes.STRING, "a cute cat, anime style");
        PromptNegative = AddInput("Prompt Negative", FieldTypes.STRING, "low res");

        ResolutionX = AddField("Resolution X", FieldTypes.INT, 512, new M_SliderBox("Width", 1, 1080, 1, 8, true) );
        ResolutionY = AddField("Resolution Y", FieldTypes.INT, 512, new M_SliderBox("Height", 1, 1080, 1, 8, true) );

        Steps = AddField("Steps", FieldTypes.INT, 20, new M_SliderBox("Steps", 1, 64, 20, 1, true) );
        CFGScale = AddField("CFG Scale", FieldTypes.FLOAT, 5, new M_NumberBox("CFG Scale", 10, 0.5, true, 1, 30) );
        Seed = AddField("Seed", FieldTypes.INT, -1, new M_NumberBox("Seed", 1, 300));  //2351539900

        Mask = AddInput("Mask", FieldTypes.LAYER);
        MaskBlur = AddField("Blur", FieldTypes.INT, 4, new M_SliderBox("Blur", 0, 64, 20, 1, true));
        MaskedContent = AddField("Masked Content", FieldTypes.INT, 1, new M_NumberBox("Masked Content", 0, 1, true, 0, 3));

     
        Control = AddInput("Control", FieldTypes.CONTROL);

        UseRunpod = AddField("Use Runpod", FieldTypes.BOOLEAN, false, new M_CheckBox("Use Runpod"));
    }


    LayerBase LayerInBox(LayerBase referenceL)
    {
        var currentBakeFrame = GenerationManager.Instance.CurrentBakedFrame();
        var referenceValue = referenceL._Animation.GetImageStatusAt(currentBakeFrame);
        Layer reference = new(referenceValue);
        reference.CopyDimensions(referenceL);
        return reference;
    }

    WriteableBitmap GetLayerInBoxCrop(LayerBase referenceL) // MAIN
    {
        var croppedRef = LayerInBox(referenceL);
        return GetCropReference(croppedRef);
    }




    public Dictionary<string, Dictionary<string, object>> Scripts { get; set; } = new();

    async Task<object?> GenerateImageValue()
    {
        IsWorking = true;
        LayerBase referenceL = await Reference.GetValue<LayerBase>();
      
        //IMAGE REFERENCE
        List<byte[]> imgRefs = new();
        if (referenceL != null) // img2img
        {
            byte[] cropping = null;
            await AppModel.InvokeAsync(() =>
            {
                cropping = GetLayerInBoxCrop(referenceL).ToByte();
            });
            imgRefs.Add(cropping);
        }

        //MASK
        LayerBase maskL = await Mask.GetValue<LayerBase>();
        byte[] maskImg = null;
        enableMasking = false;
        maskLayer = null;
        if (referenceL != null && maskL != null)
        {      
            AppModel.Invoke(() =>
            {
                enableMasking = true;
                maskLayer = maskL;

                maskImg = GetLayerInBoxCrop(maskL).ChangeMaskBlackAndWhite().ToByte();

            });
        }


        // SCRIPTS CONTROL

        Scripts.Clear();
        var script = await Control.GetValue<ScriptArg>();
        if(script != null)
            Scripts[script.Name] = new Dictionary<string, object> {   { "args", script.Args }  };


        string subendpoint = "/sdapi/v1/";
        string endpoint = "txt2img";
        if (referenceL is LayerBase)
            endpoint = "img2img";

        var payload = new
        {
            //runpod
           // api_name = endpoint,
          //  api_verb = "POST",

            //txt2img
            prompt = await Prompt.GetValue(),
            negative_prompt = await PromptNegative.GetValue(),

            width = await ResolutionX.GetValue(),
            height = await ResolutionY.GetValue(),

            steps = await Steps.GetValue<int>(),
            cfg_scale = await CFGScale.GetValue(),


            //img2img
            init_images = imgRefs,
            denoising_strength = await DenoisingStrength.GetValue(),

            //mask
            mask = maskImg,
            mask_blur = await MaskBlur.GetValue(),
            inpainting_fill = await MaskedContent.GetValue(), //default 1, reference original
            inpaint_full_res = false,
            // latent_mask = imgRefs[0],
            include_init_images = true, // no hace nada

            alwayson_scripts = Scripts,


            override_settings = new
            {
                sd_model_checkpoint = "sd_xl_base_1.0",
                sd_vae = "sdxl_vae.safetensors"
            },
        };

        StepProgress = payload.steps;
        Output.LogCascade("Generating...", payload.prompt, payload.negative_prompt, payload.width, payload.height, payload.steps, payload.cfg_scale, payload.inpainting_fill);


        string json = WebManager.Parse(payload);
        System.IO.File.WriteAllText($"{App.LocalPath}Resources/PromptPresets/jsonTest.txt", json);
        //  return null;


        string apiUrl = WebManager.Combine(Settings.instance.CurrentURL, subendpoint + endpoint);

        try
        {
            if(Settings.instance.EnablePreview)
                 CalculateProgress();

            bool useRunpod = (bool)UseRunpod.FieldValue;

            JObject? result = null;
            if (!useRunpod)
                result = await WebManager.POST(apiUrl, payload);
            else
            {
                //  var runpodPayload = new { input = payload, };
                //  result = await WebManager.POST(Settings.instance.CurrentURL, runpodPayload, Constants.AuthToken);

                var api = new { method = "POST", endpoint };
                var runpodPayload = new { input = new { api, payload } };
                result = await WebManager.POST(Settings.instance.CurrentURL, runpodPayload, Constants.AuthToken);
            }

            //convert to layer
            var imgResult = await AppModel.InvokeAsync(() =>
            { 
                var r = GetImage(result);
                if (GenerationManager.Instance.CurrentGeneratingImage is not null && r is not null)
                    GeneratedImage.SetPreview(r.ToSKBitmap());
                return new Layer(r);
            });
           

            return imgResult;
        }
        catch (Exception ex)
        {
            Output.Log(ex.Message, "PrincipledLatent");
            return null;
        }
        finally
        {
            //finalize
            Progress = 0;
            isGenerating = false;
        }

    }

    byte[]? GetImage(JObject result)
    {
        var responseImages = result["images"];
        var image = responseImages[0];

        var base64Data = image.ToString().Split(',')[0];
        byte[] bytes = Convert.FromBase64String(base64Data);


        //----extra info PD: devuelve también la imagen en byte string, se laguea ojo
      
       var parameters = result["parameters"];

       string info = result["info"].ToString();
       JObject infoObject = JObject.Parse(info);
       long seed = infoObject.Value<long>("seed");

      //  if (GenerationManager.Instance.Realtime && (long)Seed.FieldValue != -1)
      //  {
      //      Seed.FieldValue = seed;
     //   }

        return bytes;
    }


    WriteableBitmap? GetCropReference(LayerBase reference)
    {
        BoundingBox boundingBox = OutputNode.GetBoundingBoxs();
        var cropping = RendGPU.RenderArea(boundingBox, reference);

        //testing
        // Layer l = new Layer(cropping);
        // l.CopyDimensions(boundingBox);
        // ManualAPI.AddLayerBase(l);

        return cropping.ToWriteableBitmap();
    }


    bool isGenerating = false;
    async void CalculateProgress() // ------------------------------------------------- PREVIEW
    {
        isGenerating = true;
        int tries = 0;
        while (isGenerating)
        {
            await Task.Delay(1_000);

            var endpoint = "/sdapi/v1/progress";
            string apiUrl = WebManager.Combine(Settings.instance.CurrentURL, endpoint);
            var result = await WebManager.GET<JObject>(apiUrl);

            if (result == null) // server offline
            {
                tries++;
                continue;
            }
            if (tries > 3)
                break;

            float progress = result.Value<float>("progress");

            var current_image = result.Value<string>("current_image");
            var text_info = result.Value<string>("text_info");

            if (current_image != null)
            {
                var base64Data = current_image.ToString().Split(',')[0];
                var bytes = Convert.FromBase64String(base64Data);

     
                await AppModel.InvokeAsync(() =>
                {
                    if (GenerationManager.Instance.CurrentGeneratingImage is not null)
                    {
                        GenerationManager.Instance.CurrentGeneratingImage.Progress = progress;
                        var w = bytes.ToSKBitmap();
                        if (!enableMasking)
                            GeneratedImage.SetPreview(w);
                        else // MASK INPAINTING MODE
                        {
                            /*  var target = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;

                              Layer genResult = new(w);
                              genResult.CopyDimensions(target);

                              var maskedR = genResult.ApplyMask(maskLayer);

                              WriteableBitmap rrr = Renderizainador.RenderArea(target, maskedR);
                              GeneratedImage.SetPreview(rrr);*/
                            GeneratedImage.SetPreview(w);
                        }


                        //output node
                        if (ResultOutput.ConnectionOutput[0] != null)
                            ResultOutput.ConnectionOutput[0].AttachedNode.PreviewImage = w;
                    }

                 
                    Progress = progress;
                    AppModel.mainW.SetProgress(Progress, text_info);

                });
            }


        }

    }

    private string CalculateEtaTime(float eta)
    {
        eta /= 2;

        int minutes = (int)eta / 60; // Obtener la parte entera de los minutos
        int seconds = (int)eta % 60; // Obtener el residuo de los segundos

        string formattedTime = $"{minutes:D2}:{seconds:D2}"; // Formatear los minutos y segundos como "mm:ss"

        return " | " + "ETA: " + formattedTime;
    }



}

