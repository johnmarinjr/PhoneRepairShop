using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.CR.Extensions;
using PX.Objects.CR.Wizard;

namespace PX.Objects.CR.Extensions.SideBySideComparison.Link
{
	/// <summary>
	/// The extension that provides ability to link two sets of entities after performing comparison of their fields
	/// and selecting values from left or right entity sets.
	/// </summary>
	/// <typeparam name="TGraph">The entry <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TMain">The primary DAC (a <see cref="PX.Data.IBqlTable" /> type) of the <typeparam name="TGraph">graph</typeparam>.</typeparam>
	/// <typeparam name="TFilter">The type of <see cref="LinkFilter"/> that is used by the current extension.</typeparam>
	public abstract class LinkEntitiesExt<TGraph, TMain, TFilter> : CompareEntitiesExt<TGraph, TMain, LinkComparisonRow>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, new()
		where TFilter : LinkFilter, new()
	{
		#region Views

		protected override string ViewPrefix => "Link_";

		public PXSelectBase SelectEntityForLink { get; protected set; }
		public PXFilter<TFilter> Filter { get; protected set; }

		#endregion

		#region Initialize

		public override void Initialize()
		{
			base.Initialize();

			Filter = Base.GetOrCreateSelectFromView<PXFilter<TFilter>>(ViewPrefix + nameof(Filter));
			SelectEntityForLink = Base.GetOrCreateSelectFromView<PXSelectBase<TFilter>>(ViewPrefix + nameof(SelectEntityForLink));
		}

		#endregion

		#region Methods

		#region Asks

		/// <summary>
		/// Shows(and processes if called for the second time) the smart panel that shows entities to be linked.
		/// </summary>
		/// <remarks>
		/// After the method execution, the result is processed with <see cref="AskLinkWithEntityByID"/>.
		/// </remarks>
		/// <param name="entityID">Optional ID of the entity with which the current entity should be linked.</param>
		/// <exception cref="PXDialogRequiredException">Is raised to render the smart panel for user interaction.</exception>
		/// <exception cref="CRWizardBackException">Is raised when <see cref="WizardResult.Back"/> is clicked.</exception>
		/// <exception cref="CRWizardAbortException">Is raised when <see cref="WizardResult.Abort"/> is clicked.</exception>
		public virtual void SelectEntityForLinkAsk(object entityID = null)
		{
			var entityIDToLinkWith = entityID ?? GetSelectedEntityID();

			switch (AskSelectEntity())
			{
				case WebDialogResult.None:
					AskSelectEntity();
					return;

				case WebDialogResult.OK:
				case WebDialogResult.Yes:   // Next

					try
					{
						LinkAsk(entityIDToLinkWith);
					}
					catch (Exception ex)
					{
						if (ex is CRWizardBackException)
							goto case WebDialogResult.None;

						throw;
					}

					return;

				case WizardResult.Back:
					ClearAnswers();
					throw new CRWizardBackException();

				case WizardResult.Abort:
					ClearAnswers();
					throw new CRWizardAbortException();
			}
		}

		/// <summary>
		/// Shows (and processes if called for the second time) the smart panel
		/// on which a user selects the fields that remains in both records after linking.
		/// </summary>
		/// <remarks>
		/// After the method execution, the result is processed with <see cref="AskLinkWithEntityByID"/>.
		/// </remarks>
		/// <param name="entityID">Optional ID of the entity with which the current entity is linked.</param>
		/// <exception cref="PXDialogRequiredException">Is raised to render the smart panel for user interaction.</exception>
		/// <exception cref="CRWizardBackException">Is raised when <see cref="WizardResult.Back"/> is clicked.</exception>
		/// <exception cref="CRWizardAbortException">Is raised when <see cref="WizardResult.Abort"/> is clicked.</exception>
		public virtual void LinkAsk(object entityID = null)
		{
			object entityIDToLinkWith = entityID ?? GetSelectedEntityID();

			switch (AskLinkWithEntityByID(entityIDToLinkWith))
			{
				case WebDialogResult.OK:
				case WebDialogResult.Yes:   // Associate
					ProcessLink();
					break;

				case WizardResult.Back:
					ClearAnswers();
					throw new CRWizardBackException();

				case WizardResult.Abort:
					ClearAnswers();
					throw new CRWizardAbortException();
			}
		}

		#endregion

		public virtual void ProcessLink()
		{
			PreventRecursionCall.Execute(() =>
			{
				if (Filter.Current.ProcessLink is true)
				{
					ProcessComparisonResult();
				}

				UpdateMainAfterProcess();
			});

			ClearAnswers();
		}

		protected virtual void ClearAnswers()
		{
			if (SelectEntityForLink != null)
			{
				SelectEntityForLink.Cache.Current = null;
				SelectEntityForLink.View.Answer = WebDialogResult.None;
			}

			Filter.View.Answer = WebDialogResult.None;
		}

		protected abstract object GetSelectedEntityID();

		public override IEnumerable<LinkComparisonRow> UpdateComparisons(IEnumerable<LinkComparisonRow> comparisons)
		{
			foreach (var comparison in base.UpdateComparisons(comparisons))
			{
				if (!string.IsNullOrEmpty(comparison.RightValue))
				{
					comparison.Selection = ComparisonSelection.Right;
				}

				if (string.IsNullOrEmpty(comparison.LeftValue)
					|| comparison.RightFieldState.IsReadOnly
					|| !comparison.RightFieldState.Enabled)
				{
					comparison.Hidden = true;
				}

				yield return comparison;
			}
		}

		public override IEnumerable<string> GetFieldsForComparison(Type itemType, PXCache leftCache, PXCache rightCache)
		{
			return leftCache.GetFields_ContactInfo().Union(rightCache.GetFields_ContactInfo());
		}

		public virtual void UpdateMainAfterProcess() { }

		/// <summary>
		/// Shows the smart panel that displays entities to be linked.
		/// </summary>
		/// <exception cref="PXDialogRequiredException">Is raised to render the smart panel for user interaction.</exception>
		public virtual WebDialogResult AskSelectEntity()
		{
			return SelectEntityForLink?.View
				// no action is possible here for import, mobile or CB API
				?.WithAnswerForImport(WebDialogResult.Yes)
				?.WithAnswerForMobile(WebDialogResult.Yes)
				?.WithAnswerForCbApi(WebDialogResult.Yes)
				?.AskExt() ?? WebDialogResult.Yes;
		}

		/// <summary>
		/// Shows the smart panel on which a user selects fields that remains in both records after linking.
		/// </summary>
		/// <param name="entityID">The ID of the entity with which the current entity should be linked.</param>
		/// <exception cref="PXDialogRequiredException">Is raised to render the smart panel for user interaction.</exception>
		public virtual WebDialogResult AskLinkWithEntityByID(object entityID)
		{
			return Filter
				.WithActionIfNoAnswerFor(Base.IsImport || Base.IsMobile || Base.IsContractBasedAPI, () =>
				{
					Filter.Cache.Clear();
					Filter.Current = Filter.Insert();
					Filter.Current.LinkedEntityID = entityID.ToString();

					Filter.Current.ProcessLink = false;

					InitComparisonRowsViews();
				})
				.WithAnswerForImport(WebDialogResult.Yes)
				.WithAnswerForMobile(WebDialogResult.Yes)
				.WithAnswerForCbApi(WebDialogResult.Yes)
				.AskExt(null,
					(graph, view) =>
					{
						Filter.Cache.Clear();
						Filter.Current = Filter.Insert();
						Filter.Current.LinkedEntityID = entityID.ToString();

						InitComparisonRowsViews();
					});
		}

		#endregion

		#region Events

		protected virtual void _(Events.RowSelected<LinkComparisonRow> e)
		{
			e.Cache.AdjustUI(e.Row)
				.For<LinkComparisonRow.leftValueSelected>(ui => ui.Enabled = Filter.Current.ProcessLink is true)
				.SameFor<LinkComparisonRow.rightValueSelected>();
		}

		protected virtual void _(Events.RowSelected<TFilter> e)
		{
			e.Cache.AdjustUI(e.Row)
				.For<LinkFilter.caption>(ui => ui.Visible = VisibleComparisonRows.Select().ToList().Any());
		}

		#endregion
	}
}
