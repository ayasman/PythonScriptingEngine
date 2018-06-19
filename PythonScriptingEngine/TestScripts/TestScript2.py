import clr
import System
from PythonScriptingEngine import IRegisterableScript

class Calculator(IRegisterableScript):
   def add(self, argA, argB):
      return argA+argB
   def sub(self, argA, argB):
      return argA-argB
	  
def Register():
	return Calculator()