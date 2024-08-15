using Manual.API;
using Manual.Editors;
using Manual.MUI;
using Manual.Objects;
using ManualToolkit.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;
using System.Collections;
using System.Windows.Media.Imaging;
using ManualToolkit.Themes;
using System.Diagnostics;
using System.Globalization;
using MS.WindowsAPICodePack.Internal;
using System.Windows.Input;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using static Manual.Core.Nodes.ComfyUI.Comfy;
using static Manual.Core.Nodes.ComfyUI.ComfyNodeData;
using System.Windows.Media;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ManualToolkit.Generic;
using Manual.Editors.Displays;
using SkiaSharp;
using Newtonsoft.Json.Converters;
using CefSharp.DevTools.CSS;
using System.Windows.Controls;

namespace Manual.Core.Nodes.ComfyUI;






public class ComfyUIWorkflow : LatentNode
{

    NodeOption Result, Prompt, LayerRef;
    public ComfyUIWorkflow()
    {
        Name = "ComfyUI Workflow";

        Result = AddOutput("Result", FieldTypes.LAYER, ResultValue);
        Prompt = AddField("Workflow", FieldTypes.STRING, "manualW_api", new M_TextBox());
        LayerRef = AddField("Layer", FieldTypes.STRING, "Square", new M_TextBox());
    }

    async Task<object?> ResultValue()
    {
        Core.Output.Log("Loading ComfyUI");

        var image = await Run();

        Core.Output.Log("Succed");
        return image;
    }

    public static async Task<LayerBase> Run()
    {

        await Comfy.ConnectWebSocketAsync();

        dynamic promptText = GetPrompt();

        // Enviar el prompt y obtener el prompt_id
        string promptId = await Comfy.QueuePromptAsync(promptText);

        // Esperar a que la ejecución del prompt termine
       // await Comfy.WaitForExecutionAsync(promptId);

        // Una vez que la ejecución ha terminado, obtener el historial para recuperar las imágenes
        //var history = await Comfy.GetHistoryAsync(promptId);

        // Output.Log(history);
        return GetOutput("");
        //TODO: RENDIMIENTO: hay que desconectar con comfy.CloseWebSocketAsync(); el websocket una vez se cierre manual

    }

    public static LayerBase GetOutput(string historyJson)
    {
        Core.Output.Log("GetOutput");
        return ManualAPI.SelectedLayer;

        // Parse the JSON string into a JObject.
        var history = JObject.Parse(historyJson);

        // Obtener la lista de keys de los outputs para encontrar la key variable.
        var outputKeys = ((JArray)history["prompt"]).Last.ToObject<JArray>();
        var outputKey = outputKeys[0].ToString(); // Asume que la key es el primer elemento en la lista.


        // Ahora, extraer el filename usando la key variable.
        string filename = null;

        var outputs = history["outputs"];
        if(outputs == null || (outputs.Type == JTokenType.Object && !outputs.HasValues)) // error ocurred, cannot execute prompt
        {
            Core.Output.Log("error ocurred, cannot execute prompt, see server to see the error");
            return null;
        }

        if (outputs[outputKey]["images"] != null && outputs[outputKey]["images"].Any() && outputs[outputKey]["images"].First is JToken first && first["filename"] != null)
        {
            filename = outputs[outputKey]["images"].First["filename"].ToString();
        }

        if (filename != null)
        {
            string path = "";
            if (history["outputs"][outputKey]["images"].First["type"].ToString() == "temp")
                path = Path.Combine(Path.GetTempPath(), filename);
            else
                path = Path.Combine(Comfy.PATH, "output", filename);


            Layer l = null;
            AppModel.Invoke(() =>
            {
                l = new Layer(ManualCodec.ImageFromFile(path));
            });
            return l;
        }
        else
        {
            return null;
        }

    }


    //--------------------------------------------------------------------------------------------- API WORKFLOW
    /// <summary>
    /// jobject of workflow api
    /// </summary>
    /// <returns>current promptpreset transformed in comfy workflow API</returns>
    public static JObject GetPrompt()
    {
        return GetPrompt(ManualAPI.SelectedPreset);
    }
    public static JObject GetPrompt(PromptPreset preset)
    {
        var promptText = Comfy.WorkflowToAPI(preset);

        var prompt = JsonConvert.DeserializeObject<JObject>(promptText);


        // Modificar el texto de la solicitud y el seed
      //  var rndprop = GetPropertyAPI(prompt, "KSampler", "control_after_generate");
     //   if ((string)rndprop == "randomize")
   //         SetPropertyAPI(prompt, "KSampler", "seed", RandomSeed());


        return prompt;
    }
    public static JToken? GetNodeAPI(JObject prompt, string node)
    {
        //get a node
        var Node = prompt
        .Properties()
        .Select(p => p.Value)
        .FirstOrDefault(n => n["class_type"]?.ToString() == node);

        return Node;
    }
    /// <summary>
    /// THIS ONLY WORKS ON WORKFLOW API
    /// </summary>
    public static void SetPropertyAPI(JObject prompt, string node, string field, object newValue)
    {
        try
        {
            var Node = GetNodeAPI(prompt, node);
            if (Node != null)
                Node["inputs"][field] = JToken.FromObject(newValue);
        }
        catch
        {
            Core.Output.Error($"{node}, {field}", "ComfyUINodes: Property Not Found");
            throw;
        }
    }
    public static object? GetPropertyAPI(JObject prompt, string node, string field)
    {
        try
        {
            var Node = GetNodeAPI(prompt, node);
            if (Node != null)
            {
                var a = Node["inputs"][field];
                return a?.ToObject<object>();
            }
            else
                return null;
        }
        catch
        {
            Core.Output.Error($"{node}, {field}", "ComfyUINodes: Property Not Found");
            throw;
        }
    }
    public static ulong RandomSeed()
    {
        var buffer = new byte[8];
        RandomNumberGenerator.Create().GetBytes(buffer);
        var longRandom = BitConverter.ToUInt64(buffer, 0);
        longRandom = longRandom % (18446744073709551614 - 1) + 1;
        return longRandom;
    }

}




public static class Comfy
{
    /// <summary>
    ///  C:\\Users\\YO\\source\\repos\\ComfyUI_windows_portable\\ComfyUI
    /// </summary>
    public static string PATH = "C:\\Users\\YO\\source\\repos\\ComfyUI_windows_portable\\ComfyUI";

    private static readonly HttpClient _httpClient = new HttpClient();
    public static ClientWebSocket webSocket = new ClientWebSocket();
    public static string URL
    { 
        get 
        {
            return Settings.instance.CurrentURL;
        } 
    }
    public static string _serverAddress
    {
        get
        {
            return URL.Replace("http://", "").Replace("https://", "").TrimEnd('/');
        }
    } //"127.0.0.1:8188";


    static string PromptId { get; set; }
    //-------------------------------------------------------------------------------------------------------------------- GENERATE
    internal static async Task Generate(PromptPreset preset)
    {
        ImagesResult = new();
        AppModel.mainW.SetProgress(0);
        AppModel.mainW.SetMessage("Generating...");

        var prompt = ComfyUIWorkflow.GetPrompt(preset);

        PromptId = await QueuePromptAsync(prompt);
        if (PromptId == null)
            throw new Exception("error");
        

        finalizeSignal = new TaskCompletionSource<bool>();
        _isNotSamePrompt = false;

        Task.Run(ListenWebSockets);
     
        await WaitToFinalizeGeneration();
    }

    internal static async void Interrupt()
    {
        // Realizar la solicitud
        try
        {
            var response = await _httpClient.PostAsync(WebManager.Combine(URL, "interrupt"), null);

            GenerationManager.Instance.isInterrupt = true;
            GenerationManager.Instance.interrupting = false;
            Core.Output.Log("generation interrupted", "Interrupted");
        }
        catch (Exception ex)
        {
            Core.Output.Log(ex.Message, "Interrupted");
        }
    }
    internal static void ForceInterrupt()
    {
        finalizeSignal.TrySetResult(false);
    }

