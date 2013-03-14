<%@ Page Title="Maintenance Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Maintenance.aspx.cs" Inherits="GrantApplication.Maintenance" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Content ID="HeaderContent" ContentPlaceHolderID="HeadContent" runat="server">
<meta charset="utf-8">
<link rel="stylesheet" href="Styles/jquery.ui.all.css">
	<script src="Scripts/jquery-1.6.2.js" type="text/javascript"></script>
	<script src="Scripts/jquery.ui.core.js"  type="text/javascript"></script>
	<script src="Scripts/jquery.ui.widget.js"  type="text/javascript"></script>
	<script src="Scripts/jquery.ui.accordion.js"  type="text/javascript"></script>
	<link rel="stylesheet" href="Styles/demos.css">    
    <script type="text/javascript">
        var curEmp = null;
        function load() {
        }
        function crankTheItem() {
            var el = window.event.srcElement;
            var s = el.value;
            var schars = s.split(",");
            PageMethods.getSelectedEmp(schars[1],"0" ,haveAnEmp);
        }
        function haveAnEmp(e) {
            curEmp = e;
            if (curEmp != null) {
                var field = document.getElementById("txtFirst");
                field.value = curEmp.firstName;
                field = document.getElementById("txtLast");
                field.value = curEmp.lastName;
                field = document.getElementById("txtEmpNum");
                field.value = curEmp.EmpNum;
                field = document.getElementById("txtTitle");
                field.value = curEmp.jobTitle;
                field = document.getElementById("txtEmail");
                field.value = curEmp.emailAddress;
                field = document.getElementById("cbManager");
                field.checked = (curEmp.manager) ? true : false;
                if (curEmp.defaultSupervisor != null && curEmp.defaultSupervisor != "0") {
                    var defSup = curEmp.defaultSupervisor;
                    PageMethods.getDefaultSupervisor(defSup, heresASup);
                }                
            }
        }
        function heresASup(e) {
            if (curEmp != null && e != null) {
                var dropper = document.getElementById("ddlDefSup");
                if (curEmp != null) {
                    var snap = Enumerable.From(dropper.options).Where(function (val) { return val.value == e.ID.toString() }).ToArray();
                    if (snap[0] != null) {
                        dropper.selectedIndex = snap[0].index;
                        }
                    }
                }
        }
        $(function () {
            $("#accordion").accordion({
                collapsible: true
            });
        });
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div class="demo">
<div id="accordion">
	<h3><a href="#">Employee Maintenance</a></h3>
	<div>
		<p>Add or edit an employee's information.  Be careful to use all fields provided for accuracy and dependability.</p>
        <table>
            <tr>
                <td style="text-align:right; font-weight:bold">
                    <asp:Label runat="server" ID="lblEmp" Text="Just start typing employee's first name or leave blank for new entry..."></asp:Label>
                </td>
                 <td style="text-align:left">
                    <asp:TextBox runat="server" ID="txtEmps" Width="240px" ClientIDMode="Static" AutoPostBack="false" onchange="crankTheItem()">                        
                    </asp:TextBox>
                </td>
                <td>                    
                </td>                
            </tr>
        </table>
        <p></p>
        <asp:AutoCompleteExtender runat="server" ID="ace1" ClientIDMode="Static" TargetControlID="txtEmps" MinimumPrefixLength="2" ServiceMethod="GetEmpsList"  >
        </asp:AutoCompleteExtender>
        <asp:Panel runat="server" ID="pnlEmp" BackColor="#4b6c9e" Height="310px" Width="700px" ForeColor="White">
            <table style="padding: 8px 6px 8px 6px">
                <tr>
                    <td style="text-align:right">
                        <asp:Label runat="server" ID="lblFirst" Text="First Name"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:TextBox runat="server" ID="txtFirst" ClientIDMode="Static"></asp:TextBox>
                    </td>
                    <td style="text-align:right; padding-left:10px">
                        <asp:Label runat="server" ID="LblLast" Text="Last Name"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:TextBox runat="server" ID="txtLast" ClientIDMode="Static"></asp:TextBox>
                    </td>
                    <td style="text-align:right; padding-left:10px">
                        <asp:Label runat="server" ID="lblEmpNum" Text="Employee Number:" ClientIDMode="Static"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:TextBox runat="server" ID="txtEmpNum" ClientIDMode="Static"></asp:TextBox>
                    </td>
                </tr>

                 <tr style="height:40px; vertical-align:bottom">
                    <td style="text-align:right">
                        <asp:Label runat="server" ID="lblTitle" Text="Job Title"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:TextBox runat="server" ID="txtTitle" ClientIDMode="Static"></asp:TextBox>
                    </td>
                    <td style="text-align:right">
                        <asp:Label runat="server" ID="lblEmail" Text="Email Address:" ClientIDMode="Static"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:TextBox runat="server" ID="txtEmail" ClientIDMode="Static"></asp:TextBox>
                    </td>
                    <td style="text-align:right">
                        <asp:Label runat="server" ID="lblManager" Text="Manager?" ClientIDMode="Static"></asp:Label>
                    </td>
                    <td style="text-align:left">
                        <asp:CheckBox runat="server" ID="cbManager" ClientIDMode="Static" />
                    </td>                                       
                </tr>
                <tr>
                     <td style="text-align:right; vertical-align:bottom">
                        <asp:Label runat="server" ID="lblDefSup" Text="Default Supervisor"></asp:Label>
                    </td>
                    <td colspan="5" style="text-align:left; height:50px; vertical-align:bottom">
                        <asp:DropDownList runat="server" ID="ddlDefSup" ClientIDMode="Static">
                            <asp:ListItem Text="(Select a default supervisor...)" Value="0"></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr style="height:60px; vertical-align:bottom">
                    <td colspan="6" style="text-align:right">
                        <asp:Button runat="server" ID="btnCommit" Text="Submit" Width="120px" 
                            onclick="OnSubmit" />
                    </td>
                </tr>
            </table>
        </asp:Panel>
	</div>
	<h3><a href="#">Grant Maintenance</a></h3>   
	<div>
		<p>This form is available for use by the system administrator or designated appointeee and should not be used by non-authorized personnel. </p>
	</div>
	
</div>

</div><!-- End demo -->



<div class="demo-description">
<p>By default, accordions always keep one section open. To allow for all sections to be be collapsible, set the <code>collapsible</code> option to true. Click on the currently open section to collapse its content pane.</p>
</div><!-- End demo-description -->
 <asp:RoundedCornersExtender ID="rce1" runat="server" TargetControlID="pnlEmp" Radius="20">
    </asp:RoundedCornersExtender>
</asp:Content>
