using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.CR.Extensions.SideBySideComparison.Link
{
	/// <summary>
	/// The filter that is used for linking entities.
	/// </summary>
	[PXHidden]
	public class LinkFilter : IBqlTable
	{
		/// <summary>
		/// Specifies (if set to <see langword="true"/>) that the link should be processed.
		/// </summary>
		[PXBool]
		[PXUnboundDefault(true)]
		[PXUIField(DisplayName = "Process")]
		public bool? ProcessLink { get; set; }
		public abstract class processLink : BqlBool.Field<processLink> { }

		/// <summary>
		/// The caption under the grid with extra information.
		/// </summary>
		/// <remarks>
		/// Can be made hidden.
		/// </remarks>
		[PXString(IsUnicode = true)]
		[PXUnboundDefault("Select the field values that you want to use for the contact.")]
		[PXUIField(Enabled = false)]
		public string Caption { get; set; }
		public abstract class caption : BqlString.Field<caption> { }

		/// <summary>
		/// The ID of the entity that is used for linking.
		/// </summary>
		/// <value>
		/// Corresponds to the real ID of the entity, such as
		/// <see cref="BAccount.BAccountID"/> or <see cref="Contact.ContactID"/>.
		/// </value>
		[PXString]
		[PXUIField(DisplayName = "", Visible = false, Enabled = false)]
		public string LinkedEntityID { get; set; }
		public abstract class linkedEntityID : BqlString.Field<linkedEntityID> { }
	}
}
