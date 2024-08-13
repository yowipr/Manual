using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para ScrollbarArea.xaml
/// </summary>
public partial class ScrollbarArea : UserControl
{
    public static readonly DependencyProperty FrameStartProperty = DependencyProperty.Register(
        nameof(FrameStart), typeof(int), typeof(ScrollbarArea), new PropertyMetadata(0));

    public int FrameStart
    {
        get => (int)GetValue(FrameStartProperty);
        set => SetValue(FrameStartProperty, value);
    }

    public static readonly DependencyProperty FrameEndProperty = DependencyProperty.Register(
        nameof(FrameEnd), typeof(int), typeof(ScrollbarArea), new PropertyMetadata(100));

    public int FrameEnd
    {
        get => (int)GetValue(FrameEndProperty);
        set => SetValue(FrameEndProperty, value);
    }

 
    public static readonly DependencyProperty CanvasScaleXProperty = DependencyProperty.Register(
        nameof(CanvasScaleX), typeof(double), typeof(ScrollbarArea), new FrameworkPropertyMetadata(
         1.0,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    public double CanvasScaleX
    {
        get => (double)GetValue(CanvasScaleXProperty);
        set => SetValue(CanvasScaleXProperty, value);
    }
    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Aquí puedes agregar lógica para responder al cambio de valor
    }
    public ScrollbarArea()
    {
        InitializeComponent();
    }

    public event DragDeltaEventHandler LeftDragDelta
    {
        add { ZoomLeft.DragDelta += value; }
        remove { ZoomLeft.DragDelta -= value; }
    }

    public event DragDeltaEventHandler RightDragDelta
    {
        add { ZoomRight.DragDelta += value; }
        remove { ZoomRight.DragDelta -= value; }
    }

    private void ZoomLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {

    }

    private void ZoomRight_DragDelta(object sender, DragDeltaEventArgs e)
    {

    }
}


public class ScaleMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is double actualWidth && values[1] is double scale)
        {
            return actualWidth * scale;
        }
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
