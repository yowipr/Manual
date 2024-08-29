using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpeg.AutoGen;

using Manual.API;
using Manual.Core.Nodes.ComfyUI;
using Manual.Editors;
using Manual.Editors.Displays;
using Manual.MUI;
using Manual.Objects;
using Manual.Objects.UI;
using ManualToolkit.Generic;
using ManualToolkit.Windows;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using MS.WindowsAPICodePack.Internal;
using Newtonsoft.Json;

using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace Manual.Core.Nodes;


public class MenuItemNode
{
    public string Path { get; set; } = "";
    public string Description { get; set; } = "";
    public string Name { get; set; }
    public string NameType { get; set; }
    public List<MenuItemNode> SubItems { get; set; } = new();
    public Func<LatentNode> Factory { get; set; }
    public Action DoAction { get; set; }

    public MenuItemNode(string DisplayName, string nodeType, Func<LatentNode> nodeFac)
    {
        Name = DisplayName;
        NameType = nodeType;
        Factory = nodeFac;
    }
    public MenuItemNode(string DisplayName, Func<LatentNode> nodeFac)
    {
        Name = DisplayName;
        NameType = DisplayName;
        Factory = nodeFac;
    }
    public MenuItemNode(string description, string path, string name, Action doAction)
    {
        SubItems = null;
        Description = description;
        Path = path;
        Name = name;
        DoAction = doAction;
    }
    public MenuItemNode()
    {
            
    }
 
}
//------------------------------------------------------------------------------------------------------------------------------ PROMPT PRESET SERIALIZE
public partial class GenerationManager : ObservableObject 
{

    [JsonIgnore] public Prompt SelectedPrompt => ManualAPI.SelectedPreset?.Prompt;

    [ObservableProperty] bool isLoadingNodes = true;

    [JsonIgnore] public List<MenuItemNode> PromptPresetTemplates { get; set; } = new();
    [JsonIgnore] public List<MenuItemNode> PromptTemplates { get; set; } = new();

    [JsonIgnore] public static List<MenuItemNode> RegisteredNodes { get; set; } = new();
    // public List<LatentNode> NodesToShow => RegisteredNodes.Select(factory => factory()).ToList();
    [JsonIgnore] public List<MenuItemNode> NodesToShow { get; set; } = new();
    //  public MenuItemNode NodesToShow => new MenuItemNode("Add", MenuItemNode.BuildMenu());



    public GenerationManager()
    {
        Prompts.CollectionChanged += Prompts_CollectionChanged;
    }

