using Manual.Editors.Displays;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manual.Core;

/// <summary>
/// Manual manage the Updater, OJO no confundir con Squirrel.UpdateManager
/// </summary>
public static class UpdaterManager
{
    public static bool automaticMode = true;

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
       // if (!isUpdateMode)
           // Output.Log("Updating...");

            await Updater.Update();
    }

    public static async void CheckForUpdates()
    {
      //  if (!isUpdateMode)
           // Output.Log($"Checking for updates...{Updater.releaseURL}");

          await Updater.CheckForUpdates();
    }
    public static async void GithubCheckForUpdates()
    {
      //  if(!isUpdateMode)
         //  Output.Log($"Checking for updates Github ... {Updater.releaseURL}");

        await Updater.GithubCheckForUpdates();
    }

    public static void Initialize() // at start or at launcher update
    {
        Updater.releaseURL = $"{Constants.WebURL}/manual";

        Updater.OnNotNewUpdateAvailable = NotNewUpdateAvailable;
        Updater.OnNewUpdateAvailable = NewUpdateAvailable;
        Updater.OnException = UpdateException;
        Updater.OnFinishedUpdate = OnUpdateFinished;
        Updater.OnUpdateProgress = UpdateProgress;
        Updater.OnCheckingUpdatesProgress = CheckForUpdatesProgress;
    }
    static void NotNewUpdateAvailable(string message) // ALREADY UPDATED
    {
        if (!automaticMode)
        {
         //   Output.Log(message, "CurrentlyUpdated");
            TaskBar.Stop();
        }

        Settings.instance.NewUpdateAvailable = false;
    }

    static void NewUpdateAvailable(string? newRelease) // NEW UPDATE
    {
        if (newRelease == null)
            return;

        Settings.instance.NewReleaseAvailable = newRelease;
        Settings.instance.NewUpdateAvailable = true;

        if (!automaticMode)
        {
            M_MessageBox.Show($"New Version: {newRelease}\n Do you want to update? \n \n Manual will restart automatically.",
                "There is a new version available", MessageBoxButton.OKCancel, Update);
            automaticMode = false;
        }
    }

    static void UpdateException(Exception ex)
    {
      //  Output.Log(ex);
        if (isUpdateMode)
        {
            IPCManager.SendMessageToRunningApp($"Updater:error");
            Environment.Exit(0);
        }
        else if(!automaticMode)
        {
            TaskBar.Notification();
        }
    }
    public static void CheckForUpdatesProgress(int progress)
    {
        //  Output.Log(progress);
        if (isUpdateMode)
        {
            IPCManager.SendMessageToRunningApp($"Updater:{progress}");
        }
        else if(!automaticMode)
        {
            TaskBar.Set100(progress);
        }
    }
    public static void UpdateProgress(int progress)
    {
        //  Output.Log(progress);
        if (isUpdateMode)
        {
            IPCManager.SendMessageToRunningApp($"Updater:{progress}");
        }
        else if (!automaticMode)
        {
            TaskBar.Set100(progress);
        }
    }

    public static void OnUpdateFinished()
    {
        TaskBar.Stop();

        if (isUpdateMode)
            IPCManager.SendMessageToRunningApp($"Updater:100");
        else if (!automaticMode)
        {
            MessageBox.Show("Update Finished!");
        }

        Settings.instance.NewUpdateAvailable = false;

        Updater.isUpdateMode = false;
        automaticMode = true;
        //Application.Current.Shutdown();
    }



}
