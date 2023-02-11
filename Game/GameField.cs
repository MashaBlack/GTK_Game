using System;
using System.Collections.Generic;
namespace Game
{
    struct Cluster // кластер (последовательность одинаковых элементов)
    {
        public int Y { get; private set; }
        public int X { get; private set; }
        public int Length { get; private set; }
        public bool Horizontal { get; private set; }

        public Cluster(int x, int y, int length, bool horizontal)
        {
            X = x;
            Y = y;
            Length = length;
            Horizontal = horizontal;
        }
    }

    public struct Move // ход (смена элементов)
    {
        public int X1 { get; private set; }
        public int Y1 { get; private set; }
        public int X2 { get; private set; }
        public int Y2 { get; private set; }

        public Move(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
    }

    public class GameField // класс игрового поля
    {
        private const int minClusterLength = 3;

        public int FieldSize { get; private set; }
        public int NumElements { get; private set; }
        public int[,] Field { get; private set; }

        public int Score { get; private set; }

        public delegate void animateSwap(int x1, int y1, int x2, int y2);
        public event animateSwap OnSwap;

        public GameField(int fieldSize, int numElements, animateSwap onSwap)
        {
            FieldSize = fieldSize;
            NumElements = numElements;

            Field = new int[fieldSize, fieldSize];

            OnSwap += onSwap;
            do
            {
                generateField();
            } while (findMoves().Count == 0);

            Score = 0;
        }

        public void swap(int x1, int y1, int x2, int y2) // смена элементов
        {
            int tmp = Field[y1, x1];
            Field[y1, x1] = Field[y2, x2];
            Field[y2, x2] = tmp;
        }

        public bool canSwap(int x1, int y1, int x2, int y2) // проверка на возможность смены элементов
        {
            if ((x1 == x2 && y1 == y2 + 1) || (x1 == x2 && y1 == y2 - 1) ||
                (x1 == x2 + 1 && y1 == y2) || (x1 == x2 - 1 && y1 == y2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void generateField() // генерация поля
        {
            Random rand = new Random();

            for (int i = 0; i < FieldSize; i++)
            {
                for (int j = 0; j < FieldSize; j++)
                {
                    Field[i, j] = rand.Next(NumElements);
                }
            }
            resolveClusters();
        }

        public void resolveClusters() // уничтожение кластера и генерация новых элементов
        {
            List<Cluster> clusters = findClusters();
            bool isFirst = true;
            while (clusters.Count > 0)
            {
                removeClusters(clusters, isFirst);
                isFirst = false;
                generateNewElements();
                while (!checkField())
                {
                    generateNewElements();
                }
                clusters = findClusters();
            }
        }

        private void generateNewElements() // генерация новых элементов
        {
            Random rand = new Random();
            for (int j = 0; j < FieldSize; j++)
            {
                if (Field[0, j] == -1)
                {
                    Field[0, j] = rand.Next(NumElements);
                }
            }
        }

        private List<Cluster> findClusters() // поиск кластеров
        {
            List<Cluster> clusters = new List<Cluster>();

            // горизонтальные
            for (int i = 0; i < FieldSize; i++)
            {
                int matchLen = 1;
                for (int j = 0; j < FieldSize; j++)
                {
                    bool check = false;
                    if (j == FieldSize - 1)
                    {
                        check = true;
                    }
                    else
                    {
                        if (Field[i, j] == Field[i, j + 1] && Field[i, j] != -1)
                        {
                            matchLen++;
                        }
                        else
                        {
                            check = true;
                        }
                    }
                    if (check)
                    {
                        if (matchLen >= minClusterLength)
                        {
                            clusters.Add(new Cluster(i, j + 1 - matchLen, matchLen, true));
                        }
                        matchLen = 1;
                    }
                }
            }

            // вертикальные
            for (int j = 0; j < FieldSize; j++)
            {
                int matchLen = 1;
                for (int i = 0; i < FieldSize; i++)
                {
                    bool check = false;
                    if (i == FieldSize - 1)
                    {
                        check = true;
                    }
                    else
                    {
                        if (Field[i, j] == Field[i + 1, j] && Field[i, j] != -1)
                        {
                            matchLen++;
                        }
                        else
                        {
                            check = true;
                        }
                    }
                    if (check)
                    {
                        if (matchLen >= minClusterLength)
                        {
                            clusters.Add(new Cluster(i + 1 - matchLen, j, matchLen, false));
                        }
                        matchLen = 1;
                    }
                }
            }
            return clusters;
        }

        private void removeClusters(List<Cluster> clusters, bool isFirst) // уничтожение кластеров
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                Cluster cluster = clusters[i];

                if (isFirst)
                    Score += (int)cluster.Length;

                int xOffset = 0;
                int yOffset = 0;

                for (int j = 0; j < cluster.Length; j++)
                {
                    Field[cluster.X + xOffset, cluster.Y + yOffset] = -1;

                    if (cluster.Horizontal)
                    {
                        yOffset++;
                    }
                    else
                    {
                        xOffset++;
                    }
                }
            }
        }

        private bool checkField() // проверка поля на "целостность"
        {
            bool isFull = true;
            for (int i = FieldSize - 1; i >= 0; i--)
            {
                for (int j = 0; j < FieldSize; j++)
                {
                    int shift = 0;
                    while (i + shift + 1 < FieldSize && Field[i + shift + 1, j] == -1)
                    {
                        shift++;
                    }
                    if (shift != 0)
                    {
                        isFull = false;
                        swap(j, i, j, i + shift);
                        OnSwap(j, i, j, i + shift);
                    }
                }
            }
            return isFull;
        }

        public List<Move> findMoves() // поиск доступных ходов
        {
            List<Move> avaibleMoves = new List<Move>();
            int[,] diff = { { 1, 0 }, { 0, 1 } };
            for (var k = 0; k < 2; k++)
            {
                for (var i = 0; i < FieldSize - diff[k, 0]; i++)
                {
                    for (var j = 0; j < FieldSize - diff[k, 1]; j++)
                    {
                        swap(i, j, i + diff[k, 0], j + diff[k, 1]);
                        List<Cluster> clusters = findClusters();
                        swap(i, j, i + diff[k, 0], j + diff[k, 1]);

                        if (clusters.Count > 0)
                        {
                            avaibleMoves.Add(new Move(i, j, i + diff[k, 0], j + diff[k, 1]));
                        }
                    }
                }
            }
            return avaibleMoves;
        }
    }
}