<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="GrantApplication._Default" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
<script type="text/javascript">

    var weeklyTotals = null;
    var bForApproval = false;
    var reasonTxt = "";
    var status = null;
    var approvalValue = -1;
    var bImpersonate = false;

    var weekday = new Array(7);    
    weekday[0] = "Sunday";
    weekday[1] = "Monday";
    weekday[2] = "Tuesday";
    weekday[3] = "Wednesday";
    weekday[4] = "Thursday";
    weekday[5] = "Friday";
    weekday[6] = "Saturday";

    var daysInMonth = new Array(12);  //Array that holds the number of days in each month.
    daysInMonth[0] = 31;
    daysInMonth[1] = 28;
    daysInMonth[2] = 31;
    daysInMonth[3] = 30;
    daysInMonth[4] = 31;
    daysInMonth[5] = 30;
    daysInMonth[6] = 31;
    daysInMonth[7] = 31;
    daysInMonth[8] = 30;
    daysInMonth[9] = 31;
    daysInMonth[10] = 30;
    daysInMonth[11] = 31;

    var monthNames = new Array(12);
    monthNames[0] = "Jan";
    monthNames[1] = "Feb";
    monthNames[2] = "Mar";
    monthNames[3] = "Apr";
    monthNames[4] = "May";
    monthNames[5] = "Jun";
    monthNames[6] = "Jul";
    monthNames[7] = "Aug";
    monthNames[8] = "Sep";
    monthNames[9] = "Oct";
    monthNames[10] = "Nov";
    monthNames[11] = "Dec";

    var timeEntries = null;
    var selectedGrants = null;
    var grantIDs = new Array(4);
    var currentEmp = null;
    var selectedDate = null;
    for( var tt = 0; tt < 4; tt++ ) {
        grantIDs[tt] = -1;
    }

    var chosenEmployee = "";
    var weeklyCells = [];
    var totalHours = 0;

    function changeName() {
        var txt = document.getElementById("txtEmp");
        var str = txt.value;
        var ix = txt.value.indexOf(":");
        if (ix > 0) {
            txt.value = txt.value.substr(0, ix);
            chosenEmployee = txt.value;
            var txtpos = document.getElementById("txtPosition");
            txtpos.value = str.substr(ix + 1, str.length - (ix + 1));
        }
    }
    function bangTheDropper() {
        var gDrop = document.getElementById("ddlGrant0");
        var sel = gDrop.options[gDrop.selectedIndex].value;
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.GetGrantFromID(sel, 0, gotTheDropper);
    }
    function bangTheDropper2() {
        var gDrop = document.getElementById("ddlGrant1");       
       var sel = gDrop.options[gDrop.selectedIndex].value;
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.GetGrantFromID(sel, 1, gotTheDropper2);
    }
    function bangTheDropper3() {
        var gDrop = document.getElementById("ddlGrant2");
        var sel = gDrop.options[gDrop.selectedIndex].value;
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.GetGrantFromID(sel, 2, gotTheDropper3);
    }
    function bangTheDropper4() {
        var gDrop = document.getElementById("ddlGrant3");
        var sel = gDrop.options[gDrop.selectedIndex].value;
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.GetGrantFromID(sel, 3, gotTheDropper4);
    }
    function gotTheDropper(g) {
        selectedGrants = g;
        var lblCatalogNum = document.getElementById("lblCatalogNum");
        lblCatalogNum.innerText = selectedGrants[0].stateCatalogNum;
        var lblgrantNumber = document.getElementById("lblgrantNumber");
        lblgrantNumber.innerText = selectedGrants[0].grantNumber;

        var innies = document.getElementsByTagName("input");        
        var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-0") >= 0 }).ToArray();
        for (var j = 0; j < cols.length; j++) {
            cols[j].disabled = false;
            }
          
    }
    function gotTheDropper2(g) {
        selectedGrants = g;
        var lblCatalogNum = document.getElementById("lblCatalogNum2");
        lblCatalogNum.innerText = selectedGrants[1].stateCatalogNum;
        var lblgrantNumber = document.getElementById("lblgrantNumber2");
        lblgrantNumber.innerText = selectedGrants[1].grantNumber;

        var innies = document.getElementsByTagName("input");
        var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-1") >= 0 }).ToArray();
        for (var j = 0; j < cols.length; j++) {
            cols[j].disabled = false;
        }
    }
    function gotTheDropper3(g) {
        selectedGrants = g;
        var lblCatalogNum = document.getElementById("lblCatalogNum3");
        lblCatalogNum.innerText = selectedGrants[2].stateCatalogNum;
        var lblgrantNumber = document.getElementById("lblgrantNumber3");
        lblgrantNumber.innerText = selectedGrants[2].grantNumber;

        var innies = document.getElementsByTagName("input");
        var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-2") >= 0 }).ToArray();
        for (var j = 0; j < cols.length; j++) {
            cols[j].disabled = false;
        }
    }
    function gotTheDropper4(g) {
        selectedGrants = g;
        var lblCatalogNum = document.getElementById("lblCatalogNum4");
        lblCatalogNum.innerText = selectedGrants[3].stateCatalogNum;
        var lblgrantNumber = document.getElementById("lblgrantNumber4");
        lblgrantNumber.innerText = selectedGrants[3].grantNumber;

        var innies = document.getElementsByTagName("input");
        var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-3") >= 0 }).ToArray();
        for (var j = 0; j < cols.length; j++) {
            cols[j].disabled = false;
        }
    }
    function dateSelected() {
        var dater = $find("ceDate");
        var d = dater.get_selectedDate();
        var cranker = d.toDateString(); 
        var dater = new Date(cranker);                         
        var dtTxt = document.getElementById("txtDate");
        var cH2 = new Date(dtTxt.value);
        if (cH2.getMonth() != dater.getMonth()) {
            dater = new Date(dtTxt.value);
            cranker = dater.toDateString();
        }
        var dbag = cranker.split(" ");       
        var mo = dbag[1];
        var moNo = Enumerable.From(monthNames).IndexOf(mo);       
        clearTable();        
                      
       if (selectedDate == null || dater.getMonth() != selectedDate.getMonth()) {
           resetGrants();
            }
        selectedDate = new Date(cranker);        
        selectedDate.setDate(1);        
        var weekIx = 0;
        var dayIX = 0;
        for (var ix = 1; ix <= daysInMonth[moNo]; ix++) {
            dater.setDate(ix);
            addRow(dater, weekIx);
            if (dater.getDay() == 0 || ix == daysInMonth[moNo]) {
                addWeeklyTotalRow(weekIx);
                weekIx++;
                }
        }
        var innies = document.getElementsByTagName("input");
        for (var tt = 0; tt < 4; tt++) {
            var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-" + tt.toString()) >= 0 }).ToArray();
               for (var j = 0; j < cols.length; j++) {
                cols[j].disabled = true;
                }
        }
        addGrandTotalRow();
         var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
          }
        PageMethods.GetEmployeeTimeEntries(currentEmp, dater.getMonth(), dater.getFullYear(), haveSomeTimeEntries);
    }
    function haveSomeTimeEntries(teS) {
        if (teS != null) {
            timeEntries = teS;
            PageMethods.GetSelectedGrants(haveSomeGrants);
            }
    }
    function haveSomeGrants(g) {
        selectedGrants = g;
        var innies = document.getElementsByTagName("input");
        for (var k = 0; k < timeEntries.length; k++) {
            var teID = "";
            var lblCat = '';
            var lblID = "";
            if (timeEntries[k].grantID != 52 && timeEntries[k].grantID != 53 && timeEntries[k].grantID != 28) {
                var ix = findTheRightGrant(timeEntries[k].grantID);
                findGrantInCombo(selectedGrants[ix].ID, ix);
                if (ix == 0) {
                    lblID = "lblgrantNumber";
                    lblCat = "lblCatalogNum";
                }
                else {
                    lblID = "lblgrantNumber" + (ix + 1).toString();
                    lblCat = "lblCatalogNum" + (ix + 1).toString();
                }
                //var lblID = "lblgrantNumber" + ix.toString();
                var lblGRNum = document.getElementById(lblID);
                var lblCatNum = document.getElementById(lblCat);
                lblGRNum.innerText = selectedGrants[ix].grantNumber;
                lblCatNum.innerText = selectedGrants[ix].stateCatalogNum;
                if (ix >= 0) {
                    var cols = Enumerable.From(innies).Where(function (i) { return i.id.indexOf("-" + ix.toString()) >= 0 }).ToArray();
                    for (var j = 0; j < cols.length; j++) {
                        cols[j].disabled = false;
                        } 
                    }
            }                       
            var moNum = timeEntries[k].monthNumber + 1;
            if (timeEntries[k].monthNumber < 9) {
                teID = "0" + moNum.toString();
            }
            else {
                teID = moNum.toString();
            }
            if (timeEntries[k].dayNumber < 10) {
                teID += "0" + timeEntries[k].dayNumber.toString();
            }
            else {
                teID += timeEntries[k].dayNumber.toString();
            }
            if (timeEntries[k].grantID == 52) {
                teID += timeEntries[k].yearNumber.toString() + "-NG";
            }
            else if (timeEntries[k].grantID == 53) {
                teID += timeEntries[k].yearNumber.toString() + "-LV";
            }
            else {
                teID += timeEntries[k].yearNumber.toString() + "-" + ix.toString();
            }            
            var teUI = document.getElementById(teID);
            if (teUI != null) {
                teUI.value = timeEntries[k].grantHours.toString();
                }
        }
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.GetWeeklyGrantHours(HereAreTheWeeklys);
    }
    function HereAreTheWeeklys(e) {
        weeklyTotals = e;
        if (weeklyTotals != null) {
            for (var ix = 0; ix < weeklyTotals.length; ix++) {
                var gname = weeklyTotals[ix].grantName;
                var sel = findCorrectCombo(gname);
                var cell = null;
                if (weeklyTotals[ix].grantID == 52) {
                    cell = document.getElementById(weeklyTotals[ix].weekNumber.toString() + "-4");
                }
                else if (weeklyTotals[ix].grantID == 53) {
                    cell = document.getElementById(weeklyTotals[ix].weekNumber.toString() + "-5");
                }
                else {
                    cell = document.getElementById(weeklyTotals[ix].weekNumber.toString() + "-" + sel.toString());
                }
                if (cell != null) {
                    cell.innerText = weeklyTotals[ix].weeklyHours.toString();
                }
                var weekers = Enumerable.From(weeklyTotals).Where(function (wt) {return wt.weekNumber == weeklyTotals[ix].weekNumber}).ToArray();
                var tot = Enumerable.From(weekers).Sum(function (w) { return new Number(w.weeklyHours) });
                var ttID = weeklyTotals[ix].weekNumber.toString() + "-TT";
                var totTotCell = document.getElementById(ttID);
                totTotCell.innerText = tot.toString();
            }
        }
        acumulateDailyTots();
        if (bForApproval || bImpersonate) {
            var tbl = document.getElementById("tblSchedule");
            tbl.disabled = true;
        }
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
       PageMethods.getMonthStatus(haveAStatus);            
    }
    function haveAStatus(s) {
        status = s;
        var img = "";
        var btn = document.getElementById("btnSubmitter");
        var approve = document.getElementById("rblApproval");

        switch (status) {
            case 0:
                {
                    img = "New.png";
                    btn.style.display = "none";
                    break;
                }
            case 1:
                {
                    img = "Pending.png";
                    if ( approve != null) {
                        btn.style.display = "inline";
                    }
                    else {
                       btn.style.display = "none"; 
                    }
                    break;
                }
            case 2:
                {
                    img = "Disapproved.png";
                    if (approve != null) {
                        btn.style.display = "inline";
                        }
                    else {
                        btn.style.display = "none";
                        }
                    break;
                }
            case 3:
                {
                    img = "Approved.png";
                    btn.style.display = "none";
                    break;
                }
            default:
                {
                    img = "New.png";
                    break;
                }
        }
        var imgStat = document.getElementById("imgStatus");
        var query = window.location.search;
        if (query.indexOf("Review") >= 0) {
            img = "Approved.png";
            status = 3;
            }
        imgStat.src = img;
        imgStat.style.display = "inline";
        var tblSchedule = document.getElementById("tblSchedule");
        var imgF = document.getElementById("imgForward");               
        if (status == 3) {  //Already approved.            
            tblSchedule.disabled = true;            
            if (imgF != null) {
                imgF.disabled = true;
            }            
            if (approve != null) {
                approve.style.display = "none";
            }            
            if (btn != null) {
                btn.style.display = "none";
            }
        }
        else {
            if (!bImpersonate) {
                tblSchedule.disabled = false;
            }
            
            if (imgF != null) {
                imgF.disabled = false;
            }
            if (approve != null) {
                approve.style.display = "inline";
            }           
        }
    }
    function findGrantInCombo(ID, comboIx) {
        var cbID = "ddlGrant" + comboIx.toString();
        var cb = document.getElementById(cbID);
        for (var ix = 0; ix < cb.options.length; ix++) {
            if (cb.options[ix].value == ID) {
                cb.options[ix].selected = true;
                cb.disabled = true;
                return;
                }
        }
    }
    function findCorrectCombo(gName) {
        for(var iz = 0; iz < 4; iz++) {
            var cbID = "ddlGrant" + iz.toString();
            var cb = document.getElementById(cbID);
            if ( cb.options[cb.selectedIndex].text == gName) {
                return iz;
                }
            }
        return -1;
    }                  
    function findTheRightGrant(id) {
        for (var ix = 0; ix < selectedGrants.length; ix++) {
            if (selectedGrants[ix].ID == id) {
                return ix;
            }
        }
        return -1;
    }
    function clearTable() {
        var tbl = document.getElementById("tblSchedule");
        for (var i = tbl.rows.length - 1; i > 4; i--) {
            tbl.deleteRow(i);
            } 
    }
    function hours0Change() {
        var hours0 = document.getElementById("txtHours0").value;
        var el = window.event.srcElement;
        var hours0 = el.value;
        var ix = el.id.substr(el.id.length - 1, 1);
        var numHours0 = new Number(hours0);
        var daily0 = document.getElementById("cellDailyTotal" + ix);
        daily0.innerText = numHours0.toString();
        weeklyTotal += numHours0;
        var weekCell = document.getElementById("cellWeekTotal");
        weekCell.innerText = weeklyTotal.toString();
    }
    function hours1Change(e) {
        var hours1 = document.getElementById("txtHours1").value;
        var numHours1 = new Number(hours1);
        var daily1 = document.getElementById("cellDailyTotal1");
        daily1.innerText = numHours1.toString();
        weeklyTotal += numHours1;
        var weekCell = document.getElementById("cellWeekTotal");
        weekCell.innerText = weeklyTotal.toString();
    }
    function addWeeklyTotalRow(ix) {
        var tbl = document.getElementById("tblSchedule");
        var row = tbl.insertRow(-1);
        var dblCell = row.insertCell(-1);
        dblCell.colSpan = "2";
        dblCell.innerText = "Weekly Total";
        dblCell.style.backgroundColor = "yellow";
        for (var i = 0; i < 7; i++) {
            dblCell = row.insertCell(-1);
            dblCell.style.backgroundColor = "yellow";
            if (i == 6) { //The last cell on the right: the "grand total cell."
                dblCell.id = ix.toString() + "-TT";
                dblCell.style.fontWeight = "bold";
            }
            else {
                dblCell.id = ix.toString() + "-" + i.toString();
            }
            dblCell.style.color = "DarkBlue";
            dblCell.style.border = "1px dotted black";           
        }
    }

    function addGrandTotalRow() {
        var tbl = document.getElementById("tblSchedule");
        var row = tbl.insertRow(-1);
        var dblCell = row.insertCell(-1);
        dblCell.colSpan = "2";
        dblCell.innerText = "Monthly Total";
        dblCell.style.backgroundColor = "#89F5B6";
        for (var i = 0; i < 7; i++) {
            dblCell = row.insertCell(-1);
            dblCell.style.backgroundColor = "#89F5B6";                       
            dblCell.id = i.toString() + "-GT";            
            dblCell.style.color = "Black";
            dblCell.style.border = "1px dotted black";
            dblCell.style.fontWeight = "bold";
            dblCell.style.fontSize = "10pt";
        }
    }

    function addRow(dt, weekIx) {
        var tbl = document.getElementById("tblSchedule");
        var row = tbl.insertRow(-1);
        var cellDay = row.insertCell(-1);
        var clsName = "";
        var cellName = weekIx.toString() + "-" + dt.getDay().toString();
        if (tbl.rows.length % 2 == 0) {
            clsName = "table_cell";
        }
        else {
            clsName = "table_cell_alternate";
        }
        cellDay.className = clsName;
        cellDay.style.width = "100px";
        cellDay.innerText = weekday[dt.getDay()];
        var cellDate = row.insertCell(-1);
        cellDate.className = clsName;
        cellDate.innerText = dt.format("MMM dd,yyyy");
        cellDate.style.width = "100px";
        var cellgrant = row.insertCell(-1);
        var input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-0";
        input.name = cellName + "-0";
        input.style.width = "98px";
        input.onblur = updateHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant.className = clsName;
        cellgrant = row.insertCell(-1);
        input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-1";
        input.name = cellName + "-1";
        input.style.width = "98px";
        input.onblur = updateHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant.className = clsName;
        cellgrant = row.insertCell(-1);
        input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-2";
        input.name = cellName + "-2";
        input.style.width = "98px";
        input.onblur = updateHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant.className = clsName;
        cellgrant = row.insertCell(-1);
        input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-3";
        input.name = cellName + "-3";
        input.style.width = "98px";
        input.onblur = updateHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant.className = clsName;
        cellgrant = row.insertCell(-1);
        input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-NG";
        input.name = cellName + "-4";
        input.style.width = "98px";
        input.onblur = updateNGHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant.className = clsName;
        cellgrant = row.insertCell(-1);
        input = document.createElement("input");        
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-LV";
        input.name = cellName + "-5";
        input.style.width = "98px";
        input.onblur = updateLeaveHoursForTheDay;
        cellgrant.appendChild(input);
        cellgrant = row.insertCell(-1);        
        cellgrant.className = clsName;
        var txter = document.createTextNode("0");
        input = document.createElement("input");
        input.type = "text";
        input.id = dt.format("MMddyyyy") + "-DT";
        input.style.width = "98px";
        input.setAttribute('readonly', 'readonly');
        input.name = cellName + "-DT";
        input.style.color = "DarkBlue";      
        cellgrant.appendChild(input);
    }
    function acumulateDailyTots() {
        if (selectedDate != null) {
            var d = new Date();
            d.setDate(1);
            d.setFullYear(selectedDate.getFullYear());
            d.setMonth(selectedDate.getMonth());                                  
            var inputs = document.getElementsByTagName("input");
            var dailyTots = Enumerable.From(inputs).Where(function (i) { return i.id.indexOf("-DT") > 0 }).ToArray();
            for (var j = 1; j < 32; j++) {
                d.setDate(j);
                var searcher = d.format("MMddyyyy");
                var dayVals = Enumerable.From(inputs).Where(function (i) { return i.id.indexOf(searcher) >= 0 }).ToArray();
                if (dayVals != null && dayVals.length > 0) {
                    var vals = Enumerable.From(dayVals).Where(function (dv) { return dv.value != "" }).ToArray();
                    var tot = Enumerable.From(vals).Sum(function (v) { return new Number(v.value) });                    
                    searcher += "-DT";
                    var dailyTotCell = Enumerable.From(dayVals).Where(function (day) { return day.id == searcher }).First();
                    tot -= new Number(dailyTotCell.value);
                    dailyTotCell.value = tot.toString();
                }
                //var dayCell = Enumerable.From(dayVals).Where(function (dv) { return dv.id.indexOf(searcher) >= 0 }).;                
               }        
            }
        accumlateGrandTotal();
        accumlateGrantTotals();
    }
    function accumlateGrandTotal() {
        if (timeEntries != null) {
            totalHours = Enumerable.From(timeEntries).Sum(function (te) { return te.grantHours });
            var hSlap = document.getElementById("hSlap");
            hSlap.innerText = "Total Monthly Hours: " + totalHours.toString();
            }
    }
    function accumlateGrantTotals() {             
        var innies = document.getElementsByTagName("td");
        for (var i = 0; i < 6; i++) {
            var cells = Enumerable.From(innies).Where(function (cn) { return cn.id.indexOf("-" + i.toString()) == 1 }).ToArray(); //all the weekly totals for this grant
            var tot = Enumerable.From(cells).Sum(function (c) { return new Number(c.innerText) });
            var gtCell = document.getElementById(i.toString() + "-GT");
            gtCell.innerText = tot.toString();
            }                              
    }
    function updateHoursForTheDay()
    {
        var el = window.event.srcElement;
        var hrs = el.value;
        try {            
            var n = new Number(hrs);
            if (isNaN(n)) {
                el.value = "";
                return;
                }
        }
        catch (ex) {
            el.value = "";
            return;
        }
        var parts = el.id.split("-");
        var cb = document.getElementById("ddlGrant" + parts[1]);
        var grt = cb.options[cb.selectedIndex].value;
        var sel = Enumerable.From(selectedGrants).Where(function (g) { return g.ID.toString() == grt }).FirstOrDefault();
        if (sel.category.indexOf("PlaceHold") >= 0) {
            cb.style.color = "red";
            return;
        }
        cb.style.color = "black";
        updateWeeklyandDailyTots(el);
        accumlateGrantTotals();
        if (hrs == "") {
            hrs = "0";
        }
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.updateDailyGrantHours(el.id, grt, currentEmp, hrs, haveTheTotal);
   }
   function haveTheTotal(t) {
       totalHours = t;
       var slap = document.getElementById("hSlap");
       slap.innerText = "Total Monthly Hours: " + totalHours.toString();
   }
    function updateWeeklyandDailyTots(cell) {
        var name = cell.name;
        var innies = document.getElementsByTagName("input");
        var slits = name.split("-");
        var weeklys = Enumerable.From(innies).Where(function (i) { return i.name.indexOf(slits[0] + "-") == 0 && i.name.lastIndexOf("-" + slits[2]) == 3 }).ToArray();
        var dailys = Enumerable.From(innies).Where(function (i) { return i.name.indexOf(slits[0] + "-" + slits[1]) == 0 && i.name.indexOf("-DT") < 0 }).ToArray();
        var allDailys = Enumerable.From(innies).Where(function (i) { return i.name.indexOf(slits[0] + "-") == 0 && i.name.indexOf("-DT") > 0 }).ToArray();         
       // var tot = Enumerable.From(vals).Sum(function (v) { return new Number(v.value) });
        var tot = Enumerable.From(weeklys).Sum(function (val) { return new Number(val.value) });
        var totDay = Enumerable.From(dailys).Sum(function(day) {return new Number(day.value)});
        var weeklyID = slits[0] + "-" + slits[2];
        var totCell = document.getElementById(weeklyID);
        if (totCell != null) {
            totCell.innerText = tot.toString();
        }
        var dayTot = document.getElementsByName(slits[0] + "-" + slits[1] + "-DT")[0];
        if ( dayTot != null) {
            dayTot.innerText = totDay.toString();
        }
        var totTotId = slits[0] + "-TT";
        var totalTotal = Enumerable.From(allDailys).Sum(function (day) { return new Number(day.value) });
        var tt = document.getElementById(totTotId);
        tt.innerText = totalTotal.toString();
    }
    function updateNGHoursForTheDay() {
        var el = window.event.srcElement;
        var parts = el.id.split("-");
        var hrs = el.value;
        if (hrs != "") {
            updateWeeklyandDailyTots(el);
            accumlateGrantTotals(); 
             var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
           }           
            PageMethods.updateDailyNonGrantHours(el.id, currentEmp, hrs, haveTheTotal);
        }
    }
    function updateLeaveHoursForTheDay() {
        var el = window.event.srcElement;
        var parts = el.id.split("-");
        var hrs = el.value;
        if (hrs != "") {
            updateWeeklyandDailyTots(el);
            accumlateGrantTotals();
             var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
         if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
            }
            PageMethods.updateDailyLeaveHours(el.id, currentEmp, hrs, haveTheTotal);
        }
    }
    function didUpdate() {
    }
    function forwardHover() {
        window.event.srcElement.src="./Forward.png";
    }
    function forwardLeave() {
        window.event.srcElement.src = "./forward_orange.png";
    }
    function forwardClicked() {
        var ary = [];
        if (selectedGrants != null) {
            for (var i = 0; i < selectedGrants.length && i < 4; i++) {
                if (selectedGrants[i].ID != 28) {
                    var nm = "ddlSup" + (i + 1).toString();
                    var dd = document.getElementById(nm);
                    var id = dd.options[dd.selectedIndex].value;
                    ary[i] = id;
                }
            }
            var curDate = $find("ceDate");
            var d = curDate.get_selectedDate();
             var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
            if ( session == false) {
               alert("Page has timed out, you will now be redirected to the Login page.");
               window.location = "Account/Login.aspx";
            }
            PageMethods.sendOffEmail(ary, currentEmp.ID, d, mailSent);
        }
    }
    function mailSent(e) {
        var imgStat = document.getElementById("imgStatus");
        imgStat.style.display = "inline";
        imgStat.src = "Pending.png";
    }
    function empChanger() {
        var sel = window.event.srcElement;
        var op = sel.options[sel.selectedIndex];
        resetValues();
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
          }
        PageMethods.getTheCurrentEmp(op.value, haveAnEmp);
    }
    function resetValues() {
        var cal = $find("ceDate");
        var selD = cal.get_selectedDate();
        var disabled = (selD.getFullYear() > 2018);
        var drop = document.getElementById("ddlGrant0");
        drop.disabled = disabled;
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant1");
        drop.disabled = disabled;      
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant2");
        drop.disabled = disabled;
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant3");
        drop.disabled = disabled;
        drop.selectedIndex = 0;
        var cat = document.getElementById("lblgrantNumber");
        cat.innerText = "";
        cat = document.getElementById("lblgrantNumber2");
        cat.innerText = "";
        cat = document.getElementById("lblgrantNumber3");
        cat.innerText = "";
        cat = document.getElementById("lblgrantNumber4");
        cat.innerText = "";
        cat = document.getElementById("lblCatalogNum");
        cat.innerText = "";
        cat = document.getElementById("lblCatalogNum2");
        cat.innerText = "";
        cat = document.getElementById("lblCatalogNum3");
        cat.innerText = "";
        cat = document.getElementById("lblCatalogNum4");
        cat.innerText = "";
        currentEmp = null;
        clearTable();
        var d = new Date();
       
        //cal.set_selectedDate(d);
    }
    function resetGrants() {
        var drop = document.getElementById("ddlGrant0");
        drop.disabled = false;
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant1");
        drop.disabled = false;
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant2");
        drop.disabled = false;
        drop.selectedIndex = 0;
        drop = document.getElementById("ddlGrant3");
        drop.disabled = false;
        drop.selectedIndex = 0;
        var catNum = "";
        var grantNum = "";
        if (selectedGrants != null) {
            catNum = selectedGrants[3].stateCatalogNum;
            grantNum = selectedGrants[3].grantNumber;
            }
        var cat = document.getElementById("lblgrantNumber");
        cat.innerText = grantNum;
        cat = document.getElementById("lblgrantNumber2");
        cat.innerText = grantNum;
        cat = document.getElementById("lblgrantNumber3");
        cat.innerText = grantNum;
        cat = document.getElementById("lblgrantNumber4");
        cat.innerText = grantNum;
        cat = document.getElementById("lblCatalogNum");
        cat.innerText = catNum;
        cat = document.getElementById("lblCatalogNum2");
        cat.innerText = catNum;
        cat = document.getElementById("lblCatalogNum3");
        cat.innerText = catNum;
        cat = document.getElementById("lblCatalogNum4");
        cat.innerText = catNum;        
    }
    function haveAnEmp(e) {
        currentEmp = e;
        if (currentEmp != null) {
            var txtPos = document.getElementById("txtPosition");
            txtPos.value = currentEmp.jobTitle;
            findSupervisor();
        }
        else {
            window.location = "Account/Login.aspx";
        }
    }

    function findSupervisor() {
        if (currentEmp != null) {
            var sup1 = document.getElementById("ddlSup1");
            var hammer = Enumerable.From(sup1.options).Where(function (o) { return o.value == currentEmp.defaultSupervisor }).First();
            hammer.selected = true;
            var sup2 = document.getElementById("ddlSup2");
            hammer = Enumerable.From(sup2.options).Where(function (o) { return o.value == currentEmp.defaultSupervisor }).First();
            hammer.selected = true;
            var sup3 = document.getElementById("ddlSup3");
            hammer = Enumerable.From(sup3.options).Where(function (o) { return o.value == currentEmp.defaultSupervisor }).First();
            hammer.selected = true;
            var sup4 = document.getElementById("ddlSup4");
            hammer = Enumerable.From(sup4.options).Where(function (o) { return o.value == currentEmp.defaultSupervisor }).First();
            hammer.selected = true;
            }
    }
    function supChange() {
        var el = window.event.srcElement;
        var num = new Number(el.id.substring(el.id.length - 1));
        num--;
        var supID = el.options[el.selectedIndex].value;
        var cbG = document.getElementById("ddlGrant" + num.toString());
        var grt = new Number(cbG.options[cbG.selectedIndex].value);
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.AssignSupervisor(supID, grt, currentEmp.ID, haveSomeTimeEntries);
    }
    function load() {
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        var query = window.location.search;
        if (query.indexOf("approval") >= 0 || query.indexOf("Review") >= 0) {
            bForApproval = true;
            loadTableForApproval();            
            var params = query.split("?");
            var params2 = params[1].split("&");
            PageMethods.GetEmployeeTimeEnties(haveSomeTimeEntries);
        }
        else {
            var snag = document.getElementById("pnlSnagger");
            snag.style.visibility = "hidden";
            if (query.indexOf("Impersonate") >= 0) {
                bImpersonate = true;
            }
        }
    }

    function loadTableForApproval() {
        var dater = $find("ceDate");
        var query = window.location.search;
        var params = query.split("?");
        var params2 = params[1].split("&");
        var monthSplit = params2[3].split("=");
        var yrsplit = params2[4].split("=");

        try {            
            //clearTable();
            selectedDate = new Date();
            selectedDate.setFullYear(new Number(yrsplit[1]));
            selectedDate.setMonth(new Number(monthSplit[1]));           
            //selectedDate.setMonth(dater.getMonth());
            //selectedDate.setFullYear(dater.getFullYear());
            var bagger = new Number(monthSplit[1]);
            if (bagger < 0) {
                bagger = 0;
            }
            selectedDate.setDate(1);
            var dater = new Date();
            dater.setFullYear(selectedDate.getFullYear());
            dater.setMonth(selectedDate.getMonth());
            dater.setDate(1);          
            var weekIx = 0;
            var dayIX = 0;
            for (var ix = 1; ix <= daysInMonth[bagger]; ix++) {
                dater.setDate(ix);
                addRow(dater, weekIx);
                if (dater.getDay() == 0 || ix == daysInMonth[bagger]) {
                    addWeeklyTotalRow(weekIx);
                    weekIx++;
                }
            }
            var txtDate = document.getElementById("txtDate");
            txtDate.disabled = true;
        }
        catch (err) {
        }
        addGrandTotalRow();       
    }
    function Changer() {
        var snapper = window.event.srcElement;
        var apper = document.getElementById("btnSubmitter");
        var popper = $find("Popper1");
        if (snapper.value == "0") {
            apper.value = "Submit Approval";
            popper.hidePopup();
            approvalValue = 0;            
        }
        else {
            apper.value = "Submit Disapproval";
            popper.showPopup();
            approvalValue = 1;
        }
    }
    function crankTheApprover() {
        var bGoodToGo = (approvalValue == 0) ? true : false;
        var session = <%=Session["CurrentEmployeeList"] != null ? "true" : "false"%>;
        if ( session == false) {
            alert("Page has timed out, you will now be redirected to the Login page.");
            window.location = "Account/Login.aspx";
        }
        PageMethods.approveOrDisapprove(bGoodToGo, reasonTxt, approvalDone);      
    }
    function getTheReason() {
        var el = window.event.srcElement;
        reasonTxt = el.value;
    }
    function approvalDone(b) {
        if (b) {
            var snag = document.getElementById("pnlSnagger");
            snag.style.visibility = "hidden";
            var imgStat = document.getElementById("imgStatus");
            imgStat.style.display = "inline";
            if (approvalValue == 0) {
                imgStat.src = "Approved.png";
            }
            else {
                imgStat.src = "Disapproved.png";
            }            
        }
    }
    </script>
    <link href="~/Styles/Site.css" rel="stylesheet" type="text/css" />
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">    
        <h2 id="cranker" runat="server">
            Welcome to the MSTC Grant Allocation Form!
        </h2>            
        <h4 id="hSlap">Total Monthly Hours:</h4>
        <asp:Image runat="server" ID="imgStatus" ImageUrl="~/pending.png" ClientIDMode="Static" style="position:absolute; top:110px; left:960px; display:none" />                                    
        <asp:Panel runat="server" ID="pnlSnagger" ClientIDMode="Static">
            <asp:TextBox runat="server" ID="txtReason" TextMode="MultiLine" Height="30px" Width="180px" ClientIDMode="Static" Font-Names="Arial" Font-Size="8pt" onchange="getTheReason()"></asp:TextBox>            
            </asp:Panel>
            <asp:PopupControlExtender
                ID="Popper1" runat="server" TargetControlID="rblApproval" PopupControlID="pnlSnagger" OffsetX="0" OffsetY="30" ClientIDMode="Static">
            </asp:PopupControlExtender>        
        <table>
            <tr>
                <td>                    
                    <asp:Label runat="server" ID="lblEmp" Text="Employee:"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblPosition" Text="Position:"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblDate" Text="Select a date for the time entry month" ></asp:Label>
                </td>
                <td style="width:100px"></td>
                <td>
                    <asp:Label runat="server" ID="lblForward" Text="Forward..."></asp:Label>
                </td>
                <td rowspan="2">
                    <asp:RadioButtonList runat="server" ID="rblApproval" Visible="false" onclick="Changer()" ClientIDMode="Static" >
                        <asp:ListItem Value="0">Approve Monthly Hours</asp:ListItem>
                        <asp:ListItem Value="1">Disapprove Monthly Hours</asp:ListItem>
                    </asp:RadioButtonList>
                </td>                                                                                                      
            </tr>
            <tr>
                <td>
                    <asp:DropDownList runat="server" ID="ddlEmp" ClientIDMode="Static" onchange="empChanger()" Font-Size="8pt" ForeColor="#444444"></asp:DropDownList>                    
                </td>
                <td>
                    <asp:TextBox runat="server" ID="txtPosition" ReadOnly="true" ClientIDMode="Static" Font-Size="8pt" ForeColor="#444444"></asp:TextBox>
                </td>                
                <td>
                    <asp:TextBox runat="server" ID="txtDate" Font-Size="8pt" ForeColor="#444444" ClientIDMode="Static"></asp:TextBox>
                    <asp:CalendarExtender runat="server" ID="ceDate" TargetControlID="txtDate" Animated="true" Format="MMMM dd, yyyy" 
                        OnClientDateSelectionChanged="dateSelected" ClientIDMode="Static" DefaultView="Months"></asp:CalendarExtender>
                </td>
                <td></td>                
                <td>
                    <asp:Image runat="server" ID="imgForward" ImageUrl="~/forward_orange.png" ToolTip="Forward" ClientIDMode="Static" 
                        onmouseover="forwardHover()" onmouseleave="forwardLeave()" onclick="forwardClicked()"/> 
                </td>
                <td>
                    <input type="button" value="Submit Approval" onclick="crankTheApprover()" id="btnSubmitter" style="display:none" />                    
                </td>
            </tr>
            </table>
            <table id="tblSchedule">
            <tr>
                <td class="GrantHeadings" colspan="2" style="width:200px">Grant Title</td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlGrant0" onchange="bangTheDropper()" ClientIDMode="Static" CssClass="comboBox">
                    </asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlGrant1" onchange="bangTheDropper2()" ClientIDMode="Static" CssClass="comboBox">
                    </asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlGrant2" onchange="bangTheDropper3()" ClientIDMode="Static" CssClass="comboBox">
                    </asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlGrant3" onchange="bangTheDropper4()" ClientIDMode="Static" CssClass="comboBox">
                    </asp:DropDownList>
                </td>
                <td rowspan="3" colspan="3" style="background-image:url('mstc.png'); background-repeat:no-repeat; background-position:center;">
                </td>
            </tr>
            <tr>
                <td class="GrantHeadings" colspan="2">Grant Number</td>
                <td>
                    <asp:Label runat="server" ID="lblgrantNumber" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblgrantNumber2" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblgrantNumber3" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblgrantNumber4" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="GrantHeadings" colspan="2">CFDA#/state catalog#</td>
                <td>
                    <asp:Label runat="server" ID="lblCatalogNum" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblCatalogNum2" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblCatalogNum3" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>
                <td>
                    <asp:Label runat="server" ID="lblCatalogNum4" ClientIDMode="Static" BorderStyle="Solid" BorderWidth="1" BorderColor="Gray" CssClass="comboBox"></asp:Label>
                </td>                
            </tr>
            <tr>
                <td class="GrantHeadings" colspan="2">Supervisor</td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlSup1" ClientIDMode="Static" CssClass="comboBox" onchange="supChange()"></asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlSup2" ClientIDMode="Static" CssClass="comboBox" onchange="supChange()"></asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlSup3" ClientIDMode="Static" CssClass="comboBox" onchange="supChange()"></asp:DropDownList>
                </td>
                <td>
                    <asp:DropDownList runat="server" ID="ddlSup4" ClientIDMode="Static" CssClass="comboBox" onchange="supChange()"></asp:DropDownList>
                </td>
                <td colspan="3"></td>
            </tr>
            <tr>
                <td class="table_header">Day</td>
                <td class="table_header">Date</td>
                <td class="table_header">Grant Hours</td>
                <td class="table_header">Grant Hours</td>
                <td class="table_header">Grant Hours</td>
                <td class="table_header">Grant Hours</td>
                <td class="table_header">Non-Grant Hours</td>
                <td class="table_header">Leave Hours</td>
                <td style="font-weight:bold; border: 1px dotted Black">Daily Total</td>
            </tr>
            </table>                   
    <p>
        You can also find information at: <a href="https://thesource.mywilm.com/District/Grants/SitePages/Home.aspx&AuthResend1908BC2350124b5095AB75012FA405BA"
            title="MSTC Grant Information">documentation on Mid-State grants</a>.
    </p>
</asp:Content>
