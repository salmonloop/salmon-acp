// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1801 // Review unused parameters (polyfill)

#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for the init-only property support required by C# 9+ record types
/// when targeting frameworks older than .NET 5 (e.g., netstandard2.1).
/// </summary>
internal static class IsExternalInit
{
}

#endif
