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

namespace SleepApneaDiagnoser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Model model;
        
        public MainWindow()
        {
            InitializeComponent();

            model = new Model();
            model.EDF_Loaded += EDFLoaded;
            model.Preview_Chart_Drawn += PreviewChartDrawn;

            LoadRecent();
        }
        
        // Model Events
        public void EDFLoaded()
        {
            LoadRecent();

            TextBlock_Status.Text = "Waiting";
            this.IsEnabled = true;

            textBox_StartTime.Text = model.StudyStartTime.ToString();
            textBox_EndTime.Text = model.StudyEndTime.ToString();
            textBox_TimeRecord.Text = model.edfFile.Header.DurationOfDataRecordInSeconds.ToString();
            textBox_NumRecords.Text = model.edfFile.Header.NumberOfDataRecords.ToString();

            textBox_PI_Name.Text = model.edfFile.Header.PatientIdentification.PatientName;
            textBox_PI_Sex.Text = model.edfFile.Header.PatientIdentification.PatientSex;
            textBox_PI_Code.Text = model.edfFile.Header.PatientIdentification.PatientCode;
            textBox_PI_Birthdate.Text = model.edfFile.Header.PatientIdentification.PatientBirthDate.ToString();

            textBox_RI_Equipment.Text = model.edfFile.Header.RecordingIdentification.RecordingEquipment;
            textBox_RI_Code.Text = model.edfFile.Header.RecordingIdentification.RecordingCode;
            textBox_RI_Technician.Text = model.edfFile.Header.RecordingIdentification.RecordingTechnician;

            toggleButton_UseAbsoluteTime.IsChecked = false;

            timePicker_From_Abs.Value = model.StudyStartTime;
            timePicker_From_Eph.Value = HelperFunctions.DateTimetoEpoch(model.StudyStartTime, model.edfFile);
            timePicker_Period.Value = 5;

            listBox_SignalSelect.Items.Clear();
            comboBox_SignalSelect.Items.Clear();
            foreach (EDFSignal signal in model.edfFile.Header.Signals)
            {
                listBox_SignalSelect.Items.Add(signal);
                comboBox_SignalSelect.Items.Add(signal);
            }

            PlotView_signalPlot.Model = null;
        }
        public void PreviewChartDrawn()
        {
            PlotView_signalPlot.Model = model.signalPlot;
            EnableNavigation();
        }

        // Home Page Helper Functions
        public void LoadRecent()
        {
            List<string> array = model.RecentFiles.ToArray().ToList();

            itemControl_RecentEDF.Items.Clear();
            for (int x = 0; x < array.Count; x++)
                if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
                    itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
        }
        
        // Preview Helper Functions
        public void DisableNavigation()
        {
            timePicker_From_Abs.IsEnabled = false;
            timePicker_From_Eph.IsEnabled = false;
            timePicker_Period.IsEnabled = false;
            listBox_SignalSelect.IsEnabled = false;
        }
        public void EnableNavigation()
        {
            timePicker_From_Abs.IsEnabled = true;
            timePicker_From_Eph.IsEnabled = true;
            timePicker_Period.IsEnabled = true;
            listBox_SignalSelect.IsEnabled = true;
        }
       
        #region Menu Bar Events
        private void MenuItem_File_Open_Click(object sender, RoutedEventArgs e)
        {
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

            textBox_StartTime.Text = "";
            textBox_EndTime.Text = "";
            textBox_TimeRecord.Text = "";
            textBox_NumRecords.Text = "";

            textBox_StartTime.Text = "";
            textBox_EndTime.Text = "";

            textBox_PI_Name.Text = "";
            textBox_PI_Sex.Text = "";
            textBox_PI_Code.Text = "";
            textBox_PI_Birthdate.Text = "";

            textBox_RI_Equipment.Text = "";
            textBox_RI_Code.Text = "";
            textBox_RI_Technician.Text = "";

            timePicker_From_Abs.Value = null;
            timePicker_Period.Value = null;

            comboBox_SignalSelect.Items.Clear();
            listBox_SignalSelect.Items.Clear();
            PlotView_signalPlot.Model = null;
        }
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region Home Tab Events
        private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
        {
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
            List<string> array = model.RecentFiles.ToArray().ToList();
            List<string> selected = array.Where(temp => temp.Split('\\')[temp.Split('\\').Length - 1] == ((Hyperlink)sender).Inlines.FirstInline.DataContext.ToString()).ToList();
            
            if (selected.Count == 0)
            {
                MessageBox.Show("Error");
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
                        LoadRecent();
                    }
                }
            }
        }
        #endregion
                
        #region Preview Tab Events
        private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            model.SelectedSignals.Clear();
            for (int x = 0; x < listBox_SignalSelect.SelectedItems.Count; x++)
                model.SelectedSignals.Add(listBox_SignalSelect.SelectedItems[x].ToString());

            DisableNavigation();
            model.DrawChart();
        }
        private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_SignalSelect.SelectedValue != null)
            {
                EDFSignal edfsignal = model.edfFile.Header.Signals.Find(temp => temp.ToString() == comboBox_SignalSelect.SelectedValue.ToString());
                textBox_SampRecord.Text = edfsignal.NumberOfSamplesPerDataRecord.ToString();
            }
            else
            {
                textBox_SampRecord.Text = "";
            }
        }

        private void timePicker_From_Abs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
                ((Xceed.Wpf.Toolkit.DateTimeUpDown)sender).Value = (DateTime)e.OldValue;
            
            if (model.UseAbsoluteTime)
            {
                model.SetViewStartTime(timePicker_From_Abs.Value);
                model.SetViewPeriod(timePicker_Period.Value);

                timePicker_From_Eph.Value = model.GetViewStartEpoch();
                timePicker_Period.Minimum = model.GetViewPeriodMin();
                timePicker_Period.Maximum = model.GetViewPeriodMax();
                timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
                timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
                timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
                timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();

                DisableNavigation();
                model.DrawChart();
            }
        }
        private void timePicker_From_Eph_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
                ((Xceed.Wpf.Toolkit.DateTimeUpDown)sender).Value = (DateTime)e.OldValue;

            if (!model.UseAbsoluteTime)
            {
                model.SetViewStartEpoch(timePicker_From_Eph.Value);
                model.SetViewPeriod(timePicker_Period.Value);

                timePicker_From_Abs.Value = model.GetViewStartTime();
                timePicker_Period.Minimum = model.GetViewPeriodMin();
                timePicker_Period.Maximum = model.GetViewPeriodMax();
                timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
                timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
                timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
                timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();

                DisableNavigation();
                model.DrawChart();
            }
        }
        private void timePicker_Period_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            model.SetViewPeriod(timePicker_Period.Value);

            timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
            timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
            timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
            timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();

            DisableNavigation();
            model.DrawChart();
        }

        private void toggleButton_UseAbsoluteTime_Checked(object sender, RoutedEventArgs e)
        {
            model.UseAbsoluteTime = true;

            timePicker_Period.Minimum = model.GetViewPeriodMin();
            timePicker_Period.Maximum = model.GetViewPeriodMax();
            timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
            timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
            timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
            timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();

            timePicker_Period.Value = (int)(model.GetViewEndTime() - model.GetViewStartTime()).TotalSeconds;

            timePicker_From_Abs.Visibility = Visibility.Visible;
            timePicker_From_Eph.Visibility = Visibility.Hidden;
        }
        private void toggleButton_UseAbsoluteTime_Unchecked(object sender, RoutedEventArgs e)
        {
            model.UseAbsoluteTime = false;

            timePicker_Period.Minimum = model.GetViewPeriodMin();
            timePicker_Period.Maximum = model.GetViewPeriodMax();
            timePicker_From_Abs.Minimum = model.GetViewStartPickerAbsMin();
            timePicker_From_Abs.Maximum = model.GetViewStartPickerAbsMax();
            timePicker_From_Eph.Minimum = model.GetViewStartPickerEphMin();
            timePicker_From_Eph.Maximum = model.GetViewStartPickerEphMax();

            timePicker_Period.Value = model.GetViewEndEpoch() - model.GetViewStartEpoch();

            timePicker_From_Abs.Visibility = Visibility.Hidden;
            timePicker_From_Eph.Visibility = Visibility.Visible;
        }
        #endregion
        
    }

    public static class HelperFunctions
    {
        public static DateTime EpochtoDateTime(int epoch, EDFFile file)
        {
            return file.Header.StartDateTime + new TimeSpan(0, 0, epoch * file.Header.DurationOfDataRecordInSeconds);
        }
        public static TimeSpan EpochPeriodtoTimeSpan(int period, EDFFile file)
        {
            return new TimeSpan(0, 0, 0, period * file.Header.DurationOfDataRecordInSeconds);
        }
        public static int DateTimetoEpoch(DateTime time, EDFFile file)
        {
            return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)file.Header.DurationOfDataRecordInSeconds);
        }
        public static int TimeSpantoEpochPeriod(TimeSpan period, EDFFile file)
        {
            return (int)(period.TotalSeconds / file.Header.DurationOfDataRecordInSeconds);
        }
    }

    public class Model
    {
        // Loaded EDF Structure and File Name
        public EDFFile edfFile = null;
        public string fileName = null;
        // Preview Graph
        public PlotModel signalPlot = null;
        // Time Range of Graph
        public bool UseAbsoluteTime = false;
        // Selected Signals to be Previewed
        public List<string> SelectedSignals = new List<string>();
        // View Range
        private DateTime ViewStartTime = new DateTime();
        private DateTime ViewEndTime = new DateTime();

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
        }
        
        public DateTime StudyStartTime
        {
            get
            {
                DateTime value = new DateTime();
                value = edfFile.Header.StartDateTime;
                return value;
            }
        }
        public DateTime StudyEndTime
        {
            get
            {
                DateTime value = new DateTime();
                value = edfFile.Header.StartDateTime + new TimeSpan(0, 0, edfFile.Header.NumberOfDataRecords * edfFile.Header.DurationOfDataRecordInSeconds);
                return value;
            }
        }
        public int EpochDuration
        {
            get
            {
                int value = 0;
                value = (edfFile == null ? 30 : edfFile.Header.DurationOfDataRecordInSeconds);
                return value;
            }
        }

        public void SetViewStartTime(DateTime? value)
        {
            ViewStartTime = value ?? StudyStartTime;
        }
        public void SetViewStartEpoch(int? value)
        {
            ViewStartTime = HelperFunctions.EpochtoDateTime(value ?? 0, edfFile);
        }
        public void SetViewPeriod(int? value)
        {
            if (UseAbsoluteTime)
                ViewEndTime = ViewStartTime + new TimeSpan(0, 0, 0, value ?? 0);
            else
                ViewEndTime = ViewStartTime + HelperFunctions.EpochPeriodtoTimeSpan(value ?? 0, edfFile);
        }

        public DateTime GetViewStartTime()
        {
            return ViewStartTime;
        }
        public int GetViewStartEpoch()
        {
            return HelperFunctions.DateTimetoEpoch(ViewStartTime, edfFile);
        }
        public DateTime GetViewEndTime()
        {
            return ViewEndTime;
        }
        public int GetViewEndEpoch()
        {
            return HelperFunctions.DateTimetoEpoch(ViewEndTime, edfFile);
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
                return HelperFunctions.DateTimetoEpoch(StudyStartTime, edfFile);
            else
                return 0;
        }
        public int GetViewStartPickerEphMax()
        {
            if (edfFile != null)
                return HelperFunctions.DateTimetoEpoch(StudyEndTime - (ViewEndTime - ViewStartTime), edfFile);
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
                    return Math.Min((2 * 60 * 60) / EpochDuration, HelperFunctions.DateTimetoEpoch(StudyEndTime, edfFile) - HelperFunctions.DateTimetoEpoch(ViewStartTime, edfFile));
            }
            else // No File Loaded
                return 0;
        }
                
        // Load EDF
        private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            edfFile = new EDFFile();
            edfFile.readFile(e.Argument.ToString());
        }
        private void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            RecentFiles_Add(fileName);
            EDF_Loaded();
        }
        public void LoadEDFFile(string fileNameIn)
        {
            fileName = fileNameIn;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_LoadEDFFile;
            bw.RunWorkerCompleted += BW_FinishLoad;
            bw.RunWorkerAsync(fileName);
        }

        // Create Chart
        private void BW_CreateChart(object sender, DoWorkEventArgs e)
        {
            signalPlot = new PlotModel();
            signalPlot.Series.Clear();
            signalPlot.Axes.Clear();

            if (SelectedSignals.Count > 0)
            {
                DateTimeAxis xAxis = new DateTimeAxis();
                xAxis.Key = "DateTime";
                xAxis.Minimum = DateTimeAxis.ToDouble(ViewStartTime);
                xAxis.Maximum = DateTimeAxis.ToDouble(ViewEndTime);
                signalPlot.Axes.Add(xAxis);

                for (int x = 0; x < SelectedSignals.Count; x++)
                {
                    EDFSignal edfsignal = edfFile.Header.Signals.Find(temp => temp.ToString() == SelectedSignals[x]);
                    float sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

                    List<float> values = edfFile.retrieveSignalSampleValues(edfsignal);

                    int startIndex, indexCount;
                    TimeSpan startPoint = ViewStartTime - StudyStartTime;
                    TimeSpan duration = ViewEndTime - ViewStartTime;
                    startIndex = (int)(startPoint.TotalSeconds / sample_period);
                    indexCount = (int)(duration.TotalSeconds / sample_period);

                    LineSeries series = new LineSeries();
                    for (int y = startIndex; y < indexCount + startIndex; y++)
                    {
                        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(edfFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
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

                    signalPlot.Axes.Add(yAxis);
                    signalPlot.Series.Add(series);
                }
            }
        }
        private void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
        {
            Preview_Chart_Drawn();
        }
        public void DrawChart()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }

        public Action EDF_Loaded;
        public Action Preview_Chart_Drawn;
        
        public Model()
        {

        }
    }
}
