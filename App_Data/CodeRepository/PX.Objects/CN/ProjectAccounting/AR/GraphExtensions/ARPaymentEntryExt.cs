using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CN.ProjectAccounting.AR.GraphExtensions
{
	public class ARPaymentEntryExt : PXGraphExtension<ARPaymentEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.construction>();
		}

		protected virtual void ARAdjust_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			ARAdjust adjustment = (ARAdjust)e.Row;

			PMProforma proforma = PXSelect<PMProforma, Where<PMProforma.aRInvoiceDocType, Equal<Required<ARAdjust.adjdDocType>>,
				And<PMProforma.aRInvoiceRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>>>>.Select(Base, adjustment.AdjdDocType, adjustment.AdjdRefNbr);

			if (proforma != null && proforma.Corrected == true)
			{
				if (Base.Document.Current.DocType == ARDocType.Payment)
				{
					sender.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adjustment, adjustment.AdjdRefNbr, new PXSetPropertyException(PX.Objects.PM.Messages.CannotPreparePayment, adjustment.AdjdRefNbr, proforma.RefNbr));
				}
				if (Base.Document.Current.DocType == ARDocType.CreditMemo)
				{
					sender.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adjustment, adjustment.AdjdRefNbr, new PXSetPropertyException(PX.Objects.PM.Messages.CannotReverseInvoice, adjustment.AdjdRefNbr, proforma.RefNbr));
				}

			}
		}
	}
}
