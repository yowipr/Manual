using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicExpresso;
using Manual.API;
using Manual.Core.Nodes;
using Manual.Objects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
namespace Manual.Core;


public interface IMultiDrivable
{
    public List<Driver> Drivers { get; set; }
}
public interface IDrivable : IDisposable
{
    public Driver Driver { get; set; }
}


public partial class Driver : ObservableObject, IDisposable
{
    [ObservableProperty] bool isError = false;

    [ObservableProperty] bool enabled = true;
    [ObservableProperty] string expressionCode = "";

    public MvvmHelpers.ObservableRangeCollection<ExpressionSource> sources = [];
    public IDrivable target;
    public readonly string targetPropertyName;
    private Func<object> compiledExpression;
    private readonly Dictionary<string, PropertyInfo> sourceProperties = new Dictionary<string, PropertyInfo>();
    public bool twoWay;
    public string sourcePropertyName { get; set; }


    
  
    public Driver(IDrivable target, string targetPropertyName, object source, string expression, bool twoWay = false)
    {
        this.sources.CollectionChanged += Sources_CollectionChanged;

        this.target = target;
        this.targetPropertyName = targetPropertyName;
        this.twoWay = twoWay;

        ExpressionCode = expression;

       this.waitsources = [new ExpressionSource(source, "source")];
    }

    public Driver(IDrivable target, string targetPropertyName, IEnumerable<ExpressionSource> sources, string expression, bool twoWay = false)
    {
        this.sources.CollectionChanged += Sources_CollectionChanged;


        this.target = target;
        this.targetPropertyName = targetPropertyName;
        this.twoWay = twoWay;


        ExpressionCode = expression;
        //sourcePropertyName = ExtractSourcePropertyName(ExpressionCode);


        this.waitsources = sources;

    }


    IEnumerable<ExpressionSource> waitsources;
    [JsonIgnore] public bool initialized = false;
    public void Initialize()
    {
        if (initialized)
        {
            RecompileExpression();
            return;
        }

        initialized = true;

        sourcePropertyName = ExtractSourcePropertyName(ExpressionCode);

        this.sources.AddRange(waitsources);
        waitsources = null;

        // Initialize and subscribe to property changes
        target.Driver = this;

        if (target is INotifyPropertyChanged notifyPropertyChanged2)
        {
            notifyPropertyChanged2.PropertyChanged += Target_PropertyChanged;
        }
        else if (target != null)
            Output.Warning("target is not a notifiable object", $"Driver {ExpressionCode}");
        else
            Output.Warning("target is null", $"Driver {ExpressionCode}");

        RecompileExpression();
    }

    public void AddSource(ExpressionSource source)
    {
        this.sources.Add(source);
    }
    public void RemoveSource(ExpressionSource source)
    {
        this.sources.Remove(source);
    }

