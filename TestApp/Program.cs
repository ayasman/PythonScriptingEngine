using PythonScriptingEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ScriptingEngine se = new ScriptingEngine();
            se.Initialize();
            se.LoadScripts(@"C:\Projects\Python Scripting Engine\PythonScriptingEngine\TestScripts\");
        }
    }
}
