using CommonServiceLocator;
using PX.Data;
using PX.Objects.CM;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CurrencyInfo = PX.Objects.CM.Extensions.CurrencyInfo;
using CurrencyInfoAttribute = PX.Objects.CM.Extensions.CurrencyInfoAttribute;
using CuryMultDivType = PX.Objects.CM.Extensions.CuryMultDivType;

	namespace PX.Objects.Extensions.MultiCurrency
{
	/// <summary>The generic graph extension that defines the multi-currency functionality.</summary>
	/// <typeparam name="TGraph">A <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TPrimary">A DAC (a <see cref="PX.Data.IBqlTable" /> type).</typeparam>
	public abstract class MultiCurrencyGraph<TGraph, TPrimary> : PXGraphExtension<TGraph>, IPXCurrencyHelper, ICurrencyHelperEx, ICurrencyHost
			where TGraph : PXGraph
			where TPrimary : class, IBqlTable, new()
	{
		#region IPXCurrencyHelper implementation

		public CurrencyInfo GetDefaultCurrencyInfo() => GetCurrencyInfo(Documents.Current?.CuryInfoID);

		#endregion

		#region ICurrencyHost implementation

		public virtual bool IsTrackedType(Type dacType) => GetChildren().Union(GetTrackedExceptChildren()).Any(s => s.View.CacheGetItemType() == dacType);

		#endregion

		#region Mappings
		/// <summary>A class that defines the default mapping of the <see cref="Document" /> class to a DAC.</summary>
		protected class DocumentMapping : IBqlMapping
		{
			/// <exclude />
			public Type Extension => typeof(Document);
			/// <exclude />
			protected Type _table;
			/// <exclude />
			public Type Table => _table;

			/// <summary>Creates the default mapping of the <see cref="Document" /> mapped cache extension to the specified table.</summary>
			/// <param name="table">A DAC.</param>
			public DocumentMapping(Type table)
			{
				_table = table;
			}
			/// <exclude />
			public Type BAccountID = typeof(Document.bAccountID);
			/// <exclude />
			public Type BranchID = typeof(Document.branchID);
			/// <exclude />
			public Type CuryInfoID = typeof(Document.curyInfoID);
			/// <exclude />
			public Type CuryID = typeof(Document.curyID);
			/// <exclude />
			public Type DocumentDate = typeof(Document.documentDate);
		}

		/// <summary>A class that defines the default mapping of the <see cref="CurySource" /> mapped cache extension to a DAC.</summary>
		protected class CurySourceMapping : IBqlMapping
		{
			/// <exclude />
			public Type Extension => typeof(CurySource);
			/// <exclude />
			protected Type _table;
			/// <exclude />
			public Type Table => _table;

			/// <summary>Creates the default mapping of the <see cref="CurySource" /> mapped cache extension to the specified table.</summary>
			/// <param name="table">A DAC.</param>
			public CurySourceMapping(Type table)
			{
				_table = table;
			}
			/// <exclude />
			public Type CuryID = typeof(CurySource.curyID);
			/// <exclude />
			public Type CuryRateTypeID = typeof(CurySource.curyRateTypeID);
			/// <exclude />
			public Type AllowOverrideCury = typeof(CurySource.allowOverrideCury);
			/// <exclude />
			public Type AllowOverrideRate = typeof(CurySource.allowOverrideRate);
		}

		/// <summary>Returns the mapping of the <see cref="Document" /> mapped cache extension to a DAC. This method must be overridden in the implementation class of the base graph.</summary>
		/// <remarks>In the implementation graph for a particular graph, you  can either return the default mapping or override the default
		/// mapping in this method.</remarks>
		/// <example>
		///   <code title="Example" description="The following code shows the method that overrides the GetDocumentMapping() method in the implementation class. The  method overrides the default mapping of the %Document% mapped cache extension to a DAC: For the CROpportunity DAC, the DocumentDate field of the mapped cache extension is mapped to the closeDate field of the DAC; other fields of the extension are mapped by default." lang="CS">
		/// protected override DocumentMapping GetDocumentMapping()
		///  {
		///          return new DocumentMapping(typeof(CROpportunity)) {DocumentDate =  typeof(CROpportunity.closeDate)};
		///  }</code>
		/// </example>
		protected abstract DocumentMapping GetDocumentMapping();
		/// <summary>Returns the mapping of the <see cref="CurySource" /> mapped cache extension to a DAC. This method must be overridden in the implementation class of the base graph.</summary>
		/// <remarks>In the implementation graph for a particular graph, you can either return the default mapping or override the default mapping in this method.</remarks>
		/// <example>
		///   <code title="Example" description="The following code shows the method that overrides the GetCurySourceMapping() method in the implementation class. The method returns the defaul mapping of the %CurySource% mapped cache extension to the Customer DAC." lang="CS">
		/// protected override CurySourceMapping GetCurySourceMapping()
		///  {
		///      return new CurySourceMapping(typeof(Customer));
		///  }</code>
		/// </example>
		protected abstract CurySourceMapping GetCurySourceMapping();

		protected abstract PXSelectBase[] GetChildren();

		protected virtual PXSelectBase[] GetTrackedExceptChildren() => Array.Empty<PXSelectBase>();

		/// <summary>A mapping-based view of the <see cref="Document" /> data.</summary>
		public PXSelectExtension<Document> Documents;
		/// <summary>A mapping-based view of the <see cref="CurySource" /> data.</summary>
		public PXSelectExtension<CurySource> CurySource;

		/// <summary>Returns the current currency source.</summary>
		/// <returns>The default implementation returns the <see cref="CurySource" /> data view.</returns>
		/// <example>
		///   <code title="Example" description="The following code shows sample implementation of the method in the implementation class." lang="CS">
		/// public PXSelect&lt;CRSetup&gt; crCurrency;
		/// protected PXSelectExtension&lt;CurySource&gt; SourceSetup =&gt; new PXSelectExtension&lt;CurySource&gt;(crCurrency);
		///
		/// protected virtual CurySourceMapping GetSourceSetupMapping()
		/// {
		///       return new CurySourceMapping(typeof(CRSetup)) {CuryID = typeof(CRSetup.defaultCuryID), CuryRateTypeID = typeof(CRSetup.defaultRateTypeID)};
		///  }
		///
		/// protected override CurySource CurrentSourceSelect()
		/// {
		///        CurySource settings = base.CurrentSourceSelect();
		///        if (settings == null)
		///              return SourceSetup.Select();
		///        if (settings.CuryID == null || settings.CuryRateTypeID == null)
		///        {
		///              CurySource setup = SourceSetup.Select();
		///              settings = (CurySource)CurySource.Cache.CreateCopy(settings);
		///              settings.CuryID = settings.CuryID ?? setup.CuryID;
		///              settings.CuryRateTypeID = settings.CuryRateTypeID ?? setup.CuryRateTypeID;
		///        }
		///        return settings;
		/// }</code>
		/// </example>
		protected virtual CurySource CurrentSourceSelect()
		{
			return CurySource.Select();
		}
		#endregion

		#region Selects and Actions
		/// <summary>The current <see cref="CurrencyInfo" /> object of the document.</summary>
		public PXSelect<CurrencyInfo> currencyinfo;
		protected IEnumerable currencyInfo()
		{
			CurrencyInfo info = PXSelect<CurrencyInfo,
					Where<CurrencyInfo.curyInfoID, Equal<Current<Document.curyInfoID>>>>
					.Select(Base);
			if (info != null)
			{
				info.IsReadOnly = ShouldMainCurrencyInfoBeReadonly();
				yield return info;
			}
			yield break;
		}

		protected virtual bool ShouldMainCurrencyInfoBeReadonly()
		{
			return !Base.UnattendedMode && !Documents.AllowUpdate;
		}

		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>> currencyinfobykey;

		/// <summary>The <strong>Currency Toggle</strong> action.</summary>
		public PXAction<TPrimary> currencyView;
		[PXUIField(DisplayName = "Toggle Currency", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Money, Tooltip = CM.Messages.ToggleCurrencyViewTooltip, DisplayOnMainToolbar = false, CommitChanges = false)]
		protected virtual IEnumerable CurrencyView(PXAdapter adapter)
		{
			Base.Accessinfo.CuryViewState = !Base.Accessinfo.CuryViewState;
			PXCache cache = adapter.View.Cache;
			bool anyDiff = !cache.IsDirty;
			foreach (object ret in adapter.Get())
			{
				if (!anyDiff)
				{
					TPrimary item;
					if (ret is PXResult)
					{
						item = (TPrimary)((PXResult)ret)[0];
					}
					else
					{
						item = (TPrimary)ret;
					}
					if (item == null)
					{
						anyDiff = true;
				}
					else
					{
						TPrimary oldItem = _oldRow as TPrimary;
						if (item == null || oldItem == null)
						{
							anyDiff = true;
			}
						else
			{
							foreach (string field in cache.Fields)
							{
								object oldV = cache.GetValue(oldItem, field);
								object newV = cache.GetValue(item, field);
								if ((oldV != null || newV != null) && !object.Equals(oldV, newV) && (!(oldV is DateTime && newV is DateTime) || ((DateTime)oldV).Date != ((DateTime)newV).Date))
								{
									anyDiff = true;
			}
		}
		}
					}
				}
				yield return ret;
			}
			if (!anyDiff)
		{
				cache.IsDirty = false;
			}
		}
		#endregion

		#region Initialization

		protected List<PXSelectBase> ChildViews;
		protected Dictionary<Type, List<CuryField>> TrackedItems;

		public override void Initialize()
		{
			base.Initialize();

			ChildViews = new List<PXSelectBase>();
			TrackedItems = new Dictionary<Type, List<CuryField>>();
			Dictionary<Type, string> topCuryInfoIDs = new Dictionary<Type, string>();

			foreach (PXSelectBase s in GetChildren())
			{
				ChildViews.Add(s);

				Type itemType = s.View.CacheGetItemType();
				if (TrackedItems.ContainsKey(itemType))
					continue;

				List<CuryField> fields = new List<CuryField>();
				foreach (PXEventSubscriberAttribute attr in s.Cache.GetAttributesReadonly(null))
				{
					switch (attr)
					{
						case ICurrencyAttribute PXCurrencyAttr:
							fields.Add(new CuryField(PXCurrencyAttr));
							break;
						case CurrencyInfoAttribute CurrencyinfoAttr:
							if (CurrencyinfoAttr.IsTopLevel) topCuryInfoIDs[itemType] = attr.FieldName;
							break;
					}
				}

				TrackedItems.Add(itemType, fields);
			}

			foreach (PXSelectBase s in GetTrackedExceptChildren())
			{
				Type itemType = s.View.CacheGetItemType();
				if (TrackedItems.ContainsKey(itemType))
					continue;

				List<CuryField> fields = s.Cache.GetAttributesReadonly(null)
					.OfType<ICurrencyAttribute>()
					.Select(attr => new CuryField(attr))
					.ToList();
				TrackedItems.Add(itemType, fields);
			}

			foreach (KeyValuePair<Type, List<CuryField>> table in TrackedItems)
			{
				Base.RowInserting.AddHandler(table.Key,
					(s, e) => CuryRowInserting(s, e, table.Value, topCuryInfoIDs));
				Base.RowInserted.AddHandler(table.Key,
					(s, e) => CuryRowInserted(s, e, table.Value));
				Base.RowPersisting.AddHandler(table.Key,
					(s, e) => CuryRowPersisting(s, e, table.Value));
				foreach (CuryField field in table.Value)
				{
					Base.FieldUpdating.AddHandler(table.Key, field.BaseName,
						(s, e) => BaseFieldUpdating(s, e, field));
					Base.FieldUpdating.AddHandler(table.Key, field.CuryName,
						(s, e) => CuryFieldUpdating(s, e, field));
					Base.FieldVerifying.AddHandler(table.Key, field.CuryName,
						(s, e) => CuryFieldVerifying(s, e, field));
					Base.FieldSelecting.AddHandler(table.Key, field.CuryName,
						(s, e) => CuryFieldSelecting(s, e, field));
				}
			}
			if (Base.Views.Caches.Count == 0 || Base.Views.Caches[0] != typeof(CurrencyInfo))
			{
				int curyInfoIndex = Base.Views.Caches.IndexOf(typeof(CurrencyInfo));
				if (curyInfoIndex > 0)
				{
					Base.Views.Caches.RemoveAt(curyInfoIndex);
				}
				Base.Views.Caches.Insert(0, typeof(CurrencyInfo));
			}
		}

		#endregion

		#region Currency Fields Processing

		public void StoreResult(CurrencyInfo info)
		{
			this.currencyinfobykey.StoreResult(info);
		}

		public virtual CurrencyInfo GetCurrencyInfo(long? key)
		{
			if (key == null) return null;
			var info = object.Equals(currencyinfo.Current?.CuryInfoID, key.Value)
				? currencyinfo.Current
				: currencyinfo.Locate(new CurrencyInfo { CuryInfoID = key.Value })
				?? ReadCurrencyInfo(key.Value);

			if (info == null) return null;
			if (info.BasePrecision == null || info.CuryPrecision == null)
			{
				IPXCurrencyService service = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base);
				service.PopulatePrecision(currencyinfo.Cache, info);
			}
			return info;
		}

