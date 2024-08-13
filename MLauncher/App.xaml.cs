using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using MLauncher.Sections;
using MLauncher.Core;
using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Printing;
using ManualToolkit.Windows;
using ManualToolkit.Generic;
using ManualToolkit.Specific;

namespace MLauncher;

/// <summary>
/// Lógica de interacción para App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// directory path of the exe
    /// </summary>
    public static readonly string LocalPath = AppDomain.CurrentDomain.BaseDirectory;

    protected override void OnStartup(StartupEventArgs e)
    {
        IPCManager.mutexName = "ManualLauncherMutex";
        var args = IPCManager.HandleStartupArguments(e);

        base.OnStartup(e);

        if (args != null)
            OnStartWithArgs(args);

        IPCManager.ListenForMessages(ListenMessage);
        IPCManager.Cleanup();


        UpdaterManager.Initialize();

        UpdaterManager.CheckForUpdates();
    }
    void OnStartWithArgs(string[] args) //the url
    {
        string message = args[0];
        if (message.Contains("sesioncode")) // OPENED VIA BROWSER
        {
            // message = full URL "manuallauncher://?sesioncode=123"
            var sesioncode = WebManager.GetURLQuery(message, "sesioncode");
            
            // Asegura que la ventana principal esté en primer plano y activa
            if(Application.Current.MainWindow == null)
                Current.MainWindow = new MainWindow();

            var mainWindow = Application.Current.MainWindow;
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Activate();
            mainWindow.Topmost = true;  // Asegura que esté al frente
            mainWindow.Topmost = false; // Luego establece Topmost en falso para que otras ventanas puedan superponerse si es necesario


            Launch.LoadSession(sesioncode);
        }
      
    }

    void ListenMessage(string message) //------- SERVER LISTEN
    {
        if (message.Contains("sesioncode"))
        {
            OnStartWithArgs(new string[] { message });
        }

        else if (message.Contains("Updater:"))
        {
            // Extrae el número de progreso del mensaje
            string progressStr = message.Substring(message.IndexOf(':') + 1);
            if (int.TryParse(progressStr, out int progress))
            {
                if (progress == 100)
                {
                    Launch.instance.FinishedUpdating();
                    return;
                }

                // Llama a TaskBar.Set100 con el progreso obtenido
                Output.Log( progress.ToString() );
                TaskBar.Set100(progress);
            }
            else if (progressStr == "error")
            {
                Launch.instance.FinishedUpdating();
                return;
            }
        }
        else if (message.Contains("InstallFinished")) //when manual finished installing
        {
            Launch.instance.FinishedInstalling();
        }
      

    }


}




public static class AppModel
{
    public static MainWindow mainW;

    public static Login login = new();
    public static Launch launch = new();
    public static Loading loading = new();


    public static void ChangeSection(object section)
    {
        if(mainW.contentWindow.Content != section)
          mainW.contentWindow.Content = section;
    }
    public static void GoToLogin()
    {
        ChangeSection(login);
    }
    public static void GoToLaunch()
    {
        ChangeSection(launch);
    }
    public static void GoToLoading()
    {
        ChangeSection(loading);
    }

}
