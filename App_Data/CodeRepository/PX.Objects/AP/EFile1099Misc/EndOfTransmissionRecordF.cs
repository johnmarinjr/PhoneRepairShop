using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PX.Objects.AP
{
    /// <summary>
    /// End of Transmission Record (F)
    /// File format is based on IRS publication 1220 (http://www.irs.gov/pub/irs-pdf/p1220.pdf)
    /// </summary>
    public class EndOfTransmissionRecordF : I1099Record
	{
        public string RecordType { get; set; }
        public string NumberOfARecords { get; set; }
        public string TotalNumberOfPayees { get; set; }
        public string RecordSequenceNumber { get; set; }

		void I1099Record.WriteToFile(StreamWriter writer, YearFormat yearFormat)
		{
			StringBuilder dataRow = new StringBuilder(800);

			dataRow
				.Append(RecordType, startPosition: 1, fieldLength: 1)
				.Append(NumberOfARecords, startPosition: 2, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(string.Empty, startPosition: 10, fieldLength: 21, paddingChar: '0')
				.Append(string.Empty, startPosition: 31, fieldLength: 19)
				.Append(TotalNumberOfPayees, startPosition: 50, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(string.Empty, startPosition: 58, fieldLength: 442)
				.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(string.Empty, startPosition: 508, fieldLength: 241)
				.Append(string.Empty, startPosition: 749, fieldLength: 2);

			writer.WriteLine(dataRow);
		}
	}
}
