using PX.CloudServices.DAC;
using PX.Common;
using System;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal readonly struct RecognizedRecordFileInfo
	{
		public string FileName { get; }
		public byte[] FileData { get; }
		public Guid FileId { get; }
		public RecognizedRecord RecognizedRecord { get; }

		public RecognizedRecordFileInfo(string fileName, byte[] fileData, Guid fileId, RecognizedRecord record)
		{
			fileName.ThrowOnNullOrWhiteSpace(nameof(fileName));
			fileData.ThrowOnNull(nameof(fileData));

			FileName = fileName;
			FileData = fileData;
			FileId = fileId;
			RecognizedRecord = record;
		}

		public RecognizedRecordFileInfo(string fileName, byte[] fileData, Guid fileId)
			: this(fileName, fileData, fileId, null)
		{
		}
	}
}