    private void Prompts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (Prompt item in e.NewItems)
            {
                item.Init();
            }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach(Prompt item in e.OldItems)
            {
                item.Dispose();
            }
        }
    }

    public void InvokeNodesRegistered()
    {
        OnNodesRegistered?.Invoke();
    }

    public void Initialize()
    {
        ActionHistory.OnActionChange += ActionHistory_OnActionChange;

        if (Project._tcsOpen == null)
        {
            GetPromptPresetTemplates();
            GetPromptTemplates();
            BuildRegisterNodes();
        }
    }
    public static GenerationManager Instance => AppModel.project.generationManager;

    /// <summary>
    /// instantiate all relationated with nodes
    /// </summary>
    public void InstantiatePromptPreset()
    {
        //PromptPreset importPreset = ImportPromptPreset($"{App.LocalPath}Resources/PromptPresets/PromptPreset.pp");
        //if (importPreset == null)
        //{
        //    AddPreset();
        //}


        //PROMPTS
        //var prompt = new Prompt();
        //Prompts.Add(prompt);
        var promptName = "anime";
        var prompt = Prompt.V_ImportPrompt(Path.Combine(App.LocalPath, $"Resources/Templates/Prompts/{promptName}.prompt"));


        if (!PromptPresetTemplates.Any()) //not promptpreset in templates
        {
            AddPreset();
            return;
        }


        string name = "default text to image";
        string path = Path.Combine(App.LocalPath, $"Resources/Templates/PromptPresets/{name}.json");
        if (Path.Exists(path))
        {
            //PromptPresetTemplates.FirstOrDefault(m => m.Name == name)?.DoAction();
            var preset = FindPresetOrTemplate(name);
            Instance.AddPreset(preset, false, asSelected: true);
        }
        else
        {
            PromptPresetTemplates[0].DoAction();
        }


        if (SelectedPreset != null)
        {
            SelectedPreset.Name = "PromptPreset";

            //ASIGN PROMPT
            SelectedPreset.Prompt = prompt;
            //SelectedPreset.AutomaticDrivers();

        }



    }




    public static PromptPreset? GetTemplate(string templateName)
    {
        string directoryPath = Path.Combine(App.LocalPath, "Resources", "Templates", "PromptPresets");

        // Corrige la ruta utilizando Path.Combine para manejar las subcarpetas y nombres de archivos
        var normalizedTemplateName = templateName.Replace("/", Path.DirectorySeparatorChar.ToString());
        var filePath = Path.Combine(directoryPath, $"{normalizedTemplateName}.json");

        // Comprueba si el archivo existe
        if (File.Exists(filePath))
        {
            var preset = ImportPromptPreset(filePath, false);
            preset.Name = Path.GetFileNameWithoutExtension(filePath);
            return preset;
        }
        else
        {
            return null;
        }
    }



    void GetPromptPresetTemplates()
    {
        // Define el directorio de los templates
        string directoryPath = Path.Combine(App.LocalPath, "Resources", "Templates", "PromptPresets");

        // Comprueba si el directorio existe
        if (Directory.Exists(directoryPath))
        {
            // Limpia la lista de templates existente (si es necesario)
            PromptPresetTemplates.Clear();

            // Agrega templates desde el directorio raíz
            AddTemplatesFromDirectory(directoryPath, PromptPresetTemplates, "");
        }
    }

    void AddTemplatesFromDirectory(string currentDirectory, List<MenuItemNode> parentList, string currentPath)
    {
        // Obtén todas las subcarpetas
        string[] subDirectories = Directory.GetDirectories(currentDirectory);

        // Procesa cada subcarpeta
        foreach (string subDirectory in subDirectories)
        {
            string folderName = Path.GetFileName(subDirectory);
            string newPath = string.IsNullOrEmpty(currentPath) ? folderName : $"{currentPath}/{folderName}";

            // Crea un nuevo nodo para la carpeta
            var folderNode = new MenuItemNode(folderName, newPath, folderName, null)
            {
                SubItems = new List<MenuItemNode>()
            };

            // Llama recursivamente para agregar archivos y subcarpetas al nodo de la carpeta actual
            AddTemplatesFromDirectory(subDirectory, folderNode.SubItems, newPath);

            // Agrega la carpeta a la lista padre
            parentList.Add(folderNode);
        }

        // Obtén todos los archivos JSON en el directorio actual
        string[] fileEntries = Directory.GetFiles(currentDirectory, "*.json");

        // Itera sobre cada archivo y los importa
        foreach (string filePath in fileEntries)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string presetPath = string.IsNullOrEmpty(currentPath) ? fileNameWithoutExtension : $"{currentPath}/{fileNameWithoutExtension}";

            // Crea un nuevo nodo para el archivo JSON
            var fileNode = new MenuItemNode("PromptPreset", presetPath, fileNameWithoutExtension,
                () =>
                {
                    var preset = PromptPreset.FromTemplate(presetPath);
                    GenerationManager.Instance.AddPreset(preset);
                });

            // Agrega el archivo JSON a la lista padre actual
            parentList.Add(fileNode);
        }
    }




    void GetPromptPresetTemplatesOld()
    {
        // Define el directorio de los templates
        string directoryPath = Path.Combine(App.LocalPath, "Resources", "Templates", "PromptPresets");

        // Comprueba si el directorio existe
        if (Directory.Exists(directoryPath))
        {
            // Obtiene todos los archivos en el directorio
            string[] fileEntries = Directory.GetFiles(directoryPath);

            // Itera sobre cada archivo y los importa
            foreach (string filePath in fileEntries)
            {
                var filePathName = Path.GetFileNameWithoutExtension(filePath);
                var menuItem = new MenuItemNode("PromptPreset", "Template", filePathName,
                    () =>
                    {
                        var preset = PromptPreset.FromTemplate(filePathName);
                        GenerationManager.Instance.AddPreset(preset);
                    });

                PromptPresetTemplates.Add(menuItem);
                
            }
        } 
    }


    void GetPromptTemplates()
    {
        // Define el directorio de los templates
        string directoryPath = Path.Combine(App.LocalPath, "Resources", "Templates", "Prompts");

        // Comprueba si el directorio existe
        if (Directory.Exists(directoryPath))
        {
            // Obtiene todos los archivos en el directorio
            string[] fileEntries = Directory.GetFiles(directoryPath);

            // Itera sobre cada archivo y los importa
            foreach (string filePath in fileEntries)
            {
                var filePathName = Path.GetFileNameWithoutExtension(filePath);
                var menuItem = new MenuItemNode("Prompt", "Template", filePathName,
                    () =>
                    {
                        var prompt = Prompt.ImportPromptTemplate(filePathName);
                        GenerationManager.Instance.AddPrompt(prompt);

                        if (ManualAPI.SelectedPreset != null)
                            ManualAPI.SelectedPreset.Prompt = prompt;
                    });

                PromptTemplates.Add(menuItem);

            }
        }
    }
    

    //------------------------------------------------------------------------------- REGISTER NODES
    public static LatentNode? NewNodeByType(string nodeType)
    {
        var node = RegisteredNodes.FirstOrDefault(n => n.NameType == nodeType);
        if (node != null)
            return node.Factory();
        else
            return null;
    }
    public static TNode? NewNodeByType<TNode>(string nodeType) where TNode : NodeBase
    {
        var node = RegisteredNodes.FirstOrDefault(n => n.NameType == nodeType);

        if (node != null)
        {
            // Asumiendo que Factory es un método que devuelve una instancia del tipo correcto
            return node.Factory() as TNode;
        }
        else
        {
            return null;
        }
    }

    public static LatentNode? NewNodeByName(string nodeName) => NewNodeByName<LatentNode>(nodeName);
    public static TNode? NewNodeByName<TNode>(string nodeName) where TNode : NodeBase
    {
        var node = RegisteredNodes.FirstOrDefault(n => n.Name == nodeName);

        if (node != null)
        {
            // Asumiendo que Factory es un método que devuelve una instancia del tipo correcto
            return node.Factory() as TNode;
        }
        else
        {
            return null;
        }
    }


    List<MenuItemNode> GetSubItemPath(string path)
    {
        var segments = path.Split('/');
        var currentNodeList = NodesToShow; // 'menu' es la variable que almacena la raíz del árbol del menú

        foreach (var segment in segments)
        {
            var node = currentNodeList.FirstOrDefault(n => n.NameType == segment);
            if (node == null)
            {
                node = new MenuItemNode { Name = segment, NameType = segment };
                currentNodeList.Add(node);
            }
            currentNodeList = node.SubItems;
        }
        return currentNodeList;

    }
   
    public void RegisterNode(string nodeName, string nodeType, Func<LatentNode> nodeFac, string path = "")
    {      
        var menuItem = new MenuItemNode {Name = nodeName, NameType = nodeType, Factory = nodeFac, Path = path};
        menuItem.Description = "Add";
        menuItem.DoAction = () => 
        {
            var node = menuItem.Factory();
            ManualAPI.SelectedPreset.AddNode(node, ManualAPI.SelectedPreset.newNodePosition);
        };

        RegisterNode(menuItem);

    }
    public void RegisterNodes(string path, params MenuItemNode[] menuItems)
    {
        foreach (var menuItem in menuItems)
        {
            RegisterNode(menuItem, path);
        }
    }
    void RegisterNode(MenuItemNode menuItem, string? path = null)
    {
        if (path == null)
            path = menuItem.Path;

        var currentNodeList = GetSubItemPath(path);

        var item = RegisteredNodes.FirstOrDefault(n => n.NameType == menuItem.NameType);
        if (item != null)
        {
            currentNodeList.Remove(item);
            RegisteredNodes.Remove(item);
        }
        

        currentNodeList.Add(menuItem);
        RegisteredNodes.Add(menuItem);

        if(item != null)
        {
            foreach (var preset in GenerationManager.Instance.PromptPresets)
            {
                foreach (var node in preset.LatentNodes)
                {
                    if (node.NameType == item.NameType)
                    {
                        node.Refresh(item.Factory());
                    }
                }

            }

        }
    }

    void ClearRegisterNodes()
    {
        RegisteredNodes.Clear();
        ManualNodes.RegisteredManualNodes.Clear();
    }

    /// <summary>
    /// refresh the nodes registered, or initialize
    /// </summary>
    public void BuildRegisterNodes()
    {
        isNodesRegistered = false;

        AppModel.mainW?.SetProgress(1, "refreshing...");
        // RegisterNode("Number", () => new NumberNode(), "Numbers");

        ClearRegisterNodes();



        //----- SET MANUAL NODES
        ManualNodes.RegisterUtils(this);
        ManualNodes.RegisterAllNodes();


        //-------------- IMPORT COMFY NODES
        WaitToRegister();

       //RegisterNode("Search", "Search", () => new NumberNode());
    }

    public static event Event OnNodesRegistered;
    public static bool isNodesRegistered;

    //------------------------------------------------------------------------------------------------------------------- NODES LOADED
    async void WaitToRegister()
    {
        await Comfy.RegisterComfyNodes();


        if (AppModel.InstantiateThings)
        {
            AppModel.Invoke(InstantiatePromptPreset);
            AppModel.InstantiateThings = false;

            if (PromptPresets.Any())
                SelectedPreset = PromptPresets[0];
        }

        //REFRESH
        if (ManualAPI.SelectedPreset != null)
        {
            ManualAPI.SelectedPreset.Refresh();
        }


        //LOAD MODELS
        await RegisterModels();



        IsLoadingNodes = false;

        OnNodesRegistered?.Invoke();
        isNodesRegistered = true;

        AppModel.mainW?.StopProgress();
    }



    //----------------------------------------------------------------------------------------------- MODELS LIST

    public MvvmHelpers.ObservableRangeCollection<string> Models { get; private set; } = [];
    public MvvmHelpers.ObservableRangeCollection<string> Loras { get; private set; } = [];

    public MvvmHelpers.ObservableRangeCollection<string> Samplers { get; private set; } = [];
    public MvvmHelpers.ObservableRangeCollection<string> Schedulers { get; private set; } = [];


    public static async Task RegisterModels()
    {
        await SetModel(Instance.Models, "CheckpointLoaderSimple", "input.required.ckpt_name");        
        await SetModel(Instance.Loras, "LoraLoader", "input.required.lora_name");

        var ksampler = await Comfy.GetNodeInfo("KSampler");
        if (ksampler != null)
        {
            SetModel(Instance.Samplers, ksampler, "KSampler.input.required.sampler_name");
            SetModel(Instance.Schedulers, ksampler, "KSampler.input.required.scheduler");
        }

    }

    static async Task SetModel(MvvmHelpers.ObservableRangeCollection<string> models, string nodeType, string path)
    {
        var modelsList = await Comfy.GetModelList(nodeType, path);
            AppModel.Invoke(() =>
            {
                models.ReplaceRange(modelsList);
            });
    }
    static void SetModel(MvvmHelpers.ObservableRangeCollection<string> models, JToken nodeType, string fullPath)
    {
        var modelsList = Comfy.ExtractListNames(nodeType, fullPath);
        models.ReplaceRange(modelsList);
    }
    



    private void ActionHistory_OnActionChange(IUndoableAction action)
    {
        if (Realtime)
        {
            Generate();
        }
    }
    [ObservableProperty] bool realtime = false;

    [RelayCommand]
    [property: JsonIgnore]
    public void SwitchRealtime()
    {
        Realtime = !Realtime;
    }



    public static void SavePromptPreset() // current
    {
        string linkDir = $"{App.LocalPath}Resources/PromptPresets";
        var filePath = Path.Combine(linkDir, $"{Instance.SelectedPreset.Name}.pp");
        SavePromptPreset(filePath);
    }
    public static void SavePromptPreset(string filePath)
    {
        SavePromptPreset(Instance.SelectedPreset, filePath);
    }
    public static void SavePromptPreset(PromptPreset promptPreset, string filePath)
    {
        if (promptPreset.GetOutputNode() is not OutputNode)
        {
            SavePromptPresetWorkflow(promptPreset, filePath);
            return;
        }

        promptPreset.Name = Path.GetFileNameWithoutExtension(filePath);

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        string jsonString = JsonConvert.SerializeObject(promptPreset, Formatting.Indented, settings);
        File.WriteAllText(filePath, jsonString);
    }
    public static void SavePromptPresetWorkflow(PromptPreset promptPreset, string filePath) // comfyui
    {
        Comfy.ExportWorkflow(promptPreset, filePath);
    }


    public static PromptPreset? LoadPromptPreset(string filePath)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                {
                    // Ignorar el error actual y continuar con la deserialización
                    args.ErrorContext.Handled = true;
                    Output.Log($"Error al deserializar: {args.ErrorContext.Error.Message}", "LatentNodesSystem");
                }
            };

            string jsonString = File.ReadAllText(filePath);
            var promptPreset = JsonConvert.DeserializeObject<PromptPreset>(jsonString, settings);
            promptPreset?.ReconnectNodes();

            return promptPreset;
        }
        catch (Exception ex)
        {
            Output.Log($"Algo salió mal al cargar PromptPreset: {ex.Message}", "LatentNodesSystem");
            return null;
        }
    }

    public static PromptPreset? ImportPromptPreset(string filePath, bool addToPresets = true)
    {
        //is comfyui
        if(Path.GetExtension(filePath) == ".json")
        {
             var preset = Comfy.ImportWorkflow(filePath);

            if(addToPresets)
            Instance.AddPreset(preset, false);

            return preset;
        }

        //is promptpreset
        PromptPreset? promptPreset = LoadPromptPreset(filePath);
        if (promptPreset is not null)
        {
            if (addToPresets)
            {
                Instance.PromptPresets.Add(promptPreset);
                Instance.SelectedPreset = promptPreset;
            }
            promptPreset.Name = Namer.SetName(Path.GetFileNameWithoutExtension(filePath), GenerationManager.Instance.PromptPresets);
            return promptPreset;
        }
        else
            return null;


    }

    internal static void ImportComfyUINodes(string filePath)
    {
        var workflow = Comfy.ImportWorkflow(filePath);
        GenerationManager.Instance.AddPreset(workflow);

        GenerationManager.ShowNodeErrors(workflow);
    }

    public static void ShowNodeErrors(PromptPreset preset)
    {
        if (!preset.HasErrors()) return;

        List<string> s = preset.Requirements.MissingNodeTypes;
        string concatenatedNodes = String.Join("\n", s);
        string concatenatedModels = String.Join("\n", preset.Requirements.MissingModels);
        M_MessageBox.Show($"You have to install missing nodes and models, or Refresh PromptPreset:\n{concatenatedNodes}\n{concatenatedModels}", "Missing nodes or models", System.Windows.MessageBoxButton.OK);

    }

    static void InstallMissingNodes()
    {
        void okPressed()
        {
            ManualAPI.SelectedPreset.Refresh();
        }
        M_MessageBox.Show("you want to install missing nodes?", "Missing nodes", System.Windows.MessageBoxButton.OKCancel, okPressed);
    }


    //void RegisterAllLatentNodes() // LAG
    //{
    //    RegisteredNodes.Clear();
    //    var latentType = typeof(LatentNode);
    //    var derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
    //        .SelectMany(assembly => assembly.GetTypes())
    //        .Where(type => type.IsSubclassOf(latentType) && !type.IsAbstract);

    //    foreach (var type in derivedTypes)
    //    {
    //        RegisterNode(type.Name, "", () => (LatentNode)Activator.CreateInstance(type), "");
    //    }

    //}








    //--------------------------------------------------------------------------- PROMPT PRESETS (GLOBAL PROYECT) ----------------------------------------\\

    public ObservableCollection<Prompt> Prompts { get; set; } = new();


    [JsonConverter(typeof(PromptPresetConverter))]
    public ObservableCollection<PromptPreset> PromptPresets { get; set; } = new();

    [ObservableProperty] [property: JsonIgnore] private PromptPreset _selectedPreset = null;

    public delegate void PromptPresetHandler(PromptPreset preset);
    public static event PromptPresetHandler OnPromptPresetChanged;



    // --------- initialize --------- \\
    private bool changedOnce2 = false;
    partial void OnSelectedPresetChanged(PromptPreset? oldValue, PromptPreset newValue)
    {
        if (!changedOnce2)
        {
            SelectedPreset = AppModel.LoadSelectedInCollection(PromptPresets, newValue, nameof(newValue.Name)) ?? SelectedPreset;
            changedOnce2 = true;
        }

        if (oldValue != null && newValue != null && newValue.IsUpdateDriversPrompt)
        {
            // newValue.UpdateDriversSourcePrompt(newValue.Prompt, oldValue.Prompt);
            newValue.Prompt = oldValue.Prompt;
        }
        

        InvokeUpdatePromptPreset(newValue);
    }
    public void InvokeUpdatePromptPreset()
    {
        InvokeUpdatePromptPreset(ManualAPI.SelectedPreset);
    }
    public void InvokeUpdatePromptPreset(PromptPreset value)
    {
        OnPromptPresetChanged?.Invoke(value);
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void AddPreset()
    {
        PromptPreset preset = new PromptPreset();
        AddPreset(preset);
    }


    public void AddPrompt(Prompt prompt)
    {
        Prompts.Add(prompt);
    }
    /// <summary>
    /// ignored if preset already has added
    /// </summary>
    /// <param name="preset"></param>
    /// <param name="setDefaultName"></param>
    public void AddPreset(PromptPreset preset, bool setDefaultName = true, bool asSelected = true)
    {
        if (PromptPresets.Contains(preset)) return;

        if(setDefaultName == true)
           preset.Name = Namer.SetName(preset.Name, PromptPresets);

        PromptPresets.Add(preset);


        if (Prompts.Any())
            preset.Prompt = ManualAPI.SelectedPreset != null ? ManualAPI.SelectedPreset.Prompt : Prompts[0];

        if (asSelected)
          SelectedPreset = preset;  
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void DuplicatePreset()
    {
        DuplicatePreset(SelectedPreset);
    }
    public void DuplicatePreset(PromptPreset preset)
    {
        PromptPreset duplicate = preset.Duplicate();
        AddPreset(duplicate);
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void DeletePreset()
    {
        DeletePreset(SelectedPreset);
    }
    public void DeletePreset(PromptPreset preset)
    {
        int index = PromptPresets.IndexOf(preset);
        PromptPresets.Remove(preset);
        SelectedPreset = PromptPresets.ItemByIndex(index);

        if (PromptPresets.Count != 0)
        {
            if (index == PromptPresets.Count)
            {
                SelectedPreset = PromptPresets[index - 1];
            }
            else if (PromptPresets[index] != null)
            {
                SelectedPreset = PromptPresets[index];
            }
        }
        else
        {
            SelectedPreset = null;
        }

    }


    [RelayCommand]
    [property: JsonIgnore]
    public void NewPrompt()
    {
        var prompt = new Prompt();
        prompt.Name = Namer.SetName(prompt, GenerationManager.Instance.Prompts);
        GenerationManager.Instance.Prompts.Add(prompt);
        SelectedPreset.Prompt = prompt;
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void DuplicatePrompt()
    {
        var prompt = ManualAPI.SelectedPreset.Prompt.Clone();
        prompt.Name = Namer.SetName(prompt, GenerationManager.Instance.Prompts);
        GenerationManager.Instance.Prompts.Add(prompt);
        SelectedPreset.Prompt = prompt;
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void DeletePrompt() => DeletePrompt(true);
    public void DeletePrompt(bool asignAnother)
    {
        var prompts = GenerationManager.Instance.Prompts;
        var selectedPrompt = SelectedPreset.Prompt;

        if (selectedPrompt == null || !prompts.Contains(selectedPrompt))
        {
            // No hay un prompt seleccionado o el prompt seleccionado no está en la lista
            return;
        }

        int selectedIndex = prompts.IndexOf(selectedPrompt);

        if (asignAnother)
        {
            // Determinar el nuevo índice a seleccionar
            int newSelectedIndex = selectedIndex - 1;

            // Asegurarse de que el índice no sea negativo
            if (newSelectedIndex < 0 && prompts.Count > 0)
            {
                newSelectedIndex = 0;
            }

            // Seleccionar el nuevo prompt, si hay alguno
            if (newSelectedIndex >= 0 && newSelectedIndex < prompts.Count)
            {
                SelectedPreset.Prompt = prompts[newSelectedIndex];
            }
            else
            {
                SelectedPreset.Prompt = null; // O maneja esto de otra manera si prefieres
            }
        }

        // Eliminar el prompt seleccionado
        prompts.Remove(selectedPrompt);
    }




    public static void SetReachableConnection(NodeOption? nodeop)
    {
        if (nodeop is null)
        {
            foreach (var node in ManualAPI.SelectedPreset.LatentNodes)
            {
                foreach (var field in node.Fields)
                {
                    field.IsReachable = true;
                }
            }
        }
        else
        {
            foreach (var node in ManualAPI.SelectedPreset.LatentNodes)
            {
                foreach (var field in node.Fields)
                {
                    if (field.Direction != NodeOptionDirection.Field && nodeop != field && !nodeop.HasType(field))
                       field.IsReachable = false;
                }
            }
        }
    }




    public static void OnRegisteredInvoke(Action action)
    {
        OnNodesRegistered += GenerationManager_OnNodesRegistered;
        void GenerationManager_OnNodesRegistered()
        {
            action();
            OnNodesRegistered -= GenerationManager_OnNodesRegistered;
        }
    }







    #region ----------------------------------------------------------------------- SERIALIZE


    //[JsonProperty]
    //List<Graph> PromptPresetsId
    //{
    //    get => PromptPresets.Select(preset => Comfy.LoadWorkflow(preset)).ToList();
    //}



    //  [JsonProperty]
    //  [JsonConverter(typeof(PromptPresetConverter))]
    //  List<Graph> PromptPresetsId { get; set; }

    [JsonProperty]
    int SelectedPresetId
    {
        get => PromptPresets.IndexOf(SelectedPreset);
        set => OnRegisteredInvoke(()=> { SelectedPreset = PromptPresets.ItemByIndex(value); });
        
    }


    //USEFUL
    [System.Runtime.Serialization.OnDeserialized]
    internal void OnDeserializedMethod(System.Runtime.Serialization.StreamingContext context)
    {
   


    }
    internal static void RefreshPromptPreset() => GenerationManager.RefreshPromptPreset(GenerationManager.Instance.SelectedPreset);
    internal static void RefreshPromptPreset(PromptPreset selectedPreset)
    {

        int index = GenerationManager.Instance.PromptPresets.IndexOf(ManualAPI.SelectedPreset);
        GenerationManager.Instance.BuildRegisterNodes();
        GenerationManager.OnRegisteredInvoke(() =>
        {
            ManualAPI.SelectedPreset = GenerationManager.Instance.PromptPresets.ItemByIndex(index);
        });
    }



    #endregion -----------------------------------------------------------------------

}

//-------------------------------------------------------------------------------------------------------------------------------------------- SERIALIZE PROMTP PRESETS WORKFLOW
public class PromptPresetConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<Graph>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartArray)
        {
           

            // Deserializar la lista de Graph a ObservableCollection<PromptPreset>
            var pp = existingValue as ObservableCollection<PromptPreset>;
            pp.Clear();

            JArray array = JArray.Load(reader);
            graphList = array.ToObject<List<Graph>>();

            if (!array.Any())
            {
                GenerationManager.OnNodesRegistered += AsignDefaultPreset;
                return existingValue;
            }

            GenerationManager.OnNodesRegistered += AsignPromptPresets;
            return pp;
        }
        else
        {
            GenerationManager.OnNodesRegistered += AsignDefaultPreset;
            return existingValue;
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        // Serializar ObservableCollection<PromptPreset> a lista de Graph
        var presets = (ObservableCollection<PromptPreset>)value;
        var graphList = presets.Select(preset => Comfy.LoadWorkflow(preset)).ToList();
        JArray array = JArray.FromObject(graphList, serializer);
        array.WriteTo(writer);
    }

    List<Graph>? graphList;

    void AsignPromptPresets() //por si buscabas: GenerationManager.OnRegisteredInvoke(()=>{   });  
    {
        if (graphList != null)
        {
            var presets = graphList.Select(graph => Comfy.WorkflowToPreset(graph)).ToList();
            foreach (var preset in presets)
            {
                GenerationManager.Instance.PromptPresets.Add(preset);
            }
            
            graphList = null;
        }
        GenerationManager.OnNodesRegistered -= AsignPromptPresets;

    }


    void AsignDefaultPreset()
    {
        GenerationManager.Instance.InstantiatePromptPreset();
        GenerationManager.OnNodesRegistered -= AsignDefaultPreset;//AsignPromptPresets;
    }
}






//------------------------------------------------------------------------------------------------------------------------------ LINE CONNECTION
public partial class LineConnection : ObservableObject, IPositionable, IId
{
    [ObservableProperty] [property: JsonIgnore] private NodeOption output;
    [ObservableProperty] [property: JsonIgnore] private NodeOption input;

    [ObservableProperty] bool isSelected;
    public Guid Id { get; set; } = Guid.NewGuid();
    public int LinkId { get; set; }
    public Guid OutputId { get; set; }
    public Guid InputId { get; set; }

    public Guid OutputNodeId { get; set; }
    public Guid InputNodeId { get; set; }

    [JsonIgnore] public string Type0 { get; set; }

    public LineConnection(NodeOption originOutput, NodeOption targetInput)
    {
        Output = originOutput;
        Input = targetInput;

        OutputId = Output.Id;
        InputId = Input.Id;

        OutputNodeId = Output.AttachedNode.Id;
        InputNodeId = Input.AttachedNode.Id;

        UpdatePosition();
    }

    WrapperNode? FindLastWrapper(WrapperNode? wrapper)
    {
        if (wrapper == null || wrapper.ParentWrap == null)
            return wrapper;

        while (wrapper.ParentWrap != null)
        {
            wrapper = wrapper.ParentWrap;
        }

        return wrapper;
    }


    public void UpdatePosition()
    {
        if (Output == null || Output.UINode == null || Input == null || Input.UINode == null)
            return;

        var inputWraper = FindLastWrapper(Input.AttachedNode.ParentWrap);
        var outputWraper = FindLastWrapper(Output.AttachedNode.ParentWrap);
        //wrapped
        if(inputWraper != null && outputWraper != null)
        {
            UpdatePos(outputWraper.UINode, Input.AttachedNode.ParentWrap.UINode);
        }
        else if (inputWraper != null)
        {
            UpdatePos(Output.UINode.DragPointO, inputWraper.UINode);
        }
        else if (outputWraper != null)
        {
            UpdatePos(outputWraper.UINode, Input.UINode.DragPointI);
        }

        else //normal
        {
            
            if (Input.Direction == NodeOptionDirection.Input || Input.Direction == NodeOptionDirection.InputField)
            {
                FrameworkElement dragO = Output.UINode.DragPointO;
                FrameworkElement dragI = Input.UINode.DragPointI;

                if (Input.AttachedNode.IsCollapsed)
                {
                    dragI = Input.AttachedNode.UINode;
                }

                if (Output.AttachedNode.IsCollapsed)
                {
                    dragO = Output.AttachedNode.UINode;
                }

                UpdatePos(dragO, dragI);
            }
          //  else if (Input.Direction == NodeOptionDirection.InputField)
            //    UpdatePos(Output.UINode.DragPointO, Input.UINode.DragPointIF);
        }

    }
    public void UpdatePos(FrameworkElement dragOutput, FrameworkElement dragInput)
    {
        if (Output.UINode.GetCanvas is CanvasAreaControl canvas && dragOutput != null && dragInput != null)
        {
            StartPoint = canvas.elementPositionCenter(dragOutput);

            Point endPoint2 = canvas.elementPositionCenter(dragInput);

            if (endPoint2 != new Point(0, 0))
                EndPoint = endPoint2.RelativeTo(StartPoint);
        }
    }
    public void UpdatePos(FrameworkElement dragOutput, FrameworkElement dragInput, CanvasAreaControl canvas)
    {
        if (canvas != null && dragInput != null)
        {
            if (dragOutput != null)
                StartPoint = canvas.elementPositionCenter(dragOutput);

            Point endPoint2 = canvas.elementPositionCenter(dragInput);

            if (endPoint2 != new Point(0, 0))
                EndPoint = endPoint2.RelativeTo(StartPoint);

        }

    }


    [NotifyPropertyChangedFor(nameof(StartPoint))]
    [ObservableProperty] private float positionGlobalX = 0;
    [NotifyPropertyChangedFor(nameof(StartPoint))]
    [ObservableProperty] private float positionGlobalY = 0;

    [NotifyPropertyChangedFor(nameof(EndPoint))]
    [ObservableProperty] private float positionGlobalEndX = 0;
    [NotifyPropertyChangedFor(nameof(EndPoint))]
    [ObservableProperty] private float positionGlobalEndY = 0;

    public LineConnection()
    {
            
    }
    [JsonIgnore] public Point StartPoint
    {
        get => new Point(PositionGlobalX, PositionGlobalY);
        set
        {
            PositionGlobalX = (float)value.X;
            PositionGlobalY = (float)value.Y;

        }
    }
    [JsonIgnore] public Point EndPoint
    {
        get => new Point(PositionGlobalEndX, PositionGlobalEndY);
        set
        {
            PositionGlobalEndX = (float)value.X;
            PositionGlobalEndY = (float)value.Y;

        }
    }


    public LineConnection(Point point0, string type)
    {
        Type0 = type;
        UpdatePosition(point0, new Point(0, 0));
    }
    public LineConnection(Point point0) // start
    {
        UpdatePosition(point0, new Point(0,0));
    }
    public LineConnection(Point point0, Point point1)
    {
        UpdatePosition(point0, point1);
    }
  

    public void UpdatePosition(Point point0, Point point1)
    {
        PositionGlobalX = (float)point0.X;
        PositionGlobalY = (float)point0.Y;

        PositionGlobalEndX = (float)point1.X;
        PositionGlobalEndY =  (float)point1.Y;
    }
    public void UpdatePositionRelative(Point point0, Point point1)
    {
        PositionGlobalX = (float)point0.X;
        PositionGlobalY = (float)point0.Y;

        PositionGlobalEndX = (float)point0.X + (float)point1.X;
        PositionGlobalEndY = (float)point0.X + (float)point1.Y;
    }
    public void UpdatePositionEnd(Point point1)
    {
        UpdatePosition(new Point(PositionGlobalX, PositionGlobalY), point1);
    }
    public void UpdatePositionStart(Point point0)
    {
        UpdatePosition(point0, new Point(PositionGlobalEndX, PositionGlobalEndY));
    }


    #region ----------------------------------------------------------------------- SERIALIZE
    //public string TypeName
    //{
    //    get => Type0;//?.AssemblyQualifiedName;
    //    set => Type0 = value;// == null ? null : Type.GetType(value);
    //}

    #endregion -----------------------------------------------------------------------

}


//------------------------------------------------------------------------------------------------------------------------------ NODE BASE
public partial class NodeBase : ObservableObject, IPositionable, ISelectable, INamable, IId, IDisposable
{
    //UI
    internal bool ui_isLoaded = false;


    [JsonIgnore] public PromptPreset AttachedPreset { get; internal set; }
    [JsonIgnore] public NodeView UINode { get; internal set; }
    [JsonIgnore] public Action? OnUINodeLoaded { get; internal set; }


    [ObservableProperty] bool isCollapsed = false;
    internal void SwitchCollapse()
    {
        IsCollapsed = !IsCollapsed;
    }


    [ObservableProperty] [property: JsonIgnore] int stepProgress = 1;
    [ObservableProperty] [property: JsonIgnore] float progress = 0;

    [ObservableProperty] [property: JsonIgnore] bool isWorking = false;
    [ObservableProperty] [property: JsonIgnore] bool isError = false;
    [ObservableProperty] [property: JsonIgnore] bool isException = false;

    [ObservableProperty] [property: JsonIgnore] bool enablePreview = false;

    [ObservableProperty] [property: JsonIgnore] SKBitmap? previewImage;
  //  [ObservableProperty][property: JsonIgnore] ImageSource? previewImage;

    [ObservableProperty] bool isVisible = true;

    // special nodes
    [ObservableProperty] [property: JsonIgnore] bool isOutput = false;
    [ObservableProperty] [property: JsonIgnore] bool isEditable = false;
    public WrapperNode? ParentWrap { get; set; } = null;


    [ObservableProperty] string name = "node";
    [ObservableProperty] [property: JsonProperty("colorbg")] Color? color;
    [ObservableProperty] [property: JsonIgnore] bool isSelected;
    partial void OnIsSelectedChanged(bool value)
    {
        if(value)
        {
            foreach ( var nodeop in Fields)
                foreach (var lines in nodeop.LineConnect)
                    lines.IsSelected = true;
        }
        else
        {
            foreach (var nodeop in Fields)
                foreach (var lines in nodeop.LineConnect)
                    lines.IsSelected = false;
        }
    }

    [ObservableProperty] private float positionGlobalX = 0;
    [ObservableProperty] private float positionGlobalY = 0;

    public Point Position
    {
        get
        {
            return new Point(PositionGlobalX, PositionGlobalY);
        }
        set
        {
            PositionGlobalX = (float)value.X;
            PositionGlobalY = (float)value.Y;
        }
    }
    public Size Scale
    {
        get
        {
            return new Size(SizeX, SizeY);
        }
        set
        {
            SizeX = (float)value.Width;
            SizeY = (float)value.Height;
        }
    }
    partial void OnPositionGlobalXChanged(float value)
    {
        UpdatePosition();
    }
    partial void OnPositionGlobalYChanged(float value)
    {
        UpdatePosition();
    }
    public void UpdatePosition()
    {
        foreach (var field in Fields)
        {
            foreach (var line in field.LineConnect)
            {
                line.UpdatePosition();
            }

        }
    }

    public void ChangePosition(Point p)
    {
        PositionGlobalX = (float)p.X;
        PositionGlobalY = (float)p.Y;
    }

    [ObservableProperty] private float sizeX = 200;//128;
    [ObservableProperty] private float sizeY = 192;
    [JsonIgnore] public bool IsCustomSize = false;
    partial void OnSizeXChanged(float value)
    {
        UpdatePosition();
    }
    partial void OnSizeYChanged(float value)
    {
        UpdatePosition();
    }
    public Point GetPosition()
    {
        return new Point(PositionGlobalX, PositionGlobalY);
    }

    [JsonIgnore] public List<NodeOption> Inputs { get; set; } = new();
    [JsonIgnore] public List<NodeOption> Outputs { get; set; } = new();

    /// <summary>
    /// only Field, parameters
    /// </summary>
    [JsonIgnore] public List<NodeOption> WidgetFields { get; set; } = new();

    private ObservableCollection<NodeOption> _fields = new();

    /// <summary>
    /// all the fields, input, output, widgets
    /// </summary>
    public ObservableCollection<NodeOption> Fields
    {   get => _fields;
        set
        {
            _fields = value;
        } 
    }

    public string NameType { get; set; } = "nodeType";
    public NodeBase()
    {
        Fields.CollectionChanged += Fields_CollectionChanged;
    }
    public void AddNodeOption(NodeOption nodeop)
    {
        Fields.Add(nodeop);
        nodeop.AttachedNode = this;
    }
    public NodeOption AddInput(string name, string type)
    {
        var dv = async () => await Task.FromResult<object?>(null);
        return AddInput(name, type, dv);
    }
    public NodeOption AddInput(string name, string type, object defaultValue)
    {
        var dv = async () => await Task.FromResult<object?>(defaultValue);
        return AddInput(name, type, dv);
    }
    public NodeOption AddInput(string name, string type, Func<Task<object?>> defaultValueTask)
    {
        var direction = NodeOptionDirection.Input;
        var n = new NodeOption(name, direction, type, defaultValueTask );

        Fields.Add(n);
        n.AttachedNode = this;
        n.InvokeFieldValueChanged();
        return n;
    }

    public NodeOption AddOutput(string name, string type, Func<Task<object?>> defaultValueTask)
    {
        var direction = NodeOptionDirection.Output;

        var n = new NodeOption(name, direction, type,  defaultValueTask );

        Fields.Add(n);

        n.AttachedNode = this;
        return n;
    }
    public NodeOption AddOutput(string name, string type)
    {
        var direction = NodeOptionDirection.Output;

        var n = new NodeOption(name, direction, type);

        Fields.Add(n);

        n.AttachedNode = this;
        return n;
    }

    public NodeOption AddInputField(string name, string type, object? fieldValue, Type elementType)
    {
        var n = _setNodeopField(name, type, fieldValue, null, NodeOptionDirection.InputField);
        AsignElement(n, elementType);
        return n;
    }
    public NodeOption AddInputField(string name, string type, object? fieldValue, IManualElement element)
    {
        return AddInputField(name, type, fieldValue, element);
    }
    public NodeOption AddField(string name, string type, object? fieldValue, IManualElement element)
    {
        return _setNodeopField(name, type, fieldValue, element, NodeOptionDirection.Field);
    }

    public NodeOption AddField(string name, string type, object? fieldValue, Type elementType)
    {
        var n = _setNodeopField(name, type, fieldValue, null, NodeOptionDirection.Field);  
        AsignElement(n, elementType);
        return n;
    }

    NodeOption _setNodeopField(string name, string type, object? fieldValue, IManualElement element, NodeOptionDirection direction)
    {
        var n = new NodeOption(name, direction, type);
        Fields.Add(n);
        n.AttachedNode = this;

        n.FieldValue = fieldValue;
        n.FieldElementDefault = element;
        return n;
    }


    void AsignElement(NodeOption n, Type elementType)
    {
        if (typeof(IManualElement).IsAssignableFrom(elementType))
        {
            n.FieldElementDefault = (IManualElement)AppModel.Instantiate(elementType);
        }
        else
        {
            throw new ArgumentException("El tipo de control debe implementar IManualElement.", nameof(elementType));
        }

    }


    public NodeOption FindFieldOrNew(string name, NodeOptionDirection direction ,string type)
    {
        
        var f =  Fields.FirstOrDefault(node => node.Name == name);
        if (f != null)
            return f;
        else
        {
            var n = new NodeOption(name, direction, type);
            AddNodeOption(n);
            return n;
        }
    }
    public NodeOption FindField(string name)
    {
        return Fields.FirstOrDefault(node => node.Name == name);
    }
    public async Task<T> GetFieldValue<T>(string nameField)
    {
        return await FindField(nameField).GetValue<T>();
    }

    public void AvoidOverlapping(IEnumerable<NodeBase> nodesCollection)
    {
        const float padding = 129; // Espacio adicional para evitar solapamientos
        bool overlap;

        do
        {
            overlap = false; // Resetear la indicación de solapamiento

            foreach (var existingNode in nodesCollection)
            {
                if (Math.Abs(this.PositionGlobalX - existingNode.PositionGlobalX) < padding &&
                    Math.Abs(this.PositionGlobalY - existingNode.PositionGlobalY) < padding)
                {
                    // Si hay solapamiento, mover el nuevo nodo y marcar para re-verificar
                    this.PositionGlobalX += padding;
                    this.PositionGlobalY += padding;
                    overlap = true; // Marcar que hubo solapamiento
                    break; // Salir del bucle para reiniciar la verificación
                }
            }
        } while (overlap); // Continuar mientras haya solapamiento
    }


    public override string ToString()
    {
        return $"Node: {Name}, Fields: {Fields.Count}";
    }


    internal void Refresh(NodeBase newNodeBase)
    {
        Fields.Clear();
        Inputs.Clear();
        Outputs.Clear();
        WidgetFields.Clear();

        foreach (var field in newNodeBase.Fields)
        {
            Fields.Add(field);
        }
    }

    public void ConnectByPass(NodeBase node1, NodeBase node2)
    {
        if (this.AttachedPreset == null)
            node1.AttachedPreset.AddNode((LatentNode)this);

        var newNode = this;
        foreach (var field1 in node1.Fields)
        {
            if (field1.IsConnected() && !field1.IsInputOrField())
            {
                var field2 = field1.Connections[0];

                if (field2 != null && field2.IsInputOrField())
                {
                    foreach (var inputField in newNode.Fields.Where(f => f.IsInputOrField()))
                    {
                        foreach (var outputField in newNode.Fields.Where(f => !f.IsInputOrField()))
                        {
                            if (field1.HasType(inputField) && field2.HasType(outputField))
                            {
                                // Desconectar los nodos originales
                                field1.Disconnect();

                                // Conectar node1 -> newNode -> node2
                                field1.Connect(inputField);
                                outputField.Connect(field2);

                                Console.WriteLine($"Injected {newNode} between {node1} and {node2}");
                                return;
                            }
                        }
                    }
                }
            }


        }
        

        throw new InvalidOperationException("No se pudo inyectar el nuevo nodo entre los nodos proporcionados.");
    }
    public void ConnectByPass(NodeBase node1)
    {
        bool presetNull = false;
        if (this.AttachedPreset == null)
        {
            presetNull = true;
            node1.AttachedPreset.AddNode((LatentNode)this);
      
        }

        bool canByPassed = false;
        var newNode = this;
        foreach (var field1 in node1.Fields)
        {
            if (!field1.IsInputOrField() && field1.IsConnected())
            {
                var inputField = newNode.Fields.FirstOrDefault(f => f.IsInputOrField() && field1.HasType(f));

                if (inputField != null)
                {
                    var outputField = newNode.Fields.FirstOrDefault(f => !f.IsInputOrField() && field1.HasType(f));

                    if (outputField != null)
                    {
                        var originalConnections = field1.Connections.ToList();
                        foreach (var connection in originalConnections)
                        {
                            if (field1.IsInputOrField() != connection.IsInputOrField())
                            {
                                outputField.Connect(connection);
                                canByPassed = true;
                            }
                        }

                        field1.Connect(inputField);
                    }
                }
            }
        }

        if (canByPassed)
        {
            float durat = 0.2f;

            //this. is newNode;

            var thisPos = /*this.PositionGlobalX = */ (node1.PositionGlobalX + node1.SizeX) - this.SizeX;
          //  this.PositionGlobalY = node1.PositionGlobalY;

            RendGPU.AnimateFloat(
                updateAction: value => this.PositionGlobalX = value,
                from: node1.PositionGlobalX,
                to: thisPos,
                duration: durat,
                updateRender: false,
                onCompleted: () => AppModel.InvokeNext(this.UpdatePosition));

            var thisPosY = node1.PositionGlobalY;
            RendGPU.AnimateFloat(
           updateAction: value => this.PositionGlobalY = value,
           from: node1.PositionGlobalY,
           to: thisPosY,
           duration: durat,
           updateRender: false);

            var node1Pos = /*node1.PositionGlobalX =*/ thisPos - node1.SizeX - 30;

            RendGPU.AnimateFloat(
          updateAction: value => node1.PositionGlobalX = value,
          from: node1.PositionGlobalX,
          to: node1Pos,
          duration: durat,
          updateRender: false,
          onCompleted: () => AppModel.InvokeNext(node1.UpdatePosition));

            if (node1.Inputs.Any() && node1.Inputs.First()?.ConnectedNode()?.First() is NodeBase node2)
            {
               var node2Pos = /*node2.PositionGlobalX =*/ node1Pos - node2.SizeX - 30;
                RendGPU.AnimateFloat(
                    updateAction: value => node2.PositionGlobalX = value,
                    from: node2.PositionGlobalX,
                    to: node2Pos,
                    duration: durat,
                    updateRender: false,
                    onCompleted: () => AppModel.InvokeNext(node2.UpdatePosition));
            }


            AppModel.InvokeNext(() =>
            {
                this.UpdatePosition();
                node1.UpdatePosition();
            });
        }

        Console.WriteLine($"Injected {newNode} between {node1} and its connections.");
    }

    public NodeBase CheckOverlappingLines()
    {
        if (AttachedPreset == null)
            return null;

        // Obtener los límites del nodo actual
        float thisLeft = PositionGlobalX;
        float thisRight = PositionGlobalX + 20;
        float thisTop = PositionGlobalY;
        float thisBottom = PositionGlobalY + SizeY;

        foreach (var node in AttachedPreset.LatentNodes)
        {
            if (node == this) // Ignorar el nodo actual
                continue;

            // Obtener los límites del nodo iterado
            float nodeLeft = node.PositionGlobalX + (node.SizeX - 20);
            float nodeRight = node.PositionGlobalX + node.SizeX;
            float nodeTop = node.PositionGlobalY;
            float nodeBottom = node.PositionGlobalY + node.SizeY;

            // Verificar si se solapan
            bool isOverlapping = thisRight > nodeLeft &&
                                 thisLeft < nodeRight &&
                                 thisBottom > nodeTop &&
                                 thisTop < nodeBottom;

            if (isOverlapping)
            {
                return node; // Nodo solapado encontrado
            }
        }

        return null; // No se encontró ningún nodo solapado
    }

    public bool CanConnectByPass(NodeBase node1)
    {
        //TODO: hacerlo bien luego
        return Inputs.Any() && !Inputs[0].IsConnected();
    }



    internal NodeOption ConvertToInput(string name)
    {
        var field = FindField(name);
        return ConvertToInput(field);
    }
    internal NodeOption ConvertToWidget(string name)
    {
        var field = FindField(name);
        return ConvertToWidget(field);
    }
    internal NodeOption ConvertToInput(NodeOption field)
    {
      
        if(field != null && field.Direction == NodeOptionDirection.Field && field.Direction != NodeOptionDirection.InputField)
        {
            field.Direction = NodeOptionDirection.InputField;

          //  WidgetFields.Remove(field);
            Inputs.Add(field);
          
        }
        return field;
    }
    internal NodeOption ConvertToWidget(NodeOption field)
    {
        if (field.IsConnected())
            field.Disconnect();

        if (field != null && field.Direction == NodeOptionDirection.InputField && field.Direction != NodeOptionDirection.Field)
        {
            field.Direction = NodeOptionDirection.Field;

            Inputs.Remove(field);
           // WidgetFields.Add(field);
          
        }
        return field;
    }

 

    #region ----------------------------------------------------------------------- SERIALIZE

    Guid _id = Guid.NewGuid();

    public Guid Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public int? IdNode { get; set; } = null;

    private void Fields_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (NodeOption newNodeOption in e.NewItems)
            {
                var existingNodeOption = _fields.FirstOrDefault(f => f.Name == newNodeOption.Name && f != newNodeOption && f.Direction == newNodeOption.Direction);
                if (existingNodeOption != null)
                {
                    // Actualizar el existingNodeOption con propiedades del newNodeOption
                    UpdateExistingNodeOption(existingNodeOption, newNodeOption);

                    // Eliminar el newNodeOption ya que solo se usó para la actualización
                    _fields.Remove(newNodeOption);

                    //REMOVE
                    // Manejo para un NodeOption completamente nuevo, si es necesario
                    if (newNodeOption.Direction == NodeOptionDirection.Input || newNodeOption.Direction == NodeOptionDirection.InputField)
                        Inputs.Remove(newNodeOption);
                    else if (newNodeOption.Direction == NodeOptionDirection.Field)
                        WidgetFields.Remove(newNodeOption);
                    else if (newNodeOption.Direction == NodeOptionDirection.Output)
                        Outputs.Remove(newNodeOption);
                }
                else //ADD, normal behaviour, it's already added to _fields. this is after added
                {
                    // Manejo para un NodeOption completamente nuevo, si es necesario
                    if (newNodeOption.Direction == NodeOptionDirection.Input || newNodeOption.Direction == NodeOptionDirection.InputField)
                        Inputs.Add(newNodeOption);
                    else if (newNodeOption.Direction == NodeOptionDirection.Field)
                        WidgetFields.Add(newNodeOption);
                    else if (newNodeOption.Direction == NodeOptionDirection.Output)
                        Outputs.Add(newNodeOption);

                    //asign id slot
                   // if(newNodeOption.Direction != NodeOptionDirection.Output || newNodeOption.Direction == NodeOptionDirection.Field)
                         newNodeOption.IdSlot ??= Fields.IndexOf(newNodeOption);
                }
            }
        }
    }

    private void UpdateExistingNodeOption(NodeOption existingNodeOption, NodeOption newNodeOption) //----------------------SERIALIZE CUSTOM: NODEOPTIONS
    {
        // Actualiza las propiedades necesarias del newNodeOption al existingNodeOption

        existingNodeOption.Name = newNodeOption.Name;
        existingNodeOption.Direction = newNodeOption.Direction;
        existingNodeOption.FieldValue = newNodeOption.FieldValue;
        existingNodeOption.Id = newNodeOption.Id;
        existingNodeOption.IdSlot = newNodeOption.IdSlot;
        //existingNodeOption.LineConnectionId = newNodeOption.LineConnectionId;
       // existingNodeOption.Type = newNodeOption.Type;
    }

    public virtual void Dispose()
    {
        //dispose
        foreach (var field in Fields)
        {
            field.Dispose();
        }
    }


    #endregion -----------------------------------------------------------------------


    
}

