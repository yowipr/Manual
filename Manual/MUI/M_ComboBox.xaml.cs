using Manual.API;
using Manual.Core;
using Manual.Core.Nodes;
using Manual.Objects;
using ManualToolkit.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.MUI;

/// <summary>
/// an viewmodel item of a combobox usually, to show a name string
/// </summary>
/// <param name="Name"></param>
public readonly struct Item(string Name)
{
    public string Name { get; } = Name;

    public static Item New(string name)
    {
        return new Item(name);
    }
    public override bool Equals(object obj)
    {
        return obj is Item item && Name == item.Name;
    }

    public override string ToString()
    {
        return Name;
    }
}

/// <summary>
/// Lógica de interacción para M_ComboBox.xaml
/// </summary>
public partial class M_ComboBox : UserControl, IManualElement
{
    internal NodeBase AttachedNode;
    internal NodeOption AttachedNodeOption;

    public virtual IManualElement Clone()
    {
        var clone = new M_ComboBox();
        clone.DisplayMemberPath = DisplayMemberPath;
        clone.ItemsSource = this.ItemsSource;
        clone.ItemsSourceExtra = this.ItemsSourceExtra;
        //clone.SelectedItem = this.SelectedItem;
        clone.ToolTip = this.ToolTip;
        //var bind = AppModel.CloneBinding(this, SelectedItemProperty);
        //clone.InitializeBind(bind);

        return clone;
    }

    public void InitializeBind(Binding bind)
    {
         SetBinding(SelectedItemProperty, bind);
         //AsignInputText(SelectedItem);

        var d = DataContext;
    }


    public M_ComboBox(string itemSource)
    {
        InitializeComponent();

        Binding binding = new Binding(itemSource);
        this.SetBinding(ItemsSourceProperty, binding);
    }
  
    public M_ComboBox(string itemSource, string selectedItem)
    {
        InitializeComponent();

        Binding binding = new Binding(itemSource);
        this.SetBinding(ItemsSourceProperty, binding);

         var binding2 = new Binding(selectedItem);
         this.SetBinding(SelectedItemProperty, binding2);


    }

    public M_ComboBox(string itemSource, string selectedItem, string displayName)
    {
        InitializeComponent();

        DisplayMemberPath = displayName;
        Binding binding = new Binding(itemSource);
        this.SetBinding(ItemsSourceProperty, binding);

        binding = new Binding(selectedItem);
        this.SetBinding(SelectedItemProperty, binding);

    }

