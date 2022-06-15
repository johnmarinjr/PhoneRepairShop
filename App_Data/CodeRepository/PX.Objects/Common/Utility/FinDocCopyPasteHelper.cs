using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using PX.Api.Models;

namespace PX.Objects.Common
{
	public class FinDocCopyPasteHelper
	{
		private const string BranchIDFieldName = "BranchID";
		private const string OriginalObjectName = "CurrentDocument";
		private const string DesiredObjectName = "Document";

		public FinDocCopyPasteHelper(PXGraph graph)
		{
			if (graph.PrimaryItemType == null) return;

			if (graph.PrimaryItemType.GetProperty(BranchIDFieldName) == null)
				throw new InvalidOperationException("The graph is not suitable for this helper because its primary do not have field " + BranchIDFieldName);

			const string GraphDoesNotHaveView = "The graph is not suitable for this helper because it does not have view ";

			if (graph.GetType().GetField(OriginalObjectName) == null)
				throw new InvalidOperationException(GraphDoesNotHaveView + OriginalObjectName);

			if (graph.GetType().GetField(DesiredObjectName) == null)
				throw new InvalidOperationException(GraphDoesNotHaveView + DesiredObjectName);
		}

		public void SetBranchFieldCommandToTheTop(List<Command> script)
		{
			Command cmdBranch = script.Single(cmd => cmd.FieldName == BranchIDFieldName && cmd.ObjectName == OriginalObjectName);
			cmdBranch.ObjectName = DesiredObjectName;
			script.Remove(cmdBranch);
			script.Insert(0, cmdBranch);
		}
	}
}
