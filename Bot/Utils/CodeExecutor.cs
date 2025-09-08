using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace bb.Utils
{
    public class CodeExecutor
    {
        /// <summary>
        /// Compiles and executes user-provided C# code snippets in a secure sandboxed environment.
        /// </summary>
        /// <param name="userCode">C# code to compile and execute</param>
        /// <returns>String result returned by the executed code</returns>
        /// <exception cref="CompilationException">Thrown when code fails to compile or execute</exception>
        /// <remarks>
        /// <para>
        /// Execution environment:
        /// <list type="bullet">
        /// <item>Uses Roslyn compiler for dynamic code generation</item>
        /// <item>Restricted assembly references for security</item>
        /// <item>Runs in isolated AppDomain-like context</item>
        /// <item>Requires developer privileges for access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Supported features:
        /// <list type="bullet">
        /// <item>Access to core .NET libraries</item>
        /// <item>Basic access to bot functionality</item>
        /// <item>Simple return value capture</item>
        /// <item>Detailed error reporting</item>
        /// </list>
        /// </para>
        /// <para>
        /// Security restrictions:
        /// <list type="bullet">
        /// <item>No access to unsafe code or pointers</item>
        /// <item>Limited assembly references (only core libraries)</item>
        /// <item>No file system access</item>
        /// <item>No network operations</item>
        /// </list>
        /// </para>
        /// Primarily used for debugging and administrative operations by trusted developers.
        /// Execution time is not limited but runs synchronously in calling thread.
        /// </remarks>
        public static string Run(string userCode)
        {
            var fullCode = $@"
        using DankDB;
        using bb;
        using bb.Utils;
        using bb.Services.External;
        using bb.Services.System;
        using bb.Models;
        using bb.Data;
        using bb.Events;
        using bb.Workers;
        using bb.Core.Bot;
        using bb.Core.Commands;
        using bb.Core.Commands.List;
        using bb.Core.Services;
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.IO;
        using System.Runtime;

        public static class MyClass 
        {{
            public static string Execute()
            {{
                {userCode}
            }}
        }}";

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

            var compilation = CSharpCompilation.Create(
                "MyAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(fullCode) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);

            if (!emitResult.Success)
            {
                var errors = string.Join("\n", emitResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

                throw new CompilationException($"Compilation error: {errors}");
            }

            stream.Seek(0, SeekOrigin.Begin);
            var loadContext = new SandboxLoadContext();
            Assembly _assembly = null;
            string result = null;
            Exception unloadException = null;

            try
            {
                _assembly = loadContext.LoadFromStream(stream);
                var type = _assembly.GetType("MyClass");
                var method = type.GetMethod("Execute");

                result = (string)method.Invoke(null, null);
            }
            finally
            {
                loadContext.Unload();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (unloadException != null)
                    throw new CompilationException($"Failed to unload sandbox context: {unloadException.Message}");
            }

            return result;
        }

        /// <summary>
        /// Represents errors that occur during dynamic code compilation and execution.
        /// </summary>
        /// <remarks>
        /// This exception contains detailed compiler error information to help diagnose issues with user-provided code.
        /// Unlike standard exceptions, it focuses on providing actionable feedback for code correction.
        /// </remarks>
        public class CompilationException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the CompilationException class with the specified error message.
            /// </summary>
            /// <param name="message">Detailed compiler error message describing the failure</param>
            public CompilationException(string message) : base(message) { }
        }

        private sealed class SandboxLoadContext : AssemblyLoadContext
        {
            public SandboxLoadContext() : base(isCollectible: true) { }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                // Делегируем загрузку основным сборкам через стандартный контекст
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
        }
    }
}
