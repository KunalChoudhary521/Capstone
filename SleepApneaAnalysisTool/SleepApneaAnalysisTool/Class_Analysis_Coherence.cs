﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Model for variables used exclusively in the 'Coherence' sub tab of the 'Tool' tab
  /// </summary>
  public class CoherenceModel
  {
    #region Members

    /// <summary>
    /// The first selected signal
    /// </summary>
    public string CoherenceEDFSelectedSignal1;
    /// <summary>
    /// The second selected signal
    /// </summary>
    public string CoherenceEDFSelectedSignal2;
    /// <summary>
    /// The start time in 30s epochs of the signals to perform coherence analysis on
    /// </summary>
    public int CoherenceEDFStartRecord;
    /// <summary>
    /// The duration in 30s epochs of the signals to perform coherence analysis on
    /// </summary>
    public int CoherenceEDFDuration;
    /// <summary>
    /// A time domain plot of the first signal to perform coherence analysis on
    /// </summary>
    public PlotModel CoherenceSignalPlot1 = null;
    /// <summary>
    /// A time domain plot of the second signal to perform coherence analysis on
    /// </summary>
    public PlotModel CoherenceSignalPlot2 = null;
    /// <summary>
    /// The plot of the coherence signal
    /// </summary>
    public PlotModel CoherencePlot = null;
    /// <summary>
    /// If true, the progress ring should be shown
    /// If false, the progress ring should not be shown
    /// </summary>
    public bool CoherenceProgressRingEnabled = false;
    /// <summary>
    /// If true, use a constant axis
    /// If false, auto adjust to plot
    /// </summary>
    public bool CoherenceUseConstantAxis = false;

    #endregion
  }

  /// <summary>
  /// Model View for UI logic used exclusively in the 'Coherence' sub tab of the 'Tool' tab
  /// </summary>
  public class CoherenceModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    /// <summary>
    /// The settings model view, contains the loaded EDF file structure and user settings
    /// </summary>
    private SettingsModelView svm;
    /// <summary>
    /// Listens to property change events in the settings model view 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private SettingsModel sm
    {
      get
      {
        return svm.sm;
      }
    }

    // Property Changed Listener
    /// <summary>
    /// Listens to property change events in the settings model view 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Exterior_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IsEDFLoaded):
          if (!IsEDFLoaded)
          {
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
            CoherenceEDFSelectedSignal1 = null;
            CoherenceEDFSelectedSignal2 = null;
            CoherenceSignalPlot1 = null;
            CoherenceSignalPlot2 = null;
            CoherencePlot = null;
            CoherenceEDFStartRecord = 1;
            CoherenceEDFDuration = 1;
          }
          CoherenceEDFView_Changed();
          OnPropertyChanged(nameof(CoherenceEDFNavigationEnabled));
          OnPropertyChanged(nameof(IsEDFLoaded));
          break;
        case nameof(Utils.EPOCH_SEC):
          if (IsEDFLoaded)
          {
            cm.CoherenceEDFStartRecord = 1;
            cm.CoherenceEDFDuration = 1;
            CoherenceEDFView_Changed();
          }
          OnPropertyChanged(nameof(Utils.EPOCH_SEC));
          break;
        default:
          OnPropertyChanged(e.PropertyName);
          break;
      }
    }

    // Shared Properties
    /// <summary>
    /// True if a EDF file is loaded
    /// </summary>
    public bool IsEDFLoaded
    {
      get
      {
        return svm.IsEDFLoaded;
      }
    }
    /// <summary>
    /// The EDF file structure
    /// </summary>
    public EDFFile LoadedEDFFile
    {
      get
      {
        return svm.LoadedEDFFile;
      }
    }
    /// <summary>
    /// The time stamp of the beginning of the signal recordings in the EDF file
    /// </summary>
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
    /// <summary>
    /// The time stamp of the end of the signal recordings in the EDF file
    /// </summary>
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

    /// <summary>
    /// A list of all EDF signals 
    /// </summary>
    public ReadOnlyCollection<string> EDFAllSignals
    {
      get
      {
        return svm.EDFAllSignals;
      }
    }
    /// <summary>
    /// A list of all signals including EDF signals, derivative signals, and filtered signals that were not hidden by the user
    /// </summary>
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        return svm.AllNonHiddenSignals;
      }
    }
    /// <summary>
    /// True if the UI should use a dark theme
    /// </summary>
    public bool UseDarkTheme
    {
      get
      {
        return sm.UseDarkTheme;
      }
    }

    // Shared Functions
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
      return svm.GetSeriesFromSignalName(out sample_period, Signal, StartTime, EndTime);
    }
    
    #endregion
    
    /// <summary>
    /// Coherence Model
    /// </summary>
    private CoherenceModel cm = new CoherenceModel();

    #region Properties

    /// <summary>
    /// Function called when the coherence plot changes
    /// </summary>
    private void CoherencePlot_Changed()
    {
      CoherenceProgressRingEnabled = false;
    }
    /// <summary>
    /// Function called when the time interval to calculate coherence changes
    /// </summary>
    private void CoherenceEDFView_Changed()
    {
      OnPropertyChanged(nameof(CoherenceEDFStartRecord));
      OnPropertyChanged(nameof(CoherenceEDFStartTime));
      OnPropertyChanged(nameof(CoherenceEDFDuration));

      OnPropertyChanged(nameof(CoherenceEDFStartRecordMax));
      OnPropertyChanged(nameof(CoherenceEDFStartRecordMin));
      OnPropertyChanged(nameof(CoherenceEDFDurationMax));
      OnPropertyChanged(nameof(CoherenceEDFDurationMin));
    }

    /// <summary>
    /// The first selected signal
    /// </summary>
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
    /// <summary>
    /// The second selected signal
    /// </summary>
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
    /// <summary>
    /// The start time in 30s epochs of the signals to perform coherence analysis on
    /// </summary>
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
        CoherenceEDFView_Changed();
        PerformCoherenceAnalysisEDF();
      }
    }
    /// <summary>
    /// The duration in 30s epochs of the signals to perform coherence analysis on
    /// </summary>
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
        CoherenceEDFView_Changed();
        PerformCoherenceAnalysisEDF();
      }
    }
    /// <summary>
    /// A time domain plot of the first signal to perform coherence analysis on
    /// </summary>
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
    /// <summary>
    /// A time domain plot of the second signal to perform coherence analysis on
    /// </summary>
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
    /// <summary>
    /// The plot of the coherence signal
    /// </summary>
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
    /// <summary>
    /// If true, the progress ring should be shown
    /// If false, the progress ring should not be shown
    /// </summary>
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
    /// <summary>
    /// If true, the user can interact with the time UI controls
    /// </summary>
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

    /// <summary>
    /// The beginning of the time interval to calculate coherence
    /// </summary>
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
    /// <summary>
    /// The maximum beginning of the time interval to calculate coherence that a user can select
    /// </summary>
    public DateTime CoherenceEDFStartTimeMax
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          DateTime EndTime = EDFEndTime; // EDF End Time
          TimeSpan duration = Utils.EpochPeriodtoTimeSpan(CoherenceEDFDuration ?? 1); // User Selected Duration 
          return EndTime - duration;
        }
        else
          return new DateTime();
      }
    }
    /// <summary>
    /// The minimum beginning of the time interval to calculate coherence that a user can select
    /// </summary>
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
    /// <summary>
    /// The maximum beginning of the time interval to calculate coherence that a user can select
    /// </summary>
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
    /// <summary>
    /// The minimum beginning of the time interval to calculate coherence that a user can select
    /// </summary>
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
    /// <summary>
    /// The maximum length of the time interval to calculate coherence that a user can select
    /// </summary>
    public int CoherenceEDFDurationMax
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
        {
          DateTime EndTime = EDFEndTime; // EDF End Time
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
    /// <summary>
    /// The minimum length of the time interval to calculate coherence that a user can select
    /// </summary>
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

    /// <summary>
    /// If true, use a constant axis
    /// If false, auto adjust to plot
    /// </summary>
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
    #endregion

    #region Actions
    
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
    #endregion

    #region etc

    // INotify Interface
    /// <summary>
    /// Event raised when a property in this class changes value
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
    /// <summary>
    /// Function to raise PropertyChanged event 
    /// </summary>
    /// <param name="propertyName"></param>
    private void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    /// Constructor for the CoherenceModelView
    /// </summary>
    /// <param name="i_svm"></param>
    public CoherenceModelView(SettingsModelView i_svm)
    {
      svm = i_svm;
      svm.PropertyChanged += Exterior_PropertyChanged;
    }
  }
}
