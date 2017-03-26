using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using EDF;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using Excel = Microsoft.Office.Interop.Excel;

namespace SleepApneaAnalysisTool
{
  /// <summary>
  /// Factory containing business logic used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryFactory
  {
    #region Static Functions 

    private static LineSeries RemoveBiasFromSignal(LineSeries series, double bias)
    {
      // Normalization
      LineSeries series_norm = new LineSeries();
      for (int x = 0; x < series.Points.Count; x++)
      {
        series_norm.Points.Add(new DataPoint(series.Points[x].X, series.Points[x].Y - bias));
      }

      return series_norm;
    }
    private static ScatterSeries[] GetPeaksAndOnsets(LineSeries series, bool RemoveMultiplePeaks, int min_spike_length)
    {
      int spike_length = 0;
      int maxima = 0;
      int start = 0;
      bool? positive = null;
      ScatterSeries series_pos_peaks = new ScatterSeries();
      ScatterSeries series_neg_peaks = new ScatterSeries();
      ScatterSeries series_insets = new ScatterSeries();
      ScatterSeries series_onsets = new ScatterSeries();
      for (int x = 0; x < series.Points.Count; x++)
      {
        // If positive spike
        if (positive != false)
        {
          // If end of positive spike
          if (series.Points[x].Y < 0 || x == series.Points.Count - 1)
          {
            // If spike is appropriate length
            if (spike_length > min_spike_length)
            {
              if (
                  // If user does not mind consequent peaks of same sign
                  !RemoveMultiplePeaks ||
                  // If first positive peak
                  series_pos_peaks.Points.Count == 0 ||
                  // If last peak was negative
                  (series_neg_peaks.Points.Count != 0 &&
                  DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) >
                  DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))
                 )
              {
                // Add new positive peak and onset 
                series_pos_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                series_onsets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
              }
              else
              {
                // If this peak is greater than the previous
                if (series.Points[maxima].Y > series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].Y)
                {
                  // Replace previous spike maxima with latest spike maxima
                  series_pos_peaks.Points.Remove(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1]);
                  series_onsets.Points.Remove(series_onsets.Points[series_onsets.Points.Count - 1]);
                  series_pos_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                  series_onsets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
                }
              }
            }

            // Initialization for analyzing negative peak
            positive = false;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          // If middle of positive spike
          else
          {
            if (Math.Abs(series.Points[x].Y) > Math.Abs(series.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
        // If negative spike
        else
        {
          // If end of negative spike
          if (series.Points[x].Y > 0 || x == series.Points.Count - 1)
          {
            // If spike is appropriate length
            if (spike_length > min_spike_length)
            {
              if (
                  // If user does not mind consequent peaks of same sign
                  !RemoveMultiplePeaks ||
                  // If first negative peak
                  series_neg_peaks.Points.Count == 0 ||
                  // If last peak was positive 
                  (series_pos_peaks.Points.Count != 0 &&
                  DateTimeAxis.ToDateTime(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].X) <
                  DateTimeAxis.ToDateTime(series_pos_peaks.Points[series_pos_peaks.Points.Count - 1].X))
                )
              {
                // Add new negative peak and onset 
                series_neg_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                series_insets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
              }
              else
              {
                // If this peak is less than the previous
                if (series.Points[maxima].Y < series_neg_peaks.Points[series_neg_peaks.Points.Count - 1].Y)
                {
                  // Replace previous spike maxima with latest spike maxima
                  series_neg_peaks.Points.Remove(series_neg_peaks.Points[series_neg_peaks.Points.Count - 1]);
                  series_insets.Points.Remove(series_insets.Points[series_insets.Points.Count - 1]);
                  series_neg_peaks.Points.Add(new ScatterPoint(series.Points[maxima].X, series.Points[maxima].Y));
                  series_insets.Points.Add(new ScatterPoint(series.Points[start].X, series.Points[start].Y));
                }
              }
            }

            // Initialization for analyzing positive peak
            positive = true;
            spike_length = 1;
            maxima = x;
            start = x;
          }
          // If middle of negative spike
          else
          {
            if (Math.Abs(series.Points[x].Y) > Math.Abs(series.Points[maxima].Y))
              maxima = x;
            spike_length++;
          }
        }
      }

      return new ScatterSeries[] { series_insets, series_onsets, series_neg_peaks, series_pos_peaks };
    }
    
    private static double GetVarianceCoefficient(double[] values)
    {
      double mean = values.Average();
      double variance = 0;
      for (int x = 0; x < values.Length; x++)
      {
        variance += Math.Abs(values[x] - mean);
      }
      variance /= values.Length;
      return variance / Math.Abs(mean);
    }
    private static Tuple<double, double> GetRespiratorySignalBreathingPeriod(ScatterSeries[] series)
    {
      // Find Breathing Rates
      List<double> breathing_periods = new List<double>();
      for (int x = 0; x < series.Length; x++)
      {
        for (int y = 1; y < series[x].Points.Count; y++)
          breathing_periods.Add((DateTimeAxis.ToDateTime(series[x].Points[y].X) - DateTimeAxis.ToDateTime(series[x].Points[y - 1].X)).TotalSeconds);
      }

      if (breathing_periods.Count != 0) // Non-Zero Breathing Rates
      {
        // Calculate Mean 
        double mean = breathing_periods.Average();

        // Calculate Variance
        double coeff_variance = GetVarianceCoefficient(breathing_periods.ToArray());

        return new Tuple<double, double>(mean, coeff_variance);
      }
      else
      {
        return new Tuple<double, double>(0, 0);
      }
    }
    private static Tuple<double, double> GetRespiratorySignalBreathingHalfPeriod(ScatterSeries series_1, ScatterSeries series_2)
    {
      if (series_1.Points.Count > 0 && series_2.Points.Count > 0)
      {
        int index_1 = 0;
        int index_2;
        if (DateTimeAxis.ToDateTime(series_1.Points[0].X) < DateTimeAxis.ToDateTime(series_2.Points[0].X))
          index_2 = 0;
        else
          index_2 = 1;

        List<double> half_periods = new List<double>();
        while (index_2 < series_2.Points.Count && index_1 < series_1.Points.Count)
        {
          half_periods.Add((DateTimeAxis.ToDateTime(series_2.Points[index_2].X) - DateTimeAxis.ToDateTime(series_1.Points[index_1].X)).TotalSeconds);

          index_1++;
          index_2++;
        }

        if (half_periods.Count != 0)
        {
          // Calculate Mean 
          double mean = half_periods.Average();

          // Calculate Variance
          double coeff_variance = GetVarianceCoefficient(half_periods.ToArray());

          return new Tuple<double, double>(mean, coeff_variance);
        }
        else
        {
          return new Tuple<double, double>(0, 0);
        }
      }
      else
      {
        return new Tuple<double, double>(0, 0);
      }
    }
    private static Tuple<double, double> GetRespiratorySignalPeakHeight(ScatterSeries series_peaks)
    {
      List<double> peak_heights = series_peaks.Points.Select(temp => temp.Y).ToList();
      if (peak_heights.Count != 0)
      {
        // Calculate Mean 
        double mean = peak_heights.Average();

        // Calculate Variance
        double coeff_variance = GetVarianceCoefficient(peak_heights.ToArray());

        return new Tuple<double, double>(mean, coeff_variance);
      }
      else
      {
        return new Tuple<double, double>(0, 0);
      }
    }
    private static Tuple<double, double> GetRespiratorySignalFlowVolume(LineSeries series, ScatterSeries series_1, ScatterSeries series_2, float sample_period)
    {
      if (series_1.Points.Count > 0 && series_2.Points.Count > 0)
      {
        int index_1 = 0;
        int index_2;
        if (DateTimeAxis.ToDateTime(series_1.Points[0].X) < DateTimeAxis.ToDateTime(series_2.Points[0].X))
          index_2 = 0;
        else
          index_2 = 1;

        List<double> integral_sums = new List<double>();
        while (index_2 < series_2.Points.Count && index_1 < series_1.Points.Count)
        {
          DateTime EndTime = DateTimeAxis.ToDateTime(series_2.Points[index_2].X);
          DateTime StartTime = DateTimeAxis.ToDateTime(series_1.Points[index_1].X);

          double integral_sum = 0;
          int start_index = series.Points.IndexOf(series.Points.Find(temp => temp.X == DateTimeAxis.ToDouble(StartTime)));
          int end_index = series.Points.IndexOf(series.Points.Find(temp => temp.X == DateTimeAxis.ToDouble(EndTime)));
          for (int x = start_index; x <= end_index; x++)
          {
            integral_sum += series.Points[x].Y * sample_period;
          }
          integral_sums.Add(integral_sum);

          index_1++;
          index_2++;
        }

        if (integral_sums.Count != 0)
        {
          // Calculate Mean 
          double mean = integral_sums.Average();

          // Calculate Variance
          double coeff_variance = GetVarianceCoefficient(integral_sums.ToArray());

          return new Tuple<double, double>(mean, coeff_variance);
        }
        else
        {
          return new Tuple<double, double>(0, 0);
        }
      }
      else
      {
        return new Tuple<double, double>(0, 0);
      }
    }
    public static double[] GetRespAnalysisInfo(LineSeries series_in, int start_epoch, int curr_epoch, float sample_period, float MinimumPeakWidth)
    {
      int start_index = (int) ((curr_epoch - start_epoch) * 30 / sample_period);
      int count = (int)(30 / sample_period);

      List<Series> series = new List<Series>();
      series.Add(new LineSeries());
      try {
        ((LineSeries)series[0]).Points.AddRange(series_in.Points.GetRange(start_index, count));
      } catch{}

      int min_spike_length = (int)((double)((double)MinimumPeakWidth / (double)1000) / (double)sample_period);
      series.AddRange(GetPeaksAndOnsets((LineSeries)series[0], true, min_spike_length));
      double[] output = new double[14];

      Tuple<double, double> breathing_periods = RespiratoryFactory.GetRespiratorySignalBreathingPeriod(new ScatterSeries[] { (ScatterSeries)series[2], (ScatterSeries)series[1] });
      output[0] = breathing_periods.Item1;
      output[1] = breathing_periods.Item2;

      Tuple<double, double> inspir_periods = RespiratoryFactory.GetRespiratorySignalBreathingHalfPeriod((ScatterSeries)series[1], (ScatterSeries)series[2]);
      output[2] = inspir_periods.Item1;
      output[3] = inspir_periods.Item2;

      Tuple<double, double> exspir_periods = RespiratoryFactory.GetRespiratorySignalBreathingHalfPeriod((ScatterSeries)series[2], (ScatterSeries)series[1]);
      output[4] = exspir_periods.Item1;
      output[5] = exspir_periods.Item2;

      Tuple<double, double> neg_peaks = RespiratoryFactory.GetRespiratorySignalPeakHeight((ScatterSeries)series[3]);
      output[6] = neg_peaks.Item1;
      output[7] = neg_peaks.Item2;

      Tuple<double, double> pos_peaks = RespiratoryFactory.GetRespiratorySignalPeakHeight((ScatterSeries)series[4]);
      output[8] = pos_peaks.Item1;
      output[9] = pos_peaks.Item2;

      Tuple<double, double> inspir_volume = RespiratoryFactory.GetRespiratorySignalFlowVolume((LineSeries)series[0], (ScatterSeries)series[1], (ScatterSeries)series[2], sample_period);
      output[10] = inspir_volume.Item1;
      output[11] = inspir_volume.Item2;

      Tuple<double, double> exspir_volume = RespiratoryFactory.GetRespiratorySignalFlowVolume((LineSeries)series[0], (ScatterSeries)series[2], (ScatterSeries)series[1], sample_period);
      output[12] = exspir_volume.Item1;
      output[13] = exspir_volume.Item2;

      return output;
    }
    
    public static PlotModel GetRespiratorySignalPlot(string SignalName, List<float> yValues, float sample_period, float bias, bool RemoveMultiplePeaks, float MinimumPeakWidth, DateTime ViewStartTime, DateTime ViewEndTime)
    {
      // Variable To Return
      LineSeries series = new LineSeries();

      //  // Add Points to Series
      for (int y = 0; y < yValues.Count; y++)
      {
        series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(ViewStartTime + new TimeSpan(0, 0, 0, 0, (int)(sample_period * (float)y * 1000))), yValues[y]));
      }

