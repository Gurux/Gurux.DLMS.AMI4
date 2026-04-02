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

using Gurux.DLMS.AMI.Shared;
using Microsoft.OpenApi;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Internal
{
    internal class GXOpenApiSchemaFilter
    {
        static string? Document;
        internal static IResult MakeOpenApiDocument()
        {
            var doc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Gurux.DLMS.AMI API", Version = "v1" },
                Paths = new OpenApiPaths(),
                Components = new OpenApiComponents()
            };
            if (Document == null)
            {
                var asm = Assembly.GetEntryAssembly()!;
                var controllers = asm.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t))
                    .ToList();
                foreach (var it in controllers)
                {
                    var routePrefix = it.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>()?.Template
                                          ?? $"api/{it.Name.Replace("Controller", "")}";
                    routePrefix = routePrefix.Replace("[controller]", it.Name.Replace("Controller", ""));
                    var methods = it.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        var httpAttr = m.GetCustomAttributes()
                            .FirstOrDefault(a => a.GetType().Name is
                                "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or
                                "HttpDeleteAttribute" or "HttpPatchAttribute");
                        if (httpAttr is null)
                        {
                            continue;
                        }
                        var verb = httpAttr.GetType().Name.Replace("Http", "").Replace("Attribute", "").ToUpperInvariant();
                        var opType = verb switch
                        {
                            "GET" => HttpMethod.Get,
                            "POST" => HttpMethod.Post,
                            "PUT" => HttpMethod.Put,
                            "DELETE" => HttpMethod.Delete,
                            "PATCH" => HttpMethod.Patch,
                            _ => HttpMethod.Get
                        };

                        var methodTemplate = (string?)httpAttr.GetType().GetProperty("Template")?.GetValue(httpAttr);
                        var fullPath = CombineRoute(routePrefix, methodTemplate);
                        if (!doc.Paths.TryGetValue(fullPath, out var pathItem))
                        {
                            pathItem = doc.Paths[fullPath] = new OpenApiPathItem()
                            {
                                Operations = new Dictionary<HttpMethod, OpenApiOperation>()
                            };
                        }

                        var op = new OpenApiOperation
                        {
                            Summary = $"{it.Name.Replace("Controller", "")}.{m.Name}",
                            Responses = new OpenApiResponses(),
                            Parameters = new List<IOpenApiParameter>()
                        };

                        foreach (var seg in ParseRouteParams(fullPath))
                        {
                            op.Parameters.Add(new OpenApiParameter
                            {
                                Name = seg,
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                            });
                        }

                        var bodyParam = m.GetParameters()
                            .FirstOrDefault(p => !IsPrimitiveLike(p.ParameterType) &&
                                                 p.ParameterType != typeof(CancellationToken));

                        List<ExcludeOpenApiAttribute> excluded = new List<ExcludeOpenApiAttribute>();
                        List<IncludeOpenApiAttribute> included = new List<IncludeOpenApiAttribute>();
                        List<Type> stack = new List<Type>();
                        if (bodyParam is not null)
                        {
                            var inline = BuildInlineSchema(excluded, included,
                                bodyParam.ParameterType, maxDepth: 10, stack);
                            op.RequestBody = new OpenApiRequestBody
                            {
                                Required = true,
                                Content = new Dictionary<string, IOpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType { Schema = inline }
                                }
                            };
                        }

                        var retType = UnwrapReturnType(m.ReturnType);
                        if (retType is null || retType == typeof(void))
                        {
                            op.Responses["200"] = new OpenApiResponse { Description = "OK" };
                        }
                        else
                        {
                            var inline = BuildInlineSchema(excluded, included, retType,
                                maxDepth: 10, stack);
                            op.Responses["200"] = new OpenApiResponse
                            {
                                Description = "OK",
                                Content = new Dictionary<string, IOpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType { Schema = inline }
                                }
                            };
                        }
                        pathItem.Operations[opType] = op;
                    }
                }

                using var sw = new StringWriter();
                var jw = new OpenApiJsonWriter(sw);
                doc.SerializeAsV3(jw);
                Document = sw.ToString();
            }
            return Results.Text(Document, "application/json");
        }

        static Type? UnwrapReturnType(Type t)
        {
            if (typeof(Task).IsAssignableFrom(t))
            {
                t = t.IsGenericType ? t.GetGenericArguments()[0] : typeof(void);
            }
            if (t.IsGenericType && t.Name.StartsWith("ActionResult"))
            {
                t = t.GetGenericArguments().FirstOrDefault() ?? typeof(void);
            }
            return t;
        }

        static string GetSchemaName(Type t)
        {
            t = UnderlyingNullable(t);
            if (t.IsGenericType)
            {
                var gen = string.Join("", t.GetGenericArguments().Select(GetSchemaName));
                var name = t.Name.Split('`')[0];
                return name + gen;
            }
            return t.Name;
        }

        static string CombineRoute(string? prefix, string? methodTemplate)
        {
            var p = string.IsNullOrWhiteSpace(prefix) ? "" : prefix!;
            var m = string.IsNullOrWhiteSpace(methodTemplate) ? "" : methodTemplate!;
            var path = (p + "/" + m).Replace("//", "/");
            if (!path.StartsWith("/")) path = "/" + path;
            return path;
        }

        static IEnumerable<string> ParseRouteParams(string path)
        {
            int i = 0;
            while (i < path.Length)
            {
                var open = path.IndexOf('{', i);
                if (open < 0) yield break;
                var close = path.IndexOf('}', open + 1);
                if (close < 0) yield break;
                yield return path.Substring(open + 1, close - open - 1).Split(':')[0];
                i = close + 1;
            }
        }

        static bool IsPrimitiveLike(Type t) => TryMapSimple(t, out _);

        private readonly ILogger<GXOpenApiSchemaFilter> _logger;

        public GXOpenApiSchemaFilter(ILogger<GXOpenApiSchemaFilter> logger) => _logger = logger;


        private static OpenApiSchema BuildInlineSchema(
            List<ExcludeOpenApiAttribute> excluded,
            List<IncludeOpenApiAttribute> included,
            Type type,
            int maxDepth,
            List<Type> stack)
        {
            type = UnderlyingNullable(type);
            if (stack.Count > maxDepth)
            {
                string first = stack.First().Name;
                throw new Exception(string.Format("maxDepth exceeded in {0}.", first));
            }
            if (TryMapSimple(type, out var simple))
            {
                return simple;
            }

            if (IsEnumerable(type, out var elem))
            {
                var items = BuildInlineSchema(excluded, included, elem, maxDepth, stack);
                return new OpenApiSchema { Type = JsonSchemaType.Array, Items = items };
            }

            if (IsDictionary(type, out var valT))
            {
                var v = BuildInlineSchema(excluded, included, valT, maxDepth, stack);
                return new OpenApiSchema { Type = JsonSchemaType.Object, AdditionalProperties = v };
            }

            if (type.IsEnum)
            {
                var names = Enum.GetNames(type).Select(n => (JsonNode)JsonValue.Create(n)!).ToList();
                return new OpenApiSchema { Type = JsonSchemaType.String, Enum = names };
            }
            var obj = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string>(),
                Properties = new Dictionary<string, IOpenApiSchema>()
            };
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead);
#if DEBUG
            if (!IsPrimitiveLike(type))
            {
                Debug.WriteLine(new string(' ', stack.Count) + type.Name);
            }
