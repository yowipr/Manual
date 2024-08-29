using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Manual.Editors;
using Manual.MUI;
using Manual.Objects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Windows.Media.Imaging.WriteableBitmapExtensions;
using static Manual.API.ManualAPI;
using MS.WindowsAPICodePack.Internal;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using Manual.API;
using Newtonsoft.Json;
using System.DirectoryServices.ActiveDirectory;
using FFmpeg.AutoGen;
using ManualToolkit.Generic;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Transactions;
using System.Numerics;

using PointF = System.Drawing.PointF;
using SkiaSharp;
using System.Collections.Concurrent;
using CefSharp;

using FFmpeg.AutoGen;
using System.Timers;
using CefSharp.DevTools.Page;
using Manual.Core.Graphics.Audio;



namespace Manual.Core;

public partial class AnimationManager : ObservableObject
{
    public AnimationManager()
    {
            
    }

    public delegate void AnimationEvent(int currentFrame);
    public static event AnimationEvent OnFrameChanged;

    //--------------- TIMELINE -------------\\

    [ObservableProperty] bool _autoKeyframe = false;
    [ObservableProperty] [property: JsonIgnore] bool _isPlaying = false;
    [ObservableProperty] int _frameRate = 24;
    [ObservableProperty] int _playbackSpeed = 1;

    [NotifyPropertyChangedFor(nameof(FrameDuration))]
    [ObservableProperty] int _frameStart = 0;
    [NotifyPropertyChangedFor(nameof(FrameDuration))]
    [ObservableProperty] int _frameEnd = 48;


