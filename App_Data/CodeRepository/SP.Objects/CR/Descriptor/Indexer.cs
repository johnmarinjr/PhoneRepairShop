using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Data.SQLTree;
using System.Collections.Concurrent;
using PX.DbServices.QueryObjectModel;
using PX.Objects.GL;

namespace SP.Objects.CR
{
	public sealed class MatchWithBAccount<Field, Parameter> : MatchWithBAccountBase, IBqlUnary, IBqlPortalRestrictor
        where Field : IBqlOperand
        where Parameter : IBqlParameter, new()
    {
        IBqlParameter _parameter;
		private IBqlCreator _operand;

		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
        {
            result = true;
        }

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection) 
		{
			bool status = true;

			if (graph != null)
			{
				if (_parameter == null) _parameter = new Parameter();

				object val = null;
				if (_parameter.HasDefault) 
				{
					Type ft = _parameter.GetReferencedType();
					if (ft.IsNested) 
					{
						Type ct = BqlCommand.GetItemType(ft);
						PXCache cache = graph.Caches[ct];
						if (cache.Current != null) 
							val = cache.GetValue(cache.Current, ft.Name);
					}
				}

				SQLExpression fieldExpression = null;
				if (!typeof(IBqlCreator).IsAssignableFrom(typeof(Field))) 
				{
					fieldExpression = SPCommand.GetSingleField(typeof(Field), graph, info.Tables, PXDBOperation.Select);
				}
				else 
				{
					if (_operand == null) _operand = Activator.CreateInstance<Field>() as IBqlCreator;
					if (_operand == null) 
						throw new PXArgumentException("Field", ErrorMessages.OperandNotClassFieldAndNotIBqlCreator);

					status &= _operand.AppendExpression(ref fieldExpression, graph, info, selection);
				}
				exp = (fieldExpression ?? SQLExpression.None()).IsNull();

				List<int> baccounts = val != null ? GetBaccounts(graph) : null;

				if (PXContext.PXIdentity.User.IsInRole(PXAccess.GetAdministratorRole())) {
					exp = exp.Or(new SQLConst(1).EQ(1));
				}
				else if (baccounts != null && baccounts.Count > 0) {
					SQLExpression left = null;
					if (!typeof(IBqlCreator).IsAssignableFrom(typeof(Field))) {
						left = SPCommand.GetSingleField(typeof(Field), graph, info.Tables, PXDBOperation.Select);
					}
					else {
						if (_operand == null) _operand = Activator.CreateInstance<Field>() as IBqlCreator;
						if (_operand == null) {
							throw new PXArgumentException("Field", ErrorMessages.OperandNotClassFieldAndNotIBqlCreator);
						}
						status &= _operand.AppendExpression(ref left, graph, info, selection);
					}

					var seq = SQLExpression.None();
					for (int i = 0; i < baccounts.Count; i++) {
						seq=seq.Seq(baccounts[i]);
					}
					var ins = (left ?? SQLExpression.None()).In(seq);

					exp = exp.Or(ins);
				}
			}
			else if (typeof(IBqlCreator).IsAssignableFrom(typeof(Field)))
			{
				if (_operand == null) _operand = Activator.CreateInstance<Field>() as IBqlCreator;
				if (_operand == null)
				{
					throw new PXArgumentException("Field", ErrorMessages.OperandNotClassFieldAndNotIBqlCreator);
				}
				SQLExpression left = null;
				status &= _operand.AppendExpression(ref left, graph, info, selection);
				exp = (left ?? SQLExpression.None()).IsNull();
			}

			return status;
		}
	}

	internal class ChildToParentBAccountData
	{
		internal static DateTime lastBAccountUpdated = PXTimeZoneInfo.UtcNow.AddDays(-1);
		internal static DateTime lastContactUpdated = PXTimeZoneInfo.UtcNow.AddDays(-1);

		// Guid - to - BAccountID
		internal static ConcurrentDictionary<Guid, int?> UserToAssosiatedAccount = new ConcurrentDictionary<Guid, int?>();

		// BAccountID - to - "Child".BAccountID
		internal static ConcurrentDictionary<int, int?> ChildToParentAccount = new ConcurrentDictionary<int, int?>();
		internal static ConcurrentDictionary<int, List<int>> ParentToChildAccounts = new ConcurrentDictionary<int, List<int>>();

		/// <summary>
		/// On update BAccount remove child->parent if it changed
		/// </summary>
		internal static void BAccountUpdated()
		{
			var queryExecutionTime = lastBAccountUpdated;
			var newTS = PXTimeZoneInfo.UtcNow;

			foreach (PXDataRecord record in PXDatabase.SelectMulti<BAccount>(
				new PXDataField<BAccount.bAccountID>(),
				new PXDataField<BAccount.parentBAccountID>(),
				new PXDataFieldValue<BAccount.lastModifiedDateTime>(queryExecutionTime, PXComp.GE)))
			{
				int? childBAccountID = record.GetInt32(0);
				int? newParentBAccountID = record.GetInt32(1);
				if (ChildToParentAccount.TryGetValue((int)childBAccountID, out int? oldParentBAccountID) && newParentBAccountID.Equals(oldParentBAccountID))
				{
					continue;
				}
				ChildToParentAccount.TryRemove((int)childBAccountID, out oldParentBAccountID);
				RemoveFromParents(oldParentBAccountID);

				if (newParentBAccountID != null)
				{
					ChildToParentAccount.TryRemove((int)newParentBAccountID, out oldParentBAccountID);
					RemoveFromParents(oldParentBAccountID);
				}
				RemoveFromParents(childBAccountID);
				RemoveFromParents(newParentBAccountID);
			}
			lastBAccountUpdated = newTS;
		}

		/// <summary>
		/// On update Contact - update BAccount 
		/// </summary>
		internal static void ContactUpdated()
		{
			var queryExecutionTime = lastContactUpdated;
			var newTS = PXTimeZoneInfo.UtcNow;

			foreach (PXDataRecord record in PXDatabase.SelectMulti<Contact>(
				Yaql.join<Company>(Yaql.isIn(Yaql.column<Contact.userID>(), UserToAssosiatedAccount.Select(x => x.Key))),
				new PXDataField<Contact.bAccountID>(nameof(Contact)),
				new PXDataField<Contact.userID>(),
				new PXDataFieldValue<Contact.lastModifiedDateTime>(queryExecutionTime, PXComp.GE)))
			{
				int? bAccountID = record.GetInt32(0);
				Guid? userID = record.GetGuid(1);

				if (userID != null)
				{
					UserToAssosiatedAccount.AddOrUpdate((Guid)userID, bAccountID, (key, val) => bAccountID);
				}
			}

			lastContactUpdated = newTS;
		}

		/// <summary>
		/// recursively remove all parents cached in AccountForParent
		/// </summary>
		/// <param name="bAccountID"></param>
		private static void RemoveFromParents(int? bAccountID)
		{
			if (bAccountID == null)
				return;

			foreach (var item in ParentToChildAccounts.Where(x => x.Value.Contains((int)bAccountID)).ToList())
			{
				if (ParentToChildAccounts.TryRemove((int)item.Key, out List<int> childs))
				{
					if (ChildToParentAccount.TryGetValue((int)bAccountID, out int? parentBAccountID) == false)
					{
						parentBAccountID = null;
					}
					RemoveFromParents(parentBAccountID);
					foreach (var childAccount in childs)
					{
						RemoveFromParents(childAccount);
					}
				}
			}
		}

		/// <summary>
		/// read BAccountID for Contact.userID from DB
		/// </summary>
		/// <param name="userID"></param>
		/// <returns></returns>
		private static int? GetAssosiatedAccountByUserID(Guid userID)
		{
			using (PXDataRecord record = PXDatabase.SelectSingle<Contact>(
				new PXDataField<Contact.bAccountID>(),
				new PXDataFieldValue<Contact.userID>(userID, PXComp.EQ)))
			{
				return record.GetInt32(0);
			}
		}

		/// <summary>
		/// return all child BAccounts for the specified bAccountID
		/// </summary>
		/// <param name="parentBAccountID"></param>
		/// <returns></returns>
		private static List<int> GetAllChildsForParent(int parentBAccountID)
		{
			if (ParentToChildAccounts.TryGetValue(parentBAccountID, out List<int> childAccounts))
				return childAccounts;

			childAccounts = new List<int>() { parentBAccountID };

			foreach (var childBAccountID in ChildToParentAccount
				.Where(x => x.Value == parentBAccountID)
				.Select(pair => pair.Key))
			{
				var childs = GetAllChildsForParent(childBAccountID);

				childAccounts.AddRange(childs);
			}

			ParentToChildAccounts.TryAdd(parentBAccountID, childAccounts);

			return childAccounts;
		}

		internal static void FillInAccountsForParent(int parentBAccountID)
		{
			if (ChildToParentAccount.ContainsKey(parentBAccountID))
				return;

			ChildToParentAccount.TryAdd(parentBAccountID, null);

			foreach (var record in PXDatabase.SelectMulti<BAccount>(new[]
				{
					Yaql.@join<BAccount>("grandchildBA",
						Yaql.column<BAccount.bAccountID>("BAccount").eq(Yaql.column<BAccount.parentBAccountID>("grandchildBA"))
					,YaqlJoinType.LEFT)
				},
				new PXDataField<BAccount.bAccountID>("BAccount"),
				new PXDataField<BAccount.bAccountID>("grandchildBA"),
				new PXDataFieldValue<BAccount.parentBAccountID>("BAccount", parentBAccountID, PXComp.EQ)))
			{

				int childBAccountID = record.GetInt32(0) ?? 0;
				int? grandchildBAccountID = record.GetInt32(1);

				if (grandchildBAccountID != null)
				{
					FillInAccountsForParent((int)grandchildBAccountID);
				}

				if (!ChildToParentAccount.TryAdd(childBAccountID, parentBAccountID))
				{
					ChildToParentAccount[childBAccountID] = parentBAccountID;
				}

				RemoveFromParents(parentBAccountID);
			}
		}

		internal static IEnumerable<int> GetUserAccounts(Guid userID)
		{
			if (UserToAssosiatedAccount.TryGetValue(userID, out int? bAccountID) == false)
			{
				bAccountID = GetAssosiatedAccountByUserID(userID);

				UserToAssosiatedAccount.TryAdd(userID, bAccountID);
			}

			if (bAccountID == null)
				return Enumerable.Empty<int>();

			FillInAccountsForParent((int)bAccountID);

			return GetAllChildsForParent((int)bAccountID);
		}
	}

	public class MatchWithBAccountBase
	{
		private const string _TYPEID = "Warning";

		static MatchWithBAccountBase()
		{
			PXDatabase.Subscribe<BAccount>(
				ChildToParentBAccountData.BAccountUpdated,
				nameof(MatchWithBAccountBase));

			PXDatabase.Subscribe<Contact>(
				ChildToParentBAccountData.ContactUpdated,
				nameof(MatchWithBAccountBase));
		}

		protected static List<int> GetBaccounts(PXGraph graph)
		{
			if (PXGraph.ProxyIsActive)
			{
				return new List<int>();
			}

			var userID = graph?.Accessinfo?.UserID;

			if (userID == null)
				return Enumerable.Empty<int>().ToList();

			var res = ChildToParentBAccountData.GetUserAccounts((Guid)userID).ToList();

			if (res.Count == 0 && HttpContext.Current != null)
			{
				var url = string.Format("~/Frames/Error.aspx?exceptionID={0}&typeID={1}&errorcode={2}&HideScript=On",
					HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.GetBaccountsErrorMessage)),
					HttpUtility.UrlEncode(_TYPEID),
					HttpUtility.UrlEncode(PXMessages.LocalizeNoPrefix(Objects.Messages.ConfigurationError)));

				var page = HttpContext.Current.CurrentHandler as Page;

				if (page != null && page.IsCallback)
					throw new PXRedirectToUrlException(url, "");
				else
					HttpContext.Current.Response.Redirect(url);
			}

			return res;
		}

		public static bool IsParentChild(int? parent, int? child)
		{
			if (child == null) return false;
			ChildToParentBAccountData.FillInAccountsForParent((int)child);
			return ChildToParentBAccountData.ChildToParentAccount.TryGetValue((int)child, out int? val)
					&& (int.Equals(parent, val) || int.Equals(parent, child));
		}
	}

	public sealed class MatchWithBAccountNotNull<Field> : MatchWithBAccountBase, IBqlUnary, IBqlPortalRestrictor
		where Field : IBqlOperand
	{
		private IBqlCreator _operand;

		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			List<int> baccounts = GetBaccounts(cache?.Graph);

			if (System.Web.Security.Roles.IsUserInRole(PXAccess.GetUserName(), PXAccess.GetAdministratorRole()))
			{
				result = true;
			}
			else if (baccounts != null && baccounts.Count > 0)
			{
				result =
					value == null || value is int intValue && GetBaccounts(cache?.Graph).Contains(intValue);
			}
		}

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			bool status = true;

			if (graph == null)
				return status;

			List<int> baccounts = GetBaccounts(graph);

			if (PXContext.PXIdentity.User.IsInRole(PXAccess.GetAdministratorRole()))
			{
				exp = new SQLConst(1).EQ(1);
			}
			else if (baccounts != null && baccounts.Count > 0)
			{
				SQLExpression left = null;

				if (!typeof(IBqlCreator).IsAssignableFrom(typeof(Field)))
				{
					left = SPCommand.GetSingleField(typeof(Field), graph, info.Tables, PXDBOperation.Select);
				}
				else
				{
					if (_operand == null) _operand = Activator.CreateInstance<Field>() as IBqlCreator;
					if (_operand == null)
					{
						throw new PXArgumentException("Field", ErrorMessages.OperandNotClassFieldAndNotIBqlCreator);
					}
					status &= _operand.AppendExpression(ref left, graph, info, selection);
				}
				
				var seq = SQLExpression.None();
				for (int i = 0; i < baccounts.Count; i++)
				{
					seq = seq.Seq(baccounts[i]);
				}

				exp = (left ?? SQLExpression.None()).In(seq);
			}

			return status;
		}
	}

	public abstract class SPCommand : BqlCommand
    {
		internal static SQLExpression GetSingleField(Type field, PXGraph graph, List<Type> tables, PXDBOperation operation) {
			Type table0 = PX.Data.BqlCommand.GetItemType(field);

			PXCache cache = graph.Caches[table0];
			PXCommandPreparingEventArgs.FieldDescription description;
			Type table = table0;
			if (tables != null && tables.Count > 0) {
				if (tables[0].IsSubclassOf(table0)) {
					table = tables[0];
				}
				else if (!typeof(IBqlTable).IsAssignableFrom(table)) {
					Type cust = table;
					table = null;
					for (int i = 0; i < tables.Count; i++) {
						if (cust.IsAssignableFrom(tables[i])
							&& (table == null
								|| tables[i].IsAssignableFrom(table))) {
							table = tables[i];
						}
					}
					table = table ?? cust;
				}
			}
			cache.RaiseCommandPreparing(field.Name, null, null, operation, table, out description);
			return description.Expr;
		}
    }
}
