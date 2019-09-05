using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;

namespace ScriptingEngine
{
    public class IronPythonScriptingEngine : ScriptingEngineBase
    {
        private ScriptScope engineScope;
        private ScriptData lastRegistered;
        private ScriptEngine pythonEngine;
        private IDisposable registeredDisposable;

        public IronPythonScriptingEngine() :
            base()
        {
            lastRegistered = null;

            registeredDisposable = WhenScriptRegistered.Subscribe(obs =>
            {
                lastRegistered = obs;
            });
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

        public void LoadAndExecuteRegister(string scriptText)
        {
            pythonEngine.Execute(scriptText, engineScope);

            if (lastRegistered == null)
                throw new ApplicationException("Python script does not register an object with the engine.");

            lastRegistered = null;
        }

        public override void LoadScript(string file)
        {
            pythonEngine.ExecuteFile(file, engineScope);

            if (lastRegistered == null)
                throw new ApplicationException("Python script does not register an object with the engine.");

            lastRegistered.FilePath = file;
            lastRegistered = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    registeredDisposable?.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}