    internal static async Task ListenWebSockets()
    {
        bool Continue = await ConnectWebSocketAsync();
        if (!Continue)
            return;


        var buffer = new byte[1024 * 4];
        var messageBuilder = new StringBuilder();
        var imageBuffer = new List<byte>();

        while (Continue)
        {
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messageFragment);
                       
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary) 
                    {
                        // Acumular datos de imagen
                        imageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                    }
                }
                while (!result.EndOfMessage); // Continúa recibiendo hasta que el mensaje esté completo
            
                if (result.MessageType == WebSocketMessageType.Text) // its a text, json
                {
                    var message = messageBuilder.ToString();
                    messageBuilder.Clear();

                     if(Settings.instance.DebugMode)
                    Core.Output.WriteLine("ComfyUI: " + message);


                     //InterpretWebSocket(message);
                    AppModel.Invoke(() => InterpretWebSocket(message));
   
                }
                else if (result.MessageType == WebSocketMessageType.Binary) // its an image
                {
                    int firstInteger = BitConverter.ToInt32(imageBuffer.Take(4).Reverse().ToArray(), 0);
                    int secondInteger = BitConverter.ToInt32(imageBuffer.Skip(4).Take(4).Reverse().ToArray(), 0);

                    byte[] imageData = imageBuffer.Skip(8).ToArray();
                    string base64Image = Convert.ToBase64String(imageData);

                    var payload = $"{{\"type\": \"image_output\", \"firstInteger\": {firstInteger}, \"secondInteger\": {secondInteger}, \"data\": \"{base64Image}\"}}";


                    AppModel.Invoke(() => InterpretWebSocket(payload));
                    //Core.Output.Log($"image_output {firstInteger} {secondInteger}");

                    imageBuffer.Clear(); // Limpia el buffer para la próxima imagen
                }
            }
            catch (WebSocketException ex)
            {
                // Manejo de excepciones
                Core.Output.Log("ComfyUI WebSocketException: " + ex.Message, "Comfy");

                if (webSocket.State != WebSocketState.Aborted && webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "WebSocketException", CancellationToken.None);

                }
                webSocket.Dispose();
                // Decide si necesitas reconectar o terminar
                Continue = false; // Establece a true si necesitas reconectar
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------ INTERPRET WEBSOCKET SIGNAL
    static void InterpretWebSocket(string message)
    {
       // Debug.WriteLine(message);
        var response = JsonConvert.DeserializeObject<dynamic>(message);
        string type = response.type;
        //   string promptId = response["data"].Value<string>("prompt_id");

        var preset = GenerationManager.Instance.CurrentGeneratingImage?.Preset;

        if (type == "executing")
        {

            if (response.data?.node == null && response.data?.prompt_id == PromptId)
            {
                //----------------------------------------------------------------- FINALIZED QUEUE
                finalizeSignal.TrySetResult(true);
                preset?.ExecutingNode(null);
            }
            else
            {

                int? nodeId = response["data"]["node"];

                if (nodeId == null) // si es null, probablemente es porque el prompt es el mismo que el anterior
                {
                    preset.ExecutingNode(null);
                }
                // Utiliza nodeId y promptId como necesites
                else if (nodeId != null && preset != null)
                {
                    var node = preset.FindNode((int)nodeId);
                    preset.ExecutingNode(node);
                    AppModel.mainW.SetMessage($"Generating ({node.Name})");
                }
            }
        }
        else if (type == "progress")
        {

            int value = response["data"].Value<int>("value");
            int max = response["data"].Value<int>("max");
            float progress = (float)value / max;

            // Utiliza value, max y normalizedProgress como necesites
            if (preset != null)
            {
                preset.ProgressCurrentNode(progress);
                AppModel.mainW.SetProgress(progress, $"{Convert.ToInt32(progress * 100)}%");
            }
        }
        else if (type == "image_output") //--------------- result in batch
        {
           var o = preset.GetOutputNode();

            string base64 = response.data;
            int firstInt = response.firstInteger; //1
            int secondInt = response.secondInteger; //1:preview  2:final

            byte[] image = Convert.FromBase64String(base64);
            GenerationManager.Instance.CurrentGeneratingImage.OriginalImage = image;

            var imageR = image.ToSKBitmap();
            o.EnablePreview = true;
            o.PreviewImage = imageR;

            if (Settings.instance.EnablePreview && secondInt == 1 && o is ManualNodeOutput mo) // preview
            {
                ManualAPI.Animation.RemoveFrameBuffer();

                GenerationManager.Instance.CurrentGeneratingImage.PreviewImage = imageR;
                mo.ON_PREVIEW(GenerationManager.Instance.CurrentGeneratingImage);
            }
            else if (secondInt == 2) // results
            {
                ImagesResult.Add(imageR);
                //Core.Output.Log($"result {ImagesResult.Count}");
               
            }
           // Core.Output.Log($"image_output {ImagesResult.Count} {firstInt} {secondInt}");
        }
        else if (type == "execution_error")
        {
            int? nodeId = response["data"]["node_id"];
            if (nodeId != null && preset != null)
            {
                var node = preset.FindNode((int)nodeId);
                preset.ExceptionNode(node);

                string exmsg = response["data"]["exception_message"];
                string extype = response["data"]["exception_type"];
                string[] traceback = response["data"]["traceback"].ToObject<string[]>();
                Core.Output.LogCascade("Comfy", exmsg, extype, traceback);
            }
        }
        else if (type == "status")
        {
            var status = response.data.status;
            int queueRemaining = status.exec_info.queue_remaining;

            if (queueRemaining > 0)
            {
                _isNotSamePrompt = true;
            }
            else
            {
                if (!_isNotSamePrompt)// && !GenerationManager.Instance.IsProcessingImages)
                {
                    finalizeSignal.SetResult(false);
                    Core.Output.Log("PromptPreset have equal values compared to previous, change something", "Comfy");
                }
            }


        }

    }

    static bool _isNotSamePrompt;
    static List<SKBitmap> ImagesResult;

    //--------------------------------------------------------------------------------------------------- GENERATION FINALIZED

    static TaskCompletionSource<bool> finalizeSignal = new TaskCompletionSource<bool>();

    static async Task WaitToFinalizeGeneration()
    {
        //wait to finalize
        await finalizeSignal.Task;

        // finalize
        var genimg = GenerationManager.Instance.CurrentGeneratingImage;
        var preset = genimg.Preset;

        var outputNode = preset.GetOutputNode();

        //var history = await GetHistoryAsync(PromptId);
        //var h = ComfyUIWorkflow.GetOutput(history.ToString());

        if (ImagesResult != null && ImagesResult.Any())
        {
            var o = preset.GetOutputNode();

            genimg.Results = ImagesResult.ToArray();
            genimg.PreviewImage = genimg.Results[0];

            if (o is ManualNodeOutput mo)
            {
                ManualAPI.Animation.RemoveFrameBuffer();
                mo.ON_OUTPUT(genimg);
            }
            else //default behaviour
            {
                var boundingBox = preset.GetBoundingBox();

                var targetLayer = genimg.TargetLayer;
                var imageResult2 = RendGPU.Inpaint(targetLayer, boundingBox, ImagesResult.Last());
                targetLayer.Image = imageResult2;

                genimg.PreviewImage = imageResult2;
                outputNode.PreviewImage = imageResult2;


            }
        }

        ImagesResult = null;
        AppModel.mainW.StopProgress();

       
    }


    public static async Task<string?> QueuePromptAsync(dynamic promptAPI)
    {
        var data = new { prompt = promptAPI, client_id = _clientId };
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        // Crear una solicitud
        var request = new HttpRequestMessage(HttpMethod.Post, WebManager.Combine(URL, "prompt"))
        {
            Content = content
        };

        // Añadir el header de autorización
       // var authToken = UserManager.GetToken();
       // if (authToken == null && !Core.Output.DEBUGGING_MODE) //not logged
      //      return null;

      //  request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        // Realizar la solicitud
        var response = await _httpClient.SendAsync(request);

        MainWindow.ChangeServerStatusTo(MainWindow.ServerStatus.Connected);

        string responseBody = await response.Content.ReadAsStringAsync();

        var responseJson = JObject.Parse(responseBody);
        if (responseJson["error"] is JObject j_error)
        {
            if (responseJson["node_errors"] != null)
            {  // Obtener el primer nodo de error
                var nodeErrors = responseJson["node_errors"] as JObject;
                if (nodeErrors != null)
                {
                    var firstNodeError = nodeErrors.Properties().FirstOrDefault();

                    var preset = GenerationManager.Instance.CurrentGeneratingImage.Preset;

                    var node = preset.FindNode(Convert.ToInt32(firstNodeError?.Name));
                    preset.ExceptionNode(node);
                }
                else if (responseJson["node_errors"] is JArray ja)
                {
                    if (!ja.Any())
                    {
                        var message = j_error["message"];
                        Core.Output.Log(message);
                    }
                }
            }


            Core.Output.Log(responseBody, "Comfy Queue Prompt");
            //finalizeSignal.SetCanceled();

            if (responseJson["error"]["type"].ToString() == "prompt_outputs_failed_validation")
            {
                Core.Output.Log("nodes has errors");
            }
            GenerationManager.Interrupt();
            return null;
        }

        return JsonConvert.DeserializeObject<dynamic>(responseBody).prompt_id;
    }



    private static readonly string _clientId = Guid.NewGuid().ToString();
    public static async Task<bool> ConnectWebSocketAsync()
    {
        if (Comfy.webSocket.State == WebSocketState.Aborted || Comfy.webSocket.State == WebSocketState.Closed)
        {
            Comfy.webSocket.Dispose();
            Comfy.webSocket = new ClientWebSocket();
        }

        if (Comfy.webSocket.State != WebSocketState.Open)
        {
            try
            {
              // string newUrl = "miusuario_9dc7f071-a6f2-4018-9aba-c2075fd83776";
                string oldUrl = _serverAddress;
              //  string url = "2np62ly9e69djj-3000.proxy.runpod.net";
                await webSocket.ConnectAsync(new Uri($"ws://{_serverAddress}/ws?clientId={_clientId}"), CancellationToken.None);
                return true;
            }
            catch (WebSocketException ex)
            {
                // Aquí puedes manejar el error específico de WebSocket
               Core.Output.Log($"Server not found, cannot connect", "Comfy");
                return false;
            }
            catch (Exception ex)
            {
                // Aquí puedes manejar otros tipos de errores generales
                //Core.Output.Log($"Exception: {ex.Message}", "Comfy");
                webSocket = new();
                if (ex.Message.Contains("The WebSocket has already been started."))
                {
                    await ConnectWebSocketAsync();
                    return true;
                }
                else
                    return false;
            }
        }
        else
        {
            return false;
        }
    }
    internal static void Disconnect()
    {
        Task.Factory.StartNew(CloseWebSocketAsync);
        Core.Output.Log("You are only disconnected from the server, server still running");
    }
    public static async Task CloseWebSocketAsync()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Manual_Done", CancellationToken.None);
        Core.Output.Log("Disconnected web socket", "Comfy");
    }

    public static async Task WaitForExecutionAsync(string promptId)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            dynamic message = JsonConvert.DeserializeObject<dynamic>(jsonString);
            if (message.type == "executing" && message.data?.node == null && message.data?.prompt_id == promptId)
                break;
        }
        while (!result.CloseStatus.HasValue);
    }

    public static async Task<dynamic> GetHistoryAsync(string promptId)
    {

        var response = await _httpClient.GetAsync($"{WebManager.Combine(URL, "history")}/{promptId}");
        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<dynamic>(responseBody)[promptId];
    }







    /// <summary>
    /// load the nodes from server
    /// </summary>
    /// <returns></returns>
    static async Task<string> GetAllNodes()
    {
        try
        {
            var response = await _httpClient.GetAsync(WebManager.Combine(URL, "object_info") );
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType.MediaType == "application/json")
                return responseBody;
            else
                return "";
        }
        catch (HttpRequestException ex)
        {
            return "comfy_url_not_found";
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    static string GetAllNodesCache()
    {
        if (File.Exists(lazyNodesPath))
        {
            string responseBody = File.ReadAllText(lazyNodesPath);
            return responseBody;
        }
        else
            return "comfy_not_cache";
    }
    static readonly string lazyNodesPath = $"{App.LocalPath}Resources/Presets/comfy_nodes_info_cache.json";




    
    //------------------------------------------------------------------------------------------------------------------------------ LOAD COMFYUI NODES REGISTER
    public static async Task RegisterComfyNodes()
    {
        try
        {
            string? responseBody;

            responseBody = await GetAllNodes();
            if (responseBody == "comfy_url_not_found" || responseBody == "" || responseBody == "\"{\\\"detail\\\":\\\"Not Found\\\"}\"")
            {
                responseBody = GetAllNodesCache();
            }


            if (responseBody == "comfy_not_cache")
            {
                Core.Output.Log("you don't have comfy server opened, or url are wrong, or you don't have comfy", "Comfy");
                return;
            }


            var listNodes = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseBody);

            if (listNodes == null)
                return;

            if (responseBody != "" && responseBody != "\"{\\\"detail\\\":\\\"Not Found\\\"}\"")
                File.WriteAllText(lazyNodesPath, responseBody);


            foreach (var item in listNodes)
            {
                var node = item.Value;//.ToObject<Node>();
                var nodeName = item.Key;

                if (node is string s && s == "Not Found")
                    return;

                //SERIALIZE
                string nodeDisplayName = node.display_name;
                string description = node.description;
                string category = node.category;
                bool output_node = node.output_node;


          
                if (category.Split('/')[0] == "Manual")
                {
                    Func<LatentNode> comfyFactory = () => 
                    {
                        ManualNode mnode = ManualNodes.GetNode(node);
                        mnode.SetDataExp(new Node(node));
                        mnode.NameType = node.name;
                        mnode.Color = NodeColors.ColorByName(NodeColors.Violet);
                        return mnode;
                    };
                    GenerationManager.Instance.RegisterNode(nodeDisplayName, node.name.ToString(), comfyFactory, $"{node.category}");
                }
                else
                {
                    // CREATE COMFY NODE
                    Func<LatentNode> comfyFactory = () =>
                    {
                        // INITIALIZE NODE
                        var nodeUI = new ComfyUINode();
                        nodeUI.NameType = node.name;
                        nodeUI.Name = node.display_name;
                      
                        if(nodeUI.Name == "MaskBlur+")
                        {

                        }

                        //IS OUTPUT
                        nodeUI.IsOutput = output_node;
                        if (nodeUI.IsOutput)
                            nodeUI.EnablePreview = true;


                        List<NodeOption> Outputs = new();
                        List<NodeOption> Inputs = new();
                        List<NodeOption> Fields = new();



                        //OUTPUT_TYPES
                        var outputs = node.output;
                        var output_names = node.output_name;
                        int oi = 0;
                        foreach (var output in outputs)
                        {
                            string output_name = output_names[oi];
                            string output_type = output.Value;
                            
                            if(Outputs.FirstOrDefault(np => np.Name == output_name) is NodeOption duplicated_nodeop)
                                Core.Output.Warning("unexpected: an output has the same name of another, will be rewrited", "Comfy");
                            

                            var nodeop = new NodeOption(output_name, NodeOptionDirection.Output, output_type, NullReturn);
                            Outputs.Add(nodeop);
                            oi++;
                        }


                     
                        //---------------------------------------------------------------------------------------INPUT_TYPES
                        var inputsRequired = node.input.required;
                        var inputsOptional = node.input.optional;

                        var inputsOrderRequired = node["input_order"]?["required"] as JArray;
                        var inputsOrderOptional = node["input_order"]?["optional"] as JArray;

                        void SetInputs(dynamic inputsR, JArray inputOrder)
                        {
                            if (inputsR == null) return; //ignore optional usually


                            //reorder list
                            if (inputOrder != null)
                            {
                                var orderedInputs = new List<JProperty>();

                                // Reordenar inputsRequired según inputOrderRequired
                                foreach (var inputName in inputOrder)
                                {
                                    var property = inputsR[inputName.ToString()] as JProperty;
                                    if (property != null)
                                    {
                                        orderedInputs.Add(property);
                                    }
                                }

                                // Añadir cualquier input que no estuviera en inputOrderRequired (si lo deseas)
                                foreach (JProperty property in inputsR)
                                {
                                    if (!orderedInputs.Any(p => p.Name == property.Name))
                                    {
                                        orderedInputs.Add(property);
                                    }
                                }

                                // Reemplazar inputsRequired con la lista ordenada
                                inputsR = new JObject(orderedInputs);
                            }




                            foreach (var input in inputsR) // list combobox field
                            {
                                //----------------FIELD TO RENDER
                                object defaultValueUI = null;
                                float? minUI = null;
                                float? maxUI = null;
                                bool multilineUI = false;
                                string type = FieldTypes.OBJECT;
                                IManualElement element = null;
                                float stepUI = 1;
                                NodeOptionDirection directionUI = NodeOptionDirection.Field;

                                string tooltip = "";


                                string input_name = input.Name;

                                var inputDetails = input.Value;

                                bool handledUI = false;
                                if (inputDetails[0] is IList)
                                {
                                    directionUI = NodeOptionDirection.Field;
                                    
                                    // Manejar cuando el primer elemento es una lista
                                    var listValues = inputDetails[0] as IList;
                                    List<object> objects = new();
                                    foreach (JToken val in listValues)
                                    {
                                        // Aquí puedes hacer algo con cada valor de la lista
                                        objects.Add(val);
                                    }
                                    var combobox = new M_ComboBox(objects) { DisplayMemberPath = ""};

                                //   if(listValues.Count > 0)
                                     // combobox.SelectedItem = listValues[0].ToString();

                                    element = combobox;

                                    object? defau = null;
                                    if (!objects.Any())
                                        Core.Output.Log("warning: this list is empty", input_name);
                                    else if (inputDetails.Count >= 2)
                                    {
                                        defau = inputDetails[1]["default"];
                                    }
                                    else
                                        defau = objects[0];


                                    // Si defau no es un JToken, entonces usa GetType() para determinar el tipo de .NET
                                    switch (defau)
                                    {
                                        case int _:
                                            type = FieldTypes.INT;
                                            break;
                                        case float _:
                                        case double _:
                                            type = FieldTypes.FLOAT;
                                            break;
                                        case string _:
                                            type = FieldTypes.STRING;
                                            break;
                                        case bool _:
                                            type = FieldTypes.BOOLEAN;
                                            break;
                                        default:
                                            type = FieldTypes.STRING;
                                            break;
                                    }


                                    defaultValueUI = defau;
                                    handledUI = true;

                                   // var nodeop = new NodeOption(input_name, NodeOptionDirection.Field, type, defau, element);
                                    //Fields.Add(nodeop);
                                }

                                else // input field widget
                                {
                                    //determine if field or input
                                    type = inputDetails[0];
                                    bool isField = type == "INT" || type == "FLOAT" || type == "STRING" || type == "BOOLEAN";
                                   

                                    if (!isField)
                                    {
                                        directionUI = NodeOptionDirection.Input;
                                    }
                                    else
                                    {
                                        var details = inputDetails[1];
                                        if (details == null)
                                            continue;

                                        directionUI = NodeOptionDirection.Field;

                                        if (details.TryGetValue("default", out JToken value))
                                        {
                                            defaultValueUI = value.ToObject<object>();
                                        }
                                        if (details.TryGetValue("multiline", out JToken value1))
                                        {
                                            multilineUI = value1.ToObject<bool>();
                                        }
                                        if (details.TryGetValue("min", out JToken value2))
                                        {
                                            minUI = value2.ToObject<float>();
                                        }
                                        if (details.TryGetValue("max", out JToken value3))
                                        {
                                            try
                                            {
                                                // Intenta convertir el valor a int
                                                maxUI = value3.ToObject<float>();
                                            }
                                            catch (OverflowException)
                                            {
                                                // Si el valor es demasiado grande para un int, podrías convertirlo a ulong o manejar el error
                                                // Nota: esto es solo un ejemplo, ajusta la lógica según tus necesidades
                                                ulong maxUlong = value3.ToObject<ulong>();
                                                // Aquí puedes decidir cómo manejar este caso, por ejemplo, establecer maxUI a un valor predeterminado
                                                maxUI = int.MaxValue;
                                            }
                                        }
                                        if (details.TryGetValue("step", out JToken value4))
                                        {
                                            stepUI = value4.ToObject<float>();
                                        }


                                    }
                                }

                                if (inputDetails.Count > 1)
                                {
                                    tooltip = inputDetails[1] is JObject detailsObject && detailsObject.ContainsKey("tooltip")
                                   ? detailsObject["tooltip"].ToString()
                                   : string.Empty;
                                }

                                //---------------------------------------------------------------------ASIGN ELEMENT
                                if (handledUI)
                                {

                                }
                                else if (type == FieldTypes.STRING)
                                {
                                    if (multilineUI)
                                        element = new M_PromptBox() { Header = input_name };
                                    else
                                        element = new M_TextBox() { Header = input_name };
                                }
                                else if (type == FieldTypes.INT || type == FieldTypes.FLOAT)
                                {
                                    if (maxUI != null && maxUI < 10_000)
                                    {
                                        var slider = new M_SliderBox(input_name);
                                        slider.Minimum = minUI ?? double.MinValue;
                                        slider.Maximum = maxUI ?? double.MaxValue;
                                        slider.IsLimited = true;
                                        slider.Jump = stepUI;
                                        element = slider;
                                    }
                                    else
                                    {
                                        var numberbox = new M_NumberBox(input_name, 100, stepUI);
                                        // numberbox.Minimum = minUI ?? double.MinValue;
                                        // numberbox.Maximum = maxUI ?? double.MaxValue
                                        element = numberbox;
                                    }
                                }
                                else if (type == FieldTypes.BOOLEAN)
                                {
                                    element = new M_CheckBox(input_name);
                                }


                                if (element == null)
                                    element = new M_TextBox();



                                NodeOption nodeoption = null;
                                if (directionUI == NodeOptionDirection.Field)
                                {
                                    nodeoption = new NodeOption(input_name, NodeOptionDirection.Field, type, defaultValueUI, element);//nodeUI.AddField(input_name, type, defaultValueUI, element);
                                    Fields.Add(nodeoption);
                                }
                                else if (directionUI == NodeOptionDirection.Input)
                                {
                                    nodeoption = new NodeOption(input_name, NodeOptionDirection.Input, type);
                                    Inputs.Add(nodeoption);
                                }


                                //TOOLTIP
                                if (nodeoption != null)
                                {
                                    nodeoption.ToolTip = tooltip;
                                }
                            }


                        }
                        SetInputs(inputsRequired, inputsOrderRequired);
                        SetInputs(inputsOptional, inputsOrderOptional);






                        //-------------------------------------------ADD FIELDS
                        foreach (var output in Outputs)
                        {
                            nodeUI.AddNodeOption(output);
                        }
                        foreach (var input in Inputs)
                        {
                            nodeUI.AddNodeOption(input);
                        }
                        foreach (var field in Fields)
                        {
                            nodeUI.AddNodeOption(field);
                            SeekExtraField(field);
                        }


                        // SET COLOR
                        nodeUI.Color = NodeColors.ColorByOutput(nodeUI);


                        return nodeUI;
                    };

                    GenerationManager.Instance.RegisterNode(nodeDisplayName, node.name.ToString(), comfyFactory, $"ComfyUI/{node.category}");
                }

            }


        }
        catch (Exception ex)
        {
            throw;
        }

    }

    static void SeekExtraField(NodeOption nodeop)
    {
        string name = nodeop.Name;
        if (name == "seed" || name == "noise_seed")
        {
           var extraOp = new NodeOptionLinked("control_after_generate", NodeOptionDirection.Field, FieldTypes.STRING, "randomize", new M_ComboBox(["randomize", "fixed", "increment", "decrement",]) { DisplayMemberPath = ""}) { AttachedNodeOption = nodeop};
            nodeop.AttachedNode.AddNodeOption(extraOp);
        }

    }


    static async Task<object?> NullReturn()
    {
        return null;
    }

    //------------------------------------------------------------------------------------------------------- IMPORT COMFYUI NODES WORKFLOW

    public static void ImportNodesFromImage(string filePath)
    {
      //  var prompt = FileManager.ReadCustomMetadata(filePath, "prompt"); // api
        var workflow = FileManager.ReadCustomMetadata(filePath, "workflow");
        var graph = JsonConvert.DeserializeObject<Graph>(workflow);

        if (graph != null)
        {
            var preset = ImportWorkflow(graph, filePath);
            GenerationManager.Instance.AddPreset(preset, false);
        }
    }
    public static void ImportNodesFromImage(byte[] image, string urlPath)
    {
        // COMFY UI IMAGE
        var workflow = FileManager.ReadCustomMetadata(image, "workflow");
        if (workflow != null)
        {
            var graph = JsonConvert.DeserializeObject<Graph>(workflow);

            if (graph != null)
            {
                var preset = ImportWorkflow(graph, urlPath);
                GenerationManager.Instance.AddPreset(preset, false);

            }

            return;
        }

        // CIVITAI IMAGE
        workflow = FileManager.ReadCustomMetadata(image, "parameters");
        if(workflow != null)
        {
            //TODO: manage civitai workflows
            return;
        }

    }


    //------------------------------------------------------------------------------------------------- IMPORT WORKFLOW
    public static PromptPreset ImportWorkflow(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);

        var settings = new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                // Logica para determinar si se debe ignorar el error
                if (args.ErrorContext.Error.Message.Contains("Cannot deserialize"))
                {
                    args.ErrorContext.Handled = true; // Ignora el error actual
                    Core.Output.Error(args.ErrorContext.Error.Message, $"Workflow, {args.CurrentObject}");

                }
            },

            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };
        settings.Converters.Add(new WidgetsValuesConverter());

        var graph = JsonConvert.DeserializeObject<Graph>(jsonString);

        var preset = ImportWorkflow(graph, filePath);

        return preset;
    }
    public class WidgetsValuesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<object>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<List<object>>();
            }
            else if (token.Type == JTokenType.Object)
            {
                // Aquí puedes decidir cómo manejar el objeto. Por ejemplo, convertirlo a un solo elemento de la lista o manejar cada propiedad del objeto de manera específica.
                // Por simplicidad, aquí se agrega el objeto entero a la lista.
                return new List<object> { token.ToObject<Dictionary<string, object>>() };
            }
            else //no widget
            {
                //throw new JsonSerializationException("Unexpected token type: " + token.Type.ToString());
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Asegúrate de que el valor no es nulo y es una lista de objetos
            if (value == null || !(value is List<object>))
            {
                return;
            }

            List<object> list = (List<object>)value;

            if (list == null || !list.Any())
                return;

            // Escribe el nombre de la propiedad del objeto JSON que contendrá la lista
            //writer.WritePropertyName("widgets_values");

            // Comienza a escribir la lista
            writer.WriteStartArray();

            // Itera a través de cada objeto en la lista y escribe su valor
            foreach (var item in list)
            {
                // Aquí, Json.NET maneja automáticamente el tipo de cada ítem (string, int, etc.)
                serializer.Serialize(writer, item);
            }

            // Finaliza la lista
            writer.WriteEndArray();
        }

    }




    //static void InstallMissingNodes()
    //{
    //    void okPressed()
    //    {
    //        ManualAPI.SelectedPreset.Refresh();
    //    }
    //    M_MessageBox.Show("you want to install missing nodes?", "Missing nodes", System.Windows.MessageBoxButton.OKCancel, okPressed);
    //}

    static PromptPreset ImportWorkflow(Graph graph, string filePath)
    {
        if (graph.nodes == null)
        {
            Core.Output.Log("Comfy Nodes not found", "Comfy");
            return null;
        }

        
        //set nodes
        PromptPreset preset = new();
        preset.Name = Namer.SetName(Path.GetFileNameWithoutExtension(filePath), GenerationManager.Instance.PromptPresets);
       
        AsignWorkflowToPreset(preset, graph);

        //if (!GenerationManager.isNodesRegistered)
        //{
        //    GenerationManager.OnNodesRegistered += () =>
        //    {
        //        SetPrompt(preset);
        //    };
        //}
        //else
        //    SetPrompt(preset);

        return preset;
    }
    public static void SetPrompt(PromptPreset preset)
    {
        if (preset.Prompt == null)
        {
            preset.Prompt = ManualAPI.SelectedPreset != null ? ManualAPI.SelectedPreset.Prompt : GenerationManager.Instance.Prompts.First();
            preset.AutomaticDrivers();
        }
    }

    public static PromptPreset WorkflowToPreset(Graph graph)
    {
        PromptPreset preset = new();
        AsignWorkflowToPreset(preset, graph);
        return preset;
    }
    public static void AsignWorkflowToPreset(PromptPreset preset, Graph graph)
    {
        preset.LatentNodes.Clear();
        preset.Groups.Clear();
        preset.LineConnections.Clear();
        preset.Drivers.Clear();

 

        //-----------instantiate nodes
        foreach (var node in graph.nodes) //------------------ IMPORT EVERY NODE
        {
            node.Paste(preset);
        }
        if (preset.HasErrors())
        {
            preset.Errors.GraphSaved = graph;
        }

        //-------- connect nodes links
        foreach (var link in graph.RealLinks)
        {
            var latentNodes = preset.LatentNodes;

            var originNode = latentNodes.FirstOrDefault(n => n.IdNode == link.OriginId);
            var targetNode = latentNodes.FirstOrDefault(n => n.IdNode == link.TargetId);

            if (originNode == null || targetNode == null)
                continue;

            var originSlot = originNode.Outputs.ElementAtOrDefault(link.OriginSlot);
            //var originSlot = originNode.Fields.FirstOrDefault(n => n.IdSlot == link.OriginSlot);

            var targetSlot = targetNode.Inputs.ElementAtOrDefault(link.TargetSlot);
            //if(targetSlot == null)
            //     targetSlot = targetNode.Inputs.FirstOrDefault(n => n.IdSlot == link.Id); //link.Id


            if (originSlot != null && targetSlot != null)
            {
                originSlot.AttachedNode = originNode;
                originNode.AttachedPreset = preset;

                targetSlot.AttachedNode = targetNode;
                targetNode.AttachedPreset = preset;

                originSlot.Connect(targetSlot);

            }

        }



        //-----------groups
        foreach (var group in graph.groups)
        {
            preset.Groups.Add(group);
        }




        //---------------config
        var manual_config = FileManager.JsonToClass<ManualConfig>(graph.extra, "manual_config");
        manual_config?.ApplyConfig(preset);

    }


    internal static void ExportWorkflowAPI(PromptPreset promptPreset, string filePath)
    {
        var jsonString = WorkflowToAPI(promptPreset);

        File.WriteAllText(filePath, jsonString);
        Core.Output.Log($"Workflow API {promptPreset.Name} saved", "Comfy");
    }
 //-------------------------------------------------------------------------------------------------------------------------------------------------- EXPORT WORKFLOW
    internal static void ExportWorkflow(PromptPreset promptPreset, string filePath)
    {
        promptPreset.Name = Path.GetFileNameWithoutExtension(filePath);

        var graph = LoadWorkflow(promptPreset);

        var jsonString = WorkflowJson(graph);    
        File.WriteAllText(filePath, jsonString);

        Core.Output.Log($"Workflow {promptPreset.Name} saved", "Comfy");
    }
    internal static string WorkflowJson(Graph graph, Formatting format = Formatting.Indented)
    {
        var settings = new JsonSerializerSettings
        {
            //TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new FileManager.IgnorePropertiesResolver("category", "description", "output_node"),
            Converters = new List<JsonConverter> { new StringEnumConverter() },
        };
        string jsonString = JsonConvert.SerializeObject(graph, format, settings);
        return jsonString;
    }



    public class ManualConfig
    {
        public string preset_name { get; set; }
        public Matrix canvas_position { get; set; }
   
        public Prompt prompt { get; set; }

        public List<Sdriver> drivers { get; set; } = [];

        public string promptNameId { get; set; }

        public bool Pinned { get; set; }
        public ManualConfig()
        {

        }

        //----------------------------------------------------------------------------------------------- serialize MANUAL CONFIG
        public ManualConfig(PromptPreset promptPreset, bool savePrompt = false)
        {
            canvas_position = promptPreset.CanvasMatrix;
            preset_name = promptPreset.Name;

            if (savePrompt)
                prompt = promptPreset.Prompt;
            else
                promptNameId = promptPreset.Prompt?.Name;
       
            drivers = promptPreset.Drivers.Select(d => Sdriver.Save(d)).ToList();



            Pinned = promptPreset.Pinned;
        }
 

        //deserialize
        public void ApplyConfig(PromptPreset promptPreset, bool savePrompt = false)
        {
            promptPreset.CanvasMatrix = canvas_position;
            promptPreset.Name = preset_name;

            if(savePrompt)
              promptPreset.Prompt = prompt;
            else
                promptPreset.Prompt = GenerationManager.Instance.Prompts.FirstOrDefault(p => p.Name == promptNameId);


            promptPreset.Pinned = Pinned;




            var isError = promptPreset.HasErrors();
            if (isError) return;

            //------- DRIVERS
            List<Driver> wdrivers = [];
            foreach (var d in drivers)
            {
                var newDriver = Sdriver.Load(d);
                var msg = ((string)d.target.Source).Split(';');
                var node = promptPreset.node(Convert.ToInt32(msg[1]));
                if (node != null)
                {
                    var nodeop = node.field(msg[2]);
                    if (nodeop is null)
                        Core.Output.Warning($"missing node field for driver, {msg[2]}", "Drivers");
                    else
                    {
                        newDriver.target = nodeop;
                        wdrivers.Add(newDriver);
                    }
                }
                else
                    Core.Output.Warning($"missing node for driver, {msg[1]}", "Drivers");
            }

            foreach (var d in wdrivers)
                d.Initialize(); //automatically adds to promptPreset.Drivers

        }
    }


    internal static Graph LoadWorkflow(PromptPreset promptPreset, bool savePrompt = false)
    {
        Graph graph = new()
        {
            version = 0.4,
            last_link_id = 1,
            last_node_id = 1,
        };
    
        //-- NODES
        foreach (var node in promptPreset.LatentNodes)
        {
            var n = Node.Copy(node);
            graph.nodes.Add(n);

        }
        //-- LINKS
        int linkid = 1;
        List<List<object>> linkList = new();
        foreach (var line in promptPreset.LineConnections)
        {
            int? TargetSlot = line.Input.IdSlot;
            if(TargetSlot != null)
                TargetSlot = line.Input.AttachedNode.Inputs.IndexOf(line.Input);


            int OriginSlot = line.Output.AttachedNode.Outputs.IndexOf(line.Output);

            List<object> l =
              [
                  line.LinkId, // Id
                  line.Output.AttachedNode.IdNode, // OriginId
                  OriginSlot, //line.Output.IdSlot, // OriginSlot
                  line.Input.AttachedNode.IdNode, // Target Id
                  TargetSlot, //line.Input.IdSlot,// TargetSlot
                  line.Type0// Type
              ];

            linkList.Add(l);
            linkid++;
        }
        graph.links = linkList;


        //--GROUPS
        foreach (var group in promptPreset.Groups)
        {
            graph.groups.Add(group);
        }

        //MANUAL CONFIG
        graph.extra["manual_config"] = new ManualConfig(promptPreset, savePrompt);

        return graph;
    }



    //===================================================================================================================================================
    //--------------------------------------------------------------------------------------------------------------------------------------------------- WORKFLOW TO API
    public static string WorkflowToAPI(PromptPreset prompt)
    {
        var apiData = new Dictionary<string, ComfyNodeAPI>();

        var manual_nodes = new List<ManualNode>();

        var latentNodes = prompt.LatentNodes.OrderBy(node => node.IdNode).ToList();
        foreach (var node in latentNodes)
        {
            
            var nodeData = new ComfyNodeAPI
            {
                class_type = node.NameType
            };
            nodeData._meta["title"] = node.Name;

            //MANUAL NODE
            if (node is ManualNode manualNode) // el problema es el output node, to api añadele eso
            {
                var n = manualNode.TO_API(nodeData);
                if(n != null)
                   apiData[node.IdNode.ToString()] = n;

                manual_nodes.Add(manualNode);
            }

            //COMFY NODE
            else
            {
                foreach (var input in node.Inputs)
                    nodeData.inputs[input.Name] = input.ToComyData();

                foreach (var input in node.WidgetFields)
                    nodeData.inputs[input.Name] = input.ToComyData();

                apiData[node.IdNode.ToString()] = nodeData;

            }

        }

        // OPPORTUNITY TO MODIFY THE LIST
        var apiList = new ApiList(apiData);
        foreach (var node in manual_nodes)
        {
            apiList.ManageFieldTypes(node);
            node.OnApiLoaded(apiList);
        }


        string json = JsonConvert.SerializeObject(apiData, Formatting.Indented);
        var a = json;
 
        return json;

    }


}



