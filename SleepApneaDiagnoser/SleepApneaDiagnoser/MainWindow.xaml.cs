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

namespace SleepApneaDiagnoser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public EDFFile edfFile = null;
        public PlotModel signalPlot = null;

        public void BW_LoadEDFFile(object sender, DoWorkEventArgs e)
        {
            edfFile = new EDFFile();
            edfFile.readFile(e.Argument.ToString());
        }
        public void BW_FinishLoad(object sender, RunWorkerCompletedEventArgs e)
        {
            TextBlock_Status.Text = "Waiting";
            this.IsEnabled = true;

            listBox_edfSignals.Items.Clear();
            foreach (EDFSignal signal in edfFile.Header.Signals)
            {
                listBox_edfSignals.Items.Add(signal);
            }

            signalPlot = null;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_File_Open_Click(object sender, RoutedEventArgs e)
        {
            string fileName = null;
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
            listBox_edfSignals.Items.Clear();
            signalPlot = null;
        }
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void listBox_edfSignals_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LineSeries series = new LineSeries();
            
            EDFSignal edfsignal = edfFile.Header.Signals.Find(temp => temp.ToString() == listBox_edfSignals.SelectedItem.ToString());
            if (edfsignal != null)
            {
                float sample_period = 1;
                sample_period = edfsignal.SamplePeriodWithinDataRecord;
                if (sample_period == 0)
                    sample_period = 1;

                List<float> values = edfFile.retrieveSignalSampleValues(edfsignal);

                for (int x = 0; x < Math.Min(100, values.Count); x++)
                {
                    series.Points.Add(new DataPoint(sample_period * x, values[x]));
                }
            }

            signalPlot = new PlotModel { Title = listBox_edfSignals.SelectedItem.ToString() };
            signalPlot.Series.Clear();
            signalPlot.Series.Add(series);

            PlotView_signalPlot.Model = signalPlot;
        }
    }
}
