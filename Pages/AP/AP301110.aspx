<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
    ValidateRequest="false" CodeFile="AP301110.aspx.cs" Inherits="Page_AP301110"
    Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" Width="100%" runat="server" PrimaryView="Records" TypeName="PX.Objects.AP.InvoiceRecognition.IncomingDocumentsProcess"
        Visible="True">
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">    
    <px:PXMultiUpload ID="foo" runat="server" View="Records" Action="UploadFiles" DropTarget=".dropFiles1" Accept=".pdf" />

    <px:PXGrid DataSourceID="ds" runat="server" ID="grid" SkinID="PrimaryInquire" Caption="Records" Height="400px" Width="100%"
        SyncPosition="true" ShowFilterToolbar="true" AllowSearch="true" FastFilterFields="Subject,DocumentLink,MailFrom,Owner"
        AllowPaging="true" AdjustPageSize="Auto" NoteIndicator="true" FilesIndicator="false" AutoAdjustColumns="true"
        OnRowDataBound="grid_RowDataBound"  CssClass="dropFiles1">
        <Levels>
            <px:PXGridLevel DataMember="Records">
                <Columns>
                    <px:PXGridColumn DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="true" AllowMove="false" AllowSort="true" />
                    <px:PXGridColumn DataField="Subject" LinkCommand="editRecord" />
                    <px:PXGridColumn DataField="Status" Width="120px" />
                    <px:PXGridColumn DataField="DocumentLink" LinkCommand="viewDocument" Width="130px" />
                    <px:PXGridColumn DataField="CreatedDateTime" Width="120px" />
                    <px:PXGridColumn DataField="MailFrom" />
                    <px:PXGridColumn DataField="RecognizedRecordDetail__VendorID" Width="110px" />
                    <px:PXGridColumn DataField="RecognizedRecordDetail__Amount" />
                    <px:PXGridColumn DataField="Owner" Width="130px" />
                    <px:PXGridColumn DataField="RecognizedRecordDetail__Date" />
                    <px:PXGridColumn DataField="RecognizedRecordDetail__DueDate" />
                    <px:PXGridColumn DataField="RecognizedRecordDetail__VendorRef" />
                </Columns>
                <RowTemplate>
                    <px:PXSegmentMask ID="VendorID" runat="server" DataField="RecognizedRecordDetail__VendorID" AllowEdit="True" DataSourceID="ds" />
                </RowTemplate>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
        <Mode AllowUpdate="False" />
        <AutoCallBack Command="Refresh" Target="Records">
			<Behavior RepaintControls="All" />
		</AutoCallBack>
	</px:PXGrid>
    <px:PXSmartPanel ID="ErrorHistory" runat="server" Width="700px" Height="400px" Key="ErrorHistory" CommandSourceID="ds" Caption="History"
        CaptionVisible="true" AutoCallBack-Command="Refresh" AutoCallBack-Target="ErrorHistoryList">
        <px:PXGrid ID="ErrorHistoryList" runat="server" Height="200px" Width="100%" SkinID="Inquire" DataSourceID="ds" Caption="History List"
            CaptionVisible="false" AdjustPageSize="Auto" AutoAdjustColumns="True">
            <Levels>
                <px:PXGridLevel DataMember="ErrorHistory">
                    <Columns>
                        <px:PXGridColumn DataField="ErrorMessage" Width="200px" />
                        <px:PXGridColumn DataField="CloudFileId" Width="200px" />
                        <px:PXGridColumn DataField="CreatedDateTime" Width="120px" DisplayFormat="G" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Enabled="true" />
        </px:PXGrid>
        <px:PXPanel ID="ErrorHistoryButtons" runat="server" SkinID="Buttons" Width="100%">
            <px:PXButton ID="ErrorHistoryOk" runat="server" DialogResult="Cancel" Text="Close" />
        </px:PXPanel>
    </px:PXSmartPanel>
</asp:Content>
