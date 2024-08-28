#region Usings
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


using System.ComponentModel;
using System.Net.Http.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Shell;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;


using Microsoft.WindowsAPICodePack.Taskbar;
using System.Runtime.CompilerServices;
using Manual.Core;
using Manual.Editors;


using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using Accessibility;
using System.Net.NetworkInformation;
using Manual.Objects;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using Manual.Editors.Displays;
using Squirrel;

using ManualToolkit.Windows;
using static System.Net.WebRequestMethods;
using Python.Runtime;
using Manual.Objects.UI;
using System.Windows.Media.Effects;
using Manual.Editors.Displays.Launcher;
using Manual.Core.Nodes;
using ManualToolkit.Generic;
using SkiaSharp;

using System.Timers;
using ManualToolkit.Specific;

//
using Manual.Core.Graphics;
using OpenTK.Mathematics;


#endregion

namespace Manual;


/// <summary>
/// Lógica de interacción para MainWindow.xaml
/// </summary>

public partial class MainWindow : Window
{

    public static void TestsButtons(string header)
    {
        if (header == "add square")
        {
            Rend3D.Start();  
        }
        
        Shot.UpdateCurrentRender();
    }

    public static void TestButton()
    {
        var b = UserManager.instance.User?.Admin;
    }



    double l = 0;
    double t = 0;
    double w = 100;
    double h = 100;

    WindowChrome? windowChrome;

   // public ProgressBar renderBar;
    public TextBlock logTextblock;


    public MainWindow()
    {
        miBlurEffect = new BlurEffect { Radius = 0 };


        if (AppModel.mainWOld != null)
            ShowInTaskbar = false;

         Opacity = 0;

         MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
         MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

     
        InitializeComponent();

        WindowState = WindowState.Minimized;


        //references to window
        AppModel.mainW = this;
        TaskBar.SetWindow(this);

      //  renderBar = r_bar;
        logTextblock = l_textblock;

        DataContext = AppModel.project.editorsSpace;

        if (AppModel.mainWOld == null)
        {
            // splash = new();
            //   EDITORS.Children.Add(splash);
            OpenSplash();
        }

   

        PreviewMouseDown += MainWindow_MouseDown;
        PreviewMouseMove += MainWindow_MouseMove;
        PreviewMouseUp += MainWindow_MouseUp;

        DragOver += MainWindow_DragOver;
        Drop += MainWindow_Drop;
        AllowDrop = true;

        SizeChanged += MainWindow_SizeChanged;
        LocationChanged += MainWindow_LocationChanged;


        // SERVER STATUS
        info_breath = AnimationLibrary.BreatheOpacity(info_elipse);

        if(Settings.instance.AIServer.IsOpening)
            ChangeServerStatus(ServerStatus.Opening);
        else
           CheckServerStatus();
    }
    private Timer checkTimer;
    void ServerCheck()
    {
        checkTimer = new Timer(10_000); // Intervalo de 10 segundos
        checkTimer.Elapsed += async (sender, e) => await CheckServer();
        checkTimer.AutoReset = true;
        checkTimer.Enabled = true;
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        foreach (var editor in AppModel.project.editorsSpace.Current_Workspace.FloatingEditors)
            editor.UpdateRelativeLocations(editor.Left, editor.Top);
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        foreach (var editor in AppModel.project.editorsSpace.Current_Workspace.FloatingEditors)
            editor.UpdateRelativeDimensions(editor.Width, editor.Height);    
    }



    //---------------------------------------------------------------------------------------------------------------------- MAIN WINDOW CONTENT RENDERED
    private void mainwindow_ContentRendered(object sender, EventArgs e)
    {
        App.CloseSplashScreen();
     
        bool isOpeningFile = Project._tcsOpen != null;



        if (AppModel.mainWOld != null)
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
            AppModel.mainWOld.Close();
        }

        WindowState = WindowState.Maximized;
        WMax.Content = "▢";


        if (!isOpeningFile)
        {
            FadeBlackIn();
        }


        DataContext = AppModel.project.editorsSpace;

        AppModel.mainWOld = null;

        ShowInTaskbar = true;

        //if isOpening
        Project.CanOpen();

        UpdateGlow();
        FpsMonitor.Start();




