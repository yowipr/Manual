using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core.Graphics;
using Manual.MUI;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manual.Objects;

public partial class Effect
{
    [JsonIgnore] public Shader shader { get; private set; }

    static internal Dictionary<Type, Shader> shaders = [];
    public void SetupShader()
    {
        var fragCode = FragShader();
        if (!string.IsNullOrEmpty(fragCode))
        {
            // int program = Rend3D.LoadShaders(Rend3D.GetVertexShader_FrameBuffer(), fragCode);
            var program = new Shader(Rend3D.GetVertexShader_FrameBuffer(), fragCode);
            shaders[this.GetType()] = program;
        }
    }
    protected Shader GetShader()
    {
        var program = shaders[this.GetType()];
        shader = program;
        return program;
    }

    public virtual string FragShader()
    {
        return null;
    }

    public void Internal_ApplyEffect(DoubleFrameBuffer framebuffer)
    {
        Type type = this.GetType();
        if (!shaders.ContainsKey(type))
        {
            SetupShader();
        }
        if(shader is null)
        {
            GetShader();
        }

        shader.Use();

        ApplyEffect(framebuffer);
    }

    public virtual void ApplyEffect(DoubleFrameBuffer framebuffer)
    {

    }

}




public partial class E_Blur
{
    public override string FragShader()
    {
        var src = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;
const float offset = 1.0 / 600.0;

void main() {
    float kernel[9] = float[](
        1.0 / 16, 2.0 / 16, 1.0 / 16,
        2.0 / 16, 4.0 / 16, 2.0 / 16,
        1.0 / 16, 2.0 / 16, 1.0 / 16
    );

    vec4 col = vec4(0.0);
    col += kernel[0] * texture(screenTexture, TexCoord.st + vec2(-offset, offset));
    col += kernel[1] * texture(screenTexture, TexCoord.st + vec2(0.0f, offset));
    col += kernel[2] * texture(screenTexture, TexCoord.st + vec2(offset, offset));
    col += kernel[3] * texture(screenTexture, TexCoord.st + vec2(-offset, 0.0f));
    col += kernel[4] * texture(screenTexture, TexCoord.st + vec2(0.0f, 0.0f));
    col += kernel[5] * texture(screenTexture, TexCoord.st + vec2(offset, 0.0f));
    col += kernel[6] * texture(screenTexture, TexCoord.st + vec2(-offset, -offset));
    col += kernel[7] * texture(screenTexture, TexCoord.st + vec2(0.0f, -offset));
    col += kernel[8] * texture(screenTexture, TexCoord.st + vec2(offset, -offset));

    FragColor = col;
}
";

        var src2 = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform bool horizontal;

// How far from the center to take samples from the fragment you are currently on
const int radius = 6;
// Keep it between 1.0f and 2.0f (the higher this is the further the blur reaches)
float spreadBlur = 2.0f;
float weights[radius];

void main()
{             
    // Calculate the weights using the Gaussian equation
    float x = 0.0f;
    for (int i = 0; i < radius; i++)
    {
        // Decides the distance between each sample on the Gaussian function
        if (spreadBlur <= 2.0f)
            x += 3.0f / radius;
        else
            x += 6.0f / radius;

        weights[i] = exp(-0.5f * pow(x / spreadBlur, 2.0f)) / (spreadBlur * sqrt(2 * 3.14159265f));
    }


    vec2 tex_offset = 1.0f / textureSize(screenTexture, 0);
    vec3 result = texture(screenTexture, TexCoord).rgb * weights[0];

    // Calculate horizontal blur
    if(horizontal)
    {
        for(int i = 1; i < radius; i++)
        {
            // Take into account pixels to the right
            result += texture(screenTexture, TexCoord + vec2(tex_offset.x * i, 0.0)).rgb * weights[i];
            // Take into account pixels on the left
            result += texture(screenTexture, TexCoord - vec2(tex_offset.x * i, 0.0)).rgb * weights[i];
        }
    }
    // Calculate vertical blur
    else
    {
        for(int i = 1; i < radius; i++)
        {
            // Take into account pixels above
            result += texture(screenTexture, TexCoord + vec2(0.0, tex_offset.y * i)).rgb * weights[i];
            // Take into account pixels below
            result += texture(screenTexture, TexCoord - vec2(0.0, tex_offset.y * i)).rgb * weights[i];
        }
    }
    FragColor = vec4(result, 1.0f);
}
";
        return src;
    }
    public override void ApplyEffect(DoubleFrameBuffer framebuffer)
    {
        //int resolutionLoc = GL.GetUniformLocation(shaderProgram, "resolution");
        //GL.Uniform1(resolutionLoc, 512.0f);

        bool horizontal = true, firstIteration = true;
        int amount = 8;
        for (int i = 0; i < amount; i++)
        {
            shader.SetUniform("screenTexture", 0);
            shader.SetUniform("horizontal", true);

            if (firstIteration)
            {
                firstIteration = false;
            }
        }

    }
}


