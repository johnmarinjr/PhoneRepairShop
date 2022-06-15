using PX.Common;
using PX.Data;
using System.Collections;
using System.Linq;

namespace PX.Objects.CR.Extensions
{
	public delegate IEnumerable ExecuteSelectDelegate(string viewName, object[] parameters, object[] searches, string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows);

	public static class GraphExtensionHelpers
	{
		public static Extension GetProcessingExtension<Extension>(this PXGraph processingGraph)
			where Extension : PXGraphExtension
		{
			var processingExtesion = processingGraph.FindImplementation<Extension>();
			if (processingExtesion == null)
				throw new PXException(Messages.ExtensionCannotBeFound, typeof(Extension).ToString(), processingGraph.GetType().ToString());

			return processingExtesion;
		}
	}

	[PXInternalUseOnly]
	public static class GraphHelpers
	{
		public static TGraph CloneGraphState<TGraph>(this TGraph graph)
			where TGraph : PXGraph, new()
		{
			var clone = graph.Clone();

			clone.IsContractBasedAPI = graph.IsContractBasedAPI;
			clone.IsCopyPasteContext = graph.IsCopyPasteContext;
			clone.IsExport = graph.IsExport;
			clone.IsImport = graph.IsImport;
			clone.IsMobile = graph.IsMobile;

			if (clone.IsImport && !PXContext.Session.IsSessionEnabled)
			{
				// manually copy all cache currents for proper import
				foreach (var (key, cache) in graph.Caches.ToList())
				{
					var cloneCache = clone.Caches[key];
					cloneCache.Current = cache.Current;

					foreach (var item in cache.Cached)
					{
						cloneCache.SetStatus(item, cache.GetStatus(item));
					}
				}
			}

			return clone;
		}
	}
}
