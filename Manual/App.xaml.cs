using FFMediaToolkit;
using Manual.Core;
using Newtonsoft.Json;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using ManualToolkit.Windows;
using ManualToolkit.Generic;
using Microsoft.Win32;
using Manual.API;
using ManualToolkit.Specific;
using CefSharp;
using Manual.Editors.Displays;
using Manual.Editors.Displays.Launcher;
using System.Windows.Controls;
using System.Security.Principal;
using System.Windows.Interop;
using System.Windows.Media;

namespace Manual;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// directory path of the exe
    /// </summary>
    public static readonly string LocalPath = AppDomain.CurrentDomain.BaseDirectory;

 #region .----------------------------------------------------------------------SQUIRREL EVENTS
    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
        _ = WebManager.POST(Constants.WebURL + "/api/notify?about=" + "installation", "post", Constants.AuthToken);

        //  InitializeShell();
     //   MessageBox.Show("Installed");
    //    SetupStartup();
    }
    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
        _ = WebManager.POST(Constants.WebURL + "/api/notify?about=" + "uninstallation","post", Constants.AuthToken);
        MessageBox.Show("Uninstalled Correctly");
    }
    private static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun) // ARGS: --squirrel-firstrun
    {
        tools.SetProcessAppUserModelId();

        if (firstRun)
        {
            IsFirstRun = true;
        }

        // show a welcome message when the app is first installed
     //   if (firstRun) 
     //   {
            //  MessageBox.Show("Thanks for installing my application!");
    //        IPCManager.SendMessageToRunningApp("InstallFinished");
    //    }
    }
    #endregion


    public static LogoSplashScreen splashScreen;

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    protected override void OnStartup(StartupEventArgs e)
    {
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        //set manual project icon, shot icon, update main exe icon
      //   _utils();



        //first of all
        BugReporter.Initialize();

        SquirrelAwareApp.HandleEvents(
            onInitialInstall: OnAppInstall,
            onAppUninstall: OnAppUninstall,
            onEveryRun: OnAppRun);

        //-----------------------

       // if(e.Args.Length >= 1)
       // File.WriteAllText("./TestArgs.txt", string.Join("\n", e.Args));

        var args = IPCManager.HandleStartupArguments(e);

        base.OnStartup(e);


        if (args != null)
            OnStartWithArgs(e.Args);
        else
            NormalStartup();

        IPCManager.ListenForMessages(ListenMessage);
        IPCManager.Cleanup();


        UpdaterManager.CheckForUpdates();



        ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                typeof(DependencyObject),
                new FrameworkPropertyMetadata(500));

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;



    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        OnManualClosed();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        OnManualClosed();
        base.OnExit(e);
    }
    bool disposedClear = false;

    //------------------------------------------------------------------ ON MANUAL CLOSED
    void OnManualClosed()
    {
        AppModel.UninstantiateThings = true;

        if (disposedClear) return;

        disposedClear = true;

        Settings.instance.AIServer.Close();
        CefWebManager.Shutdown();

        Settings.SaveSettings();
        
    }


    public static void CloseSplashScreen()
    {
        splashScreen?.Close();
        splashScreen = null;
    }




    void ListenMessage(string message) //-- SERVER LISTEN
    {
        if (message.Contains("sesioncode")) // OPENED VIA BROWSER
        {
            // message = full URL "manuallauncher://?sesioncode=123"
            var sesioncode = WebManager.GetURLQuery(message, "sesioncode");

            //NORMAL
            if (!IsFirstRun)
            {
                // Asegura que la ventana principal esté en primer plano y activa
                if (Application.Current.MainWindow == null)
                    Current.MainWindow = new MainWindow();

                var mainWindow = Application.Current.MainWindow;
                mainWindow.WindowState = WindowState.Maximized;
            }

            else //AT FIRST
            {
                if (AppModel.windowAssetStore != null)
                {
                    AppModel.windowAssetStore.Topmost = true;
                    AppModel.windowAssetStore.Activate();
                  //  AppModel.windowAssetStore.WindowState = WindowState.Maximized;
                    AppModel.windowAssetStore.Topmost = false;
                }
                else
                {
                    var mainWindow = Application.Current.MainWindow;
                    mainWindow.WindowState = WindowState.Maximized;
                }

            }
          //    mainWindow.Activate();
         //   mainWindow.Topmost = true;  // Asegura que esté al frente
         //   mainWindow.Topmost = false; // Luego establece Topmost en falso para que otras ventanas puedan superponerse si es necesario

            UserManager.LoadSession(sesioncode);

            if (IsFirstRun)
                ((W_Launcher)(AppModel.windowAssetStore?.CustomContent))?.Setup_ComfyStart();
        }
    }

    public static bool IsFirstRun = false;
    void OnStartWithArgs(string[] args)
    {
        string message = string.Join(Environment.NewLine, args);
        if (args[0] == "--squirrel-firstrun" || args[0] == "--firstrun" || IsFirstRun) // on first app run
        {
            SetupStartup();
        }


        else if (message.Contains("sesioncode")) // OPENED VIA BROWSER
        {
            ListenMessage(message);
        }
        else if (args[0] == "--update") // la ventana no se abrirá y AppModel no se instanciará para ahorrar recursos
        {
            UpdaterManager.isUpdateMode = true;
            UpdaterManager.Initialize();
            UpdaterManager.Update();
        }
        
        else
        {
            // Verifica si parece ser una ruta de archivo
            string firstArg = args[0];
           
            if (File.Exists(firstArg))
            {
                Project._tcsOpen = new TaskCompletionSource<bool>();
                Project.OpenAsync(firstArg);
  

                NormalStartup();
                AppModel.mainW.CloseSplash();
            }
            else
            {
                NormalStartup();
            }
        }

        if(!UpdaterManager.isUpdateMode && Output.DEBUG_BUILD())
           Output.Log($"Manual started with args:\n{string.Join("\n", args)}", "OnStartWithArgs");

    }


    static void StartSplashScreen()
    {

        splashScreen = new LogoSplashScreen();
        splashScreen.ShowInTaskbar = false;

        splashScreen.Topmost = true;
        splashScreen.Show();
        //splashScreen.Topmost = false;
    }



    static void PreloadSettings()
    {
        Settings.LoadSettings();

        var server = Settings.instance.AIServer;
        //open server if it's has been opened in the other section
        if (server.IsRememberServerState && server.IsOpened)
        {
            server.IsOpened = false;
            server.Run();
        }
    }




    public static void SetupStartup()
    {
        IsFirstRun = true;

        NormalStartup(false); //preload settings, appmodel, updatemanager
        Settings.instance.FirstOpen = true;

        CloseSplashScreen();

        //start setup
        M_Window.NewShow(new W_Launcher(true), "", M_Window.TabButtonsType._X);
    }


    //------------------------------------------------------------------------------------------------- NORMAL STARTUP
    /// <summary>
    /// load settings, appmodel, and postnormal
    /// </summary>
    public static void NormalStartup(bool openWindow = true)
    {
        StartSplashScreen();
        PreloadSettings();


        AppModel.Initialize();
        UpdaterManager.Initialize();

        if(openWindow)
            PostNormalStartup();
    }
    /// <summary>
    /// init window
    /// </summary>
    public static void PostNormalStartup()
    {
        Settings.PostInitialize();

        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }



    // ---------------------------------------------------- SHELL ------------------------------------------------------ \\
    public static void InitializeShell()
    {
        if (Output.DEBUG_BUILD()) return;

        try
        {
            string applicationPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string applicationDirectory = Path.GetDirectoryName(applicationPath);

            string IconPath(string iconName) => Path.Combine(applicationDirectory, "Resources", $"{iconName}.ico");

            //PROJECT
            SetIcon(".manual", "ManualFileType", "MANUAL Project File", IconPath("projectIcon"), applicationPath);

            //SHOT
            SetIcon(".shot", "ManualShotFileType", "MANUAL Shot File", IconPath("shotIcon"), applicationPath);

            //url for opening login
            RegisterUrlScheme_ForLogin(applicationPath);

            // Notifica a Windows del cambio
            RefreshIconsWindow();

            //Output.Log("Setup successful");
        }

        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n\nYou need to open the application as administrator", "Something went wrong");
        }
    }
 
    public static void SetIcon(string extension, string progID, string description, string iconPath, string applicationPath)
    {
        // Asocia la extensión con progID
        using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension))
        {
            key.SetValue("", progID);
        }

        // Define el progID con su descripción y el ícono predeterminado
        using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progID))
        {
            key.SetValue("", description);

            using (RegistryKey defaultIconKey = key.CreateSubKey("DefaultIcon"))
            {
                defaultIconKey.SetValue("", iconPath);
            }

            // Establece el comando para abrir la extensión con tu aplicación
            using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
            {
                commandKey.SetValue("", $"\"{applicationPath}\" \"%1\"");
            }
        }

     
    }

    public static void RefreshIconsWindow()
    {
        SHChangeNotify(0x08000000, 0x0000, (IntPtr)null, (IntPtr)null);
    }

    static void RegisterUrlScheme_ForLogin(string applicationPath)
    {
        string schemeName = "manuallauncher";
      //  string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
      //  string exeFileName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

      //  string applicationPath = Path.Combine(directoryPath, exeFileName);
        UrlSchemeHandler.RegisterUrlScheme(schemeName, applicationPath);
    }


    private static bool IsWindowsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }



    //presiona este botón en caso de que la IA se descontrole
    public static void Botón_De_Autodestrucción()
    {
        OnAppUninstall(null, null);
    }

}

