using PX.Data;
using PX.Objects.CS;
using PX.Objects.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.GL
{
	[PXDBInt]
	[PXInt]
	[PXUIField(DisplayName = "Project", Visibility = PXUIVisibility.Visible)]
	[PXRestrictor(typeof(Where<PMProject.isActive, Equal<True>, Or<PMProject.nonProject, Equal<True>>>), PM.Messages.ProjectStatusIs, typeof(PMProject.contractCD), typeof(PMProject.status))]
	[PXRestrictor(typeof(Where<PMProject.baseType, NotEqual<CT.CTPRType.projectTemplate>,
		And<PMProject.baseType, NotEqual<CT.CTPRType.contractTemplate>>>), PM.Messages.TemplateContract, typeof(PMProject.contractCD))]
	[PXRestrictor(typeof(Where<PMProject.visibleInGL, Equal<True>, Or<PMProject.nonProject, Equal<True>>>), PM.Messages.ProjectInvisibleInModule, typeof(PMProject.contractCD))]
	[PXUIEnabled(typeof(Where<FeatureInstalled<FeaturesSet.projectAccounting>>))]
	[PXUIVisible(typeof(Where<FeatureInstalled<FeaturesSet.projectAccounting>>))]
	public class ActiveProjectAttribute : PM.ProjectBaseAttribute
	{

		public Type AccountFieldType { get; set; }
		public ActiveProjectAttribute() : base(null)
		{
			Filterable = true;
		}

		#region IPXFieldVerifyingSubscriber Members

		public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PMProject project = PXSelect<PMProject>.Search<PMProject.contractID>(sender.Graph, e.NewValue);

			if (project != null && project.NonProject != true && project.BaseType == CT.CTPRType.Project && AccountFieldType != null)
			{
				Account account =
					PXSelect<Account, Where<Account.accountID, Equal<Required<Account.accountID>>>>.Select(sender.Graph,
						sender.GetValue(e.Row, AccountFieldType.Name));

				if (account != null && account.AccountGroupID == null)
				{
					var newRow = sender.CreateCopy(e.Row);
					// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification]
					sender.SetValue(newRow, _FieldName, e.NewValue);
					e.NewValue = sender.GetStateExt(newRow, _FieldName);
					throw new PXSetPropertyException(PM.Messages.NoAccountGroup, PXErrorLevel.Error, account.AccountCD);
				}
			}
		}
		#endregion

		public static void VerifyAccountInInAccountGroup(Account account)
		{
			if (account != null && account.AccountGroupID == null)
			{
				throw new PXSetPropertyException(PM.Messages.NoAccountGroup, PXErrorLevel.Error, account.AccountCD);
			}
		}

		public static void VerifyAccountIsInAccountGroup<T>(PXCache cache, EventArgs e, Account account = null, bool throwOnVerifying = false) where T : IBqlField
		{
			VerifyAccountIsInAccountGroup(cache, typeof(T).Name, e, account, throwOnVerifying);
		}
		public static void VerifyAccountIsInAccountGroup(PXCache cache, string fieldName, EventArgs e, Account account = null, bool throwOnVerifying = false)
		{
			if (cache == null || e == null) return;

			var verifying = e as PXFieldVerifyingEventArgs;
			var updated = e as PXFieldUpdatedEventArgs;
			var persisting = e as PXRowPersistingEventArgs;
			var rowselected = e as PXRowSelectedEventArgs;

			var row = verifying?.Row ?? updated?.Row ?? persisting?.Row ?? rowselected?.Row;
			if (account == null)
			{
				var accountID = (verifying != null) ? verifying.NewValue : cache.GetValue(row, fieldName);
				if (accountID == null) return;
				account = (Account)PXSelectorAttribute.Select(cache, row, fieldName, accountID) ?? (Account)PXSelect<Account>.Search<Account.accountCD>(cache.Graph, accountID);
			}
			if (account == null) return;

			try
			{
				VerifyAccountInInAccountGroup(account);
			}
			catch (PXSetPropertyException ex)
			{
				cache.RaiseExceptionHandling(fieldName, row, account.AccountCD, ex);

				if (ex.ErrorLevel >= PXErrorLevel.Error && verifying != null)
				{
					verifying.Cancel = true;
					if (throwOnVerifying)
					{
						var state = cache.GetStateExt(row, fieldName) as PXFieldState;
						if (state != null)
							verifying.NewValue = state.Value;
						throw ex;
					}
				}
				else if (ex.ErrorLevel >= PXErrorLevel.Error && persisting != null)
				{
					persisting.Cancel = true;
					throw ex;
				}
			}
		}
	}
}
