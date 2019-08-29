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
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using CSScriptLibrary;

namespace ScriptingEngine
{
    internal class ScriptData
    {

    }

    public abstract class ScriptingEngineBase : IScriptingEngine, IDisposable
    {
        protected Subject<string> onRegisteredSubject = new Subject<string>();
        protected Dictionary<string, dynamic> registeredScriptObjects = new Dictionary<string, dynamic>();
        protected List<Assembly> registeredAssemblies = new List<Assembly>();
        private List<FileSystemWatcher> directoryWatchers = new List<FileSystemWatcher>();

        public IObservable<string> WhenScriptRegistered => onRegisteredSubject.Publish().RefCount();

        public ScriptingEngineBase()
        {
            
        }

        public virtual void Initialize()
        {
            registeredScriptObjects.Clear();

            AppDomain.CurrentDomain.GetAssemblies().Distinct().ToList().ForEach(asm => RegisterAssembly(asm));
        }

        protected List<Assembly> RegisteredAssemblies => registeredAssemblies;

        public void RegisterAssembly(Assembly refAssembly)
        {
            if (!registeredAssemblies.Contains(refAssembly))
                registeredAssemblies.Add(refAssembly);
        }

        public abstract void LoadScripts(string directory);

        public virtual void RegisterScript(dynamic newObject)
        {
            if (newObject is IRegisterableScript)
            {
                string key = ((IRegisterableScript)newObject).Name;
                if (!registeredScriptObjects.ContainsKey(key))
                    registeredScriptObjects.Add(key, newObject);
                else
                    registeredScriptObjects[key] = newObject;

                ((IRegisterableScript)newObject).OnRegistered();
                onRegisteredSubject.OnNext(key);
            }
        }

        public void ExecuteScript(string name, object dataContext)
        {
            if (registeredScriptObjects.ContainsKey(name))
            {
                var scriptObj = registeredScriptObjects[name];
                if (scriptObj is IExecutableScript)
                    ((IExecutableScript)scriptObj).Execute(dataContext);
            }
        }

        public object ExecuteScript(string name)
        {
            if (registeredScriptObjects.ContainsKey(name))
            {
                var scriptObj = registeredScriptObjects[name];
                if (scriptObj is IDataScript)
                    return ((IDataScript)scriptObj).Data;
            }
            return null;
        }

        public dynamic ScriptObject(string name)
        {
            if (registeredScriptObjects.ContainsKey(name))
                return registeredScriptObjects[name];
            return null;
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

        public void WatchDirectory(string path)
        {
            FileSystemWatcher dirWatcher = new FileSystemWatcher();

            Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Changed += h,
                    h => dirWatcher.Changed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {

                });

            Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Created += h,
                    h => dirWatcher.Created -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {

                });

            Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Deleted += h,
                    h => dirWatcher.Deleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {

                });

            Observable
                .FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    h => dirWatcher.Renamed += h,
                    h => dirWatcher.Renamed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {

                });

            Observable
                .FromEventPattern<ErrorEventHandler, ErrorEventArgs>(
                    h => dirWatcher.Error += h,
                    h => dirWatcher.Error -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {

                });

            dirWatcher.Path = path;
            dirWatcher.EnableRaisingEvents = true;
            
            directoryWatchers.Add(dirWatcher);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    directoryWatchers.ForEach(p => p.Dispose());
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public class CSharpScriptingEngine : ScriptingEngineBase
    {
        private MetadataReference[] references = null;

        public CSharpScriptingEngine() :
            base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();

            foreach (var asm in RegisteredAssemblies)
                references = RegisteredAssemblies.Select(r => MetadataReference.CreateFromFile(r.Location)).ToArray();
        }

        public override void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
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

    public class CSScriptEngine : ScriptingEngineBase
    {
        public CSScriptEngine() :
            base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
                    LoadAndExecuteRegister(File.ReadAllText(file));
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void LoadAndExecuteRegister(string scriptText)
        {
            dynamic newObject = CSScript.RoslynEvaluator.LoadCode(scriptText);
            RegisterScript(newObject);
        }
    }

    public class IronPythonScriptingEngine : ScriptingEngineBase
    {
        private ScriptEngine pythonEngine;
        private ScriptScope engineScope;

        public IronPythonScriptingEngine() :
            base()
        {

        }

        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, object> globalObjects = new Dictionary<string, object>();
            globalObjects.Add("ScriptingEngine", this);

            pythonEngine = Python.CreateEngine();

            foreach (var asm in RegisteredAssemblies)
                pythonEngine.Runtime.LoadAssembly(asm);

            engineScope = pythonEngine.CreateScope(globalObjects);
        }

        public override void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
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
