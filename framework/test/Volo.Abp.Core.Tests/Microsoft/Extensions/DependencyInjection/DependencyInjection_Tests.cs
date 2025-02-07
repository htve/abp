﻿using System;
using System.Collections.Generic;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

public abstract class DependencyInjection_Standard_Tests : AbpIntegratedTest<DependencyInjection_Standard_Tests.TestModule>
{
    [Fact]
    public void Singleton_Service_Should_Resolve_Dependencies_Independent_From_The_Scope()
    {
        MySingletonService singletonService;
        MyEmptyTransientService emptyTransientService;
        MyTransientService1 transientService1;

        using (var scope = ServiceProvider.CreateScope())
        {
            transientService1 = scope.ServiceProvider.GetRequiredService<MyTransientService1>();
            emptyTransientService = scope.ServiceProvider.GetRequiredService<MyEmptyTransientService>();

            transientService1.DoIt();
            transientService1.DoIt();

            singletonService = transientService1.SingletonService;
            singletonService.TransientInstances.Count.ShouldBe(2);

            transientService1.TransientInstances.Count.ShouldBe(2);
            transientService1.TransientInstances.ForEach(ts => ts.IsDisposed.ShouldBeFalse());
        }

        Assert.Equal(singletonService, GetRequiredService<MySingletonService>());

        singletonService.ResolveTransient();

        singletonService.TransientInstances.Count.ShouldBe(3);
        singletonService.TransientInstances.ForEach(ts => ts.IsDisposed.ShouldBeFalse());

        transientService1.TransientInstances.ForEach(ts => ts.IsDisposed.ShouldBeTrue());

        emptyTransientService.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void Should_Inject_Services_As_Properties()
    {
        GetRequiredService<ServiceWithPropertyInject>().PropertyInjectedService.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Inject_Services_As_Properties_For_Generic_Classes()
    {
        GetRequiredService<GenericServiceWithPropertyInject<int>>().PropertyInjectedService.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Inject_Services_As_Properties_For_Generic_Concrete_Classes()
    {
        GetRequiredService<ConcreteGenericServiceWithPropertyInject>().PropertyInjectedService.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Not_Inject_Services_As_Properties_When_Class_With_DisablePropertyInjection()
    {
        GetRequiredService<DisablePropertyInjectionOnClass>().PropertyInjectedService.ShouldBeNull();
        GetRequiredService<GenericServiceWithDisablePropertyInjectionOnClass<string>>().PropertyInjectedService.ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Inject_Services_As_Properties_When_Property_With_DisablePropertyInjection()
    {
        GetRequiredService<DisablePropertyInjectionOnProperty>().PropertyInjectedService.ShouldNotBeNull();
        GetRequiredService<DisablePropertyInjectionOnProperty>().DisablePropertyInjectionService.ShouldBeNull();

        GetRequiredService<GenericServiceWithDisablePropertyInjectionOnProperty<string>>().PropertyInjectedService.ShouldNotBeNull();
        GetRequiredService<GenericServiceWithDisablePropertyInjectionOnProperty<string>>().DisablePropertyInjectionService.ShouldBeNull();
    }

    [Fact]
    public void Singletons_Exposing_Multiple_Services_Should_Returns_The_Same_Instance()
    {
        var objectByInterfaceRef = GetRequiredService<IMySingletonExposingMultipleServices>();
        var objectByClassRef = GetRequiredService<MySingletonExposingMultipleServices>();

        ReferenceEquals(objectByInterfaceRef, objectByClassRef).ShouldBeTrue();
    }

    [Fact]
    public void Should_Get_Keyed_Services()
    {
        var bigCache = GetRequiredKeyedService<ICache>("big");
        var bigInstanceCache = GetRequiredKeyedService<ICache>("bigInstance");
        var smallCache = GetRequiredKeyedService<ICache>("small");
        var smallFactoryCache = GetRequiredKeyedService<ICache>("smallFactory");

        bigCache.GetType().ShouldBe(typeof(BigCache));
        bigInstanceCache.GetType().ShouldBe(typeof(BigCache));
        smallCache.GetType().ShouldBe(typeof(SmallCache));
        smallFactoryCache.GetType().ShouldBe(typeof(SmallCache));

        bigCache.Get("key").ShouldBe("Resolving key from big cache.");
        bigInstanceCache.Get("key").ShouldBe("Resolving key from big cache.");
        smallCache.Get("key").ShouldBe("Resolving key from small cache.");
        smallFactoryCache.Get("key").ShouldBe("Resolving key from small cache.");
    }

    public class MySingletonService : ISingletonDependency
    {
        public List<MyEmptyTransientService> TransientInstances { get; }

        public IServiceProvider ServiceProvider { get; }

        public MySingletonService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TransientInstances = new List<MyEmptyTransientService>();
        }

        public void ResolveTransient()
        {
            TransientInstances.Add(
                ServiceProvider.GetRequiredService<MyEmptyTransientService>()
            );
        }
    }

    public class MyTransientService1 : ITransientDependency
    {
        public MySingletonService SingletonService { get; }
        public IServiceProvider ServiceProvider { get; }
        public List<MyEmptyTransientService> TransientInstances { get; }

        public MyTransientService1(MySingletonService singletonService, IServiceProvider serviceProvider)
        {
            SingletonService = singletonService;
            ServiceProvider = serviceProvider;
            TransientInstances = new List<MyEmptyTransientService>();
        }

        public void DoIt()
        {
            SingletonService.ResolveTransient();

            TransientInstances.Add(
                ServiceProvider.GetRequiredService<MyEmptyTransientService>()
            );
        }
    }

    public class MyEmptyTransientService : ITransientDependency, IDisposable
    {
        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public interface IMySingletonExposingMultipleServices
    {

    }

    [ExposeServices(typeof(IMySingletonExposingMultipleServices), typeof(MySingletonExposingMultipleServices))]
    public class MySingletonExposingMultipleServices : IMySingletonExposingMultipleServices, ISingletonDependency
    {

    }

    public class TestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddType<MySingletonService>();
            context.Services.AddType<MyTransientService1>();
            context.Services.AddType<MyEmptyTransientService>();
            context.Services.AddType<ServiceWithPropertyInject>();
            context.Services.AddType<MySingletonExposingMultipleServices>();
            context.Services.AddTransient(typeof(GenericServiceWithPropertyInject<>));
            context.Services.AddTransient(typeof(GenericServiceWithDisablePropertyInjectionOnClass<>));
            context.Services.AddTransient(typeof(GenericServiceWithDisablePropertyInjectionOnProperty<>));
            context.Services.AddKeyedSingleton<ICache, BigCache>("big");
            context.Services.AddKeyedSingleton<ICache, SmallCache>("small");
            context.Services.AddKeyedSingleton<ICache>("bigInstance", new BigCache());
            context.Services.AddKeyedSingleton<ICache>("smallFactory", (sp, key) => new SmallCache());
        }
    }

    public class ServiceWithPropertyInject : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }
    }

    public class GenericServiceWithPropertyInject<T> : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }

        public T Value { get; set; }
    }

    public class ConcreteGenericServiceWithPropertyInject : GenericServiceWithPropertyInject<string>
    {

    }

    [DisablePropertyInjection]
    public class DisablePropertyInjectionOnClass : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }
    }

    public class DisablePropertyInjectionOnProperty : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }

        [DisablePropertyInjection]
        public MyEmptyTransientService DisablePropertyInjectionService { get; set; }
    }

    [DisablePropertyInjection]
    public class GenericServiceWithDisablePropertyInjectionOnClass<T> : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }

        public T Value { get; set; }
    }

    public class GenericServiceWithDisablePropertyInjectionOnProperty<T> : ITransientDependency
    {
        public MyEmptyTransientService PropertyInjectedService { get; set; }

        [DisablePropertyInjection]
        public MyEmptyTransientService DisablePropertyInjectionService { get; set; }

        public T Value { get; set; }
    }

    public interface ICache
    {
        object Get(string key);
    }
    public class BigCache : ICache
    {
        public object Get(string key) => $"Resolving {key} from big cache.";
    }

    public class SmallCache : ICache
    {
        public object Get(string key) => $"Resolving {key} from small cache.";
    }
}
