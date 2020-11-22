using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace bit23
{
    public static class DataObjectBuilder
    {
        private static ConcurrentDictionary<Type, (Type type, PropertyInfo[] properties)> _typeMappings = new ConcurrentDictionary<Type, (Type, PropertyInfo[])>();

        static DataObjectBuilder()
        {
            Default = Factory();
        }


        #region Private members

        private static T BuildInterfaceClassFromObject<T>(object source, string[] tupleElementNames, DataObjectBuilderOptions options)
        {
            var itype = typeof(T);
            var typeData = GetOrCreateType(itype);
            var sourceType = source.GetType();
            IDictionary<string, object> sourceDict = null;
            if (sourceType == typeof(JObject))
            {
                sourceDict = GetObjectAsPropertyDictionary(source as JObject, typeData.properties, options);
            }
            else if (IsTuple(sourceType))
            {
                sourceDict = GetObjectAsPropertyDictionary(source as ITuple, typeData.properties, tupleElementNames, options);
            }
            else
            {
                sourceDict = GetObjectAsPropertyDictionary(source, typeData.properties, options);
            }

            if (options != null)
            {
                if (options.TransformValue != null)
                    sourceDict = TransformValues(sourceDict, options.TransformValueFunc);
            }

            return CreateInterfaceObjectInstance<T>(typeData.type, itype, sourceDict);
        }

        private static T BuildInterfaceClassFromIDictionary<T>(IDictionary<string, object> source, DataObjectBuilderOptions options)
        {
            var itype = typeof(T);
            var typeData = GetOrCreateType(itype);

            if (options != null)
            {
                if (options.TransformValue != null)
                    source = TransformValues(source, options.TransformValueFunc);
            }

            return CreateInterfaceObjectInstance<T>(typeData.type, itype, source);
        }


        private static T CreateInterfaceObjectInstance<T>(Type type, Type interfaceType, IDictionary<string, object> source)
        {
            var ctor = (from c in type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                        let pars = c.GetParameters()
                        where
                        pars.Length == 2 &&
                        pars.First().ParameterType == typeof(Type) &&
                        pars.ElementAt(1).ParameterType == typeof(IDictionary<string, object>)
                        select c)
                       .FirstOrDefault();

            if (ctor == null)
                throw new ApplicationException();

            return (T)ctor.Invoke(new object[] { interfaceType, source });
        }

        private static (Type type, PropertyInfo[] properties) GetOrCreateType(Type interfaceType)
        {
            return _typeMappings.GetOrAdd(interfaceType, itype =>
            {
                var baseType = typeof(DynamicInterfaceObject);
                var baseArgsTypes = new Type[] { typeof(Type), typeof(IDictionary<string, object>) };
                var typeBuilder = CreateAssemblyTypeBuilder(itype, baseType);
                typeBuilder.AddInterfaceImplementation(itype);

                var interfaceProperties = itype.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                var typeBackingFields = BuildBackingFields(typeBuilder, interfaceProperties);
                var typeProperties = BuildPropertiesWithBackingFields(typeBuilder, interfaceProperties, typeBackingFields);

                BuildConstructor(typeBuilder, baseType, baseArgsTypes, typeProperties, typeBackingFields);

                return (typeBuilder.CreateTypeInfo(), interfaceProperties);
            });
        }


        private static IDictionary<string, object> TransformValues(IDictionary<string, object> valuesDict, Func<string, object, object> transform)
        {
            var result = new Dictionary<string, object>();

            foreach (var entry in valuesDict)
            {
                result.Add(entry.Key, transform(entry.Key, entry.Value));
            }
            return result;
        }

        private static IDictionary<string, object> GetObjectAsPropertyDictionary(object source, PropertyInfo[] targetProperties, DataObjectBuilderOptions options)
        {
            var result = new Dictionary<string, object>();
            var sourceType = source.GetType();

            foreach (var targetProp in targetProperties)
            {
                var p = sourceType.GetProperty(targetProp.Name);
                if (!targetProp.PropertyType.IsAssignableFrom(p.PropertyType))
                {
                    if (options.ThrowOnInvalidSourceMemberType)
                        throw new Exception("invalid source member type");
                    continue;
                }

                result.Add(targetProp.Name, p.GetMethod.Invoke(source, null));
            }

            return result;
        }

        private static IDictionary<string, object> GetObjectAsPropertyDictionary(ITuple source, PropertyInfo[] targetProperties, string[] tupleElementNames, DataObjectBuilderOptions options)
        {
            var result = new Dictionary<string, object>();
            var sourceType = source.GetType();

            var nameIndex = -1;
            var properties = sourceType.GetTypeInfo().DeclaredFields;
            foreach (var targetProp in targetProperties)
            {
                nameIndex++;

                var tupleFieldIndex = -1;

                if (tupleElementNames != null)
                {
                    tupleFieldIndex = Array.IndexOf(tupleElementNames, targetProp.Name);
                }

                if (tupleFieldIndex == -1)
                {
                    // TODO
                    //continue;
                    tupleFieldIndex = nameIndex;
                }

                var tupleFieldValue = source[tupleFieldIndex];
                if (tupleFieldValue == null)
                {
                    if (targetProp.PropertyType.IsValueType)
                    {
                        // TODO
                        throw new ArgumentException();
                    }
                }
                else
                {
                    if (!targetProp.PropertyType.IsAssignableFrom(tupleFieldValue.GetType()))
                    {
                        // TODO
                        continue;
                    }
                }

                result.Add(targetProp.Name, tupleFieldValue);
            }
            return result;
        }

        private static IDictionary<string, object> GetObjectAsPropertyDictionary(JObject source, PropertyInfo[] targetProperties, DataObjectBuilderOptions options)
        {
            var result = new Dictionary<string, object>();

            foreach (var targetProp in targetProperties)
            {
                var prop = source.Properties().FirstOrDefault(x => x.Name == targetProp.Name);
                if (prop == null)
                {
                    if (options.ThrowOnMissingSourceMember)
                        throw new Exception($"missing source member '{targetProp.Name}'");
                    continue;
                }

                object propValue = null;

                try
                {
                    propValue = prop.Value.ToObject(targetProp.PropertyType);
                }
                catch (Exception ex)
                {
                    if (options.ThrowOnInvalidSourceMemberType)
                        throw new Exception("invalid source member type");
                    continue;
                }

                result.Add(targetProp.Name, propValue);
            }
            return result;
        }



        private static bool IsTuple(Type type)
        {
            if (type.IsGenericType)
                if (type.GetInterfaces().Any(x => x.Name == "ITuple"))
                    if (type.Name.StartsWith("ValueTuple`") || type.Name.StartsWith("Tuple`"))
                        return true;
            return false;
        }

        private static TypeBuilder CreateAssemblyTypeBuilder(Type interfaceType, Type baseType)
        {
            var guid = Guid.NewGuid();
            var assemblyName = new AssemblyName($"DynamicAssembly_{guid:N}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"{interfaceType.Name}Module");
            var typeName = $"{interfaceType.Name}_{guid:N}";
            return moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);
        }

        private static IEnumerable<FieldBuilder> BuildBackingFields(TypeBuilder typeBuilder, IEnumerable<PropertyInfo> interfaceProperties)
        {
            var result = new List<FieldBuilder>();
            foreach (var property in interfaceProperties)
            {
                // build backing field
                var fieldName = $"<{property.Name}>k__BackingField";
                var fieldBuilder = typeBuilder.DefineField(fieldName, property.PropertyType, FieldAttributes.Private);

                result.Add(fieldBuilder);
            }
            return result;
        }

        private static IEnumerable<PropertyBuilder> BuildPropertiesWithBackingFields(TypeBuilder typeBuilder, IEnumerable<PropertyInfo> interfaceProperties, IEnumerable<FieldBuilder> backingFields)
        {
            var result = new List<PropertyBuilder>();

            var enum1 = interfaceProperties.GetEnumerator();
            var enum2 = backingFields.GetEnumerator();

            while (enum1.MoveNext() && enum2.MoveNext())
            {
                var prop = BuildPropertyWithBackingField(typeBuilder, enum1.Current, enum2.Current);
                result.Add(prop);
            }

            return result;
        }

        private static PropertyBuilder BuildPropertyWithBackingField(TypeBuilder typeBuilder, PropertyInfo interfacePropertyInfo, FieldBuilder backingField)
        {
            //// build backing field
            //var fieldName = $"<{interfacePropertyInfo.Name}>k__BackingField";
            //var fieldBuilder = typeBuilder.DefineField(fieldName, interfacePropertyInfo.PropertyType, FieldAttributes.Private);

            // property
            var propertyBuilder = typeBuilder.DefineProperty(interfacePropertyInfo.Name, System.Reflection.PropertyAttributes.None, interfacePropertyInfo.PropertyType, Type.EmptyTypes);

            // get method
            var repositoryGetMethod = BuildPropertyGetMethod(typeBuilder, interfacePropertyInfo, backingField);
            propertyBuilder.SetGetMethod(repositoryGetMethod);

            // set method
            var repositorySetMethod = BuildPropertySetMethod(typeBuilder, interfacePropertyInfo, backingField);
            propertyBuilder.SetSetMethod(repositorySetMethod);

            return propertyBuilder;
        }

        private static MethodBuilder BuildPropertyGetMethod(TypeBuilder typeBuilder, PropertyInfo interfacePropertyInfo, FieldBuilder backingField)
        {
            // Declaring method builder

            // Method attributes
            var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName;

            // get method
            var methodName = $"get_{interfacePropertyInfo.Name}";
            MethodBuilder getMethod = typeBuilder.DefineMethod(methodName, methodAttributes);

            // setting return type
            //getMethod.SetReturnType(typeof(IRepository<,>).MakeGenericType(typeof(Repair), typeof(string)));
            getMethod.SetReturnType(interfacePropertyInfo.PropertyType);

            // method body
            ILGenerator gen = getMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, backingField);
            gen.Emit(OpCodes.Ret);

            var interfacePropertyGet = interfacePropertyInfo.GetGetMethod();
            typeBuilder.DefineMethodOverride(getMethod, interfacePropertyGet);

            return getMethod;
        }

        private static MethodBuilder BuildPropertySetMethod(TypeBuilder typeBuilder, PropertyInfo interfacePropertyInfo, FieldBuilder backingField)
        {
            // Declaring method builder

            var interfaceHasSetMethod = interfacePropertyInfo.SetMethod != null;

            // Method attributes
            var methodAttributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName;

            if (interfaceHasSetMethod)
            {
                methodAttributes |= MethodAttributes.Public | MethodAttributes.Virtual;
            }
            else
            {
                methodAttributes |= MethodAttributes.Family;
            }

            // set method
            var methodName = $"set_{interfacePropertyInfo.Name}";
            MethodBuilder setMethod = typeBuilder.DefineMethod(methodName, methodAttributes);

            // setting return type
            setMethod.SetReturnType(typeof(void));

            // setting method parameters
            setMethod.SetParameters(interfacePropertyInfo.PropertyType);

            // parameter value
            ParameterBuilder value = setMethod.DefineParameter(1, ParameterAttributes.None, "value");

            // method body
            ILGenerator gen = setMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, backingField);
            gen.Emit(OpCodes.Ret);

            if (interfaceHasSetMethod)
            {
                var interfacePropertySet = interfacePropertyInfo.GetSetMethod();
                typeBuilder.DefineMethodOverride(setMethod, interfacePropertySet);
            }

            return setMethod;
        }

        private static ConstructorBuilder BuildConstructor(TypeBuilder typeBuilder, Type baseType, Type[] baseArgsTypes, IEnumerable<PropertyBuilder> typeProperties, IEnumerable<FieldBuilder> backingFields)
        {
            // https://docs.microsoft.com/it-it/dotnet/api/system.reflection.emit.typebuilder?view=netframework-4.8

            var ctorAttributes = System.Reflection.MethodAttributes.Assembly | System.Reflection.MethodAttributes.HideBySig;

            var ctor = typeBuilder.DefineConstructor(ctorAttributes, CallingConventions.HasThis, baseArgsTypes);

            ConstructorInfo baseCtor = baseType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                baseArgsTypes,
                null
            );

            ParameterBuilder baseArg0 = ctor.DefineParameter(1, ParameterAttributes.None, "interfaceType");
            ParameterBuilder baseArg1 = ctor.DefineParameter(2, ParameterAttributes.None, "source");



            ILGenerator gen = ctor.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            //gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Call, baseCtor);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);

            //var enum1 = typeProperties.GetEnumerator();
            //var enum2 = backingFields.GetEnumerator();
            //while (enum1.MoveNext() && enum2.MoveNext())
            //{
            //    //var getRepositoryMethod = baseType.GetMethod(
            //    //    "GetRepository",
            //    //    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            //    //);

            //    var getRepositoryMethod = typeof(RepositoryHub).GetMethod(
            //        "GetRepository",
            //        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            //        null,
            //        new Type[]{
            //            },
            //        null
            //    );

            //    gen.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Call, getRepositoryMethod);
            //    gen.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Ldarg_1);
            //    gen.Emit(OpCodes.Stfld, enum2.Current);
            //    gen.Emit(OpCodes.Nop);
            //}

            //foreach (var prop in typeProperties)
            //{
            //    var getRepositoryMethod = baseType.GetMethod(
            //        "GetRepository",
            //        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            //    );

            //    var setMethod = prop.GetSetMethod(true);

            //    gen.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Ldarg_0);
            //    gen.Emit(OpCodes.Call, getRepositoryMethod);
            //    gen.Emit(OpCodes.Stfld, setMethod);
            //    gen.Emit(OpCodes.Nop);
            //}

            gen.Emit(OpCodes.Ret);

            return ctor;
        }

        #endregion


        #region Internal members

        internal static T BuildInterfaceClass<T>(object source, string[] tupleElementNames = null, DataObjectBuilderOptions options = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var ttype = typeof(T);
            if (!ttype.IsInterface)
                throw new ArgumentException($"Type {ttype.Name} must be an interface.");

            var sourceType = source.GetType();

            if (typeof(IDictionary<string, object>).IsAssignableFrom(sourceType))
            {
                return BuildInterfaceClassFromIDictionary<T>((IDictionary<string, object>)source, options);
            }
            else
            {
                return BuildInterfaceClassFromObject<T>(source, tupleElementNames, options);
            }
        }

        #endregion


        #region Public members

        public static DataObjectBuilderFactory Default { get; private set; }


        public static DataObjectBuilderFactory Factory()
        {
            return new DataObjectBuilderFactory();
        }

        public static DataObjectBuilderFactory Factory(DataObjectBuilderOptions options)
        {
            return new DataObjectBuilderFactory(options);
        }

        public static DataObjectBuilderFactory<TInterfaceType> Factory<TInterfaceType>()
        {
            return new DataObjectBuilderFactory<TInterfaceType>();
        }

        public static DataObjectBuilderFactory<TInterfaceType> Factory<TInterfaceType>(DataObjectBuilderOptions<TInterfaceType> options)
        {
            return new DataObjectBuilderFactory<TInterfaceType>(options);
        }

        #endregion
    }
}
