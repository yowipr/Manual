using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.API;
using Manual.Core.Nodes.ComfyUI;
using Manual.Editors.Displays;
using Manual.MUI;
using Manual.Objects;
using Manual.Objects.UI;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;


//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Manual.Core.Nodes;



//--------------------------------------------------------------------------- QUEUE

public partial class GenerationManager
{
    public static event Event OnStartGenerating;

    public static event Event OnGenerating;
    public static void InvokeOnGenerating()
    {
        OnGenerating?.Invoke();
    }

    public static event Event<GeneratedImage> OnGenerated;

    [ObservableProperty] [property: JsonIgnore] bool isProcessingImages = false;
    [JsonIgnore] public bool isInterrupt = false;


  //  public ObservableCollection<GeneratedImage> AllImages { get; set; } = new();
    [JsonIgnore] public ObservableCollection<GeneratedImage> Queue { get; set; } = new();
    [JsonIgnore] public ObservableCollection<GeneratedImage> Generated { get; set; } = new();
    [ObservableProperty] [property: JsonIgnore] GeneratedImage? currentGeneratingImage = null;
    partial void OnCurrentGeneratingImageChanged(GeneratedImage? oldValue, GeneratedImage? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsQueue = false;
            oldValue.IsGenerating = false;
        }

        if (newValue is not null)
        {
            newValue.IsGenerating = true;
        }
    }


    [ObservableProperty] [property: JsonIgnore] float progress = 0;

    [NotifyPropertyChangedFor(nameof(Progress))]
    [ObservableProperty] [property: JsonIgnore] public int totalSteps = 0;

    [NotifyPropertyChangedFor(nameof(Progress))]
    [ObservableProperty] [property: JsonIgnore] int stepsCompleted = 0;


 
    public int CurrentBakedFrame()
    {
        if (CurrentGeneratingImage.TargetLayer.CurrentBakingKeyframe != null)
            return CurrentGeneratingImage.TargetLayer.CurrentBakingKeyframe.Frame;
        else return ManualAPI.CurrentFrame;
    }
    /// <summary>
    /// when is selected the target layer, and current frame is the keyframe.frame
    /// </summary>
    /// <param name="targetLayer"></param>
    /// <param name="bakeKeyframe"></param>
    /// <returns></returns>
    public static bool IsTrackingBakeKeyframe(LayerBase targetLayer, Keyframe bakeKeyframe)
    {
        return targetLayer._Animation.CurrentFrame == bakeKeyframe.Frame;
    }
    public static bool IsTrackingBakeKeyframe()
    {
        var targetLayer = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;
        var bakingKeyframe = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer.CurrentBakingKeyframe;

        if (bakingKeyframe == null || targetLayer == null)
            return false;

        return IsTrackingBakeKeyframe(targetLayer, bakingKeyframe);

    }


    public GeneratedImage AddToQueue(PromptPreset preset)
    {
        var genimg = new GeneratedImage(ManualAPI.SelectedLayer, preset);
        AddToQueue(genimg);
        return genimg;
    }
    public GeneratedImage AddToQueue()
    {
        var genimg = new GeneratedImage(ManualAPI.SelectedLayer, ManualAPI.SelectedPreset);
        AddToQueue(genimg);
        return genimg;
    }
    public void AddToQueue(GeneratedImage imagePreset)
    {
        Queue.Insert(0, imagePreset);
        if(imagePreset.IsGenerating == false)
         imagePreset.IsQueue = true;
    }
    public void RemoveFromQueue(GeneratedImage imagePreset)
    {
        Queue.Remove(imagePreset);
        imagePreset.IsQueue = false;
        imagePreset.IsGenerating = false;
    }
    public void PutOnNext(GeneratedImage imagePreset)
    {
        Queue.Move(Queue.IndexOf(imagePreset), Queue.IndexOf(Queue.Last()) - 1);
    }


    internal void CancelAllQueue()
    {
        var queueOld = Queue.ToList();
        foreach (var genimg in queueOld)
        {
            RemoveFromQueue(genimg);
        }
    }

    internal static void GenerateInBatch(SelectionCollection<Keyframe> selectedKeyframes)
    {
        var oldFrame = ManualAPI.Animation.CurrentFrame;
        foreach (var keyframe in selectedKeyframes)
        {
            ManualAPI.Animation.CurrentFrame = keyframe.Frame;
            var genimg = new GeneratedImage(ManualAPI.SelectedLayer, ManualAPI.SelectedPreset);
            genimg.BakeKeyframes.Add(keyframe);


            genimg.OnGenerated += OnGenerated;
            void OnGenerated()
            {

                if (ManualAPI.Animation.CurrentFrame == keyframe.Frame)
                {
                    var index = selectedKeyframes.IndexOf(keyframe);
                    var newFrame = selectedKeyframes.ElementAtOrDefault(index + 1);
                    if (newFrame != null)
                        ManualAPI.Animation.CurrentFrame = newFrame.Frame;
                }

                genimg.OnGenerated -= OnGenerated;
            }



            Generate(genimg);
        }
        ManualAPI.Animation.CurrentFrame = oldFrame;
    }



    public static void Generate() // -------------------------------------------------------- GENERATE MAIN
    {
        Generate(ManualAPI.SelectedPreset);
    }


    public static void Generate(PromptPreset preset)
    {
        interruptions = 0;
        //DISABLEADO
        //AppModel.mainW.CheckServerStatus();

        var genimg = new GeneratedImage(ManualAPI.SelectedLayer, preset);
        Generate(genimg);
 
    }
    public static void Generate(GeneratedImage genimg)
    {
        if (Instance.Realtime)
            Instance.GenerateRealtime(genimg);
        else
            Instance.GenerateQueue(genimg);
    }

    private CancellationTokenSource debounceCts = new CancellationTokenSource();
    private async void GenerateRealtime(GeneratedImage preset)
    {
        // Cancela cualquier operación de espera existente
        debounceCts.Cancel();
        debounceCts = new CancellationTokenSource();

        try
        {
            // Espera durante un tiempo de "debounce" de 1 segundo
            int seconds = Convert.ToInt32(Settings.instance.RealtimeDelaySeconds * 1_000);
            await Task.Delay(seconds, debounceCts.Token);
            // Si no se han realizado más llamadas a GenerateRealtime en el último segundo, ejecuta GenerateQueue
            if (!ActionHistory.IsOnAction && !IsProcessingImages)
            {
                GenerateQueue(preset);
            }
        }
        catch (TaskCanceledException)
        {
            // La espera fue cancelada, lo que significa que se hizo otra llamada a GenerateRealtime
            // No hagas nada aquí, la nueva llamada manejará la ejecución
        }
    }

    private async void GenerateQueue(GeneratedImage preset) // Main, add to queue
    {
        AddToQueue(preset);
        isInterrupt = false;

        if (IsProcessingImages || isInterrupt)
            return;

        TotalSteps = 0;
        StepsCompleted = 0;
        Progress = 0;

        TotalSteps++;
        IsProcessingImages = true;

        OnStartGenerating?.Invoke();
        while (Queue.Count > 0 && !isInterrupt)
        {
            Stopwatch crono = new();
            crono.Start();
            try
            {
                CurrentGeneratingImage = Queue.Last();
                CurrentGeneratingImage.Preset.Prompt.ChangeSeed();
                await CurrentGeneratingImage.GenerateImage(); // selectedPreset.Generate

                AppModel.mainW.SetMessage($"Queue.Count is {Queue.Count}");
                crono.Stop();
                Output.Log($"Prompt executed in {crono.Elapsed.ToReadableTime()}");
            }
            catch (Exception ex)
            {

                Output.Log($"{ex.Message} \n {ex.Source} \n {ex.StackTrace}" , "GenerateImage");
                GenerationManager.Interrupt();
                AppModel.mainW.CheckServerStatus();
               //EndAllGenerations();
            }
            finally
            {
                //finalize one queue
                if (CurrentGeneratingImage != null)
                {
                    RemoveFromQueue(CurrentGeneratingImage);
                    Generated.Insert(0, CurrentGeneratingImage);
                    CurrentGeneratingImage.Finish();

                    OnGenerated?.Invoke(CurrentGeneratingImage);
                }
                StepsCompleted++;
                Progress = StepsCompleted / TotalSteps;
                AppModel.mainW.SetProgress(Progress);
                crono.Stop();
            }
        }

        EndAllGenerations();
      
    }

    //----------------------------------------------------------------------------------------------------- FINISH GENERATION DONE
    void EndAllGenerations()
    {
        //finalize
        Queue.Clear();

        IsProcessingImages = false;
        CurrentGeneratingImage = null;
        isInterrupt = false;

        if (!Instance.Realtime)
        {
            AppModel.mainW.FinishedProgress();
            AppModel.mainW.SetAlert("Finished Batch!");
        }
        else
        {
            AppModel.mainW.StopProgress();
        }
    }
    static int interruptions = 0;
    public bool interrupting = false;
    public static void Interrupt()
    {
        Instance.CurrentGeneratingImage?.Interrupted();

        interruptions++;
        if (interruptions > 2)
            ForceInterruption();

        if (Instance.interrupting)
        {
            //cancel all
            Instance.EndAllGenerations();
            return;
        }

        Instance.interrupting = true;
        if (Instance.CurrentGeneratingImage?.Preset.GetOutputNode() is ComfyUINode)
        {
            Comfy.Interrupt();
        }
        else
        {
            Instance.isInterrupt = true;
        }

    }

    public static void ForceInterruption()
    {
        Comfy.ForceInterrupt();
    }





    //------------------------------------------------------------------------------------------------------------------------------- PREDEFINED
    [RelayCommand]
    [property: JsonIgnore]
    public void Execute_RemoveBackground()
    {
        Execute_Template("remove background");
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void Execute_Template(string templateName)
    {
        var preset = FindPresetOrTemplate(templateName);
        if(preset != null)
            Generate(preset);
        else
            Output.Log($"Preset template not found {templateName}", "Execute Template");    
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void Execute_FixFace()
    {
        Execute_Template("fix face");
    }
    public static PromptPreset? FindPresetOrTemplate(string presetTemplateName, bool automaticDrivers = false)
    {        
        PromptPreset TargetPreset = ManualAPI.FindPreset(presetTemplateName);
        if (TargetPreset == null)
        {
            TargetPreset = PromptPreset.FromTemplate(presetTemplateName, automaticDrivers);
            TargetPreset.Block = false;

            if (TargetPreset != null)
                GenerationManager.Instance.AddPreset(TargetPreset, false, false);
        }

        return TargetPreset;
    }


    /// <summary>
    /// Imports Workflow or Prompt
    /// </summary>
    /// <param name="e"></param>
    /// <param name="canvas"></param>
    /// <returns></returns>
    public async static Task ImportDropWorkflow(DragEventArgs e, CanvasAreaControl? canvas = null)
    {

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Obtener los archivos arrastrados
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            // Procesar cada archivo
            foreach (string filePath in files)
            {
                //LEGACY
                if (System.IO.Path.GetExtension(filePath).Equals(".pp", StringComparison.OrdinalIgnoreCase))
                {
                    // Llamar al método si la extensión es .pp
                    GenerationManager.ImportPromptPreset(filePath);
                }

                //JSON
                else if (System.IO.Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase)) // comfyui
                {
                    GenerationManager.ImportComfyUINodes(filePath);
                }
                //PNG
                else if (System.IO.Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase)) // comfyui
                {
                    if (FileManager.ReadCustomMetadata(filePath, "prompt") is string p)
                        Prompt.V_ImportPrompt(filePath);
                    else if (FileManager.ReadCustomMetadata(filePath, "workflow") is string p0)
                        Comfy.ImportNodesFromImage(filePath);
                }
                //PROMPT
                else if (filePath.EndsWith(".prompt", StringComparison.OrdinalIgnoreCase)) //prompt
                {
                    Prompt.V_ImportPrompt(filePath);
                }
            }

        }
        else if (e.Data.GetDataPresent(DataFormats.StringFormat)) // IMAGE FROM WEB
        {
            // Manejar imágenes arrastradas desde un navegador
            string url = (string)e.Data.GetData(DataFormats.StringFormat);
            if (await WebManager.ImageUrlFromWeb(url) is string actualUrl)
            {
                await WorkflowFromImage(actualUrl);
            }
        }
        else if (e.Data.GetDataPresent(typeof(object)))
        {
            var obj = e.Data.GetData(typeof(object));
            Point mousePos;

            var m = e.GetPosition(canvas);
            mousePos = canvas.mousePosition(m);

            var hit = canvas.HitTest(m);
            if (hit == canvas)
            {
                if (obj is LayerBase layer)
                {
                    var node = GenerationManager.NewNodeByType<NodeLayer>("M_Layer");
                    if (node != null)
                    {
                        if (ManualAPI.SelectedLayer == layer)
                            node.SetSelected();
                        else
                            node.SetLayer(layer);

                        //node.Name = $"Layer ({layer.Name})";
                        ManualAPI.SelectedPreset.AddNode(node, mousePos);
                    }
                }
            }

        }


    }
    static async Task WorkflowFromImage(string url)
    {
        // Descargar la imagen
        byte[] imageBytes = await WebManager.DownloadImageByte(url);

        Comfy.ImportNodesFromImage(imageBytes, url);
    }



    public static void ShowMessageRequireCloud()
    {
        System.Media.SystemSounds.Beep.Play();
        var mbox = M_MessageBox.Show($"Your current plan is free. To give you access to Manual Cloud we need money, and money doesn't grow on trees!",
            "Manual Cloud",
            System.Windows.MessageBoxButton.OK,
            okPressed: () =>
            {
                WebManager.OPEN(WebManager.Combine(Constants.WebURL, "pricing"));
            },
            "Upgrade to PRO"
            );
    }


}



