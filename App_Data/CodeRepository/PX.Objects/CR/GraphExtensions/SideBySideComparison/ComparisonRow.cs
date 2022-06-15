using System;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR.Extensions.SideBySideComparison.Link;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;

namespace PX.Objects.CR.Extensions.SideBySideComparison
{
	/// <summary>
	/// The selection that shows which entity field's value is selected.
	/// </summary>
	public enum ComparisonSelection
	{
		/// <summary>
		/// The value is not selected.
		/// </summary>
		/// <remarks>The <see langword="null"/> value is used as a result value.</remarks>
		None = 0,
		/// <summary>
		/// The left entity field's value is selected.
		/// </summary>
		Left = 1,
		/// <summary>
		/// The right entity field's value is selected.
		/// </summary>
		Right = 2,
	}

	/// <summary>
	/// The base class that represents a comparison between the values of the same field of two <see cref="IBqlTable"/>.
	/// The class is used to select the value from one or another field.
	/// </summary>
	/// <remarks>
	/// The child classes, which are <see cref="LinkComparisonRow"/> and <see cref="MergeComparisonRow"/>,
	/// are used in <see cref="LinkEntitiesExt{TGraph,TMain,TFilter}"/> and <see cref="MergeEntitiesExt{TGraph,TMain}"/>
	/// to show the comparison between the left and right entities.
	/// </remarks>
	[PXHidden]
	public class ComparisonRow : IBqlTable
	{
		#region ItemType

		/// <summary>
		/// The type of <see cref="IBqlTable"/>.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to <see cref="Type.FullName"/> of the related <see cref="IBqlTable"/> type.
		/// </value>
		[PXString(IsKey = true)]
		[PXUIField(DisplayName = "Item Type", Visible = false)]
		public virtual string ItemType { get; set; }
		public abstract class itemType : BqlString.Field<itemType> { }

		#endregion

		#region FieldName

		/// <summary>
		/// The name of the field of <see cref="IBqlTable"/>.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to <see langword="nameof"/> of the field property.
		/// </value>
		[PXString(IsKey = true)]
		[PXUIField(DisplayName = "Field", Enabled = false)]
		public virtual string FieldName { get; set; }
		public abstract class fieldName : BqlString.Field<fieldName> { }

		#endregion

		#region LeftHashCode

		/// <summary>
		/// The hash code (<see cref="PXCache.GetObjectHashCode"/>) of the left <see cref="IBqlTable"/>.
		/// </summary>
		[PXInt(IsKey = true)]
		[PXUIField(DisplayName = "", Visible = false)]
		public virtual int? LeftHashCode { get; set; }
		public abstract class leftHashCode : BqlString.Field<leftHashCode> { }

		#endregion

		#region RightHashCode
		
		/// <summary>
		/// The hash code (<see cref="PXCache.GetObjectHashCode"/>) of the right <see cref="IBqlTable"/>.
		/// </summary>
		[PXInt(IsKey = true)]
		[PXUIField(DisplayName = "", Visible = false)]
		public virtual int? RightHashCode { get; set; }
		public abstract class rightHashCode : BqlString.Field<rightHashCode> { }

		#endregion

		#region FieldDisplayName

		/// <summary>
		/// The display name of the <see cref="FieldName"/> field of <see cref="IBqlTable"/>.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to <see cref="PXFieldState.DisplayName"/>
		/// for <see cref="FieldName"/>.
		/// </value>
		[PXString]
		[PXUIField(DisplayName = "Field", Enabled = false)]
		public virtual string FieldDisplayName { get; set; }
		public abstract class fieldDisplayName : BqlString.Field<fieldDisplayName> { }

		#endregion

		#region Order

		/// <summary>
		/// The order of the current row in the view.
		/// </summary>
		[PXInt]
		[PXUIField(DisplayName = "", Visible = false, Enabled = false)]
		public virtual int? Order { get; set; }
		public abstract class order : BqlInt.Field<order> { }

		#endregion

		#region Hidden

		/// <summary>
		/// Specifies (if set to <see langword="true"/>) that this row is used in processing
		/// but should not be displayed in the UI.
		/// </summary>
		[PXBool]
		[PXUIField(DisplayName = "", Visible = false, Enabled = false)]
		public virtual bool? Hidden { get; set; }
		public abstract class hidden : BqlBool.Field<hidden> { }

		#endregion

		#region EnableNoneSelection

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that this row allows
		/// <see cref="Selection"/> to have the <see cref="ComparisonSelection.None"/> value.
		/// </summary>
		/// <value>
		/// The value of this field is set during the <see cref="ComparisonRow"/> creation
		/// in <see cref="CompareEntitiesExt{TGraph,TMain,TComparisonRow}"/> or in it's child classes.
		/// </value>
		[PXBool]
		[PXUIField(DisplayName = "", Enabled = false, Visible = false)]
		public virtual bool? EnableNoneSelection { get; set; }
		public abstract class enableNoneSelection : BqlBool.Field<enableNoneSelection> { }

		#endregion

		#region Selection

		/// <summary>
		/// The raw (<see cref="int"/>) version of <see cref="Selection"/>.
		/// </summary>
		/// <remarks>
		/// Use the <see cref="Selection"/> field in code unless you need it in attributes.
		/// </remarks>
		[PXInt]
		[PXUIField(DisplayName = "", Enabled = false, Visible = false)]
		public virtual int? SelectionRaw { get; set; }
		public abstract class selectionRaw : BqlInt.Field<selectionRaw> { }

