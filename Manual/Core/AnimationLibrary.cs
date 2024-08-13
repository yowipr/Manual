using Manual.Objects;
using Silk.NET.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Manual.Core;

public static class AnimationLibrary
{



    public static void StopAnimation(Storyboard storyboard)
    {
        storyboard.Stop();
    }
    public static void DoAnimationAtEnd(FrameworkElement element, Storyboard animation, Action action)
    {
        // Elimina suscripciones previas para evitar que se ejecute múltiples veces si se llama FadeOut repetidamente.
        animation.Completed -= StoryboardCompleted;
        animation.Completed += StoryboardCompleted;
        void StoryboardCompleted(object sender, EventArgs e)
        {
            action?.Invoke();
            animation.Completed -= StoryboardCompleted;
        }

        animation.Begin(element);
    }

    public static void OnFinalized(this Storyboard animation, Action action)
    {
        animation.Completed -= StoryboardCompleted;
        animation.Completed += StoryboardCompleted;
        void StoryboardCompleted(object sender, EventArgs e)
        {
            action?.Invoke();
            animation.Completed -= StoryboardCompleted;
        }
    }




    static void Asign(this Storyboard storyboard, DependencyObject target, AnimationTimeline animation, string propertyName)
    {
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(propertyName));
    }
    static void Asign(this Storyboard storyboard, DependencyObject target, AnimationTimeline animation, object property)
    {
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(property));
    }

    public static Storyboard BounceLoaded(FrameworkElement targetElement)
    {
        var easingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
        var timeSpan = TimeSpan.FromSeconds(0.2);

        var opacityAnimation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = timeSpan,
            EasingFunction = easingFunction
        };

        var scaleXAnimation = new DoubleAnimation
        {
            From = 0.8,
            To = 1,
            Duration =timeSpan,
            EasingFunction = easingFunction
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = 0.8,
            To = 1,
            Duration = timeSpan,
            EasingFunction = easingFunction
        };


        var storyboard = new Storyboard();


        storyboard.Asign(targetElement, opacityAnimation, "Opacity");
        storyboard.Asign(targetElement, scaleXAnimation, "RenderTransform.ScaleX");
        storyboard.Asign(targetElement, scaleYAnimation, "RenderTransform.ScaleY");

        storyboard.Begin();

        return storyboard;
    }



    public static Action AnimateMatrix(Matrix fromMatrix, Matrix toMatrix, TimeSpan duration, Action<Matrix> asignMatrix)
    {
        var startTime = DateTime.Now;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };


        Action stopAnimation = () =>
        {
            timer.Stop();
        };

        timer.Tick += (sender, args) =>
        {
            var now = DateTime.Now;
         //   var progress = (now - startTime) / duration; var now = DateTime.Now;

            var normalizedTime = (now - startTime).TotalMilliseconds / duration.TotalMilliseconds;
            // Asegurarse de que el tiempo normalizado no exceda 1
            normalizedTime = Math.Min(normalizedTime, 1.0);

            var exponent = 8; // Incrementa este valor para una desaceleración aún más pronunciada
            var progress = 1 - Math.Pow(1 - normalizedTime, exponent);

            if (progress >= 1)
            {
                timer.Stop();
                //   ed.CanvasMatrix = toMatrix; // Asegúrate de que terminamos exactamente en la matriz final
                asignMatrix?.Invoke(toMatrix);
            }
            else
            {
                // Interpola entre fromMatrix y toMatrix según el progreso
                var newMatrix = new Matrix(
                    fromMatrix.M11 + (toMatrix.M11 - fromMatrix.M11) * progress,
                    fromMatrix.M12 + (toMatrix.M12 - fromMatrix.M12) * progress,
                    fromMatrix.M21 + (toMatrix.M21 - fromMatrix.M21) * progress,
                    fromMatrix.M22 + (toMatrix.M22 - fromMatrix.M22) * progress,
                    fromMatrix.OffsetX + (toMatrix.OffsetX - fromMatrix.OffsetX) * progress,
                    fromMatrix.OffsetY + (toMatrix.OffsetY - fromMatrix.OffsetY) * progress);

               // ed.CanvasMatrix = newMatrix;
                asignMatrix?.Invoke(newMatrix);
            }
        };

        timer.Start();

        return stopAnimation;
    }





    public static Storyboard PressDown(FrameworkElement targetElement, double from = 1, double to = 0.8)
    {
        var easingFunction = new QuarticEase {EasingMode = EasingMode.EaseOut };
        var timeSpan = TimeSpan.FromSeconds(0.3);


        var scaleXAnimation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = timeSpan,
            EasingFunction = easingFunction
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = timeSpan,
            EasingFunction = easingFunction
        };

        var storyboard = new Storyboard();


        storyboard.Asign(targetElement, scaleXAnimation, "RenderTransform.ScaleX");
        storyboard.Asign(targetElement, scaleYAnimation, "RenderTransform.ScaleY");

        storyboard.Begin();

        return storyboard;
    }
    public static Storyboard PressDownColor(FrameworkElement targetElement, Color from, Color to)
    {
        var easingFunction = new BackEase { EasingMode = EasingMode.EaseOut };
        var timeSpan = TimeSpan.FromSeconds(0.3);


        var scaleXAnimation = new ColorAnimation
        {
            From = from,
            To = to,
            Duration = timeSpan,
            EasingFunction = easingFunction
        };


        var storyboard = new Storyboard();

        storyboard.Asign(targetElement, scaleXAnimation, "Background");
        storyboard.Begin();

        return storyboard;
    }



    public static Storyboard BreatheOpacity(FrameworkElement targetElement)
    {
        var easingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut};
        var timeSpan = TimeSpan.FromSeconds(3);

        var opacityAnimation = new DoubleAnimation
        {
            From = 0.50,
            To = 0.95,
            Duration = timeSpan,
            EasingFunction = easingFunction,

            AutoReverse = true, // Hace que la animación vaya y venga
            RepeatBehavior = RepeatBehavior.Forever,
        };

        var storyboard = new Storyboard();
        storyboard.Asign(targetElement, opacityAnimation, "Opacity");

        return storyboard;

    }

    public static Storyboard Opacity(FrameworkElement targetElement, double to, double duration)
    {
        var easingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
        var timeSpan = TimeSpan.FromSeconds(duration);

        var opacityAnimation = new DoubleAnimation
        {
            To = to,
            Duration = timeSpan,
            EasingFunction = easingFunction,
        };

        var storyboard = new Storyboard();
        storyboard.Asign(targetElement, opacityAnimation, "Opacity");

        return storyboard;

    }



    public static Storyboard TranslateCanvas(FrameworkElement targetElement, double toX, double toY, double duration)
    {
        var timeSpan = TimeSpan.FromSeconds(duration);

        // Animación para la posición X
        var moveXAnimation = new DoubleAnimation
        {
            To = toX,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };

        // Animación para la posición Y
        var moveYAnimation = new DoubleAnimation
        {
            To = toY,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };

        var storyboard = new Storyboard();
        storyboard.Asign(targetElement, moveXAnimation, Canvas.LeftProperty);
        storyboard.Asign(targetElement, moveYAnimation, Canvas.TopProperty);


        return storyboard;
    }

    public static Storyboard Translate(FrameworkElement targetElement, double toX, double toY, double duration, double? fromX = null, double? fromY = null)
    {
        var timeSpan = TimeSpan.FromSeconds(duration);

        // Crear o recuperar TranslateTransform
        var transform = targetElement.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            targetElement.RenderTransform = transform;
        }

        // Animación para la posición X
        var moveXAnimation = new DoubleAnimation
        {
            To = toX,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };
        if (fromX != null)
            moveXAnimation.From = fromX;
        
        // Animación para la posición Y
        var moveYAnimation = new DoubleAnimation
        {
            To = toY,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };
        if (fromY != null)
            moveYAnimation.From = fromY;


        var storyboard = new Storyboard();
        storyboard.Asign(targetElement, moveXAnimation, "RenderTransform.X");
        storyboard.Asign(targetElement, moveYAnimation, "RenderTransform.Y");

        return storyboard;
    }

    public static Storyboard Scale(FrameworkElement targetElement, double toX, double toY, double duration, double? fromX = null, double? fromY = null)
    {
        var timeSpan = TimeSpan.FromSeconds(duration);
        
        // Crear o recuperar TranslateTransform
        var transform = targetElement.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            targetElement.RenderTransform = transform;
        }

        // Animación para la posición X
        var scaleXAnimation = new DoubleAnimation
        {
            To = toX,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };
        if (fromX != null)
            scaleXAnimation.From = fromX;

        // Animación para la posición Y
        var scaleYAnimation = new DoubleAnimation
        {
            To = toY,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
        };
        if (fromY != null)
            scaleYAnimation.From = fromY;


        var storyboard = new Storyboard();

        if (targetElement is not Panel)
        {
            storyboard.Asign(targetElement, scaleXAnimation, "RenderTransform.ScaleX");
            storyboard.Asign(targetElement, scaleYAnimation, "RenderTransform.ScaleY");
        }
        else
        {
            Storyboard.SetTarget(scaleXAnimation, targetElement);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

            Storyboard.SetTarget(scaleYAnimation, targetElement);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);

        }

        return storyboard;
    }

    /// <summary>
    /// width and height
    /// </summary>
    /// <param name="targetElement"></param>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    /// <param name="duration"></param>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <returns></returns>
    public static Storyboard Size(FrameworkElement targetElement, double toX, double toY, double duration, double? fromX = null, double? fromY = null)
    {
        var timeSpan = TimeSpan.FromSeconds(duration);

        // Crear o recuperar TranslateTransform
        var transform = targetElement.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            targetElement.RenderTransform = transform;
        }

        // Animación para la posición X
        var scaleXAnimation = new DoubleAnimation
        {
            To = toX,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0, EasingMode = EasingMode.EaseOut }
        };
        if (fromX != null)
            scaleXAnimation.From = fromX;

        // Animación para la posición Y
        var scaleYAnimation = new DoubleAnimation
        {
            To = toY,
            Duration = timeSpan,
            EasingFunction = new BackEase { Amplitude = 0, EasingMode = EasingMode.EaseOut }
        };
        if (fromY != null)
            scaleYAnimation.From = fromY;


        var storyboard = new Storyboard();
        storyboard.Asign(targetElement, scaleXAnimation, "Width");
        storyboard.Asign(targetElement, scaleYAnimation, "Height");

        return storyboard;
    }



}