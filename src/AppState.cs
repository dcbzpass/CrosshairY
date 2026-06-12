namespace CrosshairY;

public class AppState
{
    // global settings — stored in settings.dat, NOT in profiles
    public string ProofKey  { get; set; } = "h";
    public string CycleKey  { get; set; } = "";

    public bool CaptureHidden { get; set; } = false;

    // crosshair properties — stored in profiles
    public string CrTemplate    { get; set; } = "";
    public string CrColor       { get; set; } = "#ffffff";
    public bool   CrOutline     { get; set; } = false;
    public int    CrOutlineSize { get; set; } = 1;
    public int    CrSize        { get; set; } = 100;
    public int    CrOpacity     { get; set; } = 100;
    public int    CrGap         { get; set; } = 3;

    // custom drawn crosshair pixel grid (15x15, serialized as hex string list)
    // only non-transparent pixels are stored: "row,col,#rrggbb"
    public List<string> CrCustomPixels { get; set; } = new();
    public int CrBuilderSize { get; set; } = 15;
}
