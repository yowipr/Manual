using Manual.API;
using Manual.Core.Nodes;
using Manual.Core.Nodes.ComfyUI;
using Manual.Editors.Displays;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manual.Core.Nodes.ProAPI;

internal abstract class ProAPINode : NodeOutput, IOutputNode
{
    public const string ApiURL = Constants.WebURL;  //"http://192.168.1.3:3000/";

    public ProAPINode()
    {
        IsOutput = true;
    }
    public abstract string URL(string baseURl);
    public abstract object OnSendingPrompt();

    public abstract Task OnPreview();
    public abstract Task OnOutput(JObject r);


    internal bool HandleGenerate = true;

    public async Task Generate()
    {
        try
        {
            var token = UserManager.GetToken();
            if (token == null) //not pro
            {
                M_MessageBox.Show("You need to buy Manual Pro to use this feature, click OK to see more", "Free limitation", System.Windows.MessageBoxButton.OKCancel,
                    () => WebManager.OPEN(WebManager.Combine(Constants.WebURL, "pricing")));
                return;
            }


            AppModel.Invoke(()=>AppModel.mainW.SetProgress(1, "Generating..."));

            var web = ApiURL;
            var subPath = "api/generate";

            var url = URL(WebManager.Combine(web, subPath));
            var body = OnSendingPrompt();
         
            if (HandleGenerate)
            {
                // Asume que WebManager.POST se encarga de la serialización JSON correctamente
                var response = await WebManager.POST(url, body, token);
                if (!string.IsNullOrEmpty(response.ToString()))
                {
                    await OnOutput(response);
                    //Output.Log(response);
                }
                else
                {
                    Output.Log("No response received from the API.");
                }
            }
        }
        catch (Exception ex)
        {
            Output.Log($"Error during API call: {ex.Message}");
        }
        finally
        {
            AppModel.Invoke(()=>AppModel.mainW.StopProgress());
        }
    }
}
