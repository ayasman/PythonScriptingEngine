using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PythonScriptingEngine
{
    public class ScriptingEngine
    {
        private Dictionary<string, Tuple<ScriptSource, CompiledCode>> scriptFiles = new Dictionary<string, Tuple<ScriptSource, CompiledCode>>();
        
        private ScriptEngine pythonEngine;

        public void Initialize()
        {
            pythonEngine = Python.CreateEngine();
            pythonEngine.Runtime.LoadAssembly(Assembly.GetExecutingAssembly());
            //dynamic py = pythonEngine.ExecuteFile(@"C:\Projects\Python Scripting Engine\PythonScriptingEngine\TestScripts\TestScript2.py");
            //py.obj.add(1, 2);
        }

        public void LoadScripts(string directory)
        {
            foreach (var file in GetFiles(directory))
            {
                try
                {
                    dynamic py = pythonEngine.ExecuteFile(file);
                    dynamic data = py.Register();
                }
                catch(Exception ex)
                {

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
