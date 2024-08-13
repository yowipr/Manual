using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manual.Core;
using System.Reflection;
using System.IO;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Dynamic;
using Manual.Editors;
using System.Collections.ObjectModel;
using System.Windows.Resources;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Windows.Input;
using Manual.Objects;
using Microsoft.Win32;
using ManualToolkit.Windows;
using ManualToolkit.Specific;
using Manual.MUI;
using System.Windows.Media;
using System.Security.RightsManagement;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using Manual.Core.Nodes;
using ManualToolkit.Generic;


namespace Manual.API;


public interface IPlugin
{
   public void Initialize();
}

public interface IPlugable
{
    /// <summary>
    /// in the constructor subscribe to <see cref="PluginsManager.OnUpdatePlugins"/> and put inside all variables that needs
    /// to be updated when plugins loaded or updated
    /// </summary>
    public void RefreshEditors();
}


public static partial class PluginsManager
{
  
    public static event Action OnUpdatePlugins;

    public static PluginData AddPluginAndLoad(string filePath)
    {
        var plugin = AddPlugin(filePath);
        LoadPlugin(plugin, new AggregateCatalog());
        return plugin;
    }
    /// <summary>
    /// dll or cs file path
    /// </summary>
    /// <param name="filePath"></param>
    public static PluginData AddPlugin(string filePath)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);
        string codepath;
        string dllpath;

        if (Path.GetExtension(filePath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // Si es un archivo .cs, asume que el archivo .dll tiene el mismo nombre pero con extensión .dll
            codepath = filePath;
            dllpath = Path.ChangeExtension(filePath, ".dll");
        }
        else if (Path.GetExtension(filePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            // Si es un archivo .dll, asume que el archivo .cs tiene el mismo nombre pero con extensión .cs
            codepath = Path.ChangeExtension(filePath, ".cs");
            dllpath = filePath;
        }
        else
        {
            // Maneja otros casos o muestra un mensaje de error si el archivo no es .cs o .dll
            Output.Log("El archivo no es válido (.cs o .dll)", "AddPlugin(string filePath)");
            return null;
        }

        PluginData plugin = new PluginData(name, codepath, dllpath);
        AppModel.settings.Plugins.Add(plugin);
        return plugin;
    }


    public static void LoadPlugin(PluginData pluginData, AggregateCatalog aggregateCatalog)
    {

        if (pluginData.Enabled == false)
            return;

        Assembly assembly;

        if (File.Exists(pluginData.DllPath))
        {
            byte[] bytes = File.ReadAllBytes(pluginData.DllPath);
            assembly = Assembly.Load(bytes);
        }
        else
        {
            //TODO: compilar automaticamente podría causar errores
            //  CompilePlugin(pluginData);
            Output.Log($"missing dll {pluginData.DllPath}");
            return;
        }

        var assemblyCatalog = new AssemblyCatalog(assembly);
        aggregateCatalog.Catalogs.Add(assemblyCatalog);
    }


    static void InstantiateInternalPlugins()//----------------------------------- INTERNAL
    {
        new Plugins.Text2ImageTool().Initialize();
        new Plugins.BrushTool().Initialize();
        new Plugins.MovementTool().Initialize();
        new Plugins.InspectorTool().Initialize();
        new Plugins.ShotTool().Initialize();
    }

    public static void LoadPlugins()
    {

        //reset
        AppModel.project.regionSpace = new();
        AppModel.project.toolManager.Spaces.Clear();

        InstantiateInternalPlugins();

        string scriptsPath = $"{App.LocalPath}Resources/Scripts";
        if (Output.DEBUG_BUILD())
        {
            scriptsPath = Path.Combine(Constants.ProyectURL(), "Manual", "Resources", "Scripts");
        }

        var aggregateCatalog = new AggregateCatalog();

        if (AppModel.settings.Plugins.Count == 0) // at start
        {
            string[] dllFiles = Directory.GetFiles(scriptsPath, "*.dll", SearchOption.AllDirectories);

            foreach (string filePath in dllFiles)
            {
                AddPlugin(filePath);
            }
        }

        foreach (var pluginData in AppModel.settings.Plugins)
        {
            //   if (pluginData.Name == "ControlNetEx") { Output.Log("._.XD"); continue; } // in case of errors
            LoadPlugin(pluginData, aggregateCatalog);
        }

        var container = new CompositionContainer(aggregateCatalog);
        container.ComposeParts(AppModel.project.regionSpace);

        IEnumerable<IPlugin> plugins;
        try
        {
            plugins = container.GetExportedValues<IPlugin>();
        }
        catch (Exception ex)
        {
            Output.Log(ex.Message);
            return;
        }

        foreach (var plugin in plugins)
        {
            try
            {
                plugin.Initialize();
            }
            catch (Exception ex)
            {
                Output.Log(ex.Message, "PluginManager.LoadPlugins");
            }
        }

    

        OnUpdatePlugins?.Invoke(); // update to all editors viewmodels "ED_" series

     
      //  System.Media.SystemSounds.Asterisk.Play();
      // Output.Log(AppModel.project.regionSpace.ed_canvas.top.ToString());
    }
    public static void CompileAllPlugins()
    {
        // AppModel.DisposePlugins();
        foreach (var item in AppModel.settings.Plugins)
        {
            PluginsManager.CompilePlugin(item);
        }
        //  PluginsManager.LoadPlugins();
    }
    public static void CompilePlugin(PluginData pluginData)
    {
        string code = File.ReadAllText(pluginData.CodePath);


        // Compilar el código
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        string assemblyName = System.IO.Path.GetRandomFileName();

        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        MetadataReference[] references = new MetadataReference[]
{
    MetadataReference.CreateFromFile(@$"{App.LocalPath}Microsoft.WindowsAPICodePack.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}Microsoft.WindowsAPICodePack.Shell.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}WintabDN.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}System.ComponentModel.Composition.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}CommunityToolkit.Mvvm.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}Microsoft.CodeAnalysis.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}Microsoft.CodeAnalysis.CSharp.dll"),
    MetadataReference.CreateFromFile(@$"{App.LocalPath}Newtonsoft.Json.dll"),

            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Windows.dll")),

            //  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "WindowsBase.dll")),

            MetadataReference.CreateFromFile(typeof(System.Windows.DependencyObject).Assembly.Location),
             MetadataReference.CreateFromFile(typeof(System.Windows.DependencyProperty).Assembly.Location),
             MetadataReference.CreateFromFile(typeof(System.Windows.UIElement).Assembly.Location),
              MetadataReference.CreateFromFile(typeof(FrameworkElement).Assembly.Location),  
              
        
               // MetadataReference.CreateFromFile(typeof(DynamicObject).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ExpandoObject).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location),
                MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location),

            MetadataReference.CreateFromFile(@$"{App.LocalPath}Manual.dll"),
              MetadataReference.CreateFromFile(typeof(WriteableBitmapExtensions).Assembly.Location),
                 MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.ObjectModel.dll")),
                  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
};


        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, syntaxTrees: new[] { syntaxTree }, references: references, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            // Guardar el archivo dll
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                foreach (var error in result.Diagnostics)
                {
                   
                    // console.Text += $"{error} \n";
                    Output.Log($"{error.GetMessage()} \n{error.Descriptor}\n{pluginData.DllPath}", pluginData.Name);
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());


                File.WriteAllBytes(pluginData.DllPath, ms.ToArray());
                LoadPlugins();
               // AppModel.UpdatePlugins();
              

            }
        }
        string textToSave = code;

       // using (StreamWriter writer = new StreamWriter(pluginData.CodePath)) //TODO: save script, but not necesary for now
      // {
       //     writer.Write(textToSave);
       // }


        // console.Text += "Compilation succed! \n";

       
        Output.Log("Compilation succed! \n", pluginData.Name);
        System.Media.SystemSounds.Asterisk.Play();
    }

}



