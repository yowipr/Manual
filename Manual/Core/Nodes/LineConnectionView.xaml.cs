using System;
using System.Collections.Generic;
using System.ComponentModel;
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
/// Lógica de interacción para LineConnectionView.xaml
/// </summary>
public partial class LineConnectionView : UserControl
{

    public static readonly DependencyProperty TypeColor0Property = DependencyProperty.Register(
 nameof(TypeColor0),
 typeof(SolidColorBrush),
 typeof(LineConnectionView),
 new PropertyMetadata(new SolidColorBrush(Colors.White)));

    public SolidColorBrush TypeColor0
    {
        get => (SolidColorBrush)GetValue(TypeColor0Property);
        set => SetValue(TypeColor0Property, value);
    }
    public static readonly DependencyProperty TypeColor1Property = DependencyProperty.Register(
 nameof(TypeColor1),
 typeof(SolidColorBrush),
 typeof(LineConnectionView),
 new PropertyMetadata(new SolidColorBrush(Colors.White)));

    public SolidColorBrush TypeColor1
    {
        get => (SolidColorBrush)GetValue(TypeColor1Property);
        set => SetValue(TypeColor1Property, value);
    }






    void SetTypeColor()
    {
        var line = DataContext as LineConnection;
        if (line == null) return;

        string type0 = line.Output?.Type ?? line.Type0;
        string type1 = line.Input?.Type ?? line.Type0;

        if (type0 != "" && type1 != "" && line.IsSelected)
        {
            //DISABLED bonito pero poco práctico
          //  if(line.Output.AttachedNode.IsSelected)
            TypeColor0 = new SolidColorBrush(Colors.White);

          //  if (line.Input.AttachedNode.IsSelected)
                TypeColor1 = new SolidColorBrush(Colors.White);
            return;
        }

        if(type0 != "")
            TypeColor0 = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type0)];
        else
            TypeColor0 = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type1)];

        if (type1 != "")
           TypeColor1 = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type1)];
        else
           TypeColor1 = (SolidColorBrush)Resources[FieldTypes.TypeToColorName(type0)];
    }




    public static readonly DependencyProperty EndPointProperty = DependencyProperty.Register(
    nameof(EndPoint),
    typeof(Point),
    typeof(LineConnectionView),
    new PropertyMetadata(new Point(50, 50), OnEndPointChanged));

    public Point EndPoint
    {
        get => (Point)GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    private static void OnEndPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LineConnectionView)d;
        var newEndPoint = (Point)e.NewValue;
        control.CalculateControlPoints(newEndPoint);
    }
    public void CalculateControlPoints(Point endPoint)
    {
        var startPoint = new Point(0, 0);
        double midX = (startPoint.X + endPoint.X) / 2;

        if (startPoint.X < endPoint.X)
        {
            bezier.Point1 = new Point(midX, startPoint.Y);
            bezier.Point2 = new Point(midX, endPoint.Y);
        }
        else
        {
            bezier.Point1 = new Point(-midX, startPoint.Y);
            bezier.Point2 = new Point(endPoint.X + midX, endPoint.Y);
        }
    }


    public LineConnectionView()
    {
        InitializeComponent();


        Loaded += (s, e) =>
        {
            var line = DataContext as LineConnection;
            if (line != null)
            {
                SetBinding(EndPointProperty, new Binding(nameof(line.EndPoint))
                {
                    Source = line,
                    Mode = BindingMode.OneWay
                });
                SetTypeColor();
                line.PropertyChanged += Line_PropertyChanged;
            }
        };

        this.Unloaded += UserControl_Unloaded;

    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        var line = DataContext as LineConnection;
        if (line != null)
        {
            line.PropertyChanged -= Line_PropertyChanged;
        }
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LineConnection.Output) || e.PropertyName == nameof(LineConnection.Input) || e.PropertyName == "IsSelected")
        {
            SetTypeColor();
        }
    }
}
