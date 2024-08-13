using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using ManualToolkit.Specific;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Windows.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using ManualToolkit.Generic;

namespace ManualToolkit.Themes;

// SPECIFIC
public class AdminToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is User user && user != null && user.Admin)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class AdminToVisibilityConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is User user && user != null && user.Admin)
        {
            return Visibility.Collapsed;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ProToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is User user && user.Products.TryGetValue("manual", out var product) && product.Plan == UserPlan.pro.ToString())
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


// GENERAL
//objs
public class ObjToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Verifica si el valor (User) es nulo.
        if (value != null)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ObjToVisibilityConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Verifica si el valor (User) es nulo.
        if (value != null)
        {
            return Visibility.Collapsed;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ObjectToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue;
        }

        return 0; // Valor predeterminado si no se puede convertir a int
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return Decimal.ToInt32(decimalValue);
        }

        return 0; // Valor predeterminado si no se puede convertir a int
    }

}
public class ObjToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return 0.0;
        }

        if (value is double)
        {
            return value;
        }

        if (double.TryParse(value.ToString(), out double result))
        {
            return result;
        }

        return 0.0; // O manejar de alguna otra forma
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ObjToVisibilityHiddenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Verifica si el valor (User) es nulo.
        if (value != null)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Hidden;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ObjToVisibilityHiddenConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // Verifica si el valor (User) es nulo.
        if (value != null)
        {
            return Visibility.Hidden;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}




//bool
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (Invert) boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if(value is bool b)
        {
            return Invert ? !b : b;
        }
        else
            return Invert ? null : value;
    }
}
public class ObjectToBoolConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!Invert)
            return value != null;
        else
            return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool v && v == true)
        {
            return Visibility.Collapsed;
        }
        else
        {
            return Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;
        }
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class NullOrEmptyToVisibilityConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Visible : Visibility.Collapsed;
        }
        return value == null ? Visibility.Visible: Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


public class InvertBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool)
        {
            return !(bool)value;
        }
        throw new InvalidOperationException("The value must be a boolean");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool)
        {
            return !(bool)value;
        }
        throw new InvalidOperationException("The value must be a boolean");
    }
}

public class FileNameConverter : IValueConverter 
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return null;

        string filePath = (string)value;
        return Path.GetFileNameWithoutExtension(filePath);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}//from path to only name without extension

public class ColorToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        if (parameter != null)
        {
            var resource = Application.Current.TryFindResource(parameter);
            if (resource is SolidColorBrush brush)
            {
                return brush;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class SolidColorBrushToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }

        throw new InvalidOperationException("El valor proporcionado debe ser de tipo SolidColorBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        throw new InvalidOperationException("El valor proporcionado debe ser de tipo Color");
    }
}


public class BoolToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? new SolidColorBrush(Color.FromRgb(24, 201, 100)) : new SolidColorBrush(Color.FromRgb(245, 66, 129));
        }
        throw new InvalidOperationException("The value must be a boolean");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


public class StringToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string imagePath = value as string;
        if (!string.IsNullOrEmpty(imagePath))
        {
            return new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return Visibility.Collapsed;
        }

        var enumType = value.GetType();
        if (!Enum.IsDefined(enumType, value))
        {
            return Visibility.Collapsed;
        }

        var targetValue = Enum.Parse(enumType, parameter.ToString());

        return value.Equals(targetValue) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
public class EnumToVisibilityConverterInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return Visibility.Visible;
        }

        var enumType = value.GetType();
        if (!Enum.IsDefined(enumType, value))
        {
            return Visibility.Visible;
        }

        var targetValue = Enum.Parse(enumType, parameter.ToString());

        return value.Equals(targetValue) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
public class PointToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Point point)
        {
            return new Thickness(point.X, point.Y, 0, 0);
        }
        return new Thickness();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class GenericObjectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Conversión de Modelo a Vista. Podría no ser necesaria.
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Conversión de Vista a Modelo
        if (value == null)
            return null;

        try
        {
            // Intenta convertir directamente si el tipo es compatible
            if (value.GetType() == targetType)
                return value;

            if (targetType == typeof(int) && value is double)
            {
                return (int)(double)value;
            }
            // Intenta convertir a targetType utilizando Convert.ChangeType
            var a = System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            return a;
        }
        catch
        {
            // Intenta convertir valores numéricos especiales (por ejemplo, Decimal a Double)
            if (value is IConvertible)
            {
                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
           
            return DependencyProperty.UnsetValue;
        }
    }
}



