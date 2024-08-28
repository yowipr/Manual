using Manual.API;
using Manual.Core;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Manual.API.ManualAPI;

namespace Manual.Editors.Displays;


/// <summary>
/// Lógica de interacción para W_Render.xaml
/// </summary>
public partial class W_Render : W_WindowContent
{
    public W_Render()
    {
        InitializeComponent();
        DataContext = Render_Manager;
        Render_Manager.PreviewFrame = ManualCodec.RenderFrame();

        if (Render_Manager.RenderSettings.Count == 0)
        {
            Render_Manager.NewSettings();
        }

        Loaded += W_Render_Loaded;
     }

    private void W_Render_Loaded(object sender, RoutedEventArgs e)
    {
        Render_Manager.UIWindow = this.window;
        Loaded -= W_Render_Loaded;
    }

    private void TextBox_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
        {
            SelectFolder();
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        Render_Manager.CancelRender();
        AppModel.mainW.IsEnabled = true;
        Render_Manager.RenderAnimation();
        //window.Close();
    }
    void SelectFolder()
    {
        RenderSettings renderSettings = Render_Manager.SelectedRenderSettings;
        string fileType = renderSettings.Format.ToString().ToLower();

        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = $"{fileType} Files|*.{fileType}";
        saveFileDialog.FileName = Render_Manager.OutputName;

        if (saveFileDialog.ShowDialog() == true)
        {
            string filePath = saveFileDialog.FileName;
            Render_Manager.SetPath(filePath);
        }
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SelectFolder();
    }
}







public class SKBitmapUI : SKElement
{
    // Registrar la propiedad de dependencia Source
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(SKBitmap), typeof(SKBitmapUI),
        new PropertyMetadata(null, OnSourceChanged));

    public SKBitmap Source
    {
        get => (SKBitmap)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    // Método de callback cuando la propiedad Source cambia
    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SKBitmapUI)d;
        control.InvalidateVisual(); // Forzar redibujado cuando el bitmap cambia
    }

    // Sobrescribir OnPaintSurface para dibujar el bitmap
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var bitmap = Source;
        if (bitmap != null)
        {
            // Dibujar el bitmap en el canvas
            canvas.DrawBitmap(bitmap, new SKRect(0, 0, e.Info.Width, e.Info.Height));
        }
    }
}