//------------------------------------------------------------------------------------------------------------------ MANAGE FIELD TIPES       MANUAL NODES      APILIST
public class ApiList(Dictionary<string, ComfyNodeAPI> nodes)
{
                     //"int" id
    public Dictionary<string, ComfyNodeAPI> Nodes = nodes;
    public override string ToString()
    {
        return $"ApiList {Nodes.Count}";
    }

    internal void ManageFieldTypes(ManualNode node)
    {
        if(node is NodeRenderArea)
        {

        }

        foreach (var field in node.Fields)
        {

            //input connection
            if (field.IsInputOrField())
            {
                ComfyNodeAPI apiNode = Nodes[$"{node.IdNode}"];
                var name = field.Name.ToLower().Replace(" ", "_");
                apiNode.Set(name, field.ToComyData());
                continue;
            }


            //output
            if ( field.Connections == null || !field.Connections.Any())
                continue;

            //one connection
            if(field.Connections.Count == 1)
            {
                ComfyNodeAPI apiNode = Nodes[$"{node.IdNode}"];
                var firstConn = field.Connections[0];
                if (field.RegisteredTypes.TryGetValue(firstConn.Type, out var result))
                {
                    var indata = InputData.New(firstConn.Type, result.FieldValueModified());
                    var name = field.Name.ToLower().Replace(" ", "_");
                    apiNode.Set(name, indata);
                }
                continue;
            }


            //multiple connections from different type


            //        fieldType   input connection []  
            Dictionary<string, object[]?> CachedTypes = new();

            int con = 0;
            foreach (var nodeop2 in field.Connections)
            {
                if(CachedTypes.TryGetValue(nodeop2.Type, out var cachedResult))
                {
                    if (cachedResult != null)
                    {
                      //  var nodeId = (string)cachedResult[0];
                     //   var slot = (int)cachedResult[1];
                        ChangeInputForAPI(nodeop2, cachedResult);
                    }
                    
                }
                // Intenta obtener el valor usando la clave nodeop2.Type del diccionario RegisteredTypes.
                //----------- ONLY OUTPUT FOR NOW
                else if (field.RegisteredTypes.TryGetValue(nodeop2.Type, out var result))
                {
                    if (result == null) //it's probably an input
                        continue;

                    var apiNode = Nodes[$"{node.IdNode}"];

                    // clone node and asign a new result
                    ComfyNodeAPI apiNodeClone = apiNode.Clone();

                    var indata = InputData.New(nodeop2.Type, result.FieldValueModified());
                    var r = InputData.New(nodeop2.Type, indata);
                    apiNodeClone.Set(field.Name.ToLower(), r);
                    string newIdNode;

                    //use first node at start
                    if (con == 0)
                    {
                        newIdNode = $"{node.IdNode}";
                        Nodes[newIdNode] = apiNodeClone;
                    }
                    else 
                        newIdNode = AddWithUniqueId(apiNodeClone);
                    

                    // asign connection to the cloned
                    var connect = InputConnection(newIdNode, field); //field is the original, but only need the slot_index so it's ok
                    ChangeInputForAPI(nodeop2, connect);

                    //save cache
                    CachedTypes[nodeop2.Type] = connect;
                }
                con++;
            }
            //Nodes.Remove(node.IdNode.ToString());
        }



    }



