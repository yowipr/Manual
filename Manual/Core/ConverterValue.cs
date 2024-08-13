using Manual.API;
using Manual.Editors;
using Manual.MUI;
using Manual.Objects;
using ManualToolkit.Themes;
using Microsoft.Xaml.Behaviors;
using Silk.NET.Maths;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Manual.Core;

public class ConverterLayerIndex : IValueConverter //DEPRECATED: no se usa parece
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Layer layer && parameter is ObservableCollection<Layer> layers)
        {
            int index = layers.IndexOf(layer);
            int count = layers.Count;
            return (count - index).ToString();
        }
        return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class PointNormalizerConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Point point)
        {
            // Normaliza el punto basándose en el valor de referencia 'basePoint'
            return point.Normalize();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}




public class ColorMixerMultiConverter : IMultiValueConverter
{

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Binding.DoNothing;



        Debug.WriteLine("-.--------------------------------------------------------------------------------------------------------------------------");

        Debug.WriteLine(values);

        Color color1 = GetColorFromObject(values[0]);
        Color color2 = GetColorFromObject(values[1]);

        Debug.WriteLine(color1);
        Debug.WriteLine(color2);

        if (color1 == default(Color) || color2 == default(Color))
            return Binding.DoNothing;

        if (parameter is string factorString && double.TryParse(factorString, NumberStyles.Float, CultureInfo.InvariantCulture, out double factor))
        {
            var color = Renderizainador.BlendColor(color1, color2, factor);
            Debug.WriteLine(color);
            return color;
        }
        else
        {
            return Binding.DoNothing;
        }
    }

    private Color GetColorFromObject(object value)
    {
        if (value is Color color)
        {
            return color;
        }
        else if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }

        return default(Color);
    }


    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class ImageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var threshold = 0.3f;
        if (value is WriteableBitmap bitmap)
        {
            // Supongamos que GetAverageColor es un método de extensión que has implementado en tu clase WriteableBitmapExtensions.
            Color averageColor = bitmap.GetAverageColor(threshold); // Utiliza una densidad de muestreo para mejorar el rendimiento.
            return new SolidColorBrush(averageColor);
        }
        else if (value is byte[] bytes)
        {
            Color averageColor = bytes.GetAverageColor(threshold);
            return new SolidColorBrush(averageColor);
        }

        return Binding.DoNothing; // Retorna esto si el valor de entrada no es un WriteableBitmap.
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("La conversión inversa no es soportada.");
    }
}





public class SKBitmapToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SKBitmap skBitmap)
        {
           // Output.Log("image converter sk");
            return skBitmap.ToWriteableBitmap();
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}




public class DisplayPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        if (parameter is M_ComboBox c && !string.IsNullOrEmpty(c.DisplayMemberPath))
        {
            var propertyInfo = value.GetType().GetProperty(c.DisplayMemberPath);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(value);
            }
        }
        return value.ToString(); // Default to ToString if no DisplayMemberPath or property not found
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}























//--------------------------------------------------------------------------------------------------------------------------Interactivity

public class WidthBasedVisibilityBehavior : Behavior<Panel>
{
    public static readonly DependencyProperty WidthLimitProperty =
        DependencyProperty.Register(
            "WidthLimit",
            typeof(double),
            typeof(WidthBasedVisibilityBehavior),
            new PropertyMetadata(600.0));

    public static readonly DependencyProperty ParentLevelProperty =
        DependencyProperty.Register(
            "ParentLevel",
            typeof(int),
            typeof(WidthBasedVisibilityBehavior),
            new PropertyMetadata(1, OnParentLevelChanged));

    public double WidthLimit
    {
        get { return (double)GetValue(WidthLimitProperty); }
        set { SetValue(WidthLimitProperty, value); }
    }

    public int ParentLevel
    {
        get { return (int)GetValue(ParentLevelProperty); }
        set { SetValue(ParentLevelProperty, value); }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AttachToParent(AssociatedObject);
    }

    protected override void OnDetaching()
    {
        DetachFromParent(AssociatedObject);
        base.OnDetaching();
    }

