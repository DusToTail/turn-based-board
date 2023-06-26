using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using DustyBoard.Grid2D;

public class TestGrid : MonoBehaviour
{
    public enum Shape
    {
        Square,
        Hexagon
    }

    public enum TestMode
    {
        ForEach,
        ForEachNeighbors,
        ForEachNeighborIndicesAtRange,
        ForEachNeighborIndicesWithinRange
    }
    public Shape shape;
    [Range(1, 1000)]
    public int width;
    [Range(1, 1000)]
    public int length;
    public Grid unityGrid;
    public Grid2D<Vector3> grid;
    public bool drawGizmos;
    public bool debugConsole;
    public TestMode testMode;
    public Vector2Int coord2D;
    [Range(1, 1000)]
    public int range;
    private System.Text.StringBuilder _stringBuilder;

    private void Start()
    {
        bool oddHeightOffset = true;
        if(shape == Shape.Square)
        {
            grid = new SquareGrid<Vector3>(width, length);
        }
        else if(shape == Shape.Hexagon)
        {
            grid = new HexGrid<Vector3>(width, length, oddHeightOffset);
        }
        Vector3 size = unityGrid.cellSize;
        Vector3 gap = unityGrid.cellGap;
        grid.ForEach(ctx =>
        {
            int x = ctx.xy.x;
            int y = ctx.xy.y;
            if(shape == Shape.Square)
            {
                Vector3 value = new Vector3(size.x * x, 0f, size.z * y) + new Vector3(size.x, 0f, size.z) * 0.5f + gap;
                grid.Set(ctx.xy, value);
            }
            else if(shape == Shape.Hexagon)
            {
                bool isOffSet = HexGridUtilities.IsOffset(oddHeightOffset, y);
                float _x = x * 2 * HexGridUtilities.EDGE_DISTANCE * size.x + (isOffSet ? HexGridUtilities.EDGE_DISTANCE * size.x : 0) + gap.x;
                float _y = 0f;
                float _z = y * HexGridUtilities.HEIGHT_INTERVAL * 0.75f * size.z + gap.z;
                Vector3 value = new Vector3(_x,_y,_z) + new Vector3(HexGridUtilities.WIDTH_INTERVAL * size.x / 2f, 0f, 0.5f * size.z);
                grid.Set(ctx.xy, value);
            }
            
        });
        _stringBuilder = new System.Text.StringBuilder();
    }

    private void OnDestroy()
    {
        grid.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || grid == null) { return; }

        JobHandle handle = default;
        NativeArray<Grid2D<Vector3>.LoopContext> ctxArr = default;

        if (testMode == TestMode.ForEach)
        {
            if (debugConsole)
                _stringBuilder.Clear();

            Gizmos.color = Color.yellow;
            grid.ForEach(DrawGridGizmos);

            if(debugConsole)
                Debug.Log("ForEach\n" + _stringBuilder);
        }
        else if(testMode == TestMode.ForEachNeighbors)
        {
            {
                Gizmos.color = Color.yellow;
                grid.ForEach(DrawGridGizmos);
            }

            if (debugConsole)
                _stringBuilder.Clear();

            Gizmos.color = Color.red;
            Vector2Int _xy = new(coord2D.x, coord2D.y);
            if (!grid.CheckValidCoord(_xy)) { Debug.LogWarning($"{coord2D} is invalid coordinate!"); return; }
            grid.ForEachNeighbors(_xy, DrawGridGizmos);

            if(debugConsole)
                Debug.Log("ForEachNeighbors\n" + _stringBuilder);
        }
        else if(testMode == TestMode.ForEachNeighborIndicesAtRange)
        {
            {
                Gizmos.color = Color.yellow;
                grid.ForEach(DrawGridGizmos);
            }

            if (debugConsole)
                _stringBuilder.Clear();

            Gizmos.color = Color.red;
            Vector2Int _xy = new(coord2D.x, coord2D.y);
            if (!grid.CheckValidCoord(_xy)) { Debug.LogWarning($"{coord2D} is invalid coordinate!"); return; }
            grid.ForEachNeighborsAtRange(_xy, range, DrawGridGizmos);

            if (debugConsole)
                Debug.Log("ForEachNeighborsAtRange\n" + _stringBuilder);
        }
        else if (testMode == TestMode.ForEachNeighborIndicesWithinRange)
        {
            {
                Gizmos.color = Color.yellow;
                grid.ForEach(DrawGridGizmos);
            }

            if (debugConsole)
                _stringBuilder.Clear();

            Gizmos.color = Color.red;
            Vector2Int _xy = new(coord2D.x, coord2D.y);
            if (!grid.CheckValidCoord(_xy)) { Debug.LogWarning($"{coord2D} is invalid coordinate!"); return; }
            
            grid.ForEachNeighborsWithinRange(_xy, range, DrawGridGizmos);

            if (debugConsole)
                Debug.Log("ForEachNeighborsWithinRange\n" + _stringBuilder);
        }

        handle.Complete();

        ctxArr.Dispose();
    }

    private void DrawGridGizmos(Grid2D<Vector3>.LoopContext ctx)
    {
        Gizmos.DrawWireSphere(ctx.value, 0.1f);
        Gizmos.DrawWireCube(ctx.value, unityGrid.cellSize);
        if (debugConsole)
            _stringBuilder.Append(ctx.xy + "\t");
    }
}
