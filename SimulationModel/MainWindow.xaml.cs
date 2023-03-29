using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;


namespace SimulationModel {

	public partial class MainWindow : Window {

		/*	 Dispatcher Timer	*/

		/// <summary>
		/// измеряет затраченное время
		/// </summary>
		private Stopwatch stopWatch = new Stopwatch();

		/// <summary>
		/// реальная длительность выполнения эксперимента
		/// </summary>
		public DispatcherTimer realTime = new DispatcherTimer();

		/// <summary>
		/// модельное (виртульное) время
		/// </summary>
		public DispatcherTimer virtualTime = new DispatcherTimer();

		/// <summary>
		/// виртуальное время (вспомогательная переменная)		  
		/// </summary>
		public DispatcherTimer virtualTime_sec = new DispatcherTimer();

		/// <summary>
		/// Таймер, задающий генерирование новой заявки
		/// </summary>
		private DispatcherTimer timer = new DispatcherTimer();


		/*	 Time & Data Init	*/

		/// <summary>
		/// Возвращает кол-во итераций (прогонов)
		/// </summary>
		private int iteration = 0;

		/// <summary>
		/// интервал инициализации новой заявки
		/// </summary>
		private static double task_init = 0;

		/// <summary>
		/// интервал выполнения заявки
		/// </summary>
		private static double task_runtime = 0;

		/// <summary>
		/// Вспомогательное св-во для текущего времени 
		/// </summary>
		public string CurrentTime { get; set; } = string.Empty;

		/// <summary>
		/// Вспомогательное св-во для модельного времени 
		/// </summary>
		public string ModelTime { get; set; } = string.Empty;

		/// <summary>
		/// Возвращает true, если симуляция завершена
		/// </summary>
		public bool EndOfSimulation { get; set; } = false;


		/*	 CPU & RAM	 */

		/// <summary>
		/// Процессор (стек, емкостью = 1)
		/// </summary>
		private Simulation.Processor processor;

		/// <summary>
		/// FIFO-очередь
		/// </summary>
		private Simulation.QueueLIFO queue;


		/*	 Statistic	 */

		/// <summary>
		/// Коллекция, собирающая статистику работы СМО
		/// </summary>
		//private ObservableCollection<Simulation.Statistics> statistic = new ObservableCollection<Simulation.Statistics>();
		private ObservableCollection<Simulation.Stat_temp> statistic = new ObservableCollection<Simulation.Stat_temp>();


		/*	 Model (virtual) time	 */

		/// <summary>
		/// Счетчик модельного времени
		/// </summary>
		public static double VTime { get; set; } = 0;

		/// <summary>
		/// Массив параметров для правила изменения счетчика модельного времени
		/// </summary>
		public static double[] Min { get; set; } = { Math.Round(task_init, 3), Math.Round(task_runtime, 3), Simulation.InitData.T - VTime };

		/*------------------------------------------------------------------------------------*/
		/*------------------------------------------------------------------------------------*/



		/// <summary>
		/// событие для таймера реального времени
		/// </summary>
		void TimerTick_RealTime(object sender, EventArgs e) {

			TimeSpan ts = stopWatch.Elapsed;

			if (stopWatch.IsRunning) {
				// отображение реального времени проведения моделирования
				CurrentTime = String.Format("{0:0}:{1:000}", ts.TotalSeconds, ts.Milliseconds);
				L_rTime.Content = CurrentTime;

			}
		}


		/// <summary>
		/// событие для таймера виртуального (модельного) времени
		/// </summary>
		void TimerTick_VirtualTime(object sender, EventArgs e) {

			if (stopWatch.IsRunning) {

				// отображение виртуального времени проведения моделирования
				//ModelTime = $"{stopWatch.Elapsed.TotalSeconds:0}:{stopWatch.Elapsed.Milliseconds:000}";

				// изменение счетчика модельного времени
				VTime += Min.Min();
				ModelTime = $"{VTime:0.000}";
				L_vTime.Content = ModelTime;


				// состояние очереди
				PB_RAM.Value = queue.Count;
				TB_RAM.Text = $"В очереди: {PB_RAM.Value} / {PB_RAM.Maximum}";



				// отображение состояния процессора
				// [занят]
				if (!processor.IsEmpty()) {
					E_CPU_on.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFF9E9E");
					E_CPU_on.Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString("Red");
					TB_CPU_status.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("Red");
					TB_CPU_status.Text = "– занят";

					// обнуляем время простоя процессора
					//processor.DownTime_stop();

				}
				// [свободен]
				else {
					E_CPU_on.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF91FFA5");
					E_CPU_on.Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF06B025");
					TB_CPU_status.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF06B025");
					TB_CPU_status.Text = "– свободен";

				}

				// фиксация времени простоя процессора
				processor.DownTime();

				// время простоя процессора
				//TB_CPU_downTime.Text = processor.Downtime;
				TB_CPU_downTime.Text = $"Время простоя: {Simulation.Statistics.Downtime}";
				L_Downtime.Content = $"{Simulation.Statistics.Downtime}";
			}
		}



