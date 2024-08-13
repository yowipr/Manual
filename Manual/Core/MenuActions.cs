using Manual.Editors;
using ManualToolkit.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using Manual.Editors.Displays;
using Manual.Core.Nodes;
using Manual.API;
using Manual.Objects;
using System.Windows.Media.Imaging;
using Manual.Core.Nodes.ComfyUI;
using Microsoft.WindowsAPICodePack.Dialogs;
using Manual.Editors.Displays.Launcher;
using SkiaSharp;
using SkiaSharp.Views.WPF;


namespace Manual.Core;

public partial class AppModel
{

    internal static void OpenLogin()
    {
        AppModel.mainW.FadeOut();
        if(AppModel.IsForceLogin)
            M_Window.Show(new W_Login(), "", M_Window.TabButtonsType.None);
        else
            M_Window.Show(new W_Login());
    }

    public static M_Window windowAssetStore;
    internal static void OpenAssetStore()
    {
            M_Window.NewShow(new W_Launcher(), "", M_Window.TabButtonsType._OX);
    }

    // --------------------------- FILE --------------------------- \\
    //DISABLED RELEASE: shot - se abre el shotbuilder

    public static void V_File_New()
    {
        ShotBuilder.instance.Open();
    }

    public static void File_New(bool instantiateThings = true)
    {  
        InstantiateThings = instantiateThings;
        if (instantiateThings)
        {
            var oldp = project;
            project = new();
            project.InstantiateThings();
            //output = new Output();

            if (oldp == null)
            {
                project.toolManager = new ToolManager();
                toolSpaces = new ToolSpaces();
            }
            else
            {
                project.toolManager = oldp.toolManager;
                project.editorsSpace.LoadWorkspaces();
             
            }


        }
        PluginsManager.LoadPlugins();
        project.toolManager.WorkspaceChanged(project.toolManager.Spaces.FirstOrDefault(x => x.name == project.editorsSpace.Current_Workspace.tool));


        // Mostrar ventana principal

        mainWOld = mainW;
        MainWindow m = new();// XD 
        m.Show();
        mainW = m;

        DisposePlugins();

    }

    public static void File_Open()
    {
        var dialog = new OpenFileDialog();
        dialog.DefaultExt = ".manual";
        dialog.Filter = "Manual files (*.manual;*.shot;*.png)|*.manual;*.shot;*.png|Manual Project files (*.manual)|*.manual|Shot files (*.shot)|*.shot|PNG images (*.png)|*.png|All files (*.*)|*.*";

        if (dialog.ShowDialog() == true)
        {
            string filePath = dialog.FileName;
            string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

            switch (fileExtension)
            {
                case ".manual":
                    LoadProject(filePath);
                    break;
                case ".shot":
                    project.ImportShot(filePath);
                    break;
                case ".png":
                    project.OpenPng(filePath);
                    break;
                default:
                    // Manejar otros tipos de archivos si es necesario
                    break;
            }
            AppModel.mainW.CloseSplash();
        }
    }

