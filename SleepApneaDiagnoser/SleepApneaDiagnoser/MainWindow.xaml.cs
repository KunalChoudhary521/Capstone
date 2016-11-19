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

namespace SleepApneaDiagnoser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Model model;

        /// <summary>
        /// Function called after new EDF file is loaded to populate right pane of Preview tab.
        /// </summary>
        private void EDFLoaded()
        {
            LoadRecent();
            this.IsEnabled = true;
            RefreshAll();
        }
        /// <summary>
        /// Function called to populate recent files list. Called when application is first loaded and if the recent files list changes.
        /// </summary>
        private void LoadRecent()
        {
            List<string> array = model.RecentFiles.ToArray().ToList();

            itemControl_RecentEDF.Items.Clear();
            for (int x = 0; x < array.Count; x++)
                if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
                    itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
        }
        
        private void RefreshEDFHeaderInfo()
        {
            if (model.edfFile != null)
            {
                TextBlock_Status.Text = "File: " + model.FileName;
                textBox_StartTime.Text = model.edfFile.Header.StartDateTime.ToString();
                textBox_EndTime.Text = (model.edfFile.Header.StartDateTime + new TimeSpan(0, 0, model.edfFile.Header.DurationOfDataRecordInSeconds * model.edfFile.Header.NumberOfDataRecords)).ToString();
                textBox_TimeRecord.Text = model.edfFile.Header.DurationOfDataRecordInSeconds.ToString();
                textBox_NumRecords.Text = model.edfFile.Header.NumberOfDataRecords.ToString();

                textBox_PI_Name.Text = model.edfFile.Header.PatientIdentification.PatientName;
                textBox_PI_Sex.Text = model.edfFile.Header.PatientIdentification.PatientSex;
                textBox_PI_Code.Text = model.edfFile.Header.PatientIdentification.PatientCode;
                textBox_PI_Birthdate.Text = model.edfFile.Header.PatientIdentification.PatientBirthDate.ToString();

                textBox_RI_Equipment.Text = model.edfFile.Header.RecordingIdentification.RecordingEquipment;
                textBox_RI_Code.Text = model.edfFile.Header.RecordingIdentification.RecordingCode;
                textBox_RI_Technician.Text = model.edfFile.Header.RecordingIdentification.RecordingTechnician;

                comboBox_SignalSelect.Items.Clear();
                foreach (EDFSignal signal in model.edfFile.Header.Signals)
                {
                    comboBox_SignalSelect.Items.Add(signal.Label);
                }
            }
            else
            {
                TextBlock_Status.Text = "Waiting";
                textBox_StartTime.Text = "";
                textBox_EndTime.Text = "";
                textBox_TimeRecord.Text = "";
                textBox_NumRecords.Text = "";

                textBox_PI_Name.Text = "";
                textBox_PI_Sex.Text = "";
                textBox_PI_Code.Text = "";
                textBox_PI_Birthdate.Text = "";

                textBox_RI_Equipment.Text = "";
                textBox_RI_Code.Text = "";
                textBox_RI_Technician.Text = "";

                comboBox_SignalSelect.Items.Clear();
            }
        }
        private void RefreshPreviewTimePicker()
        {
            if (model.edfFile != null)
            {
                timePicker_From_Abs.IsEnabled = true;
                timePicker_From_Eph.IsEnabled = true;
                timePicker_Period.IsEnabled = true;
                toggleButton_UseAbsoluteTime.IsEnabled = true;

                timePicker_From_Abs.Value = model.edfFile.Header.StartDateTime;
                timePicker_From_Eph.Value = 0;
                timePicker_Period.Value = 5;
            }
            else
            {
                timePicker_From_Abs.IsEnabled = false;
                timePicker_From_Eph.IsEnabled = false;
                timePicker_Period.IsEnabled = false;
                toggleButton_UseAbsoluteTime.IsEnabled = false;

                timePicker_From_Abs.Value = null;
                timePicker_From_Eph.Value = null;
                timePicker_Period.Value = null;
            }
        }
        private void RefreshPreviewSignalSelector()
        {
            if (model.edfFile != null)
            {
                listBox_SignalSelect.Items.Clear();
                foreach (EDFSignal signal in model.edfFile.Header.Signals)
                {
                    listBox_SignalSelect.Items.Add(signal.Label);
                }
                for (int x = 0; x < model.GetDerivedSignals().Length; x++)
                {
                    listBox_SignalSelect.Items.Add(model.GetDerivedSignals()[x][0]);
                }

                button_AddDerivative.IsEnabled = true;
                button_RemoveDerivative.IsEnabled = true;
                button_Categories.IsEnabled = true;
                button_Next.IsEnabled = true;
                button_Prev.IsEnabled = true;
            }
            else
            {
                listBox_SignalSelect.Items.Clear();

                button_AddDerivative.IsEnabled = false;
                button_RemoveDerivative.IsEnabled = false;
                button_Categories.IsEnabled = false;
                button_Next.IsEnabled = false;
                button_Prev.IsEnabled = false;
            }
        }
        private void RefreshAll()
        {
            RefreshEDFHeaderInfo();
            RefreshPreviewTimePicker();
            RefreshPreviewSignalSelector();
        }
        
        /// <summary>
        /// Disables the time picker controls while the chart is being drawn. Called before drawing the chart.
        /// </summary>
        private void DisableNavigation()
        {
            timePicker_From_Abs.IsEnabled = false;
            timePicker_From_Eph.IsEnabled = false;
            timePicker_Period.IsEnabled = false;
            listBox_SignalSelect.IsEnabled = false;
        }
        /// <summary>
        /// Enables the time picker controls after the chart is drawn. Called in PreviewChartDrawn.
        /// </summary>
        private void EnableNavigation()
        {
            timePicker_From_Abs.IsEnabled = true;
            timePicker_From_Eph.IsEnabled = true;
            timePicker_Period.IsEnabled = true;
            listBox_SignalSelect.IsEnabled = true;
        }
        /// <summary>
        /// Updates GUI time picker controls from the model.
        /// </summary>
        private void GetTimePickerValues()
        {
            timePicker_From_Abs.ValueChanged -= timePicker_RangeChanged;
            timePicker_From_Eph.ValueChanged -= timePicker_RangeChanged;
            timePicker_Period.ValueChanged -= timePicker_RangeChanged;

            timePicker_From_Abs.Value = model.GetViewStartTime();
            timePicker_From_Eph.Value = model.GetViewStartEpoch();
            timePicker_Period.Value = model.GetViewPeriod();

            timePicker_From_Abs.ValueChanged += timePicker_RangeChanged;
            timePicker_From_Eph.ValueChanged += timePicker_RangeChanged;
            timePicker_Period.ValueChanged += timePicker_RangeChanged;

            timePicker_Period.Minimum = model.GetViewPeriodMin();
            timePicker_Period.Maximum = model.GetViewPeriodMax();
            timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
            timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
            timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
            timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();
        }
        /// <summary>
        /// Function called after chart in preview tab is drawn to add drawing to Preview tab.
        /// </summary>
        private void PreviewChartDrawn()
        {
            PlotView_signalPlot.Model = model.SignalPlot;
            EnableNavigation();
        }

        /// <summary>
        /// Constructor for GUI class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            model = new Model();
            model.EDFLoaded += EDFLoaded;
            model.PreviewChartDrawn += PreviewChartDrawn;
            model.RecentFilesChanged += LoadRecent;

            LoadRecent();
            RefreshAll();
        }

        // Menu Bar Events
        private void MenuItem_File_Open_Click(object sender, RoutedEventArgs e)
        {
            model.edfFile = null;
            RefreshAll();

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "EDF files (*.edf)|*.edf";
            dialog.Title = "Select an EDF file";

            if (dialog.ShowDialog() == true)
            {
                TextBlock_Status.Text = "Loading EDF File";
                this.IsEnabled = false;
                model.LoadEDFFile(dialog.FileName);
            }
        }
        private void MenuItem_File_Close_Click(object sender, RoutedEventArgs e)
        {
            model.edfFile = null;
            RefreshAll();
        }
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Home Tab Events
        private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
        {
            model.edfFile = null;
            RefreshAll();

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "EDF files (*.edf)|*.edf";
            dialog.Title = "Select an EDF file";

            if (dialog.ShowDialog() == true)
            {
                TextBlock_Status.Text = "Loading EDF File";
                this.IsEnabled = false;
                model.LoadEDFFile(dialog.FileName);
            }
        }
        private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
        {
            model.edfFile = null;
            RefreshAll();

            List<string> array = model.RecentFiles.ToArray().ToList();
            List<string> selected = array.Where(temp => temp.Split('\\')[temp.Split('\\').Length - 1] == ((Hyperlink)sender).Inlines.FirstInline.DataContext.ToString()).ToList();
            
            if (selected.Count == 0)
            {
                MessageBox.Show("File not Found");
                model.EDFLoaded();
            }
            else
            {
                for (int x = 0; x < selected.Count; x++)
                {
                    if (File.Exists(selected[x]))
                    {
                        TextBlock_Status.Text = "Loading EDF File";
                        this.IsEnabled = false;
                        model.LoadEDFFile(selected[x]);
                        break;
                    }
                    else
                    {
                        MessageBox.Show("File not Found");
                        model.RecentFiles_Remove(selected[x]);
                    }
                }
            }
        }
             
        // Preview Tab Events   
        private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            model.SetSelectedSignals(listBox_SignalSelect.SelectedItems);
            
            DisableNavigation();
            model.DrawChart();
        }
        private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_SignalSelect.SelectedValue != null)
            {
                EDFSignal edfsignal = model.edfFile.Header.Signals.Find(temp => temp.Label.Trim() == comboBox_SignalSelect.SelectedValue.ToString().Trim());
                textBox_SampRecord.Text = edfsignal.NumberOfSamplesPerDataRecord.ToString();
            }
            else
            {
                textBox_SampRecord.Text = "";
            }
        }
        private void timePicker_RangeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
            {
                model.SetViewRange(timePicker_From_Abs.Value, timePicker_Period.Value);
                model.SetViewRange(timePicker_From_Eph.Value, timePicker_Period.Value);

                GetTimePickerValues();

                DisableNavigation();
                model.DrawChart();
            }
        }
        private void toggleButton_UseAbsoluteTime_Checked(object sender, RoutedEventArgs e)
        {
            model.SetAbsoluteOrEpoch(true);

            GetTimePickerValues();

            timePicker_From_Abs.Visibility = Visibility.Visible;
            timePicker_From_Eph.Visibility = Visibility.Hidden;
        }
        private void toggleButton_UseAbsoluteTime_Unchecked(object sender, RoutedEventArgs e)
        {
            model.SetAbsoluteOrEpoch(false);

            GetTimePickerValues();

            timePicker_From_Abs.Visibility = Visibility.Hidden;
            timePicker_From_Eph.Visibility = Visibility.Visible;
        }

        private void button_AddDerivative_Click(object sender, RoutedEventArgs e)
        {
            string new_signal = model.Add_Derivative();
            if (new_signal != null)
            {
                listBox_SignalSelect.Items.Add(new_signal);
            }
        }
        private void button_RemoveDerivative_Click(object sender, RoutedEventArgs e)
        {
            string[] remove_signals = model.Remove_Derivative();
            if (remove_signals != null)
            {
                for (int y = 0; y < remove_signals.Length; y++)
                {
                    listBox_SignalSelect.Items.Remove(remove_signals[y]);
                }
            }
        }
        private void button_Categories_Click(object sender, RoutedEventArgs e)
        {
            Dialog_ManageCategories dlg = new Dialog_ManageCategories();
            dlg.ShowDialog();
        }
    }

    public class Model
    {
        private static DateTime EpochtoDateTime(int epoch, EDFFile file)
        {
            return file.Header.StartDateTime + new TimeSpan(0, 0, epoch * file.Header.DurationOfDataRecordInSeconds);
        }
        private static TimeSpan EpochPeriodtoTimeSpan(int period, EDFFile file)
        {
            return new TimeSpan(0, 0, 0, period * file.Header.DurationOfDataRecordInSeconds);
        }
        private static int DateTimetoEpoch(DateTime time, EDFFile file)
        {
            return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)file.Header.DurationOfDataRecordInSeconds);
        }
        private static int TimeSpantoEpochPeriod(TimeSpan period, EDFFile file)
        {
            return (int)(period.TotalSeconds / file.Header.DurationOfDataRecordInSeconds);
        }

        // Loaded EDF Structure and File Name
        public EDFFile edfFile = null;
        public string FileName = null;
        
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

            RecentFilesChanged();
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

            RecentFilesChanged();
        }

        // Preview Graph
        public PlotModel SignalPlot = null;
        
        // Selected Signals to be Previewed
        private List<string> SelectedSignals = new List<string>();

        // Created Derivatives
        private List<string[]> DerivedSignals = new List<string[]>();

        // Categories
        private List<string> SignalCategories = new List<string>();
        private List<List<string>> SignalCategoryContents = new List<List<string>>();

        // Preview Chart Ranges
        private bool UseAbsoluteTime = false;
        private DateTime StudyStartTime
        {
            get
            {
                DateTime value = new DateTime();
                value = edfFile.Header.StartDateTime;
                return value;
            }
        }
        private DateTime StudyEndTime
        {
            get
            {
                DateTime value = new DateTime();
                value = edfFile.Header.StartDateTime + new TimeSpan(0, 0, edfFile.Header.NumberOfDataRecords * edfFile.Header.DurationOfDataRecordInSeconds);
                return value;
            }
        }
        private DateTime ViewStartTime = new DateTime();
        private DateTime ViewEndTime = new DateTime();
        private int EpochDuration
        {
            get
            {
                int value = 0;
                value = (edfFile == null ? 30 : edfFile.Header.DurationOfDataRecordInSeconds);
                return value;
            }
        }
        
        public void SetSelectedSignals(System.Collections.IList SelectedItems)
        {
            SelectedSignals.Clear();
            for (int x = 0; x < SelectedItems.Count; x++)
                SelectedSignals.Add(SelectedItems[x].ToString());
        }

        public string[][] GetDerivedSignals()
        {
            return DerivedSignals.ToArray();
        }

        public void SetAbsoluteOrEpoch(bool value)
        {
            UseAbsoluteTime = value;
        }
        public void SetViewRange(DateTime? start, int? period)
        {
            if (UseAbsoluteTime)
            {
                ViewStartTime = start ?? StudyStartTime;
                ViewEndTime = ViewStartTime + new TimeSpan(0, 0, 0, period ?? 0);
            }
        }
        public void SetViewRange(int? start, int? period)
        {
            if (!UseAbsoluteTime)
            {
                ViewStartTime = EpochtoDateTime(start ?? 0, edfFile);
                ViewEndTime = ViewStartTime + EpochPeriodtoTimeSpan(period ?? 0, edfFile);
            }
        }

        public DateTime GetViewStartTime()
        {
            return ViewStartTime;
        }
        public DateTime GetViewEndTime()
        {
            return ViewEndTime;
        }
        public int GetViewStartEpoch()
        {
            return DateTimetoEpoch(ViewStartTime, edfFile);
        }
        public int GetViewEndEpoch()
        {
            return DateTimetoEpoch(ViewEndTime, edfFile);
        }
        public int GetViewPeriod()
        {
            if (UseAbsoluteTime)
                return (int)(ViewEndTime - ViewStartTime).TotalSeconds;
            else
                return TimeSpantoEpochPeriod(ViewEndTime - ViewStartTime, edfFile);
        }

        public DateTime GetViewStartPickerAbsMin()
        {
            if (edfFile != null)
                return StudyStartTime;
            else
                return new DateTime();
        }
        public DateTime GetViewStartPickerAbsMax()
        {
            if (edfFile != null)
                return StudyEndTime - (ViewEndTime - ViewStartTime);
            else
                return new DateTime();
        }
        public int GetViewStartPickerEphMin()
        {
            if (edfFile != null)
                return DateTimetoEpoch(StudyStartTime, edfFile);
            else
                return 0;
        }
        public int GetViewStartPickerEphMax()
        {
            if (edfFile != null)
                return DateTimetoEpoch(StudyEndTime - (ViewEndTime - ViewStartTime), edfFile);
            else
                return 0;
        }
        public int GetViewPeriodMin()
        {
            if (edfFile != null) // File Loaded
                return 1;
            else // No File Loaded
                return 0;
        }
        public int GetViewPeriodMax()
        {
            if (edfFile != null) // File Loaded
            {
                if (UseAbsoluteTime)
                    return Math.Min(2 * 60 * 60, (int)(StudyEndTime - ViewStartTime).TotalSeconds);
                else
                    return Math.Min((2 * 60 * 60) / EpochDuration, DateTimetoEpoch(StudyEndTime, edfFile) - DateTimetoEpoch(ViewStartTime, edfFile));
            }
            else // No File Loaded
                return 0;
        }
                
        // Load EDF
        private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            edfFile = new EDFFile();
            edfFile.readFile(e.Argument.ToString());

            LoadCommonDerivativesFile();
        }
        private void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            RecentFiles_Add(FileName);
            EDFLoaded();
        }
        public void LoadEDFFile(string fileNameIn)
        {
            FileName = fileNameIn;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_LoadEDFFile;
            bw.RunWorkerCompleted += BW_FinishLoad;
            bw.RunWorkerAsync(FileName);
        }

        // Create/Remove Derivative
        public void LoadCommonDerivativesFile()
        {
            DerivedSignals.Clear();
            if (File.Exists("common_derivatives.txt"))
            {
                List<string> text = new StreamReader("common_derivatives.txt").ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
                for (int x = 0; x < text.Count; x++)
                {
                    string[] new_entry = text[x].Split(',');

                    if (new_entry.Length == 3)
                    {
                        if (edfFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[1].Trim()) != null) // Signals Exist
                        {
                            if (edfFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[2].Trim()) != null) // Signals Exist
                            {
                                if (edfFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[0].Trim()) == null) // Unique Name
                                {
                                    if (DerivedSignals.Where(temp => temp[0].Trim() == new_entry[0].Trim()).ToList().Count == 0) // Unique Name
                                    {
                                        DerivedSignals.Add(new_entry);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void AddToCommonDerivativesFile(string name, string signal1, string signal2)
        {
            StreamWriter sw = new StreamWriter("common_derivatives.txt", true);
            sw.WriteLine(name + "," + signal1 + "," + signal2);
            sw.Close();
        }
        public void RemoveFromCommonDerivativesFile(List<string[]> signals)
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

        public string Add_Derivative()
        {
            Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(edfFile.Header.Signals.Select(temp => temp.Label.Trim()).ToArray(), DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
            dlg.ShowDialog();

            if (dlg.DialogResult == true)
            {
                DerivedSignals.Add(new string[] { dlg.SignalName, dlg.Signal1, dlg.Signal2 });
                AddToCommonDerivativesFile(dlg.SignalName, dlg.Signal1, dlg.Signal2);
                return dlg.SignalName;
            }

            return null;
        }
        public string[] Remove_Derivative()
        {
            Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(DerivedSignals.ToArray());
            dlg.ShowDialog();

            if (dlg.DialogResult == true)
            {
                for (int x = 0; x < dlg.RemovedSignals.Length; x++)
                {
                    List<string[]> RemovedDerivatives = DerivedSignals.FindAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim()).ToList();
                    DerivedSignals.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
                    RemoveFromCommonDerivativesFile(RemovedDerivatives);
                }

                return dlg.RemovedSignals;
            }

            return null;
        }

        // Create Chart
        private void BW_CreateChart(object sender, DoWorkEventArgs e)
        {
            SignalPlot = new PlotModel();
            SignalPlot.Series.Clear();
            SignalPlot.Axes.Clear();

            if (SelectedSignals.Count > 0)
            {
                DateTimeAxis xAxis = new DateTimeAxis();
                xAxis.Key = "DateTime";
                xAxis.Minimum = DateTimeAxis.ToDouble(ViewStartTime);
                xAxis.Maximum = DateTimeAxis.ToDouble(ViewEndTime);
                SignalPlot.Axes.Add(xAxis);

                for (int x = 0; x < SelectedSignals.Count; x++)
                {
                    LineSeries series = new LineSeries();
                    if (edfFile.Header.Signals.Find(temp => temp.Label.Trim() == SelectedSignals[x].Trim()) != null) // Normal EDF Signal
                    {
                        EDFSignal edfsignal = edfFile.Header.Signals.Find(temp => temp.Label.Trim() == SelectedSignals[x].Trim());

                        float sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

                        List<float> values = edfFile.retrieveSignalSampleValues(edfsignal);

                        int startIndex, indexCount;
                        TimeSpan startPoint = ViewStartTime - StudyStartTime;
                        TimeSpan duration = ViewEndTime - ViewStartTime;
                        startIndex = (int)(startPoint.TotalSeconds / sample_period);
                        indexCount = (int)(duration.TotalSeconds / sample_period);
                        
                        for (int y = startIndex; y < indexCount + startIndex; y++)
                        {
                            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(edfFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
                        }
                    }
                    else // Derivative Signal
                    {
                        string[] deriv_info = DerivedSignals.Find(temp => temp[0] == SelectedSignals[x]);
                        EDFSignal edfsignal1 = edfFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[1].Trim());
                        EDFSignal edfsignal2 = edfFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[2].Trim());

                        List<float> values1;
                        List<float> values2;
                        float sample_period;
                        if (edfsignal1.NumberOfSamplesPerDataRecord == edfsignal2.NumberOfSamplesPerDataRecord) // No resampling
                        {
                            values1 = edfFile.retrieveSignalSampleValues(edfsignal1);
                            values2 = edfFile.retrieveSignalSampleValues(edfsignal2);
                            sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
                        }
                        else if (edfsignal1.NumberOfSamplesPerDataRecord > edfsignal2.NumberOfSamplesPerDataRecord) // Upsample signal 2
                        {
                            Processing proc = new Processing();
                            MWArray[] input = new MWArray[2];
                            input[0] = new MWNumericArray(edfFile.retrieveSignalSampleValues(edfsignal2).ToArray());
                            input[1] = edfsignal1.NumberOfSamplesPerDataRecord / edfsignal2.NumberOfSamplesPerDataRecord;

                            values1 = edfFile.retrieveSignalSampleValues(edfsignal1);
                            values2 = (
                                        (double[])(
                                            (MWNumericArray) proc.m_resample(1, input[0], input[1])[0]
                                        ).ToVector(MWArrayComponent.Real)
                                      ).ToList().Select(temp => (float)temp).ToList();

                            sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal1.NumberOfSamplesPerDataRecord;
                        }
                        else // Upsample signal 1
                        {
                            Processing proc = new Processing();
                            MWArray[] input = new MWArray[2];
                            input[0] = new MWNumericArray(edfFile.retrieveSignalSampleValues(edfsignal1).ToArray());
                            input[1] = edfsignal2.NumberOfSamplesPerDataRecord / edfsignal1.NumberOfSamplesPerDataRecord;

                            values1 = (
                                        (double[])(
                                            (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                                        ).ToVector(MWArrayComponent.Real)
                                      ).ToList().Select(temp => (float)temp).ToList();
                            values2 = edfFile.retrieveSignalSampleValues(edfsignal2);

                            sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal2.NumberOfSamplesPerDataRecord;
                        }

                        int startIndex, indexCount;
                        TimeSpan startPoint = ViewStartTime - StudyStartTime;
                        TimeSpan duration = ViewEndTime - ViewStartTime;
                        startIndex = (int)(startPoint.TotalSeconds / sample_period);
                        indexCount = (int)(duration.TotalSeconds / sample_period);

                        for (int y = startIndex; y < indexCount + startIndex; y++)
                        {
                            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(edfFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values1[y] - values2[y]));
                        }
                    }

                    series.YAxisKey = SelectedSignals[x];
                    series.XAxisKey = "DateTime";

                    LinearAxis yAxis = new LinearAxis();
                    yAxis.MajorGridlineStyle = LineStyle.Solid;
                    yAxis.MinorGridlineStyle = LineStyle.Dot;
                    yAxis.Title = SelectedSignals[x];
                    yAxis.Key = SelectedSignals[x];
                    yAxis.EndPosition = (double)1 - (double)x * ((double)1 / (double)SelectedSignals.Count);
                    yAxis.StartPosition = (double)1 - (double)(x + 1) * ((double)1 / (double)SelectedSignals.Count);

                    SignalPlot.Axes.Add(yAxis);
                    SignalPlot.Series.Add(series);
                }
            }
        }
        private void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
        {
            PreviewChartDrawn();
        }
        public void DrawChart()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }

        public Action EDFLoaded;
        public Action RecentFilesChanged;
        public Action PreviewChartDrawn;
        
        public Model()
        {

        }
    }
}
