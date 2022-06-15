using PX.CS;
using PX.Data;

namespace PX.Objects.CR.Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class CSAttributeMaint2_Activity : PXGraphExtension<CSAttributeMaint2>
	{
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Activity Type")]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public virtual void _(Events.CacheAttached<CRActivity.type> e) { }
	}
}