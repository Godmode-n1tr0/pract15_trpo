using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TRPO.ElectronicsStore.Service;

namespace TRPO.ElectronicsStore.UI;

internal static class MonitorWorkArea
{

    // Уменьшает окно при необходимости и перемещает его внутрь рабочей области текущего монитора.
    public static void FitToScreen(Window window)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (windowHandle == IntPtr.Zero || !GetMonitorInfo(MonitorFromWindow(windowHandle, 2), ref monitorInfo))
            return;
        var screenScale = VisualTreeHelper.GetDpi(window);
        double width = (monitorInfo.Work.Right - monitorInfo.Work.Left) / screenScale.DpiScaleX;
        double height = (monitorInfo.Work.Bottom - monitorInfo.Work.Top) / screenScale.DpiScaleY;
        window.MinWidth = Math.Min(780, width);
        window.MinHeight = Math.Min(600, height);
        if (window.WindowState != WindowState.Normal)
            return;
        var size = WindowLayout.GetWindowSize(window.Width, window.Height, width, height);
        window.Width = size.Width;
        window.Height = size.Height;
        // Положение окна считаем в пикселях, чтобы учесть разный масштаб мониторов.
        if (!GetWindowRect(windowHandle, out var windowBounds))
            return;
        int left = Math.Max(monitorInfo.Work.Left, Math.Min(windowBounds.Left, monitorInfo.Work.Right - (windowBounds.Right - windowBounds.Left)));
        int top = Math.Max(monitorInfo.Work.Top, Math.Min(windowBounds.Top, monitorInfo.Work.Bottom - (windowBounds.Bottom - windowBounds.Top)));
        if (left != windowBounds.Left || top != windowBounds.Top)
            SetWindowPos(windowHandle, IntPtr.Zero, left, top, 0, 0, 0x0001 | 0x0004 | 0x0010);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
    }

    // Вызывает Windows API, чтобы определить монитор, на котором находится окно.
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    // Получает границы монитора и рабочую область без панели задач через Windows API.
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    // Получает текущее положение и размеры окна в пикселях через Windows API.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect windowBounds);

    // Через Windows API задаёт положение окна; флаги определяют изменение размера и порядка окон.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr afterWindow, int left, int top, int width, int height, uint flags);
}
