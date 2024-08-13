using CommunityToolkit.Mvvm.ComponentModel;
using Manual.API;
using Manual.Core;
using Manual.Objects;
using ManualToolkit.Generic;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Serialization;

namespace Manual.Editors;

//---------------------------------------------------------------------------------------------------------------------------------------------------------  VIEW MODEL
public partial class ED_TimelineView : Editor, ICanvasMatrix
{
    public bool loaded { get; set; }
    [ObservableProperty] [property: JsonIgnore] double canvasTransformFactor = 0.227;

    //LOCAL DYNAMIC
    [JsonIgnore] public double GlobalCanvasTransformX => SelectedLocal != null ? SelectedLocal.CanvasMatrix.OffsetX : CanvasMatrix.OffsetX;
    [JsonIgnore] public double GlobalCanvasTransformY => SelectedLocal != null ? SelectedLocal.CanvasMatrix.OffsetY : CanvasMatrix.OffsetY;
    [JsonIgnore] public Point CanvasScale => SelectedLocal != null ? new Point(SelectedLocal.CanvasScaleX, SelectedLocal.CanvasScaleY) : new Point(CanvasScaleX, CanvasScaleY);



    //GLOBAL
    double global_canvasScaleX = 14.15;
     double global_canvasScaleY = 0.28;
     Matrix global_canvasMatrix = new Matrix(1, 0, 0, 1, 19, 97); //Matrix.Identity;


    [NotifyPropertyChangedFor(nameof(CanvasScale))]
    [ObservableProperty] double canvasScaleX = 14.15;
    [NotifyPropertyChangedFor(nameof(CanvasScale))]
    [ObservableProperty] double canvasScaleY = 0.28;

    [NotifyPropertyChangedFor(nameof(GlobalCanvasTransformX))]
    [NotifyPropertyChangedFor(nameof(GlobalCanvasTransformY))]
    [ObservableProperty] Matrix canvasMatrix = new Matrix(1, 0, 0, 1, 19, 97); //Matrix.Identity;



    [ObservableProperty] TimelineMode timelineMode = TimelineMode.Keyframes;
    [ObservableProperty] [property: JsonIgnore] AnimationBehaviour? selectedLocal;


    [ObservableProperty] double leftSection = 100;
    [ObservableProperty] double rightSection = 170;



 

    public ED_TimelineView()
    {

    }
    partial void OnCanvasScaleXChanged(double value)
    {
        var a = LeftSection;
        var b = RightSection;

        if (SelectedLocal != null)
            SelectedLocal.CanvasScaleX = value;
    }
    partial void OnCanvasScaleYChanged(double value)
    {
        if (SelectedLocal != null)
            SelectedLocal.CanvasScaleY = value;
    }
    partial void OnCanvasMatrixChanged(Matrix value)
    {
        if (SelectedLocal != null)
            SelectedLocal.CanvasMatrix = value;
    }

    partial void OnSelectedLocalChanged(AnimationBehaviour? oldValue, AnimationBehaviour? newValue)
    {
        //return to global
        if (oldValue != null && newValue == null)
        {
            CanvasMatrix = global_canvasMatrix;
            CanvasScaleX = global_canvasScaleX;
            CanvasScaleY = global_canvasScaleY;
        }

        //global to local
        else if(oldValue == null && newValue != null)
        {
           global_canvasMatrix = CanvasMatrix;
           global_canvasScaleX = CanvasScaleX;
           global_canvasScaleY = CanvasScaleY;

           CanvasMatrix = newValue.CanvasMatrix;
           CanvasScaleX = newValue.CanvasScaleX;
           CanvasScaleY = newValue.CanvasScaleY;
        }
    }




    public double GridWidth { get; set; }
    public double GridHeight { get; set; }
    public void DefaultPosition()
    {
        
    }



    [JsonIgnore] public Mini_InterpolateDynami Mini_InterpoDynami = new();

    [JsonIgnore] public Mini_InterpolateLinear Mini_InterpoLinear = new();
}



public class SimpleCanvasMatrix : CanvasMatrix
{
    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {

    }

    public static readonly DependencyProperty CanvasTransformProperty =
     DependencyProperty.Register(
        nameof(CanvasTransform),
         typeof(Matrix),
         typeof(SimpleCanvasMatrix),
         new FrameworkPropertyMetadata(Matrix.Identity,
         FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
         OnValueChanged,
         CoerceValue,
         true,
         UpdateSourceTrigger.PropertyChanged));

    public readonly MatrixTransform _transform = new MatrixTransform();
    public override Matrix CanvasTransform
    {
        get { return (Matrix)GetValue(CanvasTransformProperty); }
        set
        {
            var oldValue = CanvasTransform;
            SetValue(CanvasTransformProperty, value);
            OnCanvasTransformChanged?.Invoke(oldValue, value);
        }
    }

