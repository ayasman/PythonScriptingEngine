namespace ScriptingEngine
{
    public interface IExecutableScript : IRegisterableScript
    {
        void Execute(object dataContext);
    }
}