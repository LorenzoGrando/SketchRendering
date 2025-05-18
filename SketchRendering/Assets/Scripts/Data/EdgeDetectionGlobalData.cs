public static class EdgeDetectionGlobalData
{
    public enum EdgeDetectionSource
    {
        COLOR, DEPTH, DEPTH_NORMALS
    }
    
    public static readonly string COLOR_KEYWORD = "SOURCE_COLOR";
    public static readonly string DEPTH_KEYWORD = "SOURCE_DEPTH";
    public static readonly string DEPTH_NORMALS_KEYWORD = "SOURCE_DEPTH_NORMALS";
    
    public enum EdgeDetectionMethod
    {
        SOBEL_3X3, SOBEL_1X3, ROBERTS_CROSS
    }
    
    public static readonly string SOBEL_3X3_KEYWORD = "SOBEL_KERNEL_3X3";
    public static readonly string SOBEL_1X3_KEYWORD = "SOBEL_KERNEL_1X3";
    public static readonly string ROBERTS_CROSS_KEYWORD = "ROBERTS_CROSS_KERNEL";
}
