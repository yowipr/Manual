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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManualToolkit.Themes;

/// <summary>
/// Lógica de interacción para VisualGlow.xaml
/// </summary>
public partial class VisualGlow : UserControl
{
    public VisualGlow()
    {
        InitializeComponent();

        Loaded += VisualGlow_Loaded;
    }

    private void VisualGlow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVisual();
    }

    public static readonly DependencyProperty VisualProperty = DependencyProperty.Register(
       "Visual",
       typeof(Visual),
       typeof(VisualGlow),
       new PropertyMetadata(null, OnVisualChanged));

    public Visual Visual
    {
        get { return (Visual)GetValue(VisualProperty); }
        set { SetValue(VisualProperty, value); }
    }

    public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.Register(
        "ShadowDepth",
        typeof(double),
        typeof(VisualGlow),
        new PropertyMetadata(50.0, OnVisualPropertyChanged));

    public double ShadowDepth
    {
        get { return (double)GetValue(ShadowDepthProperty); }
        set { SetValue(ShadowDepthProperty, value); }
    }

    public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
        "BlurRadius",
        typeof(double),
        typeof(VisualGlow),
        new PropertyMetadata(100.0, OnVisualPropertyChanged));

    public double BlurRadius
    {
        get { return (double)GetValue(BlurRadiusProperty); }
        set { SetValue(BlurRadiusProperty, value); }
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as VisualGlow)?.UpdateVisual();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as VisualGlow)?.UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (Visual != null)
        {
            Rectangle.Fill = new VisualBrush(Visual)
            {
                Stretch = Stretch.None
            };

            Rectangle.Effect = new BlurEffect
            {
                Radius = BlurRadius
            };

            // Asumiendo que el Visual tiene propiedades de Width y Height definidas.
            Rectangle.Width = ((FrameworkElement)Visual).ActualWidth + ShadowDepth;
            Rectangle.Height = ((FrameworkElement)Visual).ActualHeight + ShadowDepth;
        }
    }
}
