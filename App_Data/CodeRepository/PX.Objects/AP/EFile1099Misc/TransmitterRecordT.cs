using System.IO;
using System.Text;

namespace PX.Objects.AP
{
	/// <summary>
	/// Transmitter Record (T) 
	/// File format is based on IRS publication 1220 (http://www.irs.gov/pub/irs-pdf/p1220.pdf)
	/// </summary>
	public class TransmitterTRecord : I1099Record
	{
		public string RecordType { get; set; }
		public string PaymentYear { get; set; }
		public string PriorYearDataIndicator { get; set; }
		public string TransmitterTIN { get; set; }
		public string TransmitterControlCode { get; set; }
		public string TestFileIndicator { get; set; }
		public string ForeignEntityIndicator { get; set; }
		public string TransmitterName { get; set; }
		public string CompanyName { get; set; }
		public string CompanyMailingAddress { get; set; }
		public string CompanyCity { get; set; }
		public string CompanyState { get; set; }
		public string CompanyZipCode { get; set; }
		public string TotalNumberofPayees { get; set; }
		public string ContactName { get; set; }
		public string ContactTelephoneAndExt { get; set; }
		public string ContactEmailAddress { get; set; }
		public string RecordSequenceNumber { get; set; }
		public string VendorIndicator { get; set; }
		public string VendorName { get; set; }
		public string VendorMailingAddress { get; set; }
		public string VendorCity { get; set; }
		public string VendorState { get; set; }
		public string VendorZipCode { get; set; }
		public string VendorContactName { get; set; }
		public string VendorContactTelephoneAndExt { get; set; }
		public string VendorForeignEntityIndicator { get; set; }

		void I1099Record.WriteToFile(StreamWriter writer, YearFormat yearFormat)
		{
			StringBuilder dataRow = new StringBuilder(800);

			dataRow
				.Append(RecordType, startPosition: 1, fieldLength: 1)
				.Append(PaymentYear, startPosition: 2, fieldLength: 4)
				.Append(PriorYearDataIndicator, startPosition: 6, fieldLength: 1)
				.Append(TransmitterTIN, startPosition: 7, fieldLength: 9, regexReplacePattern: @"[^0-9]")
				.Append(TransmitterControlCode, startPosition: 16, fieldLength: 5)
				.Append(string.Empty, startPosition: 21, fieldLength: 7)
				.Append(TestFileIndicator, startPosition: 28, fieldLength: 1)
				.Append(ForeignEntityIndicator, startPosition: 29, fieldLength: 1)
				.Append(TransmitterName, startPosition: 30, fieldLength: 80, paddingStyle: PaddingEnum.Right)
				.Append(CompanyName, startPosition: 110, fieldLength: 80, paddingStyle: PaddingEnum.Right)
				.Append(CompanyMailingAddress, startPosition: 190, fieldLength: 40, paddingStyle: PaddingEnum.Right)
				.Append(CompanyCity, startPosition: 230, fieldLength: 40, paddingStyle: PaddingEnum.Right)
				.Append(CompanyState, startPosition: 270, fieldLength: 2, paddingStyle: PaddingEnum.Right)
				.Append(CompanyZipCode, startPosition: 272, fieldLength: 9, paddingStyle: PaddingEnum.Right, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(string.Empty, startPosition: 281, fieldLength: 15, paddingStyle: PaddingEnum.Right)
				.Append(TotalNumberofPayees, startPosition: 296, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(ContactName, startPosition: 304, fieldLength: 40)
				.Append(ContactTelephoneAndExt, startPosition: 344, fieldLength: 15, paddingStyle: PaddingEnum.Right, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(ContactEmailAddress, startPosition: 359, fieldLength: 50, paddingStyle: PaddingEnum.Right, alphaCharacterCaseStyle: AlphaCharacterCaseEnum.None)
				.Append(string.Empty, startPosition: 409, fieldLength: 91, paddingStyle: PaddingEnum.Right)
				.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(string.Empty, startPosition: 508, fieldLength: 10, paddingStyle: PaddingEnum.Right)
				.Append(VendorIndicator, startPosition: 518, fieldLength: 1)
				.Append(VendorName, startPosition: 519, fieldLength: 40)
				.Append(VendorMailingAddress, startPosition: 559, fieldLength: 40)
				.Append(VendorCity, startPosition: 599, fieldLength: 40)
				.Append(VendorState, startPosition: 639, fieldLength: 2)
				.Append(VendorZipCode, startPosition: 641, fieldLength: 9, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(VendorContactName, startPosition: 650, fieldLength: 40)
				.Append(VendorContactTelephoneAndExt, startPosition: 690, fieldLength: 15, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(string.Empty, startPosition: 705, fieldLength: 35)
				.Append(VendorForeignEntityIndicator, startPosition: 740, fieldLength: 1)
				.Append(string.Empty, startPosition: 741, fieldLength: 8)
				.Append(string.Empty, startPosition: 749, fieldLength: 2);

			writer.WriteLine(dataRow);
		}

	}
}