public class NodeOptionLinked : NodeOption
{
    public NodeOption AttachedNodeOption { get; set; }

    public NodeOptionLinked(string name, NodeOptionDirection direction, string type, object? defaultValue, IManualElement element)
        : base(name, direction, type, defaultValue, element)
    {

    }

}
//------------------------------------------------------------------------------------------------------------------------------ NODE OPTION
public partial class NodeOption : ObservableObject, IId, IDisposable, INamable, IDrivable
{
    [ObservableProperty] [property: JsonIgnore] Driver driver;
    partial void OnDriverChanged(Driver? oldValue, Driver newValue)
    {
        if (AttachedNode != null && AttachedNode.AttachedPreset != null)
        {
            if (newValue != null && !AttachedNode.AttachedPreset.Drivers.Contains(newValue))
            {
                AttachedNode.AttachedPreset.Drivers.Add(newValue);
            }

            if (oldValue != null)
            {
                AttachedNode.AttachedPreset.Drivers.Remove(oldValue);
            }
        }
    }



    [ObservableProperty] bool enabled = true;
    [JsonIgnore] public NodeOptionView UINode { get; internal set; }

    [JsonIgnore] public int TotalSteps = 0;
    [JsonIgnore] public int CurrentStep = 0;

    [JsonIgnore] public NodeBase AttachedNode { get; set; }