    private static void OnParentLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidthBasedVisibilityBehavior behavior && behavior.AssociatedObject != null)
        {
            behavior.DetachFromParent(behavior.AssociatedObject);
            behavior.AttachToParent(behavior.AssociatedObject);
        }
    }

    private void AttachToParent(DependencyObject target)
    {
        var parent = GetDesiredParent(target);
        if (parent != null)
        {
            parent.SizeChanged += OnParentSizeChanged;
        }
    }

    private void DetachFromParent(DependencyObject target)
    {
        var parent = GetDesiredParent(target);
        if (parent != null)
        {
            parent.SizeChanged -= OnParentSizeChanged;
        }
    }

    private FrameworkElement GetDesiredParent(DependencyObject target)
    {
        var current = target;
        for (int i = 0; i < ParentLevel && current != null; i++)
        {
            current = VisualTreeHelper.GetParent(current);
        }
        return current as FrameworkElement;
    }

    private void OnParentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement parent)
        {
            var grid = AssociatedObject;
            grid.Visibility = parent.ActualWidth < WidthLimit ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}




public class CanvasFixedSizeBehaviour : Behavior<CanvasMatrix>
{

    protected override void OnAttached()
    {
        if (AssociatedObject.Parent is FrameworkElement parent)
        {
            AssociatedObject.Loaded += Canvas_Loaded;
        }
    }

  

    protected override void OnDetaching()
    {
        if (AssociatedObject.Parent is FrameworkElement parent)
        {
            AssociatedObject.Loaded -= Canvas_Loaded;
            AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
            parent.SizeChanged -= OnParentSizeChanged;
        }
    }


    private void Canvas_Loaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;

        //AppModel.InvokeNext(() =>
        //{
        //    var grid = AssociatedObject.Parent as FrameworkElement;
        //    var canvas = AssociatedObject;
        //    var ed = AppModel.FindAncestorByDataContext<ICanvasMatrix>(grid);
        //    ed.GridWidth = grid.ActualWidth;
        //    ed.GridHeight = grid.ActualHeight;

        //    if (!ed.loaded)
        //    {
        //        ed.DefaultPosition();
        //        ed.loaded = true;
        //    }
        //});


