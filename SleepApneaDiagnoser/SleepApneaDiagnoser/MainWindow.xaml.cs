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
            model.WriteSettings();
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
                LoadRecent();
            }
            else
            {
                for (int x = 0; x < selected.Count; x++)
                {
                    if (File.Exists(selected[x]))
                    {
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

        private void button_HideSignals_Click(object sender, RoutedEventArgs e)
        {
            model.HideSignals();
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

        private void button_PerformRespiratoryAnalysis_Click(object sender, RoutedEventArgs e)
        {
            model.PerformRespiratoryAnalysisEDF();
        }
    }

    public class ModelView : INotifyPropertyChanged
    {
        /******************************************************* STATIC FUNCTIONS *******************************************************/

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

        private static double? GetPercentileValue(float[] values_array, int percentile, int tolerance)
        {
            List<float> values = values_array.ToList();
            values.Sort();

            int index = (int)((double)percentile / (double)100 * (double)values.Count);

            return values[index];
        }
        private static double? GetPercentileValueDeriv(float[] values_array_1, float[] values_array_2, int percentile, int tolerance)
        {
            List<float> values1 = values_array_1.ToList();
            List<float> values2 = values_array_2.ToList();

            List<float> values = new List<float>();
            for (int x = 0; x < Math.Min(values_array_1.Length, values_array_2.Length); x++)
                values.Add(values_array_1[x] - values_array_2[x]);

            return GetPercentileValue(values.ToArray(), percentile, tolerance);
        }
        
        /****************************************************** HELPER FUNCTIONS *******************************************************/

        private LineSeries GetSeriesFromSignalName(out float sample_period, out double? max_y, out double? min_y, string Signal, DateTime StartTime, DateTime EndTime)
        {
            // Variable To Return
            LineSeries series = new LineSeries();

            if (LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal.Trim()) != null) // Normal EDF Signal
            {
                // Get Signal
                EDFSignal edfsignal = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == Signal);

                // Determine Array Portion
                sample_period = (float)LoadedEDFFile.Header.DurationOfDataRecordInSeconds / (float)edfsignal.NumberOfSamplesPerDataRecord;
                int startIndex, indexCount;
                TimeSpan startPoint = StartTime - LoadedEDFFile.Header.StartDateTime;
                TimeSpan duration = EndTime - StartTime;
                startIndex = (int)(startPoint.TotalSeconds / sample_period);
                indexCount = (int)(duration.TotalSeconds / sample_period);

                // Get Array
                List<float> values = LoadedEDFFile.retrieveSignalSampleValues(edfsignal);

                // Determine Y Axis Bounds
                min_y = GetMinSignalValue(Signal, values);
                max_y = GetMaxSignalValue(Signal, values);
                
                // Add Points to Series
                for (int y = startIndex; y < indexCount + startIndex; y++)
                {
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values[y]));
                }
            }
            else // Derivative Signal
            {
                // Get Signals
                string[] deriv_info = p_DerivedSignals.Find(temp => temp[0] == Signal);
                EDFSignal edfsignal1 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[1].Trim());
                EDFSignal edfsignal2 = LoadedEDFFile.Header.Signals.Find(temp => temp.Label.Trim() == deriv_info[2].Trim());

                // Get Arrays and Perform Resampling if needed
                List<float> values1;
                List<float> values2;
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

                // Determine Array Portions
                int startIndex, indexCount;
                TimeSpan startPoint = StartTime - LoadedEDFFile.Header.StartDateTime;
                TimeSpan duration = EndTime - StartTime;
                startIndex = (int)(startPoint.TotalSeconds / sample_period);
                indexCount = (int)(duration.TotalSeconds / sample_period);

                // Get Y Axis Bounds
                min_y = GetMinSignalValue(Signal, values1, values2);
                max_y = GetMaxSignalValue(Signal, values1, values2);

                // Add Points to Series
                for (int y = startIndex; y < indexCount + startIndex; y++)
                {
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(LoadedEDFFile.Header.StartDateTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), values1[y] - values2[y]));
                }
            }

            return series;
        }

        /*********************************************************** ACTIONS ***********************************************************/

        // Load EDF File
        private ProgressDialogController controller;
        private void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            controller.SetCancelable(false);

            EDFFile temp = new EDFFile();
            temp.readFile(e.Argument.ToString());
            LoadedEDFFile = temp;
            
            // Load Settings Files
            LoadSettings();
        }
        private async void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            RecentFiles_Add(LoadedEDFFileName);
            
            await controller.CloseAsync();
            await p_window.ShowMessageAsync("Success!", "EDF file loaded");
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

        // Create Preview Chart
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
                    double? min_y, max_y;
                    float sample_period;
                    LineSeries series = GetSeriesFromSignalName(out sample_period, 
                                                                out max_y, 
                                                                out min_y, 
                                                                p_PreviewSelectedSignals[x], 
                                                                (PreviewViewStartTime ?? new DateTime()), 
                                                                PreviewViewEndTime
                                                                );

                    series.YAxisKey = p_PreviewSelectedSignals[x];
                    series.XAxisKey = "DateTime";

                    LinearAxis yAxis = new LinearAxis();
                    yAxis.MajorGridlineStyle = LineStyle.Solid;
                    yAxis.MinorGridlineStyle = LineStyle.Dot;
                    yAxis.Title = p_PreviewSelectedSignals[x];
                    yAxis.Key = p_PreviewSelectedSignals[x];
                    yAxis.EndPosition = (double)1 - (double)x * ((double)1 / (double)p_PreviewSelectedSignals.Count);
                    yAxis.StartPosition = (double)1 - (double)(x + 1) * ((double)1 / (double)p_PreviewSelectedSignals.Count);
                    yAxis.Maximum = max_y ?? Double.NaN;
                    yAxis.Minimum = min_y ?? Double.NaN;

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
            else
            {
                p_window.ShowMessageAsync("Error", "Please select at least one signal from the preview.");
            }
        }
        
        // Respiratory Analysis From EDF File
        private void BW_RespiratoryAnalysisEDF(object sender, DoWorkEventArgs e)
        {
            PlotModel temp_SignalPlot = new PlotModel();
            temp_SignalPlot.Series.Clear();
            temp_SignalPlot.Axes.Clear();

            double? max_y, min_y;
            float sample_period;
            LineSeries series = GetSeriesFromSignalName(out sample_period, 
                                                        out max_y, 
                                                        out min_y, 
                                                        RespiratoryEDFSelectedSignal, 
                                                        EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile), 
                                                        EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile) + EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 0)
                                                        );

            // Plot Insets of Respiration Expiration

            // Calculate Bias
            double bias = 0;
            for (int x = 0; x < series.Points.Count; x++)
            {
                double point_1 = series.Points[x].Y;
                double point_2 = x + 1 < series.Points.Count ? series.Points[x + 1].Y : series.Points[x].Y;
                double average = (point_1 + point_2) / 2;
                bias += average / (double)series.Points.Count;
            }
            
            // Weighted Running Average (Smoothing) and Normalization
            LineSeries series_norm = new LineSeries();
            int LENGTH = 50;
            for (int x = 0; x < series.Points.Count; x+=5)
            {
                double sum = 0;
                double weight_sum = 0;
                for (int y = -LENGTH/2; y <= LENGTH/2; y++)
                {
                    double weight = (LENGTH / 2 + 1) - Math.Abs(y);
                    weight_sum += weight;
                    sum += weight * series.Points[Math.Min(series.Points.Count - 1, Math.Max(0, x - y))].Y;
                }
                double average = sum / weight_sum;

                series_norm.Points.Add(new DataPoint(series.Points[x].X, average - bias));
            }

            // Find Zero Crossings
            int cool_down_in = 0;
            int cool_down_on = 0;
            ScatterSeries series_insets = new ScatterSeries();
            ScatterSeries series_onsets = new ScatterSeries();
            for (int x = 1; x < series_norm.Points.Count; x++)
            {
                if (cool_down_in != 0)
                    cool_down_in--;
                else
                {
                    if ((series_norm.Points[x - 1].Y >= 0 && series_norm.Points[x].Y <= 0))
                    {
                        if (Math.Abs(series_norm.Points[x - 1].Y) > Math.Abs(series_norm.Points[x].Y))
                            series_insets.Points.Add(new ScatterPoint(series_norm.Points[x].X, series_norm.Points[x].Y));
                        else
                            series_insets.Points.Add(new ScatterPoint(series_norm.Points[x - 1].X, series_norm.Points[x - 1].Y));

                        cool_down_in = (int) (0.5 / sample_period);
                    }
                }
                if (cool_down_on != 0)
                    cool_down_on--;
                else
                {
                    if ((series_norm.Points[x - 1].Y <= 0 && series_norm.Points[x].Y >= 0))
                    {
                        if (Math.Abs(series_norm.Points[x - 1].Y) > Math.Abs(series_norm.Points[x].Y))
                            series_onsets.Points.Add(new ScatterPoint(series_norm.Points[x].X, series_norm.Points[x].Y));
                        else
                            series_onsets.Points.Add(new ScatterPoint(series_norm.Points[x - 1].X, series_norm.Points[x - 1].Y));

                        cool_down_on = (int)(0.5 / sample_period);
                    }
                }
            }

            series_norm.YAxisKey = RespiratoryEDFSelectedSignal;
            series_norm.XAxisKey = "DateTime";
            series_onsets.YAxisKey = RespiratoryEDFSelectedSignal;
            series_onsets.XAxisKey = "DateTime";
            series_insets.YAxisKey = RespiratoryEDFSelectedSignal;
            series_insets.XAxisKey = "DateTime";

            DateTimeAxis xAxis = new DateTimeAxis();
            xAxis.Key = "DateTime";
            xAxis.Minimum = DateTimeAxis.ToDouble(EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile));
            xAxis.Maximum = DateTimeAxis.ToDouble(EpochtoDateTime(RespiratoryEDFStartRecord ?? 0, LoadedEDFFile) + EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 0));
            temp_SignalPlot.Axes.Add(xAxis);

            LinearAxis yAxis = new LinearAxis();
            yAxis.MajorGridlineStyle = LineStyle.Solid;
            yAxis.MinorGridlineStyle = LineStyle.Dot;
            yAxis.Title = RespiratoryEDFSelectedSignal;
            yAxis.Key = RespiratoryEDFSelectedSignal;
            yAxis.Maximum = (max_y ?? Double.NaN) - bias;
            yAxis.Minimum = (min_y ?? Double.NaN) - bias;

            temp_SignalPlot.Axes.Add(yAxis);
            temp_SignalPlot.Series.Add(series_norm);
            temp_SignalPlot.Series.Add(series_onsets);
            temp_SignalPlot.Series.Add(series_insets);

            RespiratorySignalPlot = temp_SignalPlot;
        }
        private void BW_FinishRespiratoryAnalysisEDF(object sender, RunWorkerCompletedEventArgs e)
        {
        }
        public void PerformRespiratoryAnalysisEDF()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BW_RespiratoryAnalysisEDF;
            bw.RunWorkerCompleted += BW_FinishRespiratoryAnalysisEDF;
            bw.RunWorkerAsync();
        }

        /*********************************************************** SETTINGS ***********************************************************/

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

        // Preview Category Management
        private List<string> p_SignalCategories = new List<string>();
        private List<List<string>> p_SignalCategoryContents = new List<List<string>>();
        private void LoadCategoriesFile()
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
        private void WriteToCategoriesFile()
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
            dlg.ShowModalDialogExternally();

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
        private List<string[]> p_DerivedSignals = new List<string[]>();
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
            dlg.ShowModalDialogExternally();

            if (dlg.DialogResult == true)
            {
                p_DerivedSignals.Add(new string[] { dlg.SignalName, dlg.Signal1, dlg.Signal2 });
                AddToCommonDerivativesFile(dlg.SignalName, dlg.Signal1, dlg.Signal2);
            }

            OnPropertyChanged(nameof(PreviewSignals));
            OnPropertyChanged(nameof(AllNonHiddenSignals));
        }
        public void RemoveDerivative()
        {
            Dialog_Remove_Derivative dlg = new Dialog_Remove_Derivative(p_DerivedSignals.ToArray());
            dlg.ShowModalDialogExternally();

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

                    // Remove Potentially Saved Min/Max Values
                    p_SignalsMaxValues.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
                    p_SignalsMinValues.RemoveAll(temp => temp[0].Trim() == dlg.RemovedSignals[x].Trim());
                }
            }
            
            OnPropertyChanged(nameof(PreviewSignals));
            OnPropertyChanged(nameof(AllNonHiddenSignals));
        }

        // Hidden Signal Management
        private List<string> p_HiddenSignals = new List<string>();
        private void LoadHiddenSignalsFile()
        {
            p_HiddenSignals.Clear();
            if (File.Exists("hiddensignals.txt"))
            {
                p_HiddenSignals = new StreamReader("hiddensignals.txt").ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
                p_HiddenSignals = p_HiddenSignals.Select(temp => temp.Trim()).ToList();
            }
        }
        private void WriteToHiddenSignalsFile()
        {
            StreamWriter sw = new StreamWriter("hiddensignals.txt");
            for (int x = 0; x < p_HiddenSignals.Count; x++)
            {
                sw.WriteLine(p_HiddenSignals[x]);
            }
            sw.Close();
        }
        public void HideSignals()
        {
            bool[] input = new bool[EDFAllSignals.Count];
            for (int x = 0; x < EDFAllSignals.Count; x++)
            {
                if (p_HiddenSignals.Contains(EDFAllSignals[x]))
                    input[x] = true;
                else
                    input[x] = false;
            }

            Dialog_Hide_Signals dlg = new Dialog_Hide_Signals(EDFAllSignals.ToArray(), input);
            dlg.ShowModalDialogExternally();

            for (int x = 0; x < dlg.hide_signals_new.Length; x++)
            {
                if (dlg.hide_signals_new[x])
                {
                    if (!p_HiddenSignals.Contains(EDFAllSignals[x]))
                    {
                        p_HiddenSignals.Add(EDFAllSignals[x]);
                    }
                }
                else
                {
                    if (p_HiddenSignals.Contains(EDFAllSignals[x]))
                    {
                        p_HiddenSignals.Remove(EDFAllSignals[x]);
                    }
                }
            }

            OnPropertyChanged(nameof(PreviewSignals));
            OnPropertyChanged(nameof(AllNonHiddenSignals));
        }

        private List<string[]> p_SignalsMinValues = new List<string[]>();
        private List<string[]> p_SignalsMaxValues = new List<string[]>();
        private double? GetMaxSignalValue(string Signal, List<float> values)
        {
            string[] find = p_SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
                return Double.Parse(find[1]);
            else
            {
                double? value = null; 

                int j = 1;
                while (value == null && j < 100)
                {
                    j++;
                    value = GetPercentileValue(values.ToArray(), 97, j);
                }
                
                SetMaxSignalValue(Signal, value ?? 0);
                return value;
            }
        }
        private double? GetMinSignalValue(string Signal, List<float> values)
        {
            string[] find = p_SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
                return Double.Parse(find[1]);
            else
            {
                double? value = null;
                 
                int j = 1;
                while (value == null && j < 100)
                {
                    j++;
                    value = GetPercentileValue(values.ToArray(), 3, j);
                }

                SetMinSignalValue(Signal, value ?? 0);
                return value;
            }
        }
        private double? GetMaxSignalValue(string Signal, List<float> values1, List<float> values2)
        {
            string[] find = p_SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
                return Double.Parse(find[1]);
            else
            {
                double? value = null;
                
                int j = 1;
                while (value == null && j < 100)
                {
                    j++;
                    value = GetPercentileValueDeriv(values1.ToArray(), values2.ToArray(), 97, j);
                }

                SetMaxSignalValue(Signal, value ?? 0);
                return value;
            }
        }
        private double? GetMinSignalValue(string Signal, List<float> values1, List<float> values2)
        {
            string[] find = p_SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
                return Double.Parse(find[1]);
            else
            {
                double? value = null;
                
                int j = 1;
                while (value == null && j < 100)
                {
                    j++;
                    value = GetPercentileValueDeriv(values1.ToArray(), values2.ToArray(), 3, j);
                }

                SetMinSignalValue(Signal, value ?? 0);
                return value;
            }
        }
        
        private void SetMaxSignalValue(string Signal, double Value)
        {
            string[] find = p_SignalsMaxValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
            {
                p_SignalsMaxValues.Remove(find);
            }

            p_SignalsMaxValues.Add(new string[] { Signal.Trim(), Value.ToString() });
        }
        private void SetMinSignalValue(string Signal, double Value)
        {
            string[] find = p_SignalsMinValues.Find(temp => temp[0].Trim() == Signal.Trim());

            if (find != null)
            {
                p_SignalsMinValues.Remove(find);
            }

            p_SignalsMinValues.Add(new string[] { Signal.Trim(), Value.ToString() });
        }

        public void WriteSettings()
        {
            WriteToHiddenSignalsFile();
            WriteToCategoriesFile();
        }
        public void LoadSettings()
        {
            LoadHiddenSignalsFile();
            LoadCommonDerivativesFile();
            LoadCategoriesFile();
            OnPropertyChanged(nameof(PreviewSignals));
            OnPropertyChanged(nameof(AllNonHiddenSignals));
        }
        
        /*********************************************************** MEMBERS ************************************************************/

        // General Private Variables
        private MainWindow p_window;
        private EDFFile p_LoadedEDFFile = null;
        private string p_LoadedEDFFileName = null;
        
        // Preview Private Variables
        private int p_PreviewCurrentCategory = -1;
        private List<string> p_PreviewSelectedSignals = new List<string>();
        
        private bool p_PreviewUseAbsoluteTime = false;
        private DateTime p_PreviewViewStartTime = new DateTime();
        private int p_PreviewViewStartRecord = 0;
        private int p_PreviewViewDuration = 0;
        private PlotModel p_PreviewSignalPlot = null;
        private bool p_PreviewNavigationEnabled = false;

        // Respiratory Private Variables
        private string p_RespiratoryEDFSelectedSignal;
        private int p_RespiratoryEDFStartRecord;
        private int p_RespiratoryEDFDuration;
        private PlotModel p_RespiratorySignalPlot = null;

        /********************************************************** PROPERTIES **********************************************************/

        // Update Actions
        private void LoadedEDFFile_Changed()
        {
            PreviewCurrentCategory = -1;

            // Preview Time Picker
            if (p_LoadedEDFFile == null)
            {
                PreviewUseAbsoluteTime = false;
                PreviewViewStartTime = null;
                PreviewViewStartRecord = null;
                PreviewViewDuration = null;
                LoadedEDFFileName = null;
            }
            else
            {
                PreviewUseAbsoluteTime = false;
                PreviewViewStartTime = LoadedEDFFile.Header.StartDateTime;
                PreviewViewStartRecord = 0;
                PreviewViewDuration = 5;
            }
            OnPropertyChanged(nameof(PreviewNavigationEnabled));

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
            
            // Misc
            OnPropertyChanged(nameof(IsEDFLoaded));
        }
        private void PreviewCurrentCategory_Changed()
        {
            OnPropertyChanged(nameof(PreviewCurrentCategoryName));
            OnPropertyChanged(nameof(PreviewSignals));
        }
        private void PreviewUseAbsoluteTime_Changed()
        {
            OnPropertyChanged(nameof(PreviewViewDuration));

            OnPropertyChanged(nameof(PreviewViewStartTimeMax));
            OnPropertyChanged(nameof(PreviewViewStartTimeMin));
            OnPropertyChanged(nameof(PreviewViewStartRecordMax));
            OnPropertyChanged(nameof(PreviewViewStartRecordMin));
            OnPropertyChanged(nameof(PreviewViewDurationMax));
            OnPropertyChanged(nameof(PreviewViewDurationMin));

            DrawChart();
        }
        private void PreviewView_Changed()
        {
            OnPropertyChanged(nameof(PreviewViewStartRecord));
            OnPropertyChanged(nameof(PreviewViewStartTime));
            OnPropertyChanged(nameof(PreviewViewDuration));

            OnPropertyChanged(nameof(PreviewViewStartTimeMax));
            OnPropertyChanged(nameof(PreviewViewStartTimeMin));
            OnPropertyChanged(nameof(PreviewViewStartRecordMax));
            OnPropertyChanged(nameof(PreviewViewStartRecordMin));
            OnPropertyChanged(nameof(PreviewViewDurationMax));
            OnPropertyChanged(nameof(PreviewViewDurationMin));

            DrawChart();
        }

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
                LoadedEDFFile_Changed();
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
        public ReadOnlyCollection<string> AllNonHiddenSignals
        {
            get
            {
                if (IsEDFLoaded)
                {
                    List<string> output = new List<string>();
                    output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).Where(temp => !p_HiddenSignals.Contains(temp)).ToArray());
                    output.AddRange(p_DerivedSignals.Select(temp => temp[0].Trim()).ToArray());
                    return Array.AsReadOnly(output.ToArray());
                }
                else
                {
                    return Array.AsReadOnly(new string[0]);
                }
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
                PreviewCurrentCategory_Changed();
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
                        return Array.AsReadOnly(p_SignalCategoryContents[PreviewCurrentCategory].Where(temp => !p_HiddenSignals.Contains(temp)).ToArray());
                    else
                    {
                        List<string> output = new List<string>();
                        output.AddRange(LoadedEDFFile.Header.Signals.Select(temp => temp.Label.ToString().Trim()).Where(temp => !p_HiddenSignals.Contains(temp)).ToArray());
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
                PreviewUseAbsoluteTime_Changed();
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
                    PreviewView_Changed();
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
                    PreviewView_Changed();
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

                PreviewView_Changed();
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

        // Respiratory Analysis
        public string RespiratoryEDFSelectedSignal
        {
            get
            {
                return p_RespiratoryEDFSelectedSignal;
            }
            set
            {
                p_RespiratoryEDFSelectedSignal = value;
                OnPropertyChanged(nameof(RespiratoryEDFSelectedSignal));
            }
        }
        public int? RespiratoryEDFStartRecord
        {
            get
            {
                return p_RespiratoryEDFStartRecord;
            }
            set
            {
                p_RespiratoryEDFStartRecord = value ?? 0;
                OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
            }
        }
        public int? RespiratoryEDFDuration
        {
            get
            {
                return p_RespiratoryEDFDuration;
            }
            set
            {
                p_RespiratoryEDFDuration = value ?? 0;
                OnPropertyChanged(nameof(RespiratoryEDFDuration));
            }
        }
        public PlotModel RespiratorySignalPlot
        {
            get
            {
                return p_RespiratorySignalPlot;
            }
            set
            {
                p_RespiratorySignalPlot = value;
                OnPropertyChanged(nameof(RespiratorySignalPlot));
            }
        }

        /************************************************************* ETC. *************************************************************/

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
