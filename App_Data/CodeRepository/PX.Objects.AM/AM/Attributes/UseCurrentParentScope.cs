using PX.Data;
using PX.Objects.Common;

namespace PX.Objects.AM.Attributes
{
	/// <summary>
	/// Sets <see cref="PXParentAttribute"/> <see cref="PXParentAttribute.UseCurrent"/> which helps avoid a trip to the database when a process is running bulk insert and parent is still only in cache.
	/// Helpful for performance reasons for large sets of data being processed.
	/// </summary>
	public class UseCurrentParentScope : OverrideAttributePropertyScope<PXParentAttribute, bool>
	{
		/// <summary>
		/// Sets <see cref="PXParentAttribute"/> <see cref="PXParentAttribute.UseCurrent"/> which helps avoid a trip to the database when a process is running bulk insert and parent is still only in cache.
		/// Helpful for performance reasons for large sets of data being processed.
		/// </summary>
		/// <param name="cache">Cache containing PXParent fields to set UseCurrent</param>
		/// <param name="useCurrentValue"> Mapped to <see cref="PXParentAttribute.UseCurrent"/> </param>
		/// <param name="fields">Field(s) which contain PXParentAttribute. Leave null to find all and any fields with PXParent</param>
		public UseCurrentParentScope(PXCache cache, bool useCurrentValue, params System.Type[] fields)
			: base(cache,
				fields,
				(attribute, ensureNewNoteIDOnly) => attribute.UseCurrent = ensureNewNoteIDOnly,
				attribute => attribute.UseCurrent,
				useCurrentValue) { }
	}
}
