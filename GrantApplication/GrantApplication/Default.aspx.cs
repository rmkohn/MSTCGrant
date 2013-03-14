using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Data;
using System.Configuration;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml.Linq;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace GrantApplication
{
    public partial class _Default : System.Web.UI.Page
    {
        public static int[] totalDaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.Params["approval"] != null || Request.Params["Review"] != null)
                {
                    loadPageForApproval();
                    return;
                }
                if (Session["CurrentEmployeeList"] == null)
                {
                    Response.Redirect("Account/Login.aspx");
                }
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                conn.Open();
                //setDefaultSups(conn);
                //nowSetTheNumbers(conn);
                List<Grant> grants = getGrants(conn);
                //Session["Grants"] = grants;
                List<Grant> selGrants = new List<Grant>();
                foreach (Grant g in grants)
                {
                    ListItem li = new ListItem(g.grantTitle, g.ID.ToString());
                    ddlGrant0.Items.Add(li);
                    ddlGrant1.Items.Add(li);
                    ddlGrant2.Items.Add(li);
                    ddlGrant3.Items.Add(li);
                }
                for (int i = 0; i < 6; i++)
                {
                    selGrants.Add(new Grant(grants[0]));
                }
                Session["SelectedGrants"] = selGrants;
                List<Employee> emps = getEmployees(conn);
                lblCatalogNum.Text = grants[0].stateCatalogNum;
                lblCatalogNum2.Text = grants[0].stateCatalogNum;
                lblCatalogNum3.Text = grants[0].stateCatalogNum;
                lblCatalogNum4.Text = grants[0].stateCatalogNum;

                lblgrantNumber.Text = grants[0].grantNumber;
                lblgrantNumber2.Text = grants[0].grantNumber;
                lblgrantNumber3.Text = grants[0].grantNumber;
                lblgrantNumber4.Text = grants[0].grantNumber;                                              
                //performUpdate(emps, conn);
                Session["Employees"] = emps;
                Session["Grants"] = grants;
                List<Employee> myGuys = (List<Employee>)Session["CurrentEmployeeList"];
                ddlEmp.Items.Add(new ListItem("<Please select an employee>", "0"));
                foreach (Employee emp in myGuys)
                {
                    string str = emp.firstName + " " + emp.lastName + " : " + emp.jobTitle;
                    ListItem li = new ListItem();
                    li.Value = emp.ID.ToString();
                    li.Text = str;
                    ddlEmp.Items.Add(li);
                }
                ddlEmp.SelectedIndex = 0;
                populateSupervisorLists(conn);
                conn.Close();
                ceDate.EndDate = DateTime.Today;
                cranker.InnerText = "Welcome to the MSTC Grant Allocation Form " + myGuys[0].firstName + "!";
                ddlGrant0.Enabled = false;
                ddlGrant1.Enabled = false;
                ddlGrant2.Enabled = false;
                ddlGrant3.Enabled = false;
                ceDate.SelectedDate = new DateTime(2020, 1, 1);
            }            
            //updateEmails();
        }

        private bool loadPageForApproval()
        {
            lblForward.Visible = false;
            imgForward.Visible = false;
            rblApproval.Visible = true;
            bool bForApprove = (Request.Params["approval"] != null);
            Employee sup = getTheLoggedInSupervisor(System.Convert.ToInt32(Request.Params["ID"].ToString()));
            cranker.InnerText = (bForApprove) ? "Welcome, " + sup.firstName + " please approve or disapprove this month." : "Welcome, " + 
                        sup.firstName + " please review this monthly time entry.";
            Session["Supervisor"] = sup;
            int month = System.Convert.ToInt32(Request.Params["month"]);
            int year = System.Convert.ToInt32(Request.Params["Year"]);
            Session["month"] = month;
            Session["Year"] = year;

            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            List<Grant> grants = getApprovalGrant(conn);
            fillInGrantInfo(grants);
            Session["SelectedGrants"] = grants;
            List<Employee> emps = getEmployees(conn, true);
            conn.Close();
            Session["CurrentEmployeeList"] = emps;
            fillInEmpInfo(emps);
            setTheApprovalDate();
            setTheSupervisorInCBs(sup);
                                   
            return true;
        }
        private void setTheSupervisorInCBs(Employee sup)
        {
            ListItem li = new ListItem(sup.firstName + " " + sup.lastName);
            ddlSup1.Items.Add(li);
            ddlSup2.Items.Add(li);
            ddlSup3.Items.Add(li);
            ddlSup4.Items.Add(li);
            ddlSup1.Enabled = false;
            ddlSup2.Enabled = false;
            ddlSup3.Enabled = false;
            ddlSup4.Enabled = false;
        }
        private void fillInEmpInfo(List<Employee> emps)
        {
            foreach (Employee emp in emps)
            {
                string str = emp.firstName + " " + emp.lastName + " : " + emp.jobTitle;
                ListItem li = new ListItem();
                li.Value = emp.ID.ToString();
                li.Text = str;
                ddlEmp.Items.Add(li);
            }
            ddlEmp.SelectedIndex = 0;
            this.txtPosition.Text = emps[0].jobTitle;
            txtPosition.Enabled = false;
        }
        private void setTheApprovalDate()
        {
            int month = System.Convert.ToInt32(Request.Params["month"]);
            int year = System.Convert.ToInt32(Request.Params["Year"]);
            DateTime dt = new DateTime(year, month+1, 1);  //Have to adjust for difference between .Net DateTime and Javascript date.
            ceDate.SelectedDate = dt;
            //txtDate.Enabled = false;
        }
        private void fillInGrantInfo(List<Grant> grants)
        {
            List<Grant> selGrants = new List<Grant>();
            foreach (Grant g in grants)
            {
                ListItem li = new ListItem(g.grantTitle, g.ID.ToString());
                ddlGrant0.Items.Add(li);
                ddlGrant1.Items.Add(li);
                ddlGrant2.Items.Add(li);
                ddlGrant3.Items.Add(li);
            }
            for (int i = 0; i < 6; i++)
            {
                selGrants.Add(new Grant(grants[0]));
            }
            Session["SelectedGrants"] = selGrants;

            lblCatalogNum.Text = grants[0].stateCatalogNum;
            lblCatalogNum.Enabled = false;
            lblCatalogNum2.Text = grants[0].stateCatalogNum;
            lblCatalogNum2.Enabled = false;
            lblCatalogNum3.Text = grants[0].stateCatalogNum;
            lblCatalogNum3.Enabled = false;
            lblCatalogNum4.Text = grants[0].stateCatalogNum;
            lblCatalogNum4.Enabled = false;

            lblgrantNumber.Text = grants[0].grantNumber;
            lblgrantNumber.Enabled = false;
            lblgrantNumber2.Text = grants[0].grantNumber;
            lblgrantNumber2.Enabled = false;
            lblgrantNumber3.Text = grants[0].grantNumber;
            lblgrantNumber3.Enabled = false;
            lblgrantNumber4.Text = grants[0].grantNumber;
            lblgrantNumber4.Enabled = false;
        }
        private Employee getTheLoggedInSupervisor(int id)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string str = "select * from employeelist where ID=" + id.ToString();
            OleDbCommand comm = new OleDbCommand(str, conn);
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return null;
            }
            DataRow dr = set.Tables[0].Rows[0];
            Employee sup = new Employee();
            sup.ID = (int)dr[0];
            sup.EmpNum = dr[1].ToString();
            sup.lastName = dr[2].ToString();
            sup.firstName = dr[3].ToString();
            sup.jobTitle = dr[4].ToString();
            sup.emailAddress = dr[5].ToString();
            sup.registered = (bool)dr[7];
            sup.manager = true;
            conn.Close();
            return sup;
        }

        private static List<Grant> resetGrants()
        {
            List<Grant> grants = (List<Grant>)HttpContext.Current.Session["Grants"];
            //Session["Grants"] = grants;
            List<Grant> selGrants = new List<Grant>();
           
            for (int i = 0; i < 6; i++)
            {
                selGrants.Add(new Grant(grants[0]));
            }
            return selGrants;
        }

        private bool updateEmails()
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string str = "select * from EmployeeList";
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandText = str;
            comm.CommandType = CommandType.Text;
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return false;
            }
            List<Employee> emps = new List<Employee>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Employee emp = new Employee();
                emp.ID = (int)dr[0];
                emp.emailAddress = dr[5].ToString();
                emp.lastName = dr[2].ToString();
                emp.firstName = dr[3].ToString();
                emps.Add(emp);
            }
            
            OleDbCommand comm2 = new OleDbCommand();
            comm2.Connection = conn;
            
            foreach (Employee e in emps)
            {
                if (e.emailAddress == string.Empty)
                {
                    str = "update EmployeeList set EmailAddress='" + e.firstName + "." + e.lastName + "@mstc.edu' where ID=" + e.ID.ToString();
                    comm2.CommandText = str;
                    try
                    {
                        comm2.ExecuteNonQuery();
                    }
                    catch (System.Exception egg)
                    {
                        continue;
                    }                    
                }
            }
            conn.Close();
            return true;
        }

        private bool setDefaultSups(OleDbConnection conn)
        {
            string path = Request.PhysicalApplicationPath + "App_Data\\Grant Employee List All Data.csv";
            string outputPath = Request.PhysicalApplicationPath + "Output.txt";
            OleDbCommand comm = new OleDbCommand();
            comm.CommandType = CommandType.Text;
            comm.Connection = conn;
            string[] emps = File.ReadAllLines(path);
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            OleDbCommand updater = new OleDbCommand();
            updater.Connection = conn;
            updater.CommandType = CommandType.Text;
            List<string> allEmps = new List<string>();
            foreach (string s in emps)
            {
                 try
                 {
                    string[] semp = s.Split(new string[] { "," }, StringSplitOptions.None);
                    string[] strName = semp[semp.Count() - 1].Split(new string[] { " " }, StringSplitOptions.None);
                    string str = "select ID from EmployeeList where LastName = '" + strName[1] + "' and FirstName='" + strName[0] + "'";
                    comm.CommandText = str;
                    adapter.SelectCommand = comm;
                    set.Reset();                    
                    adapter.Fill(set);
                    if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                    {
                        string ss = semp[0] + "," + set.Tables[0].Rows[0][0];
                        allEmps.Add(ss);
                        //updater.CommandText = "update EmployeeList set DefaultSupervisor=" + set.Tables[0].Rows[0][0].ToString() + " where EmployeeNum=" + semp[0];
                        //updater.ExecuteNonQuery();
                    }
                }
                catch (System.Exception ex)
                {
                }                
            }
            File.WriteAllLines(outputPath, allEmps.ToArray());
            return true;
        }

        private bool nowSetTheNumbers(OleDbConnection conn)
        {
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            string outputPath = Request.PhysicalApplicationPath + "Output.txt";
            string[] ss = File.ReadAllLines(outputPath);
            foreach (string s in ss)
            {
                string[] sbag = s.Split(new string[] { "," }, StringSplitOptions.None);
                comm.CommandText = "update EmployeeList set DefaultSupervisor = " + sbag[1] + " where EmployeeNUm = " + sbag[0];
                try
                {
                    comm.ExecuteNonQuery();
                }
                catch (System.Exception ex)
                {
                }

            }
            return true;
        }

        private bool populateSupervisorLists(OleDbConnection conn)
        {
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandText = "select distinct ID, FirstName + ' ' + LastName as FullName from EmployeeList where manager=true";
            comm.CommandType = CommandType.Text;
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return false;
            }
            List<DataRow> rows = set.Tables[0].AsEnumerable().ToList();
            var bag = (from r in rows select r[1].ToString()).Distinct();
            foreach (string s in bag)
            {
                var lbag = (from dr in rows where dr[1].ToString() == s select dr).First();
                ListItem li = new ListItem(lbag[1].ToString(), lbag[0].ToString());
                    if (!ddlSup1.Items.Contains(li))
                    {
                        ddlSup1.Items.Add(li);
                        ddlSup2.Items.Add(li);
                        ddlSup3.Items.Add(li);
                        ddlSup4.Items.Add(li);
                    }                
            }
            return true;
        }
        
        private List<Grant> getGrants(OleDbConnection conn)
        {
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            comm.CommandText = "select * from GrantInfo order by ID";
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();

            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception eb)
            {
                return null;
            }
            List<Grant> grants = new List<Grant>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Grant g = new Grant();
                g.ID = (int)dr[0];
                g.stateCatalogNum = dr[1].ToString();
                g.category = dr[2].ToString();
                g.grantNumber = dr[3].ToString();
                g.grantTitle = dr[4].ToString();
                g.grantManagerLast = dr[5].ToString();
                g.grantManagerFirst = dr[6].ToString();
                grants.Add(g);
            }
            Session["Grants"] = grants;
            return grants;
        }

        private List<Grant> getApprovalGrant(OleDbConnection conn)
        {
            int grantID = System.Convert.ToInt32(Request.Params["GrantID"]);
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            comm.CommandText = "select * from GrantInfo where ID in (" + grantID.ToString() + ",52,53)";
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();

            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception eb)
            {
                return null;
            }
            List<Grant> grants = new List<Grant>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Grant g = new Grant();
                g.ID = (int)dr[0];
                g.stateCatalogNum = dr[1].ToString();
                g.category = dr[2].ToString();
                g.grantNumber = dr[3].ToString();
                g.grantTitle = dr[4].ToString();
                g.grantManagerLast = dr[5].ToString();
                g.grantManagerFirst = dr[6].ToString();
                grants.Add(g);
            }
            Session["Grants"] = grants;
            //conn.Close();
            return grants;
        }      

        private List<Employee> getEmployees(OleDbConnection conn, bool forApproval = false)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            if (forApproval)
            {
                int empID = System.Convert.ToInt32(Request.Params["Employee"]);
                comm.CommandText = "select * from EmployeeList where ID=" + empID.ToString();
            }
            else
            {
                comm.CommandText = "select * from EmployeeList";
            }
            
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();

            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception eb)
            {
                return null;
            }
            List<Employee> emps = new List<Employee>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Employee e = new Employee();
                e.ID = (int)dr[0];
                e.EmpNum = dr[1].ToString();
                e.lastName = dr[2].ToString();
                e.firstName = dr[3].ToString();
                e.jobTitle = dr[4].ToString();
                e.emailAddress = dr[5].ToString();
                e.password = dr[6].ToString();
                e.registered = (bool)dr[7];
                
                emps.Add(e);
            }
            return emps;
        }

        private void performUpdate(List<Employee> emps, OleDbConnection conn)
        {
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            foreach (Employee emp in emps)
            {
                string sfirst = emp.firstName.Replace("\"", "");
                string slast = emp.lastName.Replace("\"", "");
                string scomm = "update EmployeeList set LastName = '" + slast + "', FirstName = '" + sfirst + "' where ID=" + emp.ID.ToString();
                comm.CommandText = scomm;
                comm.ExecuteNonQuery();
                emp.lastName = slast;
                emp.firstName = sfirst;
            }
            Session["Employees"] = emps;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static string[] GetCompletionList(string prefixText, int count)
        {
            if (HttpContext.Current.Session["Employees"] != null && count > 1)
            {
                List<Employee> emps = (List<Employee>)HttpContext.Current.Session["Employees"];
                var eS = (from e in emps where e.lastName.ToLower().StartsWith(prefixText.ToLower()) select e.lastName + ", " + e.firstName + ": " + e.jobTitle).ToArray();
                return eS;
            }
            return new string[0];
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static Grant GetGrantFromString(string name)
        {
            List<Grant> grants = (List<Grant>)HttpContext.Current.Session["Grants"];
            var grant = (from g in grants where g.grantTitle.Contains(name) select g).SingleOrDefault();
            return grant;
        }
        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static Grant[] GetGrantFromID(int ID, int index)
        {
            List<Grant> grants = (List<Grant>)HttpContext.Current.Session["Grants"];
            var grant = (from g in grants where g.ID == ID select g).SingleOrDefault();
            List<Grant> selGrants = null;
            if (HttpContext.Current.Session["SelectedGrants"] != null)
            {
                selGrants = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
            }
            else
            {
                selGrants = new List<Grant>();
            }
            if (selGrants.Count < 1)
            {
                for (int ix = 0; ix < 6; ix++)
                {
                    Grant g = new Grant();
                    g.ID = -1;
                    selGrants.Add(g);
                }
            }
            Grant gg = selGrants[index];                
            var grt = (from gt in grants where gt.ID == ID select gt).SingleOrDefault();
            gg.category = grt.category;
            gg.ID = grt.ID;
            gg.grantTitle = grt.grantTitle;
            gg.grantManagerFirst = grt.grantManagerFirst;
            gg.grantManagerLast = grt.grantManagerLast;
            gg.grantNumber = grt.grantNumber;
            gg.stateCatalogNum = grt.stateCatalogNum;                
            
            HttpContext.Current.Session["SelectedGrants"] = selGrants;
            return selGrants.ToArray();
        }
        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static TimeEntry[] GetEmployeeTimeEntries(Employee emp, int month, int year)
        {            
            if (emp != null)
            {
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                OleDbCommand comm = new OleDbCommand();
                comm.Connection = conn;
                comm.CommandType = CommandType.Text;
                comm.CommandText = "select * from TimeEntry where EmpID=" + emp.ID.ToString() + " and MonthNumber=" + month.ToString() + " and YearNumber=" + year.ToString() + " order by GrantID";
                OleDbDataAdapter adapter = new OleDbDataAdapter();
                adapter.SelectCommand = comm;
                DataSet set = new DataSet();
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return null;
                }
                List<TimeEntry> timeEntries = new List<TimeEntry>();               
                List<Grant> grants = (List<Grant>)HttpContext.Current.Session["Grants"];
                List<Grant> selGrants = resetGrants();
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {
                    int selID = -1;
                    foreach (DataRow dr in set.Tables[0].Rows)
                    {
                        TimeEntry te = new TimeEntry();
                        te.ID = (int)dr[0];
                        te.monthNumber = (int)dr[1];
                        te.dayNumber = (int)dr[2];
                        te.yearNumber = (int)dr[3];
                        te.grantID = (int)dr[4];
                        te.grantHours = (double)dr[5];
                        te.empID = (int)dr[6];
                        if (dr[7] != DBNull.Value)
                        {
                            te.supervisorID = (int)dr[7];
                        }
                        timeEntries.Add(te);
                        if (selID != te.grantID)
                        {
                            var g = (from grant in grants where grant.ID == te.grantID select grant).ToList().First();
                            if (!selGrants.Contains(g))
                            {                               
                                var yy = selGrants.FindIndex(grt => grt.category == "PlaceHolder");
                                selGrants[yy] = g;                                                                                             
                            }
                            
                            selID = te.grantID;
                        }
                    }                   
                }
                HttpContext.Current.Session["SelectedGrants"] = selGrants;
                HttpContext.Current.Session["TimeEntries"] = timeEntries;
                GrantMonth.status stat = checkStatus(emp, month, year, conn);
                HttpContext.Current.Session["GrantStatus"] = stat;
                conn.Close();
                return timeEntries.ToArray();
            }
            return null;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static GrantMonth.status getMonthStatus()
        {
            if (HttpContext.Current.Session["GrantStatus"] == null)
            {
                return GrantMonth.status.New;
            }
            return (GrantMonth.status)HttpContext.Current.Session["GrantStatus"];
        }
        /**************************************************************************************************
         * First we check to see if any of the grants for this employee, for this month are pending
         * because those have to be completed yet.
         * Then we check for any disapprovals becuase those have to e dealt with.
         * Finally we see if there are any approvals.  If so, we send back approved.
         * **********************************************************************************************/
        public static GrantMonth.status checkStatus(Employee emp, int month, int year, OleDbConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            string select = "select * from WorkMonth where EmpID=" + emp.ID.ToString() + " and WorkingMonth=" + month.ToString() + " and WorkYear=" + year.ToString();
            OleDbCommand comm = new OleDbCommand(select, conn);
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return GrantMonth.status.New;
            }
            if (set.Tables == null || set.Tables.Count < 1 || set.Tables[0].Rows.Count < 1)
            {
                conn.Close();
                return GrantMonth.status.New;
            }
            try
            {
                var bag = (from row in set.Tables[0].AsEnumerable() where row.Field<int>("Status") == 1 select row).ToList(); //Pending
                if (bag != null && bag.Count > 0)
                {                    
                    return GrantMonth.status.pending;
                }

                bag = (from row in set.Tables[0].AsEnumerable() where row.Field<int>("Status") == 2 select row).ToList(); //Disapproved
                if (bag != null && bag.Count > 0)
                {                    
                    return GrantMonth.status.disapproved;
                }
                
                bag = (from row in set.Tables[0].AsEnumerable() where row.Field<int>("Status") == 3 select row).ToList(); //Approved
                if (bag != null && bag.Count > 0)
                {                    
                    return GrantMonth.status.approved;
                }                                
            }
            catch (System.Exception ex)
            {
                int ii = 0;
            }            
            return GrantMonth.status.New;
        }
        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static TimeEntry[] GetEmployeeTimeEnties()
        {
            List<Employee> emps = (List<Employee>)HttpContext.Current.Session["CurrentEmployeeList"];
            int month = System.Convert.ToInt32(HttpContext.Current.Session["month"]);            
            int year = System.Convert.ToInt32(HttpContext.Current.Session["Year"]);
            List<Grant> selGrants = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
            int grantID = selGrants.Where(g => g.ID != 52 && g.ID != 53).SingleOrDefault().ID;
            List<TimeEntry> teS = GetEmployeeTimeEntriesForApproval(emps[0], month, year, grantID);
            return teS.ToArray();
        }

        public static List<TimeEntry> GetEmployeeTimeEntriesForApproval(Employee emp, int month, int year, int grantID)
        {
            if (emp != null)
            {
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                OleDbCommand comm = new OleDbCommand();
                comm.Connection = conn;
                comm.CommandType = CommandType.Text;
               // comm.CommandText = "select * from TimeEntry where EmpID=" + emp.ID.ToString() + " and MonthNumber=" + month.ToString() + 
                //        " and (GrantID=" + grantID.ToString() + " or 52 or 53)";
                comm.CommandText = "SELECT * FROM TimeEntry WHERE (((TimeEntry.GrantID) In (" + grantID.ToString() + ",52,53)) AND ((TimeEntry.EmpID)=" + emp.ID.ToString();
                comm.CommandText += ") AND ((TimeEntry.MonthNumber)=" + month.ToString() + ")";
                comm.CommandText += "  AND ((TimeEntry.YearNumber)=" + year.ToString() + "));";

                OleDbDataAdapter adapter = new OleDbDataAdapter();
                adapter.SelectCommand = comm;
                DataSet set = new DataSet();
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return null;
                }
                List<TimeEntry> timeEntries = new List<TimeEntry>();
                List<Grant> selGrants = new List<Grant>();
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {                    
                    foreach (DataRow dr in set.Tables[0].Rows)
                    {
                        TimeEntry te = new TimeEntry();
                        te.ID = (int)dr[0];
                        te.monthNumber = (int)dr[1];
                        te.dayNumber = (int)dr[2];
                        te.yearNumber = (int)dr[3];
                        te.grantID = (int)dr[4];
                        te.grantHours = (double)dr[5];
                        te.empID = (int)dr[6];
                        if (dr[7] != DBNull.Value)
                        {
                            te.supervisorID = (int)dr[7];
                        }
                        timeEntries.Add(te);                                                                                                           
                     }
                 }
                HttpContext.Current.Session["TimeEntries"] = timeEntries;
                GrantMonth.status stat = checkStatus(emp, month, year, conn);
                HttpContext.Current.Session["GrantStatus"] = stat;
                conn.Close();
                return timeEntries;
                }                
                return null;
            }                    

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static Grant[] GetSelectedGrants()
        {
            if (HttpContext.Current.Session["SelectedGrants"] != null)
            {
                List<Grant> selGrants = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
                return selGrants.ToArray();
            }
            return null;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static TimeEntry[] AssignSupervisor(int supID, int grantID, int empID)
        {
            if (HttpContext.Current.Session["TimeEntries"] != null)
            {
                List<TimeEntry> teS = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
                var theOnes = teS.Where(te => te.empID == empID && te.grantID == grantID).ToList();
                if (theOnes != null)
                {
                    OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                    conn.Open();
                    OleDbCommand comm = new OleDbCommand();
                    comm.Connection = conn;
                    comm.CommandType = CommandType.Text;
                     
                    foreach (TimeEntry t in theOnes)
                    {
                        t.supervisorID = supID;
                        comm.CommandText = "update TimeEntry set SupervisorID=" + supID.ToString() + " where ID=" + t.ID.ToString();
                        comm.ExecuteNonQuery();
                    }
                    HttpContext.Current.Session["TimeEntries"] = teS;
                    conn.Close();
                }
                
                return teS.ToArray();
            }
            return null;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static WeeklyGrantHours[] GetWeeklyGrantHours()
        {
            string[] daysOfWeek = { "Placeholder", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            List<Grant> allGrants = (List<Grant>)HttpContext.Current.Session["Grants"];
            List<string> daysList = daysOfWeek.ToList();
            if (HttpContext.Current.Session["TimeEntries"] == null)
            {
                return null;
            }
            List<TimeEntry> timers = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
            if (timers == null || timers.Count < 1)
            {
                return null;
            }
            List<int> GrantIDs = (from tim in timers select tim.grantID).Distinct().ToList();
            List<int> weeklyMaxes = new List<int>();
            DateTime t = new DateTime(timers[0].yearNumber, timers[0].monthNumber + 1, 1); //Have to add 1 to our zero-indexed month number to yield the correct Date month.
            string dow = t.DayOfWeek.ToString();
            int ix = daysList.IndexOf(dow);
            ix = 8 - ix; //To get the number of days until the first Sunday of the month.
            for (int bb = ix; bb <= totalDaysInMonth[t.Month - 1]; bb += 7)
            {
                weeklyMaxes.Add(bb);
            }
            if (weeklyMaxes[weeklyMaxes.Count - 1] < totalDaysInMonth[t.Month - 1])
            {
                weeklyMaxes.Add(totalDaysInMonth[t.Month - 1]);
            }
            List<WeeklyGrantHours> weeklyHours = new List<WeeklyGrantHours>();
            for (int ax = 0; ax < GrantIDs.Count; ax++)
            {
                for (int xx = 0; xx < weeklyMaxes.Count; xx++)
                {
                    double totHrs = 0.0;
                    if (xx == 0)
                    {
                        totHrs = (from te in timers where te.dayNumber <= weeklyMaxes[xx] && te.grantID == GrantIDs[ax] select te.grantHours).Sum();
                    }
                    else
                    {
                        totHrs = (from te in timers where te.dayNumber <= weeklyMaxes[xx] && te.dayNumber > weeklyMaxes[xx - 1] && te.grantID == GrantIDs[ax] select te.grantHours).Sum();
                    }
                    WeeklyGrantHours wgh = new WeeklyGrantHours();
                    wgh.grantID = GrantIDs[ax];
                    wgh.grantName = (from g in allGrants where g.ID == wgh.grantID select g.grantTitle).ToList().First();
                    wgh.monthNumber = t.Month;
                    wgh.weeklyHours = totHrs;
                    wgh.weekNumber = xx;
                    wgh.yearNumber = t.Year;
                    weeklyHours.Add(wgh);

                }
            }
            HttpContext.Current.Session["WeeklyGrantHours"] = weeklyHours;
            return weeklyHours.ToArray();
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static WeeklyGrantHours[] GetWeeklyGrantHoursForApproval()
        {
            string[] daysOfWeek = { "Placeholder", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            List<Grant> allGrants = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
            List<string> daysList = daysOfWeek.ToList();
            if (HttpContext.Current.Session["TimeEntries"] == null)
            {
                return null;
            }
            List<TimeEntry> timers = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
            if (timers == null || timers.Count < 1)
            {
                return null;
            }
            List<int> GrantIDs = (from tim in timers select tim.grantID).Distinct().ToList();
            List<int> weeklyMaxes = new List<int>();
            DateTime t = new DateTime(timers[0].yearNumber, timers[0].monthNumber + 1, 1); //Have to add 1 to our zero-indexed month number to yield the correct Date month.
            string dow = t.DayOfWeek.ToString();
            int ix = daysList.IndexOf(dow);
            ix = 8 - ix; //To get the number of days until the first Sunday of the month.
            for (int bb = ix; bb <= totalDaysInMonth[t.Month]; bb += 7)
            {
                weeklyMaxes.Add(bb);
            }
            List<WeeklyGrantHours> weeklyHours = new List<WeeklyGrantHours>();
            for (int ax = 0; ax < GrantIDs.Count; ax++)
            {
                for (int xx = 0; xx < weeklyMaxes.Count; xx++)
                {
                    double totHrs = 0.0;
                    if (xx == 0)
                    {
                        totHrs = (from te in timers where te.dayNumber <= weeklyMaxes[xx] && te.grantID == GrantIDs[ax] select te.grantHours).Sum();
                    }
                    else
                    {
                        totHrs = (from te in timers where te.dayNumber <= weeklyMaxes[xx] && te.dayNumber > weeklyMaxes[xx - 1] && te.grantID == GrantIDs[ax] select te.grantHours).Sum();
                    }
                    WeeklyGrantHours wgh = new WeeklyGrantHours();
                    wgh.grantID = GrantIDs[ax];
                    wgh.grantName = (from g in allGrants where g.ID == wgh.grantID select g.grantTitle).ToList().First();
                    wgh.monthNumber = t.Month;
                    wgh.weeklyHours = totHrs;
                    wgh.weekNumber = xx;
                    wgh.yearNumber = t.Year;
                    weeklyHours.Add(wgh);

                }
            }
            HttpContext.Current.Session["WeeklyGrantHours"] = weeklyHours;
            return weeklyHours.ToArray();
        }


        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static double updateDailyGrantHours(string cellID, int grantID, Employee emp, string hours)
        {            
            int month = System.Convert.ToInt32(cellID.Substring(0, 2));
            month--;
            int day = System.Convert.ToInt32(cellID.Substring(2, 2));
            
            List<Grant> grants = (List<Grant>)HttpContext.Current.Session["Grants"];
            var g = (from gt in grants where gt.ID == grantID select gt).ToList().First();

            List<TimeEntry> timeEntries = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
            if (emp != null)
            {
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                conn.Open();
                OleDbCommand comm = new OleDbCommand();
                comm.Connection = conn;
                comm.CommandType = CommandType.Text;
                comm.CommandText = "select * from TimeEntry where EmpID=" + emp.ID.ToString() + " and MonthNumber=" + month.ToString() + " and DayNumber=" + day.ToString() + " and GrantID=" + g.ID.ToString();
                OleDbDataAdapter adapter = new OleDbDataAdapter();
                adapter.SelectCommand = comm;
                DataSet set = new DataSet();
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return 0;
                }
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {
                    var te = (from tim in timeEntries where tim.empID == emp.ID && tim.grantID == g.ID && tim.dayNumber == day select tim).SingleOrDefault();
                    if (te != null)
                    {
                        conn.Close();
                        te.grantHours = System.Convert.ToDouble(hours);
                        bool b = updateTimeEntry(te, comm, conn);
                        conn.Close();
                        return timeEntries.Sum(s => s.grantHours);
                        //return timeEntries;
                    }

                }
                else
                {
                    TimeEntry te = new TimeEntry();
                    te.grantHours = System.Convert.ToDouble(hours);
                    te.grantID = g.ID;
                    te.empID = emp.ID;
                    te.monthNumber = month;
                    te.dayNumber = day;
                    te.yearNumber = System.Convert.ToInt32(cellID.Substring(4, 4));
                    conn.Close();
                    addNewTimeEntry(te, comm, conn);
                    timeEntries.Add(te);
                    conn.Close();
                    HttpContext.Current.Session["TimeEntries"] = timeEntries;
                }
                
            }
            return timeEntries.Sum(te=>te.grantHours);
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static double updateDailyNonGrantHours(string cellID, Employee emp, string hours)
        {
            int month = System.Convert.ToInt32(cellID.Substring(0, 2));
            month--;
            int day = System.Convert.ToInt32(cellID.Substring(2, 2));           

            List<TimeEntry> timeEntries = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
            if (emp != null)
            {
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                conn.Open();
                OleDbCommand comm = new OleDbCommand();
                comm.Connection = conn;
                comm.CommandType = CommandType.Text;
                comm.CommandText = "select * from TimeEntry where EmpID=" + emp.ID.ToString() + " and MonthNumber=" + month.ToString() + " and DayNumber=" + day.ToString() + " and GrantID=52";
                OleDbDataAdapter adapter = new OleDbDataAdapter();
                adapter.SelectCommand = comm;
                DataSet set = new DataSet();
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return 0;
                }
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {
                    var te = (from tim in timeEntries where tim.empID == emp.ID && tim.grantID == 52 && tim.dayNumber == day select tim).SingleOrDefault();
                    if (te != null)
                    {
                        conn.Close();
                        te.grantHours = System.Convert.ToDouble(hours);
                        bool b = updateTimeEntry(te, comm, conn);
                        conn.Close();
                        return timeEntries.Sum(t=>t.grantHours);
                    }

                }
                else
                {
                    TimeEntry te = new TimeEntry();
                    te.grantHours = System.Convert.ToDouble(hours);
                    te.grantID = 52;  //ID for non grant
                    te.empID = emp.ID;
                    te.monthNumber = month;
                    te.dayNumber = day;
                    te.yearNumber = System.Convert.ToInt32(cellID.Substring(4, 4));
                    conn.Close();
                    addNewTimeEntry(te, comm, conn);
                    conn.Close();
                    timeEntries.Add(te);
                    HttpContext.Current.Session["TimeEntries"] = timeEntries;
                }

            }
            return timeEntries.Sum(t=>t.grantHours);
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static double updateDailyLeaveHours(string cellID, Employee emp, string hours)
        {
            int month = System.Convert.ToInt32(cellID.Substring(0, 2));
            month--;
            int day = System.Convert.ToInt32(cellID.Substring(2, 2));

            List<TimeEntry> timeEntries = (List<TimeEntry>)HttpContext.Current.Session["TimeEntries"];
            if (emp != null)
            {
                OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
                conn.Open();
                OleDbCommand comm = new OleDbCommand();
                comm.Connection = conn;
                comm.CommandType = CommandType.Text;
                comm.CommandText = "select * from TimeEntry where EmpID=" + emp.ID.ToString() + " and MonthNumber=" + month.ToString() + " and DayNumber=" + day.ToString() + " and GrantID=53";
                OleDbDataAdapter adapter = new OleDbDataAdapter();
                adapter.SelectCommand = comm;
                DataSet set = new DataSet();
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return 0;
                }
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {
                    var te = (from tim in timeEntries where tim.empID == emp.ID && tim.grantID == 53 && tim.dayNumber == day select tim).SingleOrDefault();
                    if (te != null)
                    {
                        conn.Close();
                        te.grantHours = System.Convert.ToDouble(hours);
                        bool b = updateTimeEntry(te, comm, conn);
                        conn.Close();
                        return timeEntries.Sum(t=>t.grantHours);
                    }

                }
                else
                {
                    TimeEntry te = new TimeEntry();
                    te.grantHours = System.Convert.ToDouble(hours);
                    te.grantID = 53;  //ID for non grant
                    te.empID = emp.ID;
                    te.monthNumber = month;
                    te.dayNumber = day;
                    te.yearNumber = System.Convert.ToInt32(cellID.Substring(4, 4));
                    conn.Close();
                    addNewTimeEntry(te, comm, conn);
                    timeEntries.Add(te);
                    HttpContext.Current.Session["TimeEntries"] = timeEntries;
                }

            }
            return timeEntries.Sum(t=>t.grantHours);
        }

        public static bool updateTimeEntry(TimeEntry te, OleDbCommand comm, OleDbConnection conn)
        {
            conn.Open();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            comm.CommandText = "update TimeEntry set GrantHours=" + te.grantHours.ToString() + " where ID=" + te.ID.ToString();
            comm.Connection = conn;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                return false;
            }

            return true;
        }

        public static bool addNewTimeEntry(TimeEntry te, OleDbCommand comm, OleDbConnection conn)
        {
            conn.Open();
            comm.Connection = conn;
            comm.CommandType = CommandType.Text;
            comm.CommandText = "insert into TimeEntry (MonthNumber, DayNumber, YearNumber, GrantID, GrantHours, EmpID) values (";
            comm.CommandText += te.monthNumber.ToString() + "," + te.dayNumber.ToString() + "," + te.yearNumber.ToString() + ",";
            comm.CommandText += te.grantID.ToString() + "," + te.grantHours.ToString() + "," + te.empID.ToString() + ");";
            comm.Connection = conn;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                return false;
            }
            
            return true;
        }
        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static Employee getTheCurrentEmp(int id)
        {
            if (HttpContext.Current.Session["CurrentEmployeeList"] == null)
            {
                return null;
            }
            List<Employee> emps = (List<Employee>)HttpContext.Current.Session["CurrentEmployeeList"];
            var e = (from emp in emps where emp.ID == id select emp).ToList().FirstOrDefault();
            return e;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static bool sendOffEmail(int[] supIDs, int empID, DateTime selDate)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            List<Grant> selGrants = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
            List<Employee> emps = (List<Employee>)HttpContext.Current.Session["CurrentEmployeeList"];
            var emp = emps.Where(e => e.ID == empID).SingleOrDefault();
            List<Employee> sups = new List<Employee>();

            for (int ix = 0; ix < supIDs.Count(); ix++)
            {
                Employee sup = getSupervisor(supIDs[ix], conn);
                sups.Add(sup);
                if (selGrants[ix].ID != 28 && selGrants[ix].ID != 52 && selGrants[ix].ID != 53)
                {                   
                    updateGrantStatus(sup, conn, emp, selDate, selGrants[ix].ID, GrantMonth.status.pending);
                }
            }
            if (conn.State != ConnectionState.Closed)
            {
                conn.Close();
            }

            MailObject mO = new MailObject();
            mO.grants = selGrants;
            mO.emp = emp;
            mO.selDate = selDate;
            mO.supervisors = sups;
            
            Thread t = new Thread(emailThread);
            t.Start(mO);

            //for (int xx = 0; xx < sups.Count(); xx++)
            //{
            //    Employee sup = sups[xx];
            //    MailMessage mailObj = new MailMessage("GrantAllocations@mid-state.net", sup.emailAddress, "Grant Approval Required",
            //                   formulateEmailBody(sup, emp, selDate, selGrants[xx].ID));
            //    mailObj.IsBodyHtml = true;
            //    SmtpClient SMTPServer = new SmtpClient("localhost"); //outlook.college.mstc.tech

            //    SMTPServer.Send(mailObj);
            //}
                            
            return true;
        }

        public static void emailThread(object emailObj)
        {
            MailObject mO = (MailObject)emailObj;
            for (int xx = 0; xx < mO.supervisors.Count; xx++)
            {
                if (mO.grants[xx].ID != 28 && mO.grants[xx].ID != 52 && mO.grants[xx].ID != 53)  //have to filter out non-grant and leave time from the request.  03/03/2013
                {
                    Employee sup = mO.supervisors[xx];
                    MailMessage mailObj = new MailMessage("GrantAllocations@mid-state.net", sup.emailAddress, "Grant Approval Required",
                                   formulateEmailBody(sup, mO.emp, mO.selDate, mO.grants[xx].ID));
                    mailObj.IsBodyHtml = true;
                    SmtpClient SMTPServer = new SmtpClient("localhost"); //outlook.college.mstc.tech
                    SMTPServer.ServicePoint.MaxIdleTime = 1;
                    // SMTPServer.Send(mailObj);
                    SMTPServer.SendAsync(mailObj, null);
                }
            }
        }

        public static void emailGMThread(object emailObj)
        {
            MailObject mO = (MailObject)emailObj;
            for (int xx = 0; xx < mO.supervisors.Count; xx++)
            {
                Employee sup = mO.supervisors[xx];
                MailMessage mailObj = new MailMessage("GrantAllocations@mid-state.net", sup.emailAddress, "Final Grant Approval Review", mO.bodyTxt);
                mailObj.IsBodyHtml = true;
                SmtpClient SMTPServer = new SmtpClient("localhost"); //outlook.college.mstc.tech
                SMTPServer.ServicePoint.MaxIdleTime = 1;
                SMTPServer.Send(mailObj);
                //SMTPServer.SendAsync(mailObj, null);
            }
        }

        public static void emailerBaby()
        {
            MailMessage mailObj = new MailMessage("GrantAllocations@mid-state.net", "stilsons@hotmail.com", "Grant Approval Required",
                               "Let's do some testing!");
            mailObj.IsBodyHtml = true;
            SmtpClient SMTPServer = new SmtpClient("localhost"); //outlook.college.mstc.tech
            SMTPServer.Send(mailObj);
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static bool sendApproveOrDisapproveEmail(string reasonTxt, bool approved)
        {
            List<Employee> emps = (List<Employee>)HttpContext.Current.Session["CurrentEmployeeList"];
            Employee sup = (Employee)HttpContext.Current.Session["Supervisor"];
            List<Employee> sups = new List<Employee>();
            sups.Add(sup);

            MailObject mO = new MailObject();
            mO.supervisors = sups;
            mO.emp = emps[0];
            mO.approved = approved;
            mO.reason = reasonTxt;
            mO.month = System.Convert.ToInt32(HttpContext.Current.Session["month"]);
            mO.year = System.Convert.ToInt32(HttpContext.Current.Session["Year"]);

            Thread t = new Thread(approvalThread);
            t.Start(mO);
            //SMTPServer.Send(mailObj);
            if (approved)
            {
                sendApprovalEmailToGM(mO);
            }            
            return true;
        }

        public static void approvalThread(object mailObj)
        {
            MailObject mO = (MailObject)mailObj;
            string subject = (mO.approved) ? "Grant hours approved!" : "Grant hours disapproved.";
            MailMessage mailMess = new MailMessage("GrantAllocations@mid-state.net", mO.emp.emailAddress, subject, formulateResultsEmail(mO));
            mailMess.IsBodyHtml = true;
            SmtpClient SMTPServer = new SmtpClient("localhost"); //outlook.college.mstc.tech
            SMTPServer.ServicePoint.MaxIdleTime = 1;
            try
            {
                SMTPServer.Send(mailMess);
            }
            catch (System.Exception ex)
            {
                return;
            }
        }

        public static string formulateResultsEmail(string reasonTxt, Employee emp, Employee sup, bool approved)
        {
            int month = System.Convert.ToInt32(HttpContext.Current.Session["month"]);
            int year = System.Convert.ToInt32(HttpContext.Current.Session["Year"]);

            DateTime dater = new DateTime(year, month + 1, 1); //Again, have to convert from Javascript date to .Net date.

            string bodyTxt = "<html><body><strong>Hi " + emp.firstName + ".</strong><p />";
            bodyTxt += "This email is to inform you " + sup.firstName + " " + sup.lastName + " has " + ((approved) ? "approved " : "disapproved ");
            bodyTxt += "<br />your grant entries for the month of " + dater.ToString("MMMM") + " for the following reason: <br />";
            bodyTxt += reasonTxt + "<p />";
            if (!approved)
            {
                bodyTxt += "Please take appropriate action to corrent this matter.<br />";
            }
            bodyTxt += "Thanks for using the Grant Allocation form! </body></html>";

            return bodyTxt;
        }

        public static string formulateResultsEmail(MailObject mO)
        {
            
            DateTime dater = new DateTime(mO.year, mO.month + 1, 1); //Again, have to convert from Javascript date to .Net date.

            string bodyTxt = "<html><body><strong>Hi " + mO.emp.firstName + ".</strong><p />";
            bodyTxt += "This email is to inform you " + mO.supervisors[0].firstName + " " + mO.supervisors[0].lastName + " has " + ((mO.approved) ? "approved " : "disapproved ");
            bodyTxt += "<br />your grant entries for the month of " + dater.ToString("MMMM") + " for the following reason: <br />";
            bodyTxt += mO.reason + "<p />";
            if (!mO.approved)
            {
                bodyTxt += "Please take appropriate action to corrent this matter.<br />";
            }
            bodyTxt += "Thanks for using the Grant Allocation form! </body></html>";

            return bodyTxt;
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static bool updateGrantStatus(Employee sup, OleDbConnection conn, Employee emp, DateTime selDate, int grantID, GrantMonth.status stat)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            int istat = System.Convert.ToInt32(stat);
            string select = "select * from WorkMonth where EmpID=" + emp.ID.ToString() + " and WorkingMonth=" + (selDate.Month - 1).ToString() + 
                        " and WorkYear=" + selDate.Year.ToString() + " and GrantID=" + grantID.ToString();
            string insert = "insert into WorkMonth (EmpID, WorkingMonth, WorkYear, GrantID, SupervisorID, Status) ";
            insert += "values(" + emp.ID.ToString() + "," + (selDate.Month - 1).ToString() + "," + selDate.Year.ToString() + "," + grantID.ToString() + "," + sup.ID.ToString() + ",1);";

            string update = "update WorkMonth set status=" + istat.ToString() + " where EmpID=" + emp.ID.ToString() + " and WorkingMonth=" + (selDate.Month - 1).ToString() + 
                    " and WorkYear=" + selDate.Year.ToString() + " and GrantID=" + grantID.ToString();

            OleDbCommand comm = new OleDbCommand(select, conn);
            OleDbCommand up = new OleDbCommand(update, conn);
            OleDbCommand ins = new OleDbCommand(insert, conn);

            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return false;
            }
            if (set.Tables.Count > 0 && set.Tables[0].Rows.Count > 0)
            {
                set.Reset();
                up.ExecuteNonQuery();
            }
            else
            {
                set.Reset();
                ins.ExecuteNonQuery();
            }
            
            return true;
        }

        public static Employee getSupervisor(int supID, OleDbConnection conn)
        {
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            string str = "select * from EmployeeList where ID=" + supID.ToString();
            comm.CommandText = str;
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try 
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return null;
            }
            DataRow dr = set.Tables[0].Rows[0];
            Employee sup = new Employee();
            sup.ID = (int)dr[0];
            sup.lastName = dr[2].ToString();
            sup.firstName = dr[3].ToString();
            sup.emailAddress = dr[5].ToString();
            conn.Close();

            return sup;
            
        }
        public static string formulateEmailBody(Employee sup, Employee emp, DateTime dt, int grantID)
        {                        
            StringBuilder sb = new StringBuilder();
            string email = "<html><body style='font-family:Arial'><h4>Hi " + sup.firstName + ".</h4><br />";
            sb.Append(email);
            email = emp.firstName + " " + emp.lastName + " has submitted a grant time entry for your approval. <br />";
            sb.Append(email);
            email = "To see the grant time entries, please browse to: <p />";
            sb.Append(email);
            email = "http://www.mid-state.net/GrantApplication/Default.aspx?approval=true&ID=" + sup.ID.ToString() + "&Employee=" + emp.ID.ToString() + "&month=" + (dt.Month - 1).ToString() + "&Year=" + dt.Year.ToString() + "&GrantID=" + grantID.ToString();
            sb.Append(email);
            sb.Append("<p /><h5>Thanks, the Grant Administrator</h5></body></html>");

            return sb.ToString();
        }

        [System.Web.Services.WebMethod(true)]
        [System.Web.Script.Services.ScriptMethod]
        public static bool approveOrDisapprove(bool approved, string reason)
        {
            List<Employee> emps = (List<Employee>)HttpContext.Current.Session["CurrentEmployeeList"];
            int month = (int)HttpContext.Current.Session["month"];
            int year = (int)HttpContext.Current.Session["Year"];
            List<Grant> gS = (List<Grant>)HttpContext.Current.Session["SelectedGrants"];
            List<Grant> gG = gS.Where(g => g.ID != 52 && g.ID != 53).ToList(); //Have to eliminate non-grant and leave time from the list.
            GrantMonth.status stat = (approved) ? GrantMonth.status.approved : GrantMonth.status.disapproved;
            int istat = System.Convert.ToInt32(stat);
            string update = "update WorkMonth set status=" + istat.ToString() + " where EmpID=" + emps[0].ID.ToString() + " and WorkingMonth=" + month.ToString() +
                    " and WorkYear=" + year.ToString() + " and GrantID=" + gG[0].ID.ToString();

            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            OleDbCommand comm = new OleDbCommand(update, conn);
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                return false;
            }
            conn.Close();
            return sendApproveOrDisapproveEmail(reason, approved);            
        }
        public static bool sendMarieApprovalEmail(int month, int year, Employee emp, int grantID)
        {           
           
            return true;
        }

        //This occurs once the month is approved for this grant.
        public static bool sendApprovalEmailToGM(MailObject mo)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            List<GrantMonth> gMs = getTheFinalApproved(mo, conn);
            List<Grant> gS = getGrantsFromGrantMonths(gMs, conn);
            OleDbCommand comm = new OleDbCommand();
            comm.Connection = conn;
            OleDbDataAdapter adapter = new OleDbDataAdapter();            
            DataSet set = new DataSet();
            List<Employee> emps = new List<Employee>();            
            foreach (Grant gT in gS)
            {
                string strComm = "select * from EmployeeList where LastName Like '" + gT.grantManagerLast + "%' and FirstName Like '" + gT.grantManagerFirst + "%'";                 
                comm.CommandText = strComm;
                adapter.SelectCommand = comm;
                try
                {
                    adapter.Fill(set);
                }
                catch (System.Exception ex)
                {
                    return false;
                }
                if (set.Tables != null && set.Tables[0].Rows.Count > 0)
                {
                    Employee ee = populateEmployee(set.Tables[0].Rows[0]);
                    emps.Add(ee);
                    MailObject moGM = new MailObject();
                    moGM.approved = true;
                    moGM.emp = mo.emp;
                    moGM.grants = new List<Grant>() { gT };
                    moGM.month = mo.month;
                    moGM.year = mo.year;
                    moGM.supervisors = new List<Employee>() { ee };                                        
                    moGM.bodyTxt =  formulateApprovalForGM(mo.month, mo.year, mo.emp, gT, ee);
                    Thread t = new Thread(emailGMThread);
                    t.Start(moGM);
                }                                
            }                                    
            return true;
        }

        public static List<Grant> getGrantsFromGrantMonths(List<GrantMonth> gMs, OleDbConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            var IDs = (from gm in gMs select gm.grantID.ToString() + ",").ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from GrantInfo where ID in (");
            foreach (string sID in IDs)
            {
                sb.Append(sID);
            }
            string sIDs = sb.ToString();
            sIDs = sIDs.Substring(0, sIDs.Length - 1);
            sIDs += ")";
            OleDbCommand comm = new OleDbCommand(sIDs);
            comm.Connection = conn;
            DataSet set = new DataSet();
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return null;
            }
            if (set.Tables == null || set.Tables.Count < 1)
            {
                return null;
            }
            List<Grant> gS = new List<Grant>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Grant g = populateGrant(dr);
                gS.Add(g);
            }
            return gS;
        }
        /// <summary>
        /// This checks to determine if ALL grants for the given month have been approved.
        /// </summary>
        /// <param name="mo"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static List<GrantMonth> getTheFinalApproved(MailObject mo, OleDbConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            string str = "select * from WorkMonth where EmpID=" + mo.emp.ID.ToString() + " and WorkingMonth=" + mo.month.ToString();
            str += " and WorkYear=" + mo.year.ToString();
            OleDbCommand comm = new OleDbCommand(str, conn);
            OleDbDataAdapter adapter = new OleDbDataAdapter(comm);
            DataSet set = new DataSet();
            try
            {
                adapter.Fill(set);
            }
            catch (System.Exception e)
            {
                return null;
            }
            if (set.Tables == null || set.Tables.Count < 1)
            {
                return null;
            }
            List<GrantMonth> grants = new List<GrantMonth>();
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                GrantMonth gm = new GrantMonth(dr);
                grants.Add(gm);
            }
            var sloopG = grants.Where(g => g.curStatus == System.Convert.ToInt32(GrantMonth.status.approved)).ToList();
            if (sloopG != null && sloopG.Count > 0)
            {
                conn.Close();
                return sloopG;
            }
            return null;        
        }

        public static string formulateApprovalForGM(int month, int year, Employee emp, Grant g, Employee manager)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><body><strong>Hi " + manager.firstName + "</strong><p />");
            sb.Append(emp.firstName + " " + emp.lastName + " has been approved for the " + g.grantTitle + " grant.<br />");
            sb.Append("To see the grant entries for " + emp.firstName + " please click on the following link:<br /><a href=");
            sb.Append("http://www.mid-state.net/GrantApplication/Default.aspx?Review=true&ID=" + manager.ID.ToString() + "&Employee=" + emp.ID.ToString());
            sb.Append( "&month=" + month.ToString() + "&Year=" + year.ToString() + "&GrantID=" +g.ID.ToString());
            sb.Append(">Review Time Enties</a><p />Thank you,<br />");
            sb.Append("<strong>The grant allocations team.</strong></body></html>");

            return sb.ToString();

        }
        private static Grant populateGrant(DataRow dr)
        {
            Grant g = new Grant();
            g.ID = (int)dr[0];
            g.stateCatalogNum = dr[1].ToString();
            g.category = dr[2].ToString();
            g.grantNumber = dr[3].ToString();
            g.grantTitle = dr[4].ToString();
            g.grantManagerLast = dr[5].ToString();
            g.grantManagerFirst = dr[6].ToString();

            return g;
        }

        private static Employee populateEmployee(DataRow dr)
        {
            Employee e = new Employee();
            e.ID = (int)dr[0];
            e.EmpNum = dr[1].ToString();
            e.lastName = dr[2].ToString();
            e.firstName = dr[3].ToString();
            e.jobTitle = dr[4].ToString();
            e.emailAddress = dr[5].ToString();
            e.registered = (bool)dr[7];
            e.manager = (bool)dr[8];

            return e;
        }        
        
    }
}
