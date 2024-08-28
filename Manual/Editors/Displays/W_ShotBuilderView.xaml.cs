using Manual.Core;
using Manual.Editors.Displays;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using System.Windows.Shapes;

namespace Manual.Editors.Displays;

/// <summary>
/// Lógica de interacción para W_ShotBuilderView.xaml
/// </summary>
public partial class W_ShotBuilderView : W_WindowContent
{
    public W_ShotBuilderView()
    {
        InitializeComponent();
        DataContext = AppModel.project.ShotBuilder;
    }

    public override void OnWindowOpening()
    {
        window.Width = 600;
        window.Height = 400;
        window.SetCenterToScreen();
    }
    private void Template_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var d = ((FrameworkElement)sender).DataContext as ShotTemplate;

        if(d != null)
          AppModel.project.ShotBuilder.SelectedTemplate = d;
    }


    private void Create_Click(object sender, RoutedEventArgs e)
    {
        //DISABLED RELEASE: shot
         AppModel.project.ShotBuilder.CreateSelected();
        //Shot.defaultTemplate = AppModel.project.ShotBuilder.RealSelectedTemplate;
        //AppModel.File_New();
        Close_Click(sender, e);
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close_Click(sender, e);
    }
}


public class AspectRatioToSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is int originalWidth && values[1] is int originalHeight)
        {
            double maxSize = 100.0;
            double width = originalWidth;
            double height = originalHeight;
            double aspectRatio = (double)originalWidth / originalHeight;

            if (originalWidth > originalHeight)
            {
                if (originalWidth > maxSize)
                {
                    width = maxSize;
                    height = maxSize / aspectRatio;
                }
            }
            else
            {
                if (originalHeight > maxSize)
                {
                    height = maxSize;
                    width = maxSize * aspectRatio;
                }
            }

            if (parameter != null && parameter.ToString() == "Width")
            {
                return width;
            }
            if (parameter != null && parameter.ToString() == "Height")
            {
                return height;
            }
        }

        return 100.0; // Default value if something goes wrong
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}