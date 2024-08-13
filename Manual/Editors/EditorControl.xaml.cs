using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para EditorControl.xaml
/// </summary>
public partial class EditorControl : UserControl
{
    public EditorControl()
    {
        InitializeComponent();

    }

  
}

/// <summary>
/// an empty editor to add whatever you want, for API purposes
/// </summary>


public class ED_Control : Editor
{
    [JsonIgnore] public string name { get; set; } = "Editor";
    [JsonIgnore] public byte[] icon { get; set; }
    [JsonIgnore] public string iconPath { get; set; }

 
    [JsonIgnore]
    public object body { get; set; } = 
        new Label() {
            Content = "There's nothing to say",
        HorizontalAlignment = HorizontalAlignment.Center,
        Opacity = 0.56,
        };

    public ED_Control()
    {

    }
}

