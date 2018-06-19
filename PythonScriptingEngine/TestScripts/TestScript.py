import clr
import System
clr.AddReference("System.Core")
clr.AddReference("System.Windows.Forms")
clr.ImportExtensions(System.Linq)
from System import String
from System import Guid
from TestIronPython.Scripting.Interfaces import IActionScript

class Script1(IActionScript):
    def Load(this):
        print "1"
    def Unload(this):
        print "2"
    def Execute(this):
        print "3"
    def CanExecute():
        return true

pluginID = Guid.Parse("378F108E-A65A-4599-89EC-26F99872B8FB")
relatedModuleID = Guid.Parse("407A91ED-5937-4964-9B26-10E3D17D26DB")
plugin = Script1()
ScriptingManager.RegisterScript(pluginID, relatedModuleID, plugin)