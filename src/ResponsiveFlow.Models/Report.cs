using System.Collections.Generic;

namespace ResponsiveFlow;

public readonly record struct Report(IReadOnlyCollection<ResponseInfo> Responses);
