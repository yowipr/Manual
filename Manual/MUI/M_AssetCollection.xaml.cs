using Manual.Core;
using Manual.Objects;
using System;
using System.Collections;
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
/// Lógica de interacción para M_AssetCollection.xaml
/// </summary>
public partial class M_AssetCollection : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource",
        typeof(IEnumerable),
        typeof(M_AssetCollection),
        new PropertyMetadata(default(IEnumerable)));

    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public ContextMenu assetContextMenu = new();

    public M_AssetCollection()
    {
        InitializeComponent();
    }


    public M_AssetCollection(string bindingSource)
    {
        InitializeComponent();

        SetBinding(ItemsSourceProperty, new Binding(bindingSource));
    }

    private void ItemsControl_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var assetFile = AppModel.FindAncestor<AssetFileView>(e.OriginalSource as DependencyObject);
        if (assetFile != null)
        {
            if (assetContextMenu == null)
                return;

            assetFile.ContextMenu = assetContextMenu;
            // Opcionalmente, puedes establecer el DataContext del ContextMenu para operaciones más específicas
            assetContextMenu.DataContext = assetFile.DataContext;
        }
    }


    public void AddMenuItem(string header, RoutedEventHandler onClick)
    {
        var insertItem = new MenuItem { Header = header };
        insertItem.Click += onClick;
        assetContextMenu.Items.Add(insertItem);
    }

}
