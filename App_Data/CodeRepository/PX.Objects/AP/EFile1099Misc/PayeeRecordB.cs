using PX.Data;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PX.Objects.AP
{
	/// <summary>
	/// Payee Record (B)
	/// File format is based on IRS publication 1220 (http://www.irs.gov/pub/irs-pdf/p1220.pdf)
	/// </summary>
	public class PayeeRecordB : I1099Record
	{
		public string RecordType { get; set; }
		public string PaymentYear { get; set; }
		public string CorrectedReturnIndicator { get; set; }
		public string NameControl { get; set; }
		public string TypeOfTIN { get; set; }
		public string PayerTaxpayerIdentificationNumberTIN { get; set; }
		public string PayerAccountNumberForPayee { get; set; }
		public string PayerOfficeCode { get; set; }
		public decimal PaymentAmount1 { get; set; }
		public decimal PaymentAmount2 { get; set; }
		public decimal PaymentAmount3 { get; set; }
		public decimal PaymentAmount4 { get; set; }
		public decimal PaymentAmount5 { get; set; }
		public decimal PaymentAmount6 { get; set; }
		public decimal PaymentAmount7 { get; set; }
		public decimal PaymentAmount8 { get; set; }
		public decimal PaymentAmount9 { get; set; }
		public decimal PaymentAmountA { get; set; }
		public decimal PaymentAmountB { get; set; }
		public decimal PaymentAmountC { get; set; }
		public decimal Payment { get; set; }
		public decimal PaymentAmountE { get; set; }
		public decimal PaymentAmountF { get; set; }
		public decimal PaymentAmountG { get; set; }
		public decimal PaymentAmountH { get; set; }
		public decimal PaymentAmountJ { get; set; }
		public string ForeignCountryIndicator { get; set; }
		public string PayeeNameLine { get; set; }
		public string PayeeMailingAddress { get; set; }
		public string PayeeCity { get; set; }
		public string PayeeState { get; set; }
		public string PayeeZipCode { get; set; }
		public string RecordSequenceNumber { get; set; }
		public string SecondTINNotice { get; set; }
		public string DirectSalesIndicator { get; set; }
		public string FATCA { get; set; }
		public string SpecialDataEntries { get; set; }
		public string StateIncomeTaxWithheld { get; set; }
		public string LocalIncomeTaxWithheld { get; set; }
		public string CombineFederalOrStateCode { get; set; }

		void I1099Record.WriteToFile(StreamWriter writer, YearFormat yearFormat)
		{
			StringBuilder dataRow = new StringBuilder(800);

			switch (yearFormat)
			{
				case YearFormat.F2020:

					dataRow
						.Append(RecordType, startPosition: 1, fieldLength: 1)
						.Append(PaymentYear, startPosition: 2, fieldLength: 4)
						.Append(CorrectedReturnIndicator, startPosition: 6, fieldLength: 1)
						.Append(NameControl, startPosition: 7, fieldLength: 4, regexReplacePattern: @"[^0-9a-zA-Z]")
						.Append(TypeOfTIN, startPosition: 11, fieldLength: 1)
						.Append(PayerTaxpayerIdentificationNumberTIN, startPosition: 12, fieldLength: 9, regexReplacePattern: @"[^0-9]")
						.Append(PayerAccountNumberForPayee, startPosition: 21, fieldLength: 20)
						.Append(PayerOfficeCode, startPosition: 41, fieldLength: 4)
						.Append(string.Empty, startPosition: 45, fieldLength: 10)
						.Append(PaymentAmount1, startPosition: 55, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount2, startPosition: 67, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount3, startPosition: 79, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount4, startPosition: 91, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount5, startPosition: 103, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount6, startPosition: 115, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount7, startPosition: 127, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount8, startPosition: 139, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount9, startPosition: 151, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountA, startPosition: 163, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountB, startPosition: 175, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountC, startPosition: 187, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(Payment, startPosition: 199, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountE, startPosition: 211, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountF, startPosition: 223, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountG, startPosition: 235, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(ForeignCountryIndicator, startPosition: 247, fieldLength: 1)
						.Append(PayeeNameLine, startPosition: 248, fieldLength: 80, regexReplacePattern: @"[^0-9a-zA-Z-& ]")
						.Append(string.Empty, startPosition: 328, fieldLength: 40)
						.Append(PayeeMailingAddress, startPosition: 368, fieldLength: 40)
						.Append(string.Empty, startPosition: 408, fieldLength: 40)
						.Append(PayeeCity, startPosition: 448, fieldLength: 40)
						.Append(PayeeState, startPosition: 488, fieldLength: 2)
						.Append(PayeeZipCode, startPosition: 490, fieldLength: 9, regexReplacePattern: @"[^0-9a-zA-Z]")
						.Append(string.Empty, startPosition: 499, fieldLength: 1)
						.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(string.Empty, startPosition: 508, fieldLength: 36)
						.Append(SecondTINNotice, startPosition: 544, fieldLength: 1)
						.Append(string.Empty, startPosition: 545, fieldLength: 2)
						.Append(DirectSalesIndicator, startPosition: 547, fieldLength: 1)
						.Append(FATCA, startPosition: 548, fieldLength: 1)
						.Append(string.Empty, startPosition: 549, fieldLength: 114)
						.Append(SpecialDataEntries, startPosition: 663, fieldLength: 60)
						.Append(StateIncomeTaxWithheld, startPosition: 723, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(LocalIncomeTaxWithheld, startPosition: 735, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(CombineFederalOrStateCode, startPosition: 747, fieldLength: 2)
						.Append(string.Empty, startPosition: 749, fieldLength: 2);

					break;

				case YearFormat.F2021:

					dataRow
						.Append(RecordType, startPosition: 1, fieldLength: 1)
						.Append(PaymentYear, startPosition: 2, fieldLength: 4)
						.Append(CorrectedReturnIndicator, startPosition: 6, fieldLength: 1)
						.Append(NameControl, startPosition: 7, fieldLength: 4, regexReplacePattern: @"[^0-9a-zA-Z]")
						.Append(TypeOfTIN, startPosition: 11, fieldLength: 1)
						.Append(PayerTaxpayerIdentificationNumberTIN, startPosition: 12, fieldLength: 9, regexReplacePattern: @"[^0-9]")
						.Append(PayerAccountNumberForPayee, startPosition: 21, fieldLength: 20)
						.Append(PayerOfficeCode, startPosition: 41, fieldLength: 4)
						.Append(string.Empty, startPosition: 45, fieldLength: 10)
						.Append(PaymentAmount1, startPosition: 55, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount2, startPosition: 67, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount3, startPosition: 79, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount4, startPosition: 91, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount5, startPosition: 103, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount6, startPosition: 115, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount7, startPosition: 127, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount8, startPosition: 139, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmount9, startPosition: 151, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountA, startPosition: 163, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountB, startPosition: 175, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountC, startPosition: 187, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(Payment, startPosition: 199, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountE, startPosition: 211, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountF, startPosition: 223, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountG, startPosition: 235, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountH, startPosition: 247, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(PaymentAmountJ, startPosition: 259, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(string.Empty, startPosition: 271, fieldLength: 16)
						.Append(ForeignCountryIndicator, startPosition: 287, fieldLength: 1)
						.Append(PayeeNameLine, startPosition: 288, fieldLength: 80, regexReplacePattern: @"[^0-9a-zA-Z-& ]")
						.Append(PayeeMailingAddress, startPosition: 368, fieldLength: 40)
						.Append(string.Empty, startPosition: 408, fieldLength: 40)
						.Append(PayeeCity, startPosition: 448, fieldLength: 40)
						.Append(PayeeState, startPosition: 488, fieldLength: 2)
						.Append(PayeeZipCode, startPosition: 490, fieldLength: 9, regexReplacePattern: @"[^0-9a-zA-Z]")
						.Append(string.Empty, startPosition: 499, fieldLength: 1)
						.Append(RecordSequenceNumber, startPosition: 500, fieldLength: 8, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(string.Empty, startPosition: 508, fieldLength: 36)
						.Append(SecondTINNotice, startPosition: 544, fieldLength: 1)
						.Append(string.Empty, startPosition: 545, fieldLength: 2)
						.Append(DirectSalesIndicator, startPosition: 547, fieldLength: 1)
						.Append(FATCA, startPosition: 548, fieldLength: 1)
						.Append(string.Empty, startPosition: 549, fieldLength: 114)
						.Append(SpecialDataEntries, startPosition: 663, fieldLength: 60)
						.Append(StateIncomeTaxWithheld, startPosition: 723, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(LocalIncomeTaxWithheld, startPosition: 735, fieldLength: 12, paddingStyle: PaddingEnum.Left, paddingChar: '0')
						.Append(CombineFederalOrStateCode, startPosition: 747, fieldLength: 2)
						.Append(string.Empty, startPosition: 749, fieldLength: 2);

					break;
			}	
	
			writer.WriteLine(dataRow);
		}		
	}
}
