using PX.CloudServices.DAC;
using PX.Common;
using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[PXInternalUseOnly]
	[PXHidden]
	public class RecognizedRecordForProcessing : RecognizedRecord
	{
		[PXUIField(DisplayName = "Selected")]
		[PXBool]
		public virtual bool? Selected { get; set; }
		public abstract class selected : BqlBool.Field<selected> { }

		public new abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		public new abstract class owner : BqlInt.Field<owner> { }
		public new abstract class documentLink : BqlGuid.Field<documentLink> { }
	}
}
