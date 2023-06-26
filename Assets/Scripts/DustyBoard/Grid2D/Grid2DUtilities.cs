using System;
using Unity.Burst;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public static class Grid2DUtilities
    {
        public static bool CheckValidCoord<T>(Vector2Int xy, Grid2D<T> grid) where T : unmanaged
        {
            return grid.CheckValidCoord(xy);
        }
        [BurstCompile]
        public static bool CheckValidCoord(Vector2Int xy, Grid2DConfig config)
        {
            if (xy.x < 0 || xy.y < 0 || xy.x > config.width - 1 || xy.y > config.length - 1)
            {
                return false;
            }
            return true;
        }
        [BurstCompile]
        public static bool CheckValidCoord(Vector2Int xy, int width, int length)
        {
            if (xy.x < 0 || xy.y < 0 || xy.x > width - 1 || xy.y > length - 1)
            {
                return false;
            }
            return true;
        }

        public static Vector2Int ConvertIndexTo2DCoord<T>(int index, Grid2D<T> grid) where T : unmanaged
        {
            return grid.ConvertIndexTo2DCoord(index);
        }
        [BurstCompile]
        public static Vector2Int ConvertIndexTo2DCoord(int index, Grid2DConfig config)
        {
            if (index < 0)
            {
                throw new ArgumentException("Index can not be less than 0!");
            }
            int y = (index / config.width);
            int x = index - y * config.width;
            return new Vector2Int(x, y);
        }
        [BurstCompile]
        public static Vector2Int ConvertIndexTo2DCoord(int index, int width)
        {
            if (index < 0)
            {
                throw new ArgumentException("Index can not be less than 0!");
            }
            int y = (index / width);
            int x = index - y * width;
            return new Vector2Int(x, y);
        }

        public static int Convert2DCoordToIndex<T>(Vector2Int xy, Grid2D<T> grid) where T : unmanaged
        {
            return grid.Convert2DCoordToIndex(xy);
        }
        [BurstCompile]
        public static int Convert2DCoordToIndex(int x, int y, Grid2DConfig config)
        {
            if (x < 0 || y < 0)
            {
                throw new ArgumentException("Coordinate components can not be less than 0!");
            }
            return x + config.width * y;
        }
        [BurstCompile]
        public static int Convert2DCoordToIndex(Vector2Int xy, int width)
        {
            if (xy.x < 0 || xy.y < 0)
            {
                throw new ArgumentException("Coordinate components can not be less than 0!");
            }
            return xy.x + width * xy.y;
        }
    }
}
