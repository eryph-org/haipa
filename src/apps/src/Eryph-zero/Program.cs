﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dbosoft.Hosuto.Modules.Hosting;
using Dbosoft.OVN;
using Eryph.App;
using Eryph.ModuleCore;
using Eryph.Modules.ComputeApi;
using Eryph.Modules.Network;
using Eryph.Modules.VmHostAgent;
using Eryph.Runtime.Zero.Configuration;
using Eryph.Runtime.Zero.Configuration.Clients;
using Eryph.Runtime.Zero.HttpSys;
using Eryph.Security.Cryptography;
using Eryph.VmManagement;
using JetBrains.Annotations;
using LanguageExt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using static Eryph.Modules.VmHostAgent.Networks.NetworkProviderManager<Eryph.Runtime.Zero.ConsoleRuntime>;
using static Eryph.Modules.VmHostAgent.Networks.ProviderNetworkUpdate<Eryph.Runtime.Zero.ConsoleRuntime>;
using static Eryph.Modules.VmHostAgent.Networks.ProviderNetworkUpdateInConsole<Eryph.Runtime.Zero.ConsoleRuntime>;
using static LanguageExt.Sys.Console<Eryph.Runtime.Zero.ConsoleRuntime>;
using Eryph.Runtime.Zero.Configuration.Networks;
using LanguageExt.Common;

