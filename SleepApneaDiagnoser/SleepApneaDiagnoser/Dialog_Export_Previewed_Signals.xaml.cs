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
    public static ExportSignalModel signals_to_export;
    private List<string> selected_signals;
    public Dialog_Export_Previewed_Signals(List<string> selected_signals)
    {
      InitializeComponent();

      this.selected_signals = new List<string>(selected_signals);

      this.textBox_subject_id.Text = "";
      this.textBox_epochs_from.Text = "1";
      this.textBox_epochs_to.Text = "10";
    }

    private void button_export_cancel_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
      this.Close();
    }

    //TODO: ZUR(2016-11-22)
    //There is probably a much better way of doing this, but at the moment I just did this...
    //Can come back and clean it up later when we have the time and do it the better way
    private void button_export_finish_Click(object sender, RoutedEventArgs e)
    {
      int subject_id;
      if (string.IsNullOrEmpty(this.textBox_subject_id.Text))
      {
        this.ShowMessageAsync("Error", "Please enter a subject ID");
      }
      else if (int.TryParse(this.textBox_subject_id.Text, out subject_id) == false)
      {
        this.ShowMessageAsync("Error", "Please enter a valid subject ID");
      }
      else
      {
        if (subject_id < 0)
        {
          this.ShowMessageAsync("Error", "Please enter a positive two digit ID");
        }
        else
        {
          //just in case
          subject_id = subject_id % 100; 
          int from_epochs;
          if (int.TryParse(this.textBox_epochs_from.Text, out from_epochs) == false)
          {
            this.ShowMessageAsync("Error", "Please enter a valid 'from' epochs value.");
          }
          else
          {
            if (from_epochs < 1)
            {
              this.ShowMessageAsync("Error", "From epochs must be greater than 1");
            }
            else
            {
              int to_epochs;
              if (int.TryParse(this.textBox_epochs_to.Text, out to_epochs) == false)
              {
                this.ShowMessageAsync("Error", "Please enter a valid 'to' epochs values.");
              }
              else
              {
                if (to_epochs < 0 || to_epochs <= from_epochs)
                {
                  this.ShowMessageAsync("Error", "To epochs must be greater than from epochs.");
                }
                else
                {
                  signals_to_export = new ExportSignalModel(subject_id, from_epochs, to_epochs);
                  this.DialogResult = true;
                  this.Close();
                }
              }
            }
          }
        }
      }
    }
  }
}