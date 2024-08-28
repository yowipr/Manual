using CommunityToolkit.Mvvm.ComponentModel;
using MLauncher.Core;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Squirrel;
using System.Diagnostics;
using System.Net.Http;
using System.Printing;
using System.Threading;
using Microsoft.Win32;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpCompress.Archives;
using ManualToolkit.Windows;
using ManualToolkit.Generic;
using System.Security.AccessControl;
using ManualToolkit.Specific;
using System.Windows.Controls.Primitives;
using System.Diagnostics.Contracts;

namespace MLauncher.Sections;

/// <summary>
/// Lógica de interacción para Launch.xaml
/// </summary>
public partial class LaunchView : UserControl
{
    public LaunchView()
    {
        InitializeComponent();
    }

    private void ClickSignIn(object sender, RoutedEventArgs e)
    {
        AppModel.GoToLogin();
    }

    private void UserZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (userIcon.Resources["UserContextMenu"] is ContextMenu userContextMenu)
        {
            userContextMenu.PlacementTarget = sender as UIElement;
            userContextMenu.IsOpen = true;
        }
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu)
        {
            menu.Placement = PlacementMode.Bottom;
            menu.HorizontalOffset = -menu.ActualWidth + userZone.ActualWidth;
            menu.VerticalOffset = -15;
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Launch.LogOut();
    }
}

public partial class Launch : ObservableObject
{
    public static Launch instance = AppModel.launch;

    public SoftwareCard softwareCard = new();
    public Output output = new();
    public MyPlan myPlan = new();
    public AdminPanel adminPanel = new();

    [ObservableProperty] object currentView;

    [RelayCommand] void SoftwareCardView() => CurrentView = softwareCard;
    [RelayCommand] void OutputView() => CurrentView = output;
    [RelayCommand] void MyPlanView()
    {
        CurrentView = myPlan;
        if (User.Current != null && User.Current?.Products["manual"].Plan != "free")
        {
            var _ = myPlan.FetchSubscriptionData();
            var __ = myPlan.FetchTransactionsData();
        }
    }
    [RelayCommand] void AdminPanelView() => CurrentView = adminPanel;



    public static void ChangeViewTo(object view)
    {
        AppModel.launch.CurrentView = view;
    }


    [ObservableProperty] int progress = 0;
    [ObservableProperty] string messageLog = "";

    //---------------------------------------------------------- PROPS
    [ObservableProperty] private User? user = null;
    [ObservableProperty] private bool isNewUpdate = false;
    [ObservableProperty] private bool isInstalled = false;
    [ObservableProperty] private bool isLatestVersion = true;
    [ObservableProperty] private string newVersion = "";

    [ObservableProperty] private bool isUpdating = false;
    [ObservableProperty] private bool isInstalling = false;

    public string releaseUrl => Updater.releaseURL;
    public string setupName;

    public string directoryApplicationPath;
    public string applicationPath_DEBUG;
    public string applicationPath;


    public Launch()
    {
        CurrentView = softwareCard;
    }


    public void Open()
    {
        if (IsInstalled)
        {
            Process.Start(applicationPath);
        }
        else
        {
            Install();
        }
    }
    /// <summary>
    /// first check if app is installed, when check for updates
    /// </summary>
    public void CheckUpdates()   //------------ ON START
    {
        if (File.Exists(applicationPath)) //MANUAL IS NOT INSTALLED, FIRST INSTALATION
        {
            IsInstalled = true;
            CheckNewVersion();
        }
        else
        {
            IsInstalled = false;
        }
    }

    private async void CheckNewVersion()
    {
        string? latestVersion = await GetLatestRelease(); // example: "1.0.1"
        if (latestVersion == null)
            return;

        bool isNewVersionAvailable = IsNewVersionAvailable(latestVersion); // true = outdated, false = updated

        Output.Log($"IsNewVersionAvailable: {isNewVersionAvailable}");
        Output.Log($"GetLatestVersion: {latestVersion}");

        IsLatestVersion = !isNewVersionAvailable;
        if (isNewVersionAvailable) // new version available
        {
            Output.Log($"There's a new version available: {latestVersion}");
            NewVersion = latestVersion;
        }
        else // current local version is the leatest updated
        {

        }
    }

