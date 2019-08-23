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

                    public void Execute()
                    {
                    }
                }
            }";

            CSharpScriptingEngine v = new CSharpScriptingEngine();
            v.Initialize();
            v.LoadAndExecuteRegister(codeToCompile);

            //ScriptingEngine se = new ScriptingEngine();
            //se.Initialize();
            //se.LoadScripts(@"C:\Projects\Python Scripting Engine\PythonScriptingEngine\TestScripts\");
        }
    }
}
