using BestDelivery;
using BestDeliveryApps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using DisplayPoint = System.Windows.Point;
using GeoPoint = BestDelivery.Point;

namespace BestDeliveryApps
{
    public partial class DeliveryWindow : Window
    {
        private readonly DeliveryManager routeManager;
        private DispatcherTimer animationTimer;
        private List<(double X1, double Y1, double X2, double Y2)> linesToDraw;
        private int currentLineIndex;

        public DeliveryWindow()
        {
            InitializeComponent();
            routeManager = new DeliveryManager(UpdateRouteInfo, () => { });

            animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void OptionOneTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray1, "Центр города");

        private void OptionTwoTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray2, "Окраины");

        private void OptionThreeTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray3, "Один район");

        private void OptionFourTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray4, "Разные районы");

        private void OptionFiveTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray5, "Разные приоритеты");

        private void OptionSixTrigger(object sender, RoutedEventArgs e) => routeManager.Scen(OrderArrays.GetOrderArray6, "Много заказов");

        private void GetRandomOrder(object sender, RoutedEventArgs e)
        {
            var rnd = new Random();
            int scenario = rnd.Next(1, 7);
            string scenarioName = "";

            switch (scenario)
            {
                case 1:
                    OptionOneTrigger(sender, e);
                    scenarioName = "Центр города";
                    break;
                case 2:
                    OptionTwoTrigger(sender, e);
                    scenarioName = "Окраины";
                    break;
                case 3:
                    OptionThreeTrigger(sender, e);
                    scenarioName = "Один район";
                    break;
                case 4:
                    OptionFourTrigger(sender, e);
                    scenarioName = "Разные районы";
                    break;
                case 5:
                    OptionFiveTrigger(sender, e);
                    scenarioName = "Разные приоритеты";
                    break;
                case 6:
                    OptionSixTrigger(sender, e);
                    scenarioName = "Много заказов";
                    break;
            }

            var deliveryOrder = routeManager.GetDeliveryOrder();
            RouteSequence.Text = $"Сценарий: {scenarioName}\nМаршрут: " + (deliveryOrder.Length > 0
                ? string.Join(" → ", deliveryOrder.Select(id => id == -1 ? "СКЛАД" : "#" + id))
                : "не построен");
        }

        private void RouteCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RouteCanvas);

            double margin = 50;
            double width = RouteCanvas.ActualWidth > 0 ? RouteCanvas.ActualWidth : 600;
            double height = RouteCanvas.ActualHeight > 0 ? RouteCanvas.ActualHeight : 500;

            var points = routeManager.GetActiveParcels().Select(p => p.Destination).ToList();
            double minX = points.Any() ? points.Min(p => p.X) : 0;
            double maxX = points.Any() ? points.Max(p => p.X) : 100;
            double minY = points.Any() ? points.Min(p => p.Y) : 0;
            double maxY = points.Any() ? points.Max(p => p.Y) : 100;

            double scaleX = (maxX == minX) ? 1 : (width - 2 * margin) / (maxX - minX);
            double scaleY = (maxY == minY) ? 1 : (height - 2 * margin) / (maxY - minY);
            double scale = Math.Min(scaleX, scaleY);

            double shiftX = margin - minX * scale;
            double shiftY = height - margin + minY * scale;

            double x = (pos.X - shiftX) / scale;
            double y = (shiftY - pos.Y) / scale;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите приоритет (от 0.0 до 1.0)",
                "Новый заказ",
                "0.5"
            );

            if (!double.TryParse(input.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double priority) ||
                priority < 0.0 || priority > 1.0)
            {
                MessageBox.Show("Приоритет должен быть числом от 0.0 до 1.0",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            routeManager.AddOrder(new GeoPoint { X = x, Y = y }, priority);
        }

        private void DrawRoute()
        {
            RouteCanvas.Children.Clear();
            var positions = new Dictionary<int, DisplayPoint>();

            double margin = 50;
            double width = RouteCanvas.ActualWidth > 0 ? RouteCanvas.ActualWidth : 600;
            double height = RouteCanvas.ActualHeight > 0 ? RouteCanvas.ActualHeight : 500;

            var points = routeManager.GetActiveParcels().Select(p => p.Destination).ToList();
            double minX = points.Any() ? points.Min(p => p.X) : 0;
            double maxX = points.Any() ? points.Max(p => p.X) : 100;
            double minY = points.Any() ? points.Min(p => p.Y) : 0;
            double maxY = points.Any() ? points.Max(p => p.Y) : 100;

            double scaleX = (maxX == minX) ? 1 : (width - 2 * margin) / (maxX - minX);
            double scaleY = (maxY == minY) ? 1 : (height - 2 * margin) / (maxY - minY);
            double scale = Math.Min(scaleX, scaleY);

            double shiftX = margin - minX * scale;
            double shiftY = height - margin + minY * scale;

            DisplayPoint Map(GeoPoint p) => new(p.X * scale + shiftX, shiftY - p.Y * scale);

            foreach (var order in routeManager.GetActiveParcels())
            {
                var point = Map(order.Destination);
                positions[order.ID] = point;

                var marker = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = order.ID == -1 ? Brushes.Red : Brushes.Blue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(marker, point.X - 6);
                Canvas.SetTop(marker, point.Y - 6);
                RouteCanvas.Children.Add(marker);

                var label = new TextBlock
                {
                    Text = order.ID == -1 ? "СКЛАД" : $"#{order.ID}",
                    Foreground = Brushes.Black,
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(label, point.X + 10); 
                Canvas.SetTop(label, point.Y - 5);   
                RouteCanvas.Children.Add(label);
            }

            var deliveryOrder = routeManager.GetDeliveryOrder();
            if (deliveryOrder.Length < 2)
            {
                Console.WriteLine("Маршрут слишком короткий для отрисовки линий.");
                return;
            }

            linesToDraw = new List<(double X1, double Y1, double X2, double Y2)>();
            for (int i = 0; i < deliveryOrder.Length - 1; i++)
            {
                var fromId = deliveryOrder[i];
                var toId = deliveryOrder[i + 1];

                if (!positions.ContainsKey(fromId) || !positions.ContainsKey(toId))
                {
                    Console.WriteLine($"Ошибка при отрисовке: ID {fromId} или {toId} не найдены.");
                    continue;
                }

                var from = positions[fromId];
                var to = positions[toId];
                linesToDraw.Add((from.X, from.Y, to.X, to.Y));
            }

            currentLineIndex = 0;
            animationTimer.Stop();
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (currentLineIndex >= linesToDraw.Count)
            {
                animationTimer.Stop();
                return;
            }

            var (X1, Y1, X2, Y2) = linesToDraw[currentLineIndex];
            var line = new Line
            {
                X1 = X1,
                Y1 = Y1,
                X2 = X2,
                Y2 = Y2,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 2
            };
            RouteCanvas.Children.Add(line);

            currentLineIndex++;
        }

        private void UpdateRouteInfo()
        {
            var deliveryOrder = routeManager.GetDeliveryOrder();
            var activeParcels = routeManager.GetActiveParcels();
            var hubLocation = routeManager.GetHubLocation();

            double routeLength = 0.0;
            double cost = RoutingTestLogic.CalculateRouteCost(deliveryOrder.ToList(), activeParcels.ToList(), hubLocation);
            bool testResult = RoutingTestLogic.TestRoutingSolution(hubLocation, activeParcels, deliveryOrder, out routeLength);

            if (cost < 0 || !testResult)
            {
                routeLength = DeliveryDistanceCalculator.CalculateRouteLengthManually(deliveryOrder, activeParcels);
            }

            Console.WriteLine($"Маршрут: {string.Join(", ", deliveryOrder)}");
            Console.WriteLine($"Тест пройден: {testResult}, Длина: {routeLength:F2}, Стоимость: {cost:F2}");

            DistanceInfo.Text = $"Длина пути: {routeLength:F2}";
            DrawRoute();
        }
    }
}