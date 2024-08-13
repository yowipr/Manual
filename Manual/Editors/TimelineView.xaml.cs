using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Editing;
using Manual.API;
using Manual.Core;
using Manual.Core.Nodes;
using Manual.Editors.Displays;
using Manual.MUI;
using Manual.Objects;
using ManualToolkit.Generic;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using static Manual.API.ManualAPI;

namespace Manual.Editors;

/// <summary>
/// Lógica de interacción para TimelineView.xaml
/// </summary>

public enum FirstKeyMode
{
    None,
    Scaling,
}

public partial class TimelineView : UserControl
{
    public static readonly DependencyProperty ScaleFactorProperty =
       DependencyProperty.Register(
           nameof(ScaleFactor),
           typeof(double),
           typeof(TimelineView),
           new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.AffectsRender)
       );

    public double ScaleFactor
    {
        get { return (double)GetValue(ScaleFactorProperty); }
        set { SetValue(ScaleFactorProperty, value); }
    }



    public static readonly DependencyProperty ViewModeProperty =
       DependencyProperty.Register(
           nameof(ViewMode), typeof(TimelineMode), typeof(TimelineView),
            new FrameworkPropertyMetadata(TimelineMode.Keyframes,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnModeValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    public TimelineMode ViewMode
    {
        get { return (TimelineMode)GetValue(ViewModeProperty); }
        set { SetValue(ViewModeProperty, value); }
    }
    private static void OnModeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tv = (TimelineView)d;
        tv.OnTimelineModeChanged((TimelineMode)e.NewValue);
    }

    void OnTimelineModeChanged(TimelineMode value)
    {
        if (value == TimelineMode.Keyframes)
        {
            TimedVariablesItems.Visibility = GraphLinesItems.Visibility = Visibility.Visible;
            TrackLayersItems.Visibility = Visibility.Collapsed;
            timelineContext.SelectedLocal = ManualAPI.SelectedLayer._Animation;

            Animation.UpdateUI();
        }
        else // Tracks
        {
            TimedVariablesItems.Visibility = GraphLinesItems.Visibility = Visibility.Collapsed;
            TrackLayersItems.Visibility = Visibility.Visible;
            timelineContext.SelectedLocal = null;
        }
        canvas.ContextMenu = canvas.Resources[$"ContextMenu{ViewMode}"] as ContextMenu;

        CanvasTransform = CanvasTransform;

        //UpdateChilds();
        UpdateUI();

    }


    public static readonly DependencyProperty CanvasTransformProperty =
     DependencyProperty.Register(
        nameof(CanvasTransform),
         typeof(Matrix),
         typeof(TimelineView),
         new FrameworkPropertyMetadata(Matrix.Identity,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    public readonly MatrixTransform _transform = new MatrixTransform();
    public Matrix CanvasTransform
    {
        get { return (Matrix)GetValue(CanvasTransformProperty); }
        set
        {
            SetValue(CanvasTransformProperty, value);
            UpdateUI();
        }
    }


    void UpdateUI()
    {
        var oldMatrix = _transform.Matrix;
        _transform.Matrix = CanvasTransform;
        foreach (UIElement child in canvas.Children)
        {

            if (child is FrameworkElement frameworkElement)
            {

                if (frameworkElement.Tag is string tag)
                {

                    //UIType ignores X and Y
                    //UITypeX ignores Y
                    if (tag == "UIType")
                    {
                        // Ignorar el elemento con el Tag "UIType"
                        continue;
                    }
                    else if (tag == "UITypeX")
                    {
                        TranslateTransform translateTransform = new TranslateTransform(_transform.Matrix.OffsetX, 0);
                        child.RenderTransform = translateTransform;
                        continue;
                    }
                }
            }
            child.RenderTransform = _transform;
        }



    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

    }

    public TimelineView() //----------------------------------------------------------------------------------------------------------------------------------- CTOR TIMELINEVIEW
    {

        InitializeComponent();
        CanvasTransform = new Matrix(1, 0, 0, 1, 19, 97);


        DataContextChanged += OnDataContextChanged;

        context.DataContext = AppModel.project;

        SetBinding(CanvasTransformProperty, "CanvasMatrix");

        UpdateUI();


        Loaded += TimelineView_Loaded;

        canvas.OnCanvasTransformChanged = CanvasTransformChanged;
    }

    Point realScale;
    void CanvasTransformChanged(Matrix oldValue, Matrix newValue)
    {

        realScale = timelineContext.CanvasScale;

        // var newSX = (timelineContext.CanvasScaleX * newValue.OffsetX) / oldValue.OffsetX;
        var newSX = (realScale.X * newValue.OffsetX) / oldValue.OffsetX;
            timelineContext.CanvasScaleX = newSX;

        var newSY = (realScale.Y * newValue.OffsetY) / oldValue.OffsetY;
            timelineContext.CanvasScaleY = newSY;

        Animation.UpdateUI();
       // UpdateUI();
    }

    private void TimelineView_Loaded(object sender, RoutedEventArgs e)
    {
        OnTimelineModeChanged(ViewMode);

        //  Dispatcher.Invoke(bindSections, System.Windows.Threading.DispatcherPriority.Render);

        UpdateUI();
    }
    void bindSections()
    {
        var a = DataContext as ED_TimelineView;
        var leftBinding = new Binding("DataContext.LeftSection")
        {
            Mode = BindingMode.TwoWay,
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1),
            Converter = (IValueConverter)Application.Current.Resources["gridLengthConverter"]
        };

        leftSectionColumn.SetBinding(ColumnDefinition.WidthProperty, leftBinding);

        var rightBinding = new Binding("DataContext.RightSection")
        {
            Mode = BindingMode.TwoWay,
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1),
            Converter = (IValueConverter)Application.Current.Resources["gridLengthConverter"]
        };
        rightSectionColumn.SetBinding(ColumnDefinition.WidthProperty, rightBinding);
    }


    private void canvas_MouseEnter(object sender, MouseEventArgs e)
    {
        if(!Shortcuts.IsTextBoxFocus)
          canvas.Focus();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is ED_TimelineView timelineContext)
        {
            this.timelineContext = timelineContext;
            //   Binding binding = new Binding("DataContext.TimelineMode") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1) };
            Binding binding = new Binding("DataContext.TimelineMode") { RelativeSource = RelativeSource.Self };
            this.SetBinding(ViewModeProperty, binding);

            canvas.ContextMenu = canvas.Resources[$"ContextMenu{ViewMode}"] as ContextMenu;
        }
    }

    FirstKeyMode FirstKeyMode = FirstKeyMode.None;
    bool isPan = false;
    bool isPanned = false;
    bool isPanning = false;

    double initialScaleX = 1;
    double initialScaleY = 1;

    private void Canvas_KeyDown(object sender, KeyEventArgs e)
    {

        if (e.Key == Key.Space)
        {
            isPan = true;
            canvas.Cursor = Cursors.Hand;
        }
        else if (e.Key == Key.Right)
        {
            Animation.FrameNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            Animation.FramePrevious();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            Animation.KeyframeNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            Animation.KeyframePrevious();
            e.Handled = true;
        }

        else if (e.Key == Key.M)
        {
            ViewMode = AppModel.GetNextEnumValue(ViewMode);
        }


        //----------- KEYFRAMES
        if (ViewMode == TimelineMode.Keyframes)
        {
            //FIRSTKEYS
            if (Shortcuts.IsKeyDown(e, Key.S))
            {
                if (FirstKeyMode == FirstKeyMode.Scaling)
                    FirstKeyMode = FirstKeyMode.None;

                FirstKeyMode = FirstKeyMode.Scaling;
                initialMousePosition = Mouse.GetPosition(canvas);
                initialMousePositionCanvas = _transform.Inverse.Transform(initialMousePosition);

                SetInitialKeyframesAndHandlePosition();
            }
           

            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.Key == Key.C)
                {
                    CopyKeyframes();
                }
                else if (e.Key == Key.V)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        PasteInKeyframe(pasteClipboardImage: true);
                    }
                    else
                    {
                        PasteInKeyframe();
                    }
                }

            }


            if (e.Key == Key.Delete)
            {
                Animation.DeleteSelectedKeyframes();
            }
            else if (e.Key == Key.A)
            {
                if (SelectedKeyframes.Count > 0)
                    Animation.ClearSelectedKeyframes();
                else
                    Animation.SelectAllKeyframes();
            }

            else if (e.Key == Key.I)
            {
                OpenContextMenu("ContextMenuKeyframes_Interpolation");
            }


        }



        //------------- TRACKS
        if (ViewMode == TimelineMode.Tracks)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.K)
                {
                    AnimationManager.Knife();
                }
            }
            else if (Shortcuts.IsKeyDown(e, Key.Decimal))
            {
                Shortcuts.CurrentCanvasEditor.FocusCamera(SelectedLayers);
            }

            if (e.Key == Key.Delete)
            {
                SelectedLayers.Delete();
            }
            else if (e.Key == Key.A)
            {
                SelectedLayers.SwapSelection();
            }
            else if (e.Key == Key.F12 || e.Key == Key.NumPad0)
            {
                if (SelectedLayer is ShotLayer shotL)
                {
                    if (!shotL._Animation.IsVisible(shotL._Animation.CurrentFrame))
                        shotL._Animation.CurrentFrame = shotL._Animation.FrameStart;

                    SelectedShot = shotL.ShotRef;
                }
                else
                { 
                    ViewMode = AppModel.GetNextEnumValue(ViewMode);
                }
            }

        }


        if (e.Key == Key.Escape)
        {
            CancelFirstKey();
        }
    }


    void CancelFirstKey()
    {
        if (FirstKeyMode == FirstKeyMode.Scaling)
        {
            foreach (var keyframe in SelectedKeyframes)
            {
                PixelPoint newPos = initialKeyframesPositions[keyframe];
                keyframe.Frame = newPos.X;

                keyframe.LeftHandleFrame = initialHandlePositions[keyframe].X.ToInt32();
                keyframe.RightHandleFrame = initialHandlePositions[keyframe].Z.ToInt32();

            }
        }


        if (FirstKeyMode != FirstKeyMode.None)
            FirstKeyMode = FirstKeyMode.None;
    }



    void CopyKeyframes()
    {
        ManualClipboard.Copy(SelectedKeyframes.ToList());
    }
    void PasteInKeyframe(bool pasteClipboardImage = false)
    {
        var pasted = ManualClipboard.Paste();

        if (pasteClipboardImage)
        {
            Keyframe.Insert(SelectedLayer, "Image", CurrentFrame, ManualClipboard.GetImage().ToSKBitmap());
        }
        else
        {
            if (pasted is List<Keyframe> pastedKeyframes)
            {
                var firstFrame = pastedKeyframes.OrderBy(k => k.Frame).FirstOrDefault().Frame;

                SelectedKeyframes.Clear();
                foreach (var keyframe in pastedKeyframes)
                {
                    keyframe.Frame = keyframe.Frame - firstFrame + SelectedLayer._Animation.GetActualFrame(CurrentFrame);
                    Keyframe.Insert(SelectedLayer, keyframe);
                    SelectedKeyframes.Add(keyframe);
                    keyframe.AttachedTimedVariable.IsSelected = true;
                }
            }
        }


        Shot.UpdateCurrentRender();
        Animation.UpdateUI();
    }






    bool dragging = false;
    bool draggingPath = false;
    Point initialMousePosition;
    int initialFrame;


    ED_TimelineView timelineContext;
    Point initialMousePositionCanvas;
    Matrix initialCanvasTransform;
    private Dictionary<Keyframe, PixelPoint> initialKeyframesPositions = new Dictionary<Keyframe, PixelPoint>();
    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        canvas.Focus();

        if (FirstKeyMode != FirstKeyMode.None)
            return;


        if (e.ChangedButton == MouseButton.Left)
        {
            Point mousePosition = e.GetPosition(canvas);
            if (isPan)
            {
                //CanvasAreaControl.SetLowQuality(canvas);

                initialMousePositionCanvas = _transform.Inverse.Transform(Mouse.GetPosition(canvas));
                initialMousePosition = mousePosition;
                initialCanvasTransform = CanvasTransform;

                initialScaleX = timelineContext.CanvasScaleX;
                initialScaleY = timelineContext.CanvasScaleY;

                isPanning = true;
                isPanned = true;
                //  initialCanvasPosition = new Point(timelineContext.GlobalCanvasTransformX, timelineContext.GlobalCanvasTransformY);
                return;
            }

            var frameworkElement = CanvasAreaControl.HitTest(canvas, mousePosition);
            if (frameworkElement != null)
            {
                if (frameworkElement.Name == "pathRect" || mousePosition.Y < pathRect.Height) //jump
                {
                    initialMousePosition = mousePosition;
                    initialFrame = (int)Math.Round(mousePosition.X);
                    draggingPath = true;

                //   var s = AnimationLibrary.Scale(circlePath, 2, 2, 0.6, 1, 1);
               //     s.Begin();

                    pathRect.CaptureMouse();
                   
                    if (Animation.IsPlaying)
                    {
                        Animation.Pause();
                        dragPathPlaying = true;
                    }

                    DragPath();

                    return;
                }
                else if (AppModel.FindAncestorByName("pathGrid", frameworkElement) is Grid) //drag path
                {
                    initialMousePosition = mousePosition;
                    initialFrame = (int)Math.Round(mousePosition.X);
                    //  initialFrame = CurrentFrame;
                    draggingPath = true;
                    return;
                }
                else
                {
                    switch (ViewMode)
                    {
                        case TimelineMode.Keyframes:
                            ClickKeyframeMode(frameworkElement, mousePosition);
                            break;
                        case TimelineMode.Tracks:
                            ClickTrackMode(frameworkElement, mousePosition);
                            break;
                        default:
                            break;
                    }


                }


            }
            else // SELECT
            {
                selector.StartSelect(_transform);
            }



        }
    }

    void ClickKeyframeMode(FrameworkElement frameworkElement, Point mousePosition)
    {

        // Buscar el primer ancestro KeyframeView
        Keyframe keyframe = AppModel.FindAncestorByDataContext<Keyframe>(frameworkElement);
        if (keyframe == null)
        {
            //Animation.ClearSelectedKeyframes();
            selector.StartSelect(_transform);
            return;
        }


        //if keyframe not null:

        //SELECT MANY
        if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) && SelectedKeyframes.Count != 0)
        {
            if (!SelectedKeyframes.Contains(keyframe))
                SelectedKeyframes.Add(keyframe);
            else
                SelectedKeyframes.Remove(keyframe);
        }
        // DUPLICATE
        else if (Shortcuts.IsAltPressed && SelectedKeyframes.Count != 0)
        {
            Animation.DuplicateSelectedKeyframes();

            initialMousePosition = mousePosition;
            dragging = true;
        }
        //DESELECT ALL
        else
        {
            if (SelectedKeyframes.Count == 0 || !SelectedKeyframes.Contains(keyframe))
            {
                Animation.ClearSelectedKeyframes();
                SelectedKeyframes.Add(keyframe);
                SelectedKeyframe = keyframe;
            }

            initialMousePosition = mousePosition;
            dragging = true;

        }
        SetInitialKeyframesPosition();


    }

    enum TrackParts
    {
        All,
        Start,
        End,
    }
    TrackParts CurrentTrackPart;
    // int initialTrackIndex;


    Rectangle _deltaRec;
    void ClickTrackMode(FrameworkElement frameworkElement, Point mousePosition)
    {
        var layer = AppModel.FindAncestorByDataContext<IAnimable>(frameworkElement);

        if (layer == null)
        {
            //Animation.ClearSelectedKeyframes();
            selector.StartSelect(_transform);
            return;
        }


        if (layer != null)
        {
            var anim = layer._Animation;
            initialMousePosition = mousePosition;

            if (Shortcuts.IsShiftPressed && SelectedLayers.Count != 0)
            {

                var layerBase = (LayerBase)layer;

                if (!SelectedLayers.Contains(layerBase))
                    SelectedLayers.Add(layerBase);
                else
                    SelectedLayers.Remove(layerBase);

            }
            else if (Shortcuts.IsAltPressed && SelectedLayers.Count != 0)
            {
                var layerBase = (LayerBase)layer;
                AnimationManager.DuplicateTrack(layerBase);
                SelectedLayers.SelectSingleItem(layerBase);
            }
            else
            {
                if (AppModel.FindAncestorContainsInName("dragLimit", frameworkElement) is Rectangle rec)
                {
                    if (rec.Name == "dragLimitStart")
                    {
                        CurrentTrackPart = TrackParts.Start;
                    }
                    else // dragLimitEnd
                    {
                        CurrentTrackPart = TrackParts.End;
                    }
                    _deltaRec = rec;
                    rec.CaptureMouse();
                }
                else // drag entire track
                {
                    CurrentTrackPart = TrackParts.All;

                }

                if (SelectedLayers.Count <= 1 || !SelectedLayers.Contains(layer))
                {
                    var l = (LayerBase)layer;
                    SelectedLayers.SelectSingleItem(l);
                    SelectedLayer = l;
                }


                SetInitialTracksPosition();
                dragging = true;
            }


        }
        else // selection null
        {
            SelectedLayers.Clear();

        }
    }
    private Dictionary<LayerBase, System.Numerics.Vector4> initialTracksPositions = new(); // (StartOffset, TrackIndex)
    private void SetInitialTracksPosition()
    {
        initialTracksPositions.Clear();
        foreach (LayerBase layer in SelectedLayers)
        {
            initialTracksPositions[layer] = new System.Numerics.Vector4(layer._Animation.StartOffset, -layer._Animation.TrackIndex,
                                                                         layer._Animation.FrameStart, layer._Animation.FrameEnd);
        }
    }



    //-------------------------------------------------------------------------------
    private void SetInitialKeyframesPosition()
    {
        initialKeyframesPositions.Clear();
        foreach (Keyframe keyframe in SelectedKeyframes)
        {
            initialKeyframesPositions[keyframe] = keyframe.GetPosition();
        }
    }

    private Dictionary<Keyframe, System.Numerics.Vector4> initialHandlePositions = new();
    private void SetInitialKeyframesAndHandlePosition()
    {
        initialKeyframesPositions.Clear();
        initialHandlePositions.Clear();
        foreach (Keyframe keyframe in SelectedKeyframes)
        {
            initialKeyframesPositions[keyframe] = keyframe.GetPosition();

            initialHandlePositions[keyframe] = new System.Numerics.Vector4(keyframe.LeftHandleFrame, keyframe.LeftHandleValue,
                                                                         keyframe.RightHandleFrame, keyframe.RightHandleValue);
        }
    }



    private bool _awaitingRender = false;
    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        _awaitingRender = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;

    }
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {

            if (_awaitingRender)
                return;


            if (selector.isSelecting)
            {
                selector.DoSelect(_transform);
            }

            if (isPanning)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    DoZoom();
                else
                    DoPan();

                return;
            }
            else if (draggingPath)
            {
                DragPath();
            }
            else
            {
                switch (ViewMode)
                {
                    case TimelineMode.Keyframes:
                        MoveKeyframeMode();
                        break;
                    case TimelineMode.Tracks:
                        MoveTrackMode();
                        break;
                    default:
                        break;
                }
            }

            _awaitingRender = true;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            Shot.UpdateCurrentRender();
        }
        else
        {
            switch (ViewMode)
            {
                case TimelineMode.Keyframes:
                    MoveKeyframeModeKey();
                    break;
                case TimelineMode.Tracks:

                    break;
                default:
                    break;
            }
        }



    }

    void MoveKeyframeModeKey()
    {
        //SCALE
        if (FirstKeyMode == FirstKeyMode.Scaling)
        {

            var pinX = SelectedLayer._Animation.CurrentFrame;
            double initialMouseX = initialMousePosition.X; //deltaMousePoint().X; 
            double currentMouseX = Mouse.GetPosition(canvas).X;


            double distanceFromPinToCursor = currentMouseX - pinX;
            double initialDistanceFromPinToCursor = initialMouseX - pinX;

            double scaleFactor = distanceFromPinToCursor / initialDistanceFromPinToCursor;

            foreach (var keyframe in SelectedKeyframes)
            {
                keyframe.UpdateUI = false;

                //  double initialKeyframeDistanceFromPin = initialKeyframesPositions[keyframe].X - pinX;
                //  double newKeyframeX = pinX + (initialKeyframeDistanceFromPin * scaleFactor);

                //KEYFRAME FRAME
                var initialFrame = initialKeyframesPositions[keyframe].X;
                double newKeyframeX = CalculateScaling(initialFrame, pinX, scaleFactor);
                if (!double.IsInfinity(newKeyframeX) && !double.IsNaN(newKeyframeX))
                    keyframe.Frame = newKeyframeX.ToInt32();


                //HANDLERS
                //  var handle1x = (initialHandlePositions[keyframe].X) - pinX;
                //   var handle2x = (initialHandlePositions[keyframe].Z) - pinX;

                //   double newHandle1X = (pinX + (handle1x * scaleFactor));
                //   double newHandle2X = (pinX + (handle2x * scaleFactor));



                var initial1 = initialHandlePositions[keyframe].X + initialFrame;
                double newHandle1X = CalculateScaling(initial1, pinX, scaleFactor);

                if (!double.IsInfinity(newHandle1X) && !double.IsNaN(newHandle1X))
                    keyframe.LeftHandleFrame = (newHandle1X - newKeyframeX).ToInt32();



                var initial2 = initialHandlePositions[keyframe].Z + initialFrame;
                double newHandle2X = CalculateScaling(initial2, pinX, scaleFactor);

                if (!double.IsInfinity(newHandle2X) && !double.IsNaN(newHandle2X))
                    keyframe.RightHandleFrame = (newHandle2X - newKeyframeX).ToInt32();


                keyframe.InvalidateUI();
            }

        }
    }

    double CalculateScaling(double initial, int pinX, double scaleFactor)
    {
        double initialKeyframeDistanceFromPin = initial - pinX;
        double newKeyframeX = pinX + (initialKeyframeDistanceFromPin * scaleFactor);

        return newKeyframeX;
    }

    void MoveKeyframeMode()
    {
        if (dragging && SelectedKeyframes.Count != 0)
        {
            bool isAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            foreach (var keyframe in SelectedKeyframes)
            {
                var initPos = initialKeyframesPositions[keyframe];
                var delta = deltaMousePoint();
                PixelPoint newPos = initPos + delta;
                if (!isAlt)
                    keyframe.Set(newPos);
                else
                    keyframe.Set(newPos.X, initPos.Y);

                var timed = keyframe.AttachedTimedVariable;
                Animation.NotifyActionStartChanging(timed.CurrentTarget, timed.PropertyName, keyframe.Frame);          
            }

            if (Animation.IsPlaying)
            {

                foreach (TimedVariable timedVariable in Animation.GetSelectedTimedVariables())
                    timedVariable.UpdateGraph();
            }

        }

    }
    void MoveTrackMode()
    {
        if (dragging)
        {
            bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            var _deltaMousePoint = deltaMousePoint();

            foreach (var layer in SelectedLayers)
            {

                var layerA = layer._Animation;
                switch (CurrentTrackPart)
                {

                    case TrackParts.All:
                        var newY = -initialTracksPositions[layer].Y + _deltaMousePoint.Y;
                        int newY2 = (initialTracksPositions[layer].Y + (newY / TrackHeightToYConverter.TrackHeight)).ToInt();
                        layerA.TrackIndex = -newY2;


                        int newX = initialTracksPositions[layer].X.ToInt() + _deltaMousePoint.X;

                        if (isCtrl)
                        {
                            layerA.StartOffset = newX;
                            continue;
                        }

                        var newglStart = layerA.FrameStart + newX;
                        var newglEnd = layerA.FrameEnd + newX;
                        var snapC = SnapTrack(layer, newglStart, newglEnd);


                        //SNAP
                        if (snapC.isSnap)
                        {
                            if (snapC.IsStart)
                                layerA.StartOffset = snapC.Value - layerA.FrameStart;
                            else
                                layerA.StartOffset = snapC.Value - layerA.FrameStart - layerA.FrameDuration - 1;
                        }     
                        else
                            layerA.StartOffset = newX;

                        break;





                    case TrackParts.Start:
                        int start_newX = initialTracksPositions[layer].Z.ToInt() + _deltaMousePoint.X;
                        var global_start_newX = layerA.FrameToGlobal(start_newX);
                        if (isCtrl)
                        {
                            layerA.StartOffset = start_newX;
                            continue;
                        }

                        var snapA = SnapTrack(layer, global_start_newX);
                        if (snapA.isSnap)
                            layerA.FrameStart = layerA.FrameToLocal(snapA.Value);
                        else
                            layerA.FrameStart = start_newX;

                        break;




                    case TrackParts.End:
                        var end_newX = initialTracksPositions[layer].W.ToInt() + _deltaMousePoint.X;
                        var global_end_newX = layerA.FrameToGlobal(end_newX);

                        if (isCtrl)
                        {
                            layerA.StartOffset = end_newX;
                            continue;
                        }

                        var snapB = SnapTrack(layer, global_end_newX);
                        if (snapB.isSnap)
                            layerA.FrameEnd = layerA.FrameToLocal(snapB.Value) - 1;
                        else
                            layerA.FrameEnd = end_newX - 1;
                        break;
                    default:
                        break;

                }

            }

        }
    }

  //  const int SNAP_THRESHOLD = 3; // Define un umbral para el ajuste.
    public struct FramePoint
    {
        public int Value; // El valor de GlobalFrameStart o GlobalFrameEnd.
        public LayerBase Layer; // El layer al que pertenece este punto.
        public bool IsStart; // Verdadero si el punto es GlobalFrameStart, falso si es GlobalFrameEnd.
        public bool isSnap;
        public FramePoint()
        {
                
        }
        public FramePoint(int value, bool isStart)
        {
            Value = value;
            IsStart = isStart;
        }
    }


    //----------------------------------------------------------------------------------------------------------------------------------------- SNAP TRACK
    /// <summary>
    /// returns the global frame;
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="global_newX"></param>
    /// <param name="default_newX"></param>
    /// <param name="global_newX_end"></param>
    /// <returns></returns>
    FramePoint SnapTrack(LayerBase layer, int global_newX, int? global_newX_end = null)
    {

        List<FramePoint> points = new List<FramePoint>();
        foreach (var l in layers)
        {
            if (l != layer) // Excluye el layer que estás arrastrando.
            {
                points.Add(new FramePoint { Value = l._Animation.GlobalFrameStart, Layer = l, IsStart = true });
                points.Add(new FramePoint { Value = l._Animation.GlobalFrameEnd, Layer = l, IsStart = false });
            }
        }
        //current frame, and start end shot
        points.Add(new FramePoint { Value = Animation.CurrentFrame});
        points.Add(new FramePoint { Value = Animation.FrameStart});
        points.Add(new FramePoint { Value = Animation.FrameEnd});


        int minDistance = int.MaxValue;
        FramePoint closestPoint = new FramePoint();
        bool adjustToStart = true; // Determina si ajustas al punto de inicio o fin del layer que arrastras.

        int[] po;

        if (global_newX_end != null)
            po = [global_newX, global_newX_end.Value];
        else
            po = [global_newX];
        

        // Verifica ambos puntos del layer que arrastras: GlobalFrameStart y GlobalFrameEnd.
        foreach (int checkPoint in po)
        {

            foreach (var p in points)
            {
                if(p.Layer != null && p.Layer.Name == "Square")
                {

                }

                int distance = Math.Abs(p.Value - checkPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = p;
                    closestPoint.IsStart = po[0] == checkPoint;
                }
            }
        }

        var canvasX = timelineContext.CanvasScaleX;
        int SNAP_THRESHOLD = TimelineCanvas.GetSteps(canvasX, 10, 2);

        //SNAP
        if (minDistance < SNAP_THRESHOLD)//----------- ON SNAP
        {
            closestPoint.isSnap = true;
            return closestPoint;
        }
        else
        {
            return new FramePoint() { isSnap = false};
        }


    }





    bool dragPathPlaying = false;
    private void DragPath()
    {

        double newFrame = initialFrame + deltaCanvasMouseFrame();
        newFrame /= timelineContext.CanvasScaleX;

        int value = (int)Math.Round(newFrame);
        if (ViewMode == TimelineMode.Tracks)
            CurrentFrame = value;
        else if (SelectedLayer != null)
            SelectedLayer.ShotParent.Animation.CurrentFrame = value + SelectedLayer._Animation.StartOffset; //SelectedLayer._Animation.CurrentFrame = value;


    }
    private double deltaCanvasMouseFrame()
    {
        Point mousePosition = Mouse.GetPosition(canvas);
        Vector delta = Point.Subtract(mousePosition, initialMousePosition);
        return delta.X - timelineContext.GlobalCanvasTransformX;
    }
    private PixelPoint deltaMousePoint()
    {
        Point mousePosition = Mouse.GetPosition(canvas);
        Vector delta = Point.Subtract(mousePosition, initialMousePosition);

        return PixelPoint.Divide(delta, timelineContext.CanvasScale);
    }

    private void canvasArea_MouseUp(object sender, MouseButtonEventArgs e)
    {


        //end firskeymode
        if (FirstKeyMode != FirstKeyMode.None)
        {
            FirstKeyMode = FirstKeyMode.None;

        }
        else
        {
            //CanvasAreaControl.SetNormalQuality(canvas);

            isPanning = false;

            if (!isPan)
                isPanned = false;


            if (selector.isSelecting)
            {
                var rect = selector.EndSelect(_transform);
                EndSelect(rect);
            }

            switch (ViewMode)
            {
                case TimelineMode.Keyframes:
                    keyframeMouseUp();
                    break;
                case TimelineMode.Tracks:
                    tracksMouseUp();
                    break;
                default:
                    break;
            }


            dragging = false;


            if (draggingPath)
            {
                if (dragPathPlaying)
                {
                    dragPathPlaying = false;
                    Animation.Play();
                }

                pathRect.ReleaseMouseCapture();
              //  var s = AnimationLibrary.Scale(circlePath, 1, 1, 0.2);
              //  s.Begin();
            }

            if (_deltaRec != null)
            {
                _deltaRec.ReleaseMouseCapture();
                _deltaRec = null;
            }

            draggingPath = false;



            if (selector.isSelecting)
                selector.EndSelect(_transform);
        }


        //dispose
        initialHandlePositions.Clear();
        initialKeyframesPositions.Clear();
        initialTracksPositions.Clear();


    }


    void keyframeMouseUp()
    {
        if (dragging)
        {
            var selectedKeyframes = SelectedKeyframes.ToList();
            foreach (var keyframe in selectedKeyframes)
            {
                var tv = SelectedLayer._Animation.GetTimedVariable(keyframe.AttachedTimedVariable.PropertyName);
                var oldkey = tv?.Keyframes.FirstOrDefault(k => k.Frame == keyframe.Frame && k != keyframe);

                if (oldkey != null)
                    SelectedLayer._Animation.RemoveKeyframe(oldkey);
            }

            var uniqueTimedVariables = SelectedKeyframes
           .Select(k => k.AttachedTimedVariable)
           .Distinct()
           .ToList();

            foreach (var timed in uniqueTimedVariables)
            {
                timed.ReorderKeyframes();
            }
        }
    }


    void EndSelect(Rect s)
    {
        var loc = PixelPoint.Divide(s.Location, timelineContext.CanvasScale);
        var scale = PixelPoint.Divide(s.Size, timelineContext.CanvasScale);

        var selection = new Rect(loc.ToPoint(), scale.ToSize());
        //  selection2.Location = new Point(selection.Location.X / canvasScale.X,  selection.Location.Y / canvasScale.Y);
        //  selection2.Scale(canvasScale.X, canvasScale.Y);
        if (ViewMode == TimelineMode.Keyframes)
        {

            List<Keyframe> keysIntersect = new();
            foreach (var key in SelectedLayer._Animation.GetAllKeyframes(onlyUIVisibles: true))
            {
                var p = key.ToPoint();

                if (selection.Contains(p))
                    keysIntersect.Add(key);

            }

            bool clear = !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            SelectedKeyframes.Select(keysIntersect, clear);

        }


        else if (ViewMode == TimelineMode.Tracks)
        {
            List<LayerBase> tracksIntersect = new();
            foreach (var track in Animation.GetAllTracks())
            {
                Rect rect = track.ToRect();

                if (selection.IntersectsWith(rect))
                    tracksIntersect.Add(track.AttachedLayer);

            }

            bool clear = !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            SelectedLayers.Select(tracksIntersect, clear);
        }
    }



    void tracksMouseUp()
    {
        if (dragging)
        {
            //do something
        }
    }



    private void canvas_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            canvas.Cursor = null;

            isPan = false;

            if (!isPanned && Shortcuts.CurrentCanvas == null)
                Animation.Play();
        }
    }




    //------------------------------------------------------------- CANVAS PAN AND ZOOM ---------------------------------------------------------------------------\\

    void DoPan()
    {
        Point mousePosition = _transform.Inverse.Transform(Mouse.GetPosition(canvas));
        Vector delta = Point.Subtract(mousePosition, initialMousePositionCanvas);
        var translate = new TranslateTransform(delta.X, delta.Y);
        CanvasTransform = translate.Value * CanvasTransform;

    }

    bool limitScaleX(double value)
    {
        return value > 0.00251 && value < 218;
    }
    bool limitScaleY(double value)
    {

        double limitY;
        switch (ViewMode)
        {
            case TimelineMode.Keyframes:
                limitY = 0.0251;
                break;
            case TimelineMode.Tracks:
                limitY = 0.2;
                break;
            default:
                limitY = 0.0251;
                break;
        }

        return value > limitY && value < 218;

    }





    void DoZoom()
    {
        Point mousePosition = Mouse.GetPosition(canvas);
        Vector delta = Point.Subtract(mousePosition, initialMousePosition);

        Zoom(delta);
    

    }

    void Zoom(Vector delta)
    {
        double scaleFactor = 0.01 * ScaleFactor * timelineContext.CanvasScaleX; // Factor de escala
        if (scaleFactor <= 0.4)
            delta *= scaleFactor;


        double newScaleX = initialScaleX + delta.X;

        bool limitX = limitScaleX(newScaleX);
        if (limitX)
            timelineContext.CanvasScaleX = newScaleX;

        double newScaleY = initialScaleY + delta.Y * 0.1;

        if (limitScaleY(newScaleY))
            timelineContext.CanvasScaleY = newScaleY;



        // NO BORRAR, HUELE A HISTORIA

        // matrix.OffsetX = matrix.OffsetX + initialMousePositionCanvas.X + delta.X - initialMousePosition.X - matrix.OffsetX;
        // matrix.OffsetX = -initialMousePositionCanvas.X + initialMousePosition.X + (-initialMousePositionCanvas.X * delta.X);
        // matrix.OffsetX = -initialMousePositionCanvas.X + initialMousePosition.X + (-initialMousePosition.X * delta.X);
        // matrix.OffsetX = (-initialMousePositionCanvas.X + initialMousePosition.X) - (newScaleX * delta.X); //nice
        // matrix.OffsetX = initialCanvasTransform.OffsetX - delta.X * scaleFactor * (mousePosition.X - initialCanvasTransform.OffsetX) / initialScaleX; //shat
        //  matrix.OffsetX = (-initialMousePositionCanvas.X + initialMousePosition.X) - ( (initialMousePosition.X / initialScaleX) * delta.X); // nicee
        //  matrix.OffsetX = initialCanvasTransform.OffsetX - ( (initialMousePosition.X * delta.X) / initialScaleX); // niceee


        //Position
        if (limitX)
        {
            Matrix matrix = initialCanvasTransform;  
            matrix.OffsetX = initialCanvasTransform.OffsetX - ((initialMousePositionCanvas.X * delta.X) / initialScaleX); //NICEEEEEE 3 HORAS PA ESTA MAMADERA
                                                                                                                          //  matrix.OffsetY = initialCanvasTransform.OffsetY - ((initialMousePositionCanvas.Y) / initialScaleY);
            CanvasTransform = matrix;
        }

        // update graph
        Animation.UpdateUI();
        //graph updates in a valueconverter

    }



    // ------------------ DRAG DROP
    LayerBase dropLayer;
    int dropDeltaTrackOffset = 0;
    private void canvas_DragEnter(object sender, DragEventArgs e)
    {
        if(ViewMode == TimelineMode.Tracks && SelectedLayer != dropLayer)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var file = files[0];
                if (file.IsFormat("png"))
                {
                    dropLayer = Layer.New(file);
                    dropLayer._Animation.StartOffset = dropDeltaTrackOffset;
                }
            }
            else if (e.Data.GetData(typeof(Shot)) is Shot shot)
            {
                dropLayer = new ShotLayer(shot);
                AddLayerBase(dropLayer, false, false);
            }
         
        }
    }

    private void canvas_DragLeave(object sender, DragEventArgs e)
    {
        if (dropLayer != null && ViewMode == TimelineMode.Tracks)
        {
            SelectedShot.RemoveLayer(dropLayer);
            dropLayer = null;
        }
    }

    private void canvas_DragOver(object sender, DragEventArgs e)
    {
        if (ViewMode == TimelineMode.Tracks)
        {
            Point mousePosition = e.GetPosition(canvas);
            var delta = PixelPoint.Divide(mousePosition, timelineContext.CanvasScale);

            if(dropLayer != null)
                dropLayer._Animation.StartOffset = dropDeltaTrackOffset = delta.X; 
        //    DragTrack(delta.X, delta.Y);
        }
    }

    //------------------------------------------------------------------------------------------------- TIMELINE DROP
    private void canvas_Drop(object sender, DragEventArgs e)
    {    
            if (ViewMode == TimelineMode.Keyframes)
                DropKeyframes(e);
            else if (ViewMode == TimelineMode.Tracks)
                DropTracks(e);
    }

    void DropKeyframes(DragEventArgs e)
    {
     
        int frame = SelectedLayer._Animation.GetActualFrame(CurrentFrame);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var file = files[0];

            if (file.IsFormat("png"))
            {
                var img = Renderizainador.ImageFromFile(file);
                Keyframe.Insert(SelectedLayer, "Image", frame, img.ToSKBitmap());
            }
        }
        else if (e.Data.GetDataPresent(typeof(object)))
        {
            var obj = e.Data.GetData(typeof(object));
            if(obj is LayerBase layer)
               Keyframe.Insert(SelectedLayer, "Image", frame, layer.Image);
        }
    }
    void DropTracks(DragEventArgs e)
    {
        dropLayer = null;

        return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var file = files[0];
            if (file.IsFormat("png"))
            {
                SelectedShot.RemoveLayer();

                Layer l = Layer.New(file);
                SelectedShot.generatedImages.Add(l.ImageWr);
                l._Animation.StartOffset = dropDeltaTrackOffset;

            }
        }

        else if (e.Data.GetData(typeof(Shot)) is Shot shot)
        {
            AddLayerBase(new ShotLayer(shot));
        }


    }




    //------------------------------------------------------------------------------------------------------------------------ MENUITEM RIGHT CLICK

    private void RightClickMenu(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;
        if (header == "Copy")
        {
            CopyKeyframes();
        }
        else if (header == "Paste")
        {
            PasteInKeyframe();
        }
        else if (header == "Insert Clipboard Image")
        {
            PasteInKeyframe(pasteClipboardImage: true);
        }
    }

    private void RightClickMenu_Interpolation(object sender, RoutedEventArgs e) // T
    {
        var s = sender as MenuItem;
        string header = (string)s.Header;

        TemporalInterpolation interpolation = TemporalInterpolation.Bezier;
        if (header == "Constant")
        {
            interpolation = TemporalInterpolation.Constant;
        }
        else if (header == "Linear")
        {
            interpolation = TemporalInterpolation.Linear;
        }
        else if (header == "Bezier")
        {
            interpolation = TemporalInterpolation.Bezier;
        }





        foreach (var keyframe in SelectedKeyframes)
        {
            keyframe.Interpolation = interpolation;
        }


        if (Animation.IsPlaying)
        {
            foreach (TimedVariable timedVariable in Animation.GetSelectedTimedVariables())
                timedVariable.UpdateGraph();
        }

    }



    private void AssignBakeKeyframes_Click(object sender, RoutedEventArgs e)
    {
        // Lógica para asignar como Bake Keyframes
        SelectedLayer.AddBakeKeyframe(SelectedKeyframes);
    }

    private void RemoveBakeKeyframes_Click(object sender, RoutedEventArgs e)
    {
        // Lógica para remover de los Bake Keyframes
        SelectedLayer.RemoveBakeKeyframe(SelectedKeyframes);
    }

    private void DeleteKeyframe_Click(object sender, RoutedEventArgs e)
    {
        // Lógica para eliminar el Keyframe
        foreach (var keyframe in SelectedKeyframes.ToList())
        {
            keyframe.Delete();
        }

    }


    //--------------------------------------- IN TRACKS
    private void DeleteTrack_Click(object sender, RoutedEventArgs e)
    {
        SelectedLayers.Delete();
    }
    private void EditKeyframes_Click(object sender, RoutedEventArgs e)
    {
        ViewMode = TimelineMode.Keyframes;
    }








    void OpenContextMenu(string contextMenuKeyName)
    {
        var contextMenu = canvas.FindResource(contextMenuKeyName) as ContextMenu;
        OpenContextMenu(contextMenu);
    }
    void OpenContextMenu(ContextMenu contextMenu)
    {
        // Obtener la posición actual del mouse
        var mousePosition = Mouse.GetPosition(this);

        // Configurar el ContextMenu para que se abra en la posición del mouse
        contextMenu.PlacementTarget = this; // El elemento de destino (puede ser la ventana o el canvas)
        contextMenu.Placement = PlacementMode.RelativePoint;
        contextMenu.HorizontalOffset = mousePosition.X - (contextMenu.ActualWidth / 2);
        contextMenu.VerticalOffset = mousePosition.Y - 10;

        // Abrir el ContextMenu
        contextMenu.IsOpen = true;
    }

    private void KeyframeType_Click(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = (string)s.Header;

        Animation.ChangeSelectedKeyframesType(header);
    }

    private void RightClickMenu_Tracks(object sender, RoutedEventArgs e)
    {
        var s = sender as MenuItem;
        var header = (string)s.Header;

        if (header == "Convert To Image Sequence")
        {
            if (ManualAPI.SelectedLayer is VideoLayer vl)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                vl.ConvertToKeyframes();
                Mouse.OverrideCursor = null;
            }
        }

    }






    private void RightClickMenu_GenInterpolation(object sender, RoutedEventArgs e)
    {
        var header = AppModel.GetHeader(sender);

        if (header == "Generate in Batch")
        {
            GenerationManager.GenerateInBatch(SelectedKeyframes);
        }
        else if (header == "Dynami Interpolation...")
        {
            OpenDynamiInterpolation();
        }
        else if (header == "Linear Interpolation...")
        {
            if (SelectedKeyframes.Count == 2)
                timelineContext.Mini_InterpoLinear.Open(SelectedKeyframes);
            else if (SelectedKeyframes.Count > 2)
                Output.Log("Select no more than 2 Keyframes");
            else if (SelectedKeyframes.Count < 2)
                Output.Log("Select at least 2 Keyframes");

        }

    }

    void OpenDynamiInterpolation()
    {
        if (SelectedKeyframes.Count <= 2 && SelectedKeyframes.Count > 0)
            timelineContext.Mini_InterpoDynami.Open(SelectedKeyframes);
        else
            Output.Log("Select only 2 Keyframes");
      //  else if (SelectedKeyframes.Count == 0)
       //     Output.Log("Select at least 1 Keyframe");

    }


    //------------------------------------------------------------------- SCROLLBARS

    private void H_ScrollLeftDelta(object sender, DragDeltaEventArgs e)
    {
        H_ScrollChange(e);
    }

    private void H_ScrollRightDelta(object sender, DragDeltaEventArgs e)
    {
        H_ScrollChange(e);
    }

    void H_ScrollChange(DragDeltaEventArgs e)
    {
        var changeX = e.HorizontalChange;
        var delta = new Vector(changeX, 0);
        Zoom(delta);
    }

    private void Interpolate_Click(object sender, RoutedEventArgs e)
    {
        OpenDynamiInterpolation();
    }
}


