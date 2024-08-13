using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FFmpeg.AutoGen;
using Manual.API;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;


namespace Manual.Core.Graphics;

public static class RendGL
{
    public static void Test()
    {
        UpdateVertices();
    }

    public static void Shutdown()
    {
        if(window != null)
            window.Close();
    }

    private static IWindow window;
    private static GL gl;
    static uint program;

    private static uint texture;
  

    public static void StartLoad()
    {

         System.Windows.Application.Current.Dispatcher.BeginInvoke(() => {
        // Ejecutar window.Run en un hilo separado
      
        var options = WindowOptions.Default;

        options.ShouldSwapAutomatically = false; // Impide que la ventana intercambie buffers automáticamente
        options.VSync = false; // Desactiva VSync para que puedas dibujar tan rápido como tu bucle lo permita

        options.TopMost = true;
        options.IsVisible = true;
        options.Size = new Silk.NET.Maths.Vector2D<int>(512, 512);
        options.Title = "Silk.NET OpenGL Example";

        window = Window.Create(options);
        window.Load += OnLoad;
        window.Render += OnRender;

              
         window.Run();

         }, System.Windows.Threading.DispatcherPriority.Render);

        // Wait to load});

    }


    private static unsafe void OnLoad()
    {
        window.MakeCurrent(); // Suponiendo que 'window' es tu objeto de ventana que tiene un contexto de OpenGL// Asegúrate de llamar a esto en el hilo donde vas a usar OpenGL


        gl = window.CreateOpenGL();
        LoadLayer();

        const string VertexShaderSource = @"
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
        }
        ";


        const string FragmentShaderSource = @"
        #version 330 core

