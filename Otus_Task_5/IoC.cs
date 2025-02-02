using Otus_Task_5.Commands;

namespace Otus_Task_5;

public static class IoC
{
    private static readonly Dictionary<string, Func<object[], object>> globalRegistry =
        new Dictionary<string, Func<object[], object>>();
    
    private static readonly Dictionary<string, Dictionary<string, Func<object[], object>>> scopes =
        new Dictionary<string, Dictionary<string, Func<object[], object>>>();

    private static readonly ThreadLocal<string> currentScope = new ThreadLocal<string>(() => null);
    
    private static readonly object locker = new object();
    
    internal static object Locker => locker;
    internal static Dictionary<string, Dictionary<string, Func<object[], object>>> Scopes => scopes;
    internal static ThreadLocal<string> CurrentScope => currentScope;
    
    static IoC()
    {
        RegisterGlobal("IoC.Register", (args) =>
        {
            if (args.Length < 2)
                throw new ArgumentException("IoC.Register требует два параметра.");
            if (!(args[0] is string regKey))
                throw new ArgumentException("Первый параметр должен быть строкой.");
            if (!(args[1] is Func<object[], object> factory))
                throw new ArgumentException("Второй параметр должен быть фабрикой (Func<object[], object>).");

            return new RegisterCommand(regKey, factory);
        });
        
        RegisterGlobal("Scopes.New", (args) =>
        {
            if (args.Length < 1)
                throw new ArgumentException("Scopes.New требует идентификатор скоупа.");
            if (!(args[0] is string scopeId))
                throw new ArgumentException("Идентификатор скоупа должен быть строкой.");
            return new NewScopeCommand(scopeId);
        });
        
        RegisterGlobal("Scopes.Current", (args) =>
        {
            if (args.Length < 1)
                throw new ArgumentException("Scopes.Current требует идентификатор скоупа.");
            if (!(args[0] is string scopeId))
                throw new ArgumentException("Идентификатор скоупа должен быть строкой.");
            return new CurrentScopeCommand(scopeId);
        });
    }
    
    public static T Resolve<T>(string key, params object[] args)
    {
        object result = Resolve(key, args);
        return (T)result;
    }

    /// <summary>
    /// Универсальный метод разрешения зависимости (без generic-версии).
    /// Все операции (регистрация, создание скоупа, смена скоупа, получение объекта) выполняются через него.
    /// </summary>
    public static object Resolve(string key, params object[] args)
    {

        string scopeId = currentScope.Value;
        if (!string.IsNullOrEmpty(scopeId))
        {
            lock (locker)
            {
                if (scopes.ContainsKey(scopeId) && scopes[scopeId].ContainsKey(key))
                {
                    return scopes[scopeId][key](args);
                }
            }
        }
        
        lock (locker)
        {
            if (globalRegistry.ContainsKey(key))
            {
                return globalRegistry[key](args);
            }
        }

        throw new Exception("Нет регистрации для ключа: " + key);
    }

    /// <summary>
    /// Регистрирует зависимость в глобальном реестре.
    /// </summary>
    private static void RegisterGlobal(string key, Func<object[], object> factory)
    {
        lock (locker)
        {
            globalRegistry[key] = factory;
        }
    }

    /// <summary>
    /// Регистрирует зависимость в текущем скоупе (если он установлен) или в глобальном реестре.
    /// Этот метод вызывается внутренними командами.
    /// </summary>
    internal static void RegisterDependency(string key, Func<object[], object> factory)
    {
        string scopeId = currentScope.Value;
        if (!string.IsNullOrEmpty(scopeId))
        {
            lock (locker)
            {
                if (!scopes.ContainsKey(scopeId))
                {
                    scopes[scopeId] = new Dictionary<string, Func<object[], object>>();
                }

                scopes[scopeId][key] = factory;
            }
        }
        else
        {
            lock (locker)
            {
                globalRegistry[key] = factory;
            }
        }
    }
}