namespace Eryph.Runtime.Zero;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand();
        var debugWaitOption = new System.CommandLine.Option<bool>(name: "--debuggerWait",
            () => false, "Stops and waits for a debugger to be attached");

        rootCommand.AddGlobalOption(debugWaitOption);

        var inFileOption = new System.CommandLine.Option<FileInfo?>(
            name: "--inFile",
            description: "Use input file instead of reading from stdin.");

        var outFileOption = new System.CommandLine.Option<FileInfo?>(
            name: "--outFile",
            description: "Use output file instead of writing to stdout.");

        var nonInteractiveOption = new System.CommandLine.Option<bool>(
            name: "--non-interactive",
            description: "No operator involved - commands will not query for confirmation.");

        var runCommand = new Command("run");
        runCommand.SetHandler(_ => Run(args));
        rootCommand.AddCommand(runCommand);

        var installCommand = new Command("install");
        installCommand.SetHandler(_ => SelfInstall());
        rootCommand.AddCommand(installCommand);

        var networksCommand = new Command("networks");
        rootCommand.AddCommand(networksCommand);

        var getNetworksCommand = new Command("get");
        getNetworksCommand.AddOption(outFileOption);
        getNetworksCommand.SetHandler(GetNetworks, outFileOption);
        networksCommand.AddCommand(getNetworksCommand);

        var importNetworksCommand = new Command("import");

        var noCurrentConfigCheckOption = new System.CommandLine.Option<bool>(
            name: "--no-current-config-check",
            description: "Do not check if host state is valid for current config. ");

        importNetworksCommand.AddOption(inFileOption);
        importNetworksCommand.AddOption(nonInteractiveOption);
        importNetworksCommand.AddOption(noCurrentConfigCheckOption);
        importNetworksCommand.SetHandler(ImportNetworkConfig, inFileOption,
            nonInteractiveOption, noCurrentConfigCheckOption);
        networksCommand.AddCommand(importNetworksCommand);

        var commandLineBuilder = new CommandLineBuilder(rootCommand);

        commandLineBuilder.AddMiddleware(async (context, next) =>
        {
            var debugHaltOn = context.ParseResult.GetValueForOption(debugWaitOption);

            if (debugHaltOn)
            {
                Console.WriteLine("Waiting for debugger to be attached");
                while (!Debugger.IsAttached)
                {
                    await Task.Delay(500, context.GetCancellationToken());
                }
                Console.WriteLine("Debugger attached");

            }

            await next(context);
        });

        commandLineBuilder.UseDefaults();
        var parser = commandLineBuilder.Build();
        return await parser.InvokeAsync(args);

    }

    private static Task<int> Run(string[] args)
    {
        return AdminGuard.CommandIsElevated(async () =>
        {

            var returnCode = 0;

            var logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "eryph", "zero", "logs", "debug.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File(logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    retainedFileTimeLimit: TimeSpan.FromDays(30))
                .CreateLogger();

            try
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
                Log.Logger.Information("Starting eryph-zero {version}", fileVersion.ProductVersion);

                var startupConfig = StartupConfiguration(args,
                    sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        return new
                        {
                            BasePath = configuration["basePath"],
                            SSLEndpointManager = sp.GetRequiredService<ISSLEndpointManager>(),
                            CryptoIO = sp.GetRequiredService<ICryptoIOServices>(),
                            CertificateGenerator = sp.GetRequiredService<ICertificateGenerator>(),

                        };
                    });


                await using var processLock = new ProcessFileLock(Path.Combine(ZeroConfig.GetConfigPath(), ".lock"));

                var basePathUrl = ConfigureUrl(startupConfig.BasePath);

                var endpoints = new Dictionary<string, string>
                    {
                        { "identity", $"{basePathUrl}identity" },
                        { "compute", $"{basePathUrl}compute" },
                        { "common", $"{basePathUrl}common" },
                        { "network", $"{basePathUrl}network" },

                    };

                ZeroConfig.EnsureConfiguration();

                await SystemClientGenerator.EnsureSystemClient(startupConfig.CertificateGenerator, startupConfig.CryptoIO,
                    new Uri(endpoints["identity"]));

                using var _ = await startupConfig.SSLEndpointManager
                    .EnableSslEndpoint(new SSLOptions(
                        "eryph-zero CA",
                        Network.FQDN,
                        DateTime.UtcNow.AddDays(-1),
                        365 * 5,
                        ZeroConfig.GetPrivateConfigPath(),
                        "eryphCA",
                        Guid.Parse("9412ee86-c21b-4eb8-bd89-f650fbf44931"),
                        basePathUrl));



                processLock.SetMetadata(new Dictionary<string, object>
                    {
                        {
                            "endpoints", endpoints
                        }
                    });

                try
                {
                    HostSettingsBuilder.GetHostSettings();
                }
                catch (ManagementException ex)
                {
                    if (ex.ErrorCode == ManagementStatus.InvalidNamespace)
                    {
                        await Console.Error.WriteAsync(
                            "Hyper-V ist not installed. Install Hyper-V feature and then try again.");
                        return -10;
                    }
                }

                var container = new Container();
                container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
                container.Bootstrap();
                container.RegisterInstance<IEndpointResolver>(new EndpointResolver(endpoints));

                var builder = ModulesHost.CreateDefaultBuilder(args) as ModulesHostBuilder;

                var host =
                    builder!
                        .ConfigureInternalHost(hb =>
                        {
                            hb.UseWindowsService(cfg => cfg.ServiceName = "eryph-zero");
                        })

                        .UseAspNetCore((module, webHostBuilder) =>
                        {
                            webHostBuilder.UseHttpSys(options => { options.UrlPrefixes.Add(module.Path); });
                        })
                        .UseSimpleInjector(container)
                        .ConfigureAppConfiguration((_, config) =>
                        {
                            config.AddInMemoryCollection(new Dictionary<string, string>
                            {
                                { "privateConfigPath", ZeroConfig.GetPrivateConfigPath() },
                            });
                        })
                        .HostModule<ComputeApiModule>()
                        .AddIdentityModule(container)
                        .HostModule<VmHostAgentModule>()
                        .HostModule<NetworkModule>()
                        .AddControllerModule(container)
                        .ConfigureServices(c => c.AddSingleton(_ => container.GetInstance<IEndpointResolver>()))
                        .ConfigureServices(LoggerProviderOptions.RegisterProviderOptions<
                            EventLogSettings, EventLogLoggerProvider>)
                        .ConfigureLogging((context, logging) =>
                        {
                            logging.AddSerilog();
                            // See: https://github.com/dotnet/runtime/issues/47303
                            logging.AddConfiguration(
                                context.Configuration.GetSection("Logging"));
                        })
                        .ConfigureHostOptions(cfg => cfg.ShutdownTimeout = new TimeSpan(0, 0, 15))
                        .Build();

                //starting here all errors should be considered as recoverable
                returnCode = -1;

                await host.RunAsync();

                return returnCode;
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "eryph-zero failure");
                await Console.Error.WriteAsync(ex.Message);

                return returnCode;
            }
        });

    }

    private static Uri ConfigureUrl(string basePath)
    {
        var uriBuilder = new UriBuilder(basePath);
        if (uriBuilder.Port == 0)
            uriBuilder.Port = GetAvailablePort();

        return uriBuilder.Uri;
    }

    private static T StartupConfiguration<T>(string[] args, Func<IServiceProvider, T> selectResult)
    {
        using var configHost = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "basePath", "https://localhost:0" },
                    })
                    // ReSharper disable once StringLiteralTypo
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    // ReSharper disable once StringLiteralTypo
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);

                config.AddEnvironmentVariables();

                if (args is { Length: > 0 })
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices(srv =>
            {
                srv.AddSingleton<ISSLEndpointManager, SSLEndpointManager>();
                srv.AddSingleton<ICertificateStoreService, WindowsCertificateStoreService>();
                srv.AddSingleton<IRSAProvider, RSAProvider>();
                srv.AddSingleton<ISSLEndpointRegistry, WinHttpSSLEndpointRegistry>();
                srv.AddSingleton<ICryptoIOServices, WindowsCryptoIOServices>();
                srv.AddSingleton<ICertificateGenerator, CertificateGenerator>();
            })
            .Build();

        var res = selectResult(configHost.Services);

        return res;
    }

    private static readonly IPEndPoint DefaultLoopbackEndpoint = new(IPAddress.Loopback, port: 0);

    private static int GetAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(DefaultLoopbackEndpoint);
        return (((IPEndPoint)socket.LocalEndPoint)!).Port;
    }

    private static Task<int> SelfInstall()
    {
        return AdminGuard.CommandIsElevated(async () =>
        {
            var targetDir =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "eryph", "zero");
            var zeroExe = Path.Combine(targetDir, "bin", "eryph-zero.exe");

            var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "eryph", "zero.old");

            var sysEnv = new SystemEnvironment(new NullLoggerFactory());
            var serviceManager = sysEnv.GetServiceManager("eryph-zero");

            var backupCreated = false;

            try
            {
                var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                var parentDir = baseDir.Parent?.FullName ?? throw new IOException($"Invalid path {baseDir}");

                var cancelSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);

                EitherAsync<Error, Unit> CopyService()
                {
                    return Prelude.Try(() =>
                    {
                        if (Directory.Exists(targetDir))
                        {
                            Directory.Move(targetDir, backupDir);
                            backupCreated = true;
                        }

                        CopyDirectory(parentDir, targetDir);

#if DEBUG
                        var dirName = Directory.GetDirectories(targetDir).FirstOrDefault();
                        if (dirName != null && dirName != "bin")
                        {
                            Directory.Move(dirName, Path.Combine(targetDir, "bin"));
                        }
#endif
                        return Unit.Default;
                    }).ToEitherAsync();


                }

                var installOrUpdate =
                    from serviceExists in serviceManager.ServiceExists()
                    from uStopped in serviceExists
                        ? serviceManager.EnsureServiceStopped(cancelSource.Token)
                        : Unit.Default
                    from uCopy in CopyService()
                    from uInstalled in serviceExists
                        ? serviceManager.UpdateService($"{zeroExe} run", cancelSource.Token)
                        : serviceManager.CreateService("eryph-zero", $"{zeroExe} run", cancelSource.Token)
                    from uStarted in serviceManager.EnsureServiceStarted(cancelSource.Token)
                    select Unit.Default;

                _ = await installOrUpdate.IfLeft(l => l.Throw());
                
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);

                return 0;

            }
            catch (Exception ex)
            {
                await Console.Error.WriteAsync(ex.Message);

                //undo operation
                if (backupCreated)
                    Directory.Move(backupDir, targetDir);


                var cancelSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                var rollback =
                    from serviceExists in serviceManager.ServiceExists()
                    from uStopped in serviceExists
                        ? serviceManager.EnsureServiceStopped(cancelSource.Token)
                        : Unit.Default
                    from uCopy in Prelude.Try(() =>
                    {
                        Directory.Move(backupDir, targetDir);
                        return Unit.Default;
                    } ).ToEitherAsync()

                    from uStarted in serviceManager.EnsureServiceStarted(cancelSource.Token)
                    select Unit.Default;

                _ = await rollback.IfLeft(l =>
                {
                    Console.Error.WriteLine($"Error in rollback of service update: {l.Message}");
                });
                
                return -1;
            }


 

        });
    }

    private static async Task<int> GetNetworks([CanBeNull] FileSystemInfo outFile)
    {
        var manager = new NetworkProviderManager();

        return await manager.GetCurrentConfigurationYaml()
                .MatchAsync(
                    async config =>
                    {
                        if (outFile != null)
                        {
                            await File.WriteAllTextAsync(outFile.FullName, config);
                        }
                        else
                        {
                            Console.WriteLine(config);
                        }

                        return 0;
                    }, l =>
                    {
                        Console.WriteLine(l.Message);
                        return -1;
                    }
                    );
    }

    private static async Task<int> ImportNetworkConfig([CanBeNull] FileSystemInfo inFile, bool nonInteractive,
        bool noCurrentConfigCheck)
    {
        var configString = "";
        if (inFile != null)
        {
            configString = await File.ReadAllTextAsync(inFile.FullName);
        }
        else
        {
            try
            {
                if (!Console.KeyAvailable)
                {
                    await Console.Error.WriteLineAsync(
                        "Error: Supply the new network config to stdin or use --inFile option to read from file");
                    return -1;
                }
            }
            catch (InvalidOperationException)
            {
                // ignored, expected when console is redirected
            }

            await using var reader = Console.OpenStandardInput();
            using var textReader = new StreamReader(reader);
            configString = await textReader.ReadToEndAsync();

        }

        using var psEngine = new PowershellEngine(new NullLoggerFactory().CreateLogger(""));

        var res = (await (
            from newConfig in importConfig(configString)
            from currentConfig in getCurrentConfiguration()
            from hostState in getHostStateWithProgress()
            from syncResult in noCurrentConfigCheck
                ? Prelude.SuccessAff((false, hostState))
                : from currentConfigChanges in generateChanges(hostState, currentConfig)
                  from r in syncCurrentConfigBeforeNewConfig(hostState, currentConfigChanges, nonInteractive)
                  select r
            from newConfigChanges in generateChanges(syncResult.HostState, newConfig)
            from _ in applyChangesInConsole(currentConfig, newConfigChanges,
                nonInteractive, syncResult.IsValid)

            from save in saveConfigurationYaml(configString)
            from m in writeLine("New Network configuration was imported.")
            select Unit.Default)

            .Run(new ConsoleRuntime(
                    new NullLoggerFactory(),
                    psEngine, new CancellationTokenSource())))
            .Match(
                r => 0, l =>
            {
                Console.Error.WriteLine("Error: " + l.Message);
                return -1;
            });

        return res;
    }


    static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}