		private CurrencyInfo ReadCurrencyInfo(long key)
		{
			CurrencyInfo info = currencyinfobykey.SelectSingle(key);
			if (key > 0 && PXTransactionScope.IsConnectionOutOfScope)
			{
				//The FieldUpdating event is always executed in a separate connection
				//We may face with attempt to read non committed record in this case.
				//We have to clean QueryCache result in this case.
				currencyinfobykey.View.RemoveCached(new PXCommandKey(new object[] { key }, true));
			}
			return info;
		}

		private static bool FormatValue(PXFieldUpdatingEventArgs e, System.Globalization.CultureInfo culture)
		{
			if (e.NewValue is string)
			{
				if (decimal.TryParse((string)e.NewValue, System.Globalization.NumberStyles.Any, culture, out decimal val))
				{
					e.NewValue = val;
				}
				else
				{
					e.NewValue = null;
				}
			}
			return e.NewValue != null;
		}

		protected virtual void recalculateRowBaseValues(PXCache sender, object row, IEnumerable<CuryField> fields)
		{
			foreach (CuryField field in fields)
			{
				decimal? curyValue = (decimal?)sender.GetValue(row, field.CuryName);
				CurrencyInfo curyInfo = GetCurrencyInfo(sender, row, field.CuryInfoIDName);
				field.RecalculateFieldBaseValue(sender, row, curyValue, curyInfo);
			}
		}

