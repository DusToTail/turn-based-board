using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public class HexGrid<T> : Grid2D<T> where T : struct
    {
        public bool oddHeightOffset { get; private set; }

        public HexGrid(int width, int length, bool oddHeightOffset) : base(new Grid2DConfig() { width = width, length = length, neighborCount = 6 })
        {
            this.oddHeightOffset = oddHeightOffset;
            CalculateNeighborIndices();
        }

        public override void CalculateNeighborIndices()
        {
            var job = new CalculateNeighborIndicesJob()
            {
                width = config.width,
                length = config.length,
                oddHeightOffset = oddHeightOffset,
                neighborCount = config.neighborCount,
                output = _neighbors
            };
            var handle = job.Schedule(config.Size, HexGridUtilities.BATCH_COUNT);
            handle.Complete();
            _neighbors.CopyFrom(job.output);
        }

        public override int[] GetNeighborIndicesWithinRange(Vector2Int xy, int range)
        {
            if (range < 1)
            {
                throw new ArgumentException("Range for calculating neighbors can not be less than 1!");
            }
            if (range == 1)
            {
                return GetNeighborIndices(xy);
            }

            bool isOffset = HexGridUtilities.IsOffset(oddHeightOffset, xy.y);

            // Corners
            NativeArray<Vector2Int>[] cornersPerLayer = new NativeArray<Vector2Int>[range];
            Direction[] directions = new Direction[]
            {
                Direction.Right,
                Direction.DownRight,
                Direction.DownLeft,
                Direction.Left,
                Direction.UpLeft,
                Direction.UpRight
            };
            NativeArray<Direction> cornerDirectionPerLayer = new NativeArray<Direction>(directions, Allocator.TempJob);
            NativeArray<int>[] outputPerLayer = new NativeArray<int>[range];
            GetIndicesFromLinesJob[] jobPerLayer = new GetIndicesFromLinesJob[range];
            JobHandle[] handlePerLayer = new JobHandle[range];

            for (int i = 0; i < range; i++)
            {
                int curRange = i + 1;
                Vector2Int[] coords = new Vector2Int[]
                {
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.UpLeft, i, isOffset),
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.UpRight, i, isOffset),
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.Right, i, isOffset),
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.DownRight, i, isOffset),
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.DownLeft, i, isOffset),
                    HexGridUtilities.GetCoordAtDirection(xy, Direction.Left, i, isOffset),
                };
                cornersPerLayer[i] = new NativeArray<Vector2Int>(coords, Allocator.TempJob);

                outputPerLayer[i] = new NativeArray<int>(6 * curRange, Allocator.TempJob);

                jobPerLayer[i] = new GetIndicesFromLinesJob()
                {
                    coords = cornersPerLayer[i].AsReadOnly(),
                    directions = cornerDirectionPerLayer.AsReadOnly(),
                    width = config.width,
                    length = config.length,
                    range = curRange,
                    oddHeightOffset = oddHeightOffset,
                    sharedOuput = outputPerLayer[i]
                };

                handlePerLayer[i] = jobPerLayer[i].Schedule(6, HexGridUtilities.BATCH_COUNT);
            }

            var result = new int[3 * (range + 1) * range];
            for (int i = 0; i < range; i++)
            {
                handlePerLayer[i].Complete();
                outputPerLayer[i].CopyFrom(jobPerLayer[i].sharedOuput);

                int curRange = i + 1;
                int layerIndiceCount = 6 * curRange;
                int destIndex = 3 * curRange * (curRange - 1);
                Array.Copy(outputPerLayer[i].ToArray(), 0, result, destIndex, layerIndiceCount);

                cornersPerLayer[i].Dispose();
                outputPerLayer[i].Dispose();
            }
            cornerDirectionPerLayer.Dispose();
            return result;
        }

        public override int[] GetNeighborIndicesAtRange(Vector2Int xy, int range)
        {
            if (range < 1)
            {
                throw new ArgumentException("Range for calculating neighbors can not be less than 1!");
            }
            if (range == 1)
            {
                return GetNeighborIndices(xy);
            }
            bool isOffset = HexGridUtilities.IsOffset(oddHeightOffset, xy.y);

            GetIndicesInOneLineJob leftJob, upLeftJob, upRightJob, rightJob, downRightJob, downLeftJob;
            JobHandle leftHandle, upLeftHandle, upRightHandle, rightHandle, downRightHandle, downLeftHandle;
            NativeArray<int> left = new NativeArray<int>(range, Allocator.TempJob);
            NativeArray<int> upLeft = new NativeArray<int>(range, Allocator.TempJob);
            NativeArray<int> upRight = new NativeArray<int>(range, Allocator.TempJob);
            NativeArray<int> right = new NativeArray<int>(range, Allocator.TempJob);
            NativeArray<int> downRight = new NativeArray<int>(range, Allocator.TempJob);
            NativeArray<int> downLeft = new NativeArray<int>(range, Allocator.TempJob);
            // UpLeft going Right
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.UpLeft, range, isOffset);
                upLeftJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.Right,
                    lineOutput = upLeft
                };

                upLeftHandle = upLeftJob.Schedule();
            }
            // UpRight going DownRight
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.UpRight, range, isOffset);
                upRightJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.DownRight,
                    lineOutput = upRight
                };
                upRightHandle = upRightJob.Schedule();
            }
            // Right going DownLeft
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.Right, range, isOffset);
                rightJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.DownLeft,
                    lineOutput = right
                };
                rightHandle = rightJob.Schedule();
            }
            // DownRight going Left
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.DownRight, range, isOffset);
                downRightJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.Left,
                    lineOutput = downRight
                };
                downRightHandle = downRightJob.Schedule();
            }
            //DownLeft going UpLeft
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.DownLeft, range, isOffset);
                downLeftJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.UpLeft,
                    lineOutput = downLeft
                };
                downLeftHandle = downLeftJob.Schedule();
            }
            //Left going UpRight
            {
                var xy0 = HexGridUtilities.GetCoordAtDirection(xy, Direction.Left, range, isOffset);
                leftJob = new GetIndicesInOneLineJob()
                {
                    xy = xy0,
                    width = config.width,
                    length = config.length,
                    range = range,
                    oddHeightOffset = oddHeightOffset,
                    direction = Direction.UpRight,
                    lineOutput = left
                };
                leftHandle = leftJob.Schedule();
            }

            var list = new List<int>(6 * range);

            upLeftHandle.Complete();
            list.AddRange(upLeftJob.lineOutput);
            upRightHandle.Complete();
            list.AddRange(upRightJob.lineOutput);
            rightHandle.Complete();
            list.AddRange(rightJob.lineOutput);
            downRightHandle.Complete();
            list.AddRange(downRightJob.lineOutput);
            downLeftHandle.Complete();
            list.AddRange(downLeftJob.lineOutput);
            leftHandle.Complete();
            list.AddRange(leftJob.lineOutput);

            upLeft.Dispose();
            upRight.Dispose();
            right.Dispose();
            downRight.Dispose();
            downLeft.Dispose();
            left.Dispose();
            return list.ToArray();
        }

        [BurstCompile]
        /// <summary>
        /// Use for quickly calculate the indices surrounding an index
        /// </summary>
        public struct CalculateNeighborIndicesJob : IJobParallelFor
        {
            public int width, length, neighborCount;
            public bool oddHeightOffset;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> output;

            [BurstCompile]
            public void Execute(int index)
            {
                Vector2Int xy = new(index - width * (index / width), index / width);
                bool isOffset = HexGridUtilities.IsOffset(oddHeightOffset, xy.y);
                if(isOffset)
                {
                    // Left
                    {
                        int x = xy.x - 1;
                        int y = xy.y;
                        output[index * neighborCount] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // UpLeft
                    {
                        int x = xy.x;
                        int y = xy.y + 1;
                        output[index * neighborCount + 1] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // UpRight
                    {
                        int x = xy.x + 1;
                        int y = xy.y + 1;
                        output[index * neighborCount + 2] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // Right
                    {
                        int x = xy.x + 1;
                        int y = xy.y;
                        output[index * neighborCount + 3] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // DownRight
                    {
                        int x = xy.x + 1;
                        int y = xy.y - 1;
                        output[index * neighborCount + 4] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // DownLeft
                    {
                        int x = xy.x;
                        int y = xy.y - 1;
                        output[index * neighborCount + 5] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                }
                else
                {
                    // Left
                    {
                        int x = xy.x - 1;
                        int y = xy.y;
                        output[index * neighborCount] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // UpLeft
                    {
                        int x = xy.x - 1;
                        int y = xy.y + 1;
                        output[index * neighborCount + 1] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // UpRight
                    {
                        int x = xy.x;
                        int y = xy.y + 1;
                        output[index * neighborCount + 2] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // Right
                    {
                        int x = xy.x + 1;
                        int y = xy.y;
                        output[index * neighborCount + 3] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // DownRight
                    {
                        int x = xy.x;
                        int y = xy.y - 1;
                        output[index * neighborCount + 4] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                    // DownLeft
                    {
                        int x = xy.x - 1;
                        int y = xy.y - 1;
                        output[index * neighborCount + 5] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                    }
                }
            }
        }

        [BurstCompile]
        /// <summary>
        /// Use for retrieving one layer's indices. Scheduled parallel for each coords and directions, executed similar to GetIndicesInOneLineJob.
        /// Coords is a NativeArray of 6 coordinates at the 6 corners
        /// Directions is a NativeArray of 6 directions with which the 6 coords should trace
        /// Output is a NativeArray for all indices of one layer (or simply sum of the 6 edges of the layer).
        /// </summary>
        public struct GetIndicesFromLinesJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Vector2Int>.ReadOnly coords;
            [ReadOnly]
            public NativeArray<Direction>.ReadOnly directions;
            public int width, length;
            public bool oddHeightOffset;
            public int range;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sharedOuput;

            [BurstCompile]
            public void Execute(int index)
            {
                Vector2Int xy = coords[index];
                bool isOffset = HexGridUtilities.IsOffset(oddHeightOffset, xy.y);

                for (int i = 0; i < range; i++)
                {
                    var xy_ = HexGridUtilities.GetCoordAtDirection(xy, directions[index], i, isOffset);
                    sharedOuput[index * range + i] = Grid2DUtilities.CheckValidCoord(xy_, width, length) ? xy_.x + xy_.y * width : -1;
                    if (xy == new Vector2Int(6, 7) || xy == new Vector2Int(7, 5))
                    {
                        Debug.Log($"{xy}: Direction: {directions[index]}, Range {i}, Offset: {isOffset}\nResulted in {xy_}");
                    }
                }
            }
        }

        [BurstCompile]
        /// <summary>
        /// Use for retrieving one line, defined by a line's origin index, direction to trace, and range away from origin
        /// Output is a NativeArray of all indices traversed in a line
        /// </summary>
        public struct GetIndicesInOneLineJob : IJob
        {
            public Vector2Int xy;
            public int width, length;
            public bool oddHeightOffset;
            public int range;
            public Direction direction;
            [WriteOnly]
            public NativeArray<int> lineOutput;

            [BurstCompile]
            public void Execute()
            {
                bool isOffset = HexGridUtilities.IsOffset(oddHeightOffset, xy.y);

                for (int i = 0; i < range; i++)
                {
                    var xy_ = HexGridUtilities.GetCoordAtDirection(xy, direction, i, isOffset);
                    lineOutput[i] = Grid2DUtilities.CheckValidCoord(xy_, width, length) ? xy_.x + xy_.y * width : -1;
                }
            }
        }
    }
}
