using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manual.API;
using Manual.Editors;
using Manual.Editors.Displays;
using Manual.Objects;
using Manual.Objects.UI;
using Manual.Properties;
using ManualToolkit.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Manual.Core;

public partial class EditorsSpace : ObservableObject
{

    public static EditorsSpace instance => AppModel.project?.editorsSpace;

    private static readonly string _workspacesPath = $"{App.LocalPath}Resources/Templates/Workspaces";
    //--------------- EDITORS INTERFACE -------------\\
    public ObservableCollection<WorkspaceSingle> Workspaces { get; set; } = new();
    [ObservableProperty] [property: JsonIgnore] WorkspaceSingle _current_Workspace;

    // --------- initialize --------- \\
    //  private bool changedOnce = false;

    partial void OnCurrent_WorkspaceChanged(WorkspaceSingle? oldValue, WorkspaceSingle newValue)
    {
        if (oldValue != null)
            oldValue.IsSelected = false;

        if (newValue != null)
            newValue.IsSelected = true;



        if (newValue == null) // workspace deleted
            return;

        if(AppModel.mainW != null && AppModel.mainW.splash.Visibility == Visibility.Visible)
            AppModel.mainW.CloseSplash();

        ToolManager toolManager = AppModel.project.toolManager;

        if ((newValue.tool == "" || newValue.tool == null) && toolManager.Spaces.Count != 0)
            toolManager.CurrentToolSpace = toolManager.Spaces[0];
        else
        {
            toolManager.WorkspaceChanged(toolManager.Spaces.FirstOrDefault(x => x.name == newValue.tool));
        }



        //if doens't have a canvas, will set to null;
        InvokeGlow();
    }


    public void NewEditorWindow()
    {
        Current_Workspace.AddFloatEditor();
    }
    public void NewEditorWindow(Editors editor)
    {
        Current_Workspace.AddFloatEditor(editor);
    }

    public void NewBlankWorkspace()
    {
        WorkspaceEditor e = new(Editors.Canvas);
        WorkspaceSingle s = new(e);
        s.Name = "workspace";
        Workspaces.Add(s);
        Current_Workspace = s;
    }

