using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace GrantApplication
{
	public class IISHandler1 : IHttpHandler, IRequiresSessionState
	{
		/// <summary>
		/// You will need to configure this handler in the Web.config file of your 
		/// web and register it with IIS before being able to use it. For more information
		/// see the following link: http://go.microsoft.com/?linkid=8101007
		/// </summary>
		#region IHttpHandler Members

		public bool IsReusable
		{
			// Return false in case your Managed Handler cannot be reused for another request.
			// Usually this would be false in case you have some state information preserved per request.
			get { return true; }
		}

		public void ProcessRequest(HttpContext context)
		{
		//	context.Response.ContentType = "text/json";
			context.Response.ContentType = "text/plain";
			NameValueCollection query = context.Request.QueryString;
			String command = (String)query["q"];
			if (command == "login")
			{
				doLogin(context, query["id"], query["pass"]);
			}
			else if (command == "email")
			{
				doEmailGrantId(context, query["id"]);
			}
			else
			{
				int? id = (int?)context.Session["ID"];
				if (!id.HasValue)
				{
					writeResult(context, false, "Invalid Session ID");
				}
				else
				{
					switch (command)
					{
						case "listgrants":
							IEnumerable<Grant> grants = OleDBHelper.query(
								 "SELECT GrantInfo.* FROM GrantInfo, EmployeeList"
								+ " WHERE EmployeeList.ID = ?"
								+ " AND EmployeeList.LastName = GrantInfo.GrantManagerLast"
								+ " AND EmployeeList.FirstName = GrantInfo.GrantManagerFirst"
								, new String[] { id.ToString() }
								, Grant.fromRow
							);
							writeResult(context, true, grants);
							break;
						case "listrequests":
							string status = null;
							try
							{
								status = ((int)Enum.Parse(typeof(GrantMonth.status), query["status"], true)).ToString();
							}
							catch (ArgumentNullException) { }
							catch (ArgumentException)
							{
								writeResult(context, false, query["status"] + " is not a valid status");
								return;
							}
							string sqlquery = "SELECT WorkMonth.* FROM WorkMonth, GrantInfo WHERE GrantInfo.ID = WorkMonth.GrantID"
								//string sqlquery = "SELECT WorkMonth.* FROM WorkMonth"
											+ " AND SupervisorID = " + id;
							//string[] sqlkeys = new string[] { "GrantInfo.GrantNumber", "EmployeeList.EmployeeNum", "WorkMonth.Status" };
							string[] sqlkeys = new string[] { "GrantInfo.ID", "WorkMonth.EmpID", "WorkMonth.Status" };
							string[] sqlvals = new string[] { query["grant"], query["employee"], status };
							IEnumerable<string> sqlparams;
							sqlquery = OleDBHelper.appendConditions(sqlquery, sqlkeys, sqlvals, out sqlparams);
							IEnumerable<GrantMonth> gm = OleDBHelper.query(
								sqlquery,
								sqlparams,
								row => new GrantMonth(row)
							);
							writeResult(context, true, gm);
							break;
						case "viewrequest":
							viewrequest(context, id, query);
							break;
						case "listemployees":
							IEnumerable<SafeEmployee> defaultemployees = OleDBHelper.query(
								"SELECT * FROM EmployeeList"
								+ " WHERE DefaultSupervisor = " + id
								, new string[] { }
								, SafeEmployee.fromRow
							);
							writeResult(context, true, defaultemployees);
							break;
						case "approve":
							sendApproval(context, query, id);
							break;
						case "debug":
							Dictionary<string, object> sesh = new Dictionary<string, object>();
							foreach (String key in context.Session.Keys)
							{
								sesh.Add(key, context.Session[key]);
							}
							writeResult(context, true, sesh);
							break;
						case "logout":
							context.Session.Abandon();
							writeResult(context, true, "Logged out successfully");
							break;
						default:
							writeResult(context, false, "Unrecognized request");
							break;
					}
				}
			}
		}

		private void doEmailGrantId(HttpContext context, string grantid)
		{
			string[] extragrants = new string[] { Globals.GrantID_Leave.ToString(), Globals.GrantID_NonGrant.ToString() };
			if (grantid == null)
			{
				writeResult(context, false, "Missing grant id from email");
			}
			else
			{
				Dictionary<string, IEnumerable<double>> times = renameGrantExtras(requestFromWorkMonth(0, extragrants, grantid));
				Dictionary<string, object> result = new Dictionary<string, object>();
				result.Add("hours", times);
				GrantMonth month = OleDBHelper.query(
					"SELECT * FROM WorkMonth WHERE ID = ?",
					new string[] { grantid },
					row => new GrantMonth(row)
				).SingleOrDefault();
				if (month == null)
				{
					writeResult(context, false, "No such grant");
				}
				else
				{
					IEnumerable<SafeEmployee> emps = OleDBHelper.query(
						"SELECT * FROM EmployeeList WHERE ID IN (" + month.supervisorID + ", " + month.EmpID + ")",
						new string[] { },
						SafeEmployee.fromRow
					);
					Grant grant = OleDBHelper.query(
						"SELECT * FROM GrantInfo WHERE ID = " + month.grantID,
						new string[] { },
						Grant.fromRow
					).SingleOrDefault();
					SafeEmployee employee = emps.Where(e => e.id == month.EmpID.ToString()).SingleOrDefault();
					SafeEmployee supervisor = emps.Where(e => e.id == month.supervisorID.ToString()).SingleOrDefault();
					context.Session["ID"] = month.supervisorID;
					result.Add("month", month.workMonth);
					result.Add("year", month.workYear);
					result.Add("status", month.curStatus);
					result.Add("supervisor", supervisor);
					result.Add("employee", employee);
					result.Add("grant", grant);
					writeResult(context, true, result);
				}
			}
		}
		private Dictionary<string, IEnumerable<double>> getrequest(int? id, string[] grants, string empid, string year, string month)
		{
			IEnumerable<TimeEntry> times = OleDBHelper.query(
				"SELECT TimeEntry.* FROM TimeEntry"
				//"SELECT TimeEntry.* FROM TimeEntry, GrantInfo, EmployeeList"
				//+ " WHERE TimeEntry.GrantID = GrantInfo.ID"
				//+ " AND TimeEntry.EmpID = EmployeeList.ID"
				//+ " AND TimeEntry.SupervisorID = " + id
				// the existing queries don't use it, plus it seems to be null a lot
				+ " WHERE TimeEntry.GrantID IN " + OleDBHelper.sqlInArrayParams(grants)
				+ " AND TimeEntry.EmpID = ?"
				+ " AND TimeEntry.YearNumber = ?"
				+ " AND TimeEntry.MonthNumber = ?"
				+ " ORDER BY TimeEntry.GrantID, TimeEntry.DayNumber ASC"
				, grants.Concat(new string[] { empid, year, month })
				, TimeEntry.fromRow
			);
			return groupTimes(times, grants);
		}

		private Dictionary<string, IEnumerable<double>> requestFromWorkMonth(int? id, string[] grants, string WorkMonthID)
		{
			IEnumerable<TimeEntry> times = OleDBHelper.query(
				"SELECT TimeEntry.* FROM TimeEntry, WorkMonth"
				+ " WHERE TimeEntry.EmpId = WorkMonth.EmpId"
				+ " AND WorkMonth.ID = ?"
				+ " AND (TimeEntry.GrantID = WorkMonth.GrantID"
				// special case, sql doesn't like WHERE foo IN ()
				+ (grants.Length == 0 ? "" : " OR TimeEntry.GrantID IN " + OleDBHelper.sqlInArrayParams(grants))
				+ ") AND TimeEntry.YearNumber = WorkMonth.WorkYear"
				+ " AND TimeEntry.MonthNumber = WorkMonth.WorkingMonth"
				+ " ORDER BY TimeEntry.GrantID, TimeEntry.DayNumber ASC"
				, new string[] { WorkMonthID }.Concat(grants)
				, TimeEntry.fromRow
			);
			return groupTimes(times, grants);
		}

		private Dictionary<string, IEnumerable<double>> groupTimes(IEnumerable<TimeEntry> times, string[] requiredkeys)
		{
			Dictionary<string, IEnumerable<double>> groupdict = times.GroupBy(time => time.grantID).Where(group=>group.Count() > 0).ToDictionary(
				group => group.Key.ToString(),
				group => {
					int length = DateTime.DaysInMonth(group.First().yearNumber, group.First().monthNumber+1);
					double[] days = new double[length];
					group.ForEach(entry => days[entry.dayNumber-1] = entry.grantHours); // days are 1-indexed, months are 0-indexed?
					return days.AsEnumerable();
				}
				//group => group.Select(time => time.grantHours)
			);
			requiredkeys.ForEach(key => {
				if (!groupdict.ContainsKey(key))
					groupdict.Add(key, new List<double>(0));
			});
			return groupdict;
		}

		private void viewrequest(HttpContext context, int? id, NameValueCollection query)
		{
			string grantstr = query["grant"] == null ? "" : query["grant"];
			string empid = query["employee"];
			string year = query["year"];
			string month = query["month"];
			string workmonth = query["request"];
			bool extras = query["withextras"] == "true";
			if (extras)
			{
				grantstr += "," + Globals.GrantID_Leave + "," + Globals.GrantID_NonGrant;
			}
			string[] grants = grantstr.Split(',').Where(g => !string.IsNullOrEmpty(g)).ToArray();
			Dictionary<string, IEnumerable<double>> groupTimes;
			if (workmonth != null)
			{
				groupTimes = requestFromWorkMonth(id, grants, workmonth);
			}
			else if (grants.Length > 0 && empid != null && year != null && month != null)
			{
				groupTimes = getrequest(id, grants, empid, year, month);
			}
			else {
				writeResult(context, false, "Missing employee id or grant number or date");
				return;
			}
			if (extras)
			{
				renameGrantExtras(groupTimes);
			}
			writeResult(context, true, groupTimes);
		}

		private Dictionary<string, IEnumerable<double>> renameGrantExtras(Dictionary<string, IEnumerable<double>> groupTimes)
		{
			renameIfExists(groupTimes, Globals.GrantID_Leave.ToString(), "leave");
			renameIfExists(groupTimes, Globals.GrantID_NonGrant.ToString(), "non-grant");
			return groupTimes;
		}

		private void renameIfExists<T, U>(Dictionary<T, U> dict, T oldkey, T newkey)
		{
			U val;
			if (dict.TryGetValue(oldkey, out val))
			{
				dict.Remove(oldkey);
				dict[newkey] = val;
			}
		}


		private void sendApproval(HttpContext context, NameValueCollection query, int? id)
		{
			string grant = query["grant"];
			string employee = query["employee"];
			bool? approve = null;
			int? month = null;
			int? year = null;
			try
			{
				approve = bool.Parse(query["approval"]);
				month = Int32.Parse(query["month"]);
				year = Int32.Parse(query["year"]);
			}
			catch (Exception) {
				writeResult(context, false, "Formatting error in field");
				return;
			}
			if (grant == null || month == null || year == null || employee == null)
			{
				writeResult(context, false, "Missing required field");
			}
			else
			{
				string reason = query["reason"];
				reason = reason != null ? reason : "No reason given.";
				Tuple<bool, object> result = OleDBHelper.withConnection(conn =>
				{
					Employee user = OleDBHelper.query(
						"SELECT * FROM EmployeeList WHERE ID = " + id, new string[] { }, row => new Employee(row), conn
					).First();
					List<Employee> employees = OleDBHelper.query(
						"SELECT * FROM EmployeeList WHERE ID = ?", new string[] { employee }, row => new Employee(row), conn
					).ToList();
					if (employees.Count == 0)
					{
						return new Tuple<bool, object>(false, "No such employee");
					}
					string[] grantstrs = grant.Split(',');
					string grantquery = OleDBHelper.sqlInArrayParams(grantstrs);
					List<Grant> grants = OleDBHelper.query(
						"SELECT * FROM GrantInfo WHERE ID IN " + grantquery, grantstrs, Grant.fromRow, conn
					).ToList();
					if (grants.Count == 0)
					{
						return new Tuple<bool, object>(false, "No such grant(s)");
					}
					if (GrantApplication._Default.approveOrDisapproveStateless(approve.Value, reason, employees[0], user, grants, month.Value, year.Value))
					{
						return new Tuple<bool, object>(true, "Sent email to " + employees[0].firstName + " " + employees[0].lastName);
					}
					else
					{
						return new Tuple<bool, object>(false, "Error sending email");
					}
				});
				writeResult(context, result.Item1, result.Item2);
			}
		}


		public void doLogin(HttpContext context, String empId, String pass)
		{
			empId = (empId != null) ? empId : "";
			IEnumerable<Employee> emps = OleDBHelper.query<Employee>(
				"SELECT * FROM EmployeeList WHERE registered = true AND EmployeeNum = ?",
				new String[] { empId },
				row => new Employee(row)
			);
			Employee emp = emps.FirstOrDefault();
			if (!Employee.TestPassword(emp, context.Request.QueryString["pass"]))
			{
				writeResult(context, false, "Wrong ID or password");
			}
			else
			{
				context.Session["ID"] = emp.ID;
				writeResult(context, true, "Logged in as " + emp.firstName + " " + emp.lastName);
			}
			return;
		}

		private void writeResult(HttpContext context, Boolean success, Object message)
		{
			JavaScriptSerializer s = new JavaScriptSerializer();
			Dictionary<String, Object> dict = new Dictionary<String, Object>();
			dict["success"] = success;
			dict["message"] = message;
#if DEBUG
			context.Response.Write(JsonPrettyPrinter.FormatJson(s.Serialize(dict)));
#else
			context.Response.Write(s.Serialize(dict));
#endif

		}




		class SafeEmployee
		{
			public string firstname;
			public string lastname;
			public string id;
			public SafeEmployee(Employee source)
			{
				firstname = source.firstName;
				lastname = source.lastName;
				id = source.ID.ToString();
			}
			public static SafeEmployee fromRow(DataRow row) {
				return new SafeEmployee(new Employee(row));
			}
		}

		#endregion
	}
}
