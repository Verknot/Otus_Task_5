using Moq;
using Otus_Task_5;
using Otus_Task_5.Interface;

namespace Tests;

[TestFixture]
public class AdapterGeneratorTests
{
    [SetUp]
    public void Setup()
    {
        IoC.Resolve<ICommand>(
            "IoC.Register",
            "Otus_Task_5.Interface.IMovable:position.get",
            new Func<object[], object>(args => new Vector { X = 10, Y = 20 })
        ).Execute();
        
        IoC.Resolve<ICommand>(
            "IoC.Register",
            "Otus_Task_5.Interface.IMovable:velocity.get",
            new Func<object[], object>(args => new Vector { X = 30, Y = 40 })
        ).Execute();
        
        IoC.Resolve<ICommand>(
            "IoC.Register",
            "Otus_Task_5.Interface.IMovable:position.set",
            new Func<object[], object>(args => new TestSetPositionCommand((Vector)args[1]))
        ).Execute();
        
        TestState.LastSetPosition = null;
    }

    [Test]
    public void GeneratedAdapter_ImplementsIMovableAndWorksCorrectly()
    {
        DummyClass obj = new DummyClass { Value = "TestObj" };
        
        object adapterObj = AdapterGenerator.GenerateAdapter(typeof(IMovable), obj);
        Assert.IsNotNull(adapterObj, "Адаптер не должен быть null.");
        IMovable adapter = adapterObj as IMovable;
        Assert.IsNotNull(adapter, "Адаптер не реализует интерфейс IMovable.");
        
        Vector pos = adapter.getPosition();
        Assert.AreEqual(new Vector { X = 10, Y = 20 }, pos, "getPosition вернул неверное значение.");
        
        Vector vel = adapter.getVelocity();
        Assert.AreEqual(new Vector { X = 30, Y = 40 }, vel, "getVelocity вернул неверное значение.");
        
        Vector newPos = new Vector { X = 50, Y = 60 };
        adapter.setPosition(newPos);
        Assert.IsNotNull(TestState.LastSetPosition, "setPosition не выполнился корректно.");
        Assert.AreEqual(newPos, TestState.LastSetPosition, "setPosition передал неверное значение.");
    }

    [Test]
    public void GeneratedAdapter_MoqFactoryIsCalledForGetPosition()
    {
        var factoryMock = new Mock<Func<object[], object>>();
        factoryMock.Setup(f => f(It.IsAny<object[]>()))
            .Returns(new Vector { X = 100, Y = 200 });
        
        IoC.Resolve<ICommand>(
            "IoC.Register",
            "Otus_Task_5.Interface.IMovable:position.get",
            factoryMock.Object
        ).Execute();
    
        DummyClass obj = new DummyClass { Value = "TestObj" };
        
        object adapterObj = AdapterGenerator.GenerateAdapter(typeof(IMovable), obj);
        IMovable adapter = adapterObj as IMovable;
        Assert.IsNotNull(adapter, "Адаптер не реализует интерфейс IMovable.");

        Vector pos = adapter.getPosition();

        // Assert
        factoryMock.Verify(f => f(It.IsAny<object[]>()), Times.Once, "Фабрика для getPosition должна вызываться ровно один раз.");
        Assert.AreEqual(new Vector { X = 100, Y = 200 }, pos, "getPosition вернул неверное значение, полученное из фабрики.");
    }
}