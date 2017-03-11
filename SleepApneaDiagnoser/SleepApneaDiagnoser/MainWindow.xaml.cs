﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using System.ComponentModel;
using System.IO;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace SleepApneaDiagnoser
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

    /// <summary>
    /// Function called to populate recent files list. Called when application is first loaded and if the recent files list changes.
    /// </summary>
    public void LoadRecent()
    {
      List<string> array = settings_modelview.RecentFiles.ToArray().ToList();

      itemControl_RecentEDF.Items.Clear();
      for (int x = 0; x < array.Count; x++)
        if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
          itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
    }

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

      common_modelview = new CommonModelView(this);
      settings_modelview = new SettingsModelView(this, common_modelview, settings_model);
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
      if (this.WindowState != WindowState.Maximized)
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

          column_RespAnalysis.Width = new GridLength(450, GridUnitType.Star);
          column_RespAnalysis.MinWidth = 300;
        }

        if (this.Height < 700)
        {
          row_RespiratoryAnalysisPropertiesPlot.Height = new GridLength(0, GridUnitType.Star);
        }
        else
        {
          row_RespiratoryAnalysisPropertiesPlot.Height = new GridLength(400, GridUnitType.Star);
        }
      }
    }
    private void Window_StateChanged(object sender, EventArgs e)
    {
      switch (this.WindowState)
      {
        case WindowState.Maximized:
          column_EDFHeader.Width = new GridLength(300);
          column_EDFHeader.MaxWidth = 300;
          column_EDFHeader.MinWidth = 300;

          column_RespAnalysis.Width = new GridLength(450, GridUnitType.Star);
          column_RespAnalysis.MinWidth = 300;
          row_RespiratoryAnalysisPropertiesPlot.Height = new GridLength(400, GridUnitType.Star);
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
        settings_modelview.WriteEDFSettings();
        common_modelview.LoadEDFFile(dialog.FileName);
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
            settings_modelview.WriteEDFSettings();
            common_modelview.LoadEDFFile(selected[x]);
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
    private void button_HideSignals_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.HideSignals();
    }
    private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.AddDerivative();
    }
    private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.RemoveDerivative();
    }
    private void button_Categories_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.ManageCategories();
    }
    private void button_AddFilter_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.AddFilter();
    }
    private void button_RemoveFilter_Click(object sender, RoutedEventArgs e)
    {
      settings_modelview.OpenCloseSettings();
      settings_modelview.RemoveFilter();
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
      preview_modelview.ExportSignals();
    }
    private void button_PreviewExportExcel_Click(object sender, RoutedEventArgs e)
    {
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

        // Check if Excel is installed
        Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
        if (app == null)
        {
          this.ShowMessageAsync("Error", "Excel installation not detected.\nThis application needs excel installed in order to export data.");
          return;
        }
        app.Quit();
        System.Runtime.InteropServices.Marshal.ReleaseComObject(app);

        preview_modelview.ExportExcel(dialog.FileName);
      }
    }

    // Analysis Tab Events 
    private void button_ExportRespiratoryCalculationsClick(object sender, RoutedEventArgs e)
    {
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
        
        // Check if Excel is installed
        Microsoft.Office.Interop.Excel.Application app = new Microsoft.Office.Interop.Excel.Application();
        if (app == null)
        {
          this.ShowMessageAsync("Error", "Excel installation not detected.\nThis application needs excel installed in order to export data.");
          return;
        }
        app.Quit();
        System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
        
        resp_modelview.ExportRespiratoryCalculations(dialog.FileName);
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
  }
}