    [JsonIgnore] public bool isPropertyChanged = true;
    private int _currentFrame = 0;
    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            if (isPropertyChanged)
            {
                if (_currentFrame != value)
                {
                    _currentFrame = value;
                    OnCurrentFrameChanged(value);
                    OnPropertyChanged(nameof(CurrentFrame));
                    OnPropertyChanged(nameof(CurrentTime));
                }

               // SetProperty(ref _currentFrame, value, nameof(CurrentFrame));    
            }
            else if (_currentFrame != value)
            {
                _currentFrame = value;
                OnCurrentFrameChanged(value);         
            }
        }
    }

    void CurrentFrameModified()
    {
      //  CurrentFrame++;
      //  CurrentFrame--;

     //   OnCurrentFrameChanged(CurrentFrame);
      //  OnPropertyChanged(nameof(CurrentFrame));
      //  OnPropertyChanged(nameof(CurrentTime));
    }

    public TimeSpan Duration
    {
        get => TimeSpan.FromSeconds(FrameDuration / (double)FrameRate);
        set
        {
            int totalFrames = (int)Math.Round(value.TotalSeconds * FrameRate);
            FrameEnd = FrameStart + totalFrames;
        }
    }


    [ObservableProperty] int _frameSteps = 1;

    [JsonIgnore] public int FrameDuration { get { return FrameEnd - FrameStart; } }
    [JsonIgnore] public TimeSpan CurrentTime => GetTimeAt(CurrentFrame);

    public TimeSpan GetTimeAt(int frame) => TimeSpan.FromSeconds((double)frame / FrameRate);
    public int GetFrameAt(TimeSpan timeSpan)
    {
        double totalFrames = timeSpan.TotalSeconds * FrameRate;
        return (int)Math.Round(totalFrames);
    }

    partial void OnFrameStartChanged(int value)
    {
        UpdateUI();
    }
    partial void OnFrameEndChanged(int value)
    {
        UpdateUI();
    }

    /// <summary>
    /// update timeline ui graph only
    /// </summary>
    public void UpdateUI()
    {
        // foreach (LayerBase layer in layers)
        // {
            var layer = SelectedLayer;

        if (layer != null)
        {
            foreach (TimedVariable timedVariable in layer._Animation.TimedVariables)
            {
                timedVariable.UpdateGraph();
            }
        }
        // }



    }
    public List<TimedVariable> GetSelectedTimedVariables()
    {
        // Filtrar los elementos TimedVariable que tengan la propiedad IsSelected en true
        var selectedTimedVariables = SelectedLayer._Animation.TimedVariables
            .Where(tv => tv.IsSelected)
            .ToList();

        return selectedTimedVariables;
    }


    public void InsertKeyframeConditioned(IAnimable target, string propertyName)
    {
        Type t = target.GetType();
        // DISCRIMINATOR
        var keyframe = Keyframe.CreateKeyframe(target, propertyName, CurrentFrame, 6);
        if (target is LayerBase)
        {
            if (propertyName == "Position")
            {
                Keyframe.Insert(target, $"{propertyName}X");
                Keyframe.Insert(target, $"{propertyName}Y");
            }
            else if (propertyName == "Scale")
            {
                Keyframe.Insert(target, "Width");
                Keyframe.Insert(target, "Height");
            }
            else if (propertyName == "Image")
            {
              //  if(target._Animation.GetTimedVariable("Image") is TimedVariable tv && tv.HasKeyframe(CurrentFrame))
             //       Keyframe.Insert(target, propertyName);    
            //    else
                    Keyframe.Insert(target, propertyName);
            }
            else
            {
                Keyframe.Insert(target, propertyName);
            }
        }
        else
        {
            Keyframe.Insert(target, propertyName);
        }
    }


    [JsonIgnore]
    public TimeSpan Time
    {
        get
        {
            return TimeSpan.FromSeconds((double)CurrentFrame / (FrameRate * PlaybackSpeed));
        }
    }


 

    public AnimationManager(Shot shot)
    {
        //SelectedKeyframes.CollectionChanged += SelectedKeyframes_CollectionChanged;
        AttachedShot = shot;
    }


    // ---------------------- DISPACHER ---------------------- \\

    private Timer animationTimer;
    private Stopwatch stopwatch = new Stopwatch(); // Usar System.Diagnostics

    //--------------------------------------------------------------------------------------------------------------------------------------- PLAY
    [RelayCommand]
    [property: JsonIgnore]
    public void Play()
    {
        if (IsPlaying)
        {
            Pause();
            return;
        }
        stopwatch.Start();

        //if(animationTimer != null)
        //    animationTimer.Tick -= Timer_Tick;    

        //animationTimer = new();
        //double intervalSeconds = 1.0 / (FrameRate * PlaybackSpeed);
        //animationTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
        //animationTimer.Tick += Timer_Animation;
        //IsPlaying = true;
        //animationTimer.Start();

        finishedLast = true;

        RealCurrentFrame = CurrentFrame;
        animationTimer = new Timer();
        double intervalMilliseconds = 1000.0 / (FrameRate * PlaybackSpeed);
        animationTimer.Interval = intervalMilliseconds;
        animationTimer.Elapsed += Timer_Animation;
        IsPlaying = true;
        animationTimer.Start();

    }

    public int RealCurrentFrame = 1;
    private void Timer_Animation(object? sender, ElapsedEventArgs e)
    {
        //TODO: hacer luego

        //if (Settings.instance.EnablePreviewCacheFrames)
        //{
        //    SetValue(RealCurrentFrame);
        //    RealCurrentFrame++;
        //    return;
        //}

        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        stopwatch.Restart();

        RealCurrentFrame++;

        int newFrame;
        if (AppModel.settings.FrameDrop && FrameBuffer.ContainsKey(CurrentFrame + 1))
        { 
            if (CurrentFrame >= FrameEnd)
            {
                newFrame = FrameStart;      
                RealCurrentFrame = FrameStart;
            }
            else
                newFrame = RealCurrentFrame;
        }
        else
        {
            if (CurrentFrame >= FrameEnd)
            {
                newFrame = FrameStart;
                RealCurrentFrame = FrameStart;
            }
            else
                newFrame = CurrentFrame + 1;
        }


        Application.Current.Dispatcher.BeginInvoke( () =>
        {
            if (_awaitingRender)
                return;

            //wait for updating
            _awaitingRender = true;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            isPropertyChanged = true;
            CurrentFrame = newFrame;
        });
    }

    private bool _awaitingRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitingRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;

    }


    public void NotifyActionStartChanging(IAnimable target, string propertyName)
    {
        var anim = target._Animation;
        if (anim == null)
        {
            RemoveFrameBuffers();
            return;
        }
        NotifyActionStartChanging(target, propertyName, anim.CurrentFrame);
    }
    public void NotifyActionStartChanging(IAnimable target, string propertyName, int frame)
    {
        var anim = target._Animation;
        if (anim == null)
        {
            RemoveFrameBuffers();
            return;
        }

        int start = anim.GlobalFrameStart;
        int end = anim.GlobalFrameEnd;

        if (propertyName == "Image" && anim.GetTimedVariable(propertyName) is TimedVariable timed)
        {
            var k1 = timed.GetKeyframeNext(frame);

            start = frame;
            if (k1 != null) end = k1.Frame;
        }
        else if (propertyName == "Position")
        {
            var result = SetTouples(anim, propertyName, "PositionX", "PositionY");
            start = result.start;
            end = result.end;
        }
        else if (propertyName == "Scale")
        {
            var result = SetTouples(anim, propertyName, "Width", "Height");
            start = result.start;
            end = result.end;
        }
        else if (anim.GetTimedVariable(propertyName) is TimedVariable timed2)
        {
            var k0 = timed2.GetKeyframePrevious(frame);
            var k1 = timed2.GetKeyframeNext(frame);

            if (k0 != null) start = k0.Frame;
            if (k1 != null) end = k1.Frame;
        }


        var framesToRemove = Enumerable.Range(start, end - start + 1);
        RemoveFrameBuffers(framesToRemove);


        CurrentFrameModified();
    }
    (int start, int end) SetTouples(AnimationBehaviour anim, string propertyName, string timed1, string timed2)
    {
        TimedVariable? timedX = anim.GetTimedVariable(timed1);
        TimedVariable? timedY = anim.GetTimedVariable(timed2);

        Keyframe? k0X = timedX?.GetKeyframePrevious(anim.CurrentFrame);
        Keyframe? k0Y = timedY?.GetKeyframePrevious(anim.CurrentFrame);
        Keyframe? k1X = timedX?.GetKeyframeNext(anim.CurrentFrame);
        Keyframe? k1Y = timedY?.GetKeyframeNext(anim.CurrentFrame);

        int startX = k0X?.Frame ?? anim.GlobalFrameStart;
        int startY = k0Y?.Frame ?? anim.GlobalFrameStart;
        int endX = k1X?.Frame ?? anim.GlobalFrameEnd;
        int endY = k1Y?.Frame ?? anim.GlobalFrameEnd;

        var start = Math.Min(startX, startY);
        var end = Math.Max(endX, endY);

        return (start, end);
    }


    public void NotifyActionChanged(IAnimable target, string propertyName)
    {
        if (AutoKeyframe)
        {
            InsertKeyframeConditioned(target, propertyName);
        }

      //  RenderBufferNext(CurrentFrame);
    }


    //------------------------------------------------------------------------------------------------------------ BUFFERING
    [JsonIgnore] public ConcurrentDictionary<int, SKBitmap> FrameBuffer = new();
    public event Event<int> OnFrameBuffering;


    bool finishedLast = true;
    void SetValue(int currentFrame)
    {
        if (currentFrame <= FrameStart)
            return;      
        else if (currentFrame >= FrameEnd - 10)
            return;


        int realFrame = currentFrame;
        var realFrameEnd = Math.Min(realFrame + 10, FrameEnd - 1);

        // Iniciar la carga de los próximos 24 frames en segundo plano
        if (finishedLast)
        {
            finishedLast = false;
            Task.Factory.StartNew(() => PreloadFrames(realFrame, realFrameEnd));
        }

        CurrentFrame = currentFrame;

    }
    async Task PreloadFrames(int frameStart, int frameEnd)
    {
        for (int i = frameStart; i <= frameEnd; i++)
        {
            var bitmap = await LoadFrame(i);
            if (bitmap != null)
            {
                FrameBuffer.GetOrAdd(i, bitmap);
            }

        }
        isPropertyChanged = false;

        CurrentFrame = frameStart;
        isPropertyChanged = true;
    }
    async Task<SKBitmap?> LoadFrame(int frame)
    {
        return await Task.Run(() =>
        {
            if (!FrameBuffer.ContainsKey(frame))
            {
                var bitmap = ManualCodec.RenderFrame(frame, AttachedShot);
                return bitmap;
            }
            return null;
        });

    }
    SKBitmap? GetFrameBuffer(int frame)
    {
        return FrameBuffer.GetOrAdd(frame, key =>
        {
            var bitmap = ManualCodec.RenderFrame(frame, AttachedShot);
            return bitmap ?? new SKBitmap();
        });
    }






    [JsonIgnore] public bool IsCurrentFrameABuffer = false;
    private int bufferMaxSize = 48;
    public void RenderBufferFrames()
    {
        Task.Run(() => RenderFrames());
    }

    public void RenderBufferNext(int intFrame)
    {
        if (!Settings.instance.EnablePreviewCacheFrames || Project.IsLoading) return;

        var frame = ManualCodec.RenderFrame(intFrame, AttachedShot);
        FrameBuffer.TryAdd(intFrame, frame);
        OnFrameBuffering?.Invoke(intFrame);

    }


    //---------------------------------------------------------------- REMOVE BUFFERS
    public void RemoveFrameBuffer() => RemoveFrameBuffer(CurrentFrame);
    public void RemoveFrameBuffer(int intFrame)
    {
        if (!Settings.instance.EnablePreviewCacheFrames) return;

        FrameBuffer.TryRemove(intFrame, out _);

        OnFrameBuffering?.Invoke(intFrame);
    }
    public void RemoveTrackBuffer(IAnimable target)
    {
        var anim = target._Animation;

        int start = anim.GlobalFrameStart;
        int end = anim.GlobalFrameEnd;

        var framesToRemove = Enumerable.Range(start, end - start + 1);
        RemoveFrameBuffers(framesToRemove);
    }

    public void RemoveFrameBuffers()
    {
        FrameBuffer.Clear();
        OnFrameBuffering?.Invoke(0);
    }
    public void RemoveFrameBuffers(IEnumerable<int> intFrames)
    {
        if (!Settings.instance.EnablePreviewCacheFrames) return;


        foreach (var intFrame in intFrames)
            FrameBuffer.TryRemove(intFrame, out _);

        if (intFrames.Any())
            OnFrameBuffering?.Invoke(intFrames.First());
    }




    private void RenderFrames()
    {
        IsCurrentFrameABuffer = false;
        var sw = new Stopwatch();
        sw.Start();

        FrameBuffer.Clear();
        var intFrame = CurrentFrame;
        var startFrame = intFrame;
        
        while (FrameBuffer.Count < bufferMaxSize)
        {
            if (intFrame > FrameEnd)
                break;

            RenderBufferNext(intFrame);

            intFrame++;
        }
        //Parallel.For(startFrame, bufferMaxSize, i =>
        //{
        //    var frame = ManualCodec.RenderFrame(intFrame + i);
        //    FrameBuffer.TryAdd(intFrame + i, frame);
        //});

        sw.Stop();
        Output.Log($"rendered {FrameBuffer.Count} frames in {sw.Elapsed} s");
    }

    public SKBitmap GetBufferFrame(int frame)
    {
        if (!Settings.instance.EnablePreviewCacheFrames)
        {
            return ManualCodec.RenderFrame(frame, AttachedShot);
        }
        

        if (!FrameBuffer.TryGetValue(frame, out var currentBitmap))
        {
            return null;
        }

        //else

        return currentBitmap;
    }


    public void ClearCache()
    {
        RemoveFrameBuffers();

        Shot.UpdateCurrentRender();
    }

    public void Pause()
    {
        IsPlaying = false;
        if (animationTimer is not null)
        {
            animationTimer.Stop();
            animationTimer.Elapsed -= Timer_Animation;
        }
        stopwatch.Stop();
        _awaitingRender = false;
    }
    public void Stop()
    {
        Pause();
    }

    // --------------------------------------------------------------------------------------------- FRAME 2 FRAME
    [JsonIgnore] public Shot AttachedShot { get; set; }
    void OnCurrentFrameChanged(int newValue)
    {
        if (AttachedShot == null)
            return;


         OnFrameChanged?.Invoke(newValue);

        IsCurrentFrameABuffer = FrameBuffer.ContainsKey(newValue);
        foreach (LayerBase layer in AttachedShot.layers)
        {
            layer._Animation.CurrentFrame = newValue - layer._Animation.StartOffset;
        }

       //BUFFER
       if (!IsCurrentFrameABuffer)
          RenderBufferNext(newValue);

        AttachedShot.UpdateRender();

    }




    [RelayCommand]
    [property: JsonIgnore]
    public void FrameNext()
    {
        CurrentFrame++;
    }
    [RelayCommand]
    [property: JsonIgnore]
    public void FramePrevious()
    {
        CurrentFrame--;
    }



    [JsonIgnore] public SelectionCollection<Keyframe> SelectedKeyframes { get; set; } = new();

    IAnimable SelectedAnimable => SelectedLayer;
    public void SelectAllKeyframes()
    {
        SelectedKeyframe = null;
        SelectedKeyframes.Clear();
    
        foreach (var timedVariable in SelectedAnimable._Animation.TimedVariables)
        {
           // SelectedKeyframe = timedVariable.Keyframes[0]; //TODO: mejorar
           // SelectedKeyframe.IsSelected = true;
            timedVariable.IsSelected = true;
            foreach (Keyframe keyframe in timedVariable.Keyframes)
            {      
                SelectedKeyframes.Add(keyframe);
            }
        }
    }
    public void ClearSelectedKeyframes()
    {
        SelectedKeyframe = null;
        SelectedKeyframes.Clear();
    }
    public void DeleteSelectedKeyframes()
    {
        var selected = SelectedKeyframes.ToList();

         foreach (var keyframe in selected)
         {
             keyframe.Delete();
        }
        SelectedKeyframes.Delete();
    }

    public void DuplicateSelectedKeyframes()
    {
        var cloned = SelectedKeyframes.ToList();
        SelectedKeyframes.Clear();

        foreach (var key in cloned)
        {
            var keyclone = ManualClipboard.Clone(key);
            key.AttachedTimedVariable.AddKeyframe(keyclone);
            SelectedKeyframes.Add(keyclone);
        }
    }


    Keyframe selectedKeyframe;

    [JsonIgnore]
    public Keyframe SelectedKeyframe
    {
        get => selectedKeyframe;
        set
        {
            if (selectedKeyframe != value)
            {
                OnSelectedKeyframeChangingOld(selectedKeyframe);

                selectedKeyframe = value;
                SelectedKeyframes.SelectedItem = value;

                OnSelectedKeyframeChanged(value);
                OnPropertyChanged(nameof(SelectedKeyframe));
            }
        }
    }

    void OnSelectedKeyframeChangingOld(Keyframe oldValue)
    {
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }
    }

    void OnSelectedKeyframeChanged(Keyframe newValue)
    {
        if (newValue != null)
        {
            newValue.IsSelected = true;
        }
    }



    [RelayCommand]
    [property: JsonIgnore]
    public void KeyframeNext()
    {
        if (GetKeyframeNext() is Keyframe frame)
        {
            // SelectedLayer._Animation.CurrentFrame = frame.Frame;
            CurrentFrame = frame.AttachedTimedVariable.CurrentTarget._Animation.GetActualFrame(frame.Frame);
        }
    }
    public Keyframe? GetKeyframeNext(Keyframe keyRef)
    {
        return GetKeyframeNext(keyRef.Frame);
    }
    public Keyframe? GetKeyframeNext()
    {
        return GetKeyframeNext(SelectedLayer._Animation.CurrentFrame);
    }
    public Keyframe? GetKeyframeNext(int currentFrame)
    {
        var timedVariables = SelectedLayer._Animation.TimedVariables;
        List<Keyframe> closestKeyframes = new List<Keyframe>();

        foreach (var timedVariable in timedVariables)
        {
            Keyframe closestKeyframe = null;
            int minDifference = int.MaxValue;

            foreach (var keyframe in timedVariable.Keyframes)
            {
                if (keyframe.Frame > currentFrame)
                {
                    int difference = keyframe.Frame - currentFrame;

                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closestKeyframe = keyframe;
                    }
                }
            }

            closestKeyframes.Add(closestKeyframe);
        }

        // Buscar el Keyframe más cercano entre los Keyframes más cercanos de cada timedVariable
        Keyframe closestOverallKeyframe = null;
        int minOverallDifference = int.MaxValue;

        foreach (var keyframe in closestKeyframes)
        {
            if (keyframe != null)
            {
                int difference = keyframe.Frame - currentFrame;

                if (difference < minOverallDifference)
                {
                    minOverallDifference = difference;
                    closestOverallKeyframe = keyframe;
                }
            }
        }

        return closestOverallKeyframe;
    }

    [RelayCommand]
    [property: JsonIgnore]
    public void KeyframePrevious()
    {
        if (GetKeyframePrevious() is Keyframe frame)
        {
            // SelectedLayer._Animation.CurrentFrame = frame.Frame;
            CurrentFrame = frame.AttachedTimedVariable.CurrentTarget._Animation.GetActualFrame(frame.Frame);
        }
    }

    public Keyframe? GetKeyframePrevious(Keyframe keyRef)
    {
        return GetKeyframePrevious(keyRef.Frame);
    }
    public Keyframe? GetKeyframePrevious()
    {
        return GetKeyframePrevious(SelectedLayer._Animation.CurrentFrame);
    }
    public Keyframe? GetKeyframePrevious(int currentFrame)
    {
        var timedVariables = SelectedLayer._Animation.TimedVariables;
        List<Keyframe> closestKeyframes = new List<Keyframe>();


        foreach (var timedVariable in timedVariables)
        {      
            Keyframe closestKeyframe = null;
            int minDifference = int.MaxValue;

            foreach (var keyframe in timedVariable.Keyframes)
            {
                if (keyframe.Frame < currentFrame)
                {
                    int difference = currentFrame - keyframe.Frame;

                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closestKeyframe = keyframe;
                    }
                }
            }

            closestKeyframes.Add(closestKeyframe);
        }

        // Buscar el Keyframe más cercano entre los Keyframes más cercanos de cada timedVariable
        Keyframe closestOverallKeyframe = null;
        int minOverallDifference = int.MaxValue;

        foreach (var keyframe in closestKeyframes)
        {
            if (keyframe != null)
            {
                int difference = currentFrame - keyframe.Frame;

                if (difference < minOverallDifference)
                {
                    minOverallDifference = difference;
                    closestOverallKeyframe = keyframe;
                }
            }
        }

       return closestOverallKeyframe;      
    }


    public TimedVariable GetTimedVariable(IAnimable target, string propertyName)
    {
        return target._Animation.TimedVariables.FirstOrDefault(tv => tv.PropertyName == propertyName);
    }

    public Keyframe GetKeyframe(IAnimable target, string propertyName, int frame)
    {
        return GetTimedVariable(target, propertyName).GetConstantKeyframeAt(frame);
    }




    //--------------------------------------------- KEYFRAME TYPES
     public static ObservableCollection<ColorTag> RegisteredKeyframeTypes = new()
    {
        new ColorTag("Keyframe", null),
        new ColorTag("Main Key", Colors.Red, 1.2f),
        new ColorTag("Inbetween", Colors.Yellow, 0.8f),

        new ColorTag("Reference", Colors.Purple, 1.2f),
        new ColorTag("Bake Keyframe", Colors.Gray, 0.8f),
    };



    public void ChangeSelectedKeyframesType(ColorTag type)
    {
        SelectedKeyframes.ForEach(k => k.Type = type);
    }
    public void ChangeSelectedKeyframesType(string type)
    {
        var matchingType = AnimationManager.GetKeyframeType(type);
        ChangeSelectedKeyframesType(matchingType);
    }

    public static ColorTag GetKeyframeType(string type)
    {
        return AnimationManager.RegisteredKeyframeTypes.FirstOrDefault(ct => ct.Name == type);
    }


    public static List<LayerBase> GetLayersActiveAtFrame(int frame)
    {
        // Verifica que ManualAPI y ManualAPI.layers estén inicializados adecuadamente
        if (ManualAPI.layers == null)
        {
            return new List<LayerBase>(); // Retorna una lista vacía si no hay layers
        }

        // Filtra los layers que están activos en el frame especificado
        var activeLayersAtFrame = ManualAPI.layers
            .Where(layer => layer._Animation.GlobalFrameStart <= frame && layer._Animation.GlobalFrameEnd >= frame)
            .ToList();

        return activeLayersAtFrame;
    }


    internal static void DuplicateTrack(LayerBase track, bool isSelected = false)
    {
        if (track._Animation.GetTimedVariable("Image") is TimedVariable tvImg)
            tvImg.Linked = true;

        var track2 = track.Clone();
        track2._Animation.AttachedLayer = track2;

        AddLayerBase(track2, isSelected);

        track2._Animation.StartOffset = track._Animation.StartOffset;
    }

    internal static void Knife(SelectionCollection<LayerBase> selectedLayers, int frame)
    {
        var layers = GetLayersActiveAtFrame(frame);
        if (selectedLayers.Any())
        {
            foreach (var track in selectedLayers)
                Knife(track, frame);   
        }
        else
        {
            foreach (var track in layers)
                Knife(track, frame);
        }
    }
    public static void Knife(LayerBase track, int frame)
    {
        if (track._Animation.GetTimedVariable("Image") is TimedVariable tvImg)
            tvImg.Linked = true;

        var track2 = track.Clone();

        track2._Animation.AttachedLayer = track2;

        var aframe = track._Animation.GetActualFrame(frame);
        track._Animation.FrameEnd = aframe - 1;
        track2._Animation.FrameStart = aframe;

        AddLayerBase(track2, false);
        track2._Animation.TrackIndex = track._Animation.TrackIndex;
        track2._Animation.StartOffset -= aframe;
    }
    internal static void Knife(int frame) => Knife(SelectedLayers, frame);   
    internal static void Knife() => Knife(SelectedLayers, Animation.CurrentFrame);


    internal List<AnimationBehaviour> GetAllTracks()
    {
        List<AnimationBehaviour> tracks = new();
        foreach (var layer in SelectedShot.layers)
        {
            tracks.Add(layer._Animation);
        }
        return tracks;
    }
}

