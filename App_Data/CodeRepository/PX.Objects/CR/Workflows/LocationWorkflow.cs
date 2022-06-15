using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Common;

namespace PX.Objects.CR.Workflows
{
	public static class LocationWorkflow
	{
		public static void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<LocationMaint, Location>();

			#region Categories
			var customOtherCategory = context.Categories.CreateNew(ActionCategoryNames.CustomOther,
				category => category.DisplayName(ActionCategory.Other));
			#endregion

			var isDefaultCondition = context
				.Conditions
				.FromBql<Location.isDefault.IsEqual<True>>()
				.WithSharedName("IsDefault");

			context.AddScreenConfigurationFor(screen => screen
				.WithActions(actions =>
				{
					actions.Add(g => g.validateAddresses, a => a.InFolder(customOtherCategory));
				})
				.WithCategories(categories =>
				{
					categories.Add(customOtherCategory);
				}));
		}

		public static class ActionCategoryNames
		{
			public const string CustomOther = "CustomOther";
		}

		public static class ActionCategory
		{
			public const string Other = "Other";
		}
	}
}
