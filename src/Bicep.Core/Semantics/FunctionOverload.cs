// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Azure.Deployments.Expression.Expressions;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using Newtonsoft.Json;

namespace Bicep.Core.Semantics
{
    public class FunctionOverload
    {
        public delegate TypeSymbol ReturnTypeBuilderDelegate(IEnumerable<FunctionArgumentSyntax> arguments);
        public delegate TypeSymbol AdvancedReturnTypeBuilderDelegate(AdvancedReturnTypeBuilderContext context);
        public delegate bool ExpressionEmitterDelegate(JsonTextWriter writer, EmitterContext emitterContext, FunctionCallSyntax functionCallSyntax, FunctionSymbol functionSymbol, out SyntaxBase? expression, out LanguageExpression? languageExpression);

        public class AdvancedReturnTypeBuilderContext
        {
            public IBinder Binder { get; }
            public IDiagnosticWriter Diagnostics { get; }
            public (FunctionArgumentSyntax syntax, TypeSymbol)[] Arguments { get; }

            public AdvancedReturnTypeBuilderContext(IBinder binder, IDiagnosticWriter diagnostic, (FunctionArgumentSyntax syntax, TypeSymbol)[] arguments)
            {
                Binder = binder;
                Diagnostics = diagnostic;
                Arguments = arguments;
            }
        }

        public FunctionOverload(string name, string description, ReturnTypeBuilderDelegate returnTypeBuilder, TypeSymbol returnType, IEnumerable<FixedFunctionParameter> fixedParameters, VariableFunctionParameter? variableParameter, AdvancedReturnTypeBuilderDelegate? advancedReturnTypeBuilder, ExpressionEmitterDelegate? expressionEmitter, FunctionFlags flags = FunctionFlags.Default)
        {
            Name = name;
            Description = description;
            ReturnTypeBuilder = returnTypeBuilder;
            ReturnTypeBuilderAdvanced = advancedReturnTypeBuilder;
            ExpressionEmitter = expressionEmitter;
            FixedParameters = fixedParameters.ToImmutableArray();
            VariableParameter = variableParameter;
            Flags = flags;

            MinimumArgumentCount = FixedParameters.Count(fp => fp.Required) + (VariableParameter?.MinimumCount ?? 0);
            MaximumArgumentCount = VariableParameter == null ? FixedParameters.Length : (int?)null;
            
            TypeSignature = $"({string.Join(", ", ParameterTypeSignatures)}): {returnType}";
        }

        public string Name { get; }

        public string Description { get; }

        public ImmutableArray<FixedFunctionParameter> FixedParameters { get; }

        public int MinimumArgumentCount { get; }

        public int? MaximumArgumentCount { get; }

        public VariableFunctionParameter? VariableParameter { get; }

        public ReturnTypeBuilderDelegate ReturnTypeBuilder { get; }

        public AdvancedReturnTypeBuilderDelegate? ReturnTypeBuilderAdvanced { get; }

        public ExpressionEmitterDelegate? ExpressionEmitter { get; }

        public FunctionFlags Flags { get; }

        public string TypeSignature { get; }

        public IEnumerable<string> ParameterTypeSignatures => this.FixedParameters
            .Select(fp => fp.Signature)
            .Concat(this.VariableParameter?.GenericSignature.AsEnumerable() ?? Enumerable.Empty<string>());

        public bool HasParameters => this.MinimumArgumentCount > 0 || this.MaximumArgumentCount > 0;

        public FunctionMatchResult Match(IList<TypeSymbol> argumentTypes, out ArgumentCountMismatch? argumentCountMismatch, out ArgumentTypeMismatch? argumentTypeMismatch)
        {
            argumentCountMismatch = null;
            argumentTypeMismatch = null;

            if (argumentTypes.Count < this.MinimumArgumentCount ||
                (this.MaximumArgumentCount.HasValue && argumentTypes.Count > this.MaximumArgumentCount.Value))
            {
                // Too few or too many arguments.
                argumentCountMismatch = new ArgumentCountMismatch(argumentTypes.Count, this.MinimumArgumentCount, this.MaximumArgumentCount);

                return FunctionMatchResult.Mismatch;
            }

            if (argumentTypes.All(a => a.TypeKind == TypeKind.Any))
            {
                // all argument types are "any"
                // it's a potential match at best
                return FunctionMatchResult.PotentialMatch;
            }

            for (int i = 0; i < argumentTypes.Count; i++)
            {
                var argumentType = argumentTypes[i];
                TypeSymbol expectedType;

                if (i < this.FixedParameters.Length)
                {
                    expectedType = this.FixedParameters[i].Type;
                }
                else
                {
                    if (this.VariableParameter == null)
                    {
                        // Theoretically this shouldn't happen, becase it already passed argument count checking, either:
                        // - The function takes 0 argument - argumentTypes must be empty, so it won't enter the loop
                        // - The function take at least one argument - when i >= FixedParameterTypes.Length, VariableParameterType
                        //   must not be null, otherwise, the function overload has invalid parameter count definition.
                        throw new ArgumentException($"Got unexpected null value for {nameof(this.VariableParameter)}. Ensure the function overload definition is correct: '{this.TypeSignature}'.");
                    }

                    expectedType = this.VariableParameter.Type;
                }

                if (TypeValidator.AreTypesAssignable(argumentType, expectedType) != true)
                {
                    argumentTypeMismatch = new ArgumentTypeMismatch(this, i, argumentType, expectedType);

                    return FunctionMatchResult.Mismatch;
                }
            }

            return FunctionMatchResult.Match;
        }
    }
}
