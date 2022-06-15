using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;

namespace PX.Objects.Common.Utility
{
	public class PXResultMapper
	{
		private readonly Dictionary<Type, Type> fieldMap;
		private readonly Type[] resultDefinition;
		private readonly PXGraph graph;



		public PXResultMapper(PXGraph graph, Dictionary<Type, Type> fieldMap, params Type[] resultDefinition)
		{
			this.fieldMap = fieldMap;
			this.resultDefinition = resultDefinition;
			this.graph = graph;
			this.ExtFilters = new HashSet<Type>();
			this.SuppressSorts = new HashSet<Type>();
		}
		public readonly HashSet<Type> ExtFilters;
		public readonly HashSet<Type> SuppressSorts;
		public string[] SortColumns => MapSort(PXView.SortColumns);
		public object[] Searches => MapSort<object>(PXView.SortColumns, PXView.Searches);
		public bool[] Descendings => MapSort<bool>(PXView.SortColumns, PXView.Descendings);

		public PXDelegateResult CreateDelegateResult(bool restricted = true)
		{
			PXDelegateResult list = new PXDelegateResult();
			if (this.MappedFilters.Count() != PXView.Filters.Count())
				restricted = false;

			list.IsResultFiltered = restricted;
			list.IsResultSorted = restricted;
			list.IsResultTruncated = restricted;
			return list;
		}
		public List<object> Select(PXView view, params object[] prms)
		{
			int startRow = PXView.StartRow;
			int totalRows = 0;
			return view.Select(
				PXView.Currents,
				prms,
				this.Searches,
				this.SortColumns,
				this.Descendings,
				this.Filters,
				ref startRow,
				PXView.MaximumRows,
				ref totalRows);
		}
		protected TType[] MapSort<TType>(string[] source, TType[] extParams)
		{
			var result = new List<TType>();
			for (int i = 0; i < source.Length; i++)
			{
				Type bqlField = GetBqlField(source[i]);
				if(bqlField != null && (SuppressSorts.Contains(bqlField) || SuppressSorts.Contains(bqlField.DeclaringType)))
				{
					continue;
				}				
				result.Add(extParams[i]);				
			}
			return result.ToArray();
		}
		protected string[] MapSort(string[] source)
		{
			var result = new List<string>();
			for (int i = 0; i < source.Length; i++)
			{
				Type bqlField = GetBqlField(source[i]);
				if (bqlField != null && (SuppressSorts.Contains(bqlField) || SuppressSorts.Contains(bqlField.DeclaringType)))
				{
					continue;
				}				
				result.Add(MapField(source[i]));				
			}
			return result.ToArray();
		}
		protected IEnumerable<PXFilterRow> MappedFilters
		{
			get
			{
				var list = new List<PXFilterRow>();
				bool containsExt = false;
				bool containsOr = false;
				foreach (PXFilterRow fr in PXView.Filters)
				{
					PXFilterRow map = (PXFilterRow)fr.Clone();
					Type bqlField = GetBqlField(map.DataField);
					if (bqlField != null)
					{
						if (ExtFilters.Contains(bqlField) || ExtFilters.Contains(bqlField.DeclaringType))
						{
							containsExt = true;
							continue;
						}
						else
						{
							if (fieldMap.TryGetValue(bqlField, out var result))
							{
								if (ExtFilters.Contains(result) || ExtFilters.Contains(result.DeclaringType))
								{
									containsExt = true;
									continue;
								}
								else
								{
									map.DataField = result.DeclaringType.Name + "__" + result.Name;
								}
							}
						}
					}

					if (map.OrOperator)
						containsOr = true;
					list.Add(map);
				}
				return containsExt && containsOr ? new List<PXFilterRow>() : list;
			}
		}
		public PXView.PXFilterRowCollection Filters
		{
			get
			{				
				return new PXView.PXFilterRowCollection(MappedFilters.ToArray());
			}
		}

		public string[] MapFields(string[] source)
		{
			var result = new string[source.Length];
			for(int i=0; i<source.Length; i++)
			{
				result[i] = MapField(source[i]);
			}
			return result;
		}
		
