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

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Factory containing business logic used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryFactory
  {
    #region Static Functions 
    public static LineSeries RemoveBiasFromSignal(LineSeries series, double bias)
    {
      // Normalization
      LineSeries series_norm = new LineSeries();
      for (int x = 0; x < series.Points.Count; x++)
      {
        series_norm.Points.Add(new DataPoint(series.Points[x].X, series.Points[x].Y - bias));
      }

      return series_norm;
    }
    public static Tuple<ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries> GetPeaksAndOnsets(LineSeries series, bool RemoveMultiplePeaks, int min_spike_length)
    {
      int spike_length = 0;
      int maxima = 0;
      int start = 0;
      bool? positive = null;
      ScatterSeries series_pos_peaks = new ScatterSeries();
      ScatterSeries series_neg_peaks = new ScatterSeries();
      ScatterSeries series_insets = new ScatterSeries();
      ScatterSeries series_onsets = new ScatterSeries();
      for (int x = 0; x < series.Points.Count; x++)
      {
        // If positive spike
        if (positive != false)
        {
          // If end of positive spike
          if (series.Points[x].Y < 0 || x == series.Points.Count - 1)
          {
            // If spike is appropriate length
            if (spike_length > min_spike_length)
            {
              if (
                  // If user does not mind consequent peaks of same sign
                  !RemoveMultiplePeaks ||
                  // If first positive peak
                  series_pos_peaks.Points.Count == 0 ||
                  // If last peak was negative
                  (series_neg_peaks.Points.Count != 0 &&
                  DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) >
                  DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))
                 )
              {
                // Add new positive peak and onset 
                series_pos_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                series_onsets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
              }
              else
              {
                // If this peak is greater than the previous
                if (series.Points[maxima].Y > series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].Y)
                {
                  // Replace previous spike maxima with latest spike maxima
                  series_pos_peaks.Points.Remove(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1]);
                  series_onsets.Points.Remove(series_onsets.Points[series_onsets.Points.Count - 1]);
                  series_pos_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                  series_onsets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
                }
              }
            }

            // Initialization for analyzing negative peak
            positive = false;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          // If middle of positive spike
          else
          {
            if (Math.Abs(series.Points[x].Y) > Math.Abs(series.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
        // If negative spike
        else
        {
          // If end of negative spike
          if (series.Points[x].Y > 0 || x == series.Points.Count - 1)
          {
            // If spike is appropriate length
            if (spike_length > min_spike_length)
            {
              if (
                  // If user does not mind consequent peaks of same sign
                  !RemoveMultiplePeaks ||
                  // If first negative peak
                  series_neg_peaks.Points.Count == 0 ||
                  // If last peak was positive 
                  (series_pos_peaks.Points.Count != 0 &&
                  DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) <
                  DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))
                )
              {
                // Add new negative peak and onset 
                series_neg_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                series_insets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
              }
              else
              {
                // If this peak is less than the previous
                if (series.Points[maxima].Y < series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].Y)
                {
                  // Replace previous spike maxima with latest spike maxima
                  series_neg_peaks.Points.Remove(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1]);
                  series_insets.Points.Remove(series_insets.Points[series_insets.Points.Count - 1]);
                  series_neg_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                  series_insets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
                }
              }
            }

            // Initialization for analyzing positive peak
            positive = true;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          // If middle of negative spike
          else
          {
            if (Math.Abs(series.Points[x].Y) > Math.Abs(series.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
      }

      return new Tuple<ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries>(series_insets, series_onsets, series_neg_peaks, series_pos_peaks);
    }
    public static Tuple<LineSeries, ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries, DateTimeAxis, LinearAxis> GetRespiratoryAnalysisPlot(string SignalName, List<float> yValues, float sample_period, float bias, bool RemoveMultiplePeaks, float MinimumPeakWidth, DateTime ViewStartTime, DateTime ViewEndTime)
    {
      // Variable To Return
      LineSeries series = new LineSeries();

      //  // Add Points to Series
      for (int y = 0; y < yValues.Count; y++)
      {
        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(ViewStartTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), yValues[y]));
      }

      LineSeries series_norm = RemoveBiasFromSignal(series, bias);

      // Find Peaks and Zero Crossings
      int min_spike_length = (int)((double)((double)MinimumPeakWidth / (double)1000) / (double)sample_period);
      Tuple<ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries> output = GetPeaksAndOnsets(series_norm, RemoveMultiplePeaks, min_spike_length);
      ScatterSeries series_insets = output.Item1;
      ScatterSeries series_onsets = output.Item2;
      ScatterSeries series_neg_peaks = output.Item3;
      ScatterSeries series_pos_peaks = output.Item4;

      // Modify Series colors
      series_onsets.MarkerFill = OxyColor.FromRgb(255, 0, 0);
      series_insets.MarkerFill = OxyColor.FromRgb(0, 255, 0);
      series_pos_peaks.MarkerFill = OxyColor.FromRgb(0, 0, 255);
      series_neg_peaks.MarkerFill = OxyColor.FromRgb(255, 255, 0);

      // Bind to Axes
      series_norm.YAxisKey = SignalName;
      series_norm.XAxisKey = "DateTime";
      series_onsets.YAxisKey = SignalName;
      series_onsets.XAxisKey = "DateTime";
      series_insets.YAxisKey = SignalName;
      series_insets.XAxisKey = "DateTime";
      series_pos_peaks.YAxisKey = SignalName;
      series_pos_peaks.XAxisKey = "DateTime";
      series_neg_peaks.YAxisKey = SignalName;
      series_neg_peaks.XAxisKey = "DateTime";

      // Configure Axes
      DateTimeAxis xAxis = new DateTimeAxis();
      xAxis.Key = "DateTime";
      xAxis.Minimum = DateTimeAxis.ToDouble(ViewStartTime);
      xAxis.Maximum = DateTimeAxis.ToDouble(ViewEndTime);

      LinearAxis yAxis = new LinearAxis();
      yAxis.MajorGridlineStyle = LineStyle.Solid;
      yAxis.MinorGridlineStyle = LineStyle.Dot;
      yAxis.Title = SignalName;
      yAxis.Key = SignalName;

      return new Tuple<LineSeries, ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries, DateTimeAxis, LinearAxis>(series_norm, series_insets, series_onsets, series_neg_peaks, series_pos_peaks, xAxis, yAxis);
    }
    public static Tuple<double, double> GetRespiratorySignalBreathingPeriod(ScatterSeries[] series)
    {
      // Find Breathing Rate
      List<double> breathing_periods = new List<double>();
      for (int x = 0; x < series.Length; x++)
      {
        for (int y = 1; y < series[x].Points.Count; y++)
          breathing_periods.Add((DateTimeAxis.ToDateTime(series[x].Points[y].X) - DateTimeAxis.ToDateTime(series[x].Points[y - 1].X)).TotalSeconds);
      }
      breathing_periods.Sort();

      if (breathing_periods.Count != 0)
        return new Tuple<double, double>(breathing_periods.Average(), breathing_periods[breathing_periods.Count / 2 - 1]);
      else
        return new Tuple<double, double>(0, 0);
    }
    #endregion
  }

  /// <summary>
  /// Model for variables used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryModel
  {
    #region Members

    /// <summary>
    /// The user selected signal to perform respiratory analysis on
    /// </summary>
    public string RespiratoryEDFSelectedSignal;
    /// <summary>
    /// The user selected start time for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryEDFStartRecord;
    /// <summary>
    /// The user selected period for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryEDFDuration;
    /// <summary>
    /// The respiratory analysis plot to be displayed
    /// </summary>
    public PlotModel RespiratorySignalPlot = null;
    /// <summary>
    /// The calculated mean average of the periods of the respiratory signal
    /// </summary>
    public string RespiratoryBreathingPeriodMean;
    /// <summary>
    /// The calculated median average of the periods of the respiratory signal
    /// </summary>
    public string RespiratoryBreathingPeriodMedian;
    /// <summary>
    /// A user selected option for setting the sensitivity of the peak detection of the analysis
    /// Effect where the insets, onsets, and peaks are detected
    /// Any "spike" that is less wide than the user setting in ms will be ignored
    /// </summary>
    public int RespiratoryMinimumPeakWidth = 500;
    /// <summary>
    /// A user selected option for choosing whether the analysis will allow for repeated peaks of
    /// the same polarity
    /// </summary>
    public bool RespiratoryRemoveMultiplePeaks = true;
    /// <summary>
    /// If true, use a constant axis
    /// If false, auto adjust to plot
    /// </summary>
    public bool RespiratoryUseConstantAxis = false;
    internal int? RespiratoryBinaryStart;
    internal int? RespiratoryBinaryDuration;

    public bool RespiratoryProgressRingEnabled = false;

    #endregion
  }

  /// <summary>
  /// ModelView containing UI logic used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    private CommonModelView common_data;

    // Property Changed Listener
    private void Exterior_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IsEDFLoaded):
          if (!IsEDFLoaded)
          {
            RespiratoryBreathingPeriodMean = "";
            RespiratoryBreathingPeriodMedian = "";
            RespiratorySignalPlot = null;
            RespiratoryEDFSelectedSignal = null;
            RespiratoryEDFDuration = null;
            RespiratoryEDFStartRecord = null;
          }
          else
          {
            RespiratoryBreathingPeriodMean = "";
            RespiratoryBreathingPeriodMedian = "";
            RespiratoryEDFSelectedSignal = null;
            RespiratorySignalPlot = null;
            RespiratoryEDFStartRecord = 1;
            RespiratoryEDFDuration = 1;
          }
          RespiratoryEDFView_Changed();
          OnPropertyChanged(nameof(RespiratoryEDFNavigationEnabled));
          OnPropertyChanged(nameof(IsEDFLoaded));
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
        return common_data.IsEDFLoaded;
      }
    }
    public EDFFile LoadedEDFFile
    {
      get
      {
        return common_data.LoadedEDFFile;
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
        return common_data.EDFAllSignals;
      }
    }
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        return common_data.AllNonHiddenSignals;
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
      return common_data.GetSeriesFromSignalName(out sample_period, Signal, StartTime, EndTime);
    }
    
    #endregion
    
    /// <summary>
    /// Respiratory Model
    /// </summary>
    private RespiratoryModel rm = new RespiratoryModel();
    private SettingsModel sm;
    
    #region Properties

    private void RepiratoryPlot_Changed()
    {
      RespiratoryProgressRingEnabled = false;
    }
    private void RespiratoryEDFView_Changed()
    {
      OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
      OnPropertyChanged(nameof(RespiratoryEDFStartTime));
      OnPropertyChanged(nameof(RespiratoryEDFDuration));

      OnPropertyChanged(nameof(RespiratoryEDFStartRecordMax));
      OnPropertyChanged(nameof(RespiratoryEDFStartRecordMin));
      OnPropertyChanged(nameof(RespiratoryEDFDurationMax));
      OnPropertyChanged(nameof(RespiratoryEDFDurationMin));
    }
    private void RespiratoryBinaryView_Changed()
    {
      OnPropertyChanged(nameof(RespiratoryBinaryStart));
      OnPropertyChanged(nameof(RespiratoryBinaryDuration));

      OnPropertyChanged(nameof(RespiratoryBinaryStartRecordMax));
      OnPropertyChanged(nameof(RespiratoryBinaryDurationMax));
    }

    // FlyOut
    public bool SettingsRespiratoryVisible
    {
      get
      {
        return sm.SettingsRespiratoryVisible;
      }
      set
      {
        sm.SettingsRespiratoryVisible = value;
        OnPropertyChanged(nameof(SettingsRespiratoryVisible));
      }
    }
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
      }
    }
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
      }
    }

    public bool RespiratoryUseConstantAxis
    {
      get
      {
        return rm.RespiratoryUseConstantAxis;
      }
      set
      {
        rm.RespiratoryUseConstantAxis = value;
        OnPropertyChanged(nameof(RespiratoryUseConstantAxis));
        PerformRespiratoryAnalysisEDF();
      }
    }

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
        PerformRespiratoryAnalysisEDF();
      }
    }
    public int? RespiratoryEDFStartRecord
    {
      get
      {
        if (IsEDFLoaded)
          return rm.RespiratoryEDFStartRecord;
        else
          return null;
      }
      set
      {
        if (IsEDFLoaded && rm.RespiratoryEDFStartRecord != (value ?? 1))
        {
          rm.RespiratoryEDFStartRecord = value ?? 1;
          OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
          RespiratoryEDFView_Changed();
          PerformRespiratoryAnalysisEDF();
        }
      }
    }
    public int? RespiratoryEDFDuration
    {
      get
      {
        if (IsEDFLoaded)
          return rm.RespiratoryEDFDuration;
        else
          return null;
      }
      set
      {
        if (IsEDFLoaded && rm.RespiratoryEDFDuration != (value ?? 1))
        {
          rm.RespiratoryEDFDuration = value ?? 1;
          OnPropertyChanged(nameof(RespiratoryEDFDuration));
          RespiratoryEDFView_Changed();
          PerformRespiratoryAnalysisEDF();
        }
      }
    }

    public int? RespiratoryBinaryStart
    {
      get
      {
        return rm.RespiratoryBinaryStart;
      }
      set
      {
        if (IsRespBinLoaded && rm.RespiratoryBinaryStart != (value ?? 1))
        {
          rm.RespiratoryBinaryStart = value ?? 1;
          OnPropertyChanged(nameof(RespiratoryBinaryStart));
          RespiratoryBinaryView_Changed();
          PerformRespiratoryAnalysisBinary();
        }
      }
    }
    public int? RespiratoryBinaryDuration
    {
      get
      {
        return rm.RespiratoryBinaryDuration;
      }
      set
      {
        if (IsRespBinLoaded && rm.RespiratoryBinaryDuration != (value ?? 1))
        {
          rm.RespiratoryBinaryDuration = value ?? 1;
          OnPropertyChanged(nameof(RespiratoryBinaryDuration));
          RespiratoryBinaryView_Changed();
          PerformRespiratoryAnalysisBinary();
        }
      }
    }

    public PlotModel RespiratorySignalPlot
    {
      get
      {
        return rm.RespiratorySignalPlot;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        rm.RespiratorySignalPlot = value;
        OnPropertyChanged(nameof(RespiratorySignalPlot));
        RepiratoryPlot_Changed();
      }
    }
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

    public DateTime RespiratoryEDFStartTime
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile);
        else
          return new DateTime();
      }
    }
    public DateTime RespiratoryEDFStartTimeMax
    {
      get
      {
        if (IsEDFLoaded)
        {
          DateTime EndTime = EDFEndTime; // EDF End Time
          TimeSpan duration = Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1); // User Selected Duration 
          return EndTime - duration;
        }
        else
          return new DateTime();
      }
    }
    public DateTime RespiratoryEDFStartTimeMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return LoadedEDFFile.Header.StartDateTime; // Start Time
        else
          return new DateTime();
      }
    }
    public int RespiratoryEDFStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(RespiratoryEDFStartTimeMax, LoadedEDFFile); // RespiratoryViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int RespiratoryEDFStartRecordMin
    {
      get
      {
        if (LoadedEDFFile != null)
          return Utils.DateTimetoEpoch(RespiratoryEDFStartTimeMin, LoadedEDFFile); // RespiratoryViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int RespiratoryEDFDurationMax
    {
      get
      {
        if (IsEDFLoaded) // File Loaded
        {
          DateTime EndTime = EDFEndTime; // EDF End Time
          TimeSpan duration = EndTime - (RespiratoryEDFStartTime); // Theoretical Limit Duration
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
    public int RespiratoryEDFDurationMin
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
          return 1;
        else // No File Loaded
          return 0;
      }
    }

    public int RespiratoryBinaryStartRecordMax
    {
      get
      {
        return 1 + resp_bin_max_epoch - RespiratoryBinaryDuration ?? 1;
      }
    }
    public int RespiratoryBinaryDurationMax
    {
      get
      {
        return 1 + resp_bin_max_epoch - RespiratoryBinaryStart ?? 1;
      }
    }

    public bool RespiratoryProgressRingEnabled
    {
      get
      {
        return rm.RespiratoryProgressRingEnabled;
      }
      set
      {
        rm.RespiratoryProgressRingEnabled = value;
        OnPropertyChanged(nameof(RespiratoryProgressRingEnabled));
        OnPropertyChanged(nameof(RespiratoryEDFNavigationEnabled));
      }
    }
    public bool RespiratoryEDFNavigationEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return !RespiratoryProgressRingEnabled;
      }
    }

    public int RespiratoryAnalysisBinaryFileLoaded = 0;
    public int RespiratoryBinaryMaxEpochs
    {
      get
      {
        return resp_bin_max_epoch;
      }
    }
    public bool IsRespBinLoaded
    {
      get
      {
        if (RespiratoryAnalysisBinaryFileLoaded == 1)
        {
          return true;
        }
        return false;
      }
    }

    // Used For Importing From Binary For Respiratory Signals
    private string resp_bin_sample_frequency_s;
    private string resp_bin_date_time_length;
    private string resp_bin_date_time_from;
    private string resp_bin_subject_id;
    private string resp_bin_signal_name;
    private float resp_bin_sample_period;
    private List<float> resp_signal_values;
    private int resp_bin_max_epoch;

    #endregion

    #region Actions

    /// <summary>
    /// Respiratory Analysis From Binary FIle
    /// </summary>
    public void LoadRespiratoryAnalysisBinary()
    {
      this.RespiratoryAnalysisBinaryFileLoaded = 1;
      OnPropertyChanged(nameof(IsRespBinLoaded));
      System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();

      dialog.Filter = "|*.bin";

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        // select the binary file
        FileStream bin_file = new FileStream(dialog.FileName, FileMode.Open);
        BinaryReader reader = new BinaryReader(bin_file);

        byte[] value = new byte[4];
        bool didReachEnd = false;
        this.resp_signal_values = new List<float>();
        // read the whole binary file and build the signal values
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
          try
          {
            value = reader.ReadBytes(4);
            float myFloat = System.BitConverter.ToSingle(value, 0);
            resp_signal_values.Add(myFloat);
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
        this.resp_bin_signal_name = file_reader.ReadLine();
        this.resp_bin_subject_id = file_reader.ReadLine();
        this.resp_bin_date_time_from = file_reader.ReadLine();
        this.resp_bin_date_time_length = file_reader.ReadLine();
        this.resp_bin_sample_frequency_s = file_reader.ReadLine();

        bin_file.Close();

        this.resp_bin_sample_period = 1 / float.Parse(resp_bin_sample_frequency_s);

        DateTime epochs_from_datetime = DateTime.Parse(resp_bin_date_time_from);
        DateTime epochs_to_datetime = DateTime.Parse(resp_bin_date_time_length);

        resp_bin_max_epoch = (int)epochs_to_datetime.Subtract(epochs_from_datetime).TotalSeconds / 30;
        OnPropertyChanged(nameof(RespiratoryBinaryMaxEpochs));
        rm.RespiratoryBinaryStart = 1;
        OnPropertyChanged(nameof(RespiratoryBinaryStart));
        rm.RespiratoryBinaryDuration = 1;
        OnPropertyChanged(nameof(RespiratoryBinaryDuration));
        OnPropertyChanged(nameof(RespiratoryBinaryDurationMax));
        OnPropertyChanged(nameof(RespiratoryBinaryStartRecordMax));

        PerformRespiratoryAnalysisBinary();
      }
      else
      {
      }
    }
    public void PerformRespiratoryAnalysisBinary()
    {
      RespiratoryProgressRingEnabled = true;

      // Finding From 
      int modelStartRecord = RespiratoryBinaryStart.Value;
      DateTime newFrom = DateTime.Parse(resp_bin_date_time_from);
      newFrom = newFrom.AddSeconds(30 * (modelStartRecord - 1));

      // Finding To 
      int modelLength = rm.RespiratoryBinaryDuration.Value;
      DateTime newTo = newFrom;
      newTo = newTo.AddSeconds(30 * (modelLength));

      if (newFrom < DateTime.Parse(resp_bin_date_time_from))
        newFrom = DateTime.Parse(resp_bin_date_time_from);
      if (newTo < newFrom)
        newTo = newFrom;

      int start_index = (int)(((double)(newFrom - DateTime.Parse(resp_bin_date_time_from)).TotalSeconds) / ((double)resp_bin_sample_period));
      int end_index = (int)(((double)(newTo - DateTime.Parse(resp_bin_date_time_from)).TotalSeconds) / ((double)resp_bin_sample_period));
      start_index = Math.Max(start_index, 0);
      end_index = Math.Min(end_index, resp_signal_values.Count - 1);

      PlotModel tempPlotModel = new PlotModel();
      Tuple<LineSeries, ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries, DateTimeAxis, LinearAxis> resp_plots = RespiratoryFactory.GetRespiratoryAnalysisPlot(
        resp_bin_signal_name,
        resp_signal_values.GetRange(start_index, end_index - start_index + 1),
        resp_bin_sample_period,
        resp_signal_values.Average(),
        RespiratoryRemoveMultiplePeaks,
        RespiratoryMinimumPeakWidth,
        newFrom,
        newTo
      );
      tempPlotModel.Series.Add(resp_plots.Item1);
      tempPlotModel.Series.Add(resp_plots.Item2);
      tempPlotModel.Series.Add(resp_plots.Item3);
      tempPlotModel.Series.Add(resp_plots.Item4);
      tempPlotModel.Series.Add(resp_plots.Item5);
      tempPlotModel.Axes.Add(resp_plots.Item6);
      tempPlotModel.Axes.Add(resp_plots.Item7);
      RespiratorySignalPlot = tempPlotModel;

      Tuple<double, double> breathing_periods = RespiratoryFactory.GetRespiratorySignalBreathingPeriod(new ScatterSeries[] { resp_plots.Item2, resp_plots.Item3, resp_plots.Item4, resp_plots.Item5 });
      RespiratoryBreathingPeriodMean = breathing_periods.Item1.ToString("0.## sec/breath");
      RespiratoryBreathingPeriodMedian = breathing_periods.Item2.ToString("0.## sec/breath");
    }

    // Respiratory Analysis From EDF File
    /// <summary>
    /// Background process for performing respiratory analysis
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_RespiratoryAnalysisEDF(object sender, DoWorkEventArgs e)
    {
      PlotModel temp_SignalPlot = new PlotModel();

      temp_SignalPlot.Series.Clear();
      temp_SignalPlot.Axes.Clear();

      float sample_period;
      LineSeries series = GetSeriesFromSignalName(out sample_period,
                                                  RespiratoryEDFSelectedSignal,
                                                  Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile),
                                                  Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1)
                                                  );


      PlotModel tempPlotModel = new PlotModel();
      Tuple<LineSeries, ScatterSeries, ScatterSeries, ScatterSeries, ScatterSeries, DateTimeAxis, LinearAxis> resp_plots = RespiratoryFactory.GetRespiratoryAnalysisPlot(
        RespiratoryEDFSelectedSignal,
        series.Points.Select(temp => (float)temp.Y).ToList(),
        sample_period,
        (float)(Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, false, LoadedEDFFile, sm) - Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm)),
        RespiratoryRemoveMultiplePeaks,
        RespiratoryMinimumPeakWidth,
        Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile),
        Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1)
        );

      if (RespiratoryUseConstantAxis)
      {
        resp_plots.Item7.Minimum = Utils.GetMinSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm);
        resp_plots.Item7.Maximum = Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm);
      }

      tempPlotModel.Series.Add(resp_plots.Item1);
      tempPlotModel.Series.Add(resp_plots.Item2);
      tempPlotModel.Series.Add(resp_plots.Item3);
      tempPlotModel.Series.Add(resp_plots.Item4);
      tempPlotModel.Series.Add(resp_plots.Item5);
      tempPlotModel.Axes.Add(resp_plots.Item6);
      tempPlotModel.Axes.Add(resp_plots.Item7);
      RespiratorySignalPlot = tempPlotModel;

      Tuple<double, double> breathing_periods = RespiratoryFactory.GetRespiratorySignalBreathingPeriod(new ScatterSeries[] { resp_plots.Item2, resp_plots.Item3, resp_plots.Item4, resp_plots.Item5 });
      RespiratoryBreathingPeriodMean = breathing_periods.Item1.ToString("0.## sec/breath");
      RespiratoryBreathingPeriodMedian = breathing_periods.Item2.ToString("0.## sec/breath");
    }
    /// <summary>
    /// Peforms respiratory analysis 
    /// </summary>
    public void PerformRespiratoryAnalysisEDF()
    {
      if (RespiratoryEDFSelectedSignal == null)
        return;

      RespiratoryProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_RespiratoryAnalysisEDF;
      bw.RunWorkerAsync();
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

    public RespiratoryModelView(CommonModelView i_common_data, SettingsModelView i_svm)
    {
      sm = i_svm.sm;
      common_data = i_common_data;
      common_data.PropertyChanged += Exterior_PropertyChanged;

      i_svm.PropertyChanged += Exterior_PropertyChanged;
    }
  }
}
