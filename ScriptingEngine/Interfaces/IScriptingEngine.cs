using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IScriptingEngine
    {
        void RegisterScript(object newObject);

        void ExecuteScript(string name, object dataContext);

        object ExecuteScript(string name);
    }
}
