namespace DH2.App.Commands;

/// <summary>
/// 控制台表格打印辅助(S2-4 enumerate / capture 输出)。
/// </summary>
/// <remarks>
/// 用户面输出与 Serilog 日志分离(编码规范 §5):本类只走 <see cref="Console.Out"/>。
/// 列宽按最大内容宽度自适;每行用 <see cref="Environment.NewLine"/> 结尾以利跨平台。
/// </remarks>
public static class ConsoleTable
{
    /// <summary>
    /// 以紧凑对齐格式打印单行表(列名 = 数据列,字段值 = 数据行;分隔以 <c>tab</c>)。
    /// </summary>
    /// <param name="headers">列名。</param>
    /// <param name="rows">每行字段值(顺序与 <paramref name="headers"/> 一致)。</param>
    public static void PrintTabbed(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var headerList = headers.ToList();
        Console.WriteLine(string.Join('\t', headerList));

        foreach (var row in rows)
        {
            var cells = row.Select(c => c ?? string.Empty);
            Console.WriteLine(string.Join('\t', cells));
        }
    }
}
