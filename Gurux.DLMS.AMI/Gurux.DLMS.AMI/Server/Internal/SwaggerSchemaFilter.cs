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
using AutoMapper.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Xml.Linq;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// This class is used to filter Swagger schema.
    /// </summary>
    public class SwaggerSchemaFilter : ISchemaFilter
    {
        class SchemaTreeArg
        {
            public string Name { get; set; }
            /// <summary>
            /// Included properties.
            /// </summary>
            public IEnumerable<PropertyInfo> Included;
            /// <summary>
            /// Excluded properties.
            /// </summary>
            public IEnumerable<PropertyInfo> Excluded;
            /// <summary>
            /// Context
            /// </summary>
            public SchemaFilterContext Context;
            /// <summary>
            /// Build objects.
            /// </summary>
            public Dictionary<string, OpenApiSchema> Cache;
            /// <summary>
            /// Constructor.
            /// </summary>
            public SchemaTreeArg(string name,
                IEnumerable<PropertyInfo> included,
                IEnumerable<PropertyInfo> excluded,
                SchemaFilterContext context,
                Dictionary<string, OpenApiSchema> cache)
            {
                Name = name;
                Included = included;
                Excluded = excluded;
                Context = context;
                Cache = cache;
            }

        }

        ILogger<SwaggerSchemaFilter> _logger;
        /// <summary>
        /// Constructor.
        /// </summary>
        public SwaggerSchemaFilter(ILogger<SwaggerSchemaFilter> logger)
        {
            _logger = logger;
        }
        private void UpdateProperties(Type? type,
            IEnumerable<PropertyInfo> excluded,
            IDictionary<string, OpenApiSchema> properties,
            Dictionary<string, OpenApiSchema> schemas)
        {
            if (type == null)
            {
                return;
            }
            foreach (var prop in type.GetProperties())
            {
                bool removed = false;
                foreach (var a in excluded)
                {
                    var attribs = a.GetCustomAttributes<ExcludeSwaggerAttribute>();
                    foreach (var it in attribs)
                    {
                        if (type == it.Type)
                        {
                            if (it.Exclude.Where(w => string.Compare(w, prop.Name, true) == 0).SingleOrDefault() != null)
                            {
                                properties.Remove(prop.Name);
                                removed = true;
                                break;
                            }
                            break;
                        }
                    }
                    if (!removed &&
                        prop.PropertyType != null && prop.PropertyType.IsClass &&
                        prop.PropertyType != typeof(string) &&
                        prop.PropertyType != typeof(Guid) &&
                        prop.PropertyType != typeof(Byte))
                    {
                        Type? tp;
                        if (prop.PropertyType.GetIEnumerableType() != null)
                        {
                            tp = prop.PropertyType.GetElementType();
                        }
                        else
                        {
                            tp = prop.PropertyType;
                        }
                        if (tp != null)
                        {
                            properties.Remove(prop.Name);
                            var objectSchema = schemas.Where(w => w.Key == tp.Name).SingleOrDefault();
                            OpenApiSchema target = new OpenApiSchema { Type = objectSchema.Value.Type };
                            foreach (var p in objectSchema.Value.Properties)
                            {
                                target.Properties.Add(p);
                            }
                            UpdateProperties(tp, excluded, target.Properties, schemas);
                            properties.Add(prop.Name, target);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get base property type.
        /// </summary>
        /// <param name="type">Property type.</param>
        /// <returns>Property type.</returns>
        private static Type? GetArrayPropertyType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            if (type != null &&
                type.IsClass &&
                type != typeof(string) &&
                type != typeof(Guid))
            {
                Type? ret = null;
                if (type.IsArray)
                {
                    ret = type.GetElementType();
                }
                else if (type.GetIEnumerableType() != null)
                {
                    ret = type.GenericTypeArguments[0];
                }

                if (ret != null
                    && ret.IsClass &&
                    ret != typeof(string) &&
                    ret != typeof(Guid) &&
                    ret != typeof(Byte))
                {
                    return ret;
                }
            }
            return null;
        }


        /// <summary>
        /// Get base property type.
        /// </summary>
        /// <param name="type">Property type.</param>
        /// <returns>Property type.</returns>
        private static Type? GetPropertyType(Type type)
        {
            Type? ret = type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                ret = Nullable.GetUnderlyingType(type);
            }
            if (ret != null &&
                ret.IsClass &&
                ret.GetIEnumerableType() == null &&
                ret != typeof(string) &&
                ret != typeof(Guid) &&
                ret != typeof(Byte))
            {
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Convert first char to lower case.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns>Converted string.</returns>
        private static string FirstCharToLowerCase(string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
        }

        /// <summary>
        /// Is property excluded.
        /// </summary>
        /// <param name="args">Schema tree arguments.</param>
        /// <param name="type">Proerty type.</param>
        /// <param name="name">Property name.</param>
        /// <returns>True, if property is excluded.</returns>
        private static bool IsExcluded(
            SchemaTreeArg args,
            Type type,
            string name)
        {
            foreach (var a in args.Included)
            {
                var tmp = a.GetCustomAttributes<IncludeSwaggerAttribute>();
                foreach (var it in tmp)
                {
                    if (it.Type == type)
                    {
                        if (it.Include.Where(w => string.Compare(w, name, true) == 0).SingleOrDefault() != null)
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            foreach (var a in args.Excluded)
            {
                var tmp = a.GetCustomAttributes<ExcludeSwaggerAttribute>();
                foreach (var it in tmp)
                {
                    if (it.Type == type && it.Exclude.Where(w => string.Compare(w, name, true) == 0).SingleOrDefault() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Is property included.
        /// </summary>
        /// <param name="args">Schema tree arguments.</param>
        /// <param name="type">Proerty type.</param>
        /// <param name="prop">Property info.</param>
        /// <returns>True, if property is excluded.</returns>
        private static bool IsIncluded(
            SchemaTreeArg args,
            Type type,
            PropertyInfo prop)
        {
            foreach (var a in args.Included)
            {
                var tmp = a.GetCustomAttributes<IncludeSwaggerAttribute>();
                foreach (var it in tmp)
                {
                    if (it.Type == prop.PropertyType)
                    {
                        return true;
                    }
                    if (it.Type == type)
                    {
                        if (it.Include.Where(w => string.Compare(w, prop.Name, true) == 0).SingleOrDefault() != null)
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            foreach (var a in args.Excluded)
            {
                var tmp = a.GetCustomAttributes<ExcludeSwaggerAttribute>();
                foreach (var it in tmp)
                {
                    if (it.Type == prop.PropertyType)
                    {
                        if (it.Exclude.Where(w => string.Compare(w, prop.Name, true) == 0).SingleOrDefault() != null)
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Build Swagger schema tree.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="target">Target object type.</param>
        /// <param name="schema">Open api schema.</param>
        private void BuildSchemaTree(
            SchemaTreeArg args,
            Type target,
            OpenApiSchema schema)
        {
            Type? type;
            foreach (var p in target.GetProperties())
            {
                if (!IsExcluded(args, target, p.Name))
                {
                    // Console.WriteLine(" -" + p.Name);
                    type = GetArrayPropertyType(p.PropertyType);
                    if (type != null)
                    {
                        string name = type.Name;
                        var objectSchema3 = args.Context.SchemaRepository.Schemas.Where(w => w.Key == name).SingleOrDefault();
                        if (objectSchema3.Key != null)
                        {
                            name = FirstCharToLowerCase(p.Name);
                            schema.Properties.Remove(name);
                            if (!args.Cache.ContainsKey(type.Name))
                            {
                                OpenApiSchema s = new OpenApiSchema { Type = "array" };
                                schema.Properties.Add(name, s);
                                s.Items = new OpenApiSchema { Type = objectSchema3.Value.Type };
                                args.Cache[type.Name] = s.Items;
                                // Console.WriteLine("{");
                                BuildSchemaTree(args, type, s.Items);
                                // Console.WriteLine("}");
                            }
                            else
                            {
                                schema.Properties.Add(name, args.Cache[type.Name]);
                                _logger.LogInformation(name + " gets " + type.Name + " from the cache.");
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to get " + target.Name + "." + p.Name + ".");
                        }
                    }
                    else
                    {
                        type = GetArrayPropertyType(target);
                        if (type == null)
                        {
                            type = target;
                        }

                        var objectSchema2 = args.Context.SchemaRepository.Schemas.Where(w => w.Key == type.Name).SingleOrDefault();
                        var t = objectSchema2.Value.Properties.Where(w => string.Compare(w.Key, p.Name, true) == 0).SingleOrDefault();
                        //Property is not found from the schema when JsonIgnore is used.
                        if (t.Key != null)
                        {
                            if (GetPropertyType(p.PropertyType) != null)
                            {
                                string name = FirstCharToLowerCase(p.Name);
                                if (!args.Cache.ContainsKey(p.PropertyType.Name))
                                {
                                    OpenApiSchema s = new OpenApiSchema
                                    {
                                        Type = "object",
                                        ReadOnly = t.Value.ReadOnly,
                                        WriteOnly = t.Value.WriteOnly
                                    };
                                    args.Cache[p.PropertyType.Name] = s;
                                    schema.Properties.Add(t.Key, s);
                                    type = GetPropertyType(p.PropertyType);
                                    if (type == null)
                                    {
                                        type = p.PropertyType;
                                    }
                                    if (IsIncluded(args, target, p))
                                    {
                                        // Console.WriteLine("{");
                                        BuildSchemaTree(args, type, s);
                                        // Console.WriteLine("}");
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Property is not included " + args.Name + "." + p.Name);
                                    }
                                }
                                else
                                {
                                    schema.Properties.Add(t.Key, args.Cache[p.PropertyType.Name]);
                                    _logger.LogInformation(target.Name + " gets " + p.PropertyType.Name + " from the cache.");
                                }
                            }
                            else
                            {
                                OpenApiSchema s = new OpenApiSchema
                                {
                                    Type = t.Value.Type,
                                    Format = t.Value.Format,
                                    ReadOnly = t.Value.ReadOnly,
                                    WriteOnly = t.Value.WriteOnly
                                };
                                schema.Properties.Add(t.Key, s);
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
            {
                return;
            }
            if (!context.Type.IsClass ||
                context.Type == typeof(string) ||
                context.Type == typeof(Guid) ||
                context.Type == typeof(byte))
            {
                return;
            }
            // Console.WriteLine("------------------------------------------------------------");
            // Console.WriteLine(context.Type.Name);
            var include = context.Type.GetProperties()
                .Where(t => t.GetCustomAttributes<IncludeSwaggerAttribute>().Any());
            var exclude = context.Type.GetProperties()
                .Where(t => t.GetCustomAttributes<ExcludeSwaggerAttribute>().Any());
            if (include.Any() || exclude.Any())
            {
                var list = new List<PropertyInfo>();
                list.AddRange(include);
                list.AddDistinct(exclude);
                foreach (var a in list)
                {
                    Type? type = GetArrayPropertyType(a.PropertyType);
                    if (type == null)
                    {
                        type = a.PropertyType;
                    }
                    string name = FirstCharToLowerCase(a.Name);
                    schema.Properties.Remove(name);
                    var objectSchema3 = context.SchemaRepository.Schemas.Where(w => w.Key == type.Name).SingleOrDefault();
                    Dictionary<string, OpenApiSchema> cache = new Dictionary<string, OpenApiSchema>();
                    OpenApiSchema s;
                    if (a.PropertyType.GetIEnumerableType() != null &&
                        a.PropertyType != typeof(byte) &&
                        a.PropertyType != typeof(string) &&
                        a.PropertyType != typeof(Guid))
                    {
                        s = new OpenApiSchema { Type = "array" };
                        s.Items = new OpenApiSchema();
                        SchemaTreeArg args = new SchemaTreeArg(context.Type.Name,
                            include,
                            exclude,
                            context,
                            cache);
                        BuildSchemaTree(args, type, s.Items);
                    }
                    else
                    {
                        s = new OpenApiSchema { Type = objectSchema3.Value.Type };
                        SchemaTreeArg args = new SchemaTreeArg(
                            context.Type.Name,
                            include,
                            exclude,
                            context,
                            cache);
                        BuildSchemaTree(args, type, s);
                    }
                    schema.Properties.Add(name, s);
                }
            }
            // Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }
}
