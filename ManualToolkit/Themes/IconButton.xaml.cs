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

namespace ManualToolkit.Themes
{
    /// <summary>
    /// Lógica de interacción para IconButton.xaml
    /// </summary>
    public partial class IconButton : UserControl
    {

        public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
    nameof(IconSource),
    typeof(ImageSource),
    typeof(IconButton),
    new PropertyMetadata(null));

        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }


        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
    nameof(Command),
    typeof(ICommand),
    typeof(IconButton),
    new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }


        public IconButton()
        {
            InitializeComponent();
        }
    }
}