        // Configurar el temporizador para limitar la frecuencia de las actualizaciones
        updateTimer = new DispatcherTimer();
        updateTimer.Interval = TimeSpan.FromMilliseconds(300); // Ajusta el intervalo según tus necesidades
        updateTimer.Tick += UpdateTimer_Tick;
        updateTimer.Start();


        if (AppModel.IsForceLogin)
            AppModel.OpenLogin();
    }
    private DispatcherTimer updateTimer;
    private bool needsUpdate;
    private void Canvas_LayoutUpdated(object sender, EventArgs e)
    {
        // Marcar que se necesita una actualización
        InvalidateGlow();
    }
    private void UpdateTimer_Tick(object sender, EventArgs e)
    {
        if (needsUpdate)
        {
            UpdateGlow();
            needsUpdate = false; // Resetear la bandera de actualización
        }
    }

    public void InvalidateGlow()
    {
        needsUpdate = true;
    }

    private BlurEffect miBlurEffect;

    private BitmapSource CaptureCanvasImage(FrameworkElement canvas)
    {
        var downscaleFactor = 8;

        // Calcular el tamaño reducido
        int width = (int)canvas.ActualWidth / downscaleFactor;
        int height = (int)canvas.ActualHeight / downscaleFactor;

        // Medir y organizar el canvas
        canvas.Measure(new Size(canvas.ActualWidth, canvas.ActualHeight));
        canvas.Arrange(new Rect(new Size(canvas.ActualWidth, canvas.ActualHeight)));

        // Crear el render target con tamaño reducido
        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
            width, height, 96d / downscaleFactor, 96d / downscaleFactor, PixelFormats.Pbgra32);

        // Renderizar el canvas en el render target
        renderBitmap.Render(canvas);

        // Escalar de vuelta al tamaño original
        //TransformedBitmap scaledBitmap = new TransformedBitmap(renderBitmap, new ScaleTransform(downscaleFactor, downscaleFactor));

        //   return scaledBitmap;
        return renderBitmap;
    }
    private RenderTargetBitmap CaptureCanvasLow(FrameworkElement canvas)
    {
        double dpi = 96;
        double scale = 0.1; // Escala baja para obtener una imagen de baja calidad

        int renderWidth = (int)(canvas.ActualWidth * scale);
        int renderHeight = (int)(canvas.ActualHeight * scale);

        var renderTarget = new RenderTargetBitmap(renderWidth, renderHeight, dpi * scale, dpi * scale, PixelFormats.Pbgra32);
        var visualBrush = new VisualBrush(canvas);

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(), new Size(canvas.ActualWidth, canvas.ActualHeight)));
        }

        renderTarget.Render(drawingVisual);

        return renderTarget;
    }

    public void InvokeGlow()
    {

        if (!Settings.instance.EnableGlow)
            return;

        // Crear la animación de opacidad
        DoubleAnimation opacityAnimation = new DoubleAnimation
        {
            From = 0, // Iniciar en opacidad 0
            To = 1, // Finalizar en opacidad 1
            Duration = TimeSpan.FromSeconds(3), // Duración de 3 segundos
            // Usar una función de aceleración para ralentizar hacia el final
           // EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
        };

        // Crear y configurar el Storyboard
        Storyboard storyboard = new Storyboard();
        storyboard.Children.Add(opacityAnimation);
        Storyboard.SetTarget(opacityAnimation, RectangleGlow);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

        // Iniciar la animación
        storyboard.Begin();

        InvalidateGlow();
    }

    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
       // UpdateGlow();
        //SetGlowImage(currentCanvas);
    }

    FrameworkElement currentCanvas;
    public void UpdateGlow()
    {
        EDITORS.Dispatcher.InvokeAsync(_updateGlow, DispatcherPriority.Render);
    }

    int glowSpacing = 0;

    bool _awaitGlow = false;
    async void _updateGlow()
    {
        if (_awaitGlow || ActionHistory.IsOnAction) return;

        _awaitGlow = true;
        CompositionTarget.Rendering += CompositionTarget_Rendering;

        //EnableGlow
        if (EditorsSpace.instance.CanvasView is CanvasView canvaso && Settings.instance.EnableGlow)
        {
            var canvas = canvaso.canvas;
            var rect = CalculateRelativePosition(canvas, EDITORS);

            if (rect == Rect.Empty || canvas.ActualHeight == 0)
                return;

            // Configura el control de 'VisualGlow'
            RectangleGlow.Margin = new Thickness(rect.Left, rect.Top, 0, 0);
            RectangleGlow.Width = rect.Width;
            RectangleGlow.Height = rect.Height;
            RectangleGlow.Visibility = Visibility.Visible;



            //BitmapSource canvasImage = CaptureCanvasImage(canvas);

            //// Crear un ImageBrush con la imagen capturada
            //ImageBrush imageBrush = new ImageBrush(canvasImage)
            //{
            //    Stretch = Stretch.None
            //};

            //// Asignar el ImageBrush al Fill del RectangleGlow
            //RectangleGlow.Fill = imageBrush;



            var lowQualityImage = CaptureCanvasLow(canvas);

            // Establece la imagen capturada como relleno del RectangleGlow
            RectangleGlow.Fill = new ImageBrush(lowQualityImage);


            if (currentCanvas != canvas)
            {
                //RectangleGlow.Fill = new VisualBrush(canvas)
                //{
                //    Stretch = Stretch.None
                //};

                if(currentCanvas != null)
                   currentCanvas.LayoutUpdated -= Canvas_LayoutUpdated;

                currentCanvas = canvas;

                currentCanvas.LayoutUpdated += Canvas_LayoutUpdated;

            }
        }

        //doesn't have canvas
        else
        {
            RectangleGlow.Visibility = Visibility.Collapsed;
        }
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitGlow = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    void SetGlowImage(FrameworkElement canvas)
    {
        if(canvas != null)
        RectangleGlow.Fill = CaptureCanvas(canvas);
    }
    ImageBrush CaptureCanvas(FrameworkElement canvas)
    {
        // Define el tamaño del bitmap basado en el tamaño actual del canvas.
        int width = (int)canvas.ActualWidth;
        int height = (int)canvas.ActualHeight;

        // Crea un RenderTargetBitmap.
        RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
            width, height, // Dimensiones del bitmap.
            96, 96, // DPI. Ajusta según necesidad para la calidad deseada.
            PixelFormats.Pbgra32);

        // Renderiza el canvas en el RenderTargetBitmap.
        renderTargetBitmap.Render(canvas);

        // Crea un ImageBrush usando el RenderTargetBitmap.
        ImageBrush imageBrush = new ImageBrush(renderTargetBitmap);

        return imageBrush;
    }

    private Rect CalculateRelativePosition(FrameworkElement child, FrameworkElement parent)
    {
        try
        {
            // Obtiene la posición relativa del elemento hijo dentro del elemento padre
            var transform = child.TransformToAncestor(parent);
            var topLeft = transform.Transform(new Point(0, 0));
            var bottomRight = transform.Transform(new Point(child.ActualWidth, child.ActualHeight));

            return new Rect(topLeft, bottomRight);
        }
        catch
        {
            return Rect.Empty;
        }
    }











    private void MainWindow_Drop(object sender, DragEventArgs e)
    {
        Shortcuts.DragDropAdorner_Close();
    }

    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (Shortcuts.isDragDropAdornerEnabled)
        {
            var mousePos = e.GetPosition(this);
            dragDropAdornerBody.Margin = new Thickness(mousePos.X, mousePos.Y - 17, 0, 0);
        }
    }
    public void SetDragDropAdornerPos()
    {
        
    }

    private void MainWindow_MouseMove(object sender, MouseEventArgs e)
    {
        Shortcuts.InvokeMouseMove(this, e);
    }

    private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Shortcuts.InvokeMouseDown(this, e);
    }
    private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
    {
        Shortcuts.InvokeMouseUp(this, e);
    }






    void FullScreenEditor()
    {
        // Encontrar el EditorWindow en la jerarquía de elementos visuales
        var hittedElement = PointHit();
        if (hittedElement != null)
        {
            var ed = AppModel.FindAncestor<EditorWindow>(hittedElement);

            // Si se encuentra un EditorWindow, cambiar al modo de pantalla completa
            if (ed != null)
            {
                ed.SwitchFullScreen();
            }
        }

    }


    private FrameworkElement _hitTestResultElement;
    public FrameworkElement PointHit()
    {
        Point hitPoint = Mouse.GetPosition(this);
        _hitTestResultElement = null; // Resetea el resultado del hit test

        // Define el callback del resultado del hit test
        HitTestResultCallback resultCallback = new HitTestResultCallback(HitTestResultHandler);

        // Realiza el hit test
        VisualTreeHelper.HitTest(this, null, resultCallback, new PointHitTestParameters(hitPoint));

        // Retorna el resultado del hit test después de que se haya completado
        return _hitTestResultElement;
    }

    // Esta función maneja los resultados del hit test
    private HitTestResultBehavior HitTestResultHandler(HitTestResult result)
    {
        if (result.VisualHit is FrameworkElement grid && (string)grid.Tag == "editor")
        {
            _hitTestResultElement = grid; // Guarda el resultado del hit test
            return HitTestResultBehavior.Stop; // Detiene el hit test
        }

        return HitTestResultBehavior.Continue; // Continúa con el hit test
    }




    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {

         base.OnKeyDown(e);

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
        {
            FullScreenEditor();
            e.Handled = true;
            return;
        }
        else if(Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) // SAVE
        {
            Shortcuts.SaveProject();
            e.Handled = true;
        }

        if (Shortcuts.CurrentCanvas != null && (e.Key == Key.Space || Shortcuts.IsPanning && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)))   // || e.Key == Key.LeftAlt || e.Key == Key.RightAlt))
        {
            Shortcuts.CurrentCanvas.InvokeKeyDown(this, e); 
            //e.Handled = true;
        }
        else
            Shortcuts.InvokeOnKeyDown(this, e);


        if (Shortcuts.CurrentCanvas != null && Shortcuts.IsTextBoxFocus == false)
        {
            e.Handled = true;
        }
  
    }
    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (Shortcuts.CurrentCanvas != null && (e.Key == Key.Space || (Shortcuts.IsPanning && (e.Key == Key.LeftAlt || e.Key == Key.RightAlt))))  //|| e.Key == Key.LeftAlt || e.Key == Key.RightAlt))
        {
            Shortcuts.CurrentCanvas.InvokeKeyUp(this, e);
            e.Handled = true;
        }
        else
            Shortcuts.InvokeOnKeyUp(this, e);
        


    }



    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        MenuItem mi = (MenuItem)sender;
        var header = mi.Header as string;
        header ??= ((TextBlock)mi.Header).Text;
        if (header == "New")
        {
            AppModel.V_File_New();
        }
        else if (header == "Save")
        {
            AppModel.File_Save();
        }
        else if (header == "Save As...")
        {
            AppModel.File_SaveAs();
        }

        else if (header == "Open...")
        {
            AppModel.File_Open();
        }




        else if (header == "Undo")
        {
            ActionHistory.Undo();
        }
        else if (header == "Redo")
        {
            ActionHistory.Redo();
        }

        // TRANSFORM
        else if (header == "Flip Horizontal")
        {
            Transformer.V_FlipHorizontal(ManualAPI.SelectedLayer);
        }
        else if (header == "Flip Vertical")
        {
            Transformer.V_FlipVertical(ManualAPI.SelectedLayer);
        }

        else if (header == "Apply Scale")
        {
            Transformer.ApplyScale(ManualAPI.SelectedLayer);
        }


        else if (header == "Preferences")
        {
            AppModel.Edit_Preferences();
        }



        else if (header == "New Empty Workspace")
        {
            AppModel.project.editorsSpace.NewBlankWorkspace();
        }
        else if (header == "New Editor as Window")
        {
            AppModel.project.editorsSpace.NewEditorWindow();
        }
        else if (header == "Duplicate Workspace")
        {
            AppModel.DuplicateWorkspace();
        }
        // MANUAL PRO
        else if (header == "Save Workspace")
        {
            AppModel.SaveWorkspace();
        }
        else if (header == "Delete Workspace")
        {
            AppModel.DeleteWorkspace();
        }
        //
        else if (header == "Load Workspace...")
        {
            AppModel.LoadWorkspace();
        }
        else if (header == "Save All Workspaces")
        {
            AppModel.SaveAllWorkspace();
        }
        else if (header == "Return to Default Workspaces")
        {
            AppModel.ReturnToDefaultWorkspaces();
        }
        


        else if (header == "Save Shot")
        {
            AppModel.File_SaveShot();
        }


        // IMPORT

        else if (header == "File...")
        {
           // Mouse.OverrideCursor = Cursors.Wait;
            Cursor = Cursors.Wait;
            AppModel.File_ImportFile();
            //Mouse.OverrideCursor = null;
            Cursor = null;
        }
        else if (header == "Image Sequence...")
        {
            ManualCodec.ImportImageSequence();
        }


        else if (header == "Prompt Preset...")
        {
            AppModel.File_ImportPromptPreset();
        }
        else if (header == "Prompt...")
        {
            AppModel.File_ImportPrompt();
        }

        else if (header == "Script...")
        {
            AppModel.File_ImportScript();
        }



        // EXPORT
        else if (header == "Export Prompt Preset...")
        {
            AppModel.SavePromptPreset();
        }
        else if (header == "Export Workflow API...")
        {
            AppModel.export_workflow_api();
        }

        else if (header == "Export Prompt...")
        {
            AppModel.SavePrompt();
        }

        else if(header == "Export Layer...")
        {
            AppModel.File_ExportLayer();
        }
        else if (header == "Export Script...")
        {
            AppModel.File_ExportScript();
        }

        else if (header == "Install")
        {
            M_Window.NewShow(new W_Launcher(true), "", M_Window.TabButtonsType._X);
        }
        else if (header == "Reload UI")
        {
            PluginsManager.LoadPlugins();
        }
        else if (header == "Compile all and Reload UI")
        {
            PluginsManager.CompileAllPlugins();

        }

        else if (header == "Solid Color Layer")
        {
            ManualAPI.SelectedShot.AddSolidColorLayer();
        }
        else if (header == "Adjustment Layer")
        {
            ManualAPI.SelectedShot.AddAdjustmentLayer();
        }
        else if (header == "Text")
        {
            ManualAPI.SelectedShot.AddText();
        }


        else if (header == "Render Image")
        {
            AppModel.Animation_RenderImage();
           
        }
        else if (header == "Render Animation...")
        {
            AppModel.Animation_RenderAnimation();     
        }

        //else if (header == "Render Preview Shot")
        //{
        //    ManualAPI.Animation.RenderBufferFrames();
        //}
        else if (header == "Clear Shot Preview")
        {
            ManualAPI.SelectedShot.ClearCache();
        }
        else if (header == "Clear Preview for all Shots")
        {
            AppModel.project.ClearAllShotCaches();
        }
      

        else if (header == "Insert Keyframe")
        {
            AppModel.Animation_InsertKeyframe();
        }
        else if (header == "Delete Keyframe")
        {
            AppModel.Animation_DeleteKeyframe();
        }


        else if (header == "Check for updates")
        {
            UpdaterManager.automaticMode = false;
            UpdaterManager.CheckForUpdates();

        }
        else if (header == "Update")
        {
            UpdaterManager.automaticMode = false;
            UpdaterManager.Update();
        }


        else if (header == "Render Layer Area")
        {
            var box = ManualAPI.FindLayer("Bounding Box") as BoundingBox;
            var renderArea = RendGPU.RenderArea(box, ManualAPI.SelectedLayer);
            //AppModel.OpenRenderImage(renderArea);

            RenderView rv = new(renderArea);
            rv.Title = "Render";
            rv.ShowDialog();
        }

        else if (header == "Sign In")
        {
           AppModel.OpenLogin();
        }


        else if (header == "Manual")
        {
            WebManager.OPEN("https://manualai.art");
        }
        else if (header == "Tutorials")
        {
            WebManager.OPEN("https://manualai.art/guide");
        }
        else if (header == "Reinstall Icons")
        {
            App.InitializeShell();
        }



        else if (header == "Test")
        {
            TestButton();
        }
        else
        {
            TestsButtons(header);
        }

    }


 
    /// <summary>
    /// start
    /// </summary>
    public void FadeBlackIn()
    {
        Storyboard fadeInStoryboard = (Storyboard)FindResource("startMain");
        fadeInStoryboard.Begin(this);
    }
    public void FadeBlackIn(Action actionAtEnd)
    {
        Storyboard fadeInStoryboard = (Storyboard)FindResource("startMain");
        AnimationLibrary.DoAnimationAtEnd(this, fadeInStoryboard, actionAtEnd);
    }
    public void FadeIn()
    {
        Storyboard fadeInStoryboard = (Storyboard)FindResource("fadeIn");
        fadeInStoryboard.Begin(this);
        FadeInBlur();
    }
    public void FadeIn(Action actionAtEnd)
    {
        Storyboard fadeInStoryboard = (Storyboard)FindResource("fadeIn");
        AnimationLibrary.DoAnimationAtEnd(this, fadeInStoryboard, actionAtEnd);
        FadeInBlur();
    }

    /// <summary>
    /// loading
    /// </summary>
    public void FadeOut()
    {
        Storyboard fadeInStoryboard = (Storyboard)FindResource("loadingMain");
        fadeInStoryboard.Begin(this);
        FadeOutBlur();
    }
    public void FadeOut(Action actionAtEnd)
    {
        Storyboard fadeOutStoryboard = (Storyboard)FindResource("loadingMain");
        AnimationLibrary.DoAnimationAtEnd(this, fadeOutStoryboard, actionAtEnd);
        FadeOutBlur();
    }
   

    //--------BLUR

    public void FadeInBlur()
    {
      
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        fadeInAnimation.Completed += (s, e) =>
        {
            // Opcional: Agrega lógica que se debe ejecutar justo después de que el efecto se haya aplicado completamente
            // Quita el BlurEffect de la ventana una vez que el radio vuelve a 0 para mejorar el rendimiento
            this.Effect = null;
        };

        miBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, fadeInAnimation);
    }

    public void FadeOutBlur()
    {
        // Asigna el BlurEffect a la ventana
        this.Effect = miBlurEffect;

        DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {
            To = 37,
            Duration = TimeSpan.FromSeconds(0.3),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        fadeOutAnimation.Completed += (s, e) =>
        {
       
        };

        miBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, fadeOutAnimation);
    }





