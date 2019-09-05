namespace ScriptingEngine
{
    public interface IRegisterableScript
    {
        string Name { get; }

        string Type { get; }

        void OnRegistered();
    }
}