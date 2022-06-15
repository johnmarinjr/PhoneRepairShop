using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	using static PX.Data.WorkflowAPI.BoundedTo<CustomerMaint, Customer>;

	public class CustomerMaint_Workflow : PXGraphExtension<CustomerMaint>
	{
		public static bool IsActive() => false;


		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<CustomerMaint, Customer>();

			#region Categories
			var customerManagementCategory = context.Categories.CreateNew(CategoryID.CustomerManagement,
				category => category.DisplayName(CategoryNames.CustomerManagement));
			var documentProcessingCategory = context.Categories.CreateNew(CategoryID.DocumentProcessing,
				category => category.DisplayName(CategoryNames.DocumentProcessing));
			var statementsCategory = context.Categories.CreateNew(CategoryID.Statements,
				category => category.DisplayName(CategoryNames.Statements));
			var servicesCategory = context.Categories.CreateNew(CategoryID.Services,
				category => category.DisplayName(CategoryNames.Services));
			var otherCategory = context.Categories.CreateNew(CategoryID.Other,
				category => category.DisplayName(CategoryNames.Other));
			#endregion

			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsCreateContactDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime>>(),
				IsNewInvoiceMemoDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime>>(),
				IsNewSalesOrderDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime>>(),
				IsNewPaymentDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime, CustomerStatus.creditHold, CustomerStatus.hold>>(),
				IsWriteOffBalanceDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime, CustomerStatus.creditHold>>(),
				IsGenerateOnDemandStatementDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime, CustomerStatus.creditHold, CustomerStatus.inactive>>(),
				IsRegenerateLastStatementDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime, CustomerStatus.creditHold, CustomerStatus.inactive>>(),
				IsViewBusnessAccountDisabled
					= Bql<Customer.status.IsNotIn<CustomerStatus.active, CustomerStatus.oneTime, CustomerStatus.creditHold, CustomerStatus.hold, CustomerStatus.inactive>>(),
				IsInventoryAndOrderManagementOff 
					= PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() ? Bql<True.IsEqual<False>>() : Bql<True.IsEqual<True>>(),
			}.AutoNameConditions();
			#endregion

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						// "Actions" folder
						actions.Add<CustomerMaint.CreateContactFromCustomerGraphExt>(g => 
										g.CreateContact, a => a
											.WithCategory(customerManagementCategory)
											.IsDisabledWhen(conditions.IsCreateContactDisabled));
						actions.Add<CustomerMaint.CreateContactFromCustomerGraphExt>(g =>
										g.CreateContactToolBar, a => a
											.IsDisabledWhen(conditions.IsCreateContactDisabled));
						actions.Add(g => g.newInvoiceMemo, a => a
											.WithCategory(documentProcessingCategory)
											.IsDisabledWhen(conditions.IsNewInvoiceMemoDisabled));
						actions.Add(g => g.newSalesOrder, a => a
											.WithCategory(documentProcessingCategory)
											.IsDisabledWhen(conditions.IsNewSalesOrderDisabled)
											.IsHiddenWhen(conditions.IsInventoryAndOrderManagementOff));
						actions.Add(g => g.newPayment, a => a
											.WithCategory(documentProcessingCategory)
											.IsDisabledWhen(conditions.IsNewPaymentDisabled));
						actions.Add(g => g.writeOffBalance, a => a
											.WithCategory(documentProcessingCategory)
											.IsDisabledWhen(conditions.IsWriteOffBalanceDisabled));
						actions.Add(g => g.generateOnDemandStatement, a => a
											.WithCategory(statementsCategory)
											.IsDisabledWhen(conditions.IsGenerateOnDemandStatementDisabled));
						actions.Add(g => g.regenerateLastStatement, a => a
											.WithCategory(statementsCategory)
											.IsDisabledWhen(conditions.IsRegenerateLastStatementDisabled));
						actions.Add(g => g.viewBusnessAccount, a => a
											.IsDisabledWhen(conditions.IsViewBusnessAccountDisabled));
						actions.Add<CustomerMaint.ExtendToVendor>(g => 
										g.viewVendor, a => a
											.WithCategory(otherCategory));
						actions.Add<CustomerMaint.DefContactAddressExt>(g => 
										g.ValidateAddresses, a => a
											.WithCategory(otherCategory));
						actions.Add(g => g.ChangeID, a => a
											.WithCategory(otherCategory, nameof(CustomerMaint.CreateContactFromCustomerGraphExt.CreateContact)));
						actions.Add(g => g.viewRestrictionGroups, a => a
											.WithCategory(otherCategory));
						actions.Add<CustomerMaint.ExtendToVendor>(g => 
										g.extendToVendor, a => a
											.WithCategory(customerManagementCategory));

						// "Inquiries" folder
						actions.Add(g => g.customerDocuments, a => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.statementForCustomer, a => a.WithCategory(statementsCategory));
						actions.Add(g => g.salesPrice, a => a.WithCategory(PredefinedCategory.Inquiries));

						// "Reports" folder
						actions.Add(g => g.aRBalanceByCustomer, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aRRegister, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.customerHistory, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aRAgedPastDue, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aRAgedOutstanding, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.customerDetails, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.customerStatement, a => a.WithCategory(statementsCategory));
					})
					.WithCategories(categories =>
					{
						categories.Add(customerManagementCategory);
						categories.Add(documentProcessingCategory);
						categories.Add(statementsCategory);
						categories.Add(servicesCategory); 
						categories.Add(otherCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(otherCategory));
						categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(FolderType.InquiriesFolder));
					});
			});
		}

		public static class CategoryNames
		{
			public const string CustomerManagement = "Customer Management";
			public const string DocumentProcessing = "Document Processing";
			public const string Statements = "Statements";
			public const string Services = "Services";
			public const string Other = "Other";
		}

		public static class CategoryID
		{
			public const string CustomerManagement = "CustomerManagementID";
			public const string DocumentProcessing = "DocumentProcessingID";
			public const string Statements = "StatementsID";
			public const string Services = "ServicesID";
			public const string Other = "OtherID";
		}
	}
}
