using CSScriptLibrary;
using System;

namespace ScriptingEngine
{
    public class CSScriptEngine : ScriptingEngineBase
    {
        private string lastFile;
        private ScriptData lastRegistered;

        public CSScriptEngine() :
            base()
        {
            lastRegistered = null;
            lastFile = null;
        }

        public override bool Initialize()
        {
            return base.Initialize();
        }

        public bool LoadAndExecuteRegister(string scriptText)
        {
            try
            {
                dynamic newObject = CSScript.RoslynEvaluator.LoadCode(scriptText);
                RegisterScript(newObject);

                if (lastRegistered == null)
                    throw new ApplicationException("C# script does not register an object with the engine.");
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
            dynamic newObject = CSScript.RoslynEvaluator.LoadFile(file);
            RegisterScript(newObject);

            if (lastRegistered == null)
                throw new ApplicationException("C# script does not register an object with the engine.");
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