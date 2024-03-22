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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <inheritdoc/>
    internal sealed class GXTaskLateBindService : ITaskLateBindHandler
    {
        private struct GXUserTask
        {
            public ClaimsPrincipal user;
            public IEnumerable<GXTask> tasks;
        }
        private readonly List<GXUserTask> _tasks = new List<GXUserTask>();
        private static SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly IServiceProvider _serviceProvider;
        private readonly ISystemLogRepository _systemLogRepository;

        public GXTaskLateBindService(
            ISystemLogRepository systemLogRepository,
            IServiceProvider serviceProvider)
        {
            _systemLogRepository = systemLogRepository;
            _serviceProvider = serviceProvider;
            Thread t = new Thread(() => TaskHandler());
            t.Start();
        }

        public void Add(ClaimsPrincipal user, GXTask item)
        {
            lock (_tasks)
            {
                _tasks.Add(new GXUserTask()
                {
                    user = user,
                    tasks = new GXTask[] { item }
                });
            }
            _signal.Release();
        }

        public void AddRange(ClaimsPrincipal user, IEnumerable<GXTask> tasks)
        {
            lock (_tasks)
            {
                _tasks.Add(new GXUserTask()
                {
                    user = user,
                    tasks = tasks
                });
            }
            _signal.Release();
        }

        /// <summary>
        /// Wait for workflow events and execute them.
        /// </summary>
        private async void TaskHandler()
        {
            while (true)
            {
                _signal.Wait();
                GXUserTask? task;
                lock (_tasks)
                {
                    task = _tasks.FirstOrDefault();
                    if (task != null)
                    {
                        _tasks.RemoveAt(0);
                    }
                }
                if (task != null)
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        try
                        {
                            ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                            await repository.AddAsync(task.Value.user,
                                task.Value.tasks);
                        }
                        catch (Exception ex)
                        {
                            await _systemLogRepository.AddAsync(task.Value.user, ex);
                        }
                    }
                }
            }
        }
    }
}
