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
using System.Windows.Forms;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    ModelView model;

    /// <summary>
    /// Function called to populate recent files list. Called when application is first loaded and if the recent files list changes.
    /// </summary>
    public void LoadRecent()
    {
      List<string> array = model.RecentFiles.ToArray().ToList();

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

      model = new ModelView(this);
      this.DataContext = model;
      LoadRecent();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      model.WriteSettings();
    }

    // Home Tab Events
    private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
    {
      model.LoadedEDFFile = null;

      Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
      dialog.Filter = "EDF files (*.edf)|*.edf";
      dialog.Title = "Select an EDF file";

      if (dialog.ShowDialog() == true)
      {
        model.LoadEDFFile(dialog.FileName);
      }
    }
    private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
    {
      model.LoadedEDFFile = null;

      List<string> array = model.RecentFiles.ToArray().ToList();
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
            model.LoadEDFFile(selected[x]);
            break;
          }
          else
          {
            this.ShowMessageAsync("Error", "File not Found");
            model.RecentFiles_Remove(selected[x]);
          }
        }
      }
    }

    // Preview Tab Events   
    private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      model.SetSelectedSignals(listBox_SignalSelect.SelectedItems);
    }
    private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (comboBox_SignalSelect.SelectedValue != null)
      {
        EDFSignal edfsignal = model.LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == comboBox_SignalSelect.SelectedValue.ToString().Trim());
        textBox_SampRecord.Text = ((int)((double)edfsignal.NumberOfSamplesPerDataRecord / (double)model.LoadedEDFFile.Header.DurationOfDataRecordInSeconds)).ToString();
      }
      else
      {
        textBox_SampRecord.Text = "";
      }
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

    private void button_HideSignals_Click(object sender, RoutedEventArgs e)
    {
      model.HideSignals();
    }
    private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
    {
      model.AddDerivative();
    }
    private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
    {
      model.RemoveDerivative();
    }
    private void button_Categories_Click(object sender, RoutedEventArgs e)
    {
      model.ManageCategories();
    }
    private void button_Next_Click(object sender, RoutedEventArgs e)
    {
      model.NextCategory();
    }
    private void button_Prev_Click(object sender, RoutedEventArgs e)
    {
      model.PreviousCategory();
    }

    private void export_button_Click(object sender, RoutedEventArgs e)
    {
      model.ExportSignals();
    }

    private void button_PerformRespiratoryAnalysis_Click(object sender, RoutedEventArgs e)
    {
      model.PerformRespiratoryAnalysisEDF();
    }

    private void button_PerformEEGAnalysis_Click(object sender, RoutedEventArgs e)
    {
      model.PerformEEGAnalysisEDF();
    }
  }

  #region Models
  public class PreviewModel
  {
    public int PreviewCurrentCategory = -1;
    public List<string> PreviewSelectedSignals = new List<string>();

    public bool PreviewUseAbsoluteTime = false;
    public DateTime PreviewViewStartTime = new DateTime();
    public int PreviewViewStartRecord = 0;
    public int PreviewViewDuration = 0;
    public PlotModel PreviewSignalPlot = null;
    public bool PreviewNavigationEnabled = false;
  }
  public class RespiratoryModel
  {
    public string RespiratoryEDFSelectedSignal;
    public int RespiratoryEDFStartRecord;
    public int RespiratoryEDFDuration;
    public PlotModel RespiratorySignalPlot = null;
    public string RespiratoryBreathingPeriodMean;
    public string RespiratoryBreathingPeriodMedian;
    public int RespiratoryMinimumPeakWidth = 500;
    public bool RespiratoryRemoveMultiplePeaks = true;
  }
  public class EEGModel
  {
    public string EEGEDFSelectedSignal;
    public int EEGEDFStartRecord;
    public int EEGEDFDuration;
    public PlotModel EEGSignalPlot = null;
  }
  public class SettingsModel
  {
    public List<string> SignalCategories = new List<string>();
    public List<List<string>> SignalCategoryContents = new List<List<string>>();
    public List<string[]> DerivedSignals = new List<string[]>();
    public List<string> HiddenSignals = new List<string>();
    public List<string[]> SignalsMinValues = new List<string[]>();
    public List<string[]> SignalsMaxValues = new List<string[]>();
  }
  #endregion

  public class ModelView : INotifyPropertyChanged
  {
    #region HELPER FUNCTIONS 

    /******************************************************* STATIC FUNCTIONS *******************************************************/

    // Static Functions
    /// <summary>
    /// Contains epoch definition in seconds
    /// </summary>
    private static int EPOCH_SEC = 30;
    /// <summary>
    /// Converts a point in time measured in epochs to a DateTime structure
    /// </summary>
    /// <param name="epoch"> The point in time in epoch </param>
    /// <param name="file"> The EDF File (for determining the DateTime of epoch 0) </param>
    /// <returns> The point in time in a DateTime structure </returns>
    private static DateTime EpochtoDateTime(int epoch, EDFFile file)
    {
      // DateTime = StartTime + epoch * EPOCH_SEC 
      return file.Header.StartDateTime + new TimeSpan(0, 0, epoch * EPOCH_SEC);
    }
    /// <summary>
    /// Converts a period of time measured in epoch to a TimeSpan structure
    /// </summary>
    /// <param name="period"> The period of time in epoch </param>
    /// <returns> The period of time in a TimeSpan structure</returns>
    private static TimeSpan EpochPeriodtoTimeSpan(int period)
    {
      // TimeSpan = period * EPOCH_SEC
      return new TimeSpan(0, 0, 0, period * EPOCH_SEC);
    }
    /// <summary>
    /// Converts a DateTime structure to an epoch point in time
    /// </summary>
    /// <param name="time"> The DateTime structure </param>
    /// <param name="file"> The EDF File (for determining the DateTime of epoch 0) </param>
    /// <returns> The epoch point in time </returns>
    private static int DateTimetoEpoch(DateTime time, EDFFile file)
    {
      // epoch = (DateTime - StartTime) / EPOCH_SEC
      return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)EPOCH_SEC);
    }
    /// <summary>
    /// Converts a TimeSpan structure to an epoch period
    /// </summary>
    /// <param name="period"> The TimeSpan structure </param>
    /// <returns> The epoch period </returns>
    private static int TimeSpantoEpochPeriod(TimeSpan period)
    {
      // period = TimeSpan / EPOCH_SEC
      return (int)(period.TotalSeconds / (double)EPOCH_SEC);
    }

    /// <summary>
    /// Given an array of numbers and a desired percentile returns a value at that percentile (within a given tolerance)
    /// </summary>
    /// <param name="values_array"> An array of numbers </param>
    /// <param name="percentile"> Desired percentile </param>
    /// <param name="tolerance"> Given tolerance </param>
    /// <returns> Value at given percentile </returns>
    private static double? GetPercentileValue(float[] values_array, int percentile, int tolerance)
    {
      List<float> values = values_array.ToList();
      values.Sort();

      // index = percentile/100 * length
      int index = (int)((double)percentile / (double)100 * (double)values.Count);

      return values[index];
    }
    /// <summary>
    /// Given two arrays of numbers and a desired percentile returns a value at that percentile (within a given tolerance) for the difference between the two arrays
    /// </summary>
    /// <param name="values_array_1"> First array of numbers (minuend) </param>
    /// <param name="values_array_2"> Second array of numbers (subtrahend) </param>
    /// <param name="percentile"> Desired percentile </param>
    /// <param name="tolerance"> Given tolerance </param>
    /// <returns> Value at given percentile </returns>
    private static double? GetPercentileValueDeriv(float[] values_array_1, float[] values_array_2, int percentile, int tolerance)
    {
      List<float> values1 = values_array_1.ToList();
      List<float> values2 = values_array_2.ToList();

      // Subtract Two arrays
      List<float> values = new List<float>();
      for (int x = 0; x < Math.Min(values_array_1.Length, values_array_2.Length); x++)
        values.Add(values_array_1[x] - values_array_2[x]);

      // Get percentile for the difference array
      return GetPercentileValue(values.ToArray(), percentile, tolerance);
    }

    /***************************************************** NON-STATIC FUNCTIONS *****************************************************/

    /// <summary>
    /// Returns a series of X, Y point given the signal name and start and end times. Also returns other signal information
    /// </summary>
    /// <param name="sample_period"> (Output) Time between samples for the given signal </param>
    /// <param name="max_y"> (Output) The max y value to be displayed on the graph </param>
    /// <param name="min_y"> (Output) The min y value to be displayed on the graph </param>
    /// <param name="Signal"> The desired signal to get values for </param>
    /// <param name="StartTime"> The time of the first value </param>
    /// <param name="EndTime"> The time of the last value </param>
    /// <returns> A series of X, Y points </returns>
    private LineSeries GetSeriesFromSignalName(out float sample_period, out double? max_y, out double? min_y, string Signal, DateTime StartTime, DateTime EndTime)
    {
      // Variable To Return
      LineSeries series = new LineSeries();
      series.MinimumSegmentLength = 10;

      if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal.Trim()) != null) // Normal EDF Signal
      {
        // Get Signal using API
        EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal);

        // Determine Array Portion
        sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;
        int startIndex, indexCount;
        TimeSpan startPoint = StartTime - LoadedEDFFile.Header.StartDateTime;
        TimeSpan duration = EndTime - StartTime;
        startIndex = (int)(startPoint.TotalSeconds / sample_period);
        indexCount = (int)(duration.TotalSeconds / sample_period);

        // Get Array
        List<float> values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);

        // Determine Y Axis Bounds
        min_y = GetMinSignalValue(Signal, values);
        max_y = GetMaxSignalValue(Signal, values);

        // Add Points to Series
        for (int y = startIndex; y < indexCount + startIndex; y++)
        {
          series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
        }
      }
      else // Derivative Signal
      {
        // Get Signals
        string[] deriv_info = sm.DerivedSignals.Find(temp => temp[0] == Signal);
        EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[1].Trim());
        EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[2].Trim());

        // Get Arrays and Perform Resampling if needed
        List<float> values1;
        List<float> values2;
        if (edfsignal1.NumberOfSamplesPerDataRecord == edfsignal2.NumberOfSamplesPerDataRecord) // No resampling
        {
          values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
          values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);
          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
        }
        else if (edfsignal1.NumberOfSamplesPerDataRecord > edfsignal2.NumberOfSamplesPerDataRecord) // Upsample signal 2
        {
          // Prepare Input for MATLAB function
          Processing proc = new Processing();
          MWArray[] input = new MWArray[2];
          input[0] = new MWNumericArray(LoadedEDFFile.retrieveSignalSampleValues(edfsignal2).ToArray());
          input[1] = edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord;

          values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
          // Call MATLAB function
          values2 = (
                      (double[])(
                          (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                      ).ToVector(MWArrayComponent.Real)
                    ).ToList().Select(temp => (float)temp).ToList();

          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
        }
        else // Upsample signal 1
        {
          // Prepare Input for MATLAB function
          Processing proc = new Processing();
          MWArray[] input = new MWArray[2];
          input[0] = new MWNumericArray(LoadedEDFFile.retrieveSignalSampleValues(edfsignal1).ToArray());
          input[1] = edfsignal2.NumberOfSamplesPerDataRecord / edfsignal1.NumberOfSamplesPerDataRecord;

          // Call MATLAB function
          values1 = (
                      (double[])(
                          (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                      ).ToVector(MWArrayComponent.Real)
                    ).ToList().Select(temp => (float)temp).ToList();
          values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);

          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal2.NumberOfSamplesPerDataRecord;
        }

        // Determine Array Portions
        int startIndex, indexCount;
        TimeSpan startPoint = StartTime - LoadedEDFFile.Header.StartDateTime;
        TimeSpan duration = EndTime - StartTime;
        startIndex = (int)(startPoint.TotalSeconds / sample_period);
        indexCount = (int)(duration.TotalSeconds / sample_period);

        // Get Y Axis Bounds
        min_y = GetMinSignalValue(Signal, values1, values2);
        max_y = GetMaxSignalValue(Signal, values1, values2);

        // Add Points to Series
        for (int y = startIndex; y < indexCount + startIndex; y++)
        {
          series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values1[y] - values2[y]));
        }
      }

      return series;
    }

    #endregion

    #region ACTIONS

    // Load EDF File
    /// <summary>
    /// The progress bar
    /// </summary>
    private ProgressDialogController controller;
    /// <summary>
    /// Background function for loading an EDF file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
    {
      controller.SetCancelable(false);

      EDFFile temp = new EDFFile();
      temp.readFile(e.Argument.ToString());
      LoadedEDFFile = temp;

      // Load Settings Files
      LoadSettings();
    }
    /// <summary>
    /// Background function completion function for loading an EDF file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
    {
      RecentFiles_Add(LoadedEDFFileName);

      await controller.CloseAsync();
      await p_window.ShowMessageAsync("Success!", "EDF file loaded");
    }
    /// <summary>
    /// Load EDF file
    /// </summary>
    /// <param name="fileNameIn"> Desired EDF file path </param>
    public async void LoadEDFFile(string fileNameIn)
    {
      controller = await p_window.ShowProgressAsync("Please wait...", "Loading EDF File: " + fileNameIn);

      LoadedEDFFileName = fileNameIn;
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_LoadEDFFile;
      bw.RunWorkerCompleted += BW_FinishLoad;
      bw.RunWorkerAsync(LoadedEDFFileName);
    }

    // Create Preview Chart
    /// <summary>
    /// Background function for drawing a chart in the preview tab
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_CreateChart(object sender, DoWorkEventArgs e)
    {
      PlotModel temp_PreviewSignalPlot = new PlotModel();
      temp_PreviewSignalPlot.Series.Clear();
      temp_PreviewSignalPlot.Axes.Clear();

      if (pm.PreviewSelectedSignals.Count > 0)
      {
        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.Key = "DateTime";
        xAxis.Minimum = DateTimeAxis.ToDouble(PreviewViewStartTime);
        xAxis.Maximum = DateTimeAxis.ToDouble(PreviewViewEndTime);
        temp_PreviewSignalPlot.Axes.Add(xAxis);

        for (int x = 0; x < pm.PreviewSelectedSignals.Count; x++)
        {
          double? min_y, max_y;
          float sample_period;
          LineSeries series = GetSeriesFromSignalName(out sample_period,
                                                      out max_y,
                                                      out min_y,
                                                      pm.PreviewSelectedSignals[x],
                                                      (PreviewViewStartTime ?? new DateTime()),
                                                      PreviewViewEndTime
                                                      );

          series.YAxisKey = pm.PreviewSelectedSignals[x];
          series.XAxisKey = "DateTime";

          LinearAxis yAxis = new LinearAxis();
          yAxis.MajorGridlineStyle = LineStyle.Solid;
          yAxis.MinorGridlineStyle = LineStyle.Dot;
          yAxis.Title = pm.PreviewSelectedSignals[x];
          yAxis.Key = pm.PreviewSelectedSignals[x];
          yAxis.EndPosition = (double)1 - (double)x * ((double)1 / (double)pm.PreviewSelectedSignals.Count);
          yAxis.StartPosition = (double)1 - (double)(x + 1) * ((double)1 / (double)pm.PreviewSelectedSignals.Count);
          yAxis.Maximum = max_y ?? Double.NaN;
          yAxis.Minimum = min_y ?? Double.NaN;

          temp_PreviewSignalPlot.Axes.Add(yAxis);
          temp_PreviewSignalPlot.Series.Add(series);
        }
      }

      PreviewSignalPlot = temp_PreviewSignalPlot;
    }
    /// <summary>
    /// Background completion function called after drawing chart in the preview tab
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
    {
      PreviewNavigationEnabled = true;
    }
    /// <summary>
    /// Draw a chart in the preview pane
    /// </summary>
    public void DrawChart()
    {
      PreviewNavigationEnabled = false;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_CreateChart;
      bw.RunWorkerCompleted += BW_FinishChart;
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

          bw.RunWorkerAsync(arguments);
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

      //hdr file contains metadata of the binary file
      FileStream hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + ".hdr", FileMode.OpenOrCreate);
      hdr_file.SetLength(0); //clear it's contents
      hdr_file.Close(); //flush
      hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + ".hdr", FileMode.OpenOrCreate); //reload

      StringBuilder sb_hdr = new StringBuilder(); // string builder used for writing into the file

      sb_hdr.AppendLine(signals_data.Subject_ID.ToString()) // subject id
          .AppendLine(signals_data.Epochs_From.ToString()) // epoch start
          .AppendLine(signals_data.Epochs_To.ToString()); // epoch end         

      foreach (var signal in pm.PreviewSelectedSignals)
      {
        var edfSignal = LoadedEDFFile.Header.Signals.Find(s => s.Label.Trim() == signal.Trim());
        var signalValues = LoadedEDFFile.retrieveSignalSampleValues(edfSignal).ToArray();

        FileStream bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //the binary file for each signal
        bin_file.SetLength(0); //clear it's contents
        bin_file.Close(); //flush

        bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //reload
        BinaryWriter bin_writer = new BinaryWriter(bin_file);

        int start_index = signals_data.Epochs_From * 30 * edfSignal.NumberOfSamplesPerDataRecord; // from epoch number * 30 seconds per epoch * sample rate = start time
        int end_index = signals_data.Epochs_To * 30 * edfSignal.NumberOfSamplesPerDataRecord; // to epoch number * 30 seconds per epoch * sample rate = end time

        if (end_index > signalValues.Count()) { end_index = signalValues.Count(); }


        sb_hdr.AppendLine(signal); //append signal name that is being exported in order to parse binary file later

        for (int i = start_index; i < end_index; i++)
        {
          bin_writer.Write(signalValues[i]);
        }

        bin_writer.Close();
      }

      var bytes_to_write = Encoding.ASCII.GetBytes(sb_hdr.ToString());
      hdr_file.Write(bytes_to_write, 0, bytes_to_write.Length);
      hdr_file.Close();
    }

    // Respiratory Analysis From EDF File
    /// <summary>
    /// Background function for performing Respiratory analysis on signals stored in an EDF file 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_RespiratoryAnalysisEDF(object sender, DoWorkEventArgs e)
    {
      PlotModel temp_SignalPlot = new PlotModel();

      temp_SignalPlot.Series.Clear();
      temp_SignalPlot.Axes.Clear();

      double? max_y, min_y;
      float sample_period;
      LineSeries series = GetSeriesFromSignalName(out sample_period,
                                                  out max_y,
                                                  out min_y,
                                                  RespiratoryEDFSelectedSignal,
                                                  EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile),
                                                  EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile) + EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 0)
                                                  );

      // Plot Insets of Respiration Expiration

      // Calculate Bias
      double bias = 0;
      for (int x = 0; x < series.Points.Count; x++)
      {
        double point_1 = series.Points[x].Y;
        double point_2 = x + 1 < series.Points.Count ? series.Points[x + 1].Y : series.Points[x].Y;
        double average = (point_1 + point_2) / 2;
        bias += average / (double)series.Points.Count;
      }

      // Weighted Running Average (Smoothing) and Normalization
      LineSeries series_norm = new LineSeries();
      int LENGTH = 50;
      for (int x = 0; x < series.Points.Count; x++)
      {
        double sum = 0;
        double weight_sum = 0;
        for (int y = -LENGTH / 2; y <= LENGTH / 2; y++)
        {
          double weight = (LENGTH / 2 + 1) - Math.Abs(y);
          weight_sum += weight;
          sum += weight * series.Points[Math.Min(series.Points.Count - 1, Math.Max(0, x - y))].Y;
        }
        double average = sum / weight_sum;

        series_norm.Points.Add(new DataPoint(series.Points[x].X, average - bias));
      }

      // Find Peaks and Zero Crossings
      int min_spike_length = (int)((double)((double)RespiratoryMinimumPeakWidth / (double)1000) / (double)sample_period);
      int spike_length = 0;
      int maxima = 0;
      int start = 0;
      bool? positive = null;
      ScatterSeries series_pos_peaks = new ScatterSeries();
      ScatterSeries series_neg_peaks = new ScatterSeries();
      ScatterSeries series_insets = new ScatterSeries();
      ScatterSeries series_onsets = new ScatterSeries();
      for (int x = 0; x < series_norm.Points.Count; x++)
      {
        if (positive != false)
        {
          if (series_norm.Points[x].Y < 0 || x == series_norm.Points.Count - 1)
          {
            if (maxima != 0 && spike_length > min_spike_length)
            {
              if (!RespiratoryRemoveMultiplePeaks || series_pos_peaks.Points.Count == 0 || series_neg_peaks.Points.Count == 0 ||
                  (DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) > DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))) // Last Peak Was Negative
              {
                series_pos_peaks.Points.Add(new ScatterPoint(series_norm.Points[maxima].X, series_norm.Points[maxima].Y));
                series_onsets.Points.Add(new ScatterPoint(series_norm.Points[start].X, series_norm.Points[start].Y));
              }
              else
              {
                if (series_norm.Points[maxima].Y < series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].Y) // This Peak is Less than the previous
                {
                  // Do Nothing
                }
                else
                {
                  series_pos_peaks.Points.Remove(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1]);
                  series_onsets.Points.Remove(series_onsets.Points[series_onsets.Points.Count - 1]);

                  series_pos_peaks.Points.Add(new ScatterPoint(series_norm.Points[maxima].X, series_norm.Points[maxima].Y));
                  series_onsets.Points.Add(new ScatterPoint(series_norm.Points[start].X, series_norm.Points[start].Y));
                }
              }
            }
            positive = false;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          else
          {
            if (Math.Abs(series_norm.Points[x].Y) > Math.Abs(series_norm.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
        else
        {
          if (series_norm.Points[x].Y > 0 || x == series_norm.Points.Count - 1)
          {
            if (maxima != 0 && spike_length > min_spike_length)
            {
              if (!RespiratoryRemoveMultiplePeaks || series_pos_peaks.Points.Count == 0 || series_neg_peaks.Points.Count == 0 ||
                  (DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) < DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))) // Last Peak Was Positive
              {
                series_neg_peaks.Points.Add(new ScatterPoint(series_norm.Points[maxima].X, series_norm.Points[maxima].Y));
                series_insets.Points.Add(new ScatterPoint(series_norm.Points[start].X, series_norm.Points[start].Y));
              }
              else
              {
                if (series_norm.Points[maxima].Y > series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].Y) // This Peak is Less than the previous
                {
                  // Do Nothing
                }
                else // Remove Previous Peak
                {
                  series_neg_peaks.Points.Remove(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1]);
                  series_insets.Points.Remove(series_insets.Points[series_insets.Points.Count - 1]);

                  series_neg_peaks.Points.Add(new ScatterPoint(series_norm.Points[maxima].X, series_norm.Points[maxima].Y));
                  series_insets.Points.Add(new ScatterPoint(series_norm.Points[start].X, series_norm.Points[start].Y));
                }
              }
            }
            positive = true;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          else
          {
            if (Math.Abs(series_norm.Points[x].Y) > Math.Abs(series_norm.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
      }

      series_norm.YAxisKey = RespiratoryEDFSelectedSignal;
      series_norm.XAxisKey = "DateTime";
      series_onsets.YAxisKey = RespiratoryEDFSelectedSignal;
      series_onsets.XAxisKey = "DateTime";
      series_insets.YAxisKey = RespiratoryEDFSelectedSignal;
      series_insets.XAxisKey = "DateTime";
      series_pos_peaks.YAxisKey = RespiratoryEDFSelectedSignal;
      series_pos_peaks.XAxisKey = "DateTime";
      series_neg_peaks.YAxisKey = RespiratoryEDFSelectedSignal;
      series_neg_peaks.XAxisKey = "DateTime";

      DateTimeAxis xAxis = new DateTimeAxis();
      xAxis.Key = "DateTime";
      xAxis.Minimum = DateTimeAxis.ToDouble(EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile));
      xAxis.Maximum = DateTimeAxis.ToDouble(EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile) + EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 0));
      temp_SignalPlot.Axes.Add(xAxis);

      LinearAxis yAxis = new LinearAxis();
      yAxis.MajorGridlineStyle = LineStyle.Solid;
      yAxis.MinorGridlineStyle = LineStyle.Dot;
      yAxis.Title = RespiratoryEDFSelectedSignal;
      yAxis.Key = RespiratoryEDFSelectedSignal;
      yAxis.Maximum = (max_y ?? Double.NaN) - bias;
      yAxis.Minimum = (min_y ?? Double.NaN) - bias;

      series_onsets.MarkerFill = OxyColor.FromRgb(255, 0, 0);
      series_insets.MarkerFill = OxyColor.FromRgb(0, 255, 0);
      series_pos_peaks.MarkerFill = OxyColor.FromRgb(0, 0, 255);
      series_neg_peaks.MarkerFill = OxyColor.FromRgb(255, 255, 0);

      temp_SignalPlot.Axes.Add(yAxis);
      temp_SignalPlot.Series.Add(series_norm);
      temp_SignalPlot.Series.Add(series_onsets);
      temp_SignalPlot.Series.Add(series_insets);
      temp_SignalPlot.Series.Add(series_pos_peaks);
      temp_SignalPlot.Series.Add(series_neg_peaks);

      RespiratorySignalPlot = temp_SignalPlot;

      // Find Breathing Rate
      List<double> breathing_periods = new List<double>();
      for (int x = 1; x < series_insets.Points.Count; x++)
        breathing_periods.Add((DateTimeAxis.ToDateTime(series_insets.Points[x].X) - DateTimeAxis.ToDateTime(series_insets.Points[x - 1].X)).TotalSeconds);
      for (int x = 1; x < series_onsets.Points.Count; x++)
        breathing_periods.Add((DateTimeAxis.ToDateTime(series_onsets.Points[x].X) - DateTimeAxis.ToDateTime(series_onsets.Points[x - 1].X)).TotalSeconds);
      for (int x = 1; x < series_pos_peaks.Points.Count; x++)
        breathing_periods.Add((DateTimeAxis.ToDateTime(series_pos_peaks.Points[x].X) - DateTimeAxis.ToDateTime(series_pos_peaks.Points[x - 1].X)).TotalSeconds);
      for (int x = 1; x < series_neg_peaks.Points.Count; x++)
        breathing_periods.Add((DateTimeAxis.ToDateTime(series_neg_peaks.Points[x].X) - DateTimeAxis.ToDateTime(series_neg_peaks.Points[x - 1].X)).TotalSeconds);

      breathing_periods.Sort();

      if (breathing_periods.Count > 0)
      {
        RespiratoryBreathingPeriodMean = (breathing_periods.Average()).ToString("0.## sec/breath");
        RespiratoryBreathingPeriodMedian = (breathing_periods[breathing_periods.Count / 2 - 1]).ToString("0.## sec/breath");
      }

    }
    /// <summary>
    /// Background completion function called after performing Respiratory analysis on signals stored in an EDF file 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_FinishRespiratoryAnalysisEDF(object sender, RunWorkerCompletedEventArgs e)
    {
    }
    /// <summary>
    /// Perform respiratory analysis on signals stored in an EDF file
    /// </summary>
    public void PerformRespiratoryAnalysisEDF()
    {
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_RespiratoryAnalysisEDF;
      bw.RunWorkerCompleted += BW_FinishRespiratoryAnalysisEDF;
      bw.RunWorkerAsync();
    }

    //EEG Analysis From EDF File
    private void BW_EEGAnalysisEDF(object sender, DoWorkEventArgs e)
    {

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
    #endregion

    #region SETTINGS

    // Recent File List and Functions
    public ReadOnlyCollection<string> RecentFiles
    {
      get
      {
        string[] value = null;

        if (File.Exists("recent.txt"))
        {
          StreamReader sr = new StreamReader("recent.txt");
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
      List<string> array = RecentFiles.ToArray().ToList();
      array.Insert(0, path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }
    public void RecentFiles_Remove(string path)
    {
      List<string> array = RecentFiles.ToArray().ToList();
      array.Remove(path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }

    // Preview Category Management
    private void LoadCategoriesFile()
    {
      sm.SignalCategories.Clear();
      sm.SignalCategoryContents.Clear();

      if (File.Exists("signal_categories.txt"))
      {
        StreamReader sr = new StreamReader("signal_categories.txt");
        string[] text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        for (int x = 0; x < text.Length; x++)
        {
          string line = text[x];

          string category = line.Split(',')[0].Trim();
          List<string> category_signals = new List<string>();

          for (int y = 0; y < line.Split(',').Length; y++)
          {
            if (EDFAllSignals.Contains(line.Split(',')[y].Trim()) || sm.DerivedSignals.Find(temp => temp[0].Trim() == line.Split(',')[y].Trim()) != null)
            {
              category_signals.Add(line.Split(',')[y]);
            }
          }

          if (category_signals.Count > 0)
          {
            sm.SignalCategories.Add((sm.SignalCategories.Count + 1) + ". " + category);
            sm.SignalCategoryContents.Add(category_signals);
          }
        }

        sr.Close();
      }
    }
    private void WriteToCategoriesFile()
    {
      List<string> temp_SignalCategories = new List<string>();
      List<List<string>> temp_SignalCategoriesContents = new List<List<string>>();

      if (File.Exists("signal_categories.txt"))
      {
        StreamReader sr = new StreamReader("signal_categories.txt");
        string[] text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        for (int x = 0; x < text.Length; x++)
        {
          string line = text[x];

          string category = line.Split(',')[0].Trim();
          List<string> category_signals = new List<string>();

          for (int y = 1; y < line.Split(',').Length; y++)
          {
            category_signals.Add(line.Split(',')[y]);
          }

          if (!sm.SignalCategories.Contains(category))
          {
            for (int y = 0; y < AllSignals.Count; y++)
            {
              if (category_signals.Contains(AllSignals[y]))
                category_signals.Remove(AllSignals[y]);
            }
          }

          temp_SignalCategories.Add(category);
          temp_SignalCategoriesContents.Add(category_signals);
        }

        sr.Close();
      }

      for (int x = 0; x < sm.SignalCategories.Count; x++)
      {
        if (temp_SignalCategories.Contains(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim()))
        {
          int u = temp_SignalCategories.IndexOf(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents[u].AddRange(sm.SignalCategoryContents[x].ToArray());
          temp_SignalCategoriesContents[u] = temp_SignalCategoriesContents[u].Distinct().ToList();
        }
        else
        {
          temp_SignalCategories.Add(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents.Add(sm.SignalCategoryContents[x]);
        }
      }

      StreamWriter sw = new StreamWriter("signal_categories.txt");
      for (int x = 0; x < temp_SignalCategories.Count; x++)
      {
        string line = temp_SignalCategories[x].Trim();
        if (line.Trim() != "")
        {
          for (int y = 0; y < temp_SignalCategoriesContents[x].Count; y++)
            line += "," + temp_SignalCategoriesContents[x][y].Trim();

          sw.WriteLine(line);
        }
      }
      sw.Close();
    }
    public void ManageCategories()
    {
      Dialog_Manage_Categories dlg = new Dialog_Manage_Categories(sm.SignalCategories.ToArray(), sm.SignalCategoryContents.Select(temp => temp.ToArray()).ToArray(), LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray(), sm.DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
      dlg.ShowModalDialogExternally();

      PreviewCurrentCategory = -1;
      sm.SignalCategories = dlg.categories.ToList();
      sm.SignalCategoryContents = dlg.categories_signals;
    }
    public void NextCategory()
    {
      if (PreviewCurrentCategory == sm.SignalCategories.Count - 1)
        PreviewCurrentCategory = -1;
      else
        PreviewCurrentCategory++;
    }
    public void PreviousCategory()
    {
      if (PreviewCurrentCategory == -1)
        PreviewCurrentCategory = sm.SignalCategories.Count - 1;
      else
        PreviewCurrentCategory--;
    }

    // Preview Derivative Management
    private void LoadCommonDerivativesFile()
    {
      sm.DerivedSignals.Clear();
      if (File.Exists("common_derivatives.txt"))
      {
        List<string> text = new StreamReader("common_derivatives.txt").ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        for (int x = 0; x < text.Count; x++)
        {
          string[] new_entry = text[x].Split(',');

          if (new_entry.Length == 3)
          {
            if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[1].Trim()) != null) // Signals Exist
            {
              if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[2].Trim()) != null) // Signals Exist
              {
                if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[0].Trim()) == null) // Unique Name
                {
                  if (sm.DerivedSignals.Where(temp => temp[0].Trim() == new_entry[0].Trim()).ToList().Count == 0) // Unique Name
                  {
                    sm.DerivedSignals.Add(new_entry);
                  }
                }
              }
            }
          }
        }
      }
    }
    private void AddToCommonDerivativesFile(string name, string signal1, string signal2)
    {
      StreamWriter sw = new StreamWriter("common_derivatives.txt", true);
      sw.WriteLine(name + "," + signal1 + "," + signal2);
      sw.Close();
    }
    private void RemoveFromCommonDerivativesFile(List<string[]> signals)
    {
      if (File.Exists("common_derivatives.txt"))
      {
        StreamReader sr = new StreamReader("common_derivatives.txt");
        List<string> text = sr.ReadToEnd().Split('\n').ToList();
        sr.Close();
        for (int x = 0; x < text.Count; x++)
        {
          for (int y = 0; y < signals.Count; y++)
          {
            if (text[x].Split(',').Length != 3 || text[x].Split(',')[0].Trim() == signals[y][0].Trim() && text[x].Split(',')[1].Trim() == signals[y][1].Trim() && text[x].Split(',')[2].Trim() == signals[y][2].Trim())
            {
              text.Remove(text[x]);
              x--;
            }
          }
        }

        StreamWriter sw = new StreamWriter("common_derivatives.txt");
        for (int x = 0; x < text.Count; x++)
        {
          sw.WriteLine(text[x].Trim());
        }
        sw.Close();
      }
    }
    public void AddDerivative()
    {
      Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.Trim()).ToArray(), sm.DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
      dlg.ShowModalDialogExternally();

      if (dlg.DialogResult == true)
      {
        sm.DerivedSignals.Add(new string[] { dlg.SignalName, dlg.Signal1, dlg.Signal2 });
        AddToCommonDerivativesFile(dlg.SignalName, dlg.Signal1, dlg.Signal2);
      }

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }
    public void RemoveDerivative()
    {
      Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(sm.DerivedSignals.ToArray());
      dlg.ShowModalDialogExternally();

      if (dlg.DialogResult == true)
      {
        for (int x = 0; x < dlg.RemovedSignals.Length; x++)
        {
          List<string[]> RemovedDerivatives = sm.DerivedSignals.FindAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim()).ToList();
          sm.DerivedSignals.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
          RemoveFromCommonDerivativesFile(RemovedDerivatives);

          if (pm.PreviewSelectedSignals.Contains(dlg.RemovedSignals[x].Trim()))
          {
            pm.PreviewSelectedSignals.Remove(dlg.RemovedSignals[x].Trim());
          }

          // Remove Potentially Saved Min/Max Values
          sm.SignalsMaxValues.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
          sm.SignalsMinValues.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
        }
      }

      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }

    // Hidden Signal Management
    private void LoadHiddenSignalsFile()
    {
      sm.HiddenSignals.Clear();
      if (File.Exists("hiddensignals.txt"))
      {
        StreamReader sr = new StreamReader("hiddensignals.txt");
        sm.HiddenSignals = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        sm.HiddenSignals = sm.HiddenSignals.Select(temp => temp.Trim()).Where(temp => temp != "").ToList();
        sr.Close();
      }
    }
    private void WriteToHiddenSignalsFile()
    {
      StreamWriter sw = new StreamWriter("hiddensignals.txt");
      for (int x = 0; x < sm.HiddenSignals.Count; x++)
      {
        sw.WriteLine(sm.HiddenSignals[x]);
      }
      sw.Close();
    }
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

      Dialog_Hide_Signals dlg = new Dialog_Hide_Signals(EDFAllSignals.ToArray(), input);
      dlg.ShowModalDialogExternally();

      for (int x = 0; x < dlg.hide_signals_new.Length; x++)
      {
        if (dlg.hide_signals_new[x])
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

      WriteToHiddenSignalsFile();
    }

    // Y Axis Extremes 
    private double? GetMaxSignalValue(string Signal, List<float> values)
    {
      string[] find = sm.SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
        return Double.Parse(find[1]);
      else
      {
        double? value = null;

        int j = 1;
        while (value == null && j < 100)
        {
          j++;
          value = GetPercentileValue(values.ToArray(), 97, j);
        }

        SetMaxSignalValue(Signal, value ?? 0);
        return value;
      }
    }
    private double? GetMinSignalValue(string Signal, List<float> values)
    {
      string[] find = sm.SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
        return Double.Parse(find[1]);
      else
      {
        double? value = null;

        int j = 1;
        while (value == null && j < 100)
        {
          j++;
          value = GetPercentileValue(values.ToArray(), 3, j);
        }

        SetMinSignalValue(Signal, value ?? 0);
        return value;
      }
    }
    private double? GetMaxSignalValue(string Signal, List<float> values1, List<float> values2)
    {
      string[] find = sm.SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
        return Double.Parse(find[1]);
      else
      {
        double? value = null;

        int j = 1;
        while (value == null && j < 100)
        {
          j++;
          value = GetPercentileValueDeriv(values1.ToArray(), values2.ToArray(), 97, j);
        }

        SetMaxSignalValue(Signal, value ?? 0);
        return value;
      }
    }
    private double? GetMinSignalValue(string Signal, List<float> values1, List<float> values2)
    {
      string[] find = sm.SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
        return Double.Parse(find[1]);
      else
      {
        double? value = null;

        int j = 1;
        while (value == null && j < 100)
        {
          j++;
          value = GetPercentileValueDeriv(values1.ToArray(), values2.ToArray(), 3, j);
        }

        SetMinSignalValue(Signal, value ?? 0);
        return value;
      }
    }
    private void SetMaxSignalValue(string Signal, double Value)
    {
      string[] find = sm.SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
      {
        sm.SignalsMaxValues.Remove(find);
      }

      sm.SignalsMaxValues.Add(new string[] { Signal.Trim(), Value.ToString() });
    }
    private void SetMinSignalValue(string Signal, double Value)
    {
      string[] find = sm.SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

      if (find != null)
      {
        sm.SignalsMinValues.Remove(find);
      }

      sm.SignalsMinValues.Add(new string[] { Signal.Trim(), Value.ToString() });
    }

    public void WriteSettings()
    {
      WriteToHiddenSignalsFile();
      WriteToCategoriesFile();
    }
    public void LoadSettings()
    {
      LoadHiddenSignalsFile();
      LoadCommonDerivativesFile();
      LoadCategoriesFile();
      OnPropertyChanged(nameof(PreviewSignals));
      OnPropertyChanged(nameof(AllNonHiddenSignals));
    }

    #endregion

    #region MEMBERS

    // General Private Variables
    private MainWindow p_window;
    private EDFFile p_LoadedEDFFile;
    private string p_LoadedEDFFileName = null;

    // Models
    private SettingsModel sm = new SettingsModel();
    private PreviewModel pm = new PreviewModel();
    private RespiratoryModel rm = new RespiratoryModel();

    //EEG Model
    private EEGModel eegm = new EEGModel();

    #endregion

    #region PROPERTIES 

    // Update Actions
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

        RespiratoryBreathingPeriodMean = "";
        RespiratoryBreathingPeriodMedian = "";
        RespiratorySignalPlot = null;
        RespiratoryEDFSelectedSignal = null;
        RespiratoryEDFDuration = null;
        RespiratoryEDFStartRecord = null;
      }
      else
      {
        PreviewUseAbsoluteTime = false;
        PreviewViewStartTime = LoadedEDFFile.Header.StartDateTime;
        PreviewViewStartRecord = 0;
        PreviewViewDuration = 5;

        RespiratoryBreathingPeriodMean = "";
        RespiratoryBreathingPeriodMedian = "";
        RespiratoryEDFSelectedSignal = null;
        RespiratorySignalPlot = null;
        RespiratoryEDFDuration = 1;
        RespiratoryEDFStartRecord = 0;
      }
      OnPropertyChanged(nameof(PreviewNavigationEnabled));

      // Header
      OnPropertyChanged(nameof(EDFStartTime));
      OnPropertyChanged(nameof(EDFEndTime));
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
    private void RespiratorySignalPlot_Changed()
    {
      p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespPendingChanges.Visibility = Visibility.Hidden; }));
    }
    private void RespiratoryMinimumPeakWidth_Changed()
    {
      p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespPendingChanges.Visibility = Visibility.Visible; }));
    }
    private void RespiratoryRemoveMultiplePeaks_Changed()
    {
      p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespPendingChanges.Visibility = Visibility.Visible; }));
      if (RespiratoryRemoveMultiplePeaks == true)
        p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespAllowMultiplePeaks.Text = "No"; }));
      else
        p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespAllowMultiplePeaks.Text = "Yes"; }));
    }

    // Loaded EDF Structure and File Name
    /// <summary>
    /// The currently loaded EDF file, has a 'Changed Event'
    /// </summary>
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
    /// <summary>
    /// Loaded EDF File Name, bound to the Status Bar Content
    /// </summary>
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
    /// <summary>
    /// False, if LoadedEDFFile = null, bound to the Enabled property of many controls
    /// </summary>
    public bool IsEDFLoaded
    {
      get
      {
        return LoadedEDFFile != null;
      }
    }

    // EDF Header
    /// <summary>
    /// The Time the signal recordings started, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The Time the signal recordings ended, bound to TextBox Text in Preview Tab
    /// </summary>
    public string EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
          return (LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)).ToString();
        else
          return "";
      }
    }
    /// <summary>
    /// The name of the patient the signals were measured from, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The gender of the patient the signals were measured from, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The code of the patient the signals were measured from, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The birthdate of the patient the signals were measured from, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The recording equipment the signals were measured on, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The code of the recording equipment the signals were measured on, bound to TextBox Text in Preview Tab
    /// </summary>
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
    /// <summary>
    /// The name of the technician who recorded the signals, bound to TextBox Text in Preview Tab
    /// </summary>
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

    // Signal Names
    /// <summary>
    /// Every available signal derived or from EDF
    /// </summary>
    public ReadOnlyCollection<string> AllSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          List<string> output = new List<string>();
          output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray());
          output.AddRange(sm.DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
          return Array.AsReadOnly(output.ToArray());
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }
    /// <summary>
    /// Every signal stored in the EDF file
    /// </summary>
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
    /// <summary>
    /// Every available signal derived or from EDF that is not on the hidden list
    /// </summary>
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          List<string> output = new List<string>();
          output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).Where(temp => !sm.HiddenSignals.Contains(temp)).ToArray());
          output.AddRange(sm.DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
          return Array.AsReadOnly(output.ToArray());
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }

    // Preview Signal Selection
    /// <summary>
    /// The currently selected category index, has a 'Changed Event'
    /// </summary>
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
    /// <summary>
    /// The currently selected category, bound to TextBlock in Preview Tab
    /// </summary>
    public string PreviewCurrentCategoryName
    {
      get
      {
        if (PreviewCurrentCategory == -1)
          return "All";
        else
          return sm.SignalCategories[PreviewCurrentCategory];
      }
    }
    /// <summary>
    /// All the non-hidden signals in the currently selected category, bound to the signal selection pane in the Preview Tab
    /// </summary>
    public ReadOnlyCollection<string> PreviewSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewCurrentCategory != -1)
            return Array.AsReadOnly(sm.SignalCategoryContents[PreviewCurrentCategory].Where(temp => !sm.HiddenSignals.Contains(temp)).ToArray());
          else
          {
            List<string> output = new List<string>();
            output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).Where(temp => !sm.HiddenSignals.Contains(temp)).ToArray());
            output.AddRange(sm.DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
            return Array.AsReadOnly(output.ToArray());
          }
        }
        else
        {
          return Array.AsReadOnly(new string[0]);
        }
      }
    }
    /// <summary>
    /// Updates the Selected Signals list and calls the draw chart function
    /// </summary>
    /// <param name="SelectedItems"> The new user selected signals </param>
    public void SetSelectedSignals(System.Collections.IList SelectedItems)
    {
      pm.PreviewSelectedSignals.Clear();
      for (int x = 0; x < SelectedItems.Count; x++)
        pm.PreviewSelectedSignals.Add(SelectedItems[x].ToString());

      DrawChart();
    }

    // Preview Plot Range
    /// <summary>
    /// If true, user input is in Absolute Time, if false, user input is in Epochs, bound to the ToggleButton in the Preview Tab, and the Enabled property of the UpDown controls in the Preview Tab
    /// </summary>
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
    /// <summary>
    /// The current user selected StartTime in Absolute Time, bound to the DateTimeUpDown control in the Preview Tab
    /// </summary>
    public DateTime? PreviewViewStartTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return pm.PreviewViewStartTime;
          else
            return EpochtoDateTime(pm.PreviewViewStartRecord, LoadedEDFFile);
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
          pm.PreviewViewStartTime = value ?? new DateTime();
          pm.PreviewViewStartRecord = DateTimetoEpoch(pm.PreviewViewStartTime, LoadedEDFFile);
          PreviewView_Changed();
        }
      }
    }
    /// <summary>
    /// The current user selected StartTime in Epochs, bound to the NumericUpDown control in the Preview Tab
    /// </summary>
    public int? PreviewViewStartRecord
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return DateTimetoEpoch(PreviewViewStartTime ?? new DateTime(), LoadedEDFFile);
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
          pm.PreviewViewStartRecord = value ?? 0;
          pm.PreviewViewStartTime = EpochtoDateTime(pm.PreviewViewStartRecord, LoadedEDFFile);
          PreviewView_Changed();
        }
      }
    }
    /// <summary>
    /// The current user selected duration in Seconds or Epochs, depending on the value of PreviewUseAbsoluteTime, bound to the NumericUpDown control in the Preview Tab
    /// </summary>
    public int? PreviewViewDuration
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return pm.PreviewViewDuration;
          else
            return TimeSpantoEpochPeriod(new TimeSpan(0, 0, pm.PreviewViewDuration));
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
            pm.PreviewViewDuration = value ?? 0;
          else
            pm.PreviewViewDuration = (int)EpochPeriodtoTimeSpan((value ?? 0)).TotalSeconds;
        }

        PreviewView_Changed();
      }
    }
    /// <summary>
    /// The current user selected EndTime calculated using the start time and duration
    /// </summary>
    public DateTime PreviewViewEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return (PreviewViewStartTime ?? new DateTime()) + new TimeSpan(0, 0, 0, PreviewViewDuration ?? 0);
          else
            return (PreviewViewStartTime ?? new DateTime()) + EpochPeriodtoTimeSpan(PreviewViewDuration ?? 0);
        }
        else
        {
          return new DateTime();
        }
      }
    }

    /// <summary>
    /// The maximum acceptable start time for the DateTimeUpDown control in the Preview Tab
    /// </summary>
    public DateTime PreviewViewStartTimeMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return LoadedEDFFile.Header.StartDateTime // Start Time
              + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds) // Total Duration
              - new TimeSpan(0, 0, pm.PreviewViewDuration); // View Duration
        else
          return new DateTime();
      }
    }
    /// <summary>
    /// The minimum acceptable start time for the DateTimeUpDown control in the Preview Tab
    /// </summary>
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
    /// <summary>
    /// The maximum acceptable start time for the NumericUpDown control in the Preview Tab
    /// </summary>
    public int PreviewViewStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return DateTimetoEpoch(PreviewViewStartTimeMax, LoadedEDFFile); // PreviewViewStartTimeMax to Record
        else
          return 0;
      }
    }
    /// <summary>
    /// The minimum acceptable start time for the NumericUpDown control in the Preview Tab
    /// </summary>
    public int PreviewViewStartRecordMin
    {
      get
      {
        return 0; // Record 0
      }
    }
    /// <summary>
    /// The maximum acceptable start time for the NumericUpDown control in the Preview Tab
    /// </summary>
    public int PreviewViewDurationMax
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
        {
          if (pm.PreviewUseAbsoluteTime)
            return Math.Min(
                2 * 60 * 60,
                (int)((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)) - (PreviewViewStartTime ?? new DateTime())).TotalSeconds
                );
          else
            return Math.Min(
                (int)((2 * 60 * 60) / ((double)EPOCH_SEC)),
                DateTimetoEpoch((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)), LoadedEDFFile) - DateTimetoEpoch((PreviewViewStartTime ?? new DateTime()), LoadedEDFFile)
                );
        }
        else // No File Loaded
          return 0;
      }
    }
    /// <summary>
    /// The minimum acceptable start time for the NumericUpDown control in the Preview Tab
    /// </summary>
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
    /// <summary>
    /// If true, enable the navigation controls, if false disable them, bound to all the navigation controls in the Preview Tab
    /// </summary>
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
      }
    }

    // Preview Plot
    /// <summary>
    /// The plot in the Preview Tab
    /// </summary>
    public PlotModel PreviewSignalPlot
    {
      get
      {
        return pm.PreviewSignalPlot;
      }
      set
      {
        pm.PreviewSignalPlot = value;
        OnPropertyChanged(nameof(PreviewSignalPlot));
      }
    }

    // Respiratory Analysis
    /// <summary>
    /// The user selected respiratory signal, bound to the selection box in the Respiratory Analysis tab
    /// </summary>
    public string RespiratoryEDFSelectedSignal
    {
      get
      {
        return rm.RespiratoryEDFSelectedSignal;
      }
      set
      {
        rm.RespiratoryEDFSelectedSignal = value;
        OnPropertyChanged(nameof(RespiratoryEDFSelectedSignal));
      }
    }
    /// <summary>
    /// The user selected start epoch, bound to the NumericUpDown in the Respiratory Analysis tab
    /// </summary>
    public int? RespiratoryEDFStartRecord
    {
      get
      {
        return rm.RespiratoryEDFStartRecord;
      }
      set
      {
        rm.RespiratoryEDFStartRecord = value ?? 0;
        OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
      }
    }
    /// <summary>
    /// The user selected epoch duration, bound to the NumericUpDown in the Respiratory Analysis tab
    /// </summary>
    public int? RespiratoryEDFDuration
    {
      get
      {
        return rm.RespiratoryEDFDuration;
      }
      set
      {
        rm.RespiratoryEDFDuration = value ?? 0;
        OnPropertyChanged(nameof(RespiratoryEDFDuration));
      }
    }
    /// <summary>
    /// The calculated breathing period mean, bound to the TextBox in the Respiratory Analysis tab
    /// </summary>
    public string RespiratoryBreathingPeriodMean
    {
      get
      {
        return rm.RespiratoryBreathingPeriodMean;
      }
      set
      {
        rm.RespiratoryBreathingPeriodMean = value;
        OnPropertyChanged(nameof(RespiratoryBreathingPeriodMean));
      }
    }
    /// <summary>
    /// The calculated breathing period median, bound to the TextBox in the Respiratory Analysis tab
    /// </summary>
    public string RespiratoryBreathingPeriodMedian
    {
      get
      {
        return rm.RespiratoryBreathingPeriodMedian;
      }
      set
      {
        rm.RespiratoryBreathingPeriodMedian = value;
        OnPropertyChanged(nameof(RespiratoryBreathingPeriodMedian));
      }
    }
    /// <summary>
    /// The user specified peak sensitivity in ms, bound to the NumericUpDown in the Respiratory Analysis tab
    /// </summary>
    public int RespiratoryMinimumPeakWidth
    {
      get
      {
        return rm.RespiratoryMinimumPeakWidth;
      }
      set
      {
        rm.RespiratoryMinimumPeakWidth = value;
        OnPropertyChanged(nameof(RespiratoryMinimumPeakWidth));
        RespiratoryMinimumPeakWidth_Changed();
      }
    }
    /// <summary>
    /// True if the user does not want multiple respiration peaks or expiration peaks simultaniously, bound to the togglebutton in the Respiratory Analysis tab
    /// </summary>
    public bool RespiratoryRemoveMultiplePeaks
    {
      get
      {
        return rm.RespiratoryRemoveMultiplePeaks;
      }
      set
      {
        rm.RespiratoryRemoveMultiplePeaks = value;
        OnPropertyChanged(nameof(RespiratoryRemoveMultiplePeaks));
        RespiratoryRemoveMultiplePeaks_Changed();
      }
    }
    /// <summary>
    /// The plot in the respiratory analysis tab
    /// </summary>
    public PlotModel RespiratorySignalPlot
    {
      get
      {
        return rm.RespiratorySignalPlot;
      }
      set
      {
        rm.RespiratorySignalPlot = value;
        OnPropertyChanged(nameof(RespiratorySignalPlot));
        RespiratorySignalPlot_Changed();
      }
    }

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
    public int? EEGEDFStartRecord
    {
      get
      {
        return eegm.EEGEDFStartRecord;
      }
      set
      {
        eegm.EEGEDFStartRecord = value ?? 0;
        OnPropertyChanged(nameof(EEGEDFStartRecord));
      }
    }
    public int? EEGEDFDuration
    {
      get
      {
        return eegm.EEGEDFDuration;
      }
      set
      {
        eegm.EEGEDFDuration = value ?? 0;
        OnPropertyChanged(nameof(EEGEDFDuration));
      }
    }
    public PlotModel EEGSignalPlot
    {
      get
      {
        return eegm.EEGSignalPlot;
      }
      set
      {
        eegm.EEGSignalPlot = value;
        OnPropertyChanged(nameof(EEGSignalPlot));
        p_window.Dispatcher.Invoke(new Action(() => { p_window.TextBlock_RespPendingChanges.Visibility = Visibility.Hidden; }));
      }
    }
    /****************************/
    #endregion

    #region INotify Interface

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public ModelView(MainWindow i_window)
    {
      p_window = i_window;
    }
  }
}