    private void Sources_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            foreach (ExpressionSource source in e.NewItems)
            {
                if (source.Source is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += Source_PropertyChanged;
                }
                else if (target != null)
                    Output.Warning("source is not a notifiable object", $"Driver {ExpressionCode}");
                else
                    Output.Warning("source is null, not find", $"Driver {ExpressionCode}");


                var properties = source.Source.GetType().GetProperties();
                foreach (var property in properties)
                {
                    if (!sourceProperties.ContainsKey(property.Name))
                    {
                        sourceProperties.Add(property.Name, property);
                    }
                }
            }
           
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            foreach (ExpressionSource source in e.OldItems)
            {
                if (source.Source is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged -= Source_PropertyChanged;
                }

                var properties = source.Source.GetType().GetProperties();
                foreach (var property in properties)
                {
                    if (!sourceProperties.ContainsKey(property.Name))
                    {
                        sourceProperties.Remove(property.Name);
                    }
                }
            }
        }
    }


    //----------------------------------------------------------------------------------------------------------------- SOURCE TARGET PROPERTY CHANGED
    private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!updatingSource && Enabled && sourceProperties.ContainsKey(e.PropertyName))
        {
            updatingTarget = true;
            UpdateTarget();
            updatingTarget = false;
        }
    }
    private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!updatingTarget && Enabled && e.PropertyName == targetPropertyName)
        {
            updatingSource = true;
            UpdateSource();
            updatingSource = false;
        }
    }





    bool updatingSource = false;
    bool updatingTarget = false;
    private void UpdateSource()
    {
        var targetValue = GetPropertyValue(target, targetPropertyName);

        if (sources.Count > 0)
        {
            var firstSource = sources[0].Source;
            SetPropertyValue(firstSource, sourcePropertyName, targetValue);
        }
    }
    public void UpdateSource(object newValue)
    {
        if (sources.Any())
        {
            var firstSource = sources.First().Source;
            SetPropertyValue(firstSource, sourcePropertyName, newValue);
        }
        else
        {
            var firstSource = waitsources.First().Source;
            SetPropertyValue(firstSource, ExpressionCode, newValue);
        }

    }
    private string ExtractSourcePropertyName(string expression)
    {
        // Assuming the expression is in the form "Source.Property"
        var parts = expression.Split('.');
        if (parts.Length == 2)
        {
            return parts[1]; // Return the property name
        }
        else if (parts.Length == 1)
            return expression;
        else
            return "";

        throw new InvalidOperationException("Invalid expression format.");
    }



    private void UpdateTarget()
    {
        try
        {
            // Ejecutar la expresión compilada y obtener el valor
            var value = compiledExpression();

            // Intentar establecer el valor en el target
            SetPropertyValue(target, targetPropertyName, value);
        }
        catch (KeyNotFoundException ex)
        {
            // Manejar el error y registrar un mensaje informativo
            Output.LogError($"'{targetPropertyName}' {ExpressionCode} was not found in the Properties for the target object. Ensure the property exists and is properly set.");
        }
        catch (Exception ex)
        {
            // Capturar cualquier otra excepción inesperada y registrar el error
            Output.LogError($"An unexpected error occurred while updating the target.\n Details:\n {ex.Message}");
        }
    }




    public void RecompileExpression()
    {
        if (!initialized)
        {
            Initialize();
            return;
        }
        try
        {
            compiledExpression = CompileExpression(ExpressionCode);
        }
        catch (Exception ex)
        {
            IsError = true;
            Output.LogError(ex.Message, "CompileExpression");
            return;
        }

        if(Enabled)
           UpdateTarget();
    }

    private Func<object> CompileExpressionOld(string expression)
    {
        IsError = false;

        if (sources.Count == 1 && Regex.IsMatch(expression.Trim(), @"^\w+$"))
            expression = $"{sources[0].Name}.{expression}";


        var interpreter = new Interpreter();
        foreach (var source in sources)
        {
            interpreter.SetVariable(source.Name, source.Source);
        }

        var lambdaExpression = interpreter.ParseAsDelegate<Func<object>>(expression);
        return lambdaExpression;//.Compile();
    }
    private Func<object> CompileExpression(string expression)
    {
        IsError = false;

        // Si solo hay un source y la expresión es un nombre simple (ej. "Lora3")
        if (sources.Count == 1 && Regex.IsMatch(expression.Trim(), @"^\w+$"))
        {
            var sourceObject = sources[0].Source;
            var sourceName = sources[0].Name;

            // Usar reflexión para verificar si el source tiene directamente la propiedad
            var property = sourceObject.GetType().GetProperty(expression);
            if (property != null)
            {
                // Si tiene la propiedad, ajustar la expresión para accederla directamente
                expression = $"{sourceName}.{expression}";
            }
            else
            {
                // Verificar si el source tiene un Dictionary<string, PromptProperty> llamado "Properties"
                var propertiesField = sourceObject.GetType().GetProperty("Properties");
                if (propertiesField != null)
                {
                    var dic = propertiesField.GetValue(sourceObject) as Dictionary<string, PromptProperty>;
                    if (dic != null && !dic.ContainsKey(expression))
                    {
                        var msg = $"property '{expression}' does not exist in '{sourceName}'";
                        throw new Exception(msg);
                    }

                    // Asumimos que "Properties" es un Dictionary<string, PromptProperty>
                    expression = $"{sourceName}.Properties[\"{expression}\"].Value";
                }
                else
                {
                    // Si no tiene ni la propiedad ni el diccionario, es un error
                    var msg = $"property '{expression}' does not exist in '{sourceName}'";
                    throw new Exception(msg);
                }
            }
        }

        // Crear un intérprete y asignar variables.
        var interpreter = new Interpreter();
        foreach (var source in sources)
        {
            interpreter.SetVariable(source.Name, source.Source);
        }

        try
        {
            // Evaluar la expresión
            var lambdaExpression = interpreter.ParseAsDelegate<Func<object>>(expression);
            return lambdaExpression;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error compiling expression: {ex.Message}");
        }
    }



    private void SetPropertyValue(object obj, string propertyName, object value)
    {
        // Verificar si la propiedad es null o vacía (en este caso, manejar con expresión)
        if (string.IsNullOrEmpty(propertyName))
        {
            AssignValueUsingExpression(value);
        }
        else
        {
            // Usar reflexión para obtener la propiedad en el objeto
            var property = obj.GetType().GetProperty(propertyName);

            if (property != null && property.CanWrite)
            {
                // Si la propiedad se encuentra y es writable
                if (value == null) return;

                // Si el tipo coincide, asignar directamente
                else if (value != null && property.PropertyType == value.GetType())
                {
                    property.SetValue(obj, value);
                }
                else
                {
                    // Convertir el valor al tipo de la propiedad y asignarlo
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(obj, convertedValue);
                }
            }
            else
            {
                // Si no se encuentra la propiedad directamente, buscar en el Dictionary "Properties"
                var propertiesField = obj.GetType().GetProperty("Properties");

                if (propertiesField != null)
                {
                    // Asumimos que "Properties" es un Dictionary<string, PromptProperty>
                    var propertiesDictionary = propertiesField.GetValue(obj) as Dictionary<string, PromptProperty>;
                    if (propertiesDictionary != null && propertiesDictionary.ContainsKey(propertyName))
                    {
                        // Si existe la propiedad en el diccionario, asignar el valor a PromptProperty.Value
                        propertiesDictionary[propertyName].Value = value;
                    }
                    else
                    {
                        // Si no se encuentra la propiedad en el objeto ni en el diccionario, es un error
                        Output.LogError($"Property '{propertyName}' does not exist.");
                    }
                }
                else
                {
                    // Si no tiene el diccionario "Properties", lanzar un error
                    Output.LogError($"missing 'Properties' Dictionary to find'{propertyName}'.");
                }
            }
        }
    }

    private object GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    private void AssignValueUsingExpression(object value)
    {
        // Si el valor es un string, necesitamos agregar comillas alrededor para que sea una cadena válida en la expresión.
        string formattedValue;
        if(value is JValue jv)
        {
            value = jv.Value;
        }

        if (value is string stringValue)
        {
            formattedValue = $"\"{stringValue}\"";  // Agregar comillas alrededor del valor string.
        }
        else if (value is float floatValue)
        {
            formattedValue = $"{floatValue}f";
        }
        else if (value is null)
        {
            formattedValue = "null";
        }
        else
        {
            formattedValue = value.ToString();  // Convertir el valor a su representación en cadena si no es un string.
        }

        // Construir la expresión completa con el valor formateado.
        string expression = $"{ExpressionCode} = {formattedValue}";

        // Crear un intérprete y asignar variables.
        var interpreter = new Interpreter();
        foreach (var source in sources)
        {
            interpreter.SetVariable(source.Name, source.Source);
        }

        // Evaluar la expresión de asignación.
        try
        {
            IsError = false;
            interpreter.Eval(expression);
        }
        catch (KeyNotFoundException)
        {
            IsError = true;
            Output.LogError($"Driver Cannot find {sources.First().Name} for {ExpressionCode}, consider edit the Driver");
        }
        catch (Exception ex)
        {
            IsError = true;
            // Manejar errores si es necesario (por ejemplo, logging o throwing excepciones específicas).
            Output.LogError($"Error at eval the expression {ExpressionCode} in {sources.First().Name}: {ex.Message}");
        }
    }






    public static Driver Bind(IDrivable target, string targetPropertyName, List<ExpressionSource> sources, string expression, bool twoWay = false)
    {
        var driver = new Driver(target, targetPropertyName, sources, expression, twoWay);
        driver.Initialize();
        return driver;
    }

    /// <summary>
    /// bind two way, single
    /// </summary>
    /// <returns></returns>
    public static Driver Bind(IDrivable target, string targetPropertyName, object source, string sourcePropertyName)
    {
        return Driver.Bind(target, targetPropertyName, [new ExpressionSource(source, "source")], $"source.{sourcePropertyName}", true);
    }

    public static void Delete(IDrivable target)
    {
        target.Driver?.Dispose();
        target.Driver = null;
    }
    public static void Delete(Driver driver)
    {
        Delete(driver.target);
    }




    public string Serialize()
    {
        var serializedDrivers = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        });
        return serializedDrivers;
    }

    public Driver Deserialize(string serializedData)
    {
        var deserializedDrivers = JsonConvert.DeserializeObject<Driver>(serializedData, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        });
        return deserializedDrivers;
    }



    public override string ToString()
    {
        return $"{sources}, {target}, {ExpressionCode}, {sourceProperties}";
    }

    public void Dispose()
    {
        // Initialize and subscribe to property changes
        foreach (var source in sources)
        {
            if (source.Source is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= Source_PropertyChanged;
            }

            var properties = source.Source.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (!sourceProperties.ContainsKey(property.Name))
                {
                    sourceProperties.Remove(property.Name);
                }
            }

        }



        if (target is INotifyPropertyChanged notifyPropertyChanged2)
        {
            notifyPropertyChanged2.PropertyChanged -= Target_PropertyChanged;
        }
    }


    /// <summary>
    /// changes source object with a new source for every expressionsource
    /// </summary>
    /// <param name="oldSource"></param>
    /// <param name="newSource"></param>
    public void UpdateSource(object oldSource, object newSource)
    {
        foreach (var item in sources.ToList())
        {
            if (item.Source == oldSource)
            {
                RemoveSource(item);
                AddSource(new ExpressionSource(newSource, item.Name));
                break;
            }
        }
        RecompileExpression();
    }




 
}



