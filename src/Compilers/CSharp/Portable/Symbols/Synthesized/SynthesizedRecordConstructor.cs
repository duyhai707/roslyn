﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordConstructor : SynthesizedInstanceConstructor
    {
        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public SynthesizedRecordConstructor(
            SourceMemberContainerTypeSymbol containingType,
            Binder parameterBinder,
            ParameterListSyntax parameterList,
            DiagnosticBag diagnostics)
            : base(containingType)
        {
            Parameters = ParameterHelpers.MakeParameters(
                parameterBinder,
                this,
                parameterList,
                out _,
                diagnostics,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: false);
        }

        internal override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements, DiagnosticBag diagnostics)
        {
            // Write assignments to backing fields
            //
            // {
            //     this.backingField1 = arg1
            //     ...
            //     this.backingFieldN = argN
            // }
            var containing = (SourceMemberContainerTypeSymbol)ContainingType;
            foreach (var param in Parameters)
            {
                var members = containing.GetMembers(param.Name);
                if (members.Length == 1 && members[0] is SynthesizedRecordPropertySymbol prop)
                {
                    var field = prop.BackingField;
                    statements.Add(F.Assignment(F.Field(F.This(), field), F.Parameter(param)));
                }
            }
        }
    }
}