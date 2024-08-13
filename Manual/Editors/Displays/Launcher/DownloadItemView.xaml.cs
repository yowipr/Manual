using Manual.Core;
using ManualToolkit.Windows;
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

namespace Manual.Editors.Displays.Launcher
{
    /// <summary>
    /// Lógica de interacción para DownloadItemView.xaml
    /// </summary>
    public partial class DownloadItemView : UserControl
    {
        public DownloadItemView()
        {
            InitializeComponent();
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            var op = AnimationLibrary.Opacity(cancelButton, to: 1, duration: 0.3);
            op.Begin();
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            var op = AnimationLibrary.Opacity(cancelButton, to: 0, duration: 0.3);
            op.Begin();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var m = sender as MenuItem;
            string header = (string)m.Header;

            var d = (DownloadItem)DataContext;

            if(header == "Open Folder")
            {
                FileManager.OPENFOLDER(d.FilePath);
            }


        }
    }
}
