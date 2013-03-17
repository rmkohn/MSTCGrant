using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.OleDb;
using System.Configuration;
using System.Data;
using System.IO;

namespace GrantApplication.Account
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString["ReturnUrl"]);
            LoginUser.LoginError += new EventHandler(LoginUser_LoginError);

            if (Session["CurrentEmployeeList"] != null)
            {
                pnlSwitchUser.Visible = true;
            }
            else
            {
                pnlSwitchUser.Visible = false;
            }
            
        }

        void LoginUser_LoginError(object sender, EventArgs e)
        {
            int ix = 0;
            throw new NotImplementedException();
        }

        protected void OnCrank(object sender, CommandEventArgs e)
        {
            List<Employee> emps = checkUser();
            if (emps == null || emps.Count < 1)
            {
                if (emps == null)
                {                    
                    Response.Redirect("../NotAllowed.aspx");                    
                    lblScrewedUp.Visible = true;
                }
                else
                {
                    string url = "Register.aspx";
                    if (emps[0].EmpNum != string.Empty)
                    {
                        url += "?empNum=" + emps[0].EmpNum;
                    }
                    Response.Redirect(url);
                }
            }
            else
            {
                Session["CurrentEmployeeList"] = emps;               
                Response.Redirect("../Default.aspx");
            }
        }

        protected List<Employee> checkUser()
        {
            List<Employee> emps;
            try
			{
				emps = OleDBHelper.query(
				   "select * from EmployeeList where EmployeeNUm = ?",
				   new string[] { LoginUser.UserName },
				   row => new Employee(row)
				).ToList();
            }
            catch (System.Exception)
            {
                return null;
            }
            return emps;
        }

        protected void GripNRip(object sender, EventArgs e)
        {
            List<Employee> emps = checkUser();
            if (emps == null || emps.Count < 1)
            {
                Response.Redirect("../NotAllowed.aspx");
                lblScrewedUp.Visible = true;
            } 
           else if (!emps[0].registered)
           {
                string url = "Register.aspx";
                if (emps.Count > 0 && emps[0].EmpNum != string.Empty)
                {
                    url += "?empNum=" + emps[0].EmpNum;
                }
                Response.Redirect(url);
                
            }
            else
            {
			   if (!Employee.TestPassword(emps[0], LoginUser.Password))
                {
                    lblScrewedUp.Visible = true;
                    return;
                }
                Session["CurrentEmployeeList"] = emps;
                var ebag = (from ee in emps where ee.manager == true select ee).ToList();
                if (ebag == null || ebag.Count < 1)
                {
                    Response.Redirect("../Default.aspx");
                }
                else
                {
                    List<Employee> kids = getKids(ebag[0].ID.ToString());                    
                    ListItem lbag = new ListItem("<Select a user or just click Go>", "-1");
                    ddlKids.Items.Add(lbag);
                    foreach (Employee kid in kids)
                    {
                        ListItem li = new ListItem();
                        li.Text = kid.ToString();
                        li.Value = kid.ID.ToString();
                        ddlKids.Items.Add(li);
                    }                    
                    ddlKids.SelectedValue = "-1";
                    this.pnlSwitchUser.Visible = true;
                    Session["kids"] = kids;
                    Session["LoggedIn"] = true;
                    if (emps[0].EmpNum.Contains("14805010"))  //It's Marie
                    {
                        pnlDowner.Visible = true;                       
                         Menu m = (Menu)this.Master.FindControl("NavigationMenu");
                         m.Items[2].Selectable = true;                        
                    }
                    else
                    {
                        pnlDowner.Visible = false;
                    }
                }

            }
        }
        protected void Go(object sender, EventArgs e)
        {            
            if (ddlKids.SelectedIndex <= 0)
            {
                Response.Redirect("../Default.aspx");
            }
            List<Employee> kids = (List<Employee>)Session["kids"];
            var theOne = (from k in kids where k.ID == System.Convert.ToInt32(ddlKids.SelectedValue) select k).FirstOrDefault();
            List<Employee> emps = new List<Employee>();
            emps.Add(theOne);
            Session["CurrentEmployeeList"] = emps;
            Response.Redirect("../Default.aspx?Impersonate=true");

        }

        protected void DoStuff(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            int i = 0;
        }

        protected void CrankTheDownload(object sender, EventArgs e)
        {
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            string path = Request.PhysicalApplicationPath + "App_Data\\Grants.accdb";
            string pather = Request.PhysicalApplicationPath + "Grants.accdb";
            Response.AppendHeader("Content-Disposition", "attachment; filename=Grants.accdb;");
            if (File.Exists(pather)) 
            {
                File.Delete(pather);
            }
            File.Copy(path, pather);
            FileInfo fi = new FileInfo(pather);
            byte[] byter = File.ReadAllBytes(pather);            
            Response.AddHeader("Content-Length", fi.Length.ToString());
            try
            {
                Response.BinaryWrite(byter);
                //Response.TransmitFile(fi.FullName);
                Response.Flush();
                Response.End();
            }
            catch (System.Exception ex)
            {
                Response.Write("Database download failure!");
                return;
            }
            File.Delete(pather);
        }

        protected List<Employee> getKids(string empID)
        {
            string str = "select * from EmployeeList where DefaultSupervisor=" + empID + " and registered=true";
            OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
            conn.Open();            
            OleDbCommand comm = new OleDbCommand();
            comm.CommandText = str;
            comm.CommandType = System.Data.CommandType.Text;
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
            List<Employee> emps = new List<Employee>();           
            
            foreach (DataRow dr in set.Tables[0].Rows)
            {
                Employee emp = new Employee();
                emp.defaultSupervisor = empID;
                emp.emailAddress = dr[5].ToString();
                emp.EmpNum = dr[1].ToString();
                emp.firstName = dr[3].ToString();
                emp.ID = (int)dr[0];
                emp.jobTitle = dr[4].ToString();
                emp.lastName = dr[2].ToString();
                emp.manager = false;
                emp.registered = (bool)dr[7];
                emps.Add(emp);
            }
            return emps;
        }
        //Response.ContentType = "application/pdf";
        //Response.AppendHeader("Content-Disposition", "attachment; filename=MyFile.pdf");
        //Response.TransmitFile(Server.MapPath("~/Files/MyFile.pdf"));
        //Response.End();

    }
}
