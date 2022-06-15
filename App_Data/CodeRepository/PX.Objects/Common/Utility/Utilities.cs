using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.Common.Extensions;

namespace PX.Objects.Common
{
	public static class Utilities
	{
		public static void Swap<T>(ref T first, ref T second)
		{
			T temp = first;
			first = second;
			second = temp;
		}


		public static TDestinationDAC Clone<TSourceDAC, TDestinationDAC>(PXGraph graph, TSourceDAC source)
			where TSourceDAC : class, IBqlTable, new()
			where TDestinationDAC : class, IBqlTable, new()
		{
			PXCache sourceCache = graph.Caches<TSourceDAC>();
			PXCache destinationCache = graph.Caches<TDestinationDAC>();
			TDestinationDAC result = (TDestinationDAC)destinationCache.CreateInstance();
			foreach (string field in destinationCache.Fields)
			{
				if (sourceCache.Fields.Contains(field))
				{
					destinationCache.SetValue(result, field, sourceCache.GetValue(source, field));
				}
			}

			return result;
		}

		public static PXResultset<TSourceDAC> ToResultset<TSourceDAC>(TSourceDAC item)
			where TSourceDAC : class, IBqlTable, new()
		{
			return new PXResultset<TSourceDAC>()
			{
				new PXResult<TSourceDAC>(item)
			};
		}

		/// <summary>
		/// Method designed to change CopyPaste Scripts inside CopyPasteGetScript overloads - moves listed fields after specified one.
		/// NOT RECOMMENDED - instead, consider moving the field BEFORE fields which depend on it. See usages of PX.Objects.Common.FinDocCopyPasteHelper.SetBranchFieldCommandToTheTop(...)
		/// </summary>
		/// <param name="script">Command lists which is parameter in CopyPasteGetScript</param>
		/// <param name="firstField">field, which is required to set before listed</param>
		/// <param name="fieldList">fields, whcich should be set after first one</param>
		public static void SetDependentFieldsAfterBranch(List<Api.Models.Command> script,
						(string name, string viewName) firstField,
						List<(string name, string viewName)> fieldList)
		{
			// 1) insert dependent fields after the firstField
			// 2) all fields must belong to the same view.
			int firstFieldIDIndex = script.FindIndex(cmd => cmd.FieldName == firstField.name && cmd.ObjectName == firstField.viewName);

			if (firstFieldIDIndex < 0)
				return;

			List<Api.Models.Command> commandList = new List<Api.Models.Command>();
			foreach (var item in fieldList)
			{
				Api.Models.Command cmdItem = script.Where(cmd => cmd.FieldName == item.name && cmd.ObjectName == item.viewName).SingleOrDefault();

				if (cmdItem == null)
					return;

				// All fields can be located on different views.
				// Set the same view for processing together.
				cmdItem.ObjectName = firstField.viewName;
				cmdItem.Commit = false;
				commandList.Add(cmdItem);
			}

			Api.Models.Command[] commands = commandList.ToArray();

			//last field should invoke Commit
			commands[commands.Length - 1].Commit = true;

			foreach (Api.Models.Command command in commands)
			{
				script.Remove(command);
			}
			firstFieldIDIndex = script.FindIndex(cmd => cmd.FieldName == firstField.name && cmd.ObjectName == firstField.viewName);
			script.InsertRange(firstFieldIDIndex + 1, commands);
		}

		public static void SetDependentFieldsAfterBranch<TDocument, TBranchID, TBAccountID, TLocationID>
			(List<Api.Models.Command> script, List<Api.Models.Container> containers,
				string branchViewName, string baccountViewName, params string[] additinalFields)
			where TDocument : IBqlTable
			where TBranchID : IBqlField
			where TBAccountID : IBqlField
			where TLocationID : IBqlField
		{
			string curyInfoViewName = $"_{typeof(TDocument).Name}_{nameof(CM.CurrencyInfo)}_";

			var branchCommand = CopyPasteGetCommand(script, branchViewName, typeof(TBranchID).Name);
			(string Name, string ViewName) branch = (branchCommand?.FieldName, branchCommand?.ObjectName);

			var baccountCommand = CopyPasteGetCommand(script, baccountViewName, typeof(TBAccountID).Name);
			var baccountLocationCommand = CopyPasteGetCommand(script, baccountViewName, typeof(TLocationID).Name);
			var curyCommand = CopyPasteGetCommand(script, baccountViewName, nameof(CM.CurrencyInfo.CuryID));
			var curyRateTypeCommand = CopyPasteGetCommand(script, curyInfoViewName, nameof(CM.CurrencyInfo.CuryRateTypeID));
			var curyEffDateCommand = CopyPasteGetCommand(script, curyInfoViewName, nameof(CM.CurrencyInfo.CuryEffDate));
			var sampleCuryRateCommand = CopyPasteGetCommand(script, curyInfoViewName, nameof(CM.CurrencyInfo.SampleCuryRate));
			var sampleRecipRateCommand = CopyPasteGetCommand(script, curyInfoViewName, nameof(CM.CurrencyInfo.SampleRecipRate));

			var fieldList = new List<(string Name, string ViewName, bool Commit)>()
			{
				(baccountCommand?.FieldName, baccountCommand?.ObjectName, false),
				(baccountLocationCommand?.FieldName, baccountLocationCommand?.ObjectName, true),
				(curyCommand?.FieldName, curyCommand?.ObjectName, true),
				(curyRateTypeCommand?.FieldName, curyRateTypeCommand?.ObjectName, curyRateTypeCommand?.Commit == true),
				(curyEffDateCommand?.FieldName, curyEffDateCommand?.ObjectName, curyEffDateCommand?.Commit == true),
				(sampleCuryRateCommand?.FieldName, sampleCuryRateCommand?.ObjectName, sampleCuryRateCommand?.Commit == true),
				(sampleRecipRateCommand?.FieldName, sampleRecipRateCommand?.ObjectName, sampleRecipRateCommand?.Commit == true),
			};

			foreach (var field in additinalFields)
			{
				var command = CopyPasteGetCommand(script, baccountViewName, field);

				if (command == null)
					command = CopyPasteGetCommand(script, branchViewName, field);

				if (command != null)
					fieldList.Add((command.FieldName, command.ObjectName, command.Commit));
			}

			fieldList.RemoveAll((item) => string.IsNullOrEmpty(item.Name));

			SetDependentFieldsAfterBranch(script, containers, branch, fieldList);
		}

