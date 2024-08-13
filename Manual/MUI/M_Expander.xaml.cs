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

namespace Manual.MUI
{
    /// <summary>
    /// Lógica de interacción para M_Expander.xaml
    /// </summary>
    public partial class M_Expander : Expander
    {

        public StackPanel container = new();
        public ItemsControl items { get; set; } = new();
        
        public M_Expander()
        {
            InitializeComponent();
            container.Children.Add(items);
            Content = container;
        }
        public M_Expander(StackPanel stackPanelParent, string header = "section")
        {
            InitializeComponent();
            container.Children.Add(items);
            stackPanelParent.Children.Add(this);
            Content = container;
            Header = header;
        }

        public void Add(FrameworkElement element)
        {
            element.Margin = new Thickness(0, 2, 0, 2);
            container.Children.Add(element);
        }
        public void AddRange(FrameworkElement[] elements)
        {
            foreach (FrameworkElement element in elements)
            {
                Add(element);
            }
        }
    }

}
