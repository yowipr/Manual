using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Manual.API;
using Manual.Core;
using Manual.Core.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.Objects.UI;

/// <summary>
/// Lógica de interacción para Camera2DView.xaml
/// </summary>
public partial class Camera2DView : UserControl
{
    public OpenGLHost openglHost;
    public Camera2DView()
    {
        InitializeComponent();

   
        Loaded += Camera2DView_Loaded;
        Unloaded += Camera2DView_Unloaded;



    }

    private void Camera2DView_Unloaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void Camera2DView_Loaded(object sender, RoutedEventArgs e)
    {
        var cam = (Camera2D)DataContext;
        cam.ui_camera = this;
    }




    //  SK RENDER
    private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface;
        var canvas = surface.Canvas;
        var currentFrame = ManualAPI.Animation.CurrentFrame;
        var bitmap = ManualAPI.Animation.GetBufferFrame(currentFrame);

        if (bitmap != null)
        {
            canvas.Clear();
            canvas.DrawBitmap(bitmap, new SKRect(0, 0, e.Info.Width, e.Info.Height));
        }
        else
        {
            var shot = ManualAPI.SelectedShot;
            var options = new RenderAreaOptions()
            {
                Surface = surface,
                Canvas = canvas,
                EnableEffects = shot.EnableEffects,
            };
            RendGPU.RenderFrame(shot, options); //uses this surface
        }
       
    }



}