//------------------------------------------------------------------------------------------------------------------ MiniGenerator

public partial class Mini_InterpolateDynami : MiniGenerate
{
    public List<Keyframe> MainKeyframes = new();
    public TimedVariable Timed;

    [ObservableProperty] int frames = 2;

    [NotifyPropertyChangedFor(nameof(FramesMode))]
    [ObservableProperty] bool fillMode = true;
    public bool FramesMode => !FillMode;

    [ObservableProperty] int spacing = 2;

    public List<Keyframe> generatedKeyframes = [];
    public int MaxFrames = 14;
    public Mini_InterpolateDynami()
    {
        Placeholders = GetPlaceHolder();
       //UI
        HeaderButton = "Interpolate";
    }

    internal virtual List<string> GetPlaceHolder()
    {
        return [
            "A person doing something...",

            "Walking...",
            "Dancing like nobody's watching...",
            "Jumping over the moon...",
            "Throwing a pie...",
            "Riding invisible bike...",
            "Hugging a face...",

            "Eating spaghetti elegantly...",
            "Creating a time machine with plastic bottles...",
            "Fighting with crocodiles...",
            "Ridding a horse in the moon...",

              ];
    }

    public override void InitializeUI()
    {
        columns([
            new M_CheckBox("Fill", "FillMode"),
            new M_SliderBox("Spacing", 1, 6, 1, 1, true).setHide("FillMode"),
            new M_NumberBox("Frames", 1, 1, true, 1, MaxFrames).setHide("FramesMode")
        ]);
    }

