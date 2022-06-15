using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.SQLTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR
{
	/// <summary>
	/// Combines all <see cref="PXStringListAttribute"/> from specified fields.
	/// For proper work use <see cref="CoalesceConcatValues(string[])"/>
	/// or child attribute with custom db query: <see cref="CombinedDBStringListsAttribute"/>.
	/// First not null value will be used, so order is important.
	/// </summary>
	public class CombinedStringListsAttribute : PXStringListAttribute
	{
		public const string ListsPrefix = "L__";

		public CombinedStringListsAttribute(params Type[] fields)
		{
			Fields = fields.ToList();
			// it uses already localized values from referenced lists
			IsLocalizable = false;
		}

		protected List<Type> Fields { get; }

		protected static string GetValueWithPrefix(string value, int fieldIndex)
		{
			return GetPrefix(fieldIndex) + value;
		}

		public static string GetPrefix(int fieldIndex)
		{
			return fieldIndex + ListsPrefix;
		}

		public static string CoalesceConcatValues(params string[] values)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] != null)
					return GetValueWithPrefix(values[i], i);
			}

			return null;
		}

		public override void CacheAttached(PXCache sender)
		{
			var lists = Fields.Select(f => (f.Name, Attributes: sender.GetAttributesOfType<PXStringListAttribute>(null, f.Name)));

			var values = lists.SelectMany((l, index) => GetValues(l.Name, l.Attributes, index));

			_AllowedValues = values.Select(f => f.value).ToArray();
			_AllowedLabels = values.Select(f => f.label).ToArray();

			base.CacheAttached(sender);

			IEnumerable<(string value, string label)> GetValues(string fieldName, IEnumerable<PXStringListAttribute> list, int fieldIndex)
			{
				return list.FirstOrDefault()
						?.ValueLabelDic
						.Select(f => (value: GetValueWithPrefix(f.Key, fieldIndex), label: f.Value))
						// exception only for developer
						?? throw new InvalidOperationException($"There are no defined {nameof(PXStringListAttribute)} on field {fieldName}");
			}
		}
	}

	/// <summary>
	/// Combines all <see cref="PXStringListAttribute"/> from specified fields and append SQL query during select.
	/// </summary>
	public abstract class CombinedDBStringListsAttribute : CombinedStringListsAttribute, IPXCommandPreparingSubscriber, IPXRowSelectingSubscriber
	{
		public CombinedDBStringListsAttribute(Type table, params Type[] fields)
			: base(fields)
		{
			Table = table;
		}

		protected Type Table { get; }

		public virtual void CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if (e.IsSelect())
			{
				e.BqlTable = Table;
				PrepareExpression(sender, e);
			}
		}

		protected virtual void PrepareExpression(PXCache cache, PXCommandPreparingEventArgs e)
		{
			for (int i = 0; i < Fields.Count; i++)
			{
				PrepareFieldExpression(cache, e, i);
			}
		}

		protected abstract void PrepareFieldExpression(PXCache cache, PXCommandPreparingEventArgs e, int fieldIndex);

		public virtual void RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			if (e.Row != null)
			{
				sender.SetValue(e.Row, _FieldOrdinal, e.Record.GetValue(e.Position, typeof(string)));
			}
			e.Position++;
		}
	}


	/// <summary>
	/// Combines all <see cref="PXStringListAttribute"/> from specified fields and append SQL query during select by executing coalesce on each field by another in the specified order.
	/// </summary>
	public class CoalesceCombinedDBStringListsAttribute : CombinedDBStringListsAttribute
	{
		public CoalesceCombinedDBStringListsAttribute(Type table, params Type[] fields)
			: base(table, fields)
		{ }

		protected override void PrepareFieldExpression(PXCache cache, PXCommandPreparingEventArgs e, int fieldIndex)
		{
			if (!(e.Expr is SQLSwitch swtc))
				e.Expr = swtc = new SQLSwitch();

			var field = Fields[fieldIndex];
			SQLExpression when;
			var dbCacled = cache.GetAttributes(field.Name).OfType<PXDBCalcedAttribute>().FirstOrDefault();
			if(dbCacled != null)
			{
				var newE = new PXCommandPreparingEventArgs(e.Row, e.Value, e.Operation, e.Table, e.SqlDialect);
				dbCacled.CommandPreparing(cache, newE);
				when = newE.Expr ?? SQLExpression.IsTrue(true);
			}
			else
			{
				var dbAttribute = cache.GetAttributes(field.Name).OfType<PXDBFieldAttribute>().FirstOrDefault();
				if (dbAttribute != null)
				{
					when = new Column(dbAttribute.DatabaseFieldName, dbAttribute.BqlTable ?? field.DeclaringType);
				}
				else
				{
					when = new Column(field);
				}
			}

			var value = new SQLConst(GetPrefix(fieldIndex));
			value.SetDBType(PXDbType.VarChar);
			swtc.Case(when.IsNotNull(), value.Concat(when));
		}
	}
}
