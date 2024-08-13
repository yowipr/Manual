using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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

public partial class AssetFile : ObservableObject
{
    [ObservableProperty] byte[] thumbnail;

    [ObservableProperty] byte[] icon;


    [ObservableProperty] string name;

    [ObservableProperty] string path;


}


/// <summary>
/// Lógica de interacción para AssetFile.xaml
/// </summary>
public partial class AssetFileView : UserControl
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
          nameof(Icon), typeof(BitmapImage), typeof(AssetFileView), new PropertyMetadata(default(BitmapImage)));

    public BitmapImage Icon
    {
        get => (BitmapImage)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty ThumbnailProperty = DependencyProperty.Register(
      nameof(Thumbnail), typeof(ImageSource), typeof(AssetFileView), new PropertyMetadata(default(ImageSource)));

    public ImageSource Thumbnail
    {
        get => (ImageSource)GetValue(ThumbnailProperty);
        set => SetValue(ThumbnailProperty, value);
    }


    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
          nameof(Header), typeof(string), typeof(AssetFileView), new PropertyMetadata(default(string)));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }


    AnimateUI anim;
    public AssetFileView()
    {
        InitializeComponent();

        anim = new AnimateUI(borderOver, focusValue: 0.2, unFocusValue: 0, subscribeTo: this);
    }


    private void AssetFile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Comprueba si es realmente un inicio de arrastre...
        if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1) // Puedes ajustar esta condición según necesites
        {
            var assetFile = sender as AssetFileView;
            if (assetFile != null && assetFile.DataContext != null)
            {
                // Iniciar la operación de drag-and-drop
                DragDrop.DoDragDrop(assetFile, assetFile.DataContext, DragDropEffects.Move);
            }
        }
    }



}