public partial class ExpressionSource : ObservableObject
{
    [ObservableProperty] object source;
    [ObservableProperty] string name;

    [JsonConverter(typeof(TypeNameConverter))]
    public Type type;
    public ExpressionSource(object source, string name)
    {
        Source = source;
        type = Source?.GetType();
        Name = name;
    }
    public ExpressionSource(object source)
    {
        Source = source;
        type = Source.GetType();
        Name = "source";
    }

    public ExpressionSource()
    {
            
    }

    public override string ToString()
    {
        return $"{Name}, {Source}";
    }
}



public class TypeNameConverter : JsonConverter<Type>
{
    public override void WriteJson(JsonWriter writer, Type value, JsonSerializer serializer)
    {
        if (value != null)
        {
            string typeName = $"{value.FullName}, {value.Assembly.GetName().Name}";
            writer.WriteValue(typeName);
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string typeName = (string)reader.Value;
            return Type.GetType(typeName);
        }
        return null;
    }
}


//--------------------------------------------------------------------------------------------------------------------------- SERIALIZE TYPE REGISTRY
public class SerializeTypeRegistry
{
    private static readonly Dictionary<Type, SerializeType> serializationTypes = new Dictionary<Type, SerializeType>
    {
       {typeof(Prompt), new SerializeType(typeof(Prompt),
           (obj) => $"{((Prompt)obj).Name}",
           (id) =>
           {
              var prompt = api.prompt(id);

               if(GenerationManager.Instance.Prompts.Any())
                    prompt ??= GenerationManager.Instance.Prompts.First();

               return prompt;
          }
       )},

        {typeof(NodeOption), new SerializeType(typeof(NodeOption),
           (obj) =>
           {
               var nodeop = (NodeOption)obj;
               return $"{nodeop.AttachedNode.AttachedPreset.Name};{nodeop.AttachedNode.IdNode};{nodeop.Name}";
           },
           (id) => 
           {   //vaya lio chaval
               var msg = id.Split(';');

               var preset = api.preset(msg[0]);
               if(preset != null)
               {
                   var node = preset.node(Convert.ToInt32(msg[1]));
                   if(node != null)
                   {
                    var nodeop = node.field(msg[2]);
                    return nodeop;
                   }
                   else
                   {
                       Output.Log($"node not found for driver {msg[2]}");
                       return null;
                   }
               }
               else
                   return id;
               
           }
       )},

       //TODO: HACERLO BIEN LUEGO, DRIVERS GLOBALES TYPE
         {typeof(LayerBase), new SerializeType(typeof(LayerBase),
           (obj) => $"{((LayerBase)obj).Id}",
           (id) => api.layer(Guid.Parse(id))
       )},

       {typeof(Shot), new SerializeType(typeof(Shot),
           (obj) => $"{((Shot)obj).Id}",
           (id) => api.shot(Guid.Parse(id))
       )},

           {typeof(PromptPreset), new SerializeType(typeof(PromptPreset),
           (obj) => $"{((PromptPreset)obj).Name}",
           (id) => api.shot(id)
       )}

    };




