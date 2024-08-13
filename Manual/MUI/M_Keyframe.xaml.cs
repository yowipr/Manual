using Manual.API;
using Manual.Core;
using Manual.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using static Manual.API.ManualAPI;

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para M_Keyframe.xaml
/// </summary>
public partial class M_Keyframe : CheckBox
{

    public M_Keyframe(FrameworkElement elementToBind)
    {
        InitializeComponent();
        ElementToBind = elementToBind;
    }

    private IAnimable Owner { get; set; }
    private string PropertyName { get; set; }



    private void InsertKeyframe_Click(object sender, RoutedEventArgs e)
    {
        InsertKeyframe();
    }

    FrameworkElement ElementToBind;
    
//    bool first = false;
    public void InsertKeyframe()
    {
      //  if(!first)
          SetProperty();

         Keyframe.Insert(Owner, PropertyName);
    }

    private void SetProperty()
    {
        var elementToBind = ElementToBind;
        BindingExpression bindingExpression = null;
        object owner = null;

        // Obtener la expresión de binding
        if (elementToBind.GetBindingExpression(TextBox.TextProperty) != null)
        {
            bindingExpression = elementToBind.GetBindingExpression(TextBox.TextProperty);
            owner = bindingExpression.DataItem;
        }
        else if (elementToBind.GetBindingExpression(M_NumberBox.ValueProperty) != null)
        {
            bindingExpression = elementToBind.GetBindingExpression(M_NumberBox.ValueProperty);
            owner = bindingExpression.DataItem;
        }
        else if (elementToBind.GetBindingExpression(M_SliderBox.ValueProperty) != null)
        {
            bindingExpression = elementToBind.GetBindingExpression(M_SliderBox.ValueProperty);
            owner = bindingExpression.DataItem;
        }
       /* else if (elementToBind.GetBindingExpression(M_ComboBox.SelectedItemProperty) != null)
        {
            bindingExpression = elementToBind.GetBindingExpression(M_ComboBox.SelectedItemProperty);
            owner = bindingExpression.DataItem;
        }*/

        // Obtener el nombre de la propiedad
        string propertyName = bindingExpression?.ResolvedSourcePropertyName;

        if (owner != null)
        {
            // Almacenar el nombre de la propiedad y el objeto propietario
            PropertyName = propertyName;
            Owner = owner as IAnimable;
            //first = true;
        }


    }
}
