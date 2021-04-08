// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.Semantics
{
    public class FunctionWildcardOverload : FunctionOverload
    {
        public FunctionWildcardOverload(
            string name,
            string description,
            Regex wildcardRegex,
            ReturnTypeBuilderDelegate returnTypeBuilder,
            TypeSymbol returnType,
            IEnumerable<FixedFunctionParameter> fixedArgumentTypes,
            VariableFunctionParameter? variableArgumentType,
            AdvancedReturnTypeBuilderDelegate?  advancedReturnTypeBuilder,
            ExpressionEmitterDelegate? expressionEmitterDelegate,
            FunctionFlags flags = FunctionFlags.Default)
            : base(name, description, returnTypeBuilder, returnType, fixedArgumentTypes, variableArgumentType, advancedReturnTypeBuilder, expressionEmitterDelegate, flags)
        {
            WildcardRegex = wildcardRegex;
        }

        public Regex WildcardRegex { get; }
    }
}
