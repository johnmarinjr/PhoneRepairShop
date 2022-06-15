using PX.Common;

namespace PX.Objects.IN.RelatedItems
{
	[PXLocalizable]
	public static class Messages
	{
		public const string RelatedItemsFilter = "Related Items Filter";
		public const string RelatedItem = "Related Item";
		public const string RelatedItemsHistoryFilter = "Related Items History Filter";
		public const string RelatedItemHistory = "Related Item History";

		public const string RelatedItemsField = "Related Items";

		public const string UsingInventoryAsItsRelated = "The inventory item cannot be selected as its own related item. Select another item.";
		public const string RelatedItemAlreadyExists = "A line with the {0} relation and the {1} UOM already exists for {2} for the specified date range. Remove the line or change its details.";
		public const string DuplicateRelatedItems = "Changes cannot be saved. View the Related Items tab for details.";

		#region RelatedItems button popup messages

		public const string RelatedItemsAvailable = "{0} items exist for this item. Click this button to select an item.";
		public const string RelatedItemsAvailable2 = "{0} and {1} items exist for this item. Click this button to select an item.";
		public const string RelatedItemsAvailable3 = "{0}, {1}, and {2} items exist for this item. Click this button to select an item.";
		public const string RelatedItemsAvailable4 = "{0}, {1}, {2}, and {3} items exist for this item. Click this button to select an item.";

		public const string RelatedItemsRequired = "Related items are required for this item. Click this button to select a related item.";
		public const string SubstituteItemsRequired = "This item has to be substituted. Click this button to select a substitute item.";

		#endregion

		#region Qty field messages

		public const string QuantityIsNotSufficient  = "The quantity of {0} in the {1} warehouse is not sufficient to fulfill the order. {2} items are available for this item. Click the button in the Related Items column to select an item.";
		public const string QuantityIsNotSufficient2 = "The quantity of {0} in the {1} warehouse is not sufficient to fulfill the order. {2} and {3} items are available for this item. Click the button in the Related Items column to select an item.";
		public const string QuantityIsNotSufficient3 = "The quantity of {0} in the {1} warehouse is not sufficient to fulfill the order. {2}, {3}, and {4} items are available for this item. Click the button in the Related Items column to select an item.";
		public const string QuantityIsNotSufficient4 = "The quantity of {0} in the {1} warehouse is not sufficient to fulfill the order. {2}, {3}, {4}, and {5} items are available for this item. Click the button in the Related Items column to select an item.";

		public const string AvailableQtyIsLessThanSelected = "The available quantity of the item is less than the selected quantity.";

		#endregion

		#region InventoryID field messages

		public const string LineContainsRequiredRelatedItem = "The line contains the item that requires substitution. Select a substitute item by using the button in the Related Items column.";
		
		#endregion

		#region Document messages

		public const string ShipmentCannotBeCreated = "The shipment cannot be created because it contains items that require substitution. Select substitute items by using the buttons in the Related Items column of the Details tab.";
		public const string InvoiceCannotBeReleased = "The invoice cannot be released because it contains items that require substitution. Select substitute items by using the buttons in the Related Items column of the Details tab.";

		public const string ShipmentCannotBeCreatedOnProcessingScreen = "The shipment cannot be created because it contains items that require substitution. Select substitute items by using the buttons in the Related Items column of the Details tab on the Sales Orders (SO301000) form.";
		public const string InvoiceCannotBeReleasedOnProcessingScreen = "The invoice cannot be released because it contains items that require substitution. Select substitute items by using the buttons in the Related Items column of the Details tab on the Invoices (SO303000) form.";
		#endregion
	}
}
