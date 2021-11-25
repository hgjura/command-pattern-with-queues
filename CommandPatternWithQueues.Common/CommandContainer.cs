using DryIoc;
using System;

namespace CommandPatternWithQueues.Common
{
    public class CommandContainer
    {
        Container c;
        public CommandContainer()
        {
            c = new Container();
        }

        public CommandContainer RegisterDependency<G, T>(bool IsSingleton = true, bool ResolveConstructorArguments = false, string ServiceKey = null)
        {
            c.Register(typeof(G), typeof(T), reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: ServiceKey, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null)); ;

            return this;
        }
        public CommandContainer Register<T>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where T : IRemoteCommand
        {
            c.Register<T>(reuse: Reuse.Singleton);

            c.Register<IRemoteCommand, T>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: typeof(T).Name, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null));

            return this;
        }
        public CommandContainer Register<T>(T instance) where T : IRemoteCommand
        {
            c.RegisterInstance<IRemoteCommand>(instance, serviceKey: typeof(T).Name);

            return this;
        }
        public CommandContainer Register<T>(Type[] const_types, bool IsSingleton = true) where T : IRemoteCommand
        {
            c.Register<IRemoteCommand>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, made: Made.Of(typeof(T).GetConstructor(const_types)), serviceKey: typeof(T).Name);

            return this;
        }

        public bool IsRegistered<T>() where T : IRemoteCommand
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public bool IsRegistered(string ServiceKey)
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: ServiceKey);
        }


        public IRemoteCommand Resolve<T>() where T : IRemoteCommand
        {
            return c.Resolve<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public IRemoteCommand Resolve(string serviceKey)
        {
            return c.Resolve<IRemoteCommand>(serviceKey: serviceKey);
        }

        public CommandContainer Use<T>(T instance)
        {
            c.UseInstance(instance);

            return this;
        }
    }
}
