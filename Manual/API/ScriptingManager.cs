using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.Core;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LangChain.Providers.HuggingFace;
using System.Net.Http;
using LangChain.Providers;
using Newtonsoft.Json;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media.Imaging;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using ManualToolkit.Specific;
using Python.Runtime;
using Manual.Core.Nodes;


using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using low_CSharpScript = Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript;
using Manual.Core.Graphics;

namespace Manual.API;
public static partial class PluginsManager
{
    public static void RunCode(string code)
    {
        CSharpScript.RunCode(code);
    }

    public static void LoadPluginFromFolder(string folderPath)
    {
       
    }

    static void ComposeParts(Assembly assembly, bool notify = false)
    {
        // Usar 'using' para asegurar que los recursos se liberen adecuadamente
        using (var assemblyCatalog = new AssemblyCatalog(assembly))
        using (var container = new CompositionContainer(assemblyCatalog))
        {
            container.ComposeParts(AppModel.project.regionSpace);

            try
            {
                var plugins = container.GetExportedValues<IPlugin>();
                foreach (var plugin in plugins)
                {
                    plugin.Initialize();
                    if (notify)
                        Output.Log($"Running {assembly.FullName}", "PluginsManager");
                }
            }
            catch (Exception ex)
            {
                // Registrar información detallada de la excepción
                Output.Log($"Error al cargar plugins: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
    public static void Run(CSharpScript script)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        var assembly = script.CompileCode(script.Code);

        if (assembly != null)
          ComposeParts(assembly);
 
        Mouse.OverrideCursor = null;
        Output.DoneSound();
    }


}

public partial class ScriptBase : ObservableObject, INamable
{
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [ObservableProperty] string name = "script";
    public string? PathDirectory = null;

    /// <summary>
    /// with period
    /// </summary>
    public string Extension { get; set; }

    [JsonIgnore] public string DisplayName => Name + Extension;
    public ScriptBase()
    {

    }

    public string GetFullPath() => Path.Combine(PathDirectory, DisplayName);


    [ObservableProperty] [property: JsonIgnore] TextDocument codeDocument = new("");

    public string Code
    {
        get => CodeDocument.Text;
        set
        {
            var t = new TextDocument();
            t.Text = value;
            CodeDocument = t;
        }
    }


    // UI
    [ObservableProperty] double scrollPosition;

}
public abstract class Script : ScriptBase
{
    public abstract void Compile();
}

public partial class ScriptingManager : ObservableObject
{
    public static ScriptingManager Instance => AppModel.project.scriptingManager;
    public ObservableCollection<Script> Scripts { get; set; } = new();

    [ObservableProperty] [property: JsonIgnore] Script selectedScript;

    [JsonIgnore] public ObservableCollection<MenuItemNode> ScriptTemplates { get; set; } = new();
    public ScriptingManager()
    {
        if(AppModel.InstantiateThings)
            AddScript();

        LoadScriptTemplates();
    }


    //------------------------------------------------------------------------------------------- COMPILE
    [RelayCommand]
    [property: JsonIgnore]
    public void Compile()
    {
        SelectedScript.Compile();
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void CompileLight()
    {
        if(SelectedScript is CSharpScript s)
        s.RunCode();
    }


    public Script GetTemplate(string name)
    {
        var item = ScriptTemplates.FirstOrDefault(s => s.Name == name);
        Script script = null;
        if (Path.GetExtension(item.Path) == ".cs")
            script = new CSharpScript();
        else if (Path.GetExtension(item.Path) == ".y")
            script = new YScript();
        else
            script = new ScriptAnon();

        script.Name = Path.GetFileNameWithoutExtension(item.Path);
        script.Extension = Path.GetExtension(item.Path);
        script.PathDirectory = Path.GetDirectoryName(item.Path);
        script.Code = File.ReadAllText(item.Path);

        return script;
    }
    public void AddScriptTemplate(string name)
    {
        AddScript(GetTemplate(name));
    }
    void LoadScriptTemplates()
    {
        // Limpiar la colección existente
        ScriptTemplates.Clear();

        string baseDirectory = $"{App.LocalPath}Resources/Templates/Scripts";

        // Verificar si el directorio existe
        if (Directory.Exists(baseDirectory))
        {
            // Obtener todos los archivos en el directorio y subdirectorios
            var fileEntries = Directory.GetFiles(baseDirectory, "*.*", SearchOption.AllDirectories);

            foreach (var file in fileEntries)
            {
                var menuItem = new MenuItemNode
                {
                    Name = Path.GetFileName(file),
                    Path = file
                };

                ScriptTemplates.Add(menuItem);
            }
        }
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void AddScript()
    {
        var script = new CSharpScript();
        AddScript(script);
    }
    public void AddScript(Script script)
    {
        Scripts.Add(script);
        SelectedScript = script;
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void SaveScript()
    {
        SaveScript(SelectedScript);
    }

    internal static void SaveScript(Script script)
    {
        string code = script.Code;
        if (script.PathDirectory != null)
        {
            string fullPath = script.GetFullPath();
            File.WriteAllText(fullPath, code);
        }
        else
        {
            AppModel.File_ExportScript();
        }
        Output.Log($"{script.Name}{script.Extension} saved");
    }
    internal static void SaveScript(string fullPath)
    {
        string code = Instance.SelectedScript.Code;
        File.WriteAllText(fullPath, code);
    }

    public static Dictionary<string, Func<Script>> RegisteredScript = new()
    {
        {".y" , () => new YScript()},
        {".cs", () => new CSharpScript()},
        {".nlp", () => new NLPScript()},
    };
    public static Script ScriptByExtension(string extension)
    {
        if (RegisteredScript.TryGetValue(extension, out Func<Script> scriptFactory))
        {
            return scriptFactory();
        }
        else
        {
            // Retorna una nueva instancia de ScriptAnon si la extensión no está en el diccionario
            return new ScriptAnon();
        }
    }
    internal static void LoadScript(string fullPath)
    {

        var ext = Path.GetExtension(fullPath);

        Script script = ScriptByExtension(ext);

        script.Name = Path.GetFileNameWithoutExtension(fullPath);
        script.Extension = ext;
        script.PathDirectory = Path.GetDirectoryName(fullPath);
        script.Code = File.ReadAllText(fullPath);
        Instance.AddScript(script);
    }

}
public class ScriptAnon : Script
{
    [JsonIgnore] public Action CompileAction { get; set; }
    public ScriptAnon(Action compileAction)
    {
        CompileAction = compileAction;
    }
    public ScriptAnon()
    {
        CompileAction = () => Output.Log($"cannot compile this type of script: {Extension}");
    }
    public override void Compile()
    {
        CompileAction?.Invoke();
    }
}


public class CSharpScript : Script
{
    public CSharpScript()
    {
        Extension = ".cs";
    }
    //public CSharpScript(string path)
    //{
    //    Extension = ".cs";
    //    Name = Path.GetFileNameWithoutExtension(Name);
    //    Code = File.ReadAllText(path);
    //}

    /// <summary>
    /// save the dll file
    /// </summary>
    /// <param name="code"></param>
    /// <param name="filePath"></param>
    public  void CompileDll(string code, string filePath)
    {
        var compilation = GetCompilation(code);


        using (var ms = new MemoryStream())
        {
            // Guardar el archivo dll
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                foreach (var error in result.Diagnostics)
                {
                    Output.Log($"{error.GetMessage()} \n{error.Descriptor}\n{filePath}");
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
               // Assembly assembly = Assembly.Load(ms.ToArray());
                File.WriteAllBytes(filePath, ms.ToArray());


            }
        }
    }

    public void SaveCode(string code, string filePath)
    {
        string textToSave = code;
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.Write(textToSave);
        }
    }

   CSharpCompilation GetCompilation(string code)
    {
        // Compilar el código
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
        string assemblyPath = System.IO.Path.GetRandomFileName();


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
             MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime")).Location),
              MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.ComponentModel.Primitives")).Location),
               MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Xaml")).Location),

        // MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")),
        // MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
        //  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
        //   MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Windows.dll")),

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
               //  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.ObjectModel.dll")),
                //  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")),
                  //  MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
};

        var referencedAssemblies = AppDomain.CurrentDomain
     .GetAssemblies()
     .Where(assembly => assembly != null && !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location)) // Verificar que el ensamblado no sea nulo, no sea dinámico y tenga una ubicación válida
     .Distinct()
     .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
     .ToList();

        // Agregar referencias específicas que no se pueden detectar automáticamente
        referencedAssemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime")).Location));
        referencedAssemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.ComponentModel.Primitives")).Location));
        // ... otros ensamblados específicos ...


        CSharpCompilation compilation = CSharpCompilation.Create(assemblyPath, syntaxTrees: new[] { syntaxTree }, references: referencedAssemblies, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return compilation;
    }
    public Assembly CompileCode(string code)
    {
        var compilation = GetCompilation(code);

        using (var ms = new MemoryStream())
        { 
            var result = compilation.Emit(ms);
            if (result.Success) // compiled
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                return assembly;
            }
            else
            {            
                foreach (var error in result.Diagnostics)
                {
                    Output.Log($"{error}", error.Location);
                }
            }

            return null;
        }
    }
    public override void Compile()
    {
        PluginsManager.Run(this);
        return;


        string fullpath = GetFullPath();
        string code = Code;

        var assembly = CompileCode(code);
       
        

        string textToSave = Code;

        using (StreamWriter writer = new StreamWriter(fullpath))
        {
            writer.Write(textToSave);
        }
        Output.Show("Compilation succed!");
        System.Media.SystemSounds.Asterisk.Play();
    }

    public void RunCode() => RunCode(Code);





    public static async void RunCode(string code) //---------------------------------------------- LIGHT RUN
    {
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var options = ScriptingOptions.GetDefaultOptions();

            // Compilar y ejecutar el código
            var result = await low_CSharpScript.EvaluateAsync(code, options);
            if (result == null) result = "null";
            Output.Log(result.ToString(), "Interpreter (Succeed)", System.Windows.Media.Colors.Yellow);
        }
        catch (CompilationErrorException e)
        {
            // Manejar errores de compilación
            Output.LogError("Compilation Error: " + string.Join("\n", e.Diagnostics));
        }
        catch (Exception e)
        {
            // Manejar otros errores de ejecución
            Output.LogError("Execution Error: " + e.Message);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }



    internal static Script ComfyNodeTemplate()
    {
        var script = new CSharpScript();
        string path = $"{App.LocalPath}Resources/Scripts/example/node_example.cs";
        script.Name = "MyNode";
        script.PathDirectory = null;
        if (File.Exists(path))
            script.Code = File.ReadAllText(path);

        return script;
    }


}


//---------------------------------------------------------------------------------------------------------------------------------------------- Y

public class CodeLlamaClient // HuggingFace
{
    public string ApiUrl = "https://api-inference.huggingface.co/models/codellama/CodeLlama-7b-hf";
    public string ApiKey { get; set; }
    public CodeLlamaClient(string apikey)
    {
        ApiKey = apikey;
    }
    public async Task<string> QueryAsync(string prompt)
    {
        return TestQuery();

        using (var client = new HttpClient())
        {
            var payload = new
            {
                inputs = prompt
            };

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return jsonResponse;
            }
            else
            {
                throw new Exception($"Error: {response.StatusCode}");
            }
        }
    }


    string TestQuery()
    {
       // return "[{\"generated_text\":\"text:\\nconsole log \\\"hello world\\\"\\n\\nC# code interpret:\\nConsole.WriteLine(\\\"hello world\\\");\\n\\n### 2.2.2. 变量\\n\\n#### 2.2.2.1. 变量声明\\n\\n变量声明语句的格式如下：\\n\\n```\\nvar <变量名> = <表达式>;\\n```\\n\\n变量声明语句的含义是：将表达式的\"}]";
        return "Console.WriteLine(\"hello world\");\nint number = 1;";

    }

    public static string ExtractCSharpCode(string jsonResponse)
    {

        // Deserializar la respuesta JSON
        var responseArray = JArray.Parse(jsonResponse);
        var responseText = responseArray[0]["generated_text"].ToString();

        // Buscar el inicio del código interpretado
        string interpretTag = "\nC# code interpret:\n";
        int startIndex = responseText.IndexOf(interpretTag);

        if (startIndex != -1)
        {
            // Extraer el código interpretado
            string codeInterpret = responseText.Substring(startIndex + interpretTag.Length);

            // Opcionalmente, limitar la extracción hasta el próximo marcador de sección, si existe
            int endIndex = codeInterpret.IndexOf("\n\n###");
            if (endIndex != -1)
            {
                codeInterpret = codeInterpret.Substring(0, endIndex);
            }

            return codeInterpret.Trim();
        }

        return jsonResponse; //"Código interpretado no encontrado.";
    }
}



//-------------------------------------------------------------------------------------------------------------------------------- NLP SCRIPT
public class NLPScript : Script
{
    public NLPScript()
    {
        Extension = ".nlp";
    }
    public override async void Compile()
    {
        await TranscriptNodes(Code);
    }
    public async Task TranscriptNodes(string nlpText) // -------------------------------- FOR MAKING NODES
    {
        YObject nlp = NLP.Deserialize(nlpText);

        var results = nlp["result"] as YArray;
        //the folder inside the custom_node, where __init__.py exists
        var folder_name_obj = nlp["folder_name"] as YObject;
        var folder_name = folder_name_obj.Text.Trim();

        string custom_nodes_path = Path.Combine(Settings.instance.AIServer.DirPath, "ComfyUI", "custom_nodes");

        if (folder_name == null)
            folder_name = "MyManualCustomNode";

        if (Directory.Exists(Settings.instance.AIServer.DirPath))
        {
            string path = Path.Combine(custom_nodes_path, folder_name);
            Directory.CreateDirectory(path);
            CreateInit(path, Name);
        }


        //project plugin path in comfy
        string comfy_proj_path = Path.Combine(custom_nodes_path, folder_name);
        string comfy_proj_file = Path.Combine(comfy_proj_path, Name);

        //project in manual
        string manual_proj_folder = Path.Combine($"{App.LocalPath}Resources/Scripts", folder_name);
        if (!Directory.Exists(manual_proj_folder))
        {
            Directory.CreateDirectory(manual_proj_folder);
        }
        string manual_proj_file = Path.Combine(manual_proj_folder, Name);

        foreach (var res in results)
        {
            if (res.Name == "interpreted_code" && res is YObject interpreted_code)
            {
                string type = interpreted_code.Properties["type"] as string;
                string code = interpreted_code.Value as string;


                if (type == "cs")
                {
                    CSharpScript cSharp = new();
                    cSharp.CompileDll(code, manual_proj_file + ".dll");
                    cSharp.SaveCode(code, manual_proj_file + ".cs");
                    ScriptingManager.LoadScript(manual_proj_file + ".cs");
                }
                else if (type == "py")
                {
                    File.WriteAllText(comfy_proj_file + ".py", code);
                    ScriptingManager.LoadScript(comfy_proj_file + ".py");
                }
            }
        }
        var nodes_name = ((YObject)nlp["nodes_name"]).Text.Trim();
        AddNodesToInit(comfy_proj_path, nodes_name);


        PluginsManager.AddPluginAndLoad(manual_proj_file + ".dll");
        Output.Log("Y compiled succesfully");
    }
    void CreateInit(string pathDir, string projectName)
    {
        string code = $"    from .{projectName}" + " import *\r\n  NODE_CLASS_MAPPINGS = {\r\n\r\n    }\r\n\r\n\r\n    NODE_DISPLAY_NAME_MAPPINGS = {\r\n\r\n    }\r\n";
        File.WriteAllText(Path.Combine(pathDir, "__init__.py"), code);
    }

    /*

    from .manual_nodes import *
    #from nodes import *  # Importa todas las clases de nodes.py

    NODE_CLASS_MAPPINGS = {
        "M_Layer": Layer,
    }


    NODE_DISPLAY_NAME_MAPPINGS = {
        "M_Layer": "Layer",
    }

    */

    /// <summary>
    /// node names separated by comma whitout space "node1,node2,node3
    /// </summary>
    /// <param name="initDirectory"></param>
    /// <param name="nodes_name">names separated by comma whitout space "node1,node2,node3"</param>
    public void AddNodesToInit(string initDirectory, string nodes_name)
    {
        var initPath = Path.Combine(initDirectory, "__init__.py");

        // Dividir los nombres de los nodos y sus nombres para mostrar
        var nodeEntries = nodes_name.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        // Leer el contenido del archivo __init__.py
        var lines = File.ReadAllLines(initPath).ToList();

        // Encontrar la posición para insertar en NODE_CLASS_MAPPINGS y NODE_DISPLAY_NAME_MAPPINGS
        int classMappingsInsertPos = FindInsertPosition(lines, "NODE_CLASS_MAPPINGS");
        int displayNameMappingsInsertPos = FindInsertPosition(lines, "NODE_DISPLAY_NAME_MAPPINGS");

        foreach (var entry in nodeEntries)
        {
            var parts = entry.Split(':');
            if (parts.Length != 2) continue; // Saltar si el formato no es válido

            string className = parts[0].Trim();
            string displayName = parts[1].Trim();

            string mappedName = "M_" + className;
            string classMappingEntry = $"    \"{mappedName}\": {className},";
            string displayNameMappingEntry = $"    \"{mappedName}\": \"{displayName}\",";

            // Verificar si el nodo ya existe antes de insertar
            if (!DoesNodeExist(lines, mappedName, "NODE_CLASS_MAPPINGS") && classMappingsInsertPos != -1)
            {
                lines.Insert(classMappingsInsertPos, classMappingEntry);
                classMappingsInsertPos++;
            }

            if (!DoesNodeExist(lines, mappedName, "NODE_DISPLAY_NAME_MAPPINGS") && displayNameMappingsInsertPos != -1)
            {
                lines.Insert(displayNameMappingsInsertPos, displayNameMappingEntry);
                displayNameMappingsInsertPos++;
            }
        }

        // Escribir el contenido modificado de nuevo en el archivo
        File.WriteAllLines(initPath, lines);
    }


    private bool DoesNodeExist(List<string> lines, string nodeName, string mappingName)
    {
        var mappingStart = lines.FindIndex(line => line.Trim().StartsWith(mappingName));
        var mappingEnd = lines.FindIndex(mappingStart, line => line.Trim() == "}");

        return lines.GetRange(mappingStart, mappingEnd - mappingStart).Any(line => line.Contains($"\"{nodeName}\""));
    }

    private int FindInsertPosition(List<string> lines, string mappingName)
    {
        // Encuentra la línea que contiene el cierre de la variable de mapeo
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().StartsWith(mappingName))
            {
                for (int j = i; j < lines.Count; j++)
                {
                    if (lines[j].Trim() == "}")
                    {
                        return j; // Devuelve la posición antes del cierre del diccionario
                    }
                }
            }
        }
        return -1; // No encontrado
    }

}




public static class ScriptingOptions
{
    private static readonly ScriptOptions DefaultOptions;

    static ScriptingOptions()
    {
        DefaultOptions = ScriptOptions.Default
            .AddReferences(
                typeof(RendGL).Assembly,
                typeof(RendGPU).Assembly, // Añade una referencia al ensamblado que contiene RendGL
                typeof(AppModel).Assembly,// Añade otras referencias necesarias
                typeof(ManualAPI).Assembly,
                 typeof(api).Assembly
            )
            .AddImports(
                "Manual",
                "Manual.Core",
                "Manual.Core.Graphics", // Usa el espacio de nombres donde RendGL está definido
                "Manual.API",

                "System", // Añadir otros namespaces comúnmente usados
                "System.Collections.Generic"
            );
    }

    public static ScriptOptions GetDefaultOptions() => DefaultOptions;
}
