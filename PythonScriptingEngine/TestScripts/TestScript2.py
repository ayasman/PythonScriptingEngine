import clr
import System

from System import String
from System import Guid
from PythonScriptingEngine import IRegisterableScript

class Calculator(IRegisterableScript):
	def get_Name(self):
	   return "Test"
	  
ScriptingEngine.Register(Calculator())