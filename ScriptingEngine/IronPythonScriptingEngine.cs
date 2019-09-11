using IronPython.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;

namespace ScriptingEngine
{
    public class IronPythonScriptingEngine : ScriptingEngineBase
    {
        private ScriptScope engineScope;
        private string lastFile;
        private ScriptData lastRegistered;
        private ScriptEngine pythonEngine;

        public IronPythonScriptingEngine() :
            base()
        {
            lastRegistered = null;
            lastFile = null;
        }

        public IronPythonScriptingEngine(ILogger logger) :
            base(logger)
        {
            lastRegistered = null;
            lastFile = null;
        }


        public override bool Initialize()
        {
            try
            {
                base.Initialize();

                Dictionary<string, object> globalObjects = new Dictionary<string, object>();
                globalObjects.Add("ScriptingEngine", this);

                pythonEngine = Python.CreateEngine();

                foreach (var asm in RegisteredAssemblies)
                    pythonEngine.Runtime.LoadAssembly(asm);

                engineScope = pythonEngine.CreateScope(globalObjects);

                return true;
            }
            catch (Exception ex)
            {
                onError.OnNext(ex);
            }
            return false;
        }

        public bool LoadAndExecuteRegister(string scriptText)
        {
            try
            {
                lastFile = null;
                pythonEngine.Execute(scriptText, engineScope);

                if (lastRegistered == null)
                    throw new ApplicationException("Python script does not register an object with the engine.");
                lastRegistered = null;

                return true;
            }
            catch (Exception ex)
            {
                onError.OnNext(ex);
            }
            lastRegistered = null;
            return false;
        }

        public override void LoadScript(string file)
        {
            lastFile = file;
            pythonEngine.ExecuteFile(file, engineScope);

            if (lastRegistered == null)
                throw new ApplicationException("Python script does not register an object with the engine.");
            lastRegistered = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnScriptRegistered(ScriptData scriptData)
        {
            scriptData.FilePath = lastFile;
            lastRegistered = scriptData;
        }

        protected override void OnScriptUnregistered(string name)
        {
        }
    }
}