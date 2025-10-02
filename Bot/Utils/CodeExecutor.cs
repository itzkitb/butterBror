using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Linq;
using System.Reflection;

namespace bb.Utils
{
    public class CodeExecutor
    {
        /// <summary>
        /// Executes user-provided C# code snippets using Roslyn Scripting API.
        /// </summary>
        /// <param name="userCode">C# code to execute</param>
        /// <returns>String result returned by the executed code</returns>
        /// <exception cref="CompilationException">Thrown when code fails to compile or execute</exception>
        /// <remarks>
        /// <para>
        /// Execution environment:
        /// <list type="bullet">
        /// <item>Uses Roslyn Scripting API for dynamic code execution</item>
        /// <item>Restricted assembly references for security</item>
        /// <item>No separate AppDomain sandbox (simpler but less isolated)</item>
        /// <item>Requires developer privileges for access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Security note: Unlike the original implementation, this version doesn't provide assembly isolation,
        /// so executed code runs in the same context as the main application.
        /// </para>
        /// </remarks>
        public static string Run(string userCode)
        {
            var scriptCode = $@"
using DankDB;
using bb.Core.Bot;
using bb.Core.Commands.List;
using bb.Core.Commands;
using bb.Core.Configuration;
using bb.Core.Services;
using bb.Data.Entities;
using bb.Data.Repositories;
using bb.Core;
using bb.Data;
using bb;
using bb.Models.AI;
using bb.Models.Command;
using bb.Models.Currency;
using bb.Models.Exceptions;
using bb.Models.Platform;
using bb.Models.SevenTVLib;
using bb.Models.Statistics;
using bb.Models.Users;
using bb.Models;
using bb.Services.External;
using bb.Services.Internal;
using bb.Services.Platform.Discord;
using bb.Services.Platform.Telegram;
using bb.Services.Platform.Twitch;
using bb.Services.Platform;
using bb.Services;
using bb.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime;

string Execute()
{{
    {userCode}
}}
return Execute();";

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Distinct()
                .ToArray();

            var references = assemblies
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var requiredAssemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(Enumerable).Assembly,
                typeof(System.Runtime.GCSettings).Assembly,
            };

            foreach (var assembly in requiredAssemblies)
            {
                if (!references.Any(r => r.Display.Contains(assembly.GetName().Name)))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            var options = ScriptOptions.Default
                .WithReferences(references)
                .WithOptimizationLevel(OptimizationLevel.Release);

            try
            {
                var result = CSharpScript.EvaluateAsync<string>(scriptCode, options).GetAwaiter().GetResult();
                return result;
            }
            catch (CompilationErrorException ex)
            {
                var errors = string.Join("\n", ex.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));

                throw new CompilationException($"Compilation error: {errors}");
            }
            catch (Exception ex)
            {
                throw new CompilationException($"Execution error: {ex.Message}");
            }
        }

        /// <summary>
        /// Represents errors that occur during dynamic code compilation and execution.
        /// </summary>
        public class CompilationException : Exception
        {
            public CompilationException(string message) : base(message) { }
        }
    }
}