		protected virtual void CuryRowInserting(PXCache sender, PXRowInsertingEventArgs e, List<CuryField> fields, Dictionary<Type, string> topCuryInfoIDs)
		{
			if (sender.GetItemType() != GetDocumentMapping().Table && topCuryInfoIDs.TryGetValue(sender.GetItemType(), out string curyInfoName))
			{
				CurrencyInfo info = currencyinfo.Insert(new CurrencyInfo());
				currencyinfo.Cache.IsDirty = false;
				if (info != null)
				{
					long? id = (long?)sender.GetValue(e.Row, curyInfoName);
					CurrencyInfo orig = id > 0L ? GetOriginalCurrencyInfo(id) : null;
					sender.SetValue(e.Row, curyInfoName, info.CuryInfoID);
					if (orig == null)
					{
						defaultCurrencyRate(currencyinfo.Cache, info, true, true);
					}
					else
					{
						id = info.CuryInfoID;
						currencyinfo.Cache.RestoreCopy(info, orig);
						// Acuminator disable once PX1048 RowChangesInEventHandlersAllowedForArgsOnly [Insert a new CuryInfo]
						info.CuryInfoID = id;
						currencyinfo.Cache.Remove(orig);
					}
					sender.SetValue(e.Row, nameof(CurrencyInfo.curyID), info.CuryID);
				}
			}
		}

		private CurrencyInfo GetOriginalCurrencyInfo(long? id)
		{
			if (id == null) return null;
			CurrencyInfo curyInfo = currencyinfobykey.Select(id);
			if (curyInfo == null) return null;
			else return (CurrencyInfo)currencyinfo.Cache.GetOriginal(curyInfo);
		}

		public virtual CurrencyInfo CloneCurrencyInfo(CurrencyInfo currencyInfo)
		{
			CurrencyInfo info_copy = PXCache<CurrencyInfo>.CreateCopy(currencyInfo);
			info_copy.CuryInfoID = null;
			return (CurrencyInfo)currencyinfo.Cache.Insert(info_copy);
		}

		public virtual CurrencyInfo CloneCurrencyInfo(CurrencyInfo currencyInfo, DateTime? currencyEffectiveDate)
		{
			return (CurrencyInfo)currencyinfo.Cache.Insert(new CurrencyInfo
			{
				ModuleCode = currencyInfo.ModuleCode,
				CuryRateTypeID = currencyInfo.CuryRateTypeID,
				CuryID = currencyInfo.CuryID,
				BaseCuryID = currencyInfo.BaseCuryID,
				CuryEffDate = currencyEffectiveDate
			});
		}

		protected virtual void CuryRowInserted(PXCache sender, PXRowInsertedEventArgs e, List<CuryField> fields)
		{
			recalculateRowBaseValues(sender, e.Row, fields);
		}

		protected virtual void CuryRowPersisting(PXCache sender, PXRowPersistingEventArgs e, List<CuryField> fields)
		{
			recalculateRowBaseValues(sender, e.Row, fields);
		}

