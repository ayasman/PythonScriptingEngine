namespace ScriptingEngine
{
    public interface IRegisterableScript
    {
        string Name { get; }

        void OnRegistered();
    }
}