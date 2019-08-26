using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IDataScript : IRegisterableScript
    {
        object Data { get; }
    }
}
