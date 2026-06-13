namespace CrosshairY;

public class AppState
{
    public string ProofKey  { get; set; } = "h";
    public string CycleKey  { get; set; } = "";

    public bool CaptureHidden { get; set; } = false;

    public int MonitorIndex { get; set; } = 0;

    public string CrTemplate    { get; set; } = "";
    public string CrColor       { get; set; } = "#ffffff";
    public bool   CrOutline     { get; set; } = false;
    public int    CrOutlineSize { get; set; } = 1;
    public int    CrSize        { get; set; } = 100;
    public int    CrOpacity     { get; set; } = 100;
    public int    CrGap         { get; set; } = 3;

    public List<string> CrCustomPixels { get; set; } = new();
    public int CrBuilderSize { get; set; } = 15;
}
