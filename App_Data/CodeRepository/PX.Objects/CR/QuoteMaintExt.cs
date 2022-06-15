using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Objects.Common.Discount;
using PX.Objects.EP;
using static PX.Objects.CR.QuoteMaint;

namespace PX.Objects.CR
{
	public class QuoteMaintExt : PXGraphExtension<Discount, QuoteMaint>
	{
		public override void Initialize()
		{
			PXCache sender = Base.Quote.Cache;
			var row = sender.Current as CRQuote;

			if (row != null)
			{
				VisibilityHandler(sender, row);
			}
		}

		protected virtual void CRQuote_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
		{
			sel?.Invoke(sender, e);

			CRQuote row = e.Row as CRQuote;
			if (row == null) return;

			VisibilityHandler(sender, row);
		}

		private void VisibilityHandler(PXCache sender, CRQuote row)
		{
			Standalone.CROpportunity opportunity = PXSelect<Standalone.CROpportunity,
				Where<Standalone.CROpportunity.opportunityID, Equal<Required<Standalone.CROpportunity.opportunityID>>>>.Select(Base, row.OpportunityID).FirstOrDefault();

			if (opportunity != null)
			{
				bool allowUpdate = row.IsDisabled != true && opportunity.IsActive == true;

				foreach (var type in new[]
				{
					typeof(CROpportunityDiscountDetail),
					typeof(CROpportunityProducts),
					typeof(CRTaxTran),
				})
				{
					Base.Caches[type].AllowInsert = Base.Caches[type].AllowUpdate = Base.Caches[type].AllowDelete = allowUpdate;
				}

				Base.Caches[typeof(CopyQuoteFilter)].AllowUpdate = true;
				Base.Caches[typeof(RecalcDiscountsParamFilter)].AllowUpdate = true;
			}
		}
	}
}
