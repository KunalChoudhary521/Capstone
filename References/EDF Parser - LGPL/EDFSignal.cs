using System;
using System.Collections.Generic;
using System.Text;

namespace EDF
{
    public class EDFSignal
    {
        public EDFSignal()
        {
            //empty constructor
        }

        private StringBuilder _strSignal = new StringBuilder(String.Empty);
        public int IndexNumber { get; set; }
        public string IndexNumberWithLabel 
        {
            get
            {
                return this.IndexNumber + "." + this.Label;
            }
        }
        public string Label { get; set; }
        public string LabelType { get; set; }
        public string LabelSpecification { get; set; }
        public string TransducerType { get; set; } // equal to the lower and upper bounds
        public string PhysicalDimension { get; set; }
        public string PhysicalDimensionPrefix { get; set; }
        public string PhysicalDimensionBasic { get; set; }
        public float PhysicalMinimum { get; set; }
        public float PhysicalMaximum { get; set; }
        public float DigitalMinimum { get; set; }
        public float DigitalMaximum { get; set; }
        public string Prefiltering { get; set; }
        private int _NumberOfSamplesPerDataRecord;
        public int NumberOfSamplesPerDataRecord 
        {
            get
            {
                if(_NumberOfSamplesPerDataRecord > 0)
                {
                    return _NumberOfSamplesPerDataRecord;
                } else {
                    throw new InvalidOperationException("Must provide the NumberOfSamplesPerDataRecord before accessing this Property");
                }
            }
            set
            {
                if (value > 0){
                    _NumberOfSamplesPerDataRecord = value;
                } else {
                    throw new ArgumentException("NumberOfSamplesPerDataRecord must be set to greater than 0");
                }
            }
        }

        /**
         * I don't understand the name of this parameter, yet.  It is used in getting the value out of the 2-byte integer, and was called
         * "sense" in the C sample code I learned the format from.
         */
        public float AmplifierGain //http://en.wikipedia.org/wiki/Gain 
        {
            get
            {
                return (this.PhysicalMaximum - this.PhysicalMinimum) / (this.DigitalMaximum - this.DigitalMinimum);
            }
        }
        /**
         * This is used in getting the value of the sample out of the DataRecord.  
         */
        public float Offset
        {
            get
            {
                return ((this.PhysicalMaximum / this.AmplifierGain) - this.DigitalMaximum);
            }
        }

        public float SamplePeriodWithinDataRecord { get; set; }
        public override string ToString()
        {
            return this.IndexNumberWithLabel;
        }

    }

}
