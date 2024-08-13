using Manual.API;
using Manual.MUI;
using Manual.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manual.Core.Nodes;




public class ControlNetNode : LatentNode
{
    NodeOption Output, Model, Module, Reference, Strength, LowVRAM;
    public ControlNetNode()
    {
        Name = "ControlNet";

        Output = AddOutput("Control", FieldTypes.CONTROL, OutputValue);

        Reference = AddInput("Reference", FieldTypes.LAYER);

        Module = AddField("Module", FieldTypes.STRING, "scribble_pidinet", typeof(M_TextBox));
        Model = AddField("Model", FieldTypes.STRING, "control_v11p_sd15_scribble_fp16 [4e6af23e]", typeof(M_TextBox));

        Strength = AddField("Strength", FieldTypes.FLOAT, 1, M_SliderBox.StrengthSlider() );

        LowVRAM = AddField("LowVRAM", FieldTypes.BOOLEAN, true, new M_CheckBox("LowVRAM") );
    }

    async Task<object?> OutputValue()
    {
     
        var script = new ScriptArg("controlnet", new Dictionary<string, object?>
        {
            { "module", await Module.GetValue() },
            { "model", await Model.GetValue() },
            {"lowvram", await LowVRAM.GetValue() },
            {"threshold_a", 0 },
            {"threshold_b", 0 },
            {"weight", await Strength.GetValue() },
            { "processor_res", 512},
            {"input_image",  await LayerNode.GetLayerToByte(Reference) }
        });
   

        return script;
    }


}
public class ScriptArg
{
    public string Name { get; set; }
    public List<Dictionary<string, object?>> Args { get; set; } = new();

    public ScriptArg(string name, Dictionary<string, object?> properties)
    {
        Name = name;
        Args.Add(properties);
    }

    public void AddArg(string name, object property)
    {
        Args[0][name] = property;
    }
}



public class AnimateDiffNode : LatentNode
{
    NodeOption Output, Model, FPS, LastFrame;
    public AnimateDiffNode()
    {
        Name = "AnimateDiff";

        Output = AddOutput("Control", FieldTypes.CONTROL, OutputValue);

        Model = AddField("Model", FieldTypes.STRING, "mm_sd15_v3.safetensors", typeof(M_TextBox));
        FPS = AddField("FPS", FieldTypes.INT, 8, new M_NumberBox("FPS", 1, 300));

        LastFrame = AddInput("Last Frame", FieldTypes.LAYER);

    }


    async Task<object?> OutputValue()
    {

        var script = new ScriptArg("AnimateDiff", new Dictionary<string, object?>
        {
            {"enable", true },
            { "model", await Model.GetValue() }
            ,
            {"format", new string[] { "PNG" } },
            {"fps", await FPS.GetValue() },
            {"closed_loop", "R+P" },

            {"last_frame",  await LayerNode.GetLayerToByte(LastFrame)}
           
        });

        return script;
    }

}