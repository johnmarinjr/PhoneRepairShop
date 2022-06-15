<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AM205000.aspx.cs" Inherits="Page_AM205000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" Width="100%" runat="server" Visible = "True" PrimaryView="ShiftRecords" TypeName="PX.Objects.AM.ShiftMaint" >
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Save" CommitChanges="True" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" AllowPaging="True" AllowSearch="true" AdjustPageSize="Auto" DataSourceID="ds" SkinID="Primary">
		<Levels>
			<px:PXGridLevel DataKeyNames="ShiftID" DataMember="ShiftRecords">
                <RowTemplate>
                    <px:PXMaskEdit ID="edShiftCD" runat="server" DataField="ShiftCD" />
                    <px:PXTextEdit ID="edShftDesc" runat="server" DataField="Description" />
                    <px:PXDropDown ID="edDiffType" runat="server" DataField="DiffType" />
                    <px:PXNumberEdit ID="edShftDiff" runat="server" DataField="ShftDiff" />
                    <px:PXNumberEdit ID="edCrewSize" runat="server" DataField="AMCrewSize" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn DataField="ShiftCD" Label="Shift" CommitChanges="true" />
                    <px:PXGridColumn DataField="Description" Width="180px" />
                    <px:PXGridColumn DataField="DiffType" />
                    <px:PXGridColumn DataField="ShftDiff" />
                    <px:PXGridColumn DataField="AMCrewSize" />
                </Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		<ActionBar ActionsText="False">
		</ActionBar>
    </px:PXGrid>
</asp:Content>
