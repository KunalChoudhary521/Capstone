using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using MahApps.Metro.Controls.Dialogs;
using System.Drawing;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Factory containing business logic used exclusively in the 'Preview' tab
  /// </summary>
  public static class PreviewFactory
  {
    #region Static Functions
    /// <summary>
    /// The function saves the currently previewed plot to an Excel workbook
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="PreviewPlot"></param>
    /// <param name="StartTime"></param>
    public static void SavePreviewPlotToExcel(string fileName, PlotModel PreviewPlot, DateTime StartTime)
    {
      if (PreviewPlot != null)
      {
        Excel.Application app = new Excel.Application();
        Utils.MakeExcelInteropPerformant(app, true);

        Excel.Workbook wb = app.Workbooks.Add(System.Reflection.Missing.Value);

        for (int x = 0; x < PreviewPlot.Series.Count; x++)
        {
          LineSeries series = (LineSeries)PreviewPlot.Series[x];
          object[,] signal_points = new object[series.Points.Count + 1, 3];
          #region Get Points

          signal_points[0, 0] = "Epoch";
          signal_points[0, 1] = "Date Time";
          signal_points[0, 2] = "Value";

          for (int y = 1; y < series.Points.Count + 1; y++)
          {
            signal_points[y, 0] = Utils.DateTimetoEpoch(DateTimeAxis.ToDateTime(series.Points[y - 1].X), StartTime);
            signal_points[y, 1] = DateTimeAxis.ToDateTime(series.Points[y - 1].X).ToString("MM/dd/yyyy hh:mm:ss.fff tt");
            signal_points[y, 2] = series.Points[y - 1].Y;
          }

          #endregion

          int COLUMNS = 3;
          int ROWS = signal_points.Length / COLUMNS;

          Excel.Worksheet ws = (Excel.Worksheet)wb.Sheets.Add();
          ws.Name = series.YAxis.Title;
          Utils.AddSignalToWorksheet(ws, ws.Name, signal_points, ROWS, COLUMNS, series.Color);

          System.Runtime.InteropServices.Marshal.ReleaseComObject(ws);
        }

        wb.SaveAs(fileName);
        Utils.MakeExcelInteropPerformant(app, false);
      }
    }
    #endregion
  }

  /// <summary>
  /// Model for variables used exclusively in the 'Preview' tab
  /// </summary>
  public class PreviewModel
  {
    #region Members

    /// <summary>
    /// The index of the current category to be displayed in the signal selection list
    /// -1 denotes displaying the 'All' category
    /// </summary>
    public int PreviewCurrentCategory = -1;
    /// <summary>
    /// The list of user selected signals in the signal selection list
    /// </summary>
    public List<string> PreviewSelectedSignals = new List<string>();

    /// <summary>
    /// Signal to show property info for
    /// </summary>
    public string PreviewPropertiesSelectedSignal;

    /// <summary>
    /// If true, the user selection for the start time of the plot should be date and time
    /// the user selection for the period of the plot should be in seconds
    /// If false, the user selection for the start time of the plot should be 30s epochs
    /// the user selection for the period of the plot should be in epochs
    /// </summary>
    public bool PreviewUseAbsoluteTime = false;
    /// <summary>
    /// The user selected start time of the plot in Date and Time format
    /// </summary>
    public DateTime PreviewViewStartTime = new DateTime();
    /// <summary>
    /// The user selected start time of the plot in epochs
    /// </summary>
    public int PreviewViewStartRecord = 0;
    /// <summary>
    /// The user selected period of the plot in seconds
    /// </summary>
    public int PreviewViewDuration = 0;
    /// <summary>
    /// The currently displayed preview plot
    /// </summary>
    public PlotModel PreviewSignalPlot = null;
    /// <summary>
    /// If false, the plot is currently being drawn and the tab should be disabled
    /// If true, the plot is done being drawn
    /// </summary>
    public bool PreviewNavigationEnabled = false;
    /// <summary>
    /// If false, let the plot auto adjust
    /// If true, the plot has a constant y axis
    /// </summary>
    public bool PreviewUseConstantAxis = true;

    #endregion 
  }

  /// <summary>
  /// Model View for UI logic used exclusively in the 'Preview' tab
  /// </summary>
  public class PreviewModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    /// <summary>
    /// The settings model view, contains the loaded EDF file structure and user settings
    /// </summary>
    private SettingsModelView svm;
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
        case nameof(Utils.EPOCH_SEC):
          PreviewCurrentCategory = -1;
          if (!IsEDFLoaded)
          {
            PreviewUseAbsoluteTime = false;
            PreviewViewStartTime = null;
            PreviewViewStartRecord = null;
            PreviewViewDuration = null;
          }
          else
          {
            PreviewUseAbsoluteTime = false;
            PreviewViewStartTime = LoadedEDFFile.Header.StartDateTime;
            PreviewViewStartRecord = 1;
            PreviewViewDuration = 1;
          }
          PreviewView_Changed();
          OnPropertyChanged(nameof(PreviewNavigationEnabled));

          // Header
          OnPropertyChanged(nameof(EDFStart));
          OnPropertyChanged(nameof(EDFEnd));
          OnPropertyChanged(nameof(EDFEndEpoch));
          OnPropertyChanged(nameof(EDFPatientName));
          OnPropertyChanged(nameof(EDFPatientSex));
          OnPropertyChanged(nameof(EDFPatientCode));
          OnPropertyChanged(nameof(EDFPatientBirthDate));
          OnPropertyChanged(nameof(EDFRecordEquipment));
          OnPropertyChanged(nameof(EDFRecordCode));
          OnPropertyChanged(nameof(EDFRecordTechnician));
          OnPropertyChanged(nameof(EDFAllSignals));
          OnPropertyChanged(nameof(PreviewSignals));
          OnPropertyChanged(nameof(AllNonHiddenSignals));

          OnPropertyChanged(nameof(IsEDFLoaded));
          OnPropertyChanged(nameof(Utils.EPOCH_SEC));
          break;
        default:
          OnPropertyChanged(e.PropertyName);
          break;
      }
    }
    /// <summary>
    /// Listens to the event called when the list of signals to preview changes in the settings model view
    /// </summary>
    private void Exterior_PreviewList_Updated()
    {
      PreviewCurrentCategory = -1;
      OnPropertyChanged(nameof(PreviewSignals));
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
    public ReadOnlyCollection<string> AllSignals
    {
      get
      {
        return svm.AllSignals;
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
    /// Preview Model
    /// </summary>
    private PreviewModel pm = new PreviewModel();

    #region Properties

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

    // EDF Header
    public string EDFStart
    {
      get
      {
        if (IsEDFLoaded)
          return EDFStartTime.ToString();
        else
          return null;
      }
    }
    public string EDFEnd
    {
      get
      {
        if (IsEDFLoaded)
          return EDFEndTime.ToString();
        else
          return null;
      }
    }
    public string EDFEndEpoch
    {
      get
      {
        if (IsEDFLoaded)
          return (Utils.DateTimetoEpoch(EDFEndTime, LoadedEDFFile) - 1).ToString();
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

          if (filteredSignal != null && filteredSignal.Average_Enabled)
            return filteredSignal.Average_Length.ToString("0.## ms");
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
          if (pm.PreviewViewStartTime != (value ?? EDFStartTime))
          {
            pm.PreviewViewStartTime = value ?? EDFStartTime;
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
          DateTime EndTime = EDFEndTime; // EDF End Time
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
          DateTime EndTime = EDFEndTime; // EDF End Time
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


    #endregion

    #region Actions

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

              // Get A Constant Color for the Series
              int num = AllSignals.IndexOf(pm.PreviewSelectedSignals[y]);
              List<KnownColor> colors = ((KnownColor[]) Enum.GetValues(typeof(KnownColor))).ToList();
              colors.RemoveAll(temp => Color.FromKnownColor(temp).GetBrightness() > 0.5);
              colors.RemoveAll(temp => Color.FromKnownColor(temp).Name.Contains("Gray") || Color.FromKnownColor(temp).Name.Contains("Grey") || Color.FromKnownColor(temp).Name.Contains("Black") || Color.FromKnownColor(temp).Name.Contains("Control") || Color.FromKnownColor(temp).Name.Contains("Info") || Color.FromKnownColor(temp).Name.Contains("Menu") || Color.FromKnownColor(temp).Name.Contains("Window") || Color.FromKnownColor(temp).Name.Contains("Text") || Color.FromKnownColor(temp).Name.Contains("Desktop"));
              var selected_color = Color.FromKnownColor(colors[num%colors.Count]);
              series.Color = OxyColor.FromArgb(selected_color.A, selected_color.R, selected_color.G, selected_color.B);

              // Axes Labels
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
    private void BW_ExportSignals_DoWork(object sender, DoWorkEventArgs e)
    {
      ExportSignalModel signals_data = ((List<dynamic>)e.Argument)[0];
      string location = ((List<dynamic>)e.Argument)[1];
      LineSeries signalToExport = null;
      float sampleFrequency = 0;

      foreach (var signal in pm.PreviewSelectedSignals)
      {
        EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == signal);

        DateTime startTime = Utils.EpochtoDateTime(signals_data.Epochs_From, LoadedEDFFile); // epoch start
        int end_epochs = signals_data.Epochs_From + signals_data.Epochs_Length;
        if (end_epochs > int.Parse(EDFEndEpoch))
        {
          end_epochs = int.Parse(EDFEndEpoch) + 1;
        }
        DateTime endTime = Utils.EpochtoDateTime(end_epochs, LoadedEDFFile); // epoch end

        if (edfsignal == null)
        {
          bool foundInDerived = false;
          EDFSignal oneDerivedEdfSignal = null;

          // look for the signal in the derviatives
          foreach (var derivedSignal in sm.DerivedSignals)
          {
            if (derivedSignal.DerivativeName == signal)
            {
              foundInDerived = true;
              oneDerivedEdfSignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == derivedSignal.Signal1Name);
              //derivedSamplePeriod = LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)oneDerivedEdfSignal.NumberOfSamplesPerDataRecord; ;
              sampleFrequency = (float)oneDerivedEdfSignal.NumberOfSamplesPerDataRecord / LoadedEDFFile.Header.DurationOfDataRecordInSeconds;
              break;
            }
          }

          if (foundInDerived)
          {
            signalToExport = GetSeriesFromSignalName(out sampleFrequency, signal, startTime, endTime);
          }

          bool foundInFiltered = false;

          foreach (var filteredSignal in sm.FilteredSignals) {
            if (filteredSignal.SignalName == signal)
            {
              foundInFiltered = true;
              break;
            }
          }

          if(foundInFiltered) {
            // check to see if it is a filtered signal
            signalToExport = GetSeriesFromSignalName(out sampleFrequency, signal, startTime, endTime);          
          }

          #region header_file

          FileStream hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate);
          hdr_file.SetLength(0); //clear it's contents
          hdr_file.Close(); //flush
          hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate); //reload

          StringBuilder sb_hdr = new StringBuilder(); // string builder used for writing into the file

          sb_hdr.AppendLine(signal) // name
              .AppendLine(signals_data.Subject_ID.ToString()) // subject id
              .AppendLine(Utils.EpochtoDateTime(signals_data.Epochs_From, LoadedEDFFile).ToString()) // epoch start
              .AppendLine(Utils.EpochtoDateTime(end_epochs, LoadedEDFFile).ToString()) // epoch length
              .AppendLine((1 / sampleFrequency).ToString()); // sample frequency 

          var bytes_to_write = Encoding.ASCII.GetBytes(sb_hdr.ToString());
          hdr_file.Write(bytes_to_write, 0, bytes_to_write.Length);
          hdr_file.Close();

          FileStream bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //the binary file for each signal
          bin_file.SetLength(0); //clear it's contents
          bin_file.Close(); //flush

          #endregion

          #region signal_binary_contents

          bin_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".bin", FileMode.OpenOrCreate); //reload
          BinaryWriter bin_writer = new BinaryWriter(bin_file);

          int start_index = 0;
          int end_index = signalToExport.Points.Count();

          if (start_index < 0) { start_index = 0; }

          for (int i = start_index; i < end_index; i++)
          {
            float value = (float)signalToExport.Points[i].Y;

            byte[] bytes = System.BitConverter.GetBytes(value);
            foreach (var b in bytes)
            {
              bin_writer.Write(b);
            }
          }

          bin_writer.Close();

          #endregion
        }
        else
        {
          //float sample_period = LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;
          float sample_frequency = (float)edfsignal.NumberOfSamplesPerDataRecord / LoadedEDFFile.Header.DurationOfDataRecordInSeconds;

          //hdr file contains metadata of the binary file
          FileStream hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate);
          hdr_file.SetLength(0); //clear it's contents
          hdr_file.Close(); //flush
          hdr_file = new FileStream(location + "/" + signals_data.Subject_ID + "-" + signal + ".hdr", FileMode.OpenOrCreate); //reload

          StringBuilder sb_hdr = new StringBuilder(); // string builder used for writing into the file

          int end_index = (int)(((signals_data.Epochs_From + signals_data.Epochs_Length) * Utils.EPOCH_SEC) / LoadedEDFFile.Header.DurationOfDataRecordInSeconds) * edfsignal.NumberOfSamplesPerDataRecord;

          var edfSignal = LoadedEDFFile.Header.Signals.Find(s => s.Label.Trim() == signal.Trim());
          var signalValues = LoadedEDFFile.retrieveSignalSampleValues(edfSignal).ToArray();
          if (end_index > signalValues.Count())
          {
            end_index = signalValues.Count();
          }
          int endEpochs = (int)((end_index * LoadedEDFFile.Header.DurationOfDataRecordInSeconds) / (Utils.EPOCH_SEC * edfsignal.NumberOfSamplesPerDataRecord)) + 1;


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

          int start_index = (int)(((signals_data.Epochs_From - 1) * Utils.EPOCH_SEC) / LoadedEDFFile.Header.DurationOfDataRecordInSeconds) * edfsignal.NumberOfSamplesPerDataRecord; // from epoch number * 30 seconds per epoch * sample rate = start time

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
    private void BW_ExportSignals_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      PreviewNavigationEnabled = true;
    }
    public void ExportSignals()
    {
      if (pm.PreviewSelectedSignals.Count > 0)
      {
        Dialog_Export_Previewed_Signals dlg = new Dialog_Export_Previewed_Signals(svm.p_window, this, pm.PreviewSelectedSignals);
        svm.p_window.ShowMetroDialogAsync(dlg);
      }
      else
      {
      }
    }
    public void ExportSignalsOutput(bool result, ExportSignalModel signals_to_export)
    {
      if (result == true)
      {
        FolderBrowserDialog folder_dialog = new FolderBrowserDialog();

        string location;

        if (folder_dialog.ShowDialog() == DialogResult.OK)
        {
          location = folder_dialog.SelectedPath;
        }
        else
        {
          return;
        }

        ExportSignalModel signals_data = signals_to_export;

        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += BW_ExportSignals_DoWork;
        bw.RunWorkerCompleted += BW_ExportSignals_RunWorkerCompleted;

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
        }
        else
        {
          PreviewNavigationEnabled = false;
          bw.RunWorkerAsync(arguments);
        }
      }
      else
      {
      }
    }
    
    /// <summary>
    /// Background process for exporting preview plot
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_ExportExcel_DoWork(object sender, DoWorkEventArgs e)
    {
      PreviewFactory.SavePreviewPlotToExcel(e.Argument.ToString(), PreviewSignalPlot, EDFStartTime);
    }
    /// <summary>
    /// Called when exporting preview plot finishes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_ExportExcel_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      PreviewNavigationEnabled = true;
    }
    /// <summary>
    /// Exports chart to image
    /// </summary>
    public void ExportExcel(string fileName)
    {
      PreviewNavigationEnabled = false;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_ExportExcel_DoWork;
      bw.RunWorkerCompleted += BW_ExportExcel_RunWorkerCompleted;
      bw.RunWorkerAsync(fileName);
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
    /// Constructor for the PreviewModelView
    /// </summary>
    /// <param name="i_svm"></param>
    public PreviewModelView(SettingsModelView i_svm)
    {
      svm = i_svm;
      svm.PropertyChanged += Exterior_PropertyChanged;
      svm.PreviewList_Updated += Exterior_PreviewList_Updated;
    }

  }
}