//--------------------------------------------------------------------------------------------------------------------------------- GENERATED IMAGES
public partial class GeneratedImage : ObservableObject
{
    public event Event OnGenerated;


    //UI
    internal bool ui_isLoaded = false;

    [ObservableProperty] [property: JsonIgnore] bool isError;

    [ObservableProperty] [property: JsonIgnore] bool isQueue;
    [ObservableProperty] [property: JsonIgnore] bool isGenerating;

    [ObservableProperty][property: JsonIgnore] bool isGenerated;
    partial void OnIsGeneratingChanged(bool value)
    {
        if (value)
        {
            //TargetLayer.Enabled = false;
            TargetLayer.ShotParent.IsGenerating = true;
        }
        else
        {
            //TargetLayer.Enabled = true;
            TargetLayer.ShotParent.IsGenerating = false;
            TargetLayer.ShotParent.Progress = 0;

            Progress = 0;
        }
    }

    /// <summary>
    /// from 0 to 100
    /// </summary>
    [ObservableProperty] float progress = 0;
    partial void OnProgressChanged(float value)
    {
        TargetLayer.ShotParent.Progress = value;
    }

    public byte[] OriginalImage { get; set; }

    [ObservableProperty] public SKBitmap previewImage;
    partial void OnPreviewImageChanged(SKBitmap value) // manage preview
    {
        if(TargetLayer.CurrentBakingKeyframe != null)
            TargetLayer.CurrentBakingKeyframe.Value = value;

        if (IsGenerating && GenerationManager.IsTrackingBakeKeyframe())
        {
            TargetLayer.Image = value;
        }

        Shot.UpdateCurrentRender();
    }


