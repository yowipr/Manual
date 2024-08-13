using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManualToolkit.Generic;
using ManualToolkit.Windows;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLauncher.Sections;

/// <summary>
/// Lógica de interacción para AdminPanelView.xaml
/// </summary>
public partial class AdminPanelView : UserControl
{
    public AdminPanelView()
    {
        InitializeComponent();
    }

    private void SvgPath_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void SvgPath_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void SvgPath_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                // Solo toma el primer archivo si hay varios
                string svgPath = files[0];

                // Asigna el valor al TextBox
                if (DataContext is AdminPanel viewModel)
                {
                  //  viewModel.SvgPath = svgPath;
                }
            }
        }
    }
}


public partial class AdminPanel : ObservableObject
{
    [ObservableProperty] string iconName = "icono";
    [ObservableProperty] string svgContent = "";
    [ObservableProperty] int size = 48;

    [RelayCommand]
    public async void ConvertSvgToPng()
    {
         string pPath = $"{Launch.SolutionPath}\\ManualToolkit\\Assets\\Icons\\{IconName}.png";

          bool success = await FileManager.ConvertSvgToPng(SvgContent, pPath, Size, Size);
          Output.Log($"SvgToPng succed:{success}");
        if (!success)
            MessageBox.Show("something went wrong, maybe you need to install inkscape, missing inskape in C:/Program Files/Inkscape/bin/inkscape.exe");
    }
}
