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
    public partial class MainWindow : Window
    {
        ModelView model;

        /// <summary>
        /// Function called after new EDF file is loaded to populate right pane of Preview tab.
        /// </summary>
        public void EDFLoaded()
        {
            LoadRecent();
            this.IsEnabled = true;
            RefreshAll();
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
        /// Function called after chart in preview tab is drawn to add drawing to Preview tab.
        /// </summary>
        public void PreviewChartDrawn()
        {
            PlotView_signalPlot.Model = model.PreviewSignalPlot;
            EnableNavigation();
        }
        
        private void RefreshPreviewTimePicker()
        {
            if (model.LoadedEDFFile != null)
            {
                timePicker_From_Abs.IsEnabled = true;
                timePicker_From_Eph.IsEnabled = true;
                timePicker_Period.IsEnabled = true;
                toggleButton_UseAbsoluteTime.IsEnabled = true;

                timePicker_From_Abs.Value = model.LoadedEDFFile.Header.StartDateTime;
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
        private void RefreshAll()
        {
            RefreshPreviewTimePicker();
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
        /// Updates GUI time picker controls from the 
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
        /// Constructor for GUI class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            model = new ModelView(this);
            this.DataContext = model;
            LoadRecent();
            RefreshAll();
        }

        // Menu Bar Events
        private void MenuItem_File_Open_Click(object sender, RoutedEventArgs e)
        {
            model.LoadedEDFFile = null;
            RefreshAll();

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "EDF files (*.edf)|*.edf";
            dialog.Title = "Select an EDF file";

            if (dialog.ShowDialog() == true)
            {
                this.IsEnabled = false;
                model.LoadEDFFile(dialog.FileName);
            }
        }
        private void MenuItem_File_Close_Click(object sender, RoutedEventArgs e)
        {
            model.LoadedEDFFile = null;
            RefreshAll();
        }
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Home Tab Events
        private void TextBlock_OpenEDF_Click(object sender, RoutedEventArgs e)
        {
            model.LoadedEDFFile = null;
            RefreshAll();

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
            RefreshAll();

            List<string> array = model.RecentFiles.ToArray().ToList();
            List<string> selected = array.Where(temp => temp.Split('\\')[temp.Split('\\').Length - 1] == ((Hyperlink)sender).Inlines.FirstInline.DataContext.ToString()).ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("File not Found");
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
                EDFSignal edfsignal = model.LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == comboBox_SignalSelect.SelectedValue.ToString().Trim());
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
    }

    public class ModelView : INotifyPropertyChanged
    {
        /*********************************************** THIS IS ALL A MESS THAT I NEED TO CLEAN UP *************************************/

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

        // Preview Graph
        public PlotModel PreviewSignalPlot = null;

        // Selected Signals to be Previewed
        

        // Created Derivatives
        private List<string[]> DerivedSignals = new List<string[]>();
        
        public void SetSelectedSignals(System.Collections.IList SelectedItems)
        {
            p_PreviewSelectedSignals.Clear();
            for (int x = 0; x < SelectedItems.Count; x++)
                p_PreviewSelectedSignals.Add(SelectedItems[x].ToString());
        }

        public string[][] GetDerivedSignals()
        {
            return DerivedSignals.ToArray();
        }

        public void SetAbsoluteOrEpoch(bool value)
        {
            p_PreviewUseAbsoluteTime = value;
        }
        public void SetViewRange(DateTime? start, int? period)
        {
            if (p_PreviewUseAbsoluteTime)
            {
                p_PreviewViewStartTime = start ?? LoadedEDFFile.Header.StartDateTime;
                p_PreviewViewEndTime = p_PreviewViewStartTime + new TimeSpan(0, 0, 0, period ?? 0);
            }
        }
        public void SetViewRange(int? start, int? period)
        {
            if (!p_PreviewUseAbsoluteTime)
            {
                p_PreviewViewStartTime = EpochtoDateTime(start ?? 0, LoadedEDFFile);
                p_PreviewViewEndTime = p_PreviewViewStartTime + EpochPeriodtoTimeSpan(period ?? 0, LoadedEDFFile);
            }
        }

        public DateTime GetViewStartTime()
        {
            return p_PreviewViewStartTime;
        }
        public DateTime GetViewEndTime()
        {
            return p_PreviewViewEndTime;
        }
        public int GetViewStartEpoch()
        {
            return DateTimetoEpoch(p_PreviewViewStartTime, LoadedEDFFile);
        }
        public int GetViewEndEpoch()
        {
            return DateTimetoEpoch(p_PreviewViewEndTime, LoadedEDFFile);
        }
        public int GetViewPeriod()
        {
            if (p_PreviewUseAbsoluteTime)
                return (int)(p_PreviewViewEndTime - p_PreviewViewStartTime).TotalSeconds;
            else
                return TimeSpantoEpochPeriod(p_PreviewViewEndTime - p_PreviewViewStartTime, LoadedEDFFile);
        }

        public DateTime GetViewStartPickerAbsMin()
        {
            if (LoadedEDFFile != null)
                return LoadedEDFFile.Header.StartDateTime;
            else
                return new DateTime();
        }
        public DateTime GetViewStartPickerAbsMax()
        {
            if (LoadedEDFFile != null)
                return (LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)) - (p_PreviewViewEndTime - p_PreviewViewStartTime);
            else
                return new DateTime();
        }
        public int GetViewStartPickerEphMin()
        {
            if (LoadedEDFFile != null)
                return DateTimetoEpoch(LoadedEDFFile.Header.StartDateTime, LoadedEDFFile);
            else
                return 0;
        }
        public int GetViewStartPickerEphMax()
        {
            if (LoadedEDFFile != null)
                return DateTimetoEpoch((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)) - (p_PreviewViewEndTime - p_PreviewViewStartTime), LoadedEDFFile);
            else
                return 0;
        }
        public int GetViewPeriodMin()
        {
            if (LoadedEDFFile != null) // File Loaded
                return 1;
            else // No File Loaded
                return 0;
        }
        public int GetViewPeriodMax()
        {
            if (LoadedEDFFile != null) // File Loaded
            {
                if (p_PreviewUseAbsoluteTime)
                    return Math.Min(2 * 60 * 60, (int)((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)) - p_PreviewViewStartTime).TotalSeconds);
                else
                    return Math.Min((2 * 60 * 60) / (LoadedEDFFile == null ? 30 : LoadedEDFFile.Header.DurationOfDataRecordInSeconds), DateTimetoEpoch((LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, LoadedEDFFile.Header.NumberOfDataRecords * LoadedEDFFile.Header.DurationOfDataRecordInSeconds)), LoadedEDFFile) - DateTimetoEpoch(p_PreviewViewStartTime, LoadedEDFFile));
            }
            else // No File Loaded
                return 0;
        }

        // Load EDF
        private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            EDFFile temp = new EDFFile();
            temp.readFile(e.Argument.ToString());
            LoadedEDFFile = temp;

            LoadCommonDerivativesFile();
        }
        private void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            RecentFiles_Add(LoadedEDFFileName);
            p_window.EDFLoaded();
        }
        public void LoadEDFFile(string fileNameIn)
        {
            LoadedEDFFileName = fileNameIn;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_LoadEDFFile;
            bw.RunWorkerCompleted += BW_FinishLoad;
            bw.RunWorkerAsync(LoadedEDFFileName);
        }
        
        // Create Chart
        private void BW_CreateChart(object sender, DoWorkEventArgs e)
        {
            PreviewSignalPlot = new PlotModel();
            PreviewSignalPlot.Series.Clear();
            PreviewSignalPlot.Axes.Clear();

            if (p_PreviewSelectedSignals.Count > 0)
            {
                DateTimeAxis xAxis = new DateTimeAxis();
                xAxis.Key = "DateTime";
                xAxis.Minimum = DateTimeAxis.ToDouble(p_PreviewViewStartTime);
                xAxis.Maximum = DateTimeAxis.ToDouble(p_PreviewViewEndTime);
                PreviewSignalPlot.Axes.Add(xAxis);

                for (int x = 0; x < p_PreviewSelectedSignals.Count; x++)
                {
                    LineSeries series = new LineSeries();
                    if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == p_PreviewSelectedSignals[x].Trim()) != null) // Normal EDF Signal
                    {
                        EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == p_PreviewSelectedSignals[x].Trim());

                        float sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

                        List<float> values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);

                        int startIndex, indexCount;
                        TimeSpan startPoint = p_PreviewViewStartTime - LoadedEDFFile.Header.StartDateTime;
                        TimeSpan duration = p_PreviewViewEndTime - p_PreviewViewStartTime;
                        startIndex = (int)(startPoint.TotalSeconds / sample_period);
                        indexCount = (int)(duration.TotalSeconds / sample_period);

                        for (int y = startIndex; y < indexCount + startIndex; y++)
                        {
                            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
                        }
                    }
                    else // Derivative Signal
                    {
                        string[] deriv_info = DerivedSignals.Find(temp => temp[0] == p_PreviewSelectedSignals[x]);
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
                        TimeSpan startPoint = p_PreviewViewStartTime - LoadedEDFFile.Header.StartDateTime;
                        TimeSpan duration = p_PreviewViewEndTime - p_PreviewViewStartTime;
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

                    PreviewSignalPlot.Axes.Add(yAxis);
                    PreviewSignalPlot.Series.Add(series);
                }
            }
        }
        private void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
        {
            p_window.PreviewChartDrawn();
        }
        public void DrawChart()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }

        /*********************************************** THIS PART IS OK *************************************/
        
        // Static Functions
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

        // General Private Variables
        private MainWindow p_window;
        private EDFFile p_LoadedEDFFile = null;
        private string p_LoadedEDFFileName = null;
        private List<string> p_SignalCategories = new List<string>();
        private List<List<string>> p_SignalCategoryContents = new List<List<string>>();

        // Preview Private Variables
        private int p_PreviewCurrentCategory = -1;
        private List<string> p_PreviewSelectedSignals = new List<string>();
        private bool p_PreviewUseAbsoluteTime = false;
        private DateTime p_PreviewViewStartTime = new DateTime();
        private DateTime p_PreviewViewEndTime = new DateTime();

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
                OnPropertyChanged(nameof(EDFRecordDuration));
                OnPropertyChanged(nameof(EDFNumRecords));
                OnPropertyChanged(nameof(EDFPatientName));
                OnPropertyChanged(nameof(EDFPatientSex));
                OnPropertyChanged(nameof(EDFPatientCode));
                OnPropertyChanged(nameof(EDFPatientBirthDate));
                OnPropertyChanged(nameof(EDFRecordEquipment));
                OnPropertyChanged(nameof(EDFRecordCode));
                OnPropertyChanged(nameof(EDFRecordTechnician));
                OnPropertyChanged(nameof(EDFAllSignals));

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
        public string EDFRecordDuration
        {
            get
            {
                if (IsEDFLoaded)
                    return LoadedEDFFile.Header.DurationOfDataRecordInSeconds.ToString();
                else
                    return "";
            }
        }
        public string EDFNumRecords
        {
            get
            {
                if (IsEDFLoaded)
                    return LoadedEDFFile.Header.NumberOfDataRecords.ToString();
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
                        output.AddRange(DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
                        return Array.AsReadOnly(output.ToArray());
                    }
                }
                else
                {
                    return Array.AsReadOnly(new string[0]);
                }
            }
        }

        // Category Management
        public void ManageCategories()
        {
            Dialog_Manage_Categories dlg = new Dialog_Manage_Categories(p_SignalCategories.ToArray(), p_SignalCategoryContents.Select(temp => temp.ToArray()).ToArray(), LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).ToArray(), DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
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
        
        // Derivative Management
        private void LoadCommonDerivativesFile()
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
                        if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[1].Trim()) != null) // Signals Exist
                        {
                            if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[2].Trim()) != null) // Signals Exist
                            {
                                if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == new_entry[0].Trim()) == null) // Unique Name
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
            Dialog_Add_Derivative dlg = new Dialog_Add_Derivative(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.Trim()).ToArray(), DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
            dlg.ShowDialog();

            if (dlg.DialogResult == true)
            {
                DerivedSignals.Add(new string[] { dlg.SignalName, dlg.Signal1, dlg.Signal2 });
                AddToCommonDerivativesFile(dlg.SignalName, dlg.Signal1, dlg.Signal2);
            }

            OnPropertyChanged(nameof(PreviewSignals));
        }
        public void RemoveDerivative()
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

                    if (p_PreviewSelectedSignals.Contains(dlg.RemovedSignals[x].Trim()))
                    {
                        p_PreviewSelectedSignals.Remove(dlg.RemovedSignals[x].Trim());
                    }
                }
            }

            OnPropertyChanged(nameof(PreviewSignals));
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