public class RegionSpace
{

    [JsonIgnore] public readonly ed_canvas_region ed_canvas = new();
    [JsonIgnore] public readonly ed_tools_region ed_tools = new();

    public class ed_canvas_region
    {
        [JsonIgnore] public StackPanel top { get; set; } = new();
        [JsonIgnore] public StackPanel bottom { get; set; } = new();
        [JsonIgnore] public object[] inside { get; set; }
    }


    public class ed_tools_region
    {
        [JsonIgnore]
        public ObservableCollection<Tool> tools 
        { 
            get 
            {
                return AppModel.project.toolManager.Spaces;
            }
        }


        public void AddTool(Tool tool)
        {
            AppModel.project.toolManager.Spaces.Add(tool);
        }

    }
}

public class Tool : ObservableObject
{
    [JsonIgnore] public PromptPreset TargetPreset 
    { get;
        set; }

    public dynamic Props { get; set; } = new DynamicProperties();

    public bool IsChecked;
    public string name { get; set; } = "Tool";
    [JsonIgnore] public byte[] icon { get; set; }

    private string _iconPath;
    public string iconPath 
    { 
        get
        {
            return _iconPath;
        }
        set
        {
            if (_iconPath != value)
            {
                _iconPath = value;
                SetIcon(value);
            }
        }
    }

    Cursor _cursor;
    [JsonIgnore]
    public Cursor cursor
    {
        get
        {
            return _cursor;
        }
        set
        {
            if (_cursor != value)
            {
                _cursor = value;

                if (AppModel.project.toolManager.CurrentToolSpace == this)
                    Shortcuts.UpdateCursor();
            }
        }
    }

