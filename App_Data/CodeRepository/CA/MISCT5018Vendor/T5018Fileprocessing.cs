// Decompiled
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using PX.Objects.Localizations.CA.Messages;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.GL.DAC;
using PX.SM;
using PX.Data.BQL.Fluent;

namespace PX.Objects.Localizations.CA
{
	public class T5018Fileprocessing : PXGraph<T5018Fileprocessing>
	{
		public PXCancel<T5018MasterTable> Cancel;

		public PXFilter<T5018MasterTable> MasterView;

		[PXVirtualDAC]
		public PXFilteredProcessing<T5018DetailsTable, T5018MasterTable> DetailsView;

		protected virtual void T5018MasterTable_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			T5018MasterTable masterTable = (T5018MasterTable)e.OldRow;
			T5018MasterTable masterTable2 = (T5018MasterTable)e.Row;
			if (masterTable.OrganizationID != masterTable2.OrganizationID)
			{
				DetailsView.Cache.Clear();
			}
		}

		protected void T5018MasterTable_ToDate_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			T5018MasterTable masterTable = (T5018MasterTable)e.Row;
			if (masterTable != null)
			{
				AssignHeaderDetails(masterTable);
			}
		}

		private void AssignHeaderDetails(T5018MasterTable row)
		{
			BAccount bAccount = null;
			Contact contact = null;
			Organization organization =
				PXSelectBase<Organization, PXSelect<Organization,
						Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>.Config>
					.Select(this, row.OrganizationID);
			if (organization != null)
			{
				bAccount =
					PXSelectBase<BAccount,
							PXSelect<BAccount, Where<BAccount.acctCD, Equal<Required<BAccount.acctCD>>>>.Config>
						.Select(this, organization.OrganizationCD);
			}

			EPEmployee ePEmployee =
				PXSelectBase<EPEmployee,
						PXSelect<EPEmployee, Where<EPEmployee.userID, Equal<Required<EPEmployee.userID>>>>.Config>
					.Select(this, Accessinfo.UserID);
			if (ePEmployee != null)
			{
				contact =
					PXSelectBase<Contact,
							PXSelect<Contact, Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.Config>
						.Select(this, ePEmployee.DefContactID);
			}

			if (organization != null)
			{
				row.AcctName = organization.OrganizationName;
				row.Phone2 = organization.CTelNumber;
				row.EMail = organization.CEmail;
			}

			if (bAccount != null)
			{
				row.TaxRegistrationID = bAccount.TaxRegistrationID;
			}

			if (contact != null)
			{
				row.FirstName = contact.FirstName;
				row.LastName = contact.LastName;
				row.Phone1 = contact.Phone1;
				row.Title = contact.Title;
			}

			row.Language = "E";
			row.FilingType = "O";
			if (row.FilingType == "O")
			{
				Random random = new Random();
				int num = random.Next(10000000, 99999999);
				row.SubmissionNo = string.Concat(num);
			}
		}

		protected void T5018MasterTable_FromDate_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			T5018MasterTable masterTable = (T5018MasterTable)e.Row;
			if (masterTable != null)
			{
				AssignHeaderDetails(masterTable);
			}
		}

		protected void T5018MasterTable_FilingType_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			T5018MasterTable masterTable = (T5018MasterTable)e.Row;
			if (masterTable.FilingType == "A" || masterTable.FilingType == "O")
			{
				Random random = new Random();
				int num = random.Next(10000000, 99999999);
				masterTable.SubmissionNo = string.Concat(num);
			}
		}

		protected void T5018MasterTable_SubmissionNo_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
		{
			T5018MasterTable masterTable = (T5018MasterTable)e.Row;
			string submissionNo = masterTable.SubmissionNo;
			ValidatePassword(submissionNo);
		}

		private bool ValidatePassword(string password)
		{
			Regex regex = new Regex("^([A-Za-z0-9]{8})$");
			if (password != null && !regex.IsMatch(password))
			{
				throw new PXSetPropertyException(T5018Messages.EightCharMax);
			}

			return true;
		}

		protected void T5018MasterTable_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			T5018MasterTable rowfilter = (T5018MasterTable)e.Row;
			if (rowfilter != null)
			{
				DetailsView.SetProcessDelegate(delegate (List<T5018DetailsTable> list)
				                               {
					                               T5018Fileprocessing mISCT5018Fileprocessing =
						                               PXGraph.CreateInstance<T5018Fileprocessing>();
					                               mISCT5018Fileprocessing.Process(list, rowfilter,
						                               mISCT5018Fileprocessing);
				                               });
			}
		}

		public void Process(List<T5018DetailsTable> list, T5018MasterTable rowfilter, T5018Fileprocessing graph)
		{
			if (list.Count == 0)
			{
				return;
			}

			decimal? num = default(decimal);
			Contact contact =
				PXSelectBase<Contact, PXSelectJoin<Contact,
						InnerJoin<Organization, On<Organization.bAccountID, Equal<Contact.bAccountID>,
							And<Organization.organizationName, Equal<Contact.fullName>>>>,
						Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>.Config>
					.Select(graph, rowfilter.OrganizationID);
			Address address =
				PXSelectBase<Address, PXSelectJoin<Address,
						InnerJoin<BAccount, On<Address.addressID, Equal<BAccount.defAddressID>>, InnerJoin<Organization,
							On<BAccount.bAccountID, Equal<Organization.bAccountID>>>>,
						Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>.Config>
					.Select(graph, rowfilter.OrganizationID);
			BAccount bAccount =
				PXSelectBase<BAccount, PXSelectJoin<BAccount,
						InnerJoin<Organization, On<BAccount.bAccountID, Equal<Organization.bAccountID>>>,
						Where<Organization.organizationID, Equal<Required<Organization.organizationID>>>>.Config>
					.Select(graph, rowfilter.OrganizationID);
			XmlDocument xmlDocument = new XmlDocument();
			XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "");
			XmlElement xmlElement = xmlDocument.CreateElement("Submission");
			xmlElement.SetAttribute("xmlns:ccms", "http://www.cra-arc.gc.ca/xmlns/ccms/1-0-0");
			xmlElement.SetAttribute("xmlns:sdt", "http://www.cra-arc.gc.ca/xmlns/sdt/2-2-0");
			xmlElement.SetAttribute("xmlns:ols",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols/1-0-1");
			xmlElement.SetAttribute("xmlns:ols1",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols1/1-0-1");
			xmlElement.SetAttribute("xmlns:ols10",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols10/1-0-1");
			xmlElement.SetAttribute("xmlns:ols100",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols100/1-0-1");
			xmlElement.SetAttribute("xmlns:ols12",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols12/1-0-1");
			xmlElement.SetAttribute("xmlns:ols125",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols125/1-0-1");
			xmlElement.SetAttribute("xmlns:ols140",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols140/1-0-1");
			xmlElement.SetAttribute("xmlns:ols141",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols141/1-0-1");
			xmlElement.SetAttribute("xmlns:ols2",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols2/1-0-1");
			xmlElement.SetAttribute("xmlns:ols5",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols5/1-0-1");
			xmlElement.SetAttribute("xmlns:ols50",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols50/1-0-1");
			xmlElement.SetAttribute("xmlns:ols52",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols52/1-0-1");
			xmlElement.SetAttribute("xmlns:ols6",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols6/1-0-1");
			xmlElement.SetAttribute("xmlns:ols8",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols8/1-0-1");
			xmlElement.SetAttribute("xmlns:ols8-1",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols8-1/1-0-1");
			xmlElement.SetAttribute("xmlns:ols9",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/ols9/1-0-1");
			xmlElement.SetAttribute("xmlns:olsbr",
			                        "http://www.cra-arc.gc.ca/enov/ol/interfaces/efile/partnership/olsbr/1-0-1");
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
			XmlAttribute xmlAttribute =
				xmlDocument.CreateAttribute("xsi", "noNamespaceSchemaLocation",
				                            "http://www.w3.org/2001/XMLSchema-instance");
			xmlAttribute.Value = "layout-topologie.xsd";
			xmlElement.SetAttributeNode(xmlAttribute);
			XmlNode xmlNode = xmlDocument.CreateElement("T619");
			XmlNode xmlNode2 = xmlDocument.CreateElement("sbmt_ref_id");
			xmlNode2.InnerText = rowfilter.SubmissionNo;
			xmlNode.AppendChild(xmlNode2);
			XmlNode xmlNode3 = xmlDocument.CreateElement("rpt_tcd");
			xmlNode3.InnerText = rowfilter.FilingType;
			xmlNode.AppendChild(xmlNode3);
			XmlNode xmlNode4 = xmlDocument.CreateElement("trnmtr_nbr");
			xmlNode4.InnerText = "MM555555";
			xmlNode.AppendChild(xmlNode4);
			XmlNode xmlNode5 = xmlDocument.CreateElement("trnmtr_tcd");
			xmlNode5.InnerText = "1";
			xmlNode.AppendChild(xmlNode5);
			XmlNode xmlNode6 = xmlDocument.CreateElement("summ_cnt");
			xmlNode6.InnerText = "1";
			xmlNode.AppendChild(xmlNode6);
			XmlNode xmlNode7 = xmlDocument.CreateElement("lang_cd");
			xmlNode7.InnerText = rowfilter.Language;
			xmlNode.AppendChild(xmlNode7);
			XmlNode xmlNode8 = xmlDocument.CreateElement("TRNMTR_NM");
			int num2 = 0;
			string text = null;
			string text2 = null;
			if (!string.IsNullOrEmpty(rowfilter.AcctName))
			{
				string acctName = rowfilter.AcctName;
				for (int i = 0; i < acctName.Length; i++)
				{
					char c = acctName[i];
					if (num2 <= 30)
					{
						text += c;
					}

					num2++;
					if (num2 > 31)
					{
						text2 += c;
					}
				}
			}

			XmlNode xmlNode9 = xmlDocument.CreateElement("l1_nm");
			xmlNode9.InnerText = text;
			xmlNode8.AppendChild(xmlNode9);
			if (num2 > 31)
			{
				XmlNode xmlNode10 = xmlDocument.CreateElement("l2_nm");
				xmlNode10.InnerText = text2;
				xmlNode8.AppendChild(xmlNode10);
			}

			xmlNode.AppendChild(xmlNode8);
			XmlNode xmlNode11 = xmlDocument.CreateElement("TRNMTR_ADDR");
			XmlNode xmlNode12 = xmlDocument.CreateElement("addr_l1_txt");
			xmlNode12.InnerText = address.AddressLine1;
			xmlNode11.AppendChild(xmlNode12);
			XmlNode xmlNode13 = xmlDocument.CreateElement("addr_l2_txt");
			xmlNode13.InnerText = address.AddressLine2;
			xmlNode11.AppendChild(xmlNode13);
			XmlNode xmlNode14 = xmlDocument.CreateElement("cty_nm");
			xmlNode14.InnerText = address.City;
			xmlNode11.AppendChild(xmlNode14);
			XmlNode xmlNode15 = xmlDocument.CreateElement("prov_cd");
			xmlNode15.InnerText = address.State;
			xmlNode11.AppendChild(xmlNode15);
			XmlNode xmlNode16 = xmlDocument.CreateElement("cntry_cd");
			if (address.CountryID == "US")
			{
				xmlNode16.InnerText = "USA";
			}
			else if (address.CountryID == "CA")
			{
				xmlNode16.InnerText = "CAN";
			}

			xmlNode11.AppendChild(xmlNode16);
			XmlNode xmlNode17 = xmlDocument.CreateElement("pstl_cd");
			xmlNode17.InnerText = address.PostalCode;
			xmlNode11.AppendChild(xmlNode17);
			xmlNode.AppendChild(xmlNode11);
			XmlNode xmlNode18 = xmlDocument.CreateElement("CNTC");
			XmlNode xmlNode19 = xmlDocument.CreateElement("cntc_nm");
			string text4 = (xmlNode19.InnerText = rowfilter.FirstName + " " + rowfilter.LastName);
			xmlNode18.AppendChild(xmlNode19);
			XmlNode xmlNode20 = xmlDocument.CreateElement("cntc_area_cd");
			xmlNode20.InnerText = rowfilter.AreaCode;
			xmlNode18.AppendChild(xmlNode20);
			XmlNode xmlNode21 = xmlDocument.CreateElement("cntc_phn_nbr");
			xmlNode21.InnerText = rowfilter.Phone1;
			xmlNode18.AppendChild(xmlNode21);
			XmlNode xmlNode22 = xmlDocument.CreateElement("cntc_extn_nbr");
			xmlNode22.InnerText = rowfilter.Phone2;
			xmlNode18.AppendChild(xmlNode22);
			XmlNode xmlNode23 = xmlDocument.CreateElement("cntc_email_area");
			xmlNode23.InnerText = rowfilter.EMail;
			xmlNode18.AppendChild(xmlNode23);
			XmlNode xmlNode24 = xmlDocument.CreateElement("sec_cntc_email_area");
			xmlNode24.InnerText = rowfilter.SecteMail;
			xmlNode18.AppendChild(xmlNode24);
			xmlNode.AppendChild(xmlNode18);
			xmlElement.AppendChild(xmlNode);
			XmlNode xmlNode25 = xmlDocument.CreateElement("Return");
			XmlNode xmlNode26 = xmlDocument.CreateElement("T5018");
			xmlNode25.AppendChild(xmlNode26);
			int num3 = 0;
			foreach (T5018DetailsTable item in list)
			{
				BAccountR bAccount2 =
					PXSelect<BAccountR, Where<BAccountR.acctCD, Equal<Required<BAccountR.acctCD>>>>
						.Select(this, item.VAcctCD.Trim());
				T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(bAccount2);
				Contact contact2 =
					SelectFrom<Contact>.Where<Contact.contactID.IsEqual<BAccount.primaryContactID.AsOptional>>.View
						.Select(graph, bAccount2.PrimaryContactID);

				if ((contact2 == null || String.IsNullOrWhiteSpace(contact2.LastName)) && (extension.BoxT5018 ?? 0) == 3)
				{
					PXProcessing<T5018DetailsTable>.SetError(list.IndexOf(item), T5018Messages.T5018IndividualEmptyPrimary);
				}
			}

			foreach (T5018DetailsTable item in list)
			{
				BAccountR bAccount2 =
					PXSelect<BAccountR, Where<BAccountR.acctCD, Equal<Required<BAccountR.acctCD>>>>
						.Select(this, item.VAcctCD.Trim());

				T5018BAccountExt extension = PXCache<BAccount>.GetExtension<T5018BAccountExt>(bAccount2);

				Contact contact2 =
					SelectFrom<Contact>.Where<Contact.contactID.IsEqual<BAccount.primaryContactID.AsOptional>>.View
						.Select(graph, bAccount2.PrimaryContactID);

				Address address2 =
					PXSelect<Address, Where<Address.bAccountID, Equal<Required<Address.bAccountID>>>>
						.Select(graph, bAccount2.BAccountID);

				num += item.CuryAdjdAmt;
				num3++;
				XmlNode xmlNode27 = xmlDocument.CreateElement("T5018Slip");
				xmlNode26.AppendChild(xmlNode27);

				XmlNode xmlNode33 = xmlDocument.CreateElement("sin");
				switch (extension.BoxT5018)
				{
					case 1:
						xmlNode33.InnerText = "";
						break;
					case 2:
						xmlNode33.InnerText = "";
						break;
					case 3:
						xmlNode33.InnerText = extension.SocialInsNum;
						break;
					default:
						xmlNode33.InnerText = "";
						break;
				}

				xmlNode27.AppendChild(xmlNode33);
				XmlNode xmlNode34 = xmlDocument.CreateElement("rcpnt_bn");
				if (extension.BusinessNum != null)
				{
					xmlNode34.InnerText = extension.BusinessNum;
				}
				else
				{
					xmlNode34.InnerText = "";
				}

				xmlNode27.AppendChild(xmlNode34);
				XmlNode xmlNode38 = xmlDocument.CreateElement("rcpnt_tcd");
				switch (extension.BoxT5018)
				{
					case 1:
					case 2:
						xmlNode38.InnerText = extension.BoxT5018 == 1 ? "3" : "4";
						XmlNode xmlNode35 = xmlDocument.CreateElement("CORP_PTNRP_NM");
						XmlNode xmlNode36 = xmlDocument.CreateElement("l1_nm");
						string corpName = item.VAcctName.Replace("&", "&amp;");
						xmlNode36.InnerText = corpName.Length > 30 ? corpName.Substring(0, 30) : corpName;
						xmlNode35.AppendChild(xmlNode36);
						XmlNode xmlNode37 = xmlDocument.CreateElement("l2_nm");
						xmlNode37.InnerText = corpName.Length > 30 ?
							(corpName.Substring(30).Length > 30 ?
								corpName.Substring(30, 30) :
								corpName.Substring(30)) :
							"";
						xmlNode35.AppendChild(xmlNode37);
						xmlNode27.AppendChild(xmlNode35);
						break;
					case 3:
						xmlNode38.InnerText = "1";
						XmlNode xmlNode28 = xmlDocument.CreateElement("RCPNT_NM");
						XmlNode xmlNode29 = xmlDocument.CreateElement("snm");
						XmlNode xmlNode30 = xmlDocument.CreateElement("gvn_nm");
						XmlNode xmlNode32 = xmlDocument.CreateElement("init");

						string givenName = contact2.FirstName != null ? contact2.FirstName.Split(' ')[0] : "";
						givenName = givenName.Length > 12 ? givenName.Substring(0, 12) : givenName;

						string surname = contact2.LastName;
						surname = surname.Length > 20 ? surname.Substring(0, 20) : surname;

						xmlNode29.InnerText = surname;
						xmlNode28.AppendChild(xmlNode29);

						xmlNode30.InnerText = givenName;
						xmlNode28.AppendChild(xmlNode30);


						xmlNode32.InnerText = "";
						xmlNode28.AppendChild(xmlNode32);
						xmlNode27.InsertBefore(xmlNode28, xmlNode33);
						break;
					default:
						xmlNode38.InnerText = "";
						break;
				}

				xmlNode27.AppendChild(xmlNode38);
				XmlNode xmlNode39 = xmlDocument.CreateElement("RCPNT_ADDR");
				XmlNode xmlNode40 = xmlDocument.CreateElement("addr_l1_txt");
				xmlNode40.InnerText = address2.AddressLine1;
				xmlNode39.AppendChild(xmlNode40);
				XmlNode xmlNode41 = xmlDocument.CreateElement("addr_l2_txt");
				if (address2.AddressLine2 != null)
				{
					xmlNode41.InnerText = address2.AddressLine2;
				}
				else
				{
					xmlNode41.InnerText = address2.AddressLine2;
				}

				xmlNode39.AppendChild(xmlNode41);
				XmlNode xmlNode42 = xmlDocument.CreateElement("cty_nm");
				xmlNode42.InnerText = address2.City;
				xmlNode39.AppendChild(xmlNode42);
				XmlNode xmlNode43 = xmlDocument.CreateElement("prov_cd");
				xmlNode43.InnerText = address2.State;
				xmlNode39.AppendChild(xmlNode43);
				XmlNode xmlNode44 = xmlDocument.CreateElement("cntry_cd");
				xmlNode44.InnerText = address2.CountryID;
				xmlNode39.AppendChild(xmlNode44);
				XmlNode xmlNode45 = xmlDocument.CreateElement("pstl_cd");
				xmlNode45.InnerText = address2.PostalCode;
				xmlNode39.AppendChild(xmlNode45);
				xmlNode27.AppendChild(xmlNode39);
				XmlNode xmlNode46 = xmlDocument.CreateElement("bn");
				xmlNode46.InnerText = extension.BusinessNum;
				xmlNode27.AppendChild(xmlNode46);
				XmlNode xmlNode47 = xmlDocument.CreateElement("sbctrcr_amt");
				decimal value = item.CuryAdjdAmt.Value;
				xmlNode47.InnerText = Math.Round(value, 2).ToString();
				xmlNode27.AppendChild(xmlNode47);
				XmlNode xmlNode48 = xmlDocument.CreateElement("ptnrp_filr_id");
				switch (extension.BoxT5018)
				{
					case 1:
						xmlNode48.InnerText = "1";
						break;
					case 2:
						xmlNode48.InnerText = extension.BusinessNum;
						break;
					case 3:
						xmlNode48.InnerText = "3";
						break;
					default:
						xmlNode48.InnerText = "";
						break;
				}

				xmlNode27.AppendChild(xmlNode48);
				XmlNode xmlNode49 = xmlDocument.CreateElement("rpt_tcd");
				xmlNode49.InnerText = rowfilter.FilingType;
				xmlNode27.AppendChild(xmlNode49);
				PXProcessing.SetProcessed();
			}

			XmlNode xmlNode50 = xmlDocument.CreateElement("T5018Summary");
			xmlNode26.AppendChild(xmlNode50);
			XmlNode xmlNode51 = xmlDocument.CreateElement("bn");
			xmlNode51.InnerText = bAccount.TaxRegistrationID;
			xmlNode50.AppendChild(xmlNode51);
			XmlNode xmlNode52 = xmlDocument.CreateElement("PAYR_NM");
			XmlNode xmlNode53 = xmlDocument.CreateElement("l1_nm");
			xmlNode53.InnerText = bAccount.AcctName;
			xmlNode52.AppendChild(xmlNode53);
			XmlNode xmlNode54 = xmlDocument.CreateElement("l2_nm");
			xmlNode54.InnerText = "";
			xmlNode52.AppendChild(xmlNode54);
			XmlNode xmlNode55 = xmlDocument.CreateElement("l3_nm");
			xmlNode55.InnerText = "";
			xmlNode52.AppendChild(xmlNode55);
			xmlNode50.AppendChild(xmlNode52);
			XmlNode xmlNode56 = xmlDocument.CreateElement("PAYR_ADDR");
			XmlNode xmlNode57 = xmlDocument.CreateElement("addr_l1_txt");
			xmlNode57.InnerText = address.AddressLine1;
			xmlNode56.AppendChild(xmlNode57);
			XmlNode xmlNode58 = xmlDocument.CreateElement("addr_l2_txt");
			xmlNode58.InnerText = address.AddressLine2;
			xmlNode56.AppendChild(xmlNode58);
			XmlNode xmlNode59 = xmlDocument.CreateElement("cty_nm");
			xmlNode59.InnerText = address.City;
			xmlNode56.AppendChild(xmlNode59);
			XmlNode xmlNode60 = xmlDocument.CreateElement("prov_cd");
			xmlNode60.InnerText = address.State;
			xmlNode56.AppendChild(xmlNode60);
			XmlNode xmlNode61 = xmlDocument.CreateElement("cntry_cd");
			xmlNode61.InnerText = address.CountryID;
			xmlNode56.AppendChild(xmlNode61);
			XmlNode xmlNode62 = xmlDocument.CreateElement("pstl_cd");
			xmlNode62.InnerText = address.PostalCode;
			xmlNode56.AppendChild(xmlNode62);
			xmlNode50.AppendChild(xmlNode56);
			XmlNode xmlNode63 = xmlDocument.CreateElement("CNTC");
			XmlNode xmlNode64 = xmlDocument.CreateElement("cntc_nm");
			string text8 = (xmlNode64.InnerText = rowfilter.FirstName + " " + rowfilter.LastName);
			xmlNode63.AppendChild(xmlNode64);
			XmlNode xmlNode65 = xmlDocument.CreateElement("cntc_area_cd");
			xmlNode65.InnerText = rowfilter.AreaCode;
			xmlNode63.AppendChild(xmlNode65);
			XmlNode xmlNode66 = xmlDocument.CreateElement("cntc_phn_nbr");
			xmlNode66.InnerText = rowfilter.Phone1;
			xmlNode63.AppendChild(xmlNode66);
			XmlNode xmlNode67 = xmlDocument.CreateElement("cntc_extn_nbr");
			xmlNode67.InnerText = rowfilter.Phone2;
			xmlNode63.AppendChild(xmlNode67);
			xmlNode50.AppendChild(xmlNode63);
			XmlNode xmlNode68 = xmlDocument.CreateElement("PRD_END_DT");
			XmlNode xmlNode69 = xmlDocument.CreateElement("dy");
			DateTime dateTime =
				Convert.ToDateTime(rowfilter.ToDate, CultureInfo.GetCultureInfo("ur-PK").DateTimeFormat);
			int day = dateTime.Day;
			int month = dateTime.Month;
			int year = dateTime.Year;
			xmlNode69.InnerText = dateTime.Day.ToString();
			xmlNode68.AppendChild(xmlNode69);
			XmlNode xmlNode70 = xmlDocument.CreateElement("mo");
			xmlNode70.InnerText = dateTime.Month.ToString();
			xmlNode68.AppendChild(xmlNode70);
			XmlNode xmlNode71 = xmlDocument.CreateElement("yr");
			xmlNode71.InnerText = dateTime.Year.ToString();
			xmlNode68.AppendChild(xmlNode71);
			xmlNode50.AppendChild(xmlNode68);
			XmlNode xmlNode72 = xmlDocument.CreateElement("slp_cnt");
			xmlNode72.InnerText = num3.ToString();
			xmlNode50.AppendChild(xmlNode72);
			XmlNode xmlNode73 = xmlDocument.CreateElement("tot_sbctrcr_amt");
			decimal value2 = num.Value;
			xmlNode73.InnerText = Math.Round(value2, 2).ToString();
			xmlNode50.AppendChild(xmlNode73);
			XmlNode xmlNode74 = xmlDocument.CreateElement("rpt_tcd");
			xmlNode74.InnerText = rowfilter.FilingType;
			xmlNode50.AppendChild(xmlNode74);
			xmlElement.AppendChild(xmlNode25);
			xmlDocument.AppendChild(xmlElement);
			string text9 = DateTime.Now.ToString("MMddyy");
			string text10 = "T5018-" + rowfilter.SubmissionNo + "_" + text9;

			if (rowfilter.OrganizationID.HasValue && rowfilter.FromDate.HasValue && rowfilter.ToDate.HasValue)
			{
				FileInfo fileInfo = new FileInfo(Guid.NewGuid(), text10 + ".xml", null,
				                                 Encoding.UTF8.GetBytes(xmlDocument.OuterXml));
				throw new PXRedirectToFileException(fileInfo, forceDownload: true);
			}


			MasterView.Cache.Clear();
		}

		public IEnumerable detailsView()
		{
			if (!MasterView.Current.OrganizationID.HasValue || !MasterView.Current.OrganizationID.HasValue ||
			    !MasterView.Current.FromDate.HasValue || !MasterView.Current.ToDate.HasValue)
			{
				yield break;
			}

			foreach (PXResult<BAccount, Location> vend in PXSelectBase<BAccount, PXSelectJoin<BAccount,
				InnerJoin<Location, On<BAccount.bAccountID, Equal<Location.bAccountID>>>,
				Where<T5018BAccountExt.vendorT5018, Equal<True>>>.Config>.Select(this))
			{
				decimal? TranAmt = 0m;
				PX.Objects.GL.Branch Orgdoc = null;
				BAccount empBacctDetails = vend;
				_ = (Location)vend;
				decimal histMult = 1m;
				decimal? origDocAmt;
				foreach (PXResult<Organization, PX.Objects.GL.Branch, APAdjust, APTran, APInvoice> res in PXSelectBase<
						Organization,
						PXSelectReadonly2<Organization,
							InnerJoin<PX.Objects.GL.Branch,
								On<Organization.organizationID, Equal<PX.Objects.GL.Branch.organizationID>>,
								InnerJoin<APAdjust, On<APAdjust.adjgBranchID, Equal<PX.Objects.GL.Branch.branchID>>,
									InnerJoin<
										APTran,
										On<APTran.tranType, Equal<APAdjust.adjdDocType>,
											And<APTran.refNbr, Equal<APAdjust.adjdRefNbr>>>,
										InnerJoin<APInvoice, On<APInvoice.docType, Equal<APAdjust.adjdDocType>,
											And<APInvoice.refNbr, Equal<APAdjust.adjdRefNbr>>>>>>>,
							Where<APAdjust.vendorID, Equal<Required<APAdjust.vendorID>>, And<Organization.organizationID
								,
								Equal<Required<Organization.organizationID>>,
								And<APAdjust.adjgDocDate, GreaterEqual<Required<APAdjust.adjgDocDate>>, And<
									APAdjust.adjgDocDate
									, LessEqual<Required<APAdjust.adjgDocDate>>,
									And<APAdjust.released, Equal<True>, And<APAdjust.voided, Equal<False>>>>>>>>.Config>
					.Select(this, empBacctDetails.BAccountID, MasterView.Current.OrganizationID,
					        MasterView.Current.FromDate,
					        MasterView.Current.ToDate.Value.AddDays(1.0)))
				{
					APAdjust adj = res;
					APTran tran = res;
					APInvoice doc = res;
					Orgdoc = res;
					if (!(adj.AdjdDocType == "PPM") && !(adj.AdjgDocType == "ADR"))
					{
						if (adj.AdjgDocType == "VQC" || adj.AdjgDocType == "REF" || adj.AdjgDocType == "VRF" ||
						    adj.AdjdDocType == "ADR")
						{
							histMult = -histMult;
						}

						int num;
						if (doc != null)
						{
							origDocAmt = doc.OrigDocAmt;
							num = ((!((origDocAmt.GetValueOrDefault() == default(decimal)) & origDocAmt.HasValue))
								       ? 1
								       : 0);
						}
						else
						{
							num = 0;
						}

						if (num != 0)
						{
							TranAmt +=
								(decimal?)PXCurrencyAttribute.BaseRound(this,
																		 histMult * (tran.TranAmt.Value +
																		 (tran.TranAmt * doc.TaxTotal / (doc.OrigDocAmt - doc.TaxTotal))) *
																		 adj.AdjAmt.Value /
																		 doc.OrigDocAmt.Value);
						}
					}
				}

				origDocAmt = TranAmt;
				if (((origDocAmt.GetValueOrDefault() > default(decimal)) & origDocAmt.HasValue) &&
				    TranAmt >= MasterView.Current.ThersholdAmount)
				{
					T5018DetailsTable DataResult = new T5018DetailsTable
					{
						PayerOrganizationID = ((Orgdoc != null) ? Orgdoc.BranchCD : ""),
						VAcctID = empBacctDetails.BAccountID,
						VAcctCD = empBacctDetails.AcctCD,
						VAcctName = empBacctDetails.AcctName,
						CuryAdjdAmt = TranAmt
					};
					T5018BAccountExt headerExt = empBacctDetails.GetExtension<T5018BAccountExt>();
					DataResult.LTaxRegistrationID = headerExt.BusinessNum;
					T5018DetailsTable located = DetailsView.Cache.Locate(DataResult) as T5018DetailsTable;
					yield return (located ?? DetailsView.Cache.Insert(DataResult));
					// yield return DataResult;
				}
			}
		}
	}
}

