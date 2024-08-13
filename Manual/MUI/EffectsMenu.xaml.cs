﻿using System;
using System.Collections.Generic;
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
using Manual.API;
using Manual.Core;
using Manual.Objects;

namespace Manual.MUI
{
    /// <summary>
    /// Lógica de interacción para EffectsMenu.xaml
    /// </summary>
    public partial class EffectsMenu : UserControl
    {
        public EffectsMenu()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is KeyValuePair<string, Func<Effect>> selectedEffect)
            {
                // Aquí puedes obtener la clave y la función del efecto seleccionado
              //  string effectKey = selectedEffect.Key;
                Func<Effect> effectFunc = selectedEffect.Value;

                // Aquí puedes crear una instancia del efecto utilizando la función y agregarlo a la colección deseada
                Effect effect = effectFunc.Invoke();
                ManualAPI.SelectedLayer.AddEffect(effect);
            }
        }

            private void Button_Click(object sender, RoutedEventArgs e)
        {

            Button button = (Button)sender;
            ContextMenu contextMenu = button.ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }
        }

    }
}