    public static readonly DependencyProperty DisplayMemberPathProperty =
    DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(M_ComboBox), new PropertyMetadata("Name"));
    public string DisplayMemberPath
    {
        get { return (string)GetValue(DisplayMemberPathProperty); }
        set { SetValue(DisplayMemberPathProperty, value); }
    }

    // DependencyProperty para ItemsSource
    public static readonly DependencyProperty ItemsSourceExtraProperty =
        DependencyProperty.Register(nameof(ItemsSourceExtra), typeof(IEnumerable<object>), typeof(M_ComboBox), new PropertyMetadata(null));

    public IEnumerable<object> ItemsSourceExtra
    {
        get { return (IEnumerable<object>)GetValue(ItemsSourceExtraProperty); }
        set { SetValue(ItemsSourceExtraProperty, value); }
    }



    // DependencyProperty para ItemsSource
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<object>), typeof(M_ComboBox), new PropertyMetadata(null));

    public IEnumerable<object> ItemsSource
    {
        get { return (IEnumerable<object>)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    // DependencyProperty para SelectedItem
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(M_ComboBox), new FrameworkPropertyMetadata(
         null,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));
    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Aquí puedes agregar lógica para responder al cambio de valor
        if (d is M_ComboBox instance)
        {
            instance.OnSelectedItemChanged(e.NewValue);
            // cbox.AsignInputText(e.NewValue); //no se puede
        }
    }    // Declarar el evento usando el delegado EventHandler
    public event Event<object> SelectedItemChanged;
    protected virtual void OnSelectedItemChanged(object newValue)
    {
        SelectedItemChanged?.Invoke(newValue);
    }

    public bool IsExtraItemsSelectable = true;
    public object SelectedItem
    {
        get { return GetValue(SelectedItemProperty); }
        set
        {
            if (ItemsSourceExtra != null && ItemsSourceExtra.Contains(value))
            {
                if (IsExtraItemsSelectable)
                {
                    SetValue(SelectedItemProperty, value);
                    AsignInputText(value);
                }
            }
            else
            {
                SetValue(SelectedItemProperty, value);
                AsignInputText(value);
            }
        }
    }

    public string Header
    {
        set
        {
            headerText.Text = value;
        }
    }


    //------------------------------------------------------------------------------------------------------- CTOR
    public M_ComboBox()
    {
        
        InitializeComponent();

        //  ItemsSource = ManualAPI.layers;
        //   UpdateTextBoxDisplayValue(ItemsSource.First());
    }


    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        Window window = Window.GetWindow(this);
        if (window != null)
        {
            window.MouseDown += Window_PreviewMouseDown;
            window.PreviewKeyDown += Window_PreviewKeyDown;
            window.Deactivated += Window_Deactivated;
        }
        var d = DataContext; //if this is GenerationManager, ignore, it´s de combobox in the topbar

        if(SelectedItem != null)
            AsignInputText(SelectedItem);
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        ItemsPopup.IsOpen = false;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Window window = Window.GetWindow(this);
        if (window != null)
        {
            window.MouseDown -= Window_PreviewMouseDown;
            window.PreviewKeyDown -= Window_PreviewKeyDown;
            window.Deactivated -= Window_Deactivated;
        }
    }


    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
       HitTestResult result = VisualTreeHelper.HitTest(this, e.GetPosition(this));

        if (result == null)
        {
            // No se tocó el UserControl; el resultado del hit test es null, cierra el Popup.
            if (ItemsPopup.IsOpen)
            {
                ItemsPopup.IsOpen = false;
                AsignInputText(SelectedItem);
            }
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && ItemsPopup.IsOpen)
        {
            ItemsPopup.IsOpen = false;
        }
    }

    public M_ComboBox(IEnumerable<object> itemsSource)
    {
        InitializeComponent();

        SetItemsSource(itemsSource);
    }

    public M_ComboBox(IEnumerable<object> itemsSource, string selectedItem)
    {
        InitializeComponent();

        SetItemsSource(itemsSource);

        var binding = new Binding(selectedItem);
        this.SetBinding(SelectedItemProperty, binding);
    }

    public M_ComboBox(IEnumerable<object> itemsSource, IEnumerable<object> extraItems)
    {
        InitializeComponent();

        ItemsSourceExtra = extraItems;
        SetItemsSource(itemsSource);
    }
    internal void SetItemsSource(IEnumerable<object> itemsSource)
    {
        if (!itemsSource.Any())
            return;

        if (itemsSource.First() is string)
            DisplayMemberPath = "";

        ItemsSource = itemsSource;

        //SelectedItem = itemsSource.First();

        //UpdateTextBoxDisplayValue(ItemsSource.First());
    }



    //-------------------------------------------------- REAL ITEMS
    IEnumerable<object> CombinedItems()
    {
        var separator = new Separator()
        {
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(1),
            Height= 5, 
            Foreground = new SolidColorBrush(Colors.Gray),
            Background = new SolidColorBrush(Colors.Gray)
        };

        return [..ItemsSource, separator, ..ItemsSourceExtra];
    }


    private void InputTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ItemsSourceExtra != null)
            SearchedItems = CombinedItems();
        else
            SearchedItems = ItemsSource;

        ItemsListBox.ItemsSource = SearchedItems;
        ItemsPopup.IsOpen = true;

        e.Handled = true;

        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    internal void UpdateTextBoxDisplayValue(object selectedItem) // ON SELECTED ITEM CHANGED
    {
        if (selectedItem == null || (ItemsSourceExtra != null && ItemsSourceExtra.Contains(selectedItem) && !IsExtraItemsSelectable) )
            return;


        AsignInputText(selectedItem);

     
        ItemsPopup.IsOpen = false;

        InputTextBox.IsEnabled = false;
        InputTextBox.IsEnabled = true;

        SetSelectedItem(selectedItem); //ItemsListBox.SelectedItem;

        var a = DataContext as NodeOption;
        if (a != null)
        {
            var b = a.FieldValue;
        }
    }
    internal virtual void AsignInputText(object selectedItem)
    {
    


        if (selectedItem == null)
        {
            InputTextBox.Text = "null";
            return;
        }
        else if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            if (ItemsSourceExtra != null && ItemsSourceExtra.Contains(selectedItem) && !IsExtraItemsSelectable)
                return;

            var propertyInfo = selectedItem.GetType().GetProperty(DisplayMemberPath);
            if (propertyInfo != null)
            {
                var value = propertyInfo.GetValue(selectedItem);
                InputTextBox.Text = value != null ? value.ToString() : string.Empty;
                return;
            }
        }

        InputTextBox.Text = selectedItem.ToString();
        InputTextBox.Select(InputTextBox.Text.Length, 0);
    }






    IEnumerable<object> SearchedItems;
    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!ItemsPopup.IsOpen)
            return;

        TextBox textBox = sender as TextBox;

        IEnumerable<object> itemsSource;
        if (ItemsSourceExtra != null)
            itemsSource = CombinedItems();
        else
            itemsSource = ItemsSource;


        if (textBox == null || itemsSource is null) return;

        string filterText = textBox.Text;
        var filterWords = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(word => word.Trim()).ToList();

        if (string.IsNullOrWhiteSpace(filterText))
        {
            SearchedItems = itemsSource.Cast<object>();
        }
        else
        {
            // Asumiendo que ItemsSource es IEnumerable<object> y cada objeto tiene una propiedad 'Name'
            SearchedItems = itemsSource.Cast<object>()
             .Where(item => ItemMatchesFilter(item, filterWords))
             .ToList();
        }

        // Actualiza el ListBox o Popup con los ítems filtrados
        ItemsListBox.ItemsSource = SearchedItems;

       if(SearchedItems.Any())
          ChangeOnlySearchItemsIndex(0);
    }
    private bool ItemMatchesFilter(object item, List<string> filterWords)
    {
        var itemValue = DisplayMemberPath != "" ? item.GetType().GetProperty(DisplayMemberPath)?.GetValue(item)?.ToString() : item.ToString();
        if (string.IsNullOrEmpty(itemValue))
        {
            return false;
        }

        var itemWords = itemValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(word => word.Trim()).ToList();
        return filterWords.All(filterWord => itemWords.Any(itemWord => itemWord.IndexOf(filterWord, StringComparison.OrdinalIgnoreCase) >= 0));
    }



    void ChangeOnlySearchItemsIndex(int index)
    {
        ItemsListBox.SelectionChanged -= ItemsListBox_SelectionChanged;
        ItemsListBox.SelectedIndex = index;
        ItemsListBox.SelectionChanged += ItemsListBox_SelectionChanged;
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SearchedItems != null && SearchedItems.Any())
        {
            SetSelectedItem(ItemsListBox.SelectedItem); //SearchedItems.First();
            UpdateTextBoxDisplayValue(SelectedItem);
        }
        else if ((e.Key == Key.Down || e.Key == Key.Up) && ItemsListBox.Items.Count > 0)
        {
            int currentIndex = ItemsListBox.SelectedIndex;

            if (e.Key == Key.Down)
            {
                currentIndex = (currentIndex + 1) % ItemsListBox.Items.Count;
            }
            else if (e.Key == Key.Up)
            {
                currentIndex = (currentIndex - 1 + ItemsListBox.Items.Count) % ItemsListBox.Items.Count;
            }

            ChangeOnlySearchItemsIndex(currentIndex);
            ItemsListBox.ScrollIntoView(ItemsListBox.SelectedItem);
        }



    }
    private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTextBoxDisplayValue(ItemsListBox.SelectedItem);
    }




    //-------------------------- DRAG
    private void InputTextBox_DragEnter(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }


    private void InputTextBox_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }


    private void InputTextBox_Drop(object sender, DragEventArgs e)
    {
        // Reemplaza 'MiObjeto' con el tipo de datos real que estás arrastrando
        if (e.Data.GetDataPresent(typeof(object)))
        {
            object arrastrado = e.Data.GetData(typeof(object));
            var objetoEncontrado = ItemsSource.Cast<object>().FirstOrDefault(item => item.Equals(arrastrado));

            if (objetoEncontrado != null)
            {
                UpdateTextBoxDisplayValue(objetoEncontrado);
                SetSelectedItem(objetoEncontrado);
            }
        }


    }

    public bool SelectedItemAsName = false;
    void SetSelectedItem(object selected)
    {
       if (SelectedItemAsName && selected is INamable selectedN)
            SelectedItem = selectedN.Name;
        else
        SelectedItem = selected;
    }

    private void Container_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        InputTextBox_PreviewMouseDown(sender, e);
    }



}













