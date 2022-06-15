using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.CS;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.GL;
using PX.Objects.CM;
using static PX.Objects.AP.APDocumentEnq;

namespace ReconciliationTools
{
	#region Internal Types

	[Serializable]
	public partial class APGLDiscrepancyByDocumentEnqResult : APDocumentResult, IDiscrepancyEnqResult
	{
		#region XXTurnover
		public abstract class xXTurnover : PX.Data.BQL.BqlDecimal.Field<xXTurnover> { }

		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "AP Turnover")]
		public virtual decimal? XXTurnover
		{
			get;
			set;
		}
		#endregion
		#region Discrepancy
		public abstract class discrepancy : PX.Data.BQL.BqlDecimal.Field<discrepancy> { }

		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Discrepancy")]
		public virtual decimal? Discrepancy
		{
			get
			{
				return GLTurnover - XXTurnover;
			}
		}
		#endregion
	}

	#endregion

	[TableAndChartDashboardType]
	public class APGLDiscrepancyByDocumentEnq : APGLDiscrepancyEnqGraphBase<APGLDiscrepancyByAccountEnq, APGLDiscrepancyByVendorEnqFilter, APGLDiscrepancyByDocumentEnqResult>
	{

		public APGLDiscrepancyByDocumentEnq()
		{
			PXUIFieldAttribute.SetRequired<APGLDiscrepancyByDocumentEnqResult.refNbr>(Caches[typeof(APGLDiscrepancyByDocumentEnqResult)], false);
			PXUIFieldAttribute.SetRequired<APGLDiscrepancyByDocumentEnqResult.finPeriodID>(Caches[typeof(APGLDiscrepancyByDocumentEnqResult)], false);
		}

		#region CacheAttached

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Financial Period")]
		protected virtual void APGLDiscrepancyByVendorEnqFilter_PeriodFrom_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault]
		protected virtual void APGLDiscrepancyByVendorEnqFilter_VendorID_CacheAttached(PXCache sender) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Amount")]
		protected virtual void APGLDiscrepancyByDocumentEnqResult_OrigDocAmt_CacheAttached(PXCache sender) { }

		#endregion

		public PXAction<APGLDiscrepancyByVendorEnqFilter> viewDocument;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			if (this.Rows.Current != null)
			{
				PXRedirectHelper.TryRedirect(Rows.Cache, Rows.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		protected override List<APGLDiscrepancyByDocumentEnqResult> SelectDetails()
		{
			var list = new List<APGLDiscrepancyByDocumentEnqResult>();
			APGLDiscrepancyByVendorEnqFilter header = Filter.Current;

			if (header == null ||
				header.BranchID == null ||
				header.PeriodFrom == null ||
				header.VendorID == null)
			{
				return list;
			}

			#region AP balances

			APDocumentEnq graphAP = PXGraph.CreateInstance<APDocumentEnq>();
			APDocumentEnq.APDocumentFilter filterAP = PXCache<APDocumentEnq.APDocumentFilter>.CreateCopy(graphAP.Filter.Current);
			var branch = PXAccess.GetBranch(header.BranchID);
			filterAP.OrgBAccountID = branch?.BAccountID;
			filterAP.OrganizationID = branch?.Organization?.OrganizationID;
			filterAP.BranchID = header.BranchID;
			filterAP.VendorID = header.VendorID;
			filterAP.FinPeriodID = header.PeriodFrom;
			filterAP.AccountID = header.AccountID;
			filterAP.SubCD = header.SubCD;
			filterAP.IncludeGLTurnover = true;
			filterAP = graphAP.Filter.Update(filterAP);
			
			Dictionary<ARDocKey, ARGLDiscrepancyByDocumentEnqResult> dict = new Dictionary<ARDocKey, ARGLDiscrepancyByDocumentEnqResult>();
			foreach (APDocumentResult document in graphAP.Documents.Select())
			{
				var result = new APGLDiscrepancyByDocumentEnqResult {XXTurnover = (document.APTurnover ?? 0m)};
				PXCache<APDocumentResult>.RestoreCopy(result, document);
				if (header.ShowOnlyWithDiscrepancy != true || result.Discrepancy != 0m)
					list.Add(result);
			}
			#endregion
			return list;
		}
	}
}