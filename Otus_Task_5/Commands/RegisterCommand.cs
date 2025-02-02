using Otus_Task_5.Interface;

namespace Otus_Task_5.Commands;

internal class RegisterCommand : ICommand
{
    private readonly string _regKey;
    private readonly Func<object[], object> _factory;

    public RegisterCommand(string regKey, Func<object[], object> factory)
    {
        _regKey = regKey;
        _factory = factory;
    }

    public void Execute()
    {
        IoC.RegisterDependency(_regKey, _factory);
    }
}