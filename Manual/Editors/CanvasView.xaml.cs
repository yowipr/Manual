using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Manual.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Serialization;
using Manual.API;
using CommunityToolkit.Mvvm.Messaging;
using static Manual.API.ManualAPI;
using Manual.Core.Nodes;
using System.Linq;
using Manual.MUI;
using Newtonsoft.Json;
using System;
using Manual.Objects;
using System.Collections;
using System.Collections.Generic;
using Manual.Objects.UI;
using SkiaSharp;
using System.Windows.Media.Animation;
using System.Runtime.Intrinsics.Arm;
using Manual.API;
using Manual.Core.Graphics;


namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para Canvas.xaml
/// </summary>

public partial class CanvasView : UserControl
{
    public CanvasView()
    {
        InitializeComponent();
     

        context.DataContext = AppModel.project;
        Loaded += CanvasView_Loaded;
        Unloaded += CanvasView_Unloaded;

        gridPromptContext.DataContext = AppModel.project.generationManager;

        minibuttonsGrid.DataContext = AppModel.project.generationManager;


        AsignGridContextPrompt();

       
        searchBox.CloseDoAction = CloseSearch;


        Settings.instance.PropertyChanged += Instance_PropertyChanged;
        GenerationManager.OnGenerated += GenerationManager_OnGenerated;
        ToolManager.OnToolChanged += ToolManager_OnToolChanged;
    }

    private void ToolManager_OnToolChanged(Tool? oldTool, Tool newTool)
    {

        if(oldTool != null)
            oldTool.OnRefresh -= NewTool_OnRefresh;

        if (newTool != null)
        {
            NewTool_OnRefresh(newTool);
            newTool.OnRefresh += NewTool_OnRefresh;
        }
    }

    bool isNew = false;
    private void NewTool_OnRefresh(Tool value)
    {
        var b = value.CanvasView_BottomSection?.Invoke();   
        if(b != null)
        {
            bottomSectionTool.Opacity = 0;
            bottomSectionTool.Content = b;
            var op = AnimationLibrary.Opacity(bottomSectionTool, 1, 0.6);
            op.Begin();
        }
        else
        {
            var op = AnimationLibrary.Opacity(bottomSectionTool, 0, 0.6);
            op.OnFinalized(() => 
            {
                if (!isNew)
                {
                    isNew = false;
                    bottomSectionTool.Content = b;
                }
           });
            op.Begin();
        }
        isNew = true;
    }

    private void GenerationManager_OnGenerated(GeneratedImage value)
    {
        ActionHistory.DoAction.Do(AnimateGlow, 2);
    }

    private void CanvasView_Unloaded(object sender, RoutedEventArgs e)
    {
        GenerationManager.OnGenerated -= GenerationManager_OnGenerated;
        Settings.instance.PropertyChanged -= Instance_PropertyChanged;
        ToolManager.OnToolChanged -= ToolManager_OnToolChanged;
        AppModel.project.toolManager.CurrentToolSpace.OnRefresh -= NewTool_OnRefresh;

        if (enable3D)
            Rend3D.UnregisterCanvasView(this);
    }

