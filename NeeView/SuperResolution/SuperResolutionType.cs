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
        [System.ComponentModel.Description("Waifu2x 动漫 2x")]
        Waifu2xAnime2x,

        /// <summary>
        /// Waifu2x - 动漫风格 (4x)
        /// </summary>
        [System.ComponentModel.Description("Waifu2x 动漫 4x")]
        Waifu2xAnime4x,

        /// <summary>
        /// Waifu2x - 照片风格 (2x)
        /// </summary>
        [System.ComponentModel.Description("Waifu2x 照片 2x")]
        Waifu2xPhoto2x,

        /// <summary>
        /// Waifu2x - 照片风格 (4x)
        /// </summary>
        [System.ComponentModel.Description("Waifu2x 照片 4x")]
        Waifu2xPhoto4x,

        /// <summary>
        /// RealESRGAN - 动漫风格 (4x)
        /// </summary>
        [System.ComponentModel.Description("RealESRGAN 动漫 4x")]
        RealESRGANAnime4x,

        /// <summary>
        /// RealESRGAN - 通用模型 (4x)
        /// </summary>
        [System.ComponentModel.Description("RealESRGAN 通用 4x")]
        RealESRGANGeneral4x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (2x)
        /// </summary>
        [System.ComponentModel.Description("RealCUGAN 动漫 2x")]
        RealCUGANAnime2x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (3x)
        /// </summary>
        [System.ComponentModel.Description("RealCUGAN 动漫 3x")]
        RealCUGANAnime3x,

        /// <summary>
        /// Real-CUGAN - 动漫风格 (4x)
        /// </summary>
        [System.ComponentModel.Description("RealCUGAN 动漫 4x")]
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