    bool oldTopSection = false;

    //reference KeyA only
    Keyframe KeyB;
    public void Open(IEnumerable<Keyframe> keyframes)
    {
        oldTopSection = (Shortcuts.canvasView?.DataContext as ED_CanvasView)?.TopSection ?? false;
        Shortcuts.ChangeTopSection(false);

        Open();
    
        if (MainKeyframes.Any())
        {
            foreach (var k in MainKeyframes)
                k.FrameChanged -= OnFrameChanged;

            MainKeyframes.Clear();
        }

        this.MainKeyframes = Keyframe.Reorder(keyframes);
        Timed = MainKeyframes[0].AttachedTimedVariable;

        if (MainKeyframes.Count == 1)
        {
            HeaderButton = "Generate";

            KeyB = Keyframe.InsertBake(Timed, MainKeyframes[0].Frame + (MaxFrames - 1));
            KeyB.Type = ColorTag.FromKeyframe(KeyframeTypes.Inbetween);
            MainKeyframes.Add(KeyB);
        }
        else
        {
            HeaderButton = "Interpolate";
        }


        foreach (var k in MainKeyframes)
        {
            k.Block = true;
            k.FrameChanged += OnFrameChanged;
        }

        UpdateGeneratedKeyframes();
    }


    void OnFrameChanged(int value)
    {
        UpdateGeneratedKeyframes();
    }

