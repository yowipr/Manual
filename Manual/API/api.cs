using Manual.Core;
using Manual.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json.Linq;
using Manual.MUI;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;
using System.Threading;
using System.Data.Common;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Windows.Documents;
using System.Security.Policy;
using System.Net;
using Manual.Objects.UI;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Manual.Editors.Displays;
using System.Windows.Media.Animation;
using System.Collections.Specialized;
using System.Windows.Shell;
using Manual.Core.Nodes;
using System.CodeDom;
using System.Security.Principal;
using ManualToolkit.Themes;
using ICSharpCode.AvalonEdit.Snippets;
using CefSharp.DevTools.CSS;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace Manual.API;

public static partial class ManualAPI
{

    /// <summary>
    /// only in the same shot
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="replace"></param>
    public static void ReplaceLayer(LayerBase layer, LayerBase replace)
    {

        int index = layer.ShotParent.layers.IndexOf(layer);
        if (index != -1)  // Asegurarse de que el objeto existe en la colección
        {
            // Paso 2: Reemplazar el objeto en ese índice
            layer.ShotParent.layers[index] = replace;
            replace.ShotParent = layer.ShotParent;
        }
        else
        {
            // Manejar el caso donde el objeto no se encuentra en la colección
            // Por ejemplo, podrías querer agregarlo si no está presente
            layer.ShotParent.layers.Add(replace);
        }
    }

    public static PromptPreset FindPreset(string name)
    {
        return GenerationManager.Instance.PromptPresets.FirstOrDefault(p => p.Name == name);
    }

    public static LayerBase? FindLayer(Guid id)
    {
        return layers.FirstOrDefault(layer => layer.Id == id);
    }
    public static LayerBase? FindLayer(string name)
    {
        return layers.FirstOrDefault( layer => layer.Name == name);
    }
    public static void SelectOneLayer(LayerBase layer)
    {
        SelectedLayers.SelectSingleItem(layer);
        SelectedLayer = layer;
    }

    public static void AddLayerBase(LayerBase layerBase, Shot shot, bool isSelected = true, bool notifyAction = true) // main
    {
        if (layerBase is null || shot.layers.Contains(layerBase)) return;

        if(layerBase is ShotLayer ls && ls.ShotRef == shot)
        {
            Output.Show("ShotLayer self reference loop, you can't add the Shot in itself", "ShotLayer");
            return;
        }

        var oldS = shot.SelectedLayer;

        int index = shot.layers.IndexOf(shot.SelectedLayer);

        if (index >= shot.layers.Count)
        {
            shot.layers.Insert(index + 1, layerBase);
        }
        else if (shot.layers.Count == 0)
        {
            shot.layers.Add(layerBase);
        }
        else
        {
            index = index != -1 ? index : 0;
            shot.layers.Insert(index, layerBase);
        }



        if (isSelected)
            shot.SelectedLayer = layerBase;

        layerBase._Animation.StartOffset = shot.Animation.CurrentFrame;
        layerBase._Animation.CalculateSafeTrackIndex();
        shot.Animation.RemoveTrackBuffer(layerBase);

        layerBase._Animation.SetValue(CurrentFrame);

        //NOTIFY
        if (notifyAction)
            ActionHistory.CollectionActionSelected(shot.layers, layerBase, isAdd: true,
                shot, nameof(SelectedLayer), oldS, $"Add {layerBase}");

    }
    public static void AddLayerBase(LayerBase layerBase)
    {
        AddLayerBase(layerBase, SelectedShot, true);
    }
    public static void AddLayerBase(LayerBase layerBase, bool isSelected, bool notifyAction = true)
    {
           AddLayerBase(layerBase, SelectedShot, isSelected, notifyAction);
    }

    public static void Add_GhostLayer(GhostLayer layer)
    {
        AddLayerBase(layer, false, false);
    }

    public static void RemoveLayers(IEnumerable<LayerBase> layers)
    {
        var layersOld = layers.ToList();
        foreach (var layer in layersOld)
        {
            SelectedShot.RemoveLayer(layer);
        }
    }

    public static void Remove_GhostLayer(GhostLayer layer)
    {
        if (layer != null)
        {
            layer.ShotParent?.RemoveLayer(layer, false);
        }
    }

