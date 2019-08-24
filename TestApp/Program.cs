using PythonScriptingEngine;
using ScriptingEngine;
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
            string codeToCompile = @"
            using System;
            using ScriptingEngine;
            namespace RoslynCompileSample
            {
                public class Writer : IRegisterableScript
                {
                    public void Write(string message)
                    {
                        Console.WriteLine($""you said '{message}!'"");
                    }

                    public string Name 
                    { 
                        get
                        {
                            return string.Empty;
                        }
                    }

                    public void Execute(object dataContext)
                    {
                    }
                }
            }";

            string pythonCode = @"
import clr
import System

from System import String
from System import Guid
from ScriptingEngine import IRegisterableScript

class Calculator(IRegisterableScript):
    def get_Name(self):
	    return 'Test'
    def Execute(this, dataContext):
        return

ScriptingEngine.RegisterScript(Calculator())";

            CSharpScriptingEngine v = new CSharpScriptingEngine();
            v.Initialize();
            v.LoadAndExecuteRegister(codeToCompile);

            IronPythonScriptingEngine w = new IronPythonScriptingEngine();
            w.Initialize();
            w.LoadAndExecuteRegister(pythonCode);
            
            //ScriptingEngine se = new ScriptingEngine();
            //se.Initialize();
            //se.LoadScripts(@"C:\Projects\Python Scripting Engine\PythonScriptingEngine\TestScripts\");
        }
    }
}
