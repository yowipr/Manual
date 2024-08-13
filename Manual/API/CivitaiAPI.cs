using Manual.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Manual.API;

internal static class CivitaiAPI
{
    public static Prompt GenerationDataToPrompt(string text)
    {
        var lines = text.Split('\n');
        var generationData = new Prompt();

        // Asignar PositivePrompt
        generationData.PositivePrompt = lines[0].Trim();

        // Asignar NegativePrompt
        var negativePromptMatch = Regex.Match(lines[1], @"Negative prompt:\s*(.*)", RegexOptions.IgnoreCase);
        if (negativePromptMatch.Success)
        {
            generationData.NegativePrompt = negativePromptMatch.Groups[1].Value.Trim();
        }

        // Analizar los parámetros
        var parameterLine = lines[2];
        var parameters = parameterLine.Split(',').Select(p => p.Trim()).ToList();
        foreach (var parameter in parameters)
        {
            var parts = parameter.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "Steps":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float steps))
                            generationData.Steps = steps;
                        break;
                    case "CFG scale":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float cfg))
                            generationData.CFG = cfg;                   
                        break;
                    case "Sampler":
                            //generationData.Sampler = value;
                        break;
                    case "Seed":
                        if (ulong.TryParse(value, CultureInfo.InvariantCulture, out ulong seed))
                            generationData.Seed = seed;
                        break;
                        // Agrega aquí otros parámetros que necesites
                }
            }
        }

        return generationData;
    }

    public static void ApplyGenerationDataToPrompt(Prompt prompt, string generationData)
    {
        var genData = GenerationDataToPrompt(generationData);
        ApplyGenerationDataToPrompt(prompt, genData);
    }
    public static void ApplyGenerationDataToPrompt(Prompt prompt, Prompt generationData)
    {
        prompt.RealPositivePrompt = "";
        prompt.RealNegativePrompt = "";

        if (!string.IsNullOrEmpty(generationData.PositivePrompt))
        {
            prompt.PositivePrompt = generationData.PositivePrompt;
        }

        if (!string.IsNullOrEmpty(generationData.NegativePrompt))
        {
            prompt.NegativePrompt = generationData.NegativePrompt;
        }

    }
}