using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.CM;
using PX.Objects.EP;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.CR
{
	[SerializableAttribute]
    [PXPrimaryGraph(typeof(CRSetupMaint))]
    [PXCacheName(Messages.CRSetup)]
    public partial class CRSetup : IBqlTable, PXNoteAttribute.IPXCopySettings
	{
		#region CampaignNumberingID
		public abstract class campaignNumberingID : PX.Data.BQL.BqlString.Field<campaignNumberingID> { }
		protected String _CampaignNumberingID;
		[PXDBString(10, IsUnicode = true)]
		[PXDefault("CAMPAIGN")]
		[PXUIField(DisplayName = "Campaign Numbering Sequence")]
        [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		public virtual String CampaignNumberingID
		{
			get
			{
				return this._CampaignNumberingID;
			}
			set
			{
				this._CampaignNumberingID = value;
			}
		}
		#endregion
		#region OpportunityNumberingID
		public abstract class opportunityNumberingID : PX.Data.BQL.BqlString.Field<opportunityNumberingID> { }
		protected String _OpportunityNumberingID;
		[PXDBString(10, IsUnicode = true)]
        [PXDefault("OPPORTUNTY")]
		[PXUIField(DisplayName = "Opportunity Numbering Sequence")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		public virtual String OpportunityNumberingID
		{
			get
			{
				return this._OpportunityNumberingID;
			}
			set
			{
				this._OpportunityNumberingID = value;
			}
		}
        #endregion
	    #region QuoteNumberingID
	    public abstract class quoteNumberingID : PX.Data.BQL.BqlString.Field<quoteNumberingID> { }
	    protected String _QuoteNumberingID;
	    [PXDBString(10, IsUnicode = true)]
	    [PXDefault("CRQUOTE")]
	    [PXUIField(DisplayName = "Quote Numbering Sequence")]
	    [PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
	    public virtual String QuoteNumberingID
	    {
	        get
	        {
	            return this._QuoteNumberingID;
	        }
	        set
	        {
	            this._QuoteNumberingID = value;
	        }
	    }
	    #endregion
        #region CaseNumberingID
        public abstract class caseNumberingID : PX.Data.BQL.BqlString.Field<caseNumberingID> { }
		protected String _CaseNumberingID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXDefault("CASE")]
		[PXUIField(DisplayName = "Case Numbering Sequence")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		public virtual String CaseNumberingID
		{
			get
			{
				return this._CaseNumberingID;
			}
			set
			{
				this._CaseNumberingID = value;
			}
		}
		#endregion
		#region MassMailNumberingID
		public abstract class massMailNumberingID : PX.Data.BQL.BqlString.Field<massMailNumberingID> { }
		protected String _MassMailNumberingID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXDefault("MMAIL")]
		[PXUIField(DisplayName = "Mass Mail Numbering Sequence")]
		[PXSelector(typeof(Numbering.numberingID), DescriptionField = typeof(Numbering.descr))]
		public virtual String MassMailNumberingID
		{
			get
			{
				return this._MassMailNumberingID;
			}
			set
			{
				this._MassMailNumberingID = value;
			}
		}
		#endregion
		#region DefaultCaseClassID
		public abstract class defaultCaseClassID : PX.Data.BQL.BqlString.Field<defaultCaseClassID> { }
		protected String _DefaultCaseClassID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Case Class")]
		[PXSelector(typeof(CRCaseClass.caseClassID))]
		public virtual String DefaultCaseClassID
		{
			get
			{
				return this._DefaultCaseClassID;
			}
			set
			{
				this._DefaultCaseClassID = value;
			}
		}
		#endregion
		#region DefaultOpportunityClassID
		public abstract class defaultOpportunityClassID : PX.Data.BQL.BqlString.Field<defaultOpportunityClassID> { }
		protected String _DefaultOpportunityClassID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Opportunity Class")]
		[PXSelector(typeof(CROpportunityClass.cROpportunityClassID))]
		public virtual String DefaultOpportunityClassID
		{
			get
			{
				return this._DefaultOpportunityClassID;
			}
			set
			{
				this._DefaultOpportunityClassID = value;
			}
		}
		#endregion
		#region DefaultRateTypeID
		public abstract class defaultRateTypeID : PX.Data.BQL.BqlString.Field<defaultRateTypeID> { }
		protected String _DefaultRateTypeID;
		[PXDBString(6, IsUnicode = true)]
		[PXSelector(typeof(PX.Objects.CM.CurrencyRateType.curyRateTypeID))]
		[PXForeignReference(typeof(Field<defaultRateTypeID>.IsRelatedTo<CurrencyRateType.curyRateTypeID>))]
		[PXUIField(DisplayName = "Default Rate Type ")]
		public virtual String DefaultRateTypeID
		{
			get
			{
				return this._DefaultRateTypeID;
			}
			set
			{
				this._DefaultRateTypeID = value;
			}
		}
		#endregion
		#region DefaultCuryID
		public abstract class defaultCuryID : PX.Data.BQL.BqlString.Field<defaultCuryID> { }
		protected String _DefaultCuryID;
		[PXDBString(5, IsUnicode = true)]
		[PXSelector(typeof(Currency.curyID), CacheGlobal = true)]
		[PXUIField(DisplayName = "Default Currency")]
		public virtual String DefaultCuryID
		{
			get
			{
				return this._DefaultCuryID;
			}
			set
			{
				this._DefaultCuryID = value;
			}
		}
		#endregion
		#region AllowOverrideCury
		public abstract class allowOverrideCury : PX.Data.BQL.BqlBool.Field<allowOverrideCury> { }
		protected Boolean? _AllowOverrideCury;
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Enable Currency Override")]
		public virtual Boolean? AllowOverrideCury
		{
			get
			{
				return this._AllowOverrideCury;
			}
			set
			{
				this._AllowOverrideCury = value;
			}
		}
		#endregion
		#region AllowOverrideRate
		public abstract class allowOverrideRate : PX.Data.BQL.BqlBool.Field<allowOverrideRate> { }
		protected Boolean? _AllowOverrideRate;
		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Enable Rate Override")]
		public virtual Boolean? AllowOverrideRate
		{
			get
			{
				return this._AllowOverrideRate;
			}
			set
			{
				this._AllowOverrideRate = value;
			}
		}
		#endregion
		#region LeadDefaultAssignmentMapID
		public abstract class leaddefaultAssignmentMapID : PX.Data.BQL.BqlInt.Field<leaddefaultAssignmentMapID> { }
		protected int? _LeadDefaultAssignmentMapID;
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID,
			Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeLead>>>))]
		[PXUIField(DisplayName = "Lead Assignment Map")]
		public virtual int? LeadDefaultAssignmentMapID
		{
			get
			{
				return this._LeadDefaultAssignmentMapID;
			}
			set
			{
				this._LeadDefaultAssignmentMapID = value;
			}
		}
		#endregion

		#region ContactDefaultAssignmentMapID
		public abstract class contactdefaultAssignmentMapID : PX.Data.BQL.BqlInt.Field<contactdefaultAssignmentMapID> { }
		protected int? _ContactDefaultAssignmentMapID;
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID,
			Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeContact>>>))]
		[PXUIField(DisplayName = "Contact Assignment Map")]
		public virtual int? ContactDefaultAssignmentMapID
		{
			get
			{
				return this._ContactDefaultAssignmentMapID;
			}
			set
			{
				this._ContactDefaultAssignmentMapID = value;
			}
		}
		#endregion

		#region DefaultCaseAssignmentMapID
		public abstract class defaultCaseAssignmentMapID : PX.Data.BQL.BqlInt.Field<defaultCaseAssignmentMapID> { }
		protected int? _DefaultCaseAssignmentMapID;
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID, Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeCase>>>))]
		[PXUIField(DisplayName = "Case Assignment Map")]
		public virtual int? DefaultCaseAssignmentMapID
		{
			get
			{
				return this._DefaultCaseAssignmentMapID;
			}
			set
			{
				this._DefaultCaseAssignmentMapID = value;
			}
		}
		#endregion
		#region DefaultBAccountAssignmentMapID
		public abstract class defaultBAccountAssignmentMapID : PX.Data.BQL.BqlInt.Field<defaultBAccountAssignmentMapID> { }
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID,
			Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeProspect>>>))]
		[PXUIField(DisplayName = "Business Account Assignment Map")]
		public virtual int? DefaultBAccountAssignmentMapID { get; set; }
		#endregion

		#region DefaultOpportunityAssignmentMapID
		public abstract class defaultOpportunityAssignmentMapID : PX.Data.BQL.BqlInt.Field<defaultOpportunityAssignmentMapID> { }
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID,
			Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeOpportunity>>>))]
		[PXUIField(DisplayName = "Opportunity Assignment Map")]
		public virtual int? DefaultOpportunityAssignmentMapID { get; set; }
        #endregion

        #region AssignmentMapID
        public abstract class quoteApprovalMapID : PX.Data.BQL.BqlInt.Field<quoteApprovalMapID> { }
        protected int? _QuoteApprovalMapID;
        [PXDBInt]
        [PXSelector(
            typeof(Search<
                EPAssignmentMap.assignmentMapID,
                Where<
                    EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeQuotes>,
                    And<EPAssignmentMap.mapType, NotEqual<EPMapType.assignment>>>>))]
        [PXUIField(DisplayName = "Approval Map")]
        public virtual int? QuoteApprovalMapID
        {
            get
            {
                return this._QuoteApprovalMapID;
            }
            set
            {
                this._QuoteApprovalMapID = value;
            }
        }
        #endregion

        #region QuoteApprovalNotificationID
        public abstract class quoteApprovalNotificationID : PX.Data.BQL.BqlInt.Field<quoteApprovalNotificationID> { }
        protected int? _QuoteApprovalNotificationID;
        [PXDBInt]
        [PXSelector(typeof(Search<PX.SM.Notification.notificationID>))]
        [PXDefault(292, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Pending Approval Notification")]
        public virtual int? QuoteApprovalNotificationID
        {
            get
            {
                return this._QuoteApprovalNotificationID;
            }
            set
            {
                this._QuoteApprovalNotificationID = value;
            }
        }
		#endregion


		#region DefaultLeadClassID
		public abstract class defaultLeadClassID : PX.Data.BQL.BqlString.Field<defaultLeadClassID> { }
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Lead Class")]
		[PXSelector(typeof(CRLeadClass.classID))]
		public virtual String DefaultLeadClassID { get; set; }
		#endregion

		#region DefaultContactClassID
		public abstract class defaultContactClassID : PX.Data.BQL.BqlString.Field<defaultContactClassID> { }
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Contact Class")]
		[PXSelector(typeof(CRContactClass.classID))]
		public virtual String DefaultContactClassID { get; set; }
		#endregion

		#region DefaultCustomerClassID
		public abstract class defaultCustomerClassID : PX.Data.BQL.BqlString.Field<defaultCustomerClassID> { }
		protected String _DefaultCustomerClassID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Business Account Class")]
		[PXSelector(typeof(CRCustomerClass.cRCustomerClassID))]
		public virtual String DefaultCustomerClassID
		{
			get
			{
				return this._DefaultCustomerClassID;
			}
			set
			{
				this._DefaultCustomerClassID = value;
			}
		}
		#endregion

		#region CopyNotes
		public abstract class copyNotes : PX.Data.BQL.BqlBool.Field<copyNotes> { }
		protected bool? _CopyNotes;
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Copy Notes")]
		public virtual bool? CopyNotes
		{
			get
			{
				return _CopyNotes;
			}
			set
			{
				_CopyNotes = value;
			}
		}
		#endregion
		#region CopyFiles
		public abstract class copyFiles : PX.Data.BQL.BqlBool.Field<copyFiles> { }
		protected bool? _CopyFiles;
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Copy Attachments")]
		public virtual bool? CopyFiles
		{
			get
			{
				return _CopyFiles;
			}
			set
			{
				_CopyFiles = value;
			}
		}
		#endregion

		#region DuplicateScoresNormalization
		public abstract class duplicateScoresNormalization : PX.Data.BQL.BqlBool.Field<duplicateScoresNormalization> { }
		
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Normalize Validation Scores", FieldClass = FeaturesSet.contactDuplicate.FieldClass)]
		public virtual bool? DuplicateScoresNormalization { get; set; }
		#endregion

		#region DuplicateWordsDelimiters
		public abstract class duplicateCharsDelimiters : PX.Data.BQL.BqlString.Field<duplicateCharsDelimiters> { }

		/// <summary>
		/// Delimiters used for option: <see cref="TransformationRulesAttribute.SplitWords"/> during grams calculation.
		/// For delimiters different from default
		/// (<see cref="PX.Objects.CR.Extensions.CRDuplicateEntities.CRCharsDelimitersAttribute.DefaultDelimiters"/>
		/// attach custom <see cref="PX.Objects.CR.Extensions.CRDuplicateEntities.CRCharsDelimitersAttribute"/> with required pattern.
		/// For instance, " ,:^" will use 4 delimiters: ' ', ',', ':', '^'.
		/// </summary>
		[PXString]
		[PX.Objects.CR.Extensions.CRDuplicateEntities.CRCharsDelimiters]
		public virtual string DuplicateCharsDelimiters { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXNote]
		public virtual Guid? NoteID { get; set; }
		#endregion
	}

	[PXHidden]
	[PXBreakInheritance]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class CRSetupQuoteApproval : CRSetup, IAssignedMap
	{
		public int? AssignmentMapID { get => QuoteApprovalMapID; set => _QuoteApprovalMapID = value; }
		public int? AssignmentNotificationID { get => QuoteApprovalNotificationID; set => _QuoteApprovalNotificationID = value; }
		public bool? IsActive => (AssignmentMapID != null);
	}
}
