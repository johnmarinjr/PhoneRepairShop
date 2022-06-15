using System;
using System.Collections.Generic;
using System.Linq;
using PX.CS;
using PX.Data;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.CS;

namespace PX.Objects.CR.Extensions.SideBySideComparison.Merge
{
	/// <summary>
	/// The extension that can be used to merge two sets of entities after performing comparision of their fields
	/// and selecting values from left or right entity sets.
	/// </summary>
	/// <typeparam name="TGraph">The entry <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TMain">The primary DAC (a <see cref="PX.Data.IBqlTable" /> type) of the <typeparamref name="TGraph">graph</typeparamref>.</typeparam>
	public abstract class MergeEntitiesExt<TGraph, TMain> : CompareEntitiesExt<TGraph, TMain, MergeComparisonRow>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, INotable, new()
	{
		#region Views

		protected override string ViewPrefix => "Merge_";
		public CRValidationFilter<MergeEntitiesFilter> Filter { get; protected set; }

		#endregion

		#region Ctor

		public override string LeftValueDescription => "Current Record";
		public override string RightValueDescription => "Duplicate Record";

		public override void Initialize()
		{
			base.Initialize();

			Base.RowSelected.AddHandler(Base.PrimaryItemType, PrimaryRowSelected);

			Filter = Base.GetOrCreateSelectFromView<CRValidationFilter<MergeEntitiesFilter>>(
				ViewPrefix + nameof(Filter));
		}

		#endregion

		#region Methods

		/// <summary>
		/// Performs the update of the related to target and duplicate entities
		/// that are included neither in the left <see cref="EntitiesContext"/> nor in the right <see cref="EntitiesContext"/>.
		/// </summary>
		/// <param name="targetEntity">The target entity.</param>
		/// <param name="duplicateEntity">The duplicate entity.</param>
		public abstract void MergeRelatedDocuments(TMain targetEntity, TMain duplicateEntity);

		public override IEnumerable<MergeComparisonRow> UpdateComparisons(IEnumerable<MergeComparisonRow> comparisons)
		{
			foreach (var comparison in base.UpdateComparisons(comparisons))
			{
				comparison.Selection = string.IsNullOrEmpty(comparison.RightValue) is false
					&& ( Filter.Current.TargetRecord is MergeEntitiesFilter.targetRecord.SelectedRecord
						|| string.IsNullOrEmpty(comparison.LeftValue))
					? ComparisonSelection.Right
					: ComparisonSelection.Left;

				yield return comparison;
			}
		}

		/// <summary>
		/// Shows the smart panel on which a user can select fields that will remain in the target record after merge.
		/// </summary>
		/// <returns>
		/// The web dialog result for smart panel of <see cref="Filter"/>.
		/// If the smart panel validation failed it returns <see cref="WebDialogResult.None"/>.
		/// </returns>
		/// <param name="mergeEntityID">The ID of the entity with which the current entity should be merged.</param>
		/// <exception cref="PXDialogRequiredException">Is raised to render the smart panel for user interaction.</exception>
		public virtual WebDialogResult AskMerge(object mergeEntityID)
		{
			WebDialogResult result;
			if (Filter.View.Answer != default)
			{
				result = Filter.View.Answer;
			}
			else
			{
				Filter.Cache.Clear();
				Filter.Current = CreateNewFilter(mergeEntityID);

				result = Filter.AskExt(null,
					(graph, view) =>
					{
						InitComparisonRowsViews();
					});
			}

			if (result.IsPositive() && Filter.TryValidate() is false)
			{
				return WebDialogResult.None;
			}

			return result;
		}

		public virtual MergeEntitiesFilter CreateNewFilter(object mergeEntityID)
		{
			return Filter.Insert(
				new MergeEntitiesFilter
				{
					MergeEntityID = mergeEntityID.ToString(),
				});
		}

		/// <summary>
		/// Processes all comparison rows (see <see cref="ProcessComparisons"/>)
		/// and merges related documents (see <see cref="MergeRelatedDocuments"/>).
		/// </summary>
		/// <returns>The resulting contexts for the target and duplicate sets of entities.</returns>
		public virtual (TMain Target, TMain Duplicate) ProcessMerge()
		{
			var (leftContext, rightContext) = ProcessComparisonResult();

			var (targetContext, duplicateContext) = DefineTargetAndDuplicateContexts(leftContext, rightContext);

			TMain target = targetContext.MainEntry.Single<TMain>();
			TMain duplicate = duplicateContext.MainEntry.Single<TMain>();

			MergeRelatedDocuments(target, duplicate);

			return (target, duplicate);
		}

		public override (EntitiesContext LeftContext, EntitiesContext RightContext)
			ProcessComparisons(IReadOnlyCollection<MergeComparisonRow> comparisons)
		{
			var leftContext = GetLeftEntitiesContext();
			var rightContext = GetRightEntitiesContext();

			if(Filter.Current.TargetRecord == MergeEntitiesFilter.targetRecord.CurrentRecord)
				UpdateLeftEntitiesContext(leftContext, comparisons);
			else
				UpdateRightEntitiesContext(rightContext, comparisons);

			return (leftContext, rightContext);
		}

		public override IEnumerable<string> GetFieldsForComparison(Type itemType, PXCache leftCache, PXCache rightCache)
		{
			if (itemType == typeof(CSAnswers))
				return new[] { GetAttributeField() };

			return base.GetFieldsForComparison(itemType, leftCache, rightCache)
				.Concat(GetUdfFieldsForComparison(itemType, leftCache, rightCache));
		}