      LineSeries series_norm = RemoveBiasFromSignal(series, bias);

      // Find Peaks and Zero Crossings
      int min_spike_length = (int)((double)((double)MinimumPeakWidth / (double)1000) / (double)sample_period);
      ScatterSeries[] output = GetPeaksAndOnsets(series_norm, RemoveMultiplePeaks, min_spike_length);
      ScatterSeries series_insets = output[0];
      ScatterSeries series_onsets = output[1];
      ScatterSeries series_neg_peaks = output[2];
      ScatterSeries series_pos_peaks = output[3];

      // Modify Series colors
      series_onsets.MarkerFill = OxyColor.FromRgb(255, 0, 0);
      series_insets.MarkerFill = OxyColor.FromRgb(0, 255, 0);
      series_pos_peaks.MarkerFill = OxyColor.FromRgb(0, 0, 255);
      series_neg_peaks.MarkerFill = OxyColor.FromRgb(255, 255, 0);

      // Bind to Axes
      series_norm.YAxisKey = SignalName;
      series_norm.XAxisKey = "DateTime";
      series_onsets.YAxisKey = SignalName;
      series_onsets.XAxisKey = "DateTime";
      series_insets.YAxisKey = SignalName;
      series_insets.XAxisKey = "DateTime";
      series_pos_peaks.YAxisKey = SignalName;
      series_pos_peaks.XAxisKey = "DateTime";
      series_neg_peaks.YAxisKey = SignalName;
      series_neg_peaks.XAxisKey = "DateTime";

      // Configure Axes
      DateTimeAxis xAxis = new DateTimeAxis();
      xAxis.Key = "DateTime";
      xAxis.Minimum = DateTimeAxis.ToDouble(ViewStartTime);
      xAxis.Maximum = DateTimeAxis.ToDouble(ViewEndTime);

      LinearAxis yAxis = new LinearAxis();
      yAxis.MajorGridlineStyle = LineStyle.Solid;
      yAxis.MinorGridlineStyle = LineStyle.Dot;
      yAxis.Title = SignalName;
      yAxis.Key = SignalName;

      PlotModel tempPlotModel = new PlotModel();

      tempPlotModel.Series.Add(series_norm);
      tempPlotModel.Series.Add(series_onsets);
      tempPlotModel.Series.Add(series_insets);
      tempPlotModel.Series.Add(series_neg_peaks);
      tempPlotModel.Series.Add(series_pos_peaks);
      tempPlotModel.Axes.Add(xAxis);
      tempPlotModel.Axes.Add(yAxis);

