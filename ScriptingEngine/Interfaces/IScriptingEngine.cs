namespace ScriptingEngine
{
    public interface IScriptingEngine
    {
        void ExecuteScript(string name, object dataContext);

        object ExecuteScript(string name);

        void RegisterScript(object newObject);
    }
}