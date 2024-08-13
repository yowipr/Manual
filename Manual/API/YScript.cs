using Manual.Core;
using ManualToolkit.Windows;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Manual.API;

public class YScript : Script
{
    public YScript()
    {
        Extension = ".y";
    }
    public override async void Compile()
    {
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await PreInterpret();
            //await Transcript();
        }

        catch (Exception ex)
        {
            Output.Error(ex.Message, ex.Source);
        }
        Mouse.OverrideCursor = null;
    }

    async Task TranscriptHuggingFace()
    {
        //llm_code_test
        var apiKey = Settings.instance.HuggingFaceToken; //"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";

        string prompt =
        $"text:\n" +
        $"{Code}\n" +
        $"\n" +
        $"C# code interpret:\n";

        var codeLlamaClient = new CodeLlamaClient(apiKey);
        var response = await codeLlamaClient.QueryAsync(prompt);

        var result = CodeLlamaClient.ExtractCSharpCode(response);
        Output.Log(result);

    }


    static readonly string interpreter_path = $"{App.LocalPath}Resources/Presets/YScript/interpreter_nodes.nlp";
    /// <summary>
    /// get the nlp result from LLM and decode;
    /// </summary>
    /// <param name="nlpText"></param>
    /// <returns></returns>
  

    async Task PreInterpret()
    {
        var payload = NLP.LoadFromFile<NLPPayload>(interpreter_path);
        payload.y_script = Code;

        string serializedText = NLP.Serialize(payload);
        Output.Log("y script to nlp");
        ScriptingManager.Instance.AddScript(new NLPScript() {Name = this.Name, Code = serializedText});
    }

}


internal class NLPPayload
{
    public string y_script_interpreter { get; set; }
    public string documentation_context { get; set; }

    public string y_script { get; set; }

   
    public NLPPayload()
    {
        
    }

}





public static class NLP // NATURAL LANGUAGE PROMPT
{
    public static T Deserialize<T>(string text) where T : new()
    {
        T result = new T();

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            {
                // Crear una lista del tipo adecuado
                var listType = property.PropertyType.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));

                string pattern = $@"\+\+\+{property.Name.ToLower()}(\s[^+]+)?\+\+\+(.*?)\+\+\+/{property.Name.ToLower()}\+\+\+";
                foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Singleline))
                {
                    var listItem = Activator.CreateInstance(listType);
                    PopulateObjectFromMatch(listItem, match);
                    list.Add(listItem);
                }

                property.SetValue(result, list);
            }
            else
            {
                // Procesar como propiedad individual
                string section = ExtractSection(text, $"+++{property.Name.ToLower()}+++", $"+++/{property.Name.ToLower()}+++");
                property.SetValue(result, section);
            }
        }

        return result;
    }//TODO: no puede deserializar listas
    private static void PopulateObjectFromMatch(object obj, Match match)
    {
        var properties = obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name.ToLower() == "text")
            {
                property.SetValue(obj, match.Groups[2].Value.Trim());
            }
            else
            {
                string attributePattern = $"{property.Name.ToLower()}: \"(.*?)\"";
                var attributeMatch = Regex.Match(match.Groups[1].Value, attributePattern);
                if (attributeMatch.Success)
                {
                    property.SetValue(obj, attributeMatch.Groups[1].Value);
                }
            }
        }
    }
    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var match = Regex.Match(text, $"{Regex.Escape(startMarker)}(.*?){Regex.Escape(endMarker)}", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }


    public static YObject Deserialize(string text)
    {
          var root = new YObject("root", ProcessContent(text));
        return root;
    }
    private static object ProcessContent(string content)
    {
        var matches = Regex.Matches(content, @"\+\+\+(.+?)(?:\s[^+]+)?\+\+\+(.*?)\+\+\+\/\1\+\+\+", RegexOptions.Singleline);


        if (matches.Count == 0)
        {
            return content; // Retornar como texto si no hay más secciones anidadas
        }

        var array = new YArray();
        foreach (Match match in matches)
        {
            // Regex adicional para encontrar el encabezado completo
            var fullHeaderMatch = Regex.Match(match.Value, @"^\+\+\+(.+?)\+\+\+", RegexOptions.Singleline);
            var fullHeader = fullHeaderMatch.Groups[1].Value.Trim();
            var properties = ExtractProperties(fullHeader);

            var innerContent = match.Groups[2].Value;

            // Extrae solo el nombre del encabezado
            var name = fullHeader.Split(new[] { ' ' }, 2)[0];
            var child = new YObject(name, ProcessContent(innerContent)) { Properties = properties };
            array.Add(child);
        }
        return array;
    }
    private static Dictionary<string, object> ExtractProperties(string fullHeader)
    {
        var properties = new Dictionary<string, object>();
        var headerParts = fullHeader.Split(new[] { ' ' }, 2);
        if (headerParts.Length > 1)
        {
            var propString = headerParts[1];
            var propMatches = Regex.Matches(propString, @"(\w+): ""([^""]+)""");

            foreach (Match match in propMatches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                properties[key] = value;
            }
        }
        return properties;
    }



    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        SerializeObject(sb, obj, "");
        return sb.ToString();
    }

    private static void SerializeObject(StringBuilder sb, object obj, string name)
    {
        if (obj == null) return;

        var type = obj.GetType();
        if (IsSimpleType(type))
        {
            AppendSection(sb, name, obj.ToString());
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            SerializeList(sb, obj, name);
        }
        else
        {
            foreach (var prop in type.GetProperties())
            {
                var propName = prop.Name;
                var propValue = prop.GetValue(obj);
                SerializeObject(sb, propValue, propName);
            }
        }
    }

    private static void SerializeList(StringBuilder sb, object list, string name)
    {
        sb.AppendLine($"+++{name}+++");
        var enumerable = list as IEnumerable;
        foreach (var item in enumerable)
        {
            SerializeObject(sb, item, "");
        }
        sb.AppendLine($"+++/+{name.Split(' ')[0]}+++");
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsValueType || type.IsPrimitive || new Type[] {
        typeof(String),
        typeof(Decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    }.Contains(type) || Convert.GetTypeCode(type) != TypeCode.Object;
    }

    private static void AppendSection(StringBuilder sb, string header, string content)
    {
        if (!string.IsNullOrWhiteSpace(header))
        {
            sb.AppendLine($"+++{header}+++");
        }
        sb.AppendLine(content);
        if (!string.IsNullOrWhiteSpace(header))
        {
            sb.AppendLine($"+++/+{header.Split(' ')[0]}+++");
        }
    }

    internal static T LoadFromFile<T>(string filePath) where T : new()
    {
        var inter = File.ReadAllText(filePath);
        var result = Deserialize<T>(inter);
        return result;
    }
}



