using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public static class SquareGridUtilities
    {
        public static readonly int BATCH_COUNT = 8;

        //public static readonly Dictionary<Direction, Vector2Int> DirectionDict = new Dictionary<Direction, Vector2Int>()
        //{
        //    { Direction.Left, new Vector2Int(-1,0) },
        //    { Direction.UpLeft, new Vector2Int(-1,1) },
        //    { Direction.Up, new Vector2Int(0,1) },
        //    { Direction.UpRight, new Vector2Int(1,1) },
        //    { Direction.Right, new Vector2Int(1,0) },
        //    { Direction.DownRight, new Vector2Int(1,-1) },
        //    { Direction.Down, new Vector2Int(0,-1) },
        //    { Direction.DownLeft, new Vector2Int(-1,-1) },
        //    { Direction.None, new Vector2Int(0,0) }
        //};
        public static readonly Vector2Int[] DirectionArr = new Vector2Int[]
        {
            new Vector2Int (-1,0),
            new Vector2Int (-1,1),
            new Vector2Int (0,1),
            new Vector2Int (1,1),
            new Vector2Int (1,0),
            new Vector2Int (1,-1),
            new Vector2Int (0,-1),
            new Vector2Int (-1,-1),
            new Vector2Int (0,0)
        };
    }
}
