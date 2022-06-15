﻿using PX.Data;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.GDPR
{
	[PXDBInt]
	[PXDefault(PXPseudonymizationStatusListAttribute.NotPseudonymized, PersistingCheck = PXPersistingCheck.Nothing)]
	[PXUIField(DisplayName = Messages.Pseudonymized, FieldClass = FeaturesSet.gDPRCompliance.FieldClass, IsReadOnly = true, Visible = false)]
	[PXPseudonymizationStatusList]
	public class PXPseudonymizationStatusFieldAttribute : AcctSubAttribute { }
	
	[PXDBBool]
	[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
	[PXMassUpdatableField]
	[PXContactInfoField]
	[PXUIField(DisplayName = Messages.IsConsented, FieldClass = FeaturesSet.gDPRCompliance.FieldClass)]
	public class PXConsentAgreementFieldAttribute : AcctSubAttribute, IPXFieldSelectingSubscriber, IPXFieldUpdatedSubscriber
	{
		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.gDPRCompliance>() || e.Row == null)
				return;

			var isOptedIn = sender.GetValue(e.Row, nameof(IConsentable.ConsentAgreement)) as bool?;

			var status = sender.GetStatus(e.Row);

			var uiAttribute = sender
								.GetAttributesOfType<PXUIFieldAttribute>(e.Row, nameof(IConsentable.ConsentAgreement))
								.FirstOrDefault();

			if (isOptedIn != true && uiAttribute?.Enabled == true)
			{
				e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null,
					Messages.NoConsent,
					PXErrorLevel.Warning,
					null, null, null, PXUIVisibility.Undefined, null, null, null);
			}
			else
			{
				e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null,
					null,
					PXErrorLevel.Undefined,
					null, null, null, PXUIVisibility.Undefined, null, null, null);
			}
		}

		public virtual void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.Row == null)
				return;

			var value = sender.GetValue(e.Row, nameof(IConsentable.ConsentDate));
			if (value != null)
				return;

			var newValue = sender.GetValue(e.Row, FieldName) as bool?;
			if (newValue != true)
				return;

			sender.SetValue(e.Row, nameof(IConsentable.ConsentDate), sender.Graph.Accessinfo.BusinessDate);
		}
	}

	[PXDBDate]
	[PXMassUpdatableField]
	[PXContactInfoField]
	[PXUIField(DisplayName = Messages.DateOfConsent, FieldClass = FeaturesSet.gDPRCompliance.FieldClass)]
	public class PXConsentDateFieldAttribute : AcctSubAttribute, IPXFieldSelectingSubscriber, IPXFieldVerifyingSubscriber
	{
		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row == null)
				return;

			var value = sender.GetValue(e.Row, nameof(IConsentable.ConsentAgreement));
			if (value == null)
				return;

			var isOptedIn = (bool)value;

			Required = isOptedIn;
		}

		public virtual void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null)
				return;

			var value = sender.GetValue(e.Row, nameof(IConsentable.ConsentAgreement));
			if (value == null)
				return;

			var isOptedIn = (bool)value;

			if (isOptedIn && e.NewValue == null)
				sender.RaiseExceptionHandling(
					FieldName,
					e.Row,
					sender.Graph.Accessinfo.BusinessDate,
					new PXSetPropertyException(Messages.ConsentDateNull));
		}
	}

	[PXDBDate]
	[PXMassUpdatableField]
	[PXContactInfoField]
	[PXUIField(DisplayName = Messages.ConsentExpires, FieldClass = FeaturesSet.gDPRCompliance.FieldClass)]
	public class PXConsentExpirationDateFieldAttribute : AcctSubAttribute, IPXFieldSelectingSubscriber
	{
		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.gDPRCompliance>() || e.Row == null)
				return;

			var thisValue = sender.GetValue(e.Row, FieldName);
			if (thisValue == null)
				return;
			
			var optedInValue = sender.GetValue(e.Row, nameof(IConsentable.ConsentAgreement));
			if (optedInValue == null)
				return;

			var isOptedIn = (bool)optedInValue;
			var expirationDate = (DateTime?)thisValue;
			var status = sender.GetStatus(e.Row);

			if (isOptedIn && expirationDate < sender.Graph.Accessinfo.BusinessDate && status != PXEntryStatus.Inserted)
			{
				e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null,
					Messages.ConsentExpired,
					PXErrorLevel.Warning,
					null, null, null, PXUIVisibility.Undefined, null, null, null);
			}
			else
			{
				e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null,
					null,
					PXErrorLevel.Undefined,
					null, null, null, PXUIVisibility.Undefined, null, null, null);
			}
		}
	}

	public interface IPostPseudonymizable
	{
		// restrics contains all necessary parameters for PXDatabase.Update
		List<PXDataFieldParam> InterruptPseudonimyzationHandler(List<PXDataFieldParam> restricts);
	}

	public interface IPostRestorable
	{
		// restrics contains all necessary parameters for PXDatabase.Update
		List<PXDataFieldParam> InterruptRestorationHandler(List<PXDataFieldParam> restricts);
	}

  	/// <summary>
	/// Represents an entity that should have the consent of a person to proceed.
	/// </summary>
	/// <remarks>
	/// The interface is used by the <see cref="FeaturesSet.GDPRCompliance">GDPR feature</see>.
	/// </remarks>
	public interface IConsentable
	{
		/// <summary>
		/// Specifies whether the person has given the consent to process the personal data.
		/// </summary>
		/// <value>
		/// The default value is <see langword="false"/>.
		/// </value>
		bool? ConsentAgreement { get; set; }

		/// <summary>
		/// The date when the person has given the consent to process the personal data.
		/// </summary>
		/// <value>
		/// The consent date.
		/// </value>
		DateTime? ConsentDate { get; set; }

		/// <summary>
		/// The date when the consent given by the person will be revoked.
		/// If this box is empty, the consent will never be revoked.
		/// </summary>
		/// <value>
		/// The consent expiration date.
		/// </value>
		DateTime? ConsentExpirationDate { get; set; }
	}
}
