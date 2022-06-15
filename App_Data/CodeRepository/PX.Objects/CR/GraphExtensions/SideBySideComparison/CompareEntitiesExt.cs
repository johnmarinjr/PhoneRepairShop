using PX.Data;
using PX.Objects.CR.Extensions.Cache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data.BQL.Fluent;
using PX.Objects.CR.Extensions.SideBySideComparison.Link;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;
using PX.Objects.CR.Wizard;

namespace PX.Objects.CR.Extensions.SideBySideComparison
{
	/// <summary>
	/// The base extension that provides comparison of two sets of items.
	/// It shows differences line by line and allows you to select a record from the left or right set (<see cref="EntitiesContext"/>).
	/// </summary>
	/// <remarks>
	/// The items should be aligned with each other (see <see cref="EntitiesContext"/>).
	/// For instance, it could be use to compare set of <see cref="CRLead"/> and related <see cref="CS.CSAnswers"/>.
	/// Compared entities not have to be of the same type,
	/// however it should be presented by same parent type and it must be <see cref="IBqlTable"/>.
	/// For instance, it is possible to compare <see cref="CRLead"/> with <see cref="Contact"/>,
	/// because <see cref="Contact"/> is a parent of <see cref="CRLead"/>.
	/// Use for link (see <see cref="LinkEntitiesExt{TGraph,TMain,TFilter}"/>)
	/// and for merge (see <see cref="MergeEntitiesExt{TGraph,TMain}"/>).
	/// </remarks>
	/// <typeparam name="TGraph">The entry <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TMain">The primary DAC (a <see cref="PX.Data.IBqlTable" /> type) of the <typeparam name="TGraph">graph</typeparam>.</typeparam>
	/// <typeparam name="TComparisonRow">The type of <see cref="ComparisonRow"/> that is used by the current extension.</typeparam>
	public abstract class CompareEntitiesExt<TGraph, TMain, TComparisonRow> : PXGraphExtension<TGraph>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, new()
		where TComparisonRow : ComparisonRow, new()
	{
		#region Views

		/// <summary>
		/// The prefix of generic views that is added by the current type of extension.
		/// </summary>
		/// <remarks>
		/// The property can be used to define hidden views in the base class for different child extensions.
		/// The resulting view name is a concatenation of <see cref="ViewPrefix"/> and the <see cref="PXSelectBase"/>'s property name.
		/// The property is used to built views for the following selects: <see cref="ComparisonRows"/>, <see cref="VisibleComparisonRows"/>.
		/// </remarks>
		/// <example>
		/// If ViewPrefix is "Prefix_", the following views are created:
		/// "Prefix_ComparisonRows" for <see cref="ComparisonRows"/>
		/// and "Prefix_VisibleComparisonRows" for <see cref="VisibleComparisonRows"/>.
		/// </example>
		protected abstract string ViewPrefix { get; }

		/// <summary>
		/// The view that represents all field comparisons (<see cref="ComparisonRows"/>).
		/// </summary>
		public PXSelectBase<TComparisonRow> ComparisonRows { get; protected set; }
		public IEnumerable comparisonRows()
		{
			return ComparisonRows.Cache.Cached;
		}

		/// <summary>
		/// The view that represents all visible field comparisons (<see cref="ComparisonRows"/>).
		/// </summary>
		public PXSelectBase<TComparisonRow> VisibleComparisonRows { get; protected set; }
		public IEnumerable visibleComparisonRows()
		{
			return ComparisonRows.SelectMain().Where(row => row.Hidden != true);
		}

		#endregion

		#region ctor

		/// <summary>
		/// The display name for the column for <see cref="ComparisonRow.LeftValue"/>.
		/// </summary>
		/// <remarks>
		/// The string that is provided here should be localized, but the class itself doesn't collect strings.
		/// Therefore, the string should be put inside a Messages class that is marked with <see cref="PXLocalizableAttribute"/>.
		/// </remarks>
		public virtual string LeftValueDescription => "Left record";

		/// <summary>
		/// The display name for the column for <see cref="ComparisonRow.RightValue"/>.
		/// </summary>
		/// <remarks>
		/// The string that is provided here should be localized, but the class itself doesn't collect strings.
		/// Therefore, the string should be put inside a Messages class that is marked with <see cref="PXLocalizableAttribute"/>.
		/// </remarks>
		public virtual string RightValueDescription => "Right record";

		public override void Initialize()
		{
			base.Initialize();

			Base.RowSelected.AddHandler(Base.PrimaryItemType, PrimaryRowSelected);

			ComparisonRows = Base.GetOrCreateSelectFromView<PXSelectBase<TComparisonRow>>(
				ViewPrefix + nameof(ComparisonRows),
				() => new SelectFrom<TComparisonRow>
				.OrderBy<ComparisonRow.order.Asc>
				.View(Base, new PXSelectDelegate(comparisonRows)));

			VisibleComparisonRows = Base.GetOrCreateSelectFromView<PXSelectBase<TComparisonRow>>(
				ViewPrefix + nameof(VisibleComparisonRows),
				() => new SelectFrom<TComparisonRow>
				.Where<ComparisonRow.hidden.IsEqual<True>>
				.OrderBy<ComparisonRow.order.Asc>
				.View(Base, new PXSelectDelegate(visibleComparisonRows)));
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes all <see cref="ComparisonRow"/>s and stores them in cache
		/// so that they can be viewed in the UI.
		/// </summary>
		public virtual void InitComparisonRowsViews()
		{
			StoreComparisonsInCache(GetPreparedComparisons());
		}

		/// <summary>
		/// Returns all <see cref="ComparisonRow"/>s and prepares them for displaying in the UI.
		/// </summary>
		/// <returns>
		/// The list of comparison rows.
		/// </returns>
		public virtual IEnumerable<TComparisonRow> GetPreparedComparisons()
		{
			return PrepareComparisons(GetComparisons());
		}

		/// <summary>
		/// Returns the context for the left set of items.
		/// </summary>
		/// <returns>
		/// Entities context.
		/// </returns>
		public abstract EntitiesContext GetLeftEntitiesContext();

		/// <summary>
		/// Returns the context for the right set of items.
		/// </summary>
		/// <returns>
		/// Entities context.
		/// </returns>
		public abstract EntitiesContext GetRightEntitiesContext();

		/// <summary>
		/// Returns the list of <see cref="ComparisonRow"/>s.
		/// </summary>
		/// <remarks>
		/// The returned list of rows can be filtered or updated by <see cref="PrepareComparisons"/>.
		/// </remarks>
		/// <returns>The list or comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> GetComparisons()
		{
			var leftContext = GetLeftEntitiesContext();
			var rightContext = GetRightEntitiesContext();
			int order = 0;
			foreach (var itemType in leftContext.Tables.Intersect(rightContext.Tables))
			{
				var leftEntry = leftContext[itemType];
				var rightEntry = rightContext[itemType];

				foreach (string fieldName in GetFieldsForComparison(itemType, leftEntry.Cache, rightEntry.Cache))
				{
					foreach (var (leftItem, rightItem) in MapEntries(leftEntry, rightEntry, leftContext, rightContext))
					{
						if (!TryGetStateExt(leftEntry.Cache, leftItem, fieldName, out var leftState)
							|| !TryGetStateExt(rightEntry.Cache, rightItem, fieldName, out var rightState))
							continue;

						string leftValue = GetStringValue(leftEntry.Cache, leftItem, fieldName, leftState);
						string rightValue = GetStringValue(rightEntry.Cache, rightItem, fieldName, rightState);

						if (string.IsNullOrWhiteSpace(leftValue) && string.IsNullOrWhiteSpace(rightValue))
							continue;

						if (leftValue == rightValue)
							continue;

						var comparisonRow = CreateComparisonRow(
							fieldName,
							itemType,
							ref order,
							(leftEntry.Cache, leftItem, leftValue, leftState),
							(rightEntry.Cache, rightItem, rightValue, rightState)
						);

						if (comparisonRow == null || (comparisonRow.LeftValue == comparisonRow.RightValue))
							continue;

						yield return comparisonRow;
					}
				}
			}
		}

		/// <summary>
		/// Prepares the list of <see cref="ComparisonRow"/>s (obtained by <see cref="GetComparisons"/>).
		/// </summary>
		/// <remarks>
		/// Calls the following methods for the incoming list:
		/// <see cref="FilterComparisons"/>, <see cref="UpdateComparisons"/>,
		/// <see cref="SortComparisons"/>, and <see cref="ClearUpComparisons"/>.
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The list of comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> PrepareComparisons(IEnumerable<TComparisonRow> comparisons)
		{
			comparisons = FilterComparisons(comparisons);
			comparisons = UpdateComparisons(comparisons);
			comparisons = SortComparisons(comparisons);
			comparisons = ClearUpComparisons(comparisons);
			return comparisons;
		}

		/// <summary>
		/// Filters the <see cref="ComparisonRow"/>s.
		/// </summary>
		/// <remarks>
		/// By default, the method excludes all invisible fields
		/// (for which <see cref="PXFieldState.Visible"/> is <see langword="false"/> for <see cref="ComparisonRow.LeftFieldState"/>).
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The list of comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> FilterComparisons(IEnumerable<TComparisonRow> comparisons)
		{
			foreach (var comparison in comparisons)
			{
				if (!comparison.LeftFieldState.Visible
					|| !comparison.RightFieldState.Visible)
					continue;
				yield return comparison;
			}
		}

		/// <summary>
		/// Updates the <see cref="ComparisonRow"/>s.
		/// </summary>
		/// <remarks>
		/// The method can update fields of comparison rows.
		/// By default, the method does nothing.
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The list of comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> UpdateComparisons(IEnumerable<TComparisonRow> comparisons)
		{
			return comparisons;
		}

		/// <summary>
		/// Sorts the <see cref="ComparisonRow"/>s.
		/// </summary>
		/// <remarks>
		/// The method can sort fields of comparison rows.
		/// You could change the order of rows here.
		/// You should also set <see cref="ComparisonRow.Order"/> inside this method to show rows in the proper order in the UI.
		/// By default, the method does nothing.
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The list of comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> SortComparisons(IEnumerable<TComparisonRow> comparisons)
		{
			return comparisons;
		}

		/// <summary>
		/// Clears up the <see cref="ComparisonRow"/>s from intermediate fields, which are serialized between requests.
		/// </summary>
		/// <remarks>
		/// By default, the method resets <see cref="ComparisonRow.LeftCache"/> and <see cref="ComparisonRow.RightCache"/> from caches.
		/// This method should be called to avoid performance issues with multiple cache serialization.
		/// If you override this method, you should call the base method implementation.
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The list of comparison rows.</returns>
		public virtual IEnumerable<TComparisonRow> ClearUpComparisons(IEnumerable<TComparisonRow> comparisons)
		{
			foreach (var comparison in comparisons)
			{
				comparison.LeftCache = null;
				comparison.RightCache = null;

				yield return comparison;
			}
		}

		/// <summary>
		/// Stores comparison rows (obtained by <see cref="PrepareComparisons"/>) in <see cref="PXCache"/>.
		/// It is required to show comparison rows in the UI.
		/// </summary>
		/// <remarks>
		/// The <see cref="ComparisonRows"/> and <see cref="VisibleComparisonRows"/> views rely on cached comparison rows.
		/// </remarks>
		/// <param name="result">The list of comparison rows.</param>
		public virtual void StoreComparisonsInCache(IEnumerable<TComparisonRow> result)
		{
			using (new ReadOnlyScope(ComparisonRows.Cache))
			{
				ComparisonRows.Cache.Clear();
				foreach (var row in result)
				{
					ComparisonRows.Cache.Hold(row);
				}
			}
		}

		/// <summary>
		/// Reprepares comparisons (see <see cref="PrepareComparisons"/>) that are already stored in the cache (see <see cref="StoreComparisonsInCache"/>).
		/// </summary>
		public virtual void ReprepareComparisonsInCache()
		{
			StoreComparisonsInCache(PrepareComparisons(ComparisonRows.SelectMain()));
		}

		/// <summary>
		/// Processes (see <see cref="ProcessComparisons"/>) all <see cref="ComparisonRows">comparison rows</see>
		/// and clears <see cref="PXCache"/>.
		/// </summary>
		/// <returns>The resulting contexts for the left and right sets of entities.</returns>
		public virtual (EntitiesContext LeftContext, EntitiesContext RightContext)
			ProcessComparisonResult()
		{
			var comparisons = ComparisonRows.SelectMain();

			var result = ProcessComparisons(comparisons);

			ComparisonRows.Cache.Clear();

			return result;
		}

		/// <summary>
		/// Processes the specified comparison rows.
		/// </summary>
		/// <remarks>
		/// The processing is defined in <see cref="UpdateLeftEntitiesContext"/> and <see cref="UpdateRightEntitiesContext"/>.
		/// </remarks>
		/// <param name="comparisons">The list of comparison rows.</param>
		/// <returns>The resulting contexts for the left and right sets of entities.</returns>
		public virtual (EntitiesContext LeftContext, EntitiesContext RightContext)
			ProcessComparisons(IReadOnlyCollection<TComparisonRow> comparisons)
		{
			var leftContext = GetLeftEntitiesContext();
			var rightContext = GetRightEntitiesContext();

			UpdateLeftEntitiesContext(leftContext, comparisons);
			UpdateRightEntitiesContext(rightContext, comparisons);

			return (leftContext, rightContext);
		}

		/// <summary>
		/// Updates the context for the left entity set according to the changed values in the comparison rows.
		/// </summary>
		/// <param name="context">The left entity context.</param>
		/// <param name="result">The list of comparison rows.</param>
		public virtual void UpdateLeftEntitiesContext(EntitiesContext context, IEnumerable<TComparisonRow> result)
		{
			UpdateEntitiesContext(context, result, ComparisonSelection.Left);
		}

		/// <summary>
		/// Updates the context for the right entity set according to the changed values in the comparison rows.
		/// </summary>
		/// <param name="context">The right entity context.</param>
		/// <param name="result">The list of comparison rows.</param>
		public virtual void UpdateRightEntitiesContext(EntitiesContext context, IEnumerable<TComparisonRow> result)
		{
			UpdateEntitiesContext(context, result, ComparisonSelection.Right);
		}

		/// <summary>
		/// Updates the context for the left or right (depending on <paramref name="sideToUpdate"/>) entity set according to the changed values in the comparison rows.
		/// </summary>
		/// <param name="context">The right or left entity context.</param>
		/// <param name="result">The list of comparison rows.</param>
		/// <param name="sideToUpdate">Specifies which entity set (left or right) the <paramref name="context"/> parameter represents.</param>
		public virtual void UpdateEntitiesContext(EntitiesContext context, IEnumerable<TComparisonRow> result, ComparisonSelection sideToUpdate)
		{
			foreach (var comparison in result)
			{
				string value = comparison.Selection == ComparisonSelection.None
					? null
					: comparison.Selection == ComparisonSelection.Right
						? comparison.RightValue
						: comparison.LeftValue;

				if (comparison.Selection != sideToUpdate)
				{
					var entry = context[comparison.ItemType];
					var item = entry
						.Items
						.FirstOrDefault(i => entry.Cache.GetObjectHashCode(i)
							== (sideToUpdate == ComparisonSelection.Right
								? comparison.RightHashCode
								: comparison.LeftHashCode));

					SetStringValue(entry.Cache, item, comparison.FieldName, value);
				}
			}
			using (context.PreserveCurrentsScope())
			{
				foreach (var table in context.Tables)
				{
					var entry = context[table];
					foreach (var item in entry.Items)
					{
						entry.Cache.Update(item);
					}
				}
			}
		}

		/// <summary>
		/// Tries to get <see cref="PXFieldState"/> for the <paramref name="fieldName"/> field.
		/// </summary>
		/// <param name="cache">The cache for the current entity.</param>
		/// <param name="item">The entity.</param>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="state">The resulting field state.</param>
		/// <returns>Returns <see langword="true"/> if <see cref="PXCache.GetStateExt"/> returns <see cref="PXFieldState"/>.
		/// Returns <see langword="false"/> if <see cref="PXCache.GetStateExt"/> doesn't return <see cref="PXFieldState"/>.</returns>
		public bool TryGetStateExt(PXCache cache, IBqlTable item, string fieldName, out PXFieldState state)
		{
			if (cache.GetStateExt(item, fieldName) is PXFieldState state_)
			{
				state = state_;
				return true;
			}

			state = null;
			return false;
		}

		/// <summary>
		/// Returns the fields of the entity presented in either the left cache or the right cache.
		/// </summary>
		/// <param name="itemType">The type of the entity.</param>
		/// <param name="leftCache">The cache for the left entity.</param>
		/// <param name="rightCache">The cache for the right entity.</param>
		/// <returns></returns>
		public virtual IEnumerable<string> GetFieldsForComparison(Type itemType, PXCache leftCache, PXCache rightCache)
		{
			return leftCache.GetFields_MassMergable().Union(rightCache.GetFields_MassMergable());
		}

		/// <summary>
		/// Sets the value represented with a string to the field of the entity.
		/// </summary>
		/// <param name="cache">The cache for the entity.</param>
		/// <param name="item">The entity.</param>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="stringValue">The value in the string representation.</param>
		public virtual void SetStringValue(PXCache cache, IBqlTable item, string fieldName, string stringValue)
		{
			cache.SetValue(item, fieldName, cache.ValueFromString(fieldName, stringValue));
		}

		/// <summary>
		/// Gets the value represented with a string from the field of the entity.
		/// </summary>
		/// <param name="cache">The cache for the entity.</param>
		/// <param name="item">The entity.</param>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="state">The field state from which the value should be obtained.</param>
		/// <returns>The string representation of the value.</returns>
		public virtual string GetStringValue(PXCache cache, IBqlTable item, string fieldName, PXFieldState state)
		{
			return cache.ValueToString(fieldName, cache.GetValue(item, fieldName));
		}

		/// <summary>
		/// Maps the left and right entries to each other.
		/// </summary>
		/// <remarks>
		/// By default, the method just zips all items from <paramref name="leftEntry"/> and <paramref name="rightEntry"/>.
		/// </remarks>
		/// <param name="leftEntry">The left entry of items.</param>
		/// <param name="rightEntry">The right entry of items.</param>
		/// <param name="leftContext">The left entity context.</param>
		/// <param name="rightContext">The right entity context.</param>
		/// <returns></returns>
		public virtual IEnumerable<(IBqlTable LeftItem, IBqlTable RightItem)> MapEntries(EntityEntry leftEntry, EntityEntry rightEntry, EntitiesContext leftContext, EntitiesContext rightContext)
		{
			return leftEntry.Items.Zip(rightEntry.Items, (left, right) => (left, right));
		}

		/// <summary>
		/// Creates a comparison row.
		/// </summary>
		/// <param name="fieldName">The name of the field.</param>
		/// <param name="itemType">The type of the entity.</param>
		/// <param name="order">The order of the resulting row, which is incremented inside the method.</param>
		/// <param name="left">The set of objects that represents the left field value.</param>
		/// <param name="right">The set of objects that represents the right field value.</param>
		/// <returns>The comparison row.</returns>
		public virtual TComparisonRow CreateComparisonRow(
			string fieldName,
			Type itemType,
			ref int order,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) left,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) right)
		{
			var comparison = new TComparisonRow
			{
				ItemType = itemType.FullName,
				FieldName = fieldName,
				LeftHashCode = left.Cache.GetObjectHashCode(left.Item),
				RightHashCode = right.Cache.GetObjectHashCode(right.Item),
				LeftValue = left.Value,
				RightValue = right.Value,
				FieldDisplayName = left.State.DisplayName,
				Selection = ComparisonSelection.Left,
				Order = order++,

				LeftCache = left.Cache,
				LeftFieldState = left.State,
				RightCache = right.Cache,
				RightFieldState = right.State,
			};

			TryAddSelectorDescription(left.Cache, left.Item, left.State, comparison, nameof(ComparisonRow.LeftValue_description));
			TryAddSelectorDescription(right.Cache, right.Item, right.State, comparison, nameof(ComparisonRow.RightValue_description));

			return comparison;
		}

