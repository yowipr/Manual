using Manual.Core.Nodes;
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
/// Lógica de interacción para SearchMenuBox.xaml
/// </summary>
public partial class SearchMenuBox : UserControl
{

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
    "ItemsSource",
    typeof(IEnumerable<MenuItemNode>),
    typeof(SearchMenuBox),
    new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<MenuItemNode> ItemsSource
    {
        get { return (IEnumerable<MenuItemNode>)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as SearchMenuBox;
        if (control != null)
        {
            control.OnItemsSourceChanged(e);
        }
    }

    List<MenuItemNode> SearchItems;
    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        // Aquí puedes reaccionar a los cambios. Por ejemplo, asignar el nuevo valor a tu ListBox.
        var items = e.NewValue as IEnumerable<MenuItemNode>;
        if (items != null)
        {
            SearchItems = items.Take(10).ToList();
            listBox.ItemsSource = SearchItems;
        }
    }




    public SearchMenuBox()
    {
        InitializeComponent();

    }
    public void Open()
    {
        input.Text = "";
        input.FocusText();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        listBox.ItemsSource = SearchItems;
    }

    private void UserControl_GotFocus(object sender, RoutedEventArgs e)
    {
        listBox.ItemsSource = SearchItems;
        input.FocusText();
    }


    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {

        string searchText = input.Text.ToLower();
        // var filterWords = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(word => word.Trim()).ToList();
        // listBox.ItemsSource = SearchItems.Where(item => ItemMatchesFilter(item, filterWords));
        if (ItemsSource == null)
            return;

        //listBox.ItemsSource = ItemsSource.Where(item =>
        //      MatchAllText(searchText, item.Name, item.NameType, item.Path, item.Description) && item.DoAction != null //ensure has an action
        //).ToList();

        var filteredAndSortedItems = ItemsSource
            .Where(item => MatchAllText(searchText, item.Name, item.NameType, item.Path, item.Description) && item.DoAction != null)
            .OrderByDescending(item => ExactMatchPriority(searchText, item.Name, item.NameType, item.Path, item.Description))
            .ToList();

        listBox.ItemsSource = filteredAndSortedItems;

        listBox.SelectedIndex = 0;

    }

    bool MatchAllText(string searchText, params string?[] fields)
    {
        var lowerSearchTextParts = searchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Concatena todos los campos relevantes en una sola cadena de texto, separada por espacios.
        var concatenatedFields = string.Join(" ", fields.Where(f => !string.IsNullOrEmpty(f)).Select(f => f!.ToLower()));

        // Verifica que cada parte del texto de búsqueda esté contenida en la cadena concatenada de campos.
        return lowerSearchTextParts.All(part => concatenatedFields.Contains(part));
    }

    int ExactMatchPriority(string searchText, params string?[] fields)
    {
        var lowerSearchText = searchText.ToLower();
        // Prioridad máxima si alguno de los campos es una coincidencia exacta.
        if (fields.Any(field => field?.ToLower() == lowerSearchText)) return 3;
        // Prioridad media si la búsqueda es una subcadena de cualquiera de los campos.
        if (fields.Any(field => field?.ToLower().Contains(lowerSearchText) == true)) return 1;

        // Baja prioridad para todo lo demás.
        return 0;
    }



    bool MatchText(string searchText, string? currentText)
    {
        if (string.IsNullOrEmpty(currentText)) return false;

        // Convierte ambos, searchText y currentText, a minúsculas para hacer la comparación insensible a mayúsculas.
        var lowerSearchText = searchText.ToLower();
        var lowerCurrentText = currentText.ToLower();

        // Divide searchText en palabras basado en espacios y verifica que cada palabra esté contenida en currentText.
        return lowerSearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .All(part => lowerCurrentText.Contains(part));
    }
    bool MatchTextBasic(string searchText, string? currentText)
    {
        if (currentText == null) return false;
        else return searchText.ToLower().Contains(currentText.ToLower());
    }
    private bool ItemMatchesFilter(string searchText, List<string> filterWords)
    {
        var itemWords = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(word => word.Trim()).ToList();
        return filterWords.All(filterWord => itemWords.Any(itemWord => itemWord.IndexOf(filterWord, StringComparison.OrdinalIgnoreCase) >= 0));
    }


    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ClickAction();
        }
        else if ((e.Key == Key.Down || e.Key == Key.Up) && listBox.Items.Count > 0)
        {
            int currentIndex = listBox.SelectedIndex;

            if (e.Key == Key.Down)
            {
                currentIndex = (currentIndex + 1) % listBox.Items.Count;
            }
            else if (e.Key == Key.Up)
            {
                currentIndex = (currentIndex - 1 + listBox.Items.Count) % listBox.Items.Count;
            }

            ChangeOnlySearchItemsIndex(currentIndex);
           listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
    void ChangeOnlySearchItemsIndex(int index)
    {
        //listBox.SelectionChanged -= ItemsListBox_SelectionChanged;
        listBox.SelectedIndex = index;
       // listBox.SelectionChanged += ItemsListBox_SelectionChanged;
    }


    void ClickAction()
    {
        if (listBox.SelectedItem != null)
        {
            var m = listBox.SelectedItem as MenuItemNode;
            if (m != null && m.DoAction != null)
                m.DoAction();

            CloseDoAction();
        }
    }

    public Action CloseDoAction;

    private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (item != null)
        {
            if(item.DataContext is MenuItemNode m)
            {
                listBox.SelectedItem = m;
                ClickAction();
            }
        }
    }
}
