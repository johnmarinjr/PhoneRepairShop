using System.IO;
using System.Text;

namespace PX.Objects.AP
{
	/// <summary>
	/// Payer Record (A)
	/// File format is based on IRS publication 1220 (http://www.irs.gov/pub/irs-pdf/p1220.pdf)
	/// </summary>
	public class PayerRecordA : I1099Record
	{

		public string RecordType { get; set; }
		public string PaymentYear { get; set; }
		public string CombinedFederalORStateFiler { get; set; }
		public string PayerTaxpayerIdentificationNumberTIN { get; set; }
		public string PayerNameControl { get; set; }
		public string LastFilingIndicator { get; set; }
		public string TypeofReturn { get; set; }
		public string AmountCodes { get; set; }
		public string ForeignEntityIndicator { get; set; }
		public string FirstPayerNameLine { get; set; }
		public string SecondPayerNameLine { get; set; }
		public string TransferAgentIndicator { get; set; }
		public string PayerShippingAddress { get; set; }
		public string PayerCity { get; set; }
		public string PayerState { get; set; }
		public string PayerZipCode { get; set; }
		public string PayerTelephoneAndExt { get; set; }
		public string RecordSequenceNumber { get; set; }

		void I1099Record.WriteToFile(StreamWriter writer, YearFormat yearFormat)
		{
			StringBuilder dataRow = new StringBuilder(800);

			dataRow
				.Append(RecordType, startPosition: 1, fieldLength: 1)
				.Append(PaymentYear, startPosition: 2, fieldLength: 4)
				.Append(CombinedFederalORStateFiler, startPosition: 6, fieldLength: 1)
				.Append(string.Empty, startPosition: 7, fieldLength: 5)
				.Append(PayerTaxpayerIdentificationNumberTIN, startPosition: 12, fieldLength: 9, regexReplacePattern: @"[^0-9]")
				.Append(PayerNameControl, startPosition: 21, fieldLength: 4, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(LastFilingIndicator, startPosition: 25, fieldLength: 1)
				.Append(TypeofReturn, startPosition: 26, fieldLength: 2)
				.Append(AmountCodes, startPosition: 28, fieldLength: 16)
				.Append(string.Empty, startPosition: 44, fieldLength: 8)
				.Append(ForeignEntityIndicator, startPosition: 52, fieldLength: 1)
				.Append(FirstPayerNameLine, startPosition: 53, fieldLength: 40)
				.Append(SecondPayerNameLine, startPosition: 93, fieldLength: 40)
				.Append(TransferAgentIndicator, startPosition: 133, fieldLength: 1)
				.Append(PayerShippingAddress, startPosition: 134, fieldLength: 40)
				.Append(PayerCity, startPosition: 174, fieldLength: 40)
				.Append(PayerState, startPosition: 214, fieldLength: 2)
				.Append(PayerZipCode, startPosition: 216, fieldLength: 9, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(PayerTelephoneAndExt, startPosition: 225, fieldLength: 15, regexReplacePattern: @"[^0-9a-zA-Z]")
				.Append(string.Empty, startPosition: 240, fieldLength: 260)
				.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
				.Append(string.Empty, startPosition: 508, fieldLength: 241)
				.Append(string.Empty, startPosition: 749, fieldLength: 2);

			writer.WriteLine(dataRow);
		}
	}
}