    //for Output
    object[]? InputConnection(string idNode, NodeOption nodeop)
    {
        if (!nodeop.IsInputOrField() && nodeop.IsConnected())
        {
            var connectedNode = nodeop.AttachedNode;
            var index = connectedNode.Outputs.IndexOf(nodeop);
            return InputConnection(idNode, index);
        }
        else
        {
            return null;
        }
    }
    object[] InputConnection(string idNode, int index)
    {
        return [idNode, index];
    }
    void ChangeInputForAPI(NodeOption nodeop2, object connection)
    {
        var connectedField = Nodes[nodeop2.AttachedNode.IdNode.ToString()];
        connectedField.Set(nodeop2.Name, connection);
    }


    string AddWithUniqueId(ComfyNodeAPI apiNode)
    {  
        int maxId = Nodes.Keys.Select(key => int.TryParse(key, out int id) ? id : 0).Max();
        int newId = maxId + 1;

        var i = newId.ToString();
        Nodes[i] = apiNode;

        return i;
    }







}

//----------------------------------------------------------------------------------------------------- WORKFLOW TO API


public class ComfyNodeAPI
{
    public Dictionary<string, object> inputs = new();
    public string class_type { get; set; }


    public Dictionary<string, object> _meta = new();
    public object Get(string fieldName)
    {
        object value;
        var success = inputs.TryGetValue(fieldName, out value);
        if (success)
            return value;
        else
            return null;
    }

