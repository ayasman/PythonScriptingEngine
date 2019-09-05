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
    public class ScriptData
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public dynamic ScriptObject { get; set; }
    }

    public abstract class ScriptingEngineBase : IScriptingEngine, IDisposable
    {
        protected readonly object registeredLocker = new object();
        private List<IDisposable> watcherDisposables = new List<IDisposable>();

        protected Subject<ScriptData> onRegisteredSubject = new Subject<ScriptData>();
        protected Subject<string> onDeletedSubject = new Subject<string>();
        protected Subject<string> onChangedSubject = new Subject<string>();

        protected Dictionary<string, ScriptData> registeredScriptObjects = new Dictionary<string, ScriptData>();
        protected List<Assembly> registeredAssemblies = new List<Assembly>();
        private List<FileSystemWatcher> directoryWatchers = new List<FileSystemWatcher>();

        public IObservable<ScriptData> WhenScriptRegistered => onRegisteredSubject.Publish().RefCount();

        public IObservable<string> WhenScriptDeleted => onDeletedSubject.Publish().RefCount();

        public IObservable<string> WhenScriptChanged => onChangedSubject.Publish().RefCount();

        public ScriptingEngineBase()
        {
        }

        public virtual void Initialize()
        {
            lock (registeredLocker)
            {
                registeredScriptObjects.Clear();
            }

            AppDomain.CurrentDomain.GetAssemblies().Distinct().ToList().ForEach(asm => RegisterAssembly(asm));
        }

        protected List<Assembly> RegisteredAssemblies => registeredAssemblies;

        public void RegisterAssembly(Assembly refAssembly)
        {
            if (!registeredAssemblies.Contains(refAssembly))
                registeredAssemblies.Add(refAssembly);
        }

        public abstract void LoadScript(string file);

        public void LoadScripts(string path)
        {
            foreach (var file in GetFiles(path))
            {
                try
                {
                    LoadScript(file);
                }
                catch (Exception ex)
                {
                }
            }

            WatchDirectory(path);
        }

        public virtual void RegisterScript(dynamic newObject)
        {
            if (newObject is IRegisterableScript)
            {
                string key = ((IRegisterableScript)newObject).Name;
                var newScriptData = new ScriptData() { Name = key, ScriptObject = newObject };
                bool wasNew = false;

                lock (registeredLocker)
                {
                    if (!registeredScriptObjects.ContainsKey(key))
                    {
                        wasNew = true;
                        registeredScriptObjects.Add(key, newScriptData);
                    }
                    else
                        registeredScriptObjects[key] = newScriptData;
                }

                ((IRegisterableScript)newObject).OnRegistered();

                if (!wasNew)
                    onChangedSubject.OnNext(key);
                onRegisteredSubject.OnNext(newScriptData);
            }
        }

        public void ExecuteScript(string name, object dataContext)
        {
            lock (registeredLocker)
            {
                if (registeredScriptObjects.ContainsKey(name))
                {
                    var scriptObj = registeredScriptObjects[name];
                    if (scriptObj.ScriptObject is IExecutableScript)
                        ((IExecutableScript)scriptObj.ScriptObject).Execute(dataContext);
                }
            }
        }

        public object ExecuteScript(string name)
        {
            lock (registeredLocker)
            {
                if (registeredScriptObjects.ContainsKey(name))
                {
                    var scriptObj = registeredScriptObjects[name];
                    if (scriptObj.ScriptObject is IDataScript)
                        return ((IDataScript)scriptObj.ScriptObject).Data;
                }
            }
            return null;
        }

        public ScriptData ScriptObject(string name)
        {
            lock (registeredLocker)
            {
                if (registeredScriptObjects.ContainsKey(name))
                    return registeredScriptObjects[name];
            }
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

        private void WatchDirectory(string path)
        {
            FileSystemWatcher dirWatcher = new FileSystemWatcher();

            watcherDisposables.Add(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Changed += h,
                    h => dirWatcher.Changed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    lock (registeredLocker)
                    {
                        var oldName = registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.FullPath)?.Name;
                        LoadScript(x.FullPath);

                        if (registeredScriptObjects.Count(p => p.Value.FilePath == x.FullPath) > 1)
                        {
                            registeredScriptObjects.Remove(oldName);
                            onDeletedSubject.OnNext(oldName);
                        }
                    }
                })
            );

            watcherDisposables.Add(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Created += h,
                    h => dirWatcher.Created -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    LoadScript(x.FullPath);
                })
            );

            watcherDisposables.Add(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Deleted += h,
                    h => dirWatcher.Deleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    lock (registeredLocker)
                    {
                        var scriptName = registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.FullPath).Name;
                        registeredScriptObjects.Remove(scriptName);
                        onDeletedSubject.OnNext(scriptName);
                    }
                })
            );

            watcherDisposables.Add(Observable
                .FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    h => dirWatcher.Renamed += h,
                    h => dirWatcher.Renamed -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    lock (registeredLocker)
                    {
                        registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.OldFullPath).FilePath = x.FullPath;
                    }
                })
            );

            watcherDisposables.Add(Observable
                .FromEventPattern<ErrorEventHandler, ErrorEventArgs>(
                    h => dirWatcher.Error += h,
                    h => dirWatcher.Error -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                })
            );

            dirWatcher.Path = path;
            dirWatcher.EnableRaisingEvents = true;

            directoryWatchers.Add(dirWatcher);
        }

        #region IDisposable Support

        protected bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    watcherDisposables.ForEach(p => p.Dispose());
                    directoryWatchers.ForEach(p => p.Dispose());
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    }

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

    //public class CSScriptEngine : ScriptingEngineBase
    //{
    //    public CSScriptEngine() :
    //        base()
    //    {
    //    }

    //    public override void Initialize()
    //    {
    //        base.Initialize();
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
    //        dynamic newObject = CSScript.RoslynEvaluator.LoadCode(scriptText);
    //        RegisterScript(newObject);
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