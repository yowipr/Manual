using ManualToolkit.Windows;
using Squirrel;
using Squirrel.SimpleSplat;
using Squirrel.Sources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ManualToolkit.Generic;

public static class Updater
{
    public static bool isUpdateMode = false;

    public static string releaseURL = "https://stable-diffusion-ui-pink.vercel.app/manual";               //"https://github.com/mariprrum2/m2"; //GITHUB
    public static bool isRestartAfterUpdate = true;

    //-------------------------------------------------- CHECK FOR UPDATES
    public static async Task GithubCheckForUpdates()
    {
        try
        {
            using var mgr = new GithubUpdateManager(releaseURL);
            await checkForUpdates(mgr);
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnException?.Invoke(ex);
            });
        }
    }
    public static async Task CheckForUpdates()
    {
        try
        {
            using var mgr = new UpdateManager(releaseURL);
            await checkForUpdates(mgr);
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnException?.Invoke(ex);
            });
        }
    }
    static async Task checkForUpdates(UpdateManager mgr)
    {
       
            var updateInfo = await mgr.CheckForUpdate(false, CheckingProgress);

            if (updateInfo.CurrentlyInstalledVersion != null)
            {
                Version newVersion = updateInfo.FutureReleaseEntry.Version.Version;
                Version currentVersion = updateInfo.CurrentlyInstalledVersion.Version.Version;
                bool isNewVersionAvailable = currentVersion != newVersion;

                if (isNewVersionAvailable)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnNewUpdateAvailable?.Invoke(updateInfo.FutureReleaseEntry.Version.ToString()); // new version message box
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OnNewUpdateAvailable?.Invoke(null);
                    });
                }
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnNotNewUpdateAvailable?.Invoke("Not installed");
                });
            }
    }












    //------------------------------------------- UPDATE
    public static async Task GithubUpdate()
    {
        try
        {
            using var mgr = new GithubUpdateManager(releaseURL);
            await update(mgr);
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnException?.Invoke(ex);
            });
        }
    }
    public static async Task Update()
    {
        try
        {
            using var mgr = new UpdateManager(releaseURL);
            await update(mgr);
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnException?.Invoke(ex);
            });
        }
    }
    static async Task update(UpdateManager mgr)
    {
       
            var newVersion = await mgr.UpdateApp(UpdateProgress);

            // You must restart to complete the update. 
            // This can be done later / at any time.
            if (newVersion != null && isRestartAfterUpdate)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnFinishedUpdate?.Invoke();
                });
                /*  Output.LogCascadeNamed("",
                  "new version", newVersion.Version.Version,
                  "file size", newVersion.Filesize,
                  "squirrel log", newVersion.Log()
                  );*/

                GithubUpdateManager.RestartApp();
            }
        
      


    }



    //----------------------------------------------------------- ON UPDATING

    public static Action<string>? OnNotNewUpdateAvailable;
    public static Action<string?>? OnNewUpdateAvailable;
    public static Action<int>? OnCheckingUpdatesProgress;
    public static Action<int>? OnUpdateProgress;
    public static Action<Exception> OnException = (ex) => { MessageBox.Show(ex.Message); } ;
    public static Action? OnFinishedUpdate;
    public static void CheckingProgress(int progress)
    {
       Application.Current.Dispatcher.Invoke(() =>
        {
            OnCheckingUpdatesProgress?.Invoke(progress);
        });
    }
   public static void UpdateProgress(int progress)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnUpdateProgress?.Invoke(progress);
        });
    }

}
