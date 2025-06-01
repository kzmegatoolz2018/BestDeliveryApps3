using BestDelivery;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using DisplayPoint = System.Windows.Point;
using GeoPoint = BestDelivery.Point;

namespace BestDeliveryApps
{
    public static class DeliveryRoadSimulator
    {
        private static readonly Random rnd = new Random();

        public static (double[,], double[,]) GenerateTrafficData(List<GeoPoint> points)
        {
            int n = points.Count;
            double[,] dist = new double[n, n];
            double[,] influence = new double[n, n * 3];

            double w_traffic = 0.5;
            double w_accident = 1.0;
            double w_weather = 0.3;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        dist[i, j] = 0;
                        influence[i, j * 3] = 0;
                        influence[i, j * 3 + 1] = 0;
                        influence[i, j * 3 + 2] = 0;
                        continue;
                    }

                    dist[i, j] = RoutingTestLogic.CalculateDistance(points[i], points[j]);

                    double v_traffic = rnd.NextDouble();
                    double v_accident = rnd.NextDouble() < 0.1 ? 1.0 : 0.0;
                    double v_weather = rnd.NextDouble() * 0.5;

                    influence[i, j * 3] = v_traffic;
                    influence[i, j * 3 + 1] = v_accident;
                    influence[i, j * 3 + 2] = v_weather;

                    Console.WriteLine($"Ребро {i}→{j}: distance={dist[i, j]:F2}, v_traffic={v_traffic:F2}, v_accident={v_accident:F2}, v_weather={v_weather:F2}");
                }
            }

            double[,] A_new = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        A_new[i, j] = 0;
                        continue;
                    }

                    double v_traffic = influence[i, j * 3];
                    double v_accident = influence[i, j * 3 + 1];
                    double v_weather = influence[i, j * 3 + 2];

                    A_new[i, j] = dist[i, j] * (1.0 + w_traffic * v_traffic + w_accident * v_accident + w_weather * v_weather);
                    Console.WriteLine($"A_new[{i},{j}]={A_new[i, j]:F2}");
                }
            }

            return (dist, A_new);
        }
    }
}