    [ObservableProperty] string name;

    [ObservableProperty] NodeOptionDirection direction;
    [ObservableProperty] [property: JsonIgnore] string type;

    [ObservableProperty] bool isReachable = true;

    [ObservableProperty] string toolTip = "";
    [ObservableProperty] bool isError = false;
    [JsonIgnore] public Point Position
    {
        get => AttachedNode.GetPosition();
    }
    public NodeOption(string name, NodeOptionDirection direction, string type, object? defaultValue, IManualElement element)
    {
        Name = name;
        Direction = direction;
        Type = type;

        var dv = async () => await Task.FromResult<object?>(defaultValue);
        DefaultValue = dv;
        FieldValue = defaultValue;

        FieldElementDefault = element;
    }
    public NodeOption(string name, NodeOptionDirection direction, string type, Func<Task<object?>> defaultValue)
    {
        Name = name;
        Direction = direction;
        Type = type;
        DefaultValue = defaultValue;
        SetId();
    }
    public NodeOption(string name, NodeOptionDirection direction, string type)
    {
        Name = name;
        Direction = direction;
        Type = type;
        SetId();
    }
    public NodeOption(string name, string type)
    {
        Name = name;
        Direction = NodeOptionDirection.Field;
        Type = type;
        SetId();
    }
    void SetId()
    {
        Id = Guid.NewGuid();
    }
    public NodeOption()
    {

    }


