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

namespace Manual.Objects;

/// <summary>
/// Lógica de interacción para ImgLabel.xaml
/// </summary>
public partial class ImgLabel : UserControl
{
    public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                "ImageSource",
                typeof(ImageSource),
                typeof(ImgLabel),
                new PropertyMetadata(null));

    public static readonly DependencyProperty LabelContentProperty =
        DependencyProperty.Register(
            "LabelContent",
            typeof(string),
            typeof(ImgLabel),
            new PropertyMetadata(string.Empty));

    public ImageSource ImageSource
    {
        get { return (ImageSource)GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    public string LabelContent
    {
        get { return (string)GetValue(LabelContentProperty); }
        set { SetValue(LabelContentProperty, value); }
    }

    public ImgLabel()
    {
        InitializeComponent();
        // Inicialización si es necesaria
    }
}

