using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace ScriptingEngine
{
    public abstract class ScriptingEngineBase : IScriptingEngine, IDisposable
    {
        protected readonly object registeredLocker = new object();

        protected Subject<Exception> onError = new Subject<Exception>();
        protected Subject<ScriptData> onRegisteredSubject = new Subject<ScriptData>();
        protected Subject<string> onUnregisteredSubject = new Subject<string>();
        protected Subject<string> onWarning = new Subject<string>();

        protected Dictionary<string, List<string>> registedScriptTypes = new Dictionary<string, List<string>>();
        protected List<Assembly> registeredAssemblies = new List<Assembly>();
        protected Dictionary<string, ScriptData> registeredScriptObjects = new Dictionary<string, ScriptData>();

        private List<FileSystemWatcher> directoryWatchers = new List<FileSystemWatcher>();
        private List<IDisposable> watcherDisposables = new List<IDisposable>();

        public ScriptingEngineBase()
        {
        }

        public IObservable<Exception> WhenErrorOccurs => onError.Publish().RefCount();

        public IObservable<ScriptData> WhenScriptRegistered => onRegisteredSubject.Publish().RefCount();

        public IObservable<string> WhenScriptUnregistered => onUnregisteredSubject.Publish().RefCount();

        public IObservable<string> WhenWarningOccurs => onWarning.Publish().RefCount();

        protected List<Assembly> RegisteredAssemblies => registeredAssemblies;

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

        public virtual bool Initialize()
        {
            lock (registeredLocker)
            {
                registeredScriptObjects.Clear();
                registedScriptTypes.Clear();
            }

            AppDomain.CurrentDomain.GetAssemblies().Distinct().ToList().ForEach(asm => RegisterAssembly(asm));

            return true;
        }

        public abstract void LoadScript(string file);

        public bool LoadScripts(string path, bool allowHotSwap)
        {
            bool retVal = true;
            try
            {
                foreach (var file in GetFiles(path, onError))
                {
                    try
                    {
                        LoadScript(file);
                    }
                    catch (Exception ex)
                    {
                        onError.OnNext(ex);
                        retVal = false;
                    }
                }

                if (allowHotSwap)
                    WatchDirectory(path);
            }
            catch (Exception ex)
            {
                onError.OnNext(ex);
                retVal = false;
            }
            return retVal;
        }

        public void RegisterScript(dynamic newObject)
        {
            lock (registeredLocker)
            {
                if (newObject is IRegisterableScript)
                {
                    string nameKey = ((IRegisterableScript)newObject).Name;
                    string typeKey = ((IRegisterableScript)newObject).Type;
                    var newScriptData = new ScriptData() { Name = nameKey, Type = typeKey, ScriptObject = newObject };

                    if (registeredScriptObjects.ContainsKey(nameKey))
                    {
                        onWarning.OnNext($"Script {nameKey} being unregistered and overwritten");
                        UnregisterScript(nameKey);
                    }

                    if (!registeredScriptObjects.ContainsKey(nameKey))
                        registeredScriptObjects.Add(nameKey, newScriptData);
                    else
                        registeredScriptObjects[nameKey] = newScriptData;

                    if (!registedScriptTypes.ContainsKey(typeKey))
                        registedScriptTypes.Add(typeKey, new List<string>());
                    if (!registedScriptTypes[typeKey].Contains(nameKey))
                        registedScriptTypes[typeKey].Add(nameKey);

                    // Let engine subtypes handle the new addition
                    OnScriptRegistered(newScriptData);

                    // Tell the script it has been registered
                    ((IRegisterableScript)newObject).OnRegistered();

                    // Tell everyone else that the script is registered
                    onRegisteredSubject.OnNext(newScriptData);
                }
            }
        }

        public virtual bool ReloadScripts()
        {
            bool retVal = true;
            try
            {
                lock (registeredLocker)
                {
                    var possibleFiles = registeredScriptObjects.Where(p => !string.IsNullOrEmpty(p.Value.FilePath));
                    var scriptNames = possibleFiles.Select(p => p.Value.Name).ToList();
                    var fileNames = possibleFiles.Select(p => p.Value.FilePath).ToList();

                    foreach (var name in scriptNames)
                    {
                        UnregisterScript(name);
                    }

                    foreach (var file in fileNames)
                    {
                        try
                        {
                            LoadScript(file);
                        }
                        catch (Exception ex)
                        {
                            onError.OnNext(ex);
                            retVal = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                onError.OnNext(ex);
                retVal = false;
            }
            return retVal;
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

        public List<string> ScriptsOfType(string type)
        {
            lock (registeredLocker)
            {
                if (registedScriptTypes.ContainsKey(type))
                    return registedScriptTypes[type].ToList();
                onWarning.OnNext($"No scripts of type {type} registered");
            }
            return new List<string>();
        }

        protected static IEnumerable<string> GetFiles(string path, Subject<Exception> onError)
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
                    onError.OnNext(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    onError.OnNext(ex);
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

        protected abstract void OnScriptRegistered(ScriptData scriptData);

        protected abstract void OnScriptUnregistered(string name);

        protected void RegisterAssembly(Assembly refAssembly)
        {
            if (!registeredAssemblies.Contains(refAssembly))
                registeredAssemblies.Add(refAssembly);
        }

        protected void UnregisterScript(string name)
        {
            lock (registeredLocker)
            {
                if (registeredScriptObjects.ContainsKey(name))
                    registeredScriptObjects.Remove(name);
                else
                    onWarning.OnNext($"Unable to unregister name {name}, does not exist");

                var type = registedScriptTypes.FirstOrDefault(p => p.Value.Contains(name));
                if (type.Value != null)
                {
                    registedScriptTypes[type.Key].Remove(name);
                }
                else
                    onWarning.OnNext($"Unable to unregister type {type.Key}, name {name}, does not exist");

                OnScriptUnregistered(name);

                onUnregisteredSubject.OnNext(name);
            }
        }

        private void WatchDirectory(string path)
        {
            FileSystemWatcher dirWatcher = new FileSystemWatcher();

            watcherDisposables.Add(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Changed += h,
                    h => dirWatcher.Changed -= h)
                .Throttle(new TimeSpan(10000))
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    try
                    {
                        lock (registeredLocker)
                        {
                            var oldRegistration = registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.FullPath);
                            var oldName = oldRegistration?.Name;

                            UnregisterScript(oldName);

                            LoadScript(x.FullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        onError.OnNext(ex);
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
                    try
                    {
                        LoadScript(x.FullPath);
                    }
                    catch (Exception ex)
                    {
                        onError.OnNext(ex);
                    }
                })
            );

            watcherDisposables.Add(Observable
                .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => dirWatcher.Deleted += h,
                    h => dirWatcher.Deleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(x =>
                {
                    try
                    {
                        lock (registeredLocker)
                        {
                            var scriptName = registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.FullPath).Name;
                            UnregisterScript(scriptName);
                        }
                    }
                    catch (Exception ex)
                    {
                        onError.OnNext(ex);
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
                    try
                    {
                        lock (registeredLocker)
                        {
                            registeredScriptObjects.Select(p => p.Value).FirstOrDefault(p => p.FilePath == x.OldFullPath).FilePath = x.FullPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        onError.OnNext(ex);
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
                    onError.OnNext(x.GetException());
                })
            );

            dirWatcher.Path = path;
            dirWatcher.EnableRaisingEvents = true;

            directoryWatchers.Add(dirWatcher);
        }

        #region IDisposable Support

        protected bool disposedValue = false;

        public void Dispose()
        {
            Dispose(true);
        }

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
        #endregion IDisposable Support
    }
}