    public Action<Matrix, Matrix> OnCanvasTransformChanged;

}

//--------------------------------------------------------------------------------------------------------------------------------------------------------- TIMELINE CANVAS
public class TimelineCanvas : Canvas
{
 


    public static readonly DependencyProperty ForegroundColorProperty =
       DependencyProperty.Register(
           nameof(ForegroundColor),
           typeof(Brush),
           typeof(TimelineCanvas),
           new FrameworkPropertyMetadata("#bfc1c8".ToSolidColorBrush(), FrameworkPropertyMetadataOptions.AffectsRender)
       );
    public Brush ForegroundColor
    {
        get { return (Brush)GetValue(ForegroundColorProperty); }
        set { SetValue(ForegroundColorProperty, value); }
    }



    public static readonly DependencyProperty PrimaryColorLineProperty =
       DependencyProperty.Register(
           nameof(PrimaryColorLine),
           typeof(Brush),
           typeof(TimelineCanvas),
           new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender)
       );

    public static readonly DependencyProperty SecondaryColorLineProperty =
        DependencyProperty.Register(
            nameof(SecondaryColorLine),
            typeof(Brush),
            typeof(TimelineCanvas),
            new FrameworkPropertyMetadata("#212121".ToSolidColorBrush(), FrameworkPropertyMetadataOptions.AffectsRender)
        );

    public Brush PrimaryColorLine
    {
        get { return (Brush)GetValue(PrimaryColorLineProperty); }
        set { SetValue(PrimaryColorLineProperty, value); }
    }

    public Brush SecondaryColorLine
    {
        get { return (Brush)GetValue(SecondaryColorLineProperty); }
        set { SetValue(SecondaryColorLineProperty, value); }
    }




    public static readonly DependencyProperty GlobalCanvasTransformXProperty = DependencyProperty.Register(
    nameof(GlobalCanvasTransformX),
    typeof(double),
    typeof(TimelineCanvas),
    new FrameworkPropertyMetadata(
        19.0,
        FrameworkPropertyMetadataOptions.AffectsRender,
        OnGlobalCanvasTransformXChanged));

    public double GlobalCanvasTransformX
    {
        get { return (double)GetValue(GlobalCanvasTransformXProperty); }
        set { SetValue(GlobalCanvasTransformXProperty, value);  }
    }

    private static void OnGlobalCanvasTransformXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        UpdateUI(d);
    }


    public static readonly DependencyProperty GlobalCanvasTransformYProperty = DependencyProperty.Register(
nameof(GlobalCanvasTransformY),
typeof(double),
typeof(TimelineCanvas),
new FrameworkPropertyMetadata(
    19.0,
    FrameworkPropertyMetadataOptions.AffectsRender,
    OnGlobalCanvasTransformYChanged));

    public double GlobalCanvasTransformY
    {
        get { return (double)GetValue(GlobalCanvasTransformYProperty); }
        set { SetValue(GlobalCanvasTransformYProperty, value); }
    }

    private static void OnGlobalCanvasTransformYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        UpdateUI(d);
    }






    public static readonly DependencyProperty CanvasScaleXProperty = DependencyProperty.Register(
        nameof(CanvasScaleX),
        typeof(double),
        typeof(TimelineCanvas),
        new PropertyMetadata(14.15, OnCanvasScaleXChanged));

    private static void OnCanvasScaleXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        UpdateUI(d);
    }

    public double CanvasScaleX
    {
        get { return (double)GetValue(CanvasScaleXProperty); }
        set {
            SetValue(CanvasScaleXProperty, value);
        }
    }



    public static readonly DependencyProperty CanvasScaleYProperty = DependencyProperty.Register(
       nameof(CanvasScaleY),
       typeof(double),
       typeof(TimelineCanvas),
       new PropertyMetadata(14.15, OnCanvasScaleYChanged));

    private static void OnCanvasScaleYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        UpdateUI(d);
    }

    public double CanvasScaleY
    {
        get { return (double)GetValue(CanvasScaleYProperty); }
        set
        {
            SetValue(CanvasScaleYProperty, value);
        }
    }





    public TimelineCanvas()
    {
        FocusVisualStyle = null;
    }


    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        // Aquí puedes agregar lógica para validar el valor antes de establecerlo
        return baseValue;
    }

    public static readonly DependencyProperty ViewModeProperty =
          DependencyProperty.Register(
              nameof(ViewMode), typeof(TimelineMode), typeof(TimelineCanvas),
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
        var tv = (TimeLineCanvasLines)d;
        tv.OnTimelineModeChanged((TimelineMode)e.NewValue);
    }

    void OnTimelineModeChanged(TimelineMode value)
    {
        UpdateUI();
    }






    public static void UpdateUI(DependencyObject d)
    {
        ((TimelineCanvas)d).InvalidateVisual();
    }
    public void UpdateUI()
    {
        this.InvalidateVisual();
    }

    protected int DetermineSteps()
    {
        return GetSteps(CanvasScaleX, 58, 2);
    }

    public static int GetSteps(double canvasScaleX, int start, int jumps)
    {
        if (double.IsInfinity(canvasScaleX))
            return start;

        int baseStep = 1;
        double scaleLog = Math.Log(canvasScaleX / start, jumps);
        int exponent = (int)Math.Floor(scaleLog);
        return baseStep << Math.Abs(exponent);
    }

}


