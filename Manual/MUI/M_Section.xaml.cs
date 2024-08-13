using Manual.Core;
using Manual.Core.Nodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
/// Lógica de interacción para M_Section.xaml
/// </summary>
public partial class M_Section : UserControl, IManualElement
{
    public IManualElement Clone()
    {
        var clone = new M_Section();
        AppModel.CopyBinding(clone, this, HeaderProperty);
        AppModel.CopyBinding(clone, this, IsCheckedProperty);
        AppModel.CopyBinding(clone, this, EnableCheckProperty);

       // AppModel.CopyBinding(clone, this, DataContextProperty);
        // AppModel.CopyBinding(clone.InnerContent, this.InnerContent, DataContextProperty);
      //  clone.DataContext = DataContext;
      
    //    clone.InnerContent.DataContext = this.InnerContent.DataContext;

        foreach (var item in InnerContent.Items)
        {
            if (item is IManualElement element)
                clone.Add(element.Clone());
        }

        return clone;
    }
    public void InitializeBind(Binding bind)
    {
        SetBinding(DataContextProperty, bind);
    }





    public static readonly DependencyProperty EnableCheckProperty = DependencyProperty.Register(
    nameof(EnableCheck), typeof(bool), typeof(M_Section), new PropertyMetadata(false));

    public bool EnableCheck
    {
        get { return (bool)GetValue(EnableCheckProperty); }
        set { SetValue(EnableCheckProperty, value); }
    }

    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked), typeof(bool), typeof(M_Section), new FrameworkPropertyMetadata(
         true,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }
    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        M_Section section = d as M_Section;
        if (section != null)
        {
            bool newValue = (bool)e.NewValue;
            section.IsCheckedChanged(newValue);
        }
    }
    public void IsCheckedChanged(bool value)
    {
        double op = 1;
        if (!value)
            op = 0.33;

        stackPanelCont.Opacity = op;
        header.Opacity = op;

    }


    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(M_Section), new PropertyMetadata(string.Empty));

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(M_Section), new PropertyMetadata(true));

    public bool IsExpanded
    {
        get { return (bool)GetValue(IsExpandedProperty); }
        set { SetValue(IsExpandedProperty, value); }
    }

    public ItemsControl InnerContent
    {
        get { return (ItemsControl)GetValue(InnerContentProperty); }
        set { SetValue(InnerContentProperty, value); }
    }

    // Using a DependencyProperty as the backing store for InnerContent.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty InnerContentProperty =
        DependencyProperty.Register(nameof(InnerContent), typeof(ItemsControl), typeof(M_Section), new UIPropertyMetadata(null));


    public M_Section()
    {
        InitializeComponent();
        InnerContent = new ItemsControl();
       
        if (IsChecked == false)
            IsCheckedChanged(false);

        DataContextChanged += M_Section_DataContextChanged;
    }

    public M_Section(string header, params IManualElement[] elements)
    {
        InitializeComponent();
        InnerContent = new ItemsControl();
        InnerContent.DataContext = DataContext;

        AddRange(elements);

        Header = header;
        // EnableCheck = enableCheck;

        //    if (binding != "")
        //      SetBinding(M_Section.IsCheckedProperty, binding);

        DataContextChanged += M_Section_DataContextChanged;
    }

    private void M_Section_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
       // InnerContent.DataContext = e.NewValue;
        foreach (FrameworkElement item in InnerContent.Items)
        {
         //   item.DataContext = DataContext;
        }
    }

    public M_Section(StackPanel stackPanelParent, string header = "section", bool enableCheck = false, string binding = "")
    {
        InitializeComponent();
        InnerContent = new ItemsControl();

        stackPanelParent.Children.Add(this);
        Header = header;
        EnableCheck = enableCheck;

        if (binding != "")
            SetBinding(M_Section.IsCheckedProperty, binding);
    }
    public M_Section(string header, ItemsControl itemsControl, bool enableCheck = false, string binding = "")
    {
        InitializeComponent();
        InnerContent = new ItemsControl();

        itemsControl.Items.Add(this);
        Header = header;
        EnableCheck = enableCheck;

        if (binding != "")
            SetBinding(M_Section.IsCheckedProperty, binding);
    }

    public void BindIsChecked(string binding)
    {
        SetBinding(M_Section.IsCheckedProperty, binding);
    }

    public void Add(IManualElement element)
    {
        var el = (FrameworkElement)element;
        el.Margin = new Thickness(3, 3, 3, 3);
        InnerContent.Items.Add((FrameworkElement)element);
    }
    public void AddRange(IManualElement[] elements)
    {
        foreach (IManualElement element in elements)
        {
            Add(element);
        }
    }



    public static M_Section VisualizeNode(string nodeName, string nodeOptionName)
    {
        return new M_Section(nodeOptionName, M_NodeOptionVisualizerView.WrapInGridContext(nodeName, nodeOptionName));
    }
    public static M_Section VisualizeNodeOutput()
    {
        return new M_Section("Output", M_NodeOptionVisualizerView.WrapInGridContextOutput());
    }
}

