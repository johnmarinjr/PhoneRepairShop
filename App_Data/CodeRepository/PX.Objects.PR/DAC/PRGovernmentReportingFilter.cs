using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.GL.Attributes;
using PX.Objects.TX.DAC.ReportParameters;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRGovernmentReportingFilter)]
	[Serializable]
	public class PRGovernmentReportingFilter : IBqlTable
	{
		#region OrgBAccountID
		public abstract class orgBAccountID : PX.Data.BQL.BqlInt.Field<orgBAccountID> { }
		[OrganizationTree(
			treeDataMember: typeof(TaxTreeSelect),
			onlyActive: true,
			SelectionMode = OrganizationTreeAttribute.SelectionModes.Branches,
			IsKey = true)]
		public virtual int? OrgBAccountID { get; set; }
		#endregion
		#region FederalOnly
		public abstract class federalOnly : PX.Data.BQL.BqlBool.Field<federalOnly> { }
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Show only Federal forms")]
		public bool? FederalOnly { get; set; }
		#endregion
		#region State
		public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
		[PXString(3, IsUnicode = true)]
		[PXUIField(DisplayName = "State")]
		[State(typeof(PRGovernmentReportingFilter.countryID))]
		[PXMassMergableField]
		[PXUIEnabled(typeof(Where<PRGovernmentReportingFilter.federalOnly.IsEqual<False>>))]
		[PXFormula(typeof(Switch<Case<Where<PRGovernmentReportingFilter.federalOnly.IsEqual<True>>, Null>, PRGovernmentReportingFilter.state>))]
		public string State { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXString(2)]
		[PRCountry]
		[PXUIField(DisplayName = "Country", Visible = false)]
		[PXMassMergableField]
		public string CountryID { get; set; }
		#endregion
		#region ReportingPeriod
		public abstract class reportingPeriod : PX.Data.BQL.BqlString.Field<reportingPeriod> { }
		[PXString(3)]
		[PXUIField(DisplayName = "Reporting Period")]
		[GovernmentReportingPeriod.List]
		public string ReportingPeriod { get; set; }
		#endregion

		#region DownloadAuf
		public abstract class downloadAuf : PX.Data.BQL.BqlBool.Field<downloadAuf> { }
		[PXBool]
		[PXUIField(Visible = false)]
		public bool? DownloadAuf { get; set; }
		#endregion

		#region Ein
		public abstract class ein : PX.Data.BQL.BqlString.Field<ein> { }
		[PXString(50, IsUnicode = true)]
		[PXUIField(Visible = false)]
		[PXUnboundDefault(typeof(SearchFor<BAccount.taxRegistrationID>.Where<BAccount.bAccountID.IsEqual<orgBAccountID.FromCurrent>>))]
		public string Ein { get; set; }
		#endregion
		#region AatrixVendorID
		public abstract class aatrixVendorID : PX.Data.BQL.BqlString.Field<aatrixVendorID> { }
		[PXString(50, IsUnicode = true)]
		[PXUIField(Visible = false)]
		[AatrixVendorID]
		public string AatrixVendorID { get; set; }
		#endregion
		#region AatrixSessionID
		public abstract class aatrixSessionID : PX.Data.BQL.BqlString.Field<aatrixSessionID> { }
		[PXString]
		public string AatrixSessionID { get; set; }
		#endregion
		#region OperationAborted
		public abstract class operationAborted : PX.Data.BQL.BqlString.Field<operationAborted> { }
		[PXBool]
		[PXUnboundDefault(false)]
		public bool? OperationAborted { get; set; }
		#endregion
	}

	public class AatrixVendorIDAttribute : PXEventSubscriberAttribute, IPXFieldSelectingSubscriber
	{
		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			e.ReturnValue = AatrixConfiguration.VendorID;
		}
	}
}
