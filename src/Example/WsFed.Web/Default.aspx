<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WsFed.Web.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <table class="tbl">
            <tr><th>Claim Type</th><th>Claim Value</th><th>Value Type</th><th>Subject Name</th><th>Issuer Name</th></tr>
            <asp:Repeater runat="server" ID="LocalRepeater">
                <HeaderTemplate></HeaderTemplate>
                <ItemTemplate><tr><td><%#Eval("Type") %></td><td><%#Eval("Value") %></td><td><%#Eval("ValueType") %></td><td><%#Eval("Subject") %></td><td><%#Eval("Issuer") %></td></tr></ItemTemplate>
                <FooterTemplate></FooterTemplate>
            </asp:Repeater>
        </table>
         <table class="tbl">
            <tr><th>Claim Type</th><th>Claim Value</th><th>Value Type</th><th>Subject Name</th><th>Issuer Name</th></tr>
            <asp:Repeater runat="server" ID="RemoteRepeater">
                <HeaderTemplate></HeaderTemplate>
                <ItemTemplate><tr><td><%#Eval("ClaimType") %></td><td><%#Eval("Value") %></td><td><%#Eval("ValueType") %></td><td><%#Eval("Subject") %></td><td><%#Eval("Issuer") %></td></tr></ItemTemplate>
                <FooterTemplate></FooterTemplate>
            </asp:Repeater>
        </table>
    </form>
</body>
</html>
