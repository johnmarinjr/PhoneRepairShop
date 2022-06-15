using PX.Common;
using PX.Data;
using PX.Data.BQL;
using System;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[Serializable]
	[PXInternalUseOnly]
	[PXCacheName(Messages.ExludedVendorDomains)]
	public class ExcludedVendorDomain : IBqlTable
	{
		[PXUIField(DisplayName = "Domain Name")]
		[PXDBString(255, IsKey = true, IsUnicode = true, InputMask = "")]
		public string Name { get; set; }
		public abstract class name : BqlString.Field<name> { }

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }

		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }

		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBTimestamp]
		public virtual byte[] TStamp { get; set; }
		public abstract class tStamp : BqlByteArray.Field<tStamp> { }
	}
}