    [JsonIgnore] public IManualElement? FieldElementDefault;
    public IManualElement? GetFieldElement()
    {
        return FieldElementDefault?.Clone();
    }
    
    Func<Task<object?>> _defaultValue  = async () => await Task.FromResult<object?>(null);
    [JsonIgnore]  public Func<Task<object?>> DefaultValue 
    {
        get => _defaultValue;
        set
        {
            var a = this.Name;
            _defaultValue = value;
        }
    }

    /// <summary>
    /// you nedd to use await or else not works
    /// </summary>
    /// <returns></returns>
    public async Task<object?> GetValue()
    {
        return await GetValue<object>();
    }
    //------------------------------------------------------------------- GET VALUE
    public async Task<T> GetValue<T>()
    {
      
        object? value;

        if (ConnectionInput is null)
        {
            if (Direction == NodeOptionDirection.InputField || Direction == NodeOptionDirection.Field)
            {
                value = FieldValue;
            }
            else // input or THE OUTPUT OF IOutputNode (Generate)
            {
                if (DefaultValue is not null)
                {
                    // Espera la tarea y obtiene su resultado
                    value = await DefaultValue();
                }
                else
                {
                    value = null;
                }
            }
        }
        else // output
        {
            // Asegúrate de que GetValue<T> en ConnectionInput también es asíncrono
           // ConnectionInput.AttachedNode.IsWorking = true;
            AttachedNode.AttachedPreset.ExecutingNode(AttachedNode);
            value = await ConnectionInput.GetValue<T>();
            AttachedNode.AttachedPreset.ExecutingNode(AttachedNode);
            // ConnectionInput.AttachedNode.IsWorking = false;
        }

        //convert
        try
        {
            if (value is T tValue) 
                return tValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (InvalidCastException)
        {
            return default;
        }

    }

    //----------------------------------------------------------------------------- FIELD VALUE
    public Event<object?> ValueChanged;

    [ObservableProperty] [property: JsonIgnore] object? fieldValue = null;
    partial void OnFieldValueChanged(object? value)
    {
        if (IsError)
        {
            AttachedNode.IsError = false;
            IsError = false; 
        }

        InvokeFieldValueChanged(value);
    }
    internal void InvokeFieldValueChanged(object? value)
    {
        ValueChanged?.Invoke(value);
    }
    internal void InvokeFieldValueChanged()
    {
        InvokeFieldValueChanged(FieldValue);
    }

    private NodeOption? _connectionInput = null;    
     [JsonIgnore] public NodeOption? ConnectionInput
    {
        get => _connectionInput;
        set
        {
            if (_connectionInput != value)
            {
                _connectionInput = value;
                OnPropertyChanged(nameof(ConnectionInput));
            }
        }
    }
    public delegate void PromptPresetHandler(NodeOption? connection);
    public event PromptPresetHandler OnConnectionChanged;




    private List<NodeOption> _connectionOutput = new();
    [JsonIgnore] public List<NodeOption> ConnectionOutput
    {
        get => _connectionOutput;
        set
        {
            if (_connectionOutput != value)
            {
                _connectionOutput = value;
                OnPropertyChanged(nameof(ConnectionOutput));
            }
        }
    }



    List<LineConnection> _lineConnect = new();
   [JsonIgnore] public List<LineConnection> LineConnect
    {
        get => _lineConnect;
        set
        {
            _lineConnect = value;
            //LineConnectionId.Clear();
            //foreach (var line in value)
            //{
            //    LineConnectionId.Add(ManualAPI.GetId(line));
            //}
           
        }
    }
    public bool IsConnected()
    {
        return LineConnect != null && LineConnect.Any();
    }

    public List<NodeOption> Connections
    {
        get
        {
            if (ConnectionInput != null)
                return [ConnectionInput];
            else if (ConnectionOutput != null)
                return ConnectionOutput;
            else
                return [];
        }
    }


    //------------------------------------------------------------------------------ CONNECT NODES
    public void Connect(NodeBase node, string fieldName)
    {
        var toF = node.field(fieldName);

        if (this.AttachedNode.AttachedPreset == null)
        {
            node.AttachedPreset.AddNode((LatentNode)this.AttachedNode);

            var offsetX = 20;
            AttachedNode.PositionGlobalX = node.PositionGlobalX - AttachedNode.SizeX - offsetX;
            AttachedNode.PositionGlobalY = node.PositionGlobalY;

            Connect(toF);
        }
        else
          Connect(toF);
    }


    public void Connect(NodeOption nodeOption, LineConnection lineConnection = null)
    {
        if (ConnectionInput == nodeOption || this == nodeOption)
            return;

        LineConnection line = null;

        //target
        if (Direction == NodeOptionDirection.Input || Direction == NodeOptionDirection.InputField) // this is TARGET input, conecting input to output
        {
            if (nodeOption.ConnectionInput != null) // already connected, avoid 2 connections in input
                return;

            ConnectionInput = nodeOption;
            nodeOption.ConnectionOutput.Add(this);
            if (lineConnection == null)
            {
                line = new LineConnection(nodeOption, this);
                line.Type0 = nodeOption.Type;
            }
            else
            {
                lineConnection.Output = nodeOption;
                lineConnection.Input = this;
            }
        }
        else // this is output, conecting input to output from output
        {
            if (nodeOption.Direction == NodeOptionDirection.Output) // avoid connect output to output
                return;

            if (nodeOption.ConnectionInput != null) // already connected, avoid 2 connections in input
            {
                //disconect the node to reconnect the new
                nodeOption.Disconnect();
            }

            nodeOption.Connect(this);
            return;
        }

        if(lineConnection != null)
          line = lineConnection;


        if (AttachedNode != null && AttachedNode.AttachedPreset != null)
        {
            line.LinkId = Namer.RandomId(AttachedNode.AttachedPreset.LineConnections, (l, id) => l.LinkId == id, 1);
            AttachedNode.AttachedPreset.LineConnections.Add(line);
        }
        else
            Output.Error("internal error: something went wrong connecting nodes, nodes has no AttachedPreset", "LatentNodeSystem.NodeOption");

  
        LineConnect.Add(line);
        nodeOption.LineConnect.Add(line);

        //notify connection;
        nodeOption.OnConnectionChanged?.Invoke(this);
        OnConnectionChanged?.Invoke(nodeOption);
    }

    public void Disconnect()
    {
     

        if ((Direction == NodeOptionDirection.Input || Direction == NodeOptionDirection.InputField) && ConnectionInput != null)
        {
            var nodeOption = ConnectionInput;

            if (AttachedNode != null && AttachedNode.AttachedPreset != null && LineConnect.Count > 0)
                AttachedNode.AttachedPreset.LineConnections.Remove(LineConnect[0]);

            ConnectionInput.ConnectionOutput.Remove(this);
            ConnectionInput.LineConnect.Remove(LineConnect[0]);
            ConnectionInput = null;
            LineConnect.RemoveAt(0);

            //notify connection;
            nodeOption?.OnConnectionChanged?.Invoke(null);
            OnConnectionChanged?.Invoke(null);

        }
        else // output disconect all target inputs
        {
            foreach (var connection in ConnectionOutput.ToList())
            {
                connection.Disconnect();
            }

            LineConnect.Clear();
            ConnectionOutput.Clear();

        }


    }



    public override string ToString()

    {
        var v = DefaultValue != null ? "{value}" : "null";
        return $"{Name},{Direction}, DefaultValue: {v}, ConnectionInput: {ConnectionInput?.Name ?? "null"}";
    }


    internal void ConvertToInput()
    {
        AttachedNode.ConvertToInput(this);
    }

    internal void ConvertToField()
    {
        AttachedNode.ConvertToWidget(this);
    }
    internal bool IsInputOrField()
    {
        return Direction == NodeOptionDirection.Input || Direction == NodeOptionDirection.InputField;
    }

    public virtual void Dispose()
    {
        OnConnectionChanged = null;
        ValueChanged = null;
        this.Driver = null;
        OnDispose?.Invoke();
    }
    public Action OnDispose;



    //------------------------------------------------------- SUPPORT FOR MORE THAN 1 TYPE
    [JsonIgnore] public Dictionary<string, RegisterType> RegisteredTypes = new();

    /// <summary>
    /// add a supported type, specially for Output
    /// </summary>
    /// <param name="fieldType"></param>
    /// <param name="value"></param>
    internal void AddType(string fieldType, Func<object>? value = null, string? description = null)
    {
        var t = new RegisterType(value);
        t.Description = description;
        AddType(fieldType, t);
    }
    internal void AddType(string fieldType, RegisterType typeDesc)
    {
        RegisteredTypes[fieldType] = typeDesc;
    }
    internal bool HasType(NodeOption field)
    {
        if (this.Type == FieldTypes.ANY || field.Type == FieldTypes.ANY)
            return true;

        return this.Type == field.Type || this.RegisteredTypes.Any() && this.RegisteredTypes.ContainsKey(field.Type) || field.RegisteredTypes.Any() && field.RegisteredTypes.ContainsKey(this.Type);
    }


    public List<NodeBase?> ConnectedNode()
    {
        if (IsConnected())
        {
            return Connections.Select(a => a.AttachedNode).ToList();
        }
        else
            return null;
    }


    /// <summary>
    /// finds a node in the three
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public NodeBase FindNode(Func<NodeBase, bool> predicate)
    {
        var visited = new HashSet<NodeBase>();
        var forward = !this.IsInputOrField();
        return FindNodeRecursive(this.Connections, predicate, visited, forward);
    }

    private static NodeBase FindNodeRecursive(IEnumerable<NodeOption> currentConnections, Func<NodeBase, bool> predicate, HashSet<NodeBase> visited, bool searchForward)
    {
        foreach (var currentNodeOption in currentConnections)
        {
            if (currentNodeOption == null || visited.Contains(currentNodeOption.AttachedNode))
                continue;

            var currentNode = currentNodeOption.AttachedNode;
            visited.Add(currentNode);

            if (predicate(currentNode))
                return currentNode;

            var nextConnections = searchForward
                ? currentNode.Outputs.SelectMany(output => output.Connections)
                : currentNode.Inputs.SelectMany(input => input.Connections);

            var foundNode = FindNodeRecursive(nextConnections, predicate, visited, searchForward);
            if (foundNode != null)
                return foundNode;
        }

        return null;
    }


    #region ----------------------------------------------------------------------- SERIALIZE
    //[JsonIgnore] public string TypeName
    //{
    //    get => Type;//?.AssemblyQualifiedName;
    //    set => Type = value;// == null ? null : Type.GetType(value);
    //}



    //[JsonIgnore] public List<Guid> LineConnectionId = new();
    //public List<Guid> LineConnectionIds
    //{ 
    //    get
    //    {
    //        List<Guid> guids = new();
    //        if(Direction == NodeOptionDirection.Input || Direction == NodeOptionDirection.InputField)
    //        {
    //            if(ConnectionInput != null)
    //               guids.Add(ConnectionInput.Id);            
    //        }
    //        else
    //        {
    //            foreach (var item in ConnectionOutput)
    //            {
    //                if (item != null)
    //                    guids.Add(item.Id);
    //            }
    //        }
    //        return guids;
    //    }
    //    set
    //    {
    //        LineConnectionId = value;
    //        Output.Log("AAAAAAAAAAAAAAAA");
    //    }
    //}

    Guid _id;
    public Guid Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    /// <summary>
    /// slot_index
    /// </summary>
    public int? IdSlot { get; set; }

    public object? FieldValueId
    {
        get
        {
            if (FieldValue is LayerBase layer)
                return new ManualSerialization(layer);
            else
                return FieldValue;
        }
        set
        { 
            if (value is ManualSerialization ms)
                FieldValue = ms.Load();
            else
                FieldValue = value;
        }
    }

 



    #endregion -----------------------------------------------------------------------
}

public class RegisterType
{
    public RegisterType(Func<object>? fieldValueModified)
    {
        FieldValueModified = fieldValueModified;
    }
    public Func<object>? FieldValueModified { get; set; }
    public string? Description { get; set; }
}

public enum NodeOptionDirection
{
    Input,
    InputField,
    Output,
    Field,
}

public interface IOutputNode
{
    public Task Generate();
}


//----------------------------------------------------------------------------------------------------------------------------------------- NODE COLORS
public static class NodeColors
{
    public const string NoColor = "No Color";
    public const string Red = "Red";
    public const string Brown = "Brown";
    public const string Green = "Green";
    public const string Blue = "Blue";
    public const string PaleBlue = "Pale Blue";
    public const string Cyan = "Cyan";
    public const string Violet = "Violet";
    public const string Purple = "Purple";
    public const string Pink = "Pink";
    public const string Yellow = "Yellow";
    public const string Orange = "Orange";
    public const string Black = "Black";