//-------------------------------------- TITLE BAR --------------------------------------\\

private void mainwindow_SourceInitialized(object sender, EventArgs e)
    {
    //  IntPtr handle = (new WindowInteropHelper(this)).Handle;
    //  HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
    }

    private void WMin_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WMax_Click(object sender, RoutedEventArgs e)
    {
          if (WindowState == WindowState.Maximized)
          {
            WindowChrome.SetWindowChrome(this, new WindowChrome()
            { 
                CaptionHeight = 25
            });

            WindowState = WindowState.Normal;
            WMax.Content = "❏";
        }
          else
          {


              WindowChrome.SetWindowChrome(mainwindow, null);
              WindowState = WindowState.Maximized;
              WMax.Content = "▢";

        }


     //  WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void WClose_Click(object sender, RoutedEventArgs e)
    {
        var unsavedShots = ManualAPI.project.ShotsCollection.Where(shot => !shot.IsSaved).ToList();
        if(AppModel.project.SavedPath != null && !AppModel.project.IsSaved)
        {
            AppModel.saveProjectBox(ManualAPI.project, saveProject, shutdown, null);
            System.Media.SystemSounds.Asterisk.Play();
        }
        else if (unsavedShots.Any())
        {
            //AppModel.saveShotBox(unsavedShots, saveAllShots, shutdown, null);
            AppModel.saveProjectBox(ManualAPI.project, saveProject, shutdown, null);
            System.Media.SystemSounds.Asterisk.Play();
        }
        else
            shutdown();
    }

    void saveProject()
    {
        var isSaved = Project.SaveProject();
        if(isSaved)
             shutdown();
    }
    void saveAllShots()
    {     
        if (AppModel.File_SaveAllShots())
              shutdown();
    }
    void shutdown()
    {
        Application.Current.Shutdown();
    }

    private void OpenSplash(object sender, MouseButtonEventArgs e)
    {
        OpenSplash();
        e.Handled = true;
    }
    public void OpenSplash()
    {
        splash.Visibility = Visibility.Visible;
        splashBg.Visibility = Visibility.Visible;
    }
     void CloseSplash(object sender, MouseButtonEventArgs e)
    {
        CloseSplash();
        e.Handled = true;
    }
    public void CloseSplash()
    {
        splash.Visibility = Visibility.Collapsed;
        splashBg.Visibility = Visibility.Collapsed;
    }


    private void Login_Click(object sender, MouseButtonEventArgs e)
    {
        if (User.Current != null)
        {
            var button = sender as FrameworkElement;

            ContextMenu contextMenu = loginBtn.FindResource("LoginContextMenu") as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }
        }
        else
            AppModel.OpenLogin();

    }

    private void TabItem_DragEnter(object sender, DragEventArgs e)
    {
        var w = (WorkspaceSingle)(((TabItem)sender).DataContext);
        EditorsSpace.instance.Current_Workspace = w;
    }



    //---------------- INFO CIRCLE SERVER
    private void info_elipse_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;
        if (button != null)
        {
            ContextMenu contextMenu = button.FindResource("ServerContextMenu") as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }
        }
    }


    private void ContextMenu_Click(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        string header = menuItem.Header.ToString();

        if (header == "Open Local Server")
        {
            Settings.instance.AIServer.Run();
        }
        else if (header == "Close Local Server")
        {
            Settings.instance.AIServer.Close();
        }
        else if (header == "Check Status")
        {
            AppModel.mainW.CheckServerStatus();
        }
        if (header == "Disconnect")
        {
            Core.Nodes.ComfyUI.Comfy.Disconnect();
        }
    }


    private void ContextMenu_Login_Click(object sender, RoutedEventArgs e)
    {
        MenuItem menuItem = sender as MenuItem;
        string header = menuItem.Header.ToString();

        if (header == "Sign In")
        {
            AppModel.OpenLogin();
        }
        else if (header == "Log Out")
        {
            Login.LogOut();
        }
    }

    private void logFooter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        AppModel.project.editorsSpace.NewEditorWindow(Core.Editors.Output);
    }

    private void Click_OpenOutput(object sender, RoutedEventArgs e)
    {
        AppModel.project.editorsSpace.NewEditorWindow(Core.Editors.Output);
    }
}





