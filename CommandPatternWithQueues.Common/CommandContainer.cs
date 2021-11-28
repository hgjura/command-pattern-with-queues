using DryIoc;
using System;
using System.Collections.Generic;

namespace CommandPatternWithQueues.Common
{
    public class CommandContainer
    {
        Container c;
        Dictionary<Type, Type> commandresponsetypemap;
        public CommandContainer()
        {
            c = new Container();
            commandresponsetypemap = new Dictionary<Type, Type>();
        }


        #region Commands

        public CommandContainer RegisterCommand<T>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where T : IRemoteCommand
        {
            c.Register<T>(reuse: Reuse.Singleton);

            c.Register<IRemoteCommand, T>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: typeof(T).Name, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null));

            return this;
        }
        public CommandContainer RegisterCommand<T>(T instance) where T : IRemoteCommand
        {
            c.RegisterInstance<IRemoteCommand>(instance, serviceKey: typeof(T).Name);

            return this;
        }
        public CommandContainer RegisterCommand<T>(Type[] const_types, bool IsSingleton = true) where T : IRemoteCommand
        {
            c.Register<IRemoteCommand>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, made: Made.Of(typeof(T).GetConstructor(const_types)), serviceKey: typeof(T).Name);

            return this;
        }

        public bool IsCommandRegistered<T>() where T : IRemoteCommand
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public bool IsCommandRegistered(string ServiceKey)
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: ServiceKey);
        }


        public IRemoteCommand ResolveCommand<T>() where T : IRemoteCommand
        {
            return c.Resolve<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public IRemoteCommand ResolveCommand(string serviceKey)
        {
            return c.Resolve<IRemoteCommand>(serviceKey: serviceKey);
        }
        #endregion

        #region Responses

        public CommandContainer RegisterResponse<TCommand, TResponse>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where TCommand: IRemoteCommand where TResponse : IRemoteResponse
        {
            c.Register<TResponse>(reuse: Reuse.Singleton);

            c.Register<IRemoteResponse, TResponse>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: typeof(TResponse).Name, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null));
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));

            return this;
        }
        public CommandContainer RegisterResponse<TCommand, TResponse>(TResponse instance) where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            c.RegisterInstance<IRemoteResponse>(instance, serviceKey: typeof(TResponse).Name);
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));
            
            return this;
        }
        public CommandContainer RegisterResponse<TCommand, TResponse>(Type[] const_types, bool IsSingleton = true) where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            c.Register<IRemoteResponse>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, made: Made.Of(typeof(TResponse).GetConstructor(const_types)), serviceKey: typeof(TResponse).Name);
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));

            return this;
        }

        public bool IsResponseRegistered<TCommand, TResponse>() where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            return c.IsRegistered<IRemoteResponse>(serviceKey: typeof(TResponse).Name) && commandresponsetypemap.ContainsKey(typeof(TCommand));
        }

        public bool IsResponseRegistered(string ServiceKey)
        {
            return c.IsRegistered<IRemoteResponse>(serviceKey: ServiceKey) && commandresponsetypemap.ContainsValue(ResolveResponse(ServiceKey).GetType());  
        }


        public IRemoteResponse ResolveResponse<TResponse>() where TResponse : IRemoteResponse
        {
            return c.Resolve<IRemoteResponse>(serviceKey: typeof(TResponse).Name);
        }

        public IRemoteResponse ResolveResponse(string serviceKey)
        {
            return c.Resolve<IRemoteResponse>(serviceKey: serviceKey);
        }
        public IRemoteResponse ResolveResponseFromCommand(Type CommandType)
        {
            var com = ResolveCommand(CommandType.Name);
            var respType = commandresponsetypemap[com.GetType()];
            return c.Resolve<IRemoteResponse>(serviceKey: respType.Name);
        }

        #endregion


        public CommandContainer RegisterDependency<G, T>(bool IsSingleton = true, bool ResolveConstructorArguments = false, string ServiceKey = null)
        {
            c.Register(typeof(G), typeof(T), reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: ServiceKey, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null)); ;

            return this;
        }
        public CommandContainer Use<T>(T instance)
        {
            c.UseInstance(instance);

            return this;
        }
    }
}
