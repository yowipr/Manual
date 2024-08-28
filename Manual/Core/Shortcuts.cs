using Manual.API;
using Manual.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using Manual.Editors;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Linq.Expressions;
using System.Windows.Shell;
using System.Windows.Threading;
using Manual.Editors.Displays;
using Manual.Core.Nodes;
using System.Windows.Media.Animation;
using ManualToolkit.Generic;
using System.Reflection;
using SkiaSharp;
using Manual.Objects.UI;

using static Manual.API.ManualAPI;
using System.Threading;
using Manual.Core.Graphics;

namespace Manual.Core;

public static class Shortcuts
{
    // CANVAS EVENTS
    public static event MouseButtonEventHandler CanvasMouseDown;
    public static event MouseButtonEventHandler CanvasMouseUp;
    public static event MouseEventHandler CanvasMouseMove;

    public static event MouseEventHandler CanvasMouseEnter;
    public static event MouseEventHandler CanvasMouseLeave;

    //KEY
    public static event KeyEventHandler CanvasKeyDown;
    public static event KeyEventHandler CanvasKeyUp;

    public static CanvasArea CurrentCanvas;

    public static object? GetHitClick;
    public static float MouseVelocity = 0;

    public static bool Dragging = false;
    public static bool IsTextBoxFocus = false;

    public static bool IsPanning = false;

    public static bool IsOnPreview = false;
    public static PixelPoint InitialMousePixelPosition { get; private set; }

    public static void UpdateCursor()
    {
        if(CurrentCanvas != null && ManualAPI.SelectedTool != null)
        CurrentCanvas.Cursor = ManualAPI.SelectedTool.cursor;
    }
    public static void InvokeCanvasMouseEnter(object sender, MouseEventArgs e)
    {
        CurrentCanvas = (CanvasArea)sender;
        UpdateCursor();
        CanvasMouseEnter?.Invoke(sender, e);
    }
    public static void InvokeCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        if (IsPanning)
            return;

