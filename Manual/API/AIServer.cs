using CefSharp.DevTools.Debugger;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.Core;
using Manual.Editors.Displays.Launcher;
using ManualToolkit.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manual.API;

//public interface IAIServer
//{
//    string URLPath { get; set; }
//    public void Run();
//    public void Close();
//}

public enum OutputType
{
    Message,
    Warning,
    Error
}

public class AIServersManager
{
    public void CloneRepository(string repoUrl, string localPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("git")
        {
            Arguments = $"clone {repoUrl} {localPath}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                Console.WriteLine(result);
            }
        }
    }
}

//public partial class Automatic1111Server : ObservableObject, IAIServer
//{
//    [ObservableProperty] string uRLPath  = Path.Combine(FileManager.USERPROFILE_PATH, "source\\repos\\stable-diffusion-webui");
//    BatFile batFile;

//    public async void Run()
//    {
//        string filePath = Path.Combine(URLPath, "webui.bat");
//        batFile = new BatFile(filePath);

//        Output.WriteLine("Opening Automatic1111 Server...");
//        await batFile.Open(true, OutputDataReceived, ErrorDataReceived, OnClose);
//    }
//    public void Close()
//    {
//        if (batFile != null)
//        {

//            batFile.Close();
//            Output.WriteLine("Automatic1111 Server closed");
//        }
//    }


//    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
//    {
//        Output.WriteLine("output>> " + e.Data);
//    }
//    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
//    {
//        Output.WriteLine("error>> " + e.Data);
//    }

//    void OnClose()
//    {
//        Output.Log("Automatic1111 server closed");
//    }



//}


public abstract partial class AIServer : ObservableObject
{
    /// <summary>
    /// enbed path with python
    /// </summary>
    [ObservableProperty] string dirPath = "";

    [ObservableProperty] string url = "http://127.0.0.1:8188";

    public bool IsDownloaded;

    [ObservableProperty] bool isOpened;
    [ObservableProperty] [property: JsonIgnore] bool isOpening;

    public abstract void Run();
    public abstract void Close();

    public abstract Task<bool> IsServerReachable();

    [ObservableProperty] bool isInNewWindow = false;
    [ObservableProperty] string startArguments = serverArgs;
    const string serverArgs = "--windows-standalone-build --multi-user --disable-auto-launch";

    [RelayCommand]
    [property: JsonIgnore]
    void ResetArguments()
    {
        StartArguments = serverArgs;
    }

    [ObservableProperty] bool isRememberServerState = true;


}

public partial class ComfyUIServer : AIServer// ObservableObject, IAIServer
{
    public static string DirectoryPath => Settings.instance.AIServer.DirPath;

    BatFile batFile;
    public ComfyUIServer()
    {
        DirPath = Path.Combine(FileManager.USERPROFILE_PATH, "source\\repos\\ComfyUI_windows_portable");
    }



