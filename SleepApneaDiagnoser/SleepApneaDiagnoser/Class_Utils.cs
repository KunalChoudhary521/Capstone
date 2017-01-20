using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using EDF;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;
using MATLAB_496;
using System.IO;

namespace SleepApneaDiagnoser
{
  class Utils
  {
    /******************************************************* STATIC FUNCTIONS *******************************************************/

    /// <summary>
    /// The definition of epochs in seconds
    /// </summary>
    public static int EPOCH_SEC = 30;
    /// <summary>
    /// Converts an epoch point in time to a DateTime structure
    /// </summary>
    /// <param name="epoch"> The epoch point in time to convert </param>
    /// <param name="file"> 
    /// The EDFFile class used to determine the start 
    /// DateTime corresponding to epoch 0 
    /// </param>
    /// <returns> A DateTime structure corresponding the input epoch point in time </returns>
    public static DateTime EpochtoDateTime(int epoch, EDFFile file)
    {
      // DateTime = StartTime + epoch * EPOCH_SEC
      return file.Header.StartDateTime + new TimeSpan(0, 0, epoch * EPOCH_SEC);
    }
    /// <summary>
    /// Converts an epoch duration into a TimeSpan structure
    /// </summary>
    /// <param name="period"> The epoch duration to convert </param>
    /// <returns> The TimeSpan structure corresponding to the epoch duration </returns>
    public static TimeSpan EpochPeriodtoTimeSpan(int period)
    {
      // TimeSpan = period * EPOCH_SEC
      return new TimeSpan(0, 0, 0, period * EPOCH_SEC);
    }
    /// <summary>
    /// Converts a DateTime structure into an epoch point in time
    /// </summary>
    /// <param name="time"> The DateTime structure to convert </param>
    /// <param name="file"> 
    /// The EDFFile class used to determine the start
    /// DateTime corresponding to epoch 0 
    /// </param>
    /// <returns> The epoch point in time corresponding to the input DateTime </returns>
    public static int DateTimetoEpoch(DateTime time, EDFFile file)
    {
      // epoch = (DateTime - StartTime) / EPOCH_SEC
      return (int)((time - file.Header.StartDateTime).TotalSeconds / (double)EPOCH_SEC);
    }
    /// <summary>
    /// Converts a TimeSpan structure into an epoch duration
    /// </summary>
    /// <param name="period"> The TimeSpan structure to convert </param>
    /// <returns> The epoch duration corresponding to the input TimeSpan </returns>
    public static int TimeSpantoEpochPeriod(TimeSpan period)
    {
      // epoch = TimeSpan / EPOCH_SEC
      return (int)(period.TotalSeconds / (double)EPOCH_SEC);
    }

    /// <summary>
    /// Gets a value at a specified percentile from an array
    /// </summary>
    /// <param name="values_array"> The input array </param>
    /// <param name="percentile"> The percentile of the desired value </param>
    /// <returns> The desired value at the specified percentile </returns>
    public static double? GetPercentileValue(float[] values_array, int percentile)
    {
      // Sort values in ascending order
      List<float> values = values_array.ToList();
      values.Sort();

      // index = percent * length 
      int index = (int)((double)percentile / (double)100 * (double)values.Count);

      // return desired value
      return values[index];
    }
    /// <summary>
    /// Gets a value at a specified percentile from the difference between two arrays
    /// </summary>
    /// <param name="values_array_1"> The input minuend array </param>
    /// <param name="values_array_2"> The input subtrahend array </param>
    /// <param name="percentile"> The percentile of the desired value </param>
    /// <returns> The desired value at the specified percentile </returns>
    public static double? GetPercentileValueDeriv(float[] values_array_1, float[] values_array_2, int percentile)
    {
      // Subtract two input arrays from each other
      List<float> values1 = values_array_1.ToList();
      List<float> values2 = values_array_2.ToList();
      List<float> values = new List<float>();
      for (int x = 0; x < Math.Min(values_array_1.Length, values_array_2.Length); x++)
        values.Add(values_array_1[x] - values_array_2[x]);

      // Call GetPercentileValue on difference
      return GetPercentileValue(values.ToArray(), percentile);
    }

