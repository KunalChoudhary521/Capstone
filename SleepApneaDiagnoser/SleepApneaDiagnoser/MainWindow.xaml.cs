using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using MATLAB_496;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace SleepApneaDiagnoser
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow
  {
    ModelView model;

    /// <summary>
    /// Function called after new EDF file is loaded to populate right pane of Preview tab.
    /// </summary>
    public void EDFLoaded()
    {
      LoadRecent();
      this.IsEnabled = true;
    }
    /// <summary>
    /// Function called to populate recent files list. Called when application is first loaded and if the recent files list changes.
    /// </summary>
    public void LoadRecent()
    {
      List<string> array = model.RecentFiles.ToArray().ToList();

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

      model = new ModelView(this);
      this.DataContext = model;
      LoadRecent();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
      model.WriteToCategoriesFile();
    }

    // Home Tab Events
    private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
    {
      model.LoadedEDFFile = null;

      OpenFileDialog dialog = new OpenFileDialog();
      dialog.Filter = "EDF files (*.edf)|*.edf";
      dialog.Title = "Select an EDF file";

      if (dialog.ShowDialog() == true)
      {
        this.IsEnabled = false;
        model.LoadEDFFile(dialog.FileName);
      }
    }
    private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
    {
      model.LoadedEDFFile = null;

      List<string> array = model.RecentFiles.ToArray().ToList();
      List<string> selected = array.Where(temp => temp.Split('\\')[temp.Split('\\').Length - 1] == ((Hyperlink)sender).Inlines.FirstInline.DataContext.ToString()).ToList();

      if (selected.Count == 0)
      {
        this.ShowMessageAsync("Error", "File not Found");
        EDFLoaded();
      }
      else
      {
        for (int x = 0; x < selected.Count; x++)
        {
          if (File.Exists(selected[x]))
          {
            this.IsEnabled = false;
            model.LoadEDFFile(selected[x]);
            break;
          }
          else
          {
            this.ShowMessageAsync("Error", "File not Found");
            model.RecentFiles_Remove(selected[x]);
          }
        }
      }
    }

    // Preview Tab Events   
    private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      model.SetSelectedSignals(listBox_SignalSelect.SelectedItems);
    }
    private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (comboBox_SignalSelect.SelectedValue != null)
      {
        EDFSignal edfsignal = model.LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == comboBox_SignalSelect.SelectedValue.ToString().Trim());
        textBox_SampRecord.Text = ((int)((double)edfsignal.NumberOfSamplesPerDataRecord / (double)model.LoadedEDFFile.Header.DurationOfDataRecordInSeconds)).ToString();
      }
      else
      {
        textBox_SampRecord.Text = "";
      }
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

    private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
    {
      model.AddDerivative();
    }
    private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
    {
      model.RemoveDerivative();
    }
    private void button_Categories_Click(object sender, RoutedEventArgs e)
    {
      model.ManageCategories();
    }
    private void button_Next_Click(object sender, RoutedEventArgs e)
    {
      model.NextCategory();
    }
    private void button_Prev_Click(object sender, RoutedEventArgs e)
    {
      model.PreviousCategory();
    }

    private void export_button_Click(object sender, RoutedEventArgs e)
    {      
      model.ExportSignals();
    }
  }

  public class ModelView : INotifyPropertyChanged
  {
    /*********************************************** THIS IS ALL A MESS THAT I NEED TO CLEAN UP *************************************/

    // Load EDF
    private ProgressDialogController controller;
    private async void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
    {
      controller.SetCancelable(false);

      EDFFile temp = new EDFFile();
      temp.readFile(e.Argument.ToString());
      LoadedEDFFile = temp;
      controller.SetProgress(.33);
      LoadCommonDerivativesFile();
      controller.SetProgress(.66);
      LoadCategoriesFile();
      controller.SetProgress(.100);

      await controller.CloseAsync();
    }
    private void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
    {
      RecentFiles_Add(LoadedEDFFileName);

      p_window.EDFLoaded();
      p_window.ShowMessageAsync("Success!", "EDF file loaded");

    }
    public async void LoadEDFFile(string fileNameIn)
    {
      controller = await p_window.ShowProgressAsync("Please wait...", "Loading EDF File: " + fileNameIn);

      LoadedEDFFileName = fileNameIn;
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_LoadEDFFile;
      bw.RunWorkerCompleted += BW_FinishLoad;
      bw.RunWorkerAsync(LoadedEDFFileName);
    }

    // Create Chart
    private void BW_CreateChart(object sender, DoWorkEventArgs e)
    {
      PlotModel temp_PreviewSignalPlot = new PlotModel();
      temp_PreviewSignalPlot.Series.Clear();
      temp_PreviewSignalPlot.Axes.Clear();

      if (p_PreviewSelectedSignals.Count > 0)
      {
        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.Key = "DateTime";
        xAxis.Minimum = DateTimeAxis.ToDouble(PreviewViewStartTime);
        xAxis.Maximum = DateTimeAxis.ToDouble(PreviewViewEndTime);
        temp_PreviewSignalPlot.Axes.Add(xAxis);

        for (int x = 0; x < p_PreviewSelectedSignals.Count; x++)
        {
          LineSeries series = new LineSeries();
          if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == p_PreviewSelectedSignals[x].Trim()) != null) // Normal EDF Signal
          {
            EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == p_PreviewSelectedSignals[x].Trim());

            float sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

            List<float> values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);

            int startIndex, indexCount;
            TimeSpan startPoint = (PreviewViewStartTime ?? new DateTime()) - LoadedEDFFile.Header.StartDateTime;
            TimeSpan duration = PreviewViewEndTime - (PreviewViewStartTime ?? new DateTime());
            startIndex = (int)(startPoint.TotalSeconds / sample_period);
            indexCount = (int)(duration.TotalSeconds / sample_period);

            for (int y = startIndex; y < indexCount + startIndex; y++)
            {
              series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
            }
          }
          else // Derivative Signal
          {
            string[] deriv_info = p_DerivedSignals.Find(temp => temp[0] == p_PreviewSelectedSignals[x]);
            EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[1].Trim());
            EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[2].Trim());

            List<float> values1;
            List<float> values2;
            float sample_period;
            if (edfsignal1.NumberOfSamplesPerDataRecord == edfsignal2.NumberOfSamplesPerDataRecord) // No resampling
            {
              values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
              values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);
              sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
            }
            else if (edfsignal1.NumberOfSamplesPerDataRecord > edfsignal2.NumberOfSamplesPerDataRecord) // Upsample signal 2
            {
              Processing proc = new Processing();
              MWArray[] input = new MWArray[2];
              input[0] = new MWNumericArray(LoadedEDFFile.retrieveSignalSampleValues(edfsignal2).ToArray());
              input[1] = edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord;

              values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
              values2 = (
                          (double[])(
                              (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                          ).ToVector(MWArrayComponent.Real)
                        ).ToList().Select(temp => (float)temp).ToList();

              sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
            }
            else // Upsample signal 1
            {
              Processing proc = new Processing();
              MWArray[] input = new MWArray[2];
              input[0] = new MWNumericArray(LoadedEDFFile.retrieveSignalSampleValues(edfsignal1).ToArray());
              input[1] = edfsignal2.NumberOfSamplesPerDataRecord / edfsignal1.NumberOfSamplesPerDataRecord;

              values1 = (
                          (double[])(
                              (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                          ).ToVector(MWArrayComponent.Real)
                        ).ToList().Select(temp => (float)temp).ToList();
              values2 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal2);

              sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal2.NumberOfSamplesPerDataRecord;
            }

            int startIndex, indexCount;
            TimeSpan startPoint = (PreviewViewStartTime ?? new DateTime()) - LoadedEDFFile.Header.StartDateTime;
            TimeSpan duration = PreviewViewEndTime - (PreviewViewStartTime ?? new DateTime());
            startIndex = (int)(startPoint.TotalSeconds / sample_period);
            indexCount = (int)(duration.TotalSeconds / sample_period);

            for (int y = startIndex; y < indexCount + startIndex; y++)
            {
              series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values1[y] - values2[y]));
            }
          }

          series.YAxisKey = p_PreviewSelectedSignals[x];
          series.XAxisKey = "DateTime";

          LinearAxis yAxis = new LinearAxis();
          yAxis.MajorGridlineStyle = LineStyle.Solid;
          yAxis.MinorGridlineStyle = LineStyle.Dot;
          yAxis.Title = p_PreviewSelectedSignals[x];
          yAxis.Key = p_PreviewSelectedSignals[x];
          yAxis.EndPosition = (double)1 - (double)x * ((double)1 / (double)p_PreviewSelectedSignals.Count);
          yAxis.StartPosition = (double)1 - (double)(x + 1) * ((double)1 / (double)p_PreviewSelectedSignals.Count);

          temp_PreviewSignalPlot.Axes.Add(yAxis);
          temp_PreviewSignalPlot.Series.Add(series);
        }
      }

      PreviewSignalPlot = temp_PreviewSignalPlot;
    }
    private void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
    {
      PreviewNavigationEnabled = true;
    }
    public void DrawChart()
    {
      PreviewNavigationEnabled = false;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_CreateChart;
      bw.RunWorkerCompleted += BW_FinishChart;
      bw.RunWorkerAsync();
    }

    /********************************************************************************************************************************/

    // Static Functions
    private static int EPOCH_SEC = 30;
    private static DateTime EpochtoDateTime(int epoch, EDFFile file)
    {
      return file.Header.StartDateTime + new TimeSpan(0, 0, epoch * EPOCH_SEC);
    }
    private static TimeSpan EpochPeriodtoTimeSpan(int period)
    {
      return new TimeSpan(0, 0, 0, period * EPOCH_SEC);
    }
    private static int DateTimetoEpoch(DateTime time, EDFFile file)
    {
      return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)EPOCH_SEC);
    }
    private static int TimeSpantoEpochPeriod(TimeSpan period)
    {
      return (int)(period.TotalSeconds / (double)EPOCH_SEC);
    }

    // General Private Variables
    private MainWindow p_window;
    private EDFFile p_LoadedEDFFile = null;
    private string p_LoadedEDFFileName = null;
    private List<string> p_SignalCategories = new List<string>();
    private List<List<string>> p_SignalCategoryContents = new List<List<string>>();

    // Preview Private Variables
    private int p_PreviewCurrentCategory = -1;
    private List<string> p_PreviewSelectedSignals = new List<string>();
    private List<string[]> p_DerivedSignals = new List<string[]>();

    private bool p_PreviewUseAbsoluteTime = false;
    private DateTime p_PreviewViewStartTime = new DateTime();
    private int p_PreviewViewStartRecord = 0;
    private int p_PreviewViewDuration = 0;
    private PlotModel p_PreviewSignalPlot = null;
    private bool p_PreviewNavigationEnabled = false;

    // Loaded EDF Structure and File Name
    public EDFFile LoadedEDFFile
    {
      get
      {
        return p_LoadedEDFFile;
      }
      set
      {
        p_LoadedEDFFile = value;
        PreviewCurrentCategory = -1;

        // Header
        OnPropertyChanged(nameof(EDFStartTime));
        OnPropertyChanged(nameof(EDFEndTime));
        OnPropertyChanged(nameof(EDFPatientName));
        OnPropertyChanged(nameof(EDFPatientSex));
        OnPropertyChanged(nameof(EDFPatientCode));
        OnPropertyChanged(nameof(EDFPatientBirthDate));
        OnPropertyChanged(nameof(EDFRecordEquipment));
        OnPropertyChanged(nameof(EDFRecordCode));
        OnPropertyChanged(nameof(EDFRecordTechnician));
        OnPropertyChanged(nameof(EDFAllSignals));

        // Preview Time Picker
        OnPropertyChanged(nameof(PreviewNavigationEnabled));
        if (value == null)
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
          PreviewViewStartRecord = 0;
          PreviewViewDuration = 5;
        }

        // Misc
        OnPropertyChanged(nameof(IsEDFLoaded));
      }
    }
    public string LoadedEDFFileName
    {
      get
      {
        return p_LoadedEDFFileName ?? "No File Loaded";
      }
      set
      {
        p_LoadedEDFFileName = value;
        OnPropertyChanged(nameof(LoadedEDFFileName));
      }
    }
    public bool IsEDFLoaded
    {
      get
      {
        return LoadedEDFFile != null;
      }
    }

    // Recent File List and Functions. 
    public ReadOnlyCollection<string> RecentFiles
    {
      get
      {
        string[] value = null;

        if (File.Exists("recent.txt"))
        {
          StreamReader sr = new StreamReader("recent.txt");
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
      List<string> array = RecentFiles.ToArray().ToList();
      array.Insert(0, path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }
    public void RecentFiles_Remove(string path)
    {
      List<string> array = RecentFiles.ToArray().ToList();
      array.Remove(path);
      array = array.Distinct().ToList();

      StreamWriter sw = new StreamWriter("recent.txt");
      for (int x = 0; x < array.Count; x++)
      {
        sw.WriteLine(array[x]);
      }
      sw.Close();

      p_window.LoadRecent();
    }

    // EDF Header
    public string EDFStartTime
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.StartDateTime.ToString();
        else
          return null;
      }
    }
    public string EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
          return (LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)).ToString();
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

    // Preview Category Management
    public void LoadCategoriesFile()
    {
      p_SignalCategories.Clear();
      p_SignalCategoryContents.Clear();

      if (File.Exists("signal_categories.txt"))
      {
        StreamReader sr = new StreamReader("signal_categories.txt");
        string[] text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        for (int x = 0; x < text.Length; x++)
        {
          string line = text[x];

          string category = line.Split(',')[0].Trim();
          List<string> category_signals = new List<string>();

          for (int y = 0; y < line.Split(',').Length; y++)
          {
            if (EDFAllSignals.Contains(line.Split(',')[y].Trim()) || p_DerivedSignals.Find(temp => temp[0].Trim() == line.Split(',')[y].Trim()) != null)
            {
              category_signals.Add(line.Split(',')[y]);
            }
          }

          if (category_signals.Count > 0)
          {
            p_SignalCategories.Add((p_SignalCategories.Count + 1) + ". " + category);
            p_SignalCategoryContents.Add(category_signals);
          }
        }

        sr.Close();
      }
    }
    public void WriteToCategoriesFile()
    {
      List<string> temp_SignalCategories = new List<string>();
      List<List<string>> temp_SignalCategoriesContents = new List<List<string>>();

      if (File.Exists("signal_categories.txt"))
      {
        StreamReader sr = new StreamReader("signal_categories.txt");
        string[] text = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n');

        for (int x = 0; x < text.Length; x++)
        {
          string line = text[x];

          string category = line.Split(',')[0].Trim();
          List<string> category_signals = new List<string>();

          for (int y = 1; y < line.Split(',').Length; y++)
          {
            category_signals.Add(line.Split(',')[y]);
          }

          temp_SignalCategories.Add(category);
          temp_SignalCategoriesContents.Add(category_signals);
        }

        sr.Close();
      }

      for (int x = 0; x < p_SignalCategories.Count; x++)
      {
        if (temp_SignalCategories.Contains(p_SignalCategories[x].Substring(p_SignalCategories[x].IndexOf('.') + 2).Trim()))
        {
          int u = temp_SignalCategories.IndexOf(p_SignalCategories[x].Substring(p_SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents[u].AddRange(p_SignalCategoryContents[x].ToArray());
          temp_SignalCategoriesContents[u] = temp_SignalCategoriesContents[u].Distinct().ToList();
        }
        else
        {
          temp_SignalCategories.Add(p_SignalCategories[x].Substring(p_SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents.Add(p_SignalCategoryContents[x]);
        }
      }

      StreamWriter sw = new StreamWriter("signal_categories.txt");
      for (int x = 0; x < temp_SignalCategories.Count; x++)
      {
        string line = temp_SignalCategories[x].Trim();
        if (line.Trim() != "")
        {
          for (int y = 0; y < temp_SignalCategoriesContents[x].Count; y++)
            line += "," + temp_SignalCategoriesContents[x][y].Trim();

          sw.WriteLine(line);
        }
      }
      sw.Close();
    }
    public void ManageCategories()
    {
      Dialog_Manage_Categories dlg = new Dialog_Manage_Categories(p_SignalCategories.ToArray(), p_SignalCategoryContents.Select(temp => temp.ToArray()).ToArray(), LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray(), p_DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
      dlg.ShowDialog();

      PreviewCurrentCategory = -1;
      p_SignalCategories = dlg.categories.ToList();
      p_SignalCategoryContents = dlg.categories_signals;
    }
    public void NextCategory()
    {
      if (PreviewCurrentCategory == p_SignalCategories.Count - 1)
        PreviewCurrentCategory = -1;
      else
        PreviewCurrentCategory++;
    }
    public void PreviousCategory()
    {
      if (PreviewCurrentCategory == -1)
        PreviewCurrentCategory = p_SignalCategories.Count - 1;
      else
        PreviewCurrentCategory--;
    }

    // Preview Derivative Management
    private void LoadCommonDerivativesFile()
    {
      p_DerivedSignals.Clear();
      if (File.Exists("common_derivatives.txt"))
      {
        List<string> text = new StreamReader("common_derivatives.txt").ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        for (int x = 0; x < text.Count; x++)
        {
          string[] new_entry = text[x].Split(',');

          if (new_entry.Length == 3)
          {
            if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[1].Trim()) != null) // Signals Exist
            {
              if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[2].Trim()) != null) // Signals Exist
              {
                if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[0].Trim()) == null) // Unique Name
                {
                  if (p_DerivedSignals.Where(temp => temp[0].Trim() == new_entry[0].Trim()).ToList().Count == 0) // Unique Name
                  {
                    p_DerivedSignals.Add(new_entry);
                  }
                }
              }
            }
          }
        }
      }
      OnPropertyChanged(nameof(PreviewSignals));
    }
    private void AddToCommonDerivativesFile(string name, string signal1, string signal2)
    {
      StreamWriter sw = new StreamWriter("common_derivatives.txt", true);
      sw.WriteLine(name + "," + signal1 + "," + signal2);
      sw.Close();
    }
    private void RemoveFromCommonDerivativesFile(List<string[]> signals)
    {
      if (File.Exists("common_derivatives.txt"))
      {
        StreamReader sr = new StreamReader("common_derivatives.txt");
        List<string> text = sr.ReadToEnd().Split('\n').ToList();
        sr.Close();
        for (int x = 0; x < text.Count; x++)
        {
          for (int y = 0; y < signals.Count; y++)
          {
            if (text[x].Split(',').Length != 3 || text[x].Split(',')[0].Trim() == signals[y][0].Trim() && text[x].Split(',')[1].Trim() == signals[y][1].Trim() && text[x].Split(',')[2].Trim() == signals[y][2].Trim())
            {
              text.Remove(text[x]);
              x--;
            }
          }
        }

        StreamWriter sw = new StreamWriter("common_derivatives.txt");
        for (int x = 0; x < text.Count; x++)
        {
          sw.WriteLine(text[x].Trim());
        }
        sw.Close();
      }
    }
    public void AddDerivative()
    {
      Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.Trim()).ToArray(), p_DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
      dlg.ShowDialog();

      if (dlg.DialogResult == true)
      {
        p_DerivedSignals.Add(new string[] { dlg.SignalName, dlg.Signal1, dlg.Signal2 });
        AddToCommonDerivativesFile(dlg.SignalName, dlg.Signal1, dlg.Signal2);
      }

      OnPropertyChanged(nameof(PreviewSignals));
    }
    public void RemoveDerivative()
    {
      Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(p_DerivedSignals.ToArray());
      dlg.ShowDialog();

      if (dlg.DialogResult == true)
      {
        for (int x = 0; x < dlg.RemovedSignals.Length; x++)
        {
          List<string[]> RemovedDerivatives = p_DerivedSignals.FindAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim()).ToList();
          p_DerivedSignals.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
          RemoveFromCommonDerivativesFile(RemovedDerivatives);

          if (p_PreviewSelectedSignals.Contains(dlg.RemovedSignals[x].Trim()))
          {
            p_PreviewSelectedSignals.Remove(dlg.RemovedSignals[x].Trim());
          }
        }
      }

      OnPropertyChanged(nameof(PreviewSignals));
    }

    // Preview Signal Selection
    public int PreviewCurrentCategory
    {
      get
      {
        return p_PreviewCurrentCategory;
      }
      set
      {
        p_PreviewCurrentCategory = value;
        OnPropertyChanged(nameof(PreviewCurrentCategory));
        OnPropertyChanged(nameof(PreviewCurrentCategoryName));
        OnPropertyChanged(nameof(PreviewSignals));
      }
    }
    public string PreviewCurrentCategoryName
    {
      get
      {
        if (PreviewCurrentCategory == -1)
          return "All";
        else
          return p_SignalCategories[PreviewCurrentCategory];
      }
    }
    public ReadOnlyCollection<string> PreviewSignals
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewCurrentCategory != -1)
            return Array.AsReadOnly(p_SignalCategoryContents[PreviewCurrentCategory].ToArray());
          else
          {
            List<string> output = new List<string>();
            output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray());
            output.AddRange(p_DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
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
      p_PreviewSelectedSignals.Clear();
      for (int x = 0; x < SelectedItems.Count; x++)
        p_PreviewSelectedSignals.Add(SelectedItems[x].ToString());

      DrawChart();
    }

    // Preview Plot Range
    public bool PreviewUseAbsoluteTime
    {
      get
      {
        return p_PreviewUseAbsoluteTime;
      }
      set
      {
        p_PreviewUseAbsoluteTime = value;

        OnPropertyChanged(nameof(PreviewUseAbsoluteTime));
        OnPropertyChanged(nameof(PreviewViewDuration));

        OnPropertyChanged(nameof(PreviewViewStartTimeMax));
        OnPropertyChanged(nameof(PreviewViewStartTimeMin));
        OnPropertyChanged(nameof(PreviewViewStartRecordMax));
        OnPropertyChanged(nameof(PreviewViewStartRecordMin));
        OnPropertyChanged(nameof(PreviewViewDurationMax));
        OnPropertyChanged(nameof(PreviewViewDurationMin));

        DrawChart();
      }
    }
    public DateTime? PreviewViewStartTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return p_PreviewViewStartTime;
          else
            return EpochtoDateTime(p_PreviewViewStartRecord, LoadedEDFFile);
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
          p_PreviewViewStartTime = value ?? new DateTime();
          p_PreviewViewStartRecord = DateTimetoEpoch(p_PreviewViewStartTime, LoadedEDFFile);

          OnPropertyChanged(nameof(PreviewViewStartRecord));
          OnPropertyChanged(nameof(PreviewViewStartTime));

          OnPropertyChanged(nameof(PreviewViewStartTimeMax));
          OnPropertyChanged(nameof(PreviewViewStartTimeMin));
          OnPropertyChanged(nameof(PreviewViewStartRecordMax));
          OnPropertyChanged(nameof(PreviewViewStartRecordMin));
          OnPropertyChanged(nameof(PreviewViewDurationMax));
          OnPropertyChanged(nameof(PreviewViewDurationMin));

          DrawChart();
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
            return DateTimetoEpoch(PreviewViewStartTime ?? new DateTime(), LoadedEDFFile);
          else
            return p_PreviewViewStartRecord;
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
          p_PreviewViewStartRecord = value ?? 0;
          p_PreviewViewStartTime = EpochtoDateTime(p_PreviewViewStartRecord, LoadedEDFFile);

          OnPropertyChanged(nameof(PreviewViewStartRecord));
          OnPropertyChanged(nameof(PreviewViewStartTime));

          OnPropertyChanged(nameof(PreviewViewStartTimeMax));
          OnPropertyChanged(nameof(PreviewViewStartTimeMin));
          OnPropertyChanged(nameof(PreviewViewStartRecordMax));
          OnPropertyChanged(nameof(PreviewViewStartRecordMin));
          OnPropertyChanged(nameof(PreviewViewDurationMax));
          OnPropertyChanged(nameof(PreviewViewDurationMin));

          DrawChart();
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
            return p_PreviewViewDuration;
          else
            return TimeSpantoEpochPeriod(new TimeSpan(0, 0, p_PreviewViewDuration));
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
            p_PreviewViewDuration = value ?? 0;
          else
            p_PreviewViewDuration = (int)EpochPeriodtoTimeSpan((value ?? 0)).TotalSeconds;
        }

        OnPropertyChanged(nameof(PreviewViewDuration));

        OnPropertyChanged(nameof(PreviewViewStartTimeMax));
        OnPropertyChanged(nameof(PreviewViewStartTimeMin));
        OnPropertyChanged(nameof(PreviewViewStartRecordMax));
        OnPropertyChanged(nameof(PreviewViewStartRecordMin));
        OnPropertyChanged(nameof(PreviewViewDurationMax));
        OnPropertyChanged(nameof(PreviewViewDurationMin));

        DrawChart();
      }
    }
    public DateTime PreviewViewEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          if (PreviewUseAbsoluteTime)
            return (PreviewViewStartTime ?? new DateTime()) + new TimeSpan(0, 0, 0, PreviewViewDuration ?? 0);
          else
            return (PreviewViewStartTime ?? new DateTime()) + EpochPeriodtoTimeSpan(PreviewViewDuration ?? 0);
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
          return LoadedEDFFile.Header.StartDateTime // Start Time
              + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds) // Total Duration
              - new TimeSpan(0, 0, p_PreviewViewDuration); // View Duration
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
          return DateTimetoEpoch(PreviewViewStartTimeMax, LoadedEDFFile); // PreviewViewStartTimeMax to Record
        else
          return 0;
      }
    }
    public int PreviewViewStartRecordMin
    {
      get
      {
        return 0; // Record 0
      }
    }
    public int PreviewViewDurationMax
    {
      get
      {
        if (LoadedEDFFile != null) // File Loaded
        {
          if (p_PreviewUseAbsoluteTime)
            return Math.Min(
                2 * 60 * 60, 
                (int)((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)) - (PreviewViewStartTime ?? new DateTime())).TotalSeconds
                );
          else
            return Math.Min(
                (int)((2 * 60 * 60) / ((double)EPOCH_SEC)), 
                DateTimetoEpoch((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)), LoadedEDFFile) - DateTimetoEpoch((PreviewViewStartTime ?? new DateTime()), LoadedEDFFile)
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
          return p_PreviewNavigationEnabled;
      }
      set
      {
        p_PreviewNavigationEnabled = value;
        OnPropertyChanged(nameof(PreviewNavigationEnabled));
      }
    }

    // Preview Plot
    public PlotModel PreviewSignalPlot
    {
      get
      {
        return p_PreviewSignalPlot;
      }
      set
      {
        p_PreviewSignalPlot = value;
        OnPropertyChanged(nameof(PreviewSignalPlot));
      }
    }

    // Export Previewed/Selected Signals Wizard
    public void ExportSignals()
    {
      if (p_PreviewSelectedSignals.Count > 0)
      {
        Dialog_Export_Previewed_Signals dlg = new Dialog_Export_Previewed_Signals(p_PreviewSelectedSignals);
        if (dlg.ShowDialog() == true)
        {
          List<ExportSignalModel> signals_to_export = new List<ExportSignalModel>(Dialog_Export_Previewed_Signals.signals_to_export);

          /*Processing proc = new Processing();
          MWArray[] input = new MWArray[2];
          input[0] = new MWNumericArray(LoadedEDFFile.retrieveSignalSampleValues(edfsignal2).ToArray());
          input[1] = edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord;

          values1 = LoadedEDFFile.retrieveSignalSampleValues(edfsignal1);
          values2 = (
                      (double[])(
                          (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                      ).ToVector(MWArrayComponent.Real)
                    ).ToList().Select(temp => (float)temp).ToList();

          sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;*/
        }
      }
      else {
        p_window.ShowMessageAsync("Error", "Please select at least one signal from the preview.");
      }
    }

    // INotify Interface
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public ModelView(MainWindow i_window)
    {
      p_window = i_window;
    }
  }
}
