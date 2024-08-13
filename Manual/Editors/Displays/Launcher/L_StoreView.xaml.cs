using CefSharp;
using CefSharp.Handler;
using Manual.API;
using Manual.Core;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.IO;
using CefSharp.Wpf;
using System.Collections.ObjectModel;

namespace Manual.Editors.Displays.Launcher;


public class L_Store
{
    public bool hasError = false;
    public L_Store()
    {
            
    }
}

/// <summary>
/// Lógica de interacción para L_Store.xaml
/// </summary>
public partial class L_StoreView : UserControl
{
 //   ChromiumWebBrowser browser;
    public L_StoreView()
    {
        CefWebManager.Initialize();

        InitializeComponent();


        browser.Address = WebManager.Combine(Constants.WebURL, "asset-store?manual=true"); //Address = "https://manualai.art/asset-store"

        browser.FrameLoadEnd += Browser_FrameLoadEnd;
        browser.RequestHandler = new AssetInstructionHandler();


        browser.LoadError += OnBrowserLoadError;


    }

    private void OnBrowserLoadError(object sender, LoadErrorEventArgs e)
    {
        // Puedes usar e.ErrorCode y e.ErrorText para obtener detalles sobre el error
        Console.WriteLine($"Error al cargar la página: {e.ErrorText}");

        // Por ejemplo, si quieres reaccionar específicamente a errores de conexión
        if (e.ErrorCode == CefErrorCode.NameNotResolved)
        {
            Console.WriteLine("La página no se pudo cargar porque el servidor DNS no encuentra el servidor.");
        }
        Application.Current.Dispatcher.Invoke(() =>
        {
            ((L_Store)DataContext).hasError = true;
            MessageBox.Show(e.ErrorText);
        });
    }

    private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
    {
        //when browser loaded
        if (e.Frame.IsMain)
        {
            browser.ExecuteScriptAsync("document.body.style.overflowX = 'hidden';");        //disable horizontal scrollbar
            browser.SetZoomLevel(-2.5);

            Dispatcher.Invoke(() =>
            {
                loader.Visibility = Visibility.Collapsed;
            });
        }

    }

    private void Acercar()
    {
        browser.GetZoomLevelAsync().ContinueWith(previousZoomLevel =>
        {
            var newZoomLevel = previousZoomLevel.Result + 0.5; // Ajusta este valor según necesites
            browser.SetZoomLevel(newZoomLevel);
            Debug.WriteLine($"browser zoom: {newZoomLevel}?manual=true");
        });
    }

    private void Alejar()
    {
        browser.GetZoomLevelAsync().ContinueWith(previousZoomLevel =>
        {
            var newZoomLevel = previousZoomLevel.Result - 0.5; // Ajusta este valor según necesites
            browser.SetZoomLevel(newZoomLevel);
            Debug.WriteLine($"browser zoom: {newZoomLevel}");
        });
    }


    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    Acercar();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    Alejar();
                    e.Handled = true;
                    break;
            }
        }
    }



}



public class AssetInstructionHandler : RequestHandler
{
    protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect) //--------------------------- ON ASSET INSTALLING
    {
        // Verifica si la URL intenta lanzar un protocolo externo y no es una navegación web estándar
        if (!request.Url.StartsWith("http") && !request.Url.StartsWith("https"))
        {
            // Aquí puedes manejar la URL como prefieras (mostrar un mensaje, bloquear, etc.)
            // Por ejemplo, imprimir en consola y bloquear la navegación:
            Debug.WriteLine($"AssetInstructionHandler: intento de abrir URL externa: {request.Url}");

            var commands = WebManager.ExtractQuery(request.Url, "install_asset");
            var ins = new Instructions(commands);
            ins.Name = "install_asset";
            AppModel.Invoke(() => { AppModel.launcher.Downloads.Add(ins); });
          
            //foreach (var query in querys)
            //{
            //    if(query.Key == "install_asset")
            //    {

            //    }
            //}


            return true; // Retorna true para cancelar la navegación
        }

        // Para cualquier otra cosa, usa el comportamiento predeterminado
        return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);
    }





}




public class Instructions : DownloadItem
{

    public string[] Commands { get; set; }
    public Instructions(string commands)
    {
        Commands = commands.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        onExecute = ProcessCommands;
        TotalSteps = Commands.Length;
        Name = "Installing Template...";
    }


