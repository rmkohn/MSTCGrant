using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Configuration;
using System.Data;

namespace GrantApplication.Account
{
    public partial class Register : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterUser.ContinueDestinationPageUrl = Request.QueryString["ReturnUrl"];
            if (Request.Params["empNum"] != null)
            {
                RegisterUser.UserName = Request.Params["empNum"].ToString();
            }
        }
        protected void CrankIt(object sender, EventArgs e)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string strLog = "update employeelist set EmployeeList.registered=true, EmployeeList.EmailAddress='" + RegisterUser.Email + "', EmployeeList.[UserPass]='" + RegisterUser.Password;
            strLog += "' where (EmployeeList.EmployeeNUm=" + RegisterUser.UserName + ");";
            OleDbCommand comm = new OleDbCommand();
            comm.CommandText = strLog;
            comm.CommandType = System.Data.CommandType.Text;
            comm.Connection = conn;
            Employee emp = new Employee();
            emp.EmpNum = RegisterUser.UserName;
            try
            {
                comm.ExecuteNonQuery();
                comm.CommandText = "select @@identity;";
                emp.ID = (int)comm.ExecuteScalar();
                conn.Close();
            }
            catch (System.Exception ex)
            {
                Response.Redirect("Register.aspx");
            }
            List<Employee> empList = populateEmployeeList(emp, conn, comm);
            Session["CurrentEmployeeList"] = empList;
            //FormsAuthentication.SetAuthCookie(RegisterUser.UserName, false /* createPersistentCookie */);

            string continueUrl = "./Login.aspx";
            if (String.IsNullOrEmpty(continueUrl))
            {
                continueUrl = "~/";
            }
            Response.Redirect(continueUrl);
        }
        private List<Employee> populateEmployeeList(Employee emp, OleDbConnection conn, OleDbCommand comm)
        {
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            DataSet set = new DataSet();            
            try
            {
                conn.Open();
                comm.CommandText = "select * from EmployeeList where EmployeeNum = " + emp.EmpNum;
                comm.Connection = conn;
                adapter.SelectCommand = comm;
                adapter.Fill(set);
            }
            catch (System.Exception ex)
            {
                return null;
            }
            if (set.Tables != null && set.Tables[0].Rows.Count > 0)
            {
                List<Employee> emps = new List<Employee>();
                foreach (DataRow dr in set.Tables[0].Rows)
                {
                    Employee e = new Employee();
                    e.ID = emp.ID;
                    e.emailAddress = dr[5].ToString();
                    e.EmpNum = dr[1].ToString();
                    e.firstName = dr[3].ToString();
                    e.jobTitle = dr[4].ToString();
                    e.lastName = dr[2].ToString();
                    e.registered = true;
                    e.password = dr[6].ToString();
                    emps.Add(e);
                }
                return emps;
            }
            return null;
        }
        protected void RegisterUser_CreatedUser(object sender, EventArgs e)
        {
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();
            string strLog = "update employeelist set registered=true, emailaddress='" + RegisterUser.Email + "', password='" + RegisterUser.Password;
            strLog += " where employeenum=" + Request.Params["empNum"].ToString();
            OleDbCommand comm = new OleDbCommand();
            comm.CommandText = strLog;
            comm.CommandType = System.Data.CommandType.Text;
            comm.Connection = conn;
            try
            {
                comm.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                Response.Redirect("Register.aspx");
            }
                       
            //FormsAuthentication.SetAuthCookie(RegisterUser.UserName, false /* createPersistentCookie */);

            string continueUrl = RegisterUser.ContinueDestinationPageUrl;
            if (String.IsNullOrEmpty(continueUrl))
            {
                continueUrl = "~/";
            }
            Response.Redirect(continueUrl);
        }

    }
}
