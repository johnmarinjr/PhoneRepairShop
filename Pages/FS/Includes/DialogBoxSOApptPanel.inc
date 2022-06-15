<px:PXSmartPanel ID="ServiceOrderAppointmentSettingsPanel" runat="server" Style="z-index: 108; position: absolute; left: 27px; top: 99px;"
	Caption="Create Service Order/Appointment" CaptionVisible="True"
	LoadOnDemand="True" Key="DocumentSettings" AutoCallBack-Enabled="True"
	AutoCallBack-Target="formServiceOrderAppointmentSettings" AutoCallBack-Command="Refresh"
	AcceptButtonID="PXButtonOK" CancelButtonID="PXButtonCancel"
	AllowResize="False">
	<px:PXLayoutRule runat="server" GroupCaption="Document Settings"/>
	<px:PXFormView ID="formServiceOrderAppointmentSettings" runat="server"
		DataSourceID="ds" Width="100%" Caption="Document Settings" CaptionVisible="False"
		SkinID="Transparent" DataMember="DocumentSettings" MarkRequired="Dynamic">
		<Template>
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="M" ControlSize="XM"/>

			<px:PXSelector ID="edSrvOrdType" runat="server" DataField="SrvOrdType" CommitChanges="True" TabIndex="10"/>
			<px:PXSelector ID="edBranchID" runat="server" DataField="BranchID" CommitChanges="True" TabIndex="20"/>
			<px:PXSelector ID="edBranchLocationID" runat="server" DataField="BranchLocationID" TabIndex="30" CommitChanges="True" AutoRefresh="True"/>
			<px:PXTextEdit ID="edDescription" runat="server" DataField="Description" CommitChanges="True" TabIndex="40"/>
			<px:PXSegmentMask ID="edProjectID" runat="server" DataField="ProjectID" CommitChanges="True" TabIndex="50" />
			<px:PXSelector ID="edProjectTaskID" runat="server" DataField="ProjectTaskID" CommitChanges="True" TabIndex="60"/>

			<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="XM" GroupCaption="Service Order Settings" StartGroup="True" />
			<px:PXDateTimeEdit ID="edOrderDate" runat="server" DataField="OrderDate" CommitChanges="True" TabIndex="70"/>
							
			<px:PXLayoutRule runat="server" Merge="True" />							
			<px:PXDateTimeEdit ID="edSLAETA_Date" runat="server" DataField="SLAETA_Date" TabIndex="90"/>
			<px:PXDateTimeEdit ID="edSLAETA_Time" runat="server" DataField="SLAETA_Time" TimeMode="True" SuppressLabel="True" TabIndex="90"/>
							
			<px:PXLayoutRule runat="server" />
			<px:PXSelector ID="edAssignedEmpID" runat="server" DataField="AssignedEmpID" TabIndex="100"/>
			<px:PXSelector ID="edProblemID" runat="server" DataField="ProblemID" TabIndex="110"/>

			<px:PXLayoutRule runat="server" LabelsWidth="M" ControlSize="XM" GroupCaption="Appointment Settings" StartGroup="True" />
			<px:PXLayoutRule runat="server" Merge="True" />
			<px:PXDateTimeEdit ID="edScheduledDateTimeBegin_Date" runat="server" CommitChanges="True" DataField="ScheduledDateTimeBegin_Date"  TabIndex="120"/>
			<px:PXDateTimeEdit ID="edScheduledDateTimeBegin_Time" runat="server" CommitChanges="True" DataField="ScheduledDateTimeBegin_Time" TimeMode="True" SuppressLabel="True" TabIndex="130"/>
							
			<px:PXLayoutRule runat="server" Merge="True" />
			<px:PXDateTimeEdit ID="edScheduledDateTimeEnd_Date" runat="server" CommitChanges="True" DataField="ScheduledDateTimeEnd_Date"  TabIndex="140"/>
			<px:PXDateTimeEdit ID="edScheduledDateTimeEnd_Time" runat="server" CommitChanges="True" DataField="ScheduledDateTimeEnd_Time" TimeMode="True" SuppressLabel="True" TabIndex="150"/>
			<px:PXCheckBox ID="edHandleManuallyScheduleTime" runat="server" DataField="HandleManuallyScheduleTime" AlignLeft="False" CommitChanges="True" TabIndex="160"/>
		</Template>
	</px:PXFormView>

	<px:PXPanel ID="ServiceOrderAppointmentSettingsBtn" runat="server" SkinID="Buttons">
		<px:PXButton ID="CreateAndReview" runat="server" Text="Create and review" DialogResult="Yes" />
		<px:PXButton ID="Create" runat="server" Text="Create" DialogResult="OK" />
		<px:PXButton ID="Cancel" runat="server" DialogResult="Cancel" Text="Cancel" Style="margin-left: 5px"/>
	</px:PXPanel>
</px:PXSmartPanel>