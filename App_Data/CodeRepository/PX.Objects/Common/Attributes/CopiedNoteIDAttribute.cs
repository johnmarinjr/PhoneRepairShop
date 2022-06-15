using PX.Data;
using System;

namespace PX.Objects.Common.Attributes
{
	/// <exclude/>
	public class CopiedNoteIDAttribute : PXNoteAttribute
	{
		protected Type _entityType;

		public CopiedNoteIDAttribute(Type entityType)
		{
			_entityType = entityType;
			_ForceRetain = true;
		}


		[Obsolete("The constructor is obsolete. Use the constructor without \"searches\" parameter. " +
				  "This " + nameof(CopiedNoteIDAttribute) + " constructor is exactly the same. " +
				  "It does not provide any additional functionality and does not save values of provided fields in the note. " +
				  "The constructor will be removed in a future version of Acumatica ERP.")]
		public CopiedNoteIDAttribute(Type entityType, params Type[] searches)
			: base(searches)
		{
			_entityType = entityType;
			_ForceRetain = true;
		}

		protected override string GetEntityType(PXCache cache, Guid? noteId)
			=> _entityType.FullName;
	}
}
