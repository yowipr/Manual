using CefSharp.DevTools.CSS;
using CefSharp.DevTools.Profiler;
using Manual.API;
using Manual.Core;
using Manual.Editors;
using Manual.Objects;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;



namespace Manual.Core.Graphics;

public static class MR
{
    public static void Clear()
    {
        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public static void SetBlendMode(LayerBlendMode mode)
    {

        switch (mode)
        {
            case LayerBlendMode.Normal:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
              //  GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                break;
            case LayerBlendMode.Multiply:
           //     GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                GL.BlendFuncSeparate(BlendingFactorSrc.DstColor, BlendingFactorDest.Zero, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.DstAlpha);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                break;
            case LayerBlendMode.Add:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                break;
            case LayerBlendMode.Screen:
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcColor);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                break;
            case LayerBlendMode.Difference:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
                break;
            default:
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                break;
        }
    }


    public static Vector3 NormalizeRGB(int r, int g, int b)
    {
        return new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
    }

    public static Vector3 ToVector3(this Point point, float z)
    {
        return new Vector3((float)point.X, (float)point.Y, z);
    }
    public static Vector3 ToVector3(this System.Drawing.PointF point, float z)
    {
        return new Vector3(point.X, point.Y, z);
    }



    private static BufferTarget _bufferTarget;
    private static int _buffer;
    public static void Select(int buffer)
    {
        _buffer = buffer;
        GL.BindBuffer(_bufferTarget, buffer);
    }
    public static void Deselect()
    {
        GL.BindBuffer(_bufferTarget, 0);
    }

    public static int GenBuffer(BufferTarget bufferTarget, float[] data)
    {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(bufferTarget, buffer);
        GL.BufferData(bufferTarget, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
        return buffer;
    }

    public static int GenVao()
    {
        return GL.GenVertexArray();
    }

    public static void Vao_SetBuffer_FloatArray(int vao, float[] data, int index = 0, int size = 3, VertexAttribPointerType type = VertexAttribPointerType.Float, bool normalized = false, int stride = 3 * sizeof(float), int offset = 0)
    {
        GL.BindVertexArray(vao);
        int vbo = GenBuffer(BufferTarget.ArrayBuffer, data);
        GL.VertexAttribPointer(index, size, type, normalized, stride, offset);
        GL.EnableVertexAttribArray(index);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

}


public static class Rend3D
{

    public static GLWpfControl openGLControl;
    public static int _program;
    public static int _programFrameBuffer;


    public static void UpdateQuality(Quality value)
    {
        foreach (var context in _contexts)
            context.SetQuality(value);      
    }

 


    public static void UpdateRender()
    {
        foreach (var context in _contexts)
            context.CanvasView.glControl?.InvalidateVisual();       
    }


    public static bool isInitialized;
    private static void SetupShaders()
    {
         _program = GetShadersProgram();
        _programFrameBuffer = LoadShaders(GetVertexShader_FrameBuffer(), GetFragmentShader_FrameBuffer());

    }
    public static int GetShadersProgram()
    {
        string vertexShaderSource = GetVertexShader();
        string fragmentShaderSource = GetFragmentShader();

        return LoadShaders(vertexShaderSource, fragmentShaderSource);
    }

    public static string GetVertexShader_Simple()
    {
        string fragmentShaderSource = @"
#version 460 core

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
}
        ";
        return fragmentShaderSource;
    }
    public static string GetFragmentShader_Simple()
    {
        string fragmentShaderSource = @"
          #version 460 core

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 0.0, 0.0, 1.0); // Rojo
}
        ";
        return fragmentShaderSource;
    }

    public static string GetVertexShader_FrameBuffer()
    {
        string fragmentShaderSource = @"
#version 460 core

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
    TexCoord = aTexCoord;
}
";
        return fragmentShaderSource;
    }
    public static string GetFragmentShader_FrameBuffer()
    {
        string fragmentShaderSource = @"
#version 460 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D screenTexture;

void main()
{
     FragColor = texture(screenTexture, TexCoord);
}
";
        return fragmentShaderSource;
    }


    public static string GetVertexShader()
    {
        string vertexShaderSource = @"
            #version 460 core

            layout(location = 0) in vec3 aPos;
            layout(location = 1) in vec2 aTexCoord;

            out vec2 TexCoord;

            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;

            void main()
            {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                TexCoord = aTexCoord;
            }
        ";
        return vertexShaderSource;
    }

    public static string GetFragmentShader()
    {
        string fragmentShaderSource = @"
            #version 460 core

            in vec2 TexCoord;
            out vec4 FragColor;

            uniform sampler2D texture1;
            uniform float opacity;

            void main()
            {
                vec4 texColor = texture(texture1, TexCoord);
                FragColor = vec4(texColor.rgb, texColor.a * opacity);
            }
        ";
        return fragmentShaderSource;
    }

    public static int LoadShaders(string vertexShaderSource, string fragmentShaderSource)
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompileErrors(vertexShader, "VERTEX");

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompileErrors(fragmentShader, "FRAGMENT");

        int shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);
        CheckProgramLinkErrors(shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return shaderProgram;
    }

    private static void CheckShaderCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"ERROR::SHADER_COMPILATION_ERROR of type: {type}\n{infoLog}");
        }
    }

    private static void CheckProgramLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"ERROR::PROGRAM_LINKING_ERROR\n{infoLog}");
        }
    }




    //------------------------------------------------------------------------------ REGISTER CANVAS VIEW
    private static List<RenderContext> _contexts = [];

    public static void RegisterCanvasView(CanvasView canvasView)
    {
        var context = new RenderContext(canvasView);
        _contexts.Add(context);
        context.Start();

        if (!isInitialized)
        {
            Start();
            isInitialized = true;
        }

    }

    public static void UnregisterCanvasView(CanvasView canvasView)
    {
        var context = _contexts.FirstOrDefault(rc => rc.CanvasView == canvasView);
        _contexts.Remove(context);
        context.Dispose();
    }

    //-------------------------------------------------------------------------------------------- START
    public static void Start()
    {
        SetupDebugCallback();

        //ANTIALIASING
        GL.Hint(HintTarget.MultisampleFilterHintNv, HintMode.Nicest);
        GL.Enable(EnableCap.Multisample);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

         //GL.Enable(EnableCap.PolygonSmooth);
        // GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);

        SetupShaders();

        
    }

  

    private static void SetupDebugCallback()
    {
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(DebugCallback, IntPtr.Zero);
    }

    private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
        if (Output.DEBUG_BUILD())
        {
            string messageString = Marshal.PtrToStringAnsi(message, length);
            if (messageString.StartsWith("Buffer detailed info"))
                return;

            Output.Log($"source:\n{source} \n\ntype:\n{type} \n\nseverity:\n{severity} \n\nmessage:\n{messageString}", "GL DEBUG");
        }
    }

}








