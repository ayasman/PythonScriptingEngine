namespace ScriptingEngine
{
    public interface IDataScript : IRegisterableScript
    {
        object Data { get; }
    }
}