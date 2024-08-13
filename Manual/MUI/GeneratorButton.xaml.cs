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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Manual.MUI
{
    /// <summary>
    /// Lógica de interacción para GeneratorButton.xaml
    /// </summary>
    public partial class GeneratorButton : UserControl
    {

        //AnimateUI anim;
        //AnimateUI anim2;
        public GeneratorButton()
        {
            InitializeComponent();

            DataContext = GenerationManager.Instance;

            //anim = new AnimateUI(borderOver, focusValue: 0.3, unFocusValue: 0, subscribeTo: this);
            //anim2 = new AnimateUI(borderOver2, focusValue: 0.3, unFocusValue: 0, subscribeTo: this);


            //generateButton.RenderTransform = new ScaleTransform(1, 1);

            //// Manejar eventos
            //generateButton.MouseLeftButtonDown += (s, e) => PressDown();
            //generateButton.MouseLeftButtonUp += (s, e) => PressUp();
        }

        //private void PressDown()
        //{
        //  //  this.CaptureMouse();
        //    var storyboard = AnimationLibrary.PressDown(this, 1, 0.96);
        //    Storyboard.SetTarget(storyboard, this);
        //    storyboard.Begin();

        //}

        //private void PressUp()
        //{
        //  //  this.ReleaseMouseCapture();
        //    var storyboard = AnimationLibrary.PressDown(this, 0.96, 1);

        //    Storyboard.SetTarget(storyboard, this);
        //    storyboard.Begin();
            

        //}




        private void Generate(object sender, RoutedEventArgs e)
        {
            GenerationManager.Generate();
        }

        private void Interrupt(object sender, RoutedEventArgs e)
        {
            GenerationManager.Interrupt();
        }
    }
}
