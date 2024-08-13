using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Core;
using Manual.Core.Graphics;
using Manual.Editors;
using Manual.Editors.Displays;
using Manual.Objects;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Resources;

using System.Windows.Threading;
using System.Windows.Xps;
using System.Xml.Serialization;

namespace Manual.Editors
{
    /// <summary>
    /// Lógica de interacción para EditorControl.xaml
    /// </summary>
    public partial class OutputView : UserControl
    {

        public OutputView()
        {
            InitializeComponent();
            gridContext.DataContext = AppModel.output;

        }

        private void Button_Click_Clear(object sender, RoutedEventArgs e)
        {
            if (DataContext is ED_Output d)
            {
                switch (d.SectionIndex)
                {
                    case 0:
                        Output.ClearLog();
                        break;
                    case 1:
                        Output.ClearConsoleLog();
                        break;
                    case 2:
                        Output.ClearWebSocketLog();
                        break;
                    default:
                        break;
                }

            }
        }

        private void scroll_ScrollChanged(object sender, ScrollChangedEventArgs e) // SCROLL ON BOTTOM
        {
            var scroll = sender as ScrollViewer;
            if (scroll.VerticalOffset + 5 >= scroll.ExtentHeight - scroll.ViewportHeight)  //When scroll end
                scroll.ScrollToEnd();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            var header = (string)mi.Header;

            var server = Settings.instance.AIServer;
            if (header == "Run")
            {
                server.Run();
            }
            else if (header == "Close")
            {
                server.Close();
            }
            else if (header == "Clear Console")
            {
                Button_Click_Clear(this, null);
            }
            else if (header == "Clear All Consoles")
            {
                Button_Click_Clear_All(this, null);
            }
        }


        private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                Output.InterpretUserMessage();
            
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            var o = Output.instance;

            // Comprueba si el evento de arrastre contiene datos de archivo
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Extrae los archivos como un array de strings
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Añade cada ruta de archivo a la colección UploadedFiles
                foreach (var file in files)
                {
                    o.UploadedFiles.Add(file);
                }
            }
        }

        private void Button_Click_Clear_All(object sender, RoutedEventArgs e)
        {
            Output.ClearAll();
        }
    }


    public partial class ED_Output : Editor
    {
        [ObservableProperty] int sectionIndex = 0;
        public ED_Output()
        {

        }


    }
}





namespace Manual.Core
{

    public partial class Output : ObservableObject
    {
      


        public enum AlertType
        {
            None,
            Information,
            Warning,
            Error,

        }
        public static void Show(object message)
        {
            if (message != null)
                M_MessageBox.Show(message.ToString());
            else
                M_MessageBox.Show("null");
        }
        public static void Show(string message)
        {
            M_MessageBox.Show(message);
        }
        public static void Show(string message, string title)
        {
            M_MessageBox.Show(message, title);
        }
        public static void Show(string message, string title, AlertType alert)
        {
            M_MessageBox.Show(message, title);
        }
        public static void Show(object message, object title)
        {
            Show(message.ToString() ?? "null", title.ToString() ?? "null");
        }

        public static void Notification()
        {
            TaskBar.Notification();
            DoneSound();
        }


        public static void DoneSound()
        {
            PlaySound("/Assets/done.wav");
        }

        public static void PlaySound(string pathResource)
        {
            AppModel.Invoke(() =>
            {
                Uri uri = new Uri($"pack://application:,,,{pathResource}");
                StreamResourceInfo streamInfo = Application.GetResourceStream(uri);

                if (streamInfo != null)
                {
                    SoundPlayer doneSound = new SoundPlayer(streamInfo.Stream);
                    doneSound.Play();
                }
            });
        }


        public static void ClearLog()
        {
            instance.LogEntries.Clear();
            ChangeLogFooter("");
        }

     

        public static Output instance => AppModel.output;
        public ObservableCollection<LogMessage> LogEntries { get; set; } = new();

        private LogMessage lastLog;
        public LogMessage LastLog
        {
            get => lastLog;
            private set => SetProperty(ref lastLog, value);
        }

