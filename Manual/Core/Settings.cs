using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.API;
using Manual.Core.Graphics;
using Manual.Objects;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Manual.Core;

//------------------------------------------------------------ SETTINGS PROPERTIES
public partial class Settings : ObservableObject 
{
    [ObservableProperty] bool seeFPS = false;
    [ObservableProperty] bool seeMS = false;
    partial void OnSeeFPSChanged(bool value)
    {
        if (value == true)
            FpsMonitor.Start();
        else
            FpsMonitor.Stop();
    }

    [ObservableProperty] [property: JsonIgnore] string fPS = "fps: 60";

    [ObservableProperty] bool debugMode = false;
    [ObservableProperty] bool enablePreview = true;

    public static Settings instance => AppModel.settings;

    //[ObservableProperty] private string currentURL = "http://127.0.0.1:7860";

    private string currentURL = "http://127.0.0.1:7860";
    public string CurrentURL
    {
        get
        {
            if (IsCloud && CloudURL != null)
                return CloudURL;
            else
               return currentURL;
        }
        set
        {
            currentURL = value.Trim('/');
            if (currentURL != value)
            {
                OnPropertyChanged(nameof(CurrentURL));
            }
        }
    }



    //SERVER

    [ObservableProperty] AIServer aIServer = new ComfyUIServer();
    [ObservableProperty] bool isCloud = false;
    public string? CloudURL = null;
    public async Task<string> SetCloudURL()
    {
        if (CloudURL != null)
            return CloudURL;
        else
        {
            var value = await ManualToolkit.Windows.WebManager.GET(Constants.WebURL + "/api/cloud-generation");
            CloudURL = value.Value<string>("url");
            return CloudURL;
        }
    }


    [ObservableProperty] string huggingFaceToken = "";

    [ObservableProperty] float realtimeDelaySeconds = 1;

    public string Version => Properties.Settings.Default.Version; //Properties.Settings.Default.Version; }  //"0.70-chun"; //Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public string SplashAuthor => Properties.Settings.Default.SplashAuthor;  //"Manual Team";
    public string SplashAuthor_Link => Properties.Settings.Default.SplashAuthor_Link;  //"https://manualai.art/";

    [ObservableProperty] int recentFilePathMode = 0;
    public ObservableCollection<AssetFile> RecentFilePaths { get; set; } = new();

    public ObservableCollection<PluginData> Plugins { get; set; } = new();


    [ObservableProperty] [JsonIgnore] private PluginData selectedPlugin = null;

    [RelayCommand]
    [property: JsonIgnore]
    void AddPlugin()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Archivos C# (*.cs)|*.cs";
        openFileDialog.Title = "Seleccionar archivo .cs";

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            PluginsManager.AddPlugin(filePath);
        }
    }


    //Shortcuts 
    public ObservableCollection<HotKey> HotKeys { get; set; } = new();
    public bool FirstOpen = false;
    [ObservableProperty] bool enableAnimations = true;




    // UPDATE
    [ObservableProperty] bool newUpdateAvailable = false;
    [ObservableProperty] bool discardUpdate = false;
    [ObservableProperty] string newReleaseAvailable = "";




    //GLOW
    public static event EventBool EnableGlowChanged;
    [ObservableProperty] bool enableGlow = false;
    partial void OnEnableGlowChanged(bool value)
    {
        EditorsSpace.instance?.UpdateGlow();
        EnableGlowChanged?.Invoke(value);
    }



    //ANIMATION
    [ObservableProperty] float bezierPresicion = 0.003f;
    [ObservableProperty] bool frameDrop = true;
    [ObservableProperty] bool enablePreviewCacheFrames = false;


    //BRUSHER
    [ObservableProperty] PencilCursor pencilForm = PencilCursor.Dot;
    [ObservableProperty] PencilCursor eraserForm = PencilCursor.BrushForm;

    //UNDO REDO
    [ObservableProperty] int undoSteps = 50;


    //GRAPHICS
    [ObservableProperty] bool enableGPUAceleration = true;
    partial void OnEnableGPUAcelerationChanged(bool value)
    {
        //  if (value)
         //     RendGL.StartLoad();

        //if (value)
        //    RenderOptions.ProcessRenderMode = RenderMode.Default;
        //else
        //    RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void ClearCache()
    {
        AppModel.project.ClearAllShotCaches();
    }


    [ObservableProperty] [property:JsonIgnore] bool isRender3D = false; //test

}









// ------------------------------------------------------------- STATIC SETTINGS ACTIONS ---------------------------------------------------------------------- \\
public partial class Settings
{


   public static void AddRecentFile(AssetFile assetFile)
    {
        int limitFiles = 12;

        if (assetFile == null || string.IsNullOrEmpty(assetFile.Path))
            return;

        // recent file paths
        var existingFile = instance.RecentFilePaths.FirstOrDefault(file => file.Path == assetFile.Path);

        if (existingFile == null)
        {
            instance.RecentFilePaths.Insert(0, assetFile);
            if (instance.RecentFilePaths.Count > limitFiles)
            {
                instance.RecentFilePaths.RemoveAt(instance.RecentFilePaths.Count - 1);
            }
        }
        else
        {
            instance.RecentFilePaths.Remove(existingFile);
            instance.RecentFilePaths.Insert(0, assetFile);
        }

        SaveSettings();
    }