    public static void Add_UI_Object(UI_Object obj)
    {
        SelectedShot.Add_UI_Object(obj);
    }
    public static void Remove_UI_Object(UI_Object obj)
    {
        SelectedShot.Remove_UI_Object(obj);
    }

    public static Settings settings
    {
        get { return AppModel.settings; }
        set { AppModel.settings = value; }
    }
    public static RenderManager Render_Manager
    {
        get { return project.renderManager; }
        set { project.renderManager = value; }
    }

   
    // -------  REGION SPACE ------- \\
    public static RegionSpace space
    {
        get { return AppModel.project.regionSpace; }
    }

    public static void add(FrameworkElement element, Panel space)
    {
        element.Margin = new Thickness(0, 3, 0, 3);
        space.Children.Add(element);
    }
    public static Camera2D MainCamera
    {
        get { return SelectedShot.MainCamera; }
    }

    public static Point MousePosition
    {
        get { return Shortcuts.MousePosition; }
    }
    public static PixelPoint MousePixelPosition
    {
        get { return Shortcuts.MousePixelPosition; }
    }

    public static System.Drawing.PointF MousePositionF
    {
        get { return Shortcuts.MousePositionF; }
    }

    // --------------------------------------------------------------------------  BIND  -------------------------------------- \\
    public static void SetBind(FrameworkElement control, string propertyName)
    {
        var binding = new Binding(propertyName);
   
        switch (control)
        {
            case M_TextBox textBox:
                textBox.SetBinding(TextBox.TextProperty, binding);
                break;

            case Label label:
                label.SetBinding(Label.ContentProperty, binding);
                break;

            case M_NumberBox textBox:
                /* Binding bindingn = new Binding(propertyName)
                 {
                     UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                 };
                 textBox.SetBinding(M_NumberBox.ValueProperty, bindingn);*/
                textBox.InitializeBind(new Binding(propertyName));
                break;

            case M_SliderBox sliderBox:
              /*  Binding bindingon = new Binding(propertyName)
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };*/
                sliderBox.InitializeBind(new Binding(propertyName));
                break;

            case M_ImageBox im:
                Binding bindingno = new Binding(propertyName)
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                im.SetBinding(M_ImageBox.SourceProperty, bindingno);
                break;

            case M_ComboBox textBox:
               // textBox.SetBinding(M_ComboBox.SelectedItemProperty, binding);
                break;

            case ItemsControl textBox:
                textBox.SetBinding(ItemsControl.ItemsSourceProperty, binding);
                break;

            case CheckBox textBox:
                textBox.SetBinding(CheckBox.IsCheckedProperty, binding);
                break;


        }
    }
    public static void SetBind(Panel control, string dataContext)
    {
        var binding = new Binding(dataContext);     
        control.SetBinding(Panel.DataContextProperty, binding);      
    }

    public static Brusher SelectedBrush
    {
        get { return AppModel.project.SelectedBrush; }
        set { AppModel.project.SelectedBrush = value; }
    }
    public static Tool SelectedTool
    {
        get { return AppModel.project.toolManager.CurrentToolSpace; }
        set { AppModel.project.toolManager.CurrentToolSpace = value; }
    }

    public static Project project
    {
        get { return AppModel.project; }
        set { AppModel.project = value; }
    }
 
    public static dynamic prop
    {
        get { return AppModel.project.props; }
      //  set { AppModel.Properties = value; }
    }


   
    public static MvvmHelpers.ObservableRangeCollection<LayerBase> layers
    {
        get { return AppModel.project.SelectedShot.layers; }
        set { AppModel.project.SelectedShot.layers = value; }
    }
    public static LayerBase SelectedLayer
    {
        get   {  return AppModel.project.SelectedShot.SelectedLayer;  }
        set   {  AppModel.project.SelectedShot.SelectedLayer = value;  }
    }
    public static SelectionCollection<LayerBase> SelectedLayers
    {
        get { return AppModel.project.SelectedShot.SelectedLayers; }
        set { AppModel.project.SelectedShot.SelectedLayers = value; }
    }
    public static Shot SelectedShot
    {
        get { return AppModel.project.SelectedShot; }
        set { AppModel.project.SelectedShot = value; }
    }