		private void VirtualTime_sec_Tick(object sender, EventArgs e) {

			if (stopWatch.IsRunning) {

				// прогресс по шагу
				if (PB_progressStep.Value < PB_progressStep.Maximum) {
					//PB_progressStep.Value++;
					PB_progressStep.Value = Math.Round(VTime, 0);
				}
				else {
					try {
						// вероятности
						Simulation.Statistics.ServiceProbability = (float)Math.Round((float)Simulation.Statistics.ServedTasksCount / Simulation.Statistics.IncomingTasksCount, 2)/* * 100*/;
						Simulation.Statistics.FailureProbability = (float)Math.Round(1 - Simulation.Statistics.ServiceProbability, 2);
					}
					catch (Exception) { }

					L_ServiceProbability.Content = Simulation.Statistics.ServiceProbability.ToString();
					L_FailureProbability.Content = Simulation.Statistics.FailureProbability.ToString();


					// создаем объект для хранения данных статистики по прогону
					Simulation.Stat_temp step_stat = new Simulation.Stat_temp(
						iteration + 1,
						Simulation.Statistics.IncomingTasksCount,
						Simulation.Statistics.ServedTasksCount,
						Simulation.Statistics.LostTaskCount,
						Simulation.Statistics.ServiceProbability,
						Simulation.Statistics.FailureProbability,
						Simulation.Statistics.Downtime);

					// сохраняем данные статистики по прогону
					statistic.Add(step_stat);


					if (statistic.Count > 0) {

						// средние значение
						Simulation.Stat_temp stat_avg = Simulation.Statistics.SampleMean(statistic);

						L_AVG_Downtime.Content = stat_avg.Downtime.ToString();
						L_AVG_LostTaskCount.Content = stat_avg.LostTaskCount.ToString();
						L_AVG_ServedTasksCount.Content = stat_avg.ServedTasksCount.ToString();
						L_AVG_IncomingTasksCount.Content = stat_avg.IncomingTasksCount.ToString();
						L_AVG_FailureProbability.Content = stat_avg.FailureProbability.ToString();
						L_AVG_ServiceProbability.Content = stat_avg.ServiceProbability.ToString();


						if (statistic.Count > 1) {

							// дисперсия
							Simulation.Stat_temp stat_disp = Simulation.Statistics.Dispersion(statistic);

							//L_disp_Downtime.Content = stat_disp.Downtime.ToString();
							L_disp_LostTaskCount.Content = $"{stat_disp.LostTaskCount:0.00}";
							L_disp_ServedTasksCount.Content = $"{stat_disp.ServedTasksCount:0.00}";
							L_disp_IncomingTasksCount.Content = $"{stat_disp.IncomingTasksCount:0.00}";
							L_disp_FailureProbability.Content = $"{stat_disp.FailureProbability:0.00}";
							L_disp_ServiceProbability.Content = $"{stat_disp.ServiceProbability:0.00}";
						}
					}

					VTime = 0;

					PB_progressStep.Value = 0;
					iteration++;
					ResetParamIteration();
				}

				TB_progressStep.Text = $"прогресс шага: {PB_progressStep.Value} / {PB_progressStep.Maximum}";

				// прогресс по прогонам
				PB_progressIter.Value = float.Parse(String.Format("{0:0}", iteration));
				TB_progressIter.Text = $"прогонов: {PB_progressIter.Value} / {PB_progressIter.Maximum}";


				// завершение симуляции 
				if (PB_progressIter.Value == PB_progressIter.Maximum) {

					// если время истекло, а процессор был в работе, то +1 обслуженная заявка
					if (processor.Status == Simulation.Status.Busy) {
						Simulation.Statistics.ServedTasksCount++;
					}

					// подведение итогов
					statistic.Add(Simulation.Statistics.SampleMean(statistic));

					stopWatch.Stop();
					MessageBox.Show("Результаты представлены в таблице.\nПоследняя строка таблицы содержит средние значения параметров.",
						"Время симуляции истекло", MessageBoxButton.OK, icon: MessageBoxImage.Information);
					EndOfSimulation = true;
				}
			}
		}


