using CommunityToolkit.Mvvm.ComponentModel;
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

namespace MLauncher.Sections
{
    /// <summary>
    /// Lógica de interacción para SoftwareCard.xaml
    /// </summary>
    public partial class SoftwareCardView : UserControl
    {
        public SoftwareCardView()
        {
            InitializeComponent();
            DataContext = AppModel.launch;
        }
        // ADMIN
        private void Button_Click_Build(object sender, RoutedEventArgs e)
        {
            Launch.Admin_Build();
        }



        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
             if (sender is Button btn && !AppModel.launch.IsInstalled)
                 btn.IsEnabled = false;

            AppModel.launch.Open();
        }


        private void Button_Click_Update(object sender, RoutedEventArgs e)
        {
            Launch.instance.Update();
        }



        //------------------------------ ADMIN
        bool isAdminPanelOpen = false;
        private void Button_Click_AdminOpenPanel(object sender, RoutedEventArgs e)
        {
            if (isAdminPanelOpen) // cerrar admin
            {
                adminPanel.Width = 0;
                adminOpenBtn.Content = "⚙️";
            }
            else // abrir admin
            {
                adminPanel.Width = 240;
                adminOpenBtn.Content = "X";
            }

            isAdminPanelOpen = !isAdminPanelOpen;         
        }

        private void Button_Click_ProjectFolder(object sender, RoutedEventArgs e)
        {
            Launch.Admin_OpenProjectFolder();
        }


        //---------- TESTING
        private void Button_Click_Test(object sender, RoutedEventArgs e)
        {
              Output.Log("not test asigned");
        }

        private void Button_Click_LauncherLoad(object sender, RoutedEventArgs e)
        {
            Launch.Admin_LoadingProject(Launch.LauncherName); //sets instance.AppName automatically
        }

        private void Button_Click_Publish(object sender, RoutedEventArgs e)
        {
            Launch.Admin_PublishOnline();
        }

        private void Button_Click_MoveRelease(object sender, RoutedEventArgs e)
        {
            Launch.Admin_MoveReleaseToWebRepo();
        }
    }

    public partial class SoftwareCard : ObservableObject
    {
        [ObservableProperty] string name = "";
    }
}
