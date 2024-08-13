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
using System.Windows.Shapes;

namespace Manual;

/// <summary>
/// Lógica de interacción para LogoSplashScreen.xaml
/// </summary>
public partial class LogoSplashScreen : Window
{
    public LogoSplashScreen()
    {
        InitializeComponent();

        AdjustSize();

        Loaded += LogoSplashScreen_Loaded;
    }

    private void LogoSplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        Topmost = false;

        //DISABLED: logosplash animation no funca

        //img.Opacity = 0;
        //var op = AnimationLibrary.Opacity(img, 1, 0.3);
        //op.OnFinalized(() => AnimationLibrary.BreatheOpacity(img));
        //op.Begin();
    }

    private void AdjustSize()
    {
        // Obtener la resolución de la pantalla del usuario
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        // Definir las dimensiones del splash screen en función de la resolución de la pantalla
        this.Width = screenWidth * 0.35; // Ajusta este factor según sea necesario
        this.Height = screenHeight * 0.35; // Ajusta este factor según sea necesario

        // O establecer dimensiones específicas si se desea
        // this.Width = screenWidth < 1440 ? 500 : 700;
        // this.Height = screenHeight < 900 ? 300 : 500;

        // Center the splash screen
        this.Left = (screenWidth - this.Width) / 2;
        this.Top = (screenHeight - this.Height) / 2;
    }
}
