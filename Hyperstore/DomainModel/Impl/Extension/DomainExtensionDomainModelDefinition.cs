﻿// Copyright 2014 Zenasoft.  All rights reserved.
//
// This file is part of Hyperstore.
//
//    Hyperstore is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Hyperstore is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Hyperstore.  If not, see <http://www.gnu.org/licenses/>.
 
#region Imports

using Hyperstore.Modeling.DomainExtension;

#endregion

namespace Hyperstore.Modeling
{
    internal class ExtensionSchemaDefinition : SchemaDefinition
    {
        private readonly ISchema _extendedSchema;
        private readonly ISchemaDefinition _definition;
        private readonly SchemaConstraintExtensionMode _mode;

        internal ExtensionSchemaDefinition(ISchemaDefinition definition, ISchema extendedSchema, SchemaConstraintExtensionMode mode) 
            : base(definition.SchemaName)
        {
            DebugContract.Requires(extendedSchema, "extendedSchema");
            DebugContract.Requires(definition, "definition");

            _definition = definition;
            _extendedSchema = extendedSchema;
            _mode = mode;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Called when [schema loaded].
        /// </summary>
        /// <param name="schema">
        ///  The schema.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        protected override void OnSchemaLoaded(ISchema schema)
        {
            _definition.OnSchemaLoaded(schema);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Defines the schema.
        /// </summary>
        /// <param name="schema">
        ///  The schema.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        protected override void DefineSchema(ISchema schema)
        {
            _definition.DefineSchema(schema);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Prepare dependency resolver.
        /// </summary>
        /// <param name="defaultDependencyResolver">
        ///  The default dependency resolver.
        /// </param>
        /// <returns>
        ///  An IDependencyResolver.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        protected override IDependencyResolver PrepareDependencyResolver(IDependencyResolver defaultDependencyResolver)
        {
            return _definition.PrepareDependencyResolver(defaultDependencyResolver);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Creates a schema. </summary>
        /// <param name="resolver"> The domain resolver. </param>
        /// <returns>   The new schema. </returns>
        ///-------------------------------------------------------------------------------------------------
        protected override ISchema CreateSchema(IDependencyResolver resolver)
        {
            return new Hyperstore.Modeling.DomainExtension.DomainSchemaExtension(_extendedSchema,
                                                                              resolver,
                                                                              Behavior,
                                                                              new Hyperstore.Modeling.DomainExtension.DomainExtensionConstraintsManager(resolver, _extendedSchema, _mode));
        }
    }
}