using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IScriptingEngine
    {
        void RegisterScript(object newObject);

        void ExecuteScript<T>(string name, object dataContext);
    }
}
