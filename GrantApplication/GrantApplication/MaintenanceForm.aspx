<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MaintenanceForm.aspx.cs" Inherits="GrantApplication.MaintenanceForm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Rip Snapper Slapper</title>
    <link rel="stylesheet" href="Styles/jquery.ui.all.css"  type="text/css">
	<script src="Scripts/jquery-1.6.2.js" type="text/javascript"></script>
	<script src="Scripts/jquery.ui.core.js"  type="text/javascript"></script>
	<script src="Scripts/jquery.ui.widget.js"  type="text/javascript"></script>
	<script src="Scripts/jquery.ui.accordion.js"  type="text/javascript"></script>
	<link rel="stylesheet" href="Styles/demos.css"  type="text/css">
    <script type="text/javascript">
        $(function () {
            $("#accordion").accordion({
                collapsible: true
            });
        });
    </script>
</head>
<body>
    
   <div class="demo">

<div id="accordion">
	<h3><a href="#">Section 1</a></h3>
	<div>
		<p>Mauris mauris ante, blandit et, ultrices a, suscipit eget, quam. Integer ut neque. Vivamus nisi metus, molestie vel, gravida in, condimentum sit amet, nunc. Nam a nibh. Donec suscipit eros. Nam mi. Proin viverra leo ut odio. Curabitur malesuada. Vestibulum a velit eu ante scelerisque vulputate.</p>
	</div>
	<h3><a href="#">Section 2</a></h3>
	<div>
		<p>Sed non urna. Donec et ante. Phasellus eu ligula. Vestibulum sit amet purus. Vivamus hendrerit, dolor at aliquet laoreet, mauris turpis porttitor velit, faucibus interdum tellus libero ac justo. Vivamus non quam. In suscipit faucibus urna. </p>
	</div>
	<h3><a href="#">Section 3</a></h3>
	<div>
		<p>Nam enim risus, molestie et, porta ac, aliquam ac, risus. Quisque lobortis. Phasellus pellentesque purus in massa. Aenean in pede. Phasellus ac libero ac tellus pellentesque semper. Sed ac felis. Sed commodo, magna quis lacinia ornare, quam ante aliquam nisi, eu iaculis leo purus venenatis dui. </p>
		<ul>
			<li>List item one</li>
			<li>List item two</li>
			<li>List item three</li>
		</ul>
	</div>
	<h3><a href="#">Section 4</a></h3>
	<div>
		<p>Cras dictum. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Aenean lacinia mauris vel est. </p><p>Suspendisse eu nisl. Nullam ut libero. Integer dignissim consequat lectus. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. </p>
	</div>
</div>

</div><!-- End demo -->
<div class="demo-description">
<p>By default, accordions always keep one section open. To allow for all sections to be be collapsible, set the <code>collapsible</code> option to true. Click on the currently open section to collapse its content pane.</p>
</div><!-- End demo-description -->
    
</body>
</html>
