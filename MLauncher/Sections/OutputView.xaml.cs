using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Lógica de interacción para OutputView.xaml
    /// </summary>
    public partial class OutputView : UserControl
    {
        public OutputView()
        {
            InitializeComponent();
        }
        private void scroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (scroll.VerticalOffset + 5 >= scroll.ExtentHeight - scroll.ViewportHeight)  //When scroll end
                scroll.ScrollToEnd();
        }
    }

    public partial class Output : ObservableObject
    {
        public static int Count = 0;
        [ObservableProperty] string outputs = "";

        private static readonly Output instance = AppModel.launch.output;
        public static void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                instance.Outputs += $"OUTPUT>> {message}\n";
                Count++;
            });
        }
        public static void Clear()
        {
            instance.Outputs = "";
            Count = 0;
        }

        public static void AlertSound()
        {
            System.Media.SystemSounds.Asterisk.Play();
        }
    }

}
