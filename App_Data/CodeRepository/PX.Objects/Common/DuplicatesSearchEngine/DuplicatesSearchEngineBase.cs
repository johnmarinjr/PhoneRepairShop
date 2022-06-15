using PX.Common;
using PX.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.Common
{
	[PXInternalUseOnly]
	public abstract class DuplicatesSearchEngineBase<TEntity> where TEntity : class, IBqlTable, new()
	{
		protected readonly PXCache _cache;
		protected readonly Type[] _keyFields;
		protected readonly KeyValuesComparer<TEntity> _comparator;
		protected readonly TEntity _template;

		public DuplicatesSearchEngineBase(PXCache cache, IEnumerable<Type> keyFields)
		{
			_cache = cache;
			_keyFields = keyFields.ToArray();
			_comparator = new KeyValuesComparer<TEntity>(cache, keyFields);
			_template = (TEntity)_cache.CreateInstance();
		}

		public virtual TEntity CreateEntity(IDictionary itemValues, params Type[] additionalFields)
        {
			foreach (Type field in _keyFields.Concat_(additionalFields))
			{
				object value;

				var fieldName = _cache.GetField(field);
				if (itemValues.Contains(fieldName))
				{
					value = itemValues[fieldName];
				}
				else
				{
					_cache.RaiseFieldDefaulting(fieldName, _template, out value);
				}
				if (value != null && !_cache.RaiseFieldUpdating(fieldName, _template, ref value))
				{
					value = null;
				}
				_cache.SetValue(_template, field.Name, value);
			}
			return _template;
		}

		public virtual TEntity Find(IDictionary itemValues)
		{
			return Find(CreateEntity(itemValues));
		}

		public abstract TEntity Find(TEntity item);

		public abstract void AddItem(TEntity item);
	}
}
