using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using EDF;
using OxyPlot;
using OxyPlot.Series;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro;
using OxyPlot.Axes;

namespace SleepApneaAnalysisTool
{
  #region Sub Classes

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

    public bool Average_Enabled = false;
    public float Average_Length = 0;
  }

  #endregion

  public class SettingsModel
  {
    #region Members

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

    #endregion 
  }

  public class SettingsModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    private CommonModelView common_data;

    // Property Changed Listener
    private void Exterior_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IsEDFLoaded):
          LoadEDFSettings();
          RecentFiles_Add(LoadedEDFFileName);
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
    public string LoadedEDFFileName
    {
      get
      {
        return common_data.LoadedEDFFileName;
      }
    }
    
    #endregion

    /// <summary>
    /// Preview Model
    /// </summary>
    private PreviewModel pm = new PreviewModel();
    /// <summary>
    /// Settings Model
    /// </summary>
    public SettingsModel sm;
    /// <summary>
    /// The Main Window
    /// </summary>
    public MainWindow p_window;

    #region Properties

    private void AppliedThemeColor_Changed()
    {
      OnPropertyChanged(nameof(AppliedThemeColor));
      
      Accent new_accent = Utils.ThemeColorToAccent(AppliedThemeColor);

      ThemeManager.AddAccent(new_accent.Name, new_accent.Resources.Source);
      ThemeManager.ChangeAppStyle(Application.Current, new_accent, ThemeManager.GetAppTheme(UseDarkTheme ? "BaseDark" : "BaseLight"));

      // Update all charts to dark or light theme
      var all_plotmodels = p_window.FindChildren<OxyPlot.Wpf.PlotView>().ToList();   
      for (int x = 0; x < all_plotmodels.Count; x++)
      {
        OxyPlot.Wpf.PlotView plot = all_plotmodels[x];

        PlotModel model = plot.Model;
        if (model != null)
        {
          Utils.ApplyThemeToPlot(model, UseDarkTheme);
          plot.Model.InvalidatePlot(true); 
        }
      }

      var all_datetimeupdown = p_window.FindChildren<Xceed.Wpf.Toolkit.DateTimeUpDown>().ToList();
      for (int x = 0; x < all_datetimeupdown.Count; x++)
      {
        all_datetimeupdown[x].Foreground = UseDarkTheme ? Brushes.White : Brushes.Black;
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
        if (!Directory.Exists(Utils.settings_folder))
          Directory.CreateDirectory(Utils.settings_folder);

        string[] value = null;

        if (File.Exists(Utils.settings_folder + "\\recent.txt"))
        {
          StreamReader sr = new StreamReader(Utils.settings_folder + "\\recent.txt");
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
      if (!Directory.Exists(Utils.settings_folder))
        Directory.CreateDirectory(Utils.settings_folder);

      List<string> array = RecentFiles.ToArray().ToList();
      array.Insert(0, path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter(Utils.settings_folder + "\\recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.Invoke(new Action(() => p_window.LoadRecent()));
    }
    public void RecentFiles_Remove(string path)
    {
      if (!Directory.Exists(Utils.settings_folder))
        Directory.CreateDirectory(Utils.settings_folder);

      List<string> array = RecentFiles.ToArray().ToList();
      array.Remove(path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter(Utils.settings_folder + "\\recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.Invoke(new Action(() => p_window.LoadRecent()));
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

    #endregion

    #region Helper Function

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
        if (filteredSignal.Average_Enabled)
        {
          float LENGTH;
          LENGTH = Math.Max(filteredSignal.Average_Length / (sample_period * 1000), 1);

          series = Utils.ApplyAverageFilter(series, (int)LENGTH);
        }
      }

      return series;
    }
    #endregion 

    #region Actions

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

      PreviewList_Updated();
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

    #region etc

    public event Action PreviewList_Updated;

    // INotify Interface
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public SettingsModelView(MainWindow i_window, CommonModelView i_common_data, SettingsModel i_sm)
    {
      p_window = i_window;
      common_data = i_common_data;
      sm = i_sm;

      common_data.PropertyChanged += Exterior_PropertyChanged;
    }
  }
}