    partial void OnFillModeChanged(bool value)
    {
        UpdateGeneratedKeyframes();
    }
    partial void OnSpacingChanged(int value)
    {
        UpdateGeneratedKeyframes();
    }
    partial void OnFramesChanged(int value)
    {
        UpdateGeneratedKeyframes();
    }

    public override void OnClose()
    {
 
        if (oldTopSection == true)
            Shortcuts.ChangeTopSection(true);

        foreach (var k in MainKeyframes)
            k.FrameChanged -= OnFrameChanged;
        MainKeyframes.Clear();

        generatedKeyframes.Clear();

        base.OnClose();
    }
    public override void OnCancel()
    {
        if (KeyB != null)
        {
            KeyB.Delete();
            KeyB = null;
        }

        if (oldTopSection == true)
            Shortcuts.ChangeTopSection(true);

        foreach (var k in MainKeyframes)
            k.FrameChanged -= OnFrameChanged;


        foreach (var kf in generatedKeyframes)
            kf.Delete();

        generatedKeyframes.Clear();
        
    }

    private void UpdateGeneratedKeyframes()
    {
        if (FillMode)
            Update_FillMode();
        else
            Update_FramesMode();
      
    }

    void Update_FillMode()
    {
        if (Spacing <= 0)
            return;

        // Asumimos que siempre hay exactamente dos keyframes en MainKeyframes
        int startFrame = MainKeyframes[0].Frame;
        int endFrame = MainKeyframes[1].Frame;

        // Lista para nuevos keyframes calculados
        List<Keyframe> newGeneratedKeyframes = new List<Keyframe>();

        // Calcula los frames intermedios basándote en el spacing
        int count = 0;
        for (int frame = startFrame + Spacing; frame < endFrame && count < MaxFrames; frame += Spacing)
        {
            // Verificar si ya existe un keyframe en esta posición
            var existingKf = generatedKeyframes.FirstOrDefault(k => k.Frame == frame);
            if (existingKf != null)
            {
                // Reutilizar el keyframe existente
                newGeneratedKeyframes.Add(existingKf);
            }
            else
            {
                // Crear un nuevo keyframe
                var k = Keyframe.InsertBake(Timed, frame);
                newGeneratedKeyframes.Add(k);
            }
            count++;
        }

        // Eliminar keyframes antiguos que no fueron reutilizados
        foreach (var kf in generatedKeyframes)
        {
            if (!newGeneratedKeyframes.Contains(kf))
                kf.Delete();
        }

        // Actualizar la lista de keyframes generados
        Timed.UpdateGraph();
        generatedKeyframes = newGeneratedKeyframes;
        frames = generatedKeyframes.Count;
        OnPropertyChanged("Frames");
    }

