using System;
using System.Collections.Generic;
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

namespace AI_LAB_7
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            widthOfCell = this.Width / levelSize;
            heightOfCell = this.Height / levelSize;

            canvas = drawCanvas;

            Iterate();
        }

        Canvas canvas;

        const int levelSize = 24;               // размер игрового поля
        const int maxResources = 250;               // предельное число растений
        const int maxAgents = 125;               // предельное число агентов

        const int enterAmount = 16;             // число входов
        const int exitAmount = 5;               // число выходов
        const int weight = ((enterAmount * exitAmount) + exitAmount);  // число весов

        const double firstWorkbenchArea = 8;
        const double firstWorkbenchCost = 31;
        const double firstWorkbenchPerformance = 66;

        const double secondWorkbenchArea = 13;
        const double secondWorkbenchCost = 24;
        const double secondWorkbenchPerformance = 51;

        const double MaxArea = 1000;
        const double MaxCost = 2500;

        const double reproductionEnergy = 0.7;  // энергия репродукции

        public double foodCost = MaxCost/maxResources;
        public double foodArea = MaxArea/maxResources;

        public double widthOfCell;
        public double heightOfCell;

        Random random = new Random();

        int[,,] map = new int[3, levelSize, levelSize];
        Point[] costs = new Point[maxResources];
        Point[] areas = new Point[maxResources];
        Agent[] costAgents = new Agent[maxResources];
        Agent[] areaAgents = new Agent[maxResources];
        Agent[] agents = new Agent[maxAgents];

        Point[] north = { new Point(1, -2), new Point(-1, -2), new Point(0, -2), new Point(0, -1) };
        Point[] west = { new Point(-2, -1), new Point(-2, 1), new Point(-1, 0), new Point(-1, 0) };
        Point[] close = { new Point(-1, 0), new Point(-1, 1), new Point(-1, -1), new Point(0, -1), new Point(0, 1), new Point(1, 0), new Point(1, 1), new Point(1, -1) };

        int[] agentTypeCounts = { 0, 0 };         // количество агентов по типам
        int[] agentMaxAge = { 0, 0 };             // возраст агентов по типам
        int[] agentBirths = { 0, 0 };             // количество рождений по типам
        int[] agentDeaths = { 0, 0 };             // количество гибелей по типам
        Agent[] agentMaxPtr = new Agent[2];                   // старейшие агенты по типам
        int[] agentTypeReproductions = { 0, 0 };  // количество репродукций по типам
        int[] bestAgentAge = { 0, 0 };
        int[] bestFirstBiass = new int[exitAmount];
        int[] bestFirstWeights = new int[exitAmount * enterAmount];
        int[] bestSecondBiass = new int[exitAmount];
        int[] bestSecondWeights = new int[exitAmount * enterAmount];
        int[] agentMaxGen = { 0, 0 };             // наибольшие поколения по типам

        async void Iterate()
        {

            Init();
            for (int age = 0; age < 600; age++)
            {     // главный цикл симуляции
                for (int t = (int)Level.Workbench; t <= (int)Level.Area; t++)
                    for (int i = 0; i < maxAgents; i++)
                        if ((int)agents[i].AgentType == t)
                            Simulate(agents[i]);
                await Task.Delay(10);
            }
            canvas.Children.Clear();
            canvas.Children.Add(new Label() { Content = ShowStat() });
        }

        string ShowStat()  // отображение статистики
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Результаты:\n");
            stringBuilder.Append("Первых станков всего                     " + agentTypeCounts[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Вторых станков всего                     " + agentTypeCounts[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("Возраст первых станков                   " + agentMaxAge[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Возраст вторых станков                   " + agentMaxAge[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("Рождений первых станков                  " + agentBirths[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Рождений вторых станков                  " + agentBirths[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("Гибелей первых станков                   " + agentDeaths[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Гибелей вторых станков                   " + agentDeaths[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("Репродукций первых станков               " + agentTypeReproductions[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Репродукций вторых станков               " + agentTypeReproductions[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("Наибольшие поколения первых станков      " + agentMaxGen[(int)AgentType.FirstWorkbench] + "\n");
            stringBuilder.Append("Наибольшие поколения вторых станков      " + agentMaxGen[(int)AgentType.SecondWorkbench] + "\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("Производительность всех станков: " + (agentTypeCounts[(int)AgentType.FirstWorkbench]*firstWorkbenchPerformance + agentTypeCounts[(int)AgentType.SecondWorkbench] * secondWorkbenchPerformance) + "\n");
            stringBuilder.Append("Стоимость всех станков:          " + (agentTypeCounts[(int)AgentType.FirstWorkbench] * firstWorkbenchCost + agentTypeCounts[(int)AgentType.SecondWorkbench] * secondWorkbenchCost) + "\n");
            stringBuilder.Append("При допустимой:                  " + MaxCost +"\n");
            stringBuilder.Append("Площадь всех станков:            " + (agentTypeCounts[(int)AgentType.FirstWorkbench] * firstWorkbenchArea + agentTypeCounts[(int)AgentType.SecondWorkbench] * secondWorkbenchArea) + "\n");
            stringBuilder.Append("При допустимой:                  " + MaxArea + "\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("\nВеса лучшего первого станка:\n");
            for (int i = 0; i < exitAmount; i++)
            {
                stringBuilder.Append(bestFirstBiass[i] + " ");
            }
            stringBuilder.Append("\n\n");
            for (int o = 0; o < exitAmount; o++)
            {
                for (int i = 0; i < enterAmount; i++)
                {
                    stringBuilder.Append(bestFirstWeights[(o * enterAmount) + i] + " ");
                }
                stringBuilder.Append("\n");
            }

            stringBuilder.Append("\nВеса лучшего второго станка:\n");
            for (int i = 0; i < exitAmount; i++)
            {
                stringBuilder.Append(bestSecondBiass[i] + " ");
            }
            stringBuilder.Append("\n\n");
            for (int o = 0; o < exitAmount; o++)
            {
                for (int i = 0; i < enterAmount; i++)
                {
                    stringBuilder.Append(bestSecondWeights[(o * enterAmount) + i] + " ");
                }
                stringBuilder.Append("\n");
            }
            return stringBuilder.ToString();
        }

        Point AddInEmptyCell(Level level)  // добавление в пустую ячейку
        {
            Point res = new Point();
            do
            {
                res.X = random.Next(levelSize);
                res.Y = random.Next(levelSize);
            } while (map[(int)level, (int)res.X, (int)res.Y] != 0);
            map[(int)level, (int)res.X, (int)res.Y]++;
            return res;
        }

        void AgentToMap(Agent agent)  // установка агента на карту
        {
            agent.Location = AddInEmptyCell(Level.Workbench);
        }

        void InitAgent(Agent agent)  // инициализация агента
        {
            agent.Area = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchArea / 2 : secondWorkbenchArea / 2;
            agent.Cost = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchCost / 2 : secondWorkbenchCost / 2;
            agent.Age = 0;
            agent.Generation = 1;
            agentTypeCounts[(int)agent.AgentType]++;
            AgentToMap(agent);
            for (int i = 0; i < (enterAmount * exitAmount); i++)
                agent.Weight[i] = random.Next(10) - 1;
            for (int i = 0; i < exitAmount; i++)
                agent.Biass[i] = random.Next(10) - 1;
        }
        void Init()  // инициализация модели
        {
            for (int l = 0; l < 3; l++)                    // очистка карты
                for (int y = 0; y < levelSize; y++)
                    for (int x = 0; x < levelSize; x++)
                        map[l, x, y] = 0;
            for (int p = 0; p < maxResources; p++)                 // посадка растений
            {
                areas[p] = AddInEmptyCell(Level.Area);
                costs[p] = AddInEmptyCell(Level.Cost);
                areaAgents[p] = new Agent(canvas, widthOfCell, heightOfCell) { Visual = AreaPolyline, Location = areas[p] };
                costAgents[p] = new Agent(canvas, widthOfCell, heightOfCell) { Visual = CostPolyline, Location = costs[p] };
            }

            for (int a = 0; a < maxAgents; a++)
            {
                agents[a] = new Agent(canvas, widthOfCell, heightOfCell);// инициализация агентов
                if (a < (maxAgents / 2))
                {
                    agents[a].AgentType = AgentType.FirstWorkbench;
                    agents[a].Visual = FirstWorkbenchPolyline;
                }
                else
                {
                    agents[a].AgentType = AgentType.SecondWorkbench;
                    agents[a].Visual = SecondWorkbenchPolyline;
                }
                InitAgent(agents[a]);
            }
        }
        int Clip(int z)  // переход через границу
        {
            if (z > levelSize - 1)
                z = (z % levelSize);
            else if (z < 0) z = (levelSize + z);
            return z;
        }

        void Percept(int x, int y, Agent agent, Detected input, Point[] offsets, int neg)  // восприятие
        {
            for (int p = (int)Level.Workbench; p <= (int)Level.Cost; p++)
            {
                int i = 0;
                agent.Inputs[p] = 0;
                for (i = 0; i < offsets.Length; i++)
                {
                    int xoff = Clip(x + ((int)offsets[i].X * neg));
                    int yoff = Clip(y + ((int)offsets[i].Y * neg));
                    if (map[p, xoff, yoff] != 0)
                        agent.Inputs[(int)input + p]++;
                }
            }
        }

        void KillAgent(Agent agent)  // гибель агента
        {
            agentDeaths[(int)agent.AgentType]++;
            map[0, (int)agent.Location.X, (int)agent.Location.Y]--;
            agentTypeCounts[(int)agent.AgentType]--;
            if (agent.Age > bestAgentAge[(int)agent.AgentType])
            {  // сохраняем лучшего
                if (agent.AgentType == AgentType.FirstWorkbench)
                {
                    bestFirstBiass = agent.Biass;
                    bestFirstWeights = agent.Weight;
                }
                else
                {
                    bestSecondBiass = agent.Biass;
                    bestSecondWeights = agent.Weight;
                }
            }
            if (agentTypeCounts[(int)agent.AgentType] < (maxAgents / 6))
            {
                InitAgent(agent);  // инициализация агента
            }
            else
            {                                                            // конец агента
                agent.AgentType = AgentType.Corpse;
            }
        }

        void ReproduceAgent(Agent agent, Direction direction)  // рождение потомства
        {
            Agent child;
            int i;

            if (agentTypeCounts[(int)agent.AgentType] < (maxAgents))
            {
                for (i = 0; i < maxAgents; i++)
                {
                    if (agents[i].AgentType == AgentType.Corpse)
                        break;
                }
                if (i < maxAgents)
                {
                    Point offset = new Point();

                    switch (direction)
                    {
                        case Direction.North:
                            offset = new Point(0, -2);
                            break;
                        case Direction.South:
                            offset = new Point(0, 2);
                            break;
                        case Direction.East:
                            offset = new Point(2, 0);
                            break;
                        case Direction.West:
                            offset = new Point(-2, 0);
                            break;
                    }

                    Point childrenPoint = map[0, Clip((int)(agent.Location.X + offset.X)), Clip((int)(agent.Location.Y + offset.Y))] == 0 ?
                        new Point(Clip((int)(agent.Location.X + offset.X)), Clip((int)(agent.Location.Y + offset.Y))) :
                        map[0, Clip((int)(agent.Location.X + offset.X / 2)), Clip((int)(agent.Location.Y + offset.Y)) / 2] == 0 ?
                        new Point(Clip((int)(agent.Location.X + offset.X / 2)), Clip((int)(agent.Location.Y + offset.Y / 2))) : new Point(-1, -1);

                    if (childrenPoint.X < 0)
                        return;

                    agents[i] = child = new Agent(canvas, widthOfCell, heightOfCell)
                    {
                        AgentType = agent.AgentType,
                        Generation = agent.Generation + 1,
                        Age = 0,
                        Biass = agent.Biass,
                        AgentAction = agent.AgentAction,
                        Visual = agent.AgentType == AgentType.FirstWorkbench ? FirstWorkbenchPolyline : SecondWorkbenchPolyline,
                        Weight = agent.Weight,
                        Inputs = agent.Inputs
                    };
                    
                    if (random.NextDouble() <= 0.3)
                    {
                        child.Weight[random.Next(enterAmount * exitAmount)] = random.Next(10) - 1;
                    }

                    child.Location = childrenPoint;
                    map[0, (int)child.Location.X, (int)child.Location.Y]++;

                    if (agentMaxGen[(int)child.AgentType] < child.Generation)
                        agentMaxGen[(int)child.AgentType] = child.Generation;

                    child.Area = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchArea / 2 : secondWorkbenchArea / 2;
                    child.Cost = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchCost / 2 : secondWorkbenchCost / 2;

                    agentBirths[(int)child.AgentType]++;
                    agentTypeCounts[(int)child.AgentType]++;
                    agentTypeReproductions[(int)child.AgentType]++;
                }
            }
        }

        bool ChooseObject(Level level, int ax, int ay, Point[] offsets, int neg, out int ox, out int oy)  // выбор объекта
        {
            int xoff, yoff, i = 0;

            for (i = 0; i < offsets.Length; i++)
            {
                xoff = Clip(ax + ((int)offsets[i].X * neg));
                yoff = Clip(ay + ((int)offsets[i].Y * neg));
                if (map[(int)level, xoff, yoff] != 0)
                {
                    ox = xoff; oy = yoff;
                    return true;
                }
            }
            ox = 0; oy = 0;
            return false;
        }

        void Eat(Agent agent)  // питание
        {
            int oxA = 0, oyA = 0, oxC = 0, oyC = 0, i;
            bool area = false;
            bool cost = false;

            int ax = (int)agent.Location.X;
            int ay = (int)agent.Location.Y;

                area = ChooseObject(Level.Area, ax, ay, close, 1, out oxA, out oyA);
                cost = ChooseObject(Level.Cost, ax, ay, close, 1, out oxC, out oyC);

            if (area || cost)
            {

                if (cost)
                {

                    for (i = 0; i < maxResources; i++)
                    {
                        if ((int)costs[i].X == oxC && (int)costs[i].Y == oyC)
                            break;
                    }

                    if (i < maxResources)
                    {
                        agent.Cost += foodCost;
                        //if (agent.Cost > ( agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchCost : secondWorkbenchCost ))
                        //    agent.Cost = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchCost : secondWorkbenchCost;
                        map[(int)Level.Cost, oxC, oyC]--;
                        costs[i] = AddInEmptyCell(Level.Cost);
                        canvas.Children.Remove(costAgents[i].Visual);
                        costAgents[i] = new Agent(canvas, widthOfCell, heightOfCell) { Visual = CostPolyline, Location = costs[i] };
                    }

                }
                if (area)
                {

                    for (i = 0; i < maxResources; i++)
                    {
                        if ((int)areas[i].X == oxA && (int)areas[i].Y == oyA)
                            break;
                    }

                    if (i < maxResources)
                    {
                        agent.Area += foodArea;
                        //if (agent.Area > (agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchArea : secondWorkbenchArea))
                        //    agent.Area = agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchArea : secondWorkbenchArea;
                        map[(int)Level.Area, oxA, oyA]--;
                        areas[i] = AddInEmptyCell(Level.Area);
                        canvas.Children.Remove(areaAgents[i].Visual);
                        areaAgents[i] = new Agent(canvas, widthOfCell, heightOfCell) { Visual = AreaPolyline, Location = areas[i] };
                    }

                }
            }
        }

        void Simulate(Agent agent)  // симуляция агента
        {
            int x = (int)agent.Location.X;
            int y = (int)agent.Location.Y;

                    Percept(x, y, agent, Detected.WorkbenchFromFront, north, 1);
                    Percept(x, y, agent, Detected.WorkbenchFromLeft, west, 1);
                    Percept(x, y, agent, Detected.WorkbenchFromRight, west, -1);
                    Percept(x, y, agent, Detected.WorkbenchFromBack, north, -1);

            for (int exit = 0; exit < exitAmount; exit++)
            {   // расчёт решений
                agent.AgentAction[exit] = agent.Biass[exit];    // инициализация выхода смещением
                for (int enter = 0; enter < enterAmount; enter++)
                {          // расчёт по взвешенным входам
                    agent.AgentAction[exit] += (agent.Inputs[enter] * agent.Weight[(exit * enterAmount) + enter]);
                }
            }

            int largest = -9;
            int winner = -1;
            for (int exit = 0; exit < exitAmount; exit++)
            {   // принятие решения
                if (agent.AgentAction[exit] >= largest)
                {
                    largest = agent.AgentAction[exit];
                    winner = exit;
                }
            }

            bool isFed = true;
            if (agent.Area < (reproductionEnergy * (agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchArea : secondWorkbenchArea)) ||
    agent.Cost < (reproductionEnergy * (agent.AgentType == AgentType.FirstWorkbench ? firstWorkbenchCost : secondWorkbenchCost)))
            {
                isFed = false;
            }

            // выполнение решения
            switch((AgentAction)winner)
            {
                case AgentAction.SpreadNorth:
                    if(isFed) ReproduceAgent(agent, Direction.North);
                    break;
                case AgentAction.SpreadWest:
                    if (isFed) ReproduceAgent(agent, Direction.West);
                    break;
                case AgentAction.SpreadEast:
                    if (isFed) ReproduceAgent(agent, Direction.East);
                    break;
                case AgentAction.SpreadSouth:
                    if (isFed) ReproduceAgent(agent, Direction.South);
                    break;
                case AgentAction.Eat:
                    Eat(agent);
                    break;
            }

            // затраты энергии
            if (agent.AgentType == AgentType.FirstWorkbench)
            {
                agent.Cost -= foodCost * 0.2 / firstWorkbenchPerformance * Math.Max(firstWorkbenchPerformance, secondWorkbenchPerformance);
                agent.Area -= foodArea * 0.2 / firstWorkbenchPerformance * Math.Max(firstWorkbenchPerformance, secondWorkbenchPerformance);
            }
            else
            {
                agent.Cost -= foodCost * 0.2 / firstWorkbenchPerformance * Math.Max(firstWorkbenchPerformance, secondWorkbenchPerformance);
                agent.Area -= foodArea * 0.2 / firstWorkbenchPerformance * Math.Max(firstWorkbenchPerformance, secondWorkbenchPerformance);
            }

            if (agent.Cost <= 0 || agent.Area <=0)
                KillAgent(agent);   // гибель и жизнь агента
            else
            {
                agent.Age++;
                if (agent.Age > agentMaxAge[(int)agent.AgentType])
                {  // фиксируем старейшего агента
                    agentMaxAge[(int)agent.AgentType] = agent.Age;
                    agentMaxPtr[(int)agent.AgentType] = agent;
                }
            }
        }

        class Agent
        {
            public Agent(Canvas canvas, double widthOfCell, double heightOfCell)
            {
                Inputs = new int[enterAmount];
                AgentAction = new int[exitAmount];
                Biass = new int[exitAmount];
                Weight = new int[enterAmount * exitAmount];
                Canvas = canvas;
                Area = 0;
                Cost = 0;
                this.widthOfCell = widthOfCell;
                this.heightOfCell = heightOfCell;
            }
            Canvas Canvas { get; }
            double widthOfCell;
            double heightOfCell;
            private Polyline visual;
            public Polyline Visual
            {
                get => visual;
                set
                {
                    visual = value;
                    Canvas.SetLeft(visual, Location.X * widthOfCell);
                    Canvas.SetTop(visual, Location.Y * heightOfCell);
                    Canvas.Children.Add(visual);
                }
            }
            private AgentType agentType;
            public AgentType AgentType
            {
                get => agentType;
                set
                {
                    agentType = value;

                    if (value == AgentType.Corpse)
                    {
                        Canvas.Children.Remove(visual);
                        location = new Point(-1, -1);
                    }
                }
            }
            public double Area { get; set; }
            public double Cost { get; set; }
            //public Agent Parent { get; set; }
            public int Age { get; set; }
            public int Generation { get; set; }
            private Point location;
            public Point Location
            {
                get => location;
                set
                {
                    Canvas.Children.Remove(Visual);
                    location = value;
                    Canvas.SetLeft(Visual, Location.X * widthOfCell);
                    Canvas.SetTop(Visual, Location.Y * heightOfCell);
                    Canvas.Children.Add(Visual);
                }
            }
            public int[] Inputs { get; set; }
            public int[] Weight { get; set; }
            public int[] Biass { get; set; }
            public int[] AgentAction { get; set; }
        }

        Polyline FirstWorkbenchPolyline => new Polyline
        {
            Points = new PointCollection {
                new Point(0.2 * widthOfCell, 0* heightOfCell),
                new Point(0 * widthOfCell, 0.2* heightOfCell),
                new Point(0* widthOfCell, 0.8* heightOfCell),
                new Point(0.2* widthOfCell, 1* heightOfCell),
                new Point(0.8* widthOfCell, 1* heightOfCell),
                new Point(1* widthOfCell, 0.8* heightOfCell),
                new Point(1* widthOfCell, 0.2* heightOfCell),
                new Point(0.8* widthOfCell, 0* heightOfCell),
                new Point(0.2 * widthOfCell, 0* heightOfCell),
                 },
            Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
            StrokeThickness = 0.1 * Math.Min(widthOfCell, heightOfCell)
        };

        Polyline SecondWorkbenchPolyline => new Polyline
        {
            Points = new PointCollection {
                new Point(0.3 * widthOfCell, 0* heightOfCell),
                new Point(0 * widthOfCell, 0.3* heightOfCell),
                new Point(0* widthOfCell, 0.7* heightOfCell),
                new Point(0.3* widthOfCell, 1* heightOfCell),
                new Point(0.7* widthOfCell, 1* heightOfCell),
                new Point(1* widthOfCell, 0.7* heightOfCell),
                new Point(1* widthOfCell, 0.3* heightOfCell),
                new Point(0.7* widthOfCell, 0* heightOfCell),
                new Point(0.3 * widthOfCell, 0* heightOfCell),
                 },
            Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)),
            StrokeThickness = 0.1 * Math.Min(widthOfCell, heightOfCell)
        };

        Polyline AreaPolyline => new Polyline
        {
            Points = new PointCollection {
                new Point(0.4 * widthOfCell, 0.4* heightOfCell),
                new Point(0.6 * widthOfCell, 0.4* heightOfCell),
                new Point(0.6 * widthOfCell, 0.6* heightOfCell),
                new Point(0.4 * widthOfCell, 0.6* heightOfCell),
                new Point(0.4 * widthOfCell, 0.4* heightOfCell),
                 },
            Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
            StrokeThickness = 0.1 * Math.Min(widthOfCell, heightOfCell)
        };

        Polyline CostPolyline => new Polyline
        {
            Points = new PointCollection {
                new Point(0.3 * widthOfCell, 0.3* heightOfCell),
                new Point(0.7 * widthOfCell, 0.3* heightOfCell),
                new Point(0.7 * widthOfCell, 0.7* heightOfCell),
                new Point(0.3 * widthOfCell, 0.7* heightOfCell),
                new Point(0.3 * widthOfCell, 0.3* heightOfCell),
                 },
            Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)),
            StrokeThickness = 0.1 * Math.Min(widthOfCell, heightOfCell)
        };
    }



    enum Direction
    {
        North,
        South,
        East,
        West
    }
    enum AgentAction
    {
        SpreadNorth,
        SpreadSouth,
        SpreadWest,
        SpreadEast,
        Eat
    }
    enum Level
    {
        Workbench,
        Area,
        Cost
    }
    enum AgentType
    {
        FirstWorkbench,
        SecondWorkbench,
        Corpse = -1
    }
    enum Detected
    {
        WorkbenchFromFront,
        AreaFromFront,
        CostFromFront,

        WorkbenchFromLeft,
        AreaFromLeft,
        CostFromLeft,

        WorkbenchFromRight,
        AreaFromRight,
        CostFromRight,

        WorkbenchFromBack,
        AreaFromBack,
        CostFromBack,        
    }
}
