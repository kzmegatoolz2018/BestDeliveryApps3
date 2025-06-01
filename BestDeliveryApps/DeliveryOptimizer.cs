using BestDelivery;
using BestDeliveryApps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using DisplayPoint = System.Windows.Point;
using GeoPoint = BestDelivery.Point;

namespace BestDeliveryApp
{
    public static class DeliveryOptimizer
    {
        public static int[] CreateOptimizedRoute(Order[] parcels, GeoPoint hub)
        {
            if (parcels == null) throw new ArgumentNullException(nameof(parcels));
            var orders = parcels.Where(p => p.ID != -1).ToList();
            if (orders.Count == 0) return new[] { -1, -1 };

            var points = new List<GeoPoint> { hub };
            points.AddRange(orders.Select(o => o.Destination));
            int n = points.Count;

            var trafficData = DeliveryRoadSimulator.GenerateTrafficData(points);
            double[,] dist = trafficData.Item1;
            double[,] A_new = trafficData.Item2;

            var pq = new CustomPriorityQueue<(int current, HashSet<int> visited, double g, List<int> path), double>();
            var startState = (current: 0, visited: new HashSet<int> { 0 }, g: 0.0, path: new List<int> { 0 });
            pq.Enqueue(startState, 0.0);

            HashSet<(int, string)> seen = new();
            int maxIterations = 100000;
            int iterations = 0;

            while (pq.Count > 0 && iterations++ < maxIterations)
            {
                if (!pq.TryDequeue(out var state, out double f)) continue;
                var (current, visited, g, path) = state;

                if (visited.Count == n)
                {
                    if (current != 0) continue;
                    int[] routeIds = path.Select(i => i == 0 ? -1 : orders[i - 1].ID).ToArray();
                    return routeIds ?? Array.Empty<int>();
                }

                for (int next = 0; next < n; next++)
                {
                    if (visited.Contains(next)) continue;

                    double priority = next == 0 ? 0.0 : orders[next - 1].Priority;
                    double cost = A_new[current, next] * (1.0 + priority);
                    double newG = g + cost;

                    var newVisited = new HashSet<int>(visited) { next };
                    var newPath = new List<int>(path) { next };

                    string visitedKey = string.Join(",", newVisited.OrderBy(x => x));
                    var stateKey = (next, visitedKey);
                    if (seen.Contains(stateKey)) continue;
                    seen.Add(stateKey);

                    double h = EstimateRemainingCost(next, newVisited, points, dist);
                    double newF = newG + h;

                    pq.Enqueue((next, newVisited, newG, newPath), newF);
                }
            }

            Console.WriteLine("A* не нашел маршрут, возвращаем жадный маршрут с учетом приоритетов и трафика.");
            return CreateGreedyRoute(parcels, hub, A_new, orders);
        }

        private static double EstimateRemainingCost(int current, HashSet<int> visited, List<GeoPoint> points, double[,] dist)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var unvisited = Enumerable.Range(0, points.Count).Where(i => !visited.Contains(i)).ToList();
            if (unvisited.Count == 0)
                return dist[current, 0];

            double mstCost = CalculateMSTCost(unvisited, dist);
            double minToUnvisited = unvisited.Min(i => dist[current, i]);
            double minToHub = unvisited.Min(i => dist[i, 0]);

            return mstCost + minToUnvisited + minToHub;
        }

        private static double CalculateMSTCost(List<int> nodes, double[,] dist)
        {
            if (nodes == null || dist == null) throw new ArgumentNullException();
            if (nodes.Count <= 1) return 0.0;

            double totalCost = 0.0;
            var visited = new HashSet<int> { nodes[0] };

            while (visited.Count < nodes.Count)
            {
                double minCost = double.MaxValue;
                int nextNode = -1;

                foreach (int v in visited)
                {
                    foreach (int u in nodes)
                    {
                        if (!visited.Contains(u) && dist[v, u] < minCost)
                        {
                            minCost = dist[v, u];
                            nextNode = u;
                        }
                    }
                }

                if (nextNode == -1) break;
                totalCost += minCost;
                visited.Add(nextNode);
            }

            return totalCost;
        }

        private static int[] CreateGreedyRoute(Order[] parcels, GeoPoint hub, double[,] A_new, List<Order> orders)
        {
            if (parcels == null || A_new == null || orders == null) throw new ArgumentNullException();
            var points = new List<GeoPoint> { hub };
            points.AddRange(orders.Select(o => o.Destination));
            int n = points.Count;

            List<int> route = new() { 0 };
            var remaining = new HashSet<int>(Enumerable.Range(1, n - 1));

            while (remaining.Count > 0)
            {
                int last = route[^1];
                int next = -1;
                double bestScore = double.MaxValue;

                foreach (int candidate in remaining)
                {
                    double priority = orders[candidate - 1].Priority;
                    double score = A_new[last, candidate] * (1.0 + priority);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        next = candidate;
                    }
                }

                if (next == -1) break;
                route.Add(next);
                remaining.Remove(next);
            }

            route.Add(0);
            return route.Select(i => i == 0 ? -1 : orders[i - 1].ID).ToArray();
        }
    }
}