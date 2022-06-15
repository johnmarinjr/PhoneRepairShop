using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.GL;
using PX.Objects.IN;

namespace PX.Objects.CR
{
	#region BAccountAttribute

	[PXDBInt]
	[PXInt]
	[PXUIField(DisplayName = "Business Account", Visibility = PXUIVisibility.Visible)]
	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public abstract class BAccountRawAttribute : AcctSub2Attribute
	{
		#region State

		public const string DimensionName = "BIZACCT";

		protected Type EntityType = typeof(BAccount);

		public virtual PXSelectorMode SelectorMode { get; set; } = PXSelectorMode.DisplayModeHint;

		public virtual bool HideInactiveVendors { get; set; } = false;

		public bool ViewInCRM { get; set; } = false;

		protected Type[] BAccountTypes = new []
		{
			typeof(BAccountType.vendorType),
			typeof(BAccountType.customerType),
			typeof(BAccountType.combinedType),
			typeof(BAccountType.employeeType),
			typeof(BAccountType.empCombinedType),
			typeof(BAccountType.prospectType),
			typeof(BAccountType.branchType),
			typeof(BAccountType.organizationType),
		};

		#endregion

		#region ctor

		public BAccountRawAttribute(Type entityType, Type[] bAccountTypes = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
		{
			EntityType = entityType ?? EntityType;

			BAccountTypes = bAccountTypes ?? BAccountTypes;

			PXDimensionSelectorAttribute attr =
				new PXDimensionSelectorAttribute(DimensionName, customSearchQuery ?? CreateSelect(),
					substituteKey: Field<BAccount.acctCD>(),
					fieldList: fieldList ?? new Type[]
					{
						typeof(BAccount.acctCD),
						typeof(BAccount.acctName),
						typeof(BAccount.type),
						typeof(BAccount.classID),
						typeof(BAccount.status),
						typeof(Contact.phone1),
						typeof(Address.city),
						typeof(Address.state),
						typeof(Address.countryID),
						typeof(Contact.eMail),
					})
				{
					Headers = headerList != null || fieldList != null // if field list custom and headerlist not provided - use defaults
						? headerList
						: new[]
					{
						"Account ID",
						"Account Name",
						"Type",
						"Class",
						"Customer Status",
						"Phone 1",
						"City",
						"State",
						"Country",
						"Email",
					},

					DescriptionField = Field<BAccount.acctName>(),
					SelectorMode = SelectorMode,
					Filterable = true,
					DirtyRead = true
				};

			_Attributes.Add(attr);

			_SelAttrIndex = _Attributes.Count - 1;

			_Attributes.Add(new PXRestrictorAttribute(GetBAccountTypeWhere(),
				Messages.BAccountIsType, typeof(BAccount.type))
			{
				ShowWarning = true
			});

			if (BAccountTypes.Any(type => type.IsIn(new[] { typeof(BAccountType.prospectType), typeof(BAccountType.customerType), typeof(BAccountType.combinedType) })))
			{
				_Attributes.Add(new PXRestrictorAttribute(
					typeof(Where<
						BAccount.status, IsNull,
						Or<BAccount.status, NotEqual<AR.CustomerStatus.inactive>>>),
					Messages.BusinessAccountStatus, typeof(BAccount.status))
				{
					ShowWarning = true
				});
			}

			if (HideInactiveVendors && BAccountTypes.Any(type => type.IsIn(new[] { typeof(BAccountType.vendorType), typeof(BAccountType.combinedType) })))
			{
				_Attributes.Add(new PXRestrictorAttribute(
					typeof(Where<
						BAccount.vStatus, IsNull,
						Or<BAccount.vStatus, NotEqual<AP.VendorStatus.inactive>>>),
					Messages.BusinessAccountStatus, typeof(BAccount.status))
				{
					ShowWarning = true
				});
			}
		}

		protected virtual Type GetBAccountTypeWhere()
		{
			Type bAccountTypesTypes = null;

			var isBranchExpected = BAccountTypes.Contains(typeof(BAccountType.branchType));
			var typeField = Field<BAccount.type>();

			switch (BAccountTypes.Length)
			{
				case 1:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(Equal<>), BAccountTypes[0]),
						isBranchExpected);
					break;
				case 2:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,>), BAccountTypes[0], BAccountTypes[1]),
						isBranchExpected);
					break;
				case 3:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2]),
						isBranchExpected);
					break;
				case 4:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2], BAccountTypes[3]),
						isBranchExpected);
					break;
				case 5:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2], BAccountTypes[3], BAccountTypes[4]),
						isBranchExpected);
					break;
				case 6:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,,,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2], BAccountTypes[3], BAccountTypes[4], BAccountTypes[5]),
						isBranchExpected);
					break;
				case 7:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,,,,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2], BAccountTypes[3], BAccountTypes[4], BAccountTypes[5], BAccountTypes[6]),
						isBranchExpected);
					break;
				case 8:
					bAccountTypesTypes = AppendIsBranchIfNeeded(BqlCommand.Compose(
							typeof(Where<,>),
							typeField, typeof(In3<,,,,,,,>), BAccountTypes[0], BAccountTypes[1], BAccountTypes[2], BAccountTypes[3], BAccountTypes[4], BAccountTypes[5], BAccountTypes[6], BAccountTypes[7]),
						isBranchExpected);
					break;
			}

			return bAccountTypesTypes;
		}

		protected virtual Type AppendIsBranchIfNeeded(Type originalCondition, bool isBranchExpected)
		{
			if (isBranchExpected && originalCondition.GenericTypeArguments.Length == 2)
			{
				var field = originalCondition.GenericTypeArguments[0];
				var condition = originalCondition.GenericTypeArguments[1];

				return BqlCommand.Compose(typeof(Where<,,>),
					field,
					condition,
					typeof(Or<,>), Field<BAccount.isBranch>(), typeof(Equal<>), typeof(True));
			}

			return originalCondition;
		}

		protected virtual Type CreateSelect()
		{
			return BqlTemplate.OfCommand<
					Search2<
						BqlPlaceholder.I,
					LeftJoin<Contact,
						On<Contact.bAccountID, Equal<BqlPlaceholder.I>,
						And<Contact.contactID, Equal<BqlPlaceholder.C>>>,
					LeftJoin<Address,
						On<Address.bAccountID, Equal<BqlPlaceholder.I>,
						And<Address.addressID, Equal<BqlPlaceholder.A>>>>>,
					Where<
						Match<Current<AccessInfo.userName>>>>>
				.Replace<BqlPlaceholder.I>(Field<BAccount.bAccountID>())
				.Replace<BqlPlaceholder.C>(Field<BAccount.defContactID>())
				.Replace<BqlPlaceholder.A>(Field<BAccount.defAddressID>())
				.ToType();
		}

		#endregion

		#region Events

		public override void CacheAttached(PXCache sender)
		{
			// instanciate the BAccount cache before BAccountR / BAccountCRM
			var cache = sender.Graph.Caches[typeof(BAccount)];

			base.CacheAttached(sender);

			if (ViewInCRM)
			{
				sender.Graph.RowSelecting.AddHandler(EntityType, BAccount_RowSelecting);
				sender.Graph.FieldDefaulting.AddHandler(EntityType, nameof(BAccount.ViewInCrm), BAccount_ViewInCrm_FieldDefaulting);
			}
		}

		protected virtual void BAccount_RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			sender.SetValue(e.Row, nameof(BAccount.ViewInCrm), true);
		}

		protected virtual void BAccount_ViewInCrm_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.NewValue = true;
		}

		#endregion

		#region Helpers

		protected virtual (string[], string[]) MapBAccountTypesToStringList()
		{
			List<string> values = new List<string>();
			List<string> labels = new List<string>();

			foreach (var bAccountType in BAccountTypes)
			{
				switch (bAccountType.Name)
				{
					case nameof(BAccountType.vendorType):
						values.Add(BAccountType.VendorType);
						labels.Add(Messages.VendorType);
						break;
					case nameof(BAccountType.customerType):
						values.Add(BAccountType.CustomerType);
						labels.Add(Messages.CustomerType);
						break;
					case nameof(BAccountType.combinedType):
						values.Add(BAccountType.CombinedType);
						labels.Add(Messages.CombinedType);
						break;
					case nameof(BAccountType.employeeType):
						values.Add(BAccountType.EmployeeType);
						labels.Add(Messages.EmployeeType);
						break;
					case nameof(BAccountType.empCombinedType):
						values.Add(BAccountType.EmpCombinedType);
						labels.Add(Messages.EmpCombinedType);
						break;
					case nameof(BAccountType.prospectType):
						values.Add(BAccountType.ProspectType);
						labels.Add(Messages.ProspectType);
						break;
					case nameof(BAccountType.branchType):
						values.Add(BAccountType.BranchType);
						labels.Add(Messages.BranchType);
						break;
					case nameof(BAccountType.organizationType):
						values.Add(BAccountType.OrganizationType);
						labels.Add(Messages.OrganizationType);
						break;
				}
			}

			return (values.ToArray(), labels.ToArray());
		}

		protected virtual Type Field<TField>()
		{
			return EntityType.GetNestedType(typeof(TField).Name, BindingFlags.Public | BindingFlags.IgnoreCase, true);
		}

		#endregion
	}

	#endregion

	#region BAccountAttribute

	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class BAccountAttribute : BAccountRawAttribute
	{
		public BAccountAttribute()	// for BC reducing purposes only. TODO: remove sometime...
			: this(null, null, null, null) { }

		public BAccountAttribute(Type[] bAccountTypes = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(typeof(BAccountR), bAccountTypes, customSearchQuery, fieldList, headerList) { }
	}

	#endregion

	#region CRMBAccountAttribute

	// Restricted version of selector is constructed with an overridden PrimaryGraphAttribute. Such entites will be opened only by using CRM screens
	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class CRMBAccountAttribute : BAccountRawAttribute
	{
		public CRMBAccountAttribute(Type[] bAccountTypes = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(typeof(BAccount), bAccountTypes, customSearchQuery, fieldList, headerList)
		{
			ViewInCRM = true;
		}
	}

	#endregion

	#region CustomerAndProspectAttribute

	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class CustomerAndProspectAttribute : BAccountAttribute
	{
		public CustomerAndProspectAttribute(Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(bAccountTypes: new[]
				{
					typeof(BAccountType.prospectType),
					typeof(BAccountType.customerType),
					typeof(BAccountType.combinedType),
				},
				customSearchQuery, fieldList, headerList)
		{
			this.DisplayName = "Customer";
		}
	}

	#endregion

	#region CustomerProspectVendorAttribute

	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class CustomerProspectVendorAttribute : BAccountAttribute
	{
		public CustomerProspectVendorAttribute(Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(bAccountTypes: new[]
				{
					typeof(BAccountType.prospectType),
					typeof(BAccountType.customerType),
					typeof(BAccountType.combinedType),
					typeof(BAccountType.vendorType),
				},
				customSearchQuery, fieldList, headerList)
		{
		}
	}

	#endregion

	#region ParentBAccountAttribute

	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class ParentBAccountAttribute : BAccountAttribute
	{
		public ParentBAccountAttribute(Type bAccountIDField, Type[] bAccountTypes = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(bAccountTypes: bAccountTypes ?? new[]
			{
				typeof(BAccountType.prospectType),
				typeof(BAccountType.customerType),
				typeof(BAccountType.combinedType),
				typeof(BAccountType.vendorType),
			}, customSearchQuery ?? CreateSelectWithRestriction(bAccountIDField), fieldList, headerList)
		{
			this.DisplayName = "Parent Account";
		}

		protected static Type CreateSelectWithRestriction(Type bAccountIDField)
		{
			return BqlTemplate.OfCommand<
					Search2<
						BAccountR.bAccountID,
					LeftJoin<Contact,
						On<Contact.bAccountID, Equal<BAccountR.bAccountID>,
						And<Contact.contactID, Equal<BAccountR.defContactID>>>,
					LeftJoin<Address,
						On<Address.bAccountID, Equal<BAccountR.bAccountID>,
						And<Address.addressID, Equal<BAccountR.defAddressID>>>>>,
					Where<
						Brackets<
							BqlPlaceholder.A.AsField.FromCurrent.IsNull
							.Or<BAccountR.bAccountID.IsNotEqual<BqlPlaceholder.A.AsField.FromCurrent>>
						>
						.And<Match<Current<AccessInfo.userName>>>>>>
				.Replace<BqlPlaceholder.A>(bAccountIDField)
				.ToType();
		}
	}

	#endregion

	#region ParentBAccountAttribute

	[PXAttributeFamily(typeof(AcctSubAttribute))]
	public class CRMParentBAccountAttribute : ParentBAccountAttribute
	{
		public CRMParentBAccountAttribute(Type bAccountIDField, Type[] bAccountTypes = null, Type customSearchQuery = null, Type[] fieldList = null, string[] headerList = null)
			: base(bAccountIDField, bAccountTypes, customSearchQuery, fieldList, headerList)
		{
			this.ViewInCRM = true;
		}
	}

	#endregion
}
