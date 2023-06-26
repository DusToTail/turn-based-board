using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace DustyBoard.Grid2D
{
    public class SquareGrid<T> : Grid2D<T> where T : struct
    {
        public SquareGrid(int width, int length) : base(new Grid2DConfig() { width = width, length = length, neighborCount = 8})
        {
            CalculateNeighborIndices();
        }
        public override void CalculateNeighborIndices()
        {
            var job = new CalculateNeighborIndicesJob()
            {
                width = config.width,
                length = config.length,
                neighborCount = config.neighborCount,
                output = _neighbors
            };
            var handle = job.Schedule(config.Size, SquareGridUtilities.BATCH_COUNT);
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
            // Corners
            NativeArray<Vector2Int>[] cornersPerLayer = new NativeArray<Vector2Int>[range];
            Vector2Int[] directions = new Vector2Int[]
            {
                SquareGridUtilities.DirectionArr[(int)Direction.Right],
                SquareGridUtilities.DirectionArr[(int)Direction.Down],
                SquareGridUtilities.DirectionArr[(int)Direction.Left],
                SquareGridUtilities.DirectionArr[(int)Direction.Up]
            };
            NativeArray<Vector2Int> cornerDirectionPerLayer = new NativeArray<Vector2Int>(directions, Allocator.TempJob);
            NativeArray<int>[] outputPerLayer = new NativeArray<int>[range];
            GetIndicesFromLinesJob[] jobPerLayer = new GetIndicesFromLinesJob[range];
            JobHandle[] handlePerLayer = new JobHandle[range];

            for (int i = 0; i < range; i++)
            {
                int curRange = i + 1;
                Vector2Int[] coords = new Vector2Int[]
                {
                    new (xy.x - curRange, xy.y + curRange),
                    new (xy.x + curRange, xy.y + curRange),
                    new (xy.x + curRange, xy.y - curRange),
                    new (xy.x - curRange, xy.y - curRange)
                };
                cornersPerLayer[i] = new NativeArray<Vector2Int>(coords, Allocator.TempJob);
                
                

                outputPerLayer[i] = new NativeArray<int>(8 * curRange, Allocator.TempJob);

                jobPerLayer[i] = new GetIndicesFromLinesJob()
                {
                    coords = cornersPerLayer[i].AsReadOnly(),
                    directions = cornerDirectionPerLayer.AsReadOnly(),
                    width = config.width,
                    length = config.length,
                    range = curRange,
                    sharedOuput = outputPerLayer[i]
                };

                handlePerLayer[i] = jobPerLayer[i].Schedule(4, SquareGridUtilities.BATCH_COUNT);
            }

            var result = new int[4 * (range + 1) * range];
            for(int i = 0; i < range; i++)
            {
                handlePerLayer[i].Complete();
                outputPerLayer[i].CopyFrom(jobPerLayer[i].sharedOuput);

                int curRange = i + 1;
                int layerIndiceCount = 8 * curRange;
                int destIndex = 4 * curRange * (curRange - 1);
                Array.Copy(outputPerLayer[i].ToArray(), 0, result, destIndex, layerIndiceCount);

                cornersPerLayer[i].Dispose();
                outputPerLayer[i].Dispose();
            }
            cornerDirectionPerLayer.Dispose();
            return result;
        }

        public override int[] GetNeighborIndicesAtRange(Vector2Int xy, int range)
        {
            if(range < 1)
            {
                throw new ArgumentException("Range for calculating neighbors can not be less than 1!");
            }
            if(range == 1)
            {
                return GetNeighborIndices(xy);
            }

            GetIndicesInOneLineJob upLeftJob, upRightJob, downLeftJob, downRightJob;
            JobHandle upLeftHandle, upRightHandle, downLeftHandle, downRightHandle;
            NativeArray<int> upLeft = new NativeArray<int>(2 * range, Allocator.TempJob);
            NativeArray<int> upRight = new NativeArray<int>(2 * range, Allocator.TempJob);
            NativeArray<int> downLeft = new NativeArray<int>(2 * range, Allocator.TempJob);
            NativeArray<int> downRight = new NativeArray<int>(2 * range, Allocator.TempJob);
            // UpLeft going Right
            {
                int x = xy.x - range;
                int y = xy.y + range;
                upLeftJob = new GetIndicesInOneLineJob()
                {
                    xy = new(x, y),
                    width = config.width,
                    length = config.length,
                    range = range,
                    direction = SquareGridUtilities.DirectionArr[(int)Direction.Right],
                    lineOutput = upLeft
                };

                upLeftHandle = upLeftJob.Schedule();
            }
            // UpRight going Down
            {
                int x = xy.x + range;
                int y = xy.y + range;
                upRightJob = new GetIndicesInOneLineJob()
                {
                    xy = new(x, y),
                    width = config.width,
                    length = config.length,
                    range = range,
                    direction = SquareGridUtilities.DirectionArr[(int)Direction.Down],
                    lineOutput = upRight
                };
                upRightHandle = upRightJob.Schedule();
            }
            // DownRight going Left
            {
                int x = xy.x + range;
                int y = xy.y - range;
                downRightJob = new GetIndicesInOneLineJob()
                {
                    xy = new(x, y),
                    width = config.width,
                    length = config.length,
                    range = range,
                    direction = SquareGridUtilities.DirectionArr[(int)Direction.Left],
                    lineOutput = downRight
                };
                downRightHandle = downRightJob.Schedule();
            }
            // DownLeft going Up
            {
                int x = xy.x - range;
                int y = xy.y - range;
                downLeftJob = new GetIndicesInOneLineJob()
                {
                    xy = new(x, y),
                    width = config.width,
                    length = config.length,
                    range = range,
                    direction = SquareGridUtilities.DirectionArr[(int)Direction.Up],
                    lineOutput = downLeft
                };
                downLeftHandle = downLeftJob.Schedule();
            }

            var list = new List<int>(8 * range);

            upLeftHandle.Complete();
            list.AddRange(upLeftJob.lineOutput);
            upRightHandle.Complete();
            list.AddRange(upRightJob.lineOutput);
            downRightHandle.Complete();
            list.AddRange(downRightJob.lineOutput);
            downLeftHandle.Complete();
            list.AddRange(downLeftJob.lineOutput);

            upLeft.Dispose();
            upRight.Dispose();
            downRight.Dispose();
            downLeft.Dispose();
            return list.ToArray();
        }

        [BurstCompile]
        /// <summary>
        /// Use for quickly calculate the indices surrounding an index
        /// </summary>
        public struct CalculateNeighborIndicesJob : IJobParallelFor
        {
            public int width, length, neighborCount;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> output;

            [BurstCompile]
            public void Execute(int index)
            {
                Vector2Int xy = new(index - width * (index / width), index / width);
                // Left
                {
                    int x = xy.x - 1;
                    int y = xy.y;
                    output[index * neighborCount] = (Grid2DUtilities.CheckValidCoord(new(x,y), width, length) ? x + width * y : -1);
                }
                // UpLeft
                {
                    int x = xy.x - 1;
                    int y = xy.y + 1;
                    output[index * neighborCount + 1] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // Up
                {
                    int x = xy.x;
                    int y = xy.y + 1;
                    output[index * neighborCount + 2] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // UpRight
                {
                    int x = xy.x + 1;
                    int y = xy.y + 1;
                    output[index * neighborCount + 3] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // Right
                {
                    int x = xy.x + 1;
                    int y = xy.y;
                    output[index * neighborCount + 4] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // DownRight
                {
                    int x = xy.x + 1;
                    int y = xy.y - 1;
                    output[index * neighborCount + 5] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // Down
                {
                    int x = xy.x;
                    int y = xy.y - 1;
                    output[index * neighborCount + 6] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
                // DownLeft
                {
                    int x = xy.x - 1;
                    int y = xy.y - 1;
                    output[index * neighborCount + 7] = (Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + width * y : -1);
                }
            }
        }

        [BurstCompile]
        /// <summary>
        /// Use for retrieving one layer's indices. Scheduled parallel for each coords and directions, executed similar to GetIndicesInOneLineJob.
        /// Coords is a NativeArray of 4 coordinates at the 4 corners
        /// Directions is a NativeArray of 4 directions with which the 4 coords should trace
        /// Output is a NativeArray for all indices of one layer (or simply sum of the 4 edges of the layer).
        /// </summary>
        public struct GetIndicesFromLinesJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Vector2Int>.ReadOnly coords;
            [ReadOnly]
            public NativeArray<Vector2Int>.ReadOnly directions;
            public int width, length;
            public int range;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> sharedOuput;

            [BurstCompile]
            public void Execute(int index)
            {
                Vector2Int xy = coords[index];
                Vector2Int direction = directions[index];

                for (int i = 0; i < 2 * range; i++)
                {
                    int x = xy.x + direction.x * i;
                    int y = xy.y + direction.y * i;
                    sharedOuput[index * 2 * range + i] = Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + y * width : -1;
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
            public int width,length;
            public int range;
            public Vector2Int direction;
            [WriteOnly]
            public NativeArray<int> lineOutput;

            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < 2 * range; i++)
                {
                    int x = xy.x + direction.x * i;
                    int y = xy.y + direction.y * i;
                    lineOutput[i] = Grid2DUtilities.CheckValidCoord(new(x, y), width, length) ? x + y * width : -1;
                }
            }
        }
    }
}
