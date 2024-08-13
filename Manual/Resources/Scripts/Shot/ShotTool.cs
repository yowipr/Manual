using Manual.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Manual.MUI;
using static Manual.API.ManualAPI;
using Manual.Core;
using System.Windows;
using Manual.Editors;
using System.Windows.Media;
using Manual.Objects;
using Manual;
using static Python.Runtime.TypeSpec;

namespace Plugins
{
    [Export(typeof(IPlugin))]
    public class ShotTool : IPlugin
    {

        public void Initialize()
        {

            // TOOL PROPERTIES
            string iconPath = $"{App.LocalPath}Resources/Scripts/Shot/icon.png";
            Tool t = new();
            t.name = "Shot";
            t.iconPath = iconPath;
            space.ed_tools.AddTool(t);

            //CONTEXT
            t.body.DataContext = project;
            M_StackPanel stackPanelContext = new("SelectedShot");
            add(stackPanelContext, t.body);


            M_Expander inspector_shot = new(stackPanelContext, "Shot");
            inspector_shot.AddRange(
            [
                new M_TextBox(name: "Name", "", propertyBinding: "Name"),
                new Separator(),

            ]);

            // main camera
            M_StackPanel stackPanelContext_Camera = new("MainCamera");
            add(stackPanelContext_Camera, stackPanelContext);

            Image img = new();
            img.Width = 256;
            img.Height = 256;
           


            M_Expander inspector_camera = new(stackPanelContext_Camera, "Camera");
            inspector_camera.AddRange(
            [
              //  new M_TextBox(name: "Name", "", propertyBinding: "Name"),
              //  new Separator(),

                new M_Label("Position", true),
                new M_Grid("X", "PositionX", 100, 1),
                new M_Grid("Y", "PositionY", 100, 1),

                 new Separator(),

                new M_Label("Resolution", true),
                new M_SliderBox("X", "ImageWidth", 1, 2048, 1, 8, true),
                new M_SliderBox("Y", "ImageHeight", 1, 2048, 1, 8, true),

                new Separator(),

                 new M_Label("Background", true),
                 new M_SliderBox("Opacity", "BackgroundOpacity", 0, 1, 2000, 0.01, true),

            ]);




            //ListView listView = new();
            //listView.SetBinding(ListView.ItemsSourceProperty, "ShotsCollection");
            //listView.DisplayMemberPath = "Name";
            //Button b = new();
            //b.Content = "Add as Layer";
            //b.Click += (sender, args) =>
            //{
            //    Shot s = (Shot)listView.SelectedItem;
            //    AddLayerBase(new ShotLayer(s));
            //};



            //M_Expander inspector_shots = new(t.body, "Shots Collection");
            //inspector_shots.AddRange(new UIElement[]
            //{
            //  listView,
            //  b,

            //});


            //DISABLED RELEASE: shot
            if (AppModel.DebugMode)
            {
                var shotColl = new M_AssetCollection("ShotsCollection");
                shotColl.MouseDoubleClick += ShotColl_MouseDoubleClick;
                shotColl.AddMenuItem("Open", OpenItem_Click);
                shotColl.AddMenuItem("Close", CloseItem_Click);
                shotColl.AddMenuItem("Insert as Layer", InsertItem_Click);
                shotColl.AddMenuItem("Remove from Project", DeleteItem_Click);

                M_Expander inspector_shots = new(t.body, "Shots Collection");
                inspector_shots.AddRange(
                [
                   shotColl
                ]);
            }


        }

        private void ShotColl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var shot = AppModel.FindAncestorByDataContext<Shot>((FrameworkElement)e.OriginalSource);
            project.OpenShot(shot);
        }
     
        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var shot = ((MenuItem)sender).DataContext as Shot;
            project.OpenShot(shot);
        }

        private void CloseItem_Click(object sender, RoutedEventArgs e)
        {
            var shot = ((MenuItem)sender).DataContext as Shot;
            project.CloseShot(shot);
        }

        private void InsertItem_Click(object sender, RoutedEventArgs e)
        {
            var shot = ((MenuItem)sender).DataContext as Shot;
            AddLayerBase(new ShotLayer(shot));
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var shot = ((MenuItem)sender).DataContext as Shot;
            AppModel.ShowMiniDialog("", $"Delete {shot.Name}?",
                "Delete", ()=> project.DeleteShot(shot),
                "No", null
                );

    
            
            
        }






    }

}
