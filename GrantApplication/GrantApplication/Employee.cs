using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace GrantApplication
{
	[Serializable()]
	public class Employee
	{
		public int ID = 0;
		public string EmpNum = string.Empty;
		public string lastName = string.Empty;
		public string firstName = string.Empty;
		public string jobTitle = string.Empty;
		public string emailAddress = string.Empty;
		public string password = string.Empty;
		public bool registered = false;
		public bool manager = false;
		public string defaultSupervisor = string.Empty;
		private byte[] _salt;
		public string saltstr
		{
			get
			{
				return Convert.ToBase64String(_salt);
			}
			set
			{
				_salt = Convert.FromBase64String(value);
			}
		}

		public override string ToString()
		{
			if (firstName == string.Empty && lastName == string.Empty)
			{
				return base.ToString();
			}
			return firstName + " " + lastName + " - " + jobTitle;
		}
		public Employee() { }
		public Employee(DataRow dr)
		{
			this.ID = (int)dr[0];
			this.EmpNum = dr[1].ToString();
			this.firstName = dr[3].ToString();
			this.lastName = dr[2].ToString();
			this.jobTitle = dr[4].ToString();
			if (dr[5] != DBNull.Value)
			{
				this.emailAddress = dr[5].ToString();
			}
			if (dr[6] != DBNull.Value)
			{
				this.password = dr[6].ToString();
			}
			this.registered = (bool)dr[7];
			this.manager = (bool)dr[8];
			this.defaultSupervisor = dr[9].ToString();
			if (this.defaultSupervisor == string.Empty || this.defaultSupervisor == "0")
			{
				this.defaultSupervisor = Globals.defaultSupervisorID; //Marie Schmieder's emp ID.
			}
			try
			{
				this.saltstr = dr[10].ToString();
			}
			catch (Exception)
			{
				SecurePassword();
			}
			if (this.saltstr == null || this.saltstr == string.Empty)
			{
				SecurePassword();
			}
		}

		private void SecurePassword()
		{
			string oldpw = password;
			CreateSalt();
			password = HashPassword(oldpw);
			//HttpContext.Current.Response.Write("hashing password '" + password + "' for user " + firstName + " " + lastName
			//	+ " with salt " + saltstr);
			//HttpContext.Current.Response.Write("\nIt comes to " + password);
			//HttpContext.Current.Response.Write("\nA second time (with pw '"+oldpw+"'): " + HashPassword(oldpw));
			//HttpContext.Current.Response.Write("\n\n");
			OleDBHelper.nonQuery(
				"UPDATE EmployeeList SET UserPass = ?, salt = ? WHERE ID = " + ID,
				new string[] { password, saltstr }
			);
		}

		private void CreateSalt()
		{
			_salt = new byte[8];
			System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(_salt);
		}

		public String HashPassword(string pass)
		{
			Rfc2898DeriveBytes b = new Rfc2898DeriveBytes(pass, _salt, 1000);
			return Convert.ToBase64String(b.GetBytes(24));
		}

		public static bool TestPassword(Employee emp, string p)
		{
			//if (emp != null)
			//{
			//	HttpContext.Current.Response.Write(
			//		new Dictionary<string, string> {
			//		{ "got", HttpContext.Current.Request.QueryString["pass"] },
			//		{ "hashes to", emp.HashPassword(HttpContext.Current.Request.QueryString["pass"]) },
			//		{ "with salt", emp.saltstr },
			//		{ "needs to be", emp.password }
			//	}.Select(kv => kv.Key.ToString() + ": " + kv.Value.ToString()).ToStringJoin("\n") + "\n");
			//}



			return emp != null && emp.password == emp.HashPassword(p);
		}
	}

}