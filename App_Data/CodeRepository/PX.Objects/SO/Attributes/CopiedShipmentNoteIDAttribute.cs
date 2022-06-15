using PX.Data;
using PX.Objects.Common.Attributes;
using System;

namespace PX.Objects.SO.Attributes
{
	public class CopiedShipmentNoteIDAttribute : CopiedNoteIDAttribute
	{
		public CopiedShipmentNoteIDAttribute() : base(entityType: null)
		{
		}

		[Obsolete("The constructor is obsolete. Use the parameterless constructor instead. " +
				  "The " + nameof(CopiedShipmentNoteIDAttribute) + " constructor is exactly the same as the parameterless one. " +
				  "It does not provide any additional functionality and does not save values of provided fields in the note. " +
				  "The constructor will be removed in a future version of Acumatica ERP.")]
		public CopiedShipmentNoteIDAttribute(params Type[] searches)
			: base(null, searches)
		{
		}

		protected override string GetEntityType(PXCache cache, Guid? noteId)
		{
			var cmd = new PXSelect<SOOrderShipment,
				Where<SOOrderShipment.shippingRefNoteID, Equal<Required<Note.noteID>>>>(cache.Graph);
			SOOrderShipment orderShipment = cmd.Select(noteId);
			if (orderShipment != null)
			{
				ShippingRefNoteAttribute.GetTargetTypeAndKeys(cmd.Cache, orderShipment, out Type targetType, out object[] targetKeys);
				if (targetType != null)
					return targetType.FullName;
			}
			return cache.GetItemType().FullName;
		}
	}
}
