using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;


namespace SimulationModel {

	public class Simulation {


		//// Скорость моделирования
		//public static double Speed { get; set; } = 1;


		/// <summary>
		/// Статус элемента СМО
		/// </summary>
		public enum Status {
			/// <summary>
			/// Свободен
			/// </summary>
			Free = 0,

			/// <summary>
			/// Занят
			/// </summary>
			Busy = 1
		}



		/// <summary>
		/// Исходные данные
		/// </summary>
		public class InitData {

			/// <summary>
			/// параметр интервала поступления нового задания
			/// </summary>
			public static float Lambda { get; set; } = 0.45f;

			/// <summary>
			/// параметр интервала выполнения нового задания
			/// </summary>
			public static float Mu { get; set; } = 0.4f;

			/// <summary>
			/// время (интервал) моделирования одного шага (прогона)
			/// </summary>
			public static int T { get; set; } = 300;

			/// <summary>
			/// размер очереди (СТЕКа)
			/// </summary>
			public static int M { get; set; } = 45;

			/// <summary>
			/// число прогонов
			/// </summary>
			public static int NumberOfRuns { get; set; } = 125;
		}


		/// <summary>
		/// Вспомогательный класс для работы со статистикой
		/// </summary>
		public class Stat_temp {
			public Stat_temp() { }

			public Stat_temp(int iteration, float incomingTasksCount, float servedTasksCount, float lostTaskCount, float serviceProbability, float failureProbability, double downtime) {
				this.IncomingTasksCount = incomingTasksCount;
				this.ServedTasksCount = servedTasksCount;
				this.LostTaskCount = lostTaskCount;
				this.ServiceProbability = serviceProbability;
				this.FailureProbability = failureProbability;
				this.Downtime = downtime;
				this.Iteration = iteration;
			}

			public int Iteration { get; set; }

			/// <summary>
			/// кол-во поступивших заявок   | w1
			/// </summary>
			public float IncomingTasksCount { get; set; }

			/// <summary>
			/// кол-во обслуженных заявок    | w2
			/// </summary>
			public float ServedTasksCount { get; set; }

			/// <summary>
			/// кол-во заявок, потерянных вследствие переполнения СТЕКа ВС   | w3
			/// </summary>
			public float LostTaskCount { get; set; }

			/// <summary>
			/// вероятность обслуживания пакета  | P(обсл)
			/// </summary>
			public float ServiceProbability { get; set; }

			/// <summary>
			/// вероятность отказа   | P(отк)
			/// </summary>
			public float FailureProbability { get; set; }

			/// <summary>
			/// время простоя вычислительной системы | T(пр)
			/// </summary>
			public double Downtime { get; set; }
		}


		/// <summary>
		/// Рассчитывает показатели эффективности
		/// </summary>
		public class Statistics {

			/// <summary>
			/// кол-во поступивших заявок   | w1
			/// </summary>
			public static float IncomingTasksCount { get; set; }

			/// <summary>
			/// кол-во обслуженных заявок    | w2
			/// </summary>
			public static float ServedTasksCount { get; set; }

			/// <summary>
			/// кол-во заявок, потерянных вследствие переполнения СТЕКа ВС   | w3
			/// </summary>
			public static float LostTaskCount { get; set; }

			/// <summary>
			/// вероятность обслуживания пакета  | P(обсл)
			/// </summary>
			public static float ServiceProbability { get; set; }

			/// <summary>
			/// вероятность отказа   | P(отк)
			/// </summary>
			public static float FailureProbability { get; set; }

			/// <summary>
			/// время простоя вычислительной системы | T(пр)
			/// </summary>
			public static double Downtime { get; set; }

			public static int Iteration { get; set; }

