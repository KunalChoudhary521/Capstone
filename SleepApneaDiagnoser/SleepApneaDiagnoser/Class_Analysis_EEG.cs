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
}
