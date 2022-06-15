using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.CR.Extensions.Cache;

namespace PX.Objects.CR.Extensions
{
	/// <exclude/>
	public abstract class CRUpdateRelatedContactInfoGraphExt<TGraph> : PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		#region State

		private readonly string SLOT_KEY = $"{nameof(CRUpdateRelatedContactInfoGraphExt<TGraph>)}+{typeof(TGraph).Name}";

		public bool? UpdateRelatedInfo
		{
			get
			{
				return PX.Common.PXContext.GetSlot<bool?>(SLOT_KEY);
			}
			set
			{
				PX.Common.PXContext.SetSlot<bool?>(SLOT_KEY, value);
			}
		}

		#endregion

		#region Events

		[PXOverride]
		public virtual int Persist(Type cacheType, PXDBOperation operation, Func<Type, PXDBOperation, int> del)
		{
			this.UpdateRelatedInfo = false;

			return del(cacheType, operation);
		}

		#endregion

		#region Methods

		public virtual void UpdateFieldsValuesWithoutPersist(PXCache cache, object source, object target)
		{
			foreach (var field in cache.GetFields_ContactInfo())
			{
				cache.SetValue(target, field, cache.GetValue(source, field));
			}
		}

		public virtual void UpdateFieldsValuesWithoutPersist(PXResult source, PXResult target)
		{
			foreach (var table in source.Tables)
			{
				var sourceItem = source[table];
				var targetItem = target[table];
				if (sourceItem != null && targetItem != null)
				{
					UpdateFieldsValuesWithoutPersist(Base.Caches[table], sourceItem, targetItem);
				}
			}
		}

		public virtual void SetUpdateRelatedInfo<TEntity>(Events.RowPersisting<TEntity> eventArgs, IEnumerable<string> fieldNames)
			where TEntity : class, IBqlTable, new()
		{
			var original = eventArgs.Cache.GetOriginal(eventArgs.Row) as TEntity;

			this.UpdateRelatedInfo =
				this.UpdateRelatedInfo == true
				|| original == null
				|| fieldNames.Any(fieldName => !object.Equals(eventArgs.Cache.GetValue(eventArgs.Row, fieldName), eventArgs.Cache.GetValue(original, fieldName)));
		}

		public virtual void SetUpdateRelatedInfo<TEntity, TField>(Events.RowPersisting<TEntity> eventArgs, bool _ = true) // hack to make it non-event method, otherwise Acumatica goes crrrrazy
			where TEntity : class, IBqlTable, new()
			where TField : class, IBqlField
		{
			var original = eventArgs.Cache.GetOriginal(eventArgs.Row) as TEntity;

			this.UpdateRelatedInfo =
				this.UpdateRelatedInfo == true
				|| original == null
				|| !object.Equals(eventArgs.Cache.GetValue<TField>(eventArgs.Row), eventArgs.Cache.GetValue<TField>(original));
		}

		public virtual void UpdateContact<TSelect>(PXCache cache, object row, TSelect select, params object[] pars)
			where TSelect : PXSelectBase<Contact>
		{
			var ids = select
				.Select(pars)
				.Select(l => l.GetItem<Contact>().ContactID)
				.ToList();
			if (ids.Count == 0)
				return;

			Update<TSelect, Contact, Contact.contactID>(cache, row, select, ids.OfType<object>(),
				appendants: new[]
				{
					new PXDataFieldAssign<Contact.grammValidationDateTime>(new DateTime(1900, 1, 1))
				},
				additionalCachesToCheckUpdatedEntitiesById: new[]
				{
					Base.Caches[typeof(CRLead)],
				});
		}

		public virtual void UpdateAddress<TSelect>(PXCache cache, object row, TSelect select, params object[] pars)
			where TSelect : PXSelectBase<Address>
		{
			var ids = select
				.Select(pars)
				.Select(l => l.GetItem<Address>().AddressID)
				.ToList();
			if (ids.Count == 0)
				return;

			Update<TSelect, Address, Address.addressID>(cache, row, select, ids.OfType<object>());
		}

		protected virtual void Update<TSelect, TTable, TIdField>(PXCache cache, object row, TSelect select,
			IEnumerable<object> ids, IEnumerable<PXDataFieldParam> appendants = null, IEnumerable<PXCache> additionalCachesToCheckUpdatedEntitiesById = null)
			where TIdField : IBqlField
		{
			var fieldParams = cache.GetFields_ContactInfo()
				.Select(f => new PXDataFieldAssign(f, cache.GetValue(row, f)))
				.OfType<PXDataFieldParam>();

			if (appendants != null)
			{
				fieldParams = fieldParams.Concat(appendants);
			}

			var additionalCaches = (additionalCachesToCheckUpdatedEntitiesById ?? Enumerable.Empty<PXCache>())
				.Prepend(cache.Graph.Caches[typeof(TTable)])
				.ToList();
			foreach (object id in ids)
			{
				bool changedObjectExists = false;

				foreach (var relatedCache in additionalCaches)
				{
					if (changedObjectExists)
						break;

					// don't try to change items (address) that are changed directly in graph
					changedObjectExists = relatedCache
						.Cached
						.OfType<object>()
						.Where(o => relatedCache.GetStatus(o).IsNotIn(PXEntryStatus.Notchanged, PXEntryStatus.Held))
						.Select(o => relatedCache.GetValue<TIdField>(o))
						.Any(id.Equals);
				}

				if (changedObjectExists is false)
				{
					fieldParams = fieldParams.Append(new PXDataFieldRestrict<TIdField>(id) { OrOperator = true });
				}
			}

			var array = fieldParams.ToArray();

			// otherwise updates all items in table
			if (array.OfType<PXDataFieldRestrict>().Any())
				PXDatabase.Update(typeof(TTable), array);
		}

		#endregion
	}
}
