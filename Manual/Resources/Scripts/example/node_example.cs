using Manual.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Manual.MUI;
using static Manual.API.ManualAPI;
using Manual.Core;
using System.Windows;
using Manual.Editors;
using System.Windows.Media;
using Manual.Objects.UI;
using Manual.Objects;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis;
using Manual.Core.Nodes;
using Manual.Core.Nodes.ComfyUI;

namespace NodePlugins;

[Export(typeof(IPlugin))]
public class MyNodesRegisterExample : IPlugin
{  
    //on script compiled
    public void Initialize()
    {
        GenerationManager.Instance.RegisterNode(
            nodeName: "My Node",
            nodeType: "MyNode",
            () => new MyNode(),
            "examples"
            );
    }
}

public class MyNode : ManualNode
{
    public MyNode()
    {
        Name = "My Node";

        AddOutput("output", FieldTypes.STRING);

        AddInput("input", FieldTypes.LAYER);

        AddInputField("inputfield", FieldTypes.LATENT, null, new M_NumberBox("number_box", 100, 1, true, 0, 10));

        AddField("field", FieldTypes.STRING, "test", new M_TextBox());
    
    }
    public override ComfyNodeAPI TO_API(ComfyNodeAPI data)
    {
        //you can interpret manual properties in to comfy API phyton data
        data.Set(fieldName: "Text", value: "hello world!");
        return data;
    }
}