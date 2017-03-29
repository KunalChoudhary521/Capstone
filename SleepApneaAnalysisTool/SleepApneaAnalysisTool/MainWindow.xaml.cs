using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Windows.Media;

using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using OxyPlot;
using OxyPlot.Wpf;

using Xceed.Wpf.Toolkit;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    CommonModelView common_modelview;
    PreviewModelView preview_modelview;
    RespiratoryModelView resp_modelview;
    EEGModelView eeg_modelview;
    CoherenceModelView cohere_modelview;
    SettingsModelView settings_modelview;

    #region UI Helper Functions

    // Updates the Recent Files List
    public void LoadRecent()
    {
      this.Invoke(new Action(() => {
        List<string> array = settings_modelview.RecentFiles.ToArray().ToList();

        itemControl_RecentEDF.Items.Clear();
        for (int x = 0; x < array.Count; x++)
          if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
            itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
      }));
    }

    // Loading EDF File UI
    private ProgressDialogController controller;
    private BackgroundWorker bw_progressbar = new BackgroundWorker();
    private void BW_LoadEDFFileUpDateProgress(object sender, DoWorkEventArgs e)
    {
      long process_start = Process.GetCurrentProcess().PagedMemorySize64;
      long file_size = (long)(new FileInfo(e.Argument.ToString()).Length * 2.2);
      long current_progress = 0;

      while (!bw_progressbar.CancellationPending)
      {
        current_progress = Math.Max(current_progress, Process.GetCurrentProcess().PagedMemorySize64 - process_start);
        double progress = Math.Min(99, (current_progress * 100 / (double)file_size));

        try { controller.SetProgress(progress); }
        catch { break; }
      }
    }
    private void EDFFinishedLoading()
    {
      controller.CloseAsync();
      controller = null;
      this.ShowMessageAsync("Success", "EDF File Loaded.");
    }
    private async void OpenEDF(string fileName)
    {
      // Create Progress Bar
      controller = await this.ShowProgressAsync("Please wait...", "Loading EDF File: " + fileName);

      // Progress Bar should not be cancelable
      controller.SetCancelable(false);
      controller.Maximum = 100;

      // 'Update Progress Bar' Task 
      bw_progressbar = new BackgroundWorker();
      bw_progressbar.WorkerSupportsCancellation = true;
      bw_progressbar.DoWork += BW_LoadEDFFileUpDateProgress;
      bw_progressbar.RunWorkerAsync(fileName);

      // Load File
      settings_modelview.WriteEDFSettings();
      common_modelview.LoadEDFFile(fileName);
    }

    // Personalization

    /// <summary>
    /// Function called when the user changes the UI theme color
    /// </summary>
    private void AppliedThemeColor_Changed()
    {
      Color AppliedThemeColor = settings_modelview.AppliedThemeColor;
      bool UseDarkTheme = settings_modelview.UseDarkTheme;

      Accent new_accent = Utils.ThemeColorToAccent(AppliedThemeColor);

      ThemeManager.AddAccent(new_accent.Name, new_accent.Resources.Source);
      ThemeManager.ChangeAppStyle(Application.Current, new_accent, ThemeManager.GetAppTheme(UseDarkTheme ? "BaseDark" : "BaseLight"));

      // Update all charts to dark or light theme
      var all_plotmodels = this.FindChildren<PlotView>().ToList();
      for (int x = 0; x < all_plotmodels.Count; x++)
      {
        PlotView plot = all_plotmodels[x];

        PlotModel model = plot.Model;
        if (model != null)
        {
          Utils.ApplyThemeToPlot(model, UseDarkTheme);
          plot.Model.InvalidatePlot(true);
        }
      }

      var all_datetimeupdown = this.FindChildren<DateTimeUpDown>().ToList();
      for (int x = 0; x < all_datetimeupdown.Count; x++)
      {
        all_datetimeupdown[x].Foreground = UseDarkTheme ? Brushes.White : Brushes.Black;
      }
    }

    #endregion

    /// <summary>
    /// Constructor for GUI class.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      SettingsModel settings_model = new SettingsModel();

      common_modelview = new CommonModelView();
      settings_modelview = new SettingsModelView(common_modelview, settings_model);
      resp_modelview = new RespiratoryModelView(settings_modelview);
      eeg_modelview = new EEGModelView(settings_modelview);
      cohere_modelview = new CoherenceModelView(settings_modelview);
      preview_modelview = new PreviewModelView(settings_modelview);

      this.DataContext = common_modelview;

      this.TabItem_Preview.DataContext = preview_modelview;

      this.TabItem_Respiratory.DataContext = resp_modelview;
      this.grid_SettingsRespiratory.DataContext = resp_modelview;

      this.TabItem_EEG.DataContext = eeg_modelview;

      this.TabItem_Coherence.DataContext = cohere_modelview;

      this.Flyout_Settings.DataContext = settings_modelview;
      this.grid_SettingsMainMenu.DataContext = settings_modelview;
      this.grid_SettingsPersonalization.DataContext = settings_modelview;

      settings_modelview.Load_Recent += LoadRecent;
      settings_modelview.Theme_Changed += AppliedThemeColor_Changed;
      common_modelview.EDF_Loading_Finished += EDFFinishedLoading;

      LoadRecent();
      settings_modelview.LoadAppSettings();
    }
    private void Window_Closing(object sender, CancelEventArgs e)
    {
      settings_modelview.WriteAppSettings();
      settings_modelview.WriteEDFSettings();
    }
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (this.WindowState != System.Windows.WindowState.Maximized)
      {
        if (this.Width < 1300)
        {
          column_EDFHeader.Width = new GridLength(0);
          column_EDFHeader.MaxWidth = 0;
          column_EDFHeader.MinWidth = 0;

          column_RespAnalysis.Width = new GridLength(0, GridUnitType.Star);
          column_RespAnalysis.MinWidth = 0;
        }
        else
        {
          column_EDFHeader.Width = new GridLength(300);
          column_EDFHeader.MaxWidth = 300;
          column_EDFHeader.MinWidth = 300;

          column_RespAnalysis.Width = new GridLength(450, GridUnitType.Star);
          column_RespAnalysis.MinWidth = 300;
        }
      }
    }
    private void Window_StateChanged(object sender, EventArgs e)
    {
      switch (this.WindowState)
      {
        case System.Windows.WindowState.Maximized:
          column_EDFHeader.Width = new GridLength(300);
          column_EDFHeader.MaxWidth = 300;
          column_EDFHeader.MinWidth = 300;

          column_RespAnalysis.Width = new GridLength(450, GridUnitType.Star);
          column_RespAnalysis.MinWidth = 300;
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
        OpenEDF(dialog.FileName);
      }       
    }
    private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
    {
      List<string> array = settings_modelview.RecentFiles.ToArray().ToList();
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
            OpenEDF(selected[x]);
            break;
          }
          else
          {
            this.ShowMessageAsync("Error", "File not Found");
            settings_modelview.RecentFiles_Remove(selected[x]);
          }
        }
      }
    }
    private void TextBlock_UnloadEDF_Click(object sender, RoutedEventArgs e)
    {
      common_modelview.LoadedEDFFile = null;
      common_modelview.LoadedEDFFileName = null;
      GC.Collect();
    }

    // Setting Flyout Events 
    private void button_Settings_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.SettingsMainMenuVisible = true;
      settings_modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_MainMenuClick(object sender, RoutedEventArgs e)
    {
      settings_modelview.SettingsMainMenuVisible = true;
      settings_modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_PersonalizationSettings_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.SettingsMainMenuVisible = false;
      settings_modelview.SettingsPersonalizationVisible = true;
      resp_modelview.SettingsRespiratoryVisible = false;
    }
    private void button_RespiratorySettings_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.SettingsMainMenuVisible = false;
      settings_modelview.SettingsPersonalizationVisible = false;
      resp_modelview.SettingsRespiratoryVisible = true;
    }
    private async void button_EpochDefinition_Click(object sender, RoutedEventArgs e)
    {
      string x = await this.ShowInputAsync("New Epoch Definition", "Please enter an integer epoch definition in seconds (default = 30)");
      int new_def;
      if (x != null)
      {
        if (Int32.TryParse(x, out new_def))
        {
          settings_modelview.ModifyEpochDefinition(new_def);
        }
        else
        {
          await this.ShowMessageAsync("Error", "Input value must be an integer. No changes made");
        }
      }
    }
    private void button_HideSignals_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Hide_Signals dlg = new Dialog_Hide_Signals(this, settings_modelview);                                                            
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(this, settings_modelview);
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(this, settings_modelview);
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_Categories_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Manage_Categories dlg = new Dialog_Manage_Categories(this, settings_modelview);
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_AddFilter_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Add_Filter dlg = new Dialog_Add_Filter(this, settings_modelview);                                                  
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_RemoveFilter_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();

      Dialog_Remove_Filter dlg = new Dialog_Remove_Filter(this, settings_modelview);
      this.ShowMetroDialogAsync(dlg);
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
      Dialog_Export_Previewed_Signals dlg = new Dialog_Export_Previewed_Signals(this, preview_modelview);
      this.ShowMetroDialogAsync(dlg);
    }
    private void button_PreviewExportExcel_Click(object sender, RoutedEventArgs e)
    {
      // Check if Excel is installed
      Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
      if (app == null)
      {
        this.ShowMessageAsync("Error", "Excel installation not detected.\nThis application needs excel installed in order to export data.");
        return;
      }
      app.Quit();
      System.Runtime.InteropServices.Marshal.ReleaseComObject(app);

      Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
      dialog.Filter = ".xlsx|*.xlsx";
      dialog.Title = "Select a save location";

      if (dialog.ShowDialog() == true)
      {
        // Delete file if it exists 
        try
        {
          if (File.Exists(dialog.FileName))
            File.Delete(dialog.FileName);
        }
        catch
        {
          // Should trigger if file deletion fails
          this.ShowMessageAsync("Error", "Selected file is currently in use by another process.\nDo you currently have it open in Excel?");
          return;
        }

        preview_modelview.ExportExcel(dialog.FileName);
      }
    }

    // Analysis Tab Events 
    private void button_ExportRespiratoryPlotClick(object sender, RoutedEventArgs e)
    {
      // Check if Excel is installed
      Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
      if (app == null)
      {
        this.ShowMessageAsync("Error", "Excel installation not detected.\nThis application needs excel installed in order to export data.");
        return;
      }
      app.Quit();
      System.Runtime.InteropServices.Marshal.ReleaseComObject(app);

      Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
      dialog.Filter = ".xlsx|*.xlsx";
      dialog.Title = "Select a save location";

      if (dialog.ShowDialog() == true)
      {
        // Delete file if it exists 
        try
        {
          if (File.Exists(dialog.FileName))
            File.Delete(dialog.FileName);
        }
        catch
        {
          // Should trigger if file deletion fails
          this.ShowMessageAsync("Error", "Selected file is currently in use by another process.\nDo you currently have it open in Excel?");
          return;
        }

        resp_modelview.ExportRespiratoryPlot(dialog.FileName);
      }
    }
    private void button_ExportEEGCalculations_Click(object sender, RoutedEventArgs e)
    {
      eeg_modelview.ExportEEGCalculations();
    }
    private void button_ExportEEGPlots_Click(object sender, RoutedEventArgs e)
    {
      eeg_modelview.ExportEEGPlots();
    }
    private void button_BINRespiratoryAnalysis_Click(object sender, RoutedEventArgs e)
    {
      resp_modelview.LoadRespiratoryAnalysisBinary();
    }
    private void button_BINEEGAnalysis_Click(object sender, RoutedEventArgs e)
    {
      eeg_modelview.PerformEEGAnalysisBinary();
    }
    private void button_ExportEEGPlotsBin_Click(object sender, RoutedEventArgs e)
    {
      eeg_modelview.ExportEEGPlotsBin();
    }
    private void button_ExportEEGAnalysisBin_Click(object sender, RoutedEventArgs e)
    {
      eeg_modelview.ExportEEGCalculationsBin();
    }

    private void button_DisplayAnalytics_Checked(object sender, RoutedEventArgs e)
    {
      RespSignalPlot.Visibility = resp_modelview.RespiratoryDisplayAnalytics ? Visibility.Hidden : Visibility.Visible;
      RespAnalyticsPlot.Visibility = !resp_modelview.RespiratoryDisplayAnalytics ? Visibility.Hidden : Visibility.Visible;
    }
  }
}