    public void Set(string fieldName, object value)
    {
        inputs[fieldName] = value;
    }
    public bool Remove(string fieldName)
    {
        return inputs.Remove(fieldName);
    }

    public ComfyNodeAPI Clone()
    {
        // Serializa el objeto actual a JSON
        string json = JsonConvert.SerializeObject(this);
        // Deserializa el JSON a un nuevo objeto
        return JsonConvert.DeserializeObject<ComfyNodeAPI>(json);
    }

}





public partial class ComfyUINode : LatentNode
{
  
    public ComfyUINode(Node node)
    {
        LoadAllVariables(node);
    }

    //public void Refresh()
    //{
    //    if(ErrorNodeCache != null)
    //    {
    //        var updatedNode = GenerationManager.NewNodeByType(ErrorNodeCache.type) as ComfyUINode;
    //        updatedNode.LoadGeneral(ErrorNodeCache);
    //        AttachedPreset.AddNode(updatedNode);

    //        int c = 0;
    //        foreach (var field in Fields)
    //        {
    //            var field0 = updatedNode.Fields[c];

    //            if (field.IsConnected())
    //                field.Connection!.ForEach(con => field0.Connect(con));       

    //            c++;
    //        }

    //        AttachedPreset.RemoveNode(this);      
    //        ErrorNodeCache = null;
    //    }
    //}

