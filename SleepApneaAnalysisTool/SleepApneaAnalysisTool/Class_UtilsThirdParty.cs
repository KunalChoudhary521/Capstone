using System;
using System.Collections.Generic;
using EDF;
using Excel = Microsoft.Office.Interop.Excel;

namespace SleepApneaAnalysisTool
{
  partial class Utils
  {
    // EDF

    /// <summary>
    /// Modified From EDF library function https://edf.codeplex.com/SourceControl/latest#trunk/EDFFile.cs
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

    // Excel Interop 

    /// <summary>
    /// Obtained from https://www.codeproject.com/Articles/9992/Faster-MS-Excel-Reading-using-Office-Interop-Assem
    /// </summary>
    /// <param name="app"></param>
    public static void MakeExcelInteropPerformant(Excel.Application app, bool makeperformant)
    {
      app.Visible = !makeperformant;
      app.ScreenUpdating = !makeperformant;
      app.DisplayAlerts = !makeperformant;
    }

  }
}
