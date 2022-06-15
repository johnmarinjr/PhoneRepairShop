using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.Extensions
{
	[PXInternalUseOnly]
	public static class ViewExtensions
	{
		/// <summary>
		/// The method that can be used to create a view from the specified select type or
		/// get an existing view with the specified name and create an instance of this select type in both cases as a result.
		/// </summary>
		/// <remarks>
		/// <typeparamref name="TSelect"/> must be non abstract or to be <see cref="PXSelectBase{Table}"/> or <see cref="PXSelectBase"/>.
		/// If the <see cref="PXSelectBase{Table}"/> provided as <typeparamref name="TSelect"/> than
		/// <see cref="PXSelect{Table}"/> would be returned. If it is non generic <see cref="PXSelectBase"/>
		/// than resulting <see cref="PXSelect{Table}"/> would be for the primary type of existing view,
		/// if view doesn't exist either, that it would be for the one lightweight DAC.
		/// In both cases if view doesn't exist it is considered as dummy and will return empty array (via view delegate).
		/// This method could be helpful if graph extension adds some hidden view that could (but not must) be overriden
		/// in multiple child extensions and all such views should be independent of each other.
		/// Such views could overriden by the common override views way:
		/// just add field of type <see cref="PXSelect{Table}"/> with name <paramref name="viewName"/> to the any graph extension for specified graph.
		/// </remarks>
		/// <typeparam name="TSelect">The desired select type, which
		/// must be non-abstract or be <see cref="PXSelectBase{Table}"/> or <see cref="PXSelectBase"/>.
		/// Also it must have a constructor with one parameter of the <see cref="PXGraph"/> type.</typeparam>
		/// <param name="graph">The graph.</param>
		/// <param name="viewName">The view name on which the desired <typeparamref name="TSelect"/> is based.
		/// If this view is not presented in <see cref="PXGraph.Views"/>, the new view with this name is created.</param>
		/// <param name="initializer">The initializer of the desired select.
		/// You can provide <see cref="PXSelectBase{Table}"/> as <typeparamref name="TSelect"/>
		/// and create a concrete select via initializer.</param>
		/// <returns>The desired select.</returns>
		/// <exception cref="PXException">Is raised if <typeparamref name="TSelect"/> is abstract and is not <see cref="PXSelectBase{Table}"/> or <see cref="PXSelectBase"/>
		/// or if the desired <typeparamref name="TSelect"/> cannot be instantiated for any reason.</exception>
		public static TSelect GetOrCreateSelectFromView<TSelect>(
			this PXGraph graph, string viewName, Func<PXSelectBase> initializer = null)
			where TSelect : PXSelectBase
		{
			try
			{
				if (graph.Views.TryGetValue(viewName, out var view))
				{
					return DefaultInitializer(view);
				}
				initializer ??= () => DefaultInitializer();
				var newSelect = initializer();
				graph.Views.Add(viewName, newSelect.View);
				if (newSelect is TSelect rightSelect)
					return rightSelect;

				return DefaultInitializer(newSelect.View);
			}
			catch (Exception ex)
			{
				throw new PXException(ex, MessagesNoPrefix.CannotInitializeSelectForView, typeof(TSelect).Name, viewName);
			}

			TSelect DefaultInitializer(PXView view = null)
			{
				var selectType = typeof(TSelect);
				bool suppressViewSelect = false;
				if (selectType.IsAbstract)
				{
					// allow initialize for base abstract PXSelectBase
					Type itemType;
					if (selectType.IsGenericType && selectType.GetGenericTypeDefinition() == typeof(PXSelectBase<>))
					{
						itemType = selectType.GenericTypeArguments[0];
					}
					else if (selectType == typeof(PXSelectBase))
					{
						// if we don't have view it is dummy select, so try to make it executable and fast
						// use CRSetup, as it won't be too expensive to fetch it from db "just in case..."
						itemType = view?.GetItemType() ?? typeof(CRSetup);
					}
					else
					{
						throw new PXArgumentException(nameof(TSelect),
							PXMessages.LocalizeFormatNoPrefixNLA(
								MessagesNoPrefix.CannotInitializeSelectForView_AbstractSelect,
								typeof(TSelect),
								viewName));
					}

					selectType = typeof(PXSelect<>).MakeGenericType(itemType);
					suppressViewSelect = view is null;
				}

				var select = (TSelect)Activator.CreateInstance(selectType, graph);
				if (suppressViewSelect)
				{
					view = new PXView(graph, true,
						select.View.BqlSelect, new PXSelectDelegate(Array.Empty<object>));
				}

				if (view != null)
					select.View = view;

				return select;
			}
		}
	}
}
