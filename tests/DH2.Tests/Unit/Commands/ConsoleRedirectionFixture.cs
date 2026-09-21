// DH2.Tests — Console 重定向串行化 fixture
// xUnit 默认同 Collection 内测试串行运行。跨测试类(都标 [Collection("ConsoleRedirection")])
// 共享此 fixture,保证 Console.Out/Error 重定向不会并发串。
using Xunit;

namespace DH2.Tests.Unit.Commands;

public sealed class ConsoleRedirectionFixture
{
    public object Lock { get; } = new();
}

[CollectionDefinition("ConsoleRedirection")]
public class ConsoleRedirectionCollection : ICollectionFixture<ConsoleRedirectionFixture>
{
}
