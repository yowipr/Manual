using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using CefSharp.DevTools.LayerTree;
using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Editors;
using Manual.Objects;
using Manual.Objects.UI;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;

namespace Manual.Core.Graphics;


public class RenderContext
{

    internal int _samples = 8;
    internal int Samples
    {
        get => _samples;
        private set
        {
            _samples = value;
            CanvasView.glControl.Settings.Samples = value;
        }
    }

    int RealWidth;
    int RealHeight;

    public void SetQuality(Quality value)
    {
        switch (value)
        {
            case Quality.Full:
                Samples = 8;

              //   LowWidth = 1;
              //   LowHeight = 1;
                break;
            case Quality.Medium:
                Samples = 4;

              //  LowWidth = 2;
             //   LowHeight = 2;
                break;
            case Quality.Low:
                Samples = 2;

              //  LowWidth = 4;
             //   LowHeight = 4;
                break;
            case Quality.Very_Low:
                Samples = 1;

             //   LowWidth = 8;
             //   LowHeight = 8;
                break;
            default:
                Console.WriteLine("Unknown quality setting.");
                break;
        }
        UpdateFramebuffer(RealWidth, RealHeight);

        Shot.UpdateCurrentRender();
    }

    public CanvasView CanvasView { get; set; }

    public RenderContext(CanvasView canvasView)
    {
        this.CanvasView = canvasView;
    }
    public void Start()
    {

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 4,
            MinorVersion = 6,
            RenderContinuously = false,
            Profile = OpenTK.Windowing.Common.ContextProfile.Compatability,
            Samples = this.Samples,
        };
        
        var control = CanvasView.glControl;
        control.Start(settings);
        control.Context?.MakeCurrent();
        control.Render += Render;
        control.SizeChanged += Control_SizeChanged;
        control.Context.SwapInterval = 1; // Activa VSync

        if (vao_fullscreenQuad == 0)
          StartFullscreenQuad();
    }

    private void Control_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        // Obtiene las nuevas dimensiones del control
         //  int newWidth = (int)e.NewSize.Width;
        //  int newHeight = (int)e.NewSize.Height;

        RealWidth = (int)e.NewSize.Width + 1;
        RealHeight = (int)e.NewSize.Height;

        // Actualiza el framebuffer
        UpdateFramebuffer(RealWidth, RealHeight);
    }
    public void Dispose()
    {
        CanvasView.glControl.Render -= Render;
        CanvasView.glControl.SizeChanged -= Control_SizeChanged;
    }

    //------------------------------------------------------------------------------------- MAIN SHOT RENDER
    private void Render(TimeSpan obj)
    {
        var ed = CanvasView.DataContext as ED_CanvasView;
        var MainCamera = ed.ViewportCamera;

        var _shaderProgram = Rend3D._program;

        //BACKGROUND
        //var bg = MR.NormalizeRGB(51, 51, 51);
        if (!ed.EnableGrid)
          GL.ClearColor(0.1f, 0.2f, 0.5f, 1.0f); //blue
        else
           GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f); //manual

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        //PROGRAM
        GL.UseProgram(_shaderProgram);


        // Configurar la cámara usando MainCamera de Shot3D
        float aspectRatio = ((float)CanvasView.glControl.ActualWidth) / ((float)CanvasView.glControl.ActualHeight);
        Matrix4 projection = MainCamera.GetProjectionMatrix(aspectRatio);
        Matrix4 view = MainCamera.GetViewMatrix();

        int projectionLoc = GL.GetUniformLocation(_shaderProgram, "projection");
        GL.UniformMatrix4(projectionLoc, false, ref projection);

        int viewLoc = GL.GetUniformLocation(_shaderProgram, "view");
        GL.UniformMatrix4(viewLoc, false, ref view);


        //LAYERS
        foreach (var layer in ManualAPI.SelectedShot.LayersR)
        {
            if ((!layer.Visible || !layer._Animation.IsActuallyVisible) || layer is UILayerBase)
                continue;


            if (layer.Effects.Any())
            {
                RenderLayer(layer);
            }
            else
            {
                layer.Internal_Render();
            }

        }

        //END
        GL.UseProgram(0);
    }


    //-------------------------------------- FULLSCREEN QUAD

    public static int vao_fullscreenQuad;

    void StartFullscreenQuad()
    {
        float[] quadVertices = {
        // positions        // texture coords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
    };

        vao_fullscreenQuad = GL.GenVertexArray();
        int vertexBufferObject = GL.GenBuffer();

        GL.BindVertexArray(vao_fullscreenQuad);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

       
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);


        //SetupFramebuffer(800, 600);
        UpdateFramebuffer(800, 600);
    }



    RenderBuffer render = new();
    DoubleFrameBuffer postProcess = new();


    //-------------------- FRAME BUFFER

    private int framebuffer;
    private int texture;
    private int rbo;

    private int postFramebuffer;
    private int postTexture;
    private void UpdateFramebuffer(int width, int height)
    {
       // width += 1;
        render.UpdateFrameBuffer(width, height);
        postProcess.UpdateFrameBuffer(width, height);
    }
    private void RenderLayer(LayerBase layer)
    {

        //RENDER 3D SAMPLING
        var oldFb = GL.GetInteger(GetPName.FramebufferBinding);

        render.StartDraw();

        layer.Internal_Render();


        postProcess.SetFrameBuffer(0);
        render.EndDraw(postProcess);


        //POST PROCESS

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        GL.UseProgram(Rend3D._programFrameBuffer);
        GL.Disable(EnableCap.DepthTest);

        int enabledEffectCount = 0;

        // EFFECTS
        for (int i = 0; i < layer.Effects.Count; i++)
        {
            var effect = layer.Effects[i];
            if (!effect.Enabled) continue;

            postProcess.Select(enabledEffectCount % 2 == 0 ? 1 : 0);
            MR.Clear();

            postProcess.SetFrameBuffer(enabledEffectCount % 2 == 0 ? 0 : 1);
            effect.Internal_ApplyEffect(postProcess);
            postProcess.Draw();
            enabledEffectCount++;
        }

        // RENDER FINAL
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        postProcess.SetFrameBuffer(enabledEffectCount % 2 == 0 ? 0 : 1);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, oldFb);
        GL.UseProgram(Rend3D._programFrameBuffer);
        postProcess.Draw();

        GL.Enable(EnableCap.DepthTest);
    }

}


