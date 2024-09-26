using MS.WindowsAPICodePack.Internal;
using Newtonsoft.Json.Linq;
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



    public static void ApiChangeNode_OutputToPreview(JObject graph, int outputId)
    {
        // Verifica si existe el nodo "1" y si contiene los campos esperados
        var node = graph[outputId.ToString()];
        if (node != null)
        {
            // Cambia el contenido de "inputs" -> "result" por "images"
            var inputs = node["inputs"];
            if (inputs != null && inputs["result"] != null)
            {
                // Modifica la propiedad "result" por "images"
                inputs["images"] = inputs["result"];

                // Usa la clave del valor que quieres eliminar
                (inputs as JObject)?.Remove("result"); // Elimina "result"
            }


            // Cambia el valor de "class_type"
            node["class_type"] = "PreviewImage";

            // Cambia el título en "_meta" -> "title"
            var meta = node["_meta"];
            if (meta != null && meta["title"] != null)
            {
                meta["title"] = "Preview Image";
            }
        }
    }



}