    public static Color? ColorByName(string header)
    {
        switch (header)
        {
            case "No Color":
                return null;
            case "Red":
                return "#533".ToColor();
            case "Brown":
                return "#593930".ToColor();
            case "Green":
                return "#353".ToColor();
            case "Blue":
                return "#335".ToColor();
            case "Pale Blue":
                return "#3f5159".ToColor();
            case "Cyan":
                return "#355".ToColor();
            case "Violet":
                return "#436".ToColor();
            case "Purple":
                return "#535".ToColor();
            case "Pink":
                return "#644065".ToColor();
            case "Yellow":
                return "#653".ToColor();
            case "Orange":
                return "#7E544C".ToColor();
            case "Black":
                return "#000".ToColor();
            default:
                return null;
        }

    }

    public static void ChangeNodesColor(SelectionCollection<LatentNode>? nodes, string? hexColor = null)
    {
        ChangeNodesColor(nodes, hexColor?.ToColor());
    }
    public static void ChangeNodesColor(SelectionCollection<LatentNode>? nodes, Color? color)
    {
        if (nodes != null && color != null)
            nodes.ForEach(node => node.Color = color);
        else if (nodes != null)
            nodes.ForEach(node => node.Color = null);
    }


    public static Color? ColorByOutput(ComfyUINode node)
    {
        NodeOption nodeRef = null;
        if (node.Outputs.Any())
            nodeRef = node.Outputs[0];
        else if (node.Inputs.Any())
            nodeRef = node.Inputs[0];

        if (nodeRef != null)
        {
            if (node.IsOutput || nodeRef.IsInputOrField()) // output
                return ColorByName(NodeColors.PaleBlue);

            switch (nodeRef.Type)
            {
                case FieldTypes.INT:
                    return ColorByName(Blue);
                case FieldTypes.STRING:
                    return ColorByName(Green);
                case FieldTypes.FLOAT:
                    return ColorByName(Blue);
                case FieldTypes.BOOLEAN:
                    return ColorByName(Blue);
                case FieldTypes.OBJECT:
                    return ColorByName(Blue);
                case FieldTypes.IMAGE:
                    return ColorByName(Purple);
                case FieldTypes.CONTROL:
                    return ColorByName(Green);
                case FieldTypes.CONDITIONING:
                    return ColorByName(Orange);
                case FieldTypes.CLIP:
                    return ColorByName(Yellow);
                case FieldTypes.MODEL:
                    return ColorByName(Red);
                case FieldTypes.LATENT:
                    return ColorByName(Pink);
                case FieldTypes.VAE:
                    return ColorByName(Red);
                case FieldTypes.MASK:
                    return ColorByName(Green);
                case FieldTypes.LAYER:
                    return ColorByName(Violet);
                default:
                    break;
            }

        }



        return null;
    }

}

public static class FieldTypes
{
    //general
    public const string INT = "INT";
    public const string STRING = "STRING";
    public const string FLOAT = "FLOAT";
    public const string BOOLEAN = "BOOLEAN";
    public const string OBJECT = "OBJECT";
    public const string IMAGE = "IMAGE";
  
