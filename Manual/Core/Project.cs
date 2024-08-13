using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using static System.Net.WebRequestMethods;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media;
using Manual.Editors;
using System.Collections.ObjectModel;
using Manual.Objects;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

using Manual.API;
using System.Runtime.InteropServices.JavaScript;
using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using Manual.Core.Nodes;
using Manual.Objects.UI;
using static Python.Runtime.TypeSpec;
using MetadataExtractor.Formats.Exif.Makernotes;
using Manual.Properties;
using System.Windows.Media.Imaging;
using ManualToolkit.Generic;
using Python.Runtime;
using System.Threading;

using Manual.Core.Graphics;

namespace Manual.Core;


//--------------- PROYECT -------------\\
public partial class Project : ObservableObject, ISavable  // this is the proyect settings
{
    [ObservableProperty] string? savedPath = null;
    [ObservableProperty][property: JsonIgnore] bool isSaved = false;

    public byte[] Thumbnail;
    [ObservableProperty] private string name = "project";
    [JsonIgnore] public string Version = Settings.instance.Version;


    //---------------------------------------------------------------------------- OPEN PROJECTS


    public static TaskCompletionSource<bool>? _tcsOpen;
    public static async Task OpenAsync(string filename)
    {

        if (_tcsOpen != null)
            await _tcsOpen.Task;


        _tcsOpen = null;
        Open(filename);
    }

    /// <summary>
    /// open when mainW is fully rendered
    /// </summary>
    public static void CanOpen()
    {
        if (_tcsOpen != null)
            _tcsOpen.SetResult(true);
    }



    public static void Open(string filename)
    {
        string extension = Path.GetExtension(filename).ToLower();
        var project = AppModel.project;

        switch (extension)
        {
            case ".manual":
                OpenProject(filename);
                break;
            case ".png":
                project.OpenPng(filename);
                break;
            case ".shot":
                project.ImportShot(filename);
                break;
            default:
                break;
        }
        AppModel.mainW.FadeIn();
    }

    public void OpenPng(string filename)
    {
        var shot = new Shot(false);
        AddShot(shot);

        var imPng = Layer.New(filename);

        Camera2D mainCam = new(imPng.ImageWidth, imPng.ImageHeight);
        shot.cameras.Add(mainCam);
        shot.SetMainCamera(mainCam);

        SelectedShot.MainCamera.Scale = imPng.Scale;

        var box = BoundingBox.Add();
        box.ImageWidth = imPng.ImageWidth;
        box.ImageHeight = imPng.ImageHeight;
        box.Scale = imPng.Scale;

        shot.SelectedLayer = imPng;

        SelectedShot.Name = imPng.Name;
        SelectedShot.SavedPath = filename;
        SelectedShot.IsSaved = true;

       //GenerationManager.Instance.Initialize();//.BuildRegisterNodes();
    }











    /// <summary>
    /// if has savedpath, then save, if not, dialog appears
    /// </summary>
    public static bool SaveProject()
    {
        var project = ManualAPI.project;

        if (!string.IsNullOrEmpty(project.SavedPath) && File.Exists(project.SavedPath))
        {
            SaveProject(project, project.SavedPath);
            return true;
        }
        else
        {
            return AppModel.saveProjectDialog(project);
        }
    }

    public static void SaveProject(Project project, string filename)
    {
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            string name = Path.GetFileNameWithoutExtension(filename);

            var selectedShot = project.SelectedShot;
            project.Name = name;
            project.SavedPath = filename;
            //  project.Thumbnail = ManualCodec.RenderFrame(selectedShot.Animation.CurrentFrame, selectedShot).ToByte(); //writeablebitmap
            project.SnapshotThumbnail();

            var settings = saveSettings();

            settings.Error = (sender, args) =>
            {
                // Ignora el error actual y continúa con la serialización
                Output.Log(args.CurrentObject, "Error when saving project");
                args.ErrorContext.Handled = true;
            };

            string json = JsonConvert.SerializeObject(project, settings);
            File.WriteAllText(filename, json);

            project.IsSaved = true;
            foreach (var shot in project.ShotsCollection)
            {
                //save shots with foregin url
                if (shot.SavedPath != null && shot.SavedPath != project.SavedPath)
                    project.SaveShot(shot);
                else
                {
                    shot.SavedOnProject = true;
                    shot.IsSaved = true;
                }
            }


            var file = new AssetFile()
            {
                Name = project.Name,
                Path = filename,
                Thumbnail = project.Thumbnail
            };


            Settings.AddRecentFile(file);
            Output.Log($"{project.Name} saved.");
            //  System.Media.SystemSounds.Asterisk.Play();
        }

