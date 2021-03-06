﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace We7.Framework.Factable
{
    public sealed class Container : IContainer
    {
        static readonly Type EnumerableType = typeof(IEnumerable<>);
        static MethodInfo _resolveAllMethod;
        readonly IContainerStorage _storage;

        public Container(IContainerStorage storage)
        {
            if (null == storage)
                throw new ArgumentNullException("storage");
            _storage = storage;
        }

        public ComponentRegistration Register<TComponent>() where TComponent : class
        {
            Type tCom = typeof(TComponent);
            ThrowIfNotClass(tCom);
            ComponentRegistration hwnd = new ComponentRegistration(tCom);
            hwnd.OnRegister += hwnd_OnRegister;
            return hwnd;
        }

        static bool IsEnumerableGeneric(Type type)
        {
            return type.IsGenericType && EnumerableType == type.GetGenericTypeDefinition();
        }

        static void ThrowIfNotClass(Type type)
        {
            if (null == type)
                throw new ArgumentNullException("无法使用空类型注册！");
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                throw new ArgumentException("TComponent", "无法注册为组件，不能将抽象类或泛型类的定义注册为组件");
        }

        static void ThrowIfNotInterface(Type type)
        {
            ThrowIfNotInterface(type, false);
        }

        static void ThrowIfNotInterface(Type type, bool tolerateEnumerable)
        {
            if (null == type)
                throw new ArgumentNullException("type");
            if (!type.IsInterface || type.IsGenericTypeDefinition)
                throw new ArgumentException("只能处理非泛型接口类型");
            if (!tolerateEnumerable)
            {
                if (IsEnumerableGeneric(type))
                    throw new ArgumentException("无法处理泛型集合接口 IEnumerable<T>");
            }
        }

        static void ThrowIfNoStorage(Container container)
        {
            if (null == container._storage)
                throw new InvalidOperationException("未配置类型容器存储设施，在使用此容器前，请提前配置存储设施");
        }

        static void ThrowIfInvalidTypes(Type tService, Type tComponent)
        {
            ThrowIfNotInterface(tService);
            ThrowIfNotClass(tComponent);

            if (!tService.IsAssignableFrom(tComponent))
                throw new InvalidCastException(string.Format("无法将类型{0}注册为{1}类型，因为两个类型不兼容", tComponent.FullName, tService.FullName));
        }

        void hwnd_OnRegister(object sender, EventArgs e)
        {
            ComponentRegistration hwnd = sender as ComponentRegistration;
            InternalRegister(hwnd);
        }

        void InternalRegister(ComponentRegistration descriptor)
        {
            ThrowIfNoStorage(this);
            if (null == descriptor)
                throw new ArgumentNullException("descriptor");
            ThrowIfInvalidTypes(descriptor.ServiceType, descriptor.ComponentType);
            _storage.Store(descriptor);
        }



        public TService Resolve<TService>() where TService : class
        {
            TService service = null;
            if (!TryResolve(out service))
            {
                return null;
            }
            return service;
        }

        public IEnumerable<tService> ResolveAll<tService>() where tService : class
        { 
            Type tServ = typeof(tService);
            ThrowIfNoStorage(this);
            ThrowIfNotInterface(tServ);

            IEnumerable<ComponentRegistration> descriptors = _storage.FetchAll(tServ);
            IList<tService> instances = new List<tService>();

            foreach (ComponentRegistration descriptor in descriptors)
            {
                tService instance = ResolveInstance(descriptor, null) as tService;
                if (null != instance)
                    instances.Add(instance);
            }

            return instances;
        }

        object ResolveInstance(ComponentRegistration descriptor, Stack<Type> typeDependencies)
        {
            ThrowIfNoStorage(this);
            if (null == descriptor)
                return null;

            object instance = null;
            if (descriptor.IsSingleton)
                instance = descriptor.Instance;
            if (null == instance)
            {
                instance = CreateInstance(descriptor.ComponentType, typeDependencies);
                if (null != instance && descriptor.IsSingleton)
                {
                    ComponentRegistration newInstance = descriptor.Clone();
                    newInstance.Instance = instance;
                    _storage.Store(newInstance);
                }
            }

            return instance;
        }

        object CreateInstance(Type type, Stack<Type> typeDependencies)
        {
            object instance = null;
            bool createStack = (null == typeDependencies);
            if (createStack)
                typeDependencies = new Stack<Type>();

            Type[] copyDependencies = typeDependencies.ToArray();
            foreach (Type dependency in copyDependencies)
            {
                if (dependency == type)
                {
                    while (typeDependencies.Count > 1)
                    {
                        typeDependencies.Pop();
                    }
                    throw new InvalidOperationException(string.Format("无法创建 '{0}' 类型的实例，因为无法创建其构造函数中 '{1}' 类型的参数的实                      例，该类型与当前正在实例化的类型存在先循环依赖", typeDependencies.Pop(), type));
                }
            }
            typeDependencies.Push(type);

            List<ConstructorInfo> ctors = new List<ConstructorInfo>(type.GetConstructors());
            ctors.Sort(delegate(ConstructorInfo ctor1, ConstructorInfo ctor2)
                        {
                            return ctor1.GetParameters().Length - ctor2.GetParameters().Length;
                        });

            List<ComponentRegistration> bufferedTypes = new List<ComponentRegistration>();
            foreach (ConstructorInfo ctor in ctors)
            { 
                ParameterInfo[] allParams = ctor.GetParameters();
                bool paramTypeRegistered = true;
                foreach (ParameterInfo param in allParams)
                {
                    ComponentRegistration descriptor;
                    if (!TryResolve(param.ParameterType, out descriptor))
                    {
                        paramTypeRegistered = false;
                        bufferedTypes.Clear();
                        break;
                    }
                    bufferedTypes.Add(descriptor);
                }
                if (paramTypeRegistered)
                {
                    int length = allParams.Length;
                    object[] paramsList = new object[length];
                    for (int i = 0; i < paramsList.Length; i++)
                    {
                        paramsList[i] = ResolveInstance(bufferedTypes[i], typeDependencies);
                    }
                    instance = ctor.Invoke(paramsList);
                    break;
                }
            }
            typeDependencies.Pop();
            if (createStack)
            {
                typeDependencies.Clear();
                typeDependencies = null;
            }

            return instance;
        }

        public bool TryResolve<TService>(out TService component) where TService : class
        {
            component = null;
            ComponentRegistration descriptor = null;
            Type tServ = typeof(TService);

            if (TryResolve(tServ, out descriptor))
            {
                component = ResolveInstance(descriptor, null) as TService;
                return (null != component);
            }

            return false;
        }

        bool TryResolve(Type tService, out ComponentRegistration descriptor)
        {
            ThrowIfNoStorage(this);
            ThrowIfNotInterface(tService, true);

            descriptor = null;
            if (IsEnumerableGeneric(tService))
            {
                Type actualServiceType = tService.GetGenericArguments()[0];
                if (null == _resolveAllMethod)
                    _resolveAllMethod = typeof(Container).GetMethod("ResolveAll", new Type[0]);
                MethodInfo resolveAll = _resolveAllMethod.MakeGenericMethod(actualServiceType);
                object instances = resolveAll.Invoke(this, null);

                bool hasInstance = false;
                foreach (object i in (instances as IEnumerable))
                {
                    hasInstance = true;
                    break;
                }

                if (hasInstance)
                {
                    descriptor = new ComponentRegistration(EnumerableType.MakeGenericType(new Type[]{ actualServiceType }));
                    descriptor.Instance = instances;
                    descriptor.IsSingleton = true;
                }
                return false;
            }

            descriptor = _storage.Fetch(tService);
            return descriptor != null;
        }

        public void Register(ComponentRegistration registration)
        {
            InternalRegister(registration);
        }
    }
}
