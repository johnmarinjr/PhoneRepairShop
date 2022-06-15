using System;
using PX.Data;
using PX.Objects.Localizations.GB.HMRC.Attributes;
using PX.Objects.GL.Attributes;
using PX.Objects.TX.Descriptor;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	[Serializable()]
	[PXHidden]
	public partial class VATPeriodFilter : TX.TaxPeriodFilter
	{
		#region TaxPeriodFilter

		#region OrganizationID
		public new abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }
		[Organization(true,null, IsDBField = false)]
		public override Int32? OrganizationID { get; set; }
		#endregion
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXUIField(DisplayName = "Tax Agency", Required = true)]
		[TaxAgencyActive]
		public override Int32? VendorID { get; set; }
		#endregion
		public new abstract class taxPeriodID : PX.Data.BQL.BqlInt.Field<taxPeriodID> { }
		public new abstract class orgBAccountID : PX.Data.BQL.BqlInt.Operand<orgBAccountID> { }
		public new abstract class revisionId : PX.Data.BQL.BqlInt.Field<revisionId> { }
		public new abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }
		public new abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }
		public new abstract class showDifference : PX.Data.BQL.BqlBool.Field<showDifference> { }
		public new abstract class preparedWarningMsg : PX.Data.BQL.BqlString.Field<preparedWarningMsg> { }
		#endregion

		#region Start
		/// <summary>
		/// The start date of this obligation period
		/// Date in the format YYYY-MM-DD
		/// For example: 2017-01-25
		/// </summary>
		public abstract class start : PX.Data.BQL.BqlDateTime.Field<start> { }
		[PXDate()]
		[PXUIField(DisplayName = Messages.FromDate, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? Start { get; set; }
		#endregion
		#region End
		/// <summary>
		/// The end date of this obligation period
		/// Date in the format YYYY-MM-DD
		/// For example: 2017-01-25
		/// </summary>
		public abstract class end : PX.Data.BQL.BqlDateTime.Field<end> { }
		[PXDate()]
		[PXUIField(DisplayName = Messages.ToDate, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? End { get; set; }
		#endregion
		#region Due
		/// <summary>
		/// The due date for this obligation period, in the format YYYY-MM-DD.
		/// For example: 2017-01-25. The due date for monthly/quarterly obligations is one month and seven days from the end date.
		/// The due date for Payment On Account customers is the last working day of the month after the end date.
		/// For example if the end date is 2018-02-28, the due date is 2018-03-29
		/// (because the 31 March is a Saturday and the 30 March is Good Friday).
		/// </summary>
		public abstract class due : PX.Data.BQL.BqlDateTime.Field<due> { }
		[PXDate()]
		[PXUIField(DisplayName = Messages.DueDate, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? Due { get; set; }
		#endregion
		#region Status
		/// <summary>
		/// Which obligation statuses to return (O = Open, F = Fulfilled)
		/// For example: F
		/// </summary>
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

		[PXString(1)]
		[ObligationStatus.List()]
		[PXUIField(DisplayName = Messages.Status, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual string Status { get; set; }
		#endregion
		#region PeriodKey
		/// <summary>
		/// The ID code for the period that this obligation belongs to. The format is a string of four alphanumeric characters. Occasionally the format includes the # symbol.
		/// For example: 18AD, 18A1, #001
		/// </summary>
		public abstract class periodKey : PX.Data.BQL.BqlString.Field<periodKey> { }
		[PXString(8, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = Messages.PeriodCode, Visibility = PXUIVisibility.SelectorVisible, Enabled = true, Required = false)]
		[ObligationSelector(ValidateValue = false, SelectorMode = PXSelectorMode.NoAutocomplete)]
		public virtual string PeriodKey { get; set; }
		#endregion
		#region Received
		/// <summary>
		/// The obligation received date, is returned when status is (F = Fulfilled)
		/// Date in the format YYYY-MM-DD
		/// For example: 2017-01-25
		/// </summary>
		public abstract class received : PX.Data.BQL.BqlDateTime.Field<received> { }
		[PXDate()]
		[PXUIField(DisplayName = Messages.ReceivedDate, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual DateTime? Received { get; set; }
		#endregion
	}
}