    public void LoadWorkspaces()
    {
        Workspaces.Clear();
        string folderPath = _workspacesPath;
        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        var loadedWorkspaces = new List<WorkspaceSingle>();

        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                var wi = LoadWorkspace(jsonFile); //path
                loadedWorkspaces.Add(wi);
            }
            catch (Exception ex)
            {
                Output.Log($"{ex.Message}", "this workspace cannot be loaded");
            }
        }

        foreach (var workspace in loadedWorkspaces.OrderBy(sItem => sItem.Id))
        {
            Workspaces.Add(workspace);
        }

        if (Workspaces.Any())
        {
            Current_Workspace = Workspaces[0];
        }
        else
        {
            NewBlankWorkspace();
        }
    }
    public WorkspaceSingle LoadWorkspace(string filename)
    {
        var json = File.ReadAllText(filename);
        var settings = Project.saveSettings();
        var wi = JsonConvert.DeserializeObject<WorkspaceSingle>(json, settings);

        wi.Name = Path.GetFileNameWithoutExtension(filename);

        return wi;
    }
    public void LoadWorkspaceAndAdd(string filename)
    {
        var wi = LoadWorkspace(filename);
        Workspaces.Add(wi);
        Current_Workspace = wi;
    }

    public void ReturnToDefaultWorkspace()
    {
        LoadWorkspaces();
    }
    public void SaveAllWorkspaces()
    {
        foreach (var workspace in Workspaces)
        { 
            workspace.Save();
        }
    }

    public void DuplicateWorkspace() { DuplicateWorkspace(Current_Workspace); }
    public void DuplicateWorkspace(WorkspaceItem workspace)
    {
      //  if (!UserManager.IsPro && Workspaces.Count > 6)
       //     return;

        var settings = Project.saveSettings();
        var json = JsonConvert.SerializeObject(workspace, settings);

        var duplicate = JsonConvert.DeserializeObject<WorkspaceSingle>(json, settings);

        Workspaces.Add(duplicate);
        Current_Workspace = duplicate;

    }
    public void SaveWorkspace() { SaveWorkspace(Current_Workspace); }
    public void SaveWorkspace(WorkspaceSingle workspace)
    {
        workspace.Id = Workspaces.IndexOf(workspace);
   
        var path = $"{_workspacesPath}/{workspace.Name}.json";
        var settings = Project.saveSettings();
        string json = JsonConvert.SerializeObject(workspace, settings);
        File.WriteAllText(path, json);
    }



    public void DeleteWorkspace() { DeleteWorkspace(Current_Workspace); }
    public void DeleteWorkspace(WorkspaceSingle workspace)
    {
        if (Workspaces.Count != 1)
        {
            if(Current_Workspace == workspace)
               Current_Workspace = Workspaces[Workspaces.IndexOf(workspace) - 1];

            Workspaces.Remove(workspace);
        }
    }

    public void MoveLeftWorkspace(WorkspaceSingle workspace)
    {
        int currentIndex = Workspaces.IndexOf(workspace);
        int newIndex = currentIndex - 1;
        if (currentIndex > 0) // Asegúrate de no estar en el primer elemento
        {
            Workspaces.Move(currentIndex, newIndex);
        }
    }
    public void MoveRightWorkspace(WorkspaceSingle workspace)
    {
        int currentIndex = Workspaces.IndexOf(workspace);
        int newIndex = currentIndex + 1;
        if (currentIndex < Workspaces.Count - 1) // Asegúrate de no estar en el último elemento
        {
            Workspaces.Move(currentIndex, newIndex);
        }
    }





    //----------------------------------------------------------------------------------------------- WORKSPACES EDITOR
    public static readonly string RowWay = "Row";
    public static readonly string ColumnWay = "Column";
    public void AddEditor(WorkspaceEditor spaceEditor, Space space, string way)
    {
      
       Space w;

        if (way == RowWay)
            w = new WorkspaceRow();
        else
            w = new WorkspaceColumn();


        w.Space0 = new WorkspaceEditor(spaceEditor.SelectedEditor.Name);
        w.Space1 = new WorkspaceEditor(spaceEditor.SelectedEditor.Name);

        // Replace the Space of spaceEditor by w
        if (space.Space0 == spaceEditor)
        {
            space.Space0 = w;
        }
        else
        {
            space.Space1 = w;
        }

    }

    public void CloseEditor(WorkspaceEditor spaceEditor, Space space, Space spaceParent)
    {

        WorkspaceItem w = new(); //the opposite of the editor closed

        // set the opposite
        if (space.Space0 == spaceEditor)
            w = space.Space1;
        else
            w = space.Space0;
     

        // Replace the Space0 of spaceParent
        if (spaceParent.Space0 == space)
        {
            spaceParent.Space0 = w;
        }
        else
        {
            spaceParent.Space1 = w;
        }
    }




    //-------------------- CANVAS GLOWE
    [JsonIgnore] public CanvasView? CanvasView { get; set; }
    public void UpdateGlow()
    {
        if (AppModel.mainW != null)
            AppModel.mainW.UpdateGlow();
    }
    public void InvokeGlow()
    {
        if (AppModel.mainW != null)
            AppModel.mainW.InvokeGlow();
    }



    #region ----------------------------------------------------------------------- SERIALIZE


    [JsonProperty]
    int Current_WorkspaceId
    {
        get => Workspaces.IndexOf(Current_Workspace);
        set => Current_Workspace = Workspaces.ItemByIndex(value);
    }

    #endregion -----------------------------------------------------------------------

}


//---------------------------------------------------------------------------------------------------------------------------- WORKSPACE ITEMS
public partial class WorkspaceItem : ObservableObject
{


    [JsonIgnore] public Space AttachedSpace { get; internal set; }

    public WorkspaceItem()
    {
        
    }
}