        Dragging = false;
        CanvasMouseLeave?.Invoke(sender, e);
        CurrentCanvas = null;
    }

    public static void InvokeCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsPanning)
            return;

        HitTestResult hit = VisualTreeHelper.HitTest(CurrentCanvas, e.GetPosition(CurrentCanvas));
        if(hit != null)
        GetHitClick = hit.VisualHit;

        InitialMousePixelPosition = MousePixelPosition;

        if (e.LeftButton == MouseButtonState.Pressed)
            Dragging = true;

        ActionHistory.IsOnAction = true;
        CanvasMouseDown?.Invoke(sender, e);
 
    }

    public static void InvokeCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (IsPanning)
            return;

        Dragging = false;

        ActionHistory.IsOnAction = false;
        CanvasMouseUp?.Invoke(sender, e);
    }

    private static Point previousMousePosition;
    private static DateTime previousTimestamp;

    static Stopwatch latency = new Stopwatch();

    public static void InvokeCanvasMouseMove(object sender, MouseEventArgs e)
    {
        latency.Restart(); // Reinicia el cronómetro


        Point currentMousePosition = MousePosition;
        DateTime currentTimestamp = DateTime.Now;

        double distance = Renderizainador.Distance(previousMousePosition, currentMousePosition);
        TimeSpan timeElapsed = currentTimestamp - previousTimestamp;

        MouseVelocity = (float)(distance / timeElapsed.TotalMilliseconds);

        previousMousePosition = currentMousePosition;
        previousTimestamp = currentTimestamp;

        CanvasMouseMove?.Invoke(sender, e);


        latency.Stop();
        if(Settings.instance.SeeMS)
             AppModel.mainW.SetMessage($"{latency.ElapsedMilliseconds} ms");
    }


    public static Point MousePosition
    {
        get
        {
            if (CurrentCanvas is not null)
                return CurrentCanvas._transform.Inverse.Transform(Mouse.GetPosition(CurrentCanvas));
            else
                return new Point(0, 0);
        }
    }
    public static PixelPoint MousePixelPosition
    {
        get { return MousePosition.ToPixelPoint(); }
    }
    public static System.Drawing.PointF MousePositionF
    {
        get { return new System.Drawing.PointF((float)MousePosition.X, (float)MousePosition.Y); }
    }

    public static Point GetMousePos(MouseEventArgs e)
    {
       return e.GetPosition(CurrentCanvas);
    }
    /// <summary>
    /// vector distance between initial and current mouse position
    /// </summary>
    /// <returns></returns>
    public static Point MouseDownDistance()
    {
        return (InitialMousePixelPosition - MousePosition).ToPoint();
    }
    public static ED_CanvasView CurrentCanvasEditor
    {
        get
        {

            if (CurrentCanvas != null)
                return CurrentCanvas.DataContext as ED_CanvasView;
            else if (canvasView != null)
                return (ED_CanvasView)canvasView.DataContext;
            
            return null;
        }
    }


    //MAIN WINDOW
    public static event KeyEventHandler KeyDown;
    public static event KeyEventHandler KeyUp;

    public static void InvokeOnKeyDown(object sender, KeyEventArgs e)
    {
        if (IsPanning || IsTextBoxFocus)
            return;

        foreach (HotKey hotKey in HotKeys)
        {
          //  if (hotKey.name == "Delete Layer")
           //     Debug.WriteLine("d");

            if (hotKey.canExecute && hotKey.IsKeysPressed())
            {
                hotKey.ExecuteAction();
                Debug.WriteLine(hotKey.ToString());
            }
        }

        KeyDown?.Invoke(sender, e);

        if (CurrentCanvas != null)
            CanvasKeyDown?.Invoke(sender, e);
    }

    public static void InvokeOnKeyUp(object sender, KeyEventArgs e)
    {
        if (IsPanning)
            return;

        foreach (HotKey hotKey in HotKeys)
        {
            if (
                CurrentCanvas != null && HotKeysEnabled && !IsTextBoxFocus &&
                e.Key == hotKey.key
                )
            {
                if (hotKey.canExecute)
                {
                    hotKey.ExecuteUnaction();
                }
            }

        }
       
        KeyUp?.Invoke(sender, e);

        if (CurrentCanvas != null)
            CanvasKeyUp?.Invoke(sender, e);
    }


    public static bool IsShiftPressed => Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftShift);
    public static bool IsCtrlPressed => Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl);
    public static bool IsAltPressed => Keyboard.IsKeyDown(Key.RightAlt) || Keyboard.IsKeyDown(Key.LeftAlt);
    public static bool IsCtrlShiftPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
    public static bool IsCtrlAltShiftPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

    //----------------------- MOUSEDOWN

    public static event MouseEventHandler? MouseDown;
    public static event MouseEventHandler? MouseMove;
    public static event MouseEventHandler? MouseUp;

    /// <summary>
    /// when mouse is moved more than 10 pixels before mousedown
    /// </summary>
    public static bool IsMouseDraggable;

    static Point initialMousePosition;
    static bool isMouseDown = false;
    public static void InvokeMouseDown(object sender, MouseEventArgs e)
    {
      
        isMouseDown = true;
        IsMouseDraggable = false;
        initialMousePosition = e.GetPosition(AppModel.mainW);
        MouseDown?.Invoke(sender, e);
    }
    public static void InvokeMouseMove(object sender, MouseEventArgs e)
    {
        if (isMouseDown)
        {
            var mousePos = e.GetPosition(AppModel.mainW);
            var distance = PixelPoint.DistanceLength(initialMousePosition, mousePos);

            if (distance > 10)
            {
                IsMouseDraggable = true;
            }
           
        }
      
        MouseMove?.Invoke(sender, e);
    }
    public static void InvokeMouseUp(object sender, MouseEventArgs e)
    {
        DragDown();
        MouseUp?.Invoke(sender, e);
    }
    public static void DragDown()
    {
        isMouseDown = false;
        IsMouseDraggable = false;
        if (isDragDropAdornerEnabled)
            DragDropAdorner_Close();
    }


    public static bool isDragDropAdornerEnabled = false;

    public static void DragDropAdorner_Show(object adorner, DragEventArgs e)
    {
        DragDropAdorner_Show(adorner, null);
    }
    public static void DragDropAdorner_Show(object adorner, Type template, DragEventArgs e)
    {
        if (template != null)
        {
            DataTemplate dataTemplate = new DataTemplate();

            FrameworkElementFactory labelFactory = new FrameworkElementFactory(template);
            dataTemplate.VisualTree = labelFactory;

            AppModel.mainW.dragDropAdorner.ContentTemplate = dataTemplate;
        }

        isDragDropAdornerEnabled = true;
        AppModel.mainW.dragDropAdorner.Content = adorner;
        var mousePos = e.GetPosition(AppModel.mainW);
        AppModel.mainW.dragDropAdornerBody.Margin = new Thickness(mousePos.X, mousePos.Y - 17, 0, 0);

        AppModel.mainW.dragDropAdornerBody.Visibility = Visibility.Visible;

    }
    public static void DragDropAdorner_Close()
    {
        AppModel.mainW.dragDropAdorner.ContentTemplate = null;
        AppModel.mainW.dragDropAdorner.Content = null;
        AppModel.mainW.dragDropAdornerBody.Visibility = Visibility.Collapsed;
        isDragDropAdornerEnabled = false;
    }


    // ------------------------------------------------------------------------------------------------------------------ HOTKEYS ------------------- \\
 //   public static ObservableCollection<HotKey> HotKeys { get; set; } = new();
    //public static void HotKeys_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    //{
    //    if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
    //    {
    //        foreach (HotKey item in e.NewItems)
    //        {
    //            if(HotKeys.FirstOrDefault(h => h.name == item.name && h != item) is HotKey duplicated)
    //            {
    //                AppModel.Invoke(() => HotKeys.Replace(item, duplicated));
    //            }

    //        }
    //    }
    //}

    public static ObservableCollection<HotKey> HotKeys
    {
        get => Settings.instance.HotKeys;
        set => Settings.instance.HotKeys = value;
    }

    static List<HotKey> _duplicatedHotkeys = new();
    public static void RemoveDuplicatedHotkeys()
    {
       
    }

    public static bool HotKeysEnabled = true;
    public static void RegisterHotKey(HotKey hotKey)
    {
        HotKey existingHotKey = HotKeys.FirstOrDefault(x => x.name == hotKey.name);

        if (existingHotKey != null)
        {
           // existingHotKey.action = hotKey.action;

            hotKey.hotKeyString = existingHotKey.hotKeyString;
            HotKeys.Insert(HotKeys.IndexOf(existingHotKey), hotKey);
            RemoveHotKey(existingHotKey);
           
        }
        else
        {
            HotKeys.Add(hotKey);
        }
    }
    public static void RemoveHotKey(HotKey hotKey)
    {
        if (HotKeys.Contains(hotKey))
            HotKeys.Remove(hotKey);
    }



    /* DEPRECATED private const int WH_KEYBOARD_LL = 13; 
     private const int WM_KEYDOWN = 0x0100;
     private const int WM_SYSKEYDOWN = 0x0104;

     public static IntPtr hookId = IntPtr.Zero;
     public static bool IsHookSetup { get; set; }





     private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
     private static LowLevelKeyboardProc LowLevelProc = HookCallback;

     public static void Start()
     {
         hookId = SetHook(HookCallback);
     }

     public static void Stop()
     {
         UnhookWindowsHookEx(hookId);
     }


     private static IntPtr SetHook(LowLevelKeyboardProc proc)
     {
         using (ProcessModule module = Process.GetCurrentProcess().MainModule)
         {
             return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
         }
     }
     private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
     {
          if (nCode >= 0 && Application.Current.MainWindow.IsActive)
         {
            //foreach(HotKey hotKey in HotKeys)
            // {
            //     if (
            //         CurrentCanvas != null &&
            //         Keyboard.Modifiers.HasFlag(hotKey.ctrl) &&
            //         Keyboard.Modifiers.HasFlag(hotKey.alt) &&
            //         Keyboard.Modifiers.HasFlag(hotKey.shift) &&
            //         Keyboard.IsKeyDown(hotKey.key)
            //         )
            //     {
            //         if (hotKey.canExecute)
            //         {
            //             hotKey.ExecuteAction();
            //         }
            //     }

            // }
         }

         return CallNextHookEx(hookId, nCode, wParam, lParam);
     }



     [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
     private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

     [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
     [return: MarshalAs(UnmanagedType.Bool)]
     private static extern bool UnhookWindowsHookEx(IntPtr hhk);

     [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
     private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

     [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
     private static extern IntPtr GetModuleHandle(string lpModuleName);
     */




    //----------------------------------------------------------------------------------------------------------------------------------- general hotkeys -------------\\
    public static void SetGeneralHotKeys()
    {
        //SHORTCUTS

        //GENERATE
        HotKey GenerateImageKey = new(KeyComb.None, Key.Return, GenerateImage, "Generate Image");
        HotKey GenerateApplyKey = new(KeyComb.Ctrl, Key.Return, ApplyGeneratedImage, "Apply Generated Image");
        HotKey GenerateDiscardKey = new(KeyComb.None, Key.Escape, DiscardGeneratedImage, "Discard Generated Image");
        HotKey GenerateSwitchPreviewKey = new(KeyComb.None, Key.Tab, GenerateSwitchPreview, "Switch Generated Preview");



        //   HotKey topSectionKey = new(KeyComb.None, Key.OemComma, ChangeTopSection, "Hide/See TopSection Canvas");
        HotKey addLayerKey = new(KeyComb.CtrlShift, Key.N, AddLayer, "Add Layer");
        HotKey deleteLayerKey = new(KeyComb.None, Key.Delete, DeleteLayer, "Delete Layer");
        HotKey showHideLayerKey = new(Key.H, ShowHideLayer, "Show/Hide Layer");


        HotKey CopyKey = new(KeyComb.Ctrl, Key.C, Copy, "Copy");
        HotKey PasteKey = new(KeyComb.Ctrl, Key.V, Paste, "Paste");

        HotKey UndoKey = new(KeyComb.Ctrl, Key.Z, ActionHistory.Undo, "Undo");
        HotKey RedoKey = new(KeyComb.CtrlShift, Key.Z, ActionHistory.Redo, "Redo");

    
        HotKey RenderImageKey = new(Key.F11, AppModel.Animation_RenderImage, "Open Render Export");
        HotKey RenderKey = new(KeyComb.Ctrl, Key.M, ManualAPI.ShowRenderAnimationWindow, "Open Render Export");

        HotKey DuplicateLayerKey = new(KeyComb.Ctrl, Key.D, DuplicateLayer, "Duplicate Layer");
       //HotKey ClearLayerKey = new(KeyComb.None, Key.Back, ClearLayer, "Clear Layer");


   
        HotKey FlipCanvasKey = new(KeyComb.Ctrl, Key.N, FlipCanvas, "Flip Canvas");

        PlayKey = new(KeyComb.Alt, Key.A, Play, "Play");
        HotKey PreviusFrameKey = new(KeyComb.None, Key.Left, PreviusFrame, "Previus Frame");
        PreviusFrameKey.CapsLock = true;
        HotKey NextFrameKey = new(KeyComb.None, Key.Right, NextFrame, "Next Frame");
        NextFrameKey.CapsLock = true;
        HotKey PreviusKeyframeKey = new(KeyComb.None, Key.Down, PreviusKeyframe, "Previus Keyframe");
        PreviusKeyframeKey.CapsLock = true;
        HotKey NextsKeyframeKey = new(KeyComb.None, Key.Up, NextKeyframe, "Next Keyframe");
        NextsKeyframeKey.CapsLock = true;



        // HotKey SaveKey = new(KeyComb.Ctrl, Key.S, SaveProject, "Save Project");


        //SHORTCUTS CANVAS
        HotKey topSectionKey = new(KeyComb.None, Key.OemComma, ChangeTopSection, "Hide/See TopSection Canvas");
        HotKey searchKey = new(KeyComb.None, Key.F3, OpenSearch, "Search");


        HotKey insertKeyframeKey = new(KeyComb.None, Key.Y, InsertKeyframeHotKey, "Insert Keyframe");

        HotKey focusCameraKey = new(KeyComb.None, Key.NumPad0, FocusCamera, "Focus Camera");
        HotKey focusCameraLayerKey = new(KeyComb.None, Key.Decimal, FocusSelectedLayers, "Focus Layer");


        HotKey RenderImage = new(KeyComb.None, Key.F11, AppModel.Animation_RenderImage, "Render Image");
        HotKey RenderAnimation = new(KeyComb.Ctrl, Key.M, AppModel.Animation_RenderAnimation, "Render Animation");
    }

    static void GenerateImage()
    {
      //  if (SelectedLayer.PreviewValue.IsPreviewMode)
      //      applyGenImg(true);
     //   else if (!IsOnPreview)
            GenerationManager.Generate();
    }

    static void ApplyGeneratedImage()
    {
        if (SelectedLayer.PreviewValue.IsPreviewMode)
            applyGenImg(true);
        else if (!IsOnPreview)
            GenerateImage();
    }
    static void DiscardGeneratedImage() => applyGenImg(false);   
    static void applyGenImg(bool apply)
    {
        if (SelectedLayer.PreviewValue.IsPreviewMode)
            SelectedLayer.PreviewValue.EndPreview(apply);
    }


    static void GenerateSwitchPreview()
    {
        if (SelectedLayer.PreviewValue.IsPreviewMode)
            SelectedLayer.PreviewValue.SwitchPreview();
    }


    public static Camera3D ViewportCamera => ((ED_CanvasView)Shortcuts.canvasView?.DataContext)?.ViewportCamera;
    public static CanvasView? canvasView => AppModel.project.editorsSpace.CanvasView;
    public static void ChangeTopSection()
    {
        if (canvasView?.DataContext is ED_CanvasView c)
            c.TopSection = !c.TopSection;
        //else editor borrado (ignorarlo)
    }
    public static void ChangeTopSection(bool enabled)
    {
        if (canvasView?.DataContext is ED_CanvasView c)
            c.TopSection = enabled;
    }

    static void OpenSearch()
    {
        if (canvasView != null)
        {
            canvasView.searchPopup.IsOpen = true;
            canvasView?.searchBox.Open();
        }
    }

    static void InsertKeyframeHotKey() => CurrentCanvas.OpenContextMenu("InsertKeyframetMenu");
    



    public static HotKey PlayKey;

    //   static void ChangeTopSection() { Shortcuts.CurrentCanvasEditor.TopSection = !Shortcuts.CurrentCanvasEditor.TopSection; } //TODO: CurrentCanvasEditor es null porque el datacontext es Shot por ser CanvasArea, no CanvasView
    static void AddLayer() => ManualAPI.SelectedShot.AddLayer();
    static void DeleteLayer() => ManualAPI.SelectedShot.RemoveLayer();

    static void ShowHideLayer() => ManualAPI.SelectedLayer.Visible = !ManualAPI.SelectedLayer.Visible;
    static void DuplicateLayer() => AnimationManager.DuplicateTrack(ManualAPI.SelectedLayer);//ManualAPI.AddLayerBase(ManualAPI.SelectedLayer.Clone());

    static void Copy()
    {
        if (SelectedShot.Lasso.HasSelection())
        {
            ManualClipboard.Copy(SelectedShot.Lasso.ClipLayer(SelectedLayer));
        }
        else
        {
            ManualClipboard.Copy(ManualAPI.SelectedLayer);
        }
    }
    static void Paste()
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            ManualAPI.SelectedLayer.ImageWr = ManualClipboard.GetImage();
        }
        else
        {
            var pasted = ManualClipboard.Paste();

            if(pasted is LayerBase layer)
                ManualAPI.AddLayerBase(layer);

            else if (Clipboard.ContainsImage())
                ManualAPI.SelectedShot.AddLayer(ManualClipboard.GetImage());
        }
    }

    static void FlipCanvas() => CurrentCanvas.FlipHorizontal();

    static void Play() => ManualAPI.Animation.Play();
    static void PreviusFrame() => ManualAPI.Animation.FramePrevious();
    
    static void NextFrame() => ManualAPI.Animation.FrameNext();
    static void PreviusKeyframe() => ManualAPI.Animation.KeyframePrevious();
    
    static void NextKeyframe() => ManualAPI.Animation.KeyframeNext();
    

    public static void SaveProject() => AppModel.File_Save();
    

    static void FocusCamera() => canvasView?.FocusCamera();
    
    static void FocusSelectedLayers() => ((ED_CanvasView)canvasView?.DataContext)?.FocusCamera(ManualAPI.SelectedLayers);
    


    /// <summary>
    /// just one key pressed
    /// </summary>
    /// <param name="e"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    internal static bool IsKeyDown(KeyEventArgs e, Key s) => e.Key == s && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);


    internal static bool IsCapsLock() => Keyboard.IsKeyToggled(Key.CapsLock);
}