        finally
        {
            Mouse.OverrideCursor = null;

        }
    }

    void EnsureSelections()
    {
        if (editorsSpace.Current_Workspace == null && editorsSpace.Workspaces.Any())
            editorsSpace.Current_Workspace = editorsSpace.Workspaces[0];

        if (SelectedShot == null && ShotsCollection.Any())
            SelectedShot = ShotsCollection[0];

        if (SelectedBrush == null && Brushes.Any())
            SelectedBrush = Brushes[0];

        if (generationManager.SelectedPreset == null && generationManager.PromptPresets.Any())
            generationManager.SelectedPreset = generationManager.PromptPresets[0];

        //if (generationManager.SelectedPrompt == null && generationManager.Prompts.Any())
        //    generationManager.SelectedPrompt = generationManager.Prompts.First();
    }
    public static Project LoadProject(string filename)
    {
        string json = File.ReadAllText(filename);
        var settings = saveSettings();

        settings.Error = (sender, args) =>
        {
            // Ignora el error actual y continúa con la serialización
            Output.Log($"{args.CurrentObject} \n {args.ErrorContext}", "Error when loading project, project corrupted");
            args.ErrorContext.Handled = true;
        };

        Project project = JsonConvert.DeserializeObject<Project>(json, settings);
        project.IsSaved = true;
        project.EnsureSelections();

        foreach (var shot in project.ShotsCollection)
        {
            foreach (var layer in shot.layers)
            {
                layer.OnProjectLoaded(project);
            }

        }


        project.generationManager.Initialize();


        return project;

    }

    public static void OpenProject(string filename)
    {

        try
        {
            IsLoading = true;
            AppModel.InstantiateThings = false;

            Project p = LoadProject(filename);

            p.SelectedBrush = new Brusher();
            p.SelectedBrush.Size.EnablePenPressure = true;
            p.IsSaved = true;
            p.ShotsCollection.ForEach(shot => shot.IsSaved = true);

            AppModel.project.toolManager.CurrentToolSpace = null;
            p.toolManager = AppModel.project.toolManager;

          //  p.Thumbnail = ManualCodec.RenderFrame(p.SelectedShot.Animation.CurrentFrame, p.SelectedShot).ScaleToWidth(256).ToByte(); //writeablebitmap
            p.SnapshotThumbnail();

            AppModel.project = p;
            AppModel.File_New(false);


            var file = new AssetFile()
            {
                Name = p.Name,
                Path = filename,
                Thumbnail = p.Thumbnail
            };
            Settings.AddRecentFile(file);


            // AppModel.InstantiateThings = true;


        }
        catch (Exception ex)
        {
            Output.Log(ex.Message + "\n \n corrupted project");
            AppModel.mainW.FadeBlackIn();
        }

        IsLoading = false;

    }

    private byte[] SnapshotThumbnail()
    {
        Thumbnail = ManualCodec.RenderFrame(SelectedShot.Animation.CurrentFrame, SelectedShot).ScaleToWidth(256).ToByte();
        return Thumbnail;
    }





    //--------- Global --------\\
    public dynamic props { get; set; } = new DynamicProperties();
    public static List<MenuItemNode> RegisteredActions { get; set; } = new()
     {
         //shot
         new MenuItemNode("Shot", "Add", "Layer", () => ManualAPI.SelectedShot.AddLayer()),
         new MenuItemNode("Shot", "Delete", "Layer", () => ManualAPI.SelectedShot.RemoveLayer()),
         new MenuItemNode("Shot", "Animation", "Render Image", AppModel.Animation_RenderImage),
         new MenuItemNode("Shot", "Animation", "Render Animation", AppModel.Animation_RenderAnimation),

         //project
         new MenuItemNode("Project", "Add", "Shot", () => ManualAPI.project.AddShot()),

         //layer
         new MenuItemNode("Layer", "Animation", "Insert Keyframe", AppModel.Animation_InsertKeyframe),
         new MenuItemNode("Layer", "Animation", "Delete Keyframe", AppModel.Animation_DeleteKeyframe),
        
         //generate
         new MenuItemNode("PromptPreset", "Queue", "Generate", ManualAPI.GenerateImage),
         new MenuItemNode("PromptPreset", "Queue", "Interrupt", GenerationManager.Interrupt)

     };


    //spaces for plugins
    public RegionSpace regionSpace { get; set; } = new RegionSpace();

    public GenerationManager generationManager { get; set; } = new();

    //once all variables are loaded, then load the UI, otherwise doesn't binding correctly
    public EditorsSpace editorsSpace { get; set; } = new EditorsSpace();

    //settings of brush, txt2img, ui, etc
    [JsonIgnore] public ToolManager toolManager { get; set; }


    //--------------- RENDER -------------\\
    [JsonIgnore] public RenderManager renderManager { get; set; } = new();



    // Constructor
    public Project()
    {

     
    }


    //INSTANTIATE THINGS PROJECT SHOT
    public void InstantiateThings()
    {
        if (!AppModel.InstantiateThings)
            return;

        //Shot shot = new Shot(true) { Name = "Shot" };
        //ShotsCollection.Add(shot);
        //OpenShot(shot);
        //SelectedShot = shot;


        ShotBuilder.SelectedTemplate = Shot.defaultTemplate; //ShotBuilder.Templates[2]; //0 = 512x512    //2 = 1024x1024
        ShotBuilder.CreateSelected();


        Brusher b = new();
        Brushes.Add(b);
        SelectedBrush = b;
        SelectedBrush.Size.EnablePenPressure = true;

        generationManager.Initialize();
    }

    //----------------------------------------------------------------------------- SHOT -----------------------------------------------------------------\\

    /// <summary>
    /// the opened shots
    /// </summary>
    [JsonIgnore] public MvvmHelpers.ObservableRangeCollection<Shot> ShotsCollectionOpened { get; set; } = new();


    [JsonConverter(typeof(ShotsCollectionConverter))]
    public ObservableCollection<Shot> ShotsCollection { get; set; } = new ObservableCollection<Shot>();
    [ObservableProperty][property: JsonIgnore] private Shot _selectedShot = null;

    public delegate void ShotEvent(Shot shot);
    public static event ShotEvent ShotChanged;

    // --------- initialize --------- \\
    partial void OnSelectedShotChanged(Shot oldValue, Shot newValue)
    {
        if (oldValue != null)
        {
            if (oldValue.Animation.IsPlaying)
                oldValue.Animation.Stop();

            oldValue.UpdateThumbnail();
        }

        if (newValue != null && newValue.Thumbnail == null)
        {
            //open if not oppened yet
            if (!IsShotOpen(newValue))
                ShotsCollectionOpened.Add(newValue);

            newValue.UpdateThumbnail();

        }



        ShotChanged?.Invoke(newValue);
        Shot.InvokeSelectedLayerChanged();
        Shot.UpdateCurrentRender();
    }


    [RelayCommand]
    [property: JsonIgnore]
    public void AddShot()
    {
        ShotBuilder.CreateSelected();
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void OpenAddShot()
    {
        ShotBuilder.Open();
    }

    public void AddShot(Shot shot)
    {
        ShotsCollection.Add(shot);
        SelectedShot = shot;
    }


    public void OpenShot(Shot shot)
    {
        if (!IsShotOpen(shot))
        {
            ShotsCollectionOpened.Add(shot);
        }
        SelectedShot = shot;
    }

    public bool IsShotOpen(Shot shot)
    {
        bool isOpen = ShotsCollectionOpened.Contains(shot);
        return isOpen;
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void CloseShot() => CloseShot(SelectedShot);
    public void CloseShot(Shot shot)
    {

        if (!IsShotOpen(shot) || AppModel.project.ShotsCollection.Count == 1)
            return;


        bool dontClose = false;
        if (!IsSaved)
        {
            AppModel.saveShotBox([shot], () => AppModel.saveShotDialog(shot), null, () => dontClose = true);

            if (dontClose)
                return;
        }
        ReorderOpened(shot);
        ShotsCollectionOpened.Remove(shot);

    }

    public void DeleteShot(Shot shot)
    {

        // Removemos el shot de la colección
        CloseShot(shot);
        ShotsCollection.Remove(shot);


    }
    private void ReorderOpened(Shot shot)
    {
        int index = ShotsCollectionOpened.IndexOf(shot);
        if (index == -1)
            return;

        if (ShotsCollectionOpened.Count == 1)
        {
            SelectedShot = null;
        }
        else
        {
            if (index == 0)
            {
                SelectedShot = ShotsCollectionOpened[1];
            }
            else
            {
                SelectedShot = ShotsCollectionOpened[index - 1];
            }
        }
    }
    void ReorderOpenedOld(Shot shot)
    {

        // Obtenemos el index del shot seleccionado
        int selectedIndex = ShotsCollectionOpened.IndexOf(shot);

        // Obtenemos el nuevo index de la pestaña seleccionada
        int newSelectedIndex = selectedIndex;

        if (SelectedShot != shot)
            return;


        if (ShotsCollectionOpened.Count == 0)
        {
            // Si no quedan pestañas, no seleccionamos ninguna
            SelectedShot = null;
        }
        else if (newSelectedIndex >= ShotsCollectionOpened.Count)
        {
            // Si el index anterior estaba fuera de rango, seleccionamos la última pestaña
            newSelectedIndex = ShotsCollectionOpened.Count - 1;
            SelectedShot = ShotsCollectionOpened[newSelectedIndex];
        }
        else
        {
            // Seleccionamos la pestaña anterior
            SelectedShot = ShotsCollectionOpened[newSelectedIndex];
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------- SAVE SHOT

    public static JsonSerializerSettings saveSettings()
    {
        return new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto, // Incluye información de tipo solo donde sea necesario
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };
    }

    public void SaveShot(Shot shot)
    {
        if (shot.SavedPath != null)
            SaveShot(shot, shot.SavedPath);
        else
            AppModel.saveShotDialog(shot);
    }


    public string SaveShotJson(Shot shot)
    {
        var settings = saveSettings();

        string json = JsonConvert.SerializeObject(shot, settings);
        return json;
    }
    public void SaveShot(string filename)
    {
        SaveShot(SelectedShot, filename);
    }

    public void SaveShot(Shot shot, string filename) // -------- MAIN
    {
        string extension = Path.GetExtension(filename);
        string name = Path.GetFileNameWithoutExtension(filename);

        var thumbnail = ManualCodec.RenderFrame(shot.Animation.CurrentFrame, shot).ToByte();

        shot.Name = name;
        shot.SavedPath = filename;
        //   shot.Thumbnail = thumbnail;

        if (extension == ".shot")
        {
            var settings = saveSettings();
            string json = JsonConvert.SerializeObject(shot, settings);
            File.WriteAllText(filename, json);
        }
        else if (extension == ".png")
        {
            ManualCodec.SaveImage(thumbnail, filename);
        }


        // ManualCodec.SaveShotWithThumbnail(shot, filename, preview);
        shot.IsSaved = true;
        shot.actionHistory.Saved();
        Output.Log($"{shot.Name} saved.");
    }





    public static Shot LoadShot(string filename)
    {

        string json = File.ReadAllText(filename);
        var settings = saveSettings();

        Shot shot = JsonConvert.DeserializeObject<Shot>(json, settings);

        //(Shot shot, BitmapImage image) = ManualCodec.LoadShotAndThumbnail(filename);
        //var preview = new WriteableBitmap(image);

        shot.IsSaved = true;
        shot.EnsureSelections();


        return shot;
    }
    public void ImportShot(string filename)
    {
        var shot = LoadShot(filename);
        shot.SeeAllLayerTypeFilter(true);
        AddShot(shot);
    }
    public void ImportImages(string filename)
    {
        Layer.New(filename);
    }

    internal void ClearAllShotCaches()
    {
        foreach (var shot in ShotsCollection)
            shot.ClearCache();
        

    }


    // -------------- LAZY -------------- \\ //TODO: implementar Lazy<>
    //  private Dictionary<int, UIElement> ShotUIs = new Dictionary<int, UIElement>();
    //  [ObservableProperty] private UIElement _selectedShotUI = null;





    //--------------- BRUSHES -------------\\
    ObservableCollection<Brusher> Brushes { get; set; } = new();
    [ObservableProperty][property: JsonIgnore] Brusher selectedBrush = null;

    //--------------- SCRIPTS -------------\\
    public ScriptingManager scriptingManager = new();

    [ObservableProperty] Color selectedColor = Colors.Black;
    [ObservableProperty] Color secondaryColor = Colors.White;
    [ObservableProperty] bool isColorEraser = false;



    public ShotBuilder ShotBuilder = new(true);



    #region ----------------------------------------------------------------------- SERIALIZE


    [JsonProperty]
    int SelectedShotId
    {
        get => ShotsCollection.IndexOf(SelectedShot);
        set => SelectedShot = ShotsCollection.ItemByIndex(value);
    }
    public static bool IsLoading { get; internal set; } = true;


  [JsonConverter(typeof(IdListConverter))]
  [JsonProperty]
    List<Guid> ShotsOpenedId
    {
        get
        {
            var s =  ShotsCollectionOpened.Select(s => s.Id).ToList();
            return s;
        }
        set
        {
            ShotsCollectionOpened.ReplaceRange(value.Select(id => ShotsCollection.FirstOrDefault(s => s.Id == id)));
        }
    }
    #endregion -----------------------------------------------------------------------
}






public class IdListConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<Guid>);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JArray array = JArray.FromObject(value, serializer);
        array.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var guids = serializer.Deserialize<List<Guid>>(reader);
        return guids;
    }
}