    public void LoadGeneral(Node node)
    {
        Name = node.title ?? Name ?? node.type; //node.type;//Name;
        NameType = node.type;

        IdNode = node.id;

        float offsetPos = 1f;
        //  int heightnodeop = 0; //20;
        PositionGlobalX = node.pos[0] * offsetPos;
        PositionGlobalY = node.pos[1] * offsetPos;// - (node.outputs.Count * heightnodeop);

        SizeX = node.ActualSize[0];
        SizeY = node.ActualSize[1];

        var col = node.bgcolor?.ToColor();
      //  if (col == null)
        //    col = NodeColors.ColorByOutput(this);

        Color = col;
        IsCollapsed = node.flags.TryGetValueOrDefault<bool>("collapsed");  // node.flags.TryGetValue("collapsed", out object collapsedValue) && collapsedValue is bool collapsed && collapsed;


    }
    public void LoadAllVariables(Node node) //------------------------------------------------------- WHEN WORKFLOW IMPORTED
    {    
        LoadGeneral(node);
        LoadVariables(node);
    }
    public void LoadVariables(Node node)
    {

        // instantiate nodeoptions fields
        if (node.outputs != null)
            foreach (var output in node.outputs)
            {
                var nodeop = FindFieldOrNew(output.name, NodeOptionDirection.Output, output.type);
            }


        if (node.inputs != null)
            foreach (var input in node.inputs)
            {
                if (input.widget is not null) // is inputwidget
                {
                    var innode = ConvertToInput(input.widget.name);

                    if(innode == null)
                        FindFieldOrNew(input.widget.name, NodeOptionDirection.InputField, input.type);
                }
                else // normal
                {
                    FindFieldOrNew(input.name, NodeOptionDirection.Input, input.type);
                }
            }


        if (node.widgets_values != null)
        {
            //with value names
            if (node.widgets_values.Count == 1 && node.widgets_values[0] is Dictionary<string, object> dwidgets)
            {
                for (int i = 0; i < WidgetFields.Count; i++)
                {
                    // Verifica si dwidgets contiene una clave que coincide con el Name de WidgetFields[i].
                    if (dwidgets.TryGetValue(WidgetFields[i].Name, out object value))
                    {
                        // Asigna el valor encontrado a FieldValue de WidgetFields[i].
                        WidgetFields[i].FieldValue = value;
                    }
                }
            }
            //normal
            else
            {
                for (int i = 0; i < node.widgets_values.Count; i++)
                {

                    var widgetValue = node.widgets_values[i];

                    try
                    {
                        var actualWidget = WidgetFields[i];
                        actualWidget.FieldValue = DeserializeWidget(actualWidget, widgetValue);
                    }
                    catch
                    {
                        // AddField("error", "NULL", "error", new M_TextBox());
                    
                        string type = FieldTypes.GetTypeLabel(widgetValue);
                       
                        AddField($"widget {i}", type, widgetValue, FieldTypes.ElementByType(type));
                    }
                }
            }
        }

    }

    //---------------------------------------------------------------------------------------------------------- SERIALIZE AND DESERIALIZE WIDGET FIELDS
    public static object? SerializeWidget(NodeOption widget)
    {
        var value = widget.FieldValue;
        if (value is LayerBase l)
            return l.Name;
        else if (value is Item i)
            return i.Name;

        return widget.FieldValue;
    }


    public static object DeserializeWidget(NodeOption actualWidget, object value)
    {
        if (actualWidget.FieldValue is LayerBase l && value is string s)
        {
            if (s == "Selected")
                return new Item("Selected");

            return ManualAPI.FindLayer(s);
        }

        return value;
    }


}
//---------------------------- LOAD DEFAULTS, DEPRECATED PHYTON SCRIPT READ
public partial class ComfyUINode
{
    static string PYTHON_DLL_PATH => Path.Combine("C:\\Users\\YO\\AppData\\Local\\Programs\\Python\\Python310", "python310.dll");
    static dynamic INPUT_TYPES { get; set; }
    dynamic RETURN_TYPES { get; set; }

    public ComfyUINode()
    {
            
    }

    public static void LoadFromScript2()
    {
        if(Runtime.PythonDLL == null)
             Runtime.PythonDLL = PYTHON_DLL_PATH; //la gente necesitará phyton, o ponerle el dll en manual

        PythonEngine.Initialize();

    
        // custom_nodes folder
        string pythonScriptsPath = Comfy.PATH; //Path.Combine(ComfyUIClient.PATH, "custom_nodes");
        Environment.SetEnvironmentVariable("PYTHONPATH", pythonScriptsPath, EnvironmentVariableTarget.Process);


        // Carga tu módulo de Python
        dynamic comfyUI = Py.Import("custom_nodes.Manual");

        // Llama a la función que devuelve el JSON
        string nodeClassesJson = comfyUI.get_node_classes_info();

        PythonEngine.Shutdown();


        var node = new ComfyUINode();
        node.Load(nodeClassesJson);

        var nodeClasses = JsonConvert.DeserializeObject<Dictionary<string, object>>(nodeClassesJson);
    }
    public void Load(string jsonInfo)
    {
        Core.Output.Log(jsonInfo);
        
    }

    public static ComfyUINode? New(string name)
    {
        return GenerationManager.NewNodeByName<ComfyUINode>(name);
    }
}