//---------------------------------------------------------------------------------------------------------------------------- HOTKEY
public class HotKey
{
      public string name { get; set; } = "HotKey";
    [JsonIgnore] public ModifierKeys ctrl { get; set; }
    [JsonIgnore] public ModifierKeys alt { get; set; }
    [JsonIgnore] public ModifierKeys shift { get; set; }
    [JsonIgnore] public Key key { get; set; }

    /// <summary>
    /// if need caps lock to work
    /// </summary>
    [JsonIgnore] public bool CapsLock { get; set; } = false;

    [JsonIgnore] public Action action { get; set; }
    [JsonIgnore] public Action unaction { get; set; }
    [JsonIgnore] public bool canExecute { get; set; } = true;
    [JsonIgnore] public bool ExecuteOnce { get; set; }

    [JsonIgnore] public Func<bool> Condition { get; set; } = () => Shortcuts.CurrentCanvas != null;

    private string _hotKeyString;

    public string hotKeyString
    {
        get
        {
            return _hotKeyString;
        }
        set
        {
            _hotKeyString = value;
            FromStringToHotKey();
        }
    }

    public bool IsSupportHotKey { get; set; } = false;

    public HotKey()
    {
       
    }

    public void ExecuteAction()
    {
        action?.Invoke();
    }
    public void ExecuteUnaction()
    {
        if (IsSupportHotKey)
        {
            unaction?.Invoke();
        }
    }
    public HotKey(string hotKeyString, Action action, string name)
    {
        this.name = name;
        this.action = action;
        this.hotKeyString = hotKeyString;
        FromStringToHotKey();

        Shortcuts.RegisterHotKey(this);
    }
    public HotKey (KeyComb modifierCombinations, Key key, Action action, string name)
    {
        this.name = name;
        this.action = action;

        SetModifiers(modifierCombinations);
        this.key = key;

        FromHotKeyToString();
        Shortcuts.RegisterHotKey(this);
    }
    public HotKey(Key key, Action action, string name)
    {
        this.name = name;
        this.action = action;

        SetModifiers(KeyComb.None);
        this.key = key;

        FromHotKeyToString();
        Shortcuts.RegisterHotKey(this);
    }
 
