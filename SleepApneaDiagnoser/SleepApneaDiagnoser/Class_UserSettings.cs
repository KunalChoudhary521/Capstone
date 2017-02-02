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

namespace SleepApneaDiagnoser
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

    public bool WeightedAverage_Enabled = false;
    public float WeightedAverage_Length = 0;
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

    private CommonModelView p_modelview;
    public CommonModelView common_data
    {
      get
      {
        return p_modelview;
      }
      set
      {
        p_modelview = value;
        p_modelview.PropertyChanged += Exterior_PropertyChanged;
      }
    }

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
    public ReadOnlyCollection<string> AllSignals
    {
      get
      {
        return common_data.AllSignals;
      }
    }
     
    // Shared Functions
    public LineSeries GetSeriesFromSignalName(out float sample_period, string Signal, DateTime StartTime, DateTime EndTime)
    {
      return common_data.GetSeriesFromSignalName(out sample_period, Signal, StartTime, EndTime);
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
    private MainWindow p_window;

    #region Properties

    private void AppliedThemeColor_Changed()
    {
      OnPropertyChanged(nameof(AppliedThemeColor));

      var application = System.Windows.Application.Current;
      Accent newAccent = Utils.ThemeColorToAccent(AppliedThemeColor);

      ThemeManager.AddAccent(newAccent.Name, newAccent.Resources.Source);
      ThemeManager.ChangeAppStyle(application, newAccent, ThemeManager.GetAppTheme(UseDarkTheme ? "BaseDark" : "BaseLight"));

      // Update all charts to dark or light theme
      var all_plotmodels = p_window.FindChildren<OxyPlot.Wpf.PlotView>().ToList();   
      for (int x = 0; x < all_plotmodels.Count; x++)
      {
        OxyPlot.Wpf.PlotView plot = all_plotmodels[x];

        PlotModel model = plot.Model;
        if (model != null)
        {
          Utils.ApplyThemeToPlot(model, UseDarkTheme);
          plot.Model = null;
          plot.Model = model;
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

      p_window.Invoke(new Action(() => p_window.LoadRecent()));
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

      p_window.Invoke(new Action(() => p_window.LoadRecent()));
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
    }
  }
}
