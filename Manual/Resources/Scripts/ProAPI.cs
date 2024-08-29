using Manual.API;
using Manual.Core.Nodes;
using Manual.Core.Nodes.ProAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manual;
using Manual.Core;
using Manual.Core.Nodes.ComfyUI;
using ManualToolkit.Windows;
using ICSharpCode.AvalonEdit.Utils;
using Newtonsoft.Json.Linq;
using SkiaSharp.Views.WPF;
using System.Runtime.CompilerServices;
using SkiaSharp;
using System.Net.Http.Headers;
using System.Net.Http;

namespace Plugins;



[Export(typeof(IPlugin))]
public class ProAPIGeneral : IPlugin
{
    public void Initialize()
    {
        GenerationManager.OnNodesRegistered += GenerationManager_OnNodesRegistered;
      
    }

    private void GenerationManager_OnNodesRegistered()
    {
        GenerationManager.Instance.RegisterNode("FluxAPI", "FluxAPIOutput", () => new FluxAPINode(), "ProAPI");
        GenerationManager.Instance.RegisterNode("LumaAPI", "LumaAPIOutput", () => new LumaAPINode(), "ProAPI");
    }
}


//------------------------------------------------------------------------------------------------------------------------- FLUX
internal class FluxAPINode : ProAPINode
{
    public FluxAPINode()
    {
        Name = "Flux";
        BlendMode.FieldValue = OutputBlendMode.Replace;
    }

    public override string URL(string baseUrl)
    {
        var apiName = "flux";
        var fullPath = WebManager.Combine(baseUrl, apiName);
        return fullPath;
    }
    public override object OnSendingPrompt()
    {
        // Crear un objeto que representa el cuerpo de la solicitud JSON
        var requestBody = new
        {
            prompt = ManualAPI.SelectedPreset.Prompt.PositivePrompt,
            image_size = "square",
            num_images = 1,
            //parameters = new
            //{
            //    temperature = 0.7,
            //    maxTokens = 100,
            //    stopSequences = new[] { "STOP" }
            //}
        };

        // Serializar el objeto a una cadena JSON usando System.Text.Json o Newtonsoft.Json
        return requestBody;
    }
    public override async Task OnPreview()
    {
        
    }
    public override async Task OnOutput(JObject r)
    {
        var genimg = GenerationManager.Instance.CurrentGeneratingImage;

        if (r.TryGetValue("image", out JToken imageUrlToken))
        {
            var imageUrl = imageUrlToken.ToString();
            var image = await WebManager.DownloadImage(imageUrl);
            AppModel.Invoke(() =>
            {
                var bitmap = image.ToSKBitmap();

                //set results
                genimg.PreviewImage = bitmap;
                genimg.Results = [bitmap];

                ON_OUTPUT(genimg);

            });
          
        }

        
    }
}


//---------------------------------------------------------------------------------------------------------------- LUMA

internal class LumaAPINode : ProAPINode
{
    public LumaAPINode()
    {
        Name = "Luma";
        HandleGenerate = false;

    }
    public override string URL(string baseURl)
    {
        return "luma";
    }

    public override async Task OnOutput(JObject r)
    {
        
    }

    public override async Task OnPreview()
    {
        throw new NotImplementedException();
    }

    public override object OnSendingPrompt()
    {
        var img_start = api.layer("Layer 1").Image;
        var img_end = api.layer("Layer 2").Image;

        SendRequestToDreamMachine("anime girl with blue hair, looking at the sky, wind, anime screencap",
            "que pongo acá",
            "luma_vip_video",
            img_start,
            img_end);
            
        return null;
    }

    public async void SendRequestToDreamMachine(string prompt, string callbackUrl, string model, SKBitmap startImage, SKBitmap endImage)
    {
        using (var client = new HttpClient())
        {
            // Establece la URL de la API
            //    var url = "http://{{LUMA_API_HOST}}/api/v1/generation/add";
            var token = UserManager.GetToken();
            var web = "http://192.168.1.10:3000";
            var subPath = "api/generate/luma";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = WebManager.Combine(web, subPath);

            // Crea el contenido de la solicitud como MultipartFormDataContent
            using (var formData = new MultipartFormDataContent())
            {
                // Agrega los campos de texto
                formData.Add(new StringContent(prompt), "arg_prompt");
            //    formData.Add(new StringContent(callbackUrl), "callback");
             //   formData.Add(new StringContent(model), "model");

                // Convertir SKBitmap a array de bytes
                byte[] startImageBytes = startImage.ToByte();
                byte[] endImageBytes = endImage.ToByte();

                // Agregar las imágenes como archivos
                var imageContentStart = new ByteArrayContent(startImageBytes);
                imageContentStart.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                formData.Add(imageContentStart, "arg_image", "start.png");

                var imageContentEnd = new ByteArrayContent(endImageBytes);
                imageContentEnd.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                formData.Add(imageContentEnd, "arg_image_end", "end.png");

                // Envía la solicitud POST
                var response = await client.PostAsync(url, formData);

                // Maneja la respuesta
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    // Procesa la respuesta según sea necesario
                    Manual.Core.Output.Log($"Success: {responseData}");
                }
                else
                {
                    Manual.Core.Output.Log($"Error: {response.StatusCode}, {response.ReasonPhrase}");
                }
            }
        }
    }



}
