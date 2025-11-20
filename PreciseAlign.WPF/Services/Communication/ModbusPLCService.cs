namespace PreciseAlign.WPF.Services.Communication
{
    //TODO:
    //在 Config.ini (Source 16) 中添加 [PLC] 节（IP地址, 端口, 寄存器地址）。
    //在 ConfigService(Source 19) 中读取它们。
    //实现 IPLCService(Source 11)。使用 NModbus(Source 3) 库(ModbusFactory, IModbusMaster)。
    //提供如 Task<bool> ReadTriggerAsync() 和 Task WriteOffsetsAsync(double dX, double dY, double dTheta) 的方法。
    internal class ModbusPLCService
    {
    }
}