public class Shader
{
    public readonly int program;
    private readonly Dictionary<string, int> uniformLocations;

    public Shader(string vertexShaderSource, string fragmentShaderSource)
    {

        // Compile shaders
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        // Link shaders into a program
        program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        // Check for linking errors
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Error linking shader program: {infoLog}");
        }

        // Cleanup shaders as they are no longer needed after linking
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // Cache uniform locations
        uniformLocations = new Dictionary<string, int>();
        int numberOfUniforms = 0;
        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out numberOfUniforms);
        GL.GetProgram(program, GetProgramParameterName.ActiveUniformMaxLength, out int maxNameLength);
        for (int i = 0; i < numberOfUniforms; i++)
        {
            GL.GetActiveUniform(program, i, maxNameLength, out int length, out int size, out ActiveUniformType type, out string name);
          //  GL.GetActiveUniform(handle, i, out _, out _, out _, out string name);
            int location = GL.GetUniformLocation(program, name);
            uniformLocations.Add(name, location);
        }
    }

    private int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        // Check for compilation errors
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error compiling {type} shader: {infoLog}");
        }

        return shader;
    }

    public void Use()
    {
        GL.UseProgram(program);
    }

    public int GetUniformLocation(string name)
    {
        return uniformLocations.TryGetValue(name, out int location) ? location : -1;
    }

    public void SetUniform(string name, int value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void SetUniform(string name, float value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void SetUniform(string name, Matrix4 value)
    {
        GL.UniformMatrix4(GetUniformLocation(name), false, ref value);
    }

    public void SetUniform(string name, bool value)
    {
        GL.Uniform1(GetUniformLocation(name), value ? 1 : 0);
    }
    public void SetUniform(string name, Vector4 value)
    {
        GL.Uniform4(GetUniformLocation(name), value);
    }
}




public class RenderBuffer
{
    int framebuffer;
    int texture;
    int rbo;

    public int Samples = 8;
    public int Width { get; set; }
    public int Height { get; set; }
    public RenderBuffer()
    {
            
    } 


    public void Dispose()
    {
        GL.DeleteFramebuffer(framebuffer);
        GL.DeleteTexture(texture);
        GL.DeleteRenderbuffer(rbo);
    }
    public void UpdateFrameBuffer(int width, int height)
    {
        if (framebuffer != 0)
        {
            GL.DeleteFramebuffer(framebuffer);
            GL.DeleteTexture(texture);
            GL.DeleteRenderbuffer(rbo);
        }

        SetupFrameBuffer(width, height);
    }

    public void SetupFrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;

        framebuffer = GL.GenFramebuffer();

        //TEXTURE
        texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DMultisample, texture);
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, Samples, PixelInternalFormat.Rgba, width, height, true);

        GL.BindTexture(TextureTarget.Texture2DMultisample, 0);

        //RENDERBUFFER FOR SAMPLING
        rbo = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Samples, RenderbufferStorage.Depth24Stencil8, width, height);


        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, texture, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, 0);

        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Framebuffer not complete!");


        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }



    int oldFb;
    public void StartDraw()
    {
        //RENDER FRAMEBUFFER
        oldFb = GL.GetInteger(GetPName.FramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
        GL.Enable(EnableCap.DepthTest);

        //NORMAL RENDER
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// draws the result in the framebuffer
    /// </summary>
    /// <param name="postFrame"></param>
    public void EndDraw(FrameBuffer postFrame)
    {
        GL.Disable(EnableCap.DepthTest);
        //POST DRAW
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, postFrame.framebuffer);
        var width = Width; var height = Height;
        GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);


        GL.BindFramebuffer(FramebufferTarget.Framebuffer, oldFb);
        GL.Enable(EnableCap.DepthTest);
    }
}



