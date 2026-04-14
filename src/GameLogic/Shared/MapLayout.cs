namespace BattleTank.GameLogic.Shared;

/// <summary>
/// Static map layout shared by server (collision) and client (rendering).
/// All coordinates are in world pixels (map is 1000×1000).
/// </summary>
public static class MapLayout
{
    public static readonly WallData[] Walls =
    [
        // Center — cross structure
        new(460f, 380f, 80f, 30f),   // horizontal bar top
        new(460f, 590f, 80f, 30f),   // horizontal bar bottom
        new(480f, 410f, 40f, 180f),  // vertical bar (connects the two)

        // Top-left quadrant
        new(180f, 180f, 80f, 20f),
        new(180f, 200f, 20f, 80f),

        // Top-right quadrant
        new(740f, 180f, 80f, 20f),
        new(800f, 200f, 20f, 80f),

        // Bottom-left quadrant
        new(180f, 740f, 20f, 80f),
        new(180f, 800f, 80f, 20f),

        // Bottom-right quadrant
        new(800f, 740f, 20f, 80f),
        new(740f, 800f, 80f, 20f),

        // Side corridors
        new(60f,  440f, 60f, 20f),
        new(880f, 440f, 60f, 20f),
        new(440f, 60f,  20f, 60f),
        new(440f, 880f, 20f, 60f),
    ];
}