public partial class E_InvertColor : Effect
{
    public E_InvertColor()
    {
        Name = "Invert Color";
    }

    public override string FragShader()
    {
        var src = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D texture1;

void main()
{
    vec4 texColor = texture(texture1, TexCoord);
    FragColor = vec4(1.0 - texColor.r, 1.0 - texColor.g, 1.0 - texColor.b, texColor.a);
}
";
        return src;
    }

    public override void ApplyEffect(DoubleFrameBuffer framebuffer)
    {

    }
}



public partial class E_Channels
{
    public override string FragShader()
    {
        var src = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform vec4 colorFactors;

void main()
{
    vec4 texColor = texture(screenTexture, TexCoord);
    texColor.r *= colorFactors.r;
    texColor.g *= colorFactors.g;
    texColor.b *= colorFactors.b;
    texColor.a *= colorFactors.a;
    FragColor = texColor;
}
";
        return src;
    }

    public override void ApplyEffect(DoubleFrameBuffer framebuffer)
    {
        Vector4 colorFactors = new Vector4(Red, Green, Blue, Alpha);
        shader.SetUniform("colorFactors", colorFactors);
    }
}




public partial class E_HueSaturationLights
{
    public override string FragShader()
    {
        var src = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;
uniform float hueAdjust;
uniform float saturationAdjust;
uniform float lightnessAdjust;

float hue2rgb(float p, float q, float t) {
    if (t < 0.0) t += 1.0;
    if (t > 1.0) t -= 1.0;
    if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
    if (t < 1.0 / 2.0) return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
}

vec3 rgb2hsl(vec3 color) {
    float r = color.r;
    float g = color.g;
    float b = color.b;
    
    float max = max(r, max(g, b));
    float min = min(r, min(g, b));
    float h, s, l = (max + min) / 2.0;

    if (max == min) {
        h = s = 0.0; // achromatic
    } else {
        float d = max - min;
        s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
        if (max == r) {
            h = (g - b) / d + (g < b ? 6.0 : 0.0);
        } else if (max == g) {
            h = (b - r) / d + 2.0;
        } else {
            h = (r - g) / d + 4.0;
        }
        h /= 6.0;
    }
    return vec3(h, s, l);
}

vec3 hsl2rgb(vec3 color) {
    float h = color.x;
    float s = color.y;
    float l = color.z;

    float r, g, b;

    if (s == 0.0) {
        r = g = b = l; // achromatic
    } else {
        float q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
        float p = 2.0 * l - q;
        r = hue2rgb(p, q, h + 1.0 / 3.0);
        g = hue2rgb(p, q, h);
        b = hue2rgb(p, q, h - 1.0 / 3.0);
    }
    return vec3(r, g, b);
}

void main() {
    vec4 texColor = texture(screenTexture, TexCoord);
    vec3 hsl = rgb2hsl(texColor.rgb);

    // Ajustes de HSL
    hsl.x += hueAdjust;
    hsl.y *= (1.0 + saturationAdjust);
    hsl.z += lightnessAdjust;

    // Corregir valores fuera de rango
    if (hsl.x > 1.0) hsl.x -= 1.0;
    if (hsl.x < 0.0) hsl.x += 1.0;
    hsl.y = clamp(hsl.y, 0.0, 1.0);
    hsl.z = clamp(hsl.z, 0.0, 1.0);

    vec3 rgb = hsl2rgb(hsl);
    FragColor = vec4(rgb, texColor.a);
}
";
        return src;
    }

    public override void ApplyEffect(DoubleFrameBuffer framebuffer)
    {
        shader.SetUniform("hueAdjust", Hue);
        shader.SetUniform("saturationAdjust", Saturation);
        shader.SetUniform("lightnessAdjust", Lights);
    }

    [ObservableProperty]
    private float hue = 0.0f; // Rango típico 0-360

    [ObservableProperty]
    private float saturation = 0.0f; // Rango típico 0-2 donde 1 es sin cambio

    [ObservableProperty]
    private float lights = 0.0f; // Rango típico 0-2 donde 1 es sin cambio

    public E_HueSaturationLights()
    {
        Name = "Hue/Saturation/Lights";

        ui(new M_SliderBox("Hue", -1.0f, 1.0f, 50, 0.01f, true));
        ui(new M_SliderBox("Saturation", -1.0f, 1.0f, 50, 0.01f, true));
        ui(new M_SliderBox("Lights", -1.0f, 1.0f, 50, 0.01f, true));
    }
}