    public LayerBase TargetLayer { get;set; }
    public LayerBase TargetMask { get; set; }
    public int MaskDilate { get; set; } = 0;

    public PromptPreset Preset { get; set; }
    public SKBitmap[] Results { get; set; }
    public NodeBase OutNode { get; set; }
    public BoundingBox BoundingBox { get; set; }
    public int LocalFrame { get; set; }

    public List<Keyframe> BakeKeyframes { get; set; } = new();

    public GeneratedImage(LayerBase targetLayer, PromptPreset preset)
    {
        TargetLayer = targetLayer;
        TargetLayer.AttachedImageGeneration = this;
        Preset = preset;
        PreviewImage = targetLayer.Image;
    }
    public GeneratedImage(PromptPreset preset)
    {
        TargetLayer = ManualAPI.SelectedLayer;
        TargetLayer.AttachedImageGeneration = this;
        Preset = preset;
    }


    //--------------------------------- PRINCIPAL
    public async Task GenerateImage()
    {
        //if (Settings.instance.IsCloud)
        //{
        //    await Settings.instance.SetCloudURL();
        //}
        LocalFrame = TargetLayer._Animation.CurrentFrame;
        var onode = Preset.GetOutputNode();
        OutNode = onode;

        var bbox = Preset.GetBoundingBox();
        BoundingBox = bbox;

        if(onode is ManualNodeOutput nout && onode is not IOutputNode)
        {
            nout.ON_START(this);
        }


        await Preset.Generate(Preset);

    }

    public void Finish()
    {
        Progress = 0;
        IsGenerated = true;

        if(IsError)
          TargetLayer?.PreviewValue.EndPreview(false);
        else
        {
            if (Preset.Prompt.Thumbnail == null)
                Preset.Prompt.SetThumbnail(this.Results.First());
        }
        OnGenerated?.Invoke();
    }
    public void Finish(SKBitmap result)
    {
        PreviewImage = result;
        Finish();
    }

    public static SKBitmap CombineGenerated(SKBitmap image)
    {
        var boundingBox = OutputNode.GetBoundingBoxs();
        var preview = image;

        var targetLayer = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;
        var bakingKeyframe = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer.CurrentBakingKeyframe;

        if (bakingKeyframe is NotNullAttribute)
            return RendGPU.Inpaint(targetLayer, boundingBox, preview, bakingKeyframe);
        else
            return RendGPU.Inpaint(targetLayer, boundingBox, preview);
    }
    public static void SetPreview(SKBitmap image)
    {
        GenerationManager.Instance.CurrentGeneratingImage.PreviewImage = CombineGenerated(image);
    }

    internal void Interrupted()
    {
        IsError = true;
    }
}




//--------------------------------------------------------------------------------------------------------------------------------- PROMPT PRESET
public partial class PromptPreset : ObservableObject, INamable, IId, IMultiDrivable
{
    

    [ObservableProperty] [property: JsonIgnore] Prompt prompt;

    partial void OnPromptChanged(Prompt? oldValue, Prompt newValue)
    {
        UpdateDriversSourcePrompt(oldValue, newValue);
    }

    /// <summary>
    /// only if IsUpdateDriversPrompt == true
    /// </summary>
    public void UpdateDriversSourcePrompt(Prompt? oldValue, Prompt newValue)
    {
        if (IsUpdateDriversPrompt && newValue != null && oldValue != null)
        {
            foreach (var driver in Drivers)
                driver.UpdateSource(oldValue, newValue);
        }
    }


    /// <summary>
    /// prevents from ghostly selecting other promptpreset
    /// </summary>
    [ObservableProperty] bool pinned = false;

    [ObservableProperty] bool block = false;

    [ObservableProperty] float progress = 0;
    [JsonIgnore] public int TotalSteps = 0;
    [JsonIgnore] public int StepsCompleted = 0;

    [ObservableProperty] string name = "PromptPreset";
    [ObservableProperty] Matrix canvasMatrix = Matrix.Identity;

    public MvvmHelpers.ObservableRangeCollection<LatentNode> LatentNodes { get; set; } = new();
    public ObservableCollection<LineConnection> LineConnections { get; set; } = new();
    public ObservableCollection<GroupNode> Groups { get; set; } = new();


    [JsonIgnore] SelectionCollection<LatentNode> _selectedNodes;
    [JsonIgnore]
    public SelectionCollection<LatentNode> SelectedNodes
    {
        get => _selectedNodes;
        set => ManualAPI.SetSelectionValue(ref _selectedNodes, value, LatentNodes);
    }

    public PromptPreset()
    {
        _selectedNodes = new(LatentNodes);
        LatentNodes.CollectionChanged += LatentNodes_CollectionChanged;
    }