    private void Instance_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.instance.IsTopSectionBottom))
        {
            if(DataContext is ED_CanvasView cv)
              cv.InvokeOnPropertyChanged(nameof(ED_CanvasView.IsTopSectionBottom));
        }
    }


    private void CloseSearch()
    {
        searchPopup.IsOpen = false;
    }

    void AsignGridContextPrompt()
    {

        //gridContextPrompt.AddContext(
        //    new NodeContext(n =>  n.Name == "Positive Prompt" || n.NameType == "CLIPTextEncode" &&
        //   n.Outputs.Any() && n.Outputs[0].IsConnected() && n.Outputs![0].Connection![0].Name == "positive", // is connected to ksampler positive
        //    f => f.Name == "text"),

        //    new NodeContext(n => n.Name == "Prompt", f => f.Name == "text_l" || f.Name == "text_g"),

        //    new NodeContext("Prompt", "Prompt"),

        //    new NodeContext("DynamiCrafterI2V", "prompt")
        //    );


      //  gridContextStrength.AddContext(
      //   new NodeContext("KSampler", "denoise"),

      //   new NodeContext(n => n.Name.Contains("pipe") || n.Name.Contains("Detailer"), f => f.Name == "denoise"),  

      //  new NodeContext(n => n.Name == "Vae Decode" || n.NameType == "VAEDecode" &&
      //  n.Inputs.Any() && n.Inputs[0].IsConnected() && n.Inputs![0].Connection![0].AttachedNode.FindField("denoise") != null, // is vae connected to ksampler
      //   f => f.Name == "denoise")
      //   );

      //  new NodeContext(n => n.FindField("denoise") != null,
      //f => f.Name == "denoise");

      //  gridContextStrength.DataContextChanged += GridContextPrompt_DataContextChanged;
    }

    private void GridContextPrompt_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NodeOption op2)
        {
            op2.ValueChanged -= StrengthChanged;
        }

        if (e.NewValue is NodeOption op)
        {
            op.ValueChanged += StrengthChanged;
            StrengthChanged(op.FieldValue);

        }
    }

    void StrengthChanged(object? value)
    {
        GenerationManager.Instance.SelectedPrompt.Strength = Convert.ToSingle(value);
    }

    bool enable3D = false;
    private void CanvasView_Loaded(object sender, RoutedEventArgs e)
    {

        ED_CanvasView model = DataContext as ED_CanvasView;
        model.view = this;


        EditorsSpace.instance.CanvasView = this;
        EditorsSpace.instance.UpdateGlow();

        LayoutUpdated += CanvasView_LayoutUpdated;
    }

    int loads = 0;
    private void CanvasView_LayoutUpdated(object? sender, EventArgs e)
    {
        loads++;
        if (loads > 2)
        {
            ED_CanvasView model = DataContext as ED_CanvasView;
            model.CanvasMatrix = canvas.CanvasTransform;
            model.UpdateThick();
            LayoutUpdated -= CanvasView_LayoutUpdated;
        }

    }

    private void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var selectedImage = (sender as Image)?.DataContext as WriteableBitmap;
        if (selectedImage != null)
        {
            SelectedLayer.ImageOriginalWr = selectedImage.Clone();
            SelectedLayer.ImageWr = SelectedLayer.ImageOriginalWr;
        }

    }

    private void searchPopup_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            searchPopup.IsOpen = false;
        }
    }

    private void CenterCamera_Click(object sender, MouseButtonEventArgs e)
    {
        FocusCamera();

        e.Handled = true;
    }




    public void FocusCamera()
    {
        var ed = (ED_CanvasView)DataContext;
        var oldMatrix = ed.CanvasMatrix;

        ed.FocusCamera();
        // canvas.CanvasTransform = ed.CanvasMatrix;
    }

    private void shotsTab_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(Shot)) is Shot shot)
        {
            project.OpenShot(shot);
        }
    }


    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        //var exp = sender as Expander;
        //if (exp.IsExpanded && promptBox != null)
        //{
        //    AppModel.InvokeNext(promptBox.FocusText);
        //}
    }
    private void Expander_Collapsed(object sender, RoutedEventArgs e)
    {
        var exp = sender as Expander;
        if (!exp.IsExpanded && promptBox != null)
        {
            // AppModel.InvokeNext(promptBox.FocusText);
            AppModel.InvokeNext(() => canvas.Focus());
        }
    }

    private void promptBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var ed = (ED_CanvasView)this.DataContext;
            ed.TopSection = false;
        }

        if (e.Key == Key.Enter && !Shortcuts.IsShiftPressed)
        {
            GenerationManager.Generate();
            var ed = (ED_CanvasView)this.DataContext;
            ed.TopSection = false;

            e.Handled = true;
        }

    }

    private void GeneratorButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
       // ActionHistory.DoAction.Do(AnimateGlow, 2);
    }

    void AnimateGlow()
    {
        if (!Settings.instance.EnablePreviewAnimation) return;

        var a1 = animGlow(0.5f, 1.0f);
        a1.OnFinalized(() =>
        {
            var a2 = animGlow(0.0f, 6.0f);
            a2.Begin();
        });
        a1.Begin();

    }
    Storyboard animGlow(float to, float duration)
    {
        var animatedBorder = innerGlowBorder;

        // Create the Storyboard
        Storyboard storyboard = new Storyboard();

        // Create the animations for the GradientStops
        if (Settings.instance.EnableColorfulPreview)
        {
            CreateGradientStopAnimation(storyboard, animatedBorder, 0, 0.0, 1.0, 0.0, 1.5);
            CreateGradientStopAnimation(storyboard, animatedBorder, 1, 0.33, 1.33, 0.0, 1.5);
            CreateGradientStopAnimation(storyboard, animatedBorder, 2, 0.66, 1.66, 0.0, 1.5);
            CreateGradientStopAnimation(storyboard, animatedBorder, 3, 1.0, 1.0, 0.0, 1.5);
        }

        // Create the opacity animation
        DoubleAnimation opacityAnimation = new DoubleAnimation
        {
           // From = 0,
            To = to,
            Duration = new Duration(TimeSpan.FromSeconds(duration)),
            //AutoReverse = true,
            EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }

        };

        Storyboard.SetTarget(opacityAnimation, animatedBorder);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
        storyboard.Children.Add(opacityAnimation);

        // Begin the storyboard
        //storyboard.Begin();
        return storyboard;
    }
    private void CreateGradientStopAnimation(Storyboard storyboard, Border border, int gradientStopIndex, double from, double to, double startTime, double duration)
    {
        DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime))));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + duration))));

        string gradientStopPath = $"(Border.BorderBrush).(GradientBrush.GradientStops)[{gradientStopIndex}].(GradientStop.Offset)";
        Storyboard.SetTarget(animation, border);
        Storyboard.SetTargetProperty(animation, new PropertyPath(gradientStopPath));
        storyboard.Children.Add(animation);
    }

    private void Prompt_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;
        if (button != null && button.ContextMenu != null)
        {
            // Open the context menu with the left mouse button click
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }



    private void PromptContextMenu_Click(object sender, RoutedEventArgs e)
    {
        MenuItem clickedItem = sender as MenuItem;
        string header = clickedItem.Header.ToString();
        if (header == "Open in Latent Nodes Editor")
        {
            Mouse.OverrideCursor = Cursors.Wait;
            EditorsSpace.instance.NewEditorWindow(Core.Editors.Latent_Nodes_Editor);
            Mouse.OverrideCursor = null;
        }
        else if (header == "Paste CivitAI Generation Data")
        {

            CivitaiAPI.ApplyGenerationDataToPrompt(GenerationManager.Instance.SelectedPrompt, Clipboard.GetText());
        }
        else if (header == "Duplicate")
        {
            GenerationManager.Instance.DuplicatePrompt();
        }
        else if (header == "Refresh")
        {
            GenerationManager.RefreshPromptPreset();
        }
        else if (header == "Delete")
        {
            GenerationManager.Instance.DeletePrompt();
        }
       
    }

    private void Templates_OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is MenuItemNode menuItemNode)
        {
            menuItemNode.DoAction();
        }
    }

    private void Prompt_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any(file => file.EndsWith(".prompt", StringComparison.OrdinalIgnoreCase)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Prompt_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (string file in files)
                {
                    if (file.EndsWith(".prompt", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        Prompt.V_ImportPrompt(file);
                    }
                }
            }
        }

        e.Handled = true;
    }




    private void UpdateRender(object sender, RoutedEventArgs e)
    {
        glControl.InvalidateVisual();

        ManualAPI.SelectedShot.ClearCache();
        MainCamera.ui_camera.skRender.InvalidateVisual();
        ManualAPI.SelectedShot.UpdateRender();
    }


    private void Btn3D_Click(object sender, RoutedEventArgs e)
    {
        // ((ED_CanvasView)DataContext).Is3DView = true;
        // Rend3D.Start(glControl);
        Rend3D.Start();
    }

    private void c3D_Checked(object sender, RoutedEventArgs e)
    {
        var ed = DataContext as ED_CanvasView;
        if (ed != null)
        {
            Settings.instance.IsRender3D = ed.Is3DView;

            Shot.UpdateCurrentRender();
        }
    }

    bool first3D = true;
    private void glControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue == true && !enable3D && !first3D)
        {
            Rend3D.RegisterCanvasView(this);
            enable3D = true;
        }
        first3D = false;
    }
}










