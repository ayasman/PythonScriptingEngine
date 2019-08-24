using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace ScriptingEngine
{
    public abstract class ScriptingEngineBase : IScriptingEngine
    {
        protected Dictionary<Type, Dictionary<string, object>> registeredScriptObjects = new Dictionary<Type, Dictionary<string, object>>();

        public virtual void Initialize()
        {
            registeredScriptObjects.Clear();
        }

        public abstract void LoadScripts(string directory);

        public virtual void RegisterScript(object newObject)
        {
            if (newObject is IRegisterableScript)
            {
                if (!registeredScriptObjects.ContainsKey(typeof(IRegisterableScript)))
                    registeredScriptObjects.Add(typeof(IRegisterableScript), new Dictionary<string, object>());

                registeredScriptObjects[typeof(IRegisterableScript)].Add(((IRegisterableScript)newObject).Name, newObject);
            }
        }

        public void ExecuteScript<T>(string name, object dataContext)
        {

            var scriptObj = registeredScriptObjects[typeof(T)];
            if (scriptObj is IRegisterableScript)
                ((IRegisterableScript)scriptObj).Execute(dataContext);
        }

        protected static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }

    public class CSharpScriptingEngine : ScriptingEngineBase
    {
        private readonly MetadataReference[] references = null;

        public CSharpScriptingEngine()
        {
            var refPaths = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                typeof(IScriptingEngine).GetTypeInfo().Assembly.Location,
                Assembly.GetExecutingAssembly().Location,
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
            };
            references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
        }

        public override void Initialize()
        {
            base.Initialize();
            //throw new NotImplementedException();
        }

        public override void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
                    //scriptRegistry.Add(file, pythonEngine.ExecuteFile(file, engineScope));
                    LoadAndExecuteRegister(File.ReadAllText(file));
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void LoadAndExecuteRegister(string scriptText)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptText);
            string assemblyName = System.IO.Path.GetRandomFileName();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new System.IO.MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    System.Collections.Generic.IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    var type = typeof(IRegisterableScript);
                    var types = assembly
                        .GetTypes()
                        .Where(p => type.IsAssignableFrom(p));

                    foreach (var t in types)
                    {
                        var instance = assembly.CreateInstance(t.FullName);
                        RegisterScript(instance);
                    }
                }
            }
        }
    }

    public class IronPythonScriptingEngine : ScriptingEngineBase
    {
        private ScriptEngine pythonEngine;
        private ScriptScope engineScope;

        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, object> globalObjects = new Dictionary<string, object>();
            globalObjects.Add("ScriptingEngine", this);

            pythonEngine = Python.CreateEngine();
            pythonEngine.Runtime.LoadAssembly(Assembly.GetExecutingAssembly());
            engineScope = pythonEngine.CreateScope(globalObjects);
        }

        public override void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
                    //scriptRegistry.Add(file, pythonEngine.ExecuteFile(file, engineScope));
                    pythonEngine.ExecuteFile(file, engineScope);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void LoadAndExecuteRegister(string scriptText)
        {
            pythonEngine.Execute(scriptText, engineScope);
        }
    }

    public class Class1
    {
        public void Initialize()
        {
            string codeToCompile = @"
            using System;
            using ScriptingEngine;
            namespace RoslynCompileSample
            {
                public class Writer : IRegisterableScript
                {
                    public void Write(string message)
                    {
                        Console.WriteLine($""you said '{message}!'"");
                    }

                    public string Name 
                    { 
                        get
                        {
                            return string.Empty;
                        }
                    }

                    public void Execute()
                    {
                    }
                }
            }";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

            string assemblyName = System.IO.Path.GetRandomFileName();
            var refPaths = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                typeof(IScriptingEngine).GetTypeInfo().Assembly.Location,
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new System.IO.MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    System.Collections.Generic.IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    var type = typeof(IRegisterableScript);
                    var types = assembly
                        .GetTypes()
                        .Where(p => type.IsAssignableFrom(p));

                    foreach (var t in types)
                    {
                        IRegisterableScript instance = assembly.CreateInstance(t.FullName) as IRegisterableScript;

                    }
                    //var type = assembly.GetType("RoslynCompileSample.Writer");
                    //var instance = assembly.CreateInstance("RoslynCompileSample.Writer");
                    //var meth = type.GetMember("Write").First() as MethodInfo;
                    //meth.Invoke(instance, new[] { "joel" });
                }
            }
        }
    }
}
