using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PX.Api;
using PX.Common;
using PX.Data;
using PX.DataSync;
using MSZip = System.IO.Compression;

namespace PX.Objects.Localizations.CA.DataSync
{
	public class CPA005ExportProvider : ACHProvider
    {
        public class Params
        {
            public const string FileName = "FileName";
            public const string BatchNbr = "BatchNbr";
            public const string ZipFormat = "ZipFormat";
			public const string IncomingBankCode = "BankCode";
			public const string IsTest = "IsTest";
		}

		private const string RBCcode = "003";
		private const string FCN_TestRBC = "TEST";
		private const string FCN_TestNotRBC = "0000";
		private const string HeaderLineProd = @"$$AA01CPA1464[PROD[NL$$";
		private const string HeaderLineTest = @"$$AA01CPA1464[TEST[NL$$";

		// Default capacity of the dictionnaries are based on the possibility of C,D,I,J,E,F records
		// S records are considered isolated in their own file

		protected readonly Dictionary<string, int> LogicalRecordCounter = new Dictionary<string, int>(6);
        protected readonly Dictionary<string, decimal> RunningTotal = new Dictionary<string, decimal>(6);

        protected int PreviousIndex = 1;
        protected string PreviousRecordType = string.Empty;

        protected short DestInstitutionIdentNbr;

        protected override List<PXStringState> FillParameters()
        {
            const string ENCODING = "Encoding";
            const string LINESIZE = "LineSize";
            const string BLOCK_SIZE = "BlockSize";
            const string FILE_SEQ_NUMBER = "FileSeqNumber";
            const string FILE_REFERENCE_CODE = "FileReferenceCode";

            List<PXStringState> list = base.FillParameters();

            PXStringState fileName = list.FirstOrDefault(x => x.Name == Params.FileName);
            if (fileName != null)
            {
                fileName.Value = "CPA_ACP_005.txt";
            }

            PXStringState encoding = list.FirstOrDefault(x => x.Name == ENCODING);
            if (encoding != null)
            {
                encoding.Value = "Windows-1252";
            }

            PXStringState lineSize = list.FirstOrDefault(x => x.Name == LINESIZE);
            if (lineSize != null)
            {
                lineSize.AllowedLabels = new string[] { "CDEFIJ", "S" };
                lineSize.AllowedValues = new string[] { "1464", "208" };
                lineSize.Value = "1464";
            }

            PXStringState blockSize = list.FirstOrDefault(x => x.Name == BLOCK_SIZE);
            if (blockSize != null)
            {
                blockSize.AllowedValues = new string[] { "1" };
                blockSize.Value = 1;
            }

            PXStringState zipFormat = CreateParameter(Params.ZipFormat, PXMessages.Localize(Messages.CPA005.CompressToZipFormat));
            zipFormat.AllowedValues = new string[] { "0", "1" };
            zipFormat.Value = "0";
            list.Add(zipFormat);

            List<string> paramsToRemove = new List<string>() { FILE_SEQ_NUMBER, FILE_REFERENCE_CODE };

            list = list.Where(x => !paramsToRemove.Contains(x.Name)).ToList();

			list.Add(CreateParameter(Params.IncomingBankCode, "Bank Code", String.Empty));
			list.Add(CreateParameter(Params.IsTest, "Is Test", String.Empty));
			return list;
        }

        public override string ProviderName
        {
            get
            {
                return PXMessages.Localize(Messages.CPA005.ProviderName);
            }
        }

        public virtual string GetLogicalRecordIndex(string recType)
        {
            const int maxLength = 9;
            int index = 0;

            recType = recType.ToUpper();

            if (recType == "A" || recType == "U")
            {
                // These are the always the first records of the file
                index = 1;
            }
            else if (recType == "Z" || recType == "V")
            {
                // These are the always the last records of the file
                index = PreviousIndex + 1;
            }
            else if (PXContext.GetSlot<bool>(SYExportProcess.ExportRefreshingValues))
            {
                if (LogicalRecordCounter.ContainsKey(recType))
                {
                    LogicalRecordCounter[recType] = LogicalRecordCounter[recType] + 1;
                }
                else
                {
                    LogicalRecordCounter[recType] = 1;
                }

                PreviousIndex++;

                index = PreviousIndex;
            }

            return Format(index, maxLength);
        }

        public virtual string GetRecordCount(string recType)
        {
            const int maxLength = 8;
            int count;

            recType = recType.ToUpper();

            switch (recType)
            {
                case "C":
                case "I":
                    count = GetInternalCount("C");
                    count += GetInternalCount("I");
                    break;
                case "D":
                case "J":
                    count = GetInternalCount("D");
                    count += GetInternalCount("J");
                    break;
                default:
                    count = GetInternalCount(recType);
                    break;
            }

            return Format(count, maxLength);
        }

        public virtual string GetRecordTotal(string recType)
        {
            decimal total;

            recType = recType.ToUpper();

            switch (recType)
            {
                case "C":
                case "I":
                    total = GetInternalRecordTotal("C");
                    total += GetInternalRecordTotal("I");
                    break;
                case "D":
                case "J":
                    total = GetInternalRecordTotal("D");
                    total += GetInternalRecordTotal("J");
                    break;
                default:
                    total = GetInternalRecordTotal(recType);
                    break;
            }

            return FormatAmount(total, 14, 2);
        }

