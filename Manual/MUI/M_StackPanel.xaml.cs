using Manual.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Xml.Linq;

namespace Manual.MUI
{
    /// <summary>
    /// Lógica de interacción para M_StackPanel.xaml
    /// </summary>
    public partial class M_StackPanel : StackPanel, IManualElement
    {
        public IManualElement Clone()
        {
            var clone = new M_StackPanel();

            AppModel.CopyBinding(clone, this, DataContextProperty);
            /*
          var cloneD = AppModel.CloneBinding(this, DataContextProperty);
            if (cloneD is null)
                clone.DataContext = DataContext;
            else
                clone.SetBinding(DataContextProperty, cloneD);
            */
            foreach (var children in Children)
            {
                if(children is IManualElement element)
                   clone.Add(element.Clone());
            }      
            return clone;
        }

        public void InitializeBind(Binding bind)
        {
            throw new NotImplementedException();
        }

        public M_StackPanel()
        {
            InitializeComponent();
            lastStackPanel = this;

            DataContextChanged += M_StackPanel_DataContextChanged;
        }

        private void M_StackPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (FrameworkElement item in Children)
            {
               // item.DataContext = DataContext;
            }
        }

        public M_StackPanel(string dataContext)
        {
            InitializeComponent();
            lastStackPanel = this;
            API.ManualAPI.SetBind(this, dataContext); //upsi
         
        }
        public M_StackPanel(object dataContext)
        {
            DataContext = dataContext;
            lastStackPanel = this;
        }

        public M_StackPanel(IManualElement[] elements)
        {
            InitializeComponent();
            lastStackPanel = this;
            AddRange(elements);
          
        }

        public void Add(IManualElement element)
        {
            var fr = (FrameworkElement)element;
            fr.Margin = new Thickness(0, 3, 0, 3);
            lastStackPanel.Children.Add(fr);
        }

        public void AddRange(params IManualElement[] elements)
        {
            foreach (IManualElement element in elements)
            {
                Add(element);
            }
        }

        public M_StackPanel lastStackPanel;

        /// <summary>
        /// multicontext updated in real time
        /// </summary>
        /// <param name="bindings"></param>
        /// <returns></returns>
        public M_StackPanel(object dataContext, params string[] bindings)
        {
            DataContext = dataContext;
            lastStackPanel = this;

            for (int i = 0; i < bindings.Length; i++)
            {
                var newContext = new M_StackPanel();
                lastStackPanel.Children.Add(newContext);
                newContext.SetBinding(DataContextProperty, bindings[i]);
                lastStackPanel = newContext;

            }

        }
  
    }
}