      return tempPlotModel;
    }
    public static PlotModel GetRespiratoryAnalyticsPlot(LineSeries resp_plot, string[] epochs, int start, float sample_period, float MinimumPeakWidth)
    {
      // Create Series
      List<LineSeries> all_series = new List<LineSeries>();
      for (int y = 0; y < 7; y++)
      {
        all_series.Add(new LineSeries());
        all_series[y].XAxisKey = "X";
      }
      all_series[0].Title = "Breathing Period";
      all_series[1].Title = "Inspiration Period";
      all_series[2].Title = "Expiration Period";
      all_series[3].Title = "Negative Peak";
      all_series[4].Title = "Positive Peak";
      all_series[5].Title = "Inspiration Volume";
      all_series[6].Title = "Expiration Volume";

      // Link Series to Axes
      all_series[0].YAxisKey = "Y0";
      all_series[1].YAxisKey = "Y0";
      all_series[2].YAxisKey = "Y0";
      all_series[3].YAxisKey = "Y1";
      all_series[4].YAxisKey = "Y1";
      all_series[5].YAxisKey = "Y2";
      all_series[6].YAxisKey = "Y2";

      // Populate Series with Points
      for (int x = 0; x < epochs.Length; x++)
      {
        double[] output;
        output = RespiratoryFactory.GetRespAnalysisInfo(resp_plot, start, Int32.Parse(epochs[x]), sample_period, MinimumPeakWidth);
       
        for (int y = 0; y < output.Length; y += 2)
        {
          all_series[y / 2].Points.Add(new DataPoint(Int32.Parse(epochs[x]), output[y]));
        }
      }

      // Create Y Axes 
      List<LinearAxis> y_axis = new List<LinearAxis>();
      y_axis.Add(new LinearAxis());
      y_axis.Add(new LinearAxis());
      y_axis.Add(new LinearAxis());
      y_axis[0].Key = "Y0";
      y_axis[0].Title = "Periods";
      y_axis[0].StartPosition = 0;
      y_axis[0].EndPosition = 0.333;
      y_axis[0].MajorGridlineStyle = LineStyle.Solid;
      y_axis[0].MinorGridlineStyle = LineStyle.Dot;
      y_axis[1].Key = "Y1";
      y_axis[1].Title = "Peaks";
      y_axis[1].StartPosition = 0.333;
      y_axis[1].EndPosition = 0.666;
      y_axis[1].MajorGridlineStyle = LineStyle.Solid;
      y_axis[1].MinorGridlineStyle = LineStyle.Dot;
      y_axis[2].Key = "Y2";
      y_axis[2].Title = "Volumes";
      y_axis[2].StartPosition = 0.666;
      y_axis[2].EndPosition = 1;
      y_axis[2].MajorGridlineStyle = LineStyle.Solid;
      y_axis[2].MinorGridlineStyle = LineStyle.Dot;

      // Create X Axis
      LinearAxis x_axis = new LinearAxis();
      x_axis.Key = "X";
      x_axis.Position = AxisPosition.Bottom;
      x_axis.Minimum = Int32.Parse(epochs[0]);
      x_axis.Maximum = Int32.Parse(epochs[epochs.Length - 1]);

      // Create Plot Model 
      PlotModel temp_PlotModel = new PlotModel();

      for (int x = 0; x < all_series.Count; x++)
      {
        temp_PlotModel.Series.Add(all_series[x]);
      }
      temp_PlotModel.Axes.Add(x_axis);
      temp_PlotModel.Axes.Add(y_axis[0]);
      temp_PlotModel.Axes.Add(y_axis[1]);
      temp_PlotModel.Axes.Add(y_axis[2]);
      temp_PlotModel.IsLegendVisible = false;

      temp_PlotModel.IsLegendVisible = true;
      return temp_PlotModel;
    }

    public static void SaveRespiratoryAnalysisToExcel(string fileName, string SignalName, List<string[]> signalProperties, DateTime StartTime, PlotModel plot, float sample_period)
    {
      const int COLUMNS = 7;

      List<object[,]> signal_points = new List<object[,]>();
      #region Get Points
      {
        List<DataPoint> series = ((LineSeries)plot.Series[0]).Points;
        List<ScatterPoint> insets = ((ScatterSeries)plot.Series[1]).Points;
        List<ScatterPoint> onsets = ((ScatterSeries)plot.Series[2]).Points;
        List<ScatterPoint> negpeaks = ((ScatterSeries)plot.Series[3]).Points;
        List<ScatterPoint> pospeaks = ((ScatterSeries)plot.Series[4]).Points;

        int count_in = 0, count_on = 0, count_pos = 0, count_neg = 0;
        int epoch = -1;
        int index = -1;
        int row = -1;
        for (int x = 0; x < series.Count; x++)
        {
          if (epoch != Utils.DateTimetoEpoch(DateTimeAxis.ToDateTime(series[x].X), StartTime))
          {
            epoch = Utils.DateTimetoEpoch(DateTimeAxis.ToDateTime(series[x].X), StartTime);
            index = signal_points.Count;
            row = 0;
            signal_points.Add(new object[(int)(30 / sample_period) + 1, COLUMNS]);

            // Table Header
            signal_points[index][row, 0] = "Epoch";
            signal_points[index][row, 1] = "Date Time";
            signal_points[index][row, 2] = "Value";
            signal_points[index][row, 3] = "Inspiration";
            signal_points[index][row, 4] = "Expiration";
            signal_points[index][row, 5] = "Neg. Peaks";
            signal_points[index][row, 6] = "Pos. Peaks";
            row++;
          }

          signal_points[index][row, 0] = Utils.DateTimetoEpoch(DateTimeAxis.ToDateTime(series[x].X), StartTime);
          signal_points[index][row, 1] = DateTimeAxis.ToDateTime(series[x].X).ToString("MM/dd/yyyy hh:mm:ss.fff tt");
          signal_points[index][row, 2] = series[x].Y;

          if (count_in < insets.Count && insets[count_in].X == series[x].X)
          {
            signal_points[index][row, 3] = series[x].Y;
            count_in++;
          }
          if (count_on < onsets.Count && onsets[count_on].X == series[x].X)
          {
            signal_points[index][row, 4] = series[x].Y;
            count_on++;
          }
          if (count_neg < negpeaks.Count && negpeaks[count_neg].X == series[x].X)
          {
            signal_points[index][row, 5] = series[x].Y;
            count_neg++;
          }
          if (count_pos < pospeaks.Count && pospeaks[count_pos].X == series[x].X)
          {
            signal_points[index][row, 6] = series[x].Y;
            count_pos++;
          }
          row++;
        }
      }
      #endregion 

      Excel.Application app = new Excel.Application();
      Utils.MakeExcelInteropPerformant(app, true);

      Excel.Workbook wb = app.Workbooks.Add(System.Reflection.Missing.Value);
      
      #region Sheet 2 -> Sheet N
      {
        for (int x = signal_points.Count - 1; x >= 0 ; x--)
        {
          object[,] epoch_points = signal_points[x];
          int ROWS = (epoch_points.Length / COLUMNS);
          int EPOCH = Int32.Parse(epoch_points[1, 0].ToString());

          Excel.Worksheet ws = (Excel.Worksheet)wb.Sheets.Add();
          ws.Name = "Epoch" + EPOCH;
          Utils.AddRespiratorySignalToWorksheet(ws, SignalName, epoch_points, ROWS, COLUMNS);

          System.Runtime.InteropServices.Marshal.ReleaseComObject(ws);
          ws = null;
        }
      }
      #endregion

      #region Sheet 1
      {
        Excel.Worksheet ws = (Excel.Worksheet)wb.Sheets.Add();

        ws.Name = "Analysis";

        ws.Cells[1, 1].Value = "Signal";
        ws.Cells[1, 2].Value = SignalName;
        ws.Cells[1, 1].Font.Bold = true;

        ws.Cells[3, 1].Value = "Date Time";
        ws.Cells[3, 2].Value = "Epoch";
        ws.Cells[3, 3].Value = "Breathing Period";
        ws.Cells[3, 4].Value = "Expiration Period";
        ws.Cells[3, 5].Value = "Inspiration Period";
        ws.Cells[3, 6].Value = "Negative Peak";
        ws.Cells[3, 7].Value = "Positive Peak";
        ws.Cells[3, 8].Value = "Negative Volume";
        ws.Cells[3, 9].Value = "Positive Volume";

        for (int x = 0; x < signalProperties.Count; x++)
        {
          ws.Cells[4 + x, 1].Value = Utils.EpochtoDateTime(Int32.Parse(signalProperties[x][0]), StartTime);
          ws.Cells[4 + x, 2].Value = signalProperties[x][0];
          ws.Cells[4 + x, 3].Value = signalProperties[x][1];
          ws.Cells[4 + x, 4].Value = signalProperties[x][2];
          ws.Cells[4 + x, 5].Value = signalProperties[x][3];
          ws.Cells[4 + x, 6].Value = signalProperties[x][4];
          ws.Cells[4 + x, 7].Value = signalProperties[x][5];
          ws.Cells[4 + x, 8].Value = signalProperties[x][6];
          ws.Cells[4 + x, 9].Value = signalProperties[x][7];
        }

        ws.Range[ws.Cells[3, 1], ws.Cells[3 + signalProperties.Count, 9]].Columns[1].NumberFormat = "m/d/yyyy h:mm:ss";
        ws.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, ws.Range[ws.Cells[3, 1], ws.Cells[3 + signalProperties.Count, 9]], System.Reflection.Missing.Value, Excel.XlYesNoGuess.xlGuess, System.Reflection.Missing.Value).Name = "SignalProperties";
        ws.ListObjects["SignalProperties"].TableStyle = "TableStyleLight9";
        ws.Columns["A:J"].ColumnWidth = 20;
        ws.Columns["B:I"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

        var excel_chart = ((Excel.ChartObject)((Excel.ChartObjects)ws.ChartObjects()).Add(1000, 50, 900, 500)).Chart;
        excel_chart.SetSourceData(ws.Range[ws.Cells[3, 2], ws.Cells[3 + signalProperties.Count, 9]]);
        excel_chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xlXYScatterLines;
        excel_chart.ChartWizard(Source: ws.Range[ws.Cells[3, 2], ws.Cells[3 + signalProperties.Count, 9]], Title: "Flow Signal Analytics", CategoryTitle: "Epoch", ValueTitle: "");
        excel_chart.PlotVisibleOnly = false;
        ((Excel.Series)excel_chart.SeriesCollection(1)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(2)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(3)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(4)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(5)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(6)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;
        ((Excel.Series)excel_chart.SeriesCollection(7)).ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;

        System.Runtime.InteropServices.Marshal.ReleaseComObject(ws);
      }
      #endregion
      
      wb.SaveAs(fileName);
      Utils.MakeExcelInteropPerformant(app, false);
    }

    #endregion
  }

  /// <summary>
  /// Model for variables used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryModel
  {
    #region Members

    // EDF Signal Selection

    /// <summary>
    /// The user selected signal to perform respiratory analysis on
    /// </summary>
    public string RespiratoryEDFSelectedSignal;
    /// <summary>
    /// The user selected start time for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryEDFStartRecord;
    /// <summary>
    /// The user selected period for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryEDFDuration;

    // Binary Signal Selection

    /// <summary>
    /// Structure containing binary file information
    /// </summary>
    public BinaryFile LoadedBinaryFile;
    /// <summary>
    /// The user selected start time for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryBinaryStartRecord;
    /// <summary>
    /// The user selected period for the respiratory analysis in 30s epochs
    /// </summary>
    public int RespiratoryBinaryDuration;

    // Output Plot
    public bool RespiratoryDisplayAnalytics;
    /// <summary>
    /// The respiratory signal plot to be displayed
    /// </summary>
    public PlotModel RespiratorySignalPlot = null;
    /// <summary>
    /// The respiratory analytic plot to be displayed
    /// </summary>
    public PlotModel RespiratoryAnalyticsPlot;

    // Output Analysis
    /// <summary>
    /// Epoch to provide detailed analysis on
    /// </summary>
    public string RespiratoryAnalyticsSelectedEpoch;
    
    /// <summary>
    /// The calculated mean average of the periods of the respiratory signal
    /// </summary>
    public string RespiratoryBreathingPeriodMean;
    /// <summary>
    /// The calculated coefficient of variance of the periods of the respiratory signal
    /// </summary>
    public string RespiratoryBreathingPeriodCoeffVar;

    /// <summary>
    /// The calculated mean average of the inspiration periods of the respiratory signal
    /// </summary>
    public string RespiratoryInspirationPeriodMean;
    /// <summary>
    /// The calculated coefficient of variance of the inspiration periods of the respiratory signal
    /// </summary>
    public string RespiratoryInspirationPeriodCoeffVar;

    /// <summary>
    /// The calculated mean average of the Expiration periods of the respiratory signal
    /// </summary>
    public string RespiratoryExpirationPeriodMean;
    /// <summary>
    /// The calculated coefficient of variance of the Expiration periods of the respiratory signal
    /// </summary>
    public string RespiratoryExpirationPeriodCoeffVar;

    /// <summary>
    /// The calculated mean average of the positive peaks of the respiratory signal
    /// </summary>
    public string RespiratoryPositivePeakMean;
    /// <summary>
    /// The calculated coefficient of variance of the positive peaks of the respiratory signal
    /// </summary>
    public string RespiratoryPositivePeakCoeffVar;

    /// <summary>
    /// The calculated mean average of the negative peaks of the respiratory signal
    /// </summary>
    public string RespiratoryNegativePeakMean;
    /// <summary>
    /// The calculated coefficient of variance of the negative peaks of the respiratory signal
    /// </summary>
    public string RespiratoryNegativePeakCoeffVar;

    /// <summary>
    /// The calculated mean average of the signal integral of the positive peaks of the respiratory signal
    /// </summary>
    public string RespiratoryExpirationVolumeMean;
    /// <summary>
    /// The calculated coefficient of variance of the signal integral of the positive peaks of the respiratory signal
    /// </summary>
    public string RespiratoryExpirationVolumeCoeffVar;

    /// <summary>
    /// The calculated mean average of the signal integral of the negative peaks of the respiratory signal
    /// </summary>
    public string RespiratoryInspirationVolumeMean;
    /// <summary>
    /// The calculated coefficient of variance of the signal integral of the negative peaks of the respiratory signal
    /// </summary>
    public string RespiratoryInspirationVolumeCoeffVar;

    // Settings and Options

    /// <summary>
    /// A user selected option for setting the sensitivity of the peak detection of the analysis
    /// Effect where the insets, onsets, and peaks are detected
    /// Any "spike" that is less wide than the user setting in ms will be ignored
    /// </summary>
    public int RespiratoryMinimumPeakWidth = 500;
    /// <summary>
    /// If true, use a constant axis
    /// If false, auto adjust to plot
    /// </summary>
    public bool RespiratoryUseConstantAxis = false;
    /// <summary>
    /// If true, the analysis was performed on a binary file
    /// If false, the analysis was performed on an EDF file
    /// </summary>
    public bool IsAnalysisFromBinary = false;

    // Freeze UI when Performing Analysis 
    /// <summary>
    /// True if the program is performing analysis and a progress ring should be shown
    /// </summary>
    public bool RespiratoryProgressRingEnabled = false;
    
    #endregion
  }

  /// <summary>
  /// ModelView containing UI logic used exclusively in the 'Respiratory' sub tab of the 'Analysis' tab
  /// </summary>
  public class RespiratoryModelView : INotifyPropertyChanged
  {
    #region Shared Properties and Functions

    private SettingsModelView svm;
    private SettingsModel sm
    {
      get
      {
        return svm.sm;
      }
    }

    // Property Changed Listener
    private void Exterior_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case nameof(IsEDFLoaded):
          if (!IsEDFLoaded)
          {
            if (!IsAnalysisFromBinary)
            {
              RespiratorySignalPlot = null;
              RespiratoryAnalyticsPlot = null;
              RespiratoryAnalyticsSelectedEpoch = null;
            }
            RespiratoryEDFSelectedSignal = null;
            RespiratoryEDFDuration = null;
            RespiratoryEDFStartRecord = null;
          }
          else
          {
            RespiratoryEDFSelectedSignal = null;
            RespiratoryEDFStartRecord = 1;
            RespiratoryEDFDuration = 1;
          }
          OnPropertyChanged(nameof(RespiratoryEDFNavigationEnabled));
          OnPropertyChanged(nameof(IsEDFLoaded));
          break;
        default:
          OnPropertyChanged(e.PropertyName);
          break;
      }
    }
    
    // Shared Properties
    public EDFFile LoadedEDFFile
    {
      get
      {
        return svm.LoadedEDFFile;
      }
    }
    public DateTime EDFStartTime
    {
      get
      {
        if (IsEDFLoaded)
          return LoadedEDFFile.Header.StartDateTime;
        else
          return new DateTime();
      }
    }
    public DateTime EDFEndTime
    {
      get
      {
        if (IsEDFLoaded)
        {
          DateTime EndTime = LoadedEDFFile.Header.StartDateTime
                             + new TimeSpan(
                               (long)(TimeSpan.TicksPerSecond * LoadedEDFFile.Header.DurationOfDataRecordInSeconds * LoadedEDFFile.Header.NumberOfDataRecords)
                               );
          return EndTime;
        }
        else
          return new DateTime();
      }
    }

    public bool IsEDFLoaded
    {
      get
      {
        return svm.IsEDFLoaded;
      }
    }
    public ReadOnlyCollection<string> AllNonHiddenSignals
    {
      get
      {
        return svm.AllNonHiddenSignals;
      }
    }

    public bool UseDarkTheme
    {
      get
      {
        return sm.UseDarkTheme;
      }
    }

    // Shared Functions
    public LineSeries GetSeriesFromSignalName(out float sample_period, string Signal, DateTime StartTime, DateTime EndTime)
    {
      return svm.GetSeriesFromSignalName(out sample_period, Signal, StartTime, EndTime);
    }
    public float GetSamplePeriod(string Signal)
    {
      float sample_period;
      GetSeriesFromSignalName(out sample_period, Signal, EDFStartTime, EDFStartTime);
      return sample_period;
    }

    #endregion
    
    /// <summary>
    /// Respiratory Model
    /// </summary>
    private RespiratoryModel rm = new RespiratoryModel();

    #region Properties

    // Binary File Properties
    public bool IsBinaryLoaded
    {
      get
      {
        return LoadedBinaryFile != null;
      }
    }
    public BinaryFile LoadedBinaryFile
    {
      get
      {
        return rm.LoadedBinaryFile;
      }
      set
      {
        rm.LoadedBinaryFile = value;

        if (!IsBinaryLoaded)
        {
          RespiratoryBinaryDuration = null;
          RespiratoryBinaryStartRecord = null;
          if (IsAnalysisFromBinary)
          {
            RespiratorySignalPlot = null;
            RespiratoryAnalyticsPlot = null;
            RespiratoryAnalyticsSelectedEpoch = null;
          }
        }
        else
        {
          RespiratoryBinaryDuration = 1;
          RespiratoryBinaryStartRecord = 1;
        }

        OnPropertyChanged(nameof(LoadedBinaryFile));
        OnPropertyChanged(nameof(RespiratoryBinaryNavigationEnabled));
        OnPropertyChanged(nameof(IsBinaryLoaded));
        OnPropertyChanged(nameof(RespiratoryBinaryStartRecordMax));
        OnPropertyChanged(nameof(RespiratoryBinaryDurationMax));
        OnPropertyChanged(nameof(RespiratoryBinaryMaxEpochs));
      }
    }
    public DateTime BinaryStartTime
    {
      get
      {
        if (IsBinaryLoaded)
          return DateTime.Parse(LoadedBinaryFile.date_time_from);
        else
          return new DateTime();
      }
    }
    public DateTime BinaryEndTime
    {
      get
      {
        if (IsBinaryLoaded)
        {
          DateTime EndTime = BinaryStartTime + Utils.EpochPeriodtoTimeSpan(LoadedBinaryFile.max_epoch);
          return EndTime;
        }
        else
          return new DateTime();
      }
    }
    public int RespiratoryBinaryMaxEpochs
    {
      get
      {
        if (IsBinaryLoaded)
          return LoadedBinaryFile.max_epoch;
        else
          return 0;
      }
    }

    // Property Changed Functions
    private void RepiratoryPlot_Changed()
    {
      RespiratoryProgressRingEnabled = false;
    }
    private void RespiratoryEDFView_Changed()
    {
      IsAnalysisFromBinary = false;

      OnPropertyChanged(nameof(RespiratoryEDFSelectedSignal));

      OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
      OnPropertyChanged(nameof(RespiratoryEDFDuration));

      OnPropertyChanged(nameof(RespiratoryEDFStartRecordMax));
      OnPropertyChanged(nameof(RespiratoryEDFDurationMax));

      PerformRespiratoryAnalysisEDF(false);
    }
    private void RespiratoryBinaryView_Changed()
    {
      IsAnalysisFromBinary = true;

      OnPropertyChanged(nameof(RespiratoryBinaryStartRecord));
      OnPropertyChanged(nameof(RespiratoryBinaryDuration));

      OnPropertyChanged(nameof(RespiratoryBinaryStartRecordMax));
      OnPropertyChanged(nameof(RespiratoryBinaryDurationMax));

      PerformRespiratoryAnalysisBinary(false);
    }
    private void RespiratoryAnalysisSelectedEpoch_Changed()
    {
      UpdateRespAnalysisInfo(RespiratorySignalPlot);
    }

    // Settings and Options
    public bool SettingsRespiratoryVisible
    {
      get
      {
        return sm.SettingsRespiratoryVisible;
      }
      set
      {
        sm.SettingsRespiratoryVisible = value;
        OnPropertyChanged(nameof(SettingsRespiratoryVisible));
      }
    }
    public int RespiratoryMinimumPeakWidth
    {
      get
      {
        return rm.RespiratoryMinimumPeakWidth;
      }
      set
      {
        rm.RespiratoryMinimumPeakWidth = value;
        OnPropertyChanged(nameof(RespiratoryMinimumPeakWidth));
      }
    }
    public bool RespiratoryUseConstantAxis
    {
      get
      {
        return rm.RespiratoryUseConstantAxis;
      }
      set
      {
        rm.RespiratoryUseConstantAxis = value;
        OnPropertyChanged(nameof(RespiratoryUseConstantAxis));

        if (IsAnalysisFromBinary)
          PerformRespiratoryAnalysisBinary(false);
        else
          PerformRespiratoryAnalysisEDF(false);
      }
    }

    // EDF Signal Selection
    public string RespiratoryEDFSelectedSignal
    {
      get
      {
        return rm.RespiratoryEDFSelectedSignal;
      }
      set
      {
        rm.RespiratoryEDFSelectedSignal = value;
        RespiratoryEDFView_Changed();
      }
    }
    public int? RespiratoryEDFStartRecord
    {
      get
      {
        if (IsEDFLoaded)
          return rm.RespiratoryEDFStartRecord;
        else
          return null;
      }
      set
      {
        if (IsEDFLoaded && rm.RespiratoryEDFStartRecord != (value ?? 1))
        {
          rm.RespiratoryEDFStartRecord = value ?? 1;
          RespiratoryEDFView_Changed();
        }
      }
    }
    public int? RespiratoryEDFDuration
    {
      get
      {
        if (IsEDFLoaded)
          return rm.RespiratoryEDFDuration;
        else
          return null;
      }
      set
      {
        if (IsEDFLoaded && rm.RespiratoryEDFDuration != (value ?? 1))
        {
          rm.RespiratoryEDFDuration = value ?? 1;
          RespiratoryEDFView_Changed();
        }
      }
    }

    // Binary Signal Selection
    public int? RespiratoryBinaryStartRecord
    {
      get
      {
        if (IsBinaryLoaded)
          return rm.RespiratoryBinaryStartRecord;
        else
          return null;
      }
      set
      {
        if (IsBinaryLoaded && rm.RespiratoryBinaryStartRecord != (value ?? 1))
        {
          rm.RespiratoryBinaryStartRecord = value ?? 1;
          RespiratoryBinaryView_Changed();
        }
      }
    }
    public int? RespiratoryBinaryDuration
    {
      get
      {
        if (IsBinaryLoaded)
          return rm.RespiratoryBinaryDuration;
        else
          return null;
      }
      set
      {
        if (IsBinaryLoaded && rm.RespiratoryBinaryDuration != (value ?? 1))
        {
          rm.RespiratoryBinaryDuration = value ?? 1;
          RespiratoryBinaryView_Changed();
        }
      }
    }

    // Bounds on the EDF Signal Selection
    public int RespiratoryEDFStartRecordMax
    {
      get
      {
        if (LoadedEDFFile != null)
        {
          DateTime EndTime = EDFEndTime; // EDF End Time
          TimeSpan duration = Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1); // User Selected Duration 
          DateTime RespiratoryEDFStartTimeMax = EndTime - duration;
          return Utils.DateTimetoEpoch(RespiratoryEDFStartTimeMax, LoadedEDFFile); // RespiratoryViewStartTimeMax to Record
        }
        else
          return 0;
      }
    }
    public int RespiratoryEDFDurationMax
    {
      get
      {
        if (IsEDFLoaded) // File Loaded
        {
          DateTime RespiratoryEDFStartTime = Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile);
          DateTime EndTime = EDFEndTime; // EDF End Time
          TimeSpan duration = EndTime - (RespiratoryEDFStartTime); // Theoretical Limit Duration
          TimeSpan limit = new TimeSpan(TimeSpan.TicksPerHour * 2); // Practical Limit Duration

          return Math.Min(
              Utils.TimeSpantoEpochPeriod(limit),
              Utils.TimeSpantoEpochPeriod(duration)
              );
        }
        else // No File Loaded
          return 0;
      }
    }

    // Bounds on the Binary Signal Selection
    public int RespiratoryBinaryStartRecordMax
    {
      get
      {
        if (IsBinaryLoaded)
          return 1 + LoadedBinaryFile.max_epoch - RespiratoryBinaryDuration ?? 1;
        else
          return 0;
      }
    }
    public int RespiratoryBinaryDurationMax
    {
      get
      {
        if (IsBinaryLoaded)
          return 1 + LoadedBinaryFile.max_epoch - RespiratoryBinaryStartRecord ?? 1;
        else
          return 0;
      }
    }

    // Output Plot
    public bool IsAnalysisFromBinary
    {
      get
      {
        return rm.IsAnalysisFromBinary;
      }
      set
      {
        rm.IsAnalysisFromBinary = value;
      }
    }
    public PlotModel RespiratorySignalPlot
    {
      get
      {
        return rm.RespiratorySignalPlot;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        rm.RespiratorySignalPlot = value;
        OnPropertyChanged(nameof(RespiratorySignalPlot));
        OnPropertyChanged(nameof(RespiratoryAnalysisEnabled));
        RepiratoryPlot_Changed();
      }
    }

    // Output Analysis
    public string[] RespiratoryAnalyzedEpochs
    {
      get
      {
        if (IsAnalysisFromBinary)
        {
          List<string> return_value = new List<string>();
          for (int x = RespiratoryBinaryStartRecord ?? 1; x < RespiratoryBinaryStartRecord + RespiratoryBinaryDuration; x++)
            return_value.Add(x.ToString());
          return return_value.ToArray();
        }
        else
        {
          List<string> return_value = new List<string>();
          for (int x = RespiratoryEDFStartRecord ?? 1; x < RespiratoryEDFStartRecord + RespiratoryEDFDuration; x++)
            return_value.Add(x.ToString());
          return return_value.ToArray();
        }
      }
    }
    public string RespiratoryAnalyticsSelectedEpoch
    {
      get
      {
        return rm.RespiratoryAnalyticsSelectedEpoch;
      }
      set
      {
        rm.RespiratoryAnalyticsSelectedEpoch = value;
        OnPropertyChanged(nameof(RespiratoryAnalyticsSelectedEpoch));
        RespiratoryAnalysisSelectedEpoch_Changed();
      }
    }
    public string RespiratoryBreathingPeriodMean
    {
      get
      {
        return rm.RespiratoryBreathingPeriodMean;
      }
      set
      {
        rm.RespiratoryBreathingPeriodMean = value;
        OnPropertyChanged(nameof(RespiratoryBreathingPeriodMean));
      }
    }
    public string RespiratoryBreathingPeriodCoeffVar
    {
      get
      {
        return rm.RespiratoryBreathingPeriodCoeffVar;
      }
      set
      {
        rm.RespiratoryBreathingPeriodCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryBreathingPeriodCoeffVar));
      }
    }
    public string RespiratoryInspirationPeriodMean
    {
      get
      {
        return rm.RespiratoryInspirationPeriodMean;
      }
      set
      {
        rm.RespiratoryInspirationPeriodMean = value;
        OnPropertyChanged(nameof(RespiratoryInspirationPeriodMean));
      }
    }
    public string RespiratoryInspirationPeriodCoeffVar
    {
      get
      {
        return rm.RespiratoryInspirationPeriodCoeffVar;
      }
      set
      {
        rm.RespiratoryInspirationPeriodCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryInspirationPeriodCoeffVar));
      }
    }
    public string RespiratoryExpirationPeriodMean
    {
      get
      {
        return rm.RespiratoryExpirationPeriodMean;
      }
      set
      {
        rm.RespiratoryExpirationPeriodMean = value;
        OnPropertyChanged(nameof(RespiratoryExpirationPeriodMean));
      }
    }
    public string RespiratoryExpirationPeriodCoeffVar
    {
      get
      {
        return rm.RespiratoryExpirationPeriodCoeffVar;
      }
      set
      {
        rm.RespiratoryExpirationPeriodCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryExpirationPeriodCoeffVar));
      }
    }
    public string RespiratoryPositivePeakMean
    {
      get
      {
        return rm.RespiratoryPositivePeakMean;
      }
      set
      {
        rm.RespiratoryPositivePeakMean = value;
        OnPropertyChanged(nameof(RespiratoryPositivePeakMean));
      }
    }
    public string RespiratoryPositivePeakCoeffVar
    {
      get
      {
        return rm.RespiratoryPositivePeakCoeffVar;
      }
      set
      {
        rm.RespiratoryPositivePeakCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryPositivePeakCoeffVar));
      }
    }
    public string RespiratoryNegativePeakMean
    {
      get
      {
        return rm.RespiratoryNegativePeakMean;
      }
      set
      {
        rm.RespiratoryNegativePeakMean = value;
        OnPropertyChanged(nameof(RespiratoryNegativePeakMean));
      }
    }
    public string RespiratoryNegativePeakCoeffVar
    {
      get
      {
        return rm.RespiratoryNegativePeakCoeffVar;
      }
      set
      {
        rm.RespiratoryNegativePeakCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryNegativePeakCoeffVar));
      }
    }
    public string RespiratoryExpirationVolumeMean
    {
      get
      {
        return rm.RespiratoryExpirationVolumeMean;
      }
      set
      {
        rm.RespiratoryExpirationVolumeMean = value;
        OnPropertyChanged(nameof(RespiratoryExpirationVolumeMean));
      }
    }
    public string RespiratoryExpirationVolumeCoeffVar
    {
      get
      {
        return rm.RespiratoryExpirationVolumeCoeffVar;
      }
      set
      {
        rm.RespiratoryExpirationVolumeCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryExpirationVolumeCoeffVar));
      }
    }
    public string RespiratoryInspirationVolumeMean
    {
      get
      {
        return rm.RespiratoryInspirationVolumeMean;
      }
      set
      {
        rm.RespiratoryInspirationVolumeMean = value;
        OnPropertyChanged(nameof(RespiratoryInspirationVolumeMean));
      }
    }
    public string RespiratoryInspirationVolumeCoeffVar
    {
      get
      {
        return rm.RespiratoryInspirationVolumeCoeffVar;
      }
      set
      {
        rm.RespiratoryInspirationVolumeCoeffVar = value;
        OnPropertyChanged(nameof(RespiratoryInspirationVolumeCoeffVar));
      }
    }
    public bool RespiratoryDisplayAnalytics
    {
      get
      {
        return rm.RespiratoryDisplayAnalytics;
      }
      set
      {
        rm.RespiratoryDisplayAnalytics = value;
        OnPropertyChanged(nameof(RespiratoryDisplayAnalytics));
      }
    }
    public PlotModel RespiratoryAnalyticsPlot
    {
      get
      {
        return rm.RespiratoryAnalyticsPlot;
      }
      set
      {
        Utils.ApplyThemeToPlot(value, UseDarkTheme);
        rm.RespiratoryAnalyticsPlot = value;
        OnPropertyChanged(nameof(RespiratoryAnalyticsPlot));
      }
    }

    // Freeze UI when Performing Analysis 
    public bool RespiratoryProgressRingEnabled
    {
      get
      {
        return rm.RespiratoryProgressRingEnabled;
      }
      set
      {
        rm.RespiratoryProgressRingEnabled = value;
        OnPropertyChanged(nameof(RespiratoryProgressRingEnabled));
        OnPropertyChanged(nameof(RespiratoryEDFNavigationEnabled));
        OnPropertyChanged(nameof(RespiratoryAnalysisEnabled));
        OnPropertyChanged(nameof(RespiratoryBinaryNavigationEnabled));
      }
    }
    public bool RespiratoryEDFNavigationEnabled
    {
      get
      {
        if (!IsEDFLoaded)
          return false;
        else
          return !RespiratoryProgressRingEnabled;
      }
    }
    public bool RespiratoryBinaryNavigationEnabled
    {
      get
      {
        if (!IsBinaryLoaded)
          return false;
        else
          return !RespiratoryProgressRingEnabled;
      }
    }
    public bool RespiratoryAnalysisEnabled
    {
      get
      {
        if (!RespiratoryProgressRingEnabled)
          return RespiratorySignalPlot != null;
        else
          return false;
      }
    }
        
    #endregion

    #region Actions

    // Exporting Respiratory Analysis to Excel 
    /// <summary>
    /// Background process for exporting respiratory analysis
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void BW_ExportPlot_DoWork(object sender, DoWorkEventArgs e)
    {
      List<string[]> properties = new List<string[]>();
      for (int x = 0; x < RespiratoryAnalyzedEpochs.Length; x++)
      {
        
        double[] output = IsAnalysisFromBinary ?
        RespiratoryFactory.GetRespAnalysisInfo((LineSeries) RespiratorySignalPlot.Series[0], RespiratoryBinaryStartRecord ?? 1, Int32.Parse(RespiratoryAnalyzedEpochs[x]), LoadedBinaryFile.sample_period, RespiratoryMinimumPeakWidth) :
        RespiratoryFactory.GetRespAnalysisInfo((LineSeries)RespiratorySignalPlot.Series[0], RespiratoryEDFStartRecord ?? 1, Int32.Parse(RespiratoryAnalyzedEpochs[x]), GetSamplePeriod(RespiratoryEDFSelectedSignal), RespiratoryMinimumPeakWidth);

        properties.Add(new string[] { RespiratoryAnalyzedEpochs[x], output[0].ToString(), output[2].ToString(), output[4].ToString(), output[6].ToString(), output[8].ToString(), output[10].ToString(), output[12].ToString() });
      }
      
      string SignalName = IsAnalysisFromBinary ? LoadedBinaryFile.signal_name : RespiratoryEDFSelectedSignal;
      DateTime StartTime = IsAnalysisFromBinary ? BinaryStartTime : EDFStartTime;

      float sample_period = IsAnalysisFromBinary ? LoadedBinaryFile.sample_period : GetSamplePeriod(RespiratoryEDFSelectedSignal);
      RespiratoryFactory.SaveRespiratoryAnalysisToExcel(e.Argument.ToString(), SignalName, properties, StartTime, RespiratorySignalPlot, sample_period);
    }
    /// <summary>
    /// Called when exporting respiratory analysis finishes
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_ExportPlot_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      RespiratoryProgressRingEnabled = false;
    }
    /// <summary>
    /// Exports Respiratory Calculation to Excel file
    /// </summary>
    /// <param name="fileName"> The filename of the excel file to be created </param>
    /// <returns></returns>
    public void ExportRespiratoryPlot(string fileName)
    {
      RespiratoryProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += BW_ExportPlot_DoWork;
      bw.RunWorkerCompleted += BW_ExportPlot_RunWorkerCompleted;
      bw.RunWorkerAsync(fileName);
    }
    
    // Respiratory Analysis From Binary File
    /// <summary>
    /// Loads a binary file's contents into memory 
    /// </summary>
    public void LoadRespiratoryAnalysisBinary()
    {
      System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();

      dialog.Filter = "|*.bin";

      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        BinaryFile temp = new BinaryFile(dialog.FileName);
        LoadedBinaryFile = temp;
      }
      else
      {
        LoadedBinaryFile = null;
      }
    }

    /// <summary>
    /// Background process for performing respiratory analysis on binary contents stored into memory 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void BW_RespiratoryAnalysisBinary(object sender, DoWorkEventArgs e)
    {
      // Finding From 
      int modelStartRecord = RespiratoryBinaryStartRecord.Value;
      DateTime newFrom = BinaryStartTime;
      newFrom = newFrom.AddSeconds(30 * (modelStartRecord - 1));

      // Finding To 
      int modelLength = RespiratoryBinaryDuration.Value;
      DateTime newTo = newFrom;
      newTo = newTo.AddSeconds(30 * (modelLength));

      if (newFrom < BinaryStartTime)
        newFrom = BinaryStartTime;
      if (newTo < newFrom)
        newTo = newFrom;

      int start_index = (int)(((double)(newFrom - BinaryStartTime).TotalSeconds) / ((double)LoadedBinaryFile.sample_period));
      int end_index = (int)(((double)(newTo - BinaryStartTime).TotalSeconds) / ((double)LoadedBinaryFile.sample_period));
      start_index = Math.Max(start_index, 0);
      end_index = Math.Min(end_index, LoadedBinaryFile.signal_values.Count - 1);

      PlotModel resp_plot = RespiratoryFactory.GetRespiratorySignalPlot(
        LoadedBinaryFile.signal_name,
        LoadedBinaryFile.signal_values.GetRange(start_index, end_index - start_index + 1),
        LoadedBinaryFile.sample_period,
        LoadedBinaryFile.signal_values.Average(),
        true,
        RespiratoryMinimumPeakWidth,
        newFrom,
        newTo
      );

      if (RespiratoryUseConstantAxis)
      {
        resp_plot.Axes[1].Minimum = LoadedBinaryFile.signal_values.Min();
        resp_plot.Axes[1].Maximum = LoadedBinaryFile.signal_values.Max();
      }

      UpdateRespAnalysisPlot(resp_plot);
      UpdateRespAnalysisInfo(resp_plot);
      UpdateRespAnalysisInfoPlot(resp_plot, RespiratoryBinaryStartRecord ?? 1, LoadedBinaryFile.sample_period);
      OnPropertyChanged(nameof(RespiratoryAnalyzedEpochs));
    }
    /// <summary>
    /// Performs respiratory analysis on binary contents stored into memory 
    /// </summary>
    public void PerformRespiratoryAnalysisBinary(bool setfalse)
    {
      if (!RespiratoryBinaryNavigationEnabled)
        return;

      RespiratoryProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      if (setfalse)
        bw.DoWork += (object s, DoWorkEventArgs e) => { BW_RespiratoryAnalysisBinary(s, e); RespiratoryDisplayAnalytics = false; } ;
      else
        bw.DoWork += BW_RespiratoryAnalysisBinary;
      bw.RunWorkerAsync();
    }

    // Respiratory Analysis From EDF File
    /// <summary>
    /// Background process for performing respiratory analysis
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BW_RespiratoryAnalysisEDF(object sender, DoWorkEventArgs e)
    {
      float sample_period;
      LineSeries series = GetSeriesFromSignalName(out sample_period,
                                                  RespiratoryEDFSelectedSignal,
                                                  Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile),
                                                  Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1)
                                                  );

      
      PlotModel resp_plot = RespiratoryFactory.GetRespiratorySignalPlot(
        RespiratoryEDFSelectedSignal,
        series.Points.Select(temp => (float)temp.Y).ToList(),
        sample_period,
        (float)(Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, false, LoadedEDFFile, sm) - Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm)),
        true,
        RespiratoryMinimumPeakWidth,
        Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile),
        Utils.EpochtoDateTime(RespiratoryEDFStartRecord ?? 1, LoadedEDFFile) + Utils.EpochPeriodtoTimeSpan(RespiratoryEDFDuration ?? 1)
        );

      if (RespiratoryUseConstantAxis)
      {
        resp_plot.Axes[1].Minimum = Utils.GetMinSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm);
        resp_plot.Axes[1].Maximum = Utils.GetMaxSignalValue(RespiratoryEDFSelectedSignal, true, LoadedEDFFile, sm);
      }

      UpdateRespAnalysisPlot(resp_plot);
      UpdateRespAnalysisInfo(resp_plot);
      UpdateRespAnalysisInfoPlot(resp_plot, RespiratoryEDFStartRecord ?? 1, sample_period);
      OnPropertyChanged(nameof(RespiratoryAnalyzedEpochs));
    }
    /// <summary>
    /// Peforms respiratory analysis 
    /// </summary>
    public void PerformRespiratoryAnalysisEDF(bool setfalse)
    {
      if (RespiratoryEDFSelectedSignal == null)
        return;

      RespiratoryProgressRingEnabled = true;

      BackgroundWorker bw = new BackgroundWorker();
      if (setfalse)
        bw.DoWork += (object s, DoWorkEventArgs e) => { BW_RespiratoryAnalysisEDF(s,e); RespiratoryDisplayAnalytics = false; };
      else
        bw.DoWork += BW_RespiratoryAnalysisEDF;
      bw.RunWorkerAsync();
    }
    
    private void AnalyticsClickEvent(object sender, EventArgs e)
    {
      ScreenPoint position;
      try { position = ((OxyMouseDownEventArgs)e).Position; }
      catch { position = ((OxyTouchEventArgs)e).Position; };
      
      if (RespiratoryProgressRingEnabled)
        return;

      if (IsAnalysisFromBinary)
      {
        rm.RespiratoryBinaryStartRecord = Math.Max((int)Math.Round(((LineSeries)sender).InverseTransform(position).X) - 1, 1);
        OnPropertyChanged(nameof(RespiratoryBinaryStartRecord));
        OnPropertyChanged(nameof(RespiratoryBinaryDurationMax));

        rm.RespiratoryBinaryDuration = 2;
        OnPropertyChanged(nameof(RespiratoryBinaryDuration));
        OnPropertyChanged(nameof(RespiratoryBinaryStartRecordMax));

        PerformRespiratoryAnalysisBinary(true);
      }
      else
      {
        rm.RespiratoryEDFStartRecord = Math.Max((int)Math.Round(((LineSeries)sender).InverseTransform(position).X) - 1, 1);
        OnPropertyChanged(nameof(RespiratoryEDFStartRecord));
        OnPropertyChanged(nameof(RespiratoryEDFDurationMax));

        rm.RespiratoryEDFDuration = 2;
        OnPropertyChanged(nameof(RespiratoryEDFDuration));
        OnPropertyChanged(nameof(RespiratoryEDFStartRecordMax));

        PerformRespiratoryAnalysisEDF(true);
      }
    }
    private void UpdateRespAnalysisPlot(PlotModel resp_plot)
    {
      RespiratorySignalPlot = resp_plot;
    }
    private void UpdateRespAnalysisInfo(PlotModel resp_plot)
    {
      if (RespiratoryAnalysisEnabled && RespiratoryAnalyticsSelectedEpoch != null)
      {
        double[] output = IsAnalysisFromBinary ?
        RespiratoryFactory.GetRespAnalysisInfo((LineSeries)resp_plot.Series[0], RespiratoryBinaryStartRecord ?? 1, Int32.Parse(RespiratoryAnalyticsSelectedEpoch), LoadedBinaryFile.sample_period, RespiratoryMinimumPeakWidth) :
        RespiratoryFactory.GetRespAnalysisInfo((LineSeries)resp_plot.Series[0], RespiratoryEDFStartRecord ?? 1, Int32.Parse(RespiratoryAnalyticsSelectedEpoch), GetSamplePeriod(RespiratoryEDFSelectedSignal), RespiratoryMinimumPeakWidth);
        
        RespiratoryBreathingPeriodMean = output[0].ToString("0.## s");
        RespiratoryBreathingPeriodCoeffVar = output[1].ToString("0.## %");
        RespiratoryExpirationPeriodMean = output[2].ToString("0.## s");
        RespiratoryExpirationPeriodCoeffVar = output[3].ToString("0.## %");
        RespiratoryInspirationPeriodMean = output[4].ToString("0.## s");
        RespiratoryInspirationPeriodCoeffVar = output[5].ToString("0.## %");
        RespiratoryNegativePeakMean = output[6].ToString("0.##");
        RespiratoryNegativePeakCoeffVar = output[7].ToString("0.## %");
        RespiratoryPositivePeakMean = output[8].ToString("0.##");
        RespiratoryPositivePeakCoeffVar = output[9].ToString("0.## %");
        RespiratoryExpirationVolumeMean = output[10].ToString("0.##");
        RespiratoryExpirationVolumeCoeffVar = output[11].ToString("0.## %");
        RespiratoryInspirationVolumeMean = output[12].ToString("0.##");
        RespiratoryInspirationVolumeCoeffVar = output[13].ToString("0.## %");
      }
      else
      {
        RespiratoryBreathingPeriodMean = null;
        RespiratoryBreathingPeriodCoeffVar = null;
        RespiratoryInspirationPeriodMean = null;
        RespiratoryInspirationPeriodCoeffVar = null;
        RespiratoryExpirationPeriodMean = null;
        RespiratoryExpirationPeriodCoeffVar = null;
        RespiratoryNegativePeakMean = null;
        RespiratoryNegativePeakCoeffVar = null;
        RespiratoryPositivePeakMean = null;
        RespiratoryPositivePeakCoeffVar = null;
        RespiratoryInspirationVolumeMean = null;
        RespiratoryInspirationVolumeCoeffVar = null;
        RespiratoryExpirationVolumeMean = null;
        RespiratoryExpirationVolumeCoeffVar = null;
      }
    }
    private void UpdateRespAnalysisInfoPlot(PlotModel resp_plot, int start, float sample_period)
    {
      if (RespiratoryAnalysisEnabled)
      {
        RespiratoryAnalyticsPlot = RespiratoryFactory.GetRespiratoryAnalyticsPlot((LineSeries)resp_plot.Series[0], RespiratoryAnalyzedEpochs, start, sample_period, RespiratoryMinimumPeakWidth);
        for (int x = 0; x < RespiratoryAnalyticsPlot.Series.Count; x++)
        {
          RespiratoryAnalyticsPlot.Series[x].TouchStarted += AnalyticsClickEvent;
          RespiratoryAnalyticsPlot.Series[x].MouseDown += AnalyticsClickEvent;
        }
      }
      else
        RespiratoryAnalyticsPlot = null;
    }

    #endregion

    #region etc

    // INotify Interface
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public RespiratoryModelView(SettingsModelView i_svm)
    {
      svm = i_svm;
      svm.PropertyChanged += Exterior_PropertyChanged;
    }
  }
}