		public virtual IEnumerable<string> GetUdfFieldsForComparison(Type itemType, PXCache leftCache, PXCache rightCache)
		{
			return leftCache.GetFields_Udf().Union(rightCache.GetFields_Udf());
		}

		public virtual string GetAttributeField() => nameof(CSAnswers.Value);

		public override string GetStringValue(PXCache cache, IBqlTable item, string fieldName, PXFieldState state)
		{
			if (fieldName.StartsWith(KeyValueHelper.AttributePrefix))
				return state.Value?.ToString();

			return base.GetStringValue(cache, item, fieldName, state);
		}

		public override void SetStringValue(PXCache cache, IBqlTable item, string fieldName, string stringValue)
		{
			if (cache.GetItemType() == typeof(CSAnswers))
			{
				if (nameof(CSAnswers.value).Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))
					cache.SetValue<CSAnswers.value>(item, stringValue);
			}
			else if (fieldName.StartsWith(KeyValueHelper.AttributePrefix))
			{
				cache.SetValueExt(item, fieldName, cache.ValueFromString(fieldName, stringValue));
			}
			else
			{
				base.SetStringValue(cache, item, fieldName, stringValue);
			}
		}

		public override MergeComparisonRow CreateComparisonRow(
			string fieldName, Type itemType, ref int order,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) left,
			(PXCache Cache, IBqlTable Item, string Value, PXFieldState State) right)
		{
			var item = base.CreateComparisonRow(fieldName, itemType, ref order, left, right);
			item.EnableNoneSelection = true;
			if (left.Cache.GetItemType() == typeof(CSAnswers))
			{
				var notnullEntry = left.Item is CSAnswers ? left : right;
				item.FieldDisplayName = left.Cache.GetValueExt(notnullEntry.Item, "AttributeID_description") as string;
			}
			return item;
		}

		public override IEnumerable<(IBqlTable LeftItem, IBqlTable RightItem)> MapEntries(EntityEntry leftEntry, EntityEntry rightEntry, EntitiesContext leftContext, EntitiesContext rightContext)
		{
			if (leftEntry.EntityType == typeof(CSAnswers))
			{
				var leftEntries = leftEntry.Multiple<CSAnswers>();
				var rightEntries = rightEntry.Multiple<CSAnswers>();
				var attributeIDs =
					leftEntries.Select(e => e.AttributeID.ToUpper())
					.Union(rightEntries.Select(e => e.AttributeID.ToUpper()))
					.Distinct();
				var result = new List<(IBqlTable, IBqlTable)>();
				foreach (var attributeId in attributeIDs)
				{
					var leftAnswer  = leftEntries.Where(e => e.AttributeID == attributeId).FirstOrDefault();
					var rightAnswer = rightEntries.Where(e => e.AttributeID == attributeId).FirstOrDefault();
					result.Add((
						CoalesceCSAnswers(leftAnswer, rightAnswer, leftContext.MainEntry),
						CoalesceCSAnswers(rightAnswer, leftAnswer, rightContext.MainEntry)
					));
				}
				return result;
			}

			return base.MapEntries(leftEntry, rightEntry, leftContext, rightContext);
		}

		/// <summary>
		/// Returns an existing answer (<paramref name="leftAnswer"/>) or creates one that would be inserted in the database
		/// if <paramref name="leftAnswer"/> is <see langword="null"/>.
		/// </summary>
		/// <param name="leftAnswer">The answer to check.</param>
		/// <param name="rightAnswer">The answer whose values are copied if <paramref name="leftAnswer"/> is <see langword="null"/>.</param>
		/// <param name="mainEntry">The entry of a single item whose <see cref="INotable.NoteID"/>
		/// is used for the new answer if <paramref name="leftAnswer"/> is <see langword="null"/>.</param>
		/// <returns></returns>
		protected virtual CSAnswers CoalesceCSAnswers(CSAnswers leftAnswer, CSAnswers rightAnswer, EntityEntry mainEntry)
		{
			if (leftAnswer != null) return leftAnswer;
			var answer = new CSAnswers()
			{
				AttributeID = rightAnswer.AttributeID,
				AttributeCategory = rightAnswer.AttributeCategory,
				IsRequired = rightAnswer.IsRequired,
				RefNoteID = PXNoteAttribute.GetNoteID(mainEntry.Cache, mainEntry.Single<IBqlTable>(), null)
			};
			Base.Caches[typeof(CSAnswers)].Insert(answer);
			return answer;
		}

		public virtual (EntitiesContext target, EntitiesContext duplicate)
			DefineTargetAndDuplicateContexts(EntitiesContext leftContext, EntitiesContext rightContext)
		{
			return Filter.Current.TargetRecord == MergeEntitiesFilter.targetRecord.SelectedRecord
				? (rightContext, leftContext)
				: (leftContext, rightContext);
		}

		public override IEnumerable<MergeComparisonRow> FilterComparisons(IEnumerable<MergeComparisonRow> comparisons)
		{
			foreach (var comparison in comparisons)
			{
				if (!comparison.LeftFieldState.Visible
					&& !comparison.RightFieldState.Visible)
					continue;
				yield return comparison;
			}
		}

		#endregion

		#region Events

		protected virtual void _(Events.RowSelected<MergeEntitiesFilter> e)
		{
			e.Cache.AdjustUI(e.Row)
				.For<MergeEntitiesFilter.caption>(ui => ui.Visible = VisibleComparisonRows.Select().ToList().Any());
		}

		protected virtual void _(Events.FieldUpdated<MergeEntitiesFilter, MergeEntitiesFilter.targetRecord> e)
		{
			if (Equals(e.NewValue, e.OldValue) is false)
				ReprepareComparisonsInCache();
		}

		#endregion
	}
}
