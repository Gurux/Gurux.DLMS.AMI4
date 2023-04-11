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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Script;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Gurux.DLMS.AMI.Server.Services;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Gurux.DLMS.AMI.Client.Pages.Block;
using Gurux.DLMS.AMI.Client.Pages.User;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ScriptRepository : IScriptRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IScriptGroupRepository _scriptGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IWorkflowHandler workflowHandler,
            IScriptGroupRepository scriptGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _workflowHandler = workflowHandler;
            _scriptGroupRepository = scriptGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid scriptId)
        {
            GXSelectArgs args = GXQuery.GetUsersByScript(ServerHelpers.GetUserId(User), scriptId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? scriptIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByScripts(ServerHelpers.GetUserId(User), scriptIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }


        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> scripts,
            bool delete)
        {
            if (scripts != null && scripts.Any())
            {
                if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScriptManager)))
                {
                    throw new UnauthorizedAccessException();
                }
                GXSelectArgs arg = GXSelectArgs.Select<GXScript>(a => a.Id, q => scripts.Contains(q.Id));
                List<GXScript> list = await _host.Connection.SelectAsync<GXScript>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXScript, List<string>> updates = new();
                foreach (GXScript script in list)
                {
                    script.Removed = now;
                    List<string> users = await GetUsersAsync(User, script.Id);
                    if (delete)
                    {
                        await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXScript>(script.Id));
                    }
                    else
                    {
                        _host.Connection.Update(GXUpdateArgs.Update(script, q => q.Removed));
                    }
                    updates[script] = users;
                    //Check if there is a uninstall method and call it.
                    arg = GXSelectArgs.Select<GXScriptMethod>(a => a.Id, q => q.Script == script && q.Name == "Uninstall");
                    GXScriptMethod? method = await _host.Connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                    if (method != null)
                    {
                        await RunAsync(User, method.Id);
                    }
                    //Unload script.
                    _workflowHandler.UnloadScript(script.Id);
                }
                foreach (var it in updates)
                {
                    GXScript tmp = new GXScript() { Id = it.Key.Id };
                    await _eventsNotifier.ScriptDelete(it.Value, new GXScript[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXScript[]> ListAsync(
            ClaimsPrincipal user,
            ListScripts? request,
            ListScriptsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the scripts.
                arg = GXSelectArgs.SelectAll<GXScript>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScriptsByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Methods != null && request.Filter.Methods.Any())
                {
                    arg.Columns.Add<GXScriptMethod>();
                    arg.Joins.AddInnerJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script);
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScript>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXScript>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXScript>(q => q.CreationTime);
                arg.Descending = true;
            }
            GXScript[] scripts = (await _host.Connection.SelectAsync<GXScript>(arg)).ToArray();
            if (response != null)
            {
                response.Scripts = scripts;
                if (response.Count == 0)
                {
                    response.Count = scripts.Length;
                }
            }
            //Read methods with own query.
            foreach (GXScript script in scripts)
            {
                arg = GXSelectArgs.SelectAll<GXScriptMethod>(q => q.Script == script);
                script.Methods = (await _host.Connection.SelectAsync<GXScriptMethod>(arg));
            }
            return scripts;
        }

        /// <inheritdoc />
        public async Task<GXScript> ReadAsync(
            ClaimsPrincipal user,
            Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the scripts.
                arg = GXSelectArgs.SelectAll<GXScript>(w => w.Id == id);
                arg.Joins.AddInnerJoin<GXScript, GXScriptGroupScript>(x => x.Id, y => y.ScriptId);
                arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScriptsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id);
            }
            arg.Columns.Add<GXScriptGroup>();
            arg.Columns.Add<GXScriptMethod>();
            //Byte assembly is not send for the client.
            arg.Columns.Exclude<GXScript>(e => e.ByteAssembly);
            arg.Columns.Exclude<GXScriptGroup>(e => e.Scripts);
            arg.Columns.Exclude<GXScriptMethod>(e => e.Script);
            arg.Joins.AddInnerJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script);
            arg.Distinct = true;
            GXScript script = await _host.Connection.SingleOrDefaultAsync<GXScript>(arg);
            if (script == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return script;
        }

        /// <summary>
        /// This method checks is script source code modified.
        /// </summary>
        /// <param name="script">Updated script.</param>
        /// <returns>True, if the source code has changed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        async Task<bool> IsChangedAsync(GXScript script)
        {
            if (script.Id == Guid.Empty)
            {
                //If the new script is added.
                return true;
            }
            //Remove time is not checked because adming can revert the script.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScript>(q => q.Id == script.Id);
            GXScript saved = await _host.Connection.SingleOrDefaultAsync<GXScript>(arg);
            if (saved == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            bool changed = script.SourceCode != saved.SourceCode;
            if (!changed)
            {
                //Use saved byte assembly if source code hasn't changed.
                script.ByteAssembly = saved.ByteAssembly;
            }
            return changed;
        }

        private void UpdateScript(ClaimsPrincipal User, GXScript script, out int compileTime)
        {
            if (string.IsNullOrEmpty(script.SourceCode))
            {
                throw new ArgumentNullException("Script is empty.");
            }
            List<GXScriptMethod> methods = new List<GXScriptMethod>();
            script.ByteAssembly = Compile(User, script.Id.ToString(), script.SourceCode, script.Namespaces,
                methods, out string? errorJson, out compileTime);
            if (errorJson != null)
            {
                throw new ArgumentNullException("Script compile failed. Validate script to get errors.");
            }
            var comparer = new GXScriptMethodComparer();
            List<GXScriptMethod> addedScriptMethods = methods.Except(script.Methods, comparer).ToList();
            List<GXScriptMethod> removedScriptMethods = script.Methods.Except(methods, comparer).ToList();
            if (removedScriptMethods.Any())
            {
                script.Methods.RemoveAll(q => removedScriptMethods.Contains(q));
            }
            if (addedScriptMethods.Any())
            {
                script.Methods.AddRange(addedScriptMethods);
            }
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXScript> scripts,
            Expression<Func<GXScript, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            Dictionary<GXScript, List<string>> updates = new();
            foreach (GXScript script in scripts)
            {
                //Verify script name.
                if (string.IsNullOrEmpty(script.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(script.SourceCode))
                {
                    throw new ArgumentNullException("Script is empty.");
                }
                if (script.ScriptGroups == null || !script.ScriptGroups.Any())
                {
                    //Get default script groups if not admin.
                    ListScriptGroups request = new ListScriptGroups()
                    {
                        Filter = new GXScriptGroup() { Default = true }
                    };
                    script.ScriptGroups = new List<GXScriptGroup>();
                    script.ScriptGroups.AddRange(await _scriptGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    if (!script.ScriptGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                //Update compiled script if it's changed.
                bool changed = await IsChangedAsync(script);
                if (changed)
                {
                    UpdateScript(User, script, out int compileTime);
                    if (script.Id != Guid.Empty)
                    {
                        _workflowHandler.UnloadScript(script.Id);
                    }
                }
                if (script.Id == Guid.Empty)
                {
                    script.CreationTime = now;
                    script.Creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                    GXInsertArgs args = GXInsertArgs.Insert(script);
                    args.Exclude<GXScript>(q => new { q.Updated, q.ScriptGroups, q.Methods, q.Creator });
                    _host.Connection.Insert(args);
                    list.Add(script.Id);
                    AddScriptToScriptGroups(script.Id, script.ScriptGroups);
                    AddScriptMethodsToScript(script, script.Methods);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXScript>(q => q.ConcurrencyStamp, where => where.Id == script.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != script.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    m = GXSelectArgs.Select<GXScript>(q => q.Active, where => where.Id == script.Id);
                    bool active = _host.Connection.SingleOrDefault<bool>(m);

                    script.Updated = now;
                    script.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(script, columns);
                    args.Exclude<GXScript>(q => new { q.CreationTime, q.ScriptGroups, q.Methods, q.Creator });
                    _host.Connection.Update(args);
                    //Map script groups to script.
                    List<GXScriptGroup> scriptGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IScriptGroupRepository scriptGroupRepository = scope.ServiceProvider.GetRequiredService<IScriptGroupRepository>();
                        scriptGroups = await scriptGroupRepository.GetJoinedScriptGroups(User, script.Id);
                    }
                    var comparer = new UniqueComparer<GXScriptGroup, Guid>();
                    List<GXScriptGroup> removedScriptGroups = scriptGroups.Except(script.ScriptGroups, comparer).ToList();
                    List<GXScriptGroup> addedScriptGroups = script.ScriptGroups.Except(scriptGroups, comparer).ToList();
                    if (removedScriptGroups.Any())
                    {
                        RemoveScriptsFromScriptGroup(script.Id, removedScriptGroups);
                    }
                    if (addedScriptGroups.Any())
                    {
                        AddScriptToScriptGroups(script.Id, addedScriptGroups);
                    }

                    //Handle script methods.
                    m = GXSelectArgs.SelectAll<GXScriptMethod>(where => where.Script == script);
                    List<GXScriptMethod> scriptMethods = await _host.Connection.SelectAsync<GXScriptMethod>(m);
                    var comparer2 = new GXScriptMethodComparer();
                    List<GXScriptMethod> removedScriptMethods = scriptMethods.Except(script.Methods, comparer2).ToList();
                    List<GXScriptMethod> addedScriptMethods = script.Methods.Except(scriptMethods, comparer2).ToList();
                    if (removedScriptMethods.Any())
                    {
                        RemoveScriptMethodsFromScript(removedScriptMethods);
                    }
                    if (addedScriptMethods.Any())
                    {
                        AddScriptMethodsToScript(script, addedScriptMethods);
                    }
                    if (changed)
                    {
                        //Notify the workflow handler that the scipt has been updated.
                        //This will cause that script is reload.
                        _workflowHandler.Update(script);
                    }
                    else if (script.Active.HasValue && script.Active.Value != active)
                    {
                        //If script active state has changed.
                        _workflowHandler.Update(script);
                    }
                }
                updates[script] = await GetUsersAsync(User, script.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ScriptUpdate(it.Value, new GXScript[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map script group to user groups.
        /// </summary>
        /// <param name="scriptId">Script ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        private void AddScriptToScriptGroups(Guid scriptId, IEnumerable<GXScriptGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScriptGroupScript> list = new();
            foreach (GXScriptGroup it in groups)
            {
                list.Add(new GXScriptGroupScript()
                {
                    ScriptId = scriptId,
                    ScriptGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between script group and script.
        /// </summary>
        /// <param name="scriptId">Script ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        private void RemoveScriptsFromScriptGroup(Guid scriptId, IEnumerable<GXScriptGroup> groups)
        {
            List<GXScriptGroupScript> list = new();
            foreach (var it in groups)
            {
                list.Add(new GXScriptGroupScript()
                {
                    ScriptId = scriptId,
                    ScriptGroupId = it.Id,
                });
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(list));
        }

        /// <summary>
        /// Add script methods to script.
        /// </summary>
        /// <param name="script">Script where methods are added.</param>
        /// <param name="methods">Added script methods.</param>
        private void AddScriptMethodsToScript(GXScript script, IEnumerable<GXScriptMethod> methods)
        {
            foreach (GXScriptMethod it in methods)
            {
                it.Script = script;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(methods));
        }

        /// <summary>
        /// Remove script methods from the script.
        /// </summary>
        /// <param name="methods">Removed script methods.</param>
        private void RemoveScriptMethodsFromScript(IEnumerable<GXScriptMethod> methods)
        {
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(methods));
        }

        /// <inheritdoc cref="IScriptRepository.Compile"/>
        public byte[]? Compile(ClaimsPrincipal User, string fileName, string script, string? additionalNamespaces, List<GXScriptMethod> methods, out string? errorJson, out int compileTime)
        {
            errorJson = null;
            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentNullException("No script to validate.");
            }
            List<Script.GXScriptException> errors = new();
            StringBuilder sb = new();
            List<string> namespaces = new();
            namespaces.Add("System");
            namespaces.Add("Gurux.DLMS.AMI.Script");
            namespaces.Add("Gurux.DLMS.AMI.Shared.DIs");
            namespaces.Add("Gurux.DLMS.AMI.Shared.DTOs");
            namespaces.Add("Gurux.DLMS.AMI.Shared.DTOs.Authentication");
            namespaces.Add("Gurux.DLMS.AMI.Shared.DTOs.Enums");
            if (!string.IsNullOrEmpty(additionalNamespaces))
            {
                namespaces.AddRange(additionalNamespaces.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (string it in namespaces)
            {
                if (!string.IsNullOrEmpty(it))
                {
                    sb.AppendLine("using " + it + ";");
                }
            }
            sb.AppendLine("namespace Gurux.DLMS.AMI.GeneratedScript");
            sb.AppendLine("{");
            sb.AppendLine("public class GXAmiScript");
            sb.AppendLine("{");
            sb.AppendLine("public GXAmiScript(IGXAmi ami)");
            sb.AppendLine("{");
            sb.AppendLine("Ami = ami;");
            sb.AppendLine("}");
            sb.AppendLine("public IGXAmi Ami{get;private set;}");
            sb.AppendLine(script);
            sb.AppendLine("}");
            sb.AppendLine("}");
            DateTime start = DateTime.Now;
            List<MethodDeclarationSyntax> scriptMethods = new List<MethodDeclarationSyntax>();
            byte[]? byteAssembly = GXAmiScript.Generate(ScriptLanguage.CSharp, fileName, sb.ToString(), errors, scriptMethods);
            if (methods != null && scriptMethods.Count != 0)
            {
                foreach (MethodDeclarationSyntax it in scriptMethods)
                {
                    string description = "";
                    var trivia = it.GetLeadingTrivia().SingleOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    t.IsKind(SyntaxKind.MultiLineCommentTrivia));
                    if (trivia != default)
                    {
                        var xml = trivia.GetStructure();
                        foreach (var line in xml.GetText().Lines)
                        {
                            string str = line.ToString().Replace("///", "").TrimStart();
                            if (!str.StartsWith("<summary>") &&
                                !str.StartsWith("</summary>"))
                            {
                                description += str;
                            }
                        }
                        Console.WriteLine(xml);
                    }
                    GXScriptMethod m = new GXScriptMethod()
                    {
                        Description = description,
                        Name = it.Identifier.Text,
                        Function = it.ReturnType.ToString() != "void"
                    };

                    methods.Add(m);
                }
            }
            compileTime = (int)(DateTime.Now - start).TotalMilliseconds;
            if (errors.Any())
            {
                errorJson = JsonSerializer.Serialize(errors);
            }
            return byteAssembly;
        }

        /// <inheritdoc cref="IScriptRepository.RunAsync"/>
        public async Task<object?> RunAsync(ClaimsPrincipal User, Guid methodId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptMethod>(q => q.Id == methodId);
            arg.Columns.Add<GXScript>();
            arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script, j => j.Id);
            GXScriptMethod method = _host.Connection.SingleOrDefault<GXScriptMethod>(arg);
            if (method == null || method.Script == null || method.Script.ByteAssembly == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            GXAmiScript tmp = new(_serviceProvider);
            tmp.Claims = User;
            if (User != null)
            {
                string? userId = ServerHelpers.GetUserId(User);
                if (userId != null)
                {
                    arg = GXSelectArgs.SelectAll<GXUser>(q => q.Id == userId && q.Removed == null);
                    tmp.User = _host.Connection.SingleOrDefault<GXUser>(arg);
                    tmp.Sender = tmp.User;
                }
            }
            GXScriptRunArgs args = new GXScriptRunArgs()
            {
                ByteAssembly = method.Script.ByteAssembly,
                MethodName = method.Name,
                Asyncronous = method.Asyncronous
            };
            try
            {
                return await tmp.RunAsync(args);
            }
            finally
            {
                //Unload script after the run.
                if (args.AssemblyLoadContext != null)
                {
                    args.AssemblyLoadContext.Unload();
                }
            }
        }
    }
}