public class CanvasObject3D : ObservableObject
{
    [JsonIgnore] public bool _isInitializedMesh = false;
    [JsonIgnore] public int _vao { get; set; }
    [JsonIgnore] public int _vbo { get; set; }
    [JsonIgnore] public int _ebo { get; set; }

    // Propiedades básicas para transformación
    [JsonIgnore] public Vector3 _Position { get; set; }
    [JsonIgnore] public Vector3 _Scale { get; set; }
    [JsonIgnore] public Quaternion _Rotation { get; set; }

    // Matriz de modelo para operaciones de transformación
    public CanvasObject3D()
    {
        _Position = Vector3.Zero;
        _Scale = Vector3.One;
        _Rotation = Quaternion.Identity;
    }

    public virtual Matrix4 GetModelMatrix()
    {
        return Matrix4.CreateScale(_Scale) *
               Matrix4.CreateFromQuaternion(_Rotation) *
               Matrix4.CreateTranslation(_Position);
    }

    public static Matrix4 ModelMatrixIdentity()
    {
        return Matrix4.CreateScale(Vector3.One) *
              Matrix4.CreateFromQuaternion(Quaternion.Identity) *
              Matrix4.CreateTranslation(Vector3.Zero);
    }


    internal virtual void Internal_InitializeMesh()
    {
        InitializeMesh();
    }
    internal virtual void Internal_Render()
    {
        if (!_isInitializedMesh)
        {
            Internal_InitializeMesh();
            _isInitializedMesh = true;
        }

        var _shaderProgram = Rend3D._program;
        GL.UseProgram(_shaderProgram);
       
        GL.BindVertexArray(_vao);

        //TRANSFORM
        Matrix4 modelMatrix = GetModelMatrix();
        int modelLoc = GL.GetUniformLocation(_shaderProgram, "model");
        GL.UniformMatrix4(modelLoc, false, ref modelMatrix);


        Render();

        GL.BindVertexArray(0);
        //GL.UseProgram(0);
    }
    internal virtual void Internal_DisposeMesh()
    {
        if (_isInitializedMesh)
        {
            _isInitializedMesh = false;

            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }

        DisposeMesh();

    }


    protected virtual void InitializeMesh()
    {

    }
    protected virtual void Render()
    {

    }
    protected virtual void DisposeMesh()
    {

    }
}


