using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IExecutableScript : IRegisterableScript
    {
        void Execute(object dataContext);
    }
}
