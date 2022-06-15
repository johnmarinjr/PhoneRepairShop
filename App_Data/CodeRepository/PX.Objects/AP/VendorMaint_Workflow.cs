using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;

namespace PX.Objects.AP
{
	using static PX.Data.WorkflowAPI.BoundedTo<VendorMaint, VendorR>;

	public class VendorMaint_Workflow : PXGraphExtension<VendorMaint>
	{
		public static bool IsActive() => false;

		public override void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<VendorMaint, VendorR>();

			#region Categories
			var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
				category => category.DisplayName(ActionCategory.Processing));
			var managementCategory = context.Categories.CreateNew(ActionCategoryNames.Management,
				category => category.DisplayName(ActionCategory.Management));
			var customOtherCategory = context.Categories.CreateNew(ActionCategoryNames.CustomOther,
				category => category.DisplayName(ActionCategory.Other));
			#endregion

			#region Conditions
			Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
			var conditions = new
			{
				IsCreateContactDisabled 
					= Bql<Vendor.vStatus.IsNotIn<VendorStatus.active, VendorStatus.hold>>(),
				IsNewBillAdjustmentDisabled 
					= Bql<Vendor.vStatus.IsNotIn<VendorStatus.active, VendorStatus.holdPayments, VendorStatus.oneTime>>(),
				IsNewManualCheckDisabled
					= Bql<Vendor.vStatus.IsNotIn<VendorStatus.active, VendorStatus.oneTime>>(),
				IsPayBillDisabled
					= Bql<Vendor.vStatus.IsIn<VendorStatus.holdPayments, VendorStatus.inactive, VendorStatus.hold>>()
			}.AutoNameConditions();
			#endregion

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						// "Vendor Management" folder
						actions.Add<VendorMaint.CreateContactFromVendorGraphExt>(g => g.CreateContact, a => a.WithCategory(managementCategory).IsDisabledWhen(conditions.IsCreateContactDisabled));
						actions.Add<VendorMaint.CreateContactFromVendorGraphExt>(g => g.CreateContactToolBar,a => a.IsDisabledWhen(conditions.IsCreateContactDisabled));
						actions.Add<VendorMaint.ExtendToCustomer>(e => e.extendToCustomer, a => a.WithCategory(managementCategory));

						// "Document Processing" folder
						actions.Add(g => g.newBillAdjustment, a => a.WithCategory(processingCategory).IsDisabledWhen(conditions.IsNewBillAdjustmentDisabled));
						actions.Add(g => g.newManualCheck, a => a.WithCategory(processingCategory).IsDisabledWhen(conditions.IsNewManualCheckDisabled));
						actions.Add(g => g.approveBillsForPayments, a => a.WithCategory(processingCategory));
						actions.Add(g => g.payBills, a => a.WithCategory(processingCategory).IsDisabledWhen(conditions.IsPayBillDisabled));

						// "Other" folder
						actions.Add(g => g.ChangeID, a => a.WithCategory(customOtherCategory));
						actions.Add<VendorMaint.DefContactAddressExt>(e => e.ValidateAddresses, a => a.WithCategory(customOtherCategory));
						actions.Add(g => g.viewBusnessAccount, a => a.WithCategory(customOtherCategory));
						actions.Add<VendorMaint.ExtendToCustomer>(e => e.viewCustomer, a => a.WithCategory(customOtherCategory));
						actions.Add(g => g.viewRestrictionGroups, a => a.WithCategory(customOtherCategory));

						// "Inquiries" folder
						actions.Add(g => g.vendorDetails, a => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.vendorPrice, a => a.WithCategory(PredefinedCategory.Inquiries));

						// "Reports" folder
						actions.Add(g => g.balanceByVendor, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aPDocumentRegister, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.vendorHistory, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aPAgedPastDue, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.aPAgedOutstanding, a => a.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.repVendorDetails, a => a.WithCategory(PredefinedCategory.Reports));
					})
					.WithCategories(categories =>
					{
						categories.Add(managementCategory);
						categories.Add(processingCategory);
						categories.Add(customOtherCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(customOtherCategory));
						categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(context.Categories.Get(FolderType.InquiriesFolder)));
					});
			});
		}

		public static class ActionCategoryNames
		{
			public const string Management = "VendorManagement";
			public const string Processing = "DocumentProcessing";
			public const string CustomOther = "CustomOther";
		}

		public static class ActionCategory
		{
			public const string Management = "Vendor Management";
			public const string Processing = "Document Processing";
			public const string Other = "Other";
		}
	}
}
