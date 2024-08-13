using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manual.Core.Nodes.ComfyUI;

public static class ComfyExtension
{

    /// <summary>
    /// for comfy data.inputs
    /// </summary>
    /// <returns></returns>
    public static object ToComyData(this NodeOption nodeop)
    {
        if (nodeop.IsInputOrField() && nodeop.ConnectionInput != null)
        {
            var connectedNode = nodeop.ConnectionInput.AttachedNode;
            var index = connectedNode.Outputs.IndexOf(nodeop.ConnectionInput);
            return new object[] { connectedNode.IdNode.ToString(), index };
        }
        else
        {
            return ConvertFieldValue(nodeop.Type, nodeop.FieldValue);
        }
    }

    private static object ConvertFieldValue(string type, object fieldValue)
    {
        if (fieldValue == null)
            return null;

        switch (type)
        {
            case "FLOAT":
                if (float.TryParse(fieldValue.ToString(), out float floatValue))
                    return floatValue;
                return fieldValue;

            case "INT":
                if (int.TryParse(fieldValue.ToString(), out int intValue))
                    return intValue;
                return fieldValue;

            // Add other types here as needed
            // case "DOUBLE":
            //     if (double.TryParse(fieldValue.ToString(), out double doubleValue))
            //         return doubleValue;
            //     throw new InvalidCastException($"Cannot convert {fieldValue} to DOUBLE.");

            default:
                return fieldValue; // Return the original value if no conversion is needed or type is not recognized
        }
    }




    public static void ApiModifyConnections(this NodeOption nodeop, ApiList a, object? newValue)
    {
        var connections = nodeop.Connections;
        foreach (var connect in connections)
        {
            a.Nodes[connect.AttachedNode.IdNode.ToString()].inputs[connect.Name] = newValue;
        }
    }




}