    private void LatentNodes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (LatentNode node in e.NewItems)
            {
                node.AttachedPreset = this;
               //LatentNodes.FirstOrDefault(node => node.AttachedPreset == this);
                var sameid = LatentNodes.FirstOrDefault(n => n.IdNode == node.IdNode && n != node);

                if(node.IdNode == null || sameid != null)
                     node.IdNode = Namer.RandomId(LatentNodes, (n, id) => n.IdNode == id, 1);
            }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach (LatentNode node in e.OldItems)
            {
                DisconnectByPasses(node);
                foreach (var field in node.Fields)
                {
                    field.Disconnect();
                }

                node.Dispose(); //lol    
            }
        }
    }

    public void AddNode(LatentNode node, Point position, bool avoidOverlap = false)
    {
        if(newNodePosition != position)
          newNodePosition = position;

        node.Position = newNodePosition;
        AddNode(node, avoidOverlap);
    }
    public void AddNode(LatentNode node, bool avoidOverlap = true) // when searchbox open too
    {
       
        if (avoidOverlap)
         node.AvoidOverlapping(LatentNodes);

        node.AttachedPreset = this;
        LatentNodes.Add(node);


        // Realiza el segundo cálculo que depende de la UI actualizada aquí
        if (newNodeConnect != null)
        {
            node.OnUINodeLoaded = () =>
            {
                if (newNodeConnect.Direction == NodeOptionDirection.Output)
                {
                    if (node.Inputs.Any() && node.Inputs.FirstOrDefault(f => f.HasType(newNodeConnect)) is NodeOption nodecon)
                        newNodeConnect.Connect(nodecon);
                    
                }
                else if (newNodeConnect.IsInputOrField())
                {
                    if (node.Outputs.Any() && node.Outputs.FirstOrDefault(f => f.HasType(newNodeConnect)) is NodeOption nodecon)
                        newNodeConnect.Connect(nodecon);
                }
                newNodeConnect = null;
            };
        }
    }

    public void AddNode(string nodeType)
    {
        var node = GenerationManager.NewNodeByType(nodeType);
        if(node != null)
          AddNode(node);
    }

    public void AddNodes(IEnumerable<LatentNode> latentNodes)
    {
        foreach (var node in latentNodes)
        {
            AddNode(node);
        }
    }
    public void AddNodes(params LatentNode[] latentNodes)
    {
        foreach (var node in latentNodes)
        {
            AddNode(node);
        }
    }
    public void AddNodes(List<Node> nodes)
    {
        Node.PasteNodes(nodes, this);
    }



    public void AddNodeTo(LatentNode node, string nodeField, LatentNode node2, string node2Field)
    {
        node.field(nodeField).Connect(node2.field(node2Field));
    }


    public void RemoveNode(LatentNode node)
    {
        LatentNodes.Remove(node);
    }
    void DisconnectByPasses(NodeBase node)
    {
        foreach (var input in node.Inputs)
        {
            foreach (var output in node.Outputs)
            {
                if (input.HasType(output))
                {
                    if (!input.IsConnected() || !output.IsConnected())
                        continue;

                    var i = input.Connections[0];
                    var cons = output.Connections.ToList();

                    input.Disconnect();
                    output.Disconnect();
                    foreach (var con in cons)
                    {
                        i.Connect(con);
                    }


                }
            }
        }


    }



    public void DeleteSelectedNodes()
    {
        SelectedNodes.Delete();
    }


    public void ConnectNodes(NodeOption nodeOption1, NodeOption nodeOption2)
    {
        nodeOption1.Connect(nodeOption2);
    }
    public void DisconnectNodes(NodeOption nodeOptionOutput)
    {
        LineConnections.Remove(nodeOptionOutput.LineConnect[0]);
        nodeOptionOutput.Disconnect();
    }

    public NodeBase FindNode(string name)
    {
        return LatentNodes.FirstOrDefault(node => node.Name == name);
    }
    public NodeBase FindNode(Type type)
    {
        return LatentNodes.FirstOrDefault(node => node.GetType() == type);
    }
    public NodeBase FindNode(int id)
    {
        return LatentNodes.FirstOrDefault(node => node.IdNode == id);
    }


    public void ChangeField(string nodeName, string fieldName, object newValue)
    {
       this.node(nodeName).field(fieldName).FieldValue = newValue;
    }

    //------------------------------------------------------------------------------------------------------------------------------------------- GENERATE
    [RelayCommand]
    public async Task Generate()
    {
        await Generate(ManualAPI.SelectedPreset);
    }

    public async Task Generate(PromptPreset preset)
    {
        var onode = preset.GetOutputNode();

        ExceptionNode(null);

        if (onode is IOutputNode OutNode)
        {
            ExecutingNode(onode);
            await Task.Run(OutNode.Generate);
            ExecutingNode(null);
        }
        else if (onode is ComfyUINode)
        {
            if (Settings.instance.UseCloud)
                await Comfy.GenerateServerless(preset);
            else
                await Comfy.Generate(preset);
        }
        else
        {
            Output.Log("missing output node");
        }
    }

    public NodeBase GetOutputNode()
    {
        //  return LatentNodes.FirstOrDefault(node => node is IOutputNode) as IOutputNode;
        return LatentNodes.FirstOrDefault(node => node.IsOutput == true);
    }

    public BoundingBox GetBoundingBox()
    {
        var rr = ManualAPI.layers.FirstOrDefault(l => l is BoundingBox) as BoundingBox;
        return rr;

        var o = GetOutputNode() as OutputNode;
        return o.GetBoundingBox();
    }


    public NodeBase? CurrentExecutingNode { get; set; }
    public void ExecutingNode(NodeBase? node)
    {
        if (CurrentExecutingNode == node)
            return;

        if (CurrentExecutingNode != null) //finalize node execution
        {
            CurrentExecutingNode.IsWorking = false;
            CurrentExecutingNode.Progress = 0;
          
        }
        if (node != null) //entry new node execution
        {
            node.IsWorking = true;
        }

        CurrentExecutingNode = node;
    }



    public NodeBase? CurrentExceptionNode { get; set; }
    public void ExceptionNode(NodeBase? node)
    {
        if (CurrentExceptionNode == node)
            return;
        

        if (CurrentExceptionNode != null) //finalize node execution
        {
            CurrentExceptionNode.IsException = false;

            foreach (var field in CurrentExceptionNode.Fields)
                field.IsError = false;
            
        }
        if (node != null)
        {
            node.IsException = true;
        }

        CurrentExceptionNode = node;
    }

    internal void ExceptionNode(NodeBase node, string inputName)
    {
        var field = node.Fields.FirstOrDefault(f => f.Type == inputName);
        if(field != null)
        {
            ExceptionNode(node);
            field.IsError = true;
        }
    }
    internal void ExceptionInput(NodeOption field)
    {
        if (field != null)
        {
            ExceptionNode(field.AttachedNode);
            field.IsError = true;
        }
    }



    public void ProgressCurrentNode(float progress)
    {
        if(CurrentExecutingNode != null)
        {
            CurrentExecutingNode.Progress = progress;

            if (GenerationManager.Instance.CurrentGeneratingImage != null)
            {
                GenerationManager.Instance.CurrentGeneratingImage.Progress = progress;
                GenerationManager.InvokeOnGenerating();
            }
        }
    }

    internal void UpdateUI()
    {
        foreach (var node in LatentNodes)
        {
            //node.UpdatePosition();

            if (node.UINode != null)
                node.UINode.CalculateHeight();
        }
    }



    public static PromptPreset Compose(PromptPreset preset1, PromptPreset preset2)
    {
        var promptInputs1 = new List<NodePromptInput>();
        var promptOutputs2 = new List<NodePromptOutput>();

        // Buscar nodos PromptInput y PromptOutput en preset1
        foreach (var node in preset1.LatentNodes)
        {
            if (node is NodePromptInput nodein)
                promptInputs1.Add(nodein);
        }

        // Buscar nodos PromptOutput en preset2
        foreach (var node in preset2.LatentNodes)
        {
            if (node is NodePromptOutput nodeout)
                promptOutputs2.Add(nodeout);          
        }

        // Conectar nodos basados en el Identify
        foreach (var inputNode in promptInputs1)
        {
            foreach (var outputNode in promptOutputs2)
            {
                if (inputNode.Identify == outputNode.Identify)
                {
                    // Desconectar todos los fields conectados al PromptInput
                    foreach (var inputField in inputNode.Out.Connections.ToList())
                    {
                        inputField.Disconnect();

                        // Conectar el field al PromptOutput correspondiente
                        var outputField = outputNode.In.Connections.FirstOrDefault();
                        if (outputField != null)
                        {
                            inputField.Connect(outputField);
                        }
                    }
                }
            }
        }

        // Crear un nuevo PromptPreset y combinar los nodos de preset1 y preset2
        var newPromptPreset = new PromptPreset();
        
        newPromptPreset.LatentNodes.AddRange(preset1.LatentNodes);
        newPromptPreset.LatentNodes.AddRange(preset2.LatentNodes);


        return newPromptPreset;
    }
 


    public override string ToString()
    {
        return $"{this.GetType()}, {Name}, nodes: {LatentNodes.Count}";
    }


    #region ----------------------------------------------------------------------- SERIALIZE

    Guid _id = Guid.NewGuid();

    internal NodeOption? newNodeConnect;
    internal Point newNodePosition { get; set; } = new Point(0, 0);

    public Guid Id
    {
        get => _id;
        set
        {
            _id = value;
            //  ReconnectNodes();
        }
    }



    // Método para reconstruir referencias después de la deserialización
    bool SameIdLine(List<Guid> lines, LineConnection lineConnection)
    {
        foreach (var lguid in lines)
        {
            if (lguid == lineConnection.Id)
                return true;
        }
        return false;
    }



    public void ReconnectNodes()
    {
        var linesList = LineConnections.ToList();
        LineConnections.Clear();
        foreach (var lineConnection in linesList)
        {
            var nodeOutput = LatentNodes.FirstOrDefault(n => n.Id == lineConnection.OutputNodeId);
            var nodeInput = LatentNodes.FirstOrDefault(n => n.Id == lineConnection.InputNodeId);

            if (nodeOutput != null)
            {
                var opOutput = nodeOutput.Fields.FirstOrDefault(op => op.Id == lineConnection.OutputId);
                var opInput = nodeInput.Fields.FirstOrDefault(op => op.Id == lineConnection.InputId);

                if(opOutput != null && opInput != null)
                  opOutput.Connect(opInput);
            }
        }
    }

    internal void Refresh()
    {
        GenerationManager.Instance.IsLoadingNodes = true;

        AppModel.Invoke(() =>
        {
            Graph graph;
          //  if (HasErrors())
         //       graph = Errors.GraphSaved;
         //   else
               graph = Comfy.LoadWorkflow(this);

            Comfy.AsignWorkflowToPreset(this, graph);

            if (!HasErrors())
                ClearErrors();

            UpdateUI();
        });

        GenerationManager.Instance.IsLoadingNodes = false;
    }

    public PromptPreset Duplicate()
    {
        var duplicate = new PromptPreset();
        var graph = Comfy.LoadWorkflow(this);
        Comfy.AsignWorkflowToPreset(duplicate, graph);

        duplicate.Name = Namer.SetName(this.Name, GenerationManager.Instance.PromptPresets);
        duplicate.CanvasMatrix = this.CanvasMatrix;

        return duplicate;
    }



    /// <summary>
    /// template name whitout extension
    /// </summary>
    /// <param name="templateName"></param>
    public static PromptPreset FromTemplate(string templateName, bool automaticDrivers = true)
    {
        var preset = GenerationManager.GetTemplate(templateName);
        if (preset != null)
        {
            if (automaticDrivers)
            {
                preset.Prompt = ManualAPI.SelectedPreset?.Prompt;
               // if(preset.Prompt != null)
                //    preset.AutomaticDrivers();

            }

            GenerationManager.ShowNodeErrors(preset);

            return preset;
        }
        else
        {
            Output.Log($"{templateName} not exist", "GhostPromptPreset Error");
            return null;
        }
    }


    
    [JsonProperty] string promptNameId
    {
        get => Prompt.Name;
        set => Prompt = GenerationManager.Instance.Prompts.FirstOrDefault(p => p.Name == value);
    }


    //EVER SERIALIZE IS ON Graph OR ManualConfig, NOT HERE

    #endregion -----------------------------------------------------------------------




    //---------------------------------------------------------------------------------------- DRIVERS
    [ObservableProperty] bool isUpdateDriversPrompt = true;

    [JsonConverter(typeof(DriverListJsonConverter))]
    public List<Driver> Drivers { get; set; } = [];


    public void AutomaticDrivers()
    {
         Prompt.SetDrivers(this);
    }


    public void ClearDrivers()
    {
        foreach (var driver in Drivers.ToList())
        {
            Driver.Delete(driver); // delete from Drivers in field.Driver setter
        }
        Drivers.Clear();
    }



    [ObservableProperty] [property: JsonIgnore] PresetRequirements requirements = new();

    public bool HasErrors() => Requirements.HasErrors;
    public void ClearErrors() => Requirements.ClearErrors();


}


