using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace EDF
{
    public class EDFFile
    {
        public EDFFile()
        {
            //initialize EDFHeader as part of constructor
            _header = new EDFHeader();
            //initialize EDFDataRecord List as part of constructor
            _dataRecords = new List<EDFDataRecord>();
        }

        private EDFHeader _header;
        public EDFHeader Header 
        {
            get
            {
                return _header;
            }
        }

        private List<EDFDataRecord> _dataRecords;
        public List<EDFDataRecord> DataRecords
        {
            get
            {
                return _dataRecords;
            }
        }
        
        public void readFile(string file_path)
        {
            //open the file to read the header
            FileStream file = new FileStream(file_path, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(file);
            readStream(sr);
            file.Close();
            sr.Close();

        }

        public void readStream(StreamReader sr)
        {

            parseHeaderStream(sr);
            parseDataRecordStream(sr);

        }

        public byte[] getEDFFileBytes()
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] byteArray = encoding.GetBytes(this.Header.ToString().ToCharArray());
            List<byte> byteList = new List<byte>(byteArray);
            byteList.AddRange(getCompressedDataRecordsBytes());
            return byteList.ToArray();
        }

        public List<byte> getCompressedDataRecordsBytes()
        {
            List<byte> byteList = new List<byte>();
            byte[] byteArraySample = new byte[2];
            foreach (EDFDataRecord dataRecord in this.DataRecords)
            {
                foreach (EDFSignal signal in this.Header.Signals)
                {
                    foreach (float sample in dataRecord[signal.IndexNumberWithLabel])
                    {
                        byteArraySample = BitConverter.GetBytes(Convert.ToInt16((sample / signal.AmplifierGain) - signal.Offset));
                        byteList.Add(byteArraySample[0]);
                        byteList.Add(byteArraySample[1]);
                    }
                }
            }
            return byteList;
        }

        public void saveFile(string file_path)
        {
            if (File.Exists(file_path))
            {
                File.Delete(file_path);
            }
            FileStream newFile = new FileStream(file_path, FileMode.CreateNew, FileAccess.Write);
            
            StreamWriter sw = new StreamWriter(newFile);
            this.Header.NumberOfDataRecords = this.DataRecords.Count;

            char[] headerCharArray = this.Header.ToString().ToCharArray();
            sw.Write(headerCharArray, 0, headerCharArray.Length);
            sw.Flush();

            newFile.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256), SeekOrigin.Begin);
            BinaryWriter bw = new BinaryWriter(newFile);

            byte[] byteList = getCompressedDataRecordsBytes().ToArray();
            
            bw.Write(byteList, 0, byteList.Length);
            bw.Flush();
            sw.Close();
            bw.Close();
            newFile.Close();

        }

        private void parseHeaderStream(StreamReader sr)
        {
            //parse the header to get the number of Signals (size of the Singal Header)
            char[] header = new char[256];
            sr.ReadBlock(header, 0, 256);
            this._header = new EDFHeader(header);

            //parse the signals within the header
            char[] signals = new char[this.Header.NumberOfSignalsInDataRecord * 256];
            sr.ReadBlock(signals, 0, this.Header.NumberOfSignalsInDataRecord * 256);
            this.Header.parseSignals(signals);

        }

        private void parseDataRecordStream(StreamReader sr)
        {

            //set the seek position in the file stream to the beginning of the data records.
            sr.BaseStream.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256), SeekOrigin.Begin);
            
            int dataRecordSize = 0;
            foreach (EDFSignal signal in this.Header.Signals)
            {
                signal.SamplePeriodWithinDataRecord = this.Header.DurationOfDataRecordInSeconds / signal.NumberOfSamplesPerDataRecord;
                dataRecordSize += signal.NumberOfSamplesPerDataRecord;
            }

            byte[] dataRecordBytes = new byte[dataRecordSize * 2];

            while (sr.BaseStream.Read(dataRecordBytes, 0, dataRecordSize * 2) > 0)
            {

                EDFDataRecord dataRecord = new EDFDataRecord();
                int j = 0;
                int samplesWritten = 0;
                foreach (EDFSignal signal in this.Header.Signals)
                {
                    List<float> samples = new List<float>();
                    for (int l = 0; l < signal.NumberOfSamplesPerDataRecord; l++)
                    {
                        float value = (float)(((BitConverter.ToInt16(dataRecordBytes, (samplesWritten * 2)) + (int)signal.Offset)) * signal.AmplifierGain);
                        samples.Add(value);
                        samplesWritten++;
                    }
                    dataRecord.Add(signal.IndexNumberWithLabel, samples);
                    j++;

                }
                _dataRecords.Add(dataRecord);

            }

        }

        public void deleteSignal(EDFSignal signal_to_delete)
        {
            if (this.Header.Signals.Contains(signal_to_delete))
            {
                //Remove Signal DataRecords
                foreach (EDFDataRecord dr in this.DataRecords)
                {
                    foreach (EDFSignal signal in this.Header.Signals)
                    {
                        if (signal.IndexNumberWithLabel.Equals(signal_to_delete.IndexNumberWithLabel))
                        {
                            dr.Remove(signal_to_delete.IndexNumberWithLabel);
                        }
                    }
                }
                //After removing the DataRecords then Remove the Signal from the Header
                this.Header.Signals.Remove(signal_to_delete);

                //Finally Decrement the NumberOfSignals in the Header by 1
                this.Header.NumberOfSignalsInDataRecord = this.Header.NumberOfSignalsInDataRecord - 1;

                //Change the Number Of Bytes in the Header.
                this.Header.NumberOfBytes = (256) + (256 * this.Header.Signals.Count);
            }
        }
        public void addSignal(EDFSignal signal_to_add, List<float> sampleValues)
        {

            if (this.Header.Signals.Contains(signal_to_add))
            {
                this.deleteSignal(signal_to_add);
            }

            //Remove Signal DataRecords
            int index = 0;
            foreach (EDFDataRecord dr in this.DataRecords)
            {
                    dr.Add(signal_to_add.IndexNumberWithLabel, sampleValues.GetRange(index * signal_to_add.NumberOfSamplesPerDataRecord, signal_to_add.NumberOfSamplesPerDataRecord));
                  index++;
            }
            //After removing the DataRecords then Remove the Signal from the Header
            this.Header.Signals.Add(signal_to_add);

            //Finally Decrement the NumberOfSignals in the Header by 1
            this.Header.NumberOfSignalsInDataRecord = this.Header.NumberOfSignalsInDataRecord + 1;

            //Change the Number Of Bytes in the Header.
            this.Header.NumberOfBytes = (256) + (256 * this.Header.Signals.Count);
            
        }
        public List<float> retrieveSignalSampleValues(EDFSignal signal_to_retrieve)
        {
            List<float> signalSampleValues = new List<float>();

            if (this.Header.Signals.Contains(signal_to_retrieve))
            {
                //Remove Signal DataRecords
                foreach (EDFDataRecord dr in this.DataRecords)
                {
                    foreach (EDFSignal signal in this.Header.Signals)
                    {
                        if (signal.IndexNumberWithLabel.Equals(signal_to_retrieve.IndexNumberWithLabel))
                        {
                            foreach (float value in dr[signal.IndexNumberWithLabel])
                            {
                                signalSampleValues.Add(value);
                            }
                        }
                    }
                }
            }
            return signalSampleValues;

        }
        public void exportAsCompumedics(string file_path)
        {
            foreach (EDFSignal signal in this.Header.Signals)
            {
                string signal_name = this.Header.StartDateTime.ToString("MMddyyyy_HHmm") + "_" + signal.Label;
                string new_path = string.Empty;
                if (file_path.LastIndexOf('/') == file_path.Length)
                {
                    new_path = file_path + signal_name.Replace(' ', '_');
                }
                else
                {
                    new_path = file_path + '/' + signal_name.Replace(' ', '_');
                }

                if (File.Exists(new_path))
                {
                    File.Delete(new_path);
                }
                FileStream newFile = new FileStream(new_path, FileMode.CreateNew, FileAccess.Write);

                StreamWriter sw = new StreamWriter(newFile);

                if (signal.NumberOfSamplesPerDataRecord <= 0)
                {
                    //need to pad it to be sampled every second.
                    sw.WriteLine(signal.Label + " " + "RATE:1.0Hz");
                }
                else
                {
                    sw.WriteLine(signal.Label + " " + "RATE:" + Math.Round((double)(signal.NumberOfSamplesPerDataRecord/this.Header.DurationOfDataRecordInSeconds), 2) + "Hz");
                }

                foreach (EDFDataRecord dataRecord in this.DataRecords)
                {
                    foreach (float sample in dataRecord[signal.IndexNumberWithLabel])
                    {
                        sw.WriteLine(sample);
                    }

                }
                sw.Flush();

            }


        }
    }
 
}
