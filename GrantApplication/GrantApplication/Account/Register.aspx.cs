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
			string redirectUrl = OleDBHelper.withConnection(conn =>
			{
				Employee emp = new Employee();
				emp.EmpNum = RegisterUser.UserName;
				try
				{
					// TODO I think this will work, but I'm not 100% sure.
					emp.ID = OleDBHelper.query(
						"update employeelist set registered=true, EmailAddress=?, UserPass=?"
						+ "where EmployeeNum=?;"
						+ "select @@identity"
						, new string[] { RegisterUser.Email, RegisterUser.Password, RegisterUser.UserName }
						, row => (int)row[0]
						, conn
					).First();
				}
				catch (System.Exception)
				{
					return "Register.aspx";
				}
				List<Employee> empList = populateEmployeeList(emp, conn);
				Session["CurrentEmployeeList"] = empList;
				//FormsAuthentication.SetAuthCookie(RegisterUser.UserName, false /* createPersistentCookie */);

				string continueUrl = "./Login.aspx";
				if (String.IsNullOrEmpty(continueUrl))
				{
					continueUrl = "~/";
				}
				return continueUrl;
			});
			Response.Redirect(redirectUrl);
        }
        private List<Employee> populateEmployeeList(Employee emp, OleDbConnection conn)
        {
			try
			{
				return OleDBHelper.query(
					"select * from EmployeeList where EmployeeNum = " + emp.EmpNum
					, new string[] { }
					, row => new Employee(row)
					, conn
				).ToList();
			}
            catch (System.Exception)
            {
                return null;
            }
        }
        protected void RegisterUser_CreatedUser(object sender, EventArgs e)
        {
            try
			{
				OleDBHelper.nonQuery(
					"update employeelist set registered=true, emailaddress=?, password=?"
					+ "where employeenum=?"
					, new string[] { RegisterUser.Email, RegisterUser.Password, Request.Params["empNum"].ToString() }
				);
            }
            catch (System.Exception)
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