        out vec4 out_color;

        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }
        ";

        uint vshader = gl.CreateShader(ShaderType.VertexShader);
        uint fshader = gl.CreateShader(ShaderType.FragmentShader);

        //asign GLSL code
        gl.ShaderSource(vshader, VertexShaderSource);
        gl.ShaderSource(fshader, FragmentShaderSource);

        gl.CompileShader(vshader);
        gl.CompileShader(fshader);

        //check if shaders compiled
        gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vshader));
        gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int vStatus2);
        if (vStatus2 != (int)GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fshader));


        program = gl.CreateProgram();
        gl.AttachShader(program, vshader);
        gl.AttachShader(program, fshader);

        gl.LinkProgram(program);
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(program));


        gl.DetachShader(program, vshader);
        gl.DetachShader(program, fshader);

        gl.DeleteShader(vshader);
        gl.DeleteShader(fshader);


        gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0)
            Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(program)}");



        //layer probably

        const uint positionLoc = 0;
        gl.EnableVertexAttribArray(positionLoc);
        gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);




      
    }
  








    //------------------------------------------------------------------------------------------------------------------------------- UPDATE
    private static unsafe void OnRender(double delta)
    {
        //UI
        //RenderFrame();
    }


    public static unsafe void UpdateFrame()
    {
        gl.Clear(ClearBufferMask.ColorBufferBit);

        gl.ClearColor(Color.Gray);

        //Draw Layer
        gl.BindVertexArray(vao);
        gl.UseProgram(program);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);

        DrawLayer();


        window.SwapBuffers();
        gl.Finish();
    }
    public static unsafe WriteableBitmap RenderFrame()
    {
        UpdateFrame();
        return CaptureFrame(512, 512);
    }

    public static WriteableBitmap CaptureFrame(int width, int height)
    {
        // Crear buffer para almacenar los píxeles
        var pixelData = new byte[width * height * 4];  // 4 bytes por píxel en RGBA

        // Usar código no seguro para pasar un puntero a glReadPixels
        unsafe
        {
            fixed (byte* pPixelData = pixelData)
                gl.ReadPixels(0, 0, (uint)width, (uint)height, GLEnum.Bgra, GLEnum.UnsignedByte, pPixelData);
        }

        // Crear un WriteableBitmap para cargar los datos capturados
        var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.Lock();
        Marshal.Copy(pixelData, 0, bitmap.BackBuffer, pixelData.Length);
        bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
        bitmap.Unlock();

        return bitmap;
    }
    public static unsafe WriteableBitmap createWriteableBitmap(int h, int w)
    {
        // create a new instance of a WriteableBitmap
        var m_writeableImg = new WriteableBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);

        // cast the IntPtr to a char* for the native C++ engine
        var m_bufferPtr = (char*)m_writeableImg.BackBuffer.ToPointer();

        // update the source of the Image control
        return m_writeableImg;
    }




    static unsafe void DrawTriangle()
    {
        // Create the VAO.
        uint vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        uint vertices = gl.GenBuffer();
        uint colors = gl.GenBuffer();
        uint indices = gl.GenBuffer();

        float[] vertexArray = new float[]
        {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            0.0f, 0.5f, 0.0f,
        };

        float[] colorsArray = new float[]
        {
            1.0f, 0f, 0.0f, 1.0f,
            0.0f, 0.0f, 1.0f, 1.0f,
            0.0f, 1.0f, 0.0f, 1.0f,
        };
        uint[] indexArray = new uint[]
        {
            0, 1, 2
        };

        //vertex
        gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
        gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 0, null);
        gl.EnableVertexAttribArray(0);

        //colors
        gl.BindBuffer(GLEnum.ArrayBuffer, colors);
        gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorsArray.AsSpan(), GLEnum.StaticDraw);
        gl.VertexAttribPointer(1, 4, GLEnum.Float, false, 4 * sizeof(float), null);
        gl.EnableVertexAttribArray(1);

        //unbind colors
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);

        //register vertex
        gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
        gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);


        gl.UseProgram(program);

        gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, DrawElementsType.UnsignedInt, null);
        gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);


        gl.BindVertexArray(vao);

        gl.DeleteBuffer(vertices);
        gl.DeleteBuffer(colors);
        gl.DeleteBuffer(indices);
        gl.DeleteVertexArray(vao);
    }







    private static uint vao;
    private static uint vbo;
    private static uint ebo;
    static unsafe void LoadLayer()
    {
        // Create the VAO.
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        float[] vertices =
        {
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        };

        //VBO
        vbo = gl.GenBuffer();
        verticesBufferId = vbo;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* buf = vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);

    
        
        uint[] indices =
        {
            0u, 1u, 3u,
            1u, 2u, 3u
        };

        //EBO
        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* buf = indices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
    }




    static unsafe void DrawLayer()
    {
        UpdateVertices();
    }

    static uint verticesBufferId;
    public static void UpdateVertices()
    {
        var x = ManualAPI.SelectedLayer.PositionX / 100;
        var y = ManualAPI.SelectedLayer.PositionY / 100;

       // Output.Log("vers changed");
       // float[] newVertices =
       //{
       //      0.2f,  0.5f, 0.0f,
       //      0.5f, -0.5f, 0.0f,
       //     -0.5f, -0.5f, 0.0f,
       //     -0.5f,  0.5f, 0.0f
       // };
        float[] newVertices =
        {
             0.2f + x,  0.5f - y, 0.0f,
             0.5f + x, -0.5f - y, 0.0f,
            -0.5f + x, -0.5f - y, 0.0f,
            -0.5f + x,  0.5f - y, 0.0f
        };

        // Asegúrate de enlazar el buffer correcto
        gl.BindBuffer(GLEnum.ArrayBuffer, verticesBufferId);

        // Actualiza los datos del buffer con los nuevos vértices

        gl.BufferSubData<float>(GLEnum.ArrayBuffer, IntPtr.Zero, (nuint)(newVertices.Length * sizeof(float)), newVertices);

        // Desenlaza el buffer para evitar modificaciones accidentales
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);



    }







    public static OpenGLHost GetHost()
    {
        return new OpenGLHost();
    }


}












public class OpenGLHost : HwndHost
{
    private IWindow window;
    private GL gl;
    static uint program;