		public static void SetDependentFieldsAfterBranch(List<Api.Models.Command> script, List<Api.Models.Container> containers,
						(string name, string viewName) firstField,
						List<(string name, string viewName, bool commit)> fieldList)
		{
			// 1) insert dependent fields after the firstField
			// 2) all fields must belong to the same view.
			int firstFieldIDIndex = script.FindIndex(cmd =>
				cmd.FieldName.Equals(firstField.name, StringComparison.OrdinalIgnoreCase) &&
				cmd.ObjectName.Equals(firstField.viewName, StringComparison.OrdinalIgnoreCase));

			if (firstFieldIDIndex < 0)
				return;

			List<Api.Models.Command> commandList = new List<Api.Models.Command>();
			foreach (var item in fieldList)
			{
				Api.Models.Command cmdItem = 
					script.Where(cmd =>
						cmd.FieldName.Equals(item.name, StringComparison.OrdinalIgnoreCase) &&
						cmd.ObjectName.Equals(item.viewName, StringComparison.OrdinalIgnoreCase)
					).SingleOrDefault();

				if (cmdItem == null)
					return;

				cmdItem.Commit = item.commit;
				commandList.Add(cmdItem);
			}

			Api.Models.Command[] commands = commandList.ToArray();
			var containerList = new List<Api.Models.Container>();

			foreach (Api.Models.Command command in commands)
			{
				var commandIndex = script.IndexOf(command);
				script.RemoveAt(commandIndex);
				containerList.Add(containers[commandIndex]);
				containers.RemoveAt(commandIndex);
			}
			firstFieldIDIndex = script.FindIndex(cmd =>
				cmd.FieldName.Equals(firstField.name, StringComparison.OrdinalIgnoreCase) &&
				cmd.ObjectName.Equals(firstField.viewName, StringComparison.OrdinalIgnoreCase));

			script.InsertRange(firstFieldIDIndex + 1, commands);
			containers.InsertRange(firstFieldIDIndex + 1, containerList);
		}

		public static Api.Models.Command CopyPasteGetCommand(List<Api.Models.Command> script, string graphViewName, string fieldName)
		{
			const char ApiViewNumberSeparator = ':';

			foreach (var command in script)
			{
				if (!command.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
					continue;

				var objectName = command.ObjectName;

				if (objectName.Equals(graphViewName, StringComparison.OrdinalIgnoreCase))
					return command;

				if (!objectName.StartsWith(graphViewName, StringComparison.OrdinalIgnoreCase) ||
					!objectName.Contains(ApiViewNumberSeparator))
					continue;

				var objectNameWithoutNumber = objectName.Split(ApiViewNumberSeparator).First();

				if (objectNameWithoutNumber.Equals(graphViewName, StringComparison.OrdinalIgnoreCase))
					return command;
			}

			return null;
		}

		public static void SetFieldCommandToTheTop(List<Api.Models.Command> script, List<Api.Models.Container> containers, string graphViewName, string fieldName, bool? commit = true)
		{
			Api.Models.Command fieldCommand = CopyPasteGetCommand(script, graphViewName, fieldName);

			if (fieldCommand != null)
			{
				if (commit != null)
					fieldCommand.Commit = (bool)commit;

				var index = script.IndexOf(fieldCommand);
				var fieldContainer = containers[index];
				containers.Remove(fieldContainer);
				containers.Insert(0, fieldContainer);
				script.Remove(fieldCommand);
				script.Insert(0, fieldCommand);
			}

		}

		public static TEntity CreateInstance<TEntity>(this PXCache cache, string[] sortColumns, object[] searches)
			where TEntity: class, IBqlTable, new()
        {
			if ((sortColumns?.Length ?? 0) < cache.Keys.Count)
				return null;
			object findValue(string field)
            {
				var index = sortColumns.FindIndex(c => c.Equals(field, StringComparison.InvariantCultureIgnoreCase));
				return index >= 0 && searches.Length > index
					? searches[index]
					: null;
            }
			var entity = (TEntity)cache.CreateInstance();
			foreach(var keyField in cache.Keys)
            {
				var keyFieldValue = findValue(keyField);
				if (keyFieldValue == null)
					return null;
				cache.SetValue(entity, keyField, keyFieldValue);
            }
			return entity;
		}
	}
}
