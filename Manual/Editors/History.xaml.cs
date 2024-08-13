using Manual.Core;
using System;
using System.Collections.Generic;
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

namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para History.xaml
/// </summary>
public partial class History : UserControl
{
    public History()
    {
        InitializeComponent();

        //   _model = AppModel.project.SelectedShot;
        //    DataContext = AppModel.project;

        DataContext = AppModel.project.generationManager;
    }



   // private void ChangeToGeneratedImage(object sender, MouseButtonEventArgs e)
   // {
   ////     MessageBox.Show(DataContext.ToString());
   //     return;

   //     _model = AppModel.project.SelectedShot;
   //     Border s = sender as Border;
   //     ImageBrush brush = s.Background as ImageBrush;
       
   //     _model.SelectedLayer.Image = ImageSourceToByteArray(brush.ImageSource).ToWriteableBitmap();
   // }

    private byte[] ImageSourceToByteArray(ImageSource imageSource)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // Encodificar la imagen a formato PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));
            encoder.Save(stream);

            // Devolver los bytes de la imagen codificada
            return stream.ToArray();
        }
    }
}

public partial class ED_History : Editor
{
    public ED_History()
    {
        
    }
}
