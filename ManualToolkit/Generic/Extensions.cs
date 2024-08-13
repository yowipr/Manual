using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace ManualToolkit.Generic;



//------------------------------------------ DELEGATES
public delegate void Event<T>(T value);

public delegate void Event();
public delegate void EventInt(int n);
public delegate void EventFloat(float n);
public delegate void EventProgress(int progress, string message);
public delegate void EventString(string message);
public delegate void EventBool(bool value);
public delegate void EventMatrix(Matrix value);
public static class GeneralExtension
{
    public static int ToInt(this float n)
    {
        return (int)Math.Round(n);
    }
    public static int ToInt(this string value)
    {
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        else
        {
            // Puedes manejar el escenario en el que la conversión no sea exitosa.
            // Por ejemplo, puedes lanzar una excepción o devolver un valor predeterminado.
            throw new ArgumentException("La cadena no es un número entero válido.");
        }
    }
    public static string ByteToReadableSize(this long bytesNumber)
    {
        if (bytesNumber > 1024 * 1024) // MB/s
        {
            return $"{bytesNumber / (1024 * 1024):F2} MB";
        }
        else if (bytesNumber > 1024) // KB/s
        {
            return $"{bytesNumber / 1024:F2} KB";
        }
        else // B/s
        {
            return $"{bytesNumber:F2} B";
        }
    }
    public static string ByteToReadableSize(this double bytesNumber)
    {
        return ByteToReadableSize((long)bytesNumber);
    }

    public static string ToReadableJson(this string json)
    {
        using (var stringReader = new StringReader(json))
        using (var stringWriter = new StringWriter())
        {
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }
    }


    public static string ChangeExtension(this string filePath, string newExtension)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("La ruta del archivo no puede ser nula o vacía.", nameof(filePath));
        }

        if (string.IsNullOrEmpty(newExtension))
        {
            throw new ArgumentException("La nueva extensión no puede ser nula o vacía.", nameof(newExtension));
        }

        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string newFilePath = Path.Combine(directory, fileName + "." + newExtension);

        return newFilePath;
    }

    public static TEnum ToEnum<TEnum>(this string value, bool ignoreCase = true) where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase, out var enumValue))
        {
            throw new ArgumentException($"No se pudo convertir '{value}' a {typeof(TEnum).Name}.");
        }

        return enumValue;
    }


    public static string ToStringNoDecimal(this Vector vector)
    {
        return $"({vector.X:0}, {vector.Y:0})";
    }
    public static string ToStringNoDecimal(this Point point)
    {
        return $"({point.X:0}, {point.Y:0})";
    }

    public static Point Subtract(this Point point1, Point point2)
    {
        return new Point(point1.X - point2.X, point1.Y - point2.Y);
    }
    public static Vector Subtract(this Vector point1, Point point2)
    {
        return new Vector(point1.X - point2.X, point1.Y - point2.Y);
    }

    public static Point SubtractPoint(this Vector point1, Point point2)
    {
        return new Point(point1.X - point2.X, point1.Y - point2.Y);
    }


    public static Point Add(this Point point1, Point point2)
    {
        return new Point(point1.X + point2.X, point1.Y + point2.Y);
    }
    public static Point Add(this Point point1, double x, double y )
    {
        return point1.Add(new Point(x, y));
    }
    public static Point Minus(this Point point1, Point point2)
    {
        return new Point(point1.X - point2.X, point1.Y - point2.Y);
    }

    public static void ForEach<T>(this ObservableCollection<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }

    /// <summary>
    /// duplicates the collection for safety
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="action"></param>
    public static void ForEachSafe<T>(this ObservableCollection<T> collection, Action<T> action)
    {
        var c = collection.ToList();
        foreach (var item in c)
        {
            action(item);
        }
    }


    public static T TryGetValueOrDefault<T>(this IDictionary<string, object> dictionary, string key, T defaultValue = default)
    {
        if (dictionary.TryGetValue(key, out object value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
        }
        return defaultValue;
    }

    public static T ItemByIndex<T>(this IEnumerable<T> enumerable, int index)
    {
        if (enumerable == null || index < 0)
        {
            return default(T);
        }

        // Intenta obtener el elemento en el índice especificado.
        return enumerable.Skip(index).FirstOrDefault();
    }



    /// <summary>
    /// Reemplaza un elemento en la lista con otro elemento.
    /// </summary>
    /// <typeparam name="T">El tipo de elementos en la lista.</typeparam>
    /// <param name="list">La lista donde se realizará el reemplazo.</param>
    /// <param name="oldItem">El elemento a reemplazar.</param>
    /// <param name="newItem">El nuevo elemento con el que reemplazar.</param>
    /// <returns>True si el elemento fue reemplazado exitosamente, false si el elemento original no fue encontrado.</returns>
    public static bool Replace<T>(this IList<T> list, T oldItem, T newItem)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list), "La lista no puede ser null.");
        }

        var index = list.IndexOf(oldItem);
        if (index != -1)
        {
            list[index] = newItem;
            return true;
        }

        return false;
    }


    public static string ToReadableTime(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            // Más de una hora
            return $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2} hours";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            // Menos de una hora pero más de un minuto
            return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2} minutes";
        }
        else
        {
            // Menos de un minuto
            return $"{timeSpan.Seconds} seconds";
        }
    }
    public static string ToReadableTimeExact(this TimeSpan span)
    {
        return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}",
            span.Hours,
            span.Minutes,
            span.Seconds,
            span.Milliseconds);
    }


    public static double Round(this double number, double jumps)
    {
        return Math.Round(number / jumps) * jumps;
    }


    public static float Lerp(this float t, float min, float max)
    {
        return min + (max - min) * t;
    }

}




