using PythonScriptingEngine;
using ScriptingEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

                public class Writer : IExecutableScript
                {
                    public void OnRegistered()
                    {
                        Console.WriteLine($""you said 'TRETRESTRSTRE!'"");
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
            ";

            string pythonCode = @"
import clr
import System

from System import String
from System import Guid
from ScriptingEngine import IRegisterableScript
from ScriptingEngine import IDataScript
from ScriptingEngine import IExecutableScript

class Calculator(IDataScript, IExecutableScript):
    def get_Name(self):
	    return 'Test'
    def OnRegistered(this):
        return
    def get_Data(self):
	    return Calculator()
    def Execute(this, dataContext):
        return

ScriptingEngine.RegisterScript(Calculator())";

            Task.Run(() =>
            {
                IronPythonScriptingEngine v = new IronPythonScriptingEngine();
                v.Initialize();
                v.WatchDirectory(@"C:\Test");
            });


            Thread.Sleep(120000);


            //CSharpScriptingEngine v = new CSharpScriptingEngine();
            //v.Initialize();
            //v.LoadAndExecuteRegister(codeToCompile);
            //v.ExecuteScript("Test", null);

            //CSScriptEngine css = new CSScriptEngine();
            //css.Initialize();
            //css.LoadAndExecuteRegister(codeToCompile);
            //css.ExecuteScript("Test", null);

            //IronPythonScriptingEngine w = new IronPythonScriptingEngine();
            //w.Initialize();
            //w.LoadAndExecuteRegister(pythonCode);
            //var retret = w.ExecuteScript("Test");
            //w.ExecuteScript("Test", null);

            //dynamic fds = w.ScriptObject("Test");
            //var tttt = fds.get_Data();




            //ScriptingEngine se = new ScriptingEngine();
            //se.Initialize();
            //se.LoadScripts(@"C:\Projects\Python Scripting Engine\PythonScriptingEngine\TestScripts\");
        }
    }
}
