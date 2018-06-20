using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PythonScriptingEngine
{
    public class ScriptingEngine
    {
        Dictionary<string, dynamic> scriptRegistry = new Dictionary<string, dynamic>();

        private Dictionary<string, Tuple<ScriptSource, CompiledCode>> scriptFiles = new Dictionary<string, Tuple<ScriptSource, CompiledCode>>();

        private Dictionary<Type, Dictionary<string, dynamic>> registeredScriptObjects = new Dictionary<Type, Dictionary<string, dynamic>>();
        private ScriptEngine pythonEngine;
        private ScriptScope engineScope;
        private ICallbackContext callbackContext = new CallbackContext();

        public ICallbackContext CallbackContext => callbackContext;

        public void Initialize()
        {
            Dictionary<string, dynamic> globalObjects = new Dictionary<string, dynamic>();
            globalObjects.Add("ScriptingEngine", this);
            globalObjects.Add("CallbackContext", callbackContext);

            pythonEngine = Python.CreateEngine();
            pythonEngine.Runtime.LoadAssembly(Assembly.GetExecutingAssembly());
            engineScope = pythonEngine.CreateScope(globalObjects);
        }

        public void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
                    scriptRegistry.Add(file, pythonEngine.ExecuteFile(file, engineScope));
                }
                catch(Exception ex)
                {

                }
            }
        }

        public void Register(dynamic newObject)
        {
            if (newObject is IRegisterableScript)
            {
                if (!registeredScriptObjects.ContainsKey(typeof(IRegisterableScript)))
                    registeredScriptObjects.Add(typeof(IRegisterableScript), new Dictionary<string, dynamic>());

                registeredScriptObjects[typeof(IRegisterableScript)].Add(((IRegisterableScript)newObject).Name, newObject);

                if (newObject is IExecutableScript)
                {
                    if (!registeredScriptObjects.ContainsKey(typeof(IExecutableScript)))
                        registeredScriptObjects.Add(typeof(IExecutableScript), new Dictionary<string, dynamic>());

                    registeredScriptObjects[typeof(IExecutableScript)].Add(((IRegisterableScript)newObject).Name, newObject);
                }
            }
        }

        static IEnumerable<string> GetFiles(string path)
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
}
