using Manual.API;
using Manual.Core;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;
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
using System.Windows.Shapes;

namespace Manual.Editors
{
    /// <summary>
    /// Lógica de interacción para RenderView.xaml
    /// </summary>
    public partial class RenderView : Window
    {
        WriteableBitmap rend = null;
        public RenderView()
        {
            InitializeComponent();

            rend = ManualCodec.RenderFrame().ToWriteableBitmap();
            render.Source = rend;

        }
        public RenderView(WriteableBitmap bitmap)
        {
            InitializeComponent();

            rend = bitmap;
            render.Source = bitmap;
        }
        public RenderView(SKBitmap bitmap)
        {
            InitializeComponent();

            var b  = bitmap.ToWriteableBitmap();

            rend = b;
            render.Source = b;
        }

        public RenderView(byte[] bitmap)
        {
            InitializeComponent();

            rend = bitmap.ToWriteableBitmap();
            var b = bitmap;

            render.Source = ByteToImage(b);
        }
        public static ImageSource ByteToImage(byte[] imageData)
        {
            BitmapImage biImg = new BitmapImage();
            MemoryStream ms = new MemoryStream(imageData);
            biImg.BeginInit();
            biImg.StreamSource = ms;
            biImg.EndInit();

            ImageSource imgSrc = biImg as ImageSource;

            return imgSrc;
        }

        //--------- RIGHT CLICK LAYER ----------\\
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
           

            MenuItem menuItem = sender as MenuItem;
            if (menuItem.Header.ToString() == "Save Image As...")
            {
                // Crear un cuadro de diálogo para guardar el archivo
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Archivos PNG (*.png)|*.png";
                saveFileDialog.Title = "Guardar imagen PNG";
                saveFileDialog.ShowDialog();

                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    rend = render.Source as WriteableBitmap;
                    ManualCodec.SaveImage(rend, saveFileDialog.FileName);

                     
                }
            }
            else if (menuItem.Header.ToString() == "Save PromptPreset with Image...")
            {
                // Crear un cuadro de diálogo para guardar el archivo
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Archivos PNG (*.png)|*.png";
                saveFileDialog.Title = "Guardar imagen PNG";
                saveFileDialog.ShowDialog();

                if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    rend = render.Source as WriteableBitmap;
                    ManualCodec.SaveImageWithWorkflow(rend, saveFileDialog.FileName);


                }
            }
            else if (menuItem.Header.ToString() == "Copy Image")
            {  
                  Clipboard.SetImage(rend);          
            }

            else if (menuItem.Header.ToString() == "Asign to Prompt Thumbnail")
            {
                ManualAPI.SelectedPreset.Prompt.SetThumbnail(rend.ToSKBitmap());
            }

        }


    }
}
