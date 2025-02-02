using Otus_Task_5.Interface;

namespace Otus_Task_5.Commands;

internal class CurrentScopeCommand : ICommand
{
    private readonly string _scopeId;

    public CurrentScopeCommand(string scopeId)
    {
        _scopeId = scopeId;
    }

    public void Execute()
    {
        lock (IoC.Locker)
        {
            if (!IoC.Scopes.ContainsKey(_scopeId))
            {
                throw new Exception("Скоуп не существует: " + _scopeId);
            }
            IoC.CurrentScope.Value = _scopeId;
        }
    }
}