    public async Task ProcessCommands()
    {
        CurrentStep = 0;
        foreach (var commandLine in Commands)
        {
            try
            {
                await HandleCommand(commandLine);
                CurrentStep++;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        AppModel.launcher.Downloads.Remove(this);
    }

    public async Task HandleCommand(string command)
    {
        string[] parts = command.Split(' ');
        string operation = parts[0].ToLower();

        switch (operation)
        {
            case "install":
                if (parts.Length < 2) throw new ArgumentException("No hay suficientes argumentos para install.");

                string type = null;
                string installUrl = null;
                string name = null;
                if (parts.Length == 2)
                {
                    type = "civitai";
                    installUrl = parts[1];
                }
                else if (parts.Length == 3)
                {
                    type = parts[1];
                    installUrl = parts[2];
                }
                else if (parts.Length == 4)
                {
                    type = parts[1];
                    installUrl = parts[2];
                    name = parts[3];
                }
                if (type != null && installUrl != null)
                    await Install(installUrl, type, name);
                break;


            case "wget":
                if (parts.Length < 3) throw new ArgumentException("No hay suficientes argumentos para wget.");
                string wgetUrl = parts[1];
                string destination = parts[2];
                await Wget(wgetUrl, destination);
                break;


            case "git":
                if (parts.Length < 3) throw new ArgumentException("No hay suficientes argumentos para git.");
                string subCommand = parts[1].ToLower();
                switch (subCommand)
                {
                    case "clone":
                        string gitUrl = parts[2];
                        string gitDestination = parts[3];
                        await GitClone(gitUrl, gitDestination);
                        break;
                    default:
                        Console.WriteLine($"Subcomando de git desconocido: {subCommand}");
                        break;
                }
                break;


            default:
                Output.Log($"Operación desconocida: {operation}");
                break;
        }
    }



    //general
    public async Task Wget(string url, string destinationDirectory)
    {
        await StartDownload(url, destinationDirectory);
    }
    public  async Task GitClone(string url, string destinationDirectory)
    {
        await StartClone(url, destinationDirectory);
    }




    public async Task Install(string url, string type, string fileName = null)
    {
        if (type == "node")
            await InstallNode(url);
        else
            await InstallModel(url, type, fileName);
    }
    public async Task InstallNode(string url)
    {
        string nodesPath = Path.Combine(ComfyUIServer.DirectoryPath, "ComfyUI", "custom_nodes");
        await StartClone(url, nodesPath);

    }

    public async Task InstallModel(string url, string type, string fileName = null)
    {
        string modelsPath = Path.Combine(ComfyUIServer.DirectoryPath, "ComfyUI", "models");
        string? subpath = null;


        //civitai
        if (type == "civitai" && url.Contains("civitai"))
        {
            var basePart = url.Split('?')[0]; // Esto elimina los parámetros de consulta
            string modelId = basePart.Split("/").Last();


            string modelP = $"https://civitai.com/api/v1/models/{modelId}";

            var result = await WebManager.GET(modelP);
            var t = result["type"]?.ToString();
            var name = result["name"]?.ToString();

            Debug.WriteLine($"{t}, {name}");

            switch (t)
            {
                case "Checkpoint": subpath = "checkpoints"; break;
                case "TextualInversion": subpath = "embeddings"; break;
                case "Hypernetwork": subpath = "hypernetworks"; break;
                case "LORA": subpath = "loras"; break;
                case "ControlNet": subpath = "controlnet"; break;
                default:
                    Output.Log($"something went wrong downloading{url} type: {type}, will be ignored");
                    break;
            }


            if (subpath != null)
                await StartDownload(url, Path.Combine(modelsPath, subpath), name);


        }


        //normal
        else
        {

            switch (type)
            {
                case "checkpoint": subpath = "checkpoints"; break;
                case "textual_inversion": subpath = "embeddings"; break;
                case "hypernetwork": subpath = "hypernetworks"; break;
                case "lora": subpath = "loras"; break;
                case "controlnet": subpath = "controlnet"; break;
                case "clip": subpath = "clip"; break;
                case "clip_vision": subpath = "clip_vision"; break;
                case "vae": subpath = "vae"; break;
                default:
                    subpath = type;
                    break;
            }


            if (subpath != null)
                await StartDownload(url, Path.Combine(modelsPath, subpath), fileName);


        }




    }


}
