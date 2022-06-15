using System;
using PX.Data;
using PX.Objects.SO;
using System.Collections.Generic;
using PX.Commerce.Core;
using PX.Data.WorkflowAPI;

namespace PX.Commerce.Objects
{
	[Serializable]
	public sealed class BCSOOrderExt : PXCacheExtension<SOOrder>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXAppendSelectorColumns(typeof(SOOrder.customerRefNbr))]
		public String OrderNbr { get; set; }

		#region ExternalOrderOriginal
		public abstract class externalOrderOriginal : PX.Data.BQL.BqlBool.Field<externalOrderOriginal> { }
		[PXDBBool()]
		[PXUIField(DisplayName = "External Order Original")]
		public Boolean? ExternalOrderOriginal { get; set; }
		#endregion

		#region ExternalRefundRef
		public abstract class externalRefundRef : PX.Data.BQL.BqlString.Field<externalRefundRef> { }
		[PXDBString(50, IsUnicode = true)]
		public string ExternalRefundRef { get; set; }
		#endregion

		#region ExternalOrderOrigin
		public abstract class externalOrderOrigin : PX.Data.BQL.BqlInt.Field<externalOrderOrigin> { }
		[PXDBInt()]
		[PXUIField(DisplayName = "External Order Origin")]
		[PXSelector(
			typeof(Search<BCBinding.bindingID>),
			new Type[] {
				typeof(BCBinding.bindingID),
				typeof(BCBinding.connectorType),
				typeof(BCBinding.bindingName),
				typeof(BCBinding.isActive),
				typeof(BCBinding.isDefault) },
			SubstituteKey = typeof(BCBinding.bindingName))]
		public int? ExternalOrderOrigin { get; set; }
		#endregion

		#region ExternalOrderSource
		public abstract class externalOrderSource : PX.Data.BQL.BqlString.Field<externalOrderSource> { }
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "External Order Source")]
		public string ExternalOrderSource { get; set; }
		#endregion

		#region RiskHold
		public abstract class riskHold : PX.Data.BQL.BqlBool.Field<riskHold> { }
		[PXDBBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public Boolean? RiskHold { get; set; }
		#endregion

		#region MaxRiskScore
		public abstract class maxRiskScore : PX.Data.BQL.BqlDecimal.Field<maxRiskScore> { }
		[PXDBDecimal()]
		[PXUIField(DisplayName = "Max Risk Score")]
		public decimal? MaxRiskScore { get; set; }
		#endregion

		#region RiskStatus
		public abstract class riskStatus : PX.Data.BQL.BqlString.Field<riskStatus> { }
		[PXString(IsUnicode = true)]
		[PXUIField(DisplayName = "Risk Status", Enabled = false)]
		[PXFormula(typeof(Switch<
			Case<Where<maxRiskScore, Greater<decimal60>>, BCRiskStatusAttribute.high,
			Case<Where<maxRiskScore, Greater<decimal20>, And<maxRiskScore, LessEqual<decimal60>>>, BCRiskStatusAttribute.medium,
			Case<Where<maxRiskScore, GreaterEqual<decimal0>, And<maxRiskScore, LessEqual<decimal20>>>, BCRiskStatusAttribute.low>>>,
			BCRiskStatusAttribute.none>
			))]
		public string RiskStatus { get; set; }
		#endregion

		#region Events
		public class Events : PXEntityEvent<SOOrder>.Container<Events>
		{
			public PXEntityEvent<SOOrder> RiskHoldConditionStatisfied;
		}
		#endregion

		#region Constants
		public class decimal60 : PX.Data.BQL.BqlDecimal.Constant<decimal60>
		{
			public decimal60()
				: base(60m)
			{
			}
		}

		public class decimal20 : PX.Data.BQL.BqlDecimal.Constant<decimal20>
		{
			public decimal20()
				: base(20m)
			{
			}
		}

		public class decimal0 : PX.Data.BQL.BqlDecimal.Constant<decimal0>
		{
			public decimal0()
				: base(0m)
			{
			}
		}

		public class decimal100 : PX.Data.BQL.BqlDecimal.Constant<decimal100>
		{
			public decimal100()
				: base(100m)
			{
			}
		}

		#endregion
	}
}