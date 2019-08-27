using System;
using System.Collections.Generic;
using System.Text;

namespace ScriptingEngine
{
    public interface IRegisterableScript
    {
        string Name { get; }

        void OnRegistered();
    }
}
