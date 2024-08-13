using Manual.API;
using Manual.Core;
using ManualToolkit.Themes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
/// Lógica de interacción para M_SliderBox.xaml
/// </summary>
public partial class M_SliderBox : UserControl, IManualElement
{
    public bool IsUndo
    {
        get => numberBox.IsUndo;
        set => numberBox.IsUndo = value;
    }
    public bool IsUpdateRender
    {
        get => numberBox.IsRenderUpdate;
        set => numberBox.IsRenderUpdate = value;
    }

    public IManualElement Clone()
    {
        var clone = new M_SliderBox(Header, Minimum, Maximum, Jumps, Jump, IsLimited);

    
        var bind = AppModel.CloneBinding(this, ValueProperty);
         clone.InitializeBind(bind);
        return clone;
    }
    public void InitializeBind(Binding bind)
    {
        if (bind is null)
            return;

        var genericConverter = (GenericObjectConverter)Application.Current.FindResource("genericObjectConverter");
        bind.Converter = genericConverter;
        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
        SetBinding(ValueProperty, bind);
    }

    public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
    nameof(BackgroundColor),
    typeof(Brush),
    typeof(M_SliderBox), // Asegúrate de reemplazar 'TuControl' con el nombre de tu clase
    new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));

    public Brush BackgroundColor
    {
        get { return (Brush)GetValue(BackgroundColorProperty); }
        set { SetValue(BackgroundColorProperty, value); }
    }



    #region Value Property
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
 nameof(Value),
 typeof(double),
 typeof(M_SliderBox),
 new FrameworkPropertyMetadata(
     1d, // valor predeterminado
     FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
     OnValueChanged, // controlador de cambio de valor
     CoerceValue, // controlador de validación de valor
     true, // si la propiedad debe ser animable
     UpdateSourceTrigger.PropertyChanged)); // si se debe actualizar la fuente cuando se cambia el valor
    #endregion      
    #region Header Property
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
  nameof(Header),
  typeof(string),
  typeof(M_SliderBox),
  new PropertyMetadata(""));
    #endregion
    #region Maximun Property
    public static readonly DependencyProperty MaximumProperty =
DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(M_SliderBox), new PropertyMetadata(100.0d));

    #endregion
    #region Minimun Property
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(M_SliderBox), new PropertyMetadata(1.0d));
    #endregion

  
    // Properties
    public double Value
    {
        get { return (double)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public event EventHandler<ValueChangedEventArgs> ValueChanged;
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as M_SliderBox;

        var newValue = (double)e.NewValue;
        control?.OnValueChanged((double)e.OldValue, newValue);
        //if (newValue == 0 && control.Minimum == 0)
        //    control.valueSlider.Opacity = 0;
        //else if (control.valueSlider.Opacity != 1)
        //    control.valueSlider.Opacity = 1;
    }
    // Método que dispara el evento ValueChanged
    protected virtual void OnValueChanged(double oldValue, double newValue)
    {
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));
    }



    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    public double Maximum
    {
        get { return (double)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }
    public double Minimum
    {
        get { return (double)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }

    #region ValueScale
    public enum ValueScaleType
    {
        Default,
        Decimal,
        Percentage
    }
    public static readonly DependencyProperty ValueScaleProperty = DependencyProperty.Register(
nameof(ValueScale),
typeof(ValueScaleType),
typeof(M_SliderBox),
new PropertyMetadata(ValueScaleType.Default));
    public ValueScaleType ValueScale
    {
        get { return (ValueScaleType)GetValue(ValueScaleProperty); }
        set { SetValue(ValueScaleProperty, value);
            ChangeValueScale();
        }
    }

    #endregion

    public static readonly DependencyProperty JumpProperty = DependencyProperty.Register(
   nameof(Jump), typeof(double), typeof(M_SliderBox), new PropertyMetadata(1d));

    public double Jump
    {
        get { return (double)GetValue(JumpProperty); }
        set { SetValue(JumpProperty, value); }
    }

    public static readonly DependencyProperty JumpsProperty =
DependencyProperty.Register(nameof(Jumps), typeof(double), typeof(M_SliderBox), new PropertyMetadata(100d));
    public double Jumps
    {
        get { return (double)GetValue(JumpsProperty); }
        set { SetValue(JumpsProperty, value); }
    }


    public static readonly DependencyProperty IsLimitedProperty =
    DependencyProperty.Register(nameof(IsLimited), typeof(bool), typeof(M_SliderBox), new PropertyMetadata(false));

    public bool IsLimited
    {
        get { return (bool)GetValue(IsLimitedProperty); }
        set { SetValue(IsLimitedProperty, value); }
    }



    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }

    public M_SliderBox()
    {
        InitializeComponent();
        ChangeValueScale();
    }

    public M_SliderBox(string propertyName, string bindingName, double minimunValue = 0, double maximunValue = 100, double jumps = 100, double jump = 1, bool isLimited = false, bool bindKeyframe = true)
    {
        InitializeComponent();
        // valueSlider.SetBinding(Slider.ValueProperty, new Binding(bindingName));
        // SetBinding(M_SliderBox.ValueProperty, bindingName);
        InitializeBind(new Binding(bindingName));
        Header = propertyName;
        Minimum = minimunValue;
        Maximum = maximunValue;
        Jump = jump;
        Jumps = jumps;
        IsLimited = isLimited;

        if (bindKeyframe)
            Loaded += OnLoad;


        if (jump % 1 != 0)
            ValueScale = ValueScaleType.Decimal;

        ChangeValueScale();

    }


    //NEW
    public M_SliderBox(string propertyName, double minimunValue = 0, double maximunValue = 100, double jumps = 100, double jump = 1, bool isLimited = false)
    {
        InitializeComponent();

        InitializeBind(new Binding(propertyName));
        
        Header = propertyName;
        Minimum = minimunValue;
        Maximum = maximunValue;
        Jump = jump;
        Jumps = jumps;
        IsLimited = isLimited;


        if (jump % 1 != 0)
            ValueScale = ValueScaleType.Decimal;

        ChangeValueScale();

    }
    void OnLoad(object sender, RoutedEventArgs e)
    {   
     //  ManualAPI.BindKeyframe(this, true);
    }
    void ChangeValueScale()
    {
        if (ValueScale == ValueScaleType.Decimal)
        {
            var binding = new Binding("Value") //deja vu, deja fucking vu
            {
                Source = this,
                StringFormat = "{0:0.00}"
            };
            valueTbox.SetBinding(TextBox.TextProperty, binding);
        }

        else if (ValueScale == ValueScaleType.Percentage)
        {
            var binding = new Binding("Value")
            {
                Source = this,
                StringFormat = "{0:p0}"
            };
            valueTbox.SetBinding(TextBox.TextProperty, binding);
        }
        else if (ValueScale == ValueScaleType.Default)
        {
            var binding = new Binding("Value")
            {
                Source = this,
                StringFormat = "F0"
            };
            valueTbox.SetBinding(TextBox.TextProperty, binding);
        }

    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      //  ChangeValueScale();
    }

    private void valueSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
       // MessageBox.Show("hola");
    }




    public static M_SliderBox StrengthSlider()
    {
        return new M_SliderBox("Strength", 0, 1, 2000, 0.01, true);
    }
}
