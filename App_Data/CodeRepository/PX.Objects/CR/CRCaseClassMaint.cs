using System;
using PX.Data;
using PX.Objects.CT;

namespace PX.Objects.CR
{
	public class CRCaseClassMaint : PXGraph<CRCaseClassMaint, CRCaseClass>
	{
		#region Selects

		[PXViewName(Messages.CaseClass)]
		public PXSelect<CRCaseClass>
			CaseClasses;

		[PXHidden]
		public PXSelect<CRCaseClass, 
			Where<CRCaseClass.caseClassID, Equal<Current<CRCaseClass.caseClassID>>>> 
			CaseClassesCurrent;

		[PXViewName(Messages.CaseClassReaction)]
		public PXSelect<CRClassSeverityTime, 
			Where<CRClassSeverityTime.caseClassID, Equal<Current<CRCaseClass.caseClassID>>>> 
			CaseClassesReaction;

        [PXViewName(Messages.Attributes)]
        public CSAttributeGroupList<CRCaseClass, CRCase> Mapping;

        [PXHidden]
		public PXSelect<CRSetup> 
			Setup;

		public PXSelect<CRCaseClassLaborMatrix, Where<CRCaseClassLaborMatrix.caseClassID, Equal<Current<CRCaseClass.caseClassID>>>>
			LaborMatrix;

		#endregion

		#region Events

		#region CacheAttached

		[PXDBInt(MinValue = 0, MaxValue = 1440)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRCaseClass.reopenCaseTimeInDays> e) { }

		[PXDBString(10, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(CRCaseClass.caseClassID))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void _(Events.CacheAttached<CRClassSeverityTime.caseClassID> e) { }

		[PXDefault(false)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<IN.InventoryItem.stkItem> e) { }

		#endregion

		protected virtual void _(Events.FieldVerifying<CRCaseClass, CRCaseClass.perItemBilling> e)
        {
            CRCaseClass row = e.Row as CRCaseClass;
            if (row == null) return;

            CRCase crCase = PXSelect<CRCase, Where<CRCase.caseClassID, Equal<Required<CRCaseClass.caseClassID>>, 
                                And<CRCase.isBillable, Equal<True>,
                                And<CRCase.released, Equal<False>>>>>.SelectWindowed(this, 0, 1, row.CaseClassID);

            if (crCase != null)
            {
                throw new PXSetPropertyException(Messages.CurrentClassHasUnreleasedRelatedCases, PXErrorLevel.Error);
            }
        }

		protected virtual void _(Events.RowInserted<CRCaseClass> e)
		{
			var row = e.Row as CRCaseClass;
			if (row == null) return;

			if (row.IsBillable == true)
			{
				row.RequireCustomer = true;
			}
		}

		protected virtual void _(Events.RowSelected<CRCaseClass> e)
		{
			var row = e.Row as CRCaseClass;
			if (row == null) return;

			Delete.SetEnabled(NoCaseExistsForClass(row));
		}

		protected virtual void _(Events.RowDeleted<CRCaseClass> e)
		{
			var row = e.Row as CRCaseClass;
			if (row == null) return;

			CRSetup s = Setup.Select();

			if (s != null && s.DefaultCaseClassID == row.CaseClassID)
			{
				s.DefaultCaseClassID = null;
				Setup.Update(s);
			}
		}

		protected virtual void _(Events.RowDeleting<CRCaseClass> e)
		{
			var row = e.Row as CRCaseClass;
			if (row == null) return;
			
			if (!NoCaseExistsForClass(row))
			{
				throw new PXException(Messages.RecordIsReferenced);
			}
		}

		protected virtual void _(Events.RowPersisting<CRCaseClass> e)
		{
			var row = e.Row as CRCaseClass;

			if (row == null || e.Operation == PXDBOperation.Delete)
				return;

			var currentAllowValue = e.Cache.GetValue<CRCaseClass.allowEmployeeAsContact>(row);
			var oldAllowValue = e.Cache.GetValueOriginal<CRCaseClass.allowEmployeeAsContact>(row);

			if (object.Equals(currentAllowValue, oldAllowValue) || row.AllowEmployeeAsContact == true)
				return;

			if (!NoEmployeeCaseExistsForClass(row))
			{
				throw new PXException(Messages.CaseClassOfEmployeeCase);
			}
		}

		#endregion

		protected virtual bool NoCaseExistsForClass(CRCaseClass row)
		{
			if (row == null)
				return true;

			CRCase c = PXSelect<
					CRCase,
				Where<
					CRCase.caseClassID, Equal<Required<CRCase.caseClassID>>>>
				.SelectWindowed(this, 0, 1, row.CaseClassID);

			return c == null;
		}

		protected virtual bool NoEmployeeCaseExistsForClass(CRCaseClass row)
		{
			if (row == null)
				return true;

			CRCase c = PXSelectJoin<
					CRCase,
				InnerJoin<Contact,
					On<Contact.contactID, Equal<CRCase.contactID>,
					And<Contact.contactType, Equal<ContactTypesAttribute.employee>>>>,
				Where<
					CRCase.caseClassID, Equal<Required<CRCase.caseClassID>>>>
				.SelectWindowed(this, 0, 1, row.CaseClassID);

			return c == null;
		}
	}
}
