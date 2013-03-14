<%@ Page Title="Log In" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Login.aspx.cs" Inherits="GrantApplication.Account.Login" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
<script type="text/javascript">
    function load() {
    }
</script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
        Log In
    </h2>
    <p>
        Please enter your username and password.
        <asp:HyperLink ID="RegisterHyperLink" runat="server" EnableViewState="false">Register</asp:HyperLink> if you don't have an account.
        <br /><asp:Label runat="server" ID="lblScrewedUp" Text="Sorry, incorrect password" Visible="false" Font-Bold="true" ForeColor="Red"></asp:Label>
    </p>
    <asp:Login ID="LoginUser" runat="server" EnableViewState="false" RenderOuterTable="false">
        <LayoutTemplate>
            <span class="failureNotification">
                <asp:Literal ID="FailureText" runat="server"></asp:Literal>
            </span>
            <asp:ValidationSummary ID="LoginUserValidationSummary" runat="server" CssClass="failureNotification" 
                 ValidationGroup="LoginUserValidationGroup"/>
            <div class="accountInfo">
                <fieldset class="login">
                    <legend>Account Information</legend>
                    <p>
                        <asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Username:</asp:Label>
                        <asp:TextBox ID="UserName" runat="server" CssClass="textEntry"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName" 
                             CssClass="failureNotification" ErrorMessage="User Name is required." ToolTip="User Name is required." 
                             ValidationGroup="LoginUserValidationGroup">*</asp:RequiredFieldValidator>
                    </p>
                    <p>
                        <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password">Password:</asp:Label>
                        <asp:TextBox ID="Password" runat="server" CssClass="passwordEntry" TextMode="Password"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password" 
                             CssClass="failureNotification" ErrorMessage="Password is required." ToolTip="Password is required." 
                             ValidationGroup="LoginUserValidationGroup">*</asp:RequiredFieldValidator>
                    </p>
                    <p>
                        <asp:CheckBox ID="RememberMe" runat="server"/>
                        <asp:Label ID="RememberMeLabel" runat="server" AssociatedControlID="RememberMe" CssClass="inline">Keep me logged in</asp:Label>
                    </p>
                </fieldset>
                <p class="submitButton">                                        
                    <asp:Button runat="server" ID="BangMe" Text="Log In" OnClick="GripNRip" />                                      
                </p>                                              
            </div>
        </LayoutTemplate>        
    </asp:Login>   
            <asp:Panel runat="server" ID="pnlSwitchUser">
            <fieldset>
            <legend class="login">Switch User</legend>
            <div style="text-align:left; vertical-align:top; padding: 10px">
            <table>
                <tr>                        
                    <td style="width:300px">
                            <asp:Panel runat="server" ID="pnlSwitcher" HorizontalAlign="Right">
                                <asp:Label runat="server" ID="lblKids" Text="Switch to:"></asp:Label>
                                <asp:DropDownList runat="server" ID="ddlKids" Width="220px" ForeColor="DarkGray" 
                                        OnSelectedIndexChanged="DoStuff"></asp:DropDownList>
                            </asp:Panel>
                    </td>
                    <td>
                        <asp:ImageButton runat="server" ID="btnGo" ImageUrl="~/Go.png" OnClick="Go" />
                    </td>
                </tr>
            </table>                                      
            </div>
            </fieldset>
            </asp:Panel>
            <asp:Panel runat="server" ID="pnlDowner" Visible="false">
                <fieldset>
                    <legend class="login">Download Database</legend>
                    <div style="text-align:left; vertical-align:top; padding: 10px">
                    <table>
                        <tr>                        
                            <td style="width:300px">
                                    <asp:Panel runat="server" ID="pnlDownload" HorizontalAlign="Right">Download Database
                                    <asp:ImageButton runat="server" ID="btnDowner" ClientIDMode="Static" OnClick="CrankTheDownload" ImageUrl="~/Access.png" ToolTip="Download Database" />                                      
                                    </asp:Panel>
                            </td>                           
                        </tr>
                    </table>                                      
                    </div>
            </fieldset>
            </asp:Panel>                               
    <asp:Label ID="lblCrank" runat="server" Text="Crank!" Visible="false"></asp:Label>
</asp:Content>