    public async override void Run()
    {
        //check if its already opened externally
        if (FileManager.IsProcessRunning("python", "ComfyUI"))
        {
            Cancel();
            if (await IsServerReachable(Url))
            {       
                AppModel.mainW.CheckServerStatus();
            }
            Output.Log("ComfyUI server detected externally");
            return;
        }
        else if (await IsServerReachable(Url))
        {
            AppModel.mainW.CheckServerStatus();
            Cancel();
            return;
        }


        if (!IsInNewWindow)
         AppModel.mainW.SetProgress(0.5f);
        //DirPath: C:\Users\lazar\AppData\Local\Manual\Dependencies\ComfyUI_windows_portable\ComfyUI

        var server = Settings.instance.AIServer;

        if(IsOpened || IsOpening)
        {
            Output.Log("Server already opened");
            AppModel.mainW?.StopProgress();
            return;
        }
        if(string.IsNullOrEmpty(DirPath) || !Directory.Exists(DirPath))
        {
            Output.Log("Comfy Server not installed or directory not recognized, check in Edit -> Preferences -> System -> ComfyUI");
            Cancel();
            return;
        }

        try
        {
            var dirInfo = new DirectoryInfo(DirPath);
            if (dirInfo.Name == "ComfyUI")
                dirInfo = dirInfo.Parent;

            IsOpening = true;
            string filePath = Path.Combine(dirInfo.FullName, "run_nvidia_gpu.bat");
            batFile = new BatFile(filePath);

            if (IsInNewWindow)
            {
                Output.Log("Server startig, wait until you see the comfy URL in the console window");
                AppModel.mainW?.ChangeServerStatus(MainWindow.ServerStatus.Connected);
              
            }
            else
            {
                Output.Log("Opening Server...");
                AppModel.mainW?.ChangeServerStatus(MainWindow.ServerStatus.Connecting);
            }

            var realPath = Path.Combine(dirInfo.FullName, "python_embeded/python.exe");
            var mainpyPath = "ComfyUI/main.py";
            if (!File.Exists(realPath))
            {
                // Output.Log("python.exe does not exist\n{realPath}", "Can't open Server");
                realPath = "python";
                mainpyPath = "main.py";
            }
            if (!File.Exists(realPath))
            {
                Output.Log("Comfy Server not installed or directory not recognized, check in Edit -> Preferences -> System -> ComfyUI");
                Cancel();
                return;
            }

            if (!BatFile.CheckPython())
            {
                Output.Log("cannot open Comfy Server: missing python. try using portable embeded Comfy as directory");
                Cancel();
                return;
            }

           await batFile.OpenPython(
                python_embededPath: realPath,
                programPath: Path.Combine(dirInfo.FullName, mainpyPath),
                arguments: server.StartArguments,
                OutputDataReceived, ErrorDataReceived,
                window: server.IsInNewWindow);
        }
        catch(Exception ex)
        {
            Output.LogEx(ex);
            Cancel();
        }

    }

    public override void Close()
    {
        if (batFile != null && IsOpened)
        {
            Core.Nodes.ComfyUI.Comfy.Disconnect();

            batFile.Close();
            OnClose();

            if(!AppModel.UninstantiateThings)
               IsOpened = false;
        }
    }

