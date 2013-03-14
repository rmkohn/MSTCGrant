using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.OleDb;
using System.Configuration;

namespace GrantApplication
{
    public partial class Maintenance : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            List<Employee> mans = getAllManagers();
            Session["Managers"] = mans;
            if ( mans != null) {
                  foreach (Employee man in mans) {
                      ListItem li = new ListItem(man.ToString(), man.ID.ToString());
                      ddlDefSup.Items.Add(li);
                  }
            }           
        }

        protected void GoEMployee(object sender, ImageClickEventArgs e)
        {
            pnlEmp.Visible = true;
        }

        protected void OnSubmit(object sender, EventArgs e)
        {
            
            //Do something meaningful at this point..
            Employee emp = GatherUpTheFields();
            if (emp != null)
            {
                if (Session["CurrentEmployee"] != null)
                {
                    Employee curEmp = (Employee)Session["CurrentEmployee"];
                    if (!areEqual(curEmp, emp))
                    {
                        emp.ID = curEmp.ID;
                        updateDatabase(emp);
                    }
                }
                else //It's a new employee
                {
                    AddNewEmployee(emp);
                }
            }
        }
        protected bool updateDatabase(Employee emp)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string scomm = "update EmployeeList set EmployeeNum=" + emp.EmpNum + ", LastName='" + emp.lastName + "', ";
            scomm += " FirstName='" + emp.firstName + "', JobTitle='" + emp.jobTitle + "', EmailAddress='" + emp.emailAddress + "', ";
            scomm += " registered=" + emp.registered.ToString() + ", manager=" + emp.manager.ToString();
            scomm += ", DefaultSupervisor=" + emp.defaultSupervisor + " where ID=" + emp.ID.ToString();                                    
            OleDbCommand comm = new OleDbCommand(scomm, conn);
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (System.Exception e)
            {
                return false;
            }            
            return true;
        }
        protected bool areEqual(Employee oldEmp, Employee newEmp)
        {
            if (oldEmp.defaultSupervisor != newEmp.defaultSupervisor || oldEmp.emailAddress != newEmp.emailAddress || oldEmp.EmpNum != newEmp.EmpNum ||
                oldEmp.firstName != newEmp.firstName || oldEmp.jobTitle != newEmp.jobTitle || oldEmp.lastName != newEmp.lastName || oldEmp.manager != newEmp.manager)
            {
                return false;
            }
            return true;
        }
        protected Employee GatherUpTheFields()
        {
            if (String.IsNullOrEmpty(txtEmpNum.Text) || String.IsNullOrEmpty(txtFirst.Text) || String.IsNullOrEmpty(txtLast.Text))
            {
                return null;
            }
            Employee emp = new Employee();
            emp.EmpNum = txtEmpNum.Text;
            emp.lastName = txtLast.Text;
            emp.firstName = txtFirst.Text;
            emp.jobTitle = txtTitle.Text;
            emp.emailAddress = txtEmail.Text;
            emp.manager = cbManager.Checked;
            emp.defaultSupervisor = ddlDefSup.SelectedValue;
            return emp;
        }

        protected bool AddNewEmployee(Employee newEmp)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string scomm = "insert into EmployeeList (EmployeeNUm, LastName, FirstName, JobTitle, EmailAddress, registered, manager, DefaultSupervisor) ";
            scomm += "values (" + newEmp.EmpNum + ",'" + newEmp.lastName + "','" + newEmp.firstName + "','" + newEmp.jobTitle + "','";
            scomm += newEmp.emailAddress + "'," + newEmp.registered + "," + newEmp.manager + "," + newEmp.defaultSupervisor + ")";
            OleDbCommand comm = new OleDbCommand(scomm, conn);
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
        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static string[] GetEmpsList(string prefixText, int count)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string scomm = string.Empty;
            if (prefixText.Contains(' '))
            {
                char[] chsz = { ' ' };
                string[] sbag = prefixText.Split(chsz);
                scomm = "select * from EmployeeList where FirstName like '" + sbag[0] + "%' and LastName like '" + sbag[1] + "%'";
            }
            else
            {
                scomm = "select * from EmployeeList where FirstName like '" + prefixText + "%'";
            }
            OleDbCommand comm = new OleDbCommand(scomm, conn);

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();
            try
            {
                adapter.Fill(set);
            }
            catch (Exception ex)
            {
                return null;
            }
            if (set.Tables != null && set.Tables[0].Rows.Count > 0)
            {
                DataTable tab = set.Tables[0];
                List<String> emps = new List<String>();
                foreach (DataRow r in tab.Rows)
                {
                    string s = r[3].ToString() + " " + r[2].ToString() + ", " + r[1].ToString() + ", " + r[4].ToString();
                    emps.Add(s);
                }
                return emps.ToArray();
            }
            return null;
        }

        protected void OnEmpChange(object sender, EventArgs e)
        {
            string s = txtEmps.Text;
            char[] chsz = { ',' };
            string[] sbaggers = s.Split(chsz);
        }
        protected List<Employee> getAllManagers()
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string scomm = string.Empty;
            scomm = "select * from EmployeeList where manager=true";
            OleDbCommand comm = new OleDbCommand(scomm, conn);

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();
            try
            {
                adapter.Fill(set);
            }
            catch (Exception ex)
            {
                return null;
            }
            if (set.Tables != null && set.Tables[0].Rows.Count > 0)
            {
                List<Employee> mans = new List<Employee>();
                foreach (DataRow dr in set.Tables[0].Rows)
                {
                    Employee emp = new Employee();
                    emp.ID = (int)dr[0];
                    emp.EmpNum = dr[1].ToString();
                    emp.lastName = dr[2].ToString();
                    emp.firstName = dr[3].ToString();
                    if (dr[4] != DBNull.Value)
                    {
                        emp.jobTitle = dr[4].ToString();
                    }
                    if (dr[5] != DBNull.Value)
                    {
                        emp.emailAddress = dr[5].ToString();
                    }
                    if (dr[6] != DBNull.Value)
                    {
                        emp.password = dr[6].ToString();
                    }
                    if (dr[7] != DBNull.Value)
                    {
                        emp.registered = (bool)dr[7];
                    }
                    if (dr[8] != DBNull.Value)
                    {
                        emp.manager = (bool)dr[8];
                    }
                    if (dr[9] != DBNull.Value)
                    {
                        emp.defaultSupervisor = dr[9].ToString();
                    }
                    mans.Add(emp);
                }
                return mans;              
            }
            return null;
        }

        [System.Web.Services.WebMethod(EnableSession=true)]
        [System.Web.Script.Services.ScriptMethod]
        public static Employee getSelectedEmp(string empNum, string ID = "0")
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string scomm = string.Empty;
            scomm = (ID == "0") ? "select * from EmployeeList where EmployeeNUm = " + empNum : "select * from EmployeeList where ID = " + ID;                        
            OleDbCommand comm = new OleDbCommand(scomm, conn);

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = comm;
            DataSet set = new DataSet();
            try
            {
                adapter.Fill(set);
            }
            catch (Exception ex)
            {
                return null;
            }
            if (set.Tables != null && set.Tables[0].Rows.Count > 0)
            {
                Employee emp = new Employee();
                DataRow dr = set.Tables[0].Rows[0];
                emp.ID = (int)dr[0];
                emp.EmpNum = dr[1].ToString();
                emp.lastName = dr[2].ToString();
                emp.firstName = dr[3].ToString();
                if (dr[4] != DBNull.Value)
                {
                    emp.jobTitle = dr[4].ToString();
                }
                if (dr[5] != DBNull.Value)
                {
                    emp.emailAddress = dr[5].ToString();
                }
                if (dr[6] != DBNull.Value)
                {
                    emp.password = dr[6].ToString();
                }
                if (dr[7] != DBNull.Value)
                {
                    emp.registered = (bool)dr[7];
                }
                if (dr[8] != DBNull.Value)
                {
                    emp.manager = (bool)dr[8];
                }
                if (dr[9] != DBNull.Value)
                {
                    emp.defaultSupervisor = dr[9].ToString();
                }
                if (empNum != "0") //It's calling for the default supervisor, not the employee.
                {
                    HttpContext.Current.Session["CurrentEmployee"] = emp;
                }
                return emp;
            }
            return null;
        }
        [System.Web.Services.WebMethod]
        [System.Web.Script.Services.ScriptMethod]
        public static Employee getDefaultSupervisor(string empNum)
        {
            return getSelectedEmp("0", empNum);
        }
    }
}