public interface ICanvasMatrix
{
    public double GridWidth { get; set; }
    public double GridHeight { get; set; }

    public Matrix CanvasMatrix { get; set; }

    public bool loaded { get; set; }
    /// <summary>
    /// default camera canvas matrix state
    /// </summary>
    public void DefaultPosition();
}

public partial class ED_CanvasView : Editor, IPlugable, ICanvasMatrix
{
    //3D
    public Camera3D ViewportCamera { get; set; } = new Camera3D();
    [ObservableProperty] bool is3DView = false;



    public Guid ID = Guid.NewGuid();

    [JsonIgnore] public bool IsTopSectionBottom => Settings.instance.IsTopSectionBottom;

    [JsonIgnore] public bool loaded { get; set; }

    [JsonIgnore] public CanvasView view;

    [ObservableProperty] private bool enableGrid = true;
    partial void OnEnableGridChanged(bool value)
    {
        if (view != null)
        {
            if (value)
                view.canvas.Settings_CurrentBgGridChanged(AppModel.settings.CurrentBgGrid);
            else
                view.canvas.Settings_CurrentBgGridChanged("None");

            view?.glControl.InvalidateVisual();
        }
    }


    [ObservableProperty] private bool topSection = false;
   // [ObservableProperty] private string testing = "OU MEAN2";

