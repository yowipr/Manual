using ColorPicker.Models;
using ColorPicker;
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
using Manual.Core;
using Manual.API;

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para M_ColorPicker.xaml
/// </summary>
public partial class M_ColorPicker : UserControl, IManualElement
{
    public void InitializeBind(Binding bind)
    {
        throw new NotImplementedException();
    }

    public IManualElement Clone()
    {
        throw new NotImplementedException();
    }




    // Define la propiedad de dependencia
    public static readonly DependencyProperty IsUpdateRenderProperty =
        DependencyProperty.Register("IsUpdateRender", typeof(bool), typeof(M_ColorPicker), new PropertyMetadata(true));

    // Propiedad wrapper para acceder a la propiedad de dependencia
    public bool IsUpdateRender
    {
        get { return (bool)GetValue(IsUpdateRenderProperty); }
        set { SetValue(IsUpdateRenderProperty, value); }
    }



    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Aquí puedes agregar lógica para responder al cambio de valor
       if(e.Property == SelectedColorProperty && ((M_ColorPicker)d).IsUpdateRender)
        Shot.UpdateCurrentRender();
    }

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(Color), typeof(M_ColorPicker),
             new FrameworkPropertyMetadata(
     Colors.Black,
     FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
     OnValueChanged,
     CoerceValue,
     true,
     UpdateSourceTrigger.PropertyChanged)
    );

    public Color SelectedColor
    {
        get =>(Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }




    public static readonly DependencyProperty SecondaryColorProperty = DependencyProperty.Register("SecondaryColor", typeof(Color), typeof(M_ColorPicker),
             new FrameworkPropertyMetadata(
     Colors.White,
     FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
     OnValueChanged,
     CoerceValue,
     true,
     UpdateSourceTrigger.PropertyChanged)
    );

    public Color SecondaryColor
    {
        get => (Color)GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }


    public M_ColorPicker()
    {
        InitializeComponent();
    }
    public M_ColorPicker(string selectedColorBinding)
    {
        InitializeComponent();
        SetBinding(M_ColorPicker.SelectedColorProperty, selectedColorBinding);
    }
    private void main_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var colorPicker = sender as SquarePicker;
      //  var project = colorPicker.DataContext as Project;
        if (colorPicker != null)
        {
            // Obtiene la BindingExpression asociada a la propiedad SelectedColor
            //  BindingExpression be = colorPicker.GetBindingExpression(SquarePicker.SelectedColorProperty);
           //BindingExpression be = this.GetBindingExpression(M_ColorPicker.SelectedColorProperty);

            // Actualiza el origen del enlace manualmente
           // ManualAPI.SelectedBrush.isUpdate = true;
           // be.UpdateSource();
           
        }
    }

    private void ColorDisplay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (eraserCheck.IsChecked == true)
            eraserCheck.IsChecked = false;
        
    }
}