        //SetStart();

    }

    void SetStart()
    {

        AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;

        var grid = AssociatedObject.Parent as FrameworkElement;
        var canvas = AssociatedObject;
        var ed = AppModel.FindAncestorByDataContext<ICanvasMatrix>(grid);

     
        //var eW = ed.GridWidth;
        //var eH = ed.GridHeight;

        ////first time, ignore
        //if (eW == 0 || eH == 0)
        //    return;

        ed.GridWidth = grid.ActualWidth;
        ed.GridHeight = grid.ActualHeight;

        if (!ed.loaded)
        {
            var oldSize = new Size(ed.GridWidth, ed.GridHeight);
            var newSize = new Size(grid.ActualWidth, grid.ActualHeight);

            if (oldSize.Width == 0 || oldSize.Height == 0)
            {

                // ed.CenterCamera(gridNewSize);

                //save grid
                ed.GridWidth = grid.ActualWidth;
                ed.GridHeight = grid.ActualHeight;

                ed.DefaultPosition();
                canvas.CanvasTransform = ed.CanvasMatrix;
            }
            else
            {
                SetScale(oldSize, newSize, canvas, ed.CanvasMatrix);
               // OnSizeStart();
            }
            ed.loaded = true;
        }

    }

    private void AssociatedObject_LayoutUpdated(object? sender, EventArgs e)
    {
        if (AssociatedObject.Parent is FrameworkElement parent)
        {
            AssociatedObject.Dispatcher.Invoke(OnSizeStart, DispatcherPriority.Render);
            parent.SizeChanged += OnParentSizeChanged;
            AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
        }
    }

    //recalculate size
    void OnSizeStart()
    {

        var grid = AssociatedObject.Parent as FrameworkElement;
        var canvas = AssociatedObject;

        var ed = AppModel.FindAncestorByDataContext<ICanvasMatrix>(grid);

      //  if (!ed.loaded)
        //    return;

        //if (!ed.loaded)
        //{
        //    ed.GridWidth = grid.ActualWidth;
        //    ed.GridHeight = grid.ActualHeight;


        //    ed.loaded = true;
        //}

        var gridPreviousSize = new Size(ed.GridWidth, ed.GridHeight);
        var gridNewSize = new Size(grid.ActualWidth, grid.ActualHeight);
        if (gridPreviousSize.Width == 0 || gridPreviousSize.Height == 0)
        {

            // ed.CenterCamera(gridNewSize);

            //save grid
            ed.GridWidth = grid.ActualWidth;
            ed.GridHeight = grid.ActualHeight;

            ed.DefaultPosition();
            canvas.CanvasTransform = ed.CanvasMatrix;
            return;
        }

        var matrix = ed.CanvasMatrix;


        //position
        double widthChange = (gridNewSize.Width - gridPreviousSize.Width) / 2;
        double heightChange = (gridNewSize.Height - gridPreviousSize.Height) / 2;


        var transform = new TranslateTransform(widthChange, heightChange);
        var finalT = matrix * transform.Value;

        //scale
        double scaleFactor;
        if (gridNewSize.Width > gridNewSize.Height)
            scaleFactor = gridNewSize.Width / gridPreviousSize.Width;
        else
            scaleFactor = gridNewSize.Height / gridPreviousSize.Height;
  

        Point center = new Point(gridNewSize.Width / 2, gridNewSize.Height / 2);

        Matrix matrixNew = finalT;
        matrixNew.ScaleAt(scaleFactor, scaleFactor, center.X, center.Y);

        // Aplicar la nueva matriz
        canvas.CanvasTransform = matrixNew;
        ed.CanvasMatrix = matrixNew;



    }



    //when grid move
    private void OnParentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var grid = sender as FrameworkElement;
        var canvas = AssociatedObject;

        if (grid == null && canvas == null)
            return;


        var ed = AppModel.FindAncestorByDataContext<ICanvasMatrix>(grid);

        var eW = ed.GridWidth;
        var eH = ed.GridHeight;
        //save changes
        ed.GridWidth = grid.ActualWidth;
        ed.GridHeight = grid.ActualHeight;
        //first time, ignore
        if (eW == 0 || eH == 0)
            return;

        var PreviousSize = e.PreviousSize;
        var NewSize = e.NewSize;


        // Calcula el cambio en el tamaño.
        double widthChange = (NewSize.Width - PreviousSize.Width) / 2;
        double heightChange = (NewSize.Height - PreviousSize.Height) / 2;


        var transform = new TranslateTransform(widthChange, heightChange);

        var finalT = canvas.CanvasTransform * transform.Value;
        double scaleFactor;
        Point center = new Point(NewSize.Width / 2, NewSize.Height / 2);


        if (NewSize.Width > NewSize.Height)
            scaleFactor = NewSize.Width / PreviousSize.Width;
        else
            scaleFactor = NewSize.Height / PreviousSize.Height;

        if (!double.IsInfinity(scaleFactor)) // width & height 0, divided by 0
            finalT.ScaleAt(scaleFactor, scaleFactor, center.X, center.Y);


        canvas.CanvasTransform = finalT;

    }

    void SetScale(Size PreviousSize, Size NewSize, CanvasMatrix canvas, Matrix oldCanvasTransform)
    {

        // Calcula el cambio en el tamaño.
        double widthChange = (NewSize.Width - PreviousSize.Width) / 2;
        double heightChange = (NewSize.Height - PreviousSize.Height) / 2;


        var transform = new TranslateTransform(widthChange, heightChange);

        var finalT = oldCanvasTransform * transform.Value;
        double scaleFactor;
        Point center = new Point(NewSize.Width / 2, NewSize.Height / 2);


        if (NewSize.Width > NewSize.Height)
            scaleFactor = NewSize.Width / PreviousSize.Width;
        else
            scaleFactor = NewSize.Height / PreviousSize.Height;

        if (!double.IsInfinity(scaleFactor)) // width & height 0, divided by 0
            finalT.ScaleAt(scaleFactor, scaleFactor, center.X, center.Y);


        canvas.CanvasTransform = finalT;
    }

}





