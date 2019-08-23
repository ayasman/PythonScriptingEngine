using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IScriptingEngine
    {
        void RegisterScript(dynamic newObject);
    }
}
