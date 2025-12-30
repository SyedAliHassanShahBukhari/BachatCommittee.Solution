// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BachatCommittee.Models.DTOs.Responses;

public sealed record PagedResponseDto<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
