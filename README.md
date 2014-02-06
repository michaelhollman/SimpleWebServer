This project was originally forked from [icotting/SimpleWebServer](https://github.com/icotting/SimpleWebServer).

---

# Simple Server

Simple Server is a very basic HTTP server that supports a tiny sub-set of the HTTP specification in order for students to gain a better understanding of how the web works. Simple Server supports the following basic features: 

1. Accepts HTTP GET requests
	+ allows user definition of the server web root
	+ allows user definition of the port number
	+ allows user definition of the default document
	+ supports sub-directory sites

2. Can handle response types for:
	+ HTTP OK
	+ HTTP NOT FOUND
	+ HTTP METHOD NOT ALLOWED
	+ HTTP INTERNAL SERVER ERROR

3. Accepts GET request parameters

4. Serves basic static mime-types:
	+ various images (image/*) jpg, jpeg, png, gif, bmp, tiff
	+ html (text/html) htm html
	+ all other files will be served as application/octet-stream 

5. Support for dynamic web pages from basic C# scripts or web templates
	+ the server will notify the user of compilation and runtime errors for processing scripts and templates in the response body of an HTTP Internal Server Error response

### CScripts
So called CScripts will be processed to generate HTML content for a response. The following is a simple example of a CScript:

```
string foo = "My Great Script!!!";
wout.WriteLine("<html>");
wout.WriteLine("<body>");
wout.WriteLine("<h1>");
wout.WriteLine(foo);
wout.WriteLine("</h1>");
DateTime dt = DateTime.Now;
wout.WriteLine(string.Format("<p>The current time is {0}</p>", dt.ToString("MMM dd, yyyy hhh:mm:ss")));

string val = "not provided";

try {
	val = request["user"];
} catch ( Exception e ) {}

wout.WriteLine("The value of parameter user is {0}", val);
```

The server will provide an outputstream `wout` and the request parameter dictionary `request`. The `using` keyword is not supported, all classes must be fully referenced. The server will support use of any APIs in the System namespace. Class and method declarations are not supported.

### CWebTemplates
So called CWebTemplates are basic web templates that allow C# to be embedded directly into HTML and processed by the server to generate a single HTML document. The following is a sample CWebTemplates:

```
<html>
	<body>
		{
			string foo = "My Great Template!!!";
			DateTime dt = DateTime.Now;
		}
		<h1>@{foo}</h1>
		<p>The current time is @{dt.toString("MMM dd, yyyy hh:mm:ss")}</p>

		<span>The value of the parameter user is</span> @{request["user"]}
	</body>
</html>
```

The server will evaluate any C# code found between a set of curly braces `{}`. Any SINGLE EXPRESSION found in `@{}` blocks will be written to the outputstream. The variable `wout` is not available to CWebTemplates as it is for CScripts. The `request` dictionary is made available.

*Note:* C# comments are not fully supported in that any `{`, `}`, or `@{`s within comments have the potential the parsing algorithm to get wonderfully confused.