public class Square3D : CanvasObject3D
{
    protected override void InitializeMesh()
    {
        float[] vertices = {
            -0.5f, -0.5f, 0.0f,  // bottom left
             0.5f, -0.5f, 0.0f,  // bottom right
             0.5f,  0.5f, 0.0f,  // top right
            -0.5f,  0.5f, 0.0f   // top left
        };

        uint[] indices = {
            0, 1, 2,  // first triangle
            2, 3, 0   // second triangle
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);


    }
    protected override void Render()
    {
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
    }

    protected override void DisposeMesh()
    {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
    }
}

public class Camera3D : CanvasObject3D
{
    [JsonIgnore] public float Sensitivity { get; set; } = 0.1f;

    [JsonIgnore] public Vector3 Target { get; set; } = Vector3.Zero;
    [JsonIgnore] public float Distance { get; set; } = 10.0f;


    [JsonIgnore] public Vector3 Front { get; set; }
    [JsonIgnore] public Vector3 Up { get; set; }
    [JsonIgnore] public float Pitch { get; set; } = 0.0f;
    [JsonIgnore] public float Yaw { get; set; } = -90.0f;
    [JsonIgnore] public float Fov { get; set; } = 45.0f;

    public Camera3D()
    {
        _Position = new Vector3(0.0f, 0.0f, 3.0f);
        Front = new Vector3(0.0f, 0.0f, -1.0f);
        Up = Vector3.UnitY;
        Pitch = 0.0f;
        Yaw = -90.0f;
        Fov = 45.0f;
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(_Position, _Position + Front, Up);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), aspectRatio, 0.1f, 100.0f);
    }



    public void DoPan(float xOffset, float yOffset)
    {
        xOffset *= Sensitivity * 0.1f;
        yOffset *= Sensitivity * 0.1f;

        Vector3 right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, Front));

        Target += right * xOffset + up * yOffset;
        UpdateCameraVectors();
    }
    public void DoOrbit(float xOffset, float yOffset)
    {
        xOffset *= Sensitivity * 2.0f;
        yOffset *= Sensitivity * 2.0f;

        Yaw += xOffset;
        Pitch -= yOffset;

        if (Pitch > 89.0f) Pitch = 89.0f;
        if (Pitch < -89.0f) Pitch = -89.0f;

        UpdateCameraVectors();
    }
    public void DoZoom(float yOffset)
    {
        Distance -= yOffset * 0.1f;
        if (Distance < 1.0f) Distance = 1.0f;  // Limitar la distancia mínima
        if (Distance > 100.0f) Distance = 100.0f;  // Limitar la distancia máxima
        UpdateCameraVectors();
    }


    private void UpdateCameraVectors()
    {
        // Calcular la nueva posición de la cámara usando coordenadas esféricas
        float radius = Distance;
        float pitchRadians = MathHelper.DegreesToRadians(Pitch);
        float yawRadians = MathHelper.DegreesToRadians(Yaw);

        Vector3 position;
        position.X = Target.X + radius * MathF.Cos(pitchRadians) * MathF.Cos(yawRadians);
        position.Y = Target.Y + radius * MathF.Sin(pitchRadians);
        position.Z = Target.Z + radius * MathF.Cos(pitchRadians) * MathF.Sin(yawRadians);

        _Position = position;

        Front = Vector3.Normalize(Target - _Position);
    }

    public void MovementFirstPerson(float xOffset, float yOffset)
    {
        const float sensitivity = 0.1f;
        xOffset *= sensitivity;
        yOffset *= sensitivity;

        Yaw += xOffset;
        Pitch -= yOffset;

        if (Pitch > 89.0f) Pitch = 89.0f;
        if (Pitch < -89.0f) Pitch = -89.0f;

        Vector3 front;
        front.X = (float)Math.Cos(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch));
        front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(Pitch));
        front.Z = (float)Math.Sin(MathHelper.DegreesToRadians(Yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(Pitch));
        Front = Vector3.Normalize(front);
    }


    public void FocusOnObject3D(CanvasObject3D obj)
    {
        Target = obj._Position;
        // Opcional: Ajusta la distancia de la cámara para que el objeto esté completamente visible
        Distance = 2.0f; // Puedes ajustar esto según sea necesario
       // Pitch = 0.0f;GL.Enable(EnableCap.Multisample)
        //Yaw = -90.0f;
        UpdateCameraVectors();
    }
    public void FocusOnObject(CanvasObject obj)
    {
        Target = new Vector3(obj.PositionX, obj.PositionY, obj.Index) / 512;
        Distance = 2.0f;

        UpdateCameraVectors();
    }

}
