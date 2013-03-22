﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace GrantApplication
{
	// if this gets any more complex it will definitely be time to bring in an external library
	// honestly it's what I should have done to begin with
	// but for now, <100 lines of fairly accessible code seems preferable to ~5000 incomprehensible
	// lines from a random external dependency
	// even if it's less comprehensive and almost certainly slower
	public class OleDBHelper
	{
		private OleDBHelper() { }

		static public T withConnection<T>(Func<OleDbConnection, T> fn)
		{
			OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
			conn.Open();
			T ret = fn(conn);
			conn.Close();
			return ret;
		}

		static public IEnumerable<T> query<T>(String querystr, IEnumerable<string> parameters, Func<DataRow, T> selector)
		{
			return withConnection(conn => query(querystr, parameters, selector, conn));
		}

		static public int nonQuery(String statement, IEnumerable<string> parameters)
		{
			return withConnection(conn => nonQuery(statement, parameters, conn));
		}

		//static public IEnumerable<Dictionary<string,T>> multiQuery<T>(String querystr, IEnumerable<string> parameters,
		//	Dictionary<string, Func<DataRow, T>> selectors)
		//{
		//	return withConnection(conn => multiQuery(querystr, parameters, selectors, conn));
		//}

		static public DataSet fillDataSet(String query, IEnumerable<string> parameters, OleDbConnection conn)
		{
			DbDataAdapter adapter = new OleDbDataAdapter();
			DbCommand cmd = new OleDbCommand(query, conn);
			parameters.ForEach(val =>
				cmd.Parameters.Add(new OleDbParameter(null, val))
			);
			DataSet set = new DataSet();
			adapter.SelectCommand = cmd;

			adapter.Fill(set);
			return set;
		}


		// make a parameterized query, map the returned DataRows through a provided function, and return the result
		static public IEnumerable<T> query<T>(String query, IEnumerable<string> parameters, Func<DataRow, T> selector, OleDbConnection conn)
		{
			DataSet set = fillDataSet(query, parameters, conn);
			return set.Tables[0].Rows.Flatten<DataRow>().Select(selector);
		}

		// Results don't get turned into separate tables, so this is worthless.
		//static public IEnumerable<Dictionary<string, T>> multiQuery<T>(String query, IEnumerable<string> parameters,
		//	Dictionary<string, Func<DataRow, T>> selectors, OleDbConnection conn)
		//{

		//	DataSet set = fillDataSet(query, parameters, conn);
		//	List<Dictionary<string, T>> result = new List<Dictionary<string, T>>(set.Tables[0].Rows.Count);
		//	for (int i = 0; i < set.Tables[0].Rows.Count; i++)
		//	{
		//		result.Add(selectors.ToDictionary(
		//			kv => kv.Key,
		//			kv =>kv.Value(set.Tables[kv.Key].Rows[i])
		//		));
		//	}
		//	return result;
		//}

		static public int nonQuery(String statement, IEnumerable<string> parameters, OleDbConnection conn)
		{
			DbCommand cmd = new OleDbCommand(statement, conn);
			parameters.ForEach(val =>
				cmd.Parameters.Add(new OleDbParameter(null, val))
			);

			return cmd.ExecuteNonQuery();
		}

		// add " AND sqlkey[i] = ?" type conditions to a SELECT query, for the non-null entries in sqlvals
		// returns the new query string directly, and the non-null values in sqlparams
		static public String appendConditions(string sqlquery, string[] sqlkeys, string[] sqlvals, out IEnumerable<string> sqlparams)
		{
			IEnumerable<Tuple<string, string>> sqlkv = sqlkeys
				.Zip(sqlvals, (key, val) => new Tuple<string, string>(key, val))
				.Where(kv => kv.Item2 != null);
			sqlkv.ForEach(kv => sqlquery += " AND " + kv.Item1 + " = ?");
			sqlparams = sqlkv.Select(kv => kv.Item2);
			return sqlquery;
		}

		// fill out (?)s for an IN (?, ...) statement on the provided array
		static public string sqlInArrayParams(object[] inarray)
		{
			return "(" + string.Join(", ", inarray.Select(item => "?")) + ")";
		}
	}
}