#endif
            stack.Add(type);
            foreach (var p in props)
            {
                if (p.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                {
                    continue;
                }
                bool ignore = false;
                if (p.GetCustomAttributes<ExcludeOpenApiAttribute>().Any() ||
                    p.GetCustomAttributes<IncludeOpenApiAttribute>().Any())
                {
                    excluded.AddRange(p.GetCustomAttributes<ExcludeOpenApiAttribute>());
                    included.AddRange(p.GetCustomAttributes<IncludeOpenApiAttribute>());
                }
                else
                {
                    //Check is property excluded.
                    if (excluded.Any())
                    {
                        var list = excluded.Where(w => w.Type == type);
                        foreach (var e2 in list)
                        {
                            if (e2.Exclude.Where(w => w == p.Name).Any())
                            {
                                ignore = true;
                            }
                        }
                    }
                    //Check is property included.
                    if (!ignore && included.Any())
                    {
                        var list = included.Where(w => w.Type == type);
                        //If property is included it must be on the list or it's ignored.
                        bool found = !list.Any();
                        foreach (var e2 in list)
                        {
                            if (e2.Include.Where(w => w == p.Name).Any())
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            ignore = true;
                        }
                    }

                }
                if (!ignore)
                {
                    var pt = UnderlyingNullable(p.PropertyType);
#if DEBUG
                    if (!IsPrimitiveLike(pt))
                    {
                        Debug.WriteLine(new string(' ', stack.Count) + p.PropertyType + ":" + p.Name);
                    }
#endif
                    var child = BuildInlineSchema(excluded, included, pt, maxDepth, stack);
                    obj.Properties[ToCamel(p.Name)] = child;
                    if (p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) is null)
                    {
                        obj.Required.Add(ToCamel(p.Name));
                    }
                }
            }
            stack.Remove(type);
            return obj;
        }

        private static string ToCamel(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

        private static Type UnderlyingNullable(Type t) => Nullable.GetUnderlyingType(t) ?? t;

        private static bool TryMapSimple(Type t, out OpenApiSchema schema)
        {
            t = UnderlyingNullable(t);

            if (t == typeof(string)) { schema = new OpenApiSchema { Type = JsonSchemaType.String }; return true; }
            if (t == typeof(bool)) { schema = new OpenApiSchema { Type = JsonSchemaType.Boolean }; return true; }
            if (t == typeof(int)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }; return true; }
            if (t == typeof(long)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" }; return true; }
            if (t == typeof(float)) { schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" }; return true; }
            if (t == typeof(double) || t == typeof(decimal)) { schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" }; return true; }
            if (t == typeof(DateTime) || t == typeof(DateTimeOffset)) { schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" }; return true; }
            if (t == typeof(Guid)) { schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }; return true; }
            if (t == typeof(byte[])) { schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "byte" }; return true; }
            if (t == typeof(byte)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = 0.ToString(CultureInfo.InvariantCulture), Maximum = byte.MaxValue.ToString(CultureInfo.InvariantCulture) }; return true; }
            if (t == typeof(ushort)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = 0.ToString(CultureInfo.InvariantCulture), Maximum = ushort.MaxValue.ToString(CultureInfo.InvariantCulture) }; return true; }
            if (t == typeof(uint)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = 0.ToString(CultureInfo.InvariantCulture), Maximum = uint.MaxValue.ToString(CultureInfo.InvariantCulture) }; return true; }
            if (t == typeof(ulong)) { schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64", Minimum = 0.ToString(CultureInfo.InvariantCulture), Maximum = ulong.MaxValue.ToString(CultureInfo.InvariantCulture) }; return true; }

            if (t.IsEnum)
            {
                var names = Enum.GetNames(t).Select(n => (JsonNode)JsonValue.Create(n)!).ToList();
                schema = new OpenApiSchema { Type = JsonSchemaType.String, Enum = names };
                return true;
            }

            schema = default!;
            return false;
        }

        static bool IsEnumerable(Type t, out Type elem)
        {
            if (t == typeof(string))
            {
                elem = null!;
                return false;
            }
            if (t.IsArray)
            {
                elem = t.GetElementType()!;
                return true;
            }

            var ienum = t.GetInterfaces().Concat([t])
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (ienum != null)
            {
                elem = ienum.GetGenericArguments()[0];
                return true;
            }
            elem = null!;
            return false;
        }

        private static bool IsDictionary(Type t, out Type valueType)
        {
            valueType = null!;
            var idict = t.GetInterfaces().Concat(new[] { t })
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (idict != null && idict.GetGenericArguments()[0] == typeof(string))
            {
                valueType = idict.GetGenericArguments()[1];
                return true;
            }
            return false;
        }
    }
}
