using Manual.API;
using Manual.Core;
using Manual.Core.Nodes;
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

using static Manual.API.ManualAPI;

namespace Manual.MUI
{
    /// <summary>
    /// Lógica de interacción para ImageGen.xaml
    /// </summary>
    public partial class ImageGen : UserControl
    {
        public ImageGen()
        {
            InitializeComponent();
            Loaded += ImageGen_Loaded;
        }

        private void ImageGen_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is GeneratedImage genimg)
            {
                if (!genimg.ui_isLoaded)
                {
                    AnimationLibrary.BounceLoaded(border);
                    genimg.ui_isLoaded = true;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var s = sender as MenuItem;
            string header = s.Header.ToString();

            GeneratedImage generatedImage = (GeneratedImage)this.DataContext;
            if (header == "Apply to Selected Layer")
            {
                SelectedLayer.Image = generatedImage.PreviewImage;
            }
            else if (header == "Save")
            {
                var img = generatedImage.OriginalImage;
                AppModel.SaveImage(img);
            }

            else if (header == "Copy")
            {
                ManualClipboard.Copy(generatedImage.PreviewImage);
            }
            else if (header == "Move as Next")
            {
                GenerationManager.Instance.PutOnNext(generatedImage);
            }

            else if (header == "Cancel All")
            {
                GenerationManager.Instance.CancelAllQueue();
            }
            else if (header == "Cancel")
            {
                GenerationManager.Instance.RemoveFromQueue(generatedImage);
            }

            Shot.UpdateCurrentRender();

        }

        private void ImageGen_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (DataContext is GeneratedImage data)
                {
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
                }
            }
        }




    }
}
