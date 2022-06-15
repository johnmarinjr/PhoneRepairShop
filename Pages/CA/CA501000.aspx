<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CA501000.aspx.cs" Inherits="Page_CA501000" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="BankAccountSummary" TypeName="PX.Objects.CA.CABankTransactionsProcess" >
         <CallbackCommands>
            <px:PXDSCallbackCommand Visible="False" DependOnGrid="grid" Name="viewUnmatchedTrans"/>
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="z-index: 100; left: 0px; top: 0px;" Width="100%" Height="150px"
		SkinID="PrimaryInquire" Caption="Unmatched Bank Transactions" AdjustPageSize="Auto" AllowPaging="True" FastFilterFields="CashAccountID, CashAccount__Descr" SyncPosition="true">
        <Levels>
            <px:PXGridLevel DataMember="BankAccountSummary">
                <Columns>
                    <px:PXGridColumn AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" AllowCheckAll="True" />
                    <px:PXGridColumn DataField="CashAccountCD" LinkCommand="viewUnmatchedTrans"/>
                    <px:PXGridColumn DataField="Descr"/>
                    <px:PXGridColumn DataField="ExtRefNbr"/>
                    <px:PXGridColumn DataField="CuryID"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__DebitNumber"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__CreditNumber"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__UnprocessedNumber"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__MatchedNumber"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__CuryDebitAmount"/>
                    <px:PXGridColumn DataField="CABankTranByCashAccount__CuryCreditAmount"/>
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
    </px:PXGrid>
</asp:Content>
