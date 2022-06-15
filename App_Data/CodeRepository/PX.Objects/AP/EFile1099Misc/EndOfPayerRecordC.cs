using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PX.Objects.AP
{
    /// <summary>
    /// End of Payer Record (C)
    /// File format is based on IRS publication 1220 (http://www.irs.gov/pub/irs-pdf/p1220.pdf)
    /// </summary>
    public class EndOfPayerRecordC : I1099Record
	{
        public string RecordType { get; set; }
        public string NumberOfPayees { get; set; }
        public decimal ControlTotal1 { get; set; }
        public decimal ControlTotal2 { get; set; }
        public decimal ControlTotal3 { get; set; }
        public decimal ControlTotal4 { get; set; }
        public decimal ControlTotal5 { get; set; }
        public decimal ControlTotal6 { get; set; }
        public decimal ControlTotal7 { get; set; }
        public decimal ControlTotal8 { get; set; }
        public decimal ControlTotal9 { get; set; }
        public decimal ControlTotalA { get; set; }
        public decimal ControlTotalB { get; set; }
        public decimal ControlTotalC { get; set; }
        public decimal ControlTotalD { get; set; }
        public decimal ControlTotalE { get; set; }
        public decimal ControlTotalF { get; set; }
        public decimal ControlTotalG { get; set; }
		public decimal ControlTotalH { get; set; }
		public decimal ControlTotalJ { get; set; }
        public string RecordSequenceNumber { get; set; }

		void I1099Record.WriteToFile(StreamWriter writer, YearFormat yearFormat)
		{
			StringBuilder dataRow = new StringBuilder(800);

			if (yearFormat == YearFormat.F2020)
			{
				dataRow
					.Append(RecordType, startPosition: 1, fieldLength: 1)
					.Append(NumberOfPayees, startPosition: 2, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 10, fieldLength: 6)
					.Append(ControlTotal1, startPosition: 16, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal2, startPosition: 34, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal3, startPosition: 52, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal4, startPosition: 70, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal5, startPosition: 88, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal6, startPosition: 106, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal7, startPosition: 124, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal8, startPosition: 142, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal9, startPosition: 160, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalA, startPosition: 178, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalB, startPosition: 196, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalC, startPosition: 214, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalD, startPosition: 232, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalE, startPosition: 250, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalF, startPosition: 268, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalG, startPosition: 286, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 304, fieldLength: 196, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 508, fieldLength: 241)
					.Append(string.Empty, startPosition: 749, fieldLength: 2);
			}
			else
			{
				dataRow
					.Append(RecordType, startPosition: 1, fieldLength: 1)
					.Append(NumberOfPayees, startPosition: 2, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 10, fieldLength: 6)
					.Append(ControlTotal1, startPosition: 16, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal2, startPosition: 34, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal3, startPosition: 52, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal4, startPosition: 70, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal5, startPosition: 88, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal6, startPosition: 106, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal7, startPosition: 124, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal8, startPosition: 142, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotal9, startPosition: 160, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalA, startPosition: 178, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalB, startPosition: 196, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalC, startPosition: 214, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalD, startPosition: 232, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalE, startPosition: 250, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalF, startPosition: 268, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalG, startPosition: 286, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalH, startPosition: 304, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(ControlTotalJ, startPosition: 322, fieldLength: 18, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 340, fieldLength: 160, paddingStyle: PaddingEnum.Left)
					.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
					.Append(string.Empty, startPosition: 508, fieldLength: 241)
					.Append(string.Empty, startPosition: 749, fieldLength: 2);
			}

			writer.WriteLine(dataRow);
		}
	}    
}