public partial class TimeLineCanvasNumbers : TimelineCanvas
{

    Typeface Font = new Typeface(new FontFamily(new Uri("pack://application:,,,/"), $"{App.LocalPath}Assets/#Roboto"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);


    // Define the TimelineMode dependency property
    public static readonly DependencyProperty TimelineModeProperty =
        DependencyProperty.Register(
            nameof(TimelineMode),
            typeof(TimelineMode),
            typeof(TimeLineCanvasNumbers),
            new PropertyMetadata(TimelineMode.Keyframes));

    // Create a CLR property wrapper for the dependency property
    public TimelineMode TimelineMode
    {
        get { return (TimelineMode)GetValue(TimelineModeProperty); }
        set { SetValue(TimelineModeProperty, value); }
    }

    public static readonly DependencyProperty FrameBufferProperty =
 DependencyProperty.Register("FrameBuffer", typeof(Dictionary<int, SKBitmap>), typeof(TimeLineCanvasNumbers),
     new FrameworkPropertyMetadata(new Dictionary<int, SKBitmap>(), FrameworkPropertyMetadataOptions.AffectsRender));
    public Dictionary<int, SKBitmap> FrameBuffer
    {
        get { return (Dictionary<int, SKBitmap>)GetValue(FrameBufferProperty); }
        set { SetValue(FrameBufferProperty, value); }
    }

    SolidColorBrush violet = AppModel.GetResource<SolidColorBrush>("fg_high2");
    SolidColorBrush gray = new SolidColorBrush(ManualColors.Gray);

    public TimeLineCanvasNumbers()
    {
        violet?.Freeze();
        gray?.Freeze();

        DataContextChanged += TimeLineCanvasNumbers_DataContextChanged;
    }

    private void TimeLineCanvasNumbers_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AnimationManager anim2)
            anim2.OnFrameBuffering -= Anim_OnFrameBuffering;

        if (e.NewValue is AnimationManager anim)
            anim.OnFrameBuffering += Anim_OnFrameBuffering;
     
    }

    private void Anim_OnFrameBuffering(int value)
    {
        if(AppModel.IsDispatcherSafe())
          InvalidateVisual();
    }


    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);


        //--------FRAME BUFFERS
        var renderPen = violet;//new SolidColorBrush(Colors.BlueViolet);

        if (ManualAPI.Animation != null)
        {
            var frameBuffer = ManualAPI.Animation.FrameBuffer;
            if (TimelineMode == TimelineMode.Tracks)
                frameBuffer = ManualAPI.Animation.FrameBuffer;
            else if (TimelineMode == TimelineMode.Keyframes && ManualAPI.SelectedLayer != null)
            {
                frameBuffer = ManualAPI.SelectedLayer._Animation.FrameBuffer;
                renderPen = gray;
            }

            if (TimelineMode == TimelineMode.Keyframes)
            {
                foreach (var frame in ManualAPI.Animation.FrameBuffer.Keys)
                {
                    var frameWidth = CanvasScaleX;
                    double offset = ManualAPI.SelectedLayer._Animation.StartOffset;
                    double xPosition = (frame - offset) * CanvasScaleX + GlobalCanvasTransformX;
                    if (frameWidth <= 0) frameWidth = 1;

                    var frameHeight = 4;

                    var rect = new Rect(xPosition, 22 - frameHeight, frameWidth + 1, frameHeight);
                    dc.DrawRectangle(violet, null, rect);


                }
            }

            foreach (var frame in frameBuffer.Keys)
            {
                var frameWidth = CanvasScaleX;
                double xPosition = frame * CanvasScaleX + GlobalCanvasTransformX;
                if (frameWidth <= 0) frameWidth = 1;

                var frameHeight = 2;

                var rect = new Rect(xPosition, 22 - frameHeight, frameWidth + 1, frameHeight);
                dc.DrawRectangle(renderPen, null, rect);

               
            }

          
        }



        //-------NUMBERS

        var whitePen = new Pen(PrimaryColorLine, 1);
        // Determinar la frecuencia de las líneas en función del zoom
        int frameStep = DetermineSteps();

        // Calcular el inicio y el final basado en el desplazamiento y el ancho del canvas
        double startFrame = Math.Ceiling(-GlobalCanvasTransformX / CanvasScaleX / frameStep) * frameStep;
        double endFrame = Math.Floor((ActualWidth - GlobalCanvasTransformX) / CanvasScaleX / frameStep) * frameStep;

        for (double frame = startFrame; frame <= endFrame; frame += frameStep)
        {
            double xPosition = frame * CanvasScaleX + GlobalCanvasTransformX;
            double midXPosition = (frame + frameStep / 2) * CanvasScaleX + GlobalCanvasTransformX; // Posición para la línea intermedia

            // Asegúrate de que la línea se dibuja dentro de los límites del canvas
            if (xPosition >= 0 && xPosition <= ActualWidth)
            {

                var pos = new Point(xPosition, 1);
                dc.DrawLine(whitePen, pos.Add(0.5, 15), pos.Add(0.5, 20));

                // Dibuja el texto solo en líneas negras
                if (frameStep >= 1 && midXPosition >= 0 && midXPosition <= ActualWidth)
                {
                    var formattedText = new FormattedText(
                        frame.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        Font,// new Typeface("Segoe UI"),
                        12, // Tamaño del texto
                        ForegroundColor);
                    formattedText.TextAlignment = TextAlignment.Center;
                    // Asegúrate de que el texto se dibuja dentro de los límites del canvas
                   
                    if (xPosition + 2 <= ActualWidth)
                    {                    
                        dc.DrawText(formattedText, pos); 
                    }
                  
                }
             
            }

        }


    }




}


