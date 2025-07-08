public static class EdgeDetectionGlobalData
{
    public enum EdgeDetectionSource
    {
        /*COLOR,*/ DEPTH, DEPTH_NORMALS
    }
    
    public static readonly string COLOR_KEYWORD = "SOURCE_COLOR";
    public static readonly string DEPTH_KEYWORD = "SOURCE_DEPTH";
    public static readonly string DEPTH_NORMALS_KEYWORD = "SOURCE_DEPTH_NORMALS";
    
    public enum EdgeDetectionMethod
    {
        SOBEL_3X3, SOBEL_1X3
    }
    
    public static readonly string SOBEL_3X3_KEYWORD = "SOBEL_KERNEL_3X3";
    public static readonly string SOBEL_1X3_KEYWORD = "SOBEL_KERNEL_1X3";

    public enum EdgeDetectionOutputType
    {
        OUTPUT_GREYSCALE, OUTPUT_DIRECTION_DATA
    }
    
    public static readonly string OUTPUT_GREYSCALE_KEYWORD = "OUTPUT_GREYSCALE";
    public static readonly string OUPUT_DIRECTION_KEYWORD = "OUTPUT_DIRECTION_DATA";
}