    public HotKey(KeyComb modifierCombinations, Key key, Action action, Action unaction, string name)
    {
        this.name = name;
        this.action = action;
        this.unaction = unaction;
        IsSupportHotKey = true;

        SetModifiers(modifierCombinations);
        this.key = key;

        FromHotKeyToString();
        Shortcuts.RegisterHotKey(this);
    }

    public HotKey(KeyComb subkey, string name)
    {
        this.name = name;
        SetModifiers(subkey);
        Shortcuts.RegisterHotKey(this);
    }

    public bool IsKeysPressed()
    {
        if (CapsLock && Shortcuts.IsCapsLock())
            return false;

        bool keyPressed = key != Key.None && Keyboard.IsKeyDown(key);

        bool modifiersMatch = true; // Asumimos que coincide hasta demostrar lo contrario.

        // Verifica si Ctrl es necesario y si está presionado, o si no es necesario y no está presionado.
        if (ctrl != ModifierKeys.None)
        {
            modifiersMatch &= Keyboard.Modifiers.HasFlag(ctrl);
        }
        else
        {
            modifiersMatch &= !Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        }

        // Haz lo mismo para Alt y Shift.
        if (alt != ModifierKeys.None)
        {
            modifiersMatch &= Keyboard.Modifiers.HasFlag(alt);
        }
        else
        {
            modifiersMatch &= !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        }

        if (shift != ModifierKeys.None)
        {
            modifiersMatch &= Keyboard.Modifiers.HasFlag(shift);
        }
        else
        {
            modifiersMatch &= !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        }

        bool exactMatch = modifiersMatch && keyPressed && Condition.Invoke() && Shortcuts.HotKeysEnabled && !Shortcuts.IsTextBoxFocus;

        return exactMatch;
    }