		/// <summary>
		/// Specifies the selection for the current row.
		/// It shows which <see cref="IBqlTable"/> field value (left or right) should be used
		/// or is <see langword="null"/> if <see cref="EnableNoneSelection"/> is set to <see langword="true"/>.
		/// </summary>
		/// <remarks>
		/// For attributes, use <see cref="SelectionRaw"/>.
		/// </remarks>
		public virtual ComparisonSelection Selection
		{
			get => (ComparisonSelection)SelectionRaw.GetValueOrDefault();
			set => SelectionRaw = (int)value;
		}

		#endregion

		#region LeftValueSelected

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the left <see cref="IBqlTable"/>'s field value
		/// should be used.
		/// </summary>
		/// <value>
		/// Returns <see langword="true"/> if <see cref="Selection"/> is set to <see cref="ComparisonSelection.Left"/>
		/// and <see langword="false"/> otherwise.
		/// The behavior of the set accessor depends on <see cref="EnableNoneSelection"/>.
		/// </value>
		[PXBool]
		[PXUIField(DisplayName = "")]
		[PXDependsOnFields(typeof(selectionRaw))]
		public virtual bool? LeftValueSelected
		{
			get => Selection == ComparisonSelection.Left;
			set
			{
				if (Selection == ComparisonSelection.Left
					&& value is false
					&& EnableNoneSelection is true)
					Selection = ComparisonSelection.None;
				else
					Selection = value is true ? ComparisonSelection.Left : ComparisonSelection.Right;
			}
		}
		public abstract class leftValueSelected : BqlBool.Field<leftValueSelected> { }

		#endregion

		#region LeftValue

		/// <summary>
		/// The value of the <see cref="FieldName"/> field of the left <see cref="IBqlTable"/>.
		/// </summary>
		/// <value>
		/// The internal value of the field.
		/// </value>
		[PXString]
		[PXUIField(DisplayName = "", Enabled = false)]
		public virtual string LeftValue { get; set; }
		public abstract class leftValue : BqlString.Field<leftValue> { }

		// Acuminator disable once PX1026 UnderscoresInDacDeclaration mocking for description field
		/// <summary>
		/// The display name for the <see cref="FieldName"/> field if it is a selector
		/// (see <see cref="PXSelectorAttribute"/>) for the left <see cref="IBqlTable"/>.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		[PXString]
		public virtual string LeftValue_description { get; set; }

		#endregion

		#region RightValueSelected

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the right <see cref="IBqlTable"/>'s field value
		/// should be used.
		/// </summary>
		/// <value>
		/// Returns <see langword="true"/> if <see cref="Selection"/> is set to <see cref="ComparisonSelection.Right"/>
		/// and <see langword="false"/> otherwise.
		/// The behavior of the set accessor depends on <see cref="EnableNoneSelection"/>.
		/// </value>
		[PXBool]
		[PXUIField(DisplayName = "")]
		[PXDependsOnFields(typeof(selectionRaw))]
		public virtual bool? RightValueSelected
		{
			get => Selection == ComparisonSelection.Right;
			set
			{
				if (Selection == ComparisonSelection.Right
					&& value is false
					&& EnableNoneSelection is true)
					Selection = ComparisonSelection.None;
				else
					Selection = value is true ? ComparisonSelection.Right : ComparisonSelection.Left;
			}
		}
		public abstract class rightValueSelected : BqlBool.Field<rightValueSelected> { }

		#endregion

		#region RightValue

		/// <summary>
		/// The value of the <see cref="FieldName"/> field of the right <see cref="IBqlTable"/>.
		/// </summary>
		/// <value>
		/// The internal value of the field.
		/// </value>
		[PXString]
		[PXUIField(DisplayName = "", Enabled = false)]
		public virtual string RightValue { get; set; }
		public abstract class rightValue : BqlString.Field<rightValue> { }

		// Acuminator disable once PX1026 UnderscoresInDacDeclaration mocking for description field
		/// <summary>
		/// The display name for the <see cref="FieldName"/> field if it is a selector
		/// (see <see cref="PXSelectorAttribute"/>) for the right <see cref="IBqlTable"/>.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		[PXString]
		public virtual string RightValue_description { get; set; }

		#endregion

		#region Intermediate props

		/// <summary>
		/// An intermediate property for the cache of the left <see cref="IBqlTable"/>.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		public PXCache LeftCache { get; set; }
		/// <summary>
		/// An intermediate property for the cache of the right <see cref="IBqlTable"/>.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		public PXCache RightCache { get; set; }
		/// <summary>
		/// An intermediate property for the field state of the left <see cref="IBqlTable"/>'s field.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		public PXFieldState LeftFieldState { get; set; }
		/// <summary>
		/// An intermediate property for the field state of the right <see cref="IBqlTable"/>'s field.
		/// </summary>
		/// <remarks>
		/// Do not use directly. The property is for internal use only.
		/// </remarks>
		public PXFieldState RightFieldState { get; set; }

		#endregion

		#region ToString

		// Acuminator disable once PX1031 InstanceMethodInDac [to string]
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(GetType().Name);
			if (string.IsNullOrEmpty(ItemType))
				sb.Append(" <!>");
			else
			{
				sb.Append(" | ");
				if (Hidden is true)
					sb.Append(" Hidden | ");
				sb.Append(ItemType.Split('.').Last());
				sb.Append(".");
				sb.Append(FieldName);
				sb.Append(": ");
				sb.Append(getBox(LeftValueSelected));
				sb.Append(getValue(LeftValue));
				sb.Append("<->");
				sb.Append(getBox(RightValueSelected));
				sb.Append(getValue(RightValue));
			}

			return sb.ToString();
			string getBox(bool? value) => value is true ? " [X] " : " [ ] ";
			string getValue(string value)
			{
				if (string.IsNullOrWhiteSpace(value))
					return " {} ";
				return " {" + value + "} ";
			}
		}

		#endregion
	}
}