		public string MapField(string source)
		{
			Type bqlField = GetBqlField(source);
			if (bqlField != null && fieldMap.TryGetValue(GetBqlField(source), out var result))
			{
				return result.DeclaringType.Name + "__" + result.Name;
			}
			return source;
		}
		
		private Type GetBqlField(string source)
		{
			int splitIndex = source.IndexOf("__", StringComparison.InvariantCultureIgnoreCase);
			Type type = this.resultDefinition[0];
			if (splitIndex != -1)
			{
				string sourceDac = source.Substring(0, splitIndex);
				source = source.Substring(splitIndex + 2);
				type = this.resultDefinition.FirstOrDefault(_ => _.Name == sourceDac);
				if (type == null) return null;
			}			
			PXCache cache = graph.Caches[type];
			Type bqlField = cache.GetBqlField(source);
			return bqlField;
		}

		public object CreateResult(PXResult source)
		{
			var result = new Dictionary<Type, object>();
			foreach(Type type in resultDefinition)
			{
				object resultItem = PXResult.Unwrap(source, type) ?? this.graph.Caches[type].CreateInstance();
				result.Add(type, resultItem);
			}
			foreach (Type field in fieldMap.Keys)
			{
				Type sourceType = fieldMap[field];
				Type dac = sourceType.DeclaringType;
				if (typeof(PXCacheExtension).IsAssignableFrom(dac))
				{
					dac = dac.BaseType.GetGenericArguments()[0];
				}
				PXCache cacheGet = graph.Caches[dac];

				Type destinationType = field.DeclaringType;
				if (typeof(PXCacheExtension).IsAssignableFrom(destinationType))
				{
					destinationType = field.DeclaringType.BaseType.GetGenericArguments()[0];
				}
				PXCache cacheSet = graph.Caches[destinationType];
				cacheSet.SetValue(result[destinationType], field.Name, cacheGet.GetValue(PXResult.Unwrap(source, dac), sourceType.Name));
			}

			if (resultDefinition.Length ==1 )
				return result[resultDefinition[0]];
			else
			{
				Type generic = GetResultType();
				object[] data = new object[resultDefinition.Length];
				for (int i = 0; i < resultDefinition.Length; i++)
				{
					data[i] = result[resultDefinition[i]];
				}
				return Activator.CreateInstance(generic.MakeGenericType(resultDefinition), data);
			}
		}

		private Type GetResultType()
		{
			switch (resultDefinition.Length)
			{
				case 1:
					return  typeof(PXResult<>);
				case 2:
					return  typeof(PXResult<,>);
				case 3:
					return  typeof(PXResult<,,>);
				case 4:
					return  typeof(PXResult<,,,>);
				case 5:
					return  typeof(PXResult<,,,,>);
				case 6:
					return  typeof(PXResult<,,,,,>);
				case 7:
					return  typeof(PXResult<,,,,,,>);
				case 8:
					return  typeof(PXResult<,,,,,,,>);
				case 9:
					return  typeof(PXResult<,,,,,,,,>);
				case 10:
					return  typeof(PXResult<,,,,,,,,,>);
				case 11:
					return  typeof(PXResult<,,,,,,,,,,>);
				case 12:
					return  typeof(PXResult<,,,,,,,,,,,>);
				case 13:
					return  typeof(PXResult<,,,,,,,,,,,,>);
				case 14:
					return  typeof(PXResult<,,,,,,,,,,,,,>);
				case 15:
					return  typeof(PXResult<,,,,,,,,,,,,,,>);
				case 16:
					return  typeof(PXResult<,,,,,,,,,,,,,,,>);
				case 17:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,>);
				case 18:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,>);
				case 19:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,>);
				case 20:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,>);
				case 21:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,,>);
				case 22:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,,,>);
				case 23:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,,,,>);
				case 24:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,,,,,>);
				case 25:
					return  typeof(PXResult<,,,,,,,,,,,,,,,,,,,,,,,,>);				
				default: return null;
			}
		}

	}
}
