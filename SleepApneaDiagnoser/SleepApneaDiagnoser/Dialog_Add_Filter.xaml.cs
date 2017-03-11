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
  /// Interaction logic for Dialog_Add_Filter.xaml
  /// </summary>
  public partial class Dialog_Add_Filter
  {
    public FilteredSignal filteredSignal
    {
      get
      {
        FilteredSignal filteredSignal = new FilteredSignal();

        filteredSignal.SignalName = textBox_SignalName.Text;
        filteredSignal.OriginalName = comboBox_Signal.SelectedItem.ToString();

        if (checkBox_ApplyLowPass.IsChecked == true)
        {
          filteredSignal.LowPassCutoff = float.Parse(textBox_LowPassCutoff.Text);
          filteredSignal.LowPass_Enabled = true;
        }

        if (checkBox_ApplySmoothing.IsChecked == true)
        {
          filteredSignal.Average_Length = float.Parse(textBox_SmoothingLength.Text);
          filteredSignal.Average_Enabled = true;
        }

        return filteredSignal;
      }
    }

    List<string> AllSignals_NoFiltered;
    string[] AllSignals;
    MainWindow window;
    SettingsModelView model;

    public Dialog_Add_Filter(MainWindow in_window, SettingsModelView in_model, string[] in_EDFSignals, string[] in_Derivatives, string[] in_AllSignals)
    {
      InitializeComponent();

      model = in_model;
      window = in_window;
      AllSignals = in_AllSignals;

      AllSignals_NoFiltered = in_EDFSignals.ToList();
      AllSignals_NoFiltered.AddRange(in_Derivatives);

      for (int x = 0; x < AllSignals_NoFiltered.Count; x++)
        comboBox_Signal.Items.Add(AllSignals_NoFiltered[x]);
    }

    private void button_Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogManager.HideMetroDialogAsync(window, this);
    }
    private void button_OK_Click(object sender, RoutedEventArgs e)
    {
      if (checkBox_ApplyLowPass.IsChecked ?? false)
      {
        float test;
        if (!float.TryParse(textBox_LowPassCutoff.Text, out test))
        {
          window.ShowMessageAsync("Error", "Low Pass Cutoff (Hz) must be a number");
          return;
        }
      }
      if (checkBox_ApplySmoothing.IsChecked ?? false)
      {
        float test;
        if (!float.TryParse(textBox_SmoothingLength.Text, out test))
        {
          window.ShowMessageAsync("Error", "Smoothing Filter Length (ms) must be a number");
          return;
        }
      }
      if (AllSignals.ToList().Contains(textBox_SignalName.Text.Trim()))
      {
        window.ShowMessageAsync("Error", "Please choose a unique signal name");
        return;
      }
      if (!(checkBox_ApplyLowPass.IsChecked ?? false) && !(checkBox_ApplySmoothing.IsChecked ?? false))
      {
        window.ShowMessageAsync("Error", "Please select a filter to apply");
        return;
      }

      model.AddFilterOutput(filteredSignal);
      DialogManager.HideMetroDialogAsync(window, this);
    }
    
    private void AutoRenameSignal()
    {
      try
      {
        string Action = "";
        if (checkBox_ApplyLowPass.IsChecked ?? false)
          Action += "Low Pass ";
        if (checkBox_ApplySmoothing.IsChecked ?? false)
          Action += "Smoothed ";
        Action = Action.Trim();

        textBox_SignalName.Text = comboBox_Signal.SelectedItem.ToString() + " (" + Action + ")";
      }
      catch
      {

      }
    }

    private void comboBox_Signal_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      AutoRenameSignal();
    }
    private void checkBox_ApplyLowPass_Checked(object sender, RoutedEventArgs e)
    {
      AutoRenameSignal();
    }
    private void checkBox_ApplySmoothing_Checked(object sender, RoutedEventArgs e)
    {
      AutoRenameSignal();
    }
    private void checkBox_ApplyLowPass_Unchecked(object sender, RoutedEventArgs e)
    {
      AutoRenameSignal();
    }
    private void checkBox_ApplySmoothing_Unchecked(object sender, RoutedEventArgs e)
    {
      AutoRenameSignal();
    }

  }
}