		/// <summary>
		/// Обнуляет статистику итерации
		/// </summary>
		private void ResetParamIteration() {

			// обнуление данных статистики
			Simulation.Statistics.Downtime = 0.000;
			Simulation.Statistics.LostTaskCount = 0;
			Simulation.Statistics.ServedTasksCount = 0;
			Simulation.Statistics.ServiceProbability = 0;
			Simulation.Statistics.IncomingTasksCount = 0;
			Simulation.Statistics.FailureProbability = 0;

			// сброс показателей
			L_Downtime.Content = "0:000";
			L_LostTaskCount.Content = "0";
			L_ServedTasksCount.Content = "0";
			L_IncomingTasksCount.Content = "0";
			L_FailureProbability.Content = "0";
			L_ServiceProbability.Content = "0";

			// сброс времени простоя процессора
			processor.DownTime_reset();


			// [HOT] - на всякий пересоздаем new stack(m) |~| очистка LIFO-очереди
			if (queue != null) {
				//queue.stack.Clear();
				Simulation.QueueLIFO.stack.Clear();
				queue = new Simulation.QueueLIFO();
			}

			// освобождение процессора
			if (processor != null)
				processor.stack.Clear();


			// сброс прогресса
			PB_RAM.Value = 0;
			TB_RAM.Text = $"В очереди: {PB_RAM.Value} / {PB_RAM.Maximum}";
			TB_CPU_downTime.Text = $"Время простоя: {Simulation.Statistics.Downtime}";
		}



		/// <summary>
		/// Завершает симуляцию, обнуляет статистику и показания
		/// </summary>
		private void ResetParameters() {

			// сброс времен
			CurrentTime = ModelTime = String.Format("0:000");
			VTime = 0;

			// сброс времени простоя процессора
			processor.DownTime_reset();


			// [Form] сброс таймеров
			L_rTime.Content = CurrentTime;
			L_vTime.Content = ModelTime;


			// инициализация (обновление) исходных данных
			Simulation.InitData.Lambda = float.Parse(TB_lambda.Text);
			Simulation.InitData.Mu = float.Parse(TB_mu.Text);
			Simulation.InitData.M = int.Parse(TB_m.Text);
			Simulation.InitData.T = int.Parse(TB_T.Text);
			Simulation.InitData.NumberOfRuns = int.Parse(TB_n.Text);

			PB_RAM.Maximum = Simulation.InitData.M;
			PB_progressStep.Maximum = Simulation.InitData.T;
			PB_progressIter.Maximum = Simulation.InitData.NumberOfRuns;


			// обнуление данных статистики
			ResetParamIteration();
			statistic.Clear();


			// [HOT] - на всякий пересоздаем new stack(m) |~| очистка LIFO-очереди
			if (queue != null) {
				//queue.stack.Clear();
				Simulation.QueueLIFO.stack.Clear();
				queue = new Simulation.QueueLIFO();
			}

			// освобождение процессора
			if (processor != null)
				processor.stack.Clear();


			// сброс прогресса
			PB_progressStep.Value = 0;
			PB_progressIter.Value = 0;
			PB_RAM.Value = 0;
			iteration = 0;
			TB_progressStep.Text = $"прогресс шага: {PB_progressStep.Value} / {PB_progressStep.Maximum}";
			TB_progressIter.Text = $"прогонов: {PB_progressIter.Value} / {PB_progressIter.Maximum}";
			TB_RAM.Text = $"В очереди: {PB_RAM.Value} / {PB_RAM.Maximum}";
			TB_CPU_downTime.Text = $"Время простоя: {Simulation.Statistics.Downtime}";


			L_AVG_Downtime.Content = "0:000";
			L_AVG_LostTaskCount.Content = "0";
			L_AVG_ServedTasksCount.Content = "0";
			L_AVG_IncomingTasksCount.Content = "0";
			L_AVG_FailureProbability.Content = "0";
			L_AVG_ServiceProbability.Content = "0";

			//L_disp_Downtime.Content = "0:000";
			L_disp_LostTaskCount.Content = "0";
			L_disp_ServedTasksCount.Content = "0";
			L_disp_IncomingTasksCount.Content = "0";
			L_disp_FailureProbability.Content = "0";
			L_disp_ServiceProbability.Content = "0";


			EndOfSimulation = false;
		}





		/// <summary>
		/// Выполняется при запуске программы.<br/>
		/// Инициализирует параметры.
		/// </summary>
		public MainWindow() {
			InitializeComponent();

			try {

				processor = new Simulation.Processor();
				queue = new Simulation.QueueLIFO();

				// связываем таблицу и данные из коллекции
				AllStat.ItemsSource = statistic;

				// сброс и обновление параметров
				ResetParameters();

				task_init = 0;
				task_runtime = 0;
			}
			catch (Exception ex) {
				MessageBox.Show($"Возникло непредвиденное исключение. Текст ошибки: \"{ex.Message}\"", "Внимание!");
				throw;
			}

			// инициализация таймера реального времени
			realTime.Tick += new EventHandler(TimerTick_RealTime);
			realTime.Interval = TimeSpan.FromMilliseconds(1);


			// инициализация таймера виртуального (модельного) времени
			virtualTime.Tick += new EventHandler(TimerTick_VirtualTime);
			virtualTime.Interval = TimeSpan.FromSeconds(VTime);
			//virtualTime.Interval = TimeSpan.FromMilliseconds(1);



			// виртуальное время для отображения действий посекундно
			virtualTime_sec.Tick += new EventHandler(VirtualTime_sec_Tick);
			virtualTime_sec.Interval = TimeSpan.FromSeconds(VTime);
			//virtualTime_sec.Interval = TimeSpan.FromMilliseconds(1);



			// интервал генерирования новой заявки
			timer.Tick += new EventHandler(Timer_Tick);
			timer.Interval = TimeSpan.FromSeconds(task_init);
			//timer.Interval = TimeSpan.FromMilliseconds(1);

		}