    public static void Register<T>(SerializeType serializeType)
    {
        serializationTypes[serializeType.Type] = serializeType;
    }
    public static SerializeType GetSerializer(Type type)
    {
        if (serializationTypes.TryGetValue(type, out var serializer))
        {
            return serializer as SerializeType;
        }

        // Si no se encuentra el tipo específico, busca en el tipo base
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (serializationTypes.TryGetValue(baseType, out serializer))
            {
                return serializer as SerializeType;
            }
            baseType = baseType.BaseType;
        }

        throw new InvalidOperationException($"No serializer registered for type {type}");
    }

    public static ExpressionSource SerializeReference(ExpressionSource obj)
    {
        var serializer = SerializeTypeRegistry.GetSerializer(obj.type);
        return new ExpressionSource(serializer.Serialize(obj.Source), obj.Name) { type = obj.type};
    }

    public static object DeserializeReference(ExpressionSource objref)
    {
        var serializer = SerializeTypeRegistry.GetSerializer(objref.type);
        return serializer.Deserialize(objref.Source.ToString());
    }


}
public class SerializeType
{ 

    public Type Type { get; }
    public Func<object, string> Serialize { get; }
    public Func<string, object> Deserialize { get; }
    public SerializeType(Type type, Func<object, string> serialize, Func<string, object> deserialize)
    {
        Type = type;
        Serialize = serialize;
        Deserialize = deserialize;
    }

  
}