		/// <summary>
		/// Adds a display name for a field if the field is a selector (that is, has <see cref="PXSelectorAttribute"/> assigned).
		/// </summary>
		/// <param name="cache">The cache for the entity.</param>
		/// <param name="item">The entity.</param>
		/// <param name="state">The field state.</param>
		/// <param name="comparison">The comparison row.</param>
		/// <param name="descriptionFieldName">
		/// The name of the description field:
		/// <see cref="ComparisonRow.LeftValue_description"/> or <see cref="ComparisonRow.RightValue_description"/>.
		/// </param>
		public virtual void TryAddSelectorDescription(PXCache cache, IBqlTable item, PXFieldState state, TComparisonRow comparison, string descriptionFieldName)
		{
			if (state.DescriptionName != null
				&& state.ViewName != null
				&& state.Value != null)
			{
				ComparisonRows.Cache.SetValue(comparison, descriptionFieldName,
					PXSelectorAttribute.GetField(cache, item, state.Name, cache.GetValue(item, state.Name), state.DescriptionName));
			}
		}

		#endregion

		#region Events

		protected virtual void PrimaryRowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			ComparisonRows.AllowDelete
				= ComparisonRows.AllowInsert
				= VisibleComparisonRows.AllowDelete
				= VisibleComparisonRows.AllowInsert
					= false;

