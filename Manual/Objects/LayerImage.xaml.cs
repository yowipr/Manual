using Manual.API;
using Manual.Core;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

namespace Manual.Objects;

/// <summary>
/// Lógica de interacción para LayerView.xaml
/// </summary>
public partial class LayerImage : UserControl
{
    public LayerImage()
    {
        InitializeComponent();

        //Loaded += LayerImage_Loaded;
    
        //Unloaded += LayerImage_Unloaded;
    }


    //private void LayerImage_Loaded(object sender, RoutedEventArgs e)
    //{
    //    ((LayerBase)DataContext).PropertyChanged += LayerImage_PropertyChanged;
    //}

    //private void LayerImage_Unloaded(object sender, RoutedEventArgs e)
    //{
    //    ((LayerBase)DataContext).PropertyChanged -= LayerImage_PropertyChanged;
    //}


    //private void LayerImage_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{
    //    if(e.PropertyName == "Image")
    //    {
    //        skRender.InvalidateVisual();
    //    }
    //}

    //private void SKElement_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
    //{
    //    var surface = e.Surface;
    //    var canvas = surface.Canvas;

    //    canvas.Clear(SKColors.Transparent);

    //    SKBitmap bitmap = ((LayerBase)DataContext).Image;
    //    if (bitmap != null)
    //    {
    //        canvas.DrawBitmap(bitmap, new SKPoint(0, 0));
    //    }
    //}



    private LayerBase ThisLayer
    {
        get   {  return this.DataContext as LayerBase;  }
    }


    //--------- RIGHT CLICK LAYER ----------\\
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog();
        dialog.Filter = "Imagen PNG (*.png)|*.png";
        if (dialog.ShowDialog() == true)
        {
            ManualCodec.SaveImage(ThisLayer.ImageWr, dialog.FileName);
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var image = ThisLayer.ImageWr;
        Clipboard.SetImage(image);

    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {

       // ManualAPI.SelectedPreset.TargetImage = ThisLayer;
    }


}




public class VisibilityLayerConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length == 2 && values[0] is bool value1 && values[1] is bool value2)
        {
            return value1 && value2;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


