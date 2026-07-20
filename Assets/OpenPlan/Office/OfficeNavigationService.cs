using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Deterministic grid A* over generated office bounds with line-of-sight path smoothing.</summary>
    public sealed class OfficeNavigationService
    {
        public const float CellSize = .45f;
        public const float WorkerClearance = .28f;

        private OfficeStageLayout layout;
        private Bounds gridBounds;
        private int width;
        private int height;
        private bool[] walkable;
        private float[] costs;
        private int[] parents;
        private byte[] states;
        private int[] heap;
        private int[] heapPositions;
        private int heapCount;
        private int version;
        private readonly List<int> rawPath = new List<int>(128);
        private readonly List<Vector3> smoothPath = new List<Vector3>(32);

        public OfficeNavigationService(OfficeStageLayout stageLayout)
        {
            layout = stageLayout;
            Rebuild();
        }

        public int Version => version;
        public int LastExpandedNodes { get; private set; }

        public void Invalidate() => Rebuild();

        public bool IsValidPoint(Vector3 point)
            => layout != null && layout.CanNavigateWorkerAt(point, WorkerClearance, out _);

        public bool TryFindPath(Vector3 start, Vector3 destination, out Vector3[] path, out float pathCost)
        {
            path = null;
            pathCost = 0f;
            LastExpandedNodes = 0;
            if (layout == null || walkable == null) return false;
            if (!layout.CanNavigateWorkerAt(destination, 0f, out _)) return false;
            int startIndex = FindNearestCell(start);
            int destinationIndex = FindNearestCell(destination);
            if (startIndex < 0 || destinationIndex < 0) return false;
            if (startIndex == destinationIndex)
            {
                if (!layout.CanNavigateWorkerAt(destination, 0f, out _)) return false;
                path = new[] { new Vector3(destination.x, start.y, destination.z) };
                pathCost = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(destination.x, destination.z));
                return true;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = float.PositiveInfinity;
                parents[i] = -1;
                states[i] = 0;
                heapPositions[i] = -1;
            }
            heapCount = 0;
            costs[startIndex] = 0f;
            Push(startIndex, destinationIndex);
            states[startIndex] = 1;

            while (heapCount > 0)
            {
                int current = Pop(destinationIndex);
                if (current == destinationIndex) break;
                states[current] = 2;
                LastExpandedNodes++;
                int x = current % width;
                int z = current / width;
                Visit(x - 1, z, current, destinationIndex);
                Visit(x + 1, z, current, destinationIndex);
                Visit(x, z - 1, current, destinationIndex);
                Visit(x, z + 1, current, destinationIndex);
            }
            if (parents[destinationIndex] < 0) return false;

            rawPath.Clear();
            int cursor = destinationIndex;
            while (cursor >= 0 && cursor != startIndex)
            {
                rawPath.Add(cursor);
                cursor = parents[cursor];
            }
            rawPath.Reverse();
            Smooth(start, destination);
            if (smoothPath.Count == 0) return false;
            path = smoothPath.ToArray();
            Vector3 previous = start;
            for (int i = 0; i < path.Length; i++)
            {
                pathCost += Vector2.Distance(new Vector2(previous.x, previous.z), new Vector2(path[i].x, path[i].z));
                previous = path[i];
            }
            return true;
        }

        private void Rebuild()
        {
            version++;
            if (layout == null) return;
            gridBounds = layout.WalkableBounds;
            for (int i = 0; i < layout.AdditionalWalkableRegions.Count; i++)
                gridBounds.Encapsulate(layout.AdditionalWalkableRegions[i]);
            width = Mathf.Max(2, Mathf.CeilToInt(gridBounds.size.x / CellSize) + 1);
            height = Mathf.Max(2, Mathf.CeilToInt(gridBounds.size.z / CellSize) + 1);
            int count = width * height;
            walkable = new bool[count];
            costs = new float[count];
            parents = new int[count];
            states = new byte[count];
            heap = new int[count];
            heapPositions = new int[count];
            for (int i = 0; i < count; i++) walkable[i] = layout.CanNavigateWorkerAt(CellCenter(i), WorkerClearance, out _);
        }

        private void Visit(int x, int z, int parent, int destination)
        {
            if (x < 0 || z < 0 || x >= width || z >= height) return;
            int index = z * width + x;
            if (!walkable[index] || states[index] == 2) return;
            float next = costs[parent] + CellSize;
            if (next >= costs[index] - .0001f) return;
            costs[index] = next;
            parents[index] = parent;
            if (states[index] == 0)
            {
                states[index] = 1;
                Push(index, destination);
            }
            else UpdateHeap(index, next + Heuristic(index, destination), destination);
        }

        private int FindNearestCell(Vector3 point)
        {
            int cx = Mathf.RoundToInt((point.x - gridBounds.min.x) / CellSize);
            int cz = Mathf.RoundToInt((point.z - gridBounds.min.z) / CellSize);
            for (int radius = 0; radius <= 4; radius++)
            {
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int z = cz - radius; z <= cz + radius; z++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || z < 0 || x >= width || z >= height ||
                        radius > 0 && x > cx - radius && x < cx + radius && z > cz - radius && z < cz + radius) continue;
                    int index = z * width + x;
                    if (!walkable[index]) continue;
                    Vector3 cell = CellCenter(index);
                    float distance = (new Vector2(cell.x, cell.z) - new Vector2(point.x, point.z)).sqrMagnitude;
                    if (distance < bestDistance) { best = index; bestDistance = distance; }
                }
                if (best >= 0) return best;
            }
            return -1;
        }

        private void Smooth(Vector3 start, Vector3 destination)
        {
            smoothPath.Clear();
            Vector3 anchor = new Vector3(start.x, start.y, start.z);
            int index = 0;
            while (index < rawPath.Count)
            {
                int furthest = index;
                for (int probe = rawPath.Count - 1; probe > index; probe--)
                {
                    Vector3 point = CellCenter(rawPath[probe]);
                    point.y = start.y;
                    if (!HasClearLine(anchor, point)) continue;
                    furthest = probe;
                    break;
                }
                Vector3 waypoint = CellCenter(rawPath[furthest]);
                waypoint.y = start.y;
                smoothPath.Add(waypoint);
                anchor = waypoint;
                index = furthest + 1;
            }
            Vector3 exact = new Vector3(destination.x, start.y, destination.z);
            if (layout.CanNavigateWorkerAt(exact, 0f, out _) && HasClearLine(anchor, exact))
                smoothPath[smoothPath.Count - 1] = exact;
        }

        private bool HasClearLine(Vector3 from, Vector3 to)
        {
            float distance = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
            int samples = Mathf.Max(1, Mathf.CeilToInt(distance / (CellSize * .45f)));
            for (int i = 1; i <= samples; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / (float)samples);
                if (!layout.CanNavigateWorkerAt(point, WorkerClearance, out _)) return false;
            }
            return true;
        }

        private Vector3 CellCenter(int index)
        {
            int x = index % width;
            int z = index / width;
            return new Vector3(gridBounds.min.x + x * CellSize, 0f, gridBounds.min.z + z * CellSize);
        }

        private float Heuristic(int a, int b)
        {
            int ax = a % width;
            int az = a / width;
            int bx = b % width;
            int bz = b / width;
            return (Mathf.Abs(ax - bx) + Mathf.Abs(az - bz)) * CellSize;
        }

        private void Push(int index, int destination)
        {
            int position = heapCount++;
            heap[position] = index;
            heapPositions[index] = position;
            BubbleUp(position, costs[index] + Heuristic(index, destination), destination);
        }

        private int Pop(int destination)
        {
            int result = heap[0];
            heapCount--;
            if (heapCount > 0)
            {
                heap[0] = heap[heapCount];
                heapPositions[heap[0]] = 0;
                BubbleDown(0, destination);
            }
            heapPositions[result] = -1;
            return result;
        }

        private void UpdateHeap(int index, float priority, int destination)
        {
            int position = heapPositions[index];
            if (position >= 0) BubbleUp(position, priority, destination);
        }

        private void BubbleUp(int position, float priority, int destination)
        {
            while (position > 0)
            {
                int parent = (position - 1) / 2;
                int parentIndex = heap[parent];
                float parentPriority = costs[parentIndex] + (destination >= 0 ? Heuristic(parentIndex, destination) : 0f);
                if (parentPriority < priority - .0001f || Mathf.Abs(parentPriority - priority) <= .0001f && parentIndex < heap[position]) break;
                Swap(parent, position);
                position = parent;
            }
        }

        private void BubbleDown(int position, int destination)
        {
            while (true)
            {
                int left = position * 2 + 1;
                if (left >= heapCount) return;
                int right = left + 1;
                int best = left;
                if (right < heapCount && Compare(heap[right], heap[left], destination) < 0) best = right;
                if (Compare(heap[position], heap[best], destination) <= 0) return;
                Swap(position, best);
                position = best;
            }
        }

        private int Compare(int a, int b, int destination)
        {
            float pa = costs[a] + Heuristic(a, destination);
            float pb = costs[b] + Heuristic(b, destination);
            if (pa < pb - .0001f) return -1;
            if (pa > pb + .0001f) return 1;
            return a.CompareTo(b);
        }

        private void Swap(int a, int b)
        {
            int value = heap[a];
            heap[a] = heap[b];
            heap[b] = value;
            heapPositions[heap[a]] = a;
            heapPositions[heap[b]] = b;
        }
    }
}