public partial class TimeLineCanvasLines : TimelineCanvas
{
  

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        switch (ViewMode)
        {
            case TimelineMode.Keyframes:
                KeyframeModeDraw(dc);
                break;
            case TimelineMode.Tracks:
                TrackModeDraw(dc);
                break;
            default:
                break;
        }

    }

    void KeyframeModeDraw(DrawingContext dc)
    {
        var blackPen = new Pen(PrimaryColorLine, 1);
        var grayPen = new Pen(SecondaryColorLine, 1);

        // Determinar la frecuencia de las líneas en función del zoom
        int frameStep = DetermineSteps();

        // Calcular el inicio y el final basado en el desplazamiento y el ancho del canvas
        double startFrame = Math.Ceiling(-GlobalCanvasTransformX / CanvasScaleX / frameStep) * frameStep;
        double endFrame = Math.Floor((ActualWidth - GlobalCanvasTransformX) / CanvasScaleX / frameStep) * frameStep;

        for (double frame = startFrame; frame <= endFrame; frame += frameStep)
        {
            double xPosition = frame * CanvasScaleX + GlobalCanvasTransformX;
            double midXPosition = (frame + frameStep / 2) * CanvasScaleX + GlobalCanvasTransformX; // Posición para la línea intermedia

            var pen = frame % 2 == 0 ? blackPen : grayPen;

            // Asegúrate de que la línea se dibuja dentro de los límites del canvas
            if (xPosition >= 0 && xPosition <= ActualWidth)
            {
                // Dibuja la línea
                dc.DrawLine(pen, new Point(xPosition, 16), new Point(xPosition, ActualHeight));

                // Dibuja la línea intermedia si frameStep es mayor que 1
                if (frameStep > 1 && midXPosition >= 0 && midXPosition <= ActualWidth)
                {
                    dc.DrawLine(grayPen, new Point(midXPosition, 16), new Point(midXPosition, ActualHeight));
                }


            }
        }


       // TrackModeDraw(dc);

    }
    void TrackModeDraw(DrawingContext dc)
    {
        var primaryPen = new Pen(new SolidColorBrush(Color.FromRgb(46, 46, 46)), 1);

        // Determinar la frecuencia de las líneas en función del trackHeight
        var trackStep = TrackHeightToYConverter.CalculateHeight(CanvasScaleY);
     
        // Calcular el inicio y el final basado en el desplazamiento vertical y la altura del canvas
        double startTrack = Math.Ceiling(-GlobalCanvasTransformY / CanvasScaleY / trackStep) * trackStep;
        double endTrack = Math.Floor((ActualHeight - GlobalCanvasTransformY) / CanvasScaleY / trackStep) * trackStep;

        for (double track = startTrack; track <= endTrack; track += trackStep)
        {
            double yPosition = track + GlobalCanvasTransformY;

            // Asegúrate de que la línea se dibuja dentro de los límites del canvas
            if (yPosition >= 0 && yPosition <= ActualHeight)
            {
                // Dibuja la línea vertical
                dc.DrawLine(primaryPen, new Point(0, yPosition), new Point(ActualWidth, yPosition));
            }
        }
    }




}




