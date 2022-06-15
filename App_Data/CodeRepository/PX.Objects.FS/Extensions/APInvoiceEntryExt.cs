using PX.Data;
using PX.Objects.AP;
using PX.Objects.CN.ProjectAccounting.AP.GraphExtensions;
using PX.Objects.CS;
using System;

namespace PX.Objects.FS
{
	public class APInvoiceEntryExt : PXGraphExtension<APInvoiceEntryReclassifyingExt, APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
		}

		[PXOverride]
		public virtual void Reclassify(Action baseHandler)
		{
			foreach (APTran tran in Base.Transactions.Select())
			{
				FSxAPTran extTran = Base.Transactions.Cache.GetExtension<FSxAPTran>(tran);
				if (extTran.RelatedDocNoteID != null)
				{
					throw new PXException(CN.ProjectAccounting.Descriptor.ProjectAccountingMessages.CannotReclassifiedWithServiceDoc);
				}
			}
			baseHandler();
		}
	}
}