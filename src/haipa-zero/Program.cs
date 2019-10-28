﻿using System;
using System.Diagnostics;
using System.IO;
using Haipa.Modules.Hosting;
using Haipa.Security.Cryptography;
using SimpleInjector;

namespace Haipa.Runtime.Zero
﻿{
    using Haipa.Modules.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using SimpleInjector;
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Defines the <see cref="Program" />
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The Main
        /// </summary>
        /// <param name="args">The args<see cref="string[]"/></param>
        private static void Main(string[] args)
        {
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                $"Haipa{Path.DirectorySeparatorChar}zero");

            var privateConfigPath = Path.Combine(configPath, "private");
            var clientsConfigPath = Path.Combine(privateConfigPath, "clients");

            if (!Directory.Exists(clientsConfigPath))
                Directory.CreateDirectory(clientsConfigPath);

            File.WriteAllText(Path.Combine(clientsConfigPath, "system-client.json"),
                "{ \"client_id\": \"system_client\", \"client_secret\" : \"388D45FA-B36B-4988-BA59-B187D329C207\" }");
            File.WriteAllText(Path.Combine(configPath, "zero_info"),
                $"{{ \"process_id\": \"{Process.GetCurrentProcess().Id}\", \"url\" : \"http://localhost:62189\" }}");

            Certificate.CreateSSL(new CertificateOptions
            {
                Issuer = Network.FQDN,
                FriendlyName = "Haipa Zero Management Certificate",
                Suffix = "CA",
                ValidStartDate = DateTime.UtcNow,
                ValidEndDate = DateTime.UtcNow.AddYears(5),
                Password = "password",
                ExportDirectory = Directory.GetCurrentDirectory(),
                URL = "https://localhost:62189/",
                AppID = "9412ee86-c21b-4eb8-bd89-f650fbf44931",
                CACertName = "HaipaCA.pfx"
            }); ; ;

            var container = new Container();
            container.Bootstrap(args);
            #region Identity Server Seeder
            var serviceProvider = new ServiceCollection()
    .AddDbContext<IdentityDb.ConfigurationStoreContext>(options =>
    {
        IdentityDb.IDbContextConfigurer<IdentityDb.ConfigurationStoreContext> configurer = (IdentityDb.IDbContextConfigurer<IdentityDb.ConfigurationStoreContext>)container.GetInstance(typeof(IdentityDb.IDbContextConfigurer<IdentityDb.ConfigurationStoreContext>));
        configurer.Configure(options);
    })
    .AddSingleton<IIdentityServerSeederService, IdentityServerSeederService>()
    .BuildServiceProvider();
            var seederService = serviceProvider.GetService<IIdentityServerSeederService>();
            seederService.Seed();
            #endregion
            container.RunModuleHostService("haipa-zero");
        }
    }
}
