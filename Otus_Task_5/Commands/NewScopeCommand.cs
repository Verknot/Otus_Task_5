using Otus_Task_5.Interface;

namespace Otus_Task_5.Commands;

internal class NewScopeCommand : ICommand
{
    private readonly string _scopeId;

    public NewScopeCommand(string scopeId)
    {
        _scopeId = scopeId;
    }

    public void Execute()
    {
        lock (IoC.Locker)
        {
            if (!IoC.Scopes.ContainsKey(_scopeId))
            {
                IoC.Scopes[_scopeId] = new Dictionary<string, Func<object[], object>>();
            }
        }
    }
}