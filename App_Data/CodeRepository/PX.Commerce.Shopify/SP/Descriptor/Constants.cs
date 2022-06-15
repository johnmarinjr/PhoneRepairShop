using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;

namespace PX.Commerce.Shopify
{
	public static class ShopifyConstants
	{
		//Constant Value
		public const int ApiCallLimitDefault = 2;
		public const int ApiCallLimitPlus = 4;
		public const int ProductOptionsLimit = 3;
		public const int ProductVarantsLimit = 100;

		public const string ApiVersion_202201 = "2022-01";
		public const string InventoryManagement_Shopify = "shopify";
		
		public const string ValueType_SingleString = "single_line_text_field";
		public const string ValueType_MultiString = "multi_line_text_field";
		public const string Variant = "Variant";
		public const string POSSource = "pos";
		public const string ProductImage = "ProductImage";
		public const string Bogus = "bogus";
		public const string GiftNote = "gift-note";
		public const string ShopifyPayments = "shopify_payments";
		public const string GiftCard = "gift_card";
		public const string GiftCardID = "gift_card_id";
		public const string GiftCardLastCharacters = "gift_card_last_characters";
		public const string MetafieldFormat = "Namespace.Key";
	}
}
