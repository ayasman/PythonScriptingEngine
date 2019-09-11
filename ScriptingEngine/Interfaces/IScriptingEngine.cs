namespace ScriptingEngine
{
    public interface IScriptingEngine
    {
        void ExecuteScript(string name, object dataContext);

        object ExecuteScript(string name);

        void LogDebug(string message);

        void LogError(string message);

        void LogInfo(string message);

        void LogWarning(string message);

        void RegisterScript(object newObject);
    }
}