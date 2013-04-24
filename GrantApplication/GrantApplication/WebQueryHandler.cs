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
	public class WebQueryHandler : IHttpHandler, IRequiresSessionState
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
						case "listallgrants":
							listallgrants(context, id, query);
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
						case "updatehours":
							doUpdateHours(context, id, query);
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

		private void listallgrants(HttpContext context, int? id, NameValueCollection query)
		{
			IEnumerable<Grant> grants = OleDBHelper.query(
				"SELECT GrantInfo.* FROM GrantInfo"
				+ " WHERE GrantInfo.ID NOT IN ( "
				+ Globals.GrantID_Leave + ", " 
				+ Globals.GrantID_NonGrant + ", "
				+ Globals.GrantID_Placeholder + ")"
				, Grant.fromRow
			);
			writeResult(context, true, grants);
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
			WorkMonthRequest workmonth = WorkMonthRequest.fromQuery(query);
			if (workmonth == null)
			{
				writeResult(context, false, "Missing one or more required fields");
				return;
			}
			IEnumerable<GrantMonth> months = workmonth.grantMonths;
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
				Dictionary<string, IEnumerable<double>> times = renameGrantExtras(workmonth.getTimes(extragrants));
				renameIfExists(times, month.grantID.ToString(), "grant");
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

		private void viewrequest(HttpContext context, int? id, NameValueCollection query)
		{
			bool extras = query["withextras"] == "true";
			string[] grants = extras ? extragrants : noextragrants;
			WorkMonthRequest months = WorkMonthRequest.fromQuery(query);
			if (months == null)
			{
				writeResult(context, false, "Missing employee id or grant number or date");
				return;
			}
			Dictionary<string, IEnumerable<double>> groupTimes = months.getTimes(grants);
			if (extras || query["grant"].Contains("non-grant"))
				renameIfExists(groupTimes, Globals.GrantID_NonGrant.ToString(), "non-grant");
			if (extras || query["grant"].Contains("leave"))
				renameIfExists(groupTimes, Globals.GrantID_Leave.ToString(), "leave");
					
			writeResult(context, true, groupTimes);
		}

		private void sendApproval(HttpContext context, int? id, NameValueCollection query)
		{
			bool approve;
			if (!bool.TryParse(query["approval"], out approve))
			{
				writeResult(context, false, "Formatting error in field");
				return;
			}

			// this is a terrible thing to be doing
			int? idFromEmail = (int?)context.Session["WorkMonthID"];
			if (query["id"] == null && idFromEmail.HasValue)
			{
				query = new NameValueCollection(query);
				query["id"] = idFromEmail.ToString();
			}

			IEnumerable<GrantMonth> tmpGm = WorkMonthRequest.fromQuery(query).grantMonths;
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
				WorkMonthRequest workrequest = WorkMonthRequest.fromGrantMonths(tmpGm, extragrants.Select(eg => int.Parse(eg)));
				IEnumerable<GrantMonth> extraGm = workrequest.grantMonths;
				GrantMonth[] workmonths = tmpGm.Concat(extraGm).GroupBy(month => month.ID).Select(months => months.First()).ToArray();
				// ^ union() would be better but doesn't support lambdas; this is the stackoverflow-approved substitute
				
				string reason = query["reason"];
				reason = reason != null ? reason : "No reason given.";
				var result = OleDBHelper.withConnection(conn =>
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
					if (GrantApplication._Default.approveOrDisapproveStateless(approve, reason, employee, user,
						grants, workmonths[0].workMonth, workmonths[0].workYear))
					{
						return new { success = true, message = "Sent email to " + employee.firstName + " " + employee.lastName };
					}
					else
					{
						return new { success = false, message = "Error sending email" };
					}
				});
				writeResult(context, result.success, result.message);
			}
		}

		private void doUpdateHours(HttpContext context, int? id, NameValueCollection query)
		{
			string supidstr = query["supervisor"];
			string hoursstr = query["hours"];
			//string empidstr = query["employee"];
			string yearstr = query["year"];
			string monthstr = query["month"];
			if (supidstr == null || hoursstr == null || yearstr == null || monthstr == null)
			{
				writeResult(context, false, "missing required field(s)");
				return;
			}
			int supid, empid, year, month;
			if (!int.TryParse(supidstr, out supid) || !id.HasValue || !int.TryParse(yearstr, out year) || !int.TryParse(monthstr, out month))
			{
				writeResult(context, false, "misformatted required field(s)");
				return;
			}
			empid = id.Value;

			Dictionary<int, double[]> hoursById = null;
			try
			{
				Dictionary<string, double[]> hours = new JavaScriptSerializer().Deserialize<Dictionary<string, double[]>>(hoursstr);
				hoursById = hours.ToDictionary(
					kv => WorkMonthRequest.parseQueryGrant(kv.Key),
					kv => kv.Value
				);
			}
			//catch (Exception e)
			//{
			//	string result = string.Format("parsing error in hours: {0}\nstack trace: {1}", e.Message, e.StackTrace).Replace("\n", Environment.NewLine);
			//	writeResult(context, false, result);
			//}
			finally { }

			WorkMonthRequest workrequest = new WorkMonthRequest(empid, supid, month, year, hoursById.Keys.ToArray());
			Dictionary<string, int> success = updateHours(workrequest, hoursById, supid);
			writeResult(context, true, success);
		}

		private Dictionary<string, int> updateHours(WorkMonthRequest workrequest, Dictionary<int, double[]> hours, int supervisor)
		{
			int monthlength = DateTime.DaysInMonth(workrequest.year, workrequest.month + 1);

			return OleDBHelper.withConnection(conn =>
			{
				Dictionary<string, TimeEntry[]> oldgroup = workrequest.getTimeEntries(new string[] { });
				Dictionary<string, int> results = new Dictionary<string, int> { { "added", 0 }, { "updated", 0 }, { "unchanged", 0 } };
				hours.ForEach(kv =>
				{
					int grantid = kv.Key;
					TimeEntry[] oldhours;
					IEnumerable<double> properLengthMonth = kv.Value.Concat(paddingValue(0.0)).Take(monthlength);
					if (oldgroup.TryGetValue(grantid.ToString(), out oldhours))
					{
						properLengthMonth.ForEach((time, dayIndex) =>
						{
							int day = dayIndex + 1;
							results[addOrUpdateEntry(conn, workrequest, grantid, supervisor, day, time, oldhours[dayIndex])]++;
						});
					}
					else
					{
						properLengthMonth.ForEach((time, dayIndex) =>
						{
							int day = dayIndex + 1;
							results[addOrUpdateEntry(conn, workrequest, grantid, supervisor, day, time, null)]++;
						});
					}
				});
				return results;
			});
		}

		// decide whether to addNewTimeEntry, updateTimeEntry, or neither, and deal with the weirdness of calling them
		// incidentally, neither one will set the supervisor id, but we're accepting it as an argument for future proofiness
		private string addOrUpdateEntry(OleDbConnection conn, WorkMonthRequest req, int grant, int supervisor, int day, double time, TimeEntry oldentry)
		{
			// No, seriously.  addNewTimeEntry() and updateTimeEntry() unconditionally the connection they're passed.
			// Methods in Default.aspx.cs that call them unconditionally close them first, but we're a little more careful.
			// Why is all of this being done?  I have no idea.
			if (conn.State == ConnectionState.Open)
			{
				conn.Close();
			}
			if (oldentry == null)
			{
				if (time != 0)
				{
					TimeEntry newentry = new TimeEntry(time, grant, req.employee, supervisor, req.month, day, req.year);
					_Default.addNewTimeEntry(newentry, new OleDbCommand(), conn);
					return "added";
				}
			}
			else if (oldentry.grantHours != time)
			{
				oldentry.grantHours = time;
				_Default.updateTimeEntry(oldentry, new OleDbCommand(), conn);
				return "updated";
			}
			return "unchanged";
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
				writeResult(context, true, new SafeEmployee(emp));
			}
			return;
		}

		private void writeResult(HttpContext context, Boolean success, Object message)
		{
			JavaScriptSerializer s = new JavaScriptSerializer();
			var output = new
			{
				success = success,
				message = message
			};
#if DEBUG
			context.Response.Write(JsonPrettyPrinter.FormatJson(s.Serialize(output)));

#else
			context.Response.Write(s.Serialize(output));
#endif

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

		// this has to be built in, but I can't find it for the life of me
		IEnumerable<T> paddingValue<T>(T val)
		{
			while (true)
				yield return val;
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
