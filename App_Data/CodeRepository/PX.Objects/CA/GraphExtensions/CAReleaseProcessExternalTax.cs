using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.TaxProvider;

namespace PX.Objects.CA
{
	public class CAReleaseProcessExternalTax : PXGraphExtension<CAReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.avalaraTax>();
		}

		protected Func<PXGraph, string, ITaxProvider> TaxProviderFactory;

		public override void Initialize()
		{
			TaxProviderFactory = ExternalTax.TaxProviderFactory;
		}

		public virtual bool IsExternalTax(string taxZoneID)
		{
			return ExternalTax.IsExternalTax(Base, taxZoneID);
		}

		protected Lazy<CATranEntry> LazyCaTranEntry =
			new Lazy<CATranEntry>(() => PXGraph.CreateInstance<CATranEntry>());

		[PXOverride]
		public virtual void OnBeforeRelease(CAAdj doc)
		{
			if (doc == null || doc.IsTaxValid == true || !IsExternalTax(doc.TaxZoneID))
			{
				return;
			}

			CATranEntry graph = LazyCaTranEntry.Value;
			graph.Clear();

			graph.CalculateExternalTax(doc);
		}

		[PXOverride]
		public virtual CAAdj CommitExternalTax(CAAdj doc)
		{
			if (doc?.IsTaxValid == true && doc.NonTaxable != true && IsExternalTax(doc.TaxZoneID) && doc.IsTaxPosted != true)
			{
				if (TaxPluginMaint.IsActive(Base, doc.TaxZoneID))
				{
					var service = ExternalTax.TaxProviderFactory(Base, doc.TaxZoneID);

					CATranEntry ie = PXGraph.CreateInstance<CATranEntry>();
					ie.CAAdjRecords.Current = doc;
					CATranEntryExternalTax ieExt = ie.GetExtension<CATranEntryExternalTax>();
					CommitTaxRequest request = ieExt.BuildCommitTaxRequest(doc);

					CommitTaxResult result = service.CommitTax(request);
					if (result.IsSuccess)
					{
						doc.IsTaxPosted = true;
					}
				}
			}

			return doc;
		}
	}
}
