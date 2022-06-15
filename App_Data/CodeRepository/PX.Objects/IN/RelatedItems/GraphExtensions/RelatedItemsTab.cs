using PX.Api;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.Common.Scopes;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.IN.RelatedItems
{
	/// <summary>
	/// The extension provide an ability to edit related items for the current <see cref="InventoryItem"/>.
	/// The extension is active only if one or both of the following features are enabled: <see cref="FeaturesSet.relatedItems"/> and <see cref="FeaturesSet.commerceIntegration"/>.
	/// </summary>
	/// <typeparam name="TGraph"></typeparam>
	public class RelatedItemsTab<TGraph> : PXGraphExtension<TGraph>, PXImportAttribute.IPXPrepareItems, PXImportAttribute.IPXProcess
		where TGraph : PXGraph
	{
		[PXDependToCache(typeof(InventoryItem))]
		[PXImport(typeof(INRelatedInventory))]
		public SelectFrom<INRelatedInventory>
			.Where<INRelatedInventory.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>>
			.OrderBy<INRelatedInventory.relation.Asc, INRelatedInventory.rank.Asc, INRelatedInventory.inventoryID.Asc>.View RelatedItems;

		protected InventoryItem OriginalInventory => (InventoryItem)Base.Caches<InventoryItem>().Current;

        #region Actions

        public PXAction<InventoryItem> viewRelatedItem;
		[PXUIField(DisplayName = "View Related Item", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXLookupButton]
		public virtual IEnumerable ViewRelatedItem(PXAdapter adapter)
		{
			var relatedInventory = InventoryItem.PK.Find(Base, RelatedItems.Current?.RelatedInventoryID);

			if (relatedInventory != null)
				PXRedirectHelper.TryRedirect(Base.Caches[typeof(InventoryItem)], relatedInventory, "View Related Item", PXRedirectHelper.WindowMode.NewWindow);

			return adapter.Get();
		}

		#endregion

		#region Event handlers

		#region Fields updated

		protected virtual void _(Events.FieldUpdated<INRelatedInventory, INRelatedInventory.relation> e)
		{
			e.Cache.SetValue<INRelatedInventory.rank>(e.Row, 0);
			e.Cache.SetDefaultExt<INRelatedInventory.rank>(e.Row);

			e.Cache.SetDefaultExt<INRelatedInventory.interchangeable>(e.Row);

			if(e.Row.Required == true && e.Row.Relation == InventoryRelation.UpSell)
				e.Cache.SetDefaultExt<INRelatedInventory.required>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INRelatedInventory, INRelatedInventory.uom> e)
		{
			e.Cache.SetDefaultExt<INRelatedInventory.qty>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INRelatedInventory, INRelatedInventory.isActive> e)
		{
			if ((bool?)e.NewValue == true && e.OldValue != e.NewValue)
			{
				e.Cache.SetValue<INRelatedInventory.expirationDate>(e.Row, null);
				e.Cache.SetDefaultExt<INRelatedInventory.effectiveDate>(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<INRelatedInventory, INRelatedInventory.effectiveDate> e)
		{
			var startDate = (DateTime?)e.NewValue;

			if (!IsCorrectActivePeriod((DateTime?)e.NewValue, e.Row.ExpirationDate))
				e.Cache.SetValue<INRelatedInventory.expirationDate>(e.Row, null);

			if (startDate != null && startDate > Base.Accessinfo.BusinessDate)
				e.Cache.SetValue<INRelatedInventory.isActive>(e.Row, false);
		}

		protected virtual void _(Events.FieldUpdated<INRelatedInventory, INRelatedInventory.expirationDate> e)
		{
			var endDate = (DateTime?)e.NewValue;

			if (!IsCorrectActivePeriod(endDate, e.Row.ExpirationDate) 
				|| (endDate != null && endDate < Base.Accessinfo.BusinessDate))
				e.Cache.SetValue<INRelatedInventory.isActive>(e.Row, false);
		}

        #endregion

		#region Row event handlers

		protected virtual void _(Events.RowInserted<INRelatedInventory> e)
		{
			if (Base.IsImportFromExcel && _duplicateFinder != null)
				_duplicateFinder.AddItem(e.Row);
		}

		#endregion

		#region Fields defaulting

		protected virtual void _(Events.FieldDefaulting<INRelatedInventory, INRelatedInventory.rank> e)
		{
			var relation = e.Row?.Relation;
			if (relation == null)
				return;
			INRelatedInventory maxRankedRelation = SelectFrom<INRelatedInventory>
				.Where<INRelatedInventory.inventoryID.IsEqual<INRelatedInventory.inventoryID.FromCurrent>
				.And<INRelatedInventory.relation.IsEqual<INRelatedInventory.relation.FromCurrent>>>
				.OrderBy<INRelatedInventory.rank.Desc>.View.SelectSingleBound(Base, new[] { e.Row });
			e.NewValue = (maxRankedRelation?.Rank ?? 0) + 1;
		}

		protected virtual void _(Events.FieldDefaulting<INRelatedInventory, INRelatedInventory.qty> e)
		{
			e.NewValue = CalculateRelatedItemQty(e.Row);
		}

		#endregion

		#region Fields verifying

		protected virtual void _(Events.FieldVerifying<INRelatedInventory, INRelatedInventory.required> e)
		{
			if ((bool?)e.NewValue == true && e.Row.Relation == InventoryRelation.UpSell)
			{
				e.NewValue = false;
				e.Cancel = true;
			}
		}

		#endregion

		#endregion

		protected virtual bool IsCorrectActivePeriod(DateTime? startTime, DateTime? endTime) => endTime == null || startTime == null || startTime <= endTime;

		protected virtual decimal? CalculateRelatedItemQty(INRelatedInventory relatedInventory)
        {
			if (relatedInventory == null || OriginalInventory == null)
				return null;
			if (relatedInventory.Relation != InventoryRelation.Substitute)
				return 1m;

			var origInventoryUnit = OriginalInventory.BaseUnit;

			if (origInventoryUnit == relatedInventory.UOM)
				return 1m;

			decimal qty;
			if (INUnitAttribute.TryConvertGlobalUnits(Base, origInventoryUnit, relatedInventory.UOM, 1m, INPrecision.QUANTITY, out qty))
				return qty;

			return 1m;
		}

		#region Find of duplicates

		protected virtual void CheckForDuplicates()
		{
			bool anyDuplicates = false;
			var relatedItems = RelatedItems.SelectMain();
			var duplicateFinder = new MultiDuplicatesSearchEngine<INRelatedInventory>(RelatedItems.Cache, GetAlternativeKeyFields());
			foreach (var relatedItem in relatedItems)
			{
				var items = duplicateFinder[relatedItem];
				if(items
					.Any(x => HaveIntersection(x, relatedItem)))
                {
					anyDuplicates = true;
					var error = new PXSetPropertyException(Messages.RelatedItemAlreadyExists, PXErrorLevel.RowError,
						new InventoryRelation.ListAttribute().ValueLabelDic[relatedItem.Relation],
						relatedItem.UOM,
						RelatedItems.Cache.GetValueExt<INRelatedInventory.relatedInventoryID>(relatedItem));
					if (RelatedItems.Cache.RaiseExceptionHandling<INRelatedInventory.relatedInventoryID>(relatedItem, null, error))
						throw error;
                }
				else
					duplicateFinder.AddItem(relatedItem);
			}
			if(anyDuplicates)
				throw new PXException(Messages.DuplicateRelatedItems);
		}

		protected virtual bool HaveIntersection(INRelatedInventory first, INRelatedInventory second)
        {
			return
				first.ExpirationDate == null && second.ExpirationDate == null
				|| first.ExpirationDate == null && first.EffectiveDate <= second.ExpirationDate
				|| second.ExpirationDate == null && second.EffectiveDate <= first.ExpirationDate
				|| first.EffectiveDate <= second.EffectiveDate && second.EffectiveDate <= first.ExpirationDate
				|| first.EffectiveDate <= second.ExpirationDate && second.ExpirationDate <= first.ExpirationDate
				|| second.EffectiveDate <= first.EffectiveDate && first.ExpirationDate <= second.ExpirationDate;
		}

		protected virtual Type[] GetAlternativeKeyFields()
		{
			return new[]
			{
				typeof(INRelatedInventory.relatedInventoryID),
				typeof(INRelatedInventory.relation),
				typeof(INRelatedInventory.uom)
			};
		}

		private MultiDuplicatesSearchEngine<INRelatedInventory> _duplicateFinder;

		private bool DontUpdateExistRecords
		{
			get
			{
				object dontUpdateExistRecords;
				return Base.IsImportFromExcel && PXExecutionContext.Current.Bag.TryGetValue(PXImportAttribute._DONT_UPDATE_EXIST_RECORDS, out dontUpdateExistRecords) &&
					true.Equals(dontUpdateExistRecords);
			}
		}

		#endregion

		#region PXImportAttribute.IPXPrepareItems and PXImportAttribute.IPXProcess implementations

		public virtual bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
		{
			if (viewName.Equals(nameof(RelatedItems), StringComparison.InvariantCultureIgnoreCase) && !DontUpdateExistRecords)
			{
				if (_duplicateFinder == null)
				{
					var relatedItems = RelatedItems.SelectMain();
					_duplicateFinder = new MultiDuplicatesSearchEngine<INRelatedInventory>(RelatedItems.Cache, GetAlternativeKeyFields(), relatedItems);
				}

				var row = _duplicateFinder.CreateEntity(values, typeof(INRelatedInventory.effectiveDate), typeof(INRelatedInventory.expirationDate));
				var potentialDuplicates = _duplicateFinder[row];
				var firstDuplicate = potentialDuplicates.FirstOrDefault(x => HaveIntersection(x, row));
				if(firstDuplicate != null)
				{
					if (keys.Contains(nameof(INRelatedInventory.LineID)))
						keys[nameof(INRelatedInventory.LineID)] = firstDuplicate.LineID;
					else
						keys.Add(nameof(INRelatedInventory.LineID), firstDuplicate.LineID);
				}
			}
			return true;
		}

		public virtual bool RowImporting(string viewName, object row) => row == null;
		public virtual bool RowImported(string viewName, object row, object oldRow) => oldRow == null;
		public virtual void PrepareItems(string viewName, IEnumerable items) { }
		public virtual void ImportDone(PXImportAttribute.ImportMode.Value mode)
		{
			_duplicateFinder = null;
		}
		#endregion
	}
}
