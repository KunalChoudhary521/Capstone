using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using MathWorks.MATLAB.NET.Arrays;
using EEGBandpower;
using PSD_Welch;
using EEGSpec;
using System.Threading;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Model for variables used exclusively in the 'EEG' sub tab of the 'Analysis' tab
  /// </summary>
  public class EEGModel
  {
    #region Members

    /// <summary>
    /// The user selected signal to perform eeg analysis on
    /// </summary>
    public string EEGEDFSelectedSignal;
    /// <summary>
    /// The user selected Number of Epochs for eeg analysis in 30s epochs
    /// </summary>
    public int EpochForAnalysis;
    /// <summary>
    /// The user selected Number of Epochs for eeg analysis in 30s epochs for binary files
    /// </summary>
    public int EEGBinaryEpochForAnalysis;
    /// <summary>
    /// The user selected start epoch for eeg analysis export in 30s epochs
    /// </summary>
    public int ExportEpochStart;
    /// <summary>
    /// The user selected end epoch for eeg analysis export in 30s epochs
    /// </summary>
    public int ExportEpochEnd;
    /// <summary>
    /// The eeg analysis plot to be displayed
    /// </summary>
    public PlotModel PlotAbsPwr = null;
    /// <summary>
    /// Displays the eeg absolute power plot
    /// </summary>
    public PlotModel PlotRelPwr = null;
    /// <summary>
    /// Displays the eeg relative power plot
    /// </summary>
    public PlotModel PlotSpecGram = null;
    /// <summary>
    /// Displays the eeg spectrogram power plot
    /// </summary>
    public PlotModel PlotPSD = null;
    /// <summary>
    /// Displays the eeg powe spectral density power plot
    /// </summary>
    public String[] EEGExportOptions = null;

    // Freeze UI when Performing Analysis 

    /// <summary>
    /// True if the program is performing analysis and a progress ring should be shown
    /// </summary>
    public bool EEGProgressRingEnabled = false;

    #endregion 
  }

  /// <summary>
  /// Model View for UI logic used exclusively in the 'EEG' sub tab of the 'Analysis' tab
  /// </summary>
  public class EEGModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    private SettingsModelView svm;
    private SettingsModel sm
    {
      get
      {
        return svm.sm;
      }
    }

    // Property Changed Listener
    private void Exterior_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IsEDFLoaded):
          if (!IsEDFLoaded)
          {
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
          EEGView_Changed();
          OnPropertyChanged(nameof(IsEDFLoaded));
          OnPropertyChanged(nameof(EEGEDFNavigationEnabled));
          break;
        default:
          OnPropertyChanged(e.PropertyName);
          break;
      }
    }

    // Shared Properties
    public bool IsEDFLoaded
    {
      get
      {
        return svm.IsEDFLoaded;
      }
    }
    public EDFFile LoadedEDFFile
    {
      get
      {
        return svm.LoadedEDFFile;
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

    public ReadOnlyCollection<string> EDFAllSignals
    {
      get
      {
        return svm.EDFAllSignals;
      }
    }
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        return svm.AllNonHiddenSignals;
      }
    }

    public bool UseDarkTheme
    {
      get
      {
        return sm.UseDarkTheme;
      }
    }

    // Shared Functions
    public LineSeries GetSeriesFromSignalName(out float sample_period, string Signal, DateTime StartTime, DateTime EndTime)
    {
      return svm.GetSeriesFromSignalName(out sample_period, Signal, StartTime, EndTime);
    }

    #endregion

    /// <summary>
    /// EEG Model
    /// </summary>
    private EEGModel eegm = new EEGModel();

    #region Properties

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
        if (EEGEDFSelectedSignal != null && EpochForAnalysis != null)
          PerformEEGAnalysisEDF();
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
        if (EEGEDFSelectedSignal != null && EpochForAnalysis != null)
          PerformEEGAnalysisEDF();
      }
    }

    public int? EEGEpochForAnalysisBinary
    {
      get
      {
        return eegm.EEGBinaryEpochForAnalysis;
      }
      set
      {
        eegm.EEGBinaryEpochForAnalysis = value ?? 1;

        if (IsEEGBinaryLoaded == true)
        {
          DateTime bin_start_time = DateTime.Parse(eeg_bin_date_time_from);
          DateTime curr_date_time = bin_start_time.AddSeconds(30 * (eegm.EEGBinaryEpochForAnalysis - 1));

          float sample_period = 1 / float.Parse(eeg_bin_sample_frequency_s);

          if (curr_date_time < DateTime.Parse(eeg_bin_date_time_to))
          {
            BW_EEGAnalysisBin(eeg_bin_signal_name, eeg_bin_signal_values, bin_start_time, curr_date_time, curr_date_time.AddSeconds(30), sample_period);
          }
          else
          {
            int seconds_diff = (int)(DateTime.Parse(eeg_bin_date_time_to).Subtract(DateTime.Parse(eeg_bin_date_time_from)).TotalSeconds);
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

    /********************************************************** ZABEEH'S BINARY STUFF ****************************************************/

    public int EEGAnalysisBinaryFileLoaded = 0;
    public bool IsEEGBinaryLoaded
    {
      get
      {
        if (EEGAnalysisBinaryFileLoaded == 0)
        {
          return false;
        }
        return true;
      }
    }
    public object EEGBinaryMaxEpoch
    {
      get
      {
        return eeg_bin_max_epochs.ToString();
      }
      set
      {
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

    // Freeze UI when Performing Analysis 
    public bool EEGProgressRingEnabled
    {
      get
      {
        return eegm.EEGProgressRingEnabled;
      }
      set
      {
        eegm.EEGProgressRingEnabled = value;
        OnPropertyChanged(nameof(EEGProgressRingEnabled));
        OnPropertyChanged(nameof(EEGEDFNavigationEnabled));
      }
    }
    public bool EEGEDFNavigationEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return !EEGProgressRingEnabled;
      }
    }

    #endregion

    #region Actions
    
    //EEG Analysis From Binary File
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

      BW_EEGCalculations(Signal, series, sample_period);      

      return;//for debugging only
    }

    private void BW_EEGCalculations(string Signal, LineSeries series, float sample_period)
    {
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
      double[] specTime; double[] specFrq;
      double[,] specMatrixtranspose;
      SpecGramAnalysis(out specTime, out specFrq, out specMatrixtranspose, mLabsignalSeries, sampleFreq);



      //order of bands MUST match the order of bands in fqRange array (see above)
      String[] freqBandName = new String[] { "delta", "theta", "alpha", "beta1", "beta2", "gamma1", "gamma2" };

      /*****************************Plotting absolute power graph***************************/
      PlotAbsolutePower(absPlotbandItems, freqBandName);

      /*************************************Plotting relative power graph****************************/
      PlotRelativePower(plotbandItems, freqBandName);

      /*************************Plotting Power Spectral Density *********************/
      PlotPowerSpectralDensity(psdValues, frqValues);

      /********************Plotting a heatmap for spectrogram (line 820, 2133 - PSG_viewer_v7.m)*********************/
      PlotSpectrogram(specTime, specFrq, specMatrixtranspose);


      return;//for debugging only
    }

    private void BW_FinishEEGAnalysisBin(object sender, RunWorkerCompletedEventArgs e)
    {
      EEGProgressRingEnabled = false;
    }
    public void PerformEEGAnalysisBinary()
    {
      EEGProgressRingEnabled = true;

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
        BW_FinishEEGAnalysisBin(null, null);
      }
      else
      {
      }
    }

    //EEG Analysis From EDF File
    /* Note:
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

      if (series.Points.Count == 0)
      {
        //need to type error message for User
        return;
      }

      BW_EEGCalculations(EEGEDFSelectedSignal, series, sample_period);

      // for debugging purposes only
      return;
    }
    private void BW_FinishEEGAnalysisEDF(object sender, RunWorkerCompletedEventArgs e)
    {
      EEGProgressRingEnabled = false;
    }
    public void PerformEEGAnalysisEDF()
    {
      EEGProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_EEGAnalysisEDF;
      bw.RunWorkerCompleted += BW_FinishEEGAnalysisEDF;
      bw.RunWorkerAsync();
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
    public void SpecGramAnalysis(out double[] specTime, out double[] specFrq, out double[,] specMatTPose, 
                                MWNumericArray signalArray, MWNumericArray sampleFreq)
    {
      Spectrogram computeForspec = new Spectrogram();
      MWArray[] mLabSpec = null;
      mLabSpec = computeForspec.eeg_specgram(3, signalArray, sampleFreq);//[colorData,f,t]
      MWNumericArray tempSpec = (MWNumericArray)mLabSpec[0];//already multiplied by 10*log10()
      MWNumericArray tempFrq = (MWNumericArray)mLabSpec[1];
      MWNumericArray tempTime = (MWNumericArray)mLabSpec[2];

      //# of rows = mLabSpec[0].Dimensions[0]
      double[,] specMatrix = new double[mLabSpec[0].Dimensions[0], mLabSpec[0].Dimensions[1]];
      specTime = new double[tempTime.NumberOfElements];
      specFrq = new double[tempFrq.NumberOfElements];
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

      specMatTPose = new double[mLabSpec[0].Dimensions[1], mLabSpec[0].Dimensions[0]];
      for (int j = 0; j < mLabSpec[0].Dimensions[1]; j++)//need to combine this loop with the loop above 
      {
        for (int i = 0; i < mLabSpec[0].Dimensions[0]; i++)
        {
          specMatTPose[j, i] = specMatrix[i, j];
        }
      }

      for (int i = 1; i <= specTime.Length; i++)
      {
        specTime[i - 1] = (double)tempTime[i];
      }
      for (int i = 1; i <= specFrq.Length; i++)
      {
        specFrq[i - 1] = (double)tempFrq[i];
      }
      return;//for debugging

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
      double maxY = 0.0, minY = 0.0;
      for(int i = 0; i < bandItems.Length; i++)
      {
        if(bandItems[i].Value > maxY)//y axis does not cutoff immediately when highest bar ends off
        {
          maxY = bandItems[i].Value;
        }
        if (bandItems[i].Value < minY)//y axis does not cutoff immediately when lowest bar ends off
        {
          minY = bandItems[i].Value;
        }
      }

      LinearAxis absYAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Power (db)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8, Maximum = maxY * 1.3, Minimum = minY * 1.3 };
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
      tempPSD.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Title = "Power (dB)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, Maximum = psdVal.Max() * 1.2 });
      tempPSD.Axes.Add(new LinearAxis() { Position = AxisPosition.Bottom, Title = "Frequency (Hz)", TitleFontSize = 14, TitleFontWeight = OxyPlot.FontWeights.Bold, AxisTitleDistance = 8, Maximum = frqVal.Max() * 1.02 });

      PlotPSD = tempPSD;
    }
    public void PlotSpectrogram(double[] specTime, double[] specFrq, double[,] specMatTPose)
    {
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
      HeatMapSeries specGram = new HeatMapSeries() { X0 = minTime, X1 = maxTime, Y0 = minFreq, Y1 = maxFreq, Data = specMatTPose};
      tempSpectGram.Series.Add(specGram);

      PlotSpecGram = tempSpectGram;
    }
    /*************************Exporting to .png format**************************/
    public void ExportEEGPlots()//add signal title in the analysisDir name
    {
      String plotsDir = null;
      try
      {
        plotsDir = ChooseDirectory();
        if (plotsDir == null)
        {
          return;//user did not choose a valid directory
        }
        else
        {
          plotsDir += "EEGPlots\\" + EEGEDFSelectedSignal.ToString() + "-"
                           + ((int)EpochForAnalysis).ToString() + "\\";
        }
      }
      catch (Exception ex)
      {
        return;//user did not choose a signal
      }     

      /*  If any of the plots is null, then EEG calculation was unsuccessful.
       *  In that event, plots are not exported.
      */
      if ((PlotAbsPwr == null) || (PlotRelPwr == null)
          || (PlotPSD == null) || (PlotSpecGram == null))
      {
        return;
      }
      Directory.CreateDirectory(plotsDir);//if directory already exist, this line will be ignored
      ExportEEGPlot(PlotAbsPwr, plotsDir + "AbsPower.png");
      ExportEEGPlot(PlotRelPwr, plotsDir + "RelPower.png");
      ExportEEGPlot(PlotPSD, plotsDir + "PSD.png");
      ExportEEGPlot(PlotSpecGram, plotsDir + "Spectrogram.png");
    }

    public void ExportEEGCalculations()//add signal title in the analysisDir name
    {
      if (ExportEpochEnd < ExportEpochStart)//restrict ExportEpochEnd to the max size of signal
      {
        return;
      }
      String fromToDir = EEGEDFSelectedSignal.ToString()+ "-" + ExportEpochStart.ToString() + "-" + ExportEpochEnd.ToString();
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

      double[] specTime;
      double[] specFrq;
      double[,] specMatrix;

      const int totalFreqBands = 7;
      MWNumericArray[] fqRange;
      setFreqBands(out fqRange, totalFreqBands);

      LineSeries signalPerEpoch = null;

      //Setup data to be entered in each file
      String analysisDir = ChooseDirectory();
      if(analysisDir == null)
      {
        //Do not export calculations if user did not select a valid directory
        return;
      }
      else
      {
        analysisDir += "EEGAnalysis\\" + fromToDir + "\\";
      }
      
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

      fileSetup = new StreamWriter(analysisDir + "Spectrogram.csv");
      fileSetup.WriteLine(String.Format("Epoch#,Frequency(Hz), , , ,Time(s)"));
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
        //output PSD calculations to a file
        PSDToCSV(psdValue, frqValue, i, analysisDir + "EEGPSD.csv");

        //perform Spectrogram calculations
        SpecGramAnalysis(out specTime, out specFrq, out specMatrix, signalToMatlab, sampleFreq);
        //output Spectrogram calculations to a file
        SpecGramToCSV(specTime, specFrq, specMatrix, i, analysisDir + "Spectrogram.csv");
      }

      return;//for debugging only
    }
    public String ChooseDirectory()
    {
      String userDir = null;
      Thread exportTh = new Thread(() => { userDir = ChooseDirectoryHelper(); });
      exportTh.SetApartmentState(ApartmentState.STA);
      exportTh.Start();
      exportTh.Join();

      return userDir;
    }
    //Allows user to choose the location where they would like to export Calculations or Plots
    //Dialog box in WPF: WpfDialog.txt
    public String ChooseDirectoryHelper()
    {
      String directory = null;

      using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
      {
        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
        directory = dialog.SelectedPath;
      }
      if(String.IsNullOrEmpty(directory))
      {
        return null;
      }
      else
      {
        return (directory + "\\");
      }
      
    }
    public void ExportEEGPlot(PlotModel pModel, String fileName)
    {      
      Thread exportTh = new Thread(() => Utils.ExportImage(pModel, fileName));
      exportTh.SetApartmentState(ApartmentState.STA);
      exportTh.Start();
      exportTh.Join();
    }

    /*  Export Calculations to .csv format (this can be opened in MS Excel)
     */
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
        psdStream.WriteLine(String.Format("{0},{1:0.000},{2:0.000}", epoch, psdVal[j], frqVal[j]));
      }
      psdStream.Close();
    }
    //Specgram values are written to file longitudinally, where as Spectrogram graph is latitudinally
    public void SpecGramToCSV(double[] specTime, double[] specFrq, double[,] specMatrix,
                              int epoch, String fileName)
    {
      StreamWriter specStream = File.AppendText(fileName);
      String dataLine = null;
      specStream.Write(epoch.ToString());

      dataLine += String.Format(",");//skip 2 cells for (epoch#, times)
      for (int i = 0; i < specTime.Length; i++)
      {
        dataLine += String.Format(",{0:0.000}", specTime[i]);
      }

      specStream.WriteLine(dataLine);


      for (int i = 0; i < specFrq.Length; i++)
      {
        dataLine = String.Format(",{0:0.000}", specFrq[i]);//skip 1 cell for epoch

        for (int j = 0; j < specMatrix.GetLength(0); j++)
        {
          dataLine += String.Format(",{0:0.000}", specMatrix[j,i]);
        }

        specStream.WriteLine(dataLine);
      }      

      specStream.WriteLine();//to differentiate between spctrogram values of 2 or more epochs
      specStream.Close();
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
    
    #endregion

    #region etc

    // INotify Interface
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public EEGModelView(SettingsModelView i_svm)
    {
      svm = i_svm;
      svm.PropertyChanged += Exterior_PropertyChanged;
    }
  }
}
