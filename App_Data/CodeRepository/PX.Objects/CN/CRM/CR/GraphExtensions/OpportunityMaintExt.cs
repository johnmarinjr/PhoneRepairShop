using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.CN.Common.Extensions;
using PX.Objects.CN.CRM.CR.CacheExtensions;
using PX.Objects.CN.CRM.CR.DAC;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.CN.CRM.CR.GraphExtensions
{
    public class OpportunityMaintExt : PXGraphExtension<OpportunityMaint>
    {       
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.projectQuotes>();
        }

        protected virtual void _(Events.RowSelected<CROpportunity> args)
        {
            var opportunity = args.Row;
            if (opportunity == null)
            {
                return;
            }

            SetOpportunityAmountSource(opportunity);
        }

        protected virtual void _(Events.RowInserting<CROpportunity> args)
        {
            var opportunity = args.Row;
            if (opportunity != null)
            {
                var opportunityExtension = opportunity.GetExtension<CrOpportunityExt>();
                if (opportunityExtension != null)
                {
                    opportunityExtension.Cost = 0;
                }
            }
        }

		[System.Obsolete]
		protected virtual void _(Events.FieldUpdated<CROpportunity, CrOpportunityExt.quotedAmount> args)
		{
		}

		protected virtual void _(Events.RowPersisted<CROpportunity> args)
		{
			Base.Quotes.Cache.Clear();
			Base.Quotes.View.Clear();
			Base.Quotes.View.RequestRefresh();

			var opportunity = args.Row;
			if (opportunity == null)
				return;

			SetOpportunityAmountSource(opportunity);
		}

		private void SetOpportunityAmountSource(CROpportunity opportunity)
        {
            var opportunityExtension = opportunity.GetExtension<CrOpportunityExt>();
            if (opportunityExtension != null)
            {
				if (opportunity.ManualTotalEntry != true)
				{
					var selectExistingPrimary = new PXSelect<CRQuote, Where<CRQuote.opportunityID, Equal<Required<CROpportunity.opportunityID>>, And<CRQuote.quoteID, Equal<CRQuote.defQuoteID>>>>(Base);					
					CRQuote primaryQuote = selectExistingPrimary.SelectSingle(opportunity.OpportunityID);

					if (primaryQuote != null)
					{
						var CRQuoteExtention = primaryQuote.GetExtension<PM.CacheExtensions.CRQuoteExt>();
						opportunityExtension.GrossMarginAbsolute = CRQuoteExtention.CuryGrossMarginAmount;
						opportunityExtension.GrossMarginPercentage = CRQuoteExtention.GrossMarginPct;
						opportunityExtension.Cost = CRQuoteExtention.CostTotal;
					}
				}
            }
        }
    }
}
