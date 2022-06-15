using PX.Data;
using PX.Objects.AM;
using SP.Objects.IN;
using PX.Objects.CM;

namespace SP.Objects.AM.GraphExtensions
{
	public abstract class PortalCardLinesConfigurationBase<TGraph> : PXGraphExtension<TGraph>
			where TGraph : PXGraph
	{
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PortalConfigurationSelect<
			Where<AMConfigurationResults.createdByID, Equal<Current<PortalCardLines.userID>>,
				And<AMConfigurationResults.inventoryID, Equal<Current<PortalCardLines.inventoryID>>,
				And<AMConfigurationResults.siteID, Equal<Current<PortalCardLines.siteID>>,
				And<AMConfigurationResults.uOM, Equal<Current<PortalCardLines.uOM>>,
				And<AMConfigurationResults.ordNbrRef, IsNull,
				And<AMConfigurationResults.opportunityQuoteID, IsNull>>>>>>> ItemConfiguration;

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXParent(typeof(Select<PortalCardLines,
			Where<PortalCardLines.userID, Equal<Current<AMConfigurationResults.createdByID>>,
				And<PortalCardLines.inventoryID, Equal<Current<AMConfigurationResults.inventoryID>>,
					And<PortalCardLines.siteID, Equal<Current<AMConfigurationResults.siteID>>,
						And<PortalCardLines.uOM, Equal<Current<AMConfigurationResults.uOM>>,
							And<Current<AMConfigurationResults.ordNbrRef>, IsNull,
								And<Current<AMConfigurationResults.opportunityQuoteID>, IsNull>>>>>>>))]
		protected virtual void _(Events.CacheAttached<AMConfigurationResults.siteID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		protected virtual void _(Events.CacheAttached<AMConfigurationResults.curyID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PX.Objects.CM.CurrencyInfoAttribute))]
		protected virtual void _(Events.CacheAttached<AMConfigurationResults.curyInfoID> e) { }

		public virtual void SetPersistedConfigurations()
		{
			ItemConfiguration.ConfigPersistSetPersisted();
		}

		public virtual void PersistConfigurations()
		{
			ItemConfiguration.ConfigPersistInsertUpdateNoTranScope();
		}

		protected virtual void _(Events.RowInserted<PortalCardLines> e, PXRowInserted del)
		{
			del?.Invoke(e.Cache, e.Args);

			if (e.Row == null)
			{
				return;
			}

			InsertConfigurationResult(e.Cache, e.Row);
		}

		protected abstract void InsertConfigurationResult(PXCache sender, PortalCardLines row);

		protected virtual void InsertConfigurationResult(PXCache sender, PortalCardLines row, CurrencyInfo curyInfo, int? baccountID, int? baccountLocationID)
		{
			if (row == null)
			{
                throw new PXArgumentException(nameof(row));
			}

			var rowExt = PXCache<PortalCardLines>.GetExtension<PortalCardLinesExt>(row);;

			var configurationID = rowExt?.AMConfigurationID;
			if (string.IsNullOrWhiteSpace(configurationID))
			{
				if (!ConfigurationSelect.TryGetDefaultConfigurationID(Base, row.InventoryID, row.SiteID, out configurationID))
				{
					return;
				}

				if (rowExt != null)
				{
					sender.SetValueExt<PortalCardLinesExt.aMConfigurationID>(row, configurationID);
				}
			}

			ItemConfiguration.Insert(new AMConfigurationResults
			{
				ConfigurationID = configurationID,
				// Using "createdByID" as the "UserID" key
				InventoryID = row.InventoryID,
				SiteID = row.SiteID,
				UOM = row.UOM,
				CuryID = curyInfo?.CuryID,
				CuryInfoID = curyInfo?.CuryInfoID,
				Qty = row.Qty.GetValueOrDefault(),
				CustomerID = baccountID,
				CustomerLocationID = baccountLocationID
			});
		}
	}
}
