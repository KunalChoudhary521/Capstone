using System;
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
using System.Windows.Shapes;

namespace SleepApneaDiagnoser
{
    /// <summary>
    /// Interaction logic for Dialog_Derivative.xaml
    /// </summary>
    public partial class Dialog_Add_Derivative : Window
    {
        public string SignalName
        {
            get
            {
                return textBox_SignalName.Text.Trim();
            }
        }
        public string Signal1
        {
            get
            {
                return comboBox_Signal1.Text;
            }
        }
        public string Signal2
        {
            get
            {
                return comboBox_Signal2.Text;
            }
        }

        private string[] DerivedSignals;
        private string[] Signals;

        public Dialog_Add_Derivative(string[] i_Signals, string[] i_DerivedSignals)
        {
            InitializeComponent();

            Signals = i_Signals;
            DerivedSignals = i_DerivedSignals;

            for (int x = 0; x < i_Signals.Length; x++)
            {
                comboBox_Signal1.Items.Add(i_Signals[x]);
                comboBox_Signal2.Items.Add(i_Signals[x]);
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_Signal1.Text.Trim() != "" && comboBox_Signal2.Text.Trim() != "" && textBox_SignalName.Text.Trim() != "")
            {
                if (Signals.ToList().Contains(textBox_SignalName.Text.Trim()) || DerivedSignals.ToList().Contains(textBox_SignalName.Text.Trim()))
                {
                    MessageBox.Show("Please select a unique signal name.");
                }
                else
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Please fill in empty fields.");
            }
        }
        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void comboBox_Signal1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBox_SignalName.Text = "(" + (comboBox_Signal1.SelectedItem == null ? "" : comboBox_Signal1.SelectedItem.ToString().Trim()) + ")-(" + 
                (comboBox_Signal2.SelectedItem == null ? "" : comboBox_Signal2.SelectedItem.ToString().Trim()) + ")";
        }
        private void comboBox_Signal2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBox_SignalName.Text = "(" + (comboBox_Signal1.SelectedItem == null ? "" : comboBox_Signal1.SelectedItem.ToString().Trim()) + ")-(" +
                (comboBox_Signal2.SelectedItem == null ? "" : comboBox_Signal2.SelectedItem.ToString().Trim()) + ")";
        }
    }
}