public class CustomNumberFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double number)
        {
            // Comprueba si el número es esencialmente un entero
            if (number == Math.Floor(number))
            {
                // Es un entero, retorna sin formato decimal
                return number.ToString("0", culture);
            }
            else
            {
                // Tiene decimales, aplica formato con dos decimales
                return number.ToString("F2", culture);
            }
        }
        return value; // Si no es un número, retorna el valor original
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Si necesitas convertir de vuelta el string formateado a double, aquí iría esa lógica
        throw new NotImplementedException();
    }
}


public class AdvancedNumberFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double number)
        {
            // Si el número es esencialmente un entero.
            if (number == Math.Floor(number))
            {
                return number.ToString("0", culture);
            }
            else
            {
                // Verificar el cuarto dígito después del punto decimal
                double scaled = number * 1000; // Escala para preservar el dígito relevante
                double fourthDigit = Math.Floor(scaled) % 10; // Extraer el cuarto dígito

                // Si el cuarto dígito es 9, podría ser un error de precisión flotante
                if (fourthDigit == 9)
                {
                    double fifthDigit = Math.Floor(scaled * 10) % 10; // Comprobar el siguiente dígito
                    if (fifthDigit == 9)
                    {
                        // Si el quinto dígito también es 9, redondear a F2 para evitar errores de precisión flotante
                        return number.ToString("F2", culture);
                    }
                }

                // Si el cuarto dígito es 0, usar F2, de lo contrario usar F3
                return fourthDigit == 0 ? number.ToString("F2", culture) : number.ToString("F3", culture);
            }
        }
        return value; // Si no es un número, retorna el valor original
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ChangeType(value, targetType, culture);
    }
}






public class FilePathToFolderNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Comprobar si el valor es null
        if (value == null)
        {
            return null;
        }

        string fullPath = value.ToString();

        // Intentar obtener el nombre de la carpeta del path
        try
        {
            // Obtiene la ruta del directorio
            var directoryPath = Path.GetDirectoryName(fullPath);
            // Si el path al directorio no es null o vacío, obtiene el nombre del directorio
            if (!string.IsNullOrEmpty(directoryPath))
            {
                return $"../{Path.GetFileName(directoryPath)}/"; // Obtiene el nombre de la carpeta
            }
            return null;
        }
        catch
        {
            // En caso de cualquier error, simplemente devuelve null
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack no es necesario en este caso
        throw new NotImplementedException();
    }
}


public class CollectionCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string par)
        {
            if (value is int i)
            {
                return i == par.ToInt() ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        else
        {
            // Comprobar si el valor es una colección
            if (value is int i)
            {
                return i != 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Si el valor no es una colección, considerar que está "vacío"
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class CollectionCountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string par)
        {
            if (value is int i)
            {
                return i == par.ToInt();
            }
        }
        else
        {
            // Comprobar si el valor es una colección
            if (value is int i)
            {
                return i != 0;
            }
        }

        // Si el valor no es una colección, considerar que está "vacío"
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class PointsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var points = value as ObservableCollection<Point>;
        if (points == null)
            return null;

        PointCollection collection = new PointCollection();
        foreach (var point in points)
        {
            collection.Add(point);
        }
        return collection;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}



public class RectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 4 || !(values[0] is double x) || !(values[1] is double y) ||
            !(values[2] is double width) || !(values[3] is double height))
            return Rect.Empty;

        return new Rect(x, y, width, height);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class RectSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || !(values[0] is double width) || !(values[1] is double height))
            return Rect.Empty;

        return new Rect(0, 0, width, height);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}










//---------------------------------------------------- ADJUNT PROPERTIES
public static class CustomFields
{

    //CornerRadius
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(CustomFields),
            new PropertyMetadata(new CornerRadius(12)));

    public static void SetCornerRadius(UIElement element, CornerRadius value)
    {
        element.SetValue(CornerRadiusProperty, value);
    }

    public static CornerRadius GetCornerRadius(UIElement element)
    {
        return (CornerRadius)element.GetValue(CornerRadiusProperty);
    }


    //VerticalAlignment
    public static readonly DependencyProperty VerticalAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "VerticalAlignment",
            typeof(VerticalAlignment),
            typeof(CustomFields),
            new PropertyMetadata(VerticalAlignment.Center));

    // Método para establecer VerticalAlignmentProperty
    public static void SetVerticalAlignment(UIElement element, VerticalAlignment value)
    {
        element.SetValue(VerticalAlignmentProperty, value);
    }

    // Método para obtener VerticalAlignmentProperty
    public static VerticalAlignment GetVerticalAlignment(UIElement element)
    {
        return (VerticalAlignment)element.GetValue(VerticalAlignmentProperty);
    }






}

