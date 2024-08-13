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
//using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using Manual.API;
using System.ComponentModel;

using System.Windows.Media.Animation;

using Microsoft.WindowsAPICodePack.Shell;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Configuration;
using System.Windows.Media.Imaging;
using Manual.Editors.Displays;
using System.Windows.Markup;
using Microsoft.Windows.Themes;
using ManualToolkit.Generic;
using Manual.MUI;
using Manual.Core.Nodes;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;
using static Python.Runtime.TypeSpec;
using System.Windows.Threading;
using CefSharp;
using FFMediaToolkit;
using Manual.Core.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

//using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;


/// -------------------- DICTIONARY -------------------- \\\
/// 
/// GLOBAL PUBLISHERS:
/// <see cref = "PluginsManager.OnUpdatePlugins" />
/// 
/// 
/// IMPORTANT STUFF
/// <see cref = "ToolManager" />
/// <see cref = "LoadSettings" />
/// 
/// 
/// 
/// REGISTER A NEW EDITOR
/// <see cref = "Editor.editors" />
/// 
/// 
///  ----------------------------------
///





namespace Manual.Core;

public static partial class AppModel
{

    public static bool DebugMode { get; set; } = false;
    public static bool InstantiateThings { get; set; } = true; // at startup
    public static bool UninstantiateThings { get; set; } = false; //at shutdown


    public static bool IsForceLogin { get; set; } = false;


    // public static Manual_Info manualInfo { get; set; } = new();
    public static Output output { get; set; }
    public static UserManager userManager { get; set; } = new();

    //menudo lio, pd: sí lol
    public static MainWindow mainW
    {
        get => Application.Current.MainWindow as MainWindow;
        set => Application.Current.MainWindow = value;
    }

    public static MainWindow? mainWOld { get; set; }
    public static Settings settings { get;
        set; }


    public static Manual.Editors.Displays.Launcher.LauncherModel launcher { get; set; } = new();


    // Load Plugins
    // public static DirectoryCatalog pluginCatalog = new($"{App.LocalPath}Plugins");

    public static CompositionContainer PluginsContainer;


    /// <summary>
    /// after load project, then load <see cref="LoadSettings"/>
    /// </summary>
    public static Project project { get; set; }
    

    //public static ManualAPI api = new();
    public static ToolSpaces toolSpaces { get; set; } //TODO: DEPRECATED toolSpaces


    /// <summary>
    /// no funca, es mejor crear un .bat y ejecutarlo
    /// </summary>
    public static void RestartApplication()
    {
        // Obtener la ruta del ejecutable de la aplicación actual
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        // Iniciar una nueva instancia de la aplicación
        Process.Start(exePath);
        // Cerrar la aplicación actual
        Application.Current.Shutdown();
    }

    public static void Initialize()
    {
        if (!Updater.isUpdateMode)
        {
            //----- Load FFMPEG
            string ffmpegPath = Path.GetFullPath($"{App.LocalPath}Resources/Presets/ffmpeg");

            if (FFmpegLoader.FFmpegPath != ffmpegPath) // needs this dlls in bin to work: https://github.com/BtbN/FFmpeg-Builds/releases
                FFmpegLoader.FFmpegPath = ffmpegPath;
            //-----



            // Inicializa las propiedades en el constructor estático

            output = new Output();
            
            mainW = null;
            mainWOld = null;

            settings ??= new Settings();
            PluginsContainer = new CompositionContainer();

            project = new Project();
            project.InstantiateThings(); //if needed     
            OnProjectLoaded();
          

            toolSpaces = new ToolSpaces();

        
        }
    }

    //ONE TIME
    static void OnProjectLoaded()
    {
        Project.IsLoading = false;
        Shot.defaultTemplate = ShotBuilder.instance.Templates[2]; //1024x1024

        EventManager.RegisterClassHandler(
             typeof(TextBox),
             UIElement.GotFocusEvent,
             new RoutedEventHandler(TextBoxGotFocus));

        EventManager.RegisterClassHandler(
             typeof(TextBox),
             UIElement.LostFocusEvent,
             new RoutedEventHandler(TextBoxLostFocus));


        EventManager.RegisterClassHandler(
         typeof(RichTextBox),
         UIElement.GotFocusEvent,
         new RoutedEventHandler(TextBoxGotFocus));

        EventManager.RegisterClassHandler(
             typeof(RichTextBox),
             UIElement.LostFocusEvent,
             new RoutedEventHandler(TextBoxLostFocus));

        PenPressure.Enable();

    }