    void Update_FramesMode()
    {
        if (Frames < 1)
            return;

        // Asumimos que siempre hay exactamente dos keyframes en MainKeyframes
        int startFrame = MainKeyframes[0].Frame;
        int endFrame = MainKeyframes[1].Frame;

        // Calcula el incremento de frames
        int frameIncrement = (endFrame - startFrame) / (Frames + 1);
        int currentFrame = startFrame + frameIncrement;

        // Lista para mantener los nuevos keyframes calculados
        List<Keyframe> newGeneratedKeyframes = new List<Keyframe>();

        for (int i = 0; i < Frames; i++)
        {
            // Revisa si ya existe un keyframe en esta posición
            var existingKf = generatedKeyframes.FirstOrDefault(kf => kf.Frame == currentFrame);
            if (existingKf != null)
            {
                // Reutilizar el keyframe existente
                newGeneratedKeyframes.Add(existingKf);
            }
            else
            {
                // Crear un nuevo keyframe si no existe
                var k = Keyframe.InsertBake(Timed, currentFrame);
                newGeneratedKeyframes.Add(k);
            }
            currentFrame += frameIncrement;
        }

        // Eliminar keyframes antiguos que no se reutilizaron
        foreach (var kf in generatedKeyframes)
        {
            if (!newGeneratedKeyframes.Contains(kf))
                kf.Delete();
        }

        // Actualizar la lista de keyframes generados
        generatedKeyframes = newGeneratedKeyframes;
        Timed.UpdateGraph(); // Asegúrate de actualizar el gráfico o la visualización si es necesario
    }