    [ObservableProperty] private Matrix canvasMatrix = Matrix.Identity; //CanvasArea binding CanvasTransform to CanvasMatrix
 
    [JsonIgnore] private object _topRegion;
    [JsonIgnore]
    public object TopRegion
    {
        get { return _topRegion; }
        set
        {
            if (_topRegion != value)
            {
                _topRegion = value;
                OnPropertyChanged();
            }
        }
    }


    [ObservableProperty] private bool showOverlays = true;


    [ObservableProperty] private double gridWidth;
    [ObservableProperty] private double gridHeight;


    public ED_CanvasView()
    {
        RefreshEditors();
        PluginsManager.OnUpdatePlugins += RefreshEditors;
        Project.ShotChanged += Project_ShotChanged;
    }

    public void RefreshEditors()
    {
        TopRegion = AppModel.project.regionSpace.ed_canvas.top;
        OnCanvasMatrixChanged(CanvasMatrix);
        
    }
    


    partial void OnCanvasMatrixChanged(Matrix value)
    {
        UpdateThick();
    }

    private void Project_ShotChanged(Shot shot)
    {
        UpdateThick();
    }

    public void UpdateThick()
    {
        double v = 1 / CanvasMatrix.M11;
        WeakReferenceMessenger.Default.Send(new NotifyMessage(v));
    }



