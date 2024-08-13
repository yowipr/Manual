using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;

namespace ManualToolkit.Themes;

public class ImageBorder : Border
{
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
    nameof(Stretch),
    typeof(Stretch),
    typeof(ImageBorder),
    new PropertyMetadata(Stretch.UniformToFill, OnStretchChanged));

    public Stretch Stretch
    {
        get { return (Stretch)GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ImageBorder)d).UpdateImageBrush();
    }


    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
        nameof(ImageSource),
        typeof(ImageSource),
        typeof(ImageBorder),
        new PropertyMetadata(null, OnImageSourceChanged));

    public ImageSource ImageSource
    {
        get { return (ImageSource)GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ImageBorder)d;
        control.UpdateImageSource();
    }

    private void UpdateImageSource()
    {
        this.Background = new ImageBrush(this.ImageSource) { Stretch = Stretch.UniformToFill };
    }
    public ImageBorder()
    {
        CornerRadius = new CornerRadius(11);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    public static readonly DependencyProperty ViewportProperty = DependencyProperty.Register(
      nameof(Viewport),
      typeof(Rect),
      typeof(ImageBorder),
      new PropertyMetadata(new Rect(0, 0, 1, 1), OnViewportChanged));

    public static readonly DependencyProperty ViewboxProperty = DependencyProperty.Register(
        nameof(Viewbox),
        typeof(Rect),
        typeof(ImageBorder),
        new PropertyMetadata(new Rect(0, 0, 1, 1), OnViewboxChanged));

    public Rect Viewport
    {
        get { return (Rect)GetValue(ViewportProperty); }
        set { SetValue(ViewportProperty, value); }
    }

    public Rect Viewbox
    {
        get { return (Rect)GetValue(ViewboxProperty); }
        set { SetValue(ViewboxProperty, value); }
    }

    private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ImageBorder)d).UpdateImageBrush();
    }

    private static void OnViewboxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ImageBorder)d).UpdateImageBrush();
    }

    private void UpdateImageBrush()
    {
        var brush = new ImageBrush(this.ImageSource)
        {
            Stretch = Stretch.UniformToFill,
            Viewport = this.Viewport,
            Viewbox = this.Viewbox
        };
        this.Background = brush;
    }
}




public class Squircle : Border
{
    // Define la propiedad de dependencia para la curvatura
    public static readonly DependencyProperty CurvatureProperty =                                  //DISABLED RELEASE: themes - cambiar a 4.0
        DependencyProperty.Register(nameof(Curvature), typeof(double), typeof(Squircle), new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Curvature
    {
        get { return (double)GetValue(CurvatureProperty); }
        set { SetValue(CurvatureProperty, value); }
    }

    protected override void OnRender(DrawingContext dc)
    {
        
        //base.OnRender(dc);
        //return;

        double width = ActualWidth;
        double height = ActualHeight;
        double cornerRadius = Math.Min(width, height) / 2;
        double n = Curvature * Math.Min(width, height) / 100;

        // Crear geometría del squircle para las esquinas
        var geometry = CreateSquircleGeometry(cornerRadius * 2, cornerRadius * 2, n);

        var pen = new Pen(BorderBrush, BorderThickness.Left);
        // Dibujar las cuatro esquinas
        dc.PushTransform(new TranslateTransform(0, 0));
        dc.DrawGeometry(Background, pen, geometry);
        dc.Pop();

        dc.PushTransform(new TranslateTransform(width - cornerRadius * 2, 0));
        dc.DrawGeometry(Background, pen, geometry);
        dc.Pop();

        dc.PushTransform(new TranslateTransform(0, height - cornerRadius * 2));
        dc.DrawGeometry(Background, pen, geometry);
        dc.Pop();

        dc.PushTransform(new TranslateTransform(width - cornerRadius * 2, height - cornerRadius * 2));
        dc.DrawGeometry(Background, pen, geometry);
        dc.Pop();


        //NORMAL
      //  dc.DrawRectangle(Background, new Pen(BorderBrush, BorderThickness.Left), new Rect(cornerRadius, 0, width - cornerRadius * 2, height));
      //  dc.DrawRectangle(Background, new Pen(BorderBrush, BorderThickness.Left), new Rect(0, cornerRadius, width, height - cornerRadius * 2));

        //MODIFIED
        var offset = -0.25;
        dc.DrawRectangle(Background, null, new Rect(cornerRadius, offset, (width - cornerRadius * 2), height - (offset*2)));
        dc.DrawRectangle(Background, null, new Rect(0, cornerRadius, width, height - cornerRadius * 2));
    }

    private Geometry CreateSquircleGeometry(double width, double height, double n)
    {
        var figure = new PathFigure();
        var segments = new PathSegmentCollection();

        int numPoints = 100;

        for (int i = 0; i <= numPoints; i++)
        {
            double angle = i * 2 * Math.PI / numPoints;
            double x = Math.Pow(Math.Abs(Math.Cos(angle)), 2 / n) * (width / 2) * Math.Sign(Math.Cos(angle));
            double y = Math.Pow(Math.Abs(Math.Sin(angle)), 2 / n) * (height / 2) * Math.Sign(Math.Sin(angle));

            if (i == 0)
            {
                figure.StartPoint = new Point(x + width / 2, y + height / 2);
            }
            else
            {
                segments.Add(new LineSegment(new Point(x + width / 2, y + height / 2), true));
            }
        }

        figure.Segments = segments;
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return geometry;
    }
}

