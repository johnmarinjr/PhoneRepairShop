using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Data.MassProcess;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.CR
{
	[Obsolete("Use " + nameof(CRValidationProcess))]
	[Serializable]
	public class CRLeadContactValidationProcess : PXGraph<CRLeadContactValidationProcess>
	{
		#region DACs

		[Serializable]
		[PXHidden]
		public partial class ValidationFilter : IBqlTable
		{
			#region ValidationType
			public abstract class validationType : PX.Data.BQL.BqlShort.Field<validationType> { }

			[PXDBShort()]
			[PXDefault((short)0)]
			[PXUIField(DisplayName = "Validation Type")]
			public virtual Int16? ValidationType { get; set; }
			#endregion
		}

		#endregion

		public PXCancel<ValidationFilter> Cancel;

		public CRLeadContactValidationProcess()
		{
			Actions["Process"].SetVisible(false);
			Actions.Move("Process", "Cancel");
			var setup = Setup.Current;
			PXUIFieldAttribute.SetDisplayName<Contact.displayName>(this.Caches[typeof(Contact)], Messages.Contact);
			PXUIFieldAttribute.SetDisplayName<Contact2.displayName>(this.Caches[typeof(Contact2)], Messages.PossibleDuplicated);

			var rules = PXSelect<CRValidationRules>.Select(this);
			if (rules == null || rules.Count == 0)
				throw new PXSetupNotEnteredException(Messages.DuplicateValidationRulesAreEmpty, typeof(CRValidation), typeof(CRValidation).Name);

			Contacts.ParallelProcessingOptions =
				settings =>
				{
					settings.IsEnabled = true;
				};
		}
		public void ValidationFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			ValidationFilter filter = (ValidationFilter)e.Row;
			Contacts.SetProcessDelegate((PXGraph graph, ContactAccount record) => ProcessValidation(graph, record, filter));
		}

		private static void ProcessValidation(PXGraph graph, Contact record, ValidationFilter Filter)
		{
			Type itemType = record.GetType();
			PXCache cache = PXGraph.CreateInstance<PXGraph>().Caches[itemType];
			Type graphType;
			object copy = cache.CreateCopy(record);
			PXPrimaryGraphAttribute.FindPrimaryGraph(cache, ref copy, out graphType);

			if (graphType == null)
				throw new PXException(Messages.UnableToFindGraph);

			graph = PXGraph.CreateInstance(graphType);


			graph.Views[graph.PrimaryView].Cache.Current = copy;

			var entity = CRDuplicateEntities<PXGraph, Contact>.RunActionWithAppliedSearch(
				graph,
				copy,
				nameof(CRDuplicateEntities<PXGraph, Contact>.CheckForDuplicates)) as Contact;

			record.DuplicateFound = entity?.DuplicateFound;
			record.DuplicateStatus = entity?.DuplicateStatus;
		}

		public PXSetupSelect<CRSetup> Setup;

		public PXFilter<ValidationFilter> Filter;

		[PXViewDetailsButton(typeof(ValidationFilter),
			typeof(Select<Contact,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>))]
		[PXViewDetailsButton(typeof(ValidationFilter),
			typeof(Select2<BAccount,
				InnerJoin<Contact, On<BAccount.bAccountID, Equal<Contact.bAccountID>>>,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>))]
		public PXFilteredProcessing<
				ContactAccount,
				ValidationFilter,
				Where2<
					Where<Current<ValidationFilter.validationType>, Equal<True>,
						Or<ContactAccount.duplicateStatus, Equal<DuplicateStatusAttribute.notValidated>>>,
					And<ContactAccount.isActive, Equal<True>,
					And<Where<ContactAccount.contactType, Equal<ContactTypesAttribute.lead>,
						Or<ContactAccount.contactType, Equal<ContactTypesAttribute.person>>>>>>>
			Contacts;
	}
}
