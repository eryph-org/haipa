﻿using System;
using SimpleInjector;
using Topshelf;

namespace Haipa.Modules.Hosting
{
    public static class TopshelfHostingExtensions
    {
        public static void RunModuleHostService(this Container container, string name)
        {
            var rc = HostFactory.Run(config =>
            {
                config.UseSimpleInjector(container);
                config.SetServiceName(name);
                config.Service<ModuleHost>(s =>
                {
                    s.ConstructUsingSimpleInjector();
                    s.WhenStarted((service, control) => service.Start());
                    s.WhenStopped((service, control) => service.Stop());
                });
            });
            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }


    }
}
