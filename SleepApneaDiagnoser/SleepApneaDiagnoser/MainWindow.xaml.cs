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
        string fileName = null;

        public void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            edfFile = new EDFFile();
            edfFile.readFile(e.Argument.ToString());
        }
        public void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlock_Status.Text = "Waiting";
            this.IsEnabled = true;

            textBox_FileName.Text = fileName.Split('\\')[fileName.Split('\\').Length - 1];
            textBox_NumSignals.Text = edfFile.Header.Signals.Count.ToString();
            textBox_StartTime.Text = edfFile.Header.StartDateTime.ToString();
            textBox_EndTime.Text = (edfFile.Header.StartDateTime + new TimeSpan(0, 0, edfFile.Header.NumberOfDataRecords * edfFile.Header.DurationOfDataRecordInSeconds)).ToString();

            textBox_PI_Name.Text = edfFile.Header.PatientIdentification.PatientName;
            textBox_PI_Sex.Text = edfFile.Header.PatientIdentification.PatientSex;
            textBox_PI_Code.Text = edfFile.Header.PatientIdentification.PatientCode;
            textBox_PI_Birthdate.Text = edfFile.Header.PatientIdentification.PatientBirthDate.ToString();

            textBox_RI_Equipment.Text = edfFile.Header.RecordingIdentification.RecordingEquipment;
            textBox_RI_Code.Text = edfFile.Header.RecordingIdentification.RecordingCode;
            textBox_RI_Technician.Text = edfFile.Header.RecordingIdentification.RecordingTechnician;

            timePicker_From.Value = edfFile.Header.StartDateTime;
            timePicker_To.Value = edfFile.Header.StartDateTime + new TimeSpan(0, 30, 0);

            listBox_edfSignals.Items.Clear();
            foreach (EDFSignal signal in edfFile.Header.Signals)
            {
                listBox_edfSignals.Items.Add(signal);
            }

            PlotView_signalPlot.Model = null;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

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

            textBox_FileName.Text = "";
            textBox_NumSignals.Text = "";
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
            timePicker_To.Value = null;

            listBox_edfSignals.Items.Clear();
            PlotView_signalPlot.Model = null;
        }
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void updateChart()
        {
            PlotModel signalPlot = new PlotModel();
            signalPlot.Series.Clear();
            signalPlot.Axes.Clear();

            if (listBox_edfSignals.SelectedItems.Count > 0)
            {
                DateTimeAxis xAxis = new DateTimeAxis();
                xAxis.Key = "DateTime";
                xAxis.Minimum = DateTimeAxis.ToDouble((DateTime)timePicker_From.Value);
                xAxis.Maximum = DateTimeAxis.ToDouble((DateTime)timePicker_To.Value);
                signalPlot.Axes.Add(xAxis);

                for (int x = 0; x < listBox_edfSignals.SelectedItems.Count; x++)
                {
                    EDFSignal edfsignal = edfFile.Header.Signals.Find(temp => temp.ToString() == listBox_edfSignals.SelectedItems[x].ToString());
                    if (edfsignal != null)
                    {
                        float sample_period = (float)edfFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;

                        List<float> values = edfFile.retrieveSignalSampleValues(edfsignal);
                        TimeSpan startPoint = (DateTime)timePicker_From.Value - edfFile.Header.StartDateTime;
                        int startIndex = (int)(startPoint.TotalSeconds / sample_period);
                        TimeSpan duration = (DateTime)timePicker_To.Value - (DateTime)timePicker_From.Value;
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
                        series.YAxisKey = listBox_edfSignals.SelectedItems[x].ToString();
                        series.XAxisKey = "DateTime";

                        LinearAxis yAxis = new LinearAxis();
                        yAxis.MajorGridlineStyle = LineStyle.Solid;
                        yAxis.MinorGridlineStyle = LineStyle.Dot;
                        yAxis.Title = listBox_edfSignals.SelectedItems[x].ToString();
                        yAxis.Key = listBox_edfSignals.SelectedItems[x].ToString();
                        yAxis.EndPosition = (double)1 - (double)x * ((double)1 / (double)listBox_edfSignals.SelectedItems.Count);
                        yAxis.StartPosition = (double)1 - (double)(x + 1) * ((double)1 / (double)listBox_edfSignals.SelectedItems.Count);

                        signalPlot.Axes.Add(yAxis);
                        signalPlot.Series.Add(series);
                    }
                }

            }

            PlotView_signalPlot.Model = signalPlot;
        }

        private void listBox_edfSignals_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateChart();   
        }
        private void timePicker_From_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (edfFile != null)
            {
                DateTime EndDate = edfFile.Header.StartDateTime + new TimeSpan(0, 0, edfFile.Header.NumberOfDataRecords * edfFile.Header.DurationOfDataRecordInSeconds);

                timePicker_To.Minimum = timePicker_From.Value;
                timePicker_To.Maximum = EndDate < timePicker_From.Value + new TimeSpan(2, 0, 0) ? EndDate : timePicker_From.Value + new TimeSpan(2, 0, 0);
            }
            else
            {
                timePicker_To.Minimum = new DateTime();
                timePicker_To.Maximum = new DateTime();
            }

            updateChart();
        }
        private void timePicker_To_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (edfFile != null)
            {
                timePicker_From.Minimum = (edfFile.Header.StartDateTime > timePicker_To.Value - new TimeSpan(2, 0, 0)) ? edfFile.Header.StartDateTime : timePicker_To.Value - new TimeSpan(2, 0, 0);
                timePicker_From.Maximum = timePicker_To.Value;
            }
            else
            {
                timePicker_From.Minimum = new DateTime();
                timePicker_From.Maximum = new DateTime();
            }

            updateChart();
        }
    }
}
