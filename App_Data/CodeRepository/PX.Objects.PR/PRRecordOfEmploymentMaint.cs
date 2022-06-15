using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.GL;
using System;
using System.Collections;
using System.Threading;
using System.Xml;

namespace PX.Objects.PR
{
	public class PRRecordOfEmploymentMaint : PXGraph<PRRecordOfEmploymentMaint, PRRecordOfEmployment>
	{
		public SelectFrom<PRRecordOfEmployment>.View Document;
		public SelectFrom<PRRecordOfEmployment>
			.Where<PRRecordOfEmployment.refNbr.IsEqual<PRRecordOfEmployment.refNbr.FromCurrent>>.View CurrentDocument;
		public SelectFrom<Address>.Where<Address.addressID.IsEqual<PRRecordOfEmployment.addressID.FromCurrent>>.View Address;

		public PXSetup<PRSetup> Preferences;

		public SelectFrom<PRROEStatutoryHolidayPay>
			.Where<PRROEStatutoryHolidayPay.refNbr.IsEqual<PRRecordOfEmployment.refNbr.AsOptional>>
			.View StatutoryHolidays;

		public SelectFrom<PRROEOtherMonies>
			.Where<PRROEOtherMonies.refNbr.IsEqual<PRRecordOfEmployment.refNbr.AsOptional>>
			.View OtherMonies;

		public SelectFrom<PRROEInsurableEarningsByPayPeriod>
			.Where<PRROEInsurableEarningsByPayPeriod.refNbr.IsEqual<PRRecordOfEmployment.refNbr.AsOptional>>
			.View InsurableEarnings;

		#region Actions
		public PXAction<PRRecordOfEmployment> Export;
		[PXUIField(DisplayName = "Export", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable export(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				GenerateXmlFile();
			});
			return adapter.Get();
		}