//------------------------------------------------------------------------------------------ WORKFLOW IMPORT DESERIALIZE
public class Graph
{
    public int last_node_id { get; set; }
    public int last_link_id { get; set; }
    public List<Node> nodes { get; set; } = new();


    [JsonIgnore] public List<Link> RealLinks { get; set; } = new();

    private List<List<object>> _links;
    public List<List<object>> links
    {
        get
        {
            //List<List<object>> list = new();
            //foreach (var item in RealLinks)
            //{
            //    list.Add(new List<object> { item.Id, item.OriginId, item.OriginSlot, item.TargetId, item.TargetSlot, item.Type });
            //}
            //return list;
            return _links;
        }
        set
        {
            RealLinks.Clear();
            foreach (var item in value)
            {
                RealLinks.Add(new Link(item[0], item[1], item[2], item[3], item[4], item[5]));
            }
            _links = value;
        }
    }

    public List<GroupNode> groups { get; set; } = new();
    public Dictionary<string, object> config { get; set; } = new();
    public Dictionary<string, object> extra { get; set; } = new();
    public double version { get; set; } = 0.4;
}

public class Node
{
    public Node()
    {
            
    }

    public static ComfyUINode? New(string name)
    {
        return GenerationManager.NewNodeByName<ComfyUINode>(name);
    }

    public static List<ComfyUINode> DuplicateNodes(IEnumerable<LatentNode> nodes, PromptPreset preset)
    {
        var n = Copy(nodes);
        var Mnodes = PasteNodes(n, preset);
        return Mnodes;
    }

    public static List<ComfyUINode> PasteNodes(List<Node> nodes, PromptPreset preset)
    {
        var Mnodes = nodes.Select(node =>
        {
            var n = node.Paste(preset);
            n.IdNode = Namer.RandomId(preset.LatentNodes, (n, id) => n.IdNode == id, 1);
            return n;
        }).ToList();

        return Mnodes;
    }
    public static List<Node> Copy(IEnumerable<LatentNode> nodes)
    {
      return nodes.Select(mnode => Node.Copy(mnode)).ToList();
    }


    public ComfyUINode Paste(PromptPreset preset)
    {
        var node = this;

        var comfynode = (ComfyUINode)GenerationManager.NewNodeByType(node.type);

        if (comfynode != null)
        {
            comfynode.ui_isLoaded = true;

            comfynode.IsCustomSize = true;

            if (comfynode is ManualNode manualNode)
            {
                manualNode.LoadGeneral(node);
                manualNode.OnLoad(node);
            }
            else
                comfynode.LoadAllVariables(node);

            preset.AddNode(comfynode, false);
        }
        else //node not installed
        {
            comfynode = new ComfyUINode(node);
            comfynode.IsError = true;
            comfynode.IsCustomSize = true;
            //      comfynode.ErrorNodeCache = node;
            preset.AddNode(comfynode, false);

            preset.Errors ??= new();
            preset.Errors.MissingNodeTypes.Add(node.type);
            Core.Output.Warning($"missing node: {node.type}", "ComfyUINodes");

        }


        return comfynode;
    }

    public static Node Copy(LatentNode node)
    {

        Node n = new()
        {
            id = node.IdNode ?? 0,

            title = node.Name != node.NameType ? node.Name : null,
            type = node.NameType,

            pos = [node.PositionGlobalX,
                node.PositionGlobalY],
            ActualSize = [node.SizeX, node.SizeY],
            order = node.AttachedPreset.LatentNodes.IndexOf(node), //promptPreset.LatentNodes.IndexOf(node),
            bgcolor = node.Color.ToHex()

        };
        //properties
        n.properties["Node name for S&R"] = node.NameType;

        //flags
        if (node.IsCollapsed)
            n.flags["collapsed"] = true;


        //OUTPUTS
        foreach (var output in node.Outputs)
        {
            //Output o = new() { name = output.Name, type = output.Type, slot_index = output.IdSlot ?? -1, links = output.ConnectionOutput.Select(nodeOption => nodeOption.IdSlot ?? -1).ToList() };
            var connection = output.LineConnect?.Select(ou => ou.LinkId).ToList();
            if (connection != null && !connection.Any())
                connection = null;


            // var slot = output.AttachedNode.Inputs.IndexOf(output);
            int? slot = connection != null ? output.AttachedNode.Outputs.IndexOf(output) : null;

            Output o = new() { name = output.Name, type = output.Type, slot_index = slot, links = connection };
            n.outputs.Add(o);
        }

        //INPUTS       
        foreach (var input in node.Inputs)
        {
            //Input i = new() { name = input.Name, type = input.Type, link = input.IdSlot };
            var connection = input.LineConnect?.FirstOrDefault()?.LinkId;

            if (input.Direction == NodeOptionDirection.InputField)
            {
                // var connection = widget.LineConnect?.FirstOrDefault()?.LinkId;
                var widget = input;

                if (input.IsConnected())
                {
                    Input i = new() { name = widget.Name, type = widget.Type, link = connection, widget = new Widget(widget.Name), slot_index = node.Fields.IndexOf(widget) };
                    n.inputs.Add(i);
                }
                else
                {
                    var value = ComfyUINode.SerializeWidget(widget);
                    n.widgets_values.Add(value);
                }
            }
            else
            {

                int? slot = connection != null ? input.AttachedNode.Inputs.IndexOf(input) : null;

                Input i = new() { name = input.Name, type = input.Type, slot_index = null, link = connection };
                n.inputs.Add(i);
            }
        }

        //WIDGET FIELDS
        foreach (var widget in node.WidgetFields)
        {
            var value = ComfyUINode.SerializeWidget(widget);
            n.widgets_values.Add(value);
        }


        if (!n.outputs.Any())
            n.outputs = null;
        if (!n.inputs.Any())
            n.inputs = null;
        if (!n.widgets_values.Any())
            n.widgets_values = null;


        return n;
    }

    public Node(dynamic node)
    {
        if (node.Property("display_name") != null)
            title = node.display_name;

        category = node.category;
        description = node.description;
        output_node = node.output_node;
        type = node.name;

        if (node.Property("color") != null)
            color = node.color;

        if (node.Property("bgcolor") != null)
            bgcolor = node.bgcolor;

        foreach (var output in node.output)
        {
            string output_name = output.Value;
            string output_type = output.Value;
            outputs.Add(new Output() { name = output_name, type = output_type});
        }

        var pro = node.properties;
        properties = node.properties;
    }


    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
    public string color { get; set; }

    [JsonProperty("bgcolor", NullValueHandling = NullValueHandling.Ignore)]
    public string? bgcolor { get; set; }

    public string category { get; set; }
    public string description { get; set; }
    public bool output_node { get; set; }


    public int id { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? title { get; set; } = null;
    public string type { get; set; }
    public List<float> pos { get; set; }

    [JsonProperty("size")]
    public JToken JsonSize
    {
        get
        {
            if (ActualSize.Any())
            {
                var jObject = new JObject
                {
                    ["0"] = ActualSize[0],
                    ["1"] = ActualSize[1]
                };
                return jObject;
            }
            else
            {
                return new JObject { ["0"] = 0, ["1"] = 0 };
            }
        }
        set
        {
            ActualSize.Clear();
            if (value is JObject)
            {
                var list = value.ToObject<Dictionary<string, float>>();
                ActualSize.Add(list["0"]);
                ActualSize.Add(list["1"]);
            }
            else if (value is JArray)
            {
               var list = value.ToObject<List<float>>();
                ActualSize.Add(list[0]);
                ActualSize.Add(list[1]);
            }
        }
    }

    [JsonIgnore] public List<float> ActualSize { get; set; } = new();

    public Dictionary<string, object> flags { get; set; } = new();
    public int order { get; set; } = 1;
    public int mode { get; set; } = 0;


    [JsonProperty("inputs", NullValueHandling = NullValueHandling.Ignore)]
    public List<Input> inputs { get; set; } = new();


    [JsonProperty("outputs", NullValueHandling = NullValueHandling.Ignore)]
    public List<Output> outputs { get; set; } = new();

    public Dictionary<string, string> properties { get; set; } = new();


    [JsonConverter(typeof(WidgetsValuesConverter))]
    [JsonProperty("widgets_values", NullValueHandling = NullValueHandling.Ignore)]
    public List<object>? widgets_values { get; set; } = new();


    public override string ToString()
    {
        return $"{type}, inputs:{inputs.Count}, outputs{outputs.Count}";
    }
}



public class Output
{
    
    public string name { get; set; }
    public string type { get; set; }
    public List<int> links { get; set; } = new();

    [JsonProperty("slot_index", NullValueHandling = NullValueHandling.Ignore)]
    public int? slot_index { get; set; }

    public override string ToString()
    {
        return $"{name}, type:{type}";
    }
}
public class Input
{
    public string name { get; set; }
    public string type { get; set; }
    public int? link { get; set; }

    [JsonProperty("widget", NullValueHandling = NullValueHandling.Ignore)]
    public Widget? widget { get; set; }
    [JsonProperty("slot_index", NullValueHandling = NullValueHandling.Ignore)]
    public int? slot_index { get; set; }

    public override string ToString()
    {
        return $"{name}, type:{type}";
    }
}

public class Widget
{
    public Widget(string name)
    {
        this.name = name;
    }

