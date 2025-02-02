using System.Reflection;
using System.Reflection.Emit;
using Otus_Task_5.Interface;

namespace Otus_Task_5;

public static class AdapterGenerator
{
    private static readonly AssemblyBuilder assemblyBuilder;

    private static readonly ModuleBuilder moduleBuilder;
    
    private static int typeCounter = 0;

    static AdapterGenerator()
    {
        AssemblyName aName = new AssemblyName("DynamicAdapters");
        assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicAdaptersModule");
    }

    /// <summary>
    /// Генерирует адаптер для заданного интерфейса и объекта.
    /// Пример использования:
    /// var adapter = (IMovable)AdapterGenerator.GenerateAdapter(typeof(IMovable), obj);
    /// </summary>
    public static object GenerateAdapter(Type interfaceType, object obj)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException("Type must be an interface");
        
        string typeName = interfaceType.FullName.Replace('.', '_') + "Adapter_" + (typeCounter++);
        TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName,
            TypeAttributes.Public | TypeAttributes.Class, null, new Type[] { interfaceType });
        
        FieldBuilder objField = typeBuilder.DefineField("obj", typeof(object), FieldAttributes.Private);
        
        ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public,
            CallingConventions.Standard, new Type[] { typeof(object) });
        ILGenerator ctorIL = ctor.GetILGenerator();
        ctorIL.Emit(OpCodes.Ldarg_0);
        ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
        ctorIL.Emit(OpCodes.Ldarg_0);
        ctorIL.Emit(OpCodes.Ldarg_1);
        ctorIL.Emit(OpCodes.Stfld, objField);
        ctorIL.Emit(OpCodes.Ret);
        
        foreach (MethodInfo method in interfaceType.GetMethods())
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type returnType = method.ReturnType;

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                returnType, Array.ConvertAll(parameters, p => p.ParameterType));

            ILGenerator il = methodBuilder.GetILGenerator();

            string key = interfaceType.FullName + ":";
            if (method.Name.StartsWith("get"))
            {
                string prop = method.Name.Substring(3);
                key += prop.ToLower() + ".get";
            }
            else if (method.Name.StartsWith("set"))
            {
                string prop = method.Name.Substring(3);
                key += prop.ToLower() + ".set";
            }
            else
            {
                key += method.Name;
            }
            
            MethodInfo iocResolveMethod = GetNonGenericResolveMethod();
            
            il.Emit(OpCodes.Ldstr, key);
            
            int argCount = 1;
            if (method.Name.StartsWith("set"))
                argCount++; 
            else if (!method.Name.StartsWith("get") && parameters.Length > 0)
                argCount += parameters.Length;

            il.Emit(OpCodes.Ldc_I4, argCount);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, objField);
            il.Emit(OpCodes.Stelem_Ref);

            int arrayIndex = 1;
            if (method.Name.StartsWith("set"))
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, arrayIndex);
                il.Emit(OpCodes.Ldarg_1);
                if (parameters[0].ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, parameters[0].ParameterType);
                il.Emit(OpCodes.Stelem_Ref);
            }
            else if (!method.Name.StartsWith("get"))
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, arrayIndex);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    if (parameters[i].ParameterType.IsValueType)
                        il.Emit(OpCodes.Box, parameters[i].ParameterType);
                    il.Emit(OpCodes.Stelem_Ref);
                    arrayIndex++;
                }
            }
            
            il.Emit(OpCodes.Call, iocResolveMethod);
            
            if (method.Name.StartsWith("set"))
            {
                MethodInfo executeMethod = typeof(ICommand).GetMethod("Execute", Type.EmptyTypes);
                il.Emit(OpCodes.Castclass, typeof(ICommand));
                il.Emit(OpCodes.Callvirt, executeMethod);
                il.Emit(OpCodes.Ret);
            }

            else if (method.Name.StartsWith("get"))
            {
                if (returnType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, returnType);
                else
                    il.Emit(OpCodes.Castclass, returnType);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                if (returnType == typeof(void))
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    if (returnType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, returnType);
                    else
                        il.Emit(OpCodes.Castclass, returnType);
                    il.Emit(OpCodes.Ret);
                }
            }

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        Type adapterType = typeBuilder.CreateType();
        return Activator.CreateInstance(adapterType, new object[] { obj });
    }

    /// <summary>
    /// Возвращает метод IoC.Resolve(string, object[]) – НЕ обобщённую версию.
    /// </summary>
    private static MethodInfo GetNonGenericResolveMethod()
    {
        MethodInfo[] methods =
            typeof(IoC).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name == "Resolve" && !m.IsGenericMethod)
            {
                ParameterInfo[] parms = m.GetParameters();
                if (parms.Length == 2 &&
                    parms[0].ParameterType == typeof(string) &&
                    parms[1].ParameterType == typeof(object[]))
                    return m;
            }
        }

        throw new Exception("Non-generic IoC.Resolve method not found");
    }
}