public class FrameBuffer
{
    internal int framebuffer;
    internal int texture;
    public FrameBuffer()
    {
            
    }

    public virtual void Dispose()
    {
        GL.DeleteFramebuffer(framebuffer);
        GL.DeleteTexture(texture);
    }
    public virtual void UpdateFrameBuffer(int width, int height)
    {
        if (framebuffer != 0)
        {
            GL.DeleteFramebuffer(framebuffer);
            GL.DeleteTexture(texture);
        }


        var tuples = SetupFrameBuffer(width, height);

        framebuffer = tuples._frameBuffer;
        texture = tuples._texture;
    }

    (int _frameBuffer, int _texture) SetupFrameBuffer(int width, int height)
    {
        int _framebuffer, _texture;

        //FRAMEBUFFER
        _framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _texture, 0);
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            throw new Exception("Framebuffer not complete!");

        GL.BindTexture(TextureTarget.Texture2D, 0);

        return (_framebuffer, _texture);

    }



    public virtual void Draw()
    {
        GL.BindVertexArray(RenderContext.vao_fullscreenQuad);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }


    public void Select()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
    }
}



public class DoubleFrameBuffer : FrameBuffer
{
    FrameBuffer frame0 = new();
    FrameBuffer frame1 = new();

    public DoubleFrameBuffer()
    {
        
    }
    public override void Dispose()
    {
        frame0.Dispose();
        frame1.Dispose();
    }
    public override void UpdateFrameBuffer(int width, int height)
    {
        frame0.UpdateFrameBuffer(width, height);
        frame1.UpdateFrameBuffer(width, height);


        if (framebuffer == frame1.framebuffer)
        {
            framebuffer = frame1.framebuffer;
            texture = frame1.texture;
        }
        else
        {
            framebuffer = frame0.framebuffer;
            texture = frame0.texture;
        }

    }


    public void Switch()
    {
        if (framebuffer == frame0.framebuffer)
        {
            SetFrameBuffer(1);
        }
        else
        {
            SetFrameBuffer(0);
        }
    }
    public void SetFrameBuffer(int frame)
    {
        if (frame == 1)
        {
            framebuffer = frame1.framebuffer;
            texture = frame1.texture;
        }
        else
        {
            framebuffer = frame0.framebuffer;
            texture = frame0.texture;
        }
    }

    /// <summary>
    /// 0, 1, and -1 for the opposite
    /// </summary>
    /// <param name="frame"></param>
    public void Select(int frame)
    {
        if (frame == -1)
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GetFramebuffer(opposite: true));
        else if (frame == 0)
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame0.framebuffer);
        else if (frame == 1)
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame1.framebuffer);
        
    }
    public void SelectOpposite()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, GetFramebuffer(opposite: true));
    }


    /// <summary>
    /// switch framebuffer and draw the quad screen
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }

    /// <summary>
    /// this doesn't switch
    /// </summary>
    public void ContinueDraw()
    {
        Draw();
    }


    public int GetFramebuffer(bool opposite = false)
    {
        if (opposite && framebuffer == frame0.framebuffer)
        {
            return frame1.framebuffer;
        }
        else
            return framebuffer;
    }


}