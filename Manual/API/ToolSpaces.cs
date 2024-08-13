using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Serialization;

namespace Manual.API;
public class ToolEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return AppModel.toolSpaces.currentToolSpace == parameter;
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class ToolSpaces : ObservableObject //TODO: DEPRECATED ToolSpaces
{
    public ObservableCollection<ToolSpaceContainer> Spaces { get; set; } = new();

    [ObservableProperty] public ToolSpaceContainer currentToolSpace;
    public ToolSpaces()
    {
        // Obtiene la ruta de la carpeta que contiene los scripts.
        string folderPath = $"{App.LocalPath}Resources/Scripts";

        // Obtiene la lista de carpetas en el directorio de scripts.
        string[] scriptFolders = Directory.GetDirectories(folderPath);

        // Itera sobre cada carpeta y crea un ToolSpaceContainer con su nombre y el icono que contiene.
        foreach (string scriptFolder in scriptFolders)
        {
            // Obtiene el nombre de la carpeta.
            string spaceName = Path.GetFileName(scriptFolder);

            // Obtiene el icono de la carpeta como un arreglo de bytes.
            string iconPath = Path.Combine(scriptFolder, "icon.png");


            byte[] iconBytes = null;
            if(File.Exists(iconPath))
                iconBytes = File.ReadAllBytes(iconPath);

            // Crea el ToolSpaceContainer y lo agrega a la colección Spaces.
            string[] dllFiles = Directory.GetFiles(scriptFolder, "*.dll");

            if (dllFiles.Length > 0)
            {
                string dllPath = dllFiles[0];
                ToolSpaceContainer space = new ToolSpaceContainer(spaceName, iconBytes, dllPath);
                Spaces.Add(space);
            }
            

        }
        if(Spaces.Any())
          CurrentToolSpace = Spaces.First();
    }



    [RelayCommand]
    void SelectToolSpace(object obj)
    {
        CurrentToolSpace = obj as ToolSpaceContainer;       
    }
}

public class ToolSpaceContainer //TODO: DEPRECATED ToolSpaceContainer
{
    public string Name { get; set; }
    public byte[] Icon { get; set; }
    public string DllPath { get; set; }

    public ToolSpaceContainer(string name, byte[] icon, string dllPath)
    {
        Name = name;
        Icon = icon;
        DllPath = dllPath;

        LoadSpace();
    }
    CompositionContainer PluginsContainer = new();
    [XmlIgnore] public StackPanel stackPanelContainer { get; set; } = new();

    public void LoadSpace()
    {
        stackPanelContainer.Children.Clear();

        ToolSpaceContainer c = this;
       
        var container = PluginsContainer;

      
        if (!File.Exists(c.DllPath))
        {
            return;
        }

        byte[] bytes = File.ReadAllBytes(c.DllPath);
        var assembly = Assembly.Load(bytes);

        var catalog = new AssemblyCatalog(assembly);
        container = new CompositionContainer(catalog);


        try
        {
        container.ComposeParts(this);

        var plugins = container.GetExportedValues<IToolSpace>();

      
            foreach (var plugin in plugins)
            {
                try
               {
                    StackPanel sp = new();
                    plugin.Initialize(sp);

                    stackPanelContainer.Children.Add(sp);
                }
                catch
                {
                    Debug.WriteLine("me vale verga sigo igual");
                   // MessageBox.Show(plugin.ToString());
                }
            }
        }
        catch (Exception ex)
       {
            Output.Log(ex.Message, "Something went wrong with the extension " + c.Name);
        }
    }
}

public interface IToolSpace //TODO: DEPRECATED IToolSpace
{
    void Initialize(StackPanel s);
}

[ObservableObject]
public partial class StackPanelContainer : StackPanel //TODO: DEPRECATED? StackPanelContainer
{
    public StackPanelContainer()
    {
        Button btn = new Button();
            btn.Content = "stackpanel content";
        Children.Add(btn);
    }
}