public class M_ComboBox_Layers : M_ComboBox
{
    public override IManualElement Clone()
    {
        var clone = new M_ComboBox_Layers();
        clone.DisplayMemberPath = DisplayMemberPath;
        clone.ItemsSource = this.ItemsSource;
        clone.ItemsSourceExtra = this.ItemsSourceExtra;
        clone.SelectedItem = this.SelectedItem;

        clone.AttachedNode = this.AttachedNode;

        return clone;
    }

    Action<LayerBase> OnSelectedLayerChanged;

    public bool SelectedAsDefault = true;

    string selectedBinding = "FieldValue";
    void BindField()
    {
        Binding selectedItemBinding = new Binding(selectedBinding)
        {
            Mode = BindingMode.TwoWay, // Asumiendo que deseas un binding bidireccional
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // Para actualizar al cambiar la selección
        };
        SetBinding(ComboBox.SelectedItemProperty, selectedItemBinding);

    }
    public M_ComboBox_Layers()
    {
        InitializeComponent();
        Initialize();
    }

    public M_ComboBox_Layers(string selectedBinding)
    {
        InitializeComponent();
        this.selectedBinding = selectedBinding;
        Initialize();     
    }


    void Initialize()
    {
        var selected = Item.New("Selected");
        ItemsSourceExtra = [selected];
        SetItemsSource(ManualAPI.layers);

        Shot.LayerChanged += Shot_LayerChanged;

        Unloaded += M_ComboBox_Layers_Unloaded;
        Loaded += M_ComboBox_Layers_Loaded;

    }

