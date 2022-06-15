using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.FS
{
	public abstract class SMMultiCurrencyGraph<TGraph, TPrimary> : MultiCurrencyGraph<TGraph, TPrimary>
        where TGraph : PXGraph
        where TPrimary : class, IBqlTable, new()
    {
		protected override string Module => GL.BatchModule.AR;

		protected override CurySourceMapping GetCurySourceMapping()
        {
            return new CurySourceMapping(typeof(Customer));
        }

        protected override void _(Events.RowSelected<Document> e)
        {
            base._(e);

            if (e.Row == null) return;

            PXUIFieldAttribute.SetVisible<Document.curyID>(e.Cache, e.Row, IsMultyCurrencyEnabled);
            switch (Documents.Cache.GetMain(e.Row))
            {
                case FSServiceOrder _:
					{
						ServiceOrderEntry graphServiceOrder = (ServiceOrderEntry)Documents.Cache.Graph;
						FSServiceOrder fsServiceOrderRow = graphServiceOrder.ServiceOrderRecords?.Current;

						bool isAllowedForInvoiceOrInvoiced = fsServiceOrderRow?.AllowInvoice == true || fsServiceOrderRow?.Billed == true;
						PXUIFieldAttribute.SetEnabled<Document.curyID>(e.Cache, e.Row, graphServiceOrder.ServiceOrderAppointments.Select().Count == 0
																						&& !isAllowedForInvoiceOrInvoiced);
                        return;
                    }
                case FSAppointment fsAppointmentRow:
                    {
                        PXUIFieldAttribute.SetEnabled<Document.curyID>(e.Cache, e.Row, fsAppointmentRow.SOID < 0);
                        return;
                    }
            }
        }

        public virtual bool IsMultyCurrencyEnabled
        {
            get { return PXAccess.FeatureInstalled<FeaturesSet.multicurrency>(); }
        }
    }
}
