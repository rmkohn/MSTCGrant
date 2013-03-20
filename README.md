The web interface is accessible from `<grant app root>/android?q=<command>`  
All arguments go in the query string; order doesn't matter.

The commands available so far are

- [`login`](#login) `id pass`  
- [`listemployees`](#listemployees) 
- [`listgrants`](#listgrants) `[grant] [employee] [status=new|pending|approved|disapproved|final_approved]`
- [`listrequests`](#listrequests)
- [`viewrequest`](#viewrequest) `grant employee year month`
- [`approve`](#approve)
- [`logout`](#logout)

----

### Details
<div id="login"/>

#### login

> Arguments: employee ID number, password (yes, it's terrible to have them in the query string like that, but as the site isn't signed up for https, it's kind of a moot point)  
This and [key](#key) are the only commands that will run without being logged in, everything else will issue "Invalid Session ID" errors.  
Speaking of which, hold onto any ASP.NET_SessionId cookie you get, you'll need it.

<div id="login"/>
#### listemployees
> No arguments.
List all employees that have the logged-in user as their default supervisor.  This is used to determine who they can impersonate in the online app, but unfortunately,  impersonation hasn't been implemented yet.

<div id="listgrants"/>
#### listgrants
> List all the grants for which the logged-in user is listed as the grant manager.

<div id="listrequests"/>
#### listrequests
> Optional arguments: grant ID, employee ID, status (one of new, pending, approved, disapproved, or final_approved)  
List all grant approval requests submitted to the logged-in user that meet the given criteria

<div id="viewrequest"/>
#### viewrequest
> Arguments: one or more comma-separated grant IDs, employee ID, four-digit year, any-digit month  
Optional arguments: additional comma-separated grants  
Get the gory details of one or more grants.  The json object returned is formatted like { grant id: [array-of-doubles] }.  
Non-grant time and leave are both treated as grants that just happen to have special meanings, they have IDs 52 and 53 respectively.  There's also a special placeholder value with ID 28, but you shouldn't ever need it.

<div id="approve"/>
#### approve
> Arguments: grantid employeeid approve (boolean) month year [reason]  
Send an approval/disapproval email concerning a particular grant request.  Only it doesn't use a grant request, it does it the hard way.
So ignore this for now, please.

<div id="logout"/>
#### logout
> No arguments.  
Does what it says on the tin.

<div id="key"/>
#### key
> This is actually an optional argument to the other commands.  It's meant to be used in emails, to log in as the recipient and provide some information about the grant request.  (The current emails allow users pretty much free reign).  
But none of that actually exists yet, so it just logs you in as whomever's ID you provide.