        public static void Log(object message)
        {
            Log(message?.ToString() ?? "null", "");
        }
        public static void Log(string message)
        {
            Log(message, "");
        }
        public static void Log(object message, object title)
        {
            if (message is SKBitmap sk)
                Log(sk, title?.ToString());
            else if (message is byte[] byt)
                Log(byt, title?.ToString());
            else
                Log(message?.ToString() ?? "null", title.ToString() ?? "null");
        }

        public static void Log(string message, string title) // LOG
        {
            AppModel.Invoke(() =>
            {
                if (instance == null)
                {
                    Debug.WriteLine(message, title);
                    return;
                }

                var entry = new LogMessage(message, title);
                instance.LogEntries.Add(entry);
                instance.LastLog = entry;

                ChangeLogFooter($"🛈 {title}; {message.ToSingleLine()}");
            });
        }
        public static void Log(string message, string title, Color color)
        {
            var log = new LogMessage(message, title) { MessageColor = color};
            Log(log);
        }

        public static void LogError(string message, string title = "")
        {
            var log = new LogMessage(message, title) { MessageColor = Colors.IndianRed };
            Log(log);
        }
        public static void LogWarning(string message, string title)
        {
            var log = new LogMessage(message, title) { MessageColor = Colors.Orange };
            Log(log);
        }
        public static void Log(LogMessage log)
        {
            AppModel.Invoke(() =>
            {
                if (instance == null)
                {
                    Debug.WriteLine(log.Message, log.Title);
                    return;
                }

                instance.LogEntries.Add(log);
                instance.LastLog = log;

                ChangeLogFooter($"🛈 {log.Title}; {log.Message.ToSingleLine()}");
            });
        }
        public static void Log(SKBitmap image, string title = "image")
        {
            bool enabled = true;
            if (!enabled) return;

            if (image != null)
            {
                var log = new LogMessage(image) { Title = $"{title}", Message = $"{image.Width}x{image.Height}" };
                Log(log);
            }
            else
                Log("null", title);
        }

        public static void Log(byte[] image, string title = "image")
        {
            Log(image.ToSKBitmap(), title);
        }

        public static void LogCascade(string title, params string[] messages)
        {
            string messageCascade = string.Join(Environment.NewLine, messages);
            Log(messageCascade, title);
        }
        public static void LogCascade(object title, params object[] messages)
        {
            var messageStrings = messages.Select(msg =>
            {
                if (msg == null) return string.Empty;
                if (msg.GetType().IsArray)
                {
                    // Si el objeto es un array, convertir cada elemento del array a string y unirlos
                    var array = msg as Array;
                    return string.Join(Environment.NewLine, array.Cast<object>().Select(element => element?.ToString() ?? string.Empty));
                }
                else
                {
                    // Para objetos no array, simplemente convertir a string
                    return msg.ToString();
                }
            }).ToArray();

            string messageCascade = string.Join(Environment.NewLine + Environment.NewLine, messageStrings); // Añade una línea adicional entre mensajes para mejor claridad
            Log(messageCascade, title?.ToString() ?? "null");
        }


        public static void LogCascadeNamed(string title, params string[] nameValuePairs)
        {
            if (nameValuePairs.Length % 2 != 0)
            {
                throw new ArgumentException("El número de elementos en nameValuePairs debe ser par.");
            }

            List<string> messages = new List<string>();
            for (int i = 0; i < nameValuePairs.Length; i += 2)
            {
                string name = nameValuePairs[i];
                string value = nameValuePairs[i + 1];
                messages.Add($"{name}: {value}");
            }

            string messageCascade = string.Join(Environment.NewLine, messages);
            Log(messageCascade, title);
        }
        public static void LogCascadeNamed(object title, params object[] nameValuePairs)
        {
            if (nameValuePairs.Length % 2 != 0)
            {
                throw new ArgumentException("El número de elementos en nameValuePairs debe ser par.");
            }

            List<string> messages = new List<string>();
            for (int i = 0; i < nameValuePairs.Length; i += 2)
            {
                object name = nameValuePairs[i];
                object value = nameValuePairs[i + 1];
                string nameString = name?.ToString() ?? "null";
                string valueString = value?.ToString() ?? "null";
                messages.Add($"{nameString}: {valueString}");
            }

            string messageCascade = string.Join(Environment.NewLine, messages);
            Log(messageCascade, title?.ToString() ?? "null");
        }