    //public bool IsKeysPressed()
    //{
    //    bool keyPressed = key != Key.None && Keyboard.IsKeyDown(key);

    //    bool ExactMatch = Condition.Invoke() && Shortcuts.HotKeysEnabled && !Shortcuts.IsTextBoxFocus &&
    //             Keyboard.Modifiers.HasFlag(ctrl) &&
    //             Keyboard.Modifiers.HasFlag(alt) &&
    //             Keyboard.Modifiers.HasFlag(shift) &&
    //             keyPressed;

    //    return ExactMatch;
    //}

    public bool IsSubKeysPressed()
    {
        bool ExactMatch =
                Keyboard.Modifiers.HasFlag(ctrl) &&
                Keyboard.Modifiers.HasFlag(alt) &&
                Keyboard.Modifiers.HasFlag(shift);

        return ExactMatch;
    }




    void SetModifiers(KeyComb modifierCombinations)
    {
        switch (modifierCombinations)
        {
            case KeyComb.Ctrl:
                ctrl = ModifierKeys.Control;
                alt = ModifierKeys.None;
                shift = ModifierKeys.None;
                break;
            case KeyComb.CtrlAlt:
                ctrl = ModifierKeys.Control;
                alt = ModifierKeys.Alt;
                shift = ModifierKeys.None;
                break;
            case KeyComb.CtrlAltShift:
                ctrl = ModifierKeys.Control;
                alt = ModifierKeys.Alt;
                shift = ModifierKeys.Shift;
                break;
            case KeyComb.CtrlShift:
                ctrl = ModifierKeys.Control;
                alt = ModifierKeys.None;
                shift = ModifierKeys.Shift;
                break;
            case KeyComb.Alt:
                ctrl = ModifierKeys.None;
                alt = ModifierKeys.Alt;
                shift = ModifierKeys.None;
                break;
            case KeyComb.AltShift:
                ctrl = ModifierKeys.None;
                alt = ModifierKeys.Alt;
                shift = ModifierKeys.Shift;
                break;
            case KeyComb.Shift:
                ctrl = ModifierKeys.None;
                alt = ModifierKeys.None;
                shift = ModifierKeys.Shift;
                break;
            case KeyComb.None:
                ctrl = ModifierKeys.None;
                alt = ModifierKeys.None;
                shift = ModifierKeys.None;
                break;
        }
    }
    private void FromHotKeyToString()
    {
        string hotKeyString = string.Empty;

        // Convertir las propiedades a cadena
        if (ctrl != ModifierKeys.None)
        {
            hotKeyString += "Ctrl" + "+";
        }

        if (alt != ModifierKeys.None)
        {
            hotKeyString += alt.ToString() + "+";
        }

        if (shift != ModifierKeys.None)
        {
            hotKeyString += shift.ToString() + "+";
        }

        hotKeyString += key.ToString();

        this.hotKeyString = hotKeyString;
    }
    public void FromStringToHotKey()
    {
        if (hotKeyString == null)
            return;

        string[] keys;     
        if (hotKeyString.Contains("+"))
            keys = hotKeyString.Split('+');
        else
            keys = new string[] { hotKeyString };
        

        if (keys[0].Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
        {
            keys[0] = "Control";
        }

        foreach (string keyString in keys)
        {
            ModifierKeys modifierKey;
            if (Enum.TryParse(keyString, out modifierKey))
            {
                // Asignar el valor del modificador a la propiedad correspondiente
                if (keyString.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    ctrl = modifierKey;
                else if (keyString.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    alt = modifierKey;
                else if (keyString.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    shift = modifierKey;
            }
            else
            {
                // Asignar el valor de la tecla a la propiedad correspondiente
                key = (Key)Enum.Parse(typeof(Key), keyString);
            }
        }
    }

    public override string ToString()
    {
        return $"{this.name}: {hotKeyString}";
    }
}


public enum KeyComb
{
    Ctrl,
    CtrlAlt,
    CtrlAltShift,
    CtrlShift,

    Alt,
    AltShift,

    Shift,

    None,
}



public class ActionHistory
{
    public static bool IsOnAction { get; set; } = false;


    public ActionHistory()
    {

    }

    public static ActionHistory instance => ManualAPI.SelectedShot.actionHistory;



    public delegate void ActionHandler(IUndoableAction action);
    public static event ActionHandler OnActionChange;

    //OLD But still use it
    public class DoAction
    {
        public string name { get; set; }
        public Action action { get; set; }

        public DoAction()
        {
            
        }
        public DoAction(Action action)
        {
            this.name = "unknown action";
            this.action = action;
        }
        public DoAction(string name, Action action)
        {
            this.name = name;
            this.action = action;
        }

       


        public static void Do(Action action, double waitSeconds)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(waitSeconds);

            timer.Tick += (sender, args) =>
            {
                // Detener el temporizador
                DispatcherTimer timer = (DispatcherTimer)sender;
                timer.Stop();

                action?.Invoke();
            };

            timer.Start();
        }
        public static void Do(Action action, double waitSeconds, CancellationToken cancellationToken)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(waitSeconds);

            timer.Tick += (sender, args) =>
            {
                // Detener el temporizador para limpieza
                DispatcherTimer localTimer = (DispatcherTimer)sender;
                localTimer.Stop();

                // Comprobar si la acción ha sido cancelada
                if (cancellationToken.IsCancellationRequested)
                {
                    return;  // Salir sin ejecutar la acción
                }

                action?.Invoke();
            };

            // Asegurarse de detener el temporizador si la operación es cancelada
            cancellationToken.Register(() =>
            {
                timer.Stop();
            });

            timer.Start();
        }



        bool once = true;
        public void DoOnce()
        {
            if (once)
            {
                action?.Invoke();
                once = false;
            }
        }



    }


    //-------------------------------------------------------------------------------------------------- NEW UNDO REDO
    public List<IUndoableAction> actions = new List<IUndoableAction>();
    public int currentIndex = -1;

    long realIndex = 0;
    long savedIndex = 0; // Índice cuando el proyecto fue guardado por última vez


    public void Saved()
    {
        savedIndex = realIndex;
    }
    void UpdateSavedStatus()
    {
        // Verifica si el estado actual coincide con el estado guardado
        SelectedShot.IsSaved = realIndex == savedIndex;

        Shot.UpdateCurrentRender();
    }



    void _do(IUndoableAction action)
    {
        if (actions.Contains(action) || Project.IsLoading) return;


        // Asegurar que solo mantenemos un número máximo de acciones
        int maxActions = Settings.instance.UndoSteps;
        if (actions.Count >= maxActions)
        {
            // Remueve las acciones más antiguas para hacer espacio para la nueva
            actions.RemoveAt(0);
            currentIndex--;
        }

        // Si después de añadir una acción nos encontramos después del último índice
        if (currentIndex < actions.Count - 1)
        {
            actions = actions.Take(currentIndex + 1).ToList();
        }

        actions.Add(action);
        currentIndex++;
        realIndex++;
        action.Execute();
    }




    void _undo()
    {
        if (currentIndex >= 0)
        {
            actions[currentIndex].Undo();
            currentIndex--;
            realIndex--;
            UpdateSavedStatus();
        }
    }

    void _redo()
    {
        if (currentIndex + 1 < actions.Count)
        {
            currentIndex++;
            realIndex++;
            actions[currentIndex].Redo();
            UpdateSavedStatus();
        }
    }

    public static void Undo() => instance._undo();
    public static void Redo() => instance._redo();
   




    static IUndoableAction? CurrentAction;

    /// <summary>
    /// don't forget to ActionHistory.FinalizeAction();
    /// if you don't finalize the action with FinalizeAction(object newValue), this action will be deleted automatically if you start another action
    /// </summary>
    /// <param name="targetObject"></param>
    /// <param name="propertyName"></param>
    /// <param name="oldValue"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static ChangePropertyAction StartAction(object targetObject, string propertyName, string name = "")
    {
        var change = new ChangePropertyAction(targetObject, propertyName, name);
        CurrentAction = change;

        return change;
    }
    public static CompositeAction StartAction(object targetObject, string[] propertyNames, string name = "")
    {
        var change = new CompositeAction();
        change.Name = name;

        foreach (var propertyName in propertyNames)
            change.AddAction(targetObject, propertyName, name);
        

        CurrentAction = change;

        return change;

    }
    public static CompositeAction StartAction(string name, params (object targetObject, string propertyName)[] actions)
    {
        var change = new CompositeAction();
        change.Name = name;

        foreach (var action in actions)
            change.AddAction(action.targetObject, action.propertyName, name);


        CurrentAction = change;

        return change;

    }
    public static CompositeAction StartAction(params (object targetObject, string propertyName)[] actions)
    {
        return StartAction("", actions);
    }


    /// <summary>
    /// instantly finalize action and update collction
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="item"></param>
    /// <param name="isAdd"></param>
    /// <returns></returns>
    public static ListAction<T> CollectionAction<T>(ObservableCollection<T> collection, T item, bool isAdd, string name = "")
    {
        var change = new ListAction<T>(collection, item, isAdd, name);

        CurrentAction = change;

        FinalizeAction();
        return change;
    }
    public static CompositeAction CollectionActionSelected<T>(ObservableCollection<T> collection, T item, bool isAdd, object target, string selectedItemName, T oldItem, string name = "")
    {
        var change2 = new ChangePropertyAction(target, selectedItemName, oldItem);
        var change = new ListAction<T>(collection, item, isAdd, name);

        var change3 = new CompositeAction(change, change2);
        change3.Name = name != "" ? name : (isAdd ? $"Add {item}" : $"Remove {item}");

        CurrentAction = change3;
        FinalizeAction();
        return change3;


    }


    public static void FinalizeAction()
    {
        if (CurrentAction == null) return;

       instance._do(CurrentAction);

        AppModel.project.IsSaved = false;
        OnActionChange?.Invoke(CurrentAction);

        CurrentAction = null;
        Shot.UpdateCurrentRender();
        AppModel.mainW.InvalidateGlow();
    }

    public static void CancelAction()
    {
        CurrentAction = null;
        Shot.UpdateCurrentRender();
        AppModel.mainW.InvalidateGlow();
    }

}











public interface IUndoableAction
{
    public string Name { get; set; }
    void Execute();
    void Undo();
    void Redo();
}

public class ChangePropertyAction : IUndoableAction
{
    public string Name { get; set; }

    private object targetObject;
    private object oldValue;
    private object newValue;
    private PropertyInfo propertyInfo;

    public ChangePropertyAction(object targetObject, string propertyName, string name = "")
    {
        this.targetObject = targetObject;
        propertyInfo = targetObject.GetType().GetProperty(propertyName);
        this.Name = name;

        SetInitialValue();
    }

    public ChangePropertyAction(object targetObject, string propertyName, object oldValue, string name = "")
    {
        this.targetObject = targetObject;
        propertyInfo = targetObject.GetType().GetProperty(propertyName);
        this.Name = name;
        this.oldValue = oldValue;

        SetName();
    }


    void SetInitialValue()
    {
        var oldValue = propertyInfo.GetValue(targetObject);

        this.oldValue = ModifiedValue(oldValue);

        SetName();

        // NOTIFY
        if (targetObject is IAnimable target)
        {
            Animation.NotifyActionStartChanging(target, propertyInfo.Name);
        }
    }

    //save the final value, variable changed
    public void Execute()
    {
        var newValue = propertyInfo.GetValue(targetObject);
        this.newValue = ModifiedValue(newValue);

        SetName();

        // NOTIFY
        if (targetObject is IAnimable target)
        {
            Animation.NotifyActionChanged(target, propertyInfo.Name);

            if (target is LayerBase l && l.ShotParent != null)
                l.ShotParent.IsSaved = false;

        }


    }


    void SetName()
    {
        this.Name = Name == "" || Name == null ? $"{propertyInfo.ReflectedType.Name} {propertyInfo.Name}: {this.newValue}" : Name;
    }

    object ModifiedValue(object value)
    {
        if (propertyInfo.PropertyType == typeof(SKBitmap))
        {
            var cvalue = ((SKBitmap)propertyInfo.GetValue(targetObject)).Copy();
            if (cvalue != null)
                value = cvalue;//Clone
            else value = null;
        }

        return value;
    }


    public void Undo()
    {
        if(targetObject is IAnimable tanim)
         Animation.NotifyActionStartChanging(tanim, propertyInfo.Name);

        propertyInfo.SetValue(targetObject, oldValue);
    }

    public void Redo()
    {
        if (targetObject is IAnimable tanim)
            Animation.NotifyActionStartChanging(tanim, propertyInfo.Name);

        propertyInfo.SetValue(targetObject, newValue);
    }


    public object GetNewValue() => newValue;

    public object GetOldValue() => oldValue;
    
}



public class CompositeAction : IUndoableAction
{
    private List<IUndoableAction> actions = new List<IUndoableAction>();

    public string Name { get; set; }


    public CompositeAction()
    {
            
    }

    public CompositeAction(params IUndoableAction[] actions)
    {
        foreach (var action in actions)
            AddAction(action);
    }


    public void AddAction(IUndoableAction action)
    {
        actions.Add(action);
    }
    public void AddAction(object targetObject, string propertyName, string name = "")
    {
        var action = new ChangePropertyAction(targetObject, propertyName, name);
        actions.Add(action);
    }

    public void Execute()
    {
        foreach (var action in actions)
        {
            action.Execute();
        }
    }

    public void Undo()
    {
        foreach (var action in Enumerable.Reverse(actions))
        {
            action.Undo();
        }
    }

    public void Redo()
    {
        foreach (var action in actions)
        {
            action.Redo();
        }
    }
}




public class ListAction<T> : IUndoableAction
{
    public string Name { get; set; }

    private ObservableCollection<T> collection;
    private T item;
    private bool isAdd;
    private int originalIndex;
    public ListAction(ObservableCollection<T> collection, T item, bool isAdd, string name = "")
    {
        this.Name = name != "" ? name : (isAdd? $"Add {item}" : $"Remove {item}");
        this.collection = collection;
        this.item = item;
        this.isAdd = isAdd;
        
        this.originalIndex = collection.IndexOf(item);
    }

    public void Execute()
    {
       //nothing
    }

    public void Undo()
    {
        if (isAdd)
        {
            collection.Remove(item);
        }
        else
        {
            // Asegúrate de añadir el elemento de nuevo en su índice original
            if (originalIndex >= 0 && originalIndex <= collection.Count)
            {
                collection.Insert(originalIndex, item);
            }
            else
            {
                collection.Add(item);
            }


        }
    }

    public void Redo()
    {
        if (isAdd)
        {
            if (!collection.Contains(item))
            {
                // Si originalIndex es válido, añade el elemento en ese índice;
                // de lo contrario, añádelo al final de la colección
                if (originalIndex >= 0)
                {
                    collection.Insert(originalIndex, item);
                }
                else
                {
                    collection.Add(item);
                }
            }
        }
        else
        {
            if (collection.Contains(item))
            {
                collection.Remove(item);
            }
        }


    }
}