//------------------------------------------------------------------------------------------------------------------------------------------------- FOOTER
public partial class MainWindow
{
    /// <summary>
    ///0 to 1
    /// </summary>
    /// <param name="progress"></param>
    public void SetProgress(float progress)
    {
        TaskBar.Set(progress);
        if(progressGrid.Visibility != Visibility.Visible)
             progressGrid.Visibility = Visibility.Visible;

        // r_bar.Value = progress;
        r_barBehaviour.Progress = progress;
    }
    public void SetProgress(float progress, string message)
    {
        SetProgress(progress);
        SetMessage(message);
    }
    /// <summary>
    /// stop progress and also nofity
    /// </summary>
    public void FinishedProgress()
    {
        AppModel.mainW.StopProgress();
        AppModel.mainW.Notification();
    }
    public void StopProgress()
    {
        progressGrid.Visibility = Visibility.Collapsed;
        SetMessage("");
        TaskBar.Stop();
    }
    public void Notification()
    {
        if (Settings.instance.IsNotifyGen)
        {
            TaskBar.Notification();
            if(Settings.instance.IsNotifyGenSound)
                Output.DoneSound();
        }
    }

    public void SetMessage(string message)
    {
        info_text.Text = message;
    }


    private DispatcherTimer alertTimer;


    public void SetAlert(string message, OutputType type = OutputType.Message)
    {
        // Asignamos el color dependiendo del tipo de mensaje
        string resourceColor = "log_message";
        switch (type)
        {
            case OutputType.Message:
                resourceColor = "log_message";
                break;
            case OutputType.Error:
                resourceColor = "log_error";
                break;
            case OutputType.Warning:
                resourceColor = "log_warning";
                break;
            default:
                resourceColor = "log_message";
                break;
        }
        logFooterText.Foreground = AppModel.GetThemeColorBrush(resourceColor);
        if (logFooterText.Foreground == null)
            logFooterText.Foreground = "#FFD1D6DD".ToSolidColorBrush();
        logFooterText.Text = message;
        // Si el logFooter ya es visible, simplemente reinicia el temporizador
        if (logFooter.Visibility == Visibility.Visible)
        {
            ResetAlertTimer();
            return;
        }

        logFooter.Visibility = Visibility.Visible;
        logFooter.Opacity = 1;

        // Inicia la animación de respiración
        var b = AnimationLibrary.BreatheOpacity(logFooter);
        b.Begin();

        // Configurar y empezar el temporizador si es la primera vez o si fue detenido antes
        if (alertTimer == null)
        {
            alertTimer = new DispatcherTimer();
            alertTimer.Interval = TimeSpan.FromSeconds(14);
            alertTimer.Tick += (sender, args) =>
            {
                var op = AnimationLibrary.Opacity(logFooter, 0, 2);
                op.OnFinalized(() =>
                {
                    b.Stop();
                    logFooter.Visibility = Visibility.Collapsed;
                    logFooter.Opacity = 0;
                    alertTimer.Stop();
                    alertTimer = null; // Libera el temporizador
                });
                op.Begin();
            };
        }
        alertTimer.Start(); // Iniciar o reiniciar el temporizador
    }

