//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Loader;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Agent.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Agent
{
    class Program
    {
        const string settingsFile = "settings.json";
        static ILogger _logger;
        static IGXAgentWorker worker = null;

        /// <summary>
        /// Register the agent.
        /// </summary>
        /// <returns></returns>
        public static async Task RegisterAgent(
            IServiceCollection services,
            AutoResetEvent newVersion,
            IGXAgentWorker worker,
            AgentOptions options)
        {
            string name = System.Net.Dns.GetHostName();
            Console.WriteLine("Welcome to use Gurux.DLMS.AMI.");
            Console.WriteLine(string.Format("Gurux.DLMS.AMI address: [{0}]", options.Address));
            string tmp = Console.ReadLine();
            if (!string.IsNullOrEmpty(tmp))
            {
                options.Address = tmp;
            }
            Console.WriteLine(string.Format("Enter agent name: [{0}]", name));
            tmp = Console.ReadLine();
            if (!string.IsNullOrEmpty(tmp))
            {
                name = tmp;
            }
            Console.WriteLine("Enter Personal Access Token:");
            options.Token = Console.ReadLine();
            if (options.Token == null || options.Token.Length != 64)
            {
                throw new ArgumentException("Invalid token.");
            }
            worker.Init(services, options, newVersion);
            options.Id = await worker.AddAgentAsync(name);
            if (options.Id == Guid.Empty)
            {
                throw new ArgumentException("Invalid agent Id.");
            }
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(options));
            Console.WriteLine("Connection succeeded.");
        }

        static async Task<int> Main(string[] args)
        {
            AssemblyLoadContext alc;
            AutoResetEvent newVersion = new AutoResetEvent(true);
            //Show file version info.
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            Console.WriteLine(string.Format("starting Gurux.DLMS.Agent version {0}", string.Join(";", info.FileVersion)));
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            //User cancels the application.
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += async (s, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                Environment.Exit(0);
            };

            try
            {
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter(typeof(Program).FullName, LogLevel.Debug)
                        .AddConsole();
                });
                _logger = loggerFactory.CreateLogger<Program>();

                AgentOptions options = new AgentOptions();
                //Default docker address.
                options.Address = "https://localhost:8001";
                Settings settings = new Settings();
                ////////////////////////////////////////
                //Handle command line parameters.
                int ret = Settings.GetParameters(args, settings);
                if (ret != 0)
                {
                    return ret;
                }
                if (!string.IsNullOrEmpty(settings.Host))
                {
                    options.Address = settings.Host;
                }
                if (!File.Exists(settingsFile) || args.Contains("Configure"))
                {
                    alc = new AssemblyLoadContext("tmp");
                    asm = alc.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Gurux.DLMS.AMI.Agent.Worker.dll"));
                    IGXAgentWorker worker = (IGXAgentWorker)asm.CreateInstance(typeof(GXAgentWorker).FullName);
                    IServiceCollection services = new ServiceCollection();
                    services.AddLogging(builder =>
                    {
                        builder
                            .AddFilter("Microsoft", LogLevel.Warning)
                            .AddFilter("System", LogLevel.Warning)
                            .AddFilter(typeof(Program).FullName, LogLevel.Debug)
                            .AddConsole();
                    });
                    await RegisterAgent(services, newVersion, worker, options);
                    await worker.StopAsync();
                    worker = null;
                }
                options = JsonSerializer.Deserialize<AgentOptions>(File.ReadAllText(settingsFile));
                if (options == null || options.Token == null || options.Token.Length != 64)
                {
                    throw new ArgumentException("Invalid token.");
                }
                if (options.Id == Guid.Empty)
                {
                    throw new ArgumentException("Invalid agent Id.");
                }
                if (info.ProductVersion != null && !info.ProductVersion.Contains("-local"))
                {
                    options.Version = info.ProductVersion;
                }
                else
                {
                    options.Version = null;
                    //MIKKO 
                    options.Version = "1.0.0.1";
                }

                Task t = Task.Run(() => ClosePoller(cts));
                alc = new AssemblyLoadContext("Agent.Worker", true);
                //Current agent version is returned if update fails.
                string currentVersion = options.Version;
                bool update = newVersion.WaitOne(0);
                do
                {
                    if (update)
                    {
                        if (worker != null)
                        {
                            //Save new agent version info.
                            File.WriteAllText(settingsFile, JsonSerializer.Serialize(options));
                            await worker.StopAsync();
                            worker = null;
                            alc.Unload();
                            alc = new AssemblyLoadContext("Agent.Worker", true);
                        }
                        //Start the new app and exit.
                        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        if (!string.IsNullOrEmpty(options.Version))
                        {
                            path = Path.Combine(path, "bin" + options.Version);
                            if (!Directory.Exists(path))
                            {
                                path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            }
                        }
                        try
                        {
                            asm = alc.LoadFromAssemblyPath(Path.Combine(path, "Gurux.DLMS.AMI.Agent.Worker.dll"));
                            worker = (IGXAgentWorker)asm.CreateInstance(typeof(GXAgentWorker).FullName);
                            IServiceCollection services = new ServiceCollection();
                            services.AddLogging(builder =>
                            {
                                builder
                                    .AddFilter("Microsoft", LogLevel.Warning)
                                    .AddFilter("System", LogLevel.Warning)
                                    .AddFilter(typeof(Program).FullName, LogLevel.Debug)
                                    .AddConsole();
                            });
                            worker.Init(services, options, newVersion);
                            await worker.StartAsync();
                            currentVersion = options.Version;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger.LogError("Failed to connect to the server. Invalid personal access token.");
                            break;
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError("Get next task failed. " + ex.Message);
                            //Server is closed. Wait 10 seconds before next attempt.
                            cts.Token.WaitHandle.WaitOne(10000);
                            newVersion.Set();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Get next task failed. " + ex.Message);
                            //Save previous agent version info.
                            options.Version = currentVersion;
                            File.WriteAllText(settingsFile, JsonSerializer.Serialize(options));
                            newVersion.Set();
                        }
                    }
                    //Wait until application is closed or a new version is released.
                    int wait = AutoResetEvent.WaitAny(new WaitHandle[] { cts.Token.WaitHandle, newVersion });
                    if (wait == 0)
                    {
                        //App is closed.
                        break;
                    }
                    else if (wait == 1)
                    {
                        //New version is available.
                        update = true;
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 1;
            }
            finally
            {
                await Disconnect();
            }
            return 0;
        }

        /// <summary>
        /// Get existed tasks.
        /// </summary>
        /// <returns></returns>
        private static void ClosePoller(CancellationTokenSource cts)
        {
            Console.WriteLine("----------------------------------------------------------");
            ConsoleKey k;
            while ((k = Console.ReadKey().Key) != ConsoleKey.Escape)
            {
                if (k == ConsoleKey.Delete)
                {
                    Console.Clear();
                }
                Console.WriteLine("Press Esc to close application or delete clear the console.");
            }
            cts.Cancel();
        }

        private static async Task Disconnect()
        {
            if (worker != null)
            {
                await worker.StopAsync();
                worker = null;
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // Start the worker task.
            if (worker != null)
            {
                Task t = Task.Run(() => Disconnect());
                t.Wait();
            }
        }
    }
}
