using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.CR.DAC;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.EP;
using PX.Web.UI;


namespace PX.Objects.CR
{
	public class CRDuplicateValidationSetupMaint : PXGraph<CRDuplicateValidationSetupMaint>
	{
		#region Views

		public SelectFrom<CRSetup>.View Setup;

		public SelectFrom<CRValidationTree>.View Nodes;

		[PXHidden]
		public SelectFrom<CRValidationTree>.View NodesSelect;

		protected virtual IEnumerable nodes([PXDBInt] int? nodeID)
		{
			if (nodeID == null)
			{
				return NodesSelect.Select();
			}
			else return new CRValidation[0];
		}

		public SelectFrom<
				CRValidation>
			.Where<
				CRValidation.iD.IsEqual<CRValidationTree.iD.FromCurrent>>
			.View
			CurrentNode;

		public SelectFrom<
				CRValidationRules>
			.Where<
				CRValidationRules.validationType.IsEqual<CRValidation.type.FromCurrent>>
			.View
			ValidationRules;

		public PXFilter<CRValidationRulesBuffer> Buffer;

		#endregion

		#region Actions

		public PXSave<CRSetup> Save;

		public PXCancel<CRSetup> Cancel;

		public PXAction<CRValidationRules> Copy;
		[PXButton(ImageKey = Sprite.Main.Copy, Tooltip = ActionsMessages.CopyDocument)]
		[PXUIField(DisplayName = ActionsMessages.CopyRec, Enabled = false)]
		public IEnumerable copy(PXAdapter adapter)
		{
			Buffer.Cache.Clear();
			foreach (CRValidationRules pxResult in ValidationRules.Select()
																	.RowCast<CRValidationRules>()
																	.Where(r => !String.IsNullOrEmpty(r.MatchingField)))
			{
				CRValidationRulesBuffer insertnode = Buffer.Cache.CreateInstance() as CRValidationRulesBuffer;
				insertnode.MatchingField = pxResult.MatchingField;
				insertnode.ScoreWeight = pxResult.ScoreWeight;
				insertnode.TransformationRule = pxResult.TransformationRule;
				insertnode.CreateOnEntry = pxResult.CreateOnEntry;
				Buffer.Cache.Insert(insertnode);
			}
			return adapter.Get();
		}

		public PXAction<CRValidationRules> Paste;
		[PXButton(ImageKey = Sprite.Main.Paste, Tooltip = ActionsMessages.PasteDocument)]
		[PXUIField(DisplayName = ActionsMessages.Paste, Enabled = false)]
		internal IEnumerable paste(PXAdapter adapter)
		{
			ValidationRules.Cache.Clear(); // PXCache.Update glitches on records with modified MatchingField which is a key

			List<string> matchingFields = Buffer.Cache.Cached.RowCast<CRValidationRulesBuffer>().Select(r => r.MatchingField).ToList();
			ValidationRules.Select()
				.RowCast<CRValidationRules>()
				.Where(r => !matchingFields.Contains(r.MatchingField))
				.ForEach(rule => ValidationRules.Delete(rule));

			foreach (CRValidationRulesBuffer ruleBuffer in Buffer.Cache.Cached)
			{
				if (String.IsNullOrEmpty(ruleBuffer.MatchingField)) continue;

				CRValidationRules rule = null;
				if (ValidationRules.Cache.Locate(new Dictionary<string, object>
					{
						{"ValidationType", CurrentNode.Current.Type},
						{"MatchingField", ruleBuffer.MatchingField}
					}) == 1)
				{
					rule = ValidationRules.Cache.Current as CRValidationRules;
				}

				rule = (rule == null)
						? ValidationRules.Cache.CreateInstance() as CRValidationRules
						: ValidationRules.Cache.CreateCopy(rule) as CRValidationRules;
				
				rule.ValidationType = CurrentNode.Current.Type;
				rule.MatchingField = ruleBuffer.MatchingField;
				rule.ScoreWeight = ruleBuffer.ScoreWeight;
				rule.TransformationRule = ruleBuffer.TransformationRule;
				rule.CreateOnEntry = ruleBuffer.CreateOnEntry;
				rule = (CRValidationRules)ValidationRules.Cache.Update(rule);
			}

			return adapter.Get();
		}

		#endregion

		#region Events


		#region CRValidation

		public virtual void _(Events.RowSelected<CRValidation> e)
		{
			PXUIFieldAttribute.SetEnabled<CRValidation.validateOnEntry>(e.Cache, e.Row,
				!ValidationRules
					.Select()
					.FirstTableItems
					.Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow));
		}

