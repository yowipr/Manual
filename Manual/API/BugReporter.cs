using Manual.Core;
using Manual.Editors.Displays;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace Manual.API;

public static class BugReporter
{
    public static void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        App.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ReportException(e.ExceptionObject as Exception);
    }

    private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {

        ReportException(e.Exception);
        e.Handled = true; // para evitar que la aplicación crashee
    }

    public static void ReportException(Exception ex)
    {
        // Lógica para reportar la excepción
        // Puede incluir guardar la excepción en un archivo, enviarla a un servidor, etc.

        System.Windows.Input.Mouse.OverrideCursor = null;
        Application.Current.Dispatcher.Invoke(() => { 
        M_Window.Show(new W_BugReporter(ex), "Opps, something went wrong");
        });
        Mouse.OverrideCursor = null;
    }


    public static void Send(BugReport report)
    {
        SaveReport(report);
        Output.Log("Error Reported!", "BugReporter");
    }

    static void SaveReport(BugReport report)
    {
        string json = JsonConvert.SerializeObject(report, Formatting.Indented);
        string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Log");

        Directory.CreateDirectory(directoryPath);

        string baseFilePath = Path.Combine(directoryPath, "BugReport");
        string filePath = FileManager.GetUniqueFilePath(baseFilePath, ".json");

        // Guardar el JSON en el archivo
        File.WriteAllText(filePath, json);


        //send error
        var url = Constants.WebURL;
        var result = WebManager.POST(url + "/api/notify?about=" + "error", report, Constants.AuthToken);
        if(result.ToString() == "Alert received")
        {
            Output.Log("Bug Report received.");
        }
    }


    public static void GenerateTestException()
    {
        int divisor = 0;
        int result = 10 / divisor; // Esto generará una DivideByZeroException
    }

}

public class BugReport
{
    public string StepsToReproduce { get; set; }
    public string? User { get; set; }
    public string AppVersion { get; set; }
    public string AdditionalInfo { get; set; }

    public string ExceptionMessage { get; set; }
    public string? ExceptionSource { get; set; }


    public BugReport(Exception ex, string stepsToReproduce)
    {
        ExceptionMessage = ex.Message;
        ExceptionSource = ex.Source?.ToString();
        AdditionalInfo = ex.ToString();

        if(ManualToolkit.Specific.User.Current is User u)
            User = u.Name;
        

        StepsToReproduce = stepsToReproduce;
        AppVersion = Settings.instance.Version;
    }
}