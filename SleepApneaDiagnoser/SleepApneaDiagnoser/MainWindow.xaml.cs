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
    PreviewModelView preview_modelview;
    RespiratoryModelView resp_modelview;
    CoherenceModelView cohere_modelview;
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
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      settings_model = new SettingsModel();
      modelview = new ModelView(this, settings_model);
      this.DataContext = modelview;
      this.grid_SettingsMainMenu.DataContext = modelview;
      this.grid_SettingsPersonalization.DataContext = modelview;

      resp_modelview = new RespiratoryModelView(modelview, settings_model);
      this.TabItem_Respiratory.DataContext = resp_modelview;
      this.grid_SettingsRespiratory.DataContext = resp_modelview;

      cohere_modelview = new CoherenceModelView(modelview, settings_model);
      this.TabItem_Coherence.DataContext = cohere_modelview;

      preview_modelview = new PreviewModelView(modelview, settings_model);
      this.TabItem_Preview.DataContext = preview_modelview;

      LoadRecent();
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
      preview_modelview.SetSelectedSignals(listBox_SignalSelect.SelectedItems);
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
      preview_modelview.NextCategory();
    }
    private void button_Prev_Click(object sender, RoutedEventArgs e)
    {
      preview_modelview.PreviousCategory();
    }

    private void button_ExportBinary_Click(object sender, RoutedEventArgs e)
    {
      preview_modelview.ExportSignals();
    }
    private void button_ExportImage_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
      dialog.Filter = "Image File (*.png)|*.png";
      dialog.Title = "Select an EDF file";

      if (dialog.ShowDialog() == true)
      {
        preview_modelview.ExportImage(dialog.FileName);
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
      Thread exportTh = new Thread(() => Utils.ExportImage(pModel, fileName));
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
      PreviewList_Updated();
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

      PreviewList_Updated();
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

      PreviewList_Updated();
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

      PreviewList_Updated();
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

      PreviewList_Updated();
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

      PreviewList_Updated();
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
      // Preview Time Picker
      if (p_LoadedEDFFile == null)
      {
        LoadedEDFFileName = null;

        EEGEDFSelectedSignal = null;
        ExportEpochStart = null;
        ExportEpochEnd = null;
        EpochForAnalysis = null;
      }
      else
      {
        EEGEDFSelectedSignal = null;
        EpochForAnalysis = 1;
        ExportEpochStart = 1;
        ExportEpochEnd = 1;
      }
      
      // Misc
      OnPropertyChanged(nameof(IsEDFLoaded));
      
      // Analysis
      EEGView_Changed();
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
    public DateTime EDFStartTime
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.StartDateTime;
        else
          return new DateTime();
      }
    }
    public DateTime EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          DateTime EndTime = LoadedEDFFile.Header.StartDateTime
                             + new TimeSpan(
                               (long)(TimeSpan.TicksPerSecond * LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)
                               );
          return EndTime;
        }
        else
          return new DateTime();
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

    // Signals
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
          DateTime EndTime = EDFEndTime; // EDF End Time
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

    public event Action PreviewList_Updated;

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
