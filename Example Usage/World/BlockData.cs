public struct BlockData
(
    Block block,
    string faceBack,
    string faceFront,
    string faceTop,
    string faceBottom,
    string faceLeft,
    string faceRight
)
{
    public static List<BlockData> AllBlocks =
    [
        new BlockData() { block = Block.Air },
        new BlockData(Block.Grass, "Grass Side", "Grass Side", "Grass", "Dirt", "Grass Side", "Grass Side"),
        new BlockData(Block.Dirt, "Dirt", "Dirt", "Dirt", "Dirt", "Dirt", "Dirt"),
        new BlockData(Block.Stone, "Stone", "Stone", "Stone", "Stone", "Stone", "Stone")

    ];
    public Block block = block;
    public string faceBack = faceBack;
    public string faceFront = faceFront;
    public string faceTop = faceTop;
    public string faceBottom = faceBottom;
    public string faceLeft = faceLeft;
    public string faceRight = faceRight;
}