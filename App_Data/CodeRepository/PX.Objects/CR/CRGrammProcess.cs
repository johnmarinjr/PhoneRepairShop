using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Common;
using PX.Common.Extensions;
using PX.Data;
using PX.Objects.CS;
using PX.SM;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.CR
{
	[Serializable]
	public class CRGrammProcess : PXGraph<CRGrammProcess>
	{
		public PXCancel<Contact> Cancel;
		#region Processor

		[PXInternalUseOnly]
		[Obsolete("Use " + nameof(CRGramProcessorBase))]
		public class Processor : CRGramProcessorBase
		{
			public Processor()
			{
			}

			public Processor(PXGraph graph) : base(graph)
			{
			}
		}

		#endregion

		[PXHidden] 
		public PXSelect<BAccount> baccount;

		[PXViewDetailsButton(typeof(Contact))]
		[PXViewDetailsButton(typeof(Contact),
			typeof(Select<BAccount,
				Where<BAccount.bAccountID, Equal<Current<Contact.bAccountID>>>>))]
		public SelectFrom<Contact>
			.InnerJoin<CRGramValidationDateTime.ByLead>.On<True.IsEqual<True>>
			.InnerJoin<CRGramValidationDateTime.ByContact>.On<True.IsEqual<True>>
			.InnerJoin<CRGramValidationDateTime.ByBAccount>.On<True.IsEqual<True>>
			.LeftJoin<BAccount>
				.On<BAccount.defContactID.IsEqual<Contact.contactID>
				.And<BAccount.bAccountID.IsEqual<Contact.bAccountID>>>
			.Where<
				Brackets<
					Contact.contactType.IsEqual<ContactTypesAttribute.lead>
						.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByLead.value>>
					.Or<
						Contact.contactType.IsEqual<ContactTypesAttribute.person>
						.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByContact.value>>
					>
					.Or<
						Contact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
						.And<Contact.grammValidationDateTime.IsLess<CRGramValidationDateTime.ByBAccount.value>>
					>
				>
				.And<
					BAccount.bAccountID.IsNull
					.And<Contact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>>
					.Or<BAccount.type.IsIn<
							BAccountType.prospectType,
							BAccountType.customerType,
							BAccountType.vendorType,
							BAccountType.combinedType,
							BAccountType.empCombinedType
					>>
				>
			>
			.ProcessingView
			Items;

		public PXSetup<CRSetup> Setup;

		#region Ctors

		public CRGrammProcess()
		{
			// Acuminator disable once PX1057 PXGraphCreationDuringInitialization [legacy, not sure if it could be safely replaced with (this)]
			var processor = new CRGramProcessorBase();

			if (!processor.IsRulesDefined)
				throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(CRSetup), typeof(CRSetup).Name);

			Items.Cache.AllowInsert = true;

			PXUIFieldAttribute.SetDisplayName<Contact.displayName>(Items.Cache, Messages.Contact);

			Items.SetProcessDelegate((PXGraph graph, Contact contact) =>
			{
				PersistGrams(graph, contact);
			});

			Items.ParallelProcessingOptions =
				settings =>
				{
					settings.IsEnabled = true;
				};

			Items.SuppressMerge = true;
			Items.SuppressUpdate = true;
		}

		#endregion	

		public static bool PersistGrams(PXGraph graph, Contact contact)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.contactDuplicate>()) return false;
			if (contact != null && contact.ContactID > 1)
			{
				var processor = new CRGramProcessorBase(graph);
				return processor.PersistGrams(contact);
			}
			return false;
		}
	}
}
