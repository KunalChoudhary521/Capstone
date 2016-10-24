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
        public EDFFile edfFile = null;
        public string fileName = null;

        public PlotModel signalPlot = null;

        public DateTime StudyStartTime
        {
            get
            {
                DateTime value = new DateTime();
                Dispatcher.Invoke(
                    new Action(() => { value = edfFile.Header.StartDateTime; }
                ));
                return value;
            }
        }
        public DateTime StudyEndTime
        {
            get
            {
                DateTime value = new DateTime();
                Dispatcher.Invoke(
                    new Action(() => { value = edfFile.Header.StartDateTime + new TimeSpan(0, 0, edfFile.Header.NumberOfDataRecords * edfFile.Header.DurationOfDataRecordInSeconds); }
                ));
                return value;
            }
        }
        public DateTime ViewStartTime
        {
            get
            {
                DateTime value = new DateTime();
                Dispatcher.Invoke(
                            new Action(() => { value = (DateTime) timePicker_From.Value; }
                        ));
                return value;
            }
        }
        public DateTime ViewEndTime
        {
            get
            {
                DateTime value = new DateTime();
                Dispatcher.Invoke(
                            new Action(() => { value = (DateTime)timePicker_From.Value + new TimeSpan(0, 0, (int)(timePicker_Period.Value ?? 1)); }
                        ));
                return value;
            }
        }

        public ReadOnlyCollection<string> SelectedSignals
        {
            get
            {
                string[] value = null; 

                Dispatcher.Invoke(
                    new Action(() =>
                    {
                        value = new string[listBox_SignalSelect.SelectedItems.Count];
                        for (int x = 0; x < listBox_SignalSelect.SelectedItems.Count; x++)
                            value[x] = listBox_SignalSelect.SelectedItems[x].ToString();
                    }
                ));

                return Array.AsReadOnly(value);
            }
        }

        public ReadOnlyCollection<string> RecentFiles
        {
            get
            {
                string[] value = null;

                Dispatcher.Invoke(
                    new Action(() =>
                    {
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
                    }
                ));

                return Array.AsReadOnly(value);
            }
        }
        public void addRecentFile(string path)
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

            LoadRecent();
        }
        public void removeRecentFile(string path)
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

            LoadRecent();
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadRecent();
        }

        #region Helper Functions
        public void LoadRecent()
        {
            List<string> array = RecentFiles.ToArray().ToList();

            itemControl_RecentEDF.Items.Clear();
            for (int x = 0; x < array.Count; x++)
                if (!itemControl_RecentEDF.Items.Contains(array[x].Split('\\')[array[x].Split('\\').Length - 1]))
                    itemControl_RecentEDF.Items.Add(array[x].Split('\\')[array[x].Split('\\').Length - 1]);
        }
        public void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            edfFile = new EDFFile();
            edfFile.readFile(e.Argument.ToString());
        }
        public void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            addRecentFile(fileName);

            TextBlock_Status.Text = "Waiting";
            this.IsEnabled = true;
            
            textBox_StartTime.Text = "";
            textBox_EndTime.Text = "";

            textBox_PI_Name.Text = edfFile.Header.PatientIdentification.PatientName;
            textBox_PI_Sex.Text = edfFile.Header.PatientIdentification.PatientSex;
            textBox_PI_Code.Text = edfFile.Header.PatientIdentification.PatientCode;
            textBox_PI_Birthdate.Text = edfFile.Header.PatientIdentification.PatientBirthDate.ToString();

            textBox_RI_Equipment.Text = edfFile.Header.RecordingIdentification.RecordingEquipment;
            textBox_RI_Code.Text = edfFile.Header.RecordingIdentification.RecordingCode;
            textBox_RI_Technician.Text = edfFile.Header.RecordingIdentification.RecordingTechnician;

            timePicker_From.Value = edfFile.Header.StartDateTime;
            timePicker_Period.Value = 60 * 5;

            listBox_SignalSelect.Items.Clear();
            comboBox_SignalSelect.Items.Clear();
            foreach (EDFSignal signal in edfFile.Header.Signals)
            {
                listBox_SignalSelect.Items.Add(signal);
                comboBox_SignalSelect.Items.Add(signal);
            }

            PlotView_signalPlot.Model = null;
        }
        public void BW_CreateChart(object sender, DoWorkEventArgs e)
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
                    TimeSpan startPoint = ViewStartTime - StudyStartTime;
                    int startIndex = (int)(startPoint.TotalSeconds / sample_period);
                    TimeSpan duration = ViewEndTime - ViewStartTime;
                    int indexCount = (int)(duration.TotalSeconds / sample_period);

                    LineSeries series = new LineSeries();
                    for (int y = startIndex; y < indexCount + startIndex; y++)
                    {
                        series.Points.Add(
                            new DataPoint(
                                DateTimeAxis.ToDouble(
                                    edfFile.Header.StartDateTime + new TimeSpan(
                                        0,
                                        0,
                                        0,
                                        0,
                                        (int)(sample_period * (float)y * 1000)
                                    )
                                ),
                                values[y]
                            )
                        );
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
        public void BW_FinishChart(object sender, RunWorkerCompletedEventArgs e)
        {
            PlotView_signalPlot.Model = signalPlot;
        }
        #endregion

        #region Menu Bar Events
        private void MenuItem_File_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "EDF files (*.edf)|*.edf";
            dialog.Title = "Select an EDF file";

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                TextBlock_Status.Text = "Loading EDF File";
                this.IsEnabled = false;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += BW_LoadEDFFile;
                bw.RunWorkerCompleted += BW_FinishLoad;
                bw.RunWorkerAsync(fileName);
            }
        }
        private void MenuItem_File_Close_Click(object sender, RoutedEventArgs e)
        {
            edfFile = null;
            
            textBox_StartTime.Text = "";
            textBox_EndTime.Text = "";

            textBox_PI_Name.Text = "";
            textBox_PI_Sex.Text = "";
            textBox_PI_Code.Text = "";
            textBox_PI_Birthdate.Text = "";

            textBox_RI_Equipment.Text = "";
            textBox_RI_Code.Text = "";
            textBox_RI_Technician.Text = "";

            timePicker_From.Value = null;
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
                fileName = dialog.FileName;
                TextBlock_Status.Text = "Loading EDF File";
                this.IsEnabled = false;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += BW_LoadEDFFile;
                bw.RunWorkerCompleted += BW_FinishLoad;
                bw.RunWorkerAsync(fileName);
            }
        }
        private void TextBlock_Recent_Click(object sender, RoutedEventArgs e)
        {
            List<string> array = RecentFiles.ToArray().ToList();
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

                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += BW_LoadEDFFile;
                        bw.RunWorkerCompleted += BW_FinishLoad;
                        bw.RunWorkerAsync(selected[x]);
                        break;
                    }
                }
            }
        }
        #endregion
                
        #region Preview Tab Events
        private void listBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }
        private void comboBox_SignalSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_SignalSelect.SelectedValue != null)
            {
                EDFSignal edfsignal = edfFile.Header.Signals.Find(temp => temp.ToString() == comboBox_SignalSelect.SelectedValue.ToString());
                textBox_StartTime.Text = StudyStartTime.ToString();
                textBox_EndTime.Text = StudyEndTime.ToString();
                textBox_TimeRecord.Text = edfFile.Header.DurationOfDataRecordInSeconds.ToString();
                textBox_SampRecord.Text = edfsignal.NumberOfSamplesPerDataRecord.ToString();
                textBox_NumRecords.Text = edfFile.Header.NumberOfDataRecords.ToString();
            }
            else
            {
                textBox_StartTime.Text = "";
                textBox_EndTime.Text = "";
                textBox_TimeRecord.Text = "";
                textBox_SampRecord.Text = "";
                textBox_NumRecords.Text = "";
            }
        }
        private void timePicker_From_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (edfFile != null)
            {
                timePicker_Period.Minimum = 1;
                timePicker_Period.Maximum = Math.Min(2 * 60 * 60, (int)(StudyEndTime - (DateTime)timePicker_From.Value).TotalSeconds);
            }
            else
            {
                timePicker_Period.Minimum = 0;
                timePicker_Period.Maximum = 0;
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }
        private void timePicker_Period_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (edfFile != null)
            {
                timePicker_From.Minimum = StudyStartTime;
                timePicker_From.Maximum = StudyEndTime - new TimeSpan(0,0,timePicker_Period.Value ?? 1);
            }
            else
            {
                timePicker_From.Minimum = new DateTime();
                timePicker_From.Maximum = new DateTime();
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_CreateChart;
            bw.RunWorkerCompleted += BW_FinishChart;
            bw.RunWorkerAsync();
        }
        #endregion
    }
}