public static class KeyframeTypes
{
    public const string Keyframe = "Keyframe";
    public const string MainKey = "Main Key";
    public const string Inbetween = "Inbetween";

    public const string Reference = "Reference";
    public const string BakeKeyframe = "Bake Keyframe";
}









//------------------------------------------------------------------------------------------------------------------------------------------------------KEYFRAME
public partial class Keyframe : ObservableObject, ISelectable, IBloqueable
{
    [ObservableProperty] bool block;

    [ObservableProperty] int frame;
    [ObservableProperty][property: JsonIgnore] object value;


    [ObservableProperty][property: JsonIgnore] ColorTag type = AnimationManager.GetKeyframeType("Keyframe");

    internal PropertyInfo propertyInfo { get; set; }
    [ObservableProperty] TemporalInterpolation interpolation = TemporalInterpolation.Bezier;
    partial void OnInterpolationChanged(TemporalInterpolation value)
    {
        UpdatePosition();
    }



    [ObservableProperty][property: JsonIgnore] bool isSelected;
    [ObservableProperty][property: JsonIgnore] bool isBakeKeyframe;
    [ObservableProperty][property: JsonIgnore] bool isBaking;
    partial void OnIsSelectedChanged(bool value)
    {
        bool allUnselected = AttachedTimedVariable.Keyframes.All(keyframe => !keyframe.IsSelected);
        AttachedTimedVariable.IsSelected = !allUnselected;
    }




    //-------------------------------------------------------------- PREVIEW
    [JsonIgnore] public PreviewableValue<object> PreviewValue { get; private set; }
    void InitPreviewValue()
    {
      PreviewValue =
      new(
          get: () => Value,
          set: (value) =>  Value = value
      );

    }

    public Keyframe()
    {
        InitPreviewValue();
    }

    public Keyframe(IAnimable obj, string propertyName, int frame) // main
    {
        propertyInfo = obj.GetType().GetProperty(propertyName);

        if (propertyInfo.PropertyType == typeof(SKBitmap))
        {
            Value = ((SKBitmap)propertyInfo.GetValue(obj)).Copy();
        }
        else
            Value = propertyInfo.GetValue(obj);

        Frame = frame;

        if (IsValueType(typeof(SKBitmap)))
        {
            interpolation = TemporalInterpolation.Constant;
        }

        InitPreviewValue();
    }
    public Keyframe(IAnimable obj, string propertyName, int frame, object customValue)
    {
        propertyInfo = obj.GetType().GetProperty(propertyName);
        Value = customValue;
        Frame = frame;

        if (IsValueType(typeof(SKBitmap)))
        {
            interpolation = TemporalInterpolation.Constant;
        }

        InitPreviewValue();
    }

    public object GetProperty()
    {
        EnsurePropertyInfo();
        return propertyInfo.GetValue(AttachedTimedVariable.CurrentTarget);
    }
 
    public void ApplyTo(object Target) //TODO: if target doesn't have the propertyinfo property this throw an exception
    {
        propertyInfo.SetValue(Target, Value);
    }
    public void ApplyTo(object Target, object customValue)
    {
        EnsurePropertyInfo();
        if(propertyInfo.GetValue(Target) != customValue)
            propertyInfo.SetValue(Target, customValue); // asign property to Target, with value Value
    }

    void EnsurePropertyInfo()
    {
        propertyInfo ??= AttachedTimedVariable.CurrentTarget.GetType().GetProperty(AttachedTimedVariable.PropertyName);
    }

    public static Keyframe Insert(IAnimable obj, string propertyName)
    {
        return Insert(obj, propertyName, SelectedLayer._Animation.CurrentFrame);
    }
    public static Keyframe Insert(IAnimable obj, string propertyName, int frame)
    {
        Keyframe keyframe = new(obj, propertyName, frame);
        return Insert(obj, propertyName, frame, keyframe);
    }
    public static Keyframe Insert(IAnimable obj, string propertyName, int frame, object customValue)
    {
        Keyframe keyframe = new(obj, propertyName, frame, customValue);
        return Insert(obj, propertyName, frame, keyframe);
    }


    public static Keyframe Insert(IAnimable obj, string propertyName, int frame, Keyframe keyframe) ////--------------- MAIN INSERT KEYFRAME
    {    

        TimedVariable timedVariable = obj._Animation.TimedVariables.FirstOrDefault(tv => tv.PropertyName == propertyName);
        Keyframe result;
        if (timedVariable != null) //si se encuentra, añadir el keyframe a Keyframes
        {
            timedVariable.CurrentTarget = obj;

            Keyframe sameKey = timedVariable.Keyframes.FirstOrDefault(k => k.Frame == frame);
            if (sameKey == null)
            {
               timedVariable.AddKeyframe(keyframe);
                result =  keyframe;
            }
            else
            {
                sameKey.Value = keyframe.Value;
                result = sameKey;
            }
        }
        else // si no se encuentra, crear la TimedVariable y asignar el keyframe
        {
            timedVariable = new TimedVariable(propertyName, obj);

            timedVariable.AddKeyframe(keyframe);
            obj._Animation.TimedVariables.Add(timedVariable);

           result = keyframe;
        }
        

        //handlers
        
        var keyIndex = timedVariable.Keyframes.IndexOf(result);

        var k0 = timedVariable.Keyframes.ElementAtOrDefault(keyIndex - 1);
        var k1 = result;
        var k2 = timedVariable.Keyframes.ElementAtOrDefault(keyIndex + 1);

        if(k0 != null)
        {
            var hdist0 = (k1.Frame - k0.Frame) / 3;

            k0.RightHandleFrame = hdist0;
            k1.LeftHandleFrame = -hdist0;
        }
        if (k2 != null)
        {
            var hdist1 = (k2.Frame - k1.Frame) / 3;

            k1.RightHandleFrame = hdist1;
            k2.LeftHandleFrame = -hdist1;
        }


        if (!Animation.IsPlaying)
            result.AttachedTimedVariable.SetCurrentValue();

        return result;
    }

    public static Keyframe Insert(IAnimable obj, Keyframe keyframe)
    {
        return Insert(obj, keyframe.PropertyName, keyframe.Frame, keyframe);
    }



    public static Keyframe InsertBake(TimedVariable TimedImage, int frame)
    {
        var k = new Keyframe(TimedImage.CurrentTarget, "Image", frame, null);
        k.Type = AnimationManager.GetKeyframeType(KeyframeTypes.BakeKeyframe);
        k.Interpolation = TemporalInterpolation.Constant;

        TimedImage.AddKeyframe(k, false);
        return k;
    }


    public Type GetValueType()
    {
        return AttachedTimedVariable.ValueType;
    }
    public void Set(object value)
    {
        UpdatePosition(Frame, value);
    }
    public void Set(int frame, object value)
    {
        UpdatePosition(frame, value);
    }
    public void Set(PixelPoint frameValue)
    {
        UpdatePosition(frameValue.X, frameValue.Y);
    }
    private void UpdatePosition(int frame, object value)
    {
        Frame = frame;
        var t = GetValueType();
        if (t == typeof(SKBitmap) || Value == null)
            return;

        if (Value.GetType() == value.GetType())
            Value = value;
        else if (IsValueNumberType())
        {
            var r = Convert.ChangeType(value, Value.GetType());
            if (r != null && value != null)
                Value = r;
        }
      


        var keyframes = AttachedTimedVariable.Keyframes;

        int currentIndex = keyframes.IndexOf(this);
        Keyframe previousKeyframe = currentIndex > 0 ? keyframes[currentIndex - 1] : null;
        Keyframe nextKeyframe = currentIndex < keyframes.Count - 1 ? keyframes[currentIndex + 1] : null;

     
        // Mueve los keyframes vecinos si es necesario
        if (previousKeyframe != null && previousKeyframe.Frame > Frame)
        {
            keyframes.Move(currentIndex, currentIndex - 1);
        }

        if (nextKeyframe != null && nextKeyframe.Frame < Frame)
        {
            keyframes.Move(currentIndex, currentIndex + 1);
        }

     
    }

