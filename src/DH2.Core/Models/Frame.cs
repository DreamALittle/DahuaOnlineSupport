using OpenCvSharp;

namespace DH2.Core.Models;

/// <summary>
/// 一帧截屏。<see cref="Image"/> 由调用方负责释放(OpenCvSharp <c>Mat</c> 实现了 <c>IDisposable</c>)。
/// </summary>
/// <remarks>
/// <para>M0-S2 改造(RJ-S2-02 / DEF-S2-02):</para>
/// <list type="bullet">
///   <item><see cref="Width"/> 与 <see cref="Height"/> 在构造期通过 <c>init</c> 表达式一次性缓存(<c>Image.Width</c> / <c>Image.Height</c>)。</item>
///   <item>因此 <see cref="Image"/> 被 <c>Dispose</c> 后,这两个属性仍然返回有效数值,避免访问已释放 Mat 触发的访问违例(<c>0xC0000005</c>)。</item>
///   <item>空帧占位(<c>new Mat()</c>)同样安全:构造期缓存 0;占位 Mat 仍可重复使用(由 GdiCapture.EmptyFrame 按需 new)。</item>
/// </list>
/// </remarks>
public sealed record Frame(long Hwnd, DateTime Timestamp, Mat Image)
{
    /// <summary>帧宽度(像素),构造期一次性缓存(<c>Image.Width</c>)。<see cref="Image"/> 释放后仍可读。</summary>
    public int Width { get; init; } = Image.Width;

    /// <summary>帧高度(像素),构造期一次性缓存(<c>Image.Height</c>)。<see cref="Image"/> 释放后仍可读。</summary>
    public int Height { get; init; } = Image.Height;
}
