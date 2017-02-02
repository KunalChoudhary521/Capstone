using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using MathWorks.MATLAB.NET.Arrays;

namespace SleepApneaDiagnoser
{
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
    public double yAvr = Double.NaN;

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