    //---------------------------- FOCUS
    public void DefaultPosition()
    {
        FocusCamera(MainCamera, 120, false);
    }
    internal void FocusCamera()
    {
        FocusCamera(MainCamera);
    }
    internal void FocusCamera(int offset)
    {
        FocusCamera(MainCamera, offset);
    }
    internal void FocusCamera(IEnumerable<CanvasObject> objects)
    {
        if (!objects.Any()) return;

        if (objects.First() is IAnimable a && !a._Animation.IsVisible())
            Animation.CurrentFrame = a._Animation.GlobalFrameStart;

        var rect = BoundingBox.GetBoundingBoxRect(objects);
        rect.Location = new Point(rect.Location.X + (rect.Width / 2), rect.Location.Y + (rect.Height / 2));
        FocusCamera(rect);


        if (Settings.instance.IsRender3D)
        {
            Shortcuts.ViewportCamera?.FocusOnObject(SelectedLayer);
            Shot.UpdateCurrentRender();
        }
    }
    internal void FocusCamera(CanvasObject canvasObject, int offset = 60, bool animate = true)
    {
        if (canvasObject == null) return;

        if (canvasObject is not Camera2D && canvasObject is IAnimable a && !a._Animation.IsVisible())
        {
            Animation.CurrentFrame = a._Animation.GlobalFrameStart;
            a._Animation.IsActuallyVisible = true;
            a._Animation.SetValue(Animation.CurrentFrame);
        }


        Rect rect = new(canvasObject.Position, new Size(canvasObject.RealScale.X, canvasObject.RealScale.Y));
        FocusCamera(rect, offset, animate);



        if (Settings.instance.IsRender3D)
        {
            Shortcuts.ViewportCamera?.FocusOnObject(canvasObject);
            Shot.UpdateCurrentRender();
        }
    }

    internal void FocusCamera(Rect rect, int offset = 60, bool animate = true)
    {

        Size gridSize = new(GridWidth, GridHeight);

        //   var oldC = CanvasMatrix;

        var camPos = rect.Location;
        var camSize = rect.Size;

        var sizeOffset = offset;

        //position
        double scaleX = ((gridSize.Width - sizeOffset) / camSize.Width);
        double scaleY = ((gridSize.Height - sizeOffset) / camSize.Height);
        double scaleFactor = camSize.Width > camSize.Height ? scaleX : scaleY;


        double newMidX = (gridSize.Width / 2) - camPos.X;
        double newMidY = (gridSize.Height / 2) - camPos.Y;


        var newT = new Point(newMidX, newMidY);
        var transform = new TranslateTransform(newT.X, newT.Y);



        //scale
        Matrix matrixNew = transform.Value;

        // Point center = new Point(newMidX, (newMidY / gridSize.Height) + midSize.Y);
        //  var cy = ((midSize.Y + gridSize.Height) - midPoint.Y) / 2;
        //   var cx = (newMidX / 2) * scaleX;
        //   var cy = (newMidY / 2) * scaleY;


        // var cx = newMidX - (camSize.X / (2 * gridSize.Width)) + camPos.X; simplified


        // 9 HORAS PA ESTA WEA
        var cx = newMidX - ((camSize.Width / gridSize.Width) / 2) + camPos.X;
        var cy = newMidY - ((camSize.Height / gridSize.Height) / 2) + camPos.Y;


        Point center = new Point(cx, cy);

        matrixNew.ScaleAt(scaleFactor, scaleFactor, center.X, center.Y);


        var oldMatrix = CanvasMatrix;
        CanvasMatrix = matrixNew;


        if(animate)
          AnimateCanvas(oldMatrix, CanvasMatrix);



    }


    public static void AnimateCanvas(Matrix fromMatrix, Matrix toMatrix)
    {
        if (Shortcuts.canvasView == null)
            return;

        var canvas = Shortcuts.canvasView.canvas;

        if (canvas.cancelAnimation != null)
        {
            canvas.cancelAnimation?.Invoke();
            canvas.cancelAnimation = null;
        }


        canvas.cancelAnimation =  AnimationLibrary.AnimateMatrix(fromMatrix, toMatrix, TimeSpan.FromSeconds(0.8),
                (value) => Shortcuts.canvasView?.Dispatcher.Invoke(() => { Shortcuts.canvasView.canvas.CanvasTransform = value; }));
     
    }

    internal static void FocusLayer(LayerBase layer)
    {
        Shortcuts.CurrentCanvasEditor?.FocusCamera(layer);
    }
    public static void FocusLayer(IEnumerable<CanvasObject> layers)
    {
        Shortcuts.CurrentCanvasEditor?.FocusCamera(layers);
    }

    internal void InvokeOnPropertyChanged(string v)
    {
        OnPropertyChanged(v);
    }
}