public partial class Prompt : ObservableObject, INamable, ICloneableBehaviour, IDisposable
{

    public override string ToString()
    {
        return $"Prompt: {Name}";
    }
    public void OnClone<T>(T source)
    {

    }

    [ObservableProperty] byte[] thumbnail;
    /// <summary>
    /// scaled to 126 automatically
    /// </summary>
    /// <param name="bitmap"></param>
    public void SetThumbnail(SKBitmap bitmap)
    {
        var b = bitmap.ScaleTo(126,126);
        Thumbnail = b.ToByte();
    }

    [ObservableProperty] string name = "Prompt";

    [ObservableProperty] string positivePrompt = "";
    [ObservableProperty] string negativePrompt = "";

    [ObservableProperty] string altPositivePrompt = "";
    [ObservableProperty] string altNegativePrompt = "";

    [ObservableProperty] [property: JsonIgnore] string realPositivePrompt = "";
    [ObservableProperty] [property: JsonIgnore] string realNegativePrompt = "";

    [ObservableProperty] float cFG = 8;
    [ObservableProperty] float steps = 20;

    [ObservableProperty] string sampler = "euler";
    [ObservableProperty] string scheduler = "normal";
    [ObservableProperty] ulong seed = 0;
    [ObservableProperty] int clipSkip = -1;