public partial class Space : WorkspaceItem
{
  
    [ObservableProperty] double _length0;
    [ObservableProperty] double _length1;

    [ObservableProperty] WorkspaceItem _space0;
    [ObservableProperty] WorkspaceItem _space1;
    public Space()
    {
        Length0 = 1;
        Length1 = 1;
    }

  
}

public partial class WorkspaceRow : Space
{
    public WorkspaceRow()
    {
        
    }
}
public partial class WorkspaceColumn : Space
{
    public WorkspaceColumn()
    {
        
    }
}
public partial class WorkspaceSingle : Space, INamable
{
    [ObservableProperty] [property: JsonIgnore] bool isSelected;

    [ObservableProperty] int _id;
    [ObservableProperty] string _name;

    public string tool { get; set; } = "";



    //Space0 all the stuff
    //Space1 reserved for fullscreen;
    public WorkspaceSingle()
    {
        //   if (fullScreen)
        //       SwitchFullScreenEditor(fullEditor);
        FloatingEditors.CollectionChanged += FloatingEditors_CollectionChanged;
    }
    public WorkspaceSingle(WorkspaceItem editor)
    {
        Space0 = editor;
        FloatingEditors.CollectionChanged += FloatingEditors_CollectionChanged;
    }

    public bool fullScreen = false;
    WorkspaceItem fullEditor;
    public void SwitchFullScreenEditor(WorkspaceItem space)
    {
        if (!fullScreen)
        {
            fullEditor = space;
            Space1 = Space0;
            Space0 = fullEditor;
            fullScreen = true;
        }
        else
        {
            Space0 = Space1;
            fullScreen = false;
        }
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void Duplicate()
    {
        AppModel.project.editorsSpace.DuplicateWorkspace(this);
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void Save()
    {
        AppModel.project.editorsSpace.SaveWorkspace(this);
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void Delete()
    {
        AppModel.project.editorsSpace.DeleteWorkspace(this);
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void MoveLeft()
    {
        AppModel.project.editorsSpace.MoveLeftWorkspace(this);
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void MoveRight()
    {
        AppModel.project.editorsSpace.MoveRightWorkspace(this);
    }


    partial void OnIsSelectedChanged(bool value)
    {
        if (value == true)
            ShowFloatEditors();
        else
            HideFloatEditors();
        
    }
    public ObservableCollection<WorkspaceFloatingEditor> FloatingEditors { get; set; } = new();

    public void AddFloatEditor()
    {
        FloatingEditors.Add(new WorkspaceFloatingEditor(Editors.Output));
    }
    public void AddFloatEditor(Editors editor)
    {
        FloatingEditors.Add(new WorkspaceFloatingEditor(editor));
    }
    public void RemoveFloatEditor(WorkspaceFloatingEditor editor)
    {
        FloatingEditors.Remove(editor);
    }
    private void FloatingEditors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (WorkspaceFloatingEditor newEditor in e.NewItems)
            {
                newEditor.AttachedSpace = this;
                if (IsSelected)
                    newEditor.ShowEditor();
            }
        }
        if (e.OldItems != null)
        {
            foreach (WorkspaceFloatingEditor oldEditor in e.OldItems)
            {
                if (IsSelected)
                    oldEditor.HideEditor();
            }
        }
    }


    public void ShowFloatEditors()
    {
        foreach (var editor in FloatingEditors)
        {
            editor.ShowEditor();
        }
    }
    public void HideFloatEditors()
    {
        foreach (var editor in FloatingEditors)
        {
            editor.HideEditor();
        }

    }

}

public partial class WorkspaceFloatingEditor : WorkspaceEditor
{
    public WorkspaceFloatingEditor(Editors editorName) : base(editorName)
    {
    }



    W_Editor editor;
    public void ShowEditor()
    {
        if (editor == null)
        {
            var editorWindow = new W_Editor
            {
                DataContext = this,
            };
          //  editorWindow.window.Left = this.Left;
         //   editorWindow.window.Top = this.Top;
            editor = editorWindow;
        }
            M_Window.NewShow(editor, "", M_Window.TabButtonsType.OX, false);
      
    }
    public void HideEditor()
    {
        if (editor != null)
                editor.window.Close();
    }


    //DISABLED es más chungo de lo que parece, las ventanas deben acoplarse a un lado en concreto, como dockpanel
    public double RelativeWidth { get; set; } = 0.3;
    public double RelativeHeight { get; set; } = 0.3;
    // Suponiendo que también necesites mantener las posiciones relativas
    public double RelativeLeft { get; set; } = 0.5;
    public double RelativeTop { get; set; } = 0.5;


    // Propiedades para dimensiones absolutas que calculas en base a los valores relativos
    public double Width { get; set; } = 512; // => CalculateAbsoluteWidth();
    public double Height { get; set; } = 512; // => CalculateAbsoluteHeight();

    public double Left { get; set; } // => CalculateAbsoluteLeft();
    public double Top { get; set; } // => CalculateAbsoluteTop();


    private double CalculateAbsoluteWidth() => Application.Current.MainWindow.Width * RelativeWidth;

    private double CalculateAbsoluteHeight() => Application.Current.MainWindow.Height * RelativeHeight;

    private double CalculateAbsoluteLeft() => Application.Current.MainWindow.Left * RelativeLeft;

    private double CalculateAbsoluteTop() => Application.Current.MainWindow.Top * RelativeTop;



    public void UpdateRelativeDimensions(double absoluteWidth, double absoluteHeight)
    {
        RelativeWidth = absoluteWidth / Application.Current.MainWindow.Width;
        RelativeHeight = absoluteHeight / Application.Current.MainWindow.Height;
        editor.UpdateSize();
    }

    public void UpdateRelativeLocations(double absoluteLeft, double absoluteTop)
    {
        RelativeLeft = absoluteLeft / Application.Current.MainWindow.Left;
        RelativeTop = absoluteTop / Application.Current.MainWindow.Top;

        editor.UpdateLocation();
    }
}


public partial class WorkspaceEditor : WorkspaceItem, IPlugable
{
    [ObservableProperty] RegisteredEditor _selectedEditor;


    public ObservableCollection<RegisteredEditor> RegisteredEditors { get; set; } = new();



    public RegisteredEditor GetOrCreateEditor(string name)
    {
        RegisteredEditor editor = RegisteredEditors.FirstOrDefault(e => e.Name == name);

        if (editor == null)
        {
            if (Editor.editors.TryGetValue(name, out var editorFactory))
            {
                Editor newEditor = editorFactory.Invoke();
                editor = new RegisteredEditor(name, newEditor);
                RegisteredEditors.Add(editor);
            }
            else
            {
                throw new ArgumentException($"Unknown editor name: {name}");
            }
        }

        return editor;
    }


    public WorkspaceEditor() 
    {
      //  PluginsManager_OnUpdatePlugins();
        PluginsManager.OnUpdatePlugins += RefreshEditors;

    }
    public void RefreshEditors()
    {
        foreach (var editorEntry in Editor.editors)
        {
            string editorName = editorEntry.Key;

            if (!RegisteredEditors.Any(e => e.Name == editorName))
            {
                // El editor no existe en RegisteredEditors, lo creamos y lo agregamos
                try
                {
                    Editor newEditor = editorEntry.Value.Invoke();
                    RegisteredEditor newRegisteredEditor = new RegisteredEditor(editorName, newEditor);
                    RegisteredEditors.Add(newRegisteredEditor);
                }
                catch (Exception ex)
                {
                    Output.Log(ex.Message, "some editor cannot be loaded (EditorsSpace, WorkspaceEditor)");
                }

            }
            else
            {
                //PLUS
                //DISABLED RELEASE: workspaces
                foreach (var editor in RegisteredEditors.ToList())
                {
                    if(Editor.editors.Keys.FirstOrDefault(k => k == editor.Name) is null)
                    {
                        RegisteredEditors.Remove(editor);
                    }
                }
            }
        }

    }

    public WorkspaceEditor(Editors editorName)
    {
        RefreshEditors();
        PluginsManager.OnUpdatePlugins += RefreshEditors;
       // PluginsManager_OnUpdatePlugins();
        ChangeEditorTo(editorName);
    }
    public WorkspaceEditor(string editorName)
    {
        RefreshEditors();
        PluginsManager.OnUpdatePlugins += RefreshEditors;
      //  PluginsManager_OnUpdatePlugins();
        ChangeEditorTo(editorName);
    }
    public RegisteredEditor EditorByName(string name)
    {
        return RegisteredEditors.FirstOrDefault(editor => editor.Name == name);
    }


    public void ChangeEditorTo(string editorName)
    {
        this.SelectedEditor = EditorByName(editorName);
    }
    public void ChangeEditorTo(Editors editorName)
    {
        string editorNameString = editorName.ToString().Replace("_", " ");
        this.SelectedEditor = EditorByName(editorNameString);
    }


    // --------- initialize --------- \\
    private bool changedOnce = false;
    partial void OnSelectedEditorChanged(RegisteredEditor value)
    {

        if (!changedOnce)
        {
            SelectedEditor = AppModel.LoadSelectedInCollection(RegisteredEditors, value, nameof(value.Name)) ?? SelectedEditor;
            RefreshEditors();
            changedOnce = true;
        }


        //SetGlow();
    }


}

public enum Editors
{
    Tools,
    Canvas,
    Layers,
    Timeline,
    History_5D,
    Properties,
    Code_Editor,
    Output,
    Latent_Nodes_Editor,
}

public partial class Editor : ObservableObject
{

    [JsonIgnore]
    public static Dictionary<string, Func<Editor>> editors = new Dictionary<string, Func<Editor>>
    {
        { "Tools", () => new ED_Tools() },
        { "Canvas", () => new ED_CanvasView() },
        { "Layers", () => new ED_LayerView() },
        { "Timeline", () => new ED_TimelineView() },
        { "History 5D", () => new ED_History() },
        { "Properties", () => new ED_ToolProperties() },
        { "Code Editor", () => new ED_CodeEditorView() },
        { "Output", () => new ED_Output() },
         { "Latent Nodes Editor", () => new ED_LatentNodes() },
        // 9 EDITORS
    };

    public static void RegisterEditor(string name, Func<Editor> editorControl)
    {
        editors[name] = editorControl;
    }
    public static void RegisterEditor(ED_Control editorControl)
    {
        editors[editorControl.name] = () => { return editorControl; };
    }

    public Editor()
    {
        
    }
}

public class RegisteredEditor
{
    private string _name;

    public string Name
    {
        get { return _name; }
        set
        {
            _name = value;
            SetIcon();
        }
    }


    public Editor EditorObject { get; set; } = new ED_Tools();


    private byte[] _icon;

    [JsonIgnore]
    public byte[] Icon
    {
        get { return _icon; }
        set { _icon = value; }
    }

    public RegisteredEditor(string name, Editor editor)
    {
        Name = name;
        EditorObject = editor;

        SetIcon();
    }

    public RegisteredEditor() 
    {
    
    }
    public void SetIcon()
    {
        StreamResourceInfo resourceInfo;

        Uri uriSource = new Uri($"/Editors/res/icons/editor_{Name.Replace(" ", "_")}.png", UriKind.Relative);
        try
        {
            resourceInfo = Application.GetResourceStream(uriSource);
        }
        catch
        {
            uriSource = new Uri($"/Editors/res/icons/editor_Default.png", UriKind.Relative);
            resourceInfo = Application.GetResourceStream(uriSource);
        }

        using (var stream = resourceInfo.Stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                Icon = ms.ToArray();
            }
        }


    }

}

public class GridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double val = (double)value;
        GridLength gridLength = new GridLength(val, GridUnitType.Star);
        return gridLength;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        GridLength val = (GridLength)value;

        return val.Value;
    }
}