using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using MLauncher.Sections;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MLauncher.Core;

/// <summary>
/// Manual manage the Updater, OJO no confundir con Squirrel.UpdateManager
/// </summary>
public static class UpdaterManager
{
    public static string releaseURL
    {
        get => Updater.releaseURL;
        set
        {
            Updater.releaseURL = value;
        }
    }
    public static bool isUpdateMode
    {
        get => Updater.isUpdateMode;
        set
        {
            Updater.isUpdateMode = value;
        }
    }

    public static async void Update()
    {
        if (!isUpdateMode)
            Output.Log("Updating...");

        await Updater.Update();
    }

    public static async void CheckForUpdates()
    {
        if (!isUpdateMode)
            Output.Log($"Checking for updates...{Updater.releaseURL}");

        await Updater.CheckForUpdates();
    }
    public static async void GithubCheckForUpdates()
    {
        if (!isUpdateMode)
            Output.Log($"Checking for updates Github ... {Updater.releaseURL}");

        await Updater.GithubCheckForUpdates();
    }

    public static void Initialize() // at start or at launcher update
    {
        Updater.releaseURL = $"{Constants.WebURL}/launcher";

        Updater.OnNotNewUpdateAvailable = NotNewUpdateAvailable;
        Updater.OnNewUpdateAvailable = NewUpdateAvailable;
        Updater.OnException = UpdateException;
        Updater.OnFinishedUpdate = OnUpdateFinished;
        Updater.OnUpdateProgress = UpdateProgress;
        Updater.OnCheckingUpdatesProgress = CheckForUpdatesProgress;
    }
    static void NotNewUpdateAvailable(string message)
    {
        Output.Log(message);
        TaskBar.Stop();
    }

    static void NewUpdateAvailable(string newRelease)
    {
        if(newRelease == null)
        {
            Output.Log("currently last version");
            return;
        }

        Output.Log("new version available");
        Updater.Update();
    }

    static void UpdateException(Exception ex)
    {
        Output.Log(ex.ToString());

        TaskBar.Notification();
        
    }
    public static void CheckForUpdatesProgress(int progress)
    {
           TaskBar.Set100(progress);
    }
    public static void UpdateProgress(int progress)
    {

           TaskBar.Set100(progress);
        
    }

    public static void OnUpdateFinished()
    {
        TaskBar.Stop();
       MessageBox.Show("Update Finished!");

        Updater.isUpdateMode = false;

    }





}
