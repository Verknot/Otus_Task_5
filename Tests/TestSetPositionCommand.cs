using Otus_Task_5;
using Otus_Task_5.Interface;

namespace Tests;

public class TestSetPositionCommand : ICommand
{
    private readonly Vector _newValue;
    public TestSetPositionCommand(Vector newValue)
    {
        _newValue = newValue;
    }
    public void Execute()
    {
        TestState.LastSetPosition = _newValue;
    } 
}