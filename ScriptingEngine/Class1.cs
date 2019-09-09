using IronPython.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace ScriptingEngine
{
    //public class CSharpScriptingEngine : ScriptingEngineBase
    //{
    //    private MetadataReference[] references = null;

    //    public CSharpScriptingEngine() :
    //        base()
    //    {
    //    }

    //    public override void Initialize()
    //    {
    //        base.Initialize();

    //        foreach (var asm in RegisteredAssemblies)
    //            references = RegisteredAssemblies.Select(r => MetadataReference.CreateFromFile(r.Location)).ToArray();
    //    }

    //    public override void LoadScripts(string path)
    //    {
    //        base.LoadScripts(path);

    //        foreach (var file in GetFiles(path))
    //        {
    //            try
    //            {
    //                LoadAndExecuteRegister(File.ReadAllText(file));
    //            }
    //            catch (Exception ex)
    //            {
    //            }
    //        }
    //    }

    //    public void LoadAndExecuteRegister(string scriptText)
    //    {
    //        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptText);
    //        string assemblyName = System.IO.Path.GetRandomFileName();

    //        CSharpCompilation compilation = CSharpCompilation.Create(
    //            assemblyName,
    //            syntaxTrees: new[] { syntaxTree },
    //            references: references,
    //            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    //        using (var ms = new System.IO.MemoryStream())
    //        {
    //            EmitResult result = compilation.Emit(ms);

    //            if (!result.Success)
    //            {
    //                System.Collections.Generic.IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
    //                    diagnostic.IsWarningAsError ||
    //                    diagnostic.Severity == DiagnosticSeverity.Error);

    //                foreach (Diagnostic diagnostic in failures)
    //                {
    //                    Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
    //                }
    //            }
    //            else
    //            {
    //                ms.Seek(0, System.IO.SeekOrigin.Begin);
    //                Assembly assembly = Assembly.Load(ms.ToArray());

    //                var type = typeof(IRegisterableScript);
    //                var types = assembly
    //                    .GetTypes()
    //                    .Where(p => type.IsAssignableFrom(p));

    //                foreach (var t in types)
    //                {
    //                    var instance = assembly.CreateInstance(t.FullName);
    //                    RegisterScript(instance);
    //                }
    //            }
    //        }
    //    }
    //}

    //public class Class1
    //{
    //    public void Initialize()
    //    {
    //        string codeToCompile = @"
    //        using System;
    //        using ScriptingEngine;
    //        namespace RoslynCompileSample
    //        {
    //            public class Writer : IRegisterableScript
    //            {
    //                public void Write(string message)
    //                {
    //                    Console.WriteLine($""you said '{message}!'"");
    //                }

    //                public string Name
    //                {
    //                    get
    //                    {
    //                        return string.Empty;
    //                    }
    //                }

    //                public void Execute()
    //                {
    //                }
    //            }
    //        }";

    //        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

    //        string assemblyName = System.IO.Path.GetRandomFileName();
    //        var refPaths = new[] {
    //            typeof(System.Object).GetTypeInfo().Assembly.Location,
    //            typeof(Console).GetTypeInfo().Assembly.Location,
    //            typeof(IScriptingEngine).GetTypeInfo().Assembly.Location,
    //            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
    //        };
    //        MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

    //        CSharpCompilation compilation = CSharpCompilation.Create(
    //            assemblyName,
    //            syntaxTrees: new[] { syntaxTree },
    //            references: references,
    //            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    //        using (var ms = new System.IO.MemoryStream())
    //        {
    //            EmitResult result = compilation.Emit(ms);

    //            if (!result.Success)
    //            {
    //                System.Collections.Generic.IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
    //                    diagnostic.IsWarningAsError ||
    //                    diagnostic.Severity == DiagnosticSeverity.Error);

    //                foreach (Diagnostic diagnostic in failures)
    //                {
    //                    Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
    //                }
    //            }
    //            else
    //            {
    //                ms.Seek(0, System.IO.SeekOrigin.Begin);
    //                Assembly assembly = Assembly.Load(ms.ToArray());

    //                var type = typeof(IRegisterableScript);
    //                var types = assembly
    //                    .GetTypes()
    //                    .Where(p => type.IsAssignableFrom(p));

    //                foreach (var t in types)
    //                {
    //                    IRegisterableScript instance = assembly.CreateInstance(t.FullName) as IRegisterableScript;

    //                }
    //                //var type = assembly.GetType("RoslynCompileSample.Writer");
    //                //var instance = assembly.CreateInstance("RoslynCompileSample.Writer");
    //                //var meth = type.GetMember("Write").First() as MethodInfo;
    //                //meth.Invoke(instance, new[] { "joel" });
    //            }
    //        }
    //    }
    //}
}