    [ObservableProperty] object referenceLayer = new Item("Selected");
    [ObservableProperty] float strength = 1;
    [ObservableProperty] int width = 512;
    [ObservableProperty] int height = 512;
    [ObservableProperty] int batchSize = 1;

    [ObservableProperty] string controlSeed = "randomize";

    [ObservableProperty] string model;


    [ObservableProperty] string lora0;
    [ObservableProperty] float lora0Strength;

    [ObservableProperty] string lora1;
    [ObservableProperty] float lora1Strength;


    [JsonIgnore] public ObservableCollection<string> GetAllModels => GenerationManager.Instance.Models;
    [JsonIgnore] public ObservableCollection<string> GetAllLoras => GenerationManager.Instance.Loras;
    [JsonIgnore] public ObservableCollection<string> GetAllSamplers => GenerationManager.Instance.Samplers;
    [JsonIgnore] public ObservableCollection<string> GetAllSchedulers => GenerationManager.Instance.Schedulers;

    public Prompt()
    {
        
    }

    public void Init()
    {
        GenerationManager.OnNodesRegistered += CheckModels;
  
        GenerationManager.Instance.Models.CollectionChanged += Models_CollectionChanged;
        GenerationManager.Instance.Loras.CollectionChanged += Loras_CollectionChanged;

        GenerationManager.Instance.Samplers.CollectionChanged += Samplers_CollectionChanged;
        GenerationManager.Instance.Schedulers.CollectionChanged += Schedulers_CollectionChanged;

        if(GenerationManager.isNodesRegistered)
            CheckModels();
    }

    private void CheckModels()
    {
        if (GetAllModels.Any())
        {
            OnPropertyChanged(nameof(GetAllModels));
            OnPropertyChanged(nameof(GetAllLoras));
            OnPropertyChanged(nameof(GetAllSamplers));
            OnPropertyChanged(nameof(GetAllSchedulers));

            if (!GetAllModels.Contains(Model))
                Model = GetAllModels.First();

            if (!GetAllLoras.Contains(Lora0))
                Lora0 = GetAllLoras.First();

            if (!GetAllLoras.Contains(Lora1))
                Lora1 = GetAllLoras.First();
        }
    }

    public void Dispose()
    {
        GenerationManager.OnNodesRegistered -= CheckModels;

        GenerationManager.Instance.Models.CollectionChanged -= Models_CollectionChanged;
        GenerationManager.Instance.Loras.CollectionChanged -= Loras_CollectionChanged;

        GenerationManager.Instance.Samplers.CollectionChanged -= Samplers_CollectionChanged;
        GenerationManager.Instance.Schedulers.CollectionChanged -= Schedulers_CollectionChanged;
    }

