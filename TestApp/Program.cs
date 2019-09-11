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
                    public Writer(IScriptingEngine se)
                    {
                        se.LogDebug($""gfdsgfdsgfdsgfsd"");
                    }

                    public void OnRegistered()
                    {
                        Console.WriteLine($""you said 'TRETRESTRSTRE!'"");
                    }

                    public string Name 
                    { 
                        get
                        {
                            return ""Test1"";
                        }
                    }

                    public string Type 
                    { 
                        get
                        {
                            return ""Test Types 1"";
                        }
                    }

                    public void Execute(object dataContext)
                    {
                    }
                }
            ";

            string codeToCompile1 = @"
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
                            return ""Test2"";
                        }
                    }

                    public string Type 
                    { 
                        get
                        {
                            return ""Test Types 1"";
                        }
                    }

                    public void Execute(object dataContext)
                    {
                    }
                }
            ";

            string codeToCompile2 = @"
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
                            return ""Test3"";
                        }
                    }

                    public string Type 
                    { 
                        get
                        {
                            return ""Test Types 33333333333"";
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
    def get_Type(self):
	    return 'AI Tester'
    def OnRegistered(this):
        ScriptingEngine.LogDebug('fdsjfkdlsjflkdsjkl')
        return
    def get_Data(self):
	    return Calculator()
    def Execute(this, dataContext):
        return

ScriptingEngine.RegisterScript(Calculator())";

            IronPythonScriptingEngine v = new IronPythonScriptingEngine();

            v.WhenScriptRegistered.Subscribe(obs =>
            {
                System.Diagnostics.Debug.WriteLine($"Registered script {obs.Name} -- {obs.FilePath}");
            });

            v.WhenScriptUnregistered.Subscribe(obs =>
            {
                System.Diagnostics.Debug.WriteLine($"Unregistered script {obs}");
            });

            v.WhenErrorOccurs.Subscribe(obs =>
            {
                System.Diagnostics.Debug.WriteLine($"Error: {obs.Message}");
            });

            v.WhenWarningOccurs.Subscribe(obs =>
            {
                System.Diagnostics.Debug.WriteLine($"Warning: {obs}");
            });

            //Task.Run(() =>
            //{
            //    v.Initialize();
            //    //v.LoadAndExecuteRegister(pythonCode);
            //    v.LoadScripts(@"C:\Test", true);
            //    //v.ReloadScripts();
            //    //v.WatchDirectory(@"C:\Test");
            //});


            //Thread.Sleep(60000);

            //Task.Run(() =>
            //{
            //    v.Dispose();
            //});

            //Thread.Sleep(60000);

            //CSharpScriptingEngine v = new CSharpScriptingEngine();
            //v.Initialize();
            //v.LoadAndExecuteRegister(codeToCompile);
            //v.ExecuteScript("Test", null);

            //CSScriptEngine css = new CSScriptEngine();
            //css.Initialize();
            //css.LoadAndExecuteRegister(codeToCompile);
            //css.LoadAndExecuteRegister(codeToCompile1);
            //css.LoadAndExecuteRegister(codeToCompile2);
            //css.ExecuteScript("Test", null);

            IronPythonScriptingEngine w = new IronPythonScriptingEngine();
            w.Initialize();
            w.LoadAndExecuteRegister(pythonCode);
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
