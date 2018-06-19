using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;

namespace PythonScriptingEngine
{
    public class ScriptingEngine
    {
        private Dictionary<string, Tuple<ScriptSource, CompiledCode>> scriptFiles = new Dictionary<string, Tuple<ScriptSource, CompiledCode>>();
        
        private ScriptEngine pythonEngine;

        public void Initialize()
        {
            pythonEngine = Python.CreateEngine();
        }
    }
}
