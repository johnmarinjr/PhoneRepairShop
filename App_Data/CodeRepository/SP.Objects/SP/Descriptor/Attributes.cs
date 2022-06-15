using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.SM;
using PX.TM;
using SP.Objects.CR;

namespace SP.Objects.SP
{
	#region OwnerSPAttribute

	public class OwnerSPAttribute : OwnerAttribute
	{
		protected override Type CreateSelect(Type workgroupType)
		{
			if (workgroupType == null)
				return typeof(SelectFrom<
						Owner>
					.Where<
						Owner.contactType.IsEqual<ContactTypesAttribute.employee>
						.And<Owner.employeeUserID.IsNotNull>
						.And<MatchWithBAccountNotNull<Owner.bAccountID>>>
					.SearchFor<
						Owner.contactID>);

			return BqlTemplate.OfCommand<
					Search2<
						Owner.contactID,
						LeftJoin<EPCompanyTreeMember,
							On<EPCompanyTreeMember.contactID, Equal<Owner.contactID>,
								And<EPCompanyTreeMember.workGroupID, Equal<Optional<BqlPlaceholder.A>>>>>,
						Where2<Where<
								Optional<BqlPlaceholder.A>, IsNull,
								Or<EPCompanyTreeMember.contactID, IsNotNull>>,
							And<Owner.employeeUserID, IsNotNull,
							And<MatchWithBAccountNotNull<Owner.bAccountID>>>>
					>
				>
				.Replace<BqlPlaceholder.A>(workgroupType)
				.ToType();
		}
	}

	#endregion

	#region SPCaseStatusesAttribute
	public class FinancialDocumentsFilterAttribute : PXStringListAttribute
	{
		public const string ALL = "A";
		public const string BY_COMPANY = "C";
		public const string BY_BRANCH = "B";

		public FinancialDocumentsFilterAttribute()
			: base(new[] { ALL, BY_COMPANY, BY_BRANCH },
			new[] { Messages.FromAllCompaniesAndBranches, Messages.FromCompany, Messages.FromBranch })
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			List<string> typesValues = new List<string>();
			List<string> typesLabels = new List<string>();

			typesValues.Add(ALL);
			typesLabels.Add(Messages.FromAllCompaniesAndBranches);

			if (PXAccess.FeatureInstalled<FeaturesSet.multiCompany>())
			{
				typesValues.Add(BY_COMPANY);
				typesLabels.Add(Messages.FromCompany);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.branch>())
			{
				typesValues.Add(BY_BRANCH);
				typesLabels.Add(Messages.FromBranch);
			}

			_AllowedValues = typesValues.ToArray();
			_AllowedLabels = typesLabels.ToArray();
			_NeutralAllowedLabels = null;

			base.CacheAttached(sender);
		}

		public sealed class All : Constant<string>
		{
			public All() : base(ALL) { }
		}

		public sealed class ByCompany : Constant<string>
		{
			public ByCompany() : base(BY_COMPANY) { }
		}

		public sealed class ByBranch : Constant<string>
		{
			public ByBranch() : base(BY_BRANCH) { }
		}
	}
	#endregion
}
