using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core;
using Manual.Editors.Displays;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manual.API;

public partial class UserManager : ObservableObject
{
    public static UserManager instance => AppModel.userManager;

    [ObservableProperty] User? user = null;

    [ObservableProperty] UserPlan plan = UserPlan.free;


    public static bool IsPro => AppModel.userManager.Plan == UserPlan.free;
    public static bool IsAdmin => User.isAdmin;

    internal static string? GetToken()
    {
        var token = Login.GetToken();
        return token;
    }

    /// <summary>
    /// the login automatically at start, opens the MLauncher and get the user
    /// </summary>
    public void LogIn()
    {
        SetUser();
    }

    public async void SetUser()
    {
        // string launcherDirPath = Path.Combine(FileManager.APPDATA_LOCAL_PATH, "MLauncher", "MLauncher.exe");
        //   string launcherDirPathDEBUG = Path.Combine(FileManager.USERPROFILE_PATH, "source\\repos\\Manual\\MLauncher\\bin\\Debug\\net8.0-windows7.0"); // DEBUG PATH //TODO: poner el path real del launcher

        //  string launcherDir = Output.SET_VALUE_DEBUG(launcherDirPath, launcherDirPathDEBUG);

        Login.mwtDir = App.LocalPath; //launcherDir;
        var user = await Login.LoadSession();

        if (user != null)
        {
            this.User = user;
            Plan = user.Products["manual"].Plan.ToEnum<UserPlan>();

            //if (Output.DEBUG_BUILD())
            //{
            //    Output.LogCascade(user.Name,
            //        Plan.ToString(),
            //        User.Current
            //        );
            //}

            if (User.isPro)
                InstantiateProThings();

        }
        else
        {
            
        }

     //  if(Output.DEBUG_BUILD())
    //       InstantiateProThings();
    }

 //--------------------------------------------------------------------- START PRO 
    void InstantiateProThings()
    {
       AppModel.settings.Themes.Add(Theme.ManualPro.ToString());
       Settings.ChangeTheme(Theme.ManualPro);

       AppModel.settings.BgGrids.Add(BgGrid.Dot.ToString());
       AppModel.settings.CurrentBgGrid = BgGrid.Dot.ToString();
    }



    public static async void LoadSession(string sesioncode) // at login
    {
        var user = await Login.LoadSession(sesioncode);

        if (W_Login.OpenedLogin != null)
            W_Login.OpenedLogin.window.Close();

        AppModel.Invoke(() =>
        {
            instance.User = user;
        });
    }



    public UserManager()
    {
        ManualToolkit.Specific.User.OnUserChanged += User_OnUserChanged;
    }

    private void User_OnUserChanged(User value)
    {
        AppModel.launcher.NotifyChanged("User");
    }

    internal static void LogOut()
    {
        Login.LogOut();
        AppModel.Invoke(() =>
        {
            instance.User = null;
        });
    }
}