    public static PromptPreset SelectedPreset
    {
        get { return GenerationManager.Instance.SelectedPreset; }
        set { GenerationManager.Instance.SelectedPreset = value; }
    }
    public static Keyframe SelectedKeyframe
    {
        get { return Animation.SelectedKeyframe; }
        set { Animation.SelectedKeyframe = value; }
    }
    public static SelectionCollection<Keyframe> SelectedKeyframes
    {
        get { return Animation.SelectedKeyframes; }
        set { Animation.SelectedKeyframes = value; }
    }

    public static void RenderContent(Panel stackPanel, UIElement container)
    {
        stackPanel.Children.Add(container);
        
    }
    /// <summary>
    /// creates a M_Keyframe at the right and bind it automatically
    /// </summary>
    public static M_Grid BindKeyframe(FrameworkElement element, bool setParent = false) //-------------------- KEYFRAME
    {
    
        int row = M_Grid.GetRow(element);
        int column = M_Grid.GetColumn(element);


        Panel p = element.Parent as Panel;
        //ItemsControl ic = element.Parent as ItemsControl;

        if (p != null)
            p.Children.Remove(element);

        //if (ic != null)
        //    ic.Items.Remove(element);

        if(element is M_SliderBox slider)
        {
            if(p != null)
            {
               // MessageBox.Show(p.ToString());
            }
            else
            {
                return null;
            }
        }
        M_Keyframe keyframe = new(element); // bind key to element

        M_Grid grid = new();
        grid.Column(element);
        grid.Column(keyframe, 20, GridUnitType.Pixel);
        
        M_Grid.SetRow(grid, row);
        M_Grid.SetColumn(grid, column);

        if (p != null && setParent)
            p.Children.Add(grid);

        //if (ic != null && setParent)
        //    ic.Items.Add(grid);

        return grid;
    }




    //------------------------------------ IMAGE GENERATION ---------------------------------\\


    //just in case
    public static void GenerateImage()
    {
        GenerationManager.Generate();
    }


    public static async Task<dynamic> POST(string endpoint, object Default = null)
    {
        Uri url = UrlRequest(endpoint);

        using (HttpClient client = new HttpClient())
        {
            var response = await client.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                return result;
            }
            else
            {
                // throw new Exception($"Failed to make POST request. Status code: {response.StatusCode}");
                if (Default == null)
                    Default = "Offline";

                return Default;
            }
        }
    }
    public static Uri UrlRequest(string requestUrl)
    {
        string baseUrl = settings.CurrentURL.TrimEnd('/');
        string RequestUrl = requestUrl.TrimStart('/');

        Uri url = new Uri(new Uri(baseUrl), RequestUrl);
        return url;
    }
    public static async Task<dynamic> GET(string endpoint, object Default = null)
    {    
        Uri url =  UrlRequest(endpoint);

        using (HttpClient client = new HttpClient())
        {

           var  response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                return result;
            }
            else
            {
                //  throw new Exception($"Failed to make GET request. Status code: {response.StatusCode}");
                if (Default == null)
                    Default = "Offline";

                return Default;
            }
        }
    }

    public static void DEBUG_TEST(object value)
    {
        Output.Log(value.ToString());
    }

    public static async Task<T> GET_PROP<T>(string endpoint, string propName)
    {
        dynamic responseObj = await GET(endpoint);
        string propJson = responseObj[propName].ToString();
        T propValue = JsonConvert.DeserializeObject<T>(propJson);
        return propValue;
    }

    public static async void GET_SetItems(M_ComboBox comboBox, string url, string listName, string? defaultItem = null)
    {
        try
        {
            List<string> modelListx = await GET_PROP<List<string>>(url, listName);
          //  comboBox.ItemsSource = modelListx;
          //  comboBox.SelectedItem = modelListx[0];
        }
        catch(Exception ex)
        {
            //  Output.Log(ex.Message,"error in GET_setitems");
           // comboBox.SelectedItem = "Offline";
        }
    }


    //---------------------------- GENERAL EXTENSIONS --------------------------\\
    public static int ToInt32(this double d)
    {
        return Convert.ToInt32(d);
    }
    public static int ToInt32(this float d)
    {
        return Convert.ToInt32(d);
    }


    public static Layer ToLayer(this CanvasObject canvasObject, Color color)
    {
        Layer l = new();
        l.Position = canvasObject.Position;
        l.Scale = canvasObject.Scale;
        l.AnchorPoint = canvasObject.AnchorPoint;
        l.ImageWr = Renderizainador.SolidColor(color, l.ImageWidth, l.ImageHeight);
        return l;
    }
    public static Layer ToLayer(this CanvasObject canvasObject)
    {  
        return ToLayer(canvasObject, Colors.Transparent);
    }

    public static Point WithMin(this Point point, double minX, double minY)
    {
        double x = Math.Max(point.X, minX);
        double y = Math.Max(point.Y, minY);
        return new Point(x, y);
    }

    /// <summary>
    /// remove new lines "\n"
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ToSingleLine(this string text)
    {
        string cleanedMessage = Regex.Replace(text, @"\t|\n|\r", " ");
        return cleanedMessage;
    }

    public static bool Is(this string source, params string[] list)
    {
        if (null == source) return false;
        return list.Contains(source, StringComparer.OrdinalIgnoreCase);
    }
    public static bool Is<T>(this T source, params T[] list)
    {
        if (null == source) return false;
        return list.Contains(source);
    }

    //--------------------------------------------------------ANIMATION-----------\\
    public static AnimationManager Animation
    {
        get { return SelectedShot.Animation; }
        set { SelectedShot.Animation = value; }
    }

    public static int CurrentFrame
    {
        get { return Animation.CurrentFrame; }
        set { Animation.CurrentFrame = value; }
    }

   
    public static void ShowRenderAnimationWindow()
    {
        Animation.Pause();
       // AppModel.mainW.IsEnabled = false; //TODO: se ve blanco y cutre
        M_Window.NewShow(new W_Render(), "Render Animation", M_Window.TabButtonsType.X);
    }
    public static RenderSettings SelectedRenderSettings
    {
        get { return Render_Manager.SelectedRenderSettings; }
        set { Render_Manager.SelectedRenderSettings = value; }
    }

}


