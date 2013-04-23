using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GrantApplication
{
    /// <summary>
    /// Summary description for Handler1
    /// </summary>
    public class Handler1 : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.RedirectPermanent("grantapp://approval-app/" + context.Request.QueryString, true);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}