    private void ResetAlertTimer()
    {
        if (alertTimer != null)
        {
            alertTimer.Stop();
            alertTimer.Start(); // Reiniciar el temporizador
        }
    }




    Storyboard info_breath;
    ServerStatus CurrentStatus;
    public static void ChangeServerStatusTo(ServerStatus serverStatus)
    {
        AppModel.Invoke(() => AppModel.mainW.ChangeServerStatus(serverStatus));
    }
    public void ChangeServerStatus(ServerStatus serverStatus)
    {
        if (CurrentStatus == serverStatus) return;

        AppModel.Invoke(() =>
        {
            CurrentStatus = serverStatus;

            string resource;
            string toolTipMessage;

            switch (serverStatus)
        {
            case ServerStatus.Connected:
                resource = "log_success";
                toolTipMessage = $"Connected to {Settings.instance.CurrentURL}";

                info_breath?.Stop();
                info_elipse.Opacity = 1;
                    
                    //  info_loader.Visibility = Visibility.Collapsed;
                    break;

            case ServerStatus.Disconnected:
                resource = "neutral";
                toolTipMessage = "Offline mode";
                    info_breath?.Stop();
                    Settings.instance.AIServer.IsOpened = false;
                    Settings.instance.AIServer.IsOpening = false;
                    AppModel.mainW.StopProgress();

                    // info_loader.Visibility = Visibility.Collapsed;
                    break;

            case ServerStatus.Connecting:
                resource = "log_warning";
                toolTipMessage = $"Trying to connect to {Settings.instance.CurrentURL}";
              //  info_loader.Visibility = Visibility.Visible;
                info_breath.Begin();
                break;

                case ServerStatus.Opening:
                    resource = "log_warning";
                    toolTipMessage = $"Local Server starting...";
                  //  info_loader.Visibility = Visibility.Visible;
                    info_breath.Begin();
                    Output.Log(toolTipMessage);
                    break;

                case ServerStatus.Error:
                resource = "log_error";
                toolTipMessage = "Error";
                    info_breath?.Stop();
                    Output.Log(toolTipMessage);
                    // info_loader.Visibility = Visibility.Collapsed;
                    break;
            default:
                resource = "log_warning"; // Default case, si necesitas uno.
                toolTipMessage = "Unknown status"; // Mensaje por defecto, ajusta según sea necesario.
                break;
        }

        info_elipse.ToolTip = toolTipMessage;

        var color = (SolidColorBrush)Application.Current.Resources[resource];
        info_elipse.Fill = color;

        });
    }

    public enum ServerStatus
    {
        Connected,
        Disconnected,
        Connecting,
        Opening,
        Error,
    }

    int ConnectTries = 0;
    public async void CheckServerStatus()
    {
        await CheckServer();
    }
    async Task CheckServer()
    {
        ChangeServerStatus(ServerStatus.Connecting);

        bool isConnected = false;
        ConnectTries = 0;
        while (!isConnected)
        {
            if (await Settings.instance.AIServer.IsServerReachable())
            {
                ChangeServerStatus(ServerStatus.Connected);
                isConnected = true;
            }
            else
            {
                ConnectTries++;
            }

            if (ConnectTries > 1)
            {
                ChangeServerStatus(ServerStatus.Disconnected);
                break;
            }
            

            await Task.Delay(10_000);

        }
    }
   



}
