using PX.Data;
using PX.Objects.AR;
using PX.Objects.CN.Common.Descriptor;
using PX.Objects.CN.Common.Services;
using PX.Objects.CN.ProjectAccounting.Descriptor;
using PX.Objects.CN.ProjectAccounting.PM.CacheExtensions;
using PX.Objects.CN.ProjectAccounting.PM.Descriptor;
using PX.Objects.CS;
using PX.Objects.PM;
using System;
using System.Collections;

namespace PX.Objects.CN.ProjectAccounting.AR.GraphExtensions
{
    public class ArInvoiceEntryExt : PXGraphExtension<ARInvoiceEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>() && !SiteMapExtension.IsInvoicesScreenId();
        }

        [PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<PMTask.type, NotEqual<ProjectTaskType.cost>>),
            ProjectAccountingMessages.TaskTypeIsNotAvailable, typeof(PMTask.type))]
        protected virtual void _(Events.CacheAttached<ARTran.taskID> e)
        {
        }

        protected virtual void _(Events.RowUpdated<ARTran> args)
        {
            if (args.Row is ARTran line)
            {
                object taskId = line.TaskID;
                try
                {
                    args.Cache.RaiseFieldVerifying<ARTran.taskID>(line, ref taskId);
                }
                catch (PXSetPropertyException)
                {
                    line.TaskID = null;
                }
            }
        }

		[PXOverride]
		public virtual IEnumerable PayInvoice(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseHandler)
		{
			if (Base.Document.Current != null && Base.Document.Current.Released == true)
			{
				if (Base.Document.Current.ProformaExists == true)
				{
					PMProforma proforma = PXSelect<PMProforma, Where<PMProforma.aRInvoiceDocType, Equal<Current<ARInvoice.docType>>,
						And<PMProforma.aRInvoiceRefNbr, Equal<Current<ARInvoice.refNbr>>>>>.Select(Base);

					if (proforma != null && proforma.Corrected == true)
					{
						throw new PXSetPropertyException(PX.Objects.PM.Messages.CannotPreparePayment, Base.Document.Current.RefNbr, proforma.RefNbr);
					}
				}
			}

			return baseHandler(adapter);
		}

		[PXOverride]
		public virtual IEnumerable ReverseInvoice(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseHandler)
		{
			if (Base.Document.Current != null)
			{
				if (Base.Document.Current.ProformaExists == true)
				{
					PMProforma proforma = PXSelect<PMProforma, Where<PMProforma.aRInvoiceDocType, Equal<Current<ARInvoice.docType>>,
						And<PMProforma.aRInvoiceRefNbr, Equal<Current<ARInvoice.refNbr>>>>>.Select(Base);

					if (proforma != null && proforma.Corrected == true)
					{
						throw new PXSetPropertyException(PX.Objects.PM.Messages.CannotReverseInvoice, Base.Document.Current.RefNbr, proforma.RefNbr);
					}
				}
			}

			return baseHandler(adapter);
		}

		[PXOverride]
		public virtual IEnumerable ReverseInvoiceAndApplyToMemo(PXAdapter adapter, Func<PXAdapter, IEnumerable> baseHandler)
		{
			if (Base.Document.Current != null)
			{
				if (Base.Document.Current.ProformaExists == true)
				{
					PMProforma proforma = PXSelect<PMProforma, Where<PMProforma.aRInvoiceDocType, Equal<Current<ARInvoice.docType>>,
						And<PMProforma.aRInvoiceRefNbr, Equal<Current<ARInvoice.refNbr>>>>>.Select(Base);

					if (proforma != null && proforma.Corrected == true)
					{
						throw new PXSetPropertyException(PX.Objects.PM.Messages.CannotReverseInvoice, Base.Document.Current.RefNbr, proforma.RefNbr);
					}
				}
			}

			return baseHandler(adapter);
		}
	}
}