    internal PromptPreset InterpPreset;
    public override void Generate()
    {
        if(Frames > MaxFrames)
        {
            Output.Log($"Limit of Frames are {MaxFrames}");
            return;
        }
        else if (Frames != 14)
        {
            Output.Log($"only can interpolate 14 frames");
            return;
        }

        Output.Log("Interpolating...");


        InterpPreset ??= api.preset(PresetTemplateName);
        if (InterpPreset == null)
        {
            var preset = PromptPreset.FromTemplate(PresetTemplateName, automaticDrivers: false);
            InterpPreset = preset;
            GenerationManager.Instance.AddPreset(InterpPreset, false);
        }


        // verify if its one reference or interpolation between two
        if (!InterpPreset.Pinned)
        {
            ChangePresetBeforeGenerate();
        }

        var genimg = new GeneratedImage((LayerBase)Timed.CurrentTarget, InterpPreset);

        generatedKeyframes.Concat(MainKeyframes);
        genimg.BakeKeyframes = Keyframe.Reorder(generatedKeyframes);


        GenerationManager.OnGenerated += GenerationManager_OnGenerated;
        GenerationManager.Generate(genimg);

        Close();
    }


    //---------------------------------------------------------- CHANGABLE
    public string PresetTemplateName = "toon dynami interpolation";
  