    //comfy
    public const string CONTROL = "CONTROL";
    public const string CONDITIONING = "CONDITIONING";
    public const string CLIP = "CLIP";
    public const string MODEL = "MODEL";
    public const string LATENT = "LATENT";
    public const string VAE = "VAE";
    public const string MASK = "MASK";
    public const string ANY = "ANY";

    //manual
    public const string LAYER = "LAYER";
    public const string KEYFRAME = "KEYFRAME";


    //RegisteredColors RegisteredTypeColors
    [JsonIgnore]
    public static Dictionary<string, string> TypeColors { get; set; } = new Dictionary<string, string>
{
    //general
    { INT, "c_blue" },
    { STRING, "c_yellow" },
    { FLOAT, "c_blue" },
    { BOOLEAN, "c_blue" },
    { OBJECT, "c_null" },
    { IMAGE, "c_violet" },
  
    // comfy
    { CONTROL, "c_green" },
    { CONDITIONING, "c_orange" },
    { CLIP, "c_yellow" },
    { MODEL, "c_purple" },
    { LATENT, "c_pink" },
    { VAE, "c_red" },
    { MASK, "c_green" },

    // manual
    { LAYER, "c_violet" },
};


    public static string TypeToColorName(string type)
    {
        if (type is null)
            return "c_null";

        if (TypeColors.TryGetValue(type, out string colorName))
        {
            return colorName;
        }
        return "c_null";
    }

