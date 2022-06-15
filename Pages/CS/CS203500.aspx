<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="CS203500.aspx.cs" Inherits="Page__CS203500" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" SuspendUnloading="False" TypeName="PX.Objects.Localizations.CA.CS.UnitOfMeasureMaint" PrimaryView="UnitOfMeasures">
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" 
		Width="100%" DataMember="UnitOfMeasures" TabIndex="3100">
		<Template>
			<px:PXLayoutRule runat="server" StartRow="True" ControlSize="S" LabelsWidth="S"/>
		    <px:PXSelector ID="edUnit" runat="server" DataField="Unit" CommitChanges="True" AutoRefresh="True">
            </px:PXSelector>
            <px:PXTextEdit ID="edDescr" runat="server" AlreadyLocalized="False" DataField="Descr" DefaultLocale="">
            </px:PXTextEdit>
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
	<px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="z-index: 100" 
		Width="100%" Height="150px" SkinID="Details" TabIndex="4900" TemporaryFilterCaption="Filter Applied">
		<Levels>
			<px:PXGridLevel DataKeyNames="UnitType,ItemClassID,InventoryID,ToUnit,FromUnit" DataMember="Units">
			    <RowTemplate>
                    <px:PXSelector ID="edToUnit" runat="server" AutoRefresh="True" DataField="ToUnit">
                    </px:PXSelector>
                </RowTemplate>
			    <Columns>
                    <px:PXGridColumn DataField="ToUnit" CommitChanges="True">
                    </px:PXGridColumn>
                    <px:PXGridColumn DataField="UnitMultDiv" Width="110px">
                    </px:PXGridColumn>
                    <px:PXGridColumn DataField="UnitRate" TextAlign="Right" Width="100px">
                    </px:PXGridColumn>
                </Columns>
			</px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	    <ActionBar>
            <Actions>
                <ExportExcel Enabled="False" MenuVisible="False" />
            </Actions>
        </ActionBar>
	</px:PXGrid>
</asp:Content>
