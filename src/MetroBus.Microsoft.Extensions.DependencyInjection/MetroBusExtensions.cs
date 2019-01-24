﻿using System;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using MassTransit.ExtensionsDependencyInjectionIntegration;

namespace MetroBus.Microsoft.Extensions.DependencyInjection
{
    public static class MetroBusExtensions
    {
        public static IServiceCollection AddMetroBus(this IServiceCollection serviceCollection, Action<IServiceCollectionConfigurator> configure = null)
        {
            serviceCollection.AddMassTransit(configure);
            
            return serviceCollection;
        }

        public static MetroBusInitializer RegisterConsumer<TEvent>(this MetroBusInitializer instance, string queueName, IServiceProvider serviceProvider) where TEvent : class
        {
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> action = (cfg, host) =>
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    cfg.ReceiveEndpoint(host, ConfigureReceiveEndpoint<TEvent>(instance, serviceProvider));
                }
                else
                {
                    cfg.ReceiveEndpoint(host, queueName, ConfigureReceiveEndpoint<TEvent>(instance, serviceProvider));
                }
            };

            instance.MetroBusConfiguration.BeforeBuildActions.Add(action);

            return instance;
        }

        private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TEvent>(MetroBusInitializer instance, IServiceProvider serviceProvider) where TEvent : class 
        {
            return _ =>
            {
                if (instance.MetroBusConfiguration.UseConcurrentConsumerLimit != null)
                {
                    _.UseConcurrencyLimit(instance.MetroBusConfiguration.UseConcurrentConsumerLimit.Value);
                }

                if (instance.MetroBusConfiguration.PrefetchCount != null)
                {
                    _.PrefetchCount = instance.MetroBusConfiguration.PrefetchCount.Value;
                }

                _.LoadFrom(serviceProvider);

                EndpointConvention.Map<TEvent>(_.InputAddress);
            };
        }
    }
}