    //------------- events -------------\\
    private static void TextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        Shortcuts.IsTextBoxFocus = true;
    }
    private static void TextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        Shortcuts.IsTextBoxFocus = false;
    }


    // ----------------------------------------------------------------------------- UTILS and HELPERS ------------------------------------------------------------------------------------- \\

    /// <summary>
    /// find the item that has the same property value, and return these item
    /// </summary>
    /// <typeparam name="T"> the selected item </typeparam>
    /// <param name="collection"> the collection </param>
    /// <param name="selectedItem"> the value </param>
    /// <param name="propertyName"> nameof(property) </param>
    /// <returns></returns>
    public static T LoadSelectedInCollection<T>(ObservableCollection<T> collection, T selectedItem, string propertyName) where T : class
    {
        // Find the object in the collection that has the same name as the selectedItem
        var matchingObject = collection.FirstOrDefault(x => x.GetType().GetProperty(propertyName)?.GetValue(x)?.ToString() == selectedItem.GetType().GetProperty(propertyName)?.GetValue(selectedItem)?.ToString());

        // If a matching object was found, replace the selectedItem with it
        if (matchingObject != null)
        {         
            selectedItem = matchingObject;
            return matchingObject;     
        }
        else
        {
            return null;
        }
      
    }

    public static T FindAncestor<T>(DependencyObject element) where T : class
    {
        DependencyObject parent = VisualTreeHelper.GetParent(element);

        while (parent != null)
        {
            if (parent is T ancestor)
            {
                return ancestor;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    public static T FindAncestor<T>(FrameworkElement element) where T : FrameworkElement
    {
        DependencyObject parent = VisualTreeHelper.GetParent(element);

        while (parent != null)
        {
            if (parent is T ancestor)
            {
                return ancestor;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    /// <summary>
    /// returns the datacontext
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    /// <returns></returns>
    public static T FindAncestorByDataContext<T>(FrameworkElement element) where T : class
    {
        DependencyObject parent = VisualTreeHelper.GetParent(element);

        while (parent != null)
        {
            if (parent is FrameworkElement frameworkElement && frameworkElement.DataContext is T dataContext)
            {
                return dataContext;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    public static FrameworkElement FindAncestorByName(string name, FrameworkElement element)
    {
        if (element.Name == name)
            return element;

        DependencyObject parent = VisualTreeHelper.GetParent(element);

        while (parent != null)
        {
            if (parent is FrameworkElement frameworkElement && frameworkElement.Name == name)
            {
                return frameworkElement;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
    public static FrameworkElement FindAncestorContainsInName(string nameContains, FrameworkElement element)
    {
        if (element.Name.Contains(nameContains))
            return element;
 
        DependencyObject parent = VisualTreeHelper.GetParent(element);

        while (parent != null)
        {
            if (parent is FrameworkElement frameworkElement && frameworkElement.Name.Contains(nameContains))
            {
                return frameworkElement;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }


    public static TEnum ParseEnum<TEnum>(string input) where TEnum : struct
    {
        // Verificar primero que TEnum realmente es un enum
        if (!typeof(TEnum).IsEnum)
        {
            throw new ArgumentException("TEnum debe ser un tipo enumerado.");
        }

        // Intenta parsear la entrada. Si falla, lanza una excepción
        if (Enum.TryParse(input, true, out TEnum result))
        {
            return result;
        }
        else
        {
            throw new ArgumentException($"Valor inválido para el tipo enum {typeof(TEnum).Name}: {input}");
        }
    }

    public static IEnumerable<string> GetEnumNames(Type enumType)
    {
        return Enum.GetNames(enumType);
    }

    public static T GetNextEnumValue<T>(T enumValue) where T : Enum
    {
        T[] enumValues = (T[])Enum.GetValues(typeof(T));
        int currentIndex = Array.IndexOf(enumValues, enumValue);
        int nextIndex = (currentIndex + 1) % enumValues.Length;
        return enumValues[nextIndex];
    }
    public static Color GetThemeColor(string name)
    {
        var colorBrush = (SolidColorBrush)Application.Current.Resources[name];
        return colorBrush.Color;
    }
    public static SolidColorBrush GetThemeColorBrush(string name)
    {
        return (SolidColorBrush)Application.Current.Resources[name];
    }

    public static T GetResource<T>(string name)
    {
        return (T)Application.Current.Resources[name];
    }

    public static object Instantiate(Type elementType)
    {
        return Activator.CreateInstance(elementType);
    }

    public static void Invoke(Action action)
    {
        if(Application.Current != null)
         Application.Current.Dispatcher.Invoke(action);
    }
    public static void Invoke(Action action, DispatcherPriority priority)
    {
        if (Application.Current != null)
            Application.Current.Dispatcher.Invoke(action, priority);
    }
    public static async Task InvokeAsync(Action action)
    {
        await Application.Current.Dispatcher.InvokeAsync(action);
    }

    public static async Task<TResult> InvokeAsync<TResult>(Func<TResult> function)
    {
        return await Application.Current.Dispatcher.InvokeAsync(function).Task;
    }

    public static void InvokeNext(Action action)
    {
        CompositionTarget.Rendering += OnRendering;
        void OnRendering(object sender, EventArgs e)
        {
            // Desconectar el evento para que solo se ejecute una vez
            CompositionTarget.Rendering -= OnRendering;

            // Código a ejecutar justo después de la renderización
            action();  // Suponiendo que HeightM es tu método
        }
    }

    public static void SubscribeOnce<TEventArgs>(object eventHolder, string eventName, Action<TEventArgs> action)
    {
        // Obtener la información del evento usando reflexión
        var eventInfo = eventHolder.GetType().GetEvent(eventName);
        if (eventInfo == null)
        {
            throw new ArgumentException("No event found with that name.", nameof(eventName));
        }

        Event<TEventArgs> handler = null; // Declaración fuera para referencia en sí misma
        handler = (args) =>
        {
            // Desuscribirse del evento para asegurar que la acción se ejecute solo una vez
            eventInfo.RemoveEventHandler(eventHolder, handler);

            // Ejecutar la acción pasada al método
            action(args);
        };

        // Suscribir el handler al evento
        eventInfo.AddEventHandler(eventHolder, handler);
    }

    public static void SubscribeOnce(object eventHolder, string eventName, Action action)
    {
        // Obtener la información del evento usando reflexión
        var eventInfo = eventHolder.GetType().GetEvent(eventName);
        if (eventInfo == null)
        {
            throw new ArgumentException("No event found with that name.", nameof(eventName));
        }

        Event handler = null; // Declaración fuera para referencia en sí misma
        handler = () =>
        {
            // Desuscribirse del evento para asegurar que la acción se ejecute solo una vez
            eventInfo.RemoveEventHandler(eventHolder, handler);

            // Ejecutar la acción pasada al método
            action();
        };

        // Suscribir el handler al evento
        eventInfo.AddEventHandler(eventHolder, handler);
    }




    public static Point GetAveragePosition<T>(IEnumerable<T> items, Func<T, Point> positionSelector)
    {
        if (items == null || !items.Any())
        {
            return new Point(0, 0);
        }

        double totalX = 0;
        double totalY = 0;
        int count = 0;

        foreach (var item in items)
        {
            var position = positionSelector(item);
            totalX += position.X;
            totalY += position.Y;
            count++;
        }

        return new Point(totalX / count, totalY / count);
    }
    public static Point GetAveragePosition(IEnumerable<IPositionable> items)
    {
        return GetAveragePosition(items, item => new Point(item.PositionGlobalX, item.PositionGlobalY));
    }






    public static void DisposePlugins()
    {
        PluginsContainer.Dispose();    
         PluginsContainer = new CompositionContainer();

        UpdatePlugins();

    }

    public static void UpdatePlugins()
    {
     //   string old = project.SelectedShot.CurrentTool; //TODO: esto es del viejo tool
      //  project.SelectedShot.CurrentTool = "";
      //  project.SelectedShot.CurrentTool = old;

        foreach (var space in toolSpaces.Spaces)
        {
            space.LoadSpace();
        }
    }


    public static Binding CloneBinding(DependencyObject target, DependencyProperty dp)
    {
        var bind = BindingOperations.GetBinding(target, dp);
        if (bind is null)
            return null;

        return new Binding(bind.Path.Path);
    }
    public static void CopyBinding(FrameworkElement target, FrameworkElement based, DependencyProperty dp)
    {
        var bind = CloneBinding(based, dp);
        if (bind != null)
        {
            target.SetBinding(dp, bind);
        }
        else
        {
            var value = based.GetValue(dp);
            target.SetValue(dp, value);
        }
    }
    public static Binding DeepCloneBinding(FrameworkElement target, DependencyProperty dp)
    {
        var bindingExpression = target.GetBindingExpression(dp);
        if (bindingExpression != null)
        {
            var oldBinding = bindingExpression.ParentBinding;

            var newBinding = new Binding
            {
                Path = oldBinding.Path,
                Source = oldBinding.Source,
                Mode = oldBinding.Mode,
                UpdateSourceTrigger = oldBinding.UpdateSourceTrigger,
                Converter = oldBinding.Converter,
                ConverterParameter = oldBinding.ConverterParameter,
                StringFormat = oldBinding.StringFormat
            };
            return newBinding;
        }
        else return null;
    }

    public static System.Drawing.Icon GetEmbeddedIcon(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                return new System.Drawing.Icon(stream);
            }
            else
            {
                throw new InvalidOperationException("Resource not found.");
            }
        }
    }
    public static BitmapImage LoadIconFromToolkit(string resourcePath)
    {
        var uri = new Uri($"pack://application:,,,/ManualToolkit;component/Assets/Icons/{resourcePath}");
        var bitmap = new BitmapImage(uri);
        return bitmap;
    }



    public static bool IsElementActuallyVisible(FrameworkElement element)
    {
        if (element == null)
        {
            return false;
        }

        if (element.Visibility != Visibility.Visible)
        {
            return false;
        }

        DependencyObject parent = VisualTreeHelper.GetParent(element);

        // Si el elemento no tiene padre, es un elemento raíz, así que su visibilidad determina su visibilidad efectiva
        if (parent == null)
        {
            return true;
        }

        // Si el elemento tiene padre, verifica la visibilidad del padre recursivamente
        if (parent is FrameworkElement parentElement)
        {
            return IsElementActuallyVisible(parentElement);
        }

        // En caso de que el padre no sea un FrameworkElement (poco común), consideramos que el elemento es visible efectivamente
        // Esto puede necesitar ajustes basado en tu aplicación específica y estructura de UI
        return true;
    }


    public static void SetLowQuality(FrameworkElement target, bool cache = false)
    {
        if (target == null)
            return;

        RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(target, EdgeMode.Aliased);
        RenderOptions.SetClearTypeHint(target, ClearTypeHint.Auto);

        if (cache)
            target.CacheMode ??= new BitmapCache();
    }
    public static void SetNormalQuality(FrameworkElement target, bool cache = false)
    {
        if (target == null)
            return;

        RenderOptions.SetBitmapScalingMode(target, BitmapScalingMode.HighQuality);
        RenderOptions.SetEdgeMode(target, EdgeMode.Unspecified);
        RenderOptions.SetClearTypeHint(target, ClearTypeHint.Enabled);

        if (cache)
            target.CacheMode = null;
    }

    public static string TimeFormat(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            // Formato para horas, minutos y segundos
            return string.Format("{0}:{1:D2}:{2:D2}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds) + " hours";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            // Formato para minutos y segundos
            return string.Format("{0}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds) + " minutes";
        }
        else
        {
            // Solo segundos
            return timeSpan.Seconds.ToString() + " seconds";
        }
    }

    /// <summary>
    /// Get MenuItem header
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    public static string GetHeader(object sender)
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;
        return header;
    }

    /// <summary>
    /// on debug
    /// </summary>
    /// <param name="uiElement"></param>
    public static void ForceRedraw(this UIElement uiElement)
    {
        uiElement.InvalidateVisual();
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait();
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => { })).Wait();
    }

    internal static bool IsDispatcherSafe()
    {
        return Application.Current.Dispatcher.CheckAccess();
    }
}






// ------------------- MESSAGING ------------------- \\
public class NotifyMessage : ValueChangedMessage<object>
{
    public NotifyMessage(object value) : base(value)
    {
    }
}



public static class Namer
{
    public static string SetName(INamable newItem, IEnumerable<INamable> collection)
    {
        return SetName(newItem.Name, collection);
    }

    /// <summary>
    /// ensures no repeated names, you need to name it BEFORE enter to the collection
    /// </summary>
    /// <param name="nameBase"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static string SetName(string nameBase, IEnumerable<INamable> collection)
    {
        // Regex para detectar si el nombre ya tiene un número al final, y cómo está separado (espacio o guion bajo)
        var regex = new Regex(@"(?<name>.+?)([\s_])(?<number>\d+)$");
        var match = regex.Match(nameBase);

        string newName = nameBase;
        string separator; // Se define sin valor inicial aquí

        if (match.Success)
        {
            // Si hay coincidencia, se asigna el separador basado en la captura
            separator = match.Groups[1].Value; // Correctamente captura el espacio o "_"
            // Extraer la parte del nombre y el número
            newName = match.Groups["name"].Value;
            int number = int.Parse(match.Groups["number"].Value);

            // Incrementar el número hasta encontrar un nombre único
            do
            {
                number++;
                newName = $"{match.Groups["name"].Value}{separator}{number}";
            } while (collection.Any(item => item.Name == newName));
        }
        else
        {
            // Si no hay número al final, se verifica la unicidad del nombre base y se añade un número si es necesario
            separator = " "; // Por defecto, usar espacio si no hay número
            if (collection.Any(item => item.Name == newName))
            {
                int number = 1;
                while (collection.Any(item => item.Name == $"{newName}{separator}{number}"))
                {
                    number++;
                }
                newName = $"{newName}{separator}{number}";
            }
        }

        return newName;
    }

    public static void SetName(string nameBase, IEnumerable<INamable> collection, INamable item) 
    {
        item.Name = SetName(nameBase, collection);
    }

    internal static int RandomId<T>(IEnumerable<T> list, Func<T, int, bool> predicate, int startFrom = 0)
    {
        int newId = startFrom; // Comienza desde 0
        while (list.Any(item => predicate(item, newId)))
        {
            newId++; // Incrementa si el ID ya está en uso
        }

        return newId;
    }
 

}

public interface INamable
{
    public string Name { get; set; }
}
public interface IId
{
    public Guid Id { get; set; }
}





public class AnimateUI
{
    UIElement Element { get; set; }
    DependencyProperty dp { get; set; } = UIElement.OpacityProperty;
    double FocusValue { get; set; } = 0.2;
    double UnFocusValue { get; set; } = 0;
    TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.2);

    UIElement ElementParent { get; set; }
    public AnimateUI(UIElement element, DependencyProperty propertyToAnim, double focusValue, double unFocusValue,double duration)
    {
        Element = element;
        dp = propertyToAnim;

        Element.MouseEnter += Element_MouseEnter;
        Element.MouseLeave += Element_MouseLeave;

        SetValues(focusValue, unFocusValue);
        Duration = TimeSpan.FromSeconds(duration);

    }
    public AnimateUI(UIElement element, DependencyProperty propertyToAnim, double focusValue, double unFocusValue, double duration, UIElement subscribeTo)
    {
        Element = element;
        ElementParent = subscribeTo;
        dp = propertyToAnim;

        ElementParent.MouseEnter += Element_MouseEnter;
        ElementParent.MouseLeave += Element_MouseLeave;

        SetValues(focusValue, unFocusValue);
        Duration = TimeSpan.FromSeconds(duration);

    }
    public AnimateUI(UIElement element,UIElement subscribeTo)
    {
        Element = element;
        ElementParent = subscribeTo;
     
        ElementParent.MouseEnter += Element_MouseEnter;
        ElementParent.MouseLeave += Element_MouseLeave;
    }
    public AnimateUI(UIElement element, DependencyProperty propertyToAnim, double focusValue, double unFocusValue, UIElement subscribeTo)
    {
        Element = element;
        ElementParent = subscribeTo;
        dp = propertyToAnim;

        ElementParent.MouseEnter += Element_MouseEnter;
        ElementParent.MouseLeave += Element_MouseLeave;

        SetValues(focusValue, unFocusValue);

    }
    public AnimateUI(UIElement element, double focusValue, double unFocusValue, UIElement subscribeTo)
    {
        Element = element;
        ElementParent = subscribeTo;

        ElementParent.MouseEnter += Element_MouseEnter;
        ElementParent.MouseLeave += Element_MouseLeave;

        SetValues(focusValue, unFocusValue);

    }

    // Destructor
    ~AnimateUI()
    {
        if (ElementParent == null)
        {
            Element.MouseEnter -= Element_MouseEnter;
            Element.MouseLeave -= Element_MouseLeave;
        }
        else
        {
            ElementParent.MouseEnter -= Element_MouseEnter;
            ElementParent.MouseLeave -= Element_MouseLeave;
        }
    }


    public void SetValues(double focusValue, double unFocusValue)
    {
        FocusValue = focusValue;
        UnFocusValue = unFocusValue;
    }


    private void Element_MouseEnter(object sender, MouseEventArgs e)
    {
        if (ManualAPI.settings.EnableAnimations)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = UnFocusValue,
                To = FocusValue,
                Duration = this.Duration
            };

            Element.BeginAnimation(dp, animation);
        }
        else
        {
            Element.SetValue(dp, FocusValue);
        }

    }

    private void Element_MouseLeave(object sender, MouseEventArgs e)
    {
        if (ManualAPI.settings.EnableAnimations)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = FocusValue,
                To = UnFocusValue,
                Duration = this.Duration
            };

            Element.BeginAnimation(dp, animation);
        }
        else
        {
            Element.SetValue(dp, UnFocusValue);   
        }

    }




}

public static class FpsMonitor
{
    private static readonly Stopwatch stopwatch = new Stopwatch();
    private static int frameCount = 0;

  
    private static void OnRendering(object sender, EventArgs e)
    {
        frameCount++;
        if (stopwatch.Elapsed.TotalSeconds >= 1)
        {
            var message = $"fps: {Math.Round(frameCount / stopwatch.Elapsed.TotalSeconds)}";

            AppModel.settings.FPS = message;

            // Reiniciar el contador y el cronómetro cada segundo.
            frameCount = 0;
            stopwatch.Restart();
        }
    }

    public static void Start()
    {
        CompositionTarget.Rendering += OnRendering;
        stopwatch.Start();
    }
    public static void Stop()
    {
        CompositionTarget.Rendering -= OnRendering;
        stopwatch.Stop();
    }
}


//------------------------------------------------------------------------------------------------------------- MANUAL CLIPBOARD


public static class ManualClipboard
{
    private const string CustomFormat = "ManualFormat";
    private static Type realCopiedType;
    public static void Copy<T>(T objectToCopy)
    {
        realCopiedType = objectToCopy.GetType();
        var dataObject = new DataObject();


        // También puedes agregar formatos estándar si es necesario
        if (objectToCopy is string text)
        {
            dataObject.SetData(DataFormats.Text, text);
        }
        else if (objectToCopy is Image image)
        {
            dataObject.SetData(DataFormats.Bitmap, image);
        }

        else if (objectToCopy is SKBitmap skBitmap)
        {
            dataObject.SetData(DataFormats.Bitmap, skBitmap.ToWriteableBitmap().ToBitmap());
        }
        else if (objectToCopy is WriteableBitmap bitmap)
        {
            dataObject.SetData(DataFormats.Bitmap, bitmap.ToBitmap());
        }

        // Para objetos serializables, usar un formato personalizado
        else if (objectToCopy != null)
        {
            string json = JsonConvert.SerializeObject(objectToCopy);
            dataObject.SetData(CustomFormat, json);
        }


        //else if (objectToCopy is LayerBase layer)
        //{
        //    MemoryStream pngStream = new MemoryStream();
        //    BitmapEncoder encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(layer.Image));
        //    encoder.Save(pngStream);
        //    pngStream.Seek(0, SeekOrigin.Begin);

        //    dataObject.SetData("PNG", pngStream);
        //}



        Clipboard.SetDataObject(dataObject, true);
    }

    public static T Paste<T>() where T : class
    {
        var dataObject = Clipboard.GetDataObject();
        if (dataObject != null && dataObject.GetDataPresent(CustomFormat))
        {
            string json = dataObject.GetData(CustomFormat) as string;
            return JsonConvert.DeserializeObject<T>(json);
        }
        return null;
    }


    public static object Paste()
    {
        if(realCopiedType != null)
        {
            // Obtener el método específico 'Paste<T>()' sin parámetros
            MethodInfo pasteMethod = typeof(ManualClipboard).GetMethods()
                                                            .Where(m => m.Name == "Paste" &&
                                                                        m.IsGenericMethod &&
                                                                        m.GetParameters().Length == 0)
                                                            .FirstOrDefault()
                                                            ?.MakeGenericMethod(realCopiedType);
            if (pasteMethod == null)
            {
                Output.Log("Can't paste due to a reflection error", "ManualClipboard");
                return null;
            }


            return pasteMethod.Invoke(null, null);
        }
        else
        {
            return GetImage();
        }
    }

    public static WriteableBitmap GetImage()
    {
        return new WriteableBitmap(Clipboard.GetImage()).SetAlpha();
    }


    public static T Clone<T>(T original) where T : class
    {
        if (original == null)
            return null;

        // Serializar el objeto original a JSON
        string json = JsonConvert.SerializeObject(original);

        // Deserializar el JSON para obtener una copia del mismo tipo
        T clone = JsonConvert.DeserializeObject<T>(json);

        return clone;
    }
    public static object Clone(object original)
    {
        if (original == null)
            return null;

        // Serializar el objeto original a JSON
        string json = JsonConvert.SerializeObject(original);

        // Deserializar el JSON para obtener una copia
        object clone = JsonConvert.DeserializeObject(json, original.GetType());

        return clone;
    }
    public static string Serialize(object original)
    {
        if (original == null)
            return null;

        // Serializar el objeto original a JSON
        string json = JsonConvert.SerializeObject(original);
        return json;
    }

}





public static class CefWebManager
{
    public static void Initialize()
    {
        if (Cef.IsInitialized)
            return;



        var settings = new CefSharp.Wpf.CefSettings()
        {
            // Aquí puedes configurar opciones específicas de CEF
            // Por ejemplo, para establecer un directorio de caché personalizado:
            CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
            WindowlessRenderingEnabled = true,
        };
        //settings.CefCommandLineArgs.Add("enable-gpu-rasterization", "1");
        //settings.CefCommandLineArgs.Add("enable-zero-copy", "1");
        //settings.CefCommandLineArgs.Add("disable-gpu-vsync", "0");


        settings.CefCommandLineArgs.Add("force-device-scale-factor", "1");
        //GPU
        settings.CefCommandLineArgs.Add("disable-gpu", "1"); // Desactiva la GPU
        settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1"); // Desactiva la composición de GPU
        settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1"); // Habilita la programación de frames


        // Inicializa CEF con las configuraciones especificadas.
        // Es importante que esta llamada se realice antes de crear cualquier ventana de tu aplicación que vaya a utilizar CEF. 
        Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
    }


    public static void Shutdown()
    {
        RendGL.Shutdown();
        Cef.Shutdown();
    }
}
