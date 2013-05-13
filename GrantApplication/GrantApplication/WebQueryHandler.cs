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
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
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
			context.Response.ContentType = "text/plain";
			Result result = getQueryResult(context);
			if (result == null)
				context.Response.Write("This link must be opened using the Grant Approval App.");
			else
			{
				JavaScriptSerializer s = new JavaScriptSerializer();
				var output = new
				{
					success = result.success,
					message = result.message,
					version = "1.0"
				};
#if DEBUG
				context.Response.Write(JsonPrettyPrinter.FormatJson(s.Serialize(output)));
#else
					context.Response.Write(s.Serialize(output));
#endif
			}
		}

		private Result getQueryResult(HttpContext context)
		{
			NameValueCollection query = context.Request.QueryString;
			int? id = (int?)context.Session["ID"];
			string command = (string)query["q"];
			switch (command)
			{
				case "login":
					return doLogin(context, query["id"], query["pass"]);
				case "email":
					return doEmailGrantId(context, query);
				case "listgrants":
					return testId(id, query, listgrants);
				case "listallgrants":
					return testId(id, query, listallgrants);
				case "listrequests":
					return testId(id, query, listrequests);
				case "viewrequest":
					return testId(id, query, viewrequest);
				case "listemployees":
					return testId(id, query, listemployees);
				case "approve": // plz fix me
					if (id.HasValue) return sendApproval(id.Value, query, context);
					else return testId(id, query, null);
				case "updatehours":
					return testId(id, query, doUpdateHours);
				case "sendrequest":
					return testId(id, query, sendRequest);
				case "listsupervisors":
					return testId(id, query, getAllSupervisors);
				case "logout":
					context.Session.Abandon();
					return new Result(true, "Logged out successfully");
				case "debug":
					Dictionary<string, object> sesh = new Dictionary<string, object>();
					foreach (String key in context.Session.Keys)
					{
						sesh.Add(key, context.Session[key]);
					}
					return new Result(true, sesh);
				case "":
					return null;
				case null:
					return null;
				default:
					return new Result(false, "Unrecognized request");
			}
		}

		private Result testId(int? id, NameValueCollection query, Func<int, NameValueCollection, Result> fn)
		{
			if (id.HasValue) return fn(id.Value, query);
			else return new Result(false, "Invalid Session ID");
		}

		private Result listemployees(int id, NameValueCollection query)
		{
			IEnumerable<SafeEmployee> defaultemployees = OleDBHelper.query(
				"SELECT * FROM EmployeeList"
				+ " WHERE DefaultSupervisor = " + id
				, SafeEmployee.fromRow
			);
			return new Result(true, defaultemployees);
		}

		private Result listallgrants(int id, NameValueCollection query)
		{
			IEnumerable<Grant> grants = OleDBHelper.query(
				"SELECT GrantInfo.* FROM GrantInfo"
				+ " WHERE GrantInfo.ID NOT IN ( "
				+ Globals.GrantID_Leave + ", " 
				+ Globals.GrantID_NonGrant + ", "
				+ Globals.GrantID_Placeholder + ")"
				, Grant.fromRow
			);
			return new Result(true, grants);
		}

		private Result listgrants(int id, NameValueCollection query)
		{
			IEnumerable<Grant> grants = OleDBHelper.query(
				 "SELECT GrantInfo.* FROM GrantInfo, EmployeeList"
				+ " WHERE EmployeeList.ID = ?"
				+ " AND EmployeeList.LastName = GrantInfo.GrantManagerLast"
				+ " AND EmployeeList.FirstName = GrantInfo.GrantManagerFirst"
				, Grant.fromRow
				, id.ToString()
			);
			return new Result(true, grants);
		}

		private Result listrequests(int id, NameValueCollection query)
		{
			string status = null;
			try
			{
				status = ((int)Enum.Parse(typeof(GrantMonth.status), query["status"], true)).ToString();
			}
			catch (ArgumentNullException) { }
			catch (ArgumentException)
			{
				return new Result(false, query["status"] + " is not a valid status");
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
			return new Result(true, gm);
		}

		private Result doEmailGrantId(HttpContext context, NameValueCollection query)
		{
			WorkMonthRequest workmonth = WorkMonthRequest.fromQuery(query);
			if (workmonth == null)
			{
				return new Result(false, "Missing one or more required fields");
			}
			IEnumerable<GrantMonth> months = workmonth.grantMonths;
			if (months == null || months.Count() > 1)
			{
				return new Result(false, "Missing grant id from email");
			}
			GrantMonth month = months.SingleOrDefault();
			if (month == null)
			{
				return new Result(false, "No such grant");
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
					status     = Enum.GetName(typeof(GrantMonth.status), month.curStatus),
					id         = month.ID,
					//fortune    = getFortune(),
					supervisor = supervisor,
					employee   = employee,
					grant      = grant
				};
				return new Result(true, result);
			}
		}

		private Result viewrequest(int id, NameValueCollection query)
		{
			bool extras = query["withextras"] == "true";
			string[] grants = extras ? extragrants : noextragrants;
			WorkMonthRequest months = WorkMonthRequest.fromQuery(query);
			if (months == null || months.grantids.Length == 0)
			{
				return new Result(false, "Missing grant number or date");
			}
			months.employee = id;
			Dictionary<string, IEnumerable<double>> groupTimes = months.getTimes(grants)
				.ToDictionary(kv => kv.Key, kv => kv.Value);
			if (query["grant"] != null || extras)
			{
				if (extras || query["grant"].Contains("non-grant"))
					renameIfExists(groupTimes, Globals.GrantID_NonGrant.ToString(), "non-grant");
				if (extras || query["grant"].Contains("leave"))
					renameIfExists(groupTimes, Globals.GrantID_Leave.ToString(), "leave");
			}
			if (query["withstatus"] == "true")
			{
				Dictionary<string, string> status = months.grantids.Where(
					grant => !Globals.GrantID_AllSpecial.Contains(grant))
				.ToDictionary(
					grant => WorkMonthRequest.getGrantName(grant),
					grant =>
					{
						GrantMonth rightmonth = months.grantMonths.Where(month => month.grantID == grant).SingleOrDefault();
						return Enum.GetName(typeof(GrantMonth.status), (rightmonth == null)
							? (int)GrantMonth.status.New
							: rightmonth.curStatus);
					});
				
				return new Result(true, new { hours = groupTimes, status = status });
			}
			return new Result(true, groupTimes);
		}

		private Result sendApproval(int id, NameValueCollection query, HttpContext context)
		{
			bool approve;
			if (!bool.TryParse(query["approval"], out approve))
			{
				return new Result(false, "Formatting error in field");
			}

			// this is a terrible thing to be doing
			// so, you know what?  let's not do it
			// that's what I'd like to say, but for now it has to stay in
			int? idFromEmail = (int?)context.Session["WorkMonthID"];
			if (query["id"] == null && idFromEmail.HasValue)
			{
				query = new NameValueCollection(query);
				query["id"] = idFromEmail.ToString();
			}

			WorkMonthRequest queryRequest = WorkMonthRequest.fromQuery(query);
			if (queryRequest == null || queryRequest.grantMonths == null || queryRequest.grantMonths.Count() == 0)
			{
				return new Result(false, "Missing required field or no such entry");
			}
			else
			{
				IEnumerable<GrantMonth> tmpGm = WorkMonthRequest.fromQuery(query).grantMonths;
				// It seems silly not to support both input types, but we have to have the non-grant/leave WorkMonths for approveOrDisapprove()
				// and have no way of ensuring they're included (save for some inner join shenanigans)
				// So instead, let's get one of the GrantMonths we grabbed already and use it to collect the stragglers
				WorkMonthRequest workrequest = WorkMonthRequest.fromGrantMonths(tmpGm, extragrants.Select(eg => int.Parse(eg)));
				IEnumerable<GrantMonth> extraGm = workrequest.grantMonths;
				GrantMonth[] workmonths = tmpGm.Concat(extraGm).GroupBy(month => month.ID).Select(months => months.First()).ToArray();
				// ^ union() would be better but doesn't support lambdas; this is the stackoverflow-approved substitute
				
				string reason = query["comment"];
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
				return new Result(result.success, result.message);
			}
		}

		private Result doUpdateHours(int id, NameValueCollection query)
		{
			string hoursstr = query["hours"];

			WorkMonthRequest workrequest = WorkMonthRequest.fromQuery(query);
			if (workrequest == null || !workrequest.supervisor.HasValue || hoursstr == null)
			{
				return new Result(false, "missing or misformatted required field(s)");
			}
			workrequest.employee = id;

			Dictionary<int, double[]> hoursById = null;
			try
			{
				Dictionary<string, double[]> hours = new JavaScriptSerializer().Deserialize<Dictionary<string, double[]>>(hoursstr);
				hoursById = hours.ToDictionary(
					kv => WorkMonthRequest.parseQueryGrant(kv.Key),
					kv => kv.Value
				);
			}
			catch (Exception)
			{
				return new Result(false, "unable to parse hours");
			}
			//catch (Exception e)
			//{
			//	string result = string.Format("parsing error in hours: {0}\nstack trace: {1}", e.Message, e.StackTrace).Replace("\n", Environment.NewLine);
			//	return new Result(false, result);
			//}
			finally { }

			workrequest.grantids = hoursById.Keys.ToArray();
			Dictionary<string, int> success = updateHours(workrequest, hoursById);
			return new Result(true, success);
		}

		private Dictionary<string, int> updateHours(WorkMonthRequest workrequest, Dictionary<int, double[]> hours)
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
							results[addOrUpdateEntry(conn, workrequest, grantid, day, time, oldhours[dayIndex])]++;
						});
					}
					else
					{
						properLengthMonth.ForEach((time, dayIndex) =>
						{
							int day = dayIndex + 1;
							results[addOrUpdateEntry(conn, workrequest, grantid, day, time, null)]++;
						});
					}
				});
				return results;
			});
		}

		// decide whether to addNewTimeEntry, updateTimeEntry, or neither, and deal with the weirdness of calling them
		// incidentally, neither one will set the supervisor id, but we're accepting it as an argument for future proofiness
		private string addOrUpdateEntry(OleDbConnection conn, WorkMonthRequest req, int grant, int day, double time, TimeEntry oldentry)
		{
			// No, seriously.  addNewTimeEntry() and updateTimeEntry() unconditionally open the connection they're passed.
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
					TimeEntry newentry = new TimeEntry(time, grant, req.employee.Value, req.supervisor.Value, req.month, day, req.year);
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

		private Result sendRequest(int id, NameValueCollection query)
		{
			WorkMonthRequest request = WorkMonthRequest.fromQuery(query);
			if (request == null || !request.supervisor.HasValue || request.grantids.Length != 1)
			{
				return new Result(false, "missing or misformatted required field(s)");
			}
			request.employee = id;

			GrantMonth oldgm = request.grantMonths.FirstOrDefault();
			if (oldgm != null)
			{
				GrantMonth.status status = (GrantMonth.status)oldgm.curStatus;
				if (status != GrantMonth.status.New && status != GrantMonth.status.disapproved)
				{
					return new Result(false, "request is already " + Enum.GetName(typeof(GrantMonth.status), status));
				}
			}

			Dictionary<string, TimeEntry[]> entries = request.getTimeEntries(new string[] { });
			var details = OleDBHelper.withConnection(conn => new {
				emp = OleDBHelper.query(conn, "SELECT * FROM EmployeeList WHERE ID = " + request.employee.Value, Employee.fromRow).Single(),
				grants = OleDBHelper.query(conn, "SELECT * FROM GrantInfo WHERE ID IN "+ OleDBHelper.sqlInArrayParams(request.grantids)
						, Grant.fromRow
						, request.grantids.Select(a=>a.ToString()).ToArray())
			});
			Employee emp = details.emp;
			IEnumerable<Grant> grants = details.grants;
				

			_Default.AssignSupervisorStateless(request.supervisor.Value, request.grantids[0], request.employee.Value,
				entries.SelectMany(kv => kv.Value).Where(entry => entry != null).ToList());

			if (_Default.sendOffEmailStateless(new int[] { request.supervisor.Value }
				, request.employee.Value, new DateTime(request.year, request.month + 1, 1), grants.ToList(), emp))
			{
				return new Result(true, "sent email successfully");
			}
			else
			{
				return new Result(false, "failed to send email");
			}
		}

		private Result getAllSupervisors(int id, NameValueCollection query)
		{
			IEnumerable<SafeEmployee> supervisors = OleDBHelper.query(
				"SELECT * FROM EmployeeList WHERE manager = true AND registered = true"
				, SafeEmployee.fromRow).GroupBy(emp => emp.firstname + emp.lastname).Select(emps => emps.First());
			return new Result(true, supervisors);
		}

		private Result doLogin(HttpContext context, string empId, string pass)
		{
            if (empId == null || pass == null) {
                return new Result(false, "missing required field");
            }
            Employee emp = OleDBHelper.query<Employee>(
                "SELECT * FROM EmployeeList WHERE registered = true AND EmployeeNum = ?",
                Employee.fromRow,
                empId
            ).SingleOrDefault();
            if (!emp.registered)
            {
                return new Result(false, "employee not registered");
            }
			if (!Employee.TestPassword(emp, context.Request.QueryString["pass"]))
			{
				return new Result(false, "Wrong ID or password");
			}
			else
			{
				context.Session["ID"] = emp.ID;
				return new Result(true, new SafeEmployee(emp));
			}
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

		private String getFortune()
		{
			string wd = HostingEnvironment.MapPath("~/.");
			string[] fortunes = System.IO.File.ReadAllText(wd + "/fortune.txt").Split('%');
			string fortune = fortunes[new Random().Next(fortunes.Length)];
			fortune = Regex.Replace(fortune, "[\n\r\t ]+", " ");
			return fortune;
		}

		// this has to be built in, but I can't find it for the life of me
		IEnumerable<T> paddingValue<T>(T val)
		{
			while (true)
				yield return val;
		}

		class Result
		{
			public bool success;
			public object message;
			public Result(bool success, object message)
			{
				this.success = success;
				this.message = message;
			}
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
