using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.Input;
using Manual.Core;

namespace Manual.API;

public partial class PluginData : ObservableObject
{
    [ObservableProperty] private string name = "plugin";
    [ObservableProperty] private bool enabled = true;
    [ObservableProperty] private string codePath = $"{App.LocalPath}";
    [ObservableProperty] private string dllPath = $"{App.LocalPath}";

   // [XmlIgnore]
  //  public Assembly assembly { get; set; } = null;
    public PluginData(string name, string codepath, string dllpath)
    {
        Name = name;
        CodePath = codepath;
        DllPath = dllpath;

    }

}



public partial class DynamicProperties : DynamicObject, INotifyPropertyChanged
{ 

    private Dictionary<string, object> properties = new Dictionary<string, object>();

    public event PropertyChangedEventHandler PropertyChanged;

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return properties.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        properties[binder.Name] = value;
        OnPropertyChanged(binder.Name);
        return true;
    }

    public Dictionary<string, object> GetProperties()
    {
        return properties;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public void SetProperty(string name, object value)
    {
        properties[name] = value;
        OnPropertyChanged(name);
    }

    public void RemoveProperty(string name)
    {
        var dict = (IDictionary<string, object>)this;
        dict.Remove(name);
    }

    /*  public void set(string propertyName, object value)
      {
          properties[propertyName] = value;
          OnPropertyChanged(propertyName);
      }*/

}