    //------------------------------ MANIPULATE JSON RELEASE
    public string GetCurrentVersion()
    {
        if (!Directory.Exists(directoryApplicationPath))
        {
            Output.Log($"directory does not exist:{directoryApplicationPath}");
            return "not installed";
        }

        var versionDirectories = Directory.GetDirectories(directoryApplicationPath, "app-*");
        if (versionDirectories.Length > 0)
        {
            // Ordenar las versiones para obtener la más reciente (esto asume que las versiones se ordenan alfabéticamente correctamente)
            Array.Sort(versionDirectories);

            // Tomar la última versión (la más reciente)
            var mostRecentVersionDirectory = versionDirectories[^1]; // ^1 obtiene el último elemento

            // Extraer el número de versión del nombre de la carpeta
            var version = Path.GetFileName(mostRecentVersionDirectory).Replace("app-", "");

            return version;
        }

        return "not installed"; // O devuelve una cadena vacía o cualquier valor predeterminado que desees
    }

    public bool IsNewVersionAvailable(string latestVersion)
    {
        string localVersionFolderPath = Path.Combine(directoryApplicationPath, "app-" + latestVersion);
        return !Directory.Exists(localVersionFolderPath);
    }

    public async Task<string?> GetLatestRelease()
    {
        try
        {
            using var httpClient = new HttpClient();

            string releaseInfoUrl = $"{releaseUrl}/RELEASES";
            string content = await httpClient.GetStringAsync(releaseInfoUrl);

            // Regex para extraer la versión del contenido
            var match = Regex.Match(content, @"-(\d+\.\d+\.\d+)-full\.nupkg");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                Output.Log("Could not parse version from RELEASE file.");
            }
        }
        catch (Exception ex)
        {
            Output.Log("Error getting latest release: " + ex);
        }
        return null;
    }





    //-------------------------------------------------------------------------------------------------------------------- INSTALLING

    private async void Install()
    {
        IsInstalling = true;
        Stopwatch stopwatch = new();
        stopwatch.Start();

        // Descargar el instalador
        string installerDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string installerUrl = $"{Constants.WebURL}/manual/{GetRealName()}Setup.exe";
        bool downloadSuccess = await WebManager.Download(installerUrl, installerDirectory);

        if (!downloadSuccess)
        {
            Output.Log("No se pudo descargar el instalador.");
            FinishedInstalling(false);
            return;
        }

        //------- DOWNLOAD SUCCED
        stopwatch.Stop();
        Output.Log($"Download Succed in {stopwatch.Elapsed.TotalSeconds} seconds!");

        // Abrir el instalador
        try
        {
            TaskBar.Indeterminate();
            Output.Log("instalador descargado correctamente! abriendo setup...");
            Process.Start(installerDirectory + $"{GetRealName()}Setup.exe");
        }
        catch (Exception ex)
        {
            Output.Log("No se pudo abrir el instalador: " + ex.Message);
        }

    }
    public void FinishedInstalling(bool succed = true)
    {
        IsInstalling = false;
        TaskBar.Stop();
        CheckUpdates();

        if (succed)
        {
            string installerDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string installerPath = $"{installerDirectory}{GetRealName()}Setup.exe";
            File.Delete(installerPath);
        }
    }

    public void Update()
    {
        IsUpdating = true;
        ProcessStartInfo mprocess = new(applicationPath);
        mprocess.Arguments = "--update";
        Process.Start(mprocess);
    }
    public void FinishedUpdating(bool succed = true)
    {
        IsUpdating = false;
        TaskBar.Stop();

        if (succed)
            Output.Log("Update completed successfully!");
        else
            Output.Log("Update failed");

        CheckUpdates();
    }

    //----------------------------------------------------------------------------------------------------------------------- ADMIN ONLY
    public static void Admin_LoadingProject()
    {
        Admin_LoadingProject(instance.AppName);
    }
    public static void Admin_LoadingProject(string appName)
    {
       
     //   if (!User.isAdmin)
   //         return;

        //load initials
        instance.AppName = appName;
        string realName = GetRealName();

        instance.setupName = $"{instance.AppName}Setup.exe";
        instance.directoryApplicationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), realName);
        instance.applicationPath = Path.Combine(instance.directoryApplicationPath, $"{realName}.exe");  // Ruta donde se guardará el producto

        instance.applicationPath_DEBUG = $"C:\\Users\\YO\\source\\repos\\{SolutionName}\\{instance.AppName}\\bin\\Debug\\net7.0-windows\\{instance.AppName}.exe";
    

        SolutionPath = Path.Combine(FileManager.USERPROFILE_PATH, "source", "repos", SolutionName);

        //

        instance.BuildNewVersion = Launch.Admin_GetCsprojVariable("Version");


        if (instance.AppName == ManualName)
        {
            instance.SplashAuthor = Launch.Admin_GetProjectSettings("SplashAuthor");
            var p = Launch.Admin_GetProjectSettings("Version").Split('-');
            if (p.Length >= 2)
                instance.Suffix = p[1];
        }

    }


    public const string ManualName = "Manual";
    public const string LauncherName = "MLauncher";

    [ObservableProperty] string appName = "Manual";
    public const string SolutionName = "Manual";
    /// <summary>
    /// C:\Users\YO\source\repos\Manual
    /// </summary>
    public static string SolutionPath; // main directory C:\Users\YO\source\repos\Manual
    /// <summary>
    /// C:\Users\YO\source\repos\Manual\Manual
    /// </summary>
    public static string RepoAppPath() => Path.Combine(SolutionPath, instance.AppName);// manual app C:\Users\YO\source\repos\Manual\Manual
    [ObservableProperty] string buildNewVersion = "not loaded";
    [ObservableProperty] string suffix = "not loaded";
    [ObservableProperty] string splashAuthor = "not loaded";
    [ObservableProperty] bool deletePublishReleaseFolders = true;

    static Stopwatch stopWatch = new();




    //----------------------------------------------------------------------------------------------------------------------------------------- BUILD

    static string GetRealName()
    {
        string realName = "MLauncher";
        if (instance.AppName == "Manual")
            realName = "Manual";

        return realName;
    }

    public static void BuildManual() => Admin_Build(); //you know
    public static void Admin_Build()
    {
        stopWatch.Start();


        if (instance.AppName == ManualName)
        {
            string line = string.IsNullOrEmpty(instance.Suffix) ? "" : "-";
            Admin_SetProjectSettings("Version", $"{instance.BuildNewVersion}{line}{instance.Suffix}");
        }

        string a = GetLocalCsprojPath();
        Output.Log(a);
        //

        if (instance.DeletePublishReleaseFolders)
        {
               FileManager.DeleteFolder(Path.Combine(SolutionPath, "publish"));
               FileManager.DeleteFolder(Path.Combine(SolutionPath, "Releases"));
        }


        Admin_SetCsprojVariable("Version", instance.BuildNewVersion);

        ChangeViewTo(instance.output);
        Output.Log($"--------------------------------Start Building {instance.AppName} {instance.BuildNewVersion}--------------------");

        // FileManager.OpenBat(batFilePath, false, OutputDataReceived, ErrorDataReceived, ExitBat);
        FileManager.CMD(PublishCommand(), SolutionPath, true, OutputDataReceived, ErrorDataReceived, BuildingFinished);

    }
    static string GetLocalCsprojPath()
    {
        var r = Path.Combine(SolutionPath, instance.AppName, $"{instance.AppName}.csproj");
        return r;
    }


    private static List<string> PublishCommand()
    {
        string realName = GetRealName();


        return new List<string>
    {
        "@echo off",
        ":: Variables",
        $"set VERSION={instance.BuildNewVersion}",
        $"set APPNAME={realName}",
        $"dotnet publish \"{GetLocalCsprojPath()}\" -c Release -o \".\\publish\" --self-contained -r win-x64",
        "set SQUIRREL=\"%USERPROFILE%\\.nuget\\packages\\clowd.squirrel\\2.9.42\\tools\\Squirrel.exe\"",
        "color 0A",
        "echo ^|--------------------------------------------------^|",
        "echo ^|                                                  ^|",
        "echo ^|         Compilacion completada exitosamente!     ^|",
        "echo ^|                                                  ^|",
        "echo ^|--------------------------------------------------^|",
        "color"
    };
    }



    private static List<string> PublishCommandSquirrel()
    {
        string realName = GetRealName();
      

        return new List<string>
    {
        "@echo off",
        ":: Variables",
        $"set VERSION={instance.BuildNewVersion}",
        $"set APPNAME={realName}",
        $"dotnet publish \"{GetLocalCsprojPath()}\" -c Release -o \".\\publish\"",
        "set SQUIRREL=\"%USERPROFILE%\\.nuget\\packages\\clowd.squirrel\\2.9.42\\tools\\Squirrel.exe\"",
        "%SQUIRREL% pack --framework net6,vcredist143-x86  --packId \"%APPNAME%\"  --packVersion \"%VERSION%\"  --packAuthors \"Manual Team\"  --packDir \".\\publish\" --allowUnaware",
        "color 0A",
        "echo ^|--------------------------------------------------^|",
        "echo ^|                                                  ^|",
        "echo ^|         Compilacion completada exitosamente!     ^|",
        "echo ^|                                                  ^|",
        "echo ^|--------------------------------------------------^|",
        "color"
    };
    }


    static float fixedProgress = 0;
    static bool barProgressEnded = false;
    static readonly int outputCount = Properties.Settings.Default.buildOutputCount; //1200 aproximación

    static void ChangeFixedProgress() // se llama por cada output del bat
    {
        fixedProgress++;
        var p = (fixedProgress / outputCount) * 100;

        if (fixedProgress < outputCount)
        {
            int progress = p.ToInt();
            instance.Progress = progress;
            TaskBar.Set100(progress);
        }
        else
        {
            if (!barProgressEnded)
            {
                TaskBar.Indeterminate();
                barProgressEnded = true;
            }
        }
    }
    static void BuildingFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            //--- FINISH

            Properties.Settings.Default.buildOutputCount = fixedProgress.ToInt();
            Properties.Settings.Default.Save();



            Output.Log("|--------------------------------------------------|");
            Output.Log("|                                                  |");
            Output.Log("|         Compilacion completada exitosamente!     |");
            Output.Log("|                                                  |");
            Output.Log("|--------------------------------------------------|");

            Output.Log("si no se muestran las carpetas, talvez es porque hay un error de compilación de manual");


            stopWatch.Stop();
            Output.Log($"output>> Building finished in {fixedProgress}/it, {stopWatch.Elapsed.TotalSeconds} seconds");


            fixedProgress = 0;
            instance.Progress = 0;
            barProgressEnded = false;

            TaskBar.Stop();
            TaskBar.Notification();

        //    Output.Log("ya puedes ver en la carpeta Releases el setup.exe");

        });

    }
    private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        Output.Log("output>> " + e.Data);
        ChangeFixedProgress();
    }
    private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        Output.Log("output>> " + e.Data);
        ChangeFixedProgress();
    }

    //FORMATTING
    public static string StandardToFormattedVersion(string version)
    {
        // Dividir la versión en partes usando el punto
        var parts = version.Split('.');

        // Construir la versión formateada
        string formattedVersion = parts[0] + "."; // Mantener el primer número con su punto
        for (int i = 1; i < parts.Length; i++)
        {
            formattedVersion += parts[i]; // Concatenar los números restantes
        }

        // Añadiendo el sufijo
        formattedVersion += "-" + instance.Suffix;

        return formattedVersion;
    }
    public static string FormattedToStandardVersion(string version)
    {
        // Extrayendo los números del string
        var numbers = string.Concat(version.Where(char.IsDigit));

        // Actualizando el sufijo
        instance.Suffix = new string(version.Where(c => !char.IsDigit(c)).ToArray());

        // Insertando los puntos para el formato de versión
        if (numbers.Length >= 3)
        {
            var standardVersion = numbers.Insert(1, ".").Insert(3, ".");
            return standardVersion;
        }

        // Si el formato no es el esperado, puedes retornar la versión original o lanzar una excepción
        return "error";
    }


    // PROYECT SETTINGS
    public static string Admin_GetCsprojVariable(string variableName)
    {
        string proyectCsprojPath = Path.Combine(RepoAppPath(), $"{instance.AppName}.csproj");

        XDocument csprojDocument = XDocument.Load(proyectCsprojPath);
        XElement versionElement = csprojDocument.Descendants(variableName).FirstOrDefault();

        if (versionElement != null)
        {
            return versionElement.Value;
        }
        else
        {
            throw new InvalidOperationException("No se encontró el elemento <Version> en el archivo .csproj.");
        }
    }
    public static void Admin_SetCsprojVariable(string variableName, string newVersion)
    {
        string proyectCsprojPath = Path.Combine(RepoAppPath(), $"{instance.AppName}.csproj");

        XDocument csprojDocument = XDocument.Load(proyectCsprojPath);
        XElement versionElement = csprojDocument.Descendants(variableName).FirstOrDefault();

        if (versionElement != null)
        {
            // Actualiza el valor de la versión
            versionElement.Value = newVersion;

            // Guarda los cambios en el archivo .csproj
            csprojDocument.Save(proyectCsprojPath);
        }
        else
        {
            throw new InvalidOperationException("No se encontró el elemento <Version> en el archivo .csproj.");
        }
    }


    //SETTINGS.DESIGNER.CS

    public static string Admin_GetProjectSettings(string variableName)
    {
        string proyectSettingsPath = Path.Combine(RepoAppPath(), "Properties", "Settings.Designer.cs");
        // Leer el contenido del archivo
        var lines = File.ReadAllLines(proyectSettingsPath);

        // Buscar la línea que contiene la configuración y extraer el valor
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains($"public string {variableName}"))
            {
                for (int j = i; j < lines.Length; j++)
                {
                    // Buscar la línea que contiene el valor actual y extraerlo
                    if (lines[j].Contains("return"))
                    {
                        var match = Regex.Match(lines[j], @"return ""(.*?)"";");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                        break;
                    }
                }
                break;
            }
        }

        // Si no se encuentra la propiedad, retornar null o lanzar una excepción
        return null;
    }
    public static void Admin_SetProjectSettings(string variableName, string value)
    {
        // Leer las líneas del archivo
        string proyectSettingsPath = Path.Combine(RepoAppPath(), "Properties", "Settings.Designer.cs");
        var lines = File.ReadAllLines(proyectSettingsPath);

        // Buscar la línea que contiene la configuración y modificar el valor
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains($"public string {variableName}"))
            {
                for (int j = i; j < lines.Length; j++)
                {
                    // Buscar la línea que contiene el valor actual y modificarlo
                    if (lines[j].Contains("return"))
                    {
                        lines[j] = $"            return \"{value}\";";
                        break;
                    }
                }
                break;
            }
        }

        // Guardar las líneas modificadas de nuevo en el archivo
        File.WriteAllLines(proyectSettingsPath, lines);
    }


    public static void Admin_OpenProjectFolder()
    {
        if (Directory.Exists(SolutionPath))
        {
            Process.Start("explorer.exe", SolutionPath);
        }
        else
        {
            // La carpeta no existe, puedes manejar esto como desees
            Output.Log("La carpeta no existe.");
        }

    }









    // ------------------------------------------------------------------------------------------------- PUBLISH
    static List<string> PublishToGithub()
    {
        return new List<string>
    {
        "@echo off",
        "git pull",
        "git add .",
        "git commit -m \"updating manual\" ",
        "git push",

    };
    }

    static string webRepoPath = Path.Combine(FileManager.USERPROFILE_PATH, "source", "repos", "manualweb", "stable-diffusion-ui");
    public static void Admin_MoveReleaseToWebRepo()
    {
        string appname = "manual";
        if (instance.AppName == "Manual")
            appname = "manual";
        else if (instance.AppName == "MLauncher")
            appname = "launcher";

        string appDir = Path.Combine(SolutionPath, "Releases");
        string destDir = Path.Combine(webRepoPath, "public", appname);
        FileManager.ReplaceFolderContent(appDir, destDir);

        ChangeViewTo(instance.output);
        Output.Log($"moved {appname} Releases {appDir} to {destDir}");
    }
    //git add .
    //git commit -m "mensaje"
    //git push
    public static void Admin_PublishOnline()
    {
        ChangeViewTo(instance.output);

        string link = "C:\\Users\\YO\\source\\repos\\manualweb\\stable-diffusion-ui";
        Output.Log("publishing...");
        FileManager.CMD(PublishToGithub(), link, true, OutputDataReceived, ErrorDataReceived, BuildingFinished);
    }

}









//------------------------------------------------------------------------------------------------------------------------------- MANAGE SESSION
public partial class Launch
{
    public static async void LoadSession() // at start
    {
        // var user = await Login.LoadSession();
        var user = new User() { Admin = true };
        SessionLoaded(user);
    }
    public static async void LoadSession(string sesioncode) // at login
    {
       // var user = await Login.LoadSession(sesioncode);
        var user = new User() { Admin = true };
        SessionLoaded(user);
    }
    static void SessionLoaded(User? user)
    {
 
        instance.User = user;
        AppModel.GoToLaunch();

     //   if (User.isAdmin)
            Admin_LoadingProject();
    }

    public static void LogOut()
    {
        Login.LogOut();
        instance.User = null;
    }
}