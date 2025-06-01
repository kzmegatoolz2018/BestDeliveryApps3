using BestDelivery;
using System;
using System.Linq;

namespace BestDeliveryApps
{
    public static class DeliveryDistanceCalculator
    {
        public static double CalculateRouteLengthManually(int[] deliveryOrder, Order[] parcels)
        {
            if (deliveryOrder.Length < 2 || !parcels.Any())
                return 0.0;

            double totalLength = 0.0;
            int skippedSegments = 0;

            for (int i = 0; i < deliveryOrder.Length - 1; i++)
            {
                var fromId = deliveryOrder[i];
                var toId = deliveryOrder[i + 1];

                var fromOrder = parcels.FirstOrDefault(o => o.ID == fromId);
                var toOrder = parcels.FirstOrDefault(o => o.ID == toId);

                if (fromOrder.ID != fromId || toOrder.ID != toId)
                {
                    Console.WriteLine($"Ошибка: Заказ с ID {fromId} или {toId} не найден.");
                    skippedSegments++;
                    continue;
                }

                var fromPoint = fromOrder.Destination;
                var toPoint = toOrder.Destination;
                double distance = RoutingTestLogic.CalculateDistance(fromPoint, toPoint);
                totalLength += distance;
                Console.WriteLine($"Сегмент {fromId} → {toId}: {distance:F2}");
            }

            if (skippedSegments > 0)
                Console.WriteLine($"Пропущено сегментов: {skippedSegments}, длина может быть неточной.");

            return totalLength;
        }
    }
}