using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using Manual.Core.Graphics;
using SkiaSharp;
using Newtonsoft.Json;

namespace Manual.Objects;


public partial class LayerBase
{
    [JsonIgnore] public int _texture;

    protected override void InitializeMesh()
    {
        float[] vertices = {
    // Positions          // Texture Coords
    -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,  // bottom left
     0.5f, -0.5f, 0.0f,  1.0f, 1.0f,  // bottom right
     0.5f,  0.5f, 0.0f,  1.0f, 0.0f,  // top right
    -0.5f,  0.5f, 0.0f,  0.0f, 0.0f   // top left
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

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);

        // Cargar la imagen en una textura
        LoadTexture();
    }
  

    private void LoadTexture()
    {
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _texture);

        // Establecer los parámetros de la textura
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        // Cargar la imagen

        var data = Image.Pixels;
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Image.Width, Image.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.5f);

        float maxAniso;
        GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropyExt, out maxAniso);
        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, maxAniso);


        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    //----------------------------------------------------------------------- RENDER LAYER
    protected override void Render()
    {
        GL.Disable(EnableCap.DepthTest);

        //OPACITY
        int opacityLoc = GL.GetUniformLocation(Rend3D._program, "opacity");
        GL.Uniform1(opacityLoc, RealOpacity);

        GL.Enable(EnableCap.Blend);

        MR.SetBlendMode(BlendMode);

        GL.BindTexture(TextureTarget.Texture2D, _texture);
        if (IsOnPreview)
        {
            var data = Image.Pixels;
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Image.Width, Image.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        }
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.Enable(EnableCap.DepthTest);
    }

    public void UpdateTexture()
    {
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        var data = Image.Pixels;
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Image.Width, Image.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }


    protected override void DisposeMesh()
    {
     
    }



    public override Matrix4 GetModelMatrix()
    {
        var newPos = Position.ToVector3(Index) / 400;
        newPos.Y = -newPos.Y;

        return Matrix4.CreateScale(NormalizedScale.ToVector3(_Scale.Z)) *
              Matrix4.CreateFromQuaternion(new Quaternion(_Rotation.X, _Rotation.Y, -RealRotation / 36.0f)) *
              Matrix4.CreateTranslation(newPos);
    }



}
