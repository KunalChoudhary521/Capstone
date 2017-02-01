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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using MATLAB_496;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro;
using System.Windows.Forms;
using EEGBandpower;
using PSD_Welch;
using EEGSpec;

using System.Numerics;
using MathNet.Filtering;
using MathNet.Numerics;
using System.Diagnostics;
using System.Threading;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    ModelView modelview;
    RespiratoryModelView resp_modelview;
    SettingsModel settings_model;

    /// <summary>
    /// Function called to populate recent files list. Called when application is first loaded and if the recent files list changes.
    /// </summary>
    public void LoadRecent()
    {
      List<string> array = modelview.RecentFiles.ToArray().ToList();

      itemControl_RecentEDF.Items.Clear();
      for (int x = 0; x < array.Count; x++)
        if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
          itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
    }

    /// <summary>
    /// Constructor for GUI class.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      settings_model = new SettingsModel();
      modelview = new ModelView(this, settings_model);
      this.DataContext = modelview;
      this.grid_SettingsMainMenu.DataContext = modelview;
      this.grid_SettingsPersonalization.DataContext = modelview;

      resp_modelview = new RespiratoryModelView(modelview, settings_model);
      this.grid_SettingsRespiratory.DataContext = resp_modelview;
      this.TabItem_Respiratory.DataContext = resp_modelview;
            
      LoadRecent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      modelview.LoadAppSettings();
    }
    private void Window_Closing(object sender, CancelEventArgs e)
    {
      modelview.WriteAppSettings();
      modelview.WriteEDFSettings();
    }
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (this.WindowState != WindowState.Maximized)
      {
        if (this.Width < 1000)
        {
          column_EDFHeader.Width = new GridLength(0);
          column_EDFHeader.MaxWidth = 0;
          column_EDFHeader.MinWidth = 0;
        }
        else
        {
          column_EDFHeader.Width = new GridLength(300);
          column_EDFHeader.MaxWidth = 300;
          column_EDFHeader.MinWidth = 300;
        }
      }
    }
    private void Window_StateChanged(object sender, EventArgs e)
    {
      switch (this.WindowState)
      {
        case WindowState.Maximized:
          column_EDFHeader.Width = new GridLength(300);
          column_EDFHeader.MaxWidth = 300;
          column_EDFHeader.MinWidth = 300;
          break;
      }
    }

    // Home Tab Events
    private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
      dialog.Filter = "EDF files (*.edf)|*.edf";
      dialog.Title = "Select an EDF file";

      if (dialog.ShowDialog() == true)
      {
        modelview.LoadEDFFile(dialog.FileName);
      }       
    }
    private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
    {
      List<string> array = modelview.RecentFiles.ToArray().ToList();
      List<string> selected = array.Where(temp => temp.Split('\\')[temp.Split('\\').Length - 1] == ((Hyperlink)sender).Inlines.FirstInline.DataContext.ToString()).ToList();

      if (selected.Count == 0)
      {
        this.ShowMessageAsync("Error", "File not Found");
        LoadRecent();
      }
      else
      {
        for (int x = 0; x < selected.Count; x++)
        {
          if (File.Exists(selected[x]))
          {
            modelview.LoadEDFFile(selected[x]);
            break;
          }
          else
          {
            this.ShowMessageAsync("Error", "File not Found");
            modelview.RecentFiles_Remove(selected[x]);
          }
        }
      }
    }

    // Setting Flyout Events 
    private void button_Settings_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.SettingsMainMenuVisible = true;
      modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_MainMenuClick(object sender, RoutedEventArgs e)
    {
      modelview.SettingsMainMenuVisible = true;
      modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_PersonalizationSettings_Click(object sender, RoutedEventArgs e)
    {
      modelview.SettingsMainMenuVisible = false;
      modelview.SettingsPersonalizationVisible = true;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_RespiratorySettings_Click(object sender, RoutedEventArgs e)
    {
      modelview.SettingsMainMenuVisible = false;
      modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = true;
    }
    private void button_HideSignals_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.HideSignals();
    }
    private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.AddDerivative();
    }
    private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.RemoveDerivative();
    }
    private void button_Categories_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.ManageCategories();
    }
    private void button_AddFilter_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.AddFilter();
    }
    private void button_RemoveFilter_Click(object sender, RoutedEventArgs e)
    {
      modelview.OpenCloseSettings();
      modelview.RemoveFilter();
    }

    // Preview Tab Events   
    private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      modelview.SetSelectedSignals(listBox_SignalSelect.SelectedItems);
    }
    private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void toggleButton_UseAbsoluteTime_Checked(object sender, RoutedEventArgs e)
    {
      timePicker_From_Abs.Visibility = Visibility.Visible;
      timePicker_From_Eph.Visibility = Visibility.Hidden;
    }
    private void toggleButton_UseAbsoluteTime_Unchecked(object sender, RoutedEventArgs e)
    {
      timePicker_From_Abs.Visibility = Visibility.Hidden;
      timePicker_From_Eph.Visibility = Visibility.Visible;
    }
    
    private void button_Next_Click(object sender, RoutedEventArgs e)
    {
      modelview.NextCategory();
    }
    private void button_Prev_Click(object sender, RoutedEventArgs e)
    {
      modelview.PreviousCategory();
    }

    private void button_ExportBinary_Click(object sender, RoutedEventArgs e)
    {
      modelview.ExportSignals();
    }
    private void button_ExportImage_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
      dialog.Filter = "Image File (*.png)|*.png";
      dialog.Title = "Select an EDF file";

      if (dialog.ShowDialog() == true)
      {
        modelview.ExportImage(dialog.FileName);
      }
    }

    // Analysis Tab Events 
    private void button_EDFEEGAnalysis_Click(object sender, RoutedEventArgs e)
    {
      modelview.PerformEEGAnalysisEDF();
    }
    private void button_ExportEEGCalculations_Click(object sender, RoutedEventArgs e)
    {
      modelview.ExportEEGCalculations();
    }
    private void button_BINRespiratoryAnalysis_Click(object sender, RoutedEventArgs e)
    {
      resp_modelview.LoadRespiratoryAnalysisBinary();
    }
    private void button_BINEEGAnalysis_Click(object sender, RoutedEventArgs e)
    {
      modelview.PerformEEGAnalysisBinary();
    }
  }

  public class ModelView : INotifyPropertyChanged
  {
    #region Helper Functions
    /// <summary>
    /// From a signal, returns a series of X,Y values for use with a PlotModel
    /// Also returns y axis information and the sample_period of the signal
    /// </summary>
    /// <param name="sample_period"> Variable to contain the sample period of the signal </param>
    /// <param name="Signal"> The input signal name </param>
    /// <param name="StartTime">  The input start time to be contained in the series </param>
    /// <param name="EndTime"> The input end time to be contained in the series </param>
    /// <returns> The series of X,Y values to draw on the plot </returns>
    public LineSeries GetSeriesFromSignalName(out float sample_period, string Signal, DateTime StartTime, DateTime EndTime)
    {
      // Variable To Return
      LineSeries series = new LineSeries();

      // Check if this signal needs filtering 
      bool filter = false;
      FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);
      if (filteredSignal != null)
      {
        filter = true;
        Signal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal).OriginalName;
      }

      // Get Signal
      if (EDFAllSignals.Contains(Signal))
      {
        // Get Signal
        EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal);

        // Determine Array Portion
        sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

        // Get Array
        List<float> values = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal, StartTime, EndTime);

        // Add Points to Series
        for (int y = 0; y < values.Count; y++)
        {
          series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(StartTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
        }
      }
      else // Derivative Signal
      {
        // Get Signals
        DerivativeSignal deriv_info = sm.DerivedSignals.Find(temp => temp.DerivativeName == Signal);
        EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal1Name.Trim());
        EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal2Name.Trim());

        // Get Arrays and Perform Resampling if needed
        List<float> values1;
        List<float> values2;
        if (edfsignal1.NumberOfSamplesPerDataRecord == edfsignal2.NumberOfSamplesPerDataRecord) // No resampling
        {
          values1 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal1, StartTime, EndTime);
          values2 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal2, StartTime, EndTime);
          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
        }
        else if (edfsignal1.NumberOfSamplesPerDataRecord > edfsignal2.NumberOfSamplesPerDataRecord) // Upsample signal 2
        {
          values1 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal1, StartTime, EndTime);
          values2 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal2, StartTime, EndTime);
          values2 = Utils.MATLAB_Resample(values2.ToArray(), edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord);
          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
        }
        else // Upsample signal 1
        {
          values1 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal1, StartTime, EndTime);
          values2 = Utils.retrieveSignalSampleValuesMod(LoadedEDFFile, edfsignal2, StartTime, EndTime);
          values1 = Utils.MATLAB_Resample(values1.ToArray(), edfsignal2.NumberOfSamplesPerDataRecord / edfsignal1.NumberOfSamplesPerDataRecord);
          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal2.NumberOfSamplesPerDataRecord;
        }

        // Add Points to Series
        for (int y = 0; y < Math.Min(values1.Count, values2.Count); y++)
        {
          series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(StartTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values1[y] - values2[y]));
        }
      }

      if (filter == true)
      {
        if (filteredSignal.LowPass_Enabled)
        {
          series = Utils.ApplyLowPassFilter(series, filteredSignal.LowPassCutoff, sample_period);
        }
        if (filteredSignal.WeightedAverage_Enabled)
        {
          float LENGTH;
          LENGTH = Math.Max(filteredSignal.WeightedAverage_Length / (sample_period * 1000), 1);

          series = Utils.ApplyWeightedAverageFilter(series, LENGTH);
        }
      }

      return series;
    }
    #endregion

    #region Actions

    /*********************************************************** HOME TAB ***********************************************************/

    // Load EDF File
    /// <summary>
    /// Used to control progress bar shown when edf file is being loaded
    /// </summary>
    private ProgressDialogController controller;
    /// <summary>
    /// The background worker that runs the task that updates the progress bar value
    /// </summary>
    private BackgroundWorker bw_progressbar = new BackgroundWorker();
    /// <summary>
    /// Background task that updates the progress bar
    /// </summary>
    private void BW_LoadEDFFileUpDateProgress(object sender, DoWorkEventArgs e)
    {
      long process_start = Process.GetCurrentProcess().PagedMemorySize64;
      long file_size = (long)(new FileInfo(e.Argument.ToString()).Length * 2.2);
      long current_progress = 0;

      while (!bw_progressbar.CancellationPending)
      {
        current_progress = Math.Max(current_progress, Process.GetCurrentProcess().PagedMemorySize64 - process_start);
        double progress = Math.Min(99, (current_progress * 100 / (double)file_size));

        controller.SetProgress(progress);
      }
    }
    /// <summary>
    /// Background process for loading edf file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
    {
      // Progress Bar should not be cancelable
      controller.SetCancelable(false);
      controller.Maximum = 100;

      // 'Update Progress Bar' Task 
      bw_progressbar = new BackgroundWorker();
      bw_progressbar.WorkerSupportsCancellation = true;
      bw_progressbar.DoWork += BW_LoadEDFFileUpDateProgress;
      bw_progressbar.RunWorkerAsync(e.Argument.ToString());

      // Read EDF File
      EDFFile temp = new EDFFile();
      temp.readFile(e.Argument.ToString());
      LoadedEDFFile = temp;

      // Load Settings Files
      LoadEDFSettings();

      // End 'Update Progress Bar' Task 
      bw_progressbar.CancelAsync();
      while (bw_progressbar.IsBusy)
      { }
    }
    /// <summary>
    /// Function called after background process for loading edf file finishes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
    {
      // Add loaded EDF file to Recent Files list
      RecentFiles_Add(LoadedEDFFileName);

      // Close progress bar and display message
      await controller.CloseAsync();
      await p_window.ShowMessageAsync("Success!", "EDF file loaded");
    }
    /// <summary>
    /// Loads an EDF File into memory
    /// </summary>
    /// <param name="fileNameIn"> Path to the EDF file to load </param>
    public async void LoadEDFFile(string fileNameIn)
    {
      controller = await p_window.ShowProgressAsync("Please wait...", "Loading EDF File: " + fileNameIn);

      WriteEDFSettings();
      LoadedEDFFile = null;

      LoadedEDFFileName = fileNameIn;
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_LoadEDFFile;
      bw.RunWorkerCompleted += BW_FinishLoad;
      bw.RunWorkerAsync(LoadedEDFFileName);
    }

    /********************************************************* PREVIEW TAB **********************************************************/

    /// <summary>
    /// In the preview tab, displays signals belonging to the next category
    /// </summary>
    public void NextCategory()
    {
      if (PreviewCurrentCategory == sm.SignalCategories.Count - 1)
        PreviewCurrentCategory = -1;
      else
        PreviewCurrentCategory++;
    }
    /// <summary>
    /// In the preview tab, displays signals belonging to the previous category
    /// </summary>
    public void PreviousCategory()
    {
      if (PreviewCurrentCategory == -1)
        PreviewCurrentCategory = sm.SignalCategories.Count - 1;
      else
        PreviewCurrentCategory--;
    }

    /// <summary>
    /// Background process for drawing preview chart
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_CreateChart(object sender, DoWorkEventArgs e)
    {
      // Create temporary plot model class
      PlotModel temp_PreviewSignalPlot = new PlotModel();
      temp_PreviewSignalPlot.Series.Clear();
      temp_PreviewSignalPlot.Axes.Clear();

      if (pm.PreviewSelectedSignals.Count > 0)
      {
        // Create X Axis and add to plot model
        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.Key = "DateTime";
        xAxis.Minimum = DateTimeAxis.ToDouble(PreviewViewStartTime);
        xAxis.Maximum = DateTimeAxis.ToDouble(PreviewViewEndTime);
        temp_PreviewSignalPlot.Axes.Add(xAxis);

        // In the background create series for every chart to be displayed
        List<BackgroundWorker> bw_array = new List<BackgroundWorker>();
        LineSeries[] series_array = new LineSeries[pm.PreviewSelectedSignals.Count];
        LinearAxis[] axis_array = new LinearAxis[pm.PreviewSelectedSignals.Count];
        for (int x = 0; x < pm.PreviewSelectedSignals.Count; x++)
        {
          BackgroundWorker bw = new BackgroundWorker();
          bw.DoWork += new DoWorkEventHandler(
            delegate (object sender1, DoWorkEventArgs e1)
            {
              // Get Series for each signal
              int y = (int)e1.Argument;
              float sample_period;
              LineSeries series = GetSeriesFromSignalName(out sample_period,
                                                          pm.PreviewSelectedSignals[y],
                                                          (PreviewViewStartTime ?? new DateTime()),
                                                          PreviewViewEndTime
                                                          );

              series.YAxisKey = pm.PreviewSelectedSignals[y];
              series.XAxisKey = "DateTime";

              // Create Y Axis for each signal
              LinearAxis yAxis = new LinearAxis();
              yAxis.MajorGridlineStyle = LineStyle.Solid;
              yAxis.MinorGridlineStyle = LineStyle.Dot;
              yAxis.Title = pm.PreviewSelectedSignals[y];
              yAxis.Key = pm.PreviewSelectedSignals[y];
              yAxis.EndPosition = (double)1 - (double)y * ((double)1 / (double)pm.PreviewSelectedSignals.Count);
              yAxis.StartPosition = (double)1 - (double)(y + 1) * ((double)1 / (double)pm.PreviewSelectedSignals.Count);
              if (PreviewUseConstantAxis)
              {
                yAxis.Maximum = Utils.GetMaxSignalValue(pm.PreviewSelectedSignals[y], false, LoadedEDFFile, sm);
                yAxis.Minimum = Utils.GetMinSignalValue(pm.PreviewSelectedSignals[y], false, LoadedEDFFile, sm);
              }
              series_array[y] = series;
              axis_array[y] = yAxis;
            }
          );
          bw.RunWorkerAsync(x);
          bw_array.Add(bw);
        }

        // Wait for all background processes to finish then add all series and y axises to plot model
        bool all_done = false;
        while (!all_done)
        {
          all_done = true;
          for (int y = 0; y < bw_array.Count; y++)
          {
            if (bw_array[y].IsBusy)
              all_done = false;
          }
        }
        for (int y = 0; y < series_array.Length; y++)
        {
          temp_PreviewSignalPlot.Series.Add(series_array[y]);
          temp_PreviewSignalPlot.Axes.Add(axis_array[y]);
        }
      }

      PreviewSignalPlot = temp_PreviewSignalPlot;
    }
    /// <summary>
    /// Draws a chart in the Preview tab 
    /// </summary>
    public void DrawChart()
    {
      PreviewNavigationEnabled = false;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_CreateChart;
      bw.RunWorkerAsync();
    }

    // Export Previewed/Selected Signals Wizard
    public async void ExportSignals()
    {
      if (pm.PreviewSelectedSignals.Count > 0)
      {
        Dialog_Export_Previewed_Signals dlg = new Dialog_Export_Previewed_Signals(pm.PreviewSelectedSignals);

        controller = await p_window.ShowProgressAsync("Export", "Exporting preview signals to binary...");

        controller.SetCancelable(false);

        if (dlg.ShowDialog() == true)
        {
          FolderBrowserDialog folder_dialog = new FolderBrowserDialog();

          string location;

          if (folder_dialog.ShowDialog() == DialogResult.OK)
          {
            location = folder_dialog.SelectedPath;
          }
          else
          {
            await p_window.ShowMessageAsync("Cancelled", "Action was cancelled.");

            await controller.CloseAsync();

            return;
          }

          ExportSignalModel signals_data = Dialog_Export_Previewed_Signals.signals_to_export;

          BackgroundWorker bw = new BackgroundWorker();
          bw.DoWork += BW_ExportSignals;
          bw.RunWorkerCompleted += BW_FinishExportSignals;

          List<dynamic> arguments = new List<dynamic>();
          arguments.Add(signals_data);
          arguments.Add(location);

          int end_epochs = signals_data.Epochs_From + signals_data.Epochs_Length;
          if (end_epochs > int.Parse(EDFEndEpoch))
          {
            end_epochs = int.Parse(EDFEndEpoch) - signals_data.Epochs_From + 1;
          }
          if (end_epochs <= 0)
          {
            await controller.CloseAsync();

            await p_window.ShowMessageAsync("Export Cancelled", "No data to export in the length specified");
          }
          else
          {
            bw.RunWorkerAsync(arguments);
          }
        }
        else
        {

          await controller.CloseAsync();

          await p_window.ShowMessageAsync("Export Cancelled", "Export was Cancelled");
        }
      }
      else
      {
        await controller.CloseAsync();

        await p_window.ShowMessageAsync("Error", "Please select at least one signal from the preview.");
      }
    }
    private async void BW_FinishExportSignals(object sender, RunWorkerCompletedEventArgs e)
    {
      await controller.CloseAsync();

      await p_window.ShowMessageAsync("Export Success", "Previewed signals were exported to Binary");
    }
    private void BW_ExportSignals(object sender, DoWorkEventArgs e)
    {
      ExportSignalModel signals_data = ((List<dynamic>)e.Argument)[0];
      string location = ((List<dynamic>)e.Argument)[1];

      foreach (var signal in pm.PreviewSelectedSignals)
      {
        EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == signal);
        LineSeries derivative = null;

        if (edfsignal == null)
        {
          bool foundInDerived = false;
          //float derivedSamplePeriod = 0;
          float derivedSampleFrequency;
          EDFSignal oneDerivedEdfSignal = null;

          // look for the signal in the derviatives
          foreach (var derivedSignal in sm.DerivedSignals) {
            if (derivedSignal.DerivativeName == signal) {
              foundInDerived = true;
              oneDerivedEdfSignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == derivedSignal.Signal1Name);
              //derivedSamplePeriod = LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)oneDerivedEdfSignal.NumberOfSamplesPerDataRecord; ;
              derivedSampleFrequency = (float)oneDerivedEdfSignal.NumberOfSamplesPerDataRecord / LoadedEDFFile.Header.DurationOfDataRecordInSeconds;
              break;
            }
          }

          if (foundInDerived) {
            DateTime startTime = Utils.EpochtoDateTime(signals_data.Epochs_From, LoadedEDFFile); // epoch start
            int end_epochs = signals_data.Epochs_From + signals_data.Epochs_Length;
            if (end_epochs > int.Parse(EDFEndEpoch)) {
              end_epochs = int.Parse(EDFEndEpoch) + 1;
            }
            DateTime endTime = Utils.EpochtoDateTime(end_epochs, LoadedEDFFile); // epoch end

            LineSeries derivedSeries = GetSeriesFromSignalName(out derivedSampleFrequency, signal, startTime, endTime);

            FileStream hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate);
            hdr_file.SetLength(0); //clear it's contents
            hdr_file.Close(); //flush
            hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate); //reload

            StringBuilder sb_hdr = new StringBuilder(); // string builder used for writing into the file

            sb_hdr.AppendLine(signal) // name
                .AppendLine(signals_data.Subject_ID.ToString()) // subject id
                .AppendLine(Utils.EpochtoDateTime(signals_data.Epochs_From, LoadedEDFFile).ToString()) // epoch start
                .AppendLine(Utils.EpochtoDateTime(end_epochs, LoadedEDFFile).ToString()) // epoch length
                .AppendLine((1 / derivedSampleFrequency).ToString()); // sample frequency 

            var bytes_to_write = Encoding.ASCII.GetBytes(sb_hdr.ToString());
            hdr_file.Write(bytes_to_write, 0, bytes_to_write.Length);
            hdr_file.Close();

            FileStream bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //the binary file for each signal
            bin_file.SetLength(0); //clear it's contents
            bin_file.Close(); //flush


            #region signal_binary_contents

            bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //reload
            BinaryWriter bin_writer = new BinaryWriter(bin_file);

            int start_index = 0;
            int end_index = derivedSeries.Points.Count();

            if (start_index < 0) { start_index = 0; }

            for (int i = start_index; i < end_index; i++)
            {
              float value = (float)derivedSeries.Points[i].Y;

              byte[] bytes = System.BitConverter.GetBytes(value);
              foreach (var b in bytes)
              {
                bin_writer.Write(b);
              }
            }

            bin_writer.Close();

            #endregion

          }
        } else {
          //float sample_period = LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;
          float sample_frequency = (float)edfsignal.NumberOfSamplesPerDataRecord / LoadedEDFFile.Header.DurationOfDataRecordInSeconds;

          //hdr file contains metadata of the binary file
          FileStream hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate);
          hdr_file.SetLength(0); //clear it's contents
          hdr_file.Close(); //flush
          hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate); //reload

          StringBuilder sb_hdr = new StringBuilder(); // string builder used for writing into the file

          int end_index = (int)(((signals_data.Epochs_From + signals_data.Epochs_Length) * 30) / LoadedEDFFile.Header.DurationOfDataRecordInSeconds) * edfsignal.NumberOfSamplesPerDataRecord;

          var edfSignal = LoadedEDFFile.Header.Signals.Find(s => s.Label.Trim() == signal.Trim());
          var signalValues = LoadedEDFFile.retrieveSignalSampleValues(edfSignal).ToArray();
          if (end_index > signalValues.Count()) {
            end_index = signalValues.Count();
          }
          int endEpochs = (int)((end_index * LoadedEDFFile.Header.DurationOfDataRecordInSeconds) / (30 * edfsignal.NumberOfSamplesPerDataRecord)) + 1;


          sb_hdr.AppendLine(edfsignal.Label) // name
              .AppendLine(signals_data.Subject_ID.ToString()) // subject id
              .AppendLine(Utils.EpochtoDateTime(signals_data.Epochs_From, LoadedEDFFile).ToString()) // epoch start
              .AppendLine(Utils.EpochtoDateTime(endEpochs, LoadedEDFFile).ToString()) // epoch length
              .AppendLine(sample_frequency.ToString()); // sample_period 

          var bytes_to_write = Encoding.ASCII.GetBytes(sb_hdr.ToString());
          hdr_file.Write(bytes_to_write, 0, bytes_to_write.Length);
          hdr_file.Close();        

          FileStream bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //the binary file for each signal
          bin_file.SetLength(0); //clear it's contents
          bin_file.Close(); //flush


          #region signal_binary_contents

          bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //reload
          BinaryWriter bin_writer = new BinaryWriter(bin_file);

          int start_index = (int)(((signals_data.Epochs_From - 1) * 30) / LoadedEDFFile.Header.DurationOfDataRecordInSeconds) * edfsignal.NumberOfSamplesPerDataRecord; // from epoch number * 30 seconds per epoch * sample rate = start time
          
          if (start_index < 0) { start_index = 0; }

          for (int i = start_index; i < end_index; i++)
          {
            bin_writer.Write(signalValues[i]);
          }

          bin_writer.Close();

          #endregion

        }
      }

    }
    
    /// <summary>
    /// Exports chart to image
    /// </summary>
    public void ExportImage(string fileName)
    {
      var export = new OxyPlot.Wpf.PngExporter();
      export.Width = 1280;
      export.Height = 720;
      export.Background = OxyColors.White;

      MemoryStream stream = new MemoryStream();
      FileStream file = new FileStream(fileName, FileMode.Create);

      export.Export(PreviewSignalPlot, stream);
      stream.WriteTo(file);
      file.Close();
      stream.Close();
    }
    public void ExportImage(PlotModel plot, String fileName)
    {
      var export = new OxyPlot.Wpf.PngExporter();
      export.Width = 1280;
      export.Height = 720;
      export.Background = OxyColors.White;

      MemoryStream stream = new MemoryStream();
      FileStream file = new FileStream(fileName, FileMode.Create);

      export.Export(plot, stream);
      stream.WriteTo(file);
      file.Close();
      stream.Close();
    }


    /****************************************************** EEG ANALYSIS TAB ********************************************************/

    //EEG Analysis From Binary File
    public void PerformEEGAnalysisBinary()
    {
      this.EEGAnalysisBinaryFileLoaded = 1;
      OnPropertyChanged(nameof(IsEEGBinaryLoaded));
      System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();

      dialog.Filter = "|*.bin";

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        // select the binary file
        FileStream bin_file = new FileStream(dialog.FileName, FileMode.Open);
        BinaryReader reader = new BinaryReader(bin_file);

        byte[] value = new byte[4];
        bool didReachEnd = false;
        eeg_bin_signal_values = new List<float>();
        // read the whole binary file and build the signal values
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
          try
          {
            value = reader.ReadBytes(4);
            float myFloat = System.BitConverter.ToSingle(value, 0);
            eeg_bin_signal_values.Add(myFloat);
          }
          catch (Exception ex)
          {
            didReachEnd = true;
            break;
          }
        }

        // close the binary file
        bin_file.Close();

        // get the file metadata from the header file
        bin_file = new FileStream(dialog.FileName.Remove(dialog.FileName.Length - 4, 4) + ".hdr", FileMode.Open);

        StreamReader file_reader = new StreamReader(bin_file);
        // get the signal name
        eeg_bin_signal_name = file_reader.ReadLine();
        eeg_bin_subject_id = file_reader.ReadLine();
        eeg_bin_date_time_from = file_reader.ReadLine();
        eeg_bin_date_time_to = file_reader.ReadLine();
        eeg_bin_sample_frequency_s = file_reader.ReadLine();

        eeg_bin_max_epochs = (int)(DateTime.Parse(eeg_bin_date_time_to).Subtract(DateTime.Parse(eeg_bin_date_time_from)).TotalSeconds) / 30;
        OnPropertyChanged(nameof(EEGBinaryMaxEpoch));

        bin_file.Close();

        float sample_period = 1 / float.Parse(eeg_bin_sample_frequency_s);

        DateTime epochs_from_datetime = DateTime.Parse(eeg_bin_date_time_from);
        DateTime epochs_to_datetime = DateTime.Parse(eeg_bin_date_time_to);

        // perform all of the respiratory analysis
        BW_EEGAnalysisBin(eeg_bin_signal_name, eeg_bin_signal_values, epochs_from_datetime, epochs_from_datetime, epochs_from_datetime.AddSeconds(30), sample_period);
      }
      else
      {
        p_window.ShowMessageAsync("Error", "File could not be opened.");
      }
    }
    private void BW_EEGAnalysisBin(string Signal, List<float> values, DateTime binary_start, DateTime epochs_from, DateTime epochs_to, float sample_period)
    {
      // Variable To Return
      LineSeries series = new LineSeries();

      float startIndex = (epochs_from - binary_start).Seconds * (1 / sample_period);
      float endIndex = startIndex + 30 * (1 / sample_period);

      //  // Add Points to Series
      for (var y = startIndex; y < endIndex; y++)
      {
        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(epochs_from + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[(int)y]));
      }

      if (series.Points.Count == 0)
      {
        //need to type error message for User
        return;
      }

      const int freqbands = 7;
      MWNumericArray[] fqRange;
      setFreqBands(out fqRange, freqbands);

      double[] signal = new double[series.Points.Count];
      for (int i = 0; i < series.Points.Count; i++)
      {
        signal[i] = series.Points[i].Y;
      }
      MWNumericArray mLabsignalSeries = new MWNumericArray(signal);
      MWNumericArray sampleFreq = new MWNumericArray(1 / sample_period);

      /********************************Computing Absolute Power**************************************/
      double totalPower;
      double[] absPower;
      ColumnItem[] absPlotbandItems;
      AbsPwrAnalysis(out totalPower, out absPower, out absPlotbandItems, mLabsignalSeries, fqRange, sample_period);

      /*******************************Relative & Total Power************************************/
      ColumnItem[] plotbandItems;
      RelPwrAnalysis(out plotbandItems, totalPower, absPower, freqbands);

      /*************Computing Power spectral Density (line 841 - PSG_viewer_v7.m)****************/
      double[] psdValues;
      double[] frqValues;
      PSDAnalysis(out psdValues, out frqValues, mLabsignalSeries, sampleFreq);

      /****************************Computation for Spectrogram**************************/
      Spectrogram computeForspec = new Spectrogram();
      MWArray[] mLabSpec = null;
      mLabSpec = computeForspec.eeg_specgram(3, mLabsignalSeries, sampleFreq);//[colorData,f,t]
      MWNumericArray tempSpec = (MWNumericArray)mLabSpec[0];//already multiplied by 10*log10()
      MWNumericArray tempFrq = (MWNumericArray)mLabSpec[1];
      MWNumericArray tempTime = (MWNumericArray)mLabSpec[2];

      //# of rows = mLabSpec[0].Dimensions[0]
      double[,] specMatrix = new double[mLabSpec[0].Dimensions[0], mLabSpec[0].Dimensions[1]];
      double[] specTime = new double[tempTime.NumberOfElements + 1];
      double[] specFrq = new double[tempFrq.NumberOfElements];
      int idx = 0;
      //MATLAB matrix are column-major order
      for (int i = 0; i < mLabSpec[0].Dimensions[0]; i++)
      {
        for (int j = 0; j < mLabSpec[0].Dimensions[1]; j++)
        {
          idx = (mLabSpec[0].Dimensions[0] * j) + i + 1;//(total_rows * curr_col) + curr_row
          specMatrix[i, j] = (double)tempSpec[idx];
        }
      }
      double[,] specMatrixtranspose = new double[mLabSpec[0].Dimensions[1], mLabSpec[0].Dimensions[0]];
      for (int j = 0; j < mLabSpec[0].Dimensions[1]; j++)//need to combine this loop with the loop above 
      {
        for (int i = 0; i < mLabSpec[0].Dimensions[0]; i++)
        {
          specMatrixtranspose[j, i] = specMatrix[i, j];
        }
      }

      for (int i = 1; i < specTime.Length; i++)
      {
        specTime[i] = (double)tempTime[i];
      }
      for (int i = 1; i < specFrq.Length; i++)
      {
        specFrq[i - 1] = (double)tempFrq[i];
      }


      //order of bands MUST match the order of bands in fqRange array (see above)
      String[] freqBandName = new String[] { "delta", "theta", "alpha", "beta1", "beta2", "gamma1", "gamma2" };

      /*****************************Plotting absolute power graph***************************/
      PlotAbsolutePower(absPlotbandItems, freqBandName);

      /*************************************Plotting relative power graph****************************/
      PlotRelativePower(plotbandItems, freqBandName);

      /*************************Plotting Power Spectral Density *********************/
      PlotPowerSpectralDensity(psdValues, frqValues);

      /********************Plotting a heatmap for spectrogram (line 820, 2133 - PSG_viewer_v7.m)*********************/
      PlotModel tempSpectGram = new PlotModel()
      {
        Title = "Spectrogram",
      };
      LinearColorAxis specLegend = new LinearColorAxis() { Position = AxisPosition.Right, Palette = OxyPalettes.Jet(500) };
      LinearAxis specYAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "Frequency (Hz)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8 };
      LinearAxis specXAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Time (s)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold };

      tempSpectGram.Axes.Add(specLegend);
      tempSpectGram.Axes.Add(specXAxis);
      tempSpectGram.Axes.Add(specYAxis);

      double minTime = specTime.Min(), maxTime = specTime.Max(), minFreq = specFrq.Min(), maxFreq = specFrq.Max();
      HeatMapSeries specGram = new HeatMapSeries() { X0 = minTime, X1 = maxTime, Y0 = minFreq, Y1 = maxFreq, Data = specMatrixtranspose };
      tempSpectGram.Series.Add(specGram);

      PlotSpecGram = tempSpectGram;


      /*************************Exporting to .tiff format**************************/
      String plotsDir = "EEGPlots//Epoch-" + ((int)EpochForAnalysis).ToString() + "//";
      Directory.CreateDirectory(plotsDir);//if directory already exist, this line will be ignored
      ExportEEGPlot(PlotAbsPwr, plotsDir + "AbsPower.png");
      ExportEEGPlot(PlotRelPwr, plotsDir + "RelPower.png");
      ExportEEGPlot(PlotPSD, plotsDir + "PSD-Epoch.png");

      //GenericExportImage(PlotSpecGram, "Spectrogram.png");//Need to review implementation

      return;//for debugging only
    }

    //EEG Analysis From EDF File
    /* Note to self:
     * 1  AbsPwrAnalysis returns absPower in V^2/R, so multiply absPower in by 10*Math.Log10() when 
     *    plotting absolute power or exporting absolute power calculations because 
     *    rel power = abs power(V^2/R) / totalPower(V^2/R)
     *    
     * 2  PSDPower returns power in (db), so don't mltiply by 10*Math.Log10()
     * 
     * 3  Add signal name to folders in EEGanalysis & EEGPlots directory
     * 4  Add values to bar graphs so the user can see the values of bars
    */
    private void BW_EEGAnalysisEDF(object sender, DoWorkEventArgs e)
    {
      float sample_period;
      DateTime StartEpoch = Utils.EpochtoDateTime(EpochForAnalysis ?? 1, LoadedEDFFile);
      DateTime EndEpoch = StartEpoch + Utils.EpochPeriodtoTimeSpan(1);
      LineSeries series = GetSeriesFromSignalName(out sample_period, EEGEDFSelectedSignal,
                                                  StartEpoch, EndEpoch);

      if (series.Points.Count == 0) {
        //need to type error message for User
        return;
      }

      const int freqbands = 7;
      MWNumericArray[] fqRange;
      setFreqBands(out fqRange, freqbands);

      double[] signal = new double[series.Points.Count];
      for (int i = 0; i < series.Points.Count; i++)
      {
        signal[i] = series.Points[i].Y;
      }
      MWNumericArray mLabsignalSeries = new MWNumericArray(signal);
      MWNumericArray sampleFreq = new MWNumericArray(1 / sample_period);

      /********************************Computing Absolute Power**************************************/
      double totalPower;
      double[] absPower;
      ColumnItem[] absPlotbandItems;
      AbsPwrAnalysis(out totalPower, out absPower, out absPlotbandItems, mLabsignalSeries, fqRange, sample_period);

      /*******************************Relative & Total Power************************************/
      ColumnItem[] plotbandItems;
      RelPwrAnalysis(out plotbandItems, totalPower, absPower, freqbands);

      /*************Computing Power spectral Density (line 841 - PSG_viewer_v7.m)****************/
      double[] psdValues;
      double[] frqValues;
      PSDAnalysis(out psdValues, out frqValues, mLabsignalSeries, sampleFreq);

      /****************************Computation for Spectrogram**************************/
      Spectrogram computeForspec = new Spectrogram();
      MWArray[] mLabSpec = null;
      mLabSpec = computeForspec.eeg_specgram(3, mLabsignalSeries, sampleFreq);//[colorData,f,t]
      MWNumericArray tempSpec = (MWNumericArray)mLabSpec[0];//already multiplied by 10*log10()
      MWNumericArray tempFrq = (MWNumericArray)mLabSpec[1];
      MWNumericArray tempTime = (MWNumericArray)mLabSpec[2];

      //# of rows = mLabSpec[0].Dimensions[0]
      double[,] specMatrix = new double[mLabSpec[0].Dimensions[0], mLabSpec[0].Dimensions[1]];
      double[] specTime = new double[tempTime.NumberOfElements + 1];
      double[] specFrq = new double[tempFrq.NumberOfElements];
      int idx = 0;
      //MATLAB matrix are column-major order
      for (int i = 0; i < mLabSpec[0].Dimensions[0]; i++) 
      {
        for (int j = 0; j < mLabSpec[0].Dimensions[1]; j++)
        {
          idx = (mLabSpec[0].Dimensions[0] * j) + i + 1;//(total_rows * curr_col) + curr_row
          specMatrix[i, j] = (double)tempSpec[idx];
        }
      }
      double[,] specMatrixtranspose = new double[mLabSpec[0].Dimensions[1], mLabSpec[0].Dimensions[0]];
      for (int j = 0; j < mLabSpec[0].Dimensions[1]; j++)//need to combine this loop with the loop above 
      {
        for (int i = 0; i < mLabSpec[0].Dimensions[0]; i++)
        {
          specMatrixtranspose[j, i] = specMatrix[i, j];
        }
      }

      for (int i = 1; i < specTime.Length; i++)
      {
        specTime[i] = (double)tempTime[i];
      }
      for (int i = 1; i < specFrq.Length; i++)
      {
        specFrq[i - 1] = (double)tempFrq[i];
      }


      //order of bands MUST match the order of bands in fqRange array (see above)
      String[] freqBandName = new String[] { "delta", "theta", "alpha", "beta1", "beta2", "gamma1", "gamma2" };

      /*****************************Plotting absolute power graph***************************/
      PlotAbsolutePower(absPlotbandItems, freqBandName);

      /*************************************Plotting relative power graph****************************/
      PlotRelativePower(plotbandItems, freqBandName);

      /*************************Plotting Power Spectral Density *********************/
      PlotPowerSpectralDensity(psdValues, frqValues);

      /********************Plotting a heatmap for spectrogram (line 820, 2133 - PSG_viewer_v7.m)*********************/
      PlotModel tempSpectGram = new PlotModel()
      {
        Title = "Spectrogram",
      };
      LinearColorAxis specLegend = new LinearColorAxis() { Position = AxisPosition.Right, Palette = OxyPalettes.Jet(500)};
      LinearAxis specYAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "Frequency (Hz)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8 };
      LinearAxis specXAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Time (s)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold };

      tempSpectGram.Axes.Add(specLegend);
      tempSpectGram.Axes.Add(specXAxis);
      tempSpectGram.Axes.Add(specYAxis);

      double minTime = specTime.Min(), maxTime = specTime.Max(), minFreq = specFrq.Min(), maxFreq = specFrq.Max();
      HeatMapSeries specGram = new HeatMapSeries() { X0 = minTime, X1 = maxTime, Y0 = minFreq, Y1 = maxFreq, Data = specMatrixtranspose };
      tempSpectGram.Series.Add(specGram);

      PlotSpecGram = tempSpectGram;


      /*************************Exporting to .tiff format**************************/      
      String plotsDir = "EEGPlots//" + EEGEDFSelectedSignal.ToString() + "-" 
                         + ((int)EpochForAnalysis).ToString() + "//";            
      Directory.CreateDirectory(plotsDir);//if directory already exist, this line will be ignored
      ExportEEGPlot(PlotAbsPwr, plotsDir + "AbsPower.png");
      ExportEEGPlot(PlotRelPwr, plotsDir + "RelPower.png");
      ExportEEGPlot(PlotPSD, plotsDir + "PSD.png");
      ExportEEGPlot(PlotSpecGram, plotsDir + "Spectrogram.png");

      return;//for debugging only
    }

    public void setFreqBands(out MWNumericArray[] fRange, int numberOfBands)
    {
      fRange = new MWNumericArray[numberOfBands];
      fRange[0] = new MWNumericArray(1, 2, new double[] { 0.1, 3 });//delta band
      fRange[1] = new MWNumericArray(1, 2, new double[] { 4, 7 });//theta band
      fRange[2] = new MWNumericArray(1, 2, new double[] { 8, 12 });//alpha band
      fRange[3] = new MWNumericArray(1, 2, new double[] { 13, 17 });//beta1 band
      fRange[4] = new MWNumericArray(1, 2, new double[] { 18, 30 });//beta2 band
      fRange[5] = new MWNumericArray(1, 2, new double[] { 31, 40 });//gamma1 band
      fRange[6] = new MWNumericArray(1, 2, new double[] { 41, 50 });//gamma2 band
    }

    public void AbsPwrAnalysis(out double totalPower, out double[] absPower, out ColumnItem[] absPlotbandItems,
                            MWNumericArray signalToMLab, MWNumericArray[] frqRangeToMLab, float sample_period)
    {
      EEGPower pwr = new EEGPower();
      totalPower = 0.0;
      absPower = new double[frqRangeToMLab.Length];
      MWNumericArray sampleFreq = new MWNumericArray(1 / sample_period);

      absPlotbandItems = new ColumnItem[frqRangeToMLab.Length];
      for (int i = 0; i < frqRangeToMLab.Length; i++)
      {
        absPower[i] = (double)(MWNumericArray)pwr.eeg_bandpower(signalToMLab, sampleFreq, frqRangeToMLab[i]);
        totalPower += absPower[i];
        absPlotbandItems[i] = new ColumnItem { Value = 10 * Math.Log10(absPower[i]) };//bars for abs pwr plot
      }
    }
    public void RelPwrAnalysis(out ColumnItem[] plotbandItems, double totalPower, double[] absPower, int totalFrqBands)
    {
      plotbandItems = new ColumnItem[totalFrqBands];
      double[] relPower = new double[totalFrqBands];
      for (int i = 0; i < relPower.Length; i++)
      {
        relPower[i] = 100 * (absPower[i]) / totalPower;
        plotbandItems[i] = new ColumnItem { Value = relPower[i] };//bars for rel pwr plot
      }
    }
    public void PSDAnalysis(out double[] psdValues, out double[] frqValues, MWNumericArray signalArray, MWNumericArray sampleFreq)
    {
      EEG_PSD computePSD = new EEG_PSD();
      MWArray[] mLabPSD = null;

      mLabPSD = computePSD.eeg_psd(2, signalArray, sampleFreq);
      MWNumericArray tempPsd = (MWNumericArray)mLabPSD[0];
      MWNumericArray tempFrq = (MWNumericArray)mLabPSD[1];

      psdValues = new double[tempPsd.NumberOfElements];
      frqValues = new double[tempFrq.NumberOfElements];
      for (int i = 1; i < tempPsd.NumberOfElements; i++)
      {
        psdValues[i] = 10 * Math.Log10((double)tempPsd[i]);//psd in (dB) after taking a log10
        frqValues[i] = (double)tempFrq[i];
      }
    }

    public void PlotAbsolutePower(ColumnItem[] bandItems, String[] bandFrqs)
    {
      PlotModel tempAbsPwr = new PlotModel()
      {
        Title = "Absolute Power"
      };
      ColumnSeries absPlotbars = new ColumnSeries
      {
        //Title = "Abs_Pwr",
        StrokeColor = OxyColors.Black,
        StrokeThickness = 1,
        FillColor = OxyColors.Blue//changes color of bars
      };
      absPlotbars.Items.AddRange(bandItems);

      CategoryAxis absbandLabels = new CategoryAxis { Position = AxisPosition.Bottom };

      absbandLabels.Labels.AddRange(bandFrqs);

      LinearAxis absYAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Power (db)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8 };
      tempAbsPwr.Series.Add(absPlotbars);
      tempAbsPwr.Axes.Add(absbandLabels);
      tempAbsPwr.Axes.Add(absYAxis);

      PlotAbsPwr = tempAbsPwr;
    }
    public void PlotRelativePower(ColumnItem[] bandItems, String[] bandFrqs)
    {
      PlotModel tempRelPwr = new PlotModel()
      {
        Title = "Relative Power"
      };
      ColumnSeries relPlotbars = new ColumnSeries
      {
        StrokeColor = OxyColors.Black,
        StrokeThickness = 1,
        FillColor = OxyColors.Red//changes color of bars
      };
      relPlotbars.Items.AddRange(bandItems);

      CategoryAxis relbandLabels = new CategoryAxis { Position = AxisPosition.Bottom };

      relbandLabels.Labels.AddRange(bandFrqs);

      LinearAxis relYAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Power (%)", MinimumPadding = 0, MaximumPadding = 0.06, AbsoluteMinimum = 0, TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8 };
      tempRelPwr.Series.Add(relPlotbars);
      tempRelPwr.Axes.Add(relbandLabels);
      tempRelPwr.Axes.Add(relYAxis);

      PlotRelPwr = tempRelPwr;
    }
    public void PlotPowerSpectralDensity(double[] psdVal, double[] frqVal)
    {
      PlotModel tempPSD = new PlotModel()
      {
        Title = "Power Spectral Density"
      };
      LineSeries psdSeries = new LineSeries() { Color = OxyColors.Green };
      for (int i = 0; i < psdVal.Length; i++)
      {
        psdSeries.Points.Add(new DataPoint(frqVal[i], psdVal[i]));
      }
      tempPSD.Series.Add(psdSeries);
      tempPSD.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Title = "Power (dB)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold });
      tempPSD.Axes.Add(new LinearAxis() { Position = AxisPosition.Bottom, Title = "Frequency (Hz)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8 });

      PlotPSD = tempPSD;
    }

    public void ExportEEGCalculations()//add signal title in the analysisDir name
    {
      if (ExportEpochEnd < ExportEpochStart)//restrict ExportEpochEnd to the max size of signal
      {
        return;
      }
      String fromToDir = "Epoch-" + ExportEpochStart.ToString() + "-" + ExportEpochEnd.ToString();
      double[] signalToAnalyze;
      float sample_period;

      MWNumericArray signalToMatlab;
      MWNumericArray sampleFreq;

      DateTime StartEpoch, EndEpoch;
      double[] psdValue;
      double[] frqValue;

      double totalPower;
      double[] absPower;
      ColumnItem[] absPlotbandItems;

      const int totalFreqBands = 7;
      MWNumericArray[] fqRange;
      setFreqBands(out fqRange, totalFreqBands);

      LineSeries signalPerEpoch = null;

      //Setup data to be entered in each file
      String analysisDir = "EEGAnalysis//" + fromToDir + "//";
      Directory.CreateDirectory(analysisDir);//if directory already exist, this line will be ignored

      StreamWriter fileSetup = new StreamWriter(analysisDir + "EEGSignal.csv");
      fileSetup.WriteLine(String.Format("Epoch#, X(time), Y(SigVal)"));
      fileSetup.Close();

      fileSetup = new StreamWriter(analysisDir + "EEGPSD.csv");
      fileSetup.WriteLine(String.Format("Epoch#, Power(db),Frequency(Hz)"));
      fileSetup.Close();

      fileSetup = new StreamWriter(analysisDir + "EEGAbsPwr.csv");
      fileSetup.WriteLine(String.Format("Epoch#, delta, theta, alpha, beta1, beta2, gamma1, gamma2"));
      fileSetup.Close();

      int currentEpoch = ExportEpochStart ?? 1;

      for (int i = (int)ExportEpochStart; i <= ExportEpochEnd; i++)
      {
        StartEpoch = Utils.EpochtoDateTime(i, LoadedEDFFile);
        EndEpoch = StartEpoch + Utils.EpochPeriodtoTimeSpan(1);
        signalPerEpoch = GetSeriesFromSignalName(out sample_period, EEGEDFSelectedSignal,
                                                  StartEpoch, EndEpoch);
        EDFSignalToCSV(signalPerEpoch, i, analysisDir + "EEGSignal.csv");

        signalToAnalyze = new double[signalPerEpoch.Points.Count];//select length to be more than From (on GUI)
        for (int s = 0; s < signalPerEpoch.Points.Count; s++)
        {
          signalToAnalyze[s] = signalPerEpoch.Points[s].Y;
        }

        signalToMatlab = new MWNumericArray(signalToAnalyze);
        sampleFreq = new MWNumericArray(1 / sample_period);

        //perform Absolute power calculations
        AbsPwrAnalysis(out totalPower, out absPower, out absPlotbandItems, signalToMatlab, fqRange, sample_period);
        //output Absolute power calculations to file
        AbsPwrToCSV(absPower, i, analysisDir + "EEGAbsPwr.csv");

        //No need to perform Relative power calculations, as it is not exported. It can be derived from Absolute Power.

        //perform PSD calculations
        PSDAnalysis(out psdValue, out frqValue, signalToMatlab, sampleFreq);
        //output PSD calculations to file
        PSDToCSV(psdValue, frqValue, i, analysisDir + "EEGPSD.csv");
      }

      return;//for debugging only
    }
    public void ExportEEGPlot(PlotModel pModel, String fileName)
    {
      Thread exportTh = new Thread(() => ExportImage(pModel, fileName));
      exportTh.SetApartmentState(ApartmentState.STA);
      exportTh.Start();
      exportTh.Join();
    }

    public void AbsPwrToCSV(double[] absPwrData, int epoch, String fileName)
    {
      StreamWriter absPwrStream = File.AppendText(fileName);
      String dataLine = null;
      absPwrStream.Write(epoch.ToString());

      for (int i = 0; i < absPwrData.Length; i++)
      {
        dataLine += String.Format(",{0:0.000}", 10 * Math.Log10(absPwrData[i]));
      }
      absPwrStream.WriteLine(dataLine);
      absPwrStream.Close();
    }
    public void PSDToCSV(double[] psdVal, double[] frqVal, int epoch, String fileName)
    {
      StreamWriter psdStream = File.AppendText(fileName);

      for (int j = 1; j < psdVal.Length; j++)
      {
        psdStream.WriteLine(String.Format("{0},{1:0.000},{2:0.000}", epoch,psdVal[j], frqVal[j]));
      }
      psdStream.Close();
    }

    /**************************Exporting EEG Signal to .csv*************************/

    public void EDFSignalToCSV(LineSeries dataToExport, int epoch, String fileName)
    {
      StreamWriter fileStream = File.AppendText(fileName);
      for (int i = 1; i < dataToExport.Points.Count; i++)
      {
        fileStream.WriteLine(String.Format("{0},{1:0.00},{2:0.000}", epoch, dataToExport.Points[i].X, 
                              dataToExport.Points[i].Y).ToString());
      }
      fileStream.Close();
    }

    private void BW_FinishEEGAnalysisEDF(object sender, RunWorkerCompletedEventArgs e)
    {

    }
    public void PerformEEGAnalysisEDF()
    {
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_EEGAnalysisEDF;
      bw.RunWorkerCompleted += BW_FinishEEGAnalysisEDF;
      bw.RunWorkerAsync();
    }

    /**************************************************** COHERENCE ANALYSIS TAB ****************************************************/

    /// <summary>
    /// Background process for performing coherence analysis
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_CoherenceAnalysisEDF(object sender, DoWorkEventArgs e)
    {
      #region Plot Series 1 in Time Domain 

      // Get Series 1
      float sample_period_1;
      LineSeries series_1 = GetSeriesFromSignalName(out sample_period_1,
                                                  CoherenceEDFSelectedSignal1,
                                                  Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile),
                                                  Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1)
                                                  );

      // Plot Series 1
      {
        PlotModel temp_SignalPlot = new PlotModel();

        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.Key = "DateTime";
        xAxis.Minimum = DateTimeAxis.ToDouble(Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile));
        xAxis.Maximum = DateTimeAxis.ToDouble(Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1));
        temp_SignalPlot.Axes.Add(xAxis);

        LinearAxis yAxis = new LinearAxis();
        yAxis.MajorGridlineStyle = LineStyle.Solid;
        yAxis.MinorGridlineStyle = LineStyle.Dot;
        yAxis.Title = CoherenceEDFSelectedSignal1 + " (Time)";
        yAxis.Key = CoherenceEDFSelectedSignal1 + " (Time)";

        if (CoherenceUseConstantAxis)
        {
          yAxis.Maximum = Utils.GetMaxSignalValue(CoherenceEDFSelectedSignal1, false, LoadedEDFFile, sm);
          yAxis.Minimum = Utils.GetMinSignalValue(CoherenceEDFSelectedSignal1, false, LoadedEDFFile, sm);
        }

        temp_SignalPlot.Axes.Add(yAxis);

        series_1.YAxisKey = CoherenceEDFSelectedSignal1 + " (Time)";
        series_1.XAxisKey = "DateTime";
        temp_SignalPlot.Series.Add(series_1);

        CoherenceSignalPlot1 = temp_SignalPlot;
      }

      #endregion

      #region Plot Series 2 in Time Domain 

      // Get Series 2
      float sample_period_2;
      LineSeries series_2 = GetSeriesFromSignalName(out sample_period_2,
                                                  CoherenceEDFSelectedSignal2,
                                                  Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile),
                                                  Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1)
                                                  );

      // Plot Series 2
      {
        PlotModel temp_SignalPlot = new PlotModel();

        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.Key = "DateTime";
        xAxis.Minimum = DateTimeAxis.ToDouble(Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile));
        xAxis.Maximum = DateTimeAxis.ToDouble(Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1));
        temp_SignalPlot.Axes.Add(xAxis);

        LinearAxis yAxis = new LinearAxis();
        yAxis.MajorGridlineStyle = LineStyle.Solid;
        yAxis.MinorGridlineStyle = LineStyle.Dot;
        yAxis.Title = CoherenceEDFSelectedSignal2 + " (Time)";
        yAxis.Key = CoherenceEDFSelectedSignal2 + " (Time)";

        if (CoherenceUseConstantAxis)
        {
          yAxis.Maximum = Utils.GetMaxSignalValue(CoherenceEDFSelectedSignal2, false, LoadedEDFFile, sm);
          yAxis.Minimum = Utils.GetMinSignalValue(CoherenceEDFSelectedSignal2, false, LoadedEDFFile, sm);
        }
        temp_SignalPlot.Axes.Add(yAxis);

        series_2.YAxisKey = CoherenceEDFSelectedSignal2 + " (Time)";
        series_2.XAxisKey = "DateTime";
        temp_SignalPlot.Series.Add(series_2);

        CoherenceSignalPlot2 = temp_SignalPlot;
      }

      #endregion

      #region Plot Coherence 

      // Calculate Coherence
      LineSeries coh = new LineSeries();
      {
        List<float> values1;
        List<float> values2;

        if (sample_period_1 == sample_period_2)
        {
          values1 = series_1.Points.Select(temp => (float)temp.Y).ToList();
          values2 = series_2.Points.Select(temp => (float)temp.Y).ToList();
        }
        else
        {
          if (sample_period_1 < sample_period_2) // Upsample signal 2
          {
            values1 = series_1.Points.Select(temp => (float)temp.Y).ToList();
            values2 = series_2.Points.Select(temp => (float)temp.Y).ToList();
            values2 = Utils.MATLAB_Resample(values2.ToArray(), sample_period_2 / sample_period_1);
          }
          else // Upsample signal 1
          {
            values1 = series_1.Points.Select(temp => (float)temp.Y).ToList();
            values2 = series_2.Points.Select(temp => (float)temp.Y).ToList();
            values1 = Utils.MATLAB_Resample(values1.ToArray(), sample_period_1 / sample_period_2);
          }
        }

        coh = Utils.MATLAB_Coherence(values1.ToArray(), values2.ToArray());
        coh.YAxisKey = "Coherence";
        coh.XAxisKey = "Normalized Frequency";
      }

      // Plot Coherence
      {
        PlotModel temp_plot = new PlotModel();
        temp_plot.Series.Add(coh);

        LinearAxis yAxis = new LinearAxis();
        yAxis.MajorGridlineStyle = LineStyle.Solid;
        yAxis.MinorGridlineStyle = LineStyle.Dot;
        yAxis.Title = "Coherence";
        yAxis.Key = "Coherence";
        yAxis.Maximum = 1.25;
        yAxis.Minimum = 0;
        temp_plot.Axes.Add(yAxis);

        LinearAxis xAxis = new LinearAxis();
        xAxis.Position = AxisPosition.Bottom;
        xAxis.Title = "Normalized Frequency";
        xAxis.Key = "Normalized Frequency";
        temp_plot.Axes.Add(xAxis);

        CoherencePlot = temp_plot;
      }

      #endregion
    }
    /// <summary>
    /// Performs coherence analysis between two signals
    /// </summary>
    public void PerformCoherenceAnalysisEDF()
    {
      if (CoherenceEDFSelectedSignal1 == null || CoherenceEDFSelectedSignal2 == null)
        return;

      CoherenceProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_CoherenceAnalysisEDF;
      bw.RunWorkerAsync();
    }

    /***************************************************** SETTINGS FLYOUT **********************************************************/

    /// <summary>
    /// Opens and Closes the Settings menu
    /// </summary>
    public void OpenCloseSettings()
    {
      FlyoutOpen = !FlyoutOpen;
    }

    /// <summary>
    /// Opens the Signal Category Management Wizard
    /// </summary>
    public void ManageCategories()
    {
      Dialog_Manage_Categories dlg = new Dialog_Manage_Categories(p_window,
                                                                  this,
                                                                  sm.SignalCategories.ToArray(),
                                                                  AllSignals.ToArray()
                                                                  );
      p_window.ShowMetroDialogAsync(dlg);
    }
    /// <summary>
    /// The Signal Category Management Wizard calls this function to return user input
    /// </summary>
    /// <param name="categories"> A list of categories </param>
    /// <param name="categories_signals"> A list of signals belonging to those categories </param>
    public void ManageCategoriesOutput(SignalCategory[] categories)
    {
      PreviewCurrentCategory = -1;
      sm.SignalCategories = categories.ToList();
    }

    /// <summary>
    /// Opens the Add Derivative Signal Wizard
    /// </summary>
    public void AddDerivative()
    {
      Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(p_window,
                                                            this,
                                                            EDFAllSignals.ToArray(),
                                                            AllSignals.ToArray()
                                                            );
      p_window.ShowMetroDialogAsync(dlg);
    }
    /// <summary>
    /// The Add Derivative Signal Wizard call this function to return user input
    /// </summary>
    /// <param name="name"> The name of the new derivative signal </param>
    /// <param name="signal1"> The minuend signal </param>
    /// <param name="signal2"> The subtrahend signal </param>
    public void AddDerivativeOutput(string name, string signal1, string signal2)
    {
      sm.DerivedSignals.Add(new DerivativeSignal(name, signal1, signal2));

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }
    /// <summary>
    /// Opens the Remove Derivative Signal Wizard
    /// </summary>
    public void RemoveDerivative()
    {
      Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(p_window,
                                                                  this,
                                                                  sm.DerivedSignals.Select(temp => temp.DerivativeName).ToArray());
      p_window.ShowMetroDialogAsync(dlg);
    }
    /// <summary>
    /// The Remove Derivative Signal Wizard call this function to return user input
    /// </summary>
    /// <param name="RemovedSignals"> An entry for every derivative signal to remove </param>
    public void RemoveDerivativeOutput(string[] RemovedSignals)
    {
      for (int x = 0; x < RemovedSignals.Length; x++)
      {
        List<DerivativeSignal> RemovedDerivatives = sm.DerivedSignals.FindAll(temp => temp.DerivativeName.Trim() == RemovedSignals[x].Trim()).ToList();
        sm.DerivedSignals.RemoveAll(temp => RemovedDerivatives.Contains(temp));

        // Remove Potentially Saved Min/Max Values
        sm.SignalsYAxisExtremes.RemoveAll(temp => temp.SignalName.Trim() == RemovedSignals[x].Trim());
      }

      // Remove from categories
      for (int x = 0; x < RemovedSignals.Length; x++)
      {
        for (int y = 0; y < sm.SignalCategories.Count; y++)
        {
          if (sm.SignalCategories[y].Signals.Contains(RemovedSignals[x]))
          {
            sm.SignalCategories[y].Signals.Remove(RemovedSignals[x]);
          }
        }
      }

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }

    /// <summary>
    /// Calls the Hide/Unhide Signals Wizard
    /// </summary>
    public void HideSignals()
    {
      bool[] input = new bool[EDFAllSignals.Count];
      for (int x = 0; x < EDFAllSignals.Count; x++)
      {
        if (sm.HiddenSignals.Contains(EDFAllSignals[x]))
          input[x] = true;
        else
          input[x] = false;
      }

      Dialog_Hide_Signals dlg = new Dialog_Hide_Signals(p_window,
                                                        this,
                                                        EDFAllSignals.ToArray(),
                                                        input);
      p_window.ShowMetroDialogAsync(dlg);
    }
    /// <summary>
    /// The Hide/Unhide Signals Wizard calls this function to return user inpout
    /// </summary>
    /// <param name="hide_signals_new"> An array with an entry for all EDF Signals. True means hide signal, false means show signal </param>
    public void HideSignalsOutput(bool[] hide_signals_new)
    {
      for (int x = 0; x < hide_signals_new.Length; x++)
      {
        if (hide_signals_new[x])
        {
          if (!sm.HiddenSignals.Contains(EDFAllSignals[x]))
          {
            sm.HiddenSignals.Add(EDFAllSignals[x]);
          }
        }
        else
        {
          if (sm.HiddenSignals.Contains(EDFAllSignals[x]))
          {
            sm.HiddenSignals.Remove(EDFAllSignals[x]);
          }
        }
      }

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }

    /// <summary>
    /// Call the Add Filtered Signal Wizard
    /// </summary>
    public void AddFilter()
    {
      Dialog_Add_Filter dlg = new Dialog_Add_Filter(p_window,
                                                            this,
                                                            EDFAllSignals.ToArray(),
                                                            sm.DerivedSignals.Select(temp => temp.DerivativeName).ToArray(),
                                                            AllSignals.ToArray()
                                                            );
      p_window.ShowMetroDialogAsync(dlg);

    }
    /// <summary>
    /// The Add Filtered Signal Wizard calls this function to return user input
    /// </summary>
    public void AddFilterOutput(FilteredSignal filteredSignal)
    {
      sm.FilteredSignals.Add(filteredSignal);

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }
    /// <summary>
    /// Calls the Remove Filtered Signal Wizard.
    /// </summary>
    public void RemoveFilter()
    {
      Dialog_Remove_Filter dlg = new Dialog_Remove_Filter(p_window,
                                                          this,
                                                          sm.FilteredSignals.Select(temp => temp.SignalName).ToArray());
      p_window.ShowMetroDialogAsync(dlg);
    }
    /// <summary>
    /// The Remove Filtered Signal Wizard calls this function to return user input.
    /// </summary>
    public void RemoveFilterOutput(string[] RemovedSignals)
    {
      for (int x = 0; x < RemovedSignals.Length; x++)
      {
        List<FilteredSignal> RemovedFilters = sm.FilteredSignals.FindAll(temp => temp.SignalName.Trim() == RemovedSignals[x].Trim()).ToList();
        sm.FilteredSignals.RemoveAll(temp => RemovedFilters.Contains(temp));

        // Remove Potentially Saved Min/Max Values
        sm.SignalsYAxisExtremes.RemoveAll(temp => temp.SignalName.Trim() == RemovedSignals[x].Trim());
      }

      // Remove from categories
      for (int x = 0; x < RemovedSignals.Length; x++)
      {
        for (int y = 0; y < sm.SignalCategories.Count; y++)
        {
          if (sm.SignalCategories[y].Signals.Contains(RemovedSignals[x]))
          {
            sm.SignalCategories[y].Signals.Remove(RemovedSignals[x]);
          }
        }
      }

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));

    }

    public void WriteEDFSettings()
    {
      Utils.WriteToDerivativesFile(sm.DerivedSignals.ToArray(), AllSignals.ToArray());
      Utils.WriteToFilteredSignalsFile(sm.FilteredSignals.ToArray(), AllSignals.ToArray());
      Utils.WriteToCategoriesFile(sm.SignalCategories.ToArray(), AllSignals.ToArray());
    }
    public void WriteAppSettings()
    {
      Utils.WriteToHiddenSignalsFile(sm.HiddenSignals.ToArray());
      Utils.WriteToPersonalization(UseCustomColor, ThemeColor, UseDarkTheme);
    }
    public void LoadEDFSettings()
    {
      sm.SignalsYAxisExtremes.Clear();
      sm.DerivedSignals = Utils.LoadDerivativesFile(LoadedEDFFile).ToList();
      sm.FilteredSignals = Utils.LoadFilteredSignalsFile(AllSignals.ToArray()).ToList();
      sm.SignalCategories = Utils.LoadCategoriesFile(AllSignals.ToArray()).ToList();
      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }
    public void LoadAppSettings()
    {
      sm.HiddenSignals = Utils.LoadHiddenSignalsFile().ToList();

      Color t_ThemeColor;
      bool t_UseDarkTheme;
      bool t_UseCustomColor;
      Utils.LoadPersonalization(out t_UseCustomColor, out t_ThemeColor, out t_UseDarkTheme);
      ThemeColor = t_ThemeColor;
      UseDarkTheme = t_UseDarkTheme;
      UseCustomColor = t_UseCustomColor;
    }

    #endregion

    #region Members

    /*********************************************************************************************************************************/

    /// <summary>
    /// The Window
    /// </summary>
    private MainWindow p_window;
    /// <summary>
    /// The Loaded EDF File
    /// </summary>
    private EDFFile p_LoadedEDFFile;
    /// <summary>
    /// The Loaded EDF Filename
    /// </summary>
    private string p_LoadedEDFFileName = null;

    /// <summary>
    /// Preview Model
    /// </summary>
    private PreviewModel pm = new PreviewModel();

    /// <summary>
    /// EEG Model
    /// </summary>
    private EEGModel eegm = new EEGModel();

    /// <summary>
    /// Coherence Model
    /// </summary>
    private CoherenceModel cm = new CoherenceModel();

    private SettingsModel sm;

    /*********************************************************************************************************************************/

    #endregion

    #region Properties 

    /*********************************************************************************************************************************/

    // Update Actions
    private void AppliedThemeColor_Changed()
    {
      OnPropertyChanged(nameof(AppliedThemeColor));

      var application = System.Windows.Application.Current;
      Accent newAccent = Utils.ThemeColorToAccent(AppliedThemeColor);

      ThemeManager.AddAccent(newAccent.Name, newAccent.Resources.Source);
      ThemeManager.ChangeAppStyle(application, newAccent, ThemeManager.GetAppTheme(UseDarkTheme ? "BaseDark" : "BaseLight"));

      // Update all charts to dark or light theme
      PropertyInfo[] all_plotmodels = this.GetType().GetProperties().ToList().Where(temp => temp.PropertyType == new PlotModel().GetType()).ToArray();
      for (int x = 0; x < all_plotmodels.Length; x++)
      {
        PlotModel model = (PlotModel)all_plotmodels[x].GetValue(this);

        all_plotmodels[x].SetValue(this, null);
        all_plotmodels[x].SetValue(this, model);
      }
    }
    private void LoadedEDFFile_Changed()
    {
      PreviewCurrentCategory = -1;

      // Preview Time Picker
      if (p_LoadedEDFFile == null)
      {
        PreviewUseAbsoluteTime = false;
        PreviewViewStartTime = null;
        PreviewViewStartRecord = null;
        PreviewViewDuration = null;
        LoadedEDFFileName = null;

        EEGEDFSelectedSignal = null;
        ExportEpochStart = null;
        ExportEpochEnd = null;
        EpochForAnalysis = null;

        CoherenceEDFSelectedSignal1 = null;
        CoherenceEDFSelectedSignal2 = null;
        CoherenceSignalPlot1 = null;
        CoherenceSignalPlot2 = null;
        CoherencePlot = null;
        CoherenceEDFDuration = null;
        CoherenceEDFStartRecord = null;
      }
      else
      {
        PreviewUseAbsoluteTime = false;
        PreviewViewStartTime = LoadedEDFFile.Header.StartDateTime;
        PreviewViewStartRecord = 1;
        PreviewViewDuration = 1;

        EEGEDFSelectedSignal = null;
        EpochForAnalysis = 1;
        ExportEpochStart = 1;
        ExportEpochEnd = 1;

        CoherenceEDFSelectedSignal1 = null;
        CoherenceEDFSelectedSignal2 = null;
        CoherenceSignalPlot1 = null;
        CoherenceSignalPlot2 = null;
        CoherencePlot = null;
        CoherenceEDFStartRecord = 1;
        CoherenceEDFDuration = 1;
      }
      OnPropertyChanged(nameof(PreviewNavigationEnabled));

      // Header
      OnPropertyChanged(nameof(EDFStartTime));
      OnPropertyChanged(nameof(EDFEndTime));
      OnPropertyChanged(nameof(EDFEndEpoch));
      OnPropertyChanged(nameof(EDFPatientName));
      OnPropertyChanged(nameof(EDFPatientSex));
      OnPropertyChanged(nameof(EDFPatientCode));
      OnPropertyChanged(nameof(EDFPatientBirthDate));
      OnPropertyChanged(nameof(EDFRecordEquipment));
      OnPropertyChanged(nameof(EDFRecordCode));
      OnPropertyChanged(nameof(EDFRecordTechnician));
      OnPropertyChanged(nameof(EDFAllSignals));

      // Misc
      OnPropertyChanged(nameof(IsEDFLoaded));

      // Analysis 
      OnPropertyChanged(nameof(CoherenceEDFNavigationEnabled));

      EEGView_Changed();
    }
    private void PreviewCurrentCategory_Changed()
    {
      OnPropertyChanged(nameof(PreviewCurrentCategoryName));
      OnPropertyChanged(nameof(PreviewSignals));
    }
    private void PreviewUseAbsoluteTime_Changed()
    {
      OnPropertyChanged(nameof(PreviewViewDuration));

      OnPropertyChanged(nameof(PreviewViewStartTimeMax));
      OnPropertyChanged(nameof(PreviewViewStartTimeMin));
      OnPropertyChanged(nameof(PreviewViewStartRecordMax));
      OnPropertyChanged(nameof(PreviewViewStartRecordMin));
      OnPropertyChanged(nameof(PreviewViewDurationMax));
      OnPropertyChanged(nameof(PreviewViewDurationMin));

      DrawChart();
    }
    private void PreviewUseConstantAxis_Changed()
    {
      DrawChart();
    }
    private void PreviewView_Changed()
    {
      OnPropertyChanged(nameof(PreviewViewStartRecord));
      OnPropertyChanged(nameof(PreviewViewStartTime));
      OnPropertyChanged(nameof(PreviewViewDuration));

      OnPropertyChanged(nameof(PreviewViewStartTimeMax));
      OnPropertyChanged(nameof(PreviewViewStartTimeMin));
      OnPropertyChanged(nameof(PreviewViewStartRecordMax));
      OnPropertyChanged(nameof(PreviewViewStartRecordMin));
      OnPropertyChanged(nameof(PreviewViewDurationMax));
      OnPropertyChanged(nameof(PreviewViewDurationMin));

      DrawChart();
    }
    private void PreviewSignalPlot_Changed()
    {
      PreviewNavigationEnabled = true;
    }
    private void PreviewPropertiesSelectedSignal_Changed()
    {
      OnPropertyChanged(nameof(PreviewPropertiesSelectedSignal));
      OnPropertyChanged(nameof(PreviewPropertiesSampleRate));
      OnPropertyChanged(nameof(PreviewPropertiesComponentSignal));
      OnPropertyChanged(nameof(PreviewPropertiesLowPassFilter));
      OnPropertyChanged(nameof(PreviewPropertiesSmoothFilter));
    }
    private void CoherencePlot_Changed()
    {
      CoherenceProgressRingEnabled = false;
    }
    private void CoherenceView_Changed()
    {
      OnPropertyChanged(nameof(CoherenceEDFStartRecord));
      OnPropertyChanged(nameof(CoherenceEDFStartTime));
      OnPropertyChanged(nameof(CoherenceEDFDuration));

      OnPropertyChanged(nameof(CoherenceEDFStartRecordMax));
      OnPropertyChanged(nameof(CoherenceEDFStartRecordMin));
      OnPropertyChanged(nameof(CoherenceEDFDurationMax));
      OnPropertyChanged(nameof(CoherenceEDFDurationMin));
    }
    private void EEGView_Changed()
    {
      OnPropertyChanged(nameof(EpochForAnalysis));
      OnPropertyChanged(nameof(EEGEDFStartTime));
      OnPropertyChanged(nameof(ExportEpochStart));
      OnPropertyChanged(nameof(ExportEpochEnd));

      OnPropertyChanged(nameof(EEGEDFStartRecordMax));
      OnPropertyChanged(nameof(EEGEDFStartRecordMin));
      //OnPropertyChanged(nameof(EEGEpochToMax));
      //OnPropertyChanged(nameof(EEGEpochToMin));
    }
    
    /*********************************************************** GENERAL ************************************************************/

    // Loaded EDF Structure and File Name
    public EDFFile LoadedEDFFile
    {
      get
      {
        return p_LoadedEDFFile;
      }
      set
      {
        p_LoadedEDFFile = value;
        LoadedEDFFile_Changed();
      }
    }
    public string LoadedEDFFileName
    {
      get
      {
        return p_LoadedEDFFileName ?? "No File Loaded";
      }
      set
      {
        p_LoadedEDFFileName = value;
        OnPropertyChanged(nameof(LoadedEDFFileName));
      }
    }
    public bool IsEDFLoaded
    {
      get
      {
        return LoadedEDFFile != null;
      }
    }

    // Personalization
    public Color ThemeColor
    {
      get
      {
        return sm.ThemeColor;
      }
      set
      {
        sm.ThemeColor = value;
        OnPropertyChanged(nameof(ThemeColor));
        AppliedThemeColor_Changed();
      }
    }
    public bool UseCustomColor
    {
      get
      {
        return sm.UseCustomColor;
      }
      set
      {
        sm.UseCustomColor = value;
        OnPropertyChanged(nameof(UseCustomColor));
        AppliedThemeColor_Changed();
      }
    }
    public Color AppliedThemeColor
    {
      get
      {
        if (sm.UseCustomColor)
          return sm.ThemeColor;
        else
          return ((Color)SystemParameters.WindowGlassBrush.GetValue(SolidColorBrush.ColorProperty));
      }
    }
    public bool UseDarkTheme
    {
      get
      {
        return sm.UseDarkTheme;
      }
      set
      {
        sm.UseDarkTheme = value;
        OnPropertyChanged(nameof(UseDarkTheme));
        AppliedThemeColor_Changed();
      }
    }
    
    /********************************************************* PREVIEW TAB **********************************************************/

    // EDF Header
    public string EDFStartTime
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.StartDateTime.ToString();
        else
          return null;
      }
    }
    public string EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          DateTime EndTime = LoadedEDFFile.Header.StartDateTime 
                             + new TimeSpan(
                               (long)(TimeSpan.TicksPerSecond * LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)
                               );
          return EndTime.ToString();
        }
        else
          return "";
      }
    }
    public string EDFEndEpoch
    {
      get
      {
        if (IsEDFLoaded)
          return (Utils.DateTimetoEpoch(DateTime.Parse(EDFEndTime), LoadedEDFFile) - 1).ToString();
        else
          return "";
      }
    }
    public string EDFPatientName
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.PatientIdentification.PatientName;
        else
          return "";
      }
    }
    public string EDFPatientSex
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.PatientIdentification.PatientSex;
        else
          return "";
      }
    }
    public string EDFPatientCode
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.PatientIdentification.PatientCode;
        else
          return "";
      }
    }
    public string EDFPatientBirthDate
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.PatientIdentification.PatientBirthDate.ToString();
        else
          return "";
      }
    }
    public string EDFRecordEquipment
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.RecordingIdentification.RecordingEquipment;
        else
          return "";
      }
    }
    public string EDFRecordCode
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.RecordingIdentification.RecordingCode;
        else
          return "";
      }
    }
    public string EDFRecordTechnician
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.RecordingIdentification.RecordingTechnician;
        else
          return "";
      }
    }
    public ReadOnlyCollection<string> AllSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          List<string> output = new List<string>();
          output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray());
          output.AddRange(sm.DerivedSignals.Select(temp => temp.DerivativeName.Trim()).ToArray());
          output.AddRange(sm.FilteredSignals.Select(temp => temp.SignalName));
          return Array.AsReadOnly(output.ToArray());
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }
    public ReadOnlyCollection<string> EDFAllSignals
    {
      get
      {
        if (IsEDFLoaded)
          return Array.AsReadOnly(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray());
        else
          return Array.AsReadOnly(new string[0]);
      }
    }
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          List<string> output = new List<string>();
          output.AddRange(AllSignals.Where(temp => !(EDFAllSignals.Contains(temp) && sm.HiddenSignals.Contains(temp))).ToArray());
          return Array.AsReadOnly(output.ToArray());
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }

    // Signal Properties
    public string PreviewPropertiesSelectedSignal
    {
      get
      {
        return pm.PreviewPropertiesSelectedSignal;
      }
      set
      {
        pm.PreviewPropertiesSelectedSignal = value;
        PreviewPropertiesSelectedSignal_Changed();
      }
    }
    public string PreviewPropertiesSampleRate
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          string Signal = PreviewPropertiesSelectedSignal;

          // Check signal type
          FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);
          if (filteredSignal != null) Signal = filteredSignal.OriginalName;
          EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal.Trim());
          DerivativeSignal deriv_info = sm.DerivedSignals.Find(temp => temp.DerivativeName == Signal);

          if (edfsignal != null) // Is EDF Signal
          {
            return ((int)((double)edfsignal.NumberOfSamplesPerDataRecord / (double)LoadedEDFFile.Header.DurationOfDataRecordInSeconds)).ToString();
          }
          else if (deriv_info != null) // Is Derivative Signal
          {
            EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal1Name.Trim());
            EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info.Signal2Name.Trim());
            return Math.Max(
              ((int)((double)edfsignal1.NumberOfSamplesPerDataRecord / (double)LoadedEDFFile.Header.DurationOfDataRecordInSeconds)),
              ((int)((double)edfsignal2.NumberOfSamplesPerDataRecord / (double)LoadedEDFFile.Header.DurationOfDataRecordInSeconds))
              ).ToString();
          }
          else
          {
            return "";
          }
        }
        else
        {
          return "";
        }
      }
    }
    public string PreviewPropertiesComponentSignal
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          string Signal = PreviewPropertiesSelectedSignal;

          // Check signal type
          FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);
          EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == PreviewPropertiesSelectedSignal.Trim());
          DerivativeSignal deriv_info = sm.DerivedSignals.Find(temp => temp.DerivativeName == Signal);

          if (edfsignal != null) // Is an EDF Signal 
            return "NA";
          else if (filteredSignal != null) // Is a Filtered Signal 
            return filteredSignal.OriginalName;
          else if (deriv_info != null) // Is a Derivative Signal 
          {
            return "(" + deriv_info.Signal1Name + ") - (" + deriv_info.Signal2Name + ")";
          }
          else
            return "";
        }
        else
        {
          return "";
        }
      }
    }
    public string PreviewPropertiesLowPassFilter
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          // Check if this signal is a filtered signal
          string Signal = PreviewPropertiesSelectedSignal;
          FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);

          if (filteredSignal != null && filteredSignal.LowPass_Enabled)
            return filteredSignal.LowPassCutoff.ToString("0.## Hz");
          else
            return "NA";
        }
        else
        {
          return "";
        }
      }
    }
    public string PreviewPropertiesSmoothFilter
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          // Check if this signal is a filtered signal
          string Signal = PreviewPropertiesSelectedSignal;
          FilteredSignal filteredSignal = sm.FilteredSignals.Find(temp => temp.SignalName == Signal);

          if (filteredSignal != null && filteredSignal.WeightedAverage_Enabled)
            return filteredSignal.WeightedAverage_Length.ToString("0.## ms");
          else
            return "NA";
        }
        else
        {
          return "";
        }
      }
    }

    // Preview Signal Selection
    public int PreviewCurrentCategory
    {
      get
      {
        return pm.PreviewCurrentCategory;
      }
      set
      {
        pm.PreviewCurrentCategory = value;
        OnPropertyChanged(nameof(PreviewCurrentCategory));
        PreviewCurrentCategory_Changed();
      }
    }
    public string PreviewCurrentCategoryName
    {
      get
      {
        if (PreviewCurrentCategory == -1)
          return "All";
        else
          return sm.SignalCategories[PreviewCurrentCategory].CategoryName;
      }
    }
    public ReadOnlyCollection<string> PreviewSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewCurrentCategory != -1)
            return Array.AsReadOnly(sm.SignalCategories[PreviewCurrentCategory].Signals.Where(temp => !sm.HiddenSignals.Contains(temp)).ToArray());
          else
          {
            List<string> output = new List<string>();
            output.AddRange(AllNonHiddenSignals.Where(temp => !sm.HiddenSignals.Contains(temp)).ToArray());
            return Array.AsReadOnly(output.ToArray());
          }
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }
    public void SetSelectedSignals(System.Collections.IList SelectedItems)
    {
      pm.PreviewSelectedSignals.Clear();
      for (int x = 0; x < SelectedItems.Count; x++)
        pm.PreviewSelectedSignals.Add(SelectedItems[x].ToString());

      DrawChart();
    }
    
    // Preview Plot Range
    public bool PreviewUseAbsoluteTime
    {
      get
      {
        return pm.PreviewUseAbsoluteTime;
      }
      set
      {
        pm.PreviewUseAbsoluteTime = value;
        OnPropertyChanged(nameof(PreviewUseAbsoluteTime));
        PreviewUseAbsoluteTime_Changed();
      }
    }
    public DateTime? PreviewViewStartTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return pm.PreviewViewStartTime;
          else
            return Utils.EpochtoDateTime(pm.PreviewViewStartRecord, LoadedEDFFile);
        }
        else
        {
          return null;
        }
      }
      set
      {
        if (PreviewUseAbsoluteTime && IsEDFLoaded)
        {
          if (pm.PreviewViewStartTime != (value ?? DateTime.Parse(EDFStartTime)))
          {
            pm.PreviewViewStartTime = value ?? DateTime.Parse(EDFStartTime);
            pm.PreviewViewStartRecord = Utils.DateTimetoEpoch(pm.PreviewViewStartTime, LoadedEDFFile);
            PreviewView_Changed();
          }
        }
      }
    }
    public int? PreviewViewStartRecord
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return Utils.DateTimetoEpoch(PreviewViewStartTime ?? new DateTime(), LoadedEDFFile);
          else
            return pm.PreviewViewStartRecord;
        }
        else
        {
          return null;
        }
      }
      set
      {
        if (!PreviewUseAbsoluteTime && IsEDFLoaded)
        {
          if (pm.PreviewViewStartRecord != (value ?? 1))
          {
            pm.PreviewViewStartRecord = value ?? 1;
            pm.PreviewViewStartTime = Utils.EpochtoDateTime(pm.PreviewViewStartRecord, LoadedEDFFile);
            PreviewView_Changed();
          }
        }
      }
    }
    public int? PreviewViewDuration
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return pm.PreviewViewDuration;
          else
            return Utils.TimeSpantoEpochPeriod(new TimeSpan(0, 0, pm.PreviewViewDuration));
        }
        else
        {
          return null;
        }
      }
      set
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
          {
            if (pm.PreviewViewDuration != (value ?? 1))
            {
              pm.PreviewViewDuration = value ?? 1;
              PreviewView_Changed();
            }
          }
          else
          {
            if (pm.PreviewViewDuration != (int)Utils.EpochPeriodtoTimeSpan((value ?? 1)).TotalSeconds)
            {
              pm.PreviewViewDuration = (int)Utils.EpochPeriodtoTimeSpan((value ?? 1)).TotalSeconds;
              PreviewView_Changed();
            }
          }
        }
      }
    }
    public DateTime PreviewViewEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return (PreviewViewStartTime ?? new DateTime()) + new TimeSpan(0, 0, 0, PreviewViewDuration ?? 1);
          else
            return (PreviewViewStartTime ?? new DateTime()) + Utils.EpochPeriodtoTimeSpan(PreviewViewDuration ?? 1);
        }
        else
        {
          return new DateTime();
        }
      }
    }

    public DateTime PreviewViewStartTimeMax
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          DateTime EndTime = DateTime.Parse(EDFEndTime); // EDF End Time
          TimeSpan duration = new TimeSpan(TimeSpan.TicksPerSecond * pm.PreviewViewDuration); // User Selected Duration 
          return EndTime - duration; 
        }
        else
          return new DateTime();
      }
    }
    public DateTime PreviewViewStartTimeMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return LoadedEDFFile.Header.StartDateTime; // Start Time
        else
          return new DateTime();
      }
    }
    public int PreviewViewStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(PreviewViewStartTimeMax, LoadedEDFFile); // PreviewViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int PreviewViewStartRecordMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(PreviewViewStartTimeMin, LoadedEDFFile); // PreviewViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int PreviewViewDurationMax
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
        {
          DateTime EndTime = DateTime.Parse(EDFEndTime); // EDF End Time
          TimeSpan duration = EndTime - (PreviewViewStartTime ?? new DateTime()); // Theoretical Limit Duration
          TimeSpan limit = new TimeSpan(TimeSpan.TicksPerHour * 2); // Practical Limit Duration

          if (pm.PreviewUseAbsoluteTime)
            return Math.Min(
                (int)limit.TotalSeconds,
                (int)duration.TotalSeconds
                );
          else
            return Math.Min(
                Utils.TimeSpantoEpochPeriod(limit),
                Utils.TimeSpantoEpochPeriod(duration)
                );
        }
        else // No File Loaded
          return 0;
      }
    }
    public int PreviewViewDurationMin
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
          return 1;
        else // No File Loaded
          return 0;
      }
    }
    public bool PreviewNavigationEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return pm.PreviewNavigationEnabled;
      }
      set
      {
        pm.PreviewNavigationEnabled = value;
        OnPropertyChanged(nameof(PreviewNavigationEnabled));
        OnPropertyChanged(nameof(PreviewProgressRingEnabled));
      }
    }
    public bool PreviewProgressRingEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return !pm.PreviewNavigationEnabled;
      }
    }

    // Preview Other
    public bool PreviewUseConstantAxis
    {
      get
      {
        return pm.PreviewUseConstantAxis;
      }
      set
      {
        pm.PreviewUseConstantAxis = value;
        OnPropertyChanged(nameof(PreviewUseConstantAxis));
        PreviewUseConstantAxis_Changed();
      }
    }
    // Preview Plot
    public PlotModel PreviewSignalPlot
    {
      get
      {
        return pm.PreviewSignalPlot;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        pm.PreviewSignalPlot = value;
        OnPropertyChanged(nameof(PreviewSignalPlot));
        PreviewSignalPlot_Changed();
      }
    }

    
    /****************************************************** EEG ANALYSIS TAB ********************************************************/

    //EEG Anaylsis
    public string EEGEDFSelectedSignal
    {
      get
      {
        return eegm.EEGEDFSelectedSignal;
      }
      set
      {
        eegm.EEGEDFSelectedSignal = value;
        OnPropertyChanged(nameof(EEGEDFSelectedSignal));
      }
    }
    public int? EpochForAnalysis
    {
      get
      {
        return eegm.EpochForAnalysis;
      }
      set
      {
        eegm.EpochForAnalysis = value ?? 1;
        OnPropertyChanged(nameof(EpochForAnalysis));
      }
    }

    public int? EEGEpochForAnalysisBinary {
      get {
        return eegm.EEGBinaryEpochForAnalysis;
      }
      set {
        eegm.EEGBinaryEpochForAnalysis = value ?? 1;

        if (IsEEGBinaryLoaded == true) {
          DateTime bin_start_time = DateTime.Parse(eeg_bin_date_time_from);
          DateTime curr_date_time = bin_start_time.AddSeconds(30 * (eegm.EEGBinaryEpochForAnalysis-1));

          float sample_period = 1 / float.Parse(eeg_bin_sample_frequency_s);

          if (curr_date_time < DateTime.Parse(eeg_bin_date_time_to))
          {
            BW_EEGAnalysisBin(eeg_bin_signal_name, eeg_bin_signal_values, bin_start_time, curr_date_time, curr_date_time.AddSeconds(30), sample_period);
          }
          else {
            int seconds_diff = (int) (DateTime.Parse(eeg_bin_date_time_to).Subtract(DateTime.Parse(eeg_bin_date_time_from)).TotalSeconds);
            eegm.EEGBinaryEpochForAnalysis = seconds_diff / 30;
          }
        }
        OnPropertyChanged(nameof(EEGEpochForAnalysisBinary));
      }
    }

    public int? ExportEpochStart
    {
      get
      {
        return eegm.ExportEpochStart;
      }
      set
      {
        eegm.ExportEpochStart = value ?? 1;
        OnPropertyChanged(nameof(ExportEpochStart));
        EEGView_Changed();
      }
    }
    public int? ExportEpochEnd
    {
      get
      {
        return eegm.ExportEpochEnd;
      }
      set
      {
        eegm.ExportEpochEnd = value ?? 1;
        OnPropertyChanged(nameof(ExportEpochEnd));
      }
    }
    public PlotModel PlotAbsPwr
    {
      get
      {
        return eegm.PlotAbsPwr;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        eegm.PlotAbsPwr = value;
        OnPropertyChanged(nameof(PlotAbsPwr));
      }
    }
    public PlotModel PlotRelPwr
    {
      get
      {
        return eegm.PlotRelPwr;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        eegm.PlotRelPwr = value;
        OnPropertyChanged(nameof(PlotRelPwr));
      }
    }

    public PlotModel PlotSpecGram
    {
      get
      {
        return eegm.PlotSpecGram;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        eegm.PlotSpecGram = value;
        OnPropertyChanged(nameof(PlotSpecGram));
      }
    }

    public PlotModel PlotPSD
    {
      get
      {
        return eegm.PlotPSD;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        eegm.PlotPSD = value;
        OnPropertyChanged(nameof(PlotPSD));
      }
    }
    public String[] EEGExportOptions
    {
      get
      {
        return eegm.EEGExportOptions;
      }
      set
      {
        eegm.EEGExportOptions = new String[] { "Absolute Power", "RelativePower", "PSD", "Sepctrogram" };
      }
    }

    public DateTime EEGEDFStartTime
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.EpochtoDateTime(EpochForAnalysis ?? 1, LoadedEDFFile);
        else
          return new DateTime();
      }
    }
    public DateTime EEGEDFStartTimeMax
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          DateTime EndTime = DateTime.Parse(EDFEndTime); // EDF End Time
          return EndTime;
        }
        else
          return new DateTime();
      }
    }
    public DateTime EEGEDFStartTimeMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return LoadedEDFFile.Header.StartDateTime; // Start Time
        else
          return new DateTime();
      }
    }
    public int EEGEDFStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(EEGEDFStartTimeMax, LoadedEDFFile) - 1; // EEGViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int EEGEDFStartRecordMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(EEGEDFStartTimeMin, LoadedEDFFile); // EEGViewStartTimeMax to Record
        else
          return 0;
      }
    }

    /**************************************************** COHERENCE ANALYSIS TAB ****************************************************/

    public string CoherenceEDFSelectedSignal1
    {
      get
      {
        return cm.CoherenceEDFSelectedSignal1;
      }
      set
      {
        cm.CoherenceEDFSelectedSignal1 = value;
        OnPropertyChanged(nameof(CoherenceEDFSelectedSignal1));
        PerformCoherenceAnalysisEDF();
      }
    }
    public string CoherenceEDFSelectedSignal2
    {
      get
      {
        return cm.CoherenceEDFSelectedSignal2;
      }
      set
      {
        cm.CoherenceEDFSelectedSignal2 = value;
        OnPropertyChanged(nameof(CoherenceEDFSelectedSignal2));
        PerformCoherenceAnalysisEDF();
      }
    }
    public int? CoherenceEDFStartRecord
    {
      get
      {
        if (IsEDFLoaded)
          return cm.CoherenceEDFStartRecord;
        else
          return null;
      }
      set
      {
        cm.CoherenceEDFStartRecord = value ?? 1;
        OnPropertyChanged(nameof(CoherenceEDFStartRecord));
        CoherenceView_Changed();
        PerformCoherenceAnalysisEDF();
      }
    }
    public int? CoherenceEDFDuration
    {
      get
      {
        if (IsEDFLoaded)
          return cm.CoherenceEDFDuration;
        else
          return null;
      }
      set
      {
        cm.CoherenceEDFDuration = value ?? 1;
        OnPropertyChanged(nameof(CoherenceEDFDuration));
        CoherenceView_Changed();
        PerformCoherenceAnalysisEDF();
      }
    }
    public PlotModel CoherenceSignalPlot1
    {
      get
      {
        return cm.CoherenceSignalPlot1;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        cm.CoherenceSignalPlot1 = value;
        OnPropertyChanged(nameof(CoherenceSignalPlot1));
      }
    }
    public PlotModel CoherenceSignalPlot2
    {
      get
      {
        return cm.CoherenceSignalPlot2;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        cm.CoherenceSignalPlot2 = value;
        OnPropertyChanged(nameof(CoherenceSignalPlot2));
      }
    }
    public PlotModel CoherencePlot
    {
      get
      {
        return cm.CoherencePlot;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        cm.CoherencePlot = value;
        OnPropertyChanged(nameof(CoherencePlot));
        CoherencePlot_Changed();
      }
    }
    public bool CoherenceProgressRingEnabled
    {
      get
      {
        return cm.CoherenceProgressRingEnabled;
      }
      set
      {
        cm.CoherenceProgressRingEnabled = value;
        OnPropertyChanged(nameof(CoherenceProgressRingEnabled));
        OnPropertyChanged(nameof(CoherenceEDFNavigationEnabled));
      }
    }
    public bool CoherenceEDFNavigationEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return !CoherenceProgressRingEnabled;
      }
    }

    public DateTime CoherenceEDFStartTime
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.EpochtoDateTime(CoherenceEDFStartRecord ?? 1, LoadedEDFFile);
        else
          return new DateTime();
      }
    }
    public DateTime CoherenceEDFStartTimeMax
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          DateTime EndTime = DateTime.Parse(EDFEndTime); // EDF End Time
          TimeSpan duration = Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1); // User Selected Duration 
          return EndTime - duration;
        }
        else
          return new DateTime();
      }
    }
    public DateTime CoherenceEDFStartTimeMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return LoadedEDFFile.Header.StartDateTime; // Start Time
        else
          return new DateTime();
      }
    }
    public int CoherenceEDFStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(CoherenceEDFStartTimeMax, LoadedEDFFile); // CoherenceViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int CoherenceEDFStartRecordMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(CoherenceEDFStartTimeMin, LoadedEDFFile); // CoherenceViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int CoherenceEDFDurationMax
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
        {
          DateTime EndTime = DateTime.Parse(EDFEndTime); // EDF End Time
          TimeSpan duration = EndTime - (CoherenceEDFStartTime); // Theoretical Limit Duration
          TimeSpan limit = new TimeSpan(TimeSpan.TicksPerHour * 2); // Practical Limit Duration

          return Math.Min(
              Utils.TimeSpantoEpochPeriod(limit),
              Utils.TimeSpantoEpochPeriod(duration)
              );
        }
        else // No File Loaded
          return 0;
      }
    }
    public int CoherenceEDFDurationMin
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
          return 1;
        else // No File Loaded
          return 0;
      }
    }

    public bool CoherenceUseConstantAxis
    {
      get
      {
        return cm.CoherenceUseConstantAxis;
      }
      set
      {
        cm.CoherenceUseConstantAxis = value;
        OnPropertyChanged(nameof(CoherenceUseConstantAxis));
        PerformCoherenceAnalysisEDF();
      }
    }

    /********************************************************** SETTINGS ***********************************************************/

    // Settings Flyout
    public bool FlyoutOpen
    {
      get
      {
        return sm.FlyoutOpen;
      }
      set
      {
        sm.FlyoutOpen = value;
        OnPropertyChanged(nameof(FlyoutOpen));
      }
    }
    public bool SettingsMainMenuVisible
    {
      get
      {
        return sm.SettingsMainMenuVisible;
      }
      set
      {
        sm.SettingsMainMenuVisible = value;
        OnPropertyChanged(nameof(SettingsMainMenuVisible));
      }
    }
    public bool SettingsPersonalizationVisible
    {
      get
      {
        return sm.SettingsPersonalizationVisible;
      }
      set
      {
        sm.SettingsPersonalizationVisible = value;
        OnPropertyChanged(nameof(SettingsPersonalizationVisible));
      }
    }

    // Recent File List and Functions. 
    public ReadOnlyCollection<string> RecentFiles
    {
      get
      {
        if (!Directory.Exists("Settings"))
          Directory.CreateDirectory("Settings");

        string[] value = null;

        if (File.Exists("Settings\\recent.txt"))
        {
          StreamReader sr = new StreamReader("Settings\\recent.txt");
          string[] text = sr.ReadToEnd().Split('\n');
          List<string> values = new List<string>();
          for (int x = 0; x < text.Length; x++)
            if (File.Exists(text[x].Trim()))
              values.Add(text[x].Trim());
          sr.Close();

          value = values.ToArray();
        }
        else
        {
          value = new string[0];
        }

        return Array.AsReadOnly(value);
      }
    }

    public void RecentFiles_Add(string path)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<string> array = RecentFiles.ToArray().ToList();
      array.Insert(0, path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("Settings\\recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }
    public void RecentFiles_Remove(string path)
    {
      if (!Directory.Exists("Settings"))
        Directory.CreateDirectory("Settings");

      List<string> array = RecentFiles.ToArray().ToList();
      array.Remove(path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("Settings\\recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }
   
    /********************************************************** ZABEEH'S BINARY STUFF ****************************************************/

    public int EEGAnalysisBinaryFileLoaded = 0;
    public bool IsEEGBinaryLoaded
    {
      get {
        if (EEGAnalysisBinaryFileLoaded == 0)
        {
          return false;
        }
        return true;
      }
    }
    public object EEGBinaryMaxEpoch {
      get {
        return eeg_bin_max_epochs.ToString();
      }
      set {
      }
    }

    // Used For Importing From Binary For EEG Signals
    private List<float> eeg_bin_signal_values;
    private string eeg_bin_signal_name;
    private string eeg_bin_subject_id;
    private string eeg_bin_date_time_from;
    private string eeg_bin_date_time_to;
    private string eeg_bin_sample_frequency_s;
    private DateTime eeg_bind_curr_from;
    private int eeg_bin_max_epochs;

    /*********************************************************************************************************************************/

    #endregion

    #region etc

    // INotify Interface
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public ModelView(MainWindow i_window, SettingsModel i_sm)
    {
      p_window = i_window;
      sm = i_sm;

      #region Preload MATLAB functions into memory
      {
        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += new DoWorkEventHandler(
          delegate (object sender1, DoWorkEventArgs e1)
          {
            Utils.MATLAB_Coherence(new float[] { 1, 1, 1, 1, 1, 1, 1, 1 }, new float[] { 1, 1, 1, 1, 1, 1, 1, 1 });
          }
          );
        bw.RunWorkerAsync();
      }
      {
        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += new DoWorkEventHandler(
          delegate (object sender1, DoWorkEventArgs e1)
          {
            Utils.MATLAB_Resample(new float[] { 1, 1, 1, 1, 1, 1, 1, 1 }, 2);
          }
          );
        bw.RunWorkerAsync();
      }
      #endregion 
    }
  }
}
