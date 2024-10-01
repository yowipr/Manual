using Manual.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// Lógica de interacción para NamerText.xaml
/// </summary>
public partial class NamerText : UserControl
{

    public static readonly DependencyProperty ClickCountProperty = DependencyProperty.Register(
        "ClickCount",
    typeof(int),
        typeof(NamerText),
        new PropertyMetadata(2)); // Valor predeterminado de 2

    public int ClickCount
    {
        get { return (int)GetValue(ClickCountProperty); }
        set { SetValue(ClickCountProperty, value); }
    }



    public static readonly DependencyProperty MemberPathProperty = DependencyProperty.Register(
    "MemberPath",
    typeof(string),
    typeof(NamerText), // Cambia 'TuControl' por el nombre de tu clase de control
    new PropertyMetadata("Name"));

    public string MemberPath
    {
        get { return (string)GetValue(MemberPathProperty); }
        set { SetValue(MemberPathProperty, value); }
    }



    public static readonly DependencyProperty NameTextProperty = DependencyProperty.Register(
nameof(NameText),
typeof(string),
typeof(NamerText),
new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnNameTextChanged));
    public string NameText
    {
        get { return (string)GetValue(NameTextProperty); }
        set { SetValue(NameTextProperty, value); }
    }
    private static void OnNameTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //  var control = (NamerText)d;
      //  control.layerName.Text = (string)e.NewValue;
    }


    public NamerText()
    {
        InitializeComponent();
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if(isEditing == true)
              SaveName();
    }

    private void TextBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveName();
        }
        if (e.Key == Key.Escape)
        {
            isEditing = false;
            e.Handled = true;
            txtBox.Visibility = Visibility.Collapsed;
            txtBlock.Visibility = Visibility.Visible;
        }
    }

    bool isEditing = false;

    public Func<string, string> OnNameChanging;
    void SaveName()
    {
        //  var namable = (INamable)DataContext;
        //   namable.Name = txtBox.Text;


        if (DataContext == null) return;

        // Usa reflexión para obtener la propiedad especificada por MemberPath en el DataContext
        var propertyInfo = DataContext.GetType().GetProperty(MemberPath);
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            try
            {
                // Obtiene el tipo de la propiedad
                Type propertyType = propertyInfo.PropertyType;

                // Intenta convertir el valor de txtBox.Text al tipo de la propiedad
                // Si el tipo de la propiedad es Nullable, obtiene el tipo subyacente.
                Type nonNullableType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                object convertedValue = Convert.ChangeType(txtBox.Text, nonNullableType);

                if(convertedValue is string v)
                  convertedValue = OnNameChanging?.Invoke(v);

                // Asigna el valor convertido a la propiedad
                propertyInfo.SetValue(DataContext, convertedValue, null);
            }
            catch (Exception ex)
            {
                // Maneja la excepción si la conversión falla
                Debug.WriteLine("Error al convertir y asignar el valor: " + ex.Message);
                Output.Log($"invalid value: {txtBox.Text}", "NamerText");
            }
        }




        txtBox.Visibility = Visibility.Collapsed;
        txtBlock.Visibility = Visibility.Visible;
        isEditing = false;
    }

    private void TextBlock_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != ClickCount) //default: 2
            return;

        EditName();
        e.Handled = true;
    }

    public void EditName()
    {
        isEditing = true;
        txtBox.Visibility = Visibility.Visible;
        txtBlock.Visibility = Visibility.Collapsed;

        txtBox.Text = txtBlock.Text;

        //e.Handled = true;

        txtBox.Focus();
        txtBox.SelectAll();
    }






}
