using PX.Data;
using System;
using System.Collections;

namespace PX.Objects.PM
{
	public class PMAccountGroupAccessDetail : PX.SM.UserAccess
	{
		public PXSelect<PMAccountGroup> AccountGroup;

		public PMAccountGroupAccessDetail()
		{
			AccountGroup.Cache.AllowDelete = false;
			AccountGroup.Cache.AllowInsert = false;
			PXUIFieldAttribute.SetEnabled(AccountGroup.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<PMAccountGroup.groupCD>(AccountGroup.Cache, null);
			Views.Caches.Remove(Groups.GetItemType());
			Views.Caches.Add(Groups.GetItemType());
		}

		public PXSave<PMAccountGroup> Save;
		public PXCancel<PMAccountGroup> Cancel;
		public PXFirst<PMAccountGroup> First;
		public PXPrevious<PMAccountGroup> Prev;
		public PXNext<PMAccountGroup> Next;
		public PXLast<PMAccountGroup> Last;

		protected override IEnumerable groups()
		{
			foreach (PX.SM.RelationGroup group in PXSelect<PX.SM.RelationGroup>.Select(this))
			{
				if ((group.SpecificModule == null || group.SpecificModule == typeof(PMAccountGroup).Namespace)
					&& (group.SpecificType == null || group.SpecificType == typeof(PMAccountGroup).FullName)
					|| PX.SM.UserAccess.IsIncluded(getMask(), group))
				{
					Groups.Current = group;
					yield return group;
				}
			}
		}

		protected override byte[] getMask()
		{
			byte[] mask = null;
			if (User.Current != null)
			{
				mask = User.Current.GroupMask;
			}
			else if (AccountGroup.Current != null)
			{
				mask = AccountGroup.Current.GroupMask;
			}
			return mask;
		}

		public override void Persist()
		{
			if (User.Current != null)
			{
				PopulateNeighbours<PX.SM.Users>(User, Groups);
				PXSelectorAttribute.ClearGlobalCache<PX.SM.Users>();
			}
			else if (AccountGroup.Current != null)
			{
				PopulateNeighbours<PMAccountGroup>(AccountGroup, Groups);
				PXSelectorAttribute.ClearGlobalCache<PMAccountGroup>();
			}
			else
			{
				return;
			}
			base.Persist();
		}

		#region DAC overrides

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Account Group Description", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PMAccountGroup.description> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Account Group Type", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PMAccountGroup.type> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Restriction Group Name", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PX.SM.RelationGroup.groupName> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Restriction Group Description", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void _(Events.CacheAttached<PX.SM.RelationGroup.description> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Restriction Group Type")]
		protected virtual void _(Events.CacheAttached<PX.SM.RelationGroup.groupType> e)
		{
		}

		#endregion
	}

	public class PMAccountGroupAccess : PX.SM.BaseAccess
	{
		public class RelationGroupAccountGroupSelectorAttribute : PXCustomSelectorAttribute
		{
			public RelationGroupAccountGroupSelectorAttribute(Type type)
				: base(type)
			{
			}

			public virtual IEnumerable GetRecords()
			{
				return PMAccountGroupAccess.GroupDelegate(_Graph, false);
			}
		}
		[PXDBString(128, IsKey = true, InputMask = "")]
		[PXDefault]
		[PXUIField(DisplayName = "Group Name", Visibility = PXUIVisibility.SelectorVisible)]
		[RelationGroupAccountGroupSelector(typeof(PX.SM.RelationGroup.groupName), Filterable = true)]
		protected virtual void _(Events.CacheAttached<PX.SM.RelationGroup.groupName> e)
		{
		}

		public PMAccountGroupAccess()
		{
			AccountGroup.Cache.AllowDelete = false;
			PXUIFieldAttribute.SetEnabled(AccountGroup.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<PMAccountGroup.included>(AccountGroup.Cache, null);
		}
		public PXSelect<PMAccountGroup> AccountGroup;
		protected virtual IEnumerable accountGroup(
		)
		{
			if (Group.Current != null && !String.IsNullOrEmpty(Group.Current.GroupName))
			{
				foreach (PMAccountGroup accountGroup in PXSelect<PMAccountGroup>
					.Select(this))
				{
					AccountGroup.Current = accountGroup;
					yield return accountGroup;
				}
			}
			else
			{
				yield break;
			}
		}

		
		public override void Persist()
		{
			populateNeighbours<PX.SM.Users>(Users);
			populateNeighbours<PMAccountGroup>(AccountGroup);
			populateNeighbours<PX.SM.Users>(Users);
			base.Persist();
			PXSelectorAttribute.ClearGlobalCache<PX.SM.Users>();
			PXSelectorAttribute.ClearGlobalCache<PMAccountGroup>();
		}

		static public IEnumerable GroupDelegate(PXGraph graph, bool inclInserted)
		{
			PXResultset<PX.SM.Neighbour> set = PXSelectGroupBy<PX.SM.Neighbour,
				Where<PX.SM.Neighbour.leftEntityType, Equal<accountGroupType>>,
				Aggregate<GroupBy<PX.SM.Neighbour.coverageMask,
					GroupBy<PX.SM.Neighbour.inverseMask,
					GroupBy<PX.SM.Neighbour.winCoverageMask,
					GroupBy<PX.SM.Neighbour.winInverseMask>>>>>>.Select(graph);

			foreach (PX.SM.RelationGroup group in PXSelect<PX.SM.RelationGroup>.Select(graph))
			{
				if ((!string.IsNullOrEmpty(group.GroupName) || inclInserted) &&
					(group.SpecificModule == null || group.SpecificModule == typeof(PMAccountGroup).Namespace)
					&& (group.SpecificType == null || group.SpecificType == typeof(PMAccountGroup).FullName)
					|| PX.SM.UserAccess.InNeighbours(set, group))
				{
					yield return group;
				}
			}
		}

		protected virtual IEnumerable group()
		{
			return GroupDelegate(this, true);
		}

		#region Events

		protected override void RelationGroup_RowInserted(PXCache cache, PXRowInsertedEventArgs e)
		{
			base.RelationGroup_RowInserted(cache, e);
			PX.SM.RelationGroup group = (PX.SM.RelationGroup)e.Row;
			group.SpecificModule = typeof(PMAccountGroup).Namespace;
			group.SpecificType = typeof(PMAccountGroup).FullName;
		}

		protected virtual void _(Events.RowSelected<PX.SM.RelationGroup> e)
		{
			PX.SM.RelationGroup group = e.Row as PX.SM.RelationGroup;
			if (group != null)
			{
				if (String.IsNullOrEmpty(group.GroupName))
				{
					Save.SetEnabled(false);
					AccountGroup.Cache.AllowInsert = false;
				}
				else
				{
					Save.SetEnabled(true);
					AccountGroup.Cache.AllowInsert = true;
				}
			}
		}

		protected virtual void _(Events.RowSelected<PMAccountGroup> e)
		{
			PMAccountGroup accountGroup = e.Row as PMAccountGroup;
			PX.SM.RelationGroup group = Group.Current;
			if (accountGroup != null && accountGroup.GroupMask != null && group != null && group.GroupMask != null && e.Cache.GetStatus(accountGroup) == PXEntryStatus.Notchanged)
			{
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification]
				accountGroup.Included = false;
				for (int i = 0; i < accountGroup.GroupMask.Length && i < group.GroupMask.Length; i++)
				{
					if (group.GroupMask[i] != 0x00 && (accountGroup.GroupMask[i] & group.GroupMask[i]) == group.GroupMask[i])
					{
						// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification]
						accountGroup.Included = true;
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisting<PMAccountGroup> e)
		{
			PMAccountGroup accountGroup = e.Row as PMAccountGroup;
			PX.SM.RelationGroup group = Group.Current;
			if (accountGroup != null && accountGroup.GroupMask != null && group != null && group.GroupMask != null)
			{
				if (accountGroup.GroupMask.Length < group.GroupMask.Length)
				{
					byte[] mask = accountGroup.GroupMask;
					Array.Resize<byte>(ref mask, group.GroupMask.Length);
					accountGroup.GroupMask = mask;
				}
				for (int i = 0; i < group.GroupMask.Length; i++)
				{
					if (group.GroupMask[i] == 0x00)
					{
						continue;
					}
					if (accountGroup.Included == true)
					{
						accountGroup.GroupMask[i] = (byte)(accountGroup.GroupMask[i] | group.GroupMask[i]);
					}
					else
					{
						accountGroup.GroupMask[i] = (byte)(accountGroup.GroupMask[i] & ~group.GroupMask[i]);
					}
				}
			}
		}

		#endregion
	}
}
