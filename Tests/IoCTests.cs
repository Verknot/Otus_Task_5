

using Moq;
using Otus_Task_5;
using Otus_Task_5.Interface;

namespace Tests;

    [TestFixture]
    public class IoCTests
    {
        /// <summary>
        /// Тестирует регистрацию зависимости через IoC.Register и последующее разрешение.
        /// </summary>
        [Test]
        public void RegisterAndResolve_DependencyIsResolved()
        {
            // Подготовка: создаём фабрику, возвращающую новый экземпляр DummyClass.
            Func<object[], object> factory = (args) => new DummyClass { Value = "TestValue" };

            // Регистрируем зависимость под ключом "Dummy"
            // Получаем команду регистрации и выполняем её
            ICommand registerCmd = IoC.Resolve<ICommand>("IoC.Register", "Dummy", factory);
            registerCmd.Execute();

            // Разрешаем зависимость
            DummyClass result = IoC.Resolve<DummyClass>("Dummy");
            Assert.IsNotNull(result);
            Assert.AreEqual("TestValue", result.Value);
        }

        /// <summary>
        /// Тестирует работу со скоупами: в скоупе регистрируется зависимость, и она доступна только внутри него.
        /// </summary>
        [Test]
        public void Scopes_RegisterInScope_OnlyInScope()
        {
            ICommand newScopeCmd = IoC.Resolve<ICommand>("Scopes.New", "scope1");
            newScopeCmd.Execute();
            
            ICommand currentScopeCmd = IoC.Resolve<ICommand>("Scopes.Current", "scope1");
            currentScopeCmd.Execute();
            
            Func<object[], object> factory = (args) => "InsideScope";
            ICommand registerCmd = IoC.Resolve<ICommand>("IoC.Register", "ScopedDep", factory);
            registerCmd.Execute();
            
            string valueInside = IoC.Resolve<string>("ScopedDep");
            Assert.AreEqual("InsideScope", valueInside);
            
            ICommand newScopeCmd2 = IoC.Resolve<ICommand>("Scopes.New", "emptyScope");
            newScopeCmd2.Execute();
            ICommand currentScopeCmd2 = IoC.Resolve<ICommand>("Scopes.Current", "emptyScope");
            currentScopeCmd2.Execute();

            Assert.Throws<Exception>(() => IoC.Resolve<string>("ScopedDep"),
                "Ожидается исключение при разрешении зависимости вне скоупа");
        }
        
        [Test]
        public void Multithreaded_ScopesAreThreadLocal()
        {
            const int threadCount = 5;
            var tasks = new Task[threadCount];
            var results = new string[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                tasks[threadIndex] = Task.Run(() =>
                {
                    string scopeId = "scope_thread_" + threadIndex;
                    IoC.Resolve<ICommand>("Scopes.New", scopeId).Execute();
                    IoC.Resolve<ICommand>("Scopes.Current", scopeId).Execute();
                    
                    Func<object[], object> factory = (args) => "Value_" + threadIndex;
                    IoC.Resolve<ICommand>("IoC.Register", "ThreadDep", factory).Execute();
                    
                    results[threadIndex] = IoC.Resolve<string>("ThreadDep");
                });
            }

            Task.WaitAll(tasks);
            
            for (int i = 0; i < threadCount; i++)
            {
                Assert.AreEqual("Value_" + i, results[i]);
            }
        }

        /// <summary>
        /// Тест с использованием Moq: проверяем, что при разрешении зависимости фабрика вызывается ровно один раз.
        /// </summary>
        [Test]
        public void RegisterAndResolve_UsingMoq_FactoryIsCalled()
        {
            var factoryMock = new Mock<Func<object[], object>>();
            factoryMock.Setup(f => f(It.IsAny<object[]>())).Returns(new DummyClass { Value = "MoqTest" });
            
            ICommand registerCmd = IoC.Resolve<ICommand>("IoC.Register", "MoqDummy", factoryMock.Object);
            registerCmd.Execute();
            
            DummyClass dummy = IoC.Resolve<DummyClass>("MoqDummy");
            
            factoryMock.Verify(f => f(It.IsAny<object[]>()), Times.Once);
            Assert.AreEqual("MoqTest", dummy.Value);
        }
}