    private void M_ComboBox_Layers_Loaded(object sender, RoutedEventArgs e)
    {
        var d = DataContext;
        if (SelectedAsDefault && SelectedItem != null)
        {
            //SelectedItem = ItemsSourceExtra.First(); // selected
            BindField();
            //AsignInputText(SelectedItem);
        }
    }

    private void M_ComboBox_Layers_Unloaded(object sender, RoutedEventArgs e)
    {
        Shot.LayerChanged -= Shot_LayerChanged;
    }

    public bool IsItem(string itemName)
    {
        return SelectedItem is Item item && item.Name == itemName;
    }
    /// <summary>
    /// true if is the selected layer in the combobox
    /// </summary>
    /// <returns></returns>
    public bool IsSelected()
    {
        return IsItem("Selected");
    }


    private void Shot_LayerChanged(LayerBase layer)
    {
       // if (InputTextBox.Text == "Selected")
       if(IsSelected())
        {
            //SelectedItem = layer;
            var nodeop = DataContext as NodeOption;
            if (nodeop != null)
                nodeop.ValueChanged?.Invoke(nodeop.FieldValue);// = layer;
        }
    }


    //when FieldValue changed
    internal override void AsignInputText(object selectedItem)
    {
      
        base.AsignInputText(selectedItem);
        //Shot_LayerChanged(ManualAPI.SelectedLayer);


    }
}




public class M_ComboBoxEnum : M_ComboBox
{
    public M_ComboBoxEnum(Type enumType)
    {
        ItemsSource = AppModel.GetEnumNames(enumType);
        DisplayMemberPath = "";
    }
    public M_ComboBoxEnum(Type enumType, string selectedItem)
    {
        ItemsSource = AppModel.GetEnumNames(enumType);
        DisplayMemberPath = "";

        SetBinding(ComboBox.SelectedItemProperty, new Binding(selectedItem));
    }

}