//--------------- OBJECTS ----------------\\
#region OBJECTS
public class M_TextBox : TextBox, IManualElement
{
    public void InitializeBind(Binding bind)
    {
        SetBinding(TextProperty, bind);
    }

    public IManualElement Clone()
    {
        var clone = new M_TextBox();
        return clone;
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
nameof(Header),
typeof(string),
typeof(M_TextBox),
new PropertyMetadata(""));
    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }


    public M_TextBox() 
    {

    }
    string PropertyBinding { get; set; }
    public M_TextBox(string name, string description, string propertyBinding)
    {
        SetBinding(TextBox.TextProperty, propertyBinding);
       // string ancestor = "";
       // var binding = BindingOperations.GetBinding(this, TextBox.TextProperty);

        ToolTip =  name + "\n" + description + $"\nBind: {propertyBinding}";
        PropertyBinding = propertyBinding;

        KeyDown += M_TextBox_KeyDown;
        LostFocus += M_TextBox_LostFocus;
    }


    public M_TextBox(string name, string propertyBinding)
    {
        SetBinding(TextBox.TextProperty, propertyBinding);

        ToolTip = name + "\n" + $"\nBind: {propertyBinding}";
        PropertyBinding = propertyBinding;

        KeyDown += M_TextBox_KeyDown;
        LostFocus += M_TextBox_LostFocus;
    }
    public M_TextBox(string propertyBinding)
    {
        SetBinding(TextBox.TextProperty, propertyBinding);

        ToolTip = $"\nBind: {propertyBinding}";
        PropertyBinding = propertyBinding;

        KeyDown += M_TextBox_KeyDown;
        LostFocus += M_TextBox_LostFocus;
    }

    private void M_TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Perder el foco para activar la actualización de la propiedad
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
            Shot.UpdateCurrentRender();

        }
    }
    private void M_TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Shot.UpdateCurrentRender();
    }


}



public class M_Grid : Grid, IManualElement
{
    public void InitializeBind(Binding bind)
    {
        SetBinding(DataContextProperty, bind);
    }

    public IManualElement Clone()
    {
        return new M_Grid();
    }

