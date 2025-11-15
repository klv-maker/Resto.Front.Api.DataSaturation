using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Resto.Front.Api.DataSaturation.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            // Use SelectionStart property to find the caret position.
            // Insert the previewed text into the existing text in the textbox.
            Dispatcher.Invoke(() =>
            {
                if (string.Equals(textBox.Text, "0"))
                {
                    textBox.Text = textBox.Text.Replace("0", "");
                }
                if (!Regex.IsMatch(textBox.Text, "\\d*") && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    int txtPos = textBox.SelectionStart - 1;
                    textBox.Text = textBox.Text.Remove(txtPos, 1);
                    textBox.SelectionStart = txtPos;
                }

                int txtPosReplaced = textBox.SelectionStart;
                textBox.Text = textBox.Text.Replace(" ", "");
                if (txtPosReplaced > 0)
                    textBox.SelectionStart = txtPosReplaced;
            });
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsNumber);
        }
    }
}