    public static async void LoadProject(string filename)
    {

        if (!File.Exists(filename))
        {
            Output.Log("this file has moved or no longer exists", filename);
            Settings.RemoveRecentFile(filename);
            return;
        }

        // Mostrar animación de carga
      
        Storyboard fadeInStoryboard = (Storyboard)mainW.FindResource("loadingMain");
        fadeInStoryboard.Begin(mainW);

        Mouse.OverrideCursor = Cursors.Wait;

        //Project.OpenProject(filename);
        await Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                    Project.OpenProject(filename);
            });


        });


        Mouse.OverrideCursor = null;

    }

    public static void File_Save()
    {
        Project.SaveProject();    
    }
    public static void File_SaveAs()
    {
        saveProjectDialog(project);
    }

    public static void File_ImportFile()
    {
        var dialog = new OpenFileDialog();
        // dialog.DefaultExt = ".shot"; // Filtro para solo mostrar archivos con la extensión .shot
        // dialog.Filter = "Shot files (*.shot)|*.shot|All files (*.*)|*.*"; // Filtro para mostrar todos los archivos o solo los .shot
        dialog.Multiselect = true; // Permite seleccionar varios archivos
        bool? result = dialog.ShowDialog(); // Muestra el diálogo y espera a que el usuario seleccione los archivos

        if (result == true)
        {
            foreach (string filename in dialog.FileNames)
            {
                var extension = Path.GetExtension(filename);
                if (extension == ".shot")
                {
                    project.ImportShot(filename); // Si el archivo es .shot, llama al método ImportShots
                }
                else if (extension == ".png")
                {
                    project.ImportImages(filename); // Si el archivo es una imagen, llama al método ImportImages
                }
                else if (extension == ".pp")
                {
                    GenerationManager.ImportPromptPreset(filename);
                }
            }
        }
    }


    //----------------------------------------------------------------------------------------------- SHOT

    internal static void File_SaveShot()
    {
        if (AppModel.project.SelectedShot.SavedPath != null)
        {
            AppModel.project.SaveShot(AppModel.project.SelectedShot.SavedPath);
            return;
        }

        saveShotDialog(AppModel.project.SelectedShot);
    }

    public static bool saveShotDialog(Shot shot)
    {
        var dialog = new SaveFileDialog();
        dialog.FileName = shot.Name; // establece el nombre de archivo predeterminado
        dialog.DefaultExt = ".shot";
        dialog.Filter = "Shot files (*.shot)|*.shot|All files (*.*)|*.*";
        if (dialog.ShowDialog() == true)
        {
            AppModel.project.SaveShot(shot, dialog.FileName);
            return true;
        }
        else
            return false;
    }
    public static bool saveProjectDialog(Project project)
    {
        var dialog = new SaveFileDialog();
        dialog.FileName = project.Name; // establece el nombre de archivo predeterminado
        dialog.DefaultExt = ".manual";
        dialog.Filter = "Manual Project files (*.manual)|*.manual|All files (*.*)|*.*";
        if (dialog.ShowDialog() == true)
        {
            Settings.RemoveRecentFile(project.SavedPath);
            Project.SaveProject(project, dialog.FileName);
            return true;
        }
        else
            return false;
    }
    /// <summary>
    /// return if all files are saved;
    /// </summary>
    /// <returns></returns>
    internal static bool File_SaveAllShots()
    {

        // SAVE SHOTS WITH PATH
        var unsavedShotsPath = ManualAPI.project.ShotsCollection.Where(shot => !shot.IsSaved && shot.SavedPath != null).ToList();
        foreach (var shot in unsavedShotsPath)
            AppModel.project.SaveShot(shot);



        // SAVE SHOTS WHITOUT PATH
        var unsavedShots = ManualAPI.project.ShotsCollection.Where(shot => !shot.IsSaved && shot.SavedPath == null).ToList();
        if (unsavedShots.Count == 1)
            return saveShotDialog(unsavedShots[0]);


        if (!unsavedShots.Any())
            return true;

        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            string folderPath = dialog.FileName;

            foreach (var shot in unsavedShots)
            {
                string filename = Path.Combine(folderPath, $"{shot.Name}.shot");
                AppModel.project.SaveShot(shot, filename);
            }
            return true;
        }
        else
            return false;

    }


    public static void saveShotBox(List<Shot> unsavedShots, Action? ok, Action? no, Action? cancel)
    {
        var unsavedShotNames = unsavedShots.Select(shot => $"{shot.Name}*");
        var msg = string.Join(", ", unsavedShotNames);
        M_MessageBox mbox = new(msg, "Save shots before closing?", MessageBoxButton.YesNoCancel, ok, no, cancel);
        mbox.SetBtnNames("Save", "Don't Save", "Cancel");
        M_MessageBox.ShowD(mbox);
        mbox.WClose.Visibility = Visibility.Collapsed;
    }

    public static void saveProjectBox(Project project, Action? ok, Action? no, Action? cancel)
    {
        string name = string.IsNullOrEmpty(project.SavedPath) ? project.Name : project.SavedPath;
        M_MessageBox mbox = new($"{name}*", "Save Project before closing?", MessageBoxButton.YesNoCancel, ok, no, cancel);
        mbox.SetBtnNames("Save", "Don't Save", "Cancel");
        M_MessageBox.ShowD(mbox);
        mbox.WClose.Visibility = Visibility.Collapsed;
    }




    public static void File_ImportPromptPreset()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Define las propiedades del OpenFileDialog
            Filter = "Prompt Preset files (*.json)|*.json|Image files (*.png)|*.png",
            Title = "Select a Prompt Preset File",
        };

        // Muestra el diálogo OpenFileDialog
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var fileName = openFileDialog.FileName;

                var ext = Path.GetExtension(fileName);
                if (ext == ".json")
                    Comfy.ImportWorkflow(fileName);
                else if (ext == ".png")
                    Comfy.ImportNodesFromImage(fileName);
                else if (ext == ".pp")
                GenerationManager.ImportPromptPreset(fileName);

            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de lectura de archivo o deserialización
                Output.Show("Error al cargar el archivo: " + ex.Message);
            }
        }
    }


    public static void SavePromptPreset()
    {

        var o = GenerationManager.Instance.SelectedPreset.GetOutputNode();

        bool ism = o is OutputNode;

        if (ism)
            export_promptPreset();
        else
            export_workflow();
    }


    static void export_promptPreset()
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            // Define las propiedades del SaveFileDialog
            Filter = "Prompt Preset files (*.pp)|*.pp",
            Title = "Save Prompt Preset",
            FileName = $"{GenerationManager.Instance.SelectedPreset.Name}.pp",
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // Aquí puedes llamar a tu método de guardado, pasando el nombre de archivo seleccionado
                //internal
                GenerationManager.SavePromptPreset(saveFileDialog.FileName);
                //comfy
                // Comfy.ExportWorkflow(ManualAPI.SelectedPreset, saveFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de escritura de archivo o serialización
                Output.Log(ex, "Error while saving PromptPreset pp");
            }
        }
    }
    static void export_workflow()
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            // Define las propiedades del SaveFileDialog
            Filter = "Prompt Preset files (*.json)|*.json",
            Title = "Save Prompt Preset",
            FileName = $"{GenerationManager.Instance.SelectedPreset.Name}.json",
        };

        // Muestra el diálogo SaveFileDialog
        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // Aquí puedes llamar a tu método de guardado, pasando el nombre de archivo seleccionado
                //internal
                GenerationManager.SavePromptPreset(saveFileDialog.FileName);
                //comfy
                // Comfy.ExportWorkflow(ManualAPI.SelectedPreset, saveFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de escritura de archivo o serialización
                Output.Log(ex, "Error while saving PromptPreset");
            }
        }
    }
    public static void export_workflow_api()
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            // Define las propiedades del SaveFileDialog
            Filter = "Workflow API files (*.json)|*.json",
            Title = "Save Workflow",
            FileName = $"{GenerationManager.Instance.SelectedPreset.Name}_api.json",
        };

        // Muestra el diálogo SaveFileDialog
        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // Aquí puedes llamar a tu método de guardado, pasando el nombre de archivo seleccionado
                //internal
                Comfy.ExportWorkflowAPI(ManualAPI.SelectedPreset, saveFileDialog.FileName);
                //comfy
                // Comfy.ExportWorkflow(ManualAPI.SelectedPreset, saveFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de escritura de archivo o serialización
                Output.Log(ex, "Error while saving PromptPreset api");
            }
        }
    }


    public static void ShowMiniDialog(string message, string title, string sok, Action? ok, string sno, Action? no, string scancel, Action? cancel)
    {
        ShowMiniDialog(message, title, MessageBoxButton.YesNoCancel, sok, ok, sno, no, scancel, cancel);
    }
    public static void ShowMiniDialog(string message, string title, string sok, Action? ok, string sno, Action? no)
    {
        ShowMiniDialog(message, title, MessageBoxButton.YesNo, sok, ok, sno, no, "", null);
    }

    public static void ShowMiniDialog(string message, string title, MessageBoxButton btn, string sok, Action? ok, string sno, Action? no, string scancel, Action? cancel)
    {
        M_MessageBox mbox = new(message, title, btn, ok, no, cancel);
        mbox.SetBtnNames(sok, sno, scancel);
        M_MessageBox.ShowD(mbox);
    }

    static void saveDialog(string filter, string title, string filename, Action<SaveFileDialog> onSaved, Action? onCancel, Action<Exception>? onError)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            // Define las propiedades del SaveFileDialog
            Filter = filter,
            Title = title,
            FileName = filename,
        };

        // Muestra el diálogo SaveFileDialog
        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                onSaved?.Invoke(saveFileDialog);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
        else
        {
            onCancel?.Invoke();
        }
    }


    public static void File_ImportPrompt()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Define las propiedades del OpenFileDialog
            Filter = "Prompt files (*.prompt)|*.prompt",
            Title = "Select a Prompt File",
        };

        // Muestra el diálogo OpenFileDialog
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                Prompt.V_ImportPrompt(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de lectura de archivo o deserialización
                Output.Show("Error al cargar el archivo: " + ex.Message);
            }
        }
    }

    public static void SavePrompt()
    {
        saveDialog(
            filter: "Prompt files (*.prompt)|*.prompt",
            title: "",
            filename: "",

            onSaved: (s) => Prompt.ExportPrompt(GenerationManager.Instance.SelectedPrompt, s.FileName),
            onCancel: null,
            onError: (ex) => Output.Log($"{ex}", "error")
            );
    }



    public static void File_ExportLayer()
    {
        var layer = ManualAPI.SelectedLayer;
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "PNG Image|*.png";
        saveFileDialog.Title = "Guardar Secuencia de Imágenes";
        saveFileDialog.FileName = layer.Name; // Sugerir un nombre base para los archivos

        if (saveFileDialog.ShowDialog() == true)
        {
            string directoryPath = Path.GetDirectoryName(saveFileDialog.FileName);
            if (layer is Layer l)
            {
                ManualCodec.SaveImage(layer.ImageWr, saveFileDialog.FileName);
            }
            if (layer is ImageSequence imageSequence)
            {
                string baseFileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                ExportImageSequence(imageSequence, directoryPath, baseFileName);
            }
        }
    }


    private static void ExportImageSequence(ImageSequence imageSequence, string directoryPath, string baseFileName)
    {
        var keyframes = imageSequence._Animation.GetTimedVariable("Image").Keyframes;

        foreach (var keyframe in keyframes)
        {
            if (keyframe.Value is WriteableBitmap bitmap)
            {
                string fileName = $"{baseFileName}_{keyframe.Frame:D4}.png";
                string filePath = Path.Combine(directoryPath, fileName);

                ManualCodec.SaveImage(bitmap, filePath);
            }
            
        }
    }

    public static void File_ExportScript()
    {
        var script = ScriptingManager.Instance.SelectedScript;
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Y Code |*.y";
        saveFileDialog.Title = "Save Y Code";
        saveFileDialog.FileName = script.Name; // Sugerir un nombre base para los archivos

        if (saveFileDialog.ShowDialog() == true)
        {
            ScriptingManager.SaveScript(saveFileDialog.FileName);
        }
    }
    public static void File_ImportScript()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Define las propiedades del OpenFileDialog
            Filter = "Y files (*.y)|*.y",
            Title = "Select Y File"
        };

        // Muestra el diálogo OpenFileDialog
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                ScriptingManager.LoadScript(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de lectura de archivo o deserialización
                Output.Show("Error al cargar el archivo: " + ex.Message);
            }
        }
    }





    // MANUAL PRO
    public static void DuplicateWorkspace()
    {
        project.editorsSpace.DuplicateWorkspace();
    }
    public static void SaveWorkspace()
    {
        project.editorsSpace.SaveWorkspace();
    }
    public static void LoadWorkspaces()
    {
        project.editorsSpace.LoadWorkspaces();
    }
    internal static void LoadWorkspace()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            // Define las propiedades del OpenFileDialog
            Filter = "workspace (*.json)|*.json",
            Title = "Import Workspace"
        };

        // Muestra el diálogo OpenFileDialog
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                project.editorsSpace.LoadWorkspaceAndAdd(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                // Manejar excepciones, como errores de lectura de archivo o deserialización
                Output.Show("Error al cargar el archivo: " + ex.Message);
            }
        }
    }
    public static void SaveAllWorkspace()
    {
        project.editorsSpace.SaveAllWorkspaces();
    }
    public static void ReturnToDefaultWorkspaces()
    {
        project.editorsSpace.ReturnToDefaultWorkspace();
    }
    public static void DeleteWorkspace()
    {
        project.editorsSpace.DeleteWorkspace();
    }

    // --------------------------- EDIT --------------------------- \\
    public static void Edit_Preferences()
    {
        Preferences preferencesWindow = new Preferences();
        M_Window.Show(preferencesWindow, "Preferences");
    }

    // --------------------------- ANIMATION --------------------------- \\
 /// <summary>
 /// this can be used for debug images
 /// </summary>
 /// <param name="image"></param>
    public static void OpenRenderImage(WriteableBitmap image, string title = "")
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            bool enable = true;
            if (enable)
            {
                RenderView rv = new(image);
                rv.Title = title;
                rv.ShowDialog();
            }
        });
    }
    public static void OpenRenderImage(SKBitmap image, string title = "")
    {
        OpenRenderImage(image.ToWriteableBitmap(), title);
    }
    public static void OpenRenderImage(byte[] image, string title = "")
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            bool enable = true;
            if (enable)
            {
                RenderView rv = new(image);
                rv.Title = title;
                rv.ShowDialog();
            }
        });
    }

    public static void Animation_RenderImage()
    {
        RenderView rv = new();
        rv.ShowDialog();
    }
    public static void Animation_RenderAnimation()
    {
        //   ManualCodec.RenderAnimation();
        ManualAPI.ShowRenderAnimationWindow();
    }
    public static void Animation_InsertKeyframe()
    {
        ManualAPI.SelectedLayer.InsertKeyframe(nameof(ManualAPI.SelectedLayer.ImageWr));
    }
    public static void Animation_DeleteKeyframe()
    {
        ManualAPI.Animation.DeleteSelectedKeyframes();
    }

    // --------------------------- HELP --------------------------- \\
    public static void Help_ReleaseNotes()
    {
        string url = WebManager.Combine(Constants.WebURL, "release-notes");
        WebManager.OPEN(url);
    }
    public static void Help_Docs()
    {
        string url = WebManager.Combine(Constants.WebURL, "guide");
        WebManager.OPEN(url);
    }






    public static void SaveImage(byte[] img)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "PNG Image (*.png)|*.png"; // Filtra para mostrar solo archivos PNG
        saveFileDialog.DefaultExt = "png";                 // Extensión de archivo predeterminada
        saveFileDialog.AddExtension = true;                // Asegura que la extensión se añada si el usuario no la especifica
        saveFileDialog.FileName = "Output";               // Nombre de archivo predeterminado

        // Muestra el cuadro de diálogo de guardado
        if (saveFileDialog.ShowDialog() == true)
        {
            // Llama al método personalizado para guardar la imagen
            ManualCodec.SaveImage(img, saveFileDialog.FileName);
        }
    }
}