    public M_Grid() {  }
    public void SetBackground(Color color)
    {
        Background = new SolidColorBrush(color);
    }
    /// <summary>
    /// Add a simple new blank Row with Height 1 Star
    /// </summary>
    public void Row()
    {
        RowDefinition row = new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) };
        RowDefinitions.Add(row);
    }
    /// <summary>
    /// Add a simple new blank Column with Width 1 Star
    /// </summary>
    public void Column()
    {
        ColumnDefinition column = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        ColumnDefinitions.Add(column);
    }

    public void Row(UIElement element, double height = 1, GridUnitType unit = GridUnitType.Star)
    {
        RowDefinition row = new RowDefinition()  {   Height = new GridLength(height, unit) };
        RowDefinitions.Add(row);

        Children.Add(element);
        M_Grid.SetRow(element, RowDefinitions.Count - 1);
    }
    public void Column(UIElement element, double width = 1, GridUnitType unit = GridUnitType.Star)
    {
        ColumnDefinition column = new ColumnDefinition() { Width = new GridLength(width, unit) };
        ColumnDefinitions.Add(column);

        Children.Add(element);
        M_Grid.SetColumn(element, ColumnDefinitions.Count - 1);
       
    }
    public void ColumnElements(UIElement[] elements)
    {
        foreach (UIElement element in elements)
        {
            Column(element);
        }
    }
    public void RowElements(UIElement[] elements)
    {
        foreach (UIElement element in elements)
        {
            Row(element);
        }
    }
    public void TwoColumn(UIElement element1, UIElement element2, double column2_Width = 1, GridUnitType unit = GridUnitType.Star)
    {
        Column(element1);
        Column(element2, column2_Width, unit);
    }
    public void TwoColumnT(UIElement element1, UIElement element2, double column1_Width = 1, double column2_Width = 1, GridUnitType unit = GridUnitType.Star)
    {
        Column(element1, column1_Width, unit);
        Column(element2, column2_Width, unit);
    }
    public void TwoRow(UIElement element1, UIElement element2, double row2_Height = 1, GridUnitType unit = GridUnitType.Star)
    {
        Row(element1);
        Row(element2, row2_Height, unit);
    }


    public void SectionProperty(string displayName, FrameworkElement element, string bindingPath, bool keyframeIcon = true) //------ SECTIONS
    {
        Margin = new Thickness(0, -3, 0, -3);

        M_Label label = new(displayName);
        ManualAPI.SetBind(element, bindingPath);

        this.SetBackground(Colors.Transparent);
        if(displayName != "" && displayName != null)
        this.TwoColumn(label, element, 1.8);
        else
        this.TwoColumnT(label, element, 0, 1);

        if (keyframeIcon)
            ManualAPI.BindKeyframe(element, true);
    }



    /// <summary>
    /// display a horizontal section of a property with a label and a textbox
    /// </summary>
    /// <param name="displayName">display the name in to a label</param>
    /// <param name="bindingPath">the property name</param>
    /// <param name="keyframeIcon">if its true, add a M_Keyframe at the right</param>
    public M_Grid(string displayName, FrameworkElement element, string bindingPath, bool keyframeIcon = true)
    {
        SectionProperty(displayName, element, bindingPath, keyframeIcon);
    }
    public M_Grid(string displayName, string bindingPath, double jumps = 100, double jump = 1, bool limited = false, double minimum = 0, double maximum = double.NaN, bool keyframeIcon = true)
    {
        M_NumberBox element = new();
        element.Jumps = jumps;
        element.Jump = jump;
        element.IsLimited = limited;
        element.Minimum = minimum;

        if (double.IsNaN(maximum))
        {         
            element.Maximum = double.MaxValue;
        }
        else
        {
            element.Maximum = maximum;
        }

        SectionProperty(displayName, element, bindingPath, keyframeIcon);
    }
}


#endregion

public interface ISelectable
{
    bool IsSelected { get; set; }
}

/// <summary>
/// AttachedCollection and SelectedItem recomended asign;
/// </summary>
/// <typeparam name="T"></typeparam>
public class SelectionCollection<T> : MvvmHelpers.ObservableRangeCollection<T> where T : ISelectable
{
    public SelectionCollection()
    {

    }
    public SelectionCollection(ObservableCollection<T> attachedCollection)
    {
        AttachedCollection = attachedCollection;
    }

    public new void Add(T item)
    {
        if (!Contains(item))
        {
            base.Add(item);
        }
    }