public class DriverJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Driver).IsAssignableFrom(objectType);
    }


    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var driver = (Driver)value;
        var sdriver = Sdriver.Save(driver);
        var jo = JObject.FromObject(sdriver);
        jo.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var sdriver = jo.ToObject<Sdriver>();
        return Sdriver.Load(sdriver);
    }



}



public class DriverListJsonConverter : JsonConverter<List<Driver>>
{
    public override void WriteJson(JsonWriter writer, List<Driver> value, JsonSerializer serializer)
    {
        JArray array = new JArray();
        foreach (var driver in value)
        {
            var obj = Sdriver.Save(driver);
            var jo = JObject.FromObject(obj);
            array.Add(jo);
        }
        array.WriteTo(writer);
    }

    public override List<Driver> ReadJson(JsonReader reader, Type objectType, List<Driver> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var array = JArray.Load(reader);
        var drivers = new List<Driver>();
        foreach (var item in array)
        {
            var obj = (JObject)item;
            var sdriver = obj.ToObject<Sdriver>();
            var driver = Sdriver.Load(sdriver);
            drivers.Add(driver);
        }
        return drivers;
    }


}




public class Sdriver
{
    public bool enabled { get; set; }
    public string expressionCode { get; set; }


    public List<ExpressionSource> sources { get; set; }
    public ExpressionSource target { get; set; }

    public string targetPropertyName { get; set; }

    public bool twoWay { get; set; }
    public string sourcePropertyName { get; set; }


    public static Sdriver Save(Driver driver)
    {
        return new Sdriver
        {
            enabled = driver.Enabled,
            expressionCode = driver.ExpressionCode,
            sources = driver.sources.Select(s => SerializeTypeRegistry.SerializeReference(s)).ToList(),
            target = SerializeTypeRegistry.SerializeReference(new ExpressionSource(driver.target)),
            targetPropertyName = driver.targetPropertyName,
            twoWay = driver.twoWay,
            sourcePropertyName = driver.sourcePropertyName
        };
    }

    public static Driver Load(Sdriver sdriver)
    {
        var enabled = sdriver.enabled;
        var expressionCode = sdriver.expressionCode;


        var sources = sdriver.sources.Select(s => new ExpressionSource(SerializeTypeRegistry.DeserializeReference(s), s.Name)   ).ToList();

        var target = SerializeTypeRegistry.DeserializeReference(sdriver.target) as IDrivable;
        var targetPropertyName = sdriver.targetPropertyName;
        var twoWay = sdriver.twoWay;
        var sourcePropertyName = sdriver.sourcePropertyName;

 

        var driver = new Driver(target, targetPropertyName, sources, expressionCode, twoWay)
        {
            Enabled = enabled,
        };

        return driver;
    }




}