public class ShotsCollectionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(ObservableCollection<Shot>));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var shots = value as ObservableCollection<Shot>;
        JArray array = new JArray();
        foreach (var shot in shots)
        {
            if (!string.IsNullOrEmpty(shot.SavedPath))
            {
                // Serializa un objeto anónimo con el path y otras propiedades
                var shotPathObject = new { shot.SavedPath };
                array.Add(JObject.FromObject(shotPathObject, serializer));
            }
            else
            {
                // Serializa el objeto Shot completo
                JObject shotObject = JObject.FromObject(shot, serializer);
                array.Add(shotObject);
            }
        }
        array.WriteTo(writer);
    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);
        var shots = new ObservableCollection<Shot>();
        foreach (var item in array)
        {

            if (item.Type == JTokenType.Object && item["SavedPath"] != null && !string.IsNullOrEmpty(item["SavedPath"].ToString()))
            {
                // El item es un objeto con SavedPath no vacío, carga el Shot desde el path
                string path = item["SavedPath"].ToObject<string>();
                Shot loadedShot = Project.LoadShot(path);
                shots.Add(loadedShot);
            }
            else
            {
                // El item es un objeto Shot completo, deserialízalo normalmente
                Shot shot = item.ToObject<Shot>(serializer);
                shots.Add(shot);
            }

        }
        return shots;
    }

}