public class GlowEffectBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty VisibleOnEnableProperty = DependencyProperty.Register(
    nameof(VisibleOnEnable),
    typeof(bool),
    typeof(GlowEffectBehavior),
    new PropertyMetadata(true, OnVisibleOnEnableChanged));

    public bool VisibleOnEnable
    {
        get { return (bool)GetValue(VisibleOnEnableProperty); }
        set { SetValue(VisibleOnEnableProperty, value); }
    }

    private static void OnVisibleOnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as GlowEffectBehavior;
        if (control != null)
        {
            control.UpdateVisible();
        }
    }


    protected override void OnAttached()
    {
        base.OnAttached();
       Settings.EnableGlowChanged += Settings_EnableGlowChanged;

        UpdateVisible();
    }
    protected override void OnDetaching()
    {
        base.OnDetaching();
        Settings.EnableGlowChanged -= Settings_EnableGlowChanged;
    }




    private void Settings_EnableGlowChanged(bool value)
    {
        UpdateVisible();      
    }


    void UpdateVisible()
    {
        if (AssociatedObject is FrameworkElement element)
        {
            if (VisibleOnEnable)
                element.Visibility = AppModel.settings.EnableGlow ? Visibility.Visible : Visibility.Collapsed;
            else
                element.Visibility = AppModel.settings.EnableGlow ? Visibility.Collapsed : Visibility.Visible;
        }
    }


}



public class Animation_MouseOverBehaviour : Behavior<UIElement>
{
    UIElement Element { get; set; }
    DependencyProperty dp { get; set; } = UIElement.OpacityProperty;
    double FocusValue { get; set; } = 0.2;
    double UnFocusValue { get; set; } = 0;
    TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.2);
    UIElement ElementParent { get; set; }

    protected override void OnAttached()
    {
        var element = AssociatedObject;
        Element = element;
       // ElementParent = subscribeTo;

        ElementParent.MouseEnter += Element_MouseEnter;
        ElementParent.MouseLeave += Element_MouseLeave;
    }
    protected override void OnDetaching()
    {
        if (ElementParent == null)
        {
            Element.MouseEnter -= Element_MouseEnter;
            Element.MouseLeave -= Element_MouseLeave;
        }
        else
        {
            ElementParent.MouseEnter -= Element_MouseEnter;
            ElementParent.MouseLeave -= Element_MouseLeave;
        }
    }

    public void SetValues(double focusValue, double unFocusValue)
    {
        FocusValue = focusValue;
        UnFocusValue = unFocusValue;
    }


    private void Element_MouseEnter(object sender, MouseEventArgs e)
    {
        if (ManualAPI.settings.EnableAnimations)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = UnFocusValue,
                To = FocusValue,
                Duration = this.Duration
            };

            Element.BeginAnimation(dp, animation);
        }
        else
        {
            Element.SetValue(dp, FocusValue);
        }

    }

    private void Element_MouseLeave(object sender, MouseEventArgs e)
    {
        if (ManualAPI.settings.EnableAnimations)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                From = FocusValue,
                To = UnFocusValue,
                Duration = this.Duration
            };

            Element.BeginAnimation(dp, animation);
        }
        else
        {
            Element.SetValue(dp, UnFocusValue);
        }

    }


}




public class ProgressBarAnimationBehavior : Behavior<ProgressBar>
{
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register("Progress", typeof(double), typeof(ProgressBarAnimationBehavior),
            new PropertyMetadata(0.0, OnProgressChanged));

    public double Progress
    {
        get { return (double)GetValue(ProgressProperty); }
        set { SetValue(ProgressProperty, value); }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressBarAnimationBehavior behavior && behavior.AssociatedObject != null)
        {
            behavior.AnimateProgress((double)e.OldValue, (double)e.NewValue);
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        // Asegurarse de que la animación refleje el valor inicial correcto
        AssociatedObject.Value = Progress;
    }

