using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	/// <exclude/>
	public class CRDuplicateLeadsSelectorAttribute : PXCustomSelectorAttribute
	{
		private readonly Type SourceEntityID;

		public CRDuplicateLeadsSelectorAttribute(Type sourceEntityID)
			: base(typeof(
				SelectFrom<
					CRLead>
				.InnerJoin<CRDuplicateGrams>
					.On<CRDuplicateGrams.entityID.IsEqual<CRLead.contactID>>
				.InnerJoin<CRGrams>
					.On<CRGrams.validationType.IsEqual<CRDuplicateGrams.validationType>
					.And<CRGrams.fieldName.IsEqual<CRDuplicateGrams.fieldName>
					.And<CRGrams.fieldValue.IsEqual<CRDuplicateGrams.fieldValue>>>>
				.LeftJoin<CRValidation>
					.On<CRValidation.type.IsEqual<CRGrams.validationType>>
				.Where<
					CRGrams.entityID.IsEqual<CRLead.contactID.AsOptional>
					.And<CRDuplicateGrams.validationType.IsEqual<ValidationTypesAttribute.leadToLead>>>
				.AggregateTo<
					GroupBy<CRDuplicateGrams.entityID>,
					GroupBy<CRDuplicateGrams.validationType>,
					GroupBy<CRGrams.entityID>,
					Sum<CRGrams.score>>
				.Having<
					CRGrams.score.Summarized.IsGreaterEqual<CRValidation.validationThreshold.Maximized>>
				.SearchFor<
					CRLead.contactID>),

				fieldList: new[]
				{
					typeof(CRLead.displayName),
					typeof(CRLead.salutation),
					typeof(CRLead.eMail),
					typeof(CRLead.phone1)
				}
			)
		{
			DirtyRead = true;
			DescriptionField = typeof(CRLead.displayName);
			this.SourceEntityID = sourceEntityID;
			ValidateValue = false;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			View = new PXView(_Graph, true, _Select);
		}

		private PXView View;

		public IEnumerable GetRecords()
		{
			PXCache sourceCache = _Graph.Caches[SourceEntityID.DeclaringType];
			var currentEntity = PXView.Currents?.FirstOrDefault() ?? sourceCache.Current;
			if (currentEntity == null)
				yield break;

			int? sourceID = (int?)sourceCache.GetValue(currentEntity, SourceEntityID.Name);
			var selectedRec = _Graph.Caches[typeof(CRDuplicateRecord)].Current as CRDuplicateRecord;

			foreach (PXResult rec in View.SelectMulti(sourceID))
			{
				CRLead duplicateLead = rec.GetItem<CRLead>();

				if (duplicateLead.ContactID == selectedRec.DuplicateContactID || duplicateLead.ContactID == sourceID)
					yield return duplicateLead;
			}
		}
	}

	/// <exclude/>
	public class CRDuplicateContactsSelectorAttribute : PXCustomSelectorAttribute
	{
		private readonly Type SourceEntityID;

		public CRDuplicateContactsSelectorAttribute(Type sourceEntityID)
			: base(typeof(
				SelectFrom<
					Contact>
				.InnerJoin<CRDuplicateGrams>
					.On<CRDuplicateGrams.entityID.IsEqual<Contact.contactID>>
				.InnerJoin<CRGrams>
					.On<CRGrams.validationType.IsEqual<CRDuplicateGrams.validationType>
					.And<CRGrams.fieldName.IsEqual<CRDuplicateGrams.fieldName>
					.And<CRGrams.fieldValue.IsEqual<CRDuplicateGrams.fieldValue>>>>
				.LeftJoin<CRValidation>
					.On<CRValidation.type.IsEqual<CRGrams.validationType>>
				.Where<
					CRGrams.entityID.IsEqual<Contact.contactID.AsOptional>
					.And<CRDuplicateGrams.validationType.IsEqual<ValidationTypesAttribute.contactToContact>>>
				.AggregateTo<
					GroupBy<CRDuplicateGrams.entityID>,
					GroupBy<CRDuplicateGrams.validationType>,
					GroupBy<CRDuplicateGrams.entityID>,
					Sum<CRGrams.score>>
				.Having<
					CRGrams.score.Summarized.IsGreaterEqual<CRValidation.validationThreshold.Maximized>>
				.SearchFor<
					Contact.contactID>),

				fieldList: new[]
				{
					typeof(Contact.isActive),
					typeof(Contact.displayName),
					typeof(Contact.contactType),
					typeof(Contact.salutation),
					typeof(Contact.eMail),
					typeof(Contact.phone1)
				}
			)
		{
			DirtyRead = true;
			DescriptionField = typeof(Contact.displayName);
			this.SourceEntityID = sourceEntityID;
			ValidateValue = false;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			View = new PXView(_Graph, true, _Select);
		}

		private PXView View;

		public IEnumerable GetRecords()
		{
			PXCache sourceCache = _Graph.Caches[SourceEntityID.DeclaringType];
			var currentEntity = PXView.Currents?.FirstOrDefault() ?? sourceCache.Current;
			if (currentEntity == null)
				yield break;

			int? sourceID = (int?)sourceCache.GetValue(currentEntity, SourceEntityID.Name);
			var selectedRec = _Graph.Caches[typeof(CRDuplicateRecord)].Current as CRDuplicateRecord;

			foreach (PXResult rec in View.SelectMulti(sourceID))
			{
				Contact duplicateContact = rec.GetItem<Contact>();

				if (duplicateContact.ContactID == selectedRec.DuplicateContactID || duplicateContact.ContactID == sourceID)
					yield return duplicateContact;
			}
		}
	}

	/// <exclude/>
	public class CRDuplicateBAccountSelectorAttribute : PXCustomSelectorAttribute
	{
		private readonly Type SourceEntityID;

		public CRDuplicateBAccountSelectorAttribute(Type sourceEntityID)
			: base(typeof(
				SelectFrom<
					BAccountR>
				.InnerJoin<CRDuplicateGrams>
					.On<CRDuplicateGrams.entityID.IsEqual<BAccountR.defContactID>>
				.InnerJoin<CRGrams>
					.On<CRGrams.validationType.IsEqual<CRDuplicateGrams.validationType>
					.And<CRGrams.fieldName.IsEqual<CRDuplicateGrams.fieldName>
					.And<CRGrams.fieldValue.IsEqual<CRDuplicateGrams.fieldValue>>>>
				.LeftJoin<CRValidation>
					.On<CRValidation.type.IsEqual<CRGrams.validationType>>
				.Where<
					CRGrams.entityID.IsEqual<BAccountR.defContactID.AsOptional>
					.And<CRDuplicateGrams.validationType.IsEqual<ValidationTypesAttribute.accountToAccount>>>
				.AggregateTo<
					GroupBy<CRDuplicateGrams.entityID>,
					GroupBy<CRDuplicateGrams.validationType>,
					GroupBy<CRDuplicateGrams.entityID>,
					Sum<CRGrams.score>>
				.Having<
					CRGrams.score.Summarized.IsGreaterEqual<CRValidation.validationThreshold.Maximized>>
				.SearchFor<
					BAccountR.bAccountID>),

				fieldList: new[]
				{
					typeof(BAccountR.acctCD),
					typeof(BAccountR.acctName),
					typeof(BAccountR.status),
					typeof(BAccountR.type),
					typeof(BAccountR.classID)
				}
			)
		{
			this.SourceEntityID = sourceEntityID;

			DirtyRead = true;
			DescriptionField = typeof(BAccountR.acctName);
			SubstituteKey = typeof(BAccountR.acctCD);
			ValidateValue = false;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			View = new PXView(_Graph, true, _Select);
		}

		private PXView View;

		public IEnumerable GetRecords()
		{
			PXCache sourceCache = _Graph.Caches[SourceEntityID.DeclaringType];
			var currentEntity = PXView.Currents?.FirstOrDefault() ?? sourceCache.Current;
			if (currentEntity == null)
				yield break;

			int? sourceID = (int?)sourceCache.GetValue(currentEntity, SourceEntityID.Name);
			var selectedRec = _Graph.Caches[typeof(CRDuplicateRecord)].Current as CRDuplicateRecord;

			foreach (PXResult rec in View.SelectMulti(sourceID))
			{
				BAccountR duplicateAccount = rec.GetItem<BAccountR>();

				if (duplicateAccount.DefContactID == selectedRec.DuplicateContactID || duplicateAccount.DefContactID == sourceID)
					yield return duplicateAccount;
			}
		}
	}

	/// <summary>
	/// Attribute used to set <see cref="char"/> array as field value as duplicate validation delimiters.
	/// The default value is <see cref="DefaultDelimiters"/>.
	/// For customize list of available values attach new version of attribute with not empty construction with specified delimiters string.
	/// </summary>
	/// <example>
	/// This sample shows how to replace <see cref="DefaultDelimiters"/> with following chars: ' ', '.', ',', '^'.
	/// <code>
	/// public sealed class CRSetupDelimitersExt : PXCacheExtension&lt;CRSetup&gt;
	/// {
	///     [PXString]
	///     [PX.Objects.CR.Extensions.CRDuplicateEntities.CRCharsDelimiters(" .,^")]
	///     public string DuplicateCharsDelimiters { get; set; }
	/// }
	/// </code>
	/// </example>
	public class CRCharsDelimitersAttribute : PXEventSubscriberAttribute, IPXRowSelectingSubscriber
	{
		public const string DefaultDelimiters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ ";
		
		protected string Delimiters { get; set; }

		public CRCharsDelimitersAttribute() : this(DefaultDelimiters)
		{

		}

		public CRCharsDelimitersAttribute(string delimiters)
		{
			Delimiters = delimiters ?? throw new ArgumentNullException(nameof(delimiters));
		}

		public void RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			sender.SetValue(e.Row, FieldOrdinal, Delimiters);
		}
	}
}
