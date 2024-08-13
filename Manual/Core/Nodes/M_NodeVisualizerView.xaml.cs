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

namespace Manual.Core.Nodes;

/// <summary>
/// Lógica de interacción para NodeVisualizerView.xaml
/// </summary>
public partial class M_NodeVisualizerView : UserControl
{
    public M_NodeVisualizerView()
    {
        InitializeComponent();

        Loaded += M_NodeVisualizerView_Loaded;     
    }

    private void M_NodeVisualizerView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateFields();
    }

    void UpdateFields()
    {
        var node = DataContext as NodeBase;
        if (node != null)
        {
            itemsControl.ItemsSource = node.Fields.Where(f => f.Direction != NodeOptionDirection.Output);
        }
    }
}