    public virtual void ChangePresetBeforeGenerate()
    {
        ChangeToonPresetBeforeGenerate();
    }
    public void ChangeDynamiPresetBeforeGenerate()
    {
        var dynamiNode = InterpPreset.node("DynamiCrafterI2V");
        dynamiNode.field("prompt").FieldValue = Prompt;
        dynamiNode.field("frames").FieldValue = Frames + 2;

        InterpPreset.ChangeField("FrameA", "Keyframe", MainKeyframes[0].Frame);


        //two key interp
        if (KeyB == null)
        {
            var frameAResize = InterpPreset.node("FrameA Resize");
            frameAResize.field("width").FieldValue = 512;
            frameAResize.field("height").FieldValue = 320;

            //connect if not connected
            if (InterpPreset.node("FrameB Resize").field("IMAGE") is NodeOption nodeop && !nodeop.IsConnected())
            {
                var nodeop2 = dynamiNode.field("image2");
                nodeop.Connect(nodeop2);
            }

            InterpPreset.ChangeField("FrameB", "Keyframe", MainKeyframes[1].Frame);

            //model
            InterpPreset.ChangeField("DynamiCrafterModelLoader", "ckpt_name", "dynamicrafter_512_interp_v1.ckpt");
        }

        //one key
        else
        {
            var frameAResize = InterpPreset.node("FrameA Resize");
            frameAResize.field("width").FieldValue = 1024;
            frameAResize.field("height").FieldValue = 576;


            InterpPreset.node("FrameB Resize").field("IMAGE").Disconnect();

            //model
            InterpPreset.ChangeField("DynamiCrafterModelLoader", "ckpt_name", "dynamicrafter_1024_v1.ckpt");
        }

    }