			/// <summary>
			/// Конструктор
			/// </summary>
			/// <param name="incomingTasksCount"> кол-во поступивших заявок </param>
			/// <param name="servedTasksCount"> кол-во обслуженных </param>
			/// <param name="lostTaskCount"> кол-во потерянных заявок </param>
			/// <param name="serviceProbability"> вероятность обслуживания заявки </param>
			/// <param name="failureProbability"> вероятность отказа </param>
			/// <param name="downtime"> время простоя процессора </param>
			public Statistics(int iteration, float incomingTasksCount, float servedTasksCount, float lostTaskCount, float serviceProbability, float failureProbability, double downtime) {
				Iteration = iteration;
				IncomingTasksCount = incomingTasksCount;
				ServedTasksCount = servedTasksCount;
				LostTaskCount = lostTaskCount;
				ServiceProbability = serviceProbability;
				FailureProbability = failureProbability;
				Downtime = downtime;
			}

			public Statistics() { }



			/// <summary>
			/// Вычисляет средние значения всех показателей эффективности по результатам [n] прогонов имитационной модели
			/// </summary>
			/// <param name="stats"> коллекция, содержащая статистику показателей эффективности </param>
			/// <returns> Объект, содержащий средние значения показателей эффективности </returns>
			public static Stat_temp SampleMean(ObservableCollection<Stat_temp> stats) {

				Stat_temp avg = new Stat_temp();
				//ObservableCollection<Stat_temp> stat_avg = new ObservableCollection<Stat_temp>();

				// вычисление средних значений
				avg.Iteration = -0;
				avg.IncomingTasksCount	= (float)Math.Round(stats.Average(a => a.IncomingTasksCount), 2);
				avg.ServedTasksCount	= (float)Math.Round(stats.Average(a => a.ServedTasksCount), 2);
				avg.LostTaskCount		= (float)Math.Round(stats.Average(a => a.LostTaskCount), 2);
				avg.ServiceProbability	= (float)Math.Round(stats.Average(a => a.ServiceProbability), 2);
				avg.FailureProbability	= (float)Math.Round(stats.Average(a => a.FailureProbability), 2);
				avg.Downtime			= Math.Round(stats.Average(a => a.Downtime), 3);

				// добавление в коллекцию средних значений
				//stat_avg.Add(avg);

				return avg;
			}




			/// <summary>
			/// Вычисляет дисперсию показателей эффективности
			/// </summary>
			/// <returns> Объект, содержащий дисперсии показателей эффективности </returns>
			public static Stat_temp Dispersion(ObservableCollection<Stat_temp> stats) {

				// средние значения
				Stat_temp avg = SampleMean(stats);
				
				// дисперсии
				Stat_temp disp = new Stat_temp();

				double
					sum_1 = 0,
					sum_2 = 0,
					sum_3 = 0,
					sum_4 = 0,
					sum_5 = 0;
					//sum_6 = 0;


				for (int i = 0; i < stats.Count; i++) {
					sum_1 += Math.Pow(avg.IncomingTasksCount - stats[i].IncomingTasksCount, 2);
					sum_2 += Math.Pow(avg.ServedTasksCount - stats[i].ServedTasksCount, 2);
					sum_3 += Math.Pow(avg.LostTaskCount - stats[i].LostTaskCount, 2);
					sum_4 += Math.Pow(avg.ServiceProbability - stats[i].ServiceProbability, 2);
					sum_5 += Math.Pow(avg.FailureProbability - stats[i].FailureProbability, 2);
					//sum_6 += Math.Pow(avg.Downtime - stats[i].Downtime, 3);
				}

				disp.IncomingTasksCount = (float)sum_1 / (stats.Count - 1);
				disp.ServedTasksCount	= (float)sum_2 / (stats.Count - 1);
				disp.LostTaskCount		= (float)sum_3 / (stats.Count - 1);
				disp.ServiceProbability = (float)sum_4 / (stats.Count - 1);
				disp.FailureProbability = (float)sum_5 / (stats.Count - 1);
				//disp.Downtime			= (float)sum_6 / (stats.Count - 1);

				return disp;
			}
		}




		/// <summary>
		/// Задача для обслуживания
		/// </summary>
		public class Task {

