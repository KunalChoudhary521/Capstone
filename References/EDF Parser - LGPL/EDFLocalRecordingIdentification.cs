using System;
using System.Collections.Generic;
using System.Text;

namespace EDF
{
    public class EDFLocalRecordingIdentification
    {
        public EDFLocalRecordingIdentification()
        {
            //parameterless constructor, required for XML serialization
        }
        public EDFLocalRecordingIdentification(char[] recordingIdentification)
        {
            parseRecordingIdentificationSubFields(recordingIdentification);
        }
        public DateTime RecordingStartDate { get; set; }
        public string RecordingCode { get; set; }
        public string RecordingTechnician { get; set; }
        public string RecordingEquipment { get; set; }
        public List<string> AdditionalRecordingIdentification { get; set; }
        private StringBuilder _strRecordingIdentification = new StringBuilder(string.Empty);

        private void parseRecordingIdentificationSubFields(char[] recordingIdentification)
        {
            _strRecordingIdentification = new StringBuilder(new string(recordingIdentification));
            this.AdditionalRecordingIdentification = new List<string>();

            string[] arrayRecordingInformation = _strRecordingIdentification.ToString().Trim().Split(' ');
            if (arrayRecordingInformation.Length >= 5)
            {
                if (!arrayRecordingInformation[0].ToLower().Equals("startdate"))
                {
                    this.intializeEDF();
                    throw new ArgumentException("Recording Identification must start with the text 'Startdate'");
                }
                try
                {
                    this.RecordingStartDate = DateTime.Parse(arrayRecordingInformation[1]);
                }
                catch (FormatException ex)
                {
                    System.Diagnostics.Debug.WriteLine("A FormatException occurred on the Recording Start Date, this is not in EDF+ format\n\n" + ex.StackTrace);
                    this.intializeEDF();
                    return;
                }
                this.RecordingCode = arrayRecordingInformation[2];
                this.RecordingTechnician = arrayRecordingInformation[3];
                this.RecordingEquipment = arrayRecordingInformation[4];
                for (int i = 5; i < arrayRecordingInformation.Length; i++)
                {
                    AdditionalRecordingIdentification.Add(arrayRecordingInformation[i]);
                }
            }
            else
            {
                this.intializeEDF();
            }
        }
        private void intializeEDF()
        {
            this.RecordingStartDate = DateTime.MinValue;
            this.RecordingCode = string.Empty;
            this.RecordingTechnician = string.Empty;
            this.RecordingEquipment = string.Empty;
        }
        public override String ToString()
        {

            _strRecordingIdentification = new StringBuilder(string.Empty);
            _strRecordingIdentification.Append("Startdate");
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingStartDate.ToString("dd-MMM-yyyy"));
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingCode);
            _strRecordingIdentification.Append(this.RecordingTechnician);
            _strRecordingIdentification.Append(this.RecordingEquipment);
            foreach (string info in AdditionalRecordingIdentification)
            {
                _strRecordingIdentification.Append(" ");
                _strRecordingIdentification.Append(info);
            }
            _strRecordingIdentification = new StringBuilder(_strRecordingIdentification.Length > EDFHeader.FixedLength_LocalRecordingIdentifiaction ? _strRecordingIdentification.ToString().Substring(0, EDFHeader.FixedLength_LocalRecordingIdentifiaction) : _strRecordingIdentification.ToString().PadRight(EDFHeader.FixedLength_LocalRecordingIdentifiaction));
            return _strRecordingIdentification.ToString();
        }
    }
}
