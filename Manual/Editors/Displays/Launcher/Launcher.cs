using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using CefSharp;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.API;
using Manual.Core;
using Manual.Editors.Displays.Launcher;
using ManualToolkit.Generic;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using Silk.NET.Core.Native;

namespace Manual.Editors.Displays.Launcher;

public partial class LauncherModel : ObservableObject
{
    public W_Launcher ui;
    public User User => User.Current;

    [ObservableProperty] object currentSection;


    //SECTIONS
    public L_Store l_store = new();




    public ObservableCollection<DownloadItem> Downloads { get; set; } = new();
  
    public LauncherModel()
    {
        Downloads.CollectionChanged += Downloads_CollectionChanged;
    }

    private void Downloads_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if(e.NewItems != null)
        foreach (DownloadItem item in e.NewItems)
        {
            QueueDownload();
        }

    }

    public bool isDownloading { get; set; } = false;
    async void QueueDownload()
    {
        isDownloading = true;
        while (isDownloading)
        {
            var item = Downloads.First();
            await item.onExecute();
            Downloads.Remove(item);

            // Actualiza el estado de isDownloading basado en si aún hay elementos en la cola
            isDownloading = Downloads.Count > 0;
        }


        if (((ComfyUIServer)Settings.instance.AIServer).IsDownloaded) // if comfy downloaded
        {
            ui.Setup_StartManual();
        }

    }


    public void NotifyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    public DownloadItem Download(string downloadUrl, string directoryDestinationPath, string? fileName = null)
    {
        var item = new DownloadItem();
        item.OnExecute(async ()=> await item.StartDownload(downloadUrl, directoryDestinationPath, fileName));
        AppModel.Invoke(()=>Downloads.Add(item));
     
        return item;
    }



}



public partial class DownloadItem : ObservableObject
{
    [ObservableProperty] string name = "downloading...";
    [ObservableProperty] string description;

    [NotifyPropertyChangedFor(nameof(RealProgress))]
    [ObservableProperty] float progress = 0; //from 0 to 100
    public int TotalSteps = 1;
    public int CurrentStep = 0;
    public float RealProgress => ((((Progress / 100) / TotalSteps) + CurrentStep) / (TotalSteps)) * 100;


    [ObservableProperty] BitmapImage icon;

    public long TotalGB { get; set; }
    public long CurrentBytes { get; set; }
    public TimeSpan ETA { get; set; }
    public double Velocity { get; set; }

    public string FilePath { get; set; }

    [ObservableProperty] bool error = false;
    [ObservableProperty] bool completed = false;

    [ObservableProperty] bool loading = true;
    public DownloadItem()
    {

    }
    public DownloadItem(string name, string description)
    {
        this.Name = name;
        this.Description = description;
    }
    [ObservableProperty] bool cancelled = false;
    [RelayCommand]
    public void Cancel()
    {
        Cancelled = true;
    }

    public void OnExecute(Func<Task> action)
    {
        onExecute = action;
    }
    public Func<Task> onExecute;

    public async Task<bool> StartDownload(string downloadUrl, string directoryDestinationPath, string? fileName = null)
    {

        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"{response.StatusCode} error");

                    ContentDispositionHeaderValue contentDisposition = response.Content.Headers.ContentDisposition;
                    if (fileName == null && contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileName))
                    {
                        fileName = contentDisposition.FileNameStar ?? contentDisposition.FileName;
                    }
                    else if (fileName == null)
                    {
                        fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                    }

                    fileName = WebManager.RemoveInvalidFileNameChars(fileName);

                    Name = Path.GetFileName(fileName);
                    Loading = false;

                    //start downloading
                    startDownloading?.Invoke();
                    string destinationPath = Path.Combine(directoryDestinationPath, fileName);
                    FilePath = destinationPath;
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        var totalBytes = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : 0L;
                        var totalReadBytes = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        var startTime = DateTime.Now;

                        while (isMoreToRead)
                        {
                            var readBytes = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            if (readBytes == 0)
                            {
                                isMoreToRead = false;
                                continue;
                            }

                            //writea asydnc
                            await fileStream.WriteAsync(buffer, 0, readBytes);
                            totalReadBytes += readBytes;

                            var currentTime = DateTime.Now;
                            var timeSpan = currentTime - startTime;

                            Progress = ((float)totalReadBytes / totalBytes * 100);
                            CurrentBytes = totalReadBytes;
                            TotalGB = totalBytes;
                            Velocity = totalReadBytes / timeSpan.TotalSeconds;  // bytes per second

                      
                            // Estimate ETA
                            if (Velocity > 0)
                            { // Calculate remaining bytes
                                var remainingBytes = totalBytes - totalReadBytes;

                                var secondsRemaining = remainingBytes / Velocity;
                                ETA = TimeSpan.FromSeconds(secondsRemaining);
                            }

                            Description = $"{Velocity.ByteToReadableSize()}/{TotalGB.ByteToReadableSize()} ⚬ {ETA.Minutes}";
                            downloading?.Invoke();

                        }

                     
                    }
                }
            }


            Completed = true;
            finalize?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            // Handle download error
            Debug.WriteLine("___________ DOWNLOAD ERROR ___________");
            Debug.WriteLine(ex);
            Error = true;
            finalize?.Invoke();
            return false;
        }
    }




    Action finalize;
    Action startDownloading;
    Action downloading;
    public void OnFinalize(Action action)
    {
        finalize = action;
    }
    public void OnStartDownloading(Action action)
    {
        startDownloading = action;
    }
    public void OnDownloading(Action action)
    {
        downloading = action;
    }

    public async Task StartClone(string sourceUrl, string destinationDir)
    {
        Loading = false;
        FilePath = destinationDir;

        var completed = await WebManager.GitClone(sourceUrl, destinationDir, GitSteps);

         void GitSteps(string path, int step, int totalSteps)
        {
            Progress = (step / totalSteps) * 100;
            Description = $"{step}/{totalSteps} {path}";
        }

        if (completed)
            Completed = true;
        else
            Error = true;
    }

}