        public virtual string FormatAmount14(decimal amount)
        {
            return FormatAmount(amount, 14, 2);
        }

        public new virtual string FormatDate(DateTime date)
        {
            return string.Format("0{0:yy}{1:000}", date, date.DayOfYear);
        }

        public virtual string FormatCurrencyCode(string CuryID)
        {
            CuryID = CuryID.ToUpper().Trim();

            if (CuryID != "CAD" && CuryID != "USD")
            {
                throw new PXException(Messages.CPA005.CurrencyCodeInvalid, CuryID);
            }

            return CuryID;
        }

        public virtual string FormatFileCreationNumber(string seqNbr)
        {
			bool isTest = Convert.ToBoolean(GetParameter(Params.IsTest));

			if (isTest)
			{
				string bank = GetParameter(Params.IncomingBankCode);

				if (bank == RBCcode)
				{
					return FCN_TestRBC;
				}
				else
				{
					return FCN_TestNotRBC;
				}
			}
			else
			{
				short nbr;

				if (!short.TryParse(seqNbr, out nbr))
				{
					throw new PXException(Messages.CPA005.FileCreationNumberInvalid, seqNbr);
				}

				if (nbr < 1 || nbr > 9999)
				{
					throw new PXException(Messages.CPA005.FileCreationNumberInvalid, seqNbr);
				}

				return nbr.ToString("0000");
			}
		}

        public virtual string FormatOriginatorID(string originatorID)
        {
            if (string.IsNullOrWhiteSpace(originatorID))
            {
                throw new PXException(Messages.CPA005.OriginatorIDEmpty);
            }

            originatorID = originatorID.Trim();

            if (originatorID.Length > 10)
            {
                throw new PXException(Messages.CPA005.OriginatorIDTooLong);
            }

            return originatorID.PadRight(10, ' ');
        }

        public virtual string FormatDestInstitutionIdentNbr(string identNbr)
        {
            if (string.IsNullOrWhiteSpace(identNbr))
            {
                throw new PXException(Messages.CPA005.InstitutionIdentNbrEmpty);
            }

            identNbr = identNbr.Trim();

            short ident;

            if (!short.TryParse(identNbr, out ident))
            {
                throw new PXException(Messages.CPA005.InstitutionIdentNbrInvalid, identNbr);
            }

            if (ident > 999)
            {
                throw new PXException(Messages.CPA005.InstitutionIdentNbrTooLong, identNbr);
            }

            DestInstitutionIdentNbr = ident;

            return ident.ToString("000");
        }

        public virtual string RegisterAmount(string recType, decimal amount)
        {
            recType = recType.ToUpper();

            if (PXContext.GetSlot<bool>(SYExportProcess.ExportRefreshingValues))
            {
                decimal total = GetInternalRecordTotal(recType);

                RunningTotal[recType] = total + amount;
            }

            return FormatAmount10(amount);
        }


		public virtual string GetIsTest()
		{
			return this.GetParameter(Params.IsTest);
		}

		protected override Byte[] InternalExport(String objectName, PXSYTable table)
        {
			byte[] binData = base.InternalExport(objectName, table);

			string bankCode = GetParameter(Params.IncomingBankCode);
			string isTest = GetParameter(Params.IsTest);
			bool generateTestFile = false;
			if (!string.IsNullOrEmpty(isTest))
			{
				if (isTest.ToLower() == "true")
					generateTestFile = true;
			}

			if (!string.IsNullOrEmpty(bankCode) && bankCode == RBCcode)
			{
				Encoding encoding = GetEncoding();
				String text = encoding.GetString(binData);

				if (generateTestFile)
					text = text.Insert(0, HeaderLineTest + Environment.NewLine);
				else
					text = text.Insert(0, HeaderLineProd + Environment.NewLine);

				binData = encoding.GetBytes(text);
			}

			bool zipFormat = GetParameter(Params.ZipFormat) == "1";

            if (zipFormat)
            {
                // When the zip format is requested, the target filename will end with the .zip
                // extension but the file contained in the zip archive will use the .txt extension
                //
                string fileName = GetParameter(Params.FileName);
                int dotIndex = fileName.LastIndexOf('.');

                if (dotIndex > 0) // we need at least one character before the dot
                {
                    fileName = fileName.Substring(0, dotIndex) + ".txt";
                }

                MemoryStream zipInMemory = new MemoryStream();

                using (MSZip.ZipArchive archive = new MSZip.ZipArchive(zipInMemory, MSZip.ZipArchiveMode.Create))
                {
                    MSZip.ZipArchiveEntry entry = archive.CreateEntry(fileName);

                    using (var writer = new BinaryWriter(entry.Open()))
                    {
                        writer.Write(binData);
                    }
                }

                binData = zipInMemory.GetBuffer();
            }

            return binData;
        }

		protected string Format(int value, int length)
        {
            string mask = new string('0', length);

            return value.ToString(mask);
        }

        protected int GetInternalCount(string recType)
        {
            int count = 0;

            if (LogicalRecordCounter.ContainsKey(recType))
            {
                count = LogicalRecordCounter[recType];
            }

            return count;
        }

        protected virtual decimal GetInternalRecordTotal(string recType)
        {
            decimal total = 0.0M;

            if (RunningTotal.ContainsKey(recType))
            {
                total = RunningTotal[recType];
            }

            return total;
        }
    }
}