    public override string ToString()
    {
        return $"Frame: {Frame}, Value: {Value}, Property: {AttachedTimedVariable?.PropertyName}";
    }
    public PixelPoint GetPosition()
    {
        int result;

        if (Value is float || Value is double || Value is int || Value is decimal)
            result = Convert.ToInt32(Value);
        else
            result = 0;
   

        return new PixelPoint(Frame, result);
    }

    public Point ToPoint()
    {
        double result;

        if (Value is float || Value is double || Value is int || Value is decimal)
            result = Convert.ToDouble(Value);
        else
            result = 0;

        return new Point(Frame, result);
    }
    


    //------------- INTERPOLATION -------------\\

    // [ObservableProperty] private int rightHandleFrame = 4;

    [ObservableProperty] private int rightHandleValue = 0;

  //  [ObservableProperty] private int leftHandleFrame = -4;

    [ObservableProperty] private int leftHandleValue = 0;


    private int _rightHandleFrame = 1;
    public int RightHandleFrame
    {
        get => _rightHandleFrame;
        set
        {
            if(value < 0) value = 0;
            SetProperty(ref _rightHandleFrame, value);
            OnRightHandleFrameChanged(value);
        }
    }

    private int _leftHandleFrame = -1;
    public int LeftHandleFrame
    {
        get => _leftHandleFrame;
        set
        {
            if (value > 0) value = 0;
            SetProperty(ref _leftHandleFrame, value);
            OnLeftHandleFrameChanged(value);
        }
    }


     public event Event<object> ValueChanged;
     public event Event<int> FrameChanged;


    #region BOILERPLATE para update position

    partial void OnFrameChanged(int value)
    {
        UpdatePosition();
        FrameChanged?.Invoke(value);
    }

    partial void OnValueChanged(object value)
    {
        UpdatePosition();
        ValueChanged?.Invoke(value);
    }
  
    void OnRightHandleFrameChanged(int value)
    {
        UpdatePosition();
    }

    partial void OnRightHandleValueChanged(int value)
    {
        UpdatePosition();
    }

    void OnLeftHandleFrameChanged(int value)
    {
        UpdatePosition();
    }

    partial void OnLeftHandleValueChanged(int value)
    {
        UpdatePosition();
    }

    #endregion

    private void UpdatePosition()
    {
        if(UpdateUI && AttachedTimedVariable != null)
        AttachedTimedVariable.SetCurrentValue();
    }
    [JsonIgnore] public bool UpdateUI = true;
    public void InvalidateUI()
    {
        UpdateUI = true;
        UpdatePosition();
    }

    public Point RightHandle
    {
        get
        {
            return new Point(RightHandleFrame, RightHandleValue);
        }
        set
        {
            RightHandleFrame = (int)value.X;
            RightHandleValue = (int)value.Y;
        }
    }
    public Point LeftHandle
    {
        get
        {
            return new Point(LeftHandleFrame, LeftHandleValue);
        }
        set
        {
            LeftHandleFrame = (int)value.X;
            LeftHandleValue = (int)value.Y;
        }
    }
    public static object Interpolate(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame, TemporalInterpolation interpolation)
    {
        object interpolatedValue;
        switch (interpolation)
        {
            case TemporalInterpolation.Linear:
                interpolatedValue = InterpolationMethod.Linear(keyframe1, keyframe2, intermediateFrame);
                break;
            case TemporalInterpolation.Bezier:
                interpolatedValue = InterpolationMethod.Bezier(keyframe1, keyframe2, intermediateFrame);
                break;
            default:
                throw new ArgumentException("Invalid interpolation type");
        }
        return interpolatedValue;
    }



    public bool IsValueType(Type type)
    {
        return Value?.GetType() == type;
    }

    public bool IsValueNumberType()
    {
        return IsValueNumberType(Value);
    }
    public bool IsValueNumberType(object value)
    {
        if (value is int || value is double || value is float || value is decimal)
        {
            return true;
        }

        return false;
    }

    public string PropertyName { get; set; }
    // MANAGE EXTERIOR
    [ObservableProperty] [property: JsonIgnore] TimedVariable attachedTimedVariable;

    partial void OnAttachedTimedVariableChanged(TimedVariable value)
    {
        if (value != null)
            PropertyName = value.PropertyName;
        else
            PropertyName = null;
    }

    public void Delete()
    {
        if(SelectedKeyframe == this)
            SelectedKeyframe = null;

        AttachedTimedVariable.Keyframes.Remove(this);

        if (AttachedTimedVariable.Keyframes.Count == 0) // when delete all keyframes, remove timedvariable automatically
        {
            var layer = AttachedTimedVariable.CurrentTarget;
            layer._Animation.TimedVariables.Remove(AttachedTimedVariable);

            if(layer is LayerBase l && l.BakeKeyframes.Contains(this))
            {
                l.RemoveBakeKeyframe(this);  
            }
        }

        var timed = this.AttachedTimedVariable;
        Animation.NotifyActionStartChanging(timed.CurrentTarget, timed.PropertyName, this.Frame);

        UpdatePosition();

    }



    public static Keyframe CreateKeyframe<T>(IAnimable obj, string propertyName, int frame, T value)
    {
        if (typeof(T) == typeof(SKBitmap))
        {
            return new KeyframeImage(obj, propertyName, frame) { Value = value as SKBitmap };
        }
        else if (typeof(T) == typeof(double))
        {
            return new KeyframeDouble(obj, propertyName, frame) { Value = (double)(object)value };
        }
        else if (typeof(T) == typeof(int))
        {
            return new KeyframeInt(obj, propertyName, frame) { Value = (int)(object)value };
        }
        else
        {
            // Puedes lanzar una excepción o manejar otro comportamiento en caso de que el tipo no sea válido.
            throw new ArgumentException("Tipo de valor no compatible para crear un Keyframe.");
        }
    }

    public static List<Keyframe> Reorder(IEnumerable<Keyframe> keyframes)
    {
        return keyframes.OrderBy(kf => kf.Frame).ToList();
    }




    #region ----------------------------------------------------------------------- SERIALIZE

    [JsonProperty("Type")] string TypeId
    {
        get => Type.Name;
        set => Type = AnimationManager.GetKeyframeType(value);
    }



    Type _valueType;

    [JsonProperty]
    Type valueType
    {
        get => Value?.GetType();
        set => _valueType = value;
    }

    [JsonProperty] object ValueId
    {
        get
        {
            if (Value is SKBitmap bitmap)
                return bitmap.ToByte();
            else
                return Value;
        }
        set
        {
            try
            {

                if (_valueType == typeof(SKBitmap))
                {
                    if (value is byte[] b)
                        Value = b.ToSKBitmap();
                    else if (value is string base64)
                        Value = Convert.FromBase64String(base64).ToSKBitmap();
                }
                else if (value == null)
                    Value = null;
                else
                    Value = Convert.ChangeType(value, _valueType);

            }
            catch (InvalidCastException)
            {
                Output.Log("something went wrong serializing Value", this.ToString());
                Value = null;
            }

        }
    }




    #endregion -----------------------------------------------------------------------

}
//---------------------------------------------------------------------------------------------------------------------- KEYFRAME TYPES (DEPRECATED O NO SE USA)
public class KeyframeImage : Keyframe
{
    public KeyframeImage(IAnimable obj, string propertyName, int frame) : base(obj, propertyName, frame)
    {
    }
    public new SKBitmap Value { get; set; }
}

public class KeyframeDouble : Keyframe
{
    public KeyframeDouble(IAnimable obj, string propertyName, int frame) : base(obj, propertyName, frame)
    {
    }
    public new double Value { get; set; }
}
public class KeyframeInt : Keyframe
{
    public KeyframeInt(IAnimable obj, string propertyName, int frame) : base(obj, propertyName, frame)
    {
    }
    public new int Value { get; set; }
}