        public static void ChangeLogFooter(string message)
        {
            try
            {
                if (AppModel.mainW != null)
                    //AppModel.mainW.logFooter.Text = message;
                    AppModel.mainW.SetAlert(message);
            }
            catch (Exception ex)
            {
                Output.Log(ex, message);
            }
        }

        public static void LogEx(Exception ex)
        {
           Show(ex.ToString(), ex.Source ?? "exception not controled");
        }

        public static void Error(string message, string origin)
        {
            Log(message, origin);
        }
        internal static void Warning(string message, string title)
        {
            Log(message, title);
        }


        //---------------------------------------------------------------- DEBUG--------------------------------------
        public static bool DEBUGGING_MODE => Debugger.IsAttached;
        public static bool DEBUG_BUILD()
        {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
        public static T SET_VALUE_DEBUG<T>(T releaseValue, T debugValue)
        {
            #if DEBUG
            return debugValue;
            #else
            return releaseValue;
            #endif
        }




        /// <summary>
        /// open window in a separate thread
        /// </summary>
        /// <param name="window"></param>
        public static void ShowAsync(Window window)
        {
            // Método para mostrar la ventana en un hilo secundario
            void ShowWindow()
            {
                window.Show();
                Dispatcher.Run();
            }

            // Crear y ejecutar el hilo secundario
            Thread newWindowThread = new Thread(ShowWindow);
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
        }






        //------------------------------------------------------------------------------------------------------------------------------------ CONSOLE
        [ObservableProperty] string consoleLog = "";
        [ObservableProperty] string webSocketLog = "";
        public static void WriteLine(string message)
        {
            AppModel.Invoke(() =>
            {
                instance.ConsoleLog += $"{message}\n";
            });
        }

        internal static void ClearConsoleLog()
        {
            AppModel.Invoke(() =>
            {
                instance.ConsoleLog = "";
            });
        }
        public static void WriteLineWebSocket(string message)
        {
            AppModel.Invoke(() =>
            {
                instance.WebSocketLog += $"{message}\n";
            });
        }

        internal static void ClearWebSocketLog()
        {
            AppModel.Invoke(() =>
            {
                instance.WebSocketLog = "";
            });
        }

        internal static void ClearAll()
        {
            Output.ClearLog();
            Output.ClearConsoleLog();
            Output.ClearWebSocketLog();
        }






        //----------------------------------------------   USER MESSAGES

        [ObservableProperty] string userMessage = "";

        public MvvmHelpers.ObservableRangeCollection<string> UploadedFiles { get; set; } = new();

        //enter send view ui
        public static void InterpretUserMessage()
        {
            var message = instance.UserMessage;

            LogMessage log = new(message, "You");
            log.Files.AddRange(instance.UploadedFiles);
            if (User.Current != null)
            {
                log.Title = User.Current.Name;

                if (User.Current.ImageSource != null)
                    log.Icon = User.Current.ImageSource;

            }
            else
                log.Title = "You";


            Output.Log(log);

            InterpretMessage(message);
            instance.UserMessage = "";
        }


        public static void InterpretMessage(string message)
        {
            if (message.StartsWith("compile "))
            {
                if (User.isAdmin)
                {  // Elimina el comando "compile " y las comillas extras del inicio y el final
                    string codeToCompile = message.Substring(8);
                    CSharpScript.RunCode(codeToCompile);

                }
                else
                {
                    Output.Log("this command is only for admins", "Manual Interpreter");
                }


            }
            else if (message == "clear")
            {
                Output.ClearLog();
            }
            else if (message == "hola")
            {
                LogMessage log = new("hola :3", "uwu") { MessageColor = Colors.MediumPurple };
                Output.Log(log);
            }
            else if (message == "rendgl test")
            {
                RendGL.Test();
            }


            //FILES
            else if (message == "metadata")
            {
                if (instance.UploadedFiles.Any())
                {
                    foreach (var file in instance.UploadedFiles)
                    {

                        Log($"----------{System.IO.Path.GetFileNameWithoutExtension(file)}----------");
                        var d = FileManager.ExtractMetadata(file);
                        foreach (var item in d)
                            Log($"{item.DirectoryName} - {item.TagName}", item.Description);

                    }
                }
            }

            else if (message == "png info")
            {
                if (instance.UploadedFiles.Any())
                {
                    var name = "PNG-tEXt";
                    foreach (var file in instance.UploadedFiles)
                    {
                        var d = FileManager.ReadCustomMetadata(file, name);
                        if (d != null)
                            Log(d, System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                }
            }

            else if (message == "bug report")
            {
                throw new Exception("Report a Bug");
            }

            else if (message == "refresh icon")
            {
                App.RefreshIconsWindow();
            }

            else if (message == "stopwatch play")
            {
                var finalFrame = ManualAPI.Animation.FrameEnd;
                var currentFrame = ManualAPI.Animation.CurrentFrame;

                Stopwatch sw = new();
                sw.Start();
                ManualAPI.Animation.Play();

                AnimationManager.OnFrameChanged += AnimationManager_OnFrameChanged;

                void AnimationManager_OnFrameChanged(int currentFrame)
                {
                    if (currentFrame == finalFrame)
                    {
                        sw.Stop();
                        TimeSpan final = sw.Elapsed;
                        Log($"animation real duration: {final.ToReadableTimeExact()}");

                        AnimationManager.OnFrameChanged -= AnimationManager_OnFrameChanged;

                    }
                }

            }
            else if (message == "opengl")
            {
                RendGL.StartLoad();
            }
            else if (message.StartsWith("cam3d")) //cam3d;  0,0,3  ;  0,0,-1  ;  0,-90,45
            {
                var camera = ((ED_CanvasView)Shortcuts.canvasView?.DataContext).ViewportCamera;//AppModel.project.SelectedShot.MainCamera;
                (var pos, var rot, var scal) = ParseXYZ(message);
                camera._Position = pos;
                camera.Front = rot;

                camera.Pitch = scal.X;
                camera.Yaw = scal.Y;
                camera.Fov = scal.Z;

                camera.Distance = 10;


                Shot.UpdateCurrentRender();
            }
            else if (message.StartsWith("layer3d")) //layer3d;  0,0,0  ;  0,0,0  ;  1,1,1
            {
                var layer = ManualAPI.SelectedLayer;
                (var pos, var rot, var scal) = ParseXYZ(message);
                layer._Position = pos;
                layer._Rotation = new OpenTK.Mathematics.Quaternion(rot, 1.0f);
                layer._Scale = scal;

                Shot.UpdateCurrentRender();
            }



            instance.UploadedFiles.Clear();
        }



        static (OpenTK.Mathematics.Vector3 pos, OpenTK.Mathematics.Vector3 rot, OpenTK.Mathematics.Vector3 scale) ParseXYZ(string message)
        {
            OpenTK.Mathematics.Vector3 pos = OpenTK.Mathematics.Vector3.Zero;
            OpenTK.Mathematics.Vector3 rot = OpenTK.Mathematics.Vector3.Zero;
            OpenTK.Mathematics.Vector3 scale = OpenTK.Mathematics.Vector3.One;

            // Separar el mensaje en partes
            var parts = message.Split(';');

            if (parts.Length >= 3)
            {
                // Procesar posición
                var positionValues = parts[1].Trim().Split(',');
                if (positionValues.Length == 3)
                {
                    float.TryParse(positionValues[0], out float posX);
                    float.TryParse(positionValues[1], out float posY);
                    float.TryParse(positionValues[2], out float posZ);
                    pos = new OpenTK.Mathematics.Vector3(posX, posY, posZ);
                }

                // Procesar rotación
                var rotationValues = parts[2].Trim().Split(',');
                if (rotationValues.Length == 3)
                {
                    float.TryParse(rotationValues[0], out float rotX);
                    float.TryParse(rotationValues[1], out float rotY);
                    float.TryParse(rotationValues[2], out float rotZ);
                    rot = new OpenTK.Mathematics.Vector3(rotX, rotY, rotZ);
                }

                // Procesar escala
                var scaleValues = parts[3].Trim().Split(',');
                if (scaleValues.Length == 3)
                {
                    float.TryParse(scaleValues[0], out float scaleX);
                    float.TryParse(scaleValues[1], out float scaleY);
                    float.TryParse(scaleValues[2], out float scaleZ);
                    scale = new OpenTK.Mathematics.Vector3(scaleX, scaleY, scaleZ);
                }
            }

            return (pos, rot, scale);
        }







    }








}
