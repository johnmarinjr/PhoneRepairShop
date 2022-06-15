using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.TX;
using PX.Objects.TX.Descriptor;
using PX.Objects.Common;

namespace PX.Objects.TX
{

	public class TaxReportMaint : PXGraph<TaxReportMaint>
	{	
		public const string TAG_TAXZONE = "<TAXZONE>";
		public static DateTime MAX_VALIDTO = new DateTime(9999,6,6);
		private readonly TaxReportLinesByTaxZonesReloader taxDetailsReloader;

		#region Cache Attached Events
		#region TaxBucket
		#region VendorID

		[PXDBInt(IsKey = true)]
        [PXDefault(typeof(TaxReport.vendorID))]        
        protected virtual void TaxBucket_VendorID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region BucketID
        [PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(TaxReport))]
		[PXParent(typeof(Select<TaxReport, Where<TaxReport.vendorID, Equal<Current<TaxBucket.vendorID>>>>), LeaveChildren = true)]
        [PXUIField(DisplayName = "Reporting Group", Visibility = PXUIVisibility.Visible)]
        protected virtual void TaxBucket_BucketID_CacheAttached(PXCache sender)
        {
        }
        #endregion       
        
        #endregion

		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(TaxReport.vendorID))]
		protected virtual void TaxReportLine_VendorID_CacheAttached(PXCache sender)
		{
		}

        #endregion

        public PXSave<TaxReport> Save;
		public PXCancel<TaxReport> Cancel;
		[PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXCancelButton]
		protected new virtual IEnumerable cancel(PXAdapter a)
		{
			TaxReport newReport = null;
			int? lastVendor = Report.Current?.VendorID;
			string searchVendorID = null;
			int? lastRevisionId = Report.Current?.RevisionID;
			int? searchRevisionID = null;
			#region Extract Keys
			if (a.Searches != null)
			{
				if (a.Searches.Length > 0 && a.Searches[0] != null)
					searchVendorID = a.Searches[0].ToString();
				if (a.Searches.Length > 1 && a.Searches[1] != null)
					searchRevisionID = int.Parse(a.Searches[1].ToString());
			}
			#endregion

			if (!string.IsNullOrEmpty(searchVendorID))
			{
				Vendor item = PXSelect<Vendor>.Search<Vendor.acctCD>(this, searchVendorID);
				searchVendorID = (item == null) ? null : item.AcctCD;
			}


			if (a.Searches != null && searchVendorID != null)
			{
				foreach (TaxReport e in (new PXCancel<TaxReport>(this, "Cancel")).Press(a))
				{
					Report.Cache.Clear();
					newReport = e;
					if (newReport.RevisionID != lastRevisionId && lastVendor == newReport.VendorID && searchRevisionID != null)
					{
						// switching revision manually on previous step
						if (PXSelectorAttribute.Select<TaxReport.revisionID>(Report.Cache, newReport, newReport.RevisionID) == null)
						{
							Report.Cache.RaiseExceptionHandling<TaxReport.revisionID>(newReport,
								newReport.RevisionID,
								new PXSetPropertyException(PX.Data.ErrorMessages.ValueDoesntExist,
									PXErrorLevel.Error,
									PXUIFieldAttribute.GetDisplayName<TaxReport.revisionID>(Report.Cache),
									newReport.RevisionID.ToString()));
						}
					}
					else
					{
						newReport = GetLastReportVersion(this, newReport.VendorID);
						if (newReport == null)
						{
							newReport = e;
							newReport.RevisionID = 1;
							newReport.ValidFrom = Accessinfo.BusinessDate;
							newReport.ValidTo = MAX_VALIDTO;
							newReport = (TaxReport)Report.Cache.Insert(newReport);
							Report.Cache.IsDirty = Report.Cache.Inserted.Count() > 0;
						}
					}
				}
			}

			yield return newReport;
		}

		public PXAction<TaxReport> Up;

		[PXUIField(DisplayName = ActionsMessages.RowUp, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowUp, Tooltip = ActionsMessages.ttipRowUp)]
		public virtual IEnumerable up(PXAdapter adapter)
		{
			ReportLine.ArrowUpForCurrentRow();
			return adapter.Get();
		}

		public PXAction<TaxReport> Down;


		[PXUIField(DisplayName = ActionsMessages.RowDown, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update, Enabled = true)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowDown, Tooltip = ActionsMessages.ttipRowDown)]		
		public virtual IEnumerable down(PXAdapter adapter)
		{
			ReportLine.ArrowDownForCurrentRow();
			return adapter.Get();
		}

		public PXSelect<TaxReport> Report;

		public TaxReportLinesOrderedSelect ReportLine;

		public PXSelect<TaxBucket, Where<TaxBucket.vendorID, Equal<Current<TaxReport.vendorID>>>> Bucket;

		public PXSelect <TaxBucketLine,
						Where<TaxBucketLine.vendorID, Equal<Required<TaxReport.vendorID>>,
								And<TaxBucketLine.taxReportRevisionID, Equal<Required<TaxReport.revisionID>>>>> TaxBucketLines;

		public PXSelect<TaxBucketLine, 
						Where<TaxBucketLine.vendorID, Equal<Required<TaxReportLine.vendorID>>, 
								And<TaxBucketLine.taxReportRevisionID, Equal<Required<TaxReportLine.taxReportRevisionID>>,
								And<TaxBucketLine.lineNbr, Equal<Required<TaxBucketLine.lineNbr>>>>>>
						TaxBucketLine_Vendor_LineNbr;

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		protected IEnumerable reportLine()
        {
			if (Report.Current?.VendorID == null)
				yield break;

            bool showTaxZones = Report.Current.ShowNoTemp == true;
			TaxBucketAnalizer analyzerTax = new TaxBucketAnalizer(this, Report.Current.VendorID.Value, TaxReportLineType.TaxAmount, (int)this.Report.Current.RevisionID);
			Dictionary<int, List<int>> taxBucketsDict = analyzerTax.AnalyzeBuckets(showTaxZones);
			TaxBucketAnalizer testAnalyzerTaxable = new TaxBucketAnalizer(this, (int)this.Report.Current.VendorID, TaxReportLineType.TaxableAmount, (int)this.Report.Current.RevisionID);
			Dictionary<int, List<int>> taxableBucketsDict = testAnalyzerTaxable.AnalyzeBuckets(showTaxZones);
            Dictionary<int, List<int>>[] bucketsArr = { taxBucketsDict, taxableBucketsDict };     

			Dictionary<int, TaxReportLine> taxReporLinesByLineNumber =
				PXSelect<TaxReportLine,
					Where<TaxReportLine.vendorID, Equal<Current<TaxReport.vendorID>>,
						And<TaxReportLine.taxReportRevisionID, Equal<Current<TaxReport.revisionID>>,
						And<
							Where2<
								Where<Current<TaxReport.showNoTemp>, Equal<False>,
								  And<TaxReportLine.tempLineNbr, IsNull>>,
								Or<
									Where<Current<TaxReport.showNoTemp>, Equal<True>, 
									And<
										Where<TaxReportLine.tempLineNbr, IsNull,
										And<TaxReportLine.tempLine, Equal<False>,
										Or<TaxReportLine.tempLineNbr, IsNotNull>>>>>>>>>>,
					OrderBy<
						Asc<TaxReportLine.sortOrder,
						Asc<TaxReportLine.taxZoneID>>>>
					.Select(this)
					.RowCast<TaxReportLine>()
					.ToDictionary(taxLine => taxLine.LineNbr.Value);

			foreach (TaxReportLine taxline in taxReporLinesByLineNumber.Values)
            {
				if (!showTaxZones)
				{
					foreach (Dictionary<int, List<int>> bucketsDict in bucketsArr.Where(dict => dict?.ContainsKey(taxline.LineNbr.Value) == true))
					{
						var calcRuleWithLineNumberReplacedBySortOrder =
							bucketsDict[taxline.LineNbr.Value].Where(lineNbr => taxReporLinesByLineNumber.ContainsKey(lineNbr))
															  .Select(lineNbr => taxReporLinesByLineNumber[lineNbr].SortOrder.Value)
															  .OrderBy(lineNbr => lineNbr);

						taxline.BucketSum = string.Join("+", calcRuleWithLineNumberReplacedBySortOrder);
					}
				}

                yield return taxline;
            }
        }

		public PXAction<TaxReport> viewGroupDetails;

		[PXUIField(DisplayName = Messages.ViewGroupDetails, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewGroupDetails(PXAdapter adapter)
		{
			//TO DO: delete Ask after platform team resolve
			{
				if (Report.Current != null && Bucket.Current != null)
				{
					if ((Report.Cache.Updated.Count() > 0 || Report.Cache.Inserted.Count() > 0)
						&& (Report.Ask(Messages.RedirectUnsavedDataNotification, MessageButtons.YesNo) == WebDialogResult.No))
					{
						return adapter.Get();
					}
					TaxBucketMaint graph = CreateInstance<TaxBucketMaint>();
					graph.Bucket.Current.VendorID = Report.Current.VendorID;
					graph.Bucket.Current.TaxReportRevisionID = Report.Current.RevisionID;
					graph.Bucket.Current.BucketID = Bucket.Current.BucketID;
					graph.Bucket.Current.BucketType = Bucket.Current.BucketType;

					throw new PXRedirectRequiredException(graph, Messages.ViewGroupDetails);
				}
			}
			return adapter.Get();
		}

		public PXAction<TaxReport> updateTaxZoneLines;

		[PXUIField(DisplayName = Messages.CreateReportLinesForNewTaxZones, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable UpdateTaxZoneLines(PXAdapter adapter)
		{
			taxDetailsReloader.ReloadTaxReportLinesForTaxZones();
			return adapter.Get();
		}

		public PXAction<TaxReport> createReportVersion;
		[PXUIField(DisplayName = Messages.CreateReportVerion, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(Category = ActionCategories.ReportManagement)]
		protected virtual IEnumerable CreateReportVersion(PXAdapter adapter)
		{
			if (Report.Current != null && Report.Current.VendorID != null)
			{
				TaxReport newRep = new TaxReport();
				newRep.VendorID = Report.Current.VendorID;
				newRep.RevisionID = GetLastReportVersion(this, newRep.VendorID).RevisionID + 1;
				newRep = (TaxReport)this.Caches<TaxReport>().Insert(newRep);

				Report.Cache.SetValueExt<TaxReport.validFrom>(newRep, Accessinfo.BusinessDate);
				newRep.ValidTo = TaxReportMaint.MAX_VALIDTO;
				newRep = (TaxReport)this.Caches<TaxReport>().Update(newRep);
			}
			yield return Report.Current;
		}

		public PXAction<TaxReport> copyReportVersion;
		[PXUIField(DisplayName = Messages.CopyReportVerion, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(Category = ActionCategories.ReportManagement)]
		protected virtual IEnumerable CopyReportVersion(PXAdapter adapter)
		{
			if (Report.Current != null && Report.Current.VendorID != null)
			{
				Report.Current.ShowNoTemp = false;
				PXResultset<TaxReportLine> baseReportLines = ReportLine.Select();
				PXResultset<TaxBucketLine> baseBuckets = TaxBucketLines.Select(Report.Current.VendorID, Report.Current.RevisionID);

				TaxReport cloneTaxReport = new TaxReport();
				cloneTaxReport.VendorID = Report.Current.VendorID;
				cloneTaxReport.RevisionID = GetLastReportVersion(this, cloneTaxReport.VendorID).RevisionID + 1;
				cloneTaxReport = (TaxReport)this.Caches<TaxReport>().Insert(cloneTaxReport);

				Report.Cache.SetValueExt<TaxReport.validFrom>(cloneTaxReport, Accessinfo.BusinessDate);
				cloneTaxReport.ValidTo = TaxReportMaint.MAX_VALIDTO;
				cloneTaxReport = (TaxReport)this.Caches<TaxReport>().Update(cloneTaxReport);

				/// keep ReportLine lineNbr's differences between report version and its copy,
				/// to avoid keys diplicate on copy from version with expired list of tax zones.
				/// Need to use in setting relevant values of new created BucketLines
				Dictionary<int, int> lineNbrComparer = new Dictionary<int, int>();
				foreach (TaxReportLine repLine in baseReportLines)
				{
					TaxReportLine copyLine = PXCache<TaxReportLine>.CreateCopy(repLine);
					copyLine.TempLine = false;
					copyLine.TaxReportRevisionID = cloneTaxReport.RevisionID;
					copyLine.LineNbr = null; // to avoid keys diplicate on copy from version with expired list of tax zones
					copyLine = (TaxReportLine) this.Caches<TaxReportLine>().Insert(copyLine);
					lineNbrComparer.Add((int)repLine.LineNbr, (int)copyLine.LineNbr);

					if (repLine.TempLine == true)
					{
						copyLine.TempLine = true;
						copyLine = (TaxReportLine)this.Caches<TaxReportLine>().Update(copyLine);
					}
				}

				foreach (TaxBucketLine bucketLine in baseBuckets)
				{
					if (lineNbrComparer.ContainsKey((int)bucketLine.LineNbr)) // copy only BucketLines matching template ReportLines
					{
						TaxBucketLine newBucketLine = PXCache<TaxBucketLine>.CreateCopy(bucketLine);
						newBucketLine.TaxReportRevisionID = cloneTaxReport.RevisionID;
						newBucketLine.LineNbr = lineNbrComparer[(int)bucketLine.LineNbr];
						newBucketLine = (TaxBucketLine)this.Caches<TaxBucketLine>().Insert(newBucketLine);

						// create child BucketLines for tax zones (for ReportLines with tempLineNbr != null)
						foreach (PXResult<TaxReportLine, TaxBucketLine> res in PXSelectJoin<TaxReportLine, LeftJoin<TaxBucketLine,
							On<TaxBucketLine.vendorID, Equal<TaxReportLine.vendorID>,
								And<TaxBucketLine.taxReportRevisionID, Equal<TaxReportLine.taxReportRevisionID>,
								And<TaxBucketLine.lineNbr, Equal<TaxReportLine.lineNbr>>>>>,
							Where<TaxReportLine.vendorID, Equal<Required<TaxReportLine.vendorID>>,
								And<TaxReportLine.taxReportRevisionID, Equal<Required<TaxReportLine.taxReportRevisionID>>,
								And<TaxReportLine.tempLineNbr, Equal<Required<TaxReportLine.tempLineNbr>>>>>>
							.Select(this, newBucketLine.VendorID, newBucketLine.TaxReportRevisionID, newBucketLine.LineNbr))
						{
							TaxBucketLine new_bucket = PXCache<TaxBucketLine>.CreateCopy(newBucketLine);
							new_bucket.LineNbr = ((TaxReportLine)res).LineNbr;
							new_bucket.TaxReportRevisionID = ((TaxReportLine)res).TaxReportRevisionID;
							this.Caches<TaxBucketLine>().Insert(new_bucket);
						}
					}
				}
			}
			yield return Report.Current;
		}

		public PXAction<TaxReport> deleteReportVersion;
		[PXUIField(DisplayName = Messages.DeleteReportVerion, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(Category = ActionCategories.ReportManagement)]
		protected virtual IEnumerable DeleteReportVersion(PXAdapter adapter)
		{
			if (IsReportVersionHasHistory(this, Report.Current.RevisionID, Report.Current.VendorID))
			{
				throw new PXException(Messages.ReportVerionHasHistory);
			}
			else
			{
				if (Report.Ask(Messages.DeleteNotificationHeader, Messages.DeleteReportVerionAsk, MessageButtons.YesNo) == WebDialogResult.Yes)
				{
					TaxReport currentReport = new TaxReport();
					Report.Current.ShowNoTemp = false;
					currentReport = PXCache<TaxReport>.CreateCopy(Report.Current);
					PXResultset<TaxReportLine> baseReportLines = ReportLine.Select();
					PXResultset<TaxBucketLine> baseBucketLines = TaxBucketLines.Select(currentReport.VendorID, currentReport.RevisionID);

					foreach (TaxReportLine repLine in baseReportLines)
					{
						this.Caches<TaxReportLine>().Delete(repLine);
					}
					foreach (TaxBucketLine bucketLine in baseBucketLines)
					{
						this.Caches<TaxBucketLine>().Delete(bucketLine);
					}
					this.Caches<TaxReport>().Delete(currentReport);

					TaxReport prevReportVersion = GetPreviuosReportVersion(this, currentReport);
					if (prevReportVersion != null)
					{
						prevReportVersion.ValidTo = (currentReport.ValidTo == TaxReportMaint.MAX_VALIDTO) ?
							TaxReportMaint.MAX_VALIDTO :
							currentReport.ValidTo;
						this.Caches<TaxReport>().MarkUpdated(prevReportVersion);
					}

					Actions.PressSave();
					Report.Current = prevReportVersion;
				}
			}
			yield return Report.Current;
		}

		public class TaxBucketAnalizer
		{
			private Dictionary<int, List<int>> _bucketsLinesAggregates;
			private Dictionary<int, List<int>> _bucketsLinesAggregatesSorted;
			private Dictionary<int, List<int>> _bucketsDict;
			private Dictionary<int, int> _bucketLinesOccurence;
			private Dictionary<int, Dictionary<int, int>> _bucketsLinesPairs;
			private int _bAccountID;
			private int _taxreportVersionID;
			private string _taxLineType;
			private PXGraph _graph;

			private PXSelectJoin<TaxBucketLine, 
				LeftJoin<TaxReportLine,
					On<TaxBucketLine.lineNbr, Equal<TaxReportLine.lineNbr>,
					And<TaxBucketLine.taxReportRevisionID, Equal<TaxReportLine.taxReportRevisionID>,
					And<TaxBucketLine.vendorID, Equal<TaxReportLine.vendorID>>>>>,
				Where<TaxBucketLine.vendorID, Equal<Required<TaxBucketLine.vendorID>>,
					And<TaxBucketLine.taxReportRevisionID, Equal<Required<TaxBucketLine.taxReportRevisionID>>,
					And<TaxReportLine.lineType, Equal<Required<TaxReportLine.lineType>>>>>> _vendorBucketLines;

			public Func<TaxReportLine, bool> showTaxReportLine
			{
				get;
                set;
			}

			private IEnumerable<PXResult<TaxBucketLine>> selectBucketLines(int VendorId, string LineType, int TaxReportVersion)
			{
				return _vendorBucketLines.Select(VendorId, TaxReportVersion, LineType).AsEnumerable()
										 .Where(set => showTaxReportLine(set.GetItem<TaxReportLine>()));
			}

			public TaxBucketAnalizer(PXGraph graph, int BAccountID, string TaxLineType, int TaxreportRevisionID)
			{
				_bAccountID = BAccountID;
				_taxreportVersionID = TaxreportRevisionID;
				_taxLineType = TaxLineType;
				_graph = graph;
				_vendorBucketLines = 
                    new PXSelectJoin<TaxBucketLine, 
                            LeftJoin<TaxReportLine,
                                On<TaxBucketLine.lineNbr, Equal<TaxReportLine.lineNbr>,
								And<TaxBucketLine.taxReportRevisionID, Equal<TaxReportLine.taxReportRevisionID>,
                                And<TaxBucketLine.vendorID, Equal<TaxReportLine.vendorID>>>>>,
					    Where<TaxBucketLine.vendorID, Equal<Required<TaxBucketLine.vendorID>>,
						And<TaxBucketLine.taxReportRevisionID, Equal<Required<TaxBucketLine.taxReportRevisionID>>,
                          And<TaxReportLine.lineType, Equal<Required<TaxReportLine.lineType>>>>>>(_graph);

				showTaxReportLine = line => true;
			}

			public Dictionary<int, List<int>> AnalyzeBuckets(bool CalcWithZones)
			{
				calcOccurances(CalcWithZones);
				fillAgregates();
				return _bucketsLinesAggregatesSorted;
			}

			public void DoChecks(int BucketID)
			{
				if (_bucketsDict == null)
				{
					calcOccurances(true);
					fillAgregates();
				}

				doChecks(BucketID);
			}

			#region Public Static functions

			public static void CheckTaxAgencySettings(PXGraph graph, int BAccountID, int TaxReportRevisionID)
			{
				PXResultset<TaxBucket> buckets = 
                    PXSelect<TaxBucket, 
                        Where<TaxBucket.vendorID, Equal<Required<TaxBucket.vendorID>>>>
                    .Select(graph, BAccountID);

				if (buckets == null)
                    return;

				TaxBucketAnalizer taxAnalizer = new TaxBucketAnalizer(graph, BAccountID, TaxReportLineType.TaxAmount, TaxReportRevisionID);
				TaxBucketAnalizer taxableAnalizer = new TaxBucketAnalizer(graph, BAccountID, TaxReportLineType.TaxableAmount, TaxReportRevisionID);

				foreach (TaxBucket bucket in buckets)
				{
                    int bucketID = bucket.BucketID.Value;
                    taxAnalizer.DoChecks(bucketID);
					taxableAnalizer.DoChecks(bucketID);
				}
			}

			[Obsolete("Will be removed in future versions of Acumatica")]
			public static Dictionary<int, int> TransposeDictionary(Dictionary<int, List<int>> oldDict)
			{
				if (oldDict == null)
				{
					return null;
				}

				Dictionary<int, int> newDict = new Dictionary<int, int>(capacity: oldDict.Count);

				foreach (KeyValuePair<int, List<int>> kvp in oldDict)
				{
					foreach (int val in kvp.Value)
					{
						newDict[val] = kvp.Key;
					}
				}

				return newDict;
			}

			public static bool IsSubList(List<int> searchList, List<int> subList)
			{
				if (subList.Count > searchList.Count)
				{
					return false;
				}

				for (int i = 0; i < subList.Count; i++)
				{
					if (!searchList.Contains(subList[i]))
					{
						return false;
					}
				}

				return true;
			}

			public static List<int> SubstList(List<int> searchList, List<int> substList, int substVal)
			{
				if (!IsSubList(searchList, substList))
				{
					return searchList;
				}

				List<int> resList = searchList.ToList();
                substList.ForEach(val => resList.Remove(val));
				resList.Add(substVal);
				return resList;
			}

			#endregion

			#region Private Methods
			private void calcOccurances(bool CalcWithZones)
			{
				if (!CalcWithZones)
				{
					_vendorBucketLines.WhereAnd<Where<TaxReportLine.tempLineNbr, IsNull>>();
				}

				IEnumerable<PXResult<TaxBucketLine>> BucketLineTaxAmt = selectBucketLines(_bAccountID, _taxLineType, _taxreportVersionID);

				if (BucketLineTaxAmt == null)
				{
					_bucketsDict = null;
					return;
				}

				_bucketsDict = new Dictionary<int, List<int>>();

				foreach (PXResult<TaxBucketLine> bucketLineSet in BucketLineTaxAmt)
				{
					TaxBucketLine bucketLine = (TaxBucketLine) bucketLineSet[typeof (TaxBucketLine)];
					TaxReportLine reportLine = (TaxReportLine) bucketLineSet[typeof (TaxReportLine)];

					if (bucketLine.BucketID != null && reportLine.LineNbr != null)
					{
						if (!_bucketsDict.ContainsKey((int) bucketLine.BucketID))
						{
							_bucketsDict[(int) bucketLine.BucketID] = new List<int>();
						}

						_bucketsDict[(int) bucketLine.BucketID].Add((int) bucketLine.LineNbr);
					}
				}

				List<int> bucketsList = _bucketsDict.Keys.ToList();

				for (int i = 0; i < bucketsList.Count; i++)
				{
					for (int j = i + 1; j < bucketsList.Count; j++)
					{
						if (_bucketsDict[bucketsList[i]].Count == _bucketsDict[bucketsList[j]].Count
						    && IsSubList(_bucketsDict[bucketsList[i]], _bucketsDict[bucketsList[j]]))
						{
							_bucketsDict.Remove(bucketsList[i]);
							break;
						}
					}
				}

				_bucketLinesOccurence = new Dictionary<int, int>();
				_bucketsLinesPairs = new Dictionary<int, Dictionary<int, int>>();

				foreach (KeyValuePair<int, List<int>> kvp in _bucketsDict)
				{
					foreach (int lineNbr in kvp.Value)
					{
						if (!_bucketLinesOccurence.ContainsKey(lineNbr))
						{
							_bucketLinesOccurence[lineNbr] = 0;
						}
						_bucketLinesOccurence[lineNbr]++;
					}

					for (int i = 0; i < kvp.Value.Count - 1; i++)
					{
						for (int j = i + 1; j < kvp.Value.Count; j++)
						{
							int key;
							int value;

							if (kvp.Value[i] < kvp.Value[j])
							{
								key = kvp.Value[i];
								value = kvp.Value[j];
							}
							else
							{
								key = kvp.Value[j];
								value = kvp.Value[i];
							}

							if (!_bucketsLinesPairs.ContainsKey(key))
							{
								_bucketsLinesPairs[key] = new Dictionary<int, int>();
							}

							if (!_bucketsLinesPairs[key].ContainsKey(value))
							{
								_bucketsLinesPairs[key][value] = 0;
							}

							_bucketsLinesPairs[key][value]++;
						}
					}
				}
			}

			private void fillAgregates()
			{
				if (_bucketsDict == null || _bucketLinesOccurence == null || _bucketsLinesPairs == null)
					return;

				_bucketsLinesAggregates = new Dictionary<int, List<int>>();

				foreach (KeyValuePair<int, Dictionary<int, int>> kvp in _bucketsLinesPairs)
				{
					foreach (KeyValuePair<int, int> innerkvp in kvp.Value)
					{
						if (innerkvp.Value == 1)
						{
							int keyOccurence = _bucketLinesOccurence[kvp.Key];
							int valOccurence = _bucketLinesOccurence[innerkvp.Key];
							int aggregate = 0;
							int standAloneVal = 0;

							if (keyOccurence != valOccurence)
							{
								if (keyOccurence > valOccurence)
								{
									aggregate = kvp.Key;
									standAloneVal = innerkvp.Key;
								}
								else
								{
									aggregate = innerkvp.Key;
									standAloneVal = kvp.Key;
								}
							}

							if (aggregate != 0)
							{
								if (!_bucketsLinesAggregates.ContainsKey(aggregate))
								{
									_bucketsLinesAggregates[aggregate] = new List<int>();
								}

								_bucketsLinesAggregates[aggregate].Add(standAloneVal);
							}
						}
					}
				}

				List<KeyValuePair<int, List<int>>> sortedAggregatesList = _bucketsLinesAggregates.ToList();
				sortedAggregatesList.Sort((firstPair, nextPair) => firstPair.Value.Count - nextPair.Value.Count == 0 ?
					                                                   firstPair.Key - nextPair.Key : firstPair.Value.Count - nextPair.Value.Count);
				for (int i = 0; i < sortedAggregatesList.Count; i++)
				{
					for (int j = i + 1; j < sortedAggregatesList.Count; j++)
					{
						List<int> newList = SubstList(sortedAggregatesList[j].Value, sortedAggregatesList[i].Value, sortedAggregatesList[i].Key);

						if (newList != sortedAggregatesList[j].Value)
						{
							sortedAggregatesList[j].Value.Clear();
							sortedAggregatesList[j].Value.AddRange(newList);
						}
					}
				}

				_bucketsLinesAggregatesSorted = new Dictionary<int, List<int>>();

				foreach (KeyValuePair<int, List<int>> kvp in sortedAggregatesList)
				{
					kvp.Value.Sort();
					_bucketsLinesAggregatesSorted[kvp.Key] = kvp.Value;
				}
			}

			private void doChecks(int BucketID)
			{
				if (_bucketsLinesAggregatesSorted == null)
				{
					throw new PXException(Messages.UnexpectedCall);
				}

				int standaloneLinesCount = 0;
				int aggrergateLinesCount = 0;

				if (_bucketsDict.ContainsKey(BucketID))
				{
					foreach (int line in _bucketsDict[BucketID])
					{
						//can only be 1 or more
						if (_bucketsLinesAggregatesSorted.ContainsKey(line))
						{
							aggrergateLinesCount++;
						}
						else
						{
							standaloneLinesCount++;
						}
					}

					if (aggrergateLinesCount > 0 && standaloneLinesCount == 0)
					{
						throw new PXSetPropertyException(Messages.BucketContainsOnlyAggregateLines, PXErrorLevel.Error, BucketID.ToString());
					}
				}
			}

			#endregion
		}

		public static Dictionary<int, List<int>> AnalyseBuckets(PXGraph graph, int BAccountID, int TaxReportRevisionID, string TaxLineType, bool CalcWithZones, Func<TaxReportLine, bool> ShowTaxReportLine = null)
        {
			TaxBucketAnalizer analizer = new TaxBucketAnalizer(graph, BAccountID, TaxLineType, TaxReportRevisionID);

			if (ShowTaxReportLine != null)
			{
				analizer.showTaxReportLine = ShowTaxReportLine;
			}

			return analizer.AnalyzeBuckets(CalcWithZones);
        }
  
        private void UpdateNet(object row)
		{
			bool refreshNeeded = false;
			TaxReportLine currow = row as TaxReportLine;
			currow.TaxReportRevisionID = Report.Current.RevisionID;

			if (currow.NetTax.Value && currow.TempLineNbr == null)
			{
				foreach (TaxReportLine reportrow in PXSelect<TaxReportLine, Where<TaxReportLine.vendorID, Equal<Required<TaxReport.vendorID>>, And<TaxReportLine.taxReportRevisionID, Equal<Required<TaxReport.revisionID>>>>>.Select(this,currow.VendorID, currow.TaxReportRevisionID))
				{
					if (reportrow.NetTax.Value && reportrow.LineNbr != currow.LineNbr && reportrow.TempLineNbr != currow.LineNbr)
					{
						reportrow.NetTax = false;
						ReportLine.Cache.Update(reportrow);
						refreshNeeded = true;
					}
				}
			}

			if (refreshNeeded)
			{
				ReportLine.View.RequestRefresh();
			}
		}

		public TaxReportLine CreateChildLine(TaxReportLine template, TaxZone zone)
		{
			TaxReportLine child = PXCache<TaxReportLine>.CreateCopy(template);

			child.TempLineNbr = child.LineNbr;
			child.TaxZoneID = zone.TaxZoneID;
			child.LineNbr = null;
			child.TempLine = false;
			child.ReportLineNbr = null;
			child.SortOrder = template.SortOrder;

			if (!string.IsNullOrEmpty(child.Descr))
			{
				int fid = child.Descr.IndexOf(TAG_TAXZONE, StringComparison.OrdinalIgnoreCase);

				if (fid >= 0)
				{
					child.Descr = child.Descr.Remove(fid, TAG_TAXZONE.Length).Insert(fid, child.TaxZoneID);
				}
			}

			return child;
		}

		private void UpdateZones(PXCache sender, TaxReportLine oldRow, TaxReportLine newRow)
		{			
			if (oldRow != null && (newRow == null || newRow.TempLine == false))
			{
				if (!string.IsNullOrEmpty(newRow?.Descr))
				{
					int fid = newRow.Descr.IndexOf(TAG_TAXZONE, StringComparison.OrdinalIgnoreCase);

					if (fid >= 0)
					{
						newRow.Descr = newRow.Descr.Remove(fid, TAG_TAXZONE.Length).TrimEnd(' ');
					}
				}

				DeleteChildTaxLinesForMainTaxLine(oldRow);
			}

			if (newRow?.TempLine == true && newRow.TempLine != oldRow?.TempLine)
			{
				newRow.TaxZoneID = null;

				if (string.IsNullOrEmpty(newRow.Descr) || newRow.Descr.IndexOf(TAG_TAXZONE, StringComparison.OrdinalIgnoreCase) < 0)
				{
					newRow.Descr += ' ' + TAG_TAXZONE;
				}

				foreach (TaxZone zone in PXSelect<TaxZone>.Select(this))
				{
					TaxReportLine child = CreateChildLine(newRow, zone);
					sender.Insert(child);
				}
			}

			if (newRow?.TempLine == true && oldRow?.TempLine == true)
			{
				UpdateTaxLineOnFieldUpdatedWhenDetailByTaxZoneNotChanged(sender, oldRow, newRow);
			}
		}

		private void DeleteChildTaxLinesForMainTaxLine(TaxReportLine mainLine)
		{
			var childTaxLines =
				PXSelect<TaxReportLine,
					Where<TaxReportLine.vendorID, Equal<Required<TaxReportLine.vendorID>>,
						And<TaxReportLine.taxReportRevisionID, Equal<Required<TaxReportLine.taxReportRevisionID>>,
						And<TaxReportLine.tempLineNbr, Equal<Required<TaxReportLine.tempLineNbr>>>>>>
					.Select(this, mainLine.VendorID, mainLine.TaxReportRevisionID, mainLine.LineNbr);

			foreach (TaxReportLine child in childTaxLines)
			{
				ReportLine.Cache.Delete(child);
			}
		}

		private void UpdateTaxLineOnFieldUpdatedWhenDetailByTaxZoneNotChanged(PXCache sender, TaxReportLine oldRow, TaxReportLine newRow)
		{
			var childTaxLines =
					PXSelect<TaxReportLine,
						Where<TaxReportLine.vendorID, Equal<Required<TaxReportLine.vendorID>>,
							And<TaxReportLine.taxReportRevisionID, Equal<Required<TaxReportLine.taxReportRevisionID>>,
							And<TaxReportLine.tempLineNbr, Equal<Required<TaxReportLine.tempLineNbr>>>>>>
					.Select(this, oldRow.VendorID, oldRow.TaxReportRevisionID, oldRow.LineNbr);

			foreach (TaxReportLine child in childTaxLines)
			{
				child.Descr = newRow.Descr;

				if (!string.IsNullOrEmpty(child.Descr))
				{
					int fid = child.Descr.IndexOf(TAG_TAXZONE, StringComparison.OrdinalIgnoreCase);

					if (fid >= 0)
					{
						child.Descr = child.Descr.Remove(fid, TAG_TAXZONE.Length)
												 .Insert(fid, child.TaxZoneID);
					}
				}

				child.NetTax = newRow.NetTax;
				child.LineType = newRow.LineType;
				child.LineMult = newRow.LineMult;
				child.SortOrder = newRow.SortOrder;
				sender.Update(child);
			}
		}

		protected virtual void TaxReport_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			deleteReportVersion.SetEnabled(false);
			createReportVersion.SetEnabled(false);
			copyReportVersion.SetEnabled(false);

			if (e.Row == null)
				return;

			TaxReport vendor = (TaxReport)e.Row;
			bool showWithoutTaxZones = vendor.ShowNoTemp.Value == false;

			PXUIFieldAttribute.SetVisible<TaxReportLine.tempLine>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetVisible<TaxReportLine.bucketSum>(ReportLine.Cache, null, showWithoutTaxZones);

			PXUIFieldAttribute.SetEnabled<TaxReportLine.tempLine>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetEnabled<TaxReportLine.netTax>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetEnabled<TaxReportLine.taxZoneID>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetEnabled<TaxReportLine.lineType>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetEnabled<TaxReportLine.lineMult>(ReportLine.Cache, null, showWithoutTaxZones);
			PXUIFieldAttribute.SetEnabled<TaxReportLine.sortOrder>(ReportLine.Cache, null, showWithoutTaxZones);

			ReportLine.AllowDragDrop = showWithoutTaxZones;
			ReportLine.AllowInsert = showWithoutTaxZones;

			Up.SetEnabled(showWithoutTaxZones);
			Down.SetEnabled(showWithoutTaxZones);

			CheckReportSettingsEditableAndSetWarningTo<TaxReport.revisionID>(this, sender, vendor, vendor.VendorID, vendor.RevisionID);

			if (vendor.VendorID.HasValue)
			{
				bool hasHistory = IsReportVersionHasHistory(this, vendor.RevisionID, vendor.VendorID);
				deleteReportVersion.SetEnabled(!hasHistory && Report.Cache.Inserted.Count() == 0);
				createReportVersion.SetEnabled(Report.Cache.Inserted.Count() == 0 && Report.Cache.IsDirty == false);
				copyReportVersion.SetEnabled(Report.Cache.Inserted.Count() == 0 && Report.Cache.IsDirty == false);
				PXUIFieldAttribute.SetEnabled<TaxReport.validFrom>(Report.Cache, vendor, !hasHistory && vendor.ValidTo == TaxReportMaint.MAX_VALIDTO);
			}
		}

		protected virtual void TaxReport_RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			TaxReport vendorMaster = e.Row as TaxReport;
			if(vendorMaster == null)
			{
				return;
			}

			if (vendorMaster.ShowNoTemp == null)
			{
				vendorMaster.ShowNoTemp = false;
			}

			if (vendorMaster.LineCntr == null)
			{
				vendorMaster.LineCntr = 0;
			}
		}

		protected virtual void _(Events.RowPersisting<TaxReport> e)
		{
			if (e.Row == null || e.Row.ValidFrom == null) return;

			TaxReport curReport = e.Row as TaxReport;
			PXEntryStatus repStatus = Report.Cache.GetStatus(curReport);
			if (repStatus == PXEntryStatus.Inserted ||
				repStatus == PXEntryStatus.Updated && curReport.ValidTo == MAX_VALIDTO)
			{
				TaxReport prevRepVersion = (repStatus == PXEntryStatus.Inserted ) ?
						GetLastReportVersion(this, curReport.VendorID, curReport.RevisionID) :
						GetPreviuosReportVersion(this, curReport);
				if (prevRepVersion != null)
				{
					prevRepVersion.ValidTo = curReport.ValidFrom.Value.AddDays(-1);
					this.Caches<TaxReport>().MarkUpdated(prevRepVersion);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<TaxReport.validFrom> e)
		{
			if (e.NewValue == null) return;

			TaxReport report = e.Row as TaxReport;
			TaxReport checkRep = (TaxReport)Report.Cache.CreateCopy(report);
			checkRep.ValidFrom = (DateTime?)e.NewValue;

			// check history exists
			string lastperiod = GetLastReportPeriodInHistory(this, report.VendorID);
			Vendor vendor = VendorMaint.GetByID(this, report.VendorID);
			if (lastperiod != null)
			{
				DateTime endDateOfPeriod = TaxYearMaint.GetTaxPeriodByKey(this,
					(PXAccess.GetBranch(Accessinfo.BranchID) as PXAccess.MasterCollection.Branch).Organization.OrganizationID,
					report.VendorID, lastperiod).EndDate.Value.AddDays(-1);
				if (endDateOfPeriod >= checkRep.ValidFrom)
				{
					throw new PXSetPropertyException(Messages.ValidDateIntersectHistory,
					PXErrorLevel.Error,
					FinPeriodIDFormattingAttribute.FormatForError(lastperiod),
					vendor.AcctCD);
				}
			}

			// check intersect with existing versions
			PXEntryStatus repState = Report.Cache.GetStatus(report);
			TaxReport prevRep = (repState == PXEntryStatus.Inserted) ?
				GetLastReportVersion(this, report.VendorID, report.RevisionID) :
				GetPreviuosReportVersion(this, report);
			if (prevRep != null && checkRep.ValidFrom <= prevRep.ValidFrom)
			{
				TaxReport intersectRep = GetTaxReportVersionByDate(this, checkRep.VendorID, checkRep.ValidFrom, report.RevisionID);
				PXSetPropertyException ex = null;
				if (intersectRep == null)
				{
					ex = new PXSetPropertyException(Messages.ValidDateBeforeExistingReportVersion,
						PXErrorLevel.Error,
						prevRep.RevisionID,
						prevRep.ValidFrom);
				}
				else
				{
					ex = new PXSetPropertyException(Messages.ValidDateIntersectOtherReportVersion,
						PXErrorLevel.Error,
						intersectRep.RevisionID,
						VendorMaint.GetByID(this, report.VendorID)?.AcctCD);
				}
				throw ex;
			}
		}

		protected virtual void _(Events.RowUpdated<TaxReport> e)
		{
			if (e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted && this.IsImport != true)
			{
				TaxReport originatedReportVersion = TaxReport.PK.Find(this, e.Row.VendorID, e.Row.RevisionID);
				if (e.Cache.ObjectsEqual<TaxReport.vendorID, TaxReport.revisionID, TaxReport.validFrom>(e.Row, originatedReportVersion))
				{
					e.Cache.IsDirty = false;
					Report.Cache.SetStatus(e.Row, PXEntryStatus.Notchanged);
				}
			}
		}

		protected virtual void TaxReportLine_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			TaxReportLine line = e.Row as TaxReportLine;

			if (line == null)
				return;

			PXUIFieldAttribute.SetEnabled<TaxReportLine.reportLineNbr>(sender, line, line.HideReportLine != true);
		}
		
		protected virtual void TaxReportLine_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			UpdateNet(e.Row);
			UpdateZones(sender, null, e.Row as TaxReportLine);
		}

		protected virtual void TaxReportLine_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			UpdateNet(e.Row);
			UpdateZones(sender, e.OldRow as TaxReportLine, e.Row as TaxReportLine);
		}

		protected virtual void TaxReportLine_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			UpdateZones(sender, e.Row as TaxReportLine, null);
		}

		protected virtual void TaxReportLine_HideReportLine_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			TaxReportLine line = e.Row as TaxReportLine;

			if (line == null)
				return;

			if (line.HideReportLine == true)
			{
				sender.SetValueExt<TaxReportLine.reportLineNbr>(line, null);
			}
		}

		public override void Persist()
		{
			if (Report.Current != null)
			{
				CheckAndWarnTaxBoxNumbers();
				CheckReportSettingsEditable(this, Report.Current.VendorID, Report.Current.RevisionID);
				SyncReportLinesAndBucketLines();
			}
			base.Persist();
		}

		private void SyncReportLinesAndBucketLines()
		{
			if (this.Caches<TaxReportLine>().Inserted.Count() > 0 && this.Caches<TaxReport>().Inserted.Count() > 0)
			{
				/// sync lines for copied report version
				List<TaxReportLine> reportLinesAll = PXSelect<TaxReportLine,
					Where<TaxReportLine.vendorID, Equal<TaxReport.vendorID.FromCurrent>,
					And<TaxReportLine.taxReportRevisionID, Equal<TaxReport.revisionID.FromCurrent>>>>.Select(this).RowCast<TaxReportLine>().ToList();

				List<TaxBucketLine> bucketLinesAll = TaxBucketLines.Select(Report.Current.VendorID, Report.Current.RevisionID).RowCast<TaxBucketLine>().ToList();

				foreach (TaxReportLine reportLine in reportLinesAll)
				{
					if (reportLine.TempLineNbr != null)
					{
						/// add childlines for a newly created parent line
						/// parentbuckets = bucketlines of parent report line
						var parentBuckets = TaxBucketLine_Vendor_LineNbr.Select(reportLine.VendorID, reportLine.TaxReportRevisionID, reportLine.TempLineNbr).RowCast<TaxBucketLine>();

						foreach (var bucketLine in parentBuckets)
						{
							TaxBucketLine newBucketLine = PXCache<TaxBucketLine>.CreateCopy(bucketLine);
							newBucketLine.LineNbr = reportLine.LineNbr;
							newBucketLine.TaxReportRevisionID = reportLine.TaxReportRevisionID;
							if (TaxBucketLine_Vendor_LineNbr.Cache.Locate(newBucketLine) == null)
							{
								TaxBucketLine_Vendor_LineNbr.Cache.Insert(newBucketLine);
							}
						}
					}
				}

				foreach (TaxBucketLine bucketLine in bucketLinesAll)
				{
					/// remove extra BucketLines
					TaxReportLine reportLine = reportLinesAll.Find(i => i.TaxReportRevisionID == bucketLine.TaxReportRevisionID
					&& i.VendorID == bucketLine.VendorID
					&& i.LineNbr == bucketLine.LineNbr);

					if (reportLine == null)
					{
						TaxBucketLines.Delete(bucketLine);
					}
				}
			}
			else
			{
				/// sync lines for selected report version
				TaxBucketLine_Vendor_LineNbr.Cache.Clear();

				foreach (var reportLine in ReportLine.Cache.Inserted.RowCast<TaxReportLine>())
				{
					var parentBuckets = TaxBucketLine_Vendor_LineNbr.Select(reportLine.VendorID, reportLine.TaxReportRevisionID, reportLine.TempLineNbr).RowCast<TaxBucketLine>();

					foreach (var bucketLine in parentBuckets)
					{
						TaxBucketLine newBucketLine = PXCache<TaxBucketLine>.CreateCopy(bucketLine);
						newBucketLine.LineNbr = reportLine.LineNbr;
						newBucketLine.TaxReportRevisionID = reportLine.TaxReportRevisionID;
						TaxBucketLine_Vendor_LineNbr.Cache.Insert(newBucketLine);
					}
				}

				foreach (var reportLine in ReportLine.Cache.Deleted.RowCast<TaxReportLine>())
				{
					var bucketLinesToDelete = TaxBucketLine_Vendor_LineNbr.Select(reportLine.VendorID, reportLine.TaxReportRevisionID, reportLine.LineNbr);
					foreach (var bucketLine in bucketLinesToDelete)
					{
						TaxBucketLine_Vendor_LineNbr.Cache.Delete(bucketLine);
					}
				}
			}
		}

		public static void CheckReportSettingsEditableAndSetWarningTo<TRevisionIDField>(PXGraph graph, PXCache cache, object row, int? vendorID, int? taxReportRevisionID)
			where TRevisionIDField : IBqlField
		{
			if (vendorID == null || taxReportRevisionID == null) return;

			if (PrepearedTaxPeriodForReportVersionExists(graph, vendorID, taxReportRevisionID))
			{
				var bAccIDfieldState = (PXFieldState)cache.GetStateExt<TRevisionIDField>(row);

				cache.RaiseExceptionHandling<TRevisionIDField>(row, bAccIDfieldState.Value,
					new PXSetPropertyException(Messages.TheTaxReportSettingsCannotBeModified, PXErrorLevel.Warning));
			}
		}

		public static void CheckReportSettingsEditable(PXGraph graph, int? vendorID, int? taxReportRevisionID)
		{
			if (vendorID == null || taxReportRevisionID == null) return;

			if (PrepearedTaxPeriodForReportVersionExists(graph, vendorID, taxReportRevisionID))
				throw new PXException(Messages.TheTaxReportSettingsCannotBeModified);
		}

		protected virtual bool IsReportVersionHasHistory(PXGraph graph, int? revisionId, int? vendorID)
		{
			return PXSelectGroupBy<TaxHistory,
				Where<TaxHistory.taxReportRevisionID, Equal<Required<TaxReport.revisionID>>,
					And<TaxHistory.vendorID, Equal<Required<TaxReport.vendorID>>>>,
				Aggregate<Count>>.Select(graph, revisionId, vendorID).RowCount > 0;
		}

		public static TaxReport GetLastReportVersion(PXGraph graph, int? vendorID)
		{
			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>,
				And<TaxReport.validTo, Equal<Required<TaxReport.validTo>>>>,
				OrderBy<Desc<TaxReport.validFrom>>>.Select(graph, vendorID, TaxReportMaint.MAX_VALIDTO);
		}
		/// <summary>
		/// Get last Tax Report Version for VendorID
		/// </summary>
		/// <param name="exceptRevisionID">RevisionID to be excluded from search. Set null to get max version</param>
		/// <returns></returns>
		protected virtual TaxReport GetLastReportVersion(PXGraph graph, int? vendorID, int? exceptRevisionID)
		{
			if (exceptRevisionID == null)
			{
				return GetLastReportVersion(graph, vendorID);
			}

			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>,
					And<TaxReport.revisionID, NotEqual<Required<TaxReport.revisionID>>,
					And<TaxReport.validTo, Equal<Required<TaxReport.validTo>>>>>,
				OrderBy<Desc<TaxReport.validTo>>>.SelectWindowed(graph, 0, 1, vendorID, exceptRevisionID, TaxReportMaint.MAX_VALIDTO);
		}

		protected virtual TaxReport GetPreviuosReportVersion(PXGraph graph, TaxReport taxReport)
		{
			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>,
					And<TaxReport.validFrom, LessEqual<Required<TaxReport.validTo>>, And<TaxReport.validTo, NotEqual<Required<TaxReport.validTo>>>>>,
				OrderBy<Desc<TaxReport.validTo>>>
				.SelectWindowed(graph, 0, 1, taxReport.VendorID, taxReport.ValidFrom, TaxReportMaint.MAX_VALIDTO);
		}

		protected virtual string GetLastReportPeriodInHistory(PXGraph graph, int? vendorID)
		{
			TaxHistory hist = PXSelect<TaxHistory,
							Where<TaxHistory.vendorID, Equal<Required<TaxReport.vendorID>>>,
							OrderBy<Desc<TaxHistory.taxPeriodID>>>
							.SelectWindowed(graph, 0, 1, vendorID);
			return hist == null ? null : hist.TaxPeriodID;
		}

		public static bool PrepearedTaxPeriodForReportVersionExists(PXGraph graph, int? vendorID, int? taxReportRevisionID)
		{
			TaxPeriod prepearedTaxPeriod = (TaxPeriod)PXSelectJoin<TaxPeriod,
				LeftJoin<TaxReport, On<TaxReport.vendorID, Equal<TaxPeriod.vendorID>>>,
				Where<TaxPeriod.vendorID, Equal<Required<TaxPeriod.vendorID>>,
						And<TaxReport.revisionID, Equal<Required<TaxReport.revisionID>>,
						And<TaxPeriod.status, Equal<TaxPeriodStatus.prepared>,
						And<Add<TaxPeriod.endDate, int_1>, Between<TaxReport.validFrom, TaxReport.validTo>>>>>>
				.Select(graph, vendorID, taxReportRevisionID);

			return prepearedTaxPeriod != null;
		}

		public static TaxReport GetTaxReportVersionByRevisionID(PXGraph graph, int? vendorID, int? revisionID)
		{
			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>, And<TaxReport.revisionID, Equal<Required<TaxReport.revisionID>>>>>.Select(graph, vendorID, revisionID);
		}
		public static TaxReport GetTaxReportVersionByDate(PXGraph graph, int? vendorID, DateTime? searchDate)
		{
			if (vendorID == null || searchDate == null) return null;
			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>, 
				And<TaxReport.validFrom, LessEqual<Required<TaxReport.validFrom>>>>, 
				OrderBy<Desc<TaxReport.validFrom>>>
				.Select(graph, vendorID, searchDate).RowCast<TaxReport>().ToList<TaxReport>().FirstOrDefault<TaxReport>();
		}
		/// <summary>
		/// Get Tax Report Version by date
		/// </summary>
		/// <param name="exceptRevisionID">RevisionID to be excluded from search. Set null to get all versions</param>
		/// <returns></returns>
		public static TaxReport GetTaxReportVersionByDate(PXGraph graph, int? vendorID, DateTime? searchDate, int? exceptRevisionID)
		{
			if (vendorID == null || searchDate == null) return null;

			if (exceptRevisionID == null)
				return GetTaxReportVersionByDate(graph, vendorID, searchDate);

			return PXSelect<TaxReport,
				Where<TaxReport.vendorID, Equal<Required<TaxReport.vendorID>>,
				And<TaxReport.validFrom, LessEqual<Required<TaxReport.validFrom>>,
				And<TaxReport.revisionID, NotEqual<Required<TaxReport.revisionID>>>>>,
				OrderBy<Desc<TaxReport.validFrom>>>
				.Select(graph, vendorID, searchDate, exceptRevisionID).RowCast<TaxReport>().ToList<TaxReport>().FirstOrDefault<TaxReport>();
		}

		private void CheckAndWarnTaxBoxNumbers()
        {
            HashSet<String> taxboxNumbers = new HashSet<String>();
			var taxReportLines = ReportLine.Select().RowCast<TaxReportLine>()
													.Where(line => line.ReportLineNbr != null);

			foreach (TaxReportLine line in taxReportLines)
            {
                if (ReportLine.Cache.GetStatus(line) == PXEntryStatus.Notchanged)
                {
                    taxboxNumbers.Add(line.ReportLineNbr);
                }
            }

			CheckTaxBoxNumberUniqueness(ReportLine.Cache.Inserted, taxboxNumbers);
			CheckTaxBoxNumberUniqueness(ReportLine.Cache.Updated, taxboxNumbers);
        }

        public virtual void CheckTaxBoxNumberUniqueness(IEnumerable toBeChecked, HashSet<String> taxboxNumbers)
        {
			var lineWithErrors = toBeChecked.OfType<TaxReportLine>()
											.Where(line => line.ReportLineNbr != null && !taxboxNumbers.Add(line.ReportLineNbr));

			foreach (TaxReportLine line in lineWithErrors)
			{
				ReportLine.Cache.RaiseExceptionHandling<TaxReportLine.reportLineNbr>(line, line.ReportLineNbr, 
					new PXSetPropertyException(Messages.TaxBoxNumbersMustBeUnique));
			}
        }

		protected virtual void TaxReportLine_SortOrder_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			TaxReport taxVendor = Report.Current;
			TaxReportLine reportLine = (TaxReportLine)e.Row;
			int? newSortOrder = e.NewValue as int?;

			if (reportLine == null || reportLine.TempLineNbr != null || taxVendor == null || taxVendor.ShowNoTemp == true ||
				reportLine.SortOrder == newSortOrder)
			{
				return;
			}

			if (newSortOrder == null || newSortOrder <= 0)
			{
				string errorMsg = newSortOrder == null 
					? Common.Messages.MustHaveValue 
					: Common.Messages.ShouldBePositive;

				throw new PXSetPropertyException(errorMsg, $"[{nameof(TaxReportLine.sortOrder)}]");
			}

			bool alreadyExists = ReportLine.Select()
										   .RowCast<TaxReportLine>()
										   .Any(line => line.SortOrder.Value == newSortOrder);

			if (alreadyExists)
			{
				throw new PXSetPropertyException(Messages.SortOrderNumbersMustBeUnique);
			}
		}

		protected virtual void TaxReportLine_SortOrder_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			TaxReportLine reportLine = (TaxReportLine)e.Row;

			if (reportLine == null || reportLine.TempLineNbr != null || Equals(reportLine.SortOrder, e.OldValue))
				return;

			if (reportLine.TempLine == true)
			{
				var childTaxLines =
				   PXSelect<TaxReportLine,
					   Where<TaxReportLine.vendorID, Equal<Required<TaxReportLine.vendorID>>,
							And<TaxReportLine.taxReportRevisionID, Equal<Required<TaxReportLine.taxReportRevisionID>>,
						   And<TaxReportLine.tempLineNbr, Equal<Required<TaxReportLine.tempLineNbr>>>>>>
				   .Select(this, reportLine.VendorID, reportLine.LineNbr, reportLine.TaxReportRevisionID);

				foreach (TaxReportLine childLine in childTaxLines)
				{
					childLine.SortOrder = reportLine.SortOrder;
					ReportLine.Cache.SmartSetStatus(childLine, PXEntryStatus.Updated);
				}
			}

			ReportLine.View.RequestRefresh();
		}

		protected virtual void TaxReportLine_LineType_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			TaxReportLine line = (TaxReportLine)e.Row;

			if (e.NewValue != null && line.NetTax != null && (bool)line.NetTax && (string)e.NewValue == "A")
			{
				throw new PXSetPropertyException(Messages.NetTaxMustBeTax, PXErrorLevel.RowError);
			}
		}

		protected virtual void TaxReportLine_NetTax_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			TaxReportLine line = (TaxReportLine)e.Row;

			if (e.NewValue != null && (bool)e.NewValue && line.LineType == "A")
			{
				throw new PXSetPropertyException(Messages.NetTaxMustBeTax, PXErrorLevel.RowError);
			}
		}

		public TaxReportMaint()
		{
			APSetup setup = APSetup.Current;
			PXUIFieldAttribute.SetVisible<TaxReportLine.lineNbr>(ReportLine.Cache, null, false);
			taxDetailsReloader = new TaxReportLinesByTaxZonesReloader(this);

			FieldDefaulting.AddHandler<BAccountR.type>((sender, e) => 
			{
				if (e.Row != null)
					e.NewValue = BAccountType.VendorType;
			});
		}

		public PXSetup<APSetup> APSetup;
	}
}