    public void SetIsSelected(bool isSelected)
    {
        foreach (T item in this)
        {
            if (item is not null)
                item.IsSelected = isSelected;
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (ISelectable item in e.NewItems)
            {
                if (item is not null)
                    item.IsSelected = true;
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (ISelectable item in e.OldItems)
            {
                if (item != SelectedItem)
                    item.IsSelected = false;
            }
        }
    }

    ISelectable _selectedItem = null;
    public ISelectable SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            SelectSingleItem((T)value);
        }
    }
    protected override void ClearItems()
    {
        SetIsSelected(false);
        base.ClearItems();
    }

    public void SelectSingleItem(T item)
    {
        Clear();
        Add(item);
    }


    public ObservableCollection<T> AttachedCollection;
    /// <summary>
    /// delete only selected items
    /// </summary>
    public void Delete()
    {
        var itemsToDelete = this.ToList();
        if (itemsToDelete.Count == 0)
            return;


        if (AttachedCollection != null)
        {
            foreach (T item in itemsToDelete)
                AttachedCollection.Remove(item);
        }


        Clear();
    }

    public void SelectAll()
    {
        Clear();
        AddRange(AttachedCollection);
        SetIsSelected(true);
    }

    public void SwapSelection()
    {
        if (Count > 0)
            Clear();
        else
            SelectAll();
    }

    public void Select(IEnumerable<T> items, bool clear = true)
    {
        if(clear)
          Clear();

        foreach (T item in items)
        {
            Add(item); 
        }
    }


    /// <summary>
    /// clone the objects if its possible and asign as selected
    /// </summary>
    public void Clone()
    {
        var clone = this.ToList();
        Clear();
        foreach (T item in clone)
        {
            var cloned = (T)ManualClipboard.Clone(item);
            if (cloned == null)
            {
                Output.Log($"this item cannot be cloned: {item}");
                continue;
            }

            AttachedCollection.Add(cloned);
            this.Add(cloned);
        }
    }

}



//----------------------------------------------------------------------------------------------------------------------- WINDOWS
public static partial class ManualAPI
{

    public static void StartProgressBar()
    {
        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate, AppModel.mainW);
      //  AppModel.mainW.renderBar.Visibility = Visibility.Visible;
      //  AppModel.mainW.renderBar.Value = 0;
        AppModel.mainW.info_text.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// from 0 to 100
    /// </summary>
    /// <param name="progress"></param>
    public static void SetProgressBar100(float progress)
    {
        TaskbarManager.Instance.SetProgressValue((int)Math.Round(progress), 100, AppModel.mainW);
       // AppModel.mainW.renderBar.Value = (int)Math.Round(progress);
    }

    /// <summary>
    /// from 0 to 1
    /// </summary>
    /// <param name="progress"></param>
    public static void SetProgressBar(float progress)
    {
        SetProgressBar100(progress * 100);
    }
    public static void StopProgressBar()
    {
        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, AppModel.mainW);
       // AppModel.mainW.renderBar.Visibility = Visibility.Collapsed;
        AppModel.mainW.logTextblock.Text = "";
        AppModel.mainW.info_text.Visibility = Visibility.Visible;
    }

    public static void SetProgressInfo(string info)
    {
        AppModel.mainW.logTextblock.Text = info;
    }

    public static void NotificationTaskBar()
    {
        if (!AppModel.mainW.IsActive)
        {
            WindowInteropHelper wih = new WindowInteropHelper(AppModel.mainW);
            FlashWindow(wih.Handle, true);
        }

        Output.DoneSound();

       // AppModel.mainW.renderBar.Visibility = Visibility.Collapsed;
        AppModel.mainW.info_text.Visibility = Visibility.Visible;
    }
    [DllImport("user32")] public static extern int FlashWindow(IntPtr hwnd, bool bInvert);

}//TODO: debe usarse taskbar en lugar de esto






//----------------------------------------------------------------------------------------------------------------------------------------- API 2.0
public static partial class ManualAPI
{
    public static void SetSelectionValue<T>(ref SelectionCollection<T> field, SelectionCollection<T> value, ObservableCollection<T> attachedCollection) where T : ISelectable
    {
        value.AttachedCollection ??= attachedCollection;
        field = value;
    }
    public static Guid GetId(IId value)
    {
        if (value != null)
            return value.Id;
        else
           return default;
    }

}