		public virtual void _(Events.FieldUpdated<CRValidation.validationThreshold> e)
		{
			ValidationRules
				.Select()
				.FirstTableItems
				.Where(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
				.ForEach(rule =>
				{
					rule.ScoreWeight = e.NewValue as decimal?;

					ValidationRules.Update(rule);
				});
		}

		public virtual void _(Events.RowPersisting<CRValidation> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.GramValidationDateTime == null)
			{
				e.Row.GramValidationDateTime = PXTimeZoneInfo.Now;
			}
		}

		#endregion

		#region CRValidationRules

		public virtual void _(Events.FieldSelecting<CRValidationRules, CRValidationRules.matchingField> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.ValidationType == ValidationTypesAttribute.AccountToAccount)
			{
				CreateFieldStateForFieldName(e.Args, typeof(Contact), typeof(Location));
			}
			else
			{
				CreateFieldStateForFieldName(e.Args, typeof(Contact));
			}
		}

		public virtual void _(Events.RowUpdated<CRValidationRules> e)
		{
			if (e.Row == null || e.OldRow == null)
				return;

			if (e.Row.CreateOnEntry != e.OldRow.CreateOnEntry)
			{
				ProcessBlockTypeChange(e);
			}

			if (IsSignificantlyChanged(e.Cache, e.Row, e.OldRow))
			{
				UpdateGramValidationDate(e.Row);
			}
		}

		public virtual void _(Events.RowInserted<CRValidationRules> e)
		{
			if (e.Row == null)
				return;
			
			UpdateGramValidationDate(e.Row);
		}

		public virtual void _(Events.RowDeleted<CRValidationRules> e)
		{
			if (e.Row == null)
				return;

			UpdateGramValidationDate(e.Row);
		}

		public virtual void _(Events.RowSelected<CRValidationRules> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetEnabled<CRValidationRules.validationType>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<CRValidationRules.scoreWeight>(e.Cache, e.Row, e.Row.CreateOnEntry == CreateOnEntryAttribute.Allow);
		}

		#endregion

		#endregion

		#region Methods

		public virtual void ProcessBlockTypeChange(Events.RowUpdated<CRValidationRules> e, bool dummyToSuppressEmbeddingIntoEventsList = false)
		{
			if (this.CurrentNode.Current != null && e.Row.CreateOnEntry != CreateOnEntryAttribute.Allow)
			{
				e.Row.ScoreWeight = this.CurrentNode.Current.ValidationThreshold ?? e.Row.ScoreWeight;

				var validation = this.CurrentNode.Current;

				validation.ValidateOnEntry = true;

				this.CurrentNode.Update(validation);
			}
			else
			{
				e.Cache.SetDefaultExt<CRValidationRules.scoreWeight>(e.Row);
			}
		}

		public virtual void CreateFieldStateForFieldName(PXFieldSelectingEventArgs e, params Type[] types)
		{
			List<string> allowedValues = new List<string>();
			List<string> allowedLabels = new List<string>();

			Dictionary<string, string> fields = new Dictionary<string, string>();

			foreach (var type in types)
			{
				foreach (var fieldName in this.Caches[type].GetFields_MassMergable())
				{
					PXFieldState fs = this.Caches[type].GetStateExt(null, fieldName) as PXFieldState;

					if (!fields.ContainsKey(fieldName))
						fields[fieldName] = fs != null ? fs.DisplayName : fieldName;
				}
			}

			foreach (var item in fields.OrderBy(i => i.Value))
			{
				allowedValues.Add(item.Key);
				allowedLabels.Add(item.Value);
			}

			e.ReturnState = PXStringState.CreateInstance(e.ReturnValue, 60, null, "FieldName", false, 1, null, allowedValues.ToArray(), allowedLabels.ToArray(), true, null);
		}

		public virtual bool IsSignificantlyChanged(PXCache sender, object row, object oldRow)
		{
			if (row == null || oldRow == null)
				return true;

			return !sender.ObjectsEqual<CRValidationRules.matchingField>(row, oldRow)
				|| !sender.ObjectsEqual<CRValidationRules.scoreWeight>(row, oldRow)
				|| !sender.ObjectsEqual<CRValidationRules.transformationRule>(row, oldRow);
		}

		public virtual void UpdateGramValidationDate(CRValidationRules rules)
		{
			System.Diagnostics.Debug.Assert(CurrentNode.Current?.Type == rules.ValidationType, "wrong current node");
				
			var node = PXCache<CRValidation>.CreateCopy(CurrentNode.Current);
			node.GramValidationDateTime = null;
			CurrentNode.Update(node);
		}

		#endregion

		#region Extensions

		public class GramRecalculationExt : Extensions.CRDuplicateEntities.CRGramRecalculationExt<CRDuplicateValidationSetupMaint>
		{
			public static bool IsActive() => IsFeatureActive();
		}

		#endregion
	}
}