    /// <summary>
    /// Gets the signal samples from one period of time to another
    /// </summary>
    /// <param name="file"> The EDFFile class </param>
    /// <param name="signal_to_retrieve"> The signal to get samples from </param>
    /// <param name="StartTime"> The start time to get samples from </param>
    /// <param name="EndTime"> The end time to get samples from </param>
    /// <returns> A list of the retrieved samples </returns>
    public static List<float> retrieveSignalSampleValuesMod(EDFFile file, EDFSignal signal_to_retrieve, DateTime StartTime, DateTime EndTime)
    {
      int start_sample, start_record;
      int end_sample, end_record;
      #region Find Start and End Points
      // Duration of record in seconds
      double record_duration = file.Header.DurationOfDataRecordInSeconds;
      // Samples per record
      double samples_per_record = signal_to_retrieve.NumberOfSamplesPerDataRecord;
      // The sample period of the signal (Duration of Record)/(Samples per Record)
      double sample_period = record_duration / samples_per_record;
      {
        // Time of start point in seconds
        double total_seconds = (StartTime - file.Header.StartDateTime).TotalSeconds;
        // Time of start point in samples 
        double total_samples = total_seconds / sample_period;

        start_sample = ((int)(total_samples)) % ((int)samples_per_record); // Start Sample in Record
        start_record = (int)((total_samples - start_sample) / samples_per_record); // Start Record
      }
      {
        // Time of end point in seconds
        double total_seconds = (EndTime - file.Header.StartDateTime).TotalSeconds;
        // Time of end point in samples
        double total_samples = total_seconds / sample_period - 1;

        end_sample = ((int)total_samples) % ((int)samples_per_record); // End Sample in Record
        end_record = (((int)total_samples) - end_sample) / ((int)samples_per_record); // End Record
      }
      #endregion
      List<float> signalSampleValues = new List<float>();
      if (file.Header.Signals.Contains(signal_to_retrieve))
      {
        for (int x = start_record; x <= end_record; x++)
        {
          EDFDataRecord dr = file.DataRecords[x];
          foreach (EDFSignal signal in file.Header.Signals)
          {
            if (signal.IndexNumberWithLabel.Equals(signal_to_retrieve.IndexNumberWithLabel))
            {
              int start = x == start_record ? start_sample : 0;
              int end = x == end_record ? end_sample : dr[signal.IndexNumberWithLabel].Count - 1;
              for (int y = start; y <= end; y++)
              {
                signalSampleValues.Add(dr[signal.IndexNumberWithLabel][y]);
              }
            }
          }
        }
      }
      return signalSampleValues;
    }
    /// <summary>
    /// Performs upsampling and downsampling on an array of values
    /// </summary>
    /// <param name="values"> The input array to resample </param>
    /// <param name="ratio"> The ratio between upsampling and downsampling to perform </param>
    /// <returns> The resampled array </returns>
    public static List<float> MATLAB_Resample(float[] values, float ratio)
    {
      // Prepare Input for MATLAB function
      Processing proc = new Processing();
      MWArray[] input = new MWArray[2];
      input[0] = new MWNumericArray(values);
      input[1] = ratio;
      // Call MATLAB function
      return (
                  (double[])(
                      (MWNumericArray)proc.m_resample(1, input[0], input[1])[0]
                  ).ToVector(MWArrayComponent.Real)
                ).ToList().Select(temp => (float)temp).ToList();
    }
    /// <summary>
    /// Performs coherence analysis on 2 lists of values
    /// </summary>
    /// <param name="values1"> First list of values </param>
    /// <param name="values2"> Second list of values </param>
    /// <returns> Index 1 is Y axis, Index 2 is X axis </returns>
    public static LineSeries MATLAB_Coherence(float[] values1, float[] values2)
    {
      // Prepare Input for MATLAB function
      Processing proc = new Processing();
      MWArray[] input = new MWArray[3];
      input[0] = new MWNumericArray(values1.ToArray());
      input[1] = new MWNumericArray(values2.ToArray());
      input[2] = Math.Round(Math.Sqrt(Math.Max(values1.Length, values2.Length)));

      // Call MATLAB function
      MWArray[] output = proc.m_cohere(2, input[0], input[1], input[2]);
      double[] y_values = (double[])((MWNumericArray)output[0]).ToVector(MWArrayComponent.Real);
      double[] x_values = (double[])((MWNumericArray)output[1]).ToVector(MWArrayComponent.Real);

      LineSeries series = new LineSeries();
      for (int x = 0; x < y_values.Length; x++)
        series.Points.Add(new DataPoint(x_values[x], y_values[x]));

      return series;
    }