    public void ChangeToonPresetBeforeGenerate()
    {
        var dynamiNode = InterpPreset.node("ToonCrafterInterpolation");
        dynamiNode.field("frames").FieldValue = Frames + 2;

        InterpPreset.ChangeField("FrameA", "Keyframe", MainKeyframes[0].Frame);
        InterpPreset.ChangeField("FrameB", "Keyframe", MainKeyframes[1].Frame);


        //prompt
        var positive = InterpPreset.node("Positive");
        var defaultPrompt = "anime scene";
        positive.field("text").FieldValue = !string.IsNullOrEmpty(Prompt) ? Prompt + $", {defaultPrompt}" : defaultPrompt;
    }



    private void GenerationManager_OnGenerated(GeneratedImage img)
    {
        foreach (var key in MainKeyframes)
        {
            key.Block = false;
            key.Type = ColorTag.FromKeyframe(KeyframeTypes.Keyframe);
        }

        foreach (var key in img.BakeKeyframes)
        {
            if (key.Type == KeyframeTypes.BakeKeyframe)
                key.Type = ColorTag.FromKeyframe(KeyframeTypes.Inbetween);

        }


        GenerationManager.OnGenerated -= GenerationManager_OnGenerated;
    }
}



public class Mini_InterpolateLinear : Mini_InterpolateDynami
{
    public Mini_InterpolateLinear()
    {
        PresetTemplateName = "linear interpolation";
        IsPromptEnabled = false;
    }

    public override void ChangePresetBeforeGenerate()
    {
        InterpPreset.ChangeField("FrameA", "Keyframe", MainKeyframes.First().Frame);

        InterpPreset.ChangeField("FrameB", "Keyframe", MainKeyframes.Last().Frame);

        InterpPreset.ChangeField("RIFE VFI (recommend rife47 and rife49)", "multiplier", (Frames + MainKeyframes.Count) / MainKeyframes.Count);

    }

}











public enum TimelineMode
{
    Keyframes,
    Tracks,
}




//DEPRECATED MAYBE
class TimelineTickBar : TickBar
{
    protected override void OnRender(System.Windows.Media.DrawingContext dc)
    {
        double num = this.Maximum - this.Minimum;
        double y = this.ReservedSpace * 0.5;
        FormattedText formattedText = null;
        double x = 0;
        for (double i = 0; i <= num; i += this.TickFrequency)
        {
            formattedText = new FormattedText(i.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Verdana"), 8, Brushes.White);
            if (this.Minimum == i)
                x = 0;
            else
                x += this.ActualWidth / (num / this.TickFrequency);
            dc.DrawText(formattedText, new Point(x, 10));
        }
    }
}


// --------------------------------------------------------------- KEYFRAMES CONVERTERS
public class TimeToPositionConverter : IValueConverter
{
    // La anchura total del slider timeline
    public double TotalWidth { get; set; }

    // El número total de frames en la línea de tiempo
    public int TotalFrames { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int frame)
        {
          
            return frame * 40;
        }

        return 10;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class FrameToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || !(value is int))
            return null;

        int frame = (int)value;
        int frameRate = 24;

        TimeSpan timeSpan = TimeSpan.FromSeconds((double)frame / frameRate);

        return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}",
            (int)timeSpan.TotalHours,
            timeSpan.Minutes,
            timeSpan.Seconds,
            timeSpan.Milliseconds / 10);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return 1.0;
        }

        return 0.33;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



// ---- KEYFRAME POSITION
public class FrameToXConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int frame && values[1] is double globalCanvasTransformX)
        {
            return frame * globalCanvasTransformX;
        }
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ValueToYConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {

        if (values.Length == 2 && values[0] != null && values[1] is double canvasY)
        {
            object value = values[0];
            Type valueType = value.GetType();

            if (valueType == typeof(float))
            {
                return (float)value * canvasY;
            }
            else if (valueType == typeof(int))
            {
                return (int)value * canvasY;
            }

            else if (valueType == typeof(double))
            {
                return (double)value * canvasY;
            }
            else
            {
                return 0;
            }
        }

        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}


//----------------------------------------------- GRAPH UPDATE
public class GraphPointsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[1] == DependencyProperty.UnsetValue || values[2] == DependencyProperty.UnsetValue)
            return Binding.DoNothing;

        // Obtener los valores pasados como parámetros
       // PointCollection graphPointsContext = (PointCollection)values[0];
        TimedVariable timedVariable = (TimedVariable)values[1];
        ED_TimelineView timelineContext = (ED_TimelineView)values[2];

        if (timelineContext == null || timedVariable == null || !timedVariable.Keyframes.Any())
            return Binding.DoNothing;



        if (timedVariable.GetType() == typeof(WriteableBitmap))
        {

            return ImagePoints(timedVariable, timelineContext);
        }
        else
        {
            return NumberPoints(timedVariable, timelineContext);
        }

    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }


    PointCollection NumberPoints(TimedVariable timedVariable, ED_TimelineView timelineContext)
    {
        double scaleX = timelineContext.CanvasScaleX;
        double scaleY = timelineContext.CanvasScaleY;
        PointCollection points = new();

        for (int frame = SelectedLayer._Animation.FrameStart; frame <= SelectedLayer._Animation.FrameEnd; frame++)
        {
            float value = timedVariable.GetIntValueAt(frame);

            Point point = new Point(frame * scaleX, value * scaleY);
            points.Add(point);
        }
        return points;
    }


    PointCollection ImagePoints(TimedVariable timedVariable, ED_TimelineView timelineContext)
    {
        double scaleX = timelineContext.CanvasScaleX;
        double scaleY = timelineContext.CanvasScaleY;
        PointCollection points = new();

        foreach (Keyframe keyframe in timedVariable.Keyframes)
        {
            int frame = keyframe.Frame;
            int value = 0;

            double x = frame * scaleX;
            double y = value * scaleY;

            Point point = new Point(x, y);
            points.Add(point);
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            Point startPoint = points[i];
            Point endPoint = points[i + 1];

            // Ajuste del extremo derecho en X
            endPoint.X -= 1;

            // Configurar propiedades adicionales de la línea (color, grosor, etc.)

            points.Add(startPoint);
            points.Add(endPoint);
        }

        return points;

    }


    PointCollection DefaultPoints(ED_TimelineView timelineContext)
    {
        double scaleX = timelineContext.CanvasScaleX;
        double scaleY = timelineContext.CanvasScaleY;

        PointCollection points = new();
        int value = 0;

        Point point = new Point(Animation.FrameStart, (int)value * scaleY);
        points.Add(point);


        Point point2 = new Point(Animation.FrameEnd, (int)value * scaleY);
        points.Add(point2);

        return points;
    }
}


//--------------------------------------------------------------------- TRACK CONVERTERS
public class FrameDurationToXConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int frame && values[1] is double globalCanvasTransformX)
        {
            return frame * globalCanvasTransformX + globalCanvasTransformX;
        }
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class TrackIndexToYConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {

        if (values.Length == 2 && values[0] is int index && values[1] is double canvasY)
        {
            return CalculateY(index, canvasY);
        }

        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static double CalculateY(double index, double canvasY)
    {
        return -index * TrackHeightToYConverter.CalculateHeight(canvasY);
    }
}

public class TrackHeightToYConverter : IMultiValueConverter
{
    public static readonly int TrackHeight = 110;
    public static readonly int TrackHeightOffset = -2;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {

        if (values.Length == 2 && values[0] is int index && values[1] is double canvasY)
        {
            return CalculateHeight(canvasY);
        }

        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static double CalculateHeight(double canvasY)
    {
        /*  return canvasY
                  * 110 // offset
                  + 7; //limit*/
        return canvasY * TrackHeight
            + TrackHeightOffset;
    }
}

public class TrackColorTagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            if (color == Colors.Transparent)
                return AppModel.GetThemeColorBrush("fg_sub");
            else
                return new SolidColorBrush(color);
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class ValueToNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int || value is double || value is float || value is decimal)  // Añade aquí otros tipos numéricos si es necesario
            return value;
        else if (value is WriteableBitmap || value is SkiaSharp.SKBitmap)
            return "Image";
        else
            return value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
