using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ManualToolkit.Themes
{
    public partial class Input : UserControl
    {
        public Input()
        {
            InitializeComponent();
        }

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(Input), new PropertyMetadata(null));

        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(Input), new PropertyMetadata("message"));

        public bool IsTextEntered
        {
            get { return (bool)GetValue(IsTextEnteredProperty); }
            private set { SetValue(IsTextEnteredProperty, value); }
        }

        public static readonly DependencyProperty IsTextEnteredProperty =
            DependencyProperty.Register("IsTextEntered", typeof(bool), typeof(Input), new PropertyMetadata(false));

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsTextEntered = !string.IsNullOrEmpty(((TextBox)sender).Text);
        }


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

  
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
    nameof(Text),
    typeof(string),
    typeof(Input),
    new FrameworkPropertyMetadata(
        "",
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        OnValueChanged,
        CoerceValue,
        true,
        UpdateSourceTrigger.PropertyChanged));

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            // Aquí puedes agregar lógica para validar el valor antes de establecerlo
            return baseValue;
        }
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Aquí puedes agregar lógica para responder al cambio de valor
        }

        //public string Text
        //{
        //    get => textBox.Text;
        //    set => textBox.Text = value;
        //}

        public void FocusText()
        {
            textBox.Focus();
        }
        private void UserControl_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FocusText();
        }



    }
}
