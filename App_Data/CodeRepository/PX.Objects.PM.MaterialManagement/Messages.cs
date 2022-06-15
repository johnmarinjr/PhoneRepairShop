using PX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM.MaterialManagement
{
    [PXLocalizable(Messages.Prefix)]
    public static partial class Messages
    {
        public const string Prefix = "PM.MaterialManagement Error";
        public const string StockNotInitialized =  "Project stock is not initialized";
        public const string SpecificProjectNotSupported = "Only projects tracked by quantity can be split. Select a non-project code (X) or a project that has Inventory Tracking set to Track by Project Quantity on the Projects (PM301000) form.";
		public const string NothingToShipTraced_Linked = "The {0} {1} sales order cannot be shipped in full. There are not enough stock items available for the {2} project at the {3} warehouse. To be able to create a shipment, transfer or receive the necessary quantity of the items to the {3} warehouse location linked to the {2} project.";
		public const string NothingToShipTraced_NotLinked = "The {0} {1} sales order cannot be shipped in full. There are not enough project-specific stock items available at the {3} warehouse for the {2} project. To be able to create a shipment, transfer or receive the necessary quantity of the items for the {2} project.";
		public const string MixedLocationsAreNotAllowed = "Project Specific - no mixed locations";

	}
}