    void Cancel()
    {
        IsOpened = false;
        IsOpening = false;
        AppModel.mainW?.ChangeServerStatus(MainWindow.ServerStatus.Disconnected);
        AppModel.mainW?.StopProgress();
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        ManageData(e.Data, OutputType.Message);
      
    }
    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        ManageData(e.Data, OutputType.Error);
    }

    static void ManageData(string? data, OutputType type)
    {
        if (data == null)
            return;

        if (data.StartsWith(prefix))
        {
            AppModel.Invoke(() => ServerStarted(data));
            return;
        }


        if (type == OutputType.Message)
            Output.WriteLine(data);
        else if (type == OutputType.Error)
            Output.WriteLine(data);

    }
    const string prefix = "To see the GUI go to:";
    static void ServerStarted(string? data)
    {
        AppModel.mainW?.StopProgress();

        var server = Settings.instance.AIServer;
        server.IsDownloaded = true;
        Output.Log("Server Running");

        server.Url = data.Substring(prefix.Length).Trim();
        Settings.instance.CurrentURL = server.Url;
        // ActionHistory.DoAction.Do(AppModel.mainW.CheckServerStatus, 3);
        AppModel.mainW.CheckServerStatus();

        Output.WriteLine($"Server Running on {server.Url}");
        server.IsOpened = true;
        server.IsOpening = false;
    }

    void OnClose()
    {
        Output.Log("Server closed");
        AppModel.mainW?.ChangeServerStatus(MainWindow.ServerStatus.Disconnected);
    }



    public override async Task<bool> IsServerReachable()
    {
        return await IsServerReachable(Settings.instance.CurrentURL);
    }
    public async Task<bool> IsServerReachable(string url)
    {
        using HttpClient httpClient = new();
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);

            // Si la respuesta es exitosa (código de estado 2xx)
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            // Manejar excepciones de solicitud HTTP, como cuando el servidor no está disponible
            return false;
        }
        catch (TaskCanceledException)
        {
            // Manejar excepciones cuando la solicitud se cancela (tiempo de espera agotado)
            return false;
        }
        catch (Exception)
        {
            // Manejar cualquier otra excepción inesperada
            return false;
        }
    }


    public static void InstallComfy(Action OnFinalized)
    {
        /*
        ComfyUI 1.4GB
        
         */


        var totalSteps = 4;
        var currentStep = 0;
        // var comfyurl = "https://drive.usercontent.google.com/u/0/uc?id=1Z8p8pJvt0xj2IShvsHi_3RGPHe-j4APY&export=download"; // TEST
        var comfyurl = "https://github.com/comfyanonymous/ComfyUI/releases/download/latest/ComfyUI_windows_portable_nvidia_cu121_or_cpu.7z";

        var dependenciesPath = Path.Combine(FileManager.APPDATA_LOCAL_PATH, "Manual");
        System.IO.Directory.CreateDirectory(dependenciesPath);

         dependenciesPath = Path.Combine(dependenciesPath, "Dependencies");
        System.IO.Directory.CreateDirectory(dependenciesPath);


        var download = AppModel.launcher.Download(comfyurl, dependenciesPath);
        //extract file
        download.OnStartDownloading(() => download.Name = "ComfyUI");
        download.OnFinalize(InstallComfy_Downloaded);
        download.OnDownloading(() => TaskBar.Set100((int)download.Progress));

         async void InstallComfy_Downloaded()
        {
            if(download.Error)
            {
                MessageBox.Show("download error, try later", "Manual Setup");
                return;
            }

            download.Completed = false;
            download.Loading = true;

            //-------------- STEP 1
            currentStep++;
            download.Description = $"({currentStep}/{totalSteps}) Installing ComfyUI...";
            var zipFileName = download.Name;
            var path = Path.Combine(dependenciesPath, zipFileName);

            //-------------- STEP 2
            //EXTRACT
            currentStep++;
            download.Description = $"({currentStep}/{totalSteps}) Extracting...";
            download.ETA = TimeSpan.FromMinutes(5);

            if (FileManager.GetFilePath(dependenciesPath, "ComfyUI", ".7z") is not string realPath)
                MessageBox.Show("error downloading comfy", "Manual Setup");
            else
                FileManager.Extract7zFile(realPath, dependenciesPath, endExtract);
            

            async void endExtract()
            {
                AppModel.Invoke(()=>File.Delete(realPath));

                var comfyPath = Settings.instance.AIServer.DirPath = Path.Combine(dependenciesPath, "ComfyUI_windows_portable", "ComfyUI");

                void GitSteps(string path, int step, int totalSteps)
                    => download.Progress = (((step / totalSteps) + currentStep) / (totalSteps + 1)) * 100;

                //-------------- STEP 3
                //INSTAL COMFY MANAGER
                currentStep++;
                download.Description = $"({currentStep}/{totalSteps}) Installing ComfyUI Manager...";
                var nodesPath = Path.Combine(comfyPath, "custom_nodes");
                var nodeUrl = "https://github.com/ltdrdata/ComfyUI-Manager";
                await WebManager.GitClone(nodeUrl, nodesPath, GitSteps);

                //-------------- STEP 4
                //INSTAL MANUAL NODES
                currentStep++;
                download.Description = $"({currentStep}/{totalSteps}) Installing Manual Nodes...";
                nodeUrl = "https://github.com/yowipr/ComfyUI-Manual";
                await WebManager.GitClone(nodeUrl, nodesPath, GitSteps);


                download.Completed = true;
                Settings.instance.AIServer.IsDownloaded = true;
                Settings.instance.AIServer.IsOpened = true;
 
                OnFinalized?.Invoke();
            }

        }
       
    }



}

