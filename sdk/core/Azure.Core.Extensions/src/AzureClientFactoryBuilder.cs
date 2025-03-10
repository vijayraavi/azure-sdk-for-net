﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Azure.Core.Extensions
{
    public sealed class AzureClientFactoryBuilder : IAzureClientFactoryBuilderWithConfiguration<IConfiguration>, IAzureClientsBuilderWithCredential
    {
        private readonly IServiceCollection _serviceCollection;

        internal const string DefaultClientName = "Default";

        internal AzureClientFactoryBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
            _serviceCollection.AddOptions();
            _serviceCollection.TryAddSingleton<EventSourceLogForwarder>();
        }

        IAzureClientBuilder<TClient, TOptions> IAzureClientFactoryBuilder.RegisterClientFactory<TClient, TOptions>(Func<TOptions, TClient> clientFactory)
        {
            return ((IAzureClientsBuilderWithCredential)this).RegisterClientFactory<TClient, TOptions>((options, _) => clientFactory(options));
        }

        IAzureClientBuilder<TClient, TOptions> IAzureClientFactoryBuilderWithConfiguration<IConfiguration>.RegisterClientFactory<TClient, TOptions>(IConfiguration configuration)
        {
            return ((IAzureClientsBuilderWithCredential)this).RegisterClientFactory<TClient, TOptions>(
                (options, credentials) => (TClient)ClientFactory.CreateClient(typeof(TClient), typeof(TOptions), options, configuration, credentials))
                .ConfigureOptions(configuration)
                .WithCredential(ClientFactory.CreateCredential(configuration));
        }

        public AzureClientFactoryBuilder ConfigureDefaults(Action<ClientOptions> configureOptions)
        {
            ConfigureDefaults((options, provider) => configureOptions(options));
            return this;
        }

        public AzureClientFactoryBuilder ConfigureDefaults(Action<ClientOptions, IServiceProvider> configureOptions)
        {
            _serviceCollection.Configure<AzureClientsGlobalOptions>(options => options.ConfigureOptionDelegates.Add(configureOptions));

            return this;
        }

        public AzureClientFactoryBuilder ConfigureDefaults(IConfiguration configuration)
        {
            ConfigureDefaults(options => configuration.Bind(options));

            var credentialsFromConfig = ClientFactory.CreateCredential(configuration);

            if (credentialsFromConfig != null)
            {
                UseCredential(credentialsFromConfig);
            }

            return this;
        }

        IAzureClientBuilder<TClient, TOptions> IAzureClientsBuilderWithCredential.RegisterClientFactory<TClient, TOptions>(Func<TOptions, TokenCredential, TClient> clientFactory)
        {
            var clientRegistration = new ClientRegistration<TClient, TOptions>(DefaultClientName, clientFactory);
            _serviceCollection.AddSingleton(clientRegistration);

            _serviceCollection.TryAddSingleton(typeof(IConfigureOptions<AzureClientCredentialOptions<TClient>>), typeof(DefaultCredentialClientOptionsSetup<TClient>));
            _serviceCollection.TryAddSingleton(typeof(IConfigureOptions<TOptions>), typeof(DefaultClientOptionsSetup<TOptions>));
            _serviceCollection.TryAddSingleton(typeof(IAzureClientFactory<TClient>), typeof(AzureClientFactory<TClient, TOptions>));
            _serviceCollection.TryAddSingleton(
                typeof(TClient),
                provider => provider.GetService<IAzureClientFactory<TClient>>().CreateClient(DefaultClientName));

            return new AzureClientBuilder<TClient, TOptions>(clientRegistration, _serviceCollection);
        }


        public AzureClientFactoryBuilder UseCredential(TokenCredential tokenCredential)
        {
            return UseCredential(_ => tokenCredential);
        }

        public AzureClientFactoryBuilder UseCredential(Func<IServiceProvider, TokenCredential> tokenCredentialFactory)
        {
            _serviceCollection.Configure<AzureClientsGlobalOptions>(options => options.CredentialFactory = tokenCredentialFactory);
            return this;
        }
    }
}