    public string name { get; set; }
}

public class Link
{
    public int Id { get; set; }
    public int OriginId { get; set; }
    public int OriginSlot { get; set; }
    public int TargetId { get; set; }
    public int TargetSlot { get; set; }
    public string Type { get; set; }
    public Link()
    {
    }
    public Link(object id, object originid, object originslot, object targetid, object targetslot, object type)
    {
        Id = Convert.ToInt32(id);
        OriginId = Convert.ToInt32(originid);
        OriginSlot = Convert.ToInt32(originslot);
        TargetId = Convert.ToInt32(targetid);
        TargetSlot = Convert.ToInt32(targetslot);
        Type = type.ToString();
    }

    public override string ToString()
    {
        return $"id: {Id} | OriginId: {OriginId} | OriginSlot: {OriginSlot} | TargetId: {TargetId} | TargetSlot: {TargetSlot} | Type: {Type}";
    }
}






























//--------------------------------------------------------------------------------------------------------------------------------------------- MANUAL NODES
public static partial class ManualNodes
{
    public static Dictionary<string, Func<ManualNode>> RegisteredManualNodes { get; set; } = new();
    public static void RegisterManualNode(string name, Func<ManualNode> node)
    {
        RegisteredManualNodes.Add(name, node);
    }
    /// <summary>
    /// params of Tuples, example: ("MyNode", () => new MyNode())
    /// </summary>
    /// <param name="nodeData"></param>
    public static void RegisterNode(params (string NameType, Func<ManualNode> NodeFunc)[] nodeData)
    {
        RegisteredManualNodes.Clear();
        foreach (var (name, nodeFunc) in nodeData)
        {
            RegisteredManualNodes.Add(name, nodeFunc);
        }
    }

    public static ManualNode GetNode(dynamic d)
    {
        var data = new ComfyNodeData(d);

        var a = RegisteredManualNodes.FirstOrDefault(n => n.Key == data.name);

        if (a.Value == null)
        {
            throw new ArgumentNullException("RegisteredManualNodes", "RegisterManualNode(string <name>,...) needs to be the same as in phyton NODE_CLASS_MAPPINGS");
        }

        var node = a.Value();
        node.SetData(data);
        return node;

    }

}

public class ComfyNodeData
{
    public string name { get; set; }
    public string display_name { get; set; }
    public string description { get; set; }
    public string category { get; set; }
    public bool output_node { get; set; }


    public Input input { get; set; } = new();
    public List<string> output { get; set; } = new();

    public ComfyNodeData(JObject node)
    {
        name = node["name"]?.ToString();
        display_name = node["display_name"]?.ToString();
        description = node["description"]?.ToString();
        category = node["category"]?.ToString();
        output_node = (bool)(node["output_node"] ?? false);

        if (node["input"]?["required"] != null)
        {
            input.required = node["input"]["required"].ToObject<Dictionary<string, List<object>>>();
        }

        if (node["output"] != null)
        {
            output = node["output"].ToObject<List<string>>();
        }
    }
    public class Input
    {
        public Dictionary<string, List<object>> required { get; set; } = new Dictionary<string, List<object>>();
    }
    public class Field
    {
        public Dictionary<string, object?> fields { get; set; } = new();
    }

    public override string ToString()
    {
        return $"{name}";
    }

}

/// <summary>
///  comfy node translated to Manual
/// </summary>
public abstract class ManualNode : ComfyUINode
{
    /// <summary>
    /// the original comfy node data;
    /// </summary>
    public ComfyNodeData DATA { get; private set; }
    public Node DATAEXP { get; private set; }
    internal void SetData(ComfyNodeData d)
    {
        DATA = d;
    }
    internal void SetDataExp(Node node)
    {
        DATAEXP = node;
    }

    /// <summary>
    /// when importing or simply loading the parameters, override it will disable LoadVariables(node) automatically
    /// </summary>
    /// <param name="data"></param>
    public virtual void OnLoad(Node data)
    {
        LoadVariables(data);
    }

    /// <summary>
    /// when send to server. opportunity to convert manual node in to comfy node, usually you can return data with modifications
    /// </summary>
    /// <returns></returns>
    public virtual ComfyNodeAPI TO_API(ComfyNodeAPI data)
    {
        return data;
    }


    internal virtual void OnApiLoaded(ApiList a)
    {

    }

}
public abstract class ManualNodeOutput : ManualNode
{
    public virtual void ON_START(GeneratedImage genimg)
    {

    }

    /// <summary>
    /// when finalize generation, determine what to do with the result
    /// </summary>
    public abstract void ON_OUTPUT(GeneratedImage genimg);

    /// <summary>
    /// send every preview image, step by step, do whatever with that
    /// </summary>
    /// <param name="imageR"></param>
    public virtual void ON_PREVIEW(GeneratedImage genimg)
    {
        
    }

}

public class InputData(string _type, object _value)
{
    public string type { get; set; } = _type;
    public object value { get; set; } = _value;

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
    public static string ToString(string type, object value)
    {
        return new InputData(type, value).ToJson();
    }
    public string ToJson()
    {
        return this.ToString();
    }

    internal static InputData New(string type, object value)
    {
        return new InputData(type, value);
    }
}






public class WrapperNode : LatentNode
{

    public WrapperNode()
    {
        Name = "Wrapper Node";
        NameType = "M_Wrapper";

        PropertyChanged += WrapperNode_PropertyChanged;
    }

    public WrapperNode(IEnumerable<LatentNode> nodes)
    {
        Name = "Wrapper Node";
        NameType = "M_Wrapper";

        if (nodes.Any())
            Position = AppModel.GetAveragePosition(nodes);

        foreach (var node in nodes)
            WrapNode(node);

        PropertyChanged += WrapperNode_PropertyChanged;
    }

    private void WrapperNode_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName == "PositionGlobalX" || e.PropertyName == "PositionGlobalY")
        {
            UpdateWrapPos();
        }
    }
    public void UpdateWrapPos()
    {
        foreach (var node in WrappedNodes)
            node.UpdatePosition();
    }

    public override void Dispose()
    {
        base.Dispose();
        PropertyChanged -= WrapperNode_PropertyChanged;

        foreach (var node in WrappedNodes.ToList())
            RemoveNode(node);

    }

    public ObservableCollection<WrapField> WrapFields { get; set; } = new();
    public void WrapField(NodeOption nodeOption, string displayName)
    {
        WrapFields.Add(new WrapField(nodeOption, displayName));
    }




    public ObservableCollection<LatentNode> WrappedNodes { get; set; } = new();

    public void WrapNode(LatentNode node)
    {
        WrappedNodes.Add(node);
        node.ParentWrap = this;
        node.IsVisible = false;
    }
    public void RemoveNode(LatentNode node)
    {
        WrappedNodes.Remove(node);
        node.ParentWrap = null;
        node.IsVisible = true;
    }

    internal static WrapperNode AddWrapper(PromptPreset selectedPreset, SelectionCollection<LatentNode> nodes)
    {
        var wrapper = new WrapperNode(nodes);
        selectedPreset.AddNode(wrapper);
        return wrapper;
    }
    
}

public record WrapField(NodeOption basedNodeOp, string DisplayName);










//--------------------------------------------------------------------------------------------------------------------------------------------- UTILS

public class RerouteNode : ManualNode
{
    NodeOption In, Out;
    public RerouteNode()
    {
        Name = "";
        NameType = "Reroute";

        In = AddInput("", FieldTypes.ANY);
        Out = AddOutput("", FieldTypes.ANY);
    }
}


public class PrimitiveNode : ManualNode
{
    public NodeOption Result, Widget;
    public PrimitiveNode()
    {
        Name = "Primitive";
        NameType = "PrimitiveNode";

        Result = AddOutput("connect to widget input", FieldTypes.ANY);
        Widget = AddField("Widget", null, null, typeof(M_NumberBox));

        Result.OnConnectionChanged += Result_OnConnectionChanged;
    }

    public override void OnLoad(Node data)
    {
       Widget.FieldValue = data.widgets_values[0];
    }

    public override ComfyNodeAPI TO_API(ComfyNodeAPI data)
    {
        return null;
    }

    internal override void OnApiLoaded(ApiList a)
    {
        Result.ApiModifyConnections(a, Widget.FieldValue);
    }



    private void Result_OnConnectionChanged(NodeOption? connection)
    {
        if (connection != null)
        {
            Result.Name = connection.Type;

            Widget.Name = connection.Name;
            Widget.Type = Result.Type = connection.Type;

            Widget.FieldElement = connection.FieldElement;
            UINode?.Refresh();

        }
        else
        {
            Result.Name = "connect to widget input";

            Widget.Name = "Widget";
            Widget.Type = Result.Type = null;
            Widget.FieldElement = null;
        }

    }

}



public class NodePromptInput : ManualNode
{
    [JsonIgnore] public NodeOption Out;
    NodeOption ident;

    public string Identify = "MODEL";
    public NodePromptInput()
    {
        Name = "Prompt Input";
        NameType = "PromptInput";
        Color = NodeColors.ColorByName(NodeColors.Black);

        Out = AddOutput("Input", FieldTypes.ANY);
        ident = AddField("Identify", FieldTypes.ANY, "MODEL", new M_TextBox());
    }
}


public class NodePromptOutput : ManualNode
{
    [JsonIgnore] public NodeOption In;
    NodeOption ident;

    public string Identify = "MODEL";
    public NodePromptOutput()
    {
        Name = "Prompt Output";
        NameType = "PromptOutput";
        Color = NodeColors.ColorByName(NodeColors.Black);

        In = AddInput("Output", FieldTypes.ANY);
        ident = AddField("Identify", FieldTypes.ANY, "MODEL", new M_TextBox());
    }
}