    private void Loras_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>  OnPropertyChanged(nameof(GetAllModels));
    private void Models_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(GetAllLoras));

    private void Samplers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(GetAllSamplers));
    private void Schedulers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(GetAllSchedulers));



    private bool _isUpdating;


    //
    #region BERBORREA PARA PROMPT TEXT
    partial void OnPositivePromptChanged(string? oldValue, string newValue)
    {
        UpdateRealPositivePrompt();
    }

    partial void OnAltPositivePromptChanged(string? oldValue, string newValue)
    {
        UpdateRealPositivePrompt();
    }

    partial void OnNegativePromptChanged(string? oldValue, string newValue)
    {
        UpdateRealNegativePrompt();
    }

    partial void OnAltNegativePromptChanged(string? oldValue, string newValue)
    {
        UpdateRealNegativePrompt();
    }

    partial void OnRealPositivePromptChanged(string? oldValue, string newValue)
    {
        if (_isUpdating) return;

        _isUpdating = true;

        try
        {
            if (newValue != null)
            {
                // Verifica si AltPositivePrompt ha cambiado
                string oldAlt = string.IsNullOrEmpty(AltPositivePrompt) ? "" : ", " + AltPositivePrompt;
                if (newValue.EndsWith(oldAlt))
                {
                    // Modificación en PositivePrompt
                    PositivePrompt = newValue.Substring(0, newValue.Length - oldAlt.Length).TrimEnd(new char[] { ',', ' ' });
                }
                else if (newValue.Length <= PositivePrompt.Length)
                {
                    PositivePrompt = newValue;
                    AltPositivePrompt = "";
                }
                else
                {
                    // Modificación en AltPositivePrompt
                    AltPositivePrompt = newValue.Substring(PositivePrompt.Length).TrimStart(new char[] { ',', ' ' });
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
        
    }
    private int FindSplitIndex(string oldValue, string newValue)
    {
        int minLength = Math.Min(oldValue.Length, newValue.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (oldValue[i] != newValue[i])
            {
                return i;
            }
        }
        return minLength;
    }

    partial void OnRealNegativePromptChanged(string? oldValue, string newValue)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            if (newValue != null)
            {
                // Verifica si AltPositivePrompt ha cambiado
                string oldAlt = string.IsNullOrEmpty(AltNegativePrompt) ? "" : ", " + AltNegativePrompt;
                if (newValue.EndsWith(oldAlt))
                {
                    // Modificación en PositivePrompt
                    NegativePrompt = newValue.Substring(0, newValue.Length - oldAlt.Length).TrimEnd(new char[] { ',', ' ' });
                }
                else if (newValue.Length <= NegativePrompt.Length)
                {
                    NegativePrompt = newValue;
                    AltNegativePrompt = "";
                }
                else
                {
                    // Modificación en AltPositivePrompt
                    AltNegativePrompt = newValue.Substring(NegativePrompt.Length).TrimStart(new char[] { ',', ' ' });
                }
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateRealPositivePrompt()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            RealPositivePrompt = PositivePrompt + (string.IsNullOrEmpty(AltPositivePrompt) ? "" : ", " + AltPositivePrompt);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateRealNegativePrompt()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            RealNegativePrompt = NegativePrompt + (string.IsNullOrEmpty(AltNegativePrompt) ? "" : ", " + AltNegativePrompt);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    //DISABLED: funciona pero mejor otra forma

    //partial void OnAltPositivePromptChanged(string? oldValue, string newValue)
    //{
    //    if (_isUpdating) return;

    //    _isUpdating = true;

    //    var positivePromptParts = PositivePrompt.Split(", ").ToList();
    //    var altPositivePromptParts = newValue.Split(", ").ToList();

    //    var matchingParts = altPositivePromptParts.Take(positivePromptParts.Count).ToList();
    //    if (string.Join(", ", matchingParts) != PositivePrompt)
    //    {
    //        PositivePrompt = string.Join(", ", matchingParts);
    //    }

    //    _isUpdating = false;
    //}
    //partial void OnPositivePromptChanged(string? oldValue, string newValue)
    //{
    //    if (_isUpdating) return;

    //    _isUpdating = true;



    //    if (AltPositivePrompt.StartsWith(oldValue))
    //    {
    //        AltPositivePrompt = newValue + AltPositivePrompt.Substring(oldValue.Length);
    //    }


    //    _isUpdating = false;

    //}

    #endregion
    //


    /// <summary>
    /// change seed based on ControlSeed
    /// </summary>
    public void ChangeSeed()
    {
        switch (ControlSeed)
        {
            case "randomize":
                Seed = ComfyUIWorkflow.RandomSeed();
                break;
            case "increment":
                Seed = Seed + 1;
                break;
            case "decrement":
                Seed = Seed - 1;
                break;
            default:
                break;
        }

    }


    public void AddLora(string loraName)
    {

    }
    public static PromptPreset InstantiateModel()
    {
        //   var model = ManualAPI.SelectedPreset;
        var model = new PromptPreset();

        var output = new NodePromptOutput();
        var checkpoint = Node.New("Load Checkpoint");

        model.AddNode(output);
        checkpoint.field("MODEL").Connect(output, "Output");

        //  var lora = Node.New("Load LoRA");
        //    lora.ConnectByPass(api.node("Load Checkpoint"));
        return model;
    }

    public static void TestOld()
    {
        var general = api.preset("general");
        var model = api.preset("model");

        var compose = PromptPreset.Compose(general, model);
        GenerationManager.Instance.AddPreset(compose);

    }


    public static void Test()
    {
        //var field = api.node("KSampler").field("steps");

        //var driver = new Driver([ManualAPI.SelectedPreset.Prompt],
        //    expression: "Steps",

        //    field,
        //    nameof(field.FieldValue));

        //Core.Output.Log(driver);
    }



    public void SetDrivers(PromptPreset prompt)
    {
        try
        {
            // KSAMPLER
            NodeBase? ksampler = prompt.LatentNodes.FirstOrDefault(n => n.field("cfg") != null);

            var model = prompt.node_type("CheckpointLoaderSimple");
            DetermineDriver(model, "ckpt_name");
            if(model != null)
            {
                if (model.Fields.First().ConnectedNode().First() is NodeBase loraNode && loraNode.NameType == "LoraLoader")
                {
                    BindField(loraNode, "lora_name", "Lora0");
                    BindField(loraNode, "strength_model", "Lora0Strength");
                  //  BindField(loraNode, "strength_clip", "Lora0Strength");

                    if (loraNode.Fields.First().ConnectedNode().First() is NodeBase loraNode2 && loraNode2.NameType == "LoraLoader")
                    {
                        BindField(loraNode2, "lora_name", "Lora1");
                        BindField(loraNode2, "strength_model", "Lora1Strength");
                       // BindField(loraNode2, "strength_clip", "Lora1Strength");

                    }
                }


            }

            DetermineDriver(ksampler, "seed");
            DetermineDriver(ksampler, "control_after_generate");

            DetermineDriver(ksampler, "steps");
            DetermineDriver(ksampler, "cfg");
            DetermineDriver(ksampler, "sampler_name");
            DetermineDriver(ksampler, "scheduler");

            DetermineDriver(ksampler, "denoise");


            if (ksampler != null)
            {

                // PROMPT
                if (ksampler.NameType != "KSampler")
                {
                    var toBasicPipe = prompt.node_type("ToBasicPipe");
                    ksampler = toBasicPipe ?? ksampler;
                }
                var positive = ksampler.field("positive");
                var negative = ksampler.field("negative");



                if (positive != null && positive.FindNode(n => n.NameType == "CLIPTextEncode" || n is PrimitiveNode) is NodeBase n1)
                {
                    if (string.IsNullOrEmpty(this.RealPositivePrompt))
                        this.RealPositivePrompt = (string)n1.field("text")?.FieldValue;

                    var bindName = n1.NameType == "CLIPTextEncode" ? "text" : ((PrimitiveNode)n1).Widget.Name;
                    BindField(n1, bindName, "RealPositivePrompt");
 
                }

                //if (negative.ConnectedNode().First() is NodeBase n2 && n2.NameType == "CLIPTextEncode")
                if (negative != null && negative.FindNode(n => n.NameType == "CLIPTextEncode" || n is PrimitiveNode) is NodeBase n2)
                {
                    if (string.IsNullOrEmpty(this.RealNegativePrompt))
                        this.RealNegativePrompt = (string)n2.field("text")?.FieldValue;

                    var bindName = n2.NameType == "CLIPTextEncode" ? "text" : ((PrimitiveNode)n2).Widget.Name;
                    BindField(n2, bindName, "RealNegativePrompt");
                }


                // LATENT IMAGE
                if (ksampler.field("latent_image") is NodeOption li)
                {
                    var linod = li.Connections?.First().AttachedNode;
                    var latent_image = linod?.NameType == "EmptyLatentImage" ? linod : null;
                    if (latent_image != null)
                    {
                        DetermineDriver(latent_image, "width");
                        DetermineDriver(latent_image, "height");
                        DetermineDriver(latent_image, "batch_size");
                    }
                    else if (li.FindNode(n => n.NameType == "ImageScale" || n.NameType == "LatentUpscale") is NodeBase linod2)
                    {
                        latent_image = linod2;
                        DetermineDriver(latent_image, "width");
                        DetermineDriver(latent_image, "height");
                    }
                    
                }


            }


            //CLIP SKIP
            var clipSkip = prompt.node_type("CLIPSetLastLayer");
            DetermineDriver(clipSkip, "stop_at_clip_layer");

        }
        catch (Exception ex)
        {
            Output.Warning(ex.ToString(), "some drivers cannot be binded");
        }

    }

    public Driver? DetermineDriver(NodeBase? node, string fieldName, bool initialize = true)
    {
        if (node == null) return null;

        var f = node.field(fieldName);
        if (f != null)
            return DetermineDriver(f, initialize);
        else
            return null;
    }

    public Driver DetermineDriver(NodeOption nodeop, bool initialize = true)
    {
        var sourceName = DetermineSourceName(nodeop);
        var driver = nodeop.Driver == null ? new Driver(nodeop, nameof(nodeop.FieldValue), this, expression: sourceName, true) : nodeop.Driver;

        if (initialize)
            driver.Initialize();

        return driver;
    }
    public Driver BindField(NodeBase node, string fieldName, string expressionCode, bool initialize = true)
    {
        return BindField(node.field(fieldName), expressionCode, initialize);
    }
    public Driver BindField(NodeOption nodeop, string expressionCode, bool initialize = true)
    {
        var sourceName = expressionCode;
        var driver = nodeop.Driver == null ? new Driver(nodeop, nameof(nodeop.FieldValue), this, expression: sourceName, true) : nodeop.Driver;

        if (initialize)
            driver.Initialize();

        return driver;
    }

    //EDIT DRIVER MENUITEM
    public static string DetermineSourceName(NodeOption nodeop)
    {
        string sourceName = string.Empty;

        switch (nodeop.Name)
        {
            case "denoise":
                sourceName = "Strength";
                break;
            case "cfg":
                sourceName = "CFG";
                break;
            case "steps":
                sourceName = "Steps";
                break;
            case "seed":
                sourceName = "Seed";
                break;
            case "control_after_generate":
                sourceName = "ControlSeed";
                break;
            case "sampler_name":
                sourceName = "Sampler";
                break;
            case "scheduler":
                sourceName = "Scheduler";
                break;
            case "ckpt_name":
                sourceName = "Model";
                break;
            case "width":
                sourceName = "Width";
                break;
            case "height":
                sourceName = "Height";
                break;
            case "batch_size":
                sourceName = "BatchSize";
                break;
            case "Layer":
                sourceName = "ReferenceLayer";
                break;
            case "stop_at_clip_layer":
                sourceName = "ClipSkip";
                break;
            default:
                //prompt
                if (nodeop.AttachedNode.Fields[0].IsConnected() && nodeop.AttachedNode.Fields[0].Connections[0].Name == "positive")
                {
                    sourceName = "RealPositivePrompt";
                }
                else if (nodeop.AttachedNode.Fields[0].IsConnected() && nodeop.AttachedNode.Fields[0].Connections[0].Name == "negative")
                {
                    sourceName = "RealNegativePrompt";
                }

                //lora
                else if (nodeop.Name == "lora_name")
                {
                    var prompt = nodeop.AttachedNode.AttachedPreset.Prompt;
                    if (string.IsNullOrEmpty(prompt.Lora0))
                        sourceName = "Lora0";
                    else
                        sourceName = "Lora1";
                }
                else if (nodeop.Name == "strength_model" || nodeop.Name == "strength_clip")
                {
                    var prompt = nodeop.AttachedNode.AttachedPreset.Prompt;
                    Driver? driver = null;
                    if(nodeop.Name == "strength_model")
                       driver = nodeop.AttachedNode.field("strength_clip").Driver;
                    else if(nodeop.Name == "strength_clip")
                        driver = nodeop.AttachedNode.field("strength_model").Driver;

                    if (driver == null)
                        driver = nodeop.AttachedNode.field("lora_name").Driver;

                    if (prompt.Lora0Strength == 0 || (driver?.sourcePropertyName == "Lora0Strength" || driver?.sourcePropertyName == "Lora0"))
                        sourceName = "Lora0Strength";
                    else
                        sourceName = "Lora1Strength";
                }
                break;
        }

        return sourceName;
    }






    internal static void ExportPrompt(Prompt prompt, string filePath)
    {
        prompt.Name = Path.GetFileNameWithoutExtension(filePath);

        var jsonString = PromptJson(prompt);
        File.WriteAllText(filePath, jsonString);

        Output.Log($"Prompt {prompt.Name} saved", "Manual");
    }
    internal static string PromptJson(Prompt prompt, Formatting format = Formatting.Indented)
    {
        var settings = new JsonSerializerSettings
        {
            //TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new FileManager.IgnorePropertiesResolver("category", "description", "output_node"),
            Converters = new List<JsonConverter> { new StringEnumConverter() },
        };
        string jsonString = JsonConvert.SerializeObject(prompt, format, settings);
        return jsonString;
    }


    /// <summary>
    /// import and add
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static Prompt V_ImportPrompt(string filePath)
    {
        var prompt = ImportPrompt(filePath);
        GenerationManager.Instance.AddPrompt(prompt);

        if(ManualAPI.SelectedPreset != null)
           ManualAPI.SelectedPreset.Prompt = prompt;

        return prompt;
    }
    public static Prompt ImportPromptTemplate(string templateName)
    {
        var fileName = Path.Combine(App.LocalPath, "Resources", "Templates", "Prompts", $"{templateName}.prompt");
        return ImportPrompt(fileName);
    }
    public static Prompt ImportPrompt(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        string jsonString = "";
        if (extension == ".prompt")
            jsonString = JsonImportPromptFile(filePath);
        else if (extension == ".png")
            jsonString = JsonImportPromptPng(filePath);
        
        if(string.IsNullOrEmpty(jsonString))
        {
            Output.Log($"Not a Prompt supported file: {extension}");
            return null;
        }

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

        var prompt = JsonConvert.DeserializeObject<Prompt>(jsonString);
        return prompt;
    }

    static string JsonImportPromptFile(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        return jsonString;

    }
    static string JsonImportPromptPng(string filePath)
    {
        var jsonString = FileManager.ReadCustomMetadata(filePath, "prompt");
        return jsonString;
    }

}




public partial class PresetRequirements : ObservableObject
{
    [ObservableProperty] [property: JsonIgnore] bool hasErrors = false;

    [JsonIgnore] public List<string> MissingNodeTypes { get; set; } = [];
    [JsonIgnore] public List<string> MissingModels { get; set; } = [];
    [JsonIgnore] public Graph GraphSaved { get; set; }
   
    public PresetRequirements()
    {
            
    }

    public void ClearErrors()
    {
        MissingNodeTypes.Clear();
        MissingModels.Clear();
        HasErrors = false;
    }


    public void AddMissingNode(string nodeType)
    {
        HasErrors = true;
        MissingNodeTypes.Add(nodeType);
    }

    public void AddMissingModel(string modelName)
    {
        HasErrors = true;
        MissingModels.Add(modelName);
    }


    public override string ToString()
    {
        return $"HasErrors: {HasErrors} Nodes:{MissingNodeTypes.Count} Models:{MissingModels.Count}";
    }

}












