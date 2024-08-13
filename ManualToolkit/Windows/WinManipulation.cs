using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using ManualToolkit.Generic;
using Microsoft.Win32;
using ManualToolkit.Specific;
using Newtonsoft.Json;

namespace ManualToolkit.Windows;


//------------------------------------------------------------------------------------ TASKBAR ----------------------------------------------------------------\\
public static class TaskBar
{
    static Window MainWindow;
    static TaskbarItemInfo taskBar;
    public static event EventProgress? OnProgressChanged;

    /// <summary>
    /// usually call this method at the startup of your application to know the window
    /// </summary>
    /// <param name="mainWindow"></param>
    public static void SetWindow(Window mainWindow)
    {

        MainWindow = mainWindow;
        taskBar = mainWindow.TaskbarItemInfo;
    }

    public static Window GetWindow() => MainWindow;

    public static void Indeterminate()
    {
        if (taskBar == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            taskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
        });
    }
    /// <summary>
    /// from 0 to 100
    /// </summary>
    /// <param name="progress"></param>

    public static void Set100(double progress, string message = "") // main progress set
    {
        if (taskBar == null)
            return;

        if (MainWindow == null)
        {
            throw new ArgumentNullException(nameof(MainWindow), "Missing target window, you need to use TaskBar.SetWindow(Window mainWindow)");
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (taskBar.ProgressState != TaskbarItemProgressState.Normal)
                taskBar.ProgressState = TaskbarItemProgressState.Normal;

            taskBar.ProgressValue = progress / 100;
            OnProgressChanged?.Invoke((int)progress, message);
        });
    }


    /// <summary>
    /// from 0 to 1
    /// </summary>
    /// <param name="progress"></param>
    public static void Set(float progress, string message = "")
    {
        Set100((progress * 100).ToInt(), message);
    }
    public static void Stop(string finalMessage = "")
    {
        if (taskBar == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            TaskBar.Set100(0);
            OnProgressChanged?.Invoke(0, finalMessage);
            taskBar.ProgressState = TaskbarItemProgressState.None;
        });
    }

    public static void Notification()
    {
        if (taskBar == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!MainWindow.IsActive)
            {
                Stop();
                WindowInteropHelper wih = new WindowInteropHelper(MainWindow);
                FlashWindow(wih.Handle, true);
            }
            // Output.AlertSound();
        });
    }
    [DllImport("user32")] public static extern int FlashWindow(IntPtr hwnd, bool bInvert);


    /// <summary>
    /// from 0 to finalNumber, progress has to go from 0 to finalNumber
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="finalNumber"></param>
    /// <param name="message"></param>
    public static void SetCustom(float progress, float finalNumber, string message = "")
    {
        if (finalNumber <= 0)
            throw new ArgumentException("finalNumber has to be greater than 0");
        

        // Calcula el valor normalizado en el rango 0-100.
        float normalizedProgress = (progress / finalNumber) * 100;

        // Llama a Set100 con el valor normalizado.
        Set100((int)normalizedProgress, message);
    }




}

//-------------------------------------------------------------------------- URL SCHEME HANDLER ---------------------------------------------------------------\\
public class UrlSchemeHandler
{
    public static void RegisterUrlScheme(string schemeName, string applicationPath)
    {
        // La clave de registro donde se deben realizar los cambios
        string keyName = @"Software\Classes\" + schemeName;

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyName))
        {
            key.SetValue("", "URL: " + schemeName);
            key.SetValue("URL Protocol", "");
        }

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyName + @"\DefaultIcon"))
        {
            key.SetValue("", applicationPath);
        }

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyName + @"\shell\open\command"))
        {
            key.SetValue("", "\"" + applicationPath + "\" \"%1\"");
        }
    }
}

//---------------------------------------------------------------------- INTER-PROCESS COMUNICATION ------------------------------------------------------------\\

public static class IPCManager
{
    static Mutex mutex;
    public static string mutexName = "ManualMutex";
    public static string pipeName = "ManualPipeName";


    // Verifica si la aplicación ya está en ejecución.

