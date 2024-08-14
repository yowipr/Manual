using Manual.API;
using Manual.Core;
using Manual.Editors.Displays;
using Manual.MUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Lógica de interacción para Properties.xaml
    /// </summary>
    public partial class Preferences : W_WindowContent
    {
        private readonly Settings settings = AppModel.settings;
        public Preferences()
        {
            InitializeComponent();
            DataContext = AppModel.settings;
            Loaded += Preferences_Loaded;
        }

        private void Preferences_Loaded(object sender, RoutedEventArgs e)
        {
            if(UserManager.IsAdmin || Output.DEBUG_BUILD())
              themePanel.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e) // CLOSE
        {
            Settings.SaveSettings();
            PluginsManager.LoadPlugins();
            Close_Click(sender, e);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Settings.SaveSettings();
            PluginsManager.LoadPlugins();
            Close_Click(sender, e);
        }

        public override void Close_Click(object sender, RoutedEventArgs e)
        {
            base.Close_Click(sender, e);
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            M_Section s = sender as M_Section;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string filePath = files[0]; // Tomar el primer archivo arrastrado
                    string extension = System.IO.Path.GetExtension(filePath);

                    // Establecer el valor de CodePath o DllPath en el ViewModel según la extensión del archivo
                    if (s.DataContext is PluginData viewModel)
                    {
                        if (extension.Equals(".cs", StringComparison.InvariantCultureIgnoreCase))
                        {
                            viewModel.CodePath = filePath;
                        }
                        else if (extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                        {
                            viewModel.DllPath = filePath;
                        }
                    }
                }
            }
        }

        private void RemovePlugin(object sender, RoutedEventArgs e)
        {
            Button s = sender as Button;


          //  MessageBox.Show( s.DataContext.ToString() );
            AppModel.settings.Plugins.Remove(s.DataContext as PluginData);
        }






     //   private List<Key> pressedKeys = new List<Key>();

        string shortcut = "";

        private Key key;

        string[] k = new string[4] {"", "", "", "" };

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key;
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                k[0] = "Ctrl";
            }
            else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                k[1] = "Alt";
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                k[2] = "Shift";
            }
            else
            {
                k[3] = key.ToString();
                UpdateShortcut(sender as TextBox);
            }
  
        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                k[0] = "";
            }
            else if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                k[1] = "";
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                k[2] = "";
            }
            else if (e.Key == Key.None)
            {
                k[0] = "";
                UpdateShortcut(sender as TextBox);
            }

        }

        void UpdateShortcut(TextBox tbox)
        {
            tbox.IsReadOnly = false;
            HotKey h = tbox.DataContext as HotKey;
            h.hotKeyString = string.Join("+", k.Where(s => !string.IsNullOrEmpty(s)));
            tbox.Text = h.hotKeyString;

            tbox.IsReadOnly = true;
        }

        private void Button_LocateComfy(object sender, RoutedEventArgs e)
        {
            AppModel.V_LocateComfy();
        }
    }
}
