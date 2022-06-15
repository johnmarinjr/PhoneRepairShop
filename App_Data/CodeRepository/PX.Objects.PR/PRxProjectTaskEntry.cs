using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;

namespace PX.Objects.PR
{
	public class PRxProjectTaskEntry : PXGraphExtension<ProjectTaskEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(AccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.earningsAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(SubAccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.earningsSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(AccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.benefitExpenseAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(SubAccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.benefitExpenseSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(AccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.taxExpenseAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(SubAccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.taxExpenseSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(AccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.ptoExpenseAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(SubAccountAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		public virtual void _(Events.CacheAttached<PRxPMTask.ptoExpenseSubID> e) { }
	}
}