public partial class ToolManager : ObservableObject//, IPlugable
{
    [JsonIgnore]public ObservableCollection<Tool> Spaces { get; set; } = new();
    [JsonIgnore] private Tool currentToolSpace;

    [JsonIgnore]
    public Tool CurrentToolSpace
    {
        get { return currentToolSpace; }
        set
        {
            if (currentToolSpace != value)
            {
                var OldTool = currentToolSpace;
                ToolChanged(OldTool, value);

                currentToolSpace = value;

                if (OldTool != null)
                {
                    OldTool.OnNewToolSelected();
                    if(value != null)
                    AppModel.project.editorsSpace.Current_Workspace.tool = value.name;
                }

                OnPropertyChanged();
              //  WeakReferenceMessenger.Default.Send(new NotifyMessage(value)); //TODO: talvez a futuro se necesite NotifyMessage
            }
        }
    }

    public ToolManager()
    {

        // PluginsManager_OnUpdatePlugins();
       // PluginsManager.OnUpdatePlugins += PluginsManager_OnUpdatePlugins;
        
    }

    //public void PluginsManager_OnUpdatePlugins()
    //{
    //    // Spaces = AppModel.project.regionSpace.ed_tools.tools;
    //    if (CurrentToolSpace != null)
    //    {
    //        if (AppModel.project.editorsSpace.Current_Workspace.tool != "")
    //            CurrentToolSpace = Tool.ByName(AppModel.project.editorsSpace.Current_Workspace.tool);
    //        else
    //        CurrentToolSpace = Tool.ByName(CurrentToolSpace.name);

    //    }
    //}
    public void WorkspaceChanged(Tool value)
    {
        CurrentToolSpace = value;
    }

    [JsonIgnore]
    public Tool OldTool;
    // fire events
    void ToolChanged(Tool OldTool, Tool NewTool)
    {
      //  if(OldTool.name != "Pan" && OldTool.name != "Rot")
           this.OldTool = OldTool;

        if(OldTool != null)
        OldTool.OnToolDeselected();

        if (NewTool != null)
        {
            NewTool.OnToolSelected();
            OnToolChanged?.Invoke(OldTool, NewTool);
        }

        if(Shortcuts.CurrentCanvas != null && NewTool != null)
        Shortcuts.CurrentCanvas.Cursor = NewTool.cursor;

    }
    public delegate void ToolChangedHandler(Tool? oldTool, Tool newTool);
    public static event ToolChangedHandler OnToolChanged;



}
