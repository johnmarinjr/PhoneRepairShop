using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.CR.Extensions.SideBySideComparison.Merge
{
	/// <summary>
	/// The filter that is used for merging of entities.
	/// </summary>
	[PXHidden]
	public class MergeEntitiesFilter : IBqlTable
	{
		/// <summary>
		/// Specifies the record that is used as a target.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <see cref="targetRecord.CurrentRecord"/>,
		/// <see cref="targetRecord.SelectedRecord"/>.
		/// </value>
		[PXInt]
		[PXIntList(
			new[]
			{
				targetRecord.CurrentRecord,
				targetRecord.SelectedRecord,
			},
			new[]
			{
				"Current Record",
				"Duplicate Record",
			})]
		[PXUnboundDefault(targetRecord.CurrentRecord)]
		[PXUIField(DisplayName = "Target Record")]
		public int? TargetRecord { get; set; }
		public abstract class targetRecord : BqlString.Field<targetRecord>
		{
			public const int CurrentRecord = 0;
			public const int SelectedRecord = 1;
		}

		/// <summary>
		/// The caption under the grid with extra information.
		/// </summary>
		/// <remarks>
		/// Can be made hidden.
		/// </remarks>
		[PXString(IsUnicode = true)]
		[PXUnboundDefault("Select the field values that you want to keep in the target record.")]
		[PXUIField(Enabled = false)]
		public string Caption { get; set; }
		public abstract class caption : BqlString.Field<caption> { }

		/// <summary>
		/// The ID of the entity that is used for merging.
		/// </summary>
		/// <value>
		/// Corresponds to the real ID of the entity, such as
		/// <see cref="BAccount.BAccountID"/> or <see cref="Contact.ContactID"/>.
		/// </value>
		[PXString]
		[PXUIField(DisplayName = "", Visible = false, Enabled = false)]
		public string MergeEntityID { get; set; }
		public abstract class mergeEntityID : BqlString.Field<mergeEntityID> { }
	}
}
