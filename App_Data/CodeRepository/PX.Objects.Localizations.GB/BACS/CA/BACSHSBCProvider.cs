using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Api;
using PX.Common;
using PX.Data;
using PX.DataSync;
using PX.DataSync.ACH;
using StringReader = System.IO.StringReader;

namespace PX.Objects.Localizations.GB
{

	// Data provider class for BACS export. Contains code copied from ACHSYProvider and FixWidthSYProvider.
	// Copy done due to need to use the same functionality as is those classes, except for mandatory padding ot text up to line size.
	// Also, there should not be an empty line at the ned of the file.
	//TODO: on merge the classes should be refactored to avoid copying of the code
	public class BACSHSBCProvider : BaseSchemaPaymentSYProvider, IPXSYProvider
	{
		private const string INCOMING_BATCH_NUMBER = "BatchNbr";

		protected const string NoteID = "NoteID";
		protected const string FileID = "FileName";
		protected List<String> notes = new List<String>();
		protected Int32 startRow = -1;
		protected Int32 endRow = -1;

		public BACSHSBCProvider()
		{
			//from ACHSYProvider
			_needAttachFile = false;
		}

		#region IPXSYProvider

		public string DefaultFileExtension => Extensiton;

		public override string ProviderName =>
			//TODO Localizable string
			"BACS HSBC UK Provider";

		#endregion

		#region Overrides

		public override void SetParameters(PXSYParameter[] parameters)
		{
			base.SetParameters(parameters);

			fill = new FillContext(' ', ' ', ' ', 0, 0);
		}

		//from ACHSYProvider
		public override PXFieldState[] GetSchemaFields(string objectName)
		{
			List<PXFieldState> ret = new List<PXFieldState>( base.GetSchemaFields(objectName));
			ret.Add(CreateFieldState(new SchemaFieldInfo(-1, NoteID, typeof(Int64))));
			ret.Add(CreateFieldState(new SchemaFieldInfo(-1, FileID)));
			return ret.ToArray();
		}
		public override void Export(string objectName, PXSYTable table, bool breakOnError, Action<SyProviderRowResult> callback)
		{
			startRow = 0;
			endRow = 0;
			String file = null;

			if (table.Count > 0 || table.Columns.Contains(FileID))
			{
				Int32 fileIndex = table.IndexOfColumn(FileID);

				if (fileIndex < 0)
				{
					throw new PXException(Messages.ACHColumnNotFound, FileID);
				}

				file = table[0][fileIndex];

				if (String.IsNullOrEmpty(file)) file = GetParameter(FILE_PARAM);
				for (int i = 0; i < table.Count; i++)
				{
					PXSYRow row = table[i];
					if (fileIndex >=0 && row[fileIndex] != null && row[fileIndex] != file)
					{
						endRow = i;

						SetParameters(_Parameters.Where(r => r.Name != FILE_PARAM).Append(new PXSYParameter(FILE_PARAM, file)).ToArray());
						base.Export(objectName, table, breakOnError, callback);

						startRow = endRow + 1;
						file = row[fileIndex];
					}
				}
			}

			endRow = table.Count - 1;
			if (file != null) SetParameters(_Parameters.Where(r => r.Name != FILE_PARAM).Append(new PXSYParameter(FILE_PARAM, file)).ToArray());
			if (startRow <= endRow) base.Export(objectName, table, breakOnError, callback);
		}
		protected override void InvokeCallback(PXSYTable table, Action<SyProviderRowResult> callback)
		{
			for (int i = startRow; i < endRow + 1; i++)
			{
				callback.Invoke(new SyProviderRowResult(i));
			}
		}

		protected override PX.SM.FileInfo SetFile(byte[] bytes)
		{
			PX.SM.FileInfo fi = base.SetFile(bytes);

			if (notes != null && notes.Count > 0) SaveNotes((Guid)fi.UID, notes);

			return fi;
		}

		protected virtual void SaveNotes(Guid fileId, IEnumerable<String> notesparam)
		{
			if (notesparam.Count() <= 0) return;

			foreach (String note in notesparam)
			{
				if (!String.IsNullOrEmpty(note))
				{
					using (var record = PXDatabase.SelectSingle<NoteDoc>(
						new PXDataField("NoteID"),
						new PXDataFieldValue("NoteID", PXDbType.UniqueIdentifier, Guid.Parse(note)),
						new PXDataFieldValue("FileID", PXDbType.UniqueIdentifier, fileId)))
					{

						if (record == null)
						{
							PXDatabase.Insert<NoteDoc>(
								new PXDataFieldAssign("NoteID", PXDbType.UniqueIdentifier, Guid.Parse(note)),
								new PXDataFieldAssign("FileID", PXDbType.UniqueIdentifier, fileId));
						}
					}
				}
			}
		}

		//from FixWidthSYProvider, writer updated
		protected override Byte[] InternalExport(String objectName, PXSYTable table)
		{
			//from ACHSYProvider
			notes.Clear();

			Int32 noteIndex = table.Columns.IndexOf(NoteID);
			if(noteIndex >= 0) notes.AddRange(table.Select(r => r[noteIndex]).Distinct());

			table = TrimTable(table, new String[] { NoteID, FileID }, startRow, endRow);

			//from FixWidthSYProvider
			List<SchemaFieldInfo> fields = InternalGetSchemaFields(objectName);

			String[][] array = SortTable(table, fields.Select(f => f.Name));
			FillSchema(array);

			Byte[] data = null;
			Encoding encoding = GetEncoding();
			using (BACSWriter writer = new BACSWriter(encoding))
			{
				String result = schema.GetText();
				using (StringReader reader = new StringReader(result))
				{
					String s = reader.ReadLine();
					while (s != null)
					{
						string nextS = reader.ReadLine();
						if (nextS == null)
						{
							writer.Write(s);
						}
						else
						{
							writer.WriteLine(s);
						}

						s = nextS;
					}
				}
				data = writer.BinData;
			}

			//throw new Exception();
			return data;
		}

		#endregion

		#region Public functions

		public virtual string GetBatchNbr()
		{
			return this.GetParameter(INCOMING_BATCH_NUMBER);
		}

		public virtual string GetCreationDate()
		{
			return GetFormattedDate(DateTime.Today);
		}

		public virtual string GetExpirationDate()
		{
			DateTime expirationDate = AddWorkingDays(DateTime.Today, 3);

			return GetFormattedDate(expirationDate);
		}

		public string GetProcessingDay()
		{
			DateTime processingDay = AddWorkingDays(DateTime.Today, 2);
			return GetFormattedDate(processingDay);
		}

		public DateTime AddWorkingDays(DateTime startDate, int numberOfDays)
		{
			int daysCnt = 0;
			DateTime resultDate = startDate;

			while (daysCnt < numberOfDays)
			{
				resultDate = resultDate.AddDays(1);
				if (!(resultDate.DayOfWeek == DayOfWeek.Saturday || resultDate.DayOfWeek == DayOfWeek.Sunday))
					daysCnt++;
			}

			return resultDate;
		}

		public string GetFormattedDate(DateTime date)
		{
			return $"{date:yy}{date.DayOfYear.ToString().PadLeft(3,'0')}";
		}

		public DateTime GetBusinessDate()
		{
			return PXContext.GetBusinessDate() ?? PXTimeZoneInfo.Today;
		}

		#endregion
	}

	public class BACSWriter: BaseWriter
	{
		public BACSWriter(Encoding encoding) : base(encoding)
		{
		}

		public new void WriteLine(string s)
		{
			_writer.WriteLine(s);
		}

		public void Write(string s)
		{
			_writer.Write(s);
		}
	}
}
