using Unity.Burst;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public static class HexGridUtilities
    {
        public static readonly int BATCH_COUNT = 6;

        public static readonly float WIDTH_INTERVAL = 1.732f;
        public static readonly float HEIGHT_INTERVAL = 1.5f;
        public static readonly float EDGE_DISTANCE = 0.866f;

        //public static readonly Dictionary<Direction, Vector2Int> DirectionDict = new Dictionary<Direction, Vector2Int>()
        //{
        //    { Direction.Left, new Vector2Int(-1,0) },
        //    { Direction.UpLeft, new Vector2Int(-1,1) },
        //    { Direction.Up, new Vector2Int(0,2) },
        //    { Direction.UpRight, new Vector2Int(0,1) },
        //    { Direction.Right, new Vector2Int(1,0) },
        //    { Direction.DownRight, new Vector2Int(0,-1) },
        //    { Direction.Down, new Vector2Int(0,-2) },
        //    { Direction.DownLeft, new Vector2Int(-1,-1) },
        //    { Direction.None, new Vector2Int(0,0) }
        //};

        public static readonly Vector2Int[] DirectionArr = new Vector2Int[]
        {
            new Vector2Int(-1,0), // Left
            new Vector2Int(-1,1), // UpLeft
            new Vector2Int(0,2), // Up
            new Vector2Int(0,1), // UpRight
            new Vector2Int(1,0), // Right
            new Vector2Int(0,-1), // DownRight
            new Vector2Int(0,-2), // Down
            new Vector2Int(-1,-1), // DownLeft
            new Vector2Int(0,0) // None
        };
        public static readonly Vector2Int[] OffsetDirectionArr = new Vector2Int[]
        {
            new Vector2Int(-1,0), // Left
            new Vector2Int(0,1), // UpLeft
            new Vector2Int(0,2), // Up
            new Vector2Int(1,1), // UpRight
            new Vector2Int(1,0), // Right
            new Vector2Int(1,-1), // DownRight
            new Vector2Int(0,-2), // Down
            new Vector2Int(0,-1), // DownLeft
            new Vector2Int(0,0) // None
        };

        //public static readonly Dictionary<string, Vector3> Vertices = new Dictionary<string, Vector3>()
        //{
        //    { "North", new Vector3(0, 0, 1)},
        //    { "NorthEast", new Vector3(0.866f, 0, 0.5f)},
        //    { "SouthEast", new Vector3(0.866f, 0, -0.5f)},
        //    { "South", new Vector3(0, 0, -1)},
        //    { "SouthWest", new Vector3(-0.866f, 0, -0.5f)},
        //    { "NorthWest", new Vector3(-0.866f, 0, 0.5f)},
        //};

        [BurstCompile]
        public static bool IsOffset(bool oddHeightOffset, int height)
        {
            return (oddHeightOffset && height % 2 == 1) || (!oddHeightOffset && height % 2 == 0);
        }

        [BurstCompile]
        public static Vector2Int GetCoordAtDirection(Vector2Int xy, Direction direction, bool isOffset)
        {
            if (direction == Direction.None)
            {
                return xy;
            }
            var dir = isOffset ? OffsetDirectionArr[(int)direction] : DirectionArr[(int)direction];
            return new Vector2Int(xy.x + dir.x, xy.y + dir.y);
        }
        
        [BurstCompile]
        public static Vector2Int GetCoordAtDirection(Vector2Int xy, Direction direction, int distance, bool isOffset)
        {
            if(distance == 0 || direction == Direction.None)
            {
                return xy;
            }
            if(distance == 1)
            {
                return GetCoordAtDirection(xy, direction, isOffset);
            }
            var dir = DirectionArr[(int)direction];
            
            if (direction == Direction.Left || direction == Direction.Right || direction == Direction.Up || direction == Direction.Down)
            {
                return new Vector2Int(xy.x + dir.x * distance, xy.y + dir.y * distance);
            }
            int offsetChangeCount = isOffset ? (distance + 1) / 2 : distance / 2;
            int normalChangeCount = distance - offsetChangeCount;
            var inverted = OffsetDirectionArr[(int)direction];

            int x = xy.x + normalChangeCount * dir.x + offsetChangeCount * inverted.x;
            int y = xy.y + normalChangeCount * dir.y + offsetChangeCount * inverted.y;
            return new Vector2Int(x, y);
        }
    }
}
