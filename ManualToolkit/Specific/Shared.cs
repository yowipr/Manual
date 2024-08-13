using CommunityToolkit.Mvvm.ComponentModel;
using ManualToolkit.Windows;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ManualToolkit.Generic;

namespace ManualToolkit.Specific;

//----------------------------------------------------------------------------- SHARED WITH MANUAL AND LAUNCHER


public static class Constants
{
    // https://api.runpod.ai/v2/7e1pwyobst2s9i/runsync    https://dbd556db5900deaffe.gradio.live/     https://manualai.art/api/verify-client  http://192.168.1.11:3000/api/verify-client

    public const string WebURL = "https://manualai.art";
    public const string AuthToken = "mI7g49bgI9rm6osNsIEFsUFbkwIPGTXHoJsol2kaGXQztvXSb6";
    /// <summary>
    /// C:\Users\YO\source\repos\Manual\Manual
    /// </summary>
    /// <returns></returns>
    public static string ProyectURL() => Path.Combine(FileManager.USERPROFILE_PATH, "source", "repos", "Manual");
}





//-------------------------------------------------------------------------------------------------------------- USER
public partial class User : ObservableObject
{
    [ObservableProperty] string name = "anon";
    [ObservableProperty] string? id;
    [ObservableProperty] string paid = "free";
    //  [ObservableProperty] string? image;
    [ObservableProperty] string email;
    [ObservableProperty] bool admin;

    public User()
    {
            
    }
    public Dictionary<string, ProductDetails> Products { get; set; }


    static User? current;
    public static User? Current
    {
        get => current; set
        {
            current = value;
            OnUserChanged?.Invoke(value);
        }
    }
    public static event Event<User> OnUserChanged;

    public static bool isAdmin
    {
        get
        {
            if (Current != null)
            {
                return Current.Admin;
            }
            else return false;
        }
        set
        {
            if (Current != null)
                Current.Admin = value;
        }
    }
    public static bool isPro
    {
        get
        {
            if (Current != null)
            {
                return Current.Products["manual"].Plan == UserPlan.pro.ToString();
            }
            else return false;
        }
    }

    string? _image;
    public string? Image
    {
        get => _image;
        set
        {
            if (value != null)
                LoadImage(value);
        }
    }
    [ObservableProperty] BitmapImage? imageSource = (BitmapImage)Application.Current.Resources["user-circle"];
    private async void LoadImage(string imageUrl)
    {
        ImageSource = await WebManager.DownloadImage(imageUrl);
    }

    public override string ToString()
    {
        return $"Name: {Name}, Plan: {Products["manual"].Plan}, isAdmin: {isAdmin}, Email: {Email}";
    }
}

public enum UserPlan
{
    free,
    pro,
    enterprise
}
//---------------------------- PRODUCTS
public class ProductDetails
{
    public string? Plan { get; set; }
    public string? Price { get; set; }
    public string? Interval { get; set; }
    public string? NextBillingTime { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? Address { get; set; }
    public bool? Pay { get; set; }
}

public class TransactionDetails
{
    public TransactionDetails(string grossAmount, string time, string status)
    {
        GrossAmount = grossAmount;
        Time = time;
        Status = status;
    }

    public string? GrossAmount { get; set; }
    [JsonProperty("amount_with_breakdown")]
    public JObject AmountWithBreakdown
    {
        set
        {
            if (value != null && value["gross_amount"]?["value"] != null)
            {
                GrossAmount = value["gross_amount"]?["value"]?.ToString();
            }
        }
    }
    public string? Time { get; set; }
    private string? _status;
    public string? Status
    {
        get => _status;
        set
        {
            _status = value;
            if (value != null)
            {
                StatusText = value.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ? "Paid" : "Unpaid";
                IsCompleted = value.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
    public bool IsCompleted { get; private set; }
    public string? StatusText { get; private set; }
}





//------------------------------------------------------------------------------------------------------------------ LOGIN
public partial class Login : ObservableObject
{
    public static string tpath => Path.Combine(mwtDir, "MWT.dat");
    public static string mwtDir = "";

    static Login instance;
    public Login()
    {
        instance = this;
    }


    [ObservableProperty] private string newToken = "none";
    [ObservableProperty] private User? user = null;

    public string url = $"{Constants.WebURL}/?fromLauncher=yes";
    public void LogInWeb()
    {
        WebManager.OPEN(url);
    }

    /// <summary>
    ///  null to account OR account to account OR account to null
    /// </summary>
    /// <param name="newLogin"></param>
    public void ChangeLoginTo(Login newLogin)
    {
        if (newLogin != null)
        {
            this.User = newLogin.User;
            this.NewToken = newLogin.NewToken;
        }
        else
        {
            LogOut();
        }
    }


    public static string? GetToken()
    {
        if (instance != null)
            return instance.NewToken;
        else return null; // not logged
    }
    //--------------------------------------------------------------- SESIONS
    public static async Task<User?> LoadSession() // automatic login at startup with token
    {
        User.isAdmin = true;

        if (!File.Exists(tpath))
            return null;

        return await LoadSession(FileManager.LoadToken(tpath));
    }
    public static async Task<User?> LoadSession(string token) //---------------- ON SUCCESS LOGIN / ALSO STARTUP LOGGED AUTOMATICALLY
    {

        if (token == null)
        {
            return null;
        }

        var login = await WebManager.GET<Login>($"{Constants.WebURL}/api/user", token);

        if (login == null)
            return null;


        instance = login;
        User.Current = login.User;

        Application.Current.Dispatcher.Invoke(() =>
        {
            FileManager.SaveToken(login.NewToken,tpath);
        });
        return login.User;

    }
    public static void LogOut()
    {
        if (!File.Exists(tpath))
            return;

        File.Delete(tpath);
        instance.User = null;
    }



}


