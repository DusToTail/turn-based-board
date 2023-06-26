using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public enum Direction : int
    {
        Left = 0,
        UpLeft,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        None
    }

    public abstract class Grid2D<T> : INativeDisposable where T : struct
    {
        public Grid2DConfig config { get; private set; }
        protected NativeArray<T> _values;
        protected NativeArray<int> _neighbors;

        public Grid2D(Grid2DConfig config)
        {
            _values = new NativeArray<T>(config.Size, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _neighbors = new NativeArray<int>(config.Size * config.neighborCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            this.config = config;
        }

        public T Get(Vector2Int xy)
        {
            if(!CheckValidCoord(xy))
            {
                throw new ArgumentException($"Invalid coordinate: {xy}");
            }
            return _values[Convert2DCoordToIndex(xy)];
        }
        public void Set(Vector2Int xy, T value)
        {
            if (!CheckValidCoord(xy))
            {
                throw new ArgumentException($"Invalid coordinate: {xy}");
            }
            _values[Convert2DCoordToIndex(xy)] = value;
        }
        public void ForEach(Action<LoopContext> action)
        {
            for (int i = 0; i < _values.Length; i++)
            {
                action.Invoke(new LoopContext() { value = this._values[i], xy = ConvertIndexTo2DCoord(i) });
            }
        }
        public void ForEachNeighbors(Vector2Int xy, Action<LoopContext> action)
        {
            int[] neighbors = GetNeighborIndices(xy);
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!CheckValidIndex(neighbors[i])) { continue; }
                Vector2Int _xy = ConvertIndexTo2DCoord(neighbors[i]);
                if (!CheckValidCoord(_xy)) { continue; }
                action.Invoke(new LoopContext() { value = this._values[neighbors[i]], xy = _xy });
            }
        }
        public void ForEachNeighborsAtRange(Vector2Int xy, int range, Action<LoopContext> action)
        {
            int[] neighbors = GetNeighborIndicesAtRange(xy, range);
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!CheckValidIndex(neighbors[i])) { continue; }
                Vector2Int _xy = ConvertIndexTo2DCoord(neighbors[i]);
                if (!CheckValidCoord(_xy)) { continue; }
                action.Invoke(new LoopContext() { value = this._values[neighbors[i]], xy = ConvertIndexTo2DCoord(neighbors[i]) });
            }
        }
        public void ForEachNeighborsWithinRange(Vector2Int xy, int range, Action<LoopContext> action)
        {
            int[] neighbors = GetNeighborIndicesWithinRange(xy, range);
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!CheckValidIndex(neighbors[i])) { continue; }
                Vector2Int _xy = ConvertIndexTo2DCoord(neighbors[i]);
                if (!CheckValidCoord(_xy)) { continue; }
                action.Invoke(new LoopContext() { value = this._values[neighbors[i]], xy = ConvertIndexTo2DCoord(neighbors[i]) });
            }
        }
        public int[] GetNeighborIndices(Vector2Int xy)
        {
            int index = Convert2DCoordToIndex(xy);
            return _neighbors.GetSubArray(index * config.neighborCount, config.neighborCount).ToArray();
        }

        public abstract int[] GetNeighborIndicesAtRange(Vector2Int xy, int range);
        public abstract int[] GetNeighborIndicesWithinRange(Vector2Int xy, int range);
        public abstract void CalculateNeighborIndices();

        public Vector2Int ConvertIndexTo2DCoord(int index)
        {
            if(index < 0)
            {
                throw new ArgumentException($"Invalid index: {index}");
            }
            int y = (index / config.width);
            int x = index - y * config.width;
            return new Vector2Int(x,y);
        }

        public virtual int Convert2DCoordToIndex(Vector2Int xy)
        {
            if (xy.x < 0 || xy.y < 0)
            {
                throw new ArgumentException($"Invalid coordinate: {xy}");
            }
            return xy.x + config.width * xy.y;
        }
        public virtual bool CheckValidCoord(Vector2Int xy)
        {
            if (xy.x < 0 || xy.y < 0 || xy.x > config.width - 1 || xy.y > config.length - 1)
            {
                return false;
            }
            return true;
        }
        public virtual bool CheckValidIndex(int index)
        {
            if (index < 0 || index > config.Size)
            {
                return false;
            }
            return true;
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                _values.Dispose(inputDeps),
                _neighbors.Dispose(inputDeps)
                );
        }

        public void Dispose()
        {
            _values.Dispose();
            _neighbors.Dispose();
        }

        public struct LoopContext
        {
            public T value;
            public Vector2Int xy;
        }
    }

    public struct Grid2DConfig
    {
        public int Size { get { return width * length; } }
        public int width;
        public int length;
        public int neighborCount;

        public Grid2DConfig(int width, int length, int neighborCount)
        {
            if (width < 1 || length < 1)
            {
                throw new ArgumentException($"{width} and {length} dimensions must be at least 1!");
            }
            this.width = width;
            this.length = length;
            this.neighborCount = neighborCount;
        }
    }
}