public abstract class YBase
{
    public string Name { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

}

public class YObject : YBase
{ 
    public object Value { get; set; } // Puede ser un string, YArray u otro YObject
    public string Text { get; set; } = "";
    public YObject(string name, object value)
    {
        Name = name;
        Value = value;

        if (value is string s)
            Text = s;
    }

    public override string ToString()
    {
        return $"{Name} {DictionaryToString(Properties)} +++ {Text}";
    }
    public string DictionaryToString(Dictionary<string, object> properties)
    {
        var propertyStrings = new List<string>();
        foreach (var kvp in properties)
        {
            string valueString = kvp.Value is string ? $"\"{kvp.Value}\"" : kvp.Value.ToString();
            propertyStrings.Add($"{kvp.Key}: {valueString}");
        }

        return string.Join(", ", propertyStrings);
    }


    public object this[string key]
    {
        get
        {
            if (Value is YObject yObject)
            {
                if (yObject.Value is YObject && yObject.Value is not YArray)
                    return yObject.Value;
                else if (yObject.Value is YArray ar)
                    return ar[key];
            }
            else if (Value is YArray yArray)
            {
                // Implementar lógica si se desea acceder a un array con una clave string
                return yArray[key];
            }
            throw new KeyNotFoundException($"Key {key} not found in YObject.");
        }
    }
}

public class YArray : YBase, IEnumerable<YBase>
{
    private List<YBase> items = new List<YBase>();

    public YArray()
    {
        
    }
    public YArray(string name, object value)
    {
        Name = name;

        if (value is List<YBase> e)
            items = e;
        else if (value is string s)
            items.Add(new YObject(name, value));
        else if (value is YObject o)
            items.Add(o);


    }

    public void Add(YBase item)
    {
        items.Add(item);
    }

    public IEnumerator<YBase> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public YBase this[string name]
    {
        get
        {
            foreach (var item in items)
            {
                if (item.Name == name)
                {
                    if (item is YObject o && o.Value is YArray ar)
                        return ar;
                    else
                        return item;
                }
            }
            throw new KeyNotFoundException($"Element with name '{name}' not found in YArray.");
        }
    }

}



