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


		static string[] extragrants = { Globals.GrantID_Leave.ToString(), Globals.GrantID_NonGrant.ToString() };
		static string[] noextragrants = { };

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

				doEmailGrantId(context, query);
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
							listgrants(context, id, query);
							break;
						case "listrequests":
							listrequests(context, id, query);
							break;
						case "viewrequest":
							viewrequest(context, id, query);
							break;
						case "listemployees":
							listemployees(context, id, query);
							break;
						case "approve":
							sendApproval(context, id, query);
							break;
						case "logout":
							context.Session.Abandon();
							writeResult(context, true, "Logged out successfully");
							break;
						case "debug":
							Dictionary<string, object> sesh = new Dictionary<string, object>();
							foreach (String key in context.Session.Keys)
							{
								sesh.Add(key, context.Session[key]);
							}
							writeResult(context, true, sesh);
							break;
						default:
							writeResult(context, false, "Unrecognized request");
							break;
					}
				}
			}
		}

		private void listemployees(HttpContext context, int? id, NameValueCollection query)
		{
			IEnumerable<SafeEmployee> defaultemployees = OleDBHelper.query(
				"SELECT * FROM EmployeeList"
				+ " WHERE DefaultSupervisor = " + id
				, SafeEmployee.fromRow
			);
			writeResult(context, true, defaultemployees);
		}

		private void listgrants(HttpContext context, int? id, NameValueCollection query)
		{
			IEnumerable<Grant> grants = OleDBHelper.query(
				 "SELECT GrantInfo.* FROM GrantInfo, EmployeeList"
				+ " WHERE EmployeeList.ID = ?"
				+ " AND EmployeeList.LastName = GrantInfo.GrantManagerLast"
				+ " AND EmployeeList.FirstName = GrantInfo.GrantManagerFirst"
				, Grant.fromRow
				, id.ToString()
			);
			writeResult(context, true, grants);
		}

		private void listrequests(HttpContext context, int? id, NameValueCollection query)
		{
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
			string[] sqlkeys = { "GrantInfo.ID", "WorkMonth.EmpID", "WorkMonth.Status" };
			string[] sqlvals = { query["grant"], query["employee"], status };
			IEnumerable<string> sqlparams;
			sqlquery = OleDBHelper.appendConditions(sqlquery, sqlkeys, sqlvals, out sqlparams);
			IEnumerable<GrantMonth> gm = OleDBHelper.query(
				sqlquery,
				GrantMonth.fromRow,
				sqlparams.ToArray()
			);
			writeResult(context, true, gm);
		}

		private void doEmailGrantId(HttpContext context, NameValueCollection query)
		{
			IEnumerable<GrantMonth> months = getWorkMonthsFromQuery(query);
			if (months == null || months.Count() > 1)
			{
				writeResult(context, false, "Missing grant id from email");
				return;
			}
			GrantMonth month = months.SingleOrDefault();
			if (month == null)
			{
				writeResult(context, false, "No such grant");
			}
			else
			{
				Dictionary<string, IEnumerable<double>> times = renameGrantExtras(requestFromWorkMonth(months, extragrants));
				IEnumerable<SafeEmployee> emps = OleDBHelper.query(
					"SELECT * FROM EmployeeList WHERE ID IN (" + month.supervisorID + ", " + month.EmpID + ")",
					SafeEmployee.fromRow
				);
				Grant grant = OleDBHelper.query(
					"SELECT * FROM GrantInfo WHERE ID = " + month.grantID,
					Grant.fromRow
				).SingleOrDefault();
				SafeEmployee employee = emps.Where(e => e.id == month.EmpID.ToString()).SingleOrDefault();
				SafeEmployee supervisor = emps.Where(e => e.id == month.supervisorID.ToString()).SingleOrDefault();
				context.Session["ID"] = month.supervisorID;
				context.Session["WorkMonthID"] = month.ID;

				var result = new
				{
					hours      = times,
					month      = month.workMonth,
					year       = month.workYear,
					status     = month.curStatus,
					id         = month.ID,
					supervisor = supervisor,
					employee   = employee,
					grant      = grant
				};
				writeResult(context, true, result);
			}
		}

		// return a GrantMonth object from enough details to specify a set of WorkMonth entries for a single month and employee
		private IEnumerable<GrantMonth> getWorkMonthIDs(string[] grants, string empid, string year, string month)
		{
			IEnumerable<GrantMonth> grantmonths = OleDBHelper.query(
				"SELECT * FROM WorkMonth"
				+ " WHERE GrantID IN " + OleDBHelper.sqlInArrayParams(grants)
				+ " AND EmpID = ?"
				+ " AND WorkYear= ?"
				+ " AND WorkingMonth = ?"
				+ " ORDER BY GrantID"
				, GrantMonth.fromRow
				, grants.Concat(new string[] { empid, year, month }).ToArray()
			);
			return grantmonths;
		}

		// return a GrantMonth object from a WorkMonth ID
		private IEnumerable<GrantMonth> getWorkMonthIDs(string[] workmonths)
		{
			return OleDBHelper.query(
					"SELECT * FROM WorkMonth WHERE ID IN (" + OleDBHelper.sqlInArrayParams(workmonths) + ")"
					, GrantMonth.fromRow
					, workmonths
				);
		}

		// get WorkMonths the lazy way
		// this should be the only method to handle both ways of specifying a particular month, so that we don't
		// have to have requestFromDate() etc.  The downside is an extra database request.
		// Note that grants will be sorted, not returned in input order, and leave+nongrant time should not be dealt with here
		// rather, they should be passed to requestFromWorkMonth
		private IEnumerable<GrantMonth> getWorkMonthsFromQuery(NameValueCollection query)
		{
			string grantstr = query["grant"];
			string empid = query["employee"];
			string year = query["year"];
			string month = query["month"];
			string workmonthstr = query["id"];
			try
			{
				if (workmonthstr != null)
				{
					string[] workmonths = workmonthstr.Split(',').Where(g => !string.IsNullOrEmpty(g)).OrderBy(str => str).ToArray();
					return getWorkMonthIDs(workmonths);
				}
				else if (grantstr != null && empid != null && year != null && month != null)
				{
					string[] grants = grantstr.Split(',').Where(g => !string.IsNullOrEmpty(g)).OrderBy(str => str).ToArray();
					return getWorkMonthIDs(grants, empid, year, month);
				}
			}
			catch (Exception) { }
			return null;
		}

		// return a dict of daily time entries from supplied work month IDs and additional grant ids (for non-grant/leave time)
		private Dictionary<string, IEnumerable<double>> requestFromWorkMonth(string[] WorkMonthIDs, string[] grants, int defaultMonthLength)
		{
			IEnumerable<TimeEntry> times = OleDBHelper.query(
				"SELECT TimeEntry.* FROM TimeEntry, WorkMonth"
				+ " WHERE TimeEntry.EmpId = WorkMonth.EmpId"
				+ (WorkMonthIDs.Length == 0 ? "" : " AND WorkMonth.ID IN " + OleDBHelper.sqlInArrayParams(WorkMonthIDs))
				+ " AND (TimeEntry.GrantID = WorkMonth.GrantID"
				// special case, sql doesn't like WHERE foo IN ()
				+ (grants.Length == 0 ? "" : " OR TimeEntry.GrantID IN " + OleDBHelper.sqlInArrayParams(grants))
				+ ") AND TimeEntry.YearNumber = WorkMonth.WorkYear"
				+ " AND TimeEntry.MonthNumber = WorkMonth.WorkingMonth"
				+ " ORDER BY TimeEntry.GrantID, TimeEntry.DayNumber ASC"
				, TimeEntry.fromRow
				, WorkMonthIDs.Concat(grants).ToArray()
			);
			Dictionary<string, IEnumerable<double>> grouped = groupTimes(times);

			// make sure there is some entry for each key, even if we didn't find any TimeEntries for it
			grants.ForEach(key => {
				if (!grouped.ContainsKey(key))
					grouped.Add(key, new List<double>(defaultMonthLength));
			});
			return grouped;
		}
		
		// helper method to go from getWorkMonthX to requestFromWorkMonth
		private Dictionary<string, IEnumerable<double>> requestFromWorkMonth(IEnumerable<GrantMonth> grantMonths, string[] grants)
		{
			int length = DateTime.DaysInMonth(grantMonths.First().workYear, grantMonths.First().workMonth + 1);
			return requestFromWorkMonth(grantMonths.Select(month => month.ID.ToString()).ToArray(), grants, length);
		}

		// helper method for requestFromWorkMonth
		// form TimeEntries into arrays organized by key
		private Dictionary<string, IEnumerable<double>> groupTimes(IEnumerable<TimeEntry> times)
		{
			Dictionary<string, IEnumerable<double>> groupdict = times.GroupBy(time => time.grantID).Where(group=>group.Count() > 0).ToDictionary(
				group => group.Key.ToString(),
				group => {
					int length = DateTime.DaysInMonth(group.First().yearNumber, group.First().monthNumber+1);
					double[] days = new double[length];
					group.ForEach(entry => days[entry.dayNumber-1] = entry.grantHours); // days are 1-indexed, months are 0-indexed?
					return days.AsEnumerable();
				}
			);
			return groupdict;
		}

		private void viewrequest(HttpContext context, int? id, NameValueCollection query)
		{
			bool extras = query["withextras"] == "true";
			string[] grants = extras ? extragrants : noextragrants;
			IEnumerable<GrantMonth> months = getWorkMonthsFromQuery(query);
			if (months == null)
			{
				writeResult(context, false, "Missing employee id or grant number or date");
				return;
			}
			Dictionary<string, IEnumerable<double>> groupTimes = requestFromWorkMonth(months, grants);
			if (extras)
			{
				groupTimes = renameGrantExtras(groupTimes);
			}
			writeResult(context, true, groupTimes);
		}

		// helper method for viewrequest et al.
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

		private void sendApproval(HttpContext context, int? id, NameValueCollection query)
		{
			bool? approve = null;
			try
			{
				approve = bool.Parse(query["approval"]);
			}
			catch (Exception)
			{
				writeResult(context, false, "Formatting error in field");
				return;
			}
			//GrantMonth[] gmz = tmpGm.Concat(getWorkMonthIDs(
			//GrantMonth[] gm = getWorkMonthsFromQuery(query).ToArray();
			int? idFromEmail = (int?)context.Session["WorkMonthID"];
			IEnumerable<GrantMonth> tmpGm = getWorkMonthsFromQuery(query);
			if ((tmpGm == null || tmpGm.Count() == 0) && idFromEmail.HasValue)
			{
					tmpGm = getWorkMonthIDs(new string[] { context.Session["WorkMonthID"].ToString() });
			}
			if (tmpGm == null || tmpGm.Count() == 0)
			{
				writeResult(context, false, "Missing required field or no such entry");
				return;
			}
			else
			{
				// It seems silly not to support both input types, but we have to have the non-grant/leave WorkMonths for approveOrDisapprove()
				// and have no way of ensuring they're included (save for some inner join shenanigans)
				// So instead, let's get one of the GrantMonths we grabbed already and use it to collect the stragglers
				GrantMonth firstGm = tmpGm.First();
				IEnumerable<GrantMonth> extraGm = getWorkMonthIDs(extragrants, firstGm.EmpID.ToString(), firstGm.workYear.ToString(), firstGm.workMonth.ToString());
				GrantMonth[] workmonths = tmpGm.Concat(extraGm).GroupBy(month => month.ID).Select(months => months.First()).ToArray();
				// ^ union() would be better but doesn't support lambdas; this is the stackoverflow-approved substitute
				
				string reason = query["reason"];
				reason = reason != null ? reason : "No reason given.";
				Tuple<bool, object> result = OleDBHelper.withConnection(conn =>
				{
					Employee user = OleDBHelper.query(conn,
						"SELECT * FROM EmployeeList WHERE ID = " + id, Employee.fromRow
					).SingleOrDefault();
					List<Grant> grants = OleDBHelper.query(conn,
						"SELECT * FROM GrantInfo WHERE ID IN (" + OleDBHelper.sqlInArrayParams(workmonths) + ")"
						, Grant.fromRow
						, workmonths.Select(g => g.grantID.ToString()).ToArray()
					).ToList();
					Employee employee = OleDBHelper.query(conn, "SELECT * FROM EmployeeList WHERE ID = " + workmonths[0].EmpID, Employee.fromRow
					).SingleOrDefault();
					if (GrantApplication._Default.approveOrDisapproveStateless(approve.Value, reason, employee, user,
						grants, workmonths[0].workMonth, workmonths[0].workYear))
					{
						return new Tuple<bool, object>(true, "Sent email to " + employee.firstName + " " + employee.lastName);
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
				Employee.fromRow,
				empId
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