    // Settings Persistence
    public static void LoadCategoriesFile(string[] EDFAllSignals, string[][] DerivedSignals, SettingsModel sm)
    {
      sm.SignalCategories.Clear();
      sm.SignalCategoryContents.Clear();

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
            if (EDFAllSignals.ToList().Contains(line.Split(',')[y].Trim()) || DerivedSignals.ToList().Find(temp => temp[0].Trim() == line.Split(',')[y].Trim()) != null)
            {
              category_signals.Add(line.Split(',')[y]);
            }
          }

          if (category_signals.Count > 0)
          {
            sm.SignalCategories.Add((sm.SignalCategories.Count + 1) + ". " + category);
            sm.SignalCategoryContents.Add(category_signals);
          }
        }

        sr.Close();
      }
    }
    public static void WriteToCategoriesFile(string[] AllSignals, SettingsModel sm)
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

          if (!sm.SignalCategories.Contains(category))
          {
            for (int y = 0; y < AllSignals.Length; y++)
            {
              if (category_signals.Contains(AllSignals[y]))
                category_signals.Remove(AllSignals[y]);
            }
          }

          temp_SignalCategories.Add(category);
          temp_SignalCategoriesContents.Add(category_signals);
        }

        sr.Close();
      }

      for (int x = 0; x < sm.SignalCategories.Count; x++)
      {
        if (temp_SignalCategories.Contains(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim()))
        {
          int u = temp_SignalCategories.IndexOf(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents[u].AddRange(sm.SignalCategoryContents[x].ToArray());
          temp_SignalCategoriesContents[u] = temp_SignalCategoriesContents[u].Distinct().ToList();
        }
        else
        {
          temp_SignalCategories.Add(sm.SignalCategories[x].Substring(sm.SignalCategories[x].IndexOf('.') + 2).Trim());
          temp_SignalCategoriesContents.Add(sm.SignalCategoryContents[x]);
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
    public static void LoadCommonDerivativesFile(EDFFile LoadedEDFFile, SettingsModel sm)
    {
      sm.DerivedSignals.Clear();
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
                  if (sm.DerivedSignals.Where(temp => temp[0].Trim() == new_entry[0].Trim()).ToList().Count == 0) // Unique Name
                  {
                    sm.DerivedSignals.Add(new_entry);
                  }
                }
              }
            }
          }
        }
      }
    }
    public static void AddToCommonDerivativesFile(string name, string signal1, string signal2)
    {
      StreamWriter sw = new StreamWriter("common_derivatives.txt", true);
      sw.WriteLine(name + "," + signal1 + "," + signal2);
      sw.Close();
    }
    public static void RemoveFromCommonDerivativesFile(List<string[]> signals)
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
    public static void LoadHiddenSignalsFile(SettingsModel sm)
    {
      sm.HiddenSignals.Clear();
      if (File.Exists("hiddensignals.txt"))
      {
        StreamReader sr = new StreamReader("hiddensignals.txt");
        sm.HiddenSignals = sr.ReadToEnd().Replace("\r\n", "\n").Split('\n').ToList();
        sm.HiddenSignals = sm.HiddenSignals.Select(temp => temp.Trim()).Where(temp => temp != "").ToList();
        sr.Close();
      }
    }
    public static void WriteToHiddenSignalsFile(SettingsModel sm)
    {
      StreamWriter sw = new StreamWriter("hiddensignals.txt");
      for (int x = 0; x < sm.HiddenSignals.Count; x++)
      {
        sw.WriteLine(sm.HiddenSignals[x]);
      }
      sw.Close();
    }

    }
}