    private void AnimateProgress(double from, double to)
    {
        DoubleAnimation animation = new DoubleAnimation
        {
            //From = from,
            To = to,
            Duration = TimeSpan.FromSeconds(0.6),
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        animation.Completed += (s, e) => AssociatedObject.Value = to;
        AssociatedObject.BeginAnimation(ProgressBar.ValueProperty, animation);
    }
}


public class VisibilityBoolAnimationBehavior : Behavior<UIElement>
{  
    // Define la DependencyProperty
    public static readonly DependencyProperty ToOpacityProperty = DependencyProperty.Register(
        nameof(ToOpacity),
        typeof(double),
        typeof(VisibilityBoolAnimationBehavior),
        new PropertyMetadata(1.0)
    );

    // Propiedad CLR para acceder a la DependencyProperty
    public double ToOpacity
    {
        get => (double)GetValue(ToOpacityProperty);
        set => SetValue(ToOpacityProperty, value);
    }

    public static readonly DependencyProperty IsVisibleProperty =
        DependencyProperty.Register("IsVisible", typeof(bool), typeof(VisibilityBoolAnimationBehavior),
            new PropertyMetadata(false, OnIsVisibleChanged));

    public bool IsVisible
    {
        get { return (bool)GetValue(IsVisibleProperty); }
        set { SetValue(IsVisibleProperty, value); }
    }

    private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VisibilityBoolAnimationBehavior behavior && behavior.AssociatedObject != null)
        {
            bool newValue = (bool)e.NewValue;
            behavior.AnimateVisibility(newValue);
        }
    }

    private void AnimateVisibility(bool isVisible)
    {
        DoubleAnimation animation = new DoubleAnimation
        {
            To = isVisible ? ToOpacity : 0.0,
            Duration = TimeSpan.FromSeconds(0.6),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (s, e) =>
        {
            if (!isVisible)
            {
                AssociatedObject.Visibility = Visibility.Collapsed;
            }
        };

        if (isVisible)
        {
            AssociatedObject.Visibility = Visibility.Visible;
        }

        AssociatedObject.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        // Asegurarse de que la animación refleje el valor inicial correcto
        if (IsVisible)
        {
            AssociatedObject.Visibility = Visibility.Visible;
            AssociatedObject.Opacity = ToOpacity;
        }
        else
        {
            AssociatedObject.Visibility = Visibility.Collapsed;
            AssociatedObject.Opacity = 0.0;
        }
    }
}






//------------------------------------------------------------------------------------------------- MARKUP EXTENSION



// USO   <ComboBox ItemsSource="{Binding Source={local:EnumBindingSourceExtension {x:Type local:MyEnum}}}" />

public class EnumBindingSourceExtension : MarkupExtension
{
    private Type _enumType;

    public Type EnumType
    {
        get { return this._enumType; }
        set
        {
            if (value != this._enumType)
            {
                if (null != value)
                {
                    Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (!enumType.IsEnum)
                        throw new ArgumentException("Tipo debe ser un Enum.");
                }

                this._enumType = value;
            }
        }
    }

    public EnumBindingSourceExtension() { }

    public EnumBindingSourceExtension(Type enumType)
    {
        this.EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (null == this._enumType)
            throw new InvalidOperationException("El tipo de enumeración debe ser especificado.");

        Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
        Array enumValues = actualEnumType.GetEnumValues();

        if (actualEnumType == this._enumType)
            return enumValues;

        Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
        enumValues.CopyTo(tempArray, 1);
        return tempArray;
    }
}

public class StaticResourceColor : MarkupExtension
{
    public StaticResourceColor(string resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public string ResourceKey { get; set; }
    public double Opacity { get; set; } = 1.0;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Application.Current.FindResource(ResourceKey) is SolidColorBrush brush)
        {
            return new SolidColorBrush(brush.Color) { Opacity = this.Opacity };
        }

        return null;
    }
}




public class VisibilityBinding : MarkupExtension
{
    public string Path { get; set; }
    public bool Invert { get; set; } = false;

    public VisibilityBinding(string path)
    {
        Path = path;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(Path)
        {
            Converter = new BoolToVisibilityConverter { Invert = this.Invert }
        };

        return binding.ProvideValue(serviceProvider);
    }
}