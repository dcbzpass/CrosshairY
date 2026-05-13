namespace CrosshairY;

public class AppState
{
    public string ProofKey { get; set; } = "h";

    public bool   CaptureHidden { get; set; } = false;

    public string CrTemplate    { get; set; } = "";
    public string CrColor       { get; set; } = "#ffffff";
    public bool   CrOutline     { get; set; } = false;
    public int    CrOutlineSize { get; set; } = 1;
    public int    CrSize        { get; set; } = 100;
}