    private static uint texture;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        var options = WindowOptions.Default;
        options.ShouldSwapAutomatically = true;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6));
        options.WindowBorder = WindowBorder.Hidden;
        options.Size = new Silk.NET.Maths.Vector2D<int>(300, 300);

        window = Window.Create(options);
        window.Initialize();

        gl = window.CreateOpenGL();

        OnLoad();

        unsafe
        {

            var nativeWindow = window.Native.DXHandle;
            if (nativeWindow != null)
            {
                return new HandleRef(this, nativeWindow.Value);
            }
        }


        throw new InvalidOperationException("No se pudo obtener la manija de la ventana nativa.");
    }



    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        window?.Dispose();
    }

    public void Render()
    {
        window.DoEvents(); // Process window events
     

        //update frame
        UpdateFrame();

        window.SwapBuffers(); // Swap buffers
        gl.Finish();
    }
    public unsafe void UpdateFrame()
    {
        gl.Clear(ClearBufferMask.ColorBufferBit);
        gl.ClearColor(Color.Gray);

        //Draw Layer
        gl.BindVertexArray(vao);
        gl.UseProgram(program);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);

        DrawLayer();


        window.SwapBuffers();
        gl.Finish();
    }




    private unsafe void OnLoad()
    {
       // window.MakeCurrent(); // Suponiendo que 'window' es tu objeto de ventana que tiene un contexto de OpenGL// Asegúrate de llamar a esto en el hilo donde vas a usar OpenGL


    //    gl = window.CreateOpenGL();
        LoadLayer();



        // SHADERS
        const string VertexShaderSource = @"
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        void main()
        {
            gl_Position = vec4(aPosition, 1.0);
        }
        ";


        const string FragmentShaderSource = @"
        #version 330 core

        out vec4 out_color;

        void main()
        {
            out_color = vec4(1.0, 0.5, 0.2, 1.0);
        }
        ";

        uint vshader = gl.CreateShader(ShaderType.VertexShader);
        uint fshader = gl.CreateShader(ShaderType.FragmentShader);

        //asign GLSL code
        gl.ShaderSource(vshader, VertexShaderSource);
        gl.ShaderSource(fshader, FragmentShaderSource);

        gl.CompileShader(vshader);
        gl.CompileShader(fshader);

        //check if shaders compiled
        gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + gl.GetShaderInfoLog(vshader));
        gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int vStatus2);
        if (vStatus2 != (int)GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + gl.GetShaderInfoLog(fshader));


        program = gl.CreateProgram();
        gl.AttachShader(program, vshader);
        gl.AttachShader(program, fshader);

        gl.LinkProgram(program);
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw new Exception("Program failed to link: " + gl.GetProgramInfoLog(program));


        gl.DetachShader(program, vshader);
        gl.DetachShader(program, fshader);

        gl.DeleteShader(vshader);
        gl.DeleteShader(fshader);


        gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0)
            Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(program)}");



        //layer probably

        const uint positionLoc = 0;
        gl.EnableVertexAttribArray(positionLoc);
        gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);





    }



    private static uint vao;
    private static uint vbo;
    private static uint ebo;
    unsafe void LoadLayer()
    {
        // Create the VAO.
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        float[] vertices =
        {
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        };

        //VBO
        vbo = gl.GenBuffer();
        verticesBufferId = vbo;
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* buf = vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);



        uint[] indices =
        {
            0u, 1u, 3u,
            1u, 2u, 3u
        };

        //EBO
        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* buf = indices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
    }



    unsafe void DrawLayer()
    {
        UpdateVertices();
    }

    uint verticesBufferId;
    public void UpdateVertices()
    {
        var x = ManualAPI.SelectedLayer.PositionX / 100;
        var y = ManualAPI.SelectedLayer.PositionY / 100;

        // Output.Log("vers changed");
        // float[] newVertices =
        //{
        //      0.2f,  0.5f, 0.0f,
        //      0.5f, -0.5f, 0.0f,
        //     -0.5f, -0.5f, 0.0f,
        //     -0.5f,  0.5f, 0.0f
        // };
        float[] newVertices =
        {
             0.2f + x,  0.5f - y, 0.0f,
             0.5f + x, -0.5f - y, 0.0f,
            -0.5f + x, -0.5f - y, 0.0f,
            -0.5f + x,  0.5f - y, 0.0f
        };

        // Asegúrate de enlazar el buffer correcto
        gl.BindBuffer(GLEnum.ArrayBuffer, verticesBufferId);

        // Actualiza los datos del buffer con los nuevos vértices

        gl.BufferSubData<float>(GLEnum.ArrayBuffer, IntPtr.Zero, (nuint)(newVertices.Length * sizeof(float)), newVertices);

        // Desenlaza el buffer para evitar modificaciones accidentales
        gl.BindBuffer(GLEnum.ArrayBuffer, 0);



    }

}