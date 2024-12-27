using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

// To do: реализовать Метода прямоугольников https://programm.top/c-sharp/algorithm/numerical-methods/rectangle-method/
namespace IntegralCalculation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик нажатия кнопки "Calculate"
        private void OnCalculateClick(object sender, RoutedEventArgs e)
        {
            // Проверяем корректность ввода данных
            if (!double.TryParse(TxtStart.Text, out double a) ||
                !double.TryParse(TxtEnd.Text, out double b) ||
                !int.TryParse(TxtSteps.Text, out int maxSteps) ||
                CmbThreads.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, введите корректные значения.");
                return;
            }

            // Считываем количество потоков из ComboBox
            int threads = int.Parse(((ComboBoxItem)CmbThreads.SelectedItem).Content.ToString());

            // Подготовка данных для графиков
            var linearTimes = new ChartValues<double>();
            var threadTimes = new ChartValues<double>();
            var taskTimes = new ChartValues<double>();
            var stepValues = new List<double>();

            // Измеряем время выполнения каждого метода при различных значениях steps
            for (int steps = 100; steps <= maxSteps; steps += 100) // Изменяйте шаг по необходимости
            {
                stepValues.Add(steps);

                Stopwatch stopwatch = new Stopwatch();

                // Линейный расчет интеграла
                stopwatch.Start();
                CalculateIntegralLinear(a, b, steps);
                stopwatch.Stop();
                linearTimes.Add(stopwatch.Elapsed.TotalMilliseconds); // Сохраняем время
                stopwatch.Reset();

                // Параллельный расчет с использованием Thread
                stopwatch.Start();
                CalculateIntegralParallelThread(a, b, steps, threads);
                stopwatch.Stop();
                threadTimes.Add(stopwatch.Elapsed.TotalMilliseconds); // Сохраняем время
                stopwatch.Reset();

                // Параллельный расчет с использованием Task
                stopwatch.Start();
                CalculateIntegralParallelTask(a, b, steps, threads);
                stopwatch.Stop();
                taskTimes.Add(stopwatch.Elapsed.TotalMilliseconds); // Сохраняем время
                stopwatch.Reset();
            }

            // Обновляем график, очищая предыдущие серии
            Chart.Series.Clear();
            Chart.Series.Add(new LineSeries
            {
                Title = "Линейный",
                Values = linearTimes,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 5
            });
            Chart.Series.Add(new LineSeries
            {
                Title = "Параллельный (Thread)",
                Values = threadTimes,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 5
            });
            Chart.Series.Add(new LineSeries
            {
                Title = "Параллельный (Task)",
                Values = taskTimes,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 5
            });

            // Обновляем ось X с метками шагов
            Chart.AxisX.Clear();
            Chart.AxisX.Add(new Axis
            {
                Title = "Количество элементов",
                Labels = stepValues.Select(s => s.ToString()).ToArray()
            });
        }

        // Метод для линейного вычисления интеграла
        private double CalculateIntegralLinear(double a, double b, int n)
        {
            double step = (b - a) / n; // Вычисляем шаг
            double integral = 0.0;

            for (int i = 0; i < n; i++)
            {
                double x = a + i * step; // Текущая точка
                integral += Function(x) * step; // Площадь текущего прямоугольника
            }

            return integral;
        }

        // Метод для параллельного вычисления интеграла с использованием потоков
        private double CalculateIntegralParallelThread(double a, double b, int n, int threadCount)
        {
            double step = (b - a) / n; // Вычисляем шаг
            double integral = 0.0;
            double[] results = new double[threadCount];

            Thread[] threads = new Thread[threadCount];
            int partsPerThread = n / threadCount;

            for (int i = 0; i < threadCount; i++)
            {
                int j = i; // Локальная копия для использования в лямбда-выражении
                threads[i] = new Thread(() =>
                {
                    double localIntegral = 0.0;
                    int start = j * partsPerThread;
                    int end = (j + 1) * partsPerThread;
                    for (int k = start; k < end; k++)
                    {
                        double x = a + k * step;
                        localIntegral += Function(x) * step;
                    }
                    results[j] = localIntegral;
                });
                threads[i].Start();
            }

            // Ожидаем завершения всех потоков
            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Суммируем результаты всех потоков
            foreach (var result in results)
            {
                integral += result;
            }

            return integral;
        }

        // Метод для параллельного вычисления интеграла с использованием задач
        private double CalculateIntegralParallelTask(double a, double b, int n, int taskCount)
        {
            double step = (b - a) / n; // Вычисляем шаг
            double integral = 0.0;
            Task<double>[] tasks = new Task<double>[taskCount];
            int partsPerTask = n / taskCount;

            for (int i = 0; i < taskCount; i++)
            {
                int j = i; // Локальная копия для использования в лямбда-выражении
                tasks[i] = Task.Run(() =>
                {
                    double localIntegral = 0.0;
                    int start = j * partsPerTask;
                    int end = (j + 1) * partsPerTask;
                    for (int k = start; k < end; k++)
                    {
                        double x = a + k * step;
                        localIntegral += Function(x) * step;
                    }
                    return localIntegral;
                });
            }

            // Ожидаем завершения всех задач
            Task.WaitAll(tasks);

            // Суммируем результаты всех задач
            integral = tasks.Sum(task => task.Result);

            return integral;
        }

        // Функция, которую мы интегрируем
        private double Function(double x)
        {
            return x * x; // Пример функции: f(x) = x^2
        }
    }
}