    /// <summary>
    /// check if application is already running, you can set shutdownSecondWindow = false if you dont want to close second window app
    /// </summary>
    /// <param name="shuwdownSecondWindow"></param>
    /// <returns></returns>
    public static bool IsAppAlreadyRunning(bool shuwdownSecondWindow = true)
    {
        return IsAppAlreadyRunning(mutexName, shuwdownSecondWindow);
    }
    public static bool IsAppAlreadyRunning(string mutexName, bool shuwdownSecondWindow = true)
    {
        bool firstOpen;
        mutex = new Mutex(true, mutexName, out firstOpen);

        if (!firstOpen && shuwdownSecondWindow)
        {
            // La aplicación ya está en ejecución. 
            ActivateRunningInstance();
            Application.Current.Shutdown(); // Cierra esta instancia.
        }

        return !firstOpen;
    }

    public static void SendMessageToRunningApp(string message)
    {
        using (var client = new NamedPipeClientStream(pipeName))
        {
            client.Connect();
            using (var writer = new StreamWriter(client))
            {
                writer.Write(message);
                writer.Flush();
            }
        }
    }
    public static void SendMessageToRunningApp(object obj)
    {
        string jsonMessage = JsonConvert.SerializeObject(obj);
        SendMessageToRunningApp(jsonMessage);
    }
    /// <summary>
    /// uses a MessageWrapper
    /// </summary>
    /// <param name="msgType"></param>
    /// <param name="obj"></param>
    public static void SendMessageToRunningApp(string msgType, object? obj)
    {
        var wrapper = new MessageWrapper(msgType, User.Current);
        SendMessageToRunningApp(wrapper);
    }


    public static bool isListeningMessages = false;
    public static async Task ListenForMessages(Action<string> onMessageReceived)
    {
        if (isListeningMessages) return;

        isListeningMessages = true;
        while (true)
        {
            using (var server = new NamedPipeServerStream(pipeName))
            {
                await server.WaitForConnectionAsync();
                using (var sr = new StreamReader(server))
                {
                    var message = await sr.ReadLineAsync();
                        onMessageReceived(message);
                }
            }
        }
    }

    // Libera los recursos utilizados por el Mutex.
    public static void Cleanup()
    {
        if (mutex != null)
        {
            lock (mutex)
            {
                mutex.ReleaseMutex();
            }
        }
    }





    //----------------------- MANAGE OPEN TWICE THE APP
    private static void ActivateRunningInstance()
    {
        var currentProcess = Process.GetCurrentProcess();

        // Busca las instancias de tu aplicación que estén en ejecución.
        var instances = Process.GetProcessesByName(currentProcess.ProcessName).Where(p => p.Id != currentProcess.Id);

        foreach (var instance in instances)
        {
            if (instance.MainWindowHandle != IntPtr.Zero)
            {
                // Muestra y activa la ventana de la instancia ya en ejecución.
                ShowWindowAsync(instance.MainWindowHandle, 9); // SW_RESTORE
                SetForegroundWindow(instance.MainWindowHandle);
                break;
            }
        }
    }


    [DllImport("User32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

    [DllImport("User32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);



    // ADVANCED
    /// <summary>
    /// al abrirse por segunda vez, simplemente focuseará la app ya abierta.
    /// si la app se abre con argumentos, llamará a onMessageReceived.
    /// si la app se abre con argumentos y ya estaba la app abierta, manda el mensaje a la app ya abierta.
    /// 
    /// devuelve un array string con los argumentos si existen, si no se abrió con argumentos, devuelve null
    /// </summary>
    /// <param name="e"></param>
    /// <param name="onMessageReceived"></param>
    public static string[]? HandleStartupArguments(StartupEventArgs e)
    {
        bool isAlreadyRunning = IPCManager.IsAppAlreadyRunning();
        string[]? messages = null;
        if (e.Args.Length > 0)
        {
            if (isAlreadyRunning)
            {
                IPCManager.SendMessageToRunningApp(e.Args[0]);
                Environment.Exit(0);
            }
            else
            {
                // onMessageReceived(e.Args[0]);
                messages = e.Args;
            }
        }
        return messages;
    }





    //---- CLOSE ANY WINDOW

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

    public const UInt32 WM_CLOSE = 0x0010;



}

public class MessageWrapper
{
    public string MessageType { get; set; }
    public object? MessageContent { get; set; }

    public MessageWrapper(string messageType, object? messageContent)
    {
        MessageType = messageType;
        MessageContent = messageContent;
    }
}
