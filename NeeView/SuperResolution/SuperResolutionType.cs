using System;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率算法类型
    /// </summary>
    public enum SuperResolutionType
    {
        /// <summary>
        /// 无处理
        /// </summary>
        None,

        /// <summary>
        /// Waifu2x算法
        /// </summary>
        Waifu2x,

        /// <summary>
        /// RealESRGAN算法
        /// </summary>
        RealESRGAN,

        /// <summary>
        /// Real-CUGAN算法
        /// </summary>
        RealCUGAN
    }

    /// <summary>
    /// 超分辨率模型类型
    /// </summary>
    public enum SuperResolutionModel
    {
        /// <summary>
        /// Waifu2x - 动漫风格 (2x)
        /// </summary>
        Waifu2xAnime2x,

        /// <summary>
        /// Waifu2x - 动漫风格 (4x)
        /// </summary>
        Waifu2xAnime4x,

        /// <summary>
        /// Waifu2x - 照片风格 (2x)
        /// </summary>
        Waifu2xPhoto2x,

        /// <summary>
        /// Waifu2x - 照片风格 (4x)
        /// </summary>
        Waifu2xPhoto4x,

        /// <summary>
        /// RealESRGAN - 动漫风格 (4x)
        /// </summary>
        RealESRGANAnime4x,

        /// <summary>
        /// RealESRGAN - 通用模型 (4x)
        /// </summary>
        RealESRGANGeneral4x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (2x)
        /// </summary>
        RealCUGANAnime2x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (3x)
        /// </summary>
        RealCUGANAnime3x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (4x)
        /// </summary>
        RealCUGANAnime4x
    }

    /// <summary>
    /// 超分辨率处理状态
    /// </summary>
    public enum SuperResolutionStatus
    {
        /// <summary>
        /// 等待处理
        /// </summary>
        Pending,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing,

        /// <summary>
        /// 完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 缩放模式
    /// </summary>
    public enum ScaleMode
    {
        /// <summary>
        /// 按倍数缩放
        /// </summary>
        ScaleFactor,

        /// <summary>
        /// 指定宽高
        /// </summary>
        TargetSize
    }
}
