using Manual.API;
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
/// Lógica de interacción para M_PromptBox.xaml
/// </summary>
public partial class M_PromptBox : UserControl, IManualElement
{
    public IManualElement Clone()
    {
        return new M_PromptBox() { Header = this.Header};
    }


    public void InitializeBind(Binding bind)
    {
        SetBinding(M_PromptBox.TextProperty, bind);
    }
    public static readonly DependencyProperty PlaceholderProperty =
     DependencyProperty.Register("Placeholder", typeof(string), typeof(M_PromptBox), new PropertyMetadata("Prompt..."));

    public string Placeholder
    {
        get { return (string)GetValue(PlaceholderProperty); }
        set { SetValue(PlaceholderProperty, value); }
    }


    public static readonly DependencyProperty IsTextEnteredProperty =
        DependencyProperty.Register("IsTextEntered", typeof(bool), typeof(M_PromptBox), new PropertyMetadata(false));

    public bool IsTextEntered
    {
        get { return (bool)GetValue(IsTextEnteredProperty); }
        private set { SetValue(IsTextEnteredProperty, value); }
    }



    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
     nameof(Text),
     typeof(string),
     typeof(M_PromptBox),
    new FrameworkPropertyMetadata(
         default(string),
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        return baseValue;
    }



    // Propiedad pública para acceder a la DependencyProperty
    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }


    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
    nameof(Header),
    typeof(string),
    typeof(M_PromptBox),
    new PropertyMetadata(""));
    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }


    public M_PromptBox()
    {
        InitializeComponent();
    }
    public M_PromptBox(string name, string description, string propertyBinding, bool keyframeIcon = true)
    {
        InitializeComponent();

        SetBinding(TextProperty, propertyBinding);
        ToolTip = name + "\n" + description + "\n" + propertyBinding;

        Header = name;

        if (keyframeIcon)
            ManualAPI.BindKeyframe(this, true);
    }



    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            textBox.Focus();
            e.Handled = true;
        }
    }

    public void FocusText()
    {
        textBox.Focus();
    }

    private void textBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsTextEntered = !string.IsNullOrEmpty(((TextBox)sender).Text);
    }

    public Action OnEnter;
    private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(OnEnter != null && e.Key == Key.Enter)
        {
            OnEnter?.Invoke();
            e.Handled = true;
        }
    }








    private void textBox_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void textBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void textBox_Drop(object sender, DragEventArgs e)
    {
        this.OnDrop(e);
        e.Handled = true;
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
    }

}


