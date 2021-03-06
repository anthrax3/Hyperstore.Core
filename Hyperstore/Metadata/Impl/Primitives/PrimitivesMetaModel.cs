﻿//	Copyright © 2013 - 2014, Alain Metge. All rights reserved.
//
//		This file is part of Hyperstore (http://www.hyperstore.org)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Hyperstore.Modeling.HyperGraph;
using Hyperstore.Modeling.Commands;
using System.Diagnostics;

#endregion

namespace Hyperstore.Modeling.Metadata
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///  The primitives schema.
    /// </summary>
    /// <seealso cref="T:Hyperstore.Modeling.ISchema"/>
    ///-------------------------------------------------------------------------------------------------
    public class PrimitivesSchema : InternalSchema, ISchema<PrimitivesSchemaDefinition>
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the definition.
        /// </summary>
        /// <value>
        ///  The definition.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public PrimitivesSchemaDefinition Definition
        {
            get { return null; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Name of the domain model.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public static string DomainModelName = "$"; // Name of the domain model

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services">
        ///  The services.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        internal PrimitivesSchema(IServicesContainer services)
            : base(services, "", DomainModelName)
        {
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the metadata.
        /// </summary>
        /// <param name="id">
        ///  The identifier.
        /// </param>
        /// <param name="throwErrorIfNotExists">
        ///  true to throw error if not exists.
        /// </param>
        /// <returns>
        ///  The schema information.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        protected override ISchemaInfo GetSchemaInfo(Identity id, bool throwErrorIfNotExists)
        {
            if (id == Identity.Empty)
                return SchemaEntitySchema;

            return base.GetSchemaInfo(id, throwErrorIfNotExists);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///  resources.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public override void Dispose()
        {
            base.Dispose();

            StringSchema = null;
            BooleanSchema = null;
            DateTimeSchema = null;
            TimeSpanSchema = null;
            SingleSchema = null;
            DecimalSchema = null;
            UInt16Schema = null;
            UInt32Schema = null;
            UInt64Schema = null;
            Int16Schema = null;
            Int32Schema = null;
            Int64Schema = null;
            DoubleSchema = null;
            GuidSchema = null;
            CharSchema = null;
            ByteSchema = null;
            ByteArraySchema = null;
            SchemaElementSchema = null;
            SchemaEntitySchema = null;
            SchemaValueObjectSchema = null;
            GeneratedSchemaEntitySchema = null;
            SchemaPropertySchema = null;
            IdentitySchema = null;
            TypeSchema = null;
            SchemaRelationshipSchema = null;
            ModelEntitySchema = null;
            ModelRelationshipSchema = null;
            SchemaElementHasPropertiesSchema = null;
            SchemaElementReferencesSuperElementSchema = null;
            SchemaPropertyReferencesSchemaEntitySchema = null;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the string schema.
        /// </summary>
        /// <value>
        ///  The string schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public ISchemaValueObject StringSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the boolean schema.
        /// </summary>
        /// <value>
        ///  The boolean schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject BooleanSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the date time schema.
        /// </summary>
        /// <value>
        ///  The date time schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject DateTimeSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the time span schema.
        /// </summary>
        /// <value>
        ///  The time span schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject TimeSpanSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the single schema.
        /// </summary>
        /// <value>
        ///  The single schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject SingleSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the decimal schema.
        /// </summary>
        /// <value>
        ///  The decimal schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject DecimalSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 16 schema.
        /// </summary>
        /// <value>
        ///  The u int 16 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject UInt16Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 32 schema.
        /// </summary>
        /// <value>
        ///  The u int 32 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject UInt32Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 64 schema.
        /// </summary>
        /// <value>
        ///  The u int 64 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject UInt64Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 16 schema.
        /// </summary>
        /// <value>
        ///  The int 16 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject Int16Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 64 schema.
        /// </summary>
        /// <value>
        ///  The int 64 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject Int64Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the int 32 schema.
        /// </summary>
        /// <value>
        ///  The int 32 schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject Int32Schema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the double schema.
        /// </summary>
        /// <value>
        ///  The double schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject DoubleSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the unique identifier schema.
        /// </summary>
        /// <value>
        ///  The unique identifier schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject GuidSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the character schema.
        /// </summary>
        /// <value>
        ///  The character schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject CharSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the byte schema.
        /// </summary>
        /// <value>
        ///  The byte schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject ByteSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the byte array schema.
        /// </summary>
        /// <value>
        ///  The byte array schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject ByteArraySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema entity schema.
        /// </summary>
        /// <value>
        ///  The schema entity schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity SchemaEntitySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema element schema.
        /// </summary>
        /// <value>
        ///  The schema element schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity SchemaElementSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema value object schema.
        /// </summary>
        /// <value>
        ///  The schema value object schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity SchemaValueObjectSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the generated schema entity schema.
        /// </summary>
        /// <value>
        ///  The generated schema entity schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity GeneratedSchemaEntitySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema property schema.
        /// </summary>
        /// <value>
        ///  The schema property schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity SchemaPropertySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the identity schema.
        /// </summary>
        /// <value>
        ///  The identity schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject IdentitySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the type schema.
        /// </summary>
        /// <value>
        ///  The type schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaValueObject TypeSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema relationship schema.
        /// </summary>
        /// <value>
        ///  The schema relationship schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaRelationship SchemaRelationshipSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the model entity schema.
        /// </summary>
        /// <value>
        ///  The model entity schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaEntity ModelEntitySchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the model relationship schema.
        /// </summary>
        /// <value>
        ///  The model relationship schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaRelationship ModelRelationshipSchema { get; internal set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets or sets the schema element has properties schema.
        /// </summary>
        /// <value>
        ///  The schema element has properties schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaRelationship SchemaElementHasPropertiesSchema { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the schema element references super element schema.
        /// </summary>
        /// <value>
        ///  The schema element references super element schema.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public  ISchemaRelationship SchemaElementReferencesSuperElementSchema { get; internal set; }
        internal  ISchemaRelationship SchemaPropertyReferencesSchemaEntitySchema { get; set; }

    }
}