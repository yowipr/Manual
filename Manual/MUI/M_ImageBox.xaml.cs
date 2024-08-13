using CefSharp.DevTools.CSS;
using Manual.API;
using Manual.Core;
using Manual.Objects;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
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

namespace Manual.MUI;

/// <summary>
/// Lógica de interacción para M_ImageBox.xaml
/// </summary>
public partial class M_ImageBox : UserControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
nameof(Source),
typeof(LayerBase),
typeof(M_ImageBox),
new FrameworkPropertyMetadata(
null,
FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
OnValueChanged,
CoerceValue,
true,
UpdateSourceTrigger.PropertyChanged));

    public LayerBase Source
    {
        get
        {  
            return (LayerBase)GetValue(SourceProperty);
        }
        set 
        {
            //TODO: usar actionhistory en un UIElement puede no ser buena idea a futuro
            ActionHistory.StartAction(this, nameof(Source));

            SetValue(SourceProperty, value);

            ActionHistory.FinalizeAction();

            ImageModeChanged();
        }
    }


    public enum ImageModeType
    { 
        SelectedLayer,
        Layer
    }
    public static readonly DependencyProperty ImageModeProperty = DependencyProperty.Register(
    nameof(ImageMode),
    typeof(ImageModeType),
    typeof(M_ImageBox),
    new FrameworkPropertyMetadata(
    ImageModeType.Layer,
    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
    OnValueChanged,
    CoerceValue,
    true, 
    UpdateSourceTrigger.PropertyChanged ));

    public ImageModeType ImageMode
    {
        get { return (ImageModeType)GetValue(ImageModeProperty); }
        set
        { 
            SetValue(ImageModeProperty, value);

            if (ImageMode == ImageModeType.SelectedLayer)
                isSelectedLayer = true;
            else
                isSelectedLayer = false;

        }
    }



    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Aquí puedes agregar lógica para responder al cambio de valor
      
    }
    public bool isSelectedLayer = false;

    Layer internalImage = null;
    public M_ImageBox()
    {
        InitializeComponent();
    }
    Binding binding;
    public M_ImageBox(string bindingImageData)
    {
        InitializeComponent();

            binding = new Binding(bindingImageData)
             {
                 UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
             };

           SetBinding(M_ImageBox.SourceProperty, binding);

        //  comboBox.SelectionChanged += M_ComboBox_SelectionChanged;


     //   comboBox.ItemsSource = Enum.GetValues(typeof(ImageModeType)).Cast<ImageModeType>().ToList();
    }


    private void Grid_Drop(object sender, DragEventArgs e)
    {
        // Verifica si la información de datos contiene un archivo de imagen
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string imagePath = files[0]; // obtenemos la ruta del primer archivo soltado
                                             // imgView.ImageSource = new BitmapImage(new Uri(imagePath)); // asignamos la ruta a la propiedad Source de la etiqueta Image
                SetImage(imagePath);

            }
        }
        else if (e.Data.GetData(typeof(object)) is LayerBase dropLayer)
        {
            Source = dropLayer;
           // ManualAPI.SelectedPreset.MaskImage = dropLayer;
        }
    }

    void SetImage(string imagePath)
    {
        ImageMode = ImageModeType.Layer;
        //  AppModel.shots.SelectedPreset.TargetImage_As_SelectedLayer = false;
        //  Mask.Visibility = Visibility.Visible;
        // Receiver.Source = new BitmapImage(new Uri(imagePath));
        Layer l = new Layer();
        l.ImageWr = Renderizainador.ImageFromFile(imagePath);//new BitmapImage( new Uri(imagePath) );
        Source = l;

        // contG.Height = double.NaN;
        // contG.Visibility = Visibility.Visible;


        // txt.Visibility = Visibility.Collapsed;
        // rect.Visibility = Visibility.Collapsed;
        closeBtn.Width = 30;
    }
    private void Button_Click(object sender, RoutedEventArgs e) //Delete
    {
        //comboBox.SelectedIndex = 1;
     //   AppModel.shots.SelectedPreset.TargetImage_As_SelectedLayer = false;
      //  Mask.Visibility = Visibility.Collapsed;
      //  AppModel.shots.SelectedPreset.TargetImage = null;
        Source = null;

      //  txt.Visibility = Visibility.Visible;
       // rect.Visibility = Visibility.Visible;
        closeBtn.Width = 0;

    }

    private void contG_MouseEnter(object sender, MouseEventArgs e)
    {
        closeBtn.Visibility = Visibility.Visible;
    }

    private void contG_MouseLeave(object sender, MouseEventArgs e)
    {
        closeBtn.Visibility = Visibility.Collapsed;
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2)
            return;

        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Archivos de imagen|*.png;*.jpg;*.jpeg";
        if (openFileDialog.ShowDialog() == true)
        {
            string imagePath = openFileDialog.FileName;
            SetImage(imagePath);
        }
    }

    public byte[] BitmapImageToByteArray(BitmapImage bitmapImage)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(stream);
            return stream.ToArray();
        }
    }

    void ImageModeChanged()
    {
       

        if (Source == null)
        {
           
            withImage.Visibility = Visibility.Collapsed;
            withImage.Height = 0;

            withoutImage.Visibility = Visibility.Visible;
            withoutImage.Height = double.NaN;
        }
        else //has image
        {
           
            withoutImage.Visibility = Visibility.Collapsed;
            withoutImage.Height = 0;

            withImage.Visibility = Visibility.Visible;
            withImage.Height = double.NaN;
        }
    }

    private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //   if (Source != null)
        //         Output.Log($"{binding.Source} {binding}");

      //  WriteableBitmap target = binding.Path as WriteableBitmap;

        if (ImageMode == ImageModeType.SelectedLayer)
        {

            withoutImage.Visibility = Visibility.Collapsed;
            withoutImage.Height = 0;

            withImage.Visibility = Visibility.Collapsed;
            withImage.Height = 0;

            if (ImageMode == ImageModeType.SelectedLayer)
            {

               // Source = internalImage.Image;
              //  GenerationManager.Instance.SelectedPreset.TargetImage = AppModel.project.SelectedShot.SelectedLayer.ImageData;
             //   GenerationManager.Instance.SelectedPreset.TargetImage_As_SelectedLayer = true;
            }
            else
            {
              //  Source = null;
             //   GenerationManager.Instance.SelectedPreset.TargetImage_As_SelectedLayer = false;
            }

            return;
        }


        ImageModeChanged();
    }

    private void RemoveLayer_Click(object sender, RoutedEventArgs e)
    {
        Source = null;
    }
}
