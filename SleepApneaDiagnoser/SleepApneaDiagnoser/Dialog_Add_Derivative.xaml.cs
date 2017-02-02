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
using MahApps.Metro.Controls;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for Dialog_Derivative.xaml
  /// </summary>
  public partial class Dialog_Add_Derivative
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

    private string[] AllSignals;
    private string[] Signals;
    private MetroWindow window;
    private SettingsModelView model;

    public Dialog_Add_Derivative(MetroWindow i_window, SettingsModelView i_model, string[] i_Signals, string[] i_AllSignals)
    {
      InitializeComponent();

      Signals = i_Signals;
      AllSignals = i_AllSignals;

      for (int x = 0; x < i_Signals.Length; x++)
      {
        comboBox_Signal1.Items.Add(i_Signals[x]);
        comboBox_Signal2.Items.Add(i_Signals[x]);
      }

      window = i_window;
      model = i_model;
    }

    private void button_OK_Click(object sender, RoutedEventArgs e)
    {
      if (comboBox_Signal1.Text.Trim() != "" && comboBox_Signal2.Text.Trim() != "" && textBox_SignalName.Text.Trim() != "")
      {
        if (AllSignals.ToList().Contains(textBox_SignalName.Text.Trim()))
        {
          window.ShowMessageAsync("Error", "Please select a unique signal name.");
        }
        else
        {
          model.AddDerivativeOutput(SignalName, Signal1, Signal2);
          DialogManager.HideMetroDialogAsync(window, this);
        }
      }
      else
      {
        window.ShowMessageAsync("Error", "Please fill in empty fields.");
      }
    }
    private void button_Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogManager.HideMetroDialogAsync(window, this);
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