public enum TemporalInterpolation
{
    Linear,
    Bezier,
    Constant
}
public static class InterpolationMethod
{
    public static int Linear(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame)
    {
        int frame1 = keyframe1.Frame;
        int frame2 = keyframe2.Frame;
        int value1 = Convert.ToInt32(keyframe1.Value);
        int value2 = Convert.ToInt32(keyframe2.Value);

        // Comprobamos si el tiempo intermedio está fuera del rango de los keyframes
        if (intermediateFrame <= frame1)
            return value1;
        if (intermediateFrame >= frame2)
            return value2;

        // Calculamos la fracción del tiempo intermedio entre los keyframes
        double t = (double)(intermediateFrame - frame1) / (frame2 - frame1);

        // Interpolamos el valor utilizando la fórmula de interpolación lineal
        int interpolatedValue = (int)(value1 + (value2 - value1) * t);
        return interpolatedValue;
    }
    public static int Constant(Keyframe keyframe1)
    {
        return (int)keyframe1.Value;
    }
    public static float BezierJump(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame)
    {
        float t = (float)(intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        var value1 = Convert.ToSingle(keyframe1.Value);
        var value2 = Convert.ToSingle(keyframe2.Value);

        Point keyframe1Position = new Point(keyframe1.Frame, value1);
        Point keyframe2Position = new Point(keyframe2.Frame, value2);

        // Calcula las posiciones globales de los puntos de control
        Point globalRightHandle = new Point(keyframe1.Frame + keyframe1.RightHandle.X, value2 + keyframe1.RightHandle.Y);
        Point globalLeftHandle = new Point(keyframe2.Frame + keyframe2.LeftHandle.X, value2 + keyframe2.LeftHandle.Y);

        // Calcula los puntos intermedios de la curva de Bezier
        Point p0 = keyframe1Position;
        Point p1 = globalRightHandle;
        Point p2 = globalLeftHandle;
        Point p3 = keyframe2Position;

        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // Aplica la fórmula de Bezier para calcular las coordenadas interpoladas
        float interpolatedX = (float)(uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X);
        float interpolatedY = (float)(uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y);

        return interpolatedY;
    }

    public static int BezierOld(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame)
    {
        double t = (double)(intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        Point keyframe1Position = new Point(keyframe1.Frame, (int)keyframe1.Value);
        Point keyframe2Position = new Point(keyframe2.Frame, (int)keyframe2.Value);

        // Calcula las posiciones globales de los puntos de control
        Point globalRightHandle = PixelPoint.Add(keyframe1Position, keyframe1.RightHandle);
        Point globalLeftHandle = PixelPoint.Add(keyframe2Position, keyframe2.LeftHandle);

        // Calcula los puntos intermedios de la curva de Bezier
        Point p0 = keyframe1Position;
        Point p1 = globalRightHandle;
        Point p2 = globalLeftHandle;
        Point p3 = keyframe2Position;

        double u = 1 - t;
        double tt = t * t;
        double uu = u * u;
        double uuu = uu * u;
        double ttt = tt * t;

         // Aplica la fórmula de Bezier para calcular el valor interpolado
          double interpolatedValue = uuu * p0.Y; // Primer término de la fórmula
          interpolatedValue += 3 * uu * t * p1.Y; // Segundo término
          interpolatedValue += 3 * u * tt * p2.Y; // Tercer término
          interpolatedValue += ttt * p3.Y; // Cuarto término

      
        return (int)interpolatedValue;
    }

    public static int BezierOld3(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame)
    {
        double t = (double)(intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        int keyframe1X = keyframe1.Frame;
        int keyframe1Y = (int)keyframe1.Value;

        int keyframe2X = keyframe2.Frame;
        int keyframe2Y = (int)keyframe2.Value;

        var P1 = new PixelPoint(keyframe1X, keyframe1Y);
        var P2 = new PixelPoint(keyframe1.RightHandleFrame, (int)keyframe1.RightHandleValue);
        var P3 = new PixelPoint(keyframe2.LeftHandleFrame, (int)keyframe2.LeftHandleValue);
        var P4 = new PixelPoint(keyframe2X, keyframe2Y);


        var P5 = new PixelPoint(
            (1 - t) * P1.X + t * P2.X,
            (1 - t) * P1.Y + t * P2.Y
            );

        var P6 = new PixelPoint(
            (1 - t) * P2.X + t * P3.X ,
            (1 - t) * P2.Y + t * P3.Y
            );

        var P7 = new PixelPoint(
            (1 - t) * P3.X + t * P4.X,
            (1 - t) * P3.Y + t * P4.Y
            );

        var P8 = new PixelPoint(
            (1 - t) * P5.X + t * P6.X,
            (1 - t) * P5.Y + t * P6.Y
            );
        var P9 = new PixelPoint(
            (1 - t) * P6.X + t * P7.X,
            (1 - t) * P6.Y + t * P7.Y
            );

        var BZ = new PixelPoint(
            (1 - t) * P8.X + t * P9.X,
            (1 - t) * P8.Y + t * P9.Y
            );


        return (int)BZ.Y;
    }

    public static float BezierOld4Real(Keyframe keyframe1, Keyframe keyframe2, int intermediateFrame)
    {
        float t = (float)(intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        var value1 = Convert.ToSingle(keyframe1.Value);
        var value2 = Convert.ToSingle(keyframe2.Value);

        Point keyframe1Position = new Point(keyframe1.Frame, value1);
        Point keyframe2Position = new Point(keyframe2.Frame, value2);

        // Calcula las posiciones globales de los puntos de control
        Point globalRightHandle = new Point(keyframe1.Frame + keyframe1.RightHandle.X, value1 + keyframe1.RightHandle.Y);
        Point globalLeftHandle = new Point(keyframe2.Frame + keyframe2.LeftHandle.X, value2 + keyframe2.LeftHandle.Y);

        // Calcula los puntos intermedios de la curva de Bezier
        Point p0 = keyframe1Position;
        Point p1 = globalRightHandle;
        Point p2 = globalLeftHandle;
        Point p3 = keyframe2Position;

        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // Aplica la fórmula de Bezier para calcular las coordenadas interpoladas
        float interpolatedX = (float)(uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X);
        float interpolatedY = (float)(uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y);

        return interpolatedY;
    }

    public static float BezierGod(Keyframe keyframe1, Keyframe keyframe2, float intermediateFrame)
    {
        // Primero, necesitamos normalizar el tiempo 't' para que esté entre 0 y 1.
        float t = (intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        // Luego calculamos los puntos de la curva de Bézier usando tanto las posiciones X como Y de los handles.
        // Convertimos las posiciones de los handles a un sistema de coordenadas relativas a los keyframes.
        // Suponemos que los handles están expresados en términos de desplazamiento desde la posición del keyframe.

        // Posiciones relativas de los handles
        double x1 = keyframe1.Frame + keyframe1.RightHandle.X;
        double y1 = Convert.ToSingle(keyframe1.Value) + keyframe1.RightHandle.Y;
        double x2 = keyframe2.Frame + keyframe2.LeftHandle.X;
        double y2 = Convert.ToSingle(keyframe2.Value) + keyframe2.LeftHandle.Y;

        // Valores de los keyframes
        float y0 = Convert.ToSingle(keyframe1.Value);
        float y3 = Convert.ToSingle(keyframe2.Value);

        // Calculamos los coeficientes de la curva de Bézier
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // Calculamos los puntos de la curva de Bézier
        double point = uuu * y0; // primer término
        point += 3 * uu * t * y1; // segundo término
        point += 3 * u * tt * y2; // tercer término
        point += ttt * y3; // cuarto término

        return Convert.ToSingle(point);
    }

    public static float BezierNice(Keyframe keyframe1, Keyframe keyframe2, float intermediateFrame)
    {
        // Calcular 't' basado en el frame intermedio y los frames de los keyframes.
        float t = (intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        // Convertir las posiciones de los keyframes y handles a float.
        float P0 = Convert.ToSingle(keyframe1.Value);
        float P3 = Convert.ToSingle(keyframe2.Value);

        // Las posiciones de X de los manejadores ajustan el tiempo 't' de influencia.
        float handle1X = keyframe1.Frame + keyframe1.RightHandleFrame;
        float handle2X = keyframe2.Frame + keyframe2.LeftHandleFrame;
     
        // Las posiciones de Y de los manejadores son directamente las influencias en el valor.
        float P1 = P0 + keyframe1.RightHandleValue;
        float P2 = P3 + keyframe2.LeftHandleValue;

        t = AdjustTForHandleInfluence(t, handle1X, handle2X, keyframe1.Frame, keyframe2.Frame, P1, P2);


        // Calcular los coeficientes de Bézier.
        float u = 1 - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        // Calcular los puntos de la curva de Bézier con los coeficientes.
        float B_t = uuu * P0 + // Primer término (influencia del keyframe 1)
                    3 * uu * t * P1 + // Segundo término (influencia del handle derecho de keyframe 1)
                    3 * u * tt * P2 + // Tercer término (influencia del handle izquierdo de keyframe 2)
                    ttt * P3; // Cuarto término (influencia del keyframe 2)

        return Convert.ToSingle(B_t);
    }

    private static float AdjustTForHandleInfluence(float t, float handle1X, float handle2X, float keyframe1X, float keyframe2X, float handle1Y, float handle2Y)
    {
        // Calcular la distancia entre los keyframes y los handles
        float keyframesDistance = keyframe2X - keyframe1X;
        float handle1Distance = handle1X - keyframe1X;
        float handle2Distance = keyframe2X - handle2X;

        // Calcular la influencia relativa de los manejadores
        float handle2Influence = (handle2Distance / keyframesDistance) * t;
        float handle1Influence = (handle1Distance / keyframesDistance) * (1 - t);

        // Ajustar t
        float tAdjusted = t + handle2Influence - handle1Influence;

        // Asegurarse de que t permanezca en el rango [0, 1]
        tAdjusted = Math.Max(0, tAdjusted);
        tAdjusted = Math.Min(1, tAdjusted);

        return tAdjusted;
    }







    public static float Bezier(Keyframe keyframe1, Keyframe keyframe2, float intermediateFrame)
    {
       // double t = (intermediateFrame - keyframe1.Frame) / (keyframe2.Frame - keyframe1.Frame);

        Point P0 = keyframe1.ToPoint();
        Point P3 = keyframe2.ToPoint();

        Point P1 = keyframe1.RightHandle.Add(P0);
        Point P2 = keyframe2.LeftHandle.Add(P3);

        P1.X = Math.Min(P1.X, P3.X);
        P2.X = Math.Max(P2.X, P0.X);

        var result = TheBezier(P0, P1, P2, P3, intermediateFrame);

        return result;
    }



    public static float TheBezier(Point P0, Point P1, Point P2, Point P3, float frame)
    {

        // Función para calcular un punto en la curva de Bézier para un valor de t
        Func<double, Point> bezier = (t) =>
        {
            double x = Math.Pow(1 - t, 3) * P0.X +
                       3 * Math.Pow(1 - t, 2) * t * P1.X +
                       3 * (1 - t) * Math.Pow(t, 2) * P2.X +
                       Math.Pow(t, 3) * P3.X;

            double y = Math.Pow(1 - t, 3) * P0.Y +
                       3 * Math.Pow(1 - t, 2) * t * P1.Y +
                       3 * (1 - t) * Math.Pow(t, 2) * P2.Y +
                       Math.Pow(t, 3) * P3.Y;

            return new Point(x, y);
        };

        // Explorar valores de t para encontrar aquel que se acerque más a X = 4
        double closestY = 0;
        double minDistance = double.MaxValue;

        var it = 0;
        // 500 it
        for (double t = 0; t <= 1; t += 0.003) //0.01 - 0.003
        {
            Point point = bezier(t);
            double distance = Math.Abs(point.X - frame); // Distancia desde el punto actual hasta X = 4
            if (distance < minDistance)
            {
                minDistance = distance;
                closestY = point.Y;
            }
            it++;
        }

        return Convert.ToSingle(closestY);
    }




}












public partial class TimedVariable : ObservableObject, IId, ICloneableBehaviour
{
    public void OnClone<T>(T source)
    {
        var s = source as TimedVariable;
        CurrentTarget = s.CurrentTarget;

        foreach (var k in Keyframes)
          k.propertyInfo ??= CurrentTarget.GetType().GetProperty(PropertyName);
    }



    [ObservableProperty] bool isUIVisible = true;

    /// <summary>
    /// only for read, do not touch
    /// </summary>
    [ObservableProperty] bool isLinked = false;
    partial void OnIsLinkedChanged(bool value)
    {
        _changingLinked = true;
        Linked = value;
    }
    bool _changingLinked = false;
    /// <summary>
    /// links to shot linkeds, or remove
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] bool linked = false;

    //serialization managed by other fields
    [JsonIgnore] internal List<AnimationBehaviour> AttachedAnimationBehaviours { get; set; } = new();
    partial void OnLinkedChanged(bool oldValue, bool newValue)
    {
        if (oldValue != newValue && !Project.IsLoading && !_changingLinked)
        {
            var layer = SelectedLayer._Animation;
            if (newValue == true)
                EnableLinked(layer);  
            else
                DisableLinked(layer);
        }
        _changingLinked = false;
    }
    public void EnableLinked(AnimationBehaviour anim)
    {
        Shot shot = anim.AttachedLayer.ShotParent;
        IsLinked = true;
        shot.LinkedTimedVariables.Add(this);
    }
    public void DisableLinked(AnimationBehaviour anim)
    {
        Shot shot = anim.AttachedLayer.ShotParent;
        IsLinked = false;

        if (AttachedAnimationBehaviours.Count == 1)
            shot.LinkedTimedVariables.Remove(this);
        else
        {
            TimedVariable duplicate = this.Clone();
            anim.TimedVariables.Replace(this, duplicate);
        }
     
    }




    public Guid Id { get; set; } = Guid.NewGuid();


    [ObservableProperty] [property: JsonIgnore] bool isSelected = false;

    /// <summary>
    /// this have multiple targets, changing on time
    /// </summary>
    [JsonIgnore] public IAnimable CurrentTarget;
    public string PropertyName { get; set; }

    public MvvmHelpers.ObservableRangeCollection<Keyframe> Keyframes  { get;  private set;  } = new();

    public Type ValueType;


    public TimedVariable(string propertyName, IAnimable target)
    {
        PropertyName = propertyName;
        CurrentTarget = target;

        GraphColor = GetColor(PropertyName);

        var propertyInfo = target.GetType().GetProperty(propertyName);
        ValueType = propertyInfo.GetValue(target).GetType();
        

        Keyframes.CollectionChanged += Keyframes_CollectionChanged;
    }


    public void AddKeyframe(Keyframe keyframe, bool updateGraph = true)
    {
        // Insertar un Keyframe en la posición correcta
        // int index = Keyframes.FindIndex(kf => kf.Frame > keyframe.Frame);
        int index = Keyframes.IndexOf(Keyframes.FirstOrDefault(kf => kf.Frame > keyframe.Frame));

        //keyframe.AttachedTimedVariable = this;
    
        if (index >= 0)
        {
            Keyframes.Insert(index, keyframe);
        }
        else
        {
            Keyframes.Add(keyframe); // Si no hay un Keyframe mayor, agregar al final
        }

        if(updateGraph)
           keyframe.AttachedTimedVariable.UpdateGraph();


    }

   //------------------------------------------------------------------------------------------- APPLY VALUE TO CURRENT FRAME 2 FRAME    
   public void SetValue(int frame)
    {
        if (Keyframes.Count != 0)
        {
            //if (ValueType == typeof(SKBitmap))
            //{
            //    SetImageValue(frame);
            //}
            //else
            //{
                var value = GetValueAt(frame);
                ApplyTo(CurrentTarget, value);
         //   }

            if (!Animation.IsPlaying)
                UpdateGraph();
        }
    }
    public void SetCurrentValue()
    {
        SetValue(CurrentTarget._Animation.CurrentFrame);
    }

    private void ApplyTo(object Target, object value)
    {
        Keyframes[0].ApplyTo(Target, value);
    }

    public Keyframe GetConstantKeyframeAt(int frame)
    {
        Keyframe keyframe = Keyframes.FirstOrDefault(kf => kf.Frame == frame);
        if (keyframe != null)
        {
            return keyframe;
        }
        else
        {
            Keyframe keyframe1 = Keyframes.LastOrDefault(kf => kf.Frame < frame);
            Keyframe keyframe2 = Keyframes.FirstOrDefault(kf => kf.Frame > frame);

            Keyframe currentKeyframe = keyframe1 ?? keyframe2;

            return currentKeyframe;
        }
    }

    public object GetValueAt(int frame)
    {
        Keyframe keyframe = Keyframes.FirstOrDefault(kf => kf.Frame == frame);
        // if (!keyframe.Equals(default(Keyframe))) // if CurrentFrame is on keyframe     STRUCT
        if (keyframe != null)
        {
            //keyframe.ApplyTo(Target);
            return keyframe.Value;
        }
        else //si está entre medio de 2 keyframes
        {

            Keyframe keyframe1 = Keyframes.LastOrDefault(kf => kf.Frame < frame);
            Keyframe keyframe2 = Keyframes.FirstOrDefault(kf => kf.Frame > frame);
            // if (!keyframe1.Equals(default(Keyframe)) && !keyframe2.Equals(default(Keyframe))) STRUCT          
            if (keyframe1 != null)
            {
                if (keyframe2 != null)
                {

                    var interpolation = keyframe1.Interpolation;
                    if (keyframe1.IsValueNumberType() && interpolation != TemporalInterpolation.Constant)
                    {
                        object value = Keyframe.Interpolate(keyframe1, keyframe2, frame, interpolation);
                        return value;
                    }
                    else if (interpolation == TemporalInterpolation.Constant && frame < keyframe2.Frame)
                    {
                        return keyframe1.Value;
                    }


                    else if (keyframe1.Value is SKBitmap bitmap && !Animation.IsPlaying)
                    {
                        return bitmap.Copy();
                    }
                    else // can be image SKBitmap
                    {
                        return keyframe1.Value;

                        //throw new InvalidOperationException("error en else");
                    }
                 
                }

                else  // keyframe1 == null, First Keyframe
                {
                    return keyframe1.Value;
                }

            }
            else // si llegaste aqui, probablemente la liaste pardísima
            {
                return Keyframes[0].Value;
            }

        }

 //       throw new InvalidOperationException("No se pudo obtener el valor en el frame especificado."); // probablemente hay un lugar donde el return no llega
    }
    public float GetIntValueAt(int frame)
    {

        object value = GetValueAt(frame);

        float result = 0;

        if (ValueType == typeof(SKBitmap))
        {
            result = 0;
        }
        else if (ValueType == typeof(WriteableBitmap))
        {
            result = 0;
        }
        else if(ValueType == typeof(double) || ValueType == typeof(decimal))
        {
            result = Convert.ToInt32(value);
        }
        else
        {
            result = Convert.ToSingle(value);
        }

        return result;
    }


    public (Keyframe, Keyframe) GetIntermediateKeyframes(int frame)
    {
        Keyframe keyframe1 = Keyframes.LastOrDefault(kf => kf.Frame < frame);
        Keyframe keyframe2 = Keyframes.FirstOrDefault(kf => kf.Frame > frame);

        return (keyframe1, keyframe2);
    }
    public Keyframe GetKeyframe(int frame)
    {
        return Keyframes.FirstOrDefault(kf => kf.Frame == frame);
    }




    // ------- Graph

    private PointCollection graphPoints = new PointCollection();

    [JsonIgnore]
    public PointCollection GraphPoints
    {
        get => graphPoints;
        set => SetProperty(ref graphPoints, value, nameof(GraphPoints));
    }



    [ObservableProperty] Color graphColor = Colors.OrangeRed;
    public void UpdateGraph()
    {
        if (AppModel.IsDispatcherSafe())
        {
            GraphPoints = new();
        }
    }

    public Color GetColor(string name)
    {
        if (name.Contains("X") || name.Contains("Width"))
            return "#a13d6c".ToColor(); //Colors.OrangeRed;
        else if (name.Contains("Y") || name.Contains("Height") || name.Contains("Rotation"))
            return "#E3BD30".ToColor(); //Colors.YellowGreen;
        else if (name.Contains("Z") || name.Contains("Index"))
            return "#2F82D6".ToColor(); //Colors.Blue;
        else if (name.Contains("Image"))
            return "#6A30E3".ToColor();//Colors.DarkCyan;
        else
            return GetRandomColor();
    }

    private Color GetRandomColor()
    {
        Random random = new Random();
        byte[] colorBytes = new byte[3];
        random.NextBytes(colorBytes);
        return Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
    }


    public bool HasKeyframe(int frame)
    {
        return Keyframes.Any(keyframe => keyframe.Frame == frame);
    }

    /// <summary>
    /// has a keyframe at CurrentFrame
    /// </summary>
    /// <returns></returns>
    public bool HasKeyframe()
    {
        return HasKeyframe(Animation.CurrentFrame);
    }




    public Keyframe? GetKeyframePrevious(int currentFrame)
    {
        // Obtener el keyframe con el frame más cercano anterior al currentFrame
        return Keyframes.Where(k => k.Frame < currentFrame).OrderByDescending(k => k.Frame).FirstOrDefault();
    }

    public Keyframe? GetKeyframeNext(int currentFrame)
    {
        // Obtener el keyframe con el frame más cercano posterior al currentFrame
        return Keyframes.Where(k => k.Frame > currentFrame).OrderBy(k => k.Frame).FirstOrDefault();
    }


    public void ReorderKeyframes()
    {
        var sortedKeyframes = Keyframes.OrderBy(k => k.Frame).ToList();
        Keyframes.ReplaceRange(sortedKeyframes);
    }






    #region ----------------------------------------------------------------------- SERIALIZE

    public TimedVariable()
    {
        Keyframes.CollectionChanged += Keyframes_CollectionChanged;
    }

    private void Keyframes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (Keyframe k in e.NewItems)
            {
                k.AttachedTimedVariable = this;
                
                if (CurrentTarget != null)
                   k.propertyInfo = this.CurrentTarget.GetType().GetProperty(this.PropertyName);
            }
        }
    }

 



    #endregion -----------------------------------------------------------------------



}















//---------------------------------------------------------------------------------------------------------------------------------------------------------------- ANIMATION BEHAVIOUR

public interface IAnimable
{
    public abstract AnimationBehaviour _Animation { get; set; }
}

/// <summary>
/// comportamiento basado en la linea de tiempo
/// </summary>
/// 
public partial class AnimationBehaviour : ObservableObject, IId, ICloneableBehaviour
{
    [JsonIgnore] public LayerBase AttachedLayer { get;
        set; }

    public virtual void OnClone<T>(T source)
    {
        var s = source as AnimationBehaviour;
        //  this.TimedVariables = s.TimedVariables;
        AttachedLayer = s.AttachedLayer;

        foreach (var tv in TimedVariables)
            foreach (var k in tv.Keyframes)
            {
                tv.CurrentTarget = AttachedLayer;
                k.propertyInfo ??= tv.CurrentTarget.GetType().GetProperty(tv.PropertyName);
            }
    }

    public AnimationBehaviour CloneOld()
    {
        var a = new AnimationBehaviour();
        a.AttachedLayer = AttachedLayer;
        a.TimedVariables = TimedVariables;

        a.FrameStart = FrameStart;
        a.FrameEnd = FrameEnd;
        a.FPS = FPS;
        a.TrackIndex = TrackIndex;
        a.StartOffset = StartOffset;

        return a;
    }

    public virtual void ClearCache()
    {
        FrameBuffer.Clear();
    }

    public AnimationBehaviour()
    {
        //clone

        TimedVariables.CollectionChanged += TimedVariables_CollectionChanged;

    }


    

    //------------------------------------------------------------------------- UI
    [ObservableProperty] double canvasScaleX = 14.15;
    [ObservableProperty] double canvasScaleY = 0.28;

    [ObservableProperty] Matrix canvasMatrix = new Matrix(1, 0, 0, 1, 19, 97); //Matrix.Identity;

    public Point CanvasScale { get => new Point(CanvasScaleX, CanvasScaleY); set { CanvasScaleX = value.X; CanvasScaleY = value.Y; } }


    public Guid Id { get; set; } = Guid.NewGuid();

  
    public AnimationBehaviour(LayerBase This)
    {
        AttachedLayer = This;

        TimedVariables.CollectionChanged += TimedVariables_CollectionChanged;

    }
    public AnimationBehaviour(LayerBase This, Action<int> setFrameValue)
    {
        AttachedLayer = This;
        SetFrameValue = setFrameValue;

        TimedVariables.CollectionChanged += TimedVariables_CollectionChanged;

    }

    int trackIndex = 0;
    public int TrackIndex
    {
        get => trackIndex;
        set
        {
            if (trackIndex != value)
            {
                OnTrackIndexChanging(value);

                if (value < 0)
                    trackIndex = 0;
                else
                    trackIndex = value;

                OnPropertyChanged(nameof(TrackIndex));
                AttachedLayer?.ShotParent?.Animation.RemoveTrackBuffer(AttachedLayer);
            }
        }
    }

    void OnTrackIndexChanging(int value)
    {
        if (AttachedLayer == null || AttachedLayer.ShotParent == null) //serializing
            return;

        int moveDir = TrackIndex - value;

        var layers = AttachedLayer.ShotParent.layers;
        var layerTop = layers.First();
        if (layerTop != AttachedLayer && layerTop._Animation.TrackIndex == TrackIndex && moveDir == -1) //0, top
        {
            AttachedLayer.MoveOnTop();
            return;
        }

        var layerSameTrack = layers.FirstOrDefault(layer => layer._Animation.TrackIndex == TrackIndex && layer != AttachedLayer && !AttachedLayer.ShotParent.SelectedLayers.Contains(layer));
        if (layerSameTrack != null)
    {
        int attachedLayerIndex = layers.IndexOf(AttachedLayer);
        int sameTrackLayerIndex = layers.IndexOf(layerSameTrack);

        if (attachedLayerIndex > sameTrackLayerIndex && moveDir < 0) //-1, Mover hacia arriba si no está encima
        {
            AttachedLayer.MoveSteps(moveDir);
        }
        else if (attachedLayerIndex < sameTrackLayerIndex && moveDir > 0) //1, Mover hacia abajo si no está debajo
        {
            AttachedLayer.MoveSteps(moveDir);
        }
    }


    }

    internal void CalculateSafeTrackIndex()
    {
        // Asumiendo que 'this' es un AnimationBehaviour y que AttachedLayer y ShotParent están adecuadamente inicializados
        if (AttachedLayer?.ShotParent == null)
        {
            // Manejo de error o caso inicial; depende de tus necesidades
            return;
        }

        var overlappedAnimations = AttachedLayer.ShotParent.layers
            // Filtra para obtener solo aquellos layers cuyo rango de frames se solape con este AnimationBehaviour
            .Where(layer => layer._Animation.GlobalFrameEnd >= this.GlobalFrameStart && layer._Animation.GlobalFrameStart <= this.GlobalFrameEnd)
            .Select(layer => layer._Animation)
            .ToList();

        if (overlappedAnimations.Any())
        {
            // Si hay solapamientos, encuentra el TrackIndex más alto y suma 1
            this.TrackIndex = overlappedAnimations.Max(animation => animation.TrackIndex) + 1;
        }
        else
        {
            // Si no hay solapamientos, establece el TrackIndex a 0
            this.TrackIndex = 0;
        }
    }



    [ObservableProperty] int currentFrame = 0;
    partial void OnCurrentFrameChanged(int value)
    {
        if (AttachedLayer != null && AttachedLayer.ShotParent != null && AttachedLayer.Visible &&
            IsVisible(AttachedLayer.ShotParent.Animation.CurrentFrame) &&
            !AttachedLayer.ShotParent.Animation.IsCurrentFrameABuffer
            )
        {
            //AttachedLayer.ShotParent.Animation.CurrentFrame = value + StartOffset;
            SetValue(AttachedLayer.ShotParent.Animation.CurrentFrame);
        }
    }

    [ObservableProperty] int frameStart  = 0;


    private int frameEnd = 48;
    public int FrameEnd
    {
        get { return frameEnd; }
        set
        {
            if (frameEnd != value)
            {
                int newV;
                if (value <= FrameStart)
                    newV = FrameStart;
                else
                    newV = value;

                frameEnd = newV;
                OnPropertyChanged(nameof(FrameEnd));
                OnFrameEndChanged(newV);
            }
        }
    }


    [NotifyPropertyChangedFor(nameof(FrameDuration))]
    [ObservableProperty] int globalFrameStart = 0;
    [NotifyPropertyChangedFor(nameof(FrameDuration))]
    [ObservableProperty] int globalFrameEnd = 48;

    partial void OnFrameStartChanged(int value)
    {
        GlobalFrameStart = value + StartOffset;
        UpdateVisible();
    }
    void OnFrameEndChanged(int value)
    {
       GlobalFrameEnd = value + StartOffset;
        UpdateVisible();
    }


    public int FrameToGlobal(int value)
    {
        return value + StartOffset;
    }
    public int FrameToLocal(int value)
    {
        return value - StartOffset;
    }


    public int FrameDuration
    {
        get  {
            UpdateVisible(removeBuffer: false);
            return GlobalFrameEnd - GlobalFrameStart; 
        }
    }

    int _startOffset;
    public int StartOffset
    {
        get => _startOffset;
        set
        {
            _startOffset = value;
            GlobalFrameStart = FrameStart + value;
            GlobalFrameEnd = FrameEnd + value;

            AttachedLayer?.ShotParent?.Animation.RemoveTrackBuffer(AttachedLayer);

        }
    }


    [ObservableProperty] int fPS = 24;

    public int FrameSteps => Animation.FrameRate / FPS;

    protected virtual void UpdateVisible(bool removeBuffer = true)
    {
        if(removeBuffer)
           AttachedLayer?.ShotParent?.Animation.RemoveTrackBuffer(AttachedLayer);

        IsVisible(Animation.CurrentFrame);

        if (AttachedLayer != null)
          SetValue(Animation.CurrentFrame);
    }

    [ObservableProperty][property: JsonIgnore] bool isActuallyVisible = true;



    public bool IsVisible()
    {
        return IsVisible(AttachedLayer.ShotParent.Animation.CurrentFrame);
    }
    /// <summary>
    /// check if its visible in the trqack range and update the visibility
    /// </summary>
    /// <param name="frame"></param>
    /// <returns></returns>
    public bool IsVisible(int frame)
    {
        IsActuallyVisible = frame >= GlobalFrameStart && frame <= GlobalFrameEnd && AttachedLayer != null;// && AttachedLayer.Visible;
        return IsActuallyVisible;
    }

    [JsonConverter(typeof(TimedVariableConverter))]
    public ObservableCollection<TimedVariable> TimedVariables { get; set; } = new();

    [ObservableProperty] [property: JsonIgnore] TimedVariable selectedTimedVariable = null;
    partial void OnSelectedTimedVariableChanged(TimedVariable? oldValue, TimedVariable newValue)
    {
        if(oldValue != null)
        oldValue.IsSelected = false;

        if (newValue != null)
            newValue.IsSelected = true;
    }

    public virtual void SetValue(int frame) // IsActuallyVisible = true;
    {
        foreach (TimedVariable timedVariable in TimedVariables)
        {
            if(timedVariable.CurrentTarget != AttachedLayer)
                timedVariable.CurrentTarget = AttachedLayer;

            timedVariable.SetValue(frame - StartOffset);
        }
        SetFrameValue?.Invoke(frame);
    }
    private Action<int> SetFrameValue;

    //----------------------------------------------------------HELPERS
    public Keyframe? GetKeyframe(string propertyName)
    {
        return GetKeyframe(propertyName, CurrentFrame);
    }
    public Keyframe? GetKeyframe(string propertyName, int frame)
    {
        var tv = GetTimedVariable(propertyName);
        var key = tv?.Keyframes.FirstOrDefault(k => k.Frame == frame);

        return key;
    }

    public SKBitmap GetImageStatusAt(int frame)
    {
        var currentKey = GetTimedVariable("Image")?.GetValueAt(frame);
        if (currentKey != null)
            return (SKBitmap)currentKey;
        else
            return AttachedLayer.Image;
    }

    public Keyframe? GetKeyframeStatusAt(int frame)
    {
        var timed = GetTimedVariable("Image");
        var currentKey = timed?.GetConstantKeyframeAt(frame);
        if (currentKey != null)
            return currentKey;
        else
            return null;
    }

    public Point GetPositionAt(int frame)
    {
        Keyframe? keyX = SelectedLayer._Animation.GetKeyframe("PositionX", frame);
        Keyframe? keyY = SelectedLayer._Animation.GetKeyframe("PositionY", frame);

        float x = keyX != null ? (float)keyX.Value : SelectedLayer.PositionX;
        float y = keyY != null ? (float)keyY.Value : SelectedLayer.PositionY;

        return new Point(x, y);
    }

    public TimedVariable? GetTimedVariable(string propertyName)
    {
         return TimedVariables.FirstOrDefault(t => t.PropertyName == propertyName);
    }
    public int GetActualFrame(int frame)
    {
        return frame - StartOffset;
    }


    public List<Keyframe>? GetKeyframesByType(string keyframeType)
    {
        var keyType = AnimationManager.GetKeyframeType(keyframeType);
        if (keyType == null)
            return null;

        var keyframes = new List<Keyframe>();

        foreach (var timedVariable in TimedVariables)
        {
            // Filtrar los keyframes que coincidan con keyType y añadirlos a la lista
            keyframes.AddRange(timedVariable.Keyframes.Where(k => k.Type == keyType));
        }

        //order by frames
        keyframes = keyframes.OrderBy(k => k.Frame).ToList();
        return keyframes;
    }

    public List<Keyframe>? GetKeyframeImages()
    {
        var timedImage = TimedVariables.FirstOrDefault(t => t.PropertyName == "Image");
        return timedImage?.Keyframes.ToList();
    }
    public List<Keyframe>? GetKeyframeImages(string keyframeType)
    {
        var keyType = AnimationManager.GetKeyframeType(keyframeType);
        var imageList = GetKeyframeImages();
        return imageList?.Where(k => k.Type == keyType).ToList();
    }





    //----------------------------------------------------------------------------------- BUFFER
    [JsonIgnore] public ConcurrentDictionary<int, SKBitmap> FrameBuffer = new();


    #region ----------------------------------------------------------------------- SERIALIZE



    internal void AsignLinkedTimedVariables()
    {
        var timedVariables = TimedVariables.ToList();
        foreach (var tv in timedVariables)
        {
            if (tv.IsLinked)
            {
                TimedVariable linkedVariable = AttachedLayer.ShotParent.LinkedTimedVariables
                        .FirstOrDefault(t => t.Id == tv.Id);

                if (linkedVariable != null)
                {
                    linkedVariable.AttachedAnimationBehaviours.Add(this);
                    TimedVariables.Replace(tv, linkedVariable);
                }
            }
        }
    }


    private void TimedVariables_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {

        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (TimedVariable tm in e.NewItems)
            {
                if (!tm.AttachedAnimationBehaviours.Contains(this))
                    tm.AttachedAnimationBehaviours.Add(this);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (TimedVariable tm in e.OldItems)
            {
                tm.AttachedAnimationBehaviours.Remove(this);
            }
        }
    }

    internal void RemoveKeyframe(Keyframe keyframe)
    {
        keyframe.Delete();
    }

    internal List<Keyframe> GetAllKeyframes(bool onlyUIVisibles = false)
    {
        List<Keyframe> keyframes = new();
        foreach (var timed in TimedVariables)
        {
            if (timed.IsUIVisible)
            {
                foreach (var key in timed.Keyframes)
                    keyframes.Add(key);
            }
            
        }
        return keyframes;
    }

    internal Rect ToRect()
    {
        var ly = TrackIndexToYConverter.CalculateY(TrackIndex, 1);
        var loc = new Point(GlobalFrameStart, ly);

        var sy = TrackHeightToYConverter.CalculateHeight(1);
        var scale = new Size(FrameDuration, sy);
       
        Rect rect = new(loc, scale);
        return rect;
    }



    #endregion -----------------------------------------------------------------------

}


public class TimedVariableConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {   
        return objectType == typeof(ObservableCollection<TimedVariable>);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var timedVariables = (ObservableCollection<TimedVariable>)value;
        JArray array = new JArray();
        foreach (var timedVariable in timedVariables)
        {
            if (timedVariable.IsLinked)
            {
                // Solo serializa el Id si IsLinked es true
                array.Add(JObject.FromObject(new { timedVariable.Id }));
            }
            else
            {
                // Serializa el objeto completo
                array.Add(JObject.FromObject(timedVariable, serializer));
            }
        }
        array.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var collection = existingValue as ObservableCollection<TimedVariable> ?? new ObservableCollection<TimedVariable>();
        collection.Clear(); // Limpia la colección existente

        JArray array = JArray.Load(reader);
        foreach (var item in array)
        {
            if (item["Id"] != null && item.Children().Count() == 1) // Supone que solo contiene el Id
            {
                var id = Guid.Parse(item["Id"].Value<string>());
                // Crea un TimedVariable con solo el Id y IsLinked = true
                var linkedTimedVariable = new TimedVariable
                {
                    Id = id,
                    IsLinked = true
                };
                collection.Add(linkedTimedVariable); // Añade a la colección existente
            }
            else
            {
                // Deserializa el objeto completo
                var timedVariable = item.ToObject<TimedVariable>(serializer);
                collection.Add(timedVariable); // Añade a la colección existente
            }
        }

        return collection;
    }

}




//--------------------------------------------------------------------------------------------------------------------------------------------- SPECIAL BEHAVIOURS.

public partial class AnimationBehaviour
{
    public class VideoBased : AnimationBehaviour//------------------------------------------------------- VIDEO BASED
    {
        new VideoLayer AttachedLayer { get => (VideoLayer)base.AttachedLayer; set => base.AttachedLayer = value; }
        public VideoBased()
        {
                
        }
        public VideoBased(VideoLayer This) : base(This)
        {
            AttachedLayer = This;

            using (var mediaFile = MediaFile.Open(AttachedLayer.FilePath))
            {
                var videoStream = mediaFile.Video;
                FrameEnd = Animation.GetFrameAt(videoStream.Info.Duration);
            }
        }
        public override void SetValue(int frame)
        {
            base.SetValue(frame);
            SetActualFrame(frame);
        }


        //--------------------- VIDEO BUFFERING


        public void SetActualFrame(int frame)
        {
            if(frame < FrameStart)
            {
                var b = GetFrameBuffer(FrameStart);
                if (b != null)
                    AttachedLayer.Image = b;

                return;
            }
            else if (frame >= FrameEnd)
            {
                var b = GetFrameBuffer(FrameEnd - 1);
                if (b != null)
                    AttachedLayer.Image = b;

                return;
            }

            if (!Settings.instance.EnablePreviewCacheFrames)
            {
                var b = ManualCodec.GetVideoFrame(AttachedLayer.FilePath, frame, AttachedLayer.ShotParent);
                if(b != null)
                    AttachedLayer.Image = b;

                return;
            }

            if (AttachedLayer.ShotParent == null) return;

            int realFrame = frame - StartOffset;
            var realFrameEnd = Math.Min(realFrame + 100, FrameEnd - 1);

            // Iniciar la carga de los próximos 24 frames en segundo plano
            if (finishedLast)
            {
                finishedLast = false;

                firstFrame = realFrame;
                lastFrame = realFrameEnd;

                Task.Factory.StartNew(() => PreloadFrames(realFrame, realFrameEnd));

            }


            var bitmap = GetFrameBuffer(realFrame);
            if(bitmap != null)
                AttachedLayer.Image = bitmap;

        }
        int bufferNext = 24;
        int lastFrame = 1;
        int firstFrame = 0;

        bool finishedLast = true;
        async Task PreloadFrames(int frameStart, int frameEnd)
        {
            using var mediaFile = MediaFile.Open(AttachedLayer.FilePath);
            var videoStream = mediaFile.Video;

      
            for (int i = frameStart; i <= frameEnd; i++)
            {
                var bitmap = await LoadFrame(videoStream, i);
                if (bitmap != null)
                {
                    FrameBuffer.GetOrAdd(i, bitmap);
                }

            }
            finishedLast = true;
        }



        async Task<SKBitmap?> LoadFrame(VideoStream videoStream, int frame)
        {
            return await Task.Run(() =>
            {


                if (!FrameBuffer.ContainsKey(frame))
                {
                    var frameTime = AttachedLayer.ShotParent.Animation.GetTimeAt(frame);
                    if (frame < 0 || frameTime > videoStream.Info.Duration)
                        return null;

                    if (videoStream.TryGetFrame(frameTime, out ImageData videoFrame))
                    {
                        return videoFrame.ToSKBitmap();
                    }
                    else
                    {
                        var duration = videoStream.Info.Duration;
                        frameTime = duration - TimeSpan.FromSeconds(1.0 / videoStream.Info.AvgFrameRate);

                        if (videoStream.TryGetFrame(frameTime, out ImageData videoFrame2))
                        {
                            return videoFrame2.ToSKBitmap();
                        }
                    }
                }
                return null;
            });

        }
        SKBitmap? GetFrameBuffer(int frame)
        {
            return FrameBuffer.GetOrAdd(frame, key =>
            {
                var bitmap = ManualCodec.GetVideoFrame(AttachedLayer.FilePath, key, AttachedLayer.ShotParent);
                return bitmap ?? new SKBitmap();
            });
        }

        public override void ClearCache()
        {
            base.ClearCache();
        }
    }



    public class AudioBased : AnimationBehaviour //-------------------------------------------------------------- AUDIO LAYER
    {

        new AudioLayer AttachedLayer { get => (AudioLayer)base.AttachedLayer; set => base.AttachedLayer = value; }
        public AudioBased(AudioLayer This) : base(This)
        {
            AttachedLayer = This;
        }
        public override void SetValue(int frame)
        {
            base.SetValue(frame);
            SetActualFrame(frame);
        }

        void SetActualFrame(int frame)
        {
            if(frame == 0)
            {
                //AudioPlayer.instance.LoadAndPlayAudio(AttachedLayer.FilePath);
            }
        }
    }











    public class ShotBased : AnimationBehaviour //------------------------------------------- SHOT BASED
    {
        [JsonIgnore] new ShotLayer AttachedLayer
        { 
            get => (ShotLayer)base.AttachedLayer;
            set => base.AttachedLayer = value;   
        }

   

        public ShotBased()
        {
                
        }
        
        public ShotBased(ShotLayer This) : base(This)
        {
            AttachedLayer = This;

        }

        public override void SetValue(int frame)
        {
            base.SetValue(frame);
            SetActualFrame(frame);
        }

        public void SetActualFrame(int frame)
        {

            int realFrame = frame - StartOffset;

            AttachedLayer.ShotRef.Animation.CurrentFrame = realFrame;
              AttachedLayer.Image = AttachedLayer.ShotRef.Animation.GetBufferFrame(realFrame);

        }
    }









}






public interface IBloqueable
{
    public bool Block { get; set; }
}



//-------------------------------------------------------------------------------------------------------------------- PREVIEWABLE VALUE
/*
 //-------------------- USE

     [JsonIgnore] public PreviewableValue<SKBitmap> PreviewValue { get; private set; }
    void InitPreviewValue()
    {
        PreviewValue =
        new(
            set: (value) => Image = value,
            get: () => Image
        );

    }
 
 */

public partial class PreviewableValue<T> : ObservableObject
{
    public static event Event<PreviewableValue<T>> OnPreviewChanged;
    static void InvokePreviewChanged(PreviewableValue<T> previewValue) => OnPreviewChanged?.Invoke(previewValue);


    IBloqueable AttachedTarget;

    public T originalValue;
    public T previewValue;

    [ObservableProperty] [property: JsonIgnore]  private bool isPreviewMode;
    [ObservableProperty] [property: JsonIgnore]  private bool isPreview;
    [ObservableProperty][property: JsonIgnore] private bool isFinished;

    private readonly Action<T> setValueAction;
    private readonly Func<T> getValueFunc;
    public PreviewableValue(Func<T> get, Action<T> set)
    {
        this.setValueAction = set;
        this.getValueFunc = get;
    }

    public event Event<bool> OnEndPreview;



    partial void OnIsPreviewChanged(bool value)
    {
        if (IsPreviewMode)
        {
            SwitchPreview(value);
        }
    }

    public void StartPreview(T newValue)
    {
        if (IsPreviewMode)
        {
            previewValue = newValue;
            SwitchPreview(true);
            return;
        }

        IsPreviewMode = true;

        originalValue = getValueFunc();
        previewValue = newValue;
        setValueAction(previewValue);

        IsPreview = true;

        InvokePreviewChanged(this);
    }

    public void FinishedPreview(T newValue)
    {
        StartPreview(newValue);
        IsFinished = true;

        InvokePreviewChanged(this);
    }

    public void SwitchPreview()
    {
        IsPreview = !IsPreview;
    }

    public void SwitchPreview(bool preview)
    {
        if (!IsPreviewMode) return;

        //IsPreview = preview;
        if (preview && previewValue != null)
            setValueAction(previewValue);
        else
            setValueAction(originalValue);

        InvokePreviewChanged(this);
        Shot.UpdateCurrentRender();
    }

    [JsonIgnore] public bool IsApplied = false;

    [RelayCommand]
    [property: JsonIgnore]
    public void EndPreview(bool apply)
    {
        if (!IsPreviewMode) return;
        IsPreviewMode = false;

        if (apply)
        {
            setValueAction(previewValue);
        }
        else
            setValueAction(originalValue);

        IsApplied = apply;
        InvokePreviewChanged(this);

        originalValue = default(T);
        previewValue = default(T);

        IsPreview = false;
        IsFinished = false;

        OnEndPreview?.Invoke(apply);
        Shot.UpdateCurrentRender();
    }
}
















//-------------------------------------------------------------------------------------- TODO: ver luego como va la cosa de LRU cache
public class LRUCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _cacheMap;
    private readonly LinkedList<(TKey Key, TValue Value)> _lruList;

    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cacheMap = new Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>>(capacity);
        _lruList = new LinkedList<(TKey Key, TValue Value)>();
    }

