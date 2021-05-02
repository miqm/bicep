// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bicep.Core.FileSystem;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;

namespace Bicep.Core.Semantics
{
    public interface IBinder : ISyntaxHierarchy
    {
        ResourceScope TargetScope { get; }

        FileSymbol FileSymbol { get; }

        Uri FileUri { get; }

        IFileResolver FileResolver { get; }


        Symbol? GetSymbolInfo(SyntaxBase syntax);

        ImmutableArray<DeclaredSymbol>? TryGetCycle(DeclaredSymbol declaredSymbol);
    }
}