			ComparisonRows.AllowSelect
				= VisibleComparisonRows.AllowSelect
					= ComparisonRows.Cache.Cached
						.Cast<TComparisonRow>()
						.Any(row => row.Hidden != true);

			// have to be in Primary selecting because LoadOnDemand renders description instantly
			Base.Caches[typeof(TComparisonRow)]
				.AdjustUIReadonly()
				.For<ComparisonRow.leftValue>(a => a.DisplayName = LeftValueDescription)
				.For<ComparisonRow.rightValue>(a => a.DisplayName = RightValueDescription);
		}

		protected virtual void _(Events.FieldSelecting<TComparisonRow, ComparisonRow.leftValue> e)
		{
			if (e.Row != null)
			{
				e.ReturnState = PXFieldState.CreateInstance(
					e.Row.LeftFieldState,
					e.Row.LeftFieldState.DataType,
					enabled: false);
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldSelecting<TComparisonRow, ComparisonRow.rightValue> e)
		{
			if (e.Row != null)
			{
				e.ReturnState = PXFieldState.CreateInstance(
					e.Row.RightFieldState,
					e.Row.RightFieldState.DataType,
					enabled: false);
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.RowPersisting<TComparisonRow> e)
		{
			e.Cancel = true;
		}

		#endregion
	}
}
