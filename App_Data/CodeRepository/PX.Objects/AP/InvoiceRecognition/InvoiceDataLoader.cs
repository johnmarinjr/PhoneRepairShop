using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Data;
using PX.Objects.AP.InvoiceRecognition.DAC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal class InvoiceDataLoader
	{
		private const string DateTimeStringFormat = "yyyy-MM-dd";
		private const char ViewPlusFieldNameSeparator = '.';

		private readonly Document _recognizedDocument;
		private readonly APInvoiceRecognitionEntry _graph;
		private readonly string[] _detailOrderedFields;
		private readonly int? _vendorId;
		private readonly bool _loadVendorIdOnly;

		public InvoiceDataLoader(DocumentRecognitionResult recognitionResult, APInvoiceRecognitionEntry graph, string[] detailOrderedFields,
			int? vendorId, bool loadVendorIdOnly)
		{
			recognitionResult.ThrowOnNull(nameof(recognitionResult));
			graph.ThrowOnNull(nameof(graph));
			detailOrderedFields.ThrowOnNull(nameof(detailOrderedFields));

			_recognizedDocument = recognitionResult?.Documents?.Count > 0 ?
				recognitionResult.Documents[0] :
				null;
			_graph = graph;
			_detailOrderedFields = detailOrderedFields;
			_vendorId = vendorId;
			_loadVendorIdOnly = loadVendorIdOnly;
		}

		public void Load(object primaryRow)
		{
			primaryRow.ThrowOnNull(nameof(primaryRow));

			LoadPrimary(primaryRow);
			LoadDetails();
		}

		private void LoadPrimary(object row)
		{
			row.ThrowOnNull(nameof(row));

			if (_vendorId != null)
			{
				SetExtValue(_graph.Document.Cache, row, nameof(APRecognizedInvoice.VendorID), _vendorId);
			}

			if (_loadVendorIdOnly)
			{
				return;
			}

			var areFieldsRecognized = _recognizedDocument?.Fields != null;
			if (!areFieldsRecognized)
			{
				return;
			}

			foreach (var fieldPair in _recognizedDocument.Fields)
			{
				var viewPlusFieldName = fieldPair.Key;
				if (string.IsNullOrWhiteSpace(viewPlusFieldName))
				{
					continue;
				}

				var (viewName, fieldName) = GetFieldInfo(fieldPair.Key);
				if (string.IsNullOrWhiteSpace(viewName) || string.IsNullOrWhiteSpace(fieldName))
				{
					continue;
				}

				var cache = _graph.Views[viewName].Cache;
				LoadPrimaryField(cache, row, fieldName, fieldPair.Value);
			}
		}

		internal static (string ViewName, string FieldName) GetFieldInfo(string viewNameFieldName)
		{
			var names = viewNameFieldName.Split(ViewPlusFieldNameSeparator);

			if (names.Length != 2)
			{
				return (null, null);
			}

			return (names[0], names[1]);
		}

		private void LoadPrimaryField(PXCache cache, object row, string fieldName, Field field)
		{
			SetFieldExtValue(cache, row, fieldName, field);
		}

		private void LoadDetails()
		{
			var areDetailsRecognized = _recognizedDocument?.Details != null;
			if (!areDetailsRecognized)
			{
				return;
			}

			foreach (var detailPair in _recognizedDocument.Details)
			{
				var rows = detailPair.Value?.Value;
				if (rows == null)
				{
					continue;
				}

				LoadDetailsRows(rows);
			}
		}

		private void LoadDetailsRows(IList<DetailValue> rows)
		{
			foreach (var row in rows)
			{
				var fields = row.Fields;
				if (fields == null)
				{
					continue;
				}

				var fieldList = fields.ToList();
				fieldList.Sort(CompareDetailFields);

				LoadDetailsRow(fieldList);
			}
		}

		private int CompareDetailFields(KeyValuePair<string, Field> x, KeyValuePair<string, Field> y)
		{
			var (_, fieldNameX) = GetFieldInfo(x.Key);
			var (_, fieldNameY) = GetFieldInfo(y.Key);

			var indexOfX = Array.IndexOf(_detailOrderedFields, fieldNameX);
			var indexofY = Array.IndexOf(_detailOrderedFields, fieldNameY);

			return indexOfX.CompareTo(indexofY);
		}

		private void LoadDetailsRow(List<KeyValuePair<string, Field>> fields)
		{
			object row = null;
			PXCache cache = null;

			foreach (var fieldPair in fields)
			{
				var viewPlusFieldName = fieldPair.Key;
				if (string.IsNullOrWhiteSpace(viewPlusFieldName))
				{
					continue;
				}

				var (viewName, fieldName) = GetFieldInfo(fieldPair.Key);
				if (string.IsNullOrWhiteSpace(viewName) || string.IsNullOrWhiteSpace(fieldName))
				{
					continue;
				}

				if (cache == null)
				{
					cache = _graph.Views[viewName].Cache;
				}

				if (row == null)
				{
					row = cache.Insert();
				}

				LoadDetailsField(cache, row, fieldName, fieldPair.Value);
			}

			if (cache != null && row != null)
			{
				// To update parent's Details Total
				try
				{
					cache.Update(row);
				}
				catch (PXFieldValueProcessingException e)
				{
					var newValue = e.ErrorValue ?? cache.GetValueExt(row, e.FieldName);
					cache.RaiseExceptionHandling(e.FieldName, row, newValue, e);
				}
			}
		}

		private void LoadDetailsField(PXCache cache, object row, string fieldName, Field field)
		{
			SetFieldExtValue(cache, row, fieldName, field);
		}

		internal static void SetFieldExtValue(PXCache cache, object row, string fieldName, Field field)
		{
			if (field == null)
			{
				return;
			}

			var fieldState = cache.GetStateExt(null, fieldName) as PXFieldState;
			if (fieldState == null || fieldState.DataType == null)
			{
				return;
			}

			var fieldType = fieldState.DataType;
			var extValue = ParseExtValue(fieldType, field.Value, field.Ocr?.Text);
			if (extValue == null)
			{
				return;
			}

			var valueType = extValue.GetType();
			var needToConvertValue = !fieldType.IsAssignableFrom(valueType);
			if (needToConvertValue)
			{
				if (valueType == typeof(string) && fieldType == typeof(DateTime) &&
					DateTime.TryParseExact(extValue as string, DateTimeStringFormat, null, DateTimeStyles.None, out var dateExtValue))
				{
					extValue = (DateTime?)dateExtValue;
				}
				else
				{
					try
					{
					extValue = Convert.ChangeType(extValue, fieldType);
				}
					catch
					{
						return;
					}
				}
			}

			SetExtValue(cache, row, fieldName, extValue);
		}

		private static void SetExtValue(PXCache cache, object row, string fieldName, object extValue)
		{
			try
			{
				cache.SetValueExt(row, fieldName, extValue);
			}
			catch (PXSetPropertyException e)
			{
				cache.RaiseExceptionHandling(fieldName, row, extValue, e);
			}
		}

		private static object ParseExtValue(Type fieldType, object fieldValue, string fieldTextValue)
		{
			if (fieldValue != null)
			{
				return fieldValue;
			}

			if (string.IsNullOrEmpty(fieldTextValue))
			{
				return null;
			}

			if (fieldType == typeof(string))
			{
				return fieldTextValue;
			}

			if (fieldType == typeof(int?) && int.TryParse(fieldTextValue, out var intExtValue))
			{
				return (int?)intExtValue;
			}

			if (fieldType == typeof(decimal?) && decimal.TryParse(fieldTextValue, out var decimalExtValue))
			{
				return (decimal?)decimalExtValue;
			}

			if (fieldType == typeof(DateTime?) &&
				DateTime.TryParseExact(fieldTextValue, DateTimeStringFormat, null, DateTimeStyles.None, out var dateExtValue))
			{
				return (DateTime?)dateExtValue;
			}

			return null;
		}
	}
}
