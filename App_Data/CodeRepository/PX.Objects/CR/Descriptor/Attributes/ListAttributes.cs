using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.CR
{
	#region ContactStatus

	public class ContactStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(Active, Messages.Active),
				(Inactive, Messages.Inactive)
			) { }
		}

		public const string Active = "A";
		public const string Inactive = "I";

		public class active : PX.Data.BQL.BqlString.Constant<active>
		{
			public active() : base(Active) { }
		}
		public class inactive : PX.Data.BQL.BqlString.Constant<inactive>
		{
			public inactive() : base(Inactive) { }
		}
	}

	#endregion

	#region LocationStatus

	public class LocationStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(Active, Messages.Active),
				(Inactive, Messages.Inactive)
			)
			{ }
		}

		public const string Active = "A";
		public const string Inactive = "I";

		public class active : PX.Data.BQL.BqlString.Constant<active>
		{
			public active() : base(Active) { }
		}
		public class inactive : PX.Data.BQL.BqlString.Constant<inactive>
		{
			public inactive() : base(Inactive) { }
		}
	}

	#endregion

	#region ValidationTypesAttribute

	public class ValidationTypesAttribute : PXStringListAttribute
	{
		public ValidationTypesAttribute()
			: base(
				(LeadToLead, Messages.LeadToLeadValidation),
				(LeadToContact, Messages.LeadToContactValidation),
				(ContactToContact, Messages.ContactToContactValidation),
				(ContactToLead, Messages.ContactLeadValidation),
				(LeadToAccount, Messages.LeadToAccountValidation),
				(ContactToAccount, Messages.ContactToAccountValidation),
				(AccountToAccount, Messages.AccountToAccountValidation)
			)
		{
		}
		public const string LeadToLead = "LL";
		public const string LeadToContact = "LC";
		public const string ContactToContact = "CC";
		public const string ContactToLead = "CL";
		public const string LeadToAccount = "LA";
		public const string ContactToAccount = "CA";
		public const string AccountToAccount = "AA";

		public sealed class leadToLead : PX.Data.BQL.BqlString.Constant<leadToLead>
		{
			public leadToLead() : base(LeadToLead) { }
		}
		public sealed class leadToContact : PX.Data.BQL.BqlString.Constant<leadToContact>
		{
			public leadToContact() : base(LeadToContact) { }
		}
		public sealed class contactToContact : PX.Data.BQL.BqlString.Constant<contactToContact>
		{
			public contactToContact() : base(ContactToContact) { }
		}
		public sealed class contactToLead : PX.Data.BQL.BqlString.Constant<contactToLead>
		{
			public contactToLead() : base(ContactToLead) { }
		}
		public sealed class leadToAccount : PX.Data.BQL.BqlString.Constant<leadToAccount>
		{
			public leadToAccount() : base(LeadToAccount) { }
		}
		public sealed class contactToAccount : PX.Data.BQL.BqlString.Constant<contactToAccount>
		{
			public contactToAccount() : base(ContactToAccount) { }
		}
		public sealed class accountToAccount : PX.Data.BQL.BqlString.Constant<accountToAccount>
		{
			public accountToAccount() : base(AccountToAccount) { }
		}
	}

	#endregion
	
	#region OpportunityStatus
	/// <summary>
	/// Statuses for <see cref="CROpportunity.status"/> used by default in system workflow.
	/// Values could be changed and extended by workflow.
	/// </summary>
	public class OpportunityStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(New, "New"),
				(Open, "Open"),
				(Won, "Won"),
				(Lost, "Lost"))
			{ }
		}

		public const string New = "N";
		public const string Open = "O";
		public const string Won = "W";
		public const string Lost = "L";

		public class @new : BqlString.Constant<@new>
		{
			public @new() : base(New) { }
		}

		public class open : BqlString.Constant<open>
		{
			public open() : base(Open) { }
		}

		public class won : BqlString.Constant<won>
		{
			public won() : base(Won) { }
		}

		public class lost : BqlString.Constant<lost>
		{
			public lost() : base(Lost) { }
		}
	}
	#endregion

	#region OpportunityReason
	public class OpportunityReason
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(Created, "Created"),
				(Technology, "Technology"),
				(Relationship, "Relationship"),
				(Price, "Price"),
				(Other, "Other"),
				(InProcess, "In Process"),
				(CompanyMaturity, "Company Maturity"),
				(ConvertedFromLead, "Converted from Lead"),
				(Qualified, "Qualified"),
				(OrderPlaced, "Order Placed"))
			{ }
		}

		/*
		 * following statuses could still be used (don't use those abbreviations if possible):
		 * 
		 * Canceled = "CL"
		 * Assign = "NA"
		 * Functionality = "FC"
		 */

		public const string Created = "CR";
		public const string Technology = "TH";
		public const string Relationship = "RL";
		public const string Price = "PR";
		public const string Other = "OT";
		public const string InProcess = "IP";
		public const string CompanyMaturity = "CM";
		public const string ConvertedFromLead = "FL";
		public const string Qualified = "QL";
		public const string OrderPlaced = "OP";
	}
	#endregion
}
