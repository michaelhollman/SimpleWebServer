using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace WebServer
{
    public class CWebTemplateProcessor : IScriptProcessor
    {
        private enum StatementType
        {
            Code,
            Expression,
            Html
        }

        private ICodeCompiler _compiler;
        private CompilerParameters _parameters;

        private const string _classTemplate = "using System;" +
                                              "namespace Server {" +
                                              "public class Executor {" +
                                              "public void Execute(System.Collections.Generic.Dictionary<string, string> request, string[] __outputExpressionValues__) {" +
                                              "{0} } } }";

        public CWebTemplateProcessor()
        {
            var provider = new CSharpCodeProvider();
            _compiler = provider.CreateCompiler();
            _parameters = new CompilerParameters();
            _parameters.ReferencedAssemblies.Add("system.dll");
            _parameters.GenerateInMemory = true;
            _parameters.CompilerOptions = "/t:library";
        }


        public ScriptResult ProcessScript(string path, IDictionary<string, string> requestParameters)
        {
            // Phase 0
            var statements = new List<Statement>();
            var statementBuilder = new StringBuilder();
            var htmlBuilder = new StringBuilder();
            var codeBuilder = new StringBuilder();
            var expressionCount = 0;

            // Phase 1: split file into statements
            var text = File.ReadAllText(path);
            // loop over every character in the file and split into statements
            var openCurlyCount = 0;
            var currentType = StatementType.Html;
            for (var i = 0; i < text.Length; i++)
            {
                var currentChar = text[i];

                if (currentChar == '{')
                {
                    openCurlyCount++;
                    if (currentType == StatementType.Html)
                    {
                        statements.Add(new Statement(statementBuilder.ToString(), currentType));
                        statementBuilder = new StringBuilder();
                        currentType = StatementType.Code;
                    }
                    else
                    {
                        statementBuilder.Append(currentChar);
                    }
                }
                else if (currentChar == '}')
                {
                    if (openCurlyCount > 0)
                    {
                        openCurlyCount--;
                    }

                    if (currentType != StatementType.Html && openCurlyCount == 0)
                    {
                        statements.Add(new Statement(statementBuilder.ToString(), currentType));
                        statementBuilder = new StringBuilder();
                        currentType = StatementType.Html;
                    }
                    else
                    {
                        statementBuilder.Append(currentChar);
                    }
                }
                else if (currentChar == '@' && i + 1 < text.Length && text[i + 1] == '{')
                {
                    if (currentType == StatementType.Html)
                    {
                        // increment i to skip '{'
                        i++;
                        openCurlyCount++;

                        statements.Add(new Statement(statementBuilder.ToString(), currentType));
                        statementBuilder = new StringBuilder();
                        currentType = StatementType.Expression;
                    }
                    else
                    {
                        statementBuilder.Append(currentChar);
                    }
                }
                else
                {
                    statementBuilder.Append(currentChar);
                }
            }

            // just in case there's some unfinished statement
            if (statementBuilder.Length > 0)
            {
                statements.Add(new Statement(statementBuilder.ToString(), currentType));
            }

            // Phase 2: split statements into html and code
            foreach (var statement in statements)
            {
                if (statement.Type == StatementType.Html)
                {
                    htmlBuilder.Append(statement.Text);
                }
                else if (statement.Type == StatementType.Code)
                {
                    codeBuilder.Append(statement.Text);
                }
                else // StatementType.Expression
                {
                    // string.Format won't let you format {{#}}
                    htmlBuilder.Append(new StringBuilder().Append('{').Append(expressionCount).Append('}'));
                    codeBuilder.AppendLine(string.Format("__outputExpressionValues__[{0}]=({1}).ToString();", expressionCount, statement.Text));
                    expressionCount++;
                }
            }

            // Phase 3: run code. taken from CScriptProcessor
            var source = _classTemplate.Replace("{0}", codeBuilder.ToString());
            var result = _compiler.CompileAssemblyFromSource(_parameters, source);
            if (result.Errors.Count > 0)
            {
                var errorBody = new StringBuilder();
                errorBody.Append("<html><body>");
                errorBody.Append("<h1>Script Compilation Errors</h1>");
                errorBody.Append("<p>The following errors occurred processing the requested resource</p>");
                errorBody.Append("<ul>");
                foreach (CompilerError error in result.Errors)
                {
                    errorBody.Append(string.Format("<li>{0}:{1} - Error: {2}</li>", error.Line, error.Column, error.ErrorText));
                }
                errorBody.Append("</ul>");
                errorBody.Append("</body></html>");

                return new ScriptResult()
                {
                    Error = true,
                    Result = errorBody.ToString()
                };
            }

            var codeAssembly = result.CompiledAssembly;
            var instance = codeAssembly.CreateInstance("Server.Executor");
            var instanceType = instance.GetType();
            var executionMethod = instanceType.GetMethod("Execute", new Type[] { typeof(Dictionary<string, string>), typeof(string[]) });

            var outputExpressions = new String[expressionCount];
            try
            {
                executionMethod.Invoke(instance, new object[] { requestParameters, outputExpressions });
            }
            catch (Exception e)
            {
                return new ScriptResult()
                {
                    Error = true,
                    Result = string.Format("<html><body><h1>Runtime Error</h1><p>The following runtime error occurred:</p><p>{0}</p></body></html>", e.InnerException != null ? e.InnerException.Message : e.Message)
                };
            }

            // Phase 4: put expression values into html
            return new ScriptResult
            {
                Error = false,
                Result = string.Format(htmlBuilder.ToString(), outputExpressions)
            };
        }

        private class Statement
        {
            public string Text { get; set; }
            public StatementType Type { get; set; }

            public Statement(string text, StatementType type)
            {
                Text = text;
                Type = type;
            }
        }
    }
}
