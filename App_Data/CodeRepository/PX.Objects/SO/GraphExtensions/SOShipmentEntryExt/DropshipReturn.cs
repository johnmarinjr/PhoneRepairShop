using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.GraphExtensions.SOShipmentEntryExt
{
	public class DropshipReturn : PXGraphExtension<SOShipmentEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();

		[PXOverride]
		public virtual IEnumerable<PXResult<SOShipLine, SOLine>> CollectDropshipDetails(SOOrderShipment shipment,
			Func<SOOrderShipment, IEnumerable<PXResult<SOShipLine, SOLine>>> baseFunc)
		{
			if (shipment.Operation != SOOperation.Receipt)
			{
				foreach (var ret in baseFunc(shipment))
					yield return ret;
				yield break;
			}

			foreach (PXResult<POReceiptLine, SOLine> line in PXSelectJoin<POReceiptLine,
				InnerJoin<SOLine, On<POReceiptLine.FK.SOLine>>,
				Where<POReceiptLine.lineType, In3<POLineType.goodsForDropShip, POLineType.nonStockForDropShip>,
					And<POReceiptLine.receiptType, Equal<Required<POReceiptLine.receiptType>>,
					And<POReceiptLine.receiptNbr, Equal<Required<POReceiptLine.receiptNbr>>,
					And<SOLine.orderType, Equal<Current<SOOrderShipment.orderType>>, And<SOLine.orderNbr, Equal<Current<SOOrderShipment.orderNbr>>>>>>>>
				.SelectMultiBound(Base,
					new object[] { shipment },
					shipment.Operation == SOOperation.Receipt ? POReceiptType.POReturn : POReceiptType.POReceipt,
					shipment.ShipmentNbr))
			{
				yield return new PXResult<SOShipLine, SOLine>(SOShipLine.FromDropShip(line, line), line);
			}
		}
	}
}
