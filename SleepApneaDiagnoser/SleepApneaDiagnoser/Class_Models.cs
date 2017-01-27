using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Model for variables used exclusively in the 'Preview' tab
  /// </summary>
  public class PreviewModel
  {
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
  }

  /// <summary>
  /// Model for variables used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryModel
  {
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
  }

  /// <summary>
  /// Model for variables used exclusively in the 'EEG' sub tab of the 'Analysis' tab
  /// </summary>
  public class EEGModel
  {
    /// <summary>
    /// The user selected signal to perform eeg analysis on
    /// </summary>
    public string EEGEDFSelectedSignal;
    /// <summary>
    /// The user selected start time for the eeg analysis in 30s epochs
    /// </summary>
    public int EEGEDFStartRecord;
    /// <summary>
    /// The user selected period for the eeg analysis in 30s epochs
    /// </summary>
    public int EEGEDFDuration;
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
    /// Displays the eeg spectrogram power plot
    /// </summary>
    public String[] EEGExportOptions = null;
    /// <summary>
    /// Displays the eeg spectrogram power plot
    /// </summary>
  }

  /// <summary>
  /// Model for variables used exclusively in the 'Coherence' sub tab of the 'Tool' tab
  /// </summary>
  public class CoherenceModel
  {
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
  }

  public class SignalCategory
  {
    public string CategoryName = "";
    public string CategoryNameNoNumber
    {
      get
      {
        if (CategoryName.Contains('.'))
        {
          return CategoryName.Substring(CategoryName.IndexOf('.') + 2).Trim();
        }
        else
        {
          return CategoryName;
        }
      }
    }
    public List<string> Signals = new List<string>();
    
    public SignalCategory(string name)
    {
      CategoryName = name;
    }
  }
  public class DerivativeSignal
  {
    public string DerivativeName;
    public string Signal1Name;
    public string Signal2Name;

    public DerivativeSignal(string name, string signal1, string signal2)
    {
      DerivativeName = name;
      Signal1Name = signal1;
      Signal2Name = signal2;      
    }
  }
  public class SignalYAxisExtremes
  {
    public string SignalName = "";
    public double yMax = Double.NaN;
    public double yMin = Double.NaN;

    public SignalYAxisExtremes(string name)
    {
      SignalName = name;
    }
  }
  public class FilteredSignal
  {
    public string SignalName = "";
    public string OriginalName = "";

    public bool LowPass_Enabled = false;
    public float LowPassCutoff = 0;
    
    public bool WeightedAverage_Enabled = false;
    public float WeightedAverage_Length = 0;
  }

  public class SettingsModel
  {
    public bool FlyoutOpen = false;
    public bool SettingsMainMenuVisible = true;
    public bool SettingsPersonalizationVisible = false;
    public bool SettingsRespiratoryVisible = false;
    public List<SignalCategory> SignalCategories = new List<SignalCategory>();
    public List<DerivativeSignal> DerivedSignals = new List<DerivativeSignal>();
    public List<string> HiddenSignals = new List<string>();
    public List<SignalYAxisExtremes> SignalsYAxisExtremes = new List<SignalYAxisExtremes>();
    public List<FilteredSignal> FilteredSignals = new List<FilteredSignal>();

    public System.Windows.Media.Color ThemeColor = System.Windows.Media.Colors.Blue;
    public bool UseCustomColor = false;
    public bool UseDarkTheme = false;
  }
}
