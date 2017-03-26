using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SleepApneaAnalysisTool
{

  public class BinaryFile
  {
    public string sample_frequency_s;
    public string date_time_to;
    public string date_time_from;
    public string subject_id;
    public string signal_name;
    public float sample_period;
    public List<float> signal_values;

    public BinaryFile(string fileName)
    {
      // select the binary file
      FileStream bin_file = new FileStream(fileName, FileMode.Open);
      BinaryReader reader = new BinaryReader(bin_file);

      byte[] value = new byte[4];
      this.signal_values = new List<float>();
      // read the whole binary file and build the signal values
      while (reader.BaseStream.Position != reader.BaseStream.Length)
      {
        try
        {
          value = reader.ReadBytes(4);
          float myFloat = System.BitConverter.ToSingle(value, 0);
          signal_values.Add(myFloat);
        }
        catch
        {
          break;
        }
      }

      // close the binary file
      bin_file.Close();

      // get the file metadata from the header file
      bin_file = new FileStream(fileName.Remove(fileName.Length - 4, 4) + ".hdr", FileMode.Open);

      StreamReader file_reader = new StreamReader(bin_file);
      // get the signal name
      this.signal_name = file_reader.ReadLine();
      this.subject_id = file_reader.ReadLine();
      this.date_time_from = file_reader.ReadLine();
      this.date_time_to = file_reader.ReadLine();
      this.sample_frequency_s = file_reader.ReadLine();

      bin_file.Close();

      this.sample_period = 1 / float.Parse(this.sample_frequency_s);
    }
  }
}