    internal static IManualElement ElementByType(string type)
    {
        return type switch
        {
            BOOLEAN => new M_CheckBox(),
            STRING => new M_TextBox(),
            INT => new M_NumberBox(),
            FLOAT => new M_SliderBox(),
            _ => new M_TextBox(),
        };
    }

    public static string GetTypeLabel(object value)
    {
        var t = value?.GetType();

        switch (value)
        {
            case null:
                return "NULL";
            case int:
                return "INT";
            case float:
                return "FLOAT";
            case double d:
                // Verificar si el double es realmente un entero
                if (d == (int)d)
                {
                    return "INT"; // El double no tiene parte decimal, así que lo tratamos como INT
                }
                // Verificar si el double puede ser tratado como float sin perder precisión
                else if ((float)d == d)
                {
                    return "FLOAT"; // El double puede ser tratado como float
                }
                return "DOUBLE"; // Por defecto, si es un número con decimales que no encaja como INT o FLOAT
            default:
                return "STRING"; // Por defecto, para cualquier otro tipo
        }
    }

}








//----------------------------------------------------------------------------------------------------------------------------------- LATENT NODES

//---------------------------------------------------------------- MATH NODES
public partial class LatentNode : NodeBase
{

}

public partial class NumberNode : LatentNode
{
    NodeOption Num;
    public NumberNode()
    {
        Name = "Number";

        AddOutput("NumberX", FieldTypes.INT, NumberXValue);
        Num = AddInputField("Num", FieldTypes.INT, 2, typeof(M_NumberBox));
    }

    async Task<object?> NumberXValue()
    {
        await Task.Delay(2_000);
        return await Num.GetValue<object?>();
    }

}
public partial class MultiplyNode : LatentNode
{
    public MultiplyNode()
    {
        Name = "Multiply";

        AddOutput("Result", FieldTypes.INT, ResultValue);
        AddInput("NumberX", FieldTypes.INT, 2);

        AddInputField("NumberY", FieldTypes.INT, 2, typeof(M_NumberBox));
    }

    async Task<object?> ResultValue()
    {
        var x = await GetFieldValue<int>("NumberX");
        var y = await GetFieldValue<int>("NumberY");
        return x * y;
    }
   
}

public partial class OutputLogNode : LatentNode, IOutputNode
{
    static NodeOption result;
    public OutputLogNode()
    {
        Name = "Output Log";
        IsOutput = true;
        result = AddInput("Result", FieldTypes.OBJECT, "_error");
    }


   public async Task Generate()
    {
        var r = await result.GetValue<object?>();
        Output.Log(r);
    }
}


//----------------------------------------------------------------------------------------------------- LATENT NODES
public partial class LayerNode : LatentNode
{
    NodeOption Result, LayerRef, Normalize;
    public LayerNode()
    {
        Name = "Layer";

        Result = AddOutput("Layer", FieldTypes.LAYER, LayerRefValue);
        LayerRef = AddField("LayerRef", FieldTypes.LAYER, ManualAPI.SelectedLayer, new M_ComboBox(ManualAPI.layers) );
        Normalize = AddField("Normalize", FieldTypes.BOOLEAN, false, new M_CheckBox("Normalize"));

        EnablePreview = true;
        LayerRef.ValueChanged = LayerRef_ValueChanged;
    }

    void LayerRef_ValueChanged(object? value)
    {
        var layer = value as LayerBase;
        if (layer != null)
            PreviewImage = layer.Image;
    }

    async Task<object?> LayerRefValue()
    {
        var layer = (LayerBase)LayerRef.FieldValue;
        LayerBase l = layer;
        if ((bool)Normalize.FieldValue == true)
        {
            await AppModel.InvokeAsync(() =>
            {
                var box = GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;
                l = layer.Clone();
                l.CopyDimensions(box);
            });
        }

        return l;
    }


    public static byte[] ConvertToByte(LayerBase layer)
    {
        byte[] refimg = null;
        if (layer != null)
        {
            AppModel.Invoke(() =>
            {
                var img = layer.ImageWr;//.ScaleTo(512, 512);
                refimg = img.ToByte();
            });
            return refimg;
        }
        else
            return null;

    }
    public static async Task<byte[]> GetLayerToByte(NodeOption layerNodeOption)
    {
        var referenceLayer = await layerNodeOption.GetValue<LayerBase>();
        return ConvertToByte(referenceLayer);
    }

}


public partial class PromptNode : LatentNode
{

    NodeOption text, prompt;
    public PromptNode()
    {
        Name = "Prompt";
        text = AddOutput("Text", FieldTypes.STRING, TextValue);
        prompt= AddField("Prompt", FieldTypes.STRING, "cute cat", typeof(M_PromptBox));
      //  prompt = AddField("Prompt", typeof(ObservableCollection<PromptTag>), "hola", typeof(PromptBox));

    }

    async Task<object?> TextValue()
    {
        //  IEnumerable<PromptTag> promptTags =  await prompt.GetValue<ObservableCollection<PromptTag>>();
        //  return SentenceTagger.ConvertToString(promptTags);
        return prompt.FieldValue;
    }


}



//------------------------------------------------------------------------------------------------------------------------ OUTPUT
public partial class OutputNode : LatentNode, IOutputNode
{

    NodeOption result, target;
    public OutputNode()
    {
        Name = "Output";
        IsOutput = true;
        EnablePreview = true;

        result = AddInput("Result", FieldTypes.LAYER, "null");
      //  target = AddInputField("Target", typeof(LayerBase), "Current", typeof(M_TextBox));
        target = AddInputField("Target", FieldTypes.LAYER, ManualAPI.SelectedLayer, new M_ComboBox(ManualAPI.layers));
    }


    LayerBase TargetLayer = null;
    public async Task Generate()
    {
        var lname = (LayerBase)target.FieldValue;//GenerationManager.Instance.CurrentGeneratingImage.TargetLayer;
        TargetLayer = lname;//LayerNode.GetLayer(lname);
        GenerationManager.Instance.CurrentGeneratingImage.TargetLayer = TargetLayer;

        if (TargetLayer == null)
        {
            Output.Log("TargetLayer is null", "OutputNode");
          
            Finished();
            return;
        }

        if (TargetLayer.BakeKeyframes.Count == 0)
        {
            if (TargetLayer._Animation.GetKeyframe("Image") is Keyframe actualKeyframe)
                await AppModel.InvokeAsync( () => TargetLayer.AddBakeKeyframe(actualKeyframe) ); //asign actual
            else
                await AppModel.InvokeAsync( () => TargetLayer.AddBakeKeyframe(Keyframe.Insert(TargetLayer, "Image")) ); //new
        }
        

        var bakeKeyframes = TargetLayer.BakeKeyframes.ToList();
        foreach (var bkey in bakeKeyframes) //------------------------------------------------------------------- MANAGE BAKE KEYFRAMES
        {
            if (GenerationManager.Instance.isInterrupt)
            {
                Output.Log("Canceled", "OutputNode");
                Finished();
                return;
            }

            AppModel.Invoke(() =>
            {
                TargetLayer.CurrentBakingKeyframe = bkey;
            });
            var result = await GetFieldValue<LayerBase>("Result"); // -------------- GET RESULT IMAGE
            if (result is null)
            {
                Output.Log("Result is null", "something went wrong in OutputNode");
                Finished();
                return;
            }
            await AppModel.InvokeAsync(() =>
            {
                var boundingBox = GetBoundingBox();

                //  var imageResult = Renderizainador.InpaintLayerInTime(TargetLayer, boundingBox, result, bkey);
                var imageResult = RendGPU.Inpaint(TargetLayer, boundingBox, result.Image);
                bkey.Value = imageResult;

                TargetLayer.RemoveBakeKeyframe(bkey);
                int currentIndex = bakeKeyframes.IndexOf(bkey);
                if (GenerationManager.IsTrackingBakeKeyframe() && currentIndex + 1 < bakeKeyframes.Count)
                {
                    // obtener el siguiente para saltar hacia allí               
                    var nextKeyframe = bakeKeyframes[currentIndex + 1];
                    ManualAPI.CurrentFrame = nextKeyframe.Frame;

                }

                PreviewImage = imageResult;
            });
        }

        

        Finished();

        //    await AppModel.InvokeAsync(() => EndGeneration(r));
    }


    void Finished()
    {
        AppModel.Invoke(() => // END GENERATION
        {
            if(TargetLayer != null)
                TargetLayer.CurrentBakingKeyframe = null;
        });
       
    }


    public static BoundingBox GetBoundingBoxs()
    {
        return (BoundingBox)ManualAPI.layers.FirstOrDefault(l => l.GetType() == typeof(BoundingBox));
    }
    public BoundingBox GetBoundingBox()
    {
        return (BoundingBox)ManualAPI.layers.FirstOrDefault(l => l.GetType() == typeof(BoundingBox));
    }

}




public partial class GroupNode : ObservableObject, IPositionable, INamable
{
    [ObservableProperty] [property: JsonProperty("title")] string name = "Group";
    [ObservableProperty] Color color = Colors.AliceBlue;

    [ObservableProperty] float positionGlobalX = 0;
    [ObservableProperty] float positionGlobalY = 0;

    [ObservableProperty] int sizeX = 0;
    [ObservableProperty] int sizeY = 0;

    [ObservableProperty] [property: JsonProperty("font_size")] int fontSize = 14;
    public GroupNode()
    {
            
    }


    // --------------------- comfy serialize    
    [JsonProperty("color")] public string _color 
    {
        get => Color.ToHex();
        set => Color = value.ToColor();
    }
    public int[] bounding
    {
        get
        {
            return [Convert.ToInt32(PositionGlobalX), Convert.ToInt32(PositionGlobalY), SizeX, SizeY];
        }
        set
        {
            PositionGlobalX = value[0];
            PositionGlobalY = value[1];
            SizeX = value[2];
            SizeY = value[3];
        }
    }

}
