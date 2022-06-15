using PX.Data;
using PX.Objects.CR.Wizard;


namespace PX.Objects.CR.Extensions.SideBySideComparison.Link
{
	/// <summary>
	/// The event-based version of <see cref="LinkEntitiesExt{TGraph,TMain,TFilter}"/>, which is an extension
	/// that provides ability to link two sets of entities after performing comparision of their fields
	/// and selecting values from left or right entity sets.
	/// </summary>
	/// <remarks>
	/// Triggers opening of smart panel for linking of entities when <see cref="TUpdatingField"/> is updated in <see cref="TUpdatingEntity"/>
	/// and additional conditions are met.
	/// </remarks>
	/// <typeparam name="TGraph">The entry <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TMain">The primary DAC (a <see cref="PX.Data.IBqlTable" /> type) of the <typeparamref name="TGraph">graph</typeparamref>.</typeparam>
	/// <typeparam name="TFilter">The type of <see cref="LinkFilter"/> that is used by the current extension.</typeparam>
	public abstract class LinkEntitiesExt_EventBased<TGraph, TMain, TFilter, TUpdatingEntity, TUpdatingField> : LinkEntitiesExt<TGraph, TMain, TFilter>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, new()
		where TFilter : LinkFilter, new()
		where TUpdatingEntity : class, IBqlTable, INotable, new()
		where TUpdatingField : class, IBqlField
	{
		#region Initialization

		public virtual PXCache UpdatingEntityCache => Base.Caches[typeof(TUpdatingEntity)];
		public virtual TUpdatingEntity UpdatingEntityCurrent => UpdatingEntityCache.Current as TUpdatingEntity;
		public virtual string FieldName => UpdatingEntityCache.GetField(typeof(TUpdatingField));

		#endregion

		#region Methods

		public override void UpdateMainAfterProcess()
		{
			UpdatingEntityCache.SetValue<TUpdatingField>(
				UpdatingEntityCurrent,
				UpdatingEntityCache.ValueFromString(FieldName,
					Filter.Current.LinkedEntityID));
			UpdatingEntityCache.Update(UpdatingEntityCurrent);
		}

		protected virtual bool ShouldProcess(PXCache cache, TUpdatingEntity row, TUpdatingEntity oldRow)
		{
			var atomicAction = Base.IsImport || Base.IsMobile || Base.IsContractBasedAPI;

			return
				atomicAction && ValueChanged(cache, row, oldRow)
				|| !atomicAction && ((SelectEntityForLink?.View?.Answer ?? WebDialogResult.None) != WebDialogResult.None
									|| Filter.View.Answer != WebDialogResult.None
									|| ValueChanged(cache, row, oldRow));
		}

		protected virtual bool ValueChanged(PXCache cache, TUpdatingEntity row, TUpdatingEntity oldRow)
		{
			return
				cache.GetValue<TUpdatingField>(row) is object newValue
				&& !newValue.Equals(cache.GetValue<TUpdatingField>(oldRow));
		}

		#endregion

		#region Events

		protected virtual void _(Events.RowUpdated<TUpdatingEntity> e, PXRowUpdated del)
		{
			PreventRecursionCall.Execute(() =>
			{
				del?.Invoke(e.Cache, e.Args);

				if (e.Row == null
					|| !ShouldProcess(e.Cache, e.Row, e.OldRow)
					|| Base.UnattendedMode
					|| Base.IsCopyPasteContext)
					return;

				if (Filter.View.Answer == WizardResult.Abort)
				{
					e.Cache.SetValue<TUpdatingField>(e.Row, e.Cache.GetValue<TUpdatingField>(e.OldRow));

					throw new CRWizardAbortException();
				}

				try
				{
					LinkAsk(e.Cache.GetValue<TUpdatingField>(e.Row));
				}
				catch (CRWizardBackException)
				{
					// suppress
				}
			});
		}

		#endregion
	}
}