			/// <summary>
			/// Генерирование новой заявки
			/// </summary>
			/// <returns> время выполнения новой заявки </returns>
			public static double Generation() {
				Random random = new Random();

				// по показательному закону распределения
				//return Math.Abs(Math.Round(1 - (-InitData.Lambda * Math.Log(random.NextDouble())), 2));


				double pow = -InitData.Lambda * random.NextDouble(0, 10);
				return Math.Round(Math.Abs(InitData.Lambda * Math.Pow(Math.E, pow)), 2);
			}
		}



		/// <summary>
		/// Очередь [RAM], принимающая задачи по принципу LIFO (стек)
		/// </summary>
		public class QueueLIFO {

			public static Stack<double> stack;

			/// <summary>
			/// Число элементов в LIFO-очереди 
			/// </summary>
			public int Count => stack.Count;

			// конструктор
			public QueueLIFO() =>
				stack = new Stack<double>(InitData.M);


			/// <summary>
			/// Функция, проверяющая LIFO-очередь на свободное место 
			/// </summary>
			/// <returns> true, если в очереди есть свободное место <br/>
			/// false, если в очереди нет мест </returns>
			public bool FreeSpaceCheck() =>
				InitData.M - stack.Count > 0;

			/// <returns> true, если очередь пустая <br/> 
			/// false, если очередь НЕ пустая </returns>
			public static bool IsEmpty() =>
				stack.Count == 0;
		}






		/// <summary>
		/// Процессор [CPU], имитирующий выполнение задания
		/// </summary>  
		public class Processor {

			/// <summary>
			/// Время работы процессора
			/// </summary>
			private DispatcherTimer CPU_time;

			private Stopwatch Downtime_watch = new Stopwatch();

			/// <summary>
			/// процессор
			/// </summary>
			public Stack<double> stack;


			public Processor() =>
				stack = new Stack<double>(1);


			/// <summary>
			/// индикатор состояния процессора: <br/>
			/// 0 (false) - процессор свободен <br/> 1 (true) - занят
			/// </summary>
			public Status Status {
				get => stack.Count == 0 ? Status.Free : Status.Busy;
				set { }
			}

			/// <returns> Возвращает true, если процессор свободен </returns>
			public bool IsEmpty() => 
				stack.Count == 0;


			/// <returns> Время выполнения новой заявки </returns>
			public static double Generation() {
				Random random = new Random();

				// по показательному закону распределения
				//return Math.Abs(Math.Round( 1 - (-InitData.Mu * Math.Log(random.NextDouble())), 2));

				double pow = -InitData.Mu * random.NextDouble(0, 10);
				return Math.Round(Math.Abs(InitData.Mu * Math.Pow(Math.E, pow)), 2);
			}


			public void Add(double task) {

				//task = 1;

				CPU_time = new DispatcherTimer();
				CPU_time.Interval = TimeSpan.FromSeconds(task);
				//CPU_time.Interval = TimeSpan.FromMilliseconds(1);


				CPU_time.Tick += CPU_time_Tick;

				// Статус = занят
				Status = Status.Busy;
				stack.Push(task);

				CPU_time.Start();
			}



			/// <summary>
			/// Завершает выполнение задачи
			/// </summary>
			private void CPU_time_Tick(object sender, EventArgs e) {

				if (!IsEmpty()) {
					stack.Pop();
					CPU_time.Stop();
					Status = Status.Free;

					// +1 к кол-ву выполненных задач
					Statistics.ServedTasksCount++;
				}
				if (IsEmpty() && !QueueLIFO.IsEmpty()) {

					Add(QueueLIFO.stack.Pop());
				}
			}


			/// <summary>
			/// Фиксирует время простоя процессора
			/// </summary>
			public void DownTime() {

				if (IsEmpty()) {
					Downtime_watch.Start();
					Statistics.Downtime = Math.Round(Downtime_watch.Elapsed.TotalMilliseconds / 1000, 3);
				}
				else
					Downtime_watch.Stop();
			}

			/// <summary>
			/// Обнуляет время простоя процессора
			/// </summary>
			public void DownTime_reset() => Downtime_watch.Reset();

			/// <summary>
			/// Останавливает вычисление времени простоя процессора
			/// </summary>
			public void DownTime_stop() => Downtime_watch.Stop();
		}
	}
}