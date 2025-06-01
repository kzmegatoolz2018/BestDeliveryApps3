using BestDelivery;
using BestDeliveryApp;
using BestDeliveryApps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Threading;
using DisplayPoint = System.Windows.Point;
using GeoPoint = BestDelivery.Point;

namespace BestDeliveryApps
{
    public class DeliveryManager
    {
        private Order[] activeParcels = Array.Empty<Order>();
        private GeoPoint hubLocation;
        private int[] deliveryOrder = Array.Empty<int>();
        private readonly Random rnd = new Random();
        private readonly DispatcherTimer updateTimer;
        private readonly Action updateRouteInfoCallback;
        private readonly Action refreshParcelListCallback;

        public DeliveryManager(Action updateRouteInfoCallback, Action refreshParcelListCallback)
        {
            this.updateRouteInfoCallback = updateRouteInfoCallback;
            this.refreshParcelListCallback = refreshParcelListCallback;
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };
            updateTimer.Tick += Timer_Tick;
        }

        public void Scen(Func<Order[]> fetchOrders, string description)
        {
            updateTimer.Stop();

            activeParcels = fetchOrders();
            if (activeParcels == null || !activeParcels.Any())
            {
                MessageBox.Show("Ошибка: Список заказов пуст.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var duplicateIds = activeParcels.GroupBy(o => o.ID).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicateIds.Any())
            {
                MessageBox.Show($"Ошибка: Обнаружены дубликаты ID заказов: {string.Join(", ", duplicateIds)}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var hubOrder = activeParcels.FirstOrDefault(o => o.ID == -1);
            if (hubOrder.ID != -1)
            {
                MessageBox.Show("Ошибка: Склад не найден в списке заказов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            hubLocation = hubOrder.Destination;
            deliveryOrder = DeliveryOptimizer.CreateOptimizedRoute(activeParcels, hubLocation);
            refreshParcelListCallback();
            updateRouteInfoCallback();

            updateTimer.Start();
            Console.WriteLine("Таймер автообновления маршрута запущен (интервал: 6 секунд).");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (activeParcels == null || !activeParcels.Any())
            {
                Console.WriteLine("Автообновление: Нет активных заказов, таймер остановлен.");
                updateTimer.Stop();
                return;
            }

            var hubOrder = activeParcels.FirstOrDefault(o => o.ID == -1);
            if (hubOrder.ID != -1)
            {
                Console.WriteLine("Автообновление: Склад не найден, таймер остановлен.");
                updateTimer.Stop();
                return;
            }

            hubLocation = hubOrder.Destination;
            deliveryOrder = DeliveryOptimizer.CreateOptimizedRoute(activeParcels, hubLocation);
            Console.WriteLine("Маршрут автоматически обновлен.");
            refreshParcelListCallback();
            updateRouteInfoCallback();
        }

        public void AddOrder()
        {
            updateTimer.Stop();

            var id = activeParcels.Any() ? activeParcels.Max(p => p.ID) + 1 : 1;
            var x = rnd.NextDouble() * 100;
            var y = rnd.NextDouble() * 100;
            var priority = rnd.NextDouble();

            var list = activeParcels.ToList();
            list.Add(new Order { ID = id, Destination = new GeoPoint { X = x, Y = y }, Priority = priority });
            activeParcels = list.ToArray();

            var hubOrder = activeParcels.FirstOrDefault(o => o.ID == -1);
            if (hubOrder.ID != -1)
            {
                MessageBox.Show("Ошибка: Склад не найден, маршрут не может быть построен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            hubLocation = hubOrder.Destination;

            deliveryOrder = DeliveryOptimizer.CreateOptimizedRoute(activeParcels, hubLocation);
            refreshParcelListCallback();
            updateRouteInfoCallback();

            updateTimer.Start();
            Console.WriteLine("Таймер автообновления маршрута перезапущен после добавления заказа.");
        }

        public void AddOrder(GeoPoint destination, double priority)
        {
            updateTimer.Stop();

            var newId = activeParcels.Any() ? activeParcels.Max(o => o.ID) + 1 : 1;
            var newOrder = new Order
            {
                ID = newId,
                Destination = destination,
                Priority = priority
            };

            var list = activeParcels.ToList();
            list.Add(newOrder);
            activeParcels = list.ToArray();

            var hubOrder = activeParcels.FirstOrDefault(o => o.ID == -1);
            if (hubOrder.ID != -1)
            {
                MessageBox.Show("Ошибка: Склад не найден, маршрут не может быть построен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            hubLocation = hubOrder.Destination;

            deliveryOrder = DeliveryOptimizer.CreateOptimizedRoute(activeParcels, hubLocation);
            refreshParcelListCallback();
            updateRouteInfoCallback();

            updateTimer.Start();
            Console.WriteLine("Таймер автообновления маршрута перезапущен после добавления заказа.");
        }

        public Order[] GetActiveParcels() => activeParcels;
        public GeoPoint GetHubLocation() => hubLocation;
        public int[] GetDeliveryOrder() => deliveryOrder;
    }
}