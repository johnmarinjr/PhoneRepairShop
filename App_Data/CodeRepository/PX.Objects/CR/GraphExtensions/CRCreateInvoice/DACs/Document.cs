using PX.Data;
using System;

namespace PX.Objects.CR.Extensions.CRCreateInvoice
{
	/// <exclude/>
	[PXHidden]
	public class Document : PXMappedCacheExtension
	{
		#region OpportunityID
		public abstract class opportunityID : IBqlField { }
		public virtual string OpportunityID { get; set; }
		#endregion

		#region QuoteID
		public abstract class quoteID : IBqlField { }
		public virtual Guid? QuoteID { get; set; }
		#endregion

		#region Subject
		public abstract class subject : IBqlField { }
		public virtual string Subject { get; set; }
		#endregion

		#region BAccountID
		public abstract class bAccountID : IBqlField { }
		public virtual int? BAccountID { get; set; }
		#endregion

		#region LocationID
		public abstract class locationID : IBqlField { }
		public virtual int? LocationID { get; set; }
		#endregion

		#region ContactID
		public abstract class contactID : IBqlField { }
		public virtual int? ContactID { get; set; }
		#endregion

		#region TaxZoneID
		public abstract class taxZoneID : IBqlField { }
		public virtual string TaxZoneID { get; set; }
		#endregion

		#region TaxCalcMode
		public abstract class taxCalcMode : IBqlField { }
		public virtual string TaxCalcMode { get; set; }
		#endregion

		#region ManualTotalEntry
		public abstract class manualTotalEntry : IBqlField { }
		public virtual bool? ManualTotalEntry { get; set; }
		#endregion

		#region CuryID
		public abstract class curyID : IBqlField { }
		public virtual string CuryID { get; set; }
		#endregion

		#region CuryInfoID
		public abstract class curyInfoID : IBqlField { }
		public virtual Int64? CuryInfoID { get; set; }
		#endregion

		#region CuryAmount
		public abstract class curyAmount : IBqlField { }
		public virtual decimal? CuryAmount { get; set; }
		#endregion

		#region CuryDiscTot
		public abstract class curyDiscTot : IBqlField { }
		public virtual decimal? CuryDiscTot { get; set; }
		#endregion

		#region ProjectID
		public abstract class projectID : IBqlField { }
		public virtual int? ProjectID { get; set; }
		#endregion

		#region BranchID
		public abstract class branchID : IBqlField { }
		public virtual int? BranchID { get; set; }
		#endregion

		#region NoteID
		public abstract class noteID : IBqlField { }
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region CloseDate
		public abstract class closeDate : IBqlField { }
		public virtual DateTime? CloseDate { get; set; }
		#endregion

		#region TermsID
		public abstract class termsID : IBqlField { }
		public virtual string TermsID { get; set; }
		#endregion

		#region ExternalTaxExemptionNumber
		public abstract class externalTaxExemptionNumber : IBqlField { }
		public virtual string ExternalTaxExemptionNumber { get; set; }
		#endregion

		#region AvalaraCustomerUsageType
		public abstract class avalaraCustomerUsageType : IBqlField { }
		public virtual string AvalaraCustomerUsageType { get; set; }
		#endregion

		#region IsPrimary
		public abstract class isPrimary : IBqlField { }
		public virtual bool? IsPrimary { get; set; }
		#endregion
	}
}