		protected virtual void CuryFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e, CuryField curyField)
		{
			if (!FormatValue(e, sender.Graph.Culture)) return;

			CurrencyInfo info = GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);

			if (info != null)
			{
				e.NewValue = Math.Round((decimal)e.NewValue, curyField.CustomPrecision ?? info.CuryPrecision ?? 2, MidpointRounding.AwayFromZero);
			}
		}

		protected virtual void BaseFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e, CuryField curyField)
		{
			if (!FormatValue(e, sender.Graph.Culture)) return;

			CurrencyInfo info = GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);

			if (info != null)
			{
				e.NewValue = Math.Round((decimal)e.NewValue, curyField.CustomPrecision ?? info.BasePrecision ?? 2, MidpointRounding.AwayFromZero);
			}
		}

		protected virtual void CuryFieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, CuryField curyField)
		{
			CurrencyInfo curyInfo = GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);
			curyField.RecalculateFieldBaseValue(sender, e.Row, e.NewValue, curyInfo);
		}

		/// <summary>
		/// Try to obtain CurrencyInfo. This overload searches for CurrencyInfo which was already persisted
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="row"></param>
		/// <param name="curyInfoIDField"></param>
		/// <returns></returns>
		public CurrencyInfo GetCurrencyInfo(PXCache sender, object row, string curyInfoIDField)
		{
			long? key = GetCurrencyInfoID(sender, row, curyInfoIDField);

			CurrencyInfo info = GetCurrencyInfo(key);

			if (info == null && key != null && key.Value < 0L)
			{
				long? newkey = CurrencyInfoAttribute.GetPersistedCuryInfoID(sender, key);
				if (newkey != null && newkey.Value > 0L)
				{
					info = GetCurrencyInfo(newkey);
				}
			}

			return info;
		}

		private long? GetCurrencyInfoID(PXCache sender, object row, string curyInfoIDField)
		{
			return sender.GetValue(row, curyInfoIDField) as long? ?? Documents.Current?.CuryInfoID;
		}

		protected virtual void CuryFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e, CuryField curyField)
		{
			if (sender.Graph.Accessinfo.CuryViewState && !string.IsNullOrEmpty(curyField.BaseName))
			{
				e.ReturnValue = sender.GetValue(e.Row, curyField.BaseName);
				if (CM.PXCurrencyAttribute.IsNullOrEmpty(e.ReturnValue as decimal?))
				{
					object curyValue = sender.GetValue(e.Row, curyField.CuryName);
					CurrencyInfo curyInfo = GetCurrencyInfo(sender, e.Row, curyField.CuryInfoIDName);

					curyField.RecalculateFieldBaseValue(sender, e.Row, curyValue, curyInfo, true);
					e.ReturnValue = sender.GetValue(e.Row, curyField.BaseName);
				}

				if (e.IsAltered)
				{
					e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, enabled: false);
				}
			}
		}

		#endregion

		#region Document Events
		/// <summary>The FieldUpdated2 event handler for the <see cref="Document.BAccountID" /> field. When the BAccountID field value is changed, <see cref="Document.CuryID" /> is assigned the default
		/// value.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.FieldUpdated<Document, Document.bAccountID> e)
		{
			if (e.ExternalCall || e.Row?.CuryID == null || Base.IsWithinContext)
			{
				SourceFieldUpdated<Document.curyInfoID, Document.curyID, Document.documentDate>(e.Cache, e.Row);
			}
		}
		protected virtual void _(Events.FieldUpdated<Document, Document.branchID> e)
		{
			bool resetCuryID = (e.ExternalCall || e.Row?.CuryID == null);
			SourceFieldUpdated<Document.curyInfoID, Document.curyID, Document.documentDate>(e.Cache, e.Row, resetCuryID);
		}
		protected virtual void SourceFieldUpdated<CuryInfoID, CuryID, DocumentDate>(PXCache sender, IBqlTable row)
			where CuryInfoID : class, IBqlField
			where CuryID : class, IBqlField
			where DocumentDate : class, IBqlField
		{
			SourceFieldUpdated<CuryInfoID, CuryID, DocumentDate>(sender, row, true);
		}
		protected virtual void SourceFieldUpdated<CuryInfoID, CuryID, DocumentDate>(PXCache sender, IBqlTable row, bool resetCuryID)
			where CuryInfoID : class, IBqlField
			where CuryID : class, IBqlField
			where DocumentDate : class, IBqlField
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>() && row != null)
			{
				CurrencyInfo info = GetCurrencyInfo((long?)sender.GetValue<CuryInfoID>(row));
				if (info != null)
				{
					CurrencyInfo old = PXCache<CurrencyInfo>.CreateCopy(info);
					if (resetCuryID)
						currencyinfo.Cache.SetDefaultExt<CurrencyInfo.curyID>(info);
					else
					{
						currencyinfo.Cache.RaiseFieldDefaulting<CurrencyInfo.baseCuryID>(info, out object newBaseCuryID);
						if (object.Equals(info.BaseCuryID, newBaseCuryID))
							return;
					}

					currencyinfo.Cache.SetDefaultExt<CurrencyInfo.baseCuryID>(info);
					if (info.ModuleCode == null)
						currencyinfo.Cache.SetDefaultExt<CurrencyInfo.moduleCode>(info);
					currencyinfo.Cache.SetDefaultExt<CurrencyInfo.curyRateTypeID>(info);
					currencyinfo.Cache.SetDefaultExt<CurrencyInfo.curyEffDate>(info);

					if (info.CuryInfoID > 0
						&& (CurrencyCollection.IsBaseCuryInfo(old) ||
							CurrencyCollection.IsBaseCuryInfo(info)))
					{
						info = PXCache<CurrencyInfo>.CreateCopy(info);
						info.CuryInfoID = null;
						info = (CurrencyInfo)currencyinfo.Cache.Insert(info);
						sender.SetValueExt<CuryInfoID>(row, info.CuryInfoID);
						if (old.CuryID == old.BaseCuryID)
						{
							currencyinfo.Cache.Remove(old);
						}
						else
						{
							currencyinfo.Cache.SetStatus(old, PXEntryStatus.Deleted);
						}
					}
					else currencyinfo.Cache.MarkUpdated(info);
				
					currencyinfo.Cache.RaiseRowUpdated(info, old);
				}

				if (info != null)
				{
					sender.SetValue<CuryID>(row, info.CuryID);
				}
				string warning = PXUIFieldAttribute.GetError<CurrencyInfo.curyEffDate>(Base.Caches[typeof(CurrencyInfo)], info);
				if (!string.IsNullOrEmpty(warning))
				{
					sender.RaiseExceptionHandling<DocumentDate>(row,
						sender.GetValue<DocumentDate>(row),
						new PXSetPropertyException(warning, PXErrorLevel.Warning));
				}
			}
		}
		/// <summary>The FieldDefaulting2 event handler for the <see cref="Document.DocumentDate" /> field. When the DocumentDate field value is changed, <see cref="CurrencyInfo.curyEffDate"/> is changed to DocumentDate value.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.FieldUpdated<Document, Document.documentDate> e)
		{
			DateFieldUpdated<Document.curyInfoID, Document.documentDate>(e.Cache, e.Row);
		}
		protected virtual void DateFieldUpdated<CuryInfoID, DocumentDate>(PXCache sender, IBqlTable row)
			where CuryInfoID : class, IBqlField
			where DocumentDate : class, IBqlField
		{
			if (row != null)
			{
				CurrencyInfo info = GetCurrencyInfo((long?)sender.GetValue<CuryInfoID>(row));
				if (info != null)
				{
					CurrencyInfo copy = PXCache<CurrencyInfo>.CreateCopy(info);
					currencyinfo.SetValueExt<CurrencyInfo.curyEffDate>(info, sender.GetValue<DocumentDate>(row));
					string message = PXUIFieldAttribute.GetError<CurrencyInfo.curyEffDate>(currencyinfo.Cache, info);
					if (!string.IsNullOrEmpty(message))
					{
						sender.RaiseExceptionHandling<DocumentDate>(row, null, new PXSetPropertyException(message, PXErrorLevel.Warning));
					}
					currencyinfo.Cache.RaiseRowUpdated(info, copy);
					if (currencyinfo.Cache.GetStatus(info) != PXEntryStatus.Inserted)
					{
						currencyinfo.Cache.SetStatus(info, PXEntryStatus.Updated);
					}
				}
			}
		}

		protected virtual void _(Events.FieldSelecting<Document, Document.curyViewState> e)
		{
			e.ReturnValue = Base.Accessinfo.CuryViewState;
			e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, typeof(bool),
				isKey: false,
				nullable: false,
				required: -1,
				fieldName: nameof(e.Row.CuryViewState),
				errorLevel: PXErrorLevel.Undefined,
				enabled: false,
				visible: false,
				readOnly: true,
				visibility: PXUIVisibility.Visible);
		}

		protected virtual void _(Events.FieldSelecting<Document, Document.curyID> e)
		{
			e.ReturnValue = CuryIDFieldSelecting<Document.curyInfoID>(e.Cache, e.Row) ?? e.ReturnValue;
		}

		protected virtual string CuryIDFieldSelecting<CuryInfoID>(PXCache sender, object row)
			where CuryInfoID : class, IBqlField
		{
			CurrencyInfo info = GetCurrencyInfo((long?)sender.GetValue<CuryInfoID>(row));

			if (info == null) return null;
			else return Base.Accessinfo.CuryViewState
					? info.BaseCuryID
					: info.CuryID;
		}

		protected virtual void _(Events.FieldVerifying<Document, Document.curyID> e)
		{
			if (Base.Accessinfo.CuryViewState)
			{
				e.NewValue = e.Row?.CuryID;
				return;
			}
			CurrencyInfo info = GetCurrencyInfo(e.Row?.CuryInfoID);
			if (info == null || object.Equals(info.CuryID, e.NewValue)) return;

			CurrencyInfo old = PXCache<CurrencyInfo>.CreateCopy(info);
			if (old.CuryInfoID > 0 && (
					CM.CurrencyCollection.IsBaseCuryInfo(old) ||
					CM.CurrencyCollection.IsBaseCuryInfo(info, (string)e.NewValue)))
			{
				info = PXCache<CurrencyInfo>.CreateCopy(info);
				DefaultRateType(info);
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Insert a new CuryInfo]
				info.CuryInfoID = null;
				// Acuminator disable once PX1044 ChangesInPXCacheInEventHandlers [Insert a new CuryInfo]
				info = currencyinfo.Cache.Insert(info) as CurrencyInfo;
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Insert a new CuryInfo]
				e.Cache.SetValueExt<Document.curyInfoID>(e.Row, info.CuryInfoID);

				if (old.CuryID == old.BaseCuryID) currencyinfo.Cache.Remove(old);
				else currencyinfo.Cache.SetStatus(old, PXEntryStatus.Deleted);

				ValidateCurrencyInfo(info, e);
				currencyinfo.Cache.RaiseRowUpdated(info, old);
			}
			else
			{
				DefaultRateType(info);
				ValidateCurrencyInfo(info, e);
				currencyinfo.Cache.MarkUpdated(info);
				currencyinfo.Cache.RaiseRowUpdated(info, old);
			}
		}

		private void DefaultRateType(CurrencyInfo info)
		{
			if (info.CuryRateTypeID == null)
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Insert a new CuryInfo]
				currencyinfo.Cache.SetDefaultExt<CurrencyInfo.curyRateTypeID>(info);
		}

		private void ValidateCurrencyInfo(CurrencyInfo info, Events.FieldVerifying<Document, Document.curyID> e)
		{
			currencyinfo.SetValueExt<CurrencyInfo.curyID>(info, e.ExternalCall ? new PXCache.ExternalCallMarker(e.NewValue) : e.NewValue);
			// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Insert a new CuryInfo]
			currencyinfo.Cache.SetDefaultExt<CurrencyInfo.curyEffDate>(info);
			string message = PXUIFieldAttribute.GetError<CurrencyInfo.curyID>(currencyinfo.Cache, info);
			if (string.IsNullOrEmpty(message) == false)
			{
				e.Cache.RaiseExceptionHandling<Document.curyID>(e.Row, e.NewValue, new PXSetPropertyException(message, PXErrorLevel.Warning));
			}
		}

		protected virtual void _(Events.FieldSelecting<Document, Document.curyRate> e)
		{
			e.ReturnValue = CuryRateFieldSelecting<Document.curyInfoID>(e.Cache, e.Row);
		}

		protected virtual decimal? CuryRateFieldSelecting<CuryInfoID>(PXCache sender, object row)
			where CuryInfoID : class, IBqlField
		{
			decimal? returnValue = null;

			bool curyviewstate = Base.Accessinfo.CuryViewState;

			CurrencyInfo info = GetCurrencyInfo((long?)sender.GetValue<CuryInfoID>(row));

			if (info != null)
			{
				if (!curyviewstate)
				{
					returnValue = info.SampleCuryRate;
				}
				else
				{
					returnValue = 1m;
				}
			}

			return returnValue;
		}

		/// <summary>The RowSelected event handler for the <see cref="Document" /> DAC. The handler sets the value of the Enabled property of <see cref="Document.CuryID"/> according to the value of this property of <see cref="CurySource.AllowOverrideCury"/>.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.RowSelected<Document> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<Document.curyID>(e.Cache, e.Row, AllowOverrideCury());
			}
		}

		protected virtual bool AllowOverrideCury()
		{
			CurySource source = CurrentSourceSelect();
			return (source == null || source.AllowOverrideCury == true) && !Base.Accessinfo.CuryViewState;
		}

		object _oldRow;
		protected virtual void _(Events.RowUpdating<Document> e)
		{
			_oldRow = e.Row;
			DocumentRowUpdating<Document.curyInfoID, Document.curyID>(e.Cache, e.NewRow);
		}
		protected virtual void DocumentRowUpdating<CuryInfoID, CuryID>(PXCache sender, object row)
			where CuryInfoID : class, IBqlField
			where CuryID : class, IBqlField
		{
			long? key = (long?)sender.GetValue<CuryInfoID>(row);
			if (key != null && key.Value < 0L)
			{
				bool found = false;
				foreach (CurrencyInfo cached in currencyinfo.Cache.Inserted)
				{
					if (object.Equals(cached.CuryInfoID, key))
					{
						found = true;
						break;
					}
				}

				//when populatesavedvalues is called in ExecuteSelect we can sometimes endup here
				if (!found)
				{
					sender.SetValue<CuryInfoID>(row, null);
					key = null;
				}
			}

			if (key == null)
			{
				CurrencyInfo info = new CurrencyInfo();
				info = currencyinfo.Insert(info);
				currencyinfo.Cache.IsDirty = false;
				if (info != null)
				{
					sender.SetValue<CuryInfoID>(row, info.CuryInfoID);
					sender.SetValue<CuryID>(row, info.CuryID);
					defaultCurrencyRate(currencyinfo.Cache, info, true, true);
				}
			}
		}

		protected virtual void _(Events.RowInserting<Document> e)
		{
			DocumentRowInserting<Document.curyInfoID, Document.curyID>(e.Cache, e.Row);
		}

		protected virtual void DocumentRowInserting<CuryInfoID, CuryID>(PXCache sender, object row)
			where CuryInfoID : class, IBqlField
			where CuryID : class, IBqlField
		{
			long? id = (long?)sender.GetValue<CuryInfoID>(row);
			if (id < 0L) return;

			CurrencyInfo info = currencyinfo.Insert(new CurrencyInfo());
			currencyinfo.Cache.IsDirty = false;
			if (info != null)
			{
				sender.SetValue<CuryInfoID>(row, info.CuryInfoID);
				CurrencyInfo orig = GetOriginalCurrencyInfo(id);

				if (orig == null)
				{
					defaultCurrencyRate(currencyinfo.Cache, info, true, true);
				}
				else
				{
					id = info.CuryInfoID;
					currencyinfo.Cache.RestoreCopy(info, orig);
					info.CuryInfoID = id;
					currencyinfo.Cache.Remove(orig);
				}
				sender.SetValue<CuryID>(row, info.CuryID);
			}
		}
		#endregion

		#region CurrencyInfo Events
		/// <summary>The FieldDefaulting2 event handler for the <see cref="CurrencyInfo.CuryID" /> field. The CuryID field takes the current value of <see cref="CurySource.CuryID"/>.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyID> e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				CurySource source = CurrentSourceSelect();
				if (!string.IsNullOrEmpty(source?.CuryID))
				{
					e.NewValue = source.CuryID;
				}
				else
				{
					e.NewValue = GetBaseCurency();
				}
			}
			else
			{
				e.NewValue = GetBaseCurency();
			}
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.baseCuryID> e)
		{
			e.NewValue = GetBaseCurency();
			e.Cancel = true;
		}

		protected virtual string GetBaseCurency()
		{
			int? branchID = Documents.Current?.BranchID;
			if (branchID == null)
			{
				PXCache primaryCache = Base.Caches[typeof(TPrimary)];
				object primaryItem = Documents.Cache.GetMain(Documents.Current);
				string branchIDFieldName = GetDocumentMapping().BranchID.Name;

				primaryCache.RaiseFieldDefaulting(branchIDFieldName, primaryItem, out object defaultBranchID);
				branchID = (int?)defaultBranchID ?? Base.Accessinfo.BranchID;
			}

			return ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base).BaseCuryID(branchID);
		}

		/// <summary>The FieldDefaulting2 event handler for the <see cref="CurrencyInfo.CuryRateTypeID" /> field. The CuryRateTypeID field takes the current value of <see cref="CurySource.CuryRateTypeID"/>.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyRateTypeID> e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				CurySource source = CurrentSourceSelect();
				if (!string.IsNullOrEmpty(source?.CuryRateTypeID))
				{
					e.NewValue = source.CuryRateTypeID;
					e.Cancel = true;
				}
				else if (e.Row != null && !String.IsNullOrEmpty(e.Row.ModuleCode))
				{
					e.NewValue = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base).DefaultRateTypeID(e.Row.ModuleCode);
					e.Cancel = true;
				}
			}
		}

		/// <summary>
		/// Module, to set in CurrencyInfo
		/// </summary>
		protected abstract string Module { get; }

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.moduleCode> e)
		{
			e.NewValue = Module;
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.curyRateTypeID> e)
		{
			defaultEffectiveDate(e.Cache, e.Row);
			try
			{
				defaultCurrencyRate(e.Cache, e.Row, true, false);
			}
			catch (PXSetPropertyException ex)
			{
				if (e.ExternalCall)
				{
					e.Cache.RaiseExceptionHandling<CurrencyInfo.curyRateTypeID>(e.Row, e.Row.CuryRateTypeID, ex);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<CurrencyInfo, CurrencyInfo.curyRateTypeID> e)
		{
			if (!PXAccess.FeatureInstalled<CS.FeaturesSet.multicurrency>())
			{
				e.Cancel = true;
			}
		}

		/// <summary>The FieldDefaulting2 event handler for the <see cref="CurrencyInfo.CuryEffDate" /> field. The CuryEffDate field takes the current value of <see cref="Document.DocumentDate"/>.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyEffDate> e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				e.NewValue = Documents.Cache.Current != null && Documents.Current.DocumentDate != null
					? Documents.Current.DocumentDate
					: e.Cache.Graph.Accessinfo.BusinessDate;
			}
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.curyEffDate> e)
		{
			try
			{
				defaultCurrencyRate(e.Cache, e.Row, true, false);
			}
			catch (PXSetPropertyException ex)
			{
				e.Cache.RaiseExceptionHandling<CurrencyInfo.curyEffDate>(e.Row, e.Row.CuryEffDate, ex);
			}
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.curyID> e)
		{
			defaultEffectiveDate(e.Cache, e.Row);
			try
			{
				defaultCurrencyRate(e.Cache, e.Row, true, false);
			}
			catch (PXSetPropertyException ex)
			{
				e.Cache.RaiseExceptionHandling<CurrencyInfo.curyID>(e.Row, e.Row.CuryID, ex);
			}
			e.Row.CuryPrecision = null;
		}

		protected bool? currencyInfoDirty;

		protected virtual void _(Events.RowUpdating<CurrencyInfo> e)
		{
			if (e.Row.IsReadOnly == true)
			{
				e.Cancel = true;
			}
			else
			{
				currencyInfoDirty = e.Cache.IsDirty;
			}
		}

		protected virtual void _(Events.RowUpdated<CurrencyInfo> e)
		{
			if ((String.IsNullOrEmpty(e.Row.CuryID) || String.IsNullOrEmpty(e.Row.BaseCuryID)))
			{
				e.Row.BaseCuryID = e.OldRow.BaseCuryID;
				e.Row.CuryID = e.OldRow.CuryID;
			}
			if (currencyInfoDirty == false
				&& e.Row.CuryID == e.OldRow.CuryID
				&& e.Row.CuryRateTypeID == e.OldRow.CuryRateTypeID
				&& e.Row.CuryEffDate == e.OldRow.CuryEffDate
				&& e.Row.CuryMultDiv == e.OldRow.CuryMultDiv
				&& e.Row.CuryRate == e.OldRow.CuryRate)
			{
				e.Cache.IsDirty = false;
				currencyInfoDirty = null;
			}
			foreach (PXSelectBase childView in ChildViews)
			{
				Type itemType = childView.View.CacheGetItemType();
				foreach (var curyFields in TrackedItems[itemType].GroupBy(f => f.CuryInfoIDName))
				{
					if (itemType == GetDocumentMapping().Table)
					{
						long? docCuryInfoId = (long?)Documents.Cache.GetValue(Documents.Current?.Base, curyFields.Key);
						if (e.Row.CuryInfoID != docCuryInfoId)
							continue;

						recalculateRowBaseValues(Documents.Cache, Documents.Current?.Base, curyFields);
					}
					else
					{
						foreach (object result in childView.View.SelectMulti())
						{
							object item = result is PXResult ? ((PXResult)result)[0] : result;
							long? itemCuryInfoId = GetCurrencyInfoID(childView.Cache, item, curyFields.Key);
							if (e.Row.CuryInfoID != itemCuryInfoId)
								continue;

							recalculateRowBaseValues(childView.Cache, item, curyFields);
							childView.Cache.MarkUpdated(item);
						}
					}
				}
			}
		}
		
		/// <summary>The RowSelected event handler for the <see cref="CurrencyInfo" /> DAC. The handler sets the values of the Enabled property of the UI fields of <see cref="CurrencyInfo"/> according to the values of this property of the corresponding fields of <see cref="CurySource"/>.</summary>
		/// <param name="e">Parameters of the event.</param>
		protected virtual void _(Events.RowSelected<CurrencyInfo> e)
		{
			if (e.Row != null)
			{
				e.Row.DisplayCuryID = e.Row.CuryID;

				CurySource source = CurrentSourceSelect();
				bool curyenabled = AllowOverrideRate(e.Cache, e.Row, source);

				long? baseCuryInfoID = CurrencyCollection.MatchBaseCuryInfoId(e.Row);
				PXUIFieldAttribute.SetVisible<CurrencyInfo.curyRateTypeID>(e.Cache, e.Row, baseCuryInfoID == null);
				PXUIFieldAttribute.SetVisible<CurrencyInfo.curyEffDate>(e.Cache, e.Row, baseCuryInfoID == null);

				PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyMultDiv>(e.Cache, e.Row, curyenabled);
				PXUIFieldAttribute.SetEnabled<CurrencyInfo.baseCuryID>(e.Cache, e.Row, false);

				PXUIFieldAttribute.SetEnabled<CurrencyInfo.displayCuryID>(e.Cache, e.Row, false);
				PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyID>(e.Cache, e.Row, true);

				PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyRateTypeID>(e.Cache, e.Row, curyenabled);
				PXUIFieldAttribute.SetEnabled<CurrencyInfo.curyEffDate>(e.Cache, e.Row, curyenabled);
				PXUIFieldAttribute.SetEnabled<CurrencyInfo.sampleCuryRate>(e.Cache, e.Row, curyenabled);
				PXUIFieldAttribute.SetEnabled<CurrencyInfo.sampleRecipRate>(e.Cache, e.Row, curyenabled);
			}
		}

		protected virtual bool AllowOverrideRate(PXCache sender, CurrencyInfo info, CurySource source)
		{
			bool curyenabled = true;

			if (source != null && source.AllowOverrideRate != true
				|| info.IsReadOnly == true || (info.CuryID == info.BaseCuryID))
			{
				curyenabled = false;
			}

			return curyenabled;
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.curyPrecision> e)
		{
			e.NewValue = Convert.ToInt16(ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base)
						.CuryDecimalPlaces(e.Row.CuryID));
		}

		protected virtual void _(Events.FieldDefaulting<CurrencyInfo, CurrencyInfo.basePrecision> e)
		{
			e.NewValue = Convert.ToInt16(ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base)
						.CuryDecimalPlaces(e.Row.BaseCuryID));
		}

		protected virtual void defaultEffectiveDate(PXCache sender, CurrencyInfo info)
		{
			if (sender.RaiseFieldDefaulting<CurrencyInfo.curyEffDate>(info, out object newValue))
			{
				sender.RaiseFieldUpdating<CurrencyInfo.curyEffDate>(info, ref newValue);
			}
			info.CuryEffDate = (DateTime?)newValue;
		}

		protected virtual void defaultCurrencyRate(PXCache sender, CurrencyInfo info, bool forceDefault, bool suppressErrors)
		{
			if (info.IsReadOnly == true) return;

			IPXCurrencyRate rate = info.SearchForNewRate(Base);
			
			if (rate != null)
			{
				DateTime? UserCuryEffDate = info.CuryEffDate;
				rate.Populate(info);

				if (!suppressErrors && rate.CuryEffDate < UserCuryEffDate)
				{
					int days = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(Base).GetRateEffDays(info.CuryRateTypeID);
					if (days > 0 && ((TimeSpan)(UserCuryEffDate - rate.CuryEffDate)).Days >= days)
					{
						throw new CM.PXRateIsNotDefinedForThisDateException(info.CuryRateTypeID, rate.FromCuryID, rate.ToCuryID, (DateTime)UserCuryEffDate);
					}
				}
			}
			else if (forceDefault)
			{
				if (string.Equals(info.CuryID, info.BaseCuryID))
				{
					bool dirty = sender.IsDirty;
					CurrencyInfo dflt = new CurrencyInfo();
					sender.SetDefaultExt<CurrencyInfo.curyRate>(dflt);
					sender.SetDefaultExt<CurrencyInfo.curyMultDiv>(dflt);
					sender.SetDefaultExt<CurrencyInfo.recipRate>(dflt);
					info.CuryRate = Math.Round((decimal)dflt.CuryRate, 8);
					info.CuryMultDiv = dflt.CuryMultDiv;
					info.RecipRate = Math.Round((decimal)dflt.RecipRate, 8);
					sender.IsDirty = dirty;
				}
				else if (!suppressErrors)
				{
					info.CuryRate = null;
					info.RecipRate = null;
					info.CuryMultDiv = "M";

					if (info.CuryRateTypeID != null && info.CuryEffDate != null)
						throw new PXSetPropertyException(CM.Messages.RateNotFound, PXErrorLevel.Warning);
				}
			}
		}

		protected virtual bool checkRateVariance(PXCache sender, CurrencyInfo info)
		{
			CMSetup CMSetup = GetCMSetup();
			if (CMSetup?.RateVarianceWarn != true || CMSetup?.RateVariance == 0) return false;

			IPXCurrencyRate rate = info.SearchForNewRate(Base);

			if (rate?.CuryRate == null || rate?.CuryRate == 0) return false;
			else return Math.Abs(CalculateRateVariance(info, rate)) > CMSetup.RateVariance;
		}

		//TODO: probably, CMSetup can be moved from target screen graphs to this extension 
		private CMSetup GetCMSetup()
		{
			CMSetup CMSetup = (CMSetup)Base.Caches[typeof(CMSetup)].Current;
			if (CMSetup == null)
			{
				CMSetup = PXSelectReadonly<CMSetup>.Select(Base);
			}
			return CMSetup;
		}

		private static decimal CalculateRateVariance(CurrencyInfo info, IPXCurrencyRate rate)
		{
			decimal rateDifference = (decimal)info.CuryRate - (decimal)rate.CuryRate;
			if (rate.CuryMultDiv == info.CuryMultDiv || info.CuryRate == 0)
			{
				return 100 * rateDifference / (decimal)rate.CuryRate;
			}
			else
			{
				return 100 * (1 / rateDifference) / (decimal)rate.CuryRate;
			}
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.sampleRecipRate> e)
		{
			if (e.ExternalCall)
			{
				decimal rate = Math.Round((decimal)e.Row.SampleRecipRate, 8);
				if (rate == 0)
					rate = 1;
				e.Row.CuryRate = rate;
				e.Row.RecipRate = Math.Round((decimal)(1 / rate), 8);
				e.Row.CuryMultDiv = "D";
				if (checkRateVariance(e.Cache, e.Row))
				{
					PXUIFieldAttribute.SetWarning<CurrencyInfo.sampleRecipRate>(e.Cache, e.Row, CM.Messages.RateVarianceExceeded);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.baseCuryID> e)
		{
			e.Row.BasePrecision = null;
		}

		protected virtual void _(Events.FieldUpdated<CurrencyInfo, CurrencyInfo.sampleCuryRate> e)
		{
			if (!e.ExternalCall) return;

			decimal rate = Math.Round((decimal)e.Row.SampleCuryRate, 8);

			bool hasCurrencyRateDefaulted = false;

			if (rate == 0)
			{
				try
				{
					defaultCurrencyRate(e.Cache, e.Row, true, false);
					hasCurrencyRateDefaulted = true;
				}
				catch (PXSetPropertyException)
				{
					rate = 1;
				}
			}

			if (!hasCurrencyRateDefaulted)
			{
				e.Row.CuryRate = rate;
				e.Row.RecipRate = Math.Round(1m / rate, 8);
				e.Row.CuryMultDiv = CuryMultDivType.Mult;
			}

			if (checkRateVariance(e.Cache, e.Row))
			{
				PXUIFieldAttribute.SetWarning<CurrencyInfo.sampleCuryRate>(
					e.Cache,
					e.Row,
					CM.Messages.RateVarianceExceeded);
			}
		}

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public virtual object GetRow(PXCache sender, object row)
		{
			return row;
		}
		#endregion
	}
}