    private string _cursorPath;
    public string cursorPath
    {
        get
        {
            return _cursorPath;
        }
        set
        {
            if (_cursorPath != value)
            {
                _cursorPath = value;
                SetCursor(value);
            }
        }
    }
    public void SetCursor(string path)
    {
        cursor = new Cursor(path);

    }

    public string description { get; set; } = "";


    [JsonIgnore] public StackPanel bottom { get; set; } = new();

    public static Tool ByName(string Name)
    {
        return AppModel.project.toolManager.Spaces.FirstOrDefault(tool => tool.name == Name);
    }

    public void SetIcon(string iconPath)
    {
        if (File.Exists(iconPath))
            icon = File.ReadAllBytes(iconPath);
        else
        {
            //  string imagePath = "/Manual;Editors/component/res/icons/tool_brush.png";

            //  Uri imageUri = new Uri(imagePath, UriKind.RelativeOrAbsolute);
            //  StreamResourceInfo resourceInfo = Application.GetResourceStream(imageUri);

            var resourceInfo = Application.GetResourceStream(new Uri("Editors/res/icons/editor_Default.png", UriKind.Relative)).Stream;


            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                resourceInfo.CopyTo(memoryStream);
                imageBytes = memoryStream.ToArray();
                icon = imageBytes;
            }

        }
    }


    public Tool()
    {
        SetIcon(iconPath);

    }


    public static void Register(Tool tool)
    {
        ManualAPI.space.ed_tools.AddTool(tool);
    }


    public void SelectTargetPreset()
    {
        if (TargetPreset != null)
        {
            GenerationManager.Instance.AddPreset(TargetPreset, false, false);
            
            if(GenerationManager.Instance.SelectedPreset != null && !GenerationManager.Instance.SelectedPreset.Pinned)
                GenerationManager.Instance.SelectedPreset = TargetPreset;
        }
    }
    public PromptPreset? SetTargetPreset(string presetTemplateName)
    {
        if (GenerationManager.Instance.IsLoadingNodes) return null;

        TargetPreset = GenerationManager.FindPresetOrTemplate(presetTemplateName);
        return TargetPreset;

    }




    public bool targetPresetOnSelected = true;
    public virtual void OnToolSelected()
    {
        if(targetPresetOnSelected)
            SelectTargetPreset();
    }
    public virtual void OnToolDeselected()
    {
      
    }

    public virtual void ChangeToolToThis()
    {
        ManualAPI.SelectedTool = this;
       
    }
    public virtual void ChangeToolToOld()
    {
        ManualAPI.SelectedTool = OldTool;

    }
    public virtual void OnNewToolSelected()
    {
        //
    }
    public static Tool OldTool
    {
        get { return ManualAPI.project.toolManager.OldTool; }
    }



    [JsonIgnore] public StackPanel body { get; set; } = new();


    [JsonIgnore] public List<Func<IManualElement>> elements { get; set; } = new();

    public void AddField(IManualElement manualElement)
    {
        //  AddField(() => manualElement.Clone());
        ManualAPI.add((FrameworkElement)manualElement, body);
    }
    private void AddField(Func<IManualElement> manualElement)
    {
        elements.Add(manualElement);
    }
    public void AddFields(IEnumerable<Func<IManualElement>> manualElements)
    {
        foreach ( var manualElement in manualElements)
        {
            AddField(manualElement);
        }
    }

    [JsonIgnore] public object DataContext { get; set; } = null;
    public M_StackPanel InitializeBody()
    {
        M_StackPanel stackPanel = new();
        if(DataContext is not null)
            stackPanel.DataContext = DataContext;

           foreach (var element in elements)
           {
               //UIElement e = (UIElement)element.Invoke();
               stackPanel.Add( element() );
           }

        return stackPanel;
    }


    /// <summary>
    /// datacontext is tool
    /// </summary>
    /// <param name="title"></param>
    /// <param name="elements"></param>
    /// 
    public M_Section section(string title, params IManualElement[] elements)
    {
        return section(title, "", elements);
    }
    public M_Section section(string title, string checkBinding, params IManualElement[] elements)
    {
        var s = section(body, title, checkBinding, elements);
        s.DataContext = this;
        return s;
    }
    public M_Section section(StackPanel _body, string title, params IManualElement[] elements)
    {
        return section(_body, title, "", elements);
    }
    public M_Section section(StackPanel _body, string title, string checkBinding, params IManualElement[] elements)
    {
        bool withCheck = checkBinding != "";
        M_Section sec = new(_body, title, withCheck, checkBinding);
        sec.AddRange(elements);
        return sec;
    }



    [JsonIgnore] public Func<IManualElement>? CanvasView_BottomSection;

    public event Event<Tool> OnRefresh;

    public void InvokeRefresh()
    {
        OnRefresh?.Invoke(this);
    }

    public void UpdateCanvas_BottomSection(Func<IManualElement>? bottomSection)
    {
        CanvasView_BottomSection = bottomSection;
        InvokeRefresh();
    }
}

