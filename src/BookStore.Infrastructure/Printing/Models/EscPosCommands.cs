namespace BookStore.Infrastructure.Printing.Models;

public static class EscPosCommands
{
    public static readonly byte[] Initialize = [0x1B, 0x40];
    public static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
    public static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01];
    public static readonly byte[] AlignRight = [0x1B, 0x61, 0x02];
    public static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
    public static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
    public static readonly byte[] UnderlineOn = [0x1B, 0x2D, 0x01];
    public static readonly byte[] UnderlineOff = [0x1B, 0x2D, 0x00];
    public static readonly byte[] NormalSize = [0x1D, 0x21, 0x00];
    public static readonly byte[] DoubleHeight = [0x1D, 0x21, 0x01];
    public static readonly byte[] FeedAndCut = [0x1D, 0x56, 0x42, 0x00];
    public static readonly byte[] OpenCashDrawer = [0x1B, 0x70, 0x00, 0x32, 0xFA];
}
