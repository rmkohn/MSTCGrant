The web interface is accessible from `<grant app root>/android?q=<command>`  
All arguments go in the query string; order doesn't matter.

The commands available so far are

- [`login`](#login) `id pass`  
- [`listemployees`](#listemployees) 
- [`listgrants`](#listgrants) `[grant] [employee] [status=new|pending|approved|disapproved|final_approved]`
- [`listrequests`](#listrequests)
- [`viewrequest`](#viewrequest) `(employee year month | id) [grant] [withextras]`
- [`approve`](#approve) `[(employee grant year month | id)] approve [reason]`
- [`logout`](#logout)
- [`debug`](#debug)
- [`email`](#email) `(employee grant year month | id)`
- [`updatehours`](#updatehours) `supervisor year month hours`
- [`listallgrants`](#listallgrants)
- [`sendrequest`](#sendrequest) `year month grant supervisor`

----

### Details
<div id="login"/>

#### login

> Arguments: employee ID number, password (yes, it's terrible to have them in the query string like that, but as the site isn't signed up for https, it's kind of a moot point)  

> This and [key](#key) are the only commands that will run without being logged in, everything else will issue "Invalid Session ID" errors.  
Speaking of which, hold onto any ASP.NET_SessionId cookie you get, you'll need it.

<div id="login"/>
#### listemployees
> No arguments.

> List all employees that have the logged-in user as their default supervisor.  This is used to determine who they can impersonate in the online app, but unfortunately,  impersonation hasn't been implemented yet.

<div id="listgrants"/>
#### listgrants
> No arguments.

> List all the grants for which the logged-in user is listed as the grant manager.

<div id="listrequests"/>
#### listrequests
> Optional arguments: grant ID, employee ID, status (one of new, pending, approved, disapproved, or final_approved)  

> List all grant approval requests submitted to the logged-in user that meet the given criteria

<div id="viewrequest"/>
#### viewrequest
> Arguments: one or more comma-separated grant IDs, employee ID, four-digit year, any-digit month  
Alternative arguments: grant request ID, zero or more comma separated grants (optional)  
Optional arguments: `withextras=true` adds the special grants for non-grant time and leave time to your request and returns them in the arrays `non-grant` and `leave`.  

> Get the gory details of one or more grants.  The json object returned is formatted like { grant id: [array-of-doubles] }.  
Passing `non-grant` and/or `leave` as grant ids will work, so the `withextras` argument isn't even needed.
(Non-grant time and leave are both treated as grants that just happen to have special meanings, they have IDs 52 and 53 respectively.  There's also a special placeholder value with ID 28, which shouldn't ever be needed.)

<div id="approve"/>
#### approve
> Arguments: approval (boolean: whether the user is approving or rejecting this grant request)  
Optional arguments: reason for approval/disapproval, grant request ID or employee ID, year, and month  

> Send an approval/disapproval email concerning a particular grant request.  If you've used the `email` command during this session, and no grant request is provided, the email request will be used instead.  

<div id="logout"/>
#### logout
> No arguments.  

> Logs you out.

<div id="key"/>
#### key
> This is actually an optional argument to the other commands.  It's meant to be used in emails, to log in as the recipient and provide some information about the grant request.  (The current emails allow users pretty much free rein).  
But none of that actually exists yet, so it just logs you in as whomever's ID you provide.

<div id="debug"/>
#### debug
> No arguments.

> Dump all the information the server is holding about your current session.  At present this is just the logged-in user's ID.

<div id="email"/>
#### email
> Arguments: id

> This should be called with the id provided in a grant approval request email.  `message` in the server response holds a json object with `month`, `year`, `supervisor` (itself a json object with entries for `firstname` `lastname` and `id`), `employee` (ditto), `status` (as per `listgrants`), `grant`, and `hours` (as per `viewrequest` with `withextras=true`, except that the hours for the grant will be under `grant` instead of the grant ID)  

<div id="updatehours"/>
#### updatehours
> Arguments: employee, year, month, hours

> `Hours` should be a JSON object with the same format as is passed back by viewrequest; use the IDs of whichever grants you'd like to update, as well as `nongrant` and `leave`, as keys pointing to arrays of doubles holding the new times for the month.  Any fields left out are left alone (they're all optional).  Arrays that don't fit the length of the month are currently truncated or padded with zeroes as appropriate, but that should probably be changed.  
> The result passed back is a JSON object containing the number of TimeEntry fields that were added, changed, and left unchanged.  

<div id="listallgrants"/>
#### listallgrants
> No arguments.

> Return information about every grant in the database.  This is intended to provide a list of grants for the time-entry app.

<div id="sendrequest"/>
#### sendrequest
> Arguments: year, month, grant, supervisor

> Send an "approval required" email.  You still need to have a supervisor ID from somewhere, and I'm not yet sure how the web app decides which ones to show you.
