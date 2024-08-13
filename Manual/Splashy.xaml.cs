using Manual.Core;
using Manual.Objects;
using ManualToolkit.Themes;
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

namespace Manual
{
    /// <summary>
    /// Lógica de interacción para Splashy.xaml
    /// </summary>
    public partial class Splashy : UserControl
    {
        public Splashy()
        {
            InitializeComponent();
            DataContext = AppModel.settings;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var file = (AssetFile)((FrameworkElement)e.Source).DataContext;
            AppModel.LoadProject(file.Path);

           // AppModel.mainW.EDITORS.Children.Remove(this);
        }

        private void Open(object sender, MouseButtonEventArgs e)
        {
            AppModel.File_Open();
        }

        private void New(object sender, MouseButtonEventArgs e)
        {
            AppModel.V_File_New();
        }

        private void ReleaseNotes(object sender, MouseButtonEventArgs e)
        {
            AppModel.Help_ReleaseNotes();
        }
        private void Docs(object sender, MouseButtonEventArgs e)
        {
            AppModel.Help_Docs();
        }

        private void SplashAuthor_Click(object sender, MouseButtonEventArgs e)
        {
            WebManager.OPEN(Settings.instance.SplashAuthor_Link);
        }
    }
}
