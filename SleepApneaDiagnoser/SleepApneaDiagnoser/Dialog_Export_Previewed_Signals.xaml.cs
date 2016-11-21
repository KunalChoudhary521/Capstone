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
using MahApps.Metro.Controls.Dialogs;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for Dialog_Export_Previewed_Signals.xaml
  /// </summary>
  public partial class Dialog_Export_Previewed_Signals
  {
    public static List<ExportSignalModel> signals_to_export;
    private int signal_number;
    private List<string> selected_signals;
    public Dialog_Export_Previewed_Signals(List<string> selected_signals)
    {
      InitializeComponent();

      this.selected_signals = new List<string>(selected_signals);
      signals_to_export = new List<ExportSignalModel>();
      signal_number = 0;

      this.textBox_signal_name.Text = selected_signals.First();
      this.textBox_epochs_from.Text = "0";
      this.textBox_epochs_to.Text = "10";
      button_next_update();
    }

    private void button_export_cancel_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
      this.Close();
    }

    private void button_export_next_finish_Click(object sender, RoutedEventArgs e)
    {
      add_signal();
      if (signal_number < selected_signals.Count - 1)
      {
        signal_number++;
        this.textBox_signal_name.Text = selected_signals[signal_number];
        this.textBox_epochs_from.Text = "0";
        this.textBox_epochs_to.Text = "10";
        button_next_update();
      }
      else {      
        this.DialogResult = true;
        this.Close();
      }
    }

    private void button_next_update() {
      if (signal_number < selected_signals.Count - 1)
      {
        this.button_export_next_finish.Content = "Next";
      }
      else
      {
        this.button_export_next_finish.Content = "Export";
      }
    }

    private void add_signal() {
      ExportSignalModel signal = new ExportSignalModel(selected_signals[signal_number], textBox_epochs_from.Text, textBox_epochs_to.Text);
      signals_to_export.Add(signal);
    }
  }
}
