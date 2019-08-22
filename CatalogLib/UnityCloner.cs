using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace CatalogLib
{
    public sealed class UnityCloner
    {
        private delegate void DeepCopyDelegate(UnityCloner cloner, object source, object target);

        private delegate void DeepCopyStructDelegate<T>(UnityCloner cloner, T source, ref T target) where T : struct;

        private static readonly Dictionary<Type, Delegate> DeepCopyMethods = new Dictionary<Type, Delegate>();
        private static AssemblyDefinition _cacheAssembly;
        private static ModuleDefinition _cacheModule;
        private static TypeDefinition _cacheClass;
        private static TypeReference _voidRef;

        private readonly Dictionary<object, object> _clonedInstances = new Dictionary<object, object>();

        public bool CloneMaterials { get; set; }

        public bool CloneMeshes { get; set; }

        public void CopyComponentSettings<T>(T source, T dest)
            where T : Component
        {
        }

        public void CopyHierarchySettings(GameObject source, GameObject dest)
        {
        }

        public GameObject DeepClone(GameObject source)
        {
            throw new NotImplementedException();
        }

        public void DeepClone<T>(T component, GameObject newParent)
            where T : Component
        {
            throw new NotImplementedException();
        }

        public void MergeComponents(GameObject source, GameObject dest)
        {
        }

        public void MergeHierarchy(GameObject source, GameObject dest, bool cloneMissingChildObjects)
        {
        }

        public T DeepClone<T>(T instance)
            where T : class
        {
            if (instance == null)
                return null;

            var type = instance.GetType();
            if (type == typeof(string))
                return instance;

            if (instance is Array array)
                return (T)(object)DeepCloneGeneralArray(array);

            if (!_clonedInstances.TryGetValue(instance, out var clone))
            {
                clone = FormatterServices.GetUninitializedObject(type);
                _clonedInstances.Add(instance, clone);
                DeepCopy(instance, clone);
            }

            return (T)clone;
        }

        public T DeepCloneStruct<T>(T instance)
            where T : struct
        {
            if (typeof(T).IsPrimitive)
                return instance;

            var result = new T();
            DeepCopyStruct(instance, ref result);
            return result;
        }

        public T? DeepCloneNullable<T>(T? instance)
            where T : struct
        {
            if (instance == null)
                return null;

            if (typeof(T).IsPrimitive || typeof(T).IsEnum)
                return instance;

            var result = new T();
            DeepCopyStruct(instance.Value, ref result);
            return result;
        }

        public object DeepCloneGeneral(object instance)
        {
            if (instance == null)
                return null;

            if (instance is string)
                return instance;

            var type = instance.GetType();
            if (type.IsValueType)
            {
                //Primitive and Enum values can be copied directly
                if (type.IsPrimitive || type.IsEnum)
                    return instance;

                //Nullable types need to have their inner value cloned
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var cloneNullable = typeof(UnityCloner).GetMethod("DeepCloneNullable",
                                                                      BindingFlags.Public | BindingFlags.Instance)
                                                           .MakeGenericMethod(type.GetGenericArguments()[0]);
                    return cloneNullable.Invoke(this, new[] { instance });
                }

                //Dynamically invoke the appropriate cloning method for this structure type
                var cloneStruct = typeof(UnityCloner).GetMethod("DeepCloneStruct",
                                                                BindingFlags.Public | BindingFlags.Instance)
                                                     .MakeGenericMethod(type);
                return cloneStruct.Invoke(this, new[] { instance });
            }

            //Use arbitrary reference type deep cloning
            return DeepClone(instance);
        }

        public T[] DeepCloneArray<T>(T[] array)
            where T : class
        {
            if (array == null)
                return null;

            T[] result;
            if (!_clonedInstances.TryGetValue(array, out var existingClone))
            {
                result = new T[array.Length];
                _clonedInstances.Add(array, result);

                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = DeepClone(array[i]);
                }
            }
            else
            {
                result = (T[])existingClone;
            }

            return result;
        }

        public T[] DeepCloneStructArray<T>(T[] array)
            where T : struct
        {
            if (array == null)
                return null;

            T[] result;
            if (!_clonedInstances.TryGetValue(array, out var existingClone))
            {
                result = new T[array.Length];
                _clonedInstances.Add(array, result);

                for (int i = 0; i < array.Length; i++)
                {
                    if (typeof(T).IsPrimitive)
                    {
                        result[i] = array[i];
                    }
                    else
                    {
                        DeepCopyStruct(array[i], ref result[i]);
                    }
                }
            }
            else
            {
                result = (T[])existingClone;
            }

            return result;
        }

        public Array DeepCloneGeneralArray(Array array)
        {
            if (array == null)
                return null;

            Array result;
            if (!_clonedInstances.TryGetValue(array, out var existingClone))
            {
                var elementType = array.GetType().GetElementType();
                var lengths = Enumerable.Range(0, array.Rank)
                                        .Select(array.GetLength).ToArray();
                result = Array.CreateInstance(elementType, lengths);

                _clonedInstances.Add(array, result);

                if (elementType.IsValueType)
                {
                    var deepClone = typeof(UnityCloner).GetMethod("DeepCloneStruct",
                                                                  BindingFlags.Instance | BindingFlags.Public)
                                                       .MakeGenericMethod(elementType);
                    foreach (var indices in ArrayUtils.IterateIndices(lengths))
                    {
                        var value = array.GetValue(indices);
                        var cloned = elementType.IsPrimitive
                                         ? value
                                         : deepClone.Invoke(this, new object[] { value });
                        result.SetValue(cloned, indices);
                    }
                }
                else
                {
                    var deepClone = typeof(UnityCloner).GetMethod("DeepClone",
                                                                  BindingFlags.Instance | BindingFlags.Public)
                                                       .MakeGenericMethod(elementType);
                    foreach (var indices in ArrayUtils.IterateIndices(lengths))
                    {
                        var value = array.GetValue(indices);
                        var cloned = deepClone.Invoke(this, new object[] { value });
                        result.SetValue(cloned, indices);
                    }
                }
            }
            else
            {
                result = (Array)existingClone;
            }

            return result;
        }

        private void DeepCopy(object source, object target)
        {
            var commonType = GetCommonAncestor(source.GetType(), target.GetType());
            var deepCopy = GetDeepCopyDelegate(commonType);
            deepCopy(this, source, target);
        }

        private Type GetCommonAncestor(Type type1, Type type2)
        {
            if (type1 == type2)
                return type1;

            foreach (var base1 in GetTypeAndAncestors(type1))
            {
                foreach (var base2 in GetTypeAndAncestors(type2))
                {
                    if (base1 == base2)
                        return base1;
                }
            }

            return typeof(object);
        }

        private IEnumerable<Type> GetTypeAndAncestors(Type type)
        {
            Type baseType = type;
            while (baseType != null)
            {
                yield return baseType;
                baseType = type.BaseType;
            }
        }

        private void DeepCopyStruct<T>(T source, ref T target)
            where T : struct
        {
            var deepCopy = GetDeepCopyStructDelegate<T>();
            deepCopy(this, source, ref target);
        }

        private static DeepCopyDelegate GetDeepCopyDelegate(Type type)
        {
            DeepCopyDelegate deepCopy;
            lock (DeepCopyMethods)
            {
                Delegate del;
                if (!DeepCopyMethods.TryGetValue(type, out del))
                {
                    del = BuildDeepCopyMethod(type);
                    DeepCopyMethods.Add(type, del);
                }
                deepCopy = (DeepCopyDelegate)del;
            }

            return deepCopy;
        }

        private static DeepCopyStructDelegate<T> GetDeepCopyStructDelegate<T>()
            where T : struct
        {
            DeepCopyStructDelegate<T> deepCopy;
            lock (DeepCopyMethods)
            {
                Delegate del;
                if (!DeepCopyMethods.TryGetValue(typeof(T), out del))
                {
                    del = BuildDeepCopyMethod(typeof(T));
                    DeepCopyMethods.Add(typeof(T), del);
                }
                deepCopy = (DeepCopyStructDelegate<T>)del;
            }

            return deepCopy;
        }

        private static Delegate BuildDeepCopyMethod(Type type)
        {
            if (type.IsArray)
                throw new InvalidOperationException("Cannot perform deep copy on array types; use DeepCloneArray or DeepCloneStructArray instead");

            var module = Assembly.GetExecutingAssembly().GetLoadedModules()[0];
            var method = type.IsValueType
                             ? BuildMethod("DeepCopyStruct", new[] { typeof(UnityCloner), type, type.MakeByRefType()})
                             : BuildMethod("DeepCopy", new[] { typeof(UnityCloner), typeof(object), typeof(object)});
            var il = new ILCursor(new ILContext(method));

            var fields = GetAllFields(type);
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                if (fieldType == typeof(object))
                {
                    //This could be any type, including a boxed value type
                    //For this we use a generic cloning that supports boxed types
                    il.Emit(OpCodes.Ldarg_2);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldfld, field);
                    var cloneMethod = typeof(UnityCloner)
                        .GetMethod("DeepCloneGeneral", BindingFlags.Public | BindingFlags.Instance);
                    il.Emit(OpCodes.Call, cloneMethod);

                    il.Emit(OpCodes.Stfld, field);
                }
                else if (fieldType == typeof(string) || fieldType.IsPrimitive || fieldType.IsEnum)
                {
                    DirectCopyValue(il, field);
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    il.Emit(OpCodes.Ldarg_2);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldfld, field);
                    var cloneNullable = typeof(UnityCloner).GetMethod("DeepCloneNullable",
                                                                      BindingFlags.Public | BindingFlags.Instance)
                                                           .MakeGenericMethod(fieldType.GetGenericArguments()[0]);
                    il.Emit(OpCodes.Call, cloneNullable);

                    il.Emit(OpCodes.Stfld, field);
                }
                else if (fieldType.IsValueType)
                {
                    DeepCopyValue(il, field, fieldType);
                }
                else if (fieldType.IsArray)
                {
                    DeepCopyArray(il, fieldType, field);
                }
                else
                {
                    DeepCopyReference(il, field);
                }
            }
            il.Emit(OpCodes.Ret);

            //TODO
            throw new NotImplementedException();
            //return type.IsValueType
            //    ? method.CreateDelegate(typeof(DeepCopyStructDelegate<>).MakeGenericType(type))
            //    : method.CreateDelegate(typeof(DeepCopyDelegate));

            //_cacheAssembly.
        }

        private static MethodDefinition BuildMethod(string name, Type[] parameters)
        {
            if (_cacheAssembly == null)
            {
                var assemblyName = new AssemblyNameDefinition("UnityCloner.Cache", new Version(1,0));
                _cacheAssembly = AssemblyDefinition.CreateAssembly(assemblyName, "MainModule", ModuleKind.Dll);
                _cacheModule = _cacheAssembly.MainModule;
                _cacheClass = new TypeDefinition("UnityCloner", "CacheMethods",
                                              TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class |
                                              TypeAttributes.Public);
                _cacheModule.Types.Add(_cacheClass);
                _voidRef = _cacheModule.ImportReference(typeof(void));
            }


            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, _voidRef);
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramDef = new ParameterDefinition($"P{i}", ParameterAttributes.None,
                                                       _cacheModule.ImportReference(parameters[i]));
                method.Parameters.Add(paramDef);
                method.DeclaringType = _cacheClass;
            }

            return method;
        }

        private static void DeepCopyValue(ILCursor il, FieldInfo field, Type fieldType)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldflda, field);

            var copyMethod = typeof(UnityCloner)
                             .GetMethod("DeepCopyStruct", BindingFlags.NonPublic | BindingFlags.Instance)
                             .MakeGenericMethod(fieldType);
            il.Emit(OpCodes.Call, copyMethod);
        }

        private static void DeepCopyArray(ILCursor il, Type fieldType, FieldInfo field)
        {
            //We'll be storing the new array into the target field
            il.Emit(OpCodes.Ldarg_2);

            int rank = fieldType.GetArrayRank();
            if (rank == 1)
            {
                il.Emit(OpCodes.Ldarg_0);
                var elementType = fieldType.GetElementType();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldfld, field);

                if (elementType.IsValueType)
                {
                    var deepClone = typeof(UnityCloner)
                                    .GetMethod("DeepCloneStructArray", BindingFlags.Public | BindingFlags.Instance)
                                    .MakeGenericMethod(elementType);
                    il.Emit(OpCodes.Call, deepClone);
                }
                else
                {
                    var deepClone = typeof(UnityCloner)
                                    .GetMethod("DeepCloneArray", BindingFlags.Public | BindingFlags.Instance)
                                    .MakeGenericMethod(elementType);
                    il.Emit(OpCodes.Call, deepClone);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Castclass, typeof(Array));

                var deepClone = typeof(UnityCloner).GetMethod("DeepCloneGeneralArray",
                                                              BindingFlags.Public | BindingFlags.Instance);
                il.Emit(OpCodes.Call, deepClone);
                il.Emit(OpCodes.Castclass, fieldType);
            }

            il.Emit(OpCodes.Stfld, field);
        }

        private static void DirectCopyValue(ILCursor il, FieldInfo field)
        {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Stfld, field);
        }

        private static void DeepCopyReference(ILCursor il, FieldInfo field)
        {
            il.Emit(OpCodes.Ldarg_2);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, field);

            var cloneMethod = typeof(UnityCloner)
                              .GetMethod("DeepClone", BindingFlags.Public | BindingFlags.Instance)
                              .MakeGenericMethod(field.FieldType);
            il.Emit(OpCodes.Call, cloneMethod);

            il.Emit(OpCodes.Stfld, field);
        }

        private static IEnumerable<FieldInfo> GetAllFields(Type derivedType)
        {
            var type = derivedType;
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                    yield return field;

                type = type.BaseType;
            }
        }
    }
}
