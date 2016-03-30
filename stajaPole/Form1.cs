using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace stajaPole
{
    public partial class Form1 : Form
    {
        //Переменные
        #region
        PictureBox[] jachPB, //Ячейки полигона
                     robPB; //Роботы

        int[,] distanceMatrix = new int[100, 100]; //Хранение матрицы расстояний: если переход в клетку возможен - 1

        int[][] zadanijaMatrix = new int[100][]; //Хранение матрицы заданий

        bool workEnd = false, //Хранение состояния работы: завершена/не завершена
             started = false; //Хранение состояния работы: начата/не начата

        int enterJach = 0, //Хранение номера входной ячейки
            needToVisit = 0, //Хранение кол-ва ячеек, которые осталось посетить
            numOfClicks = 0, //Хранение числа нажатий по карте - для проверки, производилась ли рисовка коридоров
            numberOfDrones = 1, //Хранение количества дронов
            sharSpeed = 1; //Хранение скорости движения дронов
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        //Рисование сетки-полигона
        private void Form1_Load(object sender, EventArgs e)
        {
            jachPB = new PictureBox[100];

            int jachNum = 0;

            for (int x = 0; x < 500; x += 50)
            {
                for (int y = 0; y < 500; y += 50)
                {
                    jachPB[jachNum] = new PictureBox();
                    jachPB[jachNum].Location = new Point(x, y);
                    jachPB[jachNum].Name = "ClosedWay";
                    jachPB[jachNum].Size = new Size(50, 50);
                    jachPB[jachNum].BackColor = Color.White;
                    jachPB[jachNum].Visible = true;
                    jachPB[jachNum].MouseMove += new MouseEventHandler(this.Form1_MouseMove);

                    Controls.Add(jachPB[jachNum]);

                    Controls.SetChildIndex(jachPB[jachNum], 0);
                    jachNum++;
                }
            }
        }

        //Рисование лабиринта
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (started == false)
            {
                if (e.Button == MouseButtons.Left)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        if ((Cursor.Position.Y >= jachPB[i].Location.Y) &&
                            (Cursor.Position.Y <= jachPB[i].Location.Y + 50) &&
                            (Cursor.Position.X >= jachPB[i].Location.X) &&
                            (Cursor.Position.X <= jachPB[i].Location.X + 50))
                        {
                            jachPB[i].BackColor = Color.Black;
                            jachPB[i].Name = "OpenedWay";

                            numOfClicks++;

                            if (numOfClicks == 1)
                            {
                                enterJach = i;
                            }
                        }
                    }
                }
            }
        }

        //Рисование дронов и запуск движения
        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (numOfClicks == 0)
            {
                var result = MessageBox.Show("Лабиринт не нарисован!",
                                             "Ошибка",
                                             MessageBoxButtons.OK,
                                             MessageBoxIcon.Error);
                return;
            }

            if (started == false)
            {
                jachPB[enterJach].BackColor = Color.Yellow;
                jachPB[enterJach].Name = "VisitedWay";

                for (int i = 0; i != 100; i++)
                {
                    if (jachPB[i].Name == "OpenedWay")
                    {
                        needToVisit++;
                    }
                }

                started = true;

                calculateMatrix();

                robPB = new PictureBox[numberOfDrones];

                for (int sharNumber = 0; sharNumber < numberOfDrones; sharNumber++)
                {
                    int sharCoordX, sharCoordY;

                    sharCoordX = jachPB[enterJach].Location.X + 20;
                    sharCoordY = jachPB[enterJach].Location.Y + 20;

                    robPB[sharNumber] = new PictureBox();
                    robPB[sharNumber].Location = new Point(sharCoordX,
                                                            sharCoordY);
                    robPB[sharNumber].Name = "Shar" + sharNumber.ToString();
                    robPB[sharNumber].Size = new Size(10, 10);
                    robPB[sharNumber].BackColor = Color.Blue;
                    robPB[sharNumber].Visible = true;

                    Controls.Add(robPB[sharNumber]);

                    Controls.SetChildIndex(robPB[sharNumber], 1);
                }

                Dogon.Start();
            }
        }

        //Вычисление матрицы расстояний
        private void calculateMatrix()
        {
            for (int i = 0; i != 100; i++)
            {
                for (int j = 0; j != 100; j++)
                {
                    if ((jachPB[i].Name != "ClosedWay") && (jachPB[j].Name != "ClosedWay"))
                    {
                        //Вычисление катета X между серединами двух клеток
                        double graniX = (jachPB[i].Location.X + 25) - (jachPB[j].Location.X + 25);
                        //Вычисление катета Y между серединами двух клеток
                        double graniY = (jachPB[i].Location.Y + 25) - (jachPB[j].Location.Y + 25);

                        //Вычисление гипотенузы между серединами двух клеток
                        double graniDist = Math.Sqrt(Math.Pow(graniX, 2) + Math.Pow(graniY, 2));

                        //Если расстояние до клетки = 50, значит это клетка, граничащая с текущей по вертикали или по горизонтали
                        if (graniDist == 50)
                        {
                            distanceMatrix[i, j] = 1;
                        }
                        //Если расстояние до клетки = 0, значит это текущая же клетка
                        //Если расстояние до клетки = sqrt(50^2+50^2) = 71, значит это клетка, граничащая с текущей по диагонали
                        //Если расстояние до клетки > 71, значит эта клетка вообще не граничит с текущей
                        else if ((graniDist == 0) || (graniDist > 50))
                        {
                            distanceMatrix[i, j] = 0;
                        }
                    }
                }
            }

            createTasks(enterJach);
        }

        //Генерация списка задач, трассировка путей, удаление дубликатов
        private void createTasks(int placeCoord)
        {
            if (placeCoord != enterJach)
            {
                //Удаление старых заданий перед загрузкой новых
                for (int k = 0; k < zadanijaMatrix.Length; k++)
                {
                    zadanijaMatrix[k] = null;
                }
            }

            int calc = 0;

            for (int i = 0; i != 100; i++)
            {
                if (jachPB[i].Name == "OpenedWay")
                {
                    Array.Resize(ref zadanijaMatrix, 100);
                    zadanijaMatrix[calc] = Dijkstra(distanceMatrix, placeCoord, i);
                    calc++;
                }
            }

            //Удаление пустых заданий
            zadanijaMatrix = zadanijaMatrix.Where(x => x != null).ToArray();

            //Если задание входит в состав других заданий (т.е. не уникально) - более короткое удаляется
            #region
            List<HashSet<int>> sets = zadanijaMatrix.Select(set => new HashSet<int>(set)).ToList();
            var superSets = sets.Where(s1 => !sets.Any(s2 => s1 != s2 && s1.IsSubsetOf(s2)));

            //Удаление всех заданий перед загрузкой уникальных
            for (int k = 0; k < zadanijaMatrix.Length; k++)
            {
                zadanijaMatrix[k] = null;
            }

            int alpha = 0;

            foreach (var set in superSets)
            {
                zadanijaMatrix[alpha] = set.ToArray();
                alpha++;
            }
            #endregion

            //Удаление пустых заданий
            zadanijaMatrix = zadanijaMatrix.Where(x => x != null).ToArray();
        }

        //Алгоритм Дейкстры
        public static int[] Dijkstra(int[,] GR, int st, int finale)
        {
            // Определяем количество вершин в графе. Точнее, это индекс последнего элемента матрицы +1.
            int V = GR.GetUpperBound(1) + 1,
                count,
                index = 100000,
                i,
                u,
                m = st + 1;

            int[] distance = new int[V];

            bool[] visited = new bool[V];

            string shortset_way = null;

            for (i = 0; i < V; i++)
            {
                distance[i] = 100000;
                visited[i] = false;
            }

            distance[st] = 0;

            for (count = 0; count < V - 1; count++)
            {
                int min = 100000;

                for (i = 0; i < V; i++)
                {
                    if ((!visited[i]) &&
                    (distance[i] <= min))
                    {
                        min = distance[i];
                        index = i;
                    }
                }

                u = index;
                visited[u] = true;

                for (i = 0; i < V; i++)
                {
                    if ((!visited[i]) &&
                    ((GR[u, i] != 0) &&
                    (distance[u] != 100000)) &&
                    (distance[u] + GR[u, i] < distance[i]))
                    {
                        distance[i] = distance[u] + GR[u, i];
                    }
                }
            }

            int dis_to_finish = distance[finale];
            shortset_way += finale.ToString();

            while (dis_to_finish > 0)
            {
                for (i = 0; i < V; i++)
                {
                    if ((GR[i, finale] != 0) &&
                    (dis_to_finish - GR[i, finale] == distance[i]))
                    {
                        shortset_way = shortset_way + '-' + i.ToString();
                        dis_to_finish = distance[i];
                        finale = i;
                    }
                }
            }

            //Тут собственно выводится кратчайший путь, но в виде строки. Пример: 1-5-6
            string[] array = shortset_way.Split('-');
            int[] shortezd = new int[array.Length];

            for (i = 0; i < array.Length; i++)
            {
                shortezd[i] = Convert.ToInt32(array[i]);
            }

            shortezd = shortezd.Reverse().ToArray();
            return shortezd;
        }

        //Движение дронов с помощью метода потенциальных полей
        public void Dogon_Tick(object sender, EventArgs e)
        {
            for (int sharNumber = 0; sharNumber < numberOfDrones; sharNumber++)
            {

                //Стараемся двигаться к вершинам графов
                #region 
                for (int i = 0; i < zadanijaMatrix.Length; i++)
                {
                    int j;
                    for (j = 0; j < zadanijaMatrix[i].Length; j++)
                    {
                        //Вычисление расстояния между серединами клетки и шара
                        double jachDist = Math.Sqrt(Math.Pow(((jachPB[zadanijaMatrix[i][j]].Location.X + 25) - (robPB[sharNumber].Location.X + 5)), 2) +
                                                    Math.Pow(((jachPB[zadanijaMatrix[i][j]].Location.Y + 25) - (robPB[sharNumber].Location.Y + 5)), 2));

                        while (jachDist > 10)
                        {
                            //Избегаем столкновения с другими шарами
                            #region
                            for (int preSharNumber = 0; preSharNumber < numberOfDrones; preSharNumber++)
                            {
                                //Если вычли единицу из 0-го шара - перескакиваем на последний шар
                                if (preSharNumber < 0)
                                {
                                    preSharNumber = numberOfDrones - 1;
                                }

                                //Вычисление расстояния между серединами 2-х шаров
                                double sharDist = Math.Sqrt(Math.Pow(((robPB[preSharNumber].Location.X + 5) - (robPB[sharNumber].Location.X + 5)), 2) +
                                                            Math.Pow(((robPB[preSharNumber].Location.Y + 5) - (robPB[sharNumber].Location.Y + 5)), 2));

                                //Чтобы шары не пересекались даже в случае контакта диагоналями, расстояние между ними должно быть равно их гипотенузе
                                //т.е. sqrt(100+100)=14
                                if (sharDist != 0 && sharDist < 15)
                                {
                                    robPB[sharNumber].Location = new Point(robPB[sharNumber].Location.X -
                                                                           Convert.ToInt32((((robPB[preSharNumber].Location.X + 5) - (robPB[sharNumber].Location.X + 5)) / sharDist * sharSpeed)),
                                                                           robPB[sharNumber].Location.Y -
                                                                           Convert.ToInt32((((robPB[preSharNumber].Location.Y + 5) - (robPB[sharNumber].Location.Y + 5)) / sharDist * sharSpeed)));
                                }
                            }
                            #endregion

                            //Избегаем столкновения со стенами лабиринта
                            #region
                            for (int iWalls = 0; iWalls != 100; iWalls++)
                            {
                                if (jachPB[iWalls].Name == "ClosedWay")
                                {
                                    //Вычисление расстояния между серединами шара и стены
                                    double stenaDist = Math.Sqrt(Math.Pow(((jachPB[iWalls].Location.X + 25) - (robPB[sharNumber].Location.X + 5)), 2) +
                                                                 Math.Pow(((jachPB[iWalls].Location.Y + 25) - (robPB[sharNumber].Location.Y + 5)), 2));

                                    //Чтобы шары не пересекались со стеной даже в случае контакта диагоналями, расстояние между ними должно быть равно их гипотенузе
                                    //т.е. sqrt(625+625)=35
                                    if (stenaDist < 50)
                                    {
                                        robPB[sharNumber].Location = new Point(robPB[sharNumber].Location.X -
                                                                                Convert.ToInt32((((jachPB[iWalls].Location.X + 25) - (robPB[sharNumber].Location.X + 5)) / stenaDist * sharSpeed)),
                                                                                robPB[sharNumber].Location.Y -
                                                                                Convert.ToInt32((((jachPB[iWalls].Location.Y + 25) - (robPB[sharNumber].Location.Y + 5)) / stenaDist * sharSpeed)));
                                    }
                                }
                            }
                            #endregion

                            //Двигаемся
                            #region
                            //Вычисление расстояния между серединами клетки и шара
                            jachDist = Math.Sqrt(Math.Pow(((jachPB[zadanijaMatrix[i][j]].Location.X + 25) - (robPB[sharNumber].Location.X + 5)), 2) +
                                                 Math.Pow(((jachPB[zadanijaMatrix[i][j]].Location.Y + 25) - (robPB[sharNumber].Location.Y + 5)), 2));

                            robPB[sharNumber].Location = new Point(robPB[sharNumber].Location.X +
                                                                   Convert.ToInt32((((jachPB[zadanijaMatrix[i][j]].Location.X + 25) - (robPB[sharNumber].Location.X + 5)) / jachDist * sharSpeed)),
                                                                   robPB[sharNumber].Location.Y +
                                                                   Convert.ToInt32((((jachPB[zadanijaMatrix[i][j]].Location.Y + 25) - (robPB[sharNumber].Location.Y + 5)) / jachDist * sharSpeed)));

                            //Помещаем именем и цветом посещенные ячейки
                            if ((jachDist <= 13) && (jachPB[zadanijaMatrix[i][j]].Name == "OpenedWay"))
                            {
                                jachPB[zadanijaMatrix[i][j]].BackColor = Color.Red;
                                jachPB[zadanijaMatrix[i][j]].Name = "VisitedWay";
                                needToVisit--;

                                if ((needToVisit == 0) && (workEnd == false))
                                {
                                    workEnd = true;
                                    label1.Text = "Задача выполнена!";
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    //Сохраняем текущее местоположение для подачи его в алгоритм как входного
                    int nowCoord = zadanijaMatrix[i][j - 1];

                    //Формируем новые задания относительно нового местоположения
                    createTasks(nowCoord);
                }
            }
        }

        //Очистка неугодного коридора
        private void отменаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (started != true)
            {
                numOfClicks = 0;
                Form1_Load(sender, e);
            }
        }

        //Закрытие окна
        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
