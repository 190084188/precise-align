namespace PreciseAlign.Core.Interfaces
{
    /// <summary>
    /// 提供与具体工艺流程相关的配置信息。
    /// 它的职责是解释配置数据，并提供符合业务逻辑的结构。
    /// </summary>
    public interface IProcessConfigService
    {
        /// <summary>
        /// 获取流程步骤与相机ID的映射关系。
        /// </summary>
        /// <returns>一个字典，键为流程步骤名称，值为该步骤使用的相机ID数组。</returns>
        Dictionary<string, string[]> GetProcessStepCameraMapping();
    }
}
