using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.CR
{
	// real type doesn't matter
	using CRDuplicateEntities = CRDuplicateEntities<PXGraph, BAccount>;
	public class CRValidationProcess : PXGraph<CRValidationProcess>
	{
		#region ValidationFilter
		[PXHidden]
		public partial class ValidationFilter : IBqlTable
		{
			public const int ValidateNewAndUpdated = 0;
			public class validateNewAndUpdated : BqlInt.Constant<validateNewAndUpdated>
			{
				public validateNewAndUpdated() : base(ValidateNewAndUpdated) { }
			}
			public const int ValidateAll = 1;
			public class validateAll : BqlInt.Constant<validateAll>
			{
				public validateAll() : base(ValidateAll) { }
			}
			#region ValidationType
			[PXInt]
			[PXDefault(ValidateNewAndUpdated)]
			[PXIntList(
				new[] { ValidateNewAndUpdated, ValidateAll },
				new[] { "Validate Only New and Updated Records", "Validate All Records" })]
			[PXUIField(DisplayName = "Validation Type")]
			public virtual int? ValidationType { get; set; }
			public abstract class validationType : BqlInt.Field<validationType> { }
			#endregion
		}
		#endregion

		#region Selects
		public PXSetupSelect<CRSetup> Setup;
		public PXFilter<ValidationFilter> Filter;

		[PXViewDetailsButton(typeof(ValidationFilter),
			typeof(Select2<BAccount,
				InnerJoin<Contact, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>),
			WindowMode = PXRedirectHelper.WindowMode.New)]
		[PXViewDetailsButton(typeof(ValidationFilter),
			typeof(Select<Contact,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>),
			WindowMode = PXRedirectHelper.WindowMode.New)]
		public SelectFrom<ContactAccountLead>
			.Where<
				Brackets<
					Brackets<
							ContactAccountLead.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
						.And<ContactAccountLead.type.IsNotIn<BAccountType.branchType, BAccountType.organizationType>>
						.And<ContactAccountLead.defContactID.IsEqual<Contact.contactID>>
						.And<ContactAccountLead.accountStatus.IsNotEqual<CustomerStatus.inactive>>
					>
					.Or<Brackets<
							ContactAccountLead.contactType.IsIn<
								ContactTypesAttribute.lead,
								ContactTypesAttribute.person>
						.And<ContactAccountLead.isActive.IsEqual<True>>
					>>
				>
				.And<Brackets<
						ValidationFilter.validationType.FromCurrent.IsEqual<ValidationFilter.validateAll>
					.Or<ContactAccountLead.duplicateStatus.IsEqual<DuplicateStatusAttribute.notValidated>>
				>>
			>
			.ProcessingView
			.FilteredBy<ValidationFilter>
			Contacts;

		#endregion

		// existance of cancel is required to avoid defaulting of radiobutton on the UI after processing...
		public PXCancel<ValidationFilter> Cancel;


		public CRValidationProcess()
		{
			Actions["Process"].SetVisible(false);
			Cancel.SetVisible(false);
			Actions.Move("Process", "Cancel");

			if (Setup.Current == null)
				throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(CRValidation), typeof(CRValidation).Name);

			Contacts.ParallelProcessingOptions =
				settings =>
				{
					settings.IsEnabled = true;
				};
		}

		public virtual void _(Events.RowSelected<ValidationFilter> e)
		{
			var row = e.Row;
			Contacts.SetProcessDelegate(ProcessValidation);
		}

		private static void ProcessValidation(List<ContactAccountLead> list)
		{
			PXCache cache = PXGraph.CreateInstance<PXGraph>().Caches[typeof(ContactAccountLead)];
			// cached graphs
			var graphs = new Dictionary<Type, PXGraph>();
			var items = list.Select(i =>
			{
				var copy = cache.CreateCopy(i);
				PXPrimaryGraphAttribute.FindPrimaryGraph(cache, ref copy, out var graphType);
				if (!graphs.TryGetValue(graphType, out var graph))
					graphs.Add(graphType, graph = PXGraph.CreateInstance(graphType));
				return (graph, i, copy);
			});

			int counter = 0;
			foreach (var (graph, item, copy) in items)
			{
				PXProcessing.SetCurrentItem(item);
				graph.Views[graph.PrimaryView].Cache.Current = copy;
				try
				{
					// type doesn't matter
					var result = CRDuplicateEntities.RunActionWithAppliedSearch(
						graph, copy, nameof(CRDuplicateEntities.CheckForDuplicates));

					var resultContact = PXResult.Unwrap<Contact>(result);
					if(resultContact != null)
					{
						item.DuplicateFound = resultContact.DuplicateFound;
						item.DuplicateStatus = resultContact.DuplicateStatus;
						PXProcessing.SetProcessed<ContactAccountLead>();
					}
					else
					{
						PXProcessing.SetError<ContactAccountLead>(counter, MessagesNoPrefix.CannotValidateItemForDuplicates);
					}
				}
				catch (Exception e)
				{
					PXProcessing.SetError<ContactAccountLead>(counter, e);
				}
				graph.Clear();
				counter++;
			}
		}
	}
}
