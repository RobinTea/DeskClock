using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.Drawing.Text;

namespace DesktopClockOverlay
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        private SolidColorBrush _selectedBrush = new SolidColorBrush(Colors.White);

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            LoadFonts();
            InitializeValues();

            FontComboBox.SelectionChanged += FontComboBox_SelectionChanged;
            FontSizeTextBox.TextChanged += FontSizeTextBox_TextChanged;
            PosXSlider.ValueChanged += PositionSlider_ValueChanged;
            PosYSlider.ValueChanged += PositionSlider_ValueChanged;
            CustomTextBox.TextChanged += CustomTextBox_TextChanged;
            ShowDateCheckBox.Checked += ShowDateCheckBox_Checked;
            ShowDateCheckBox.Unchecked += ShowDateCheckBox_Checked;
        }

        private void ShowDateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _mainWindow.SetShowDate(ShowDateCheckBox.IsChecked == true);
        }

        private void LoadFonts()
        {
            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (var font in fonts.Families)
            {
                FontComboBox.Items.Add(font.Name);
            }

            FontComboBox.SelectedItem = "Segoe UI";
        }

        private void InitializeValues()
        {
            var screens = Screen.AllScreens;
            var primaryScreen = Screen.PrimaryScreen;

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            PosXSlider.Minimum = primaryScreen.Bounds.Left;
            PosXSlider.Maximum = screenWidth - _mainWindow.Width;
            PosYSlider.Minimum = primaryScreen.Bounds.Top;
            PosYSlider.Maximum = screenHeight - _mainWindow.Height;

            FontSizeTextBox.Text = _mainWindow.ClockText.FontSize.ToString();
            PosXSlider.Value = _mainWindow.Left;
            PosYSlider.Value = _mainWindow.Top;
            CustomTextBox.Text = _mainWindow.CustomText ?? "";

            ShowDateCheckBox.IsChecked = _mainWindow.IsDateShown; //public property in MainWindow
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyStyle();
        }

        private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            if (FontComboBox.SelectedItem != null && double.TryParse(FontSizeTextBox.Text, out double fontSize))
            {
                string fontFamily = FontComboBox.SelectedItem.ToString();
                _mainWindow.UpdateClockStyle(fontFamily, fontSize, _selectedBrush);
            }
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mainWindow.UpdateClockPosition(PosXSlider.Value, PosYSlider.Value);
        }

        private void CustomTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _mainWindow.UpdateCustomText(CustomTextBox.Text);
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var drawingColor = dlg.Color;
                    var mediaColor = Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                    _selectedBrush = new SolidColorBrush(mediaColor);
                    ApplyStyle();
                }
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Optionally force apply again (useful if values changed but events didn't fire)
            ApplyStyle();
            _mainWindow.UpdateClockPosition(PosXSlider.Value, PosYSlider.Value);
            _mainWindow.UpdateCustomText(CustomTextBox.Text);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}

