using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using System;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[PXInternalUseOnly]
	[Serializable]
	[PXCacheName(Messages.RecognizedVendorMapping)]
	public class RecognizedVendorMapping : IBqlTable
	{
		private const int VENDOR_PREFIX_MAX_LENGTH = 191;

		public static class FK
		{
			public class BusinessAccount : BAccount.PK.ForeignKeyOf<RecognizedVendorMapping>.By<vendorID> { }
		}

		[PXDBGuid(withDefaulting: true, IsKey = true)]
		[PXDefault]
		public virtual Guid? Id { get; set; }
		public abstract class id : BqlGuid.Field<id> { }

		[PXDBString(VENDOR_PREFIX_MAX_LENGTH, IsUnicode = true)]
		[PXDefault]
		public virtual string VendorNamePrefix { get; set; }
		public abstract class vendorNamePrefix : BqlString.Field<vendorNamePrefix> { }

		[PXDBString(IsUnicode = true)]
		[PXDefault]
		public virtual string VendorName { get; set; }
		public abstract class vendorName : BqlString.Field<vendorName> { }

		[PXParent(typeof(FK.BusinessAccount))]
		[PXDBInt]
		[PXDefault]
		public virtual int? VendorID { get; set; }
		public abstract class vendorID : BqlInt.Field<vendorID> { }

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

		internal static string GetVendorPrefixFromName(string vendorName)
		{
			vendorName.ThrowOnNull(nameof(vendorName));

			return vendorName.Length <= VENDOR_PREFIX_MAX_LENGTH ? vendorName : vendorName.Substring(0, VENDOR_PREFIX_MAX_LENGTH);
		}
	}
}
