using System;
using System.Web;

namespace GrantApplication
{
	public class _301Handler : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext context)
		{
			context.Response.RedirectPermanent("grantapp://approval-app?" + context.Request.QueryString, true);
		}
	}
}