		public PXAction<PRRecordOfEmployment> Reopen;
		[PXUIField(DisplayName = "Reopen", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable reopen(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<PRRecordOfEmployment> MarkAsSubmitted;
		[PXUIField(DisplayName = "Mark as Submitted", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable markAsSubmitted(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				string roeUri = "https://www.canada.ca/en/employment-social-development/programs/ei/ei-list/ei-roe/access-roe.html";
				throw new PXRedirectToUrlException(roeUri, PXBaseRedirectException.WindowMode.New, true, "Redirect:" + roeUri);
			});

			return adapter.Get();
		}

		public PXAction<PRRecordOfEmployment> Amend;
		[PXUIField(DisplayName = "Amend", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable amend(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				PRRecordOfEmploymentMaint roeGraph = PXGraph.CreateInstance<PRRecordOfEmploymentMaint>();

				PRRecordOfEmployment originalROE = Document.Current;

				PRRecordOfEmployment amendmentROE = PXCache<PRRecordOfEmployment>.CreateCopy(originalROE);
				amendmentROE.Amendment = true;
				amendmentROE.RefNbr = null;
				amendmentROE.AmendedRefNbr = originalROE.RefNbr;
				amendmentROE.NoteID = Guid.NewGuid();
				amendmentROE = roeGraph.Document.Insert(amendmentROE);
				roeGraph.Actions.PressSave();

				foreach (PRROEInsurableEarningsByPayPeriod originalRecord in roeGraph.InsurableEarnings.Select(originalROE.RefNbr))
				{
					PRROEInsurableEarningsByPayPeriod newRecord = PXCache<PRROEInsurableEarningsByPayPeriod>.CreateCopy(originalRecord);
					newRecord.RefNbr = amendmentROE.RefNbr;
					roeGraph.InsurableEarnings.Insert(newRecord);
				}

				foreach (PRROEStatutoryHolidayPay originalRecord in roeGraph.StatutoryHolidays.Select(originalROE.RefNbr))
				{
					PRROEStatutoryHolidayPay newRecord = PXCache<PRROEStatutoryHolidayPay>.CreateCopy(originalRecord);
					newRecord.RefNbr = amendmentROE.RefNbr;
					roeGraph.StatutoryHolidays.Insert(newRecord);
				}

				foreach (PRROEOtherMonies originalRecord in roeGraph.OtherMonies.Select(originalROE.RefNbr))
				{
					PRROEOtherMonies newRecord = PXCache<PRROEOtherMonies>.CreateCopy(originalRecord);
					newRecord.RefNbr = amendmentROE.RefNbr;
					roeGraph.OtherMonies.Insert(newRecord);
				}

				roeGraph.Actions.PressSave();
				Document.Current = PRRecordOfEmployment.PK.Find(roeGraph, amendmentROE.AmendedRefNbr);
			});
			return adapter.Get();
		}

		public PXAction<PRRecordOfEmployment> ShowFinalPaycheck;
		[PXUIField(DisplayName = "Show Final Paycheck", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual void showFinalPaycheck()
		{
			PRRecordOfEmployment recordOfEmployment = Document.Current;
			if (!string.IsNullOrWhiteSpace(recordOfEmployment.OrigDocType) && !string.IsNullOrWhiteSpace(recordOfEmployment.OrigRefNbr))
			{
				PRPayment payment = PRPayment.PK.Find(this, recordOfEmployment.OrigDocType, recordOfEmployment.OrigRefNbr);

				PRPayChecksAndAdjustments graph = PXGraph.CreateInstance<PRPayChecksAndAdjustments>();
				graph.Document.Current = payment;
				throw new PXRedirectRequiredException(graph, true, "Pay Checks and Adjustments");
			}
		}
		#endregion

		#region Event Handlers

		protected void _(Events.RowSelected<PRRecordOfEmployment> e)
		{
			PRRecordOfEmployment currentRecord = e.Row;
			if (e.Row == null)
			{
				return;
			}

			PXEntryStatus entryStatus = e.Cache.GetStatus(e.Row);

			string roeStatus = currentRecord.Status;
			Document.Cache.AllowDelete = roeStatus == ROEStatus.Open || roeStatus == ROEStatus.Exported;

			Export.SetEnabled(entryStatus != PXEntryStatus.Inserted);
			bool amendmentFieldsEnabed = e.Row.OrigDocType == null && e.Row.OrigRefNbr == null && (roeStatus == ROEStatus.Open || roeStatus == ROEStatus.Exported);
			PXUIFieldAttribute.SetEnabled<PRRecordOfEmployment.amendment>(e.Cache, e.Row, amendmentFieldsEnabed);
			PXUIFieldAttribute.SetEnabled<PRRecordOfEmployment.amendedRefNbr>(e.Cache, e.Row, amendmentFieldsEnabed);

			PXUIFieldAttribute.SetEnabled<PRRecordOfEmployment.employeeID>(e.Cache, e.Row, string.IsNullOrWhiteSpace(e.Row.OrigDocType) && string.IsNullOrWhiteSpace(e.Row.OrigRefNbr));
						
			ShowFinalPaycheck.SetEnabled(!string.IsNullOrWhiteSpace(Document.Current.OrigDocType) && !string.IsNullOrWhiteSpace(Document.Current.OrigRefNbr));
		}

		protected virtual void _(Events.FieldUpdated<PRRecordOfEmployment, PRRecordOfEmployment.branchID> e)
		{
			if (e.Row == null)
			{
				return;
			}

			BAccount bAccount = PXSelectJoin<BAccountR,
				InnerJoin<Branch, On<Branch.bAccountID, Equal<BAccountR.bAccountID>>>,
				Where<Branch.branchID, Equal<Required<Branch.branchID>>>>.Select(this, e.NewValue);

			e.Row.AddressID = bAccount.DefAddressID;
			e.Row.CRAPayrollAccountNumber = PXCache<BAccount>.GetExtension<PRxBAccount>(bAccount)?.CRAPayrollAccountNumber;
		}

		protected virtual void _(Events.FieldUpdated<Address, Address.countryID> e)
		{
			if (e.Row != null && e.OldValue != e.NewValue)
			{
				e.Row.State = null;
			}
		}

		protected virtual void _(Events.RowInserted<Address> e)
		{
			if (e.Row != null && e.Row.AddressID > 0)
			{
				Document.Current.AddressID = e.Row.AddressID;
			}
		}

		protected virtual void _(Events.RowPersisting<PRRecordOfEmployment> e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (e.Row.CRAPayrollAccountNumber == null)
			{
				e.Cache.RaiseExceptionHandling<PRRecordOfEmployment.craPayrollAccountNumber>(
					e.Row,
					null,
					new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(e.Row.CRAPayrollAccountNumber))));
			}

			if (e.Row.AddressID == null || e.Row.AddressID < 1)
			{
				e.Cache.RaiseExceptionHandling<PRRecordOfEmployment.addressID>(
					e.Row,
					null,
					new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(e.Row.AddressID))));
			}
		}

		protected virtual void _(Events.RowDeleted<PRRecordOfEmployment> e)
		{
			if (!string.IsNullOrWhiteSpace(e.Row?.AmendedRefNbr))
			{
				PRRecordOfEmployment amendedRoe = PRRecordOfEmployment.PK.Find(e.Cache.Graph, e.Row.AmendedRefNbr);
				amendedRoe.Status = ROEStatus.NeedsAmendment;
				Document.Cache.Update(amendedRoe);
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.EmployerNameB4)]
		protected virtual void _(Events.CacheAttached<PRRecordOfEmployment.branchID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.AddressLine1B4)]
		protected virtual void _(Events.CacheAttached<Address.addressLine1> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.AddressLine2B4)]
		protected virtual void _(Events.CacheAttached<Address.addressLine2> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.CityB4)]
		protected virtual void _(Events.CacheAttached<Address.city> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.CountryB4)]
		protected virtual void _(Events.CacheAttached<Address.countryID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.StateB4)]
		protected virtual void _(Events.CacheAttached<Address.state> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = Messages.PostalCodeB4)]
		protected virtual void _(Events.CacheAttached<Address.postalCode> e) { }
		#endregion

		public virtual void GenerateXmlFile()
		{
			PRRecordOfEmploymentMaint roeGraph = PXGraph.CreateInstance<PRRecordOfEmploymentMaint>();
			PRRecordOfEmployment roe = Document.Current;
			roeGraph.Document.Current = roe;

			XmlDocument xmlDocument = new XmlDocument();
			XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "");
			xmlDocument.AppendChild(xmlDeclaration);

			AppendHeader(xmlDocument);
			AppendROETag(xmlDocument, roeGraph);

			byte[] xmlFile;
			using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
			{
				xmlDocument.Save(memoryStream);
				xmlFile = memoryStream.ToArray();
			}

			var fileInfo = new PX.SM.FileInfo(string.Format("RecordOfEmployment_{0}.xml", roe.RefNbr), null, xmlFile);
			throw new PXRedirectToFileException(fileInfo, true);
		}

		public virtual void AppendHeader(XmlDocument xmlDocument)
		{
			XmlElement roeHeader = xmlDocument.CreateElement("ROEHEADER");
			SetAttribute(xmlDocument, roeHeader, "FileVersion", "W-2.0");
			SetAttribute(xmlDocument, roeHeader, "SoftwareVendor", "Acumatica");
			SetAttribute(xmlDocument, roeHeader, "ProductName", "Acumatica Payroll");
			SetAttribute(xmlDocument, roeHeader, "ProductVersion", PXVersionInfo.Version);
			xmlDocument.AppendChild(roeHeader);
		}

		public virtual void AppendROETag(XmlDocument xmlDocument, PRRecordOfEmploymentMaint roeGraph)
		{
			PRRecordOfEmployment roe = roeGraph.Document.Current;
			PREmployee currentEmployee = PREmployee.PK.Find(roeGraph, roe.EmployeeID);
			Contact contact = SelectFrom<Contact>.Where<Contact.contactID.IsEqual<P.AsInt>>.View.SelectSingleBound(roeGraph, null, currentEmployee.DefContactID);
			Address employeeAddress = CR.Address.PK.Find(roeGraph, currentEmployee.DefAddressID);
			PREmployeeAttribute socialSecurityNumberAttribute = SelectFrom<PREmployeeAttribute>
				.Where<PREmployeeAttribute.bAccountID.IsEqual<P.AsInt>
					.And<PREmployeeAttribute.aatrixMapping.IsEqual<P.AsInt>>>
				.View.SelectSingleBound(roeGraph, null, roe.EmployeeID, PX.Payroll.AatrixField.EMP.SocialSecurityNumber);

			string firstName = GetTruncatedString(contact?.FirstName ?? currentEmployee.AcctName, 20);
			string initials = GetTruncatedString(contact?.MidName, 4);
			string lastName = GetTruncatedString(contact?.LastName ?? currentEmployee.AcctName, 28);
			string phone = contact?.Phone1 ?? string.Empty;
			string areaCode = string.Empty;
			string phoneNumber = string.Empty;
			phone = phone.Replace("+1", string.Empty).Replace(" ", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty).Replace("-", string.Empty);
			if (phone.Length >= 10)
			{
				areaCode = phone.Substring(0, 3);
				phoneNumber = phone.Substring(3, 7);
			}
			string addressLine1 = string.Empty;
			if (!string.IsNullOrEmpty(employeeAddress.AddressLine1) && !string.IsNullOrEmpty(employeeAddress.AddressLine2))
			{
				addressLine1 = GetTruncatedString(employeeAddress.AddressLine1 + ", " + employeeAddress.AddressLine2, 35);
			}
			else if (!string.IsNullOrEmpty(employeeAddress.AddressLine1))
			{
				addressLine1 = GetTruncatedString(employeeAddress.AddressLine1, 35);
			}
			else if (!string.IsNullOrEmpty(employeeAddress.AddressLine2))
			{
				addressLine1 = GetTruncatedString(employeeAddress.AddressLine2, 35);
			}
			string addressLine2 = GetTruncatedString(employeeAddress.City, 35);
			string addressLine3 = GetTruncatedString(employeeAddress.State + ", " + employeeAddress.CountryID, 35);
			string postalCode = GetTruncatedString(employeeAddress.PostalCode, 10);

			XmlElement roeTag = xmlDocument.CreateElement("ROE");
			SetAttribute(xmlDocument, roeTag, "PrintingLanguage", "E");
			SetAttribute(xmlDocument, roeTag, "Issue", "D");

			if (roe.Amendment == true && !string.IsNullOrWhiteSpace(roe.AmendedRefNbr))
			{
				AppendNode(xmlDocument, roeTag, "B2", roe.AmendedRefNbr);
			}
			AppendNode(xmlDocument, roeTag, "B5", roe.CRAPayrollAccountNumber);
			AppendNode(xmlDocument, roeTag, "B6", GetPeriodType(roe.PeriodType));
			AppendNode(xmlDocument, roeTag, "B8", socialSecurityNumberAttribute.Value);

			XmlNode b9 = xmlDocument.CreateElement("B9");

			AppendNode(xmlDocument, b9, "FN", firstName);
			if (!string.IsNullOrWhiteSpace(initials))
			{
				AppendNode(xmlDocument, b9, "MN", initials);
			}
			AppendNode(xmlDocument, b9, "LN", lastName);
			AppendNode(xmlDocument, b9, "A1", addressLine1);
			AppendNode(xmlDocument, b9, "A2", addressLine2);
			AppendNode(xmlDocument, b9, "A3", addressLine3);
			AppendNode(xmlDocument, b9, "PC", postalCode);
			roeTag.AppendChild(b9);

			if (roe.FirstDayWorked != null)
			{
				AppendNode(xmlDocument, roeTag, "B10", roe.FirstDayWorked.Value.ToString("yyyy-MM-dd"));
			}
			if (roe.LastDayForWhichPaid != null)
			{
				AppendNode(xmlDocument, roeTag, "B11", roe.LastDayForWhichPaid.Value.ToString("yyyy-MM-dd"));
			}
			if (roe.FinalPayPeriodEndingDate != null)
			{
				AppendNode(xmlDocument, roeTag, "B12", roe.FinalPayPeriodEndingDate.Value.ToString("yyyy-MM-dd"));
			}

			XmlNode b14 = xmlDocument.CreateElement("B14");
			AppendNode(xmlDocument, b14, "CD", "U");

			AppendNode(xmlDocument, roeTag, "B15A", roe.TotalInsurableHours.GetValueOrDefault().ToString("0.00"));
			AppendNode(xmlDocument, roeTag, "B15B", roe.TotalInsurableEarnings.GetValueOrDefault().ToString("0.00"));

			XmlNode b15c = xmlDocument.CreateElement("B15C");
			int payPeriodNumber = 0;
			foreach (PRROEInsurableEarningsByPayPeriod insurableEarningsByPayPeriod in roeGraph.InsurableEarnings.Select())
			{
				payPeriodNumber++;
				XmlElement pp = xmlDocument.CreateElement("PP");
				SetAttribute(xmlDocument, pp, "nbr", payPeriodNumber.ToString());
				b15c.AppendChild(pp);

				XmlNode amt = xmlDocument.CreateElement("AMT");
				amt.InnerText = insurableEarningsByPayPeriod.InsurableEarnings.GetValueOrDefault().ToString("0.00");
				b15c.AppendChild(amt);
			}
			roeTag.AppendChild(b15c);

			XmlNode b16 = xmlDocument.CreateElement("B16");
			AppendNode(xmlDocument, b16, "CD", roe.ReasonForROE);
			AppendNode(xmlDocument, b16, "FN", firstName);
			AppendNode(xmlDocument, b16, "LN", lastName);
			AppendNode(xmlDocument, b16, "AC", areaCode);
			AppendNode(xmlDocument, b16, "TEL", phoneNumber);
			roeTag.AppendChild(b16);

			XmlNode b17B = xmlDocument.CreateElement("B17B");
			int holidayNumber = 0;
			foreach (PRROEStatutoryHolidayPay statutoryHolidayPay in roeGraph.StatutoryHolidays.Select())
			{
				holidayNumber++;
				XmlElement sh = xmlDocument.CreateElement("SH");
				SetAttribute(xmlDocument, sh, "nbr", holidayNumber.ToString());
				b17B.AppendChild(sh);

				XmlNode dt = xmlDocument.CreateElement("DT");
				dt.InnerText = statutoryHolidayPay.Date.Value.ToString("yyyy-MM-dd");
				b17B.AppendChild(dt);

				XmlNode amt = xmlDocument.CreateElement("AMT");
				amt.InnerText = statutoryHolidayPay.Amount.GetValueOrDefault().ToString("0.00");
				b17B.AppendChild(amt);
					
				if (holidayNumber >= 10)
				{
					break;
				}
			}
			roeTag.AppendChild(b17B);

			XmlNode b17c = xmlDocument.CreateElement("B17C");
			int otherMoniesNumber = 0;
			foreach (PRROEOtherMonies otherMonies in roeGraph.OtherMonies.Select())
			{
				otherMoniesNumber++;
				XmlElement om = xmlDocument.CreateElement("OM");
				SetAttribute(xmlDocument, om, "nbr", holidayNumber.ToString());
				b17c.AppendChild(om);

				XmlElement cd = xmlDocument.CreateElement("CD");
				cd.InnerText = "B11";
				b17c.AppendChild(om);

				XmlNode amt = xmlDocument.CreateElement("AMT");
				amt.InnerText = otherMonies.Amount.GetValueOrDefault().ToString("0.00");
				b17c.AppendChild(amt);

				if (otherMoniesNumber >= 3)
				{
					break;
				}
			}
			roeTag.AppendChild(b17c);

			AppendNode(xmlDocument, roeTag, "B18", roe.Comments);

			xmlDocument.DocumentElement.AppendChild(roeTag);
		}



		public static string GetTruncatedString(string value, int length)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			if (value.Length < length)
			{
				return value;
			}

			return value.Substring(0, length);
		}

		public static void AppendNode(XmlDocument xmlDocument, XmlNode parentNode, string nodeName, string nodeValue)
		{
			XmlNode xmlNode = xmlDocument.CreateElement(nodeName);
			xmlNode.InnerText = nodeValue;
			parentNode.AppendChild(xmlNode);
		}

		public static void SetAttribute(XmlDocument xmlDocument, XmlElement xmlElement, string attributeName, string attributeValue)
		{
			XmlAttribute xmlAttribute = xmlDocument.CreateAttribute(attributeName);
			xmlAttribute.Value = attributeValue;
			xmlElement.SetAttributeNode(xmlAttribute);
		}

		public static string GetPeriodType(string periodType)
		{
			switch (periodType)
			{
				case FinPeriodType.BiWeek:
					return "W";
				case FinPeriodType.Month:
					return "M";
				case FinPeriodType.BiMonth:
					return "S";
				case FinPeriodType.CustomPeriodsNumber:
					return "S";
				case FinPeriodType.Week:
				default:
					return "W";
			}
		}

		#region Address Lookup Extension
		public class PRRecordOfEmploymentMaintAddressLookupExtension : CR.Extensions.AddressLookupExtension<PRRecordOfEmploymentMaint, PRRecordOfEmployment, Address>
		{
			protected override string AddressView => nameof(Base.Address);
		}

		#endregion
	}
}