    public TValue Get(TKey key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return node.Value.Value;
        }
        return default;
    }

    public void Add(TKey key, TValue value)
    {
        if (_cacheMap.ContainsKey(key))
        {
            var node = _cacheMap[key];
            node.Value = (key, value);
            _lruList.Remove(node);
            _lruList.AddFirst(node);
        }
        else
        {
            if (_cacheMap.Count >= _capacity)
            {
                var lru = _lruList.Last;
                _cacheMap.Remove(lru.Value.Key);
                _lruList.RemoveLast();
            }

            var newNode = new LinkedListNode<(TKey Key, TValue Value)>((key, value));
            _lruList.AddFirst(newNode);
            _cacheMap[key] = newNode;
        }
    }

    public bool Contains(TKey key)
    {
        return _cacheMap.ContainsKey(key);
    }


    // Método para comprimir una imagen antes de almacenarla en la caché
    public SKBitmap CompressImage(SKBitmap bitmap)
    {
        using (var image = SKImage.FromBitmap(bitmap))
        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 75)) // Ajusta el formato y la calidad según sea necesario
        {
            return SKBitmap.Decode(data);
        }
    }

    // Uso de la caché con compresión
    public void CacheFrame(int frame, SKBitmap bitmap)
    {
        var compressedBitmap = CompressImage(bitmap);
     //   _cacheMap.Add((T)frame, compressedBitmap);
    }
}
