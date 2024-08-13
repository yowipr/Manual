using Manual.API;
using Manual.Core;
using ManualToolkit;
using ManualToolkit.Generic;
using ManualToolkit.Themes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.MUI;
public class ValueChangedEventArgs : EventArgs
{
    public double OldValue { get; }
    public double NewValue { get; }

    public ValueChangedEventArgs(double oldValue, double newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public interface IManualElement
{
    public void InitializeBind(Binding bind);
    public IManualElement Clone();
}


/// <summary>
/// Lógica de interacción para M_NumberBox.xaml
/// </summary>
public partial class M_NumberBox : UserControl, IManualElement
{
    public static readonly DependencyProperty IsUndoProperty =
          DependencyProperty.Register(nameof(IsUndo), typeof(bool), typeof(M_NumberBox),
              new PropertyMetadata(true));

    public bool IsUndo
    {
        get { return (bool)GetValue(IsUndoProperty); }
        set { SetValue(IsUndoProperty, value); }
    }

    public static readonly DependencyProperty IsRenderUpdateProperty =
        DependencyProperty.Register(nameof(IsRenderUpdate), typeof(bool), typeof(M_NumberBox),
            new PropertyMetadata(true));

    public bool IsRenderUpdate
    {
        get { return (bool)GetValue(IsRenderUpdateProperty); }
        set { SetValue(IsRenderUpdateProperty, value); }
    }



    public IManualElement Clone()
    {
        var clone =  new M_NumberBox(Header, Jumps, Jump, IsLimited, Minimum, Maximum);
    
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


    string SelectedBrush = "bg_full";
    string NormalBrush = "bg3";
    void ChangeBgColor(string brush)
    {
        this.SetResourceReference(ForegroundProperty, brush);
    }



    #region Value Property
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
     nameof(Value),
     typeof(double),
     typeof(M_NumberBox),
     new FrameworkPropertyMetadata(
         1d,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue, 
         true,
         UpdateSourceTrigger.PropertyChanged));
    #endregion
    #region Jumps Property
    public static readonly DependencyProperty JumpsProperty =
DependencyProperty.Register(nameof(Jumps), typeof(double), typeof(M_NumberBox), new PropertyMetadata(100d));
    #endregion
    #region Header Property
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
     nameof(Header),
     typeof(string),
     typeof(M_NumberBox),
     new PropertyMetadata(""));
    #endregion
    public double Value
    {
        get { return (double)GetValue(ValueProperty); }
        set
        {
            double roundedValue = Math.Round(value, 2); // Redondea a 2 decimales
            SetValue(ValueProperty, roundedValue);
        }
    }
    public event EventHandler<ValueChangedEventArgs> ValueChanged;
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as M_NumberBox;
        control?.OnValueChanged((double)e.OldValue, (double)e.NewValue);
    }
    // Método que dispara el evento ValueChanged
    protected virtual void OnValueChanged(double oldValue, double newValue)
    {
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));
    }


    public double Jumps
    {
        get { return (double)GetValue(JumpsProperty); }
        set { SetValue(JumpsProperty, value); }
    }
    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }


    public static readonly DependencyProperty TransparentBgProperty =
        DependencyProperty.Register(
            "TransparentBg",
            typeof(bool),
            typeof(M_NumberBox),
            new PropertyMetadata(false));

    public bool TransparentBg
    {
        get { return (bool)GetValue(TransparentBgProperty); }
        set { SetValue(TransparentBgProperty, value); }
    }

    public static readonly DependencyProperty JumpProperty = DependencyProperty.Register(
       nameof(Jump), typeof(double), typeof(M_NumberBox), new PropertyMetadata(1d));

    public double Jump
    {
        get { return (double)GetValue(JumpProperty); }
        set { SetValue(JumpProperty, value); }
    }

    #region Maximun Property
    public static readonly DependencyProperty MaximumProperty =
DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(M_NumberBox), new PropertyMetadata(100.0d));

    #endregion
    #region Minimun Property
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(M_NumberBox), new PropertyMetadata(1.0d));
    #endregion
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

    public static readonly DependencyProperty IsLimitedProperty =
    DependencyProperty.Register(nameof(IsLimited), typeof(bool), typeof(M_NumberBox), new PropertyMetadata(false));

    public bool IsLimited
    {
        get { return (bool)GetValue(IsLimitedProperty); }
        set { SetValue(IsLimitedProperty, value); }
    }




    public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
    nameof(StringFormat),
    typeof(string),
    typeof(M_NumberBox), // Reemplaza 'TuUserControl' con el nombre de tu UserControl
    new PropertyMetadata("", OnStringFormatChanged));

    public string StringFormat
    {
        get => (string)GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }
    private static void OnStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as M_NumberBox;
        control?.ApplyStringFormat();
    }

    private void ApplyStringFormat()
    {
        //var binding = new Binding("Value")
        //{
        //    Source = this, // Asegúrate de ajustar la fuente del Binding según tu contexto
        //};
        //if (StringFormat != "")
        //    binding.StringFormat = StringFormat;


        //textBox.SetBinding(TextBox.TextProperty, binding);   
    }


    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
 
    public M_NumberBox()
    {
        InitializeComponent();
    }
    //public M_NumberBox(string displayName, double jumps = 100, double jump = 1, bool limited = false, double minimum = 0, double maximum = double.NaN)
    //{
    //    InitializeComponent();


    //    Header = displayName;

    //    Jumps = jumps;
    //    Jump = jump;
    //    IsLimited = limited;
    //    Minimum = minimum;

    //    if (double.IsNaN(maximum))
    //        Maximum = double.MaxValue; 
    //    else
    //        Maximum = maximum;
        
    //}
    public M_NumberBox(string displayName, string bindingName, double jumps = 100, double jump = 1, bool limited = false, double minimum = 0, double maximum = double.NaN)
    {
        InitializeComponent();

        InitializeBind(new Binding(bindingName));
        Header = displayName;

        Jumps = jumps;
        Jump = jump;
        IsLimited = limited;
        Minimum = minimum;

        if (double.IsNaN(maximum))
            Maximum = double.MaxValue;
        else
            Maximum = maximum;

    }
    public M_NumberBox(string bindingName, double jumps = 100, double jump = 1, bool limited = false, double minimum = 0, double maximum = double.NaN)
    {
        InitializeComponent();

        if(!string.IsNullOrEmpty(bindingName))
           InitializeBind(new Binding(bindingName));

        Header = bindingName;

        Jumps = jumps;
        Jump = jump;
        IsLimited = limited;
        Minimum = minimum;

        if (double.IsNaN(maximum))
            Maximum = double.MaxValue;
        else
            Maximum = maximum;

    }


    private Point startPoint;
    private object initialValue;
   // private double jumpings = 0.5;
   
 //  public int Jumps = 100; // 100 steps

    private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(textBox);
       
        initialValue = Value;  
        textBox.CaptureMouse();
    }
    bool isDragging;

    private void TextBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (textBox.IsMouseCaptured)
        {
            if (Shortcuts.IsMouseDraggable)
                mousedragable = true;

            double newValue;
            if (!IsLimited)
            {
                Point currentPoint = e.GetPosition(textBox);
                double distance = currentPoint.X - startPoint.X;

                double increment = Math.Round(((double)distance * Jumps / 50) * Jump) * Jump;
                 newValue = Convert.ToDouble(initialValue) + increment;
            }
            else
            {
                Point currentPoint = e.GetPosition(textBox);  // Obtiene la posición relativa al textBox

                double distance = currentPoint.X - startPoint.X;
                double proportionalChange = (distance / textBox.ActualWidth) * (Maximum - Minimum);
                newValue = Convert.ToDouble(initialValue) + proportionalChange;

                // Ajustar newValue para que sea múltiplo de Jump
                if (Jump > 0)
                {
                    newValue = Math.Round(newValue / Jump) * Jump;
                }

                // Asegurar que newValue esté dentro de los límites permitidos
                newValue = Math.Max(Math.Min(newValue, Maximum), Minimum);


            }


            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) //SNAP
            {
                double roundness = 1;
                var dist = Math.Abs(Maximum - Minimum);
                if (dist == 1 || (dist >= 0.9 && dist <= 1.1))
                    roundness = 0.05;
                else if (dist > 1_000)
                    roundness = 8;

                newValue = newValue.Round(roundness);
            }

            var m = Math.Abs(Value - newValue);
            if (m > 0.00001)
            {
                mousedragable = true;
                Value = newValue;
                if (IsRenderUpdate)
                    Shot.UpdateCurrentRender();
                
            }
        }

    }


    bool mousedragable;
    private void TextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDragging = false;
        textBox.ReleaseMouseCapture();

        //clicked
        if (!mousedragable)
        {
            selected = true;
            textBox.Focusable = true;
            textBox.Focus();
        }
        else
        {
            ApplyChanges();
        }
    }

    bool selected;

    bool isInLimits(double value) => Maximum > value && value > Minimum;
    private void IncreaseButton_Click(object sender, RoutedEventArgs e)
    {
        StartUpdateAction();

        var value = Convert.ToDouble(Value);
        if (IsLimited && isInLimits(value))
        {
            if (value < Maximum)
                Value = value + Jump;
        }
        else
        {
            Value = value + Jump;
        }
        if (IsUndo)
            ActionHistory.FinalizeAction();
    }

    private void DecreaseButton_Click(object sender, RoutedEventArgs e)
    {
        StartUpdateAction();


        var value = Convert.ToDouble(Value);
        if (IsLimited && isInLimits(value))
        {
            if (value > Minimum)
            Value = value - Jump;
        }
        else
        {
            Value = value - Jump;
        }
        if (IsUndo)
            ActionHistory.FinalizeAction();
    }




    void StartUpdateAction()
    {
        if (IsUndo)
            ActionHistory.StartAction(this, nameof(Value));

        if (IsRenderUpdate)
        {
            BindingExpression binding = this.GetBindingExpression(ValueProperty);
            if (binding != null)
            {
                var dataItem = binding.DataItem;
                var bindingPath = binding.ParentBinding.Path.Path;

                if (dataItem is IAnimable a)
                    ManualAPI.Animation.NotifyActionStartChanging(a, bindingPath);

                //M_SLIDERBOX
                else if (dataItem is M_SliderBox s)
                {
                    BindingExpression binding2 = s.GetBindingExpression(M_SliderBox.ValueProperty);
                    if (binding2 != null)
                    {
                        dataItem = binding2.DataItem;
                        bindingPath = binding2.ParentBinding.Path.Path;

                        if (dataItem is IAnimable a2)
                            ManualAPI.Animation.NotifyActionStartChanging(a2, bindingPath);
                    }
                }
            }

        }

    }

    //----------------------------------------------------------------------------- START
    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (selected)
           return;

        e.Handled = true;

        textBox.Focus();
        textBox.SelectAll();

        if (!TransparentBg)
            ChangeBgColor(SelectedBrush);


        selected = true;

        oldValue = Value;

        StartUpdateAction();

    }
    double oldValue;
    private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            ConfirmChange();
        }
    }

    private void textBox_MouseEnter(object sender, MouseEventArgs e)
    {
        Cursor = Cursors.ScrollWE;
    }
    private void textBox_MouseLeave(object sender, MouseEventArgs e)
    {
        Cursor = null;
        Mouse.OverrideCursor = null;
    }
    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
       
        left.Visibility = Visibility.Visible;
        right.Visibility = Visibility.Visible;
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
      
        left.Visibility = Visibility.Hidden;
        right.Visibility = Visibility.Hidden;

       
        // ConfirmChange();
    }


    void ConfirmChange()
    {
      
        Keyboard.ClearFocus();

        if (!TransparentBg)
             ChangeBgColor(NormalBrush);

        // Value = textBox.Text;
        string text = textBox.Text;

        // Verificar si el texto tiene letras
        if (text.Any(c => !Char.IsDigit(c)))
        {
            // Remover los caracteres que no son dígitos, signo negativo o separador decimal
            string cleanedText = new string(text.Where(c => Char.IsDigit(c) || c == '-' || c == ',' || c == '.'
            || c == '+' || c == '*' || c == '/' || c == 'x' || c == '(' || c == ')' || c == '^').ToArray());

            // Reemplazar "," por "." si es necesario
            cleanedText = cleanedText.Replace(",", ".");

            Value = double.Parse( OperateString(cleanedText) );

            // Establecer el índice del cursor al final del texto
          //  textBox.CaretIndex = Value.Length;
        }
        else
        {
            Value = double.Parse(text);
        }
        if (textBox.Text == "")
            Value = 0d;


        if(oldValue != Value && IsUndo)
             ActionHistory.FinalizeAction();

        if (IsRenderUpdate)
            Shot.UpdateCurrentRender();
    }

    public string OperateString(string text)
    {
        DataTable dt = new DataTable();
        var result = dt.Compute(text, "");
        return result.ToString();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Header != "")
        {
          
            head.Visibility = Visibility.Visible;
            textBox.Padding = new Thickness(0, 0, -50, 0);
        }
    }

    private void textBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyChanges();
    }

    void ApplyChanges()
    {
        mousedragable = false;

        selected = false;
        textBox.Focusable = false;

        if (!TransparentBg)
            ChangeBgColor(NormalBrush);


        // ConfirmChange();

        if (IsUndo)
        {
            if (oldValue != Value)
                ActionHistory.FinalizeAction();
            else
                ActionHistory.CancelAction();
        }
    }

  
}




public class M_Button : Button, IManualElement
{
    public IManualElement Clone()
    {
        return new M_Button(Content.ToString(), click);
    }
    public void InitializeBind(Binding bind)
    {
        SetBinding(ContentProperty, bind);
    }

    //static M_Button()
    //{
    //    DefaultStyleKeyProperty.OverrideMetadata(typeof(M_Button),
    //     new FrameworkPropertyMetadata(typeof(Button)));
    //}


    public M_Button()
    {
        
    }

    public M_Button(string content)
    {

        Content = content;
    }
    public M_Button(string content, Action onClick)
    {
        
        Content = content;
        click = onClick;
        Click += M_Button_Click;
    }
    Action click;
    private void M_Button_Click(object sender, RoutedEventArgs e)
    {
        click?.Invoke();  
    }
}

public class M_Separator : Separator, IManualElement
{
    public IManualElement Clone()
    {
        return new M_Separator();
    }

    public void InitializeBind(Binding bind)
    {

    }

    public M_Separator()
    {
            
    }


}