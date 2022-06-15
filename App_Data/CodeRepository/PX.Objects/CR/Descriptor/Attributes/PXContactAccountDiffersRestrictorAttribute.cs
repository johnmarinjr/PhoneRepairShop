using System;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.IN;
using PX.Objects.IN.Attributes;

namespace PX.Objects.CR
{
	public class PXContactAccountDiffersRestrictorAttribute : RestrictorWithParametersAttribute
	{
		public PXContactAccountDiffersRestrictorAttribute(Type where, params Type[] messageParameters)
			: base(where, "", messageParameters) { }

		public override object[] GetMessageParameters(PXCache sender, object itemres, object row)
		{
			var contact = PXResult.Unwrap<Contact>(itemres);

			if (contact != null && contact.ContactType == ContactTypesAttribute.Employee)
			{
				_Message = Messages.ContactBAccountDifferForEmployee;
			}
			else
			{
				_Message = Messages.ContractBAccountDiffer;
			}

			return base.GetMessageParameters(sender, itemres, row);
		}
	}
}
