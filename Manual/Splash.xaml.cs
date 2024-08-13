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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.Json;
using Manual.Editors;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Security.Policy;
using System.Net.Http;
using System.Threading;
using System.ComponentModel;
using Manual.Core;

namespace Manual
{
    /// <summary>
    /// Lógica de interacción para Splash.xaml
    /// </summary>
    public partial class Splash : Window
    {
        public Splash()
        {
            InitializeComponent();
           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // this.Topmost = true;

            // Create the animation and start it
            //Storyboard fadeInStoryboard = (Storyboard)FindResource("splashAnimation");
           // fadeInStoryboard.Begin(this);


        }
        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void MyWindow_Deactivated(object sender, EventArgs e)
        {
            AppModel.mainW.Topmost = true;
            Close();           
            AppModel.mainW.Topmost = false;
        }
    }
}