		// Генерирование новой заявки
		private void Timer_Tick(object sender, EventArgs e) {

			if (stopWatch.IsRunning) {

				// вычисление нового интервала получения заявки
				task_init = Simulation.Task.Generation();

				// вычисление нового интервала выполнения заявки
				task_runtime = Simulation.Processor.Generation();

				// обновляем интервал получения заявки
				timer.Interval = TimeSpan.FromSeconds(task_init);
				//timer.Interval = TimeSpan.FromMilliseconds(1);


				// параметры для изменения счетчика модельного времени
				Min = new double[] { Math.Round(task_init, 3), Math.Round(task_runtime, 3), Simulation.InitData.T - VTime };
				//VTime += Min.Min();


				Start_Simulation();
			}
		}



		/// <summary>
		/// Симулирует выполнение действий с заявкой
		/// </summary>
		public void Start_Simulation() {

			//Simulation.Statistics.IncomingTasksCount++;

			// выполнение заявки в процессоре
			if (processor.IsEmpty()) {
				processor.Add(task_runtime);

				// поступивших заявок +1
				Simulation.Statistics.IncomingTasksCount++;
			}

			// добавление заявки в очередь, если там есть место
			else if (!processor.IsEmpty() && queue.FreeSpaceCheck()) {

				// поступивших заявок +1
				Simulation.Statistics.IncomingTasksCount++;

				Simulation.QueueLIFO.stack.Push(task_runtime);
			}

			// если процессор занят и в очереди нет мест
			else if (!processor.IsEmpty() && !queue.FreeSpaceCheck())
				Simulation.Statistics.LostTaskCount++;


			// поступивших заявок
			L_IncomingTasksCount.Content = Simulation.Statistics.IncomingTasksCount.ToString();

			// обслуженных заявок
			L_ServedTasksCount.Content = Simulation.Statistics.ServedTasksCount.ToString();

			// необслуженных заявок
			L_LostTaskCount.Content = Simulation.Statistics.LostTaskCount.ToString();
		}




		/*****************		Menu panel		******************/

		// Play
		private void Play_Click(object sender, EventArgs e) {

			if (!EndOfSimulation) {

				stopWatch.Start();

				realTime.Start();
				virtualTime.Start();
				virtualTime_sec.Start();
				timer.Start();
			}
			else {
				MessageBoxResult result = MessageBox.Show("Хотите начать симуляцию сначала?", 
					"Подтвердите действие", MessageBoxButton.YesNo, icon:MessageBoxImage.Question);

				if (result == MessageBoxResult.Yes) {
					stopWatch.Restart();
					stopWatch.Stop();

					ResetParameters();
				}
				else
					MessageBox.Show("Результаты представлены в таблице.\nПоследняя строка таблицы содержит средние значения параметров.", 
						"Время симуляции истекло", MessageBoxButton.OK, icon: MessageBoxImage.Information);
			}
		}


		// Pause
		private void Pause_Click(object sender, RoutedEventArgs e) {
			if (stopWatch.IsRunning) {
				stopWatch.Stop();
				processor.DownTime_stop();
			}
		}


		// Stop
		private void Stop_Click(object sender, RoutedEventArgs e) {
			stopWatch.Restart();
			stopWatch.Stop();

			ResetParameters();
		}


		//// Speed Up
		//private void SpeedUp_Click(object sender, RoutedEventArgs e) {
		//	Simulation.Speed *= 2;
		//	speedText.Content = "x" + Simulation.Speed.ToString();
		//}


		//// Speed Down
		//private void SpeedDown_Click(object sender, RoutedEventArgs e) {
		//	Simulation.Speed /= 2;
		//	speedText.Content = "x" + Simulation.Speed.ToString();
		//}


		//// Speed Reset
		//private void SpeedReset_Click(object sender, RoutedEventArgs e) {
		//	Simulation.Speed = 1;
		//	speedText.Content = "x" + Simulation.Speed.ToString();
		//}

	}
}