    /// <summary>
    /// remove if file doesn't exist anymore
    /// </summary>
    /// <param name="filename"></param>
    public static void RemoveRecentFile(AssetFile assetFile)
    {
        if (assetFile != null)
        {
            var fileToRemove = instance.RecentFilePaths.FirstOrDefault(file => file.Path == assetFile.Path);
            if (fileToRemove != null && !File.Exists(assetFile.Path))
            {
                instance.RecentFilePaths.Remove(fileToRemove);
            }
        }
    }
    public static void RemoveRecentFile(string filename)
    {
        if (!string.IsNullOrEmpty(filename))
        {
            var fileToRemove = instance.RecentFilePaths.FirstOrDefault(file => file.Path == filename);
            if (fileToRemove != null && !File.Exists(filename))
            {
                instance.RecentFilePaths.Remove(fileToRemove);
            }
        }
    }

    public static void SaveSettings()
    {
        var hk = Shortcuts.HotKeys;

        string jsonUrl = $"{App.LocalPath}Resources/preferences.json";

        string settingsJson = JsonConvert.SerializeObject(AppModel.settings, Formatting.Indented);
        File.WriteAllText(jsonUrl, settingsJson);

        Shortcuts.HotKeys = hk;

        //  System.Media.SystemSounds.Asterisk.Play();
    }

    // ----------- SETTINGS LOAD ------------ \\
    public static void LoadSettings() //-------------------------------------------- START
    {
 
        //Shortcuts.HotKeys.Clear();
        string jsonUrl = $"{App.LocalPath}/Resources/preferences.json";

        if (!File.Exists(jsonUrl))
        {
            SaveSettings();
            return;
        }

        string jsonString = File.ReadAllText(jsonUrl);


        AppModel.userManager.LogIn();
        using (var reader = new JsonTextReader(new StringReader(jsonString)))
        {
            JsonSerializer serializer = new JsonSerializer();
            AppModel.settings = serializer.Deserialize<Settings>(reader);

        }

        if (instance.FirstOpen)
        {
         //   App.InitializeShell();

            instance.RecentFilePaths.Clear();
            instance.FirstOpen = false;
            SaveSettings();
        }



    }
    public static void PostInitialize()
    {

        SentenceTagger.LoadTagTypes();

        AppModel.project.toolManager = new();

        //project loaded, then load editors
        Shortcuts.SetGeneralHotKeys();

        var project = AppModel.project;
        PluginsManager.LoadPlugins();
        project.editorsSpace.LoadWorkspaces();
        project.toolManager.WorkspaceChanged(project.toolManager.Spaces.FirstOrDefault(x => x.name == project.editorsSpace.Current_Workspace.tool));


    }


    //------------------------------------- THEMES
    [JsonIgnore] public ObservableCollection<string> Themes { get; set; } = new() { "Humble", "Psycho", "DejaVu", "Dark"};

    [ObservableProperty] string currentTheme = "Humble";

    partial void OnCurrentThemeChanged(string value)
    {

        ChangeTheme(value);
    }
    public static void ChangeTheme(string newTheme)
    {
        string themeDir = "pack://application:,,,/ManualToolkit;component/Themes/ColorThemes";
        string themePath = $"{themeDir}/{newTheme}Theme.xaml";

        try
        {
            var newThemeDict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Absolute) };
            Application.Current.Resources.MergedDictionaries[0] = newThemeDict;
        }
        catch (Exception ex)
        {
            Output.Log(ex.Message, $"theme does not exist: {newTheme}");
        }

        if(Application.Current.MainWindow is MainWindow mw)
          mw.UpdateLayout();

    }

    public static void ChangeTheme(Theme newTheme)
    {
        if (newTheme == Theme.ManualPro && AppModel.userManager.Plan == UserPlan.free)
            return;

           ChangeTheme(newTheme.ToString());
    }



    //------------------ BgGrid
    [JsonIgnore] public ObservableCollection<string> BgGrids { get; set; } = new() { "Line", "Plus"};

    [ObservableProperty] string currentBgGrid = "Line";
    partial void OnCurrentBgGridChanged(string value) => CurrentBgGridChanged?.Invoke(value);
    
    public static event EventString? CurrentBgGridChanged;
    public static void InvokeOnCurrentBgGrid(string value) => CurrentBgGridChanged?.Invoke(value);
    public static void InvokeOnCurrentBgGrid() => CurrentBgGridChanged?.Invoke(AppModel.settings.CurrentBgGrid);

    [ObservableProperty] int selectedTabIndex = 0;



    //CANVAS VIEW
    [ObservableProperty] bool isTopSectionBottom = true;


    [ObservableProperty] bool enablePreviewAnimation = true;
    [ObservableProperty] bool enableColorfulPreview = true;



    //NOTIFICATION
    [ObservableProperty] bool isNotifyGenSound = true;
    [ObservableProperty] bool isNotifyGen = true;



    //-------------- CANVAS VIEW 3D PREFERENCES
    [ObservableProperty] Quality quality = Quality.Full;
    partial void OnQualityChanged(Quality value) => Rend3D.UpdateQuality(value);
    

}

public enum Theme
{
    Humble,
    Psycho,
    DejaVu,
    ManualPro,
    Dark,

}
public enum BgGrid
{
    Line,
    Plus,
    Dot
}







//-------------------------------------------------------------------------------------------------------- MANUAL SERIALIZE
public class ManualSerialization
{
    public string TypeId { get; set; }
    public string Name { get; set; }


    public ManualSerialization()
    {
            
    }
    /// <summary>
    /// serialize type and name
    /// </summary>
    /// <param name="obj"></param>
    public ManualSerialization(INamable obj)
    {
        TypeId = obj.GetType().AssemblyQualifiedName;
        Name = obj.Name;
    }

    public object? Load()
    {
        Type type = TypeId == null ? null : Type.GetType(TypeId);

        if (typeof(LayerBase).IsAssignableFrom(type))
            return ManualAPI.FindLayer(Name);
        else
            return null;
    }
}