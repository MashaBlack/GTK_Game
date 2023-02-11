using Gtk;
using Gdk;
using Cairo;
using System;
namespace Game
{
    struct Offset // смещение элементов (для анимации)
    {
        public int X, Y;
        public Offset(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    enum State { Game, Animation } // паттерн состояния
    // game - процесс игры (ждёт действие пользователя)
    // Animation – анимация (хода, уничтожения кластера и тд)

    public partial class GameWindow : Gtk.Window
    {
        GameField gameField;
        private const int fieldSize = 8;
        private const int numElements = 5;
        private const int elSize = 70;
        private const int margin = 2;
        private const int gameTime = 60 * 1000;
        private const int timerFrequency = 100;
        private double[,] colors = {
            {0, 0, 0}, // белый
            {255, 0, 0}, // красный
            {255, 165, 0}, // оранжевый
            {255, 255, 0}, // жёлтый
            {0, 255, 0}, // зелёный
            {0, 0, 255}, // голубой
        };

        private Offset[,] offsets;
        private int chosenX = -1, chosenY = -1, swapX, swapY;
        private int timeLeft = gameTime;
        private bool isChosen = false;
        private bool wasMove = false;
        private State state;

        public GameWindow() :
            base(Gtk.WindowType.Toplevel)
        {
            this.Build();

            state = Game.State.Game;

            for (int i = 0; i < colors.Length / 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    colors[i, j] /= 255.0;
                }
            }

            offsets = new Offset[fieldSize, fieldSize];
            for (int i = 0; i < fieldSize; i++)
            {
                for (int j = 0; j < fieldSize; j++)
                {
                    offsets[i, j] = new Offset(0, 0);
                }
            }

            gameField = new GameField(fieldSize, numElements, updateOffsets); // создаём экземпляр класса GameField

            drawingarea.AddEvents((int)EventMask.ButtonPressMask); // устанавливаем маску события
            drawingarea.ButtonPressEvent += OnFieldClick; // добавляем обработчик события

            GLib.Timeout.Add(timerFrequency, new GLib.TimeoutHandler(OnTimer));  // таймер
        }

        bool OnTimer()
        {
            drawingarea.QueueDraw();
            animation();
            updateTime();
            updateScore();
            if (checkEnd())
                return false;
            return true;
        }

        private void animation() // анимация перемещения, уничтожения кластеров и добавления новых элементов
        {
            bool isAnim = false;
            for (int i = 0; i < fieldSize; i++)
            {
                for (int j = 0; j < fieldSize; j++)
                {
                    if (offsets[i, j].X != 0)
                    {
                        offsets[i, j].X -= offsets[i, j].X / Math.Abs(offsets[i, j].X) * 24;
                        isAnim = true;
                    }
                    if (offsets[i, j].Y != 0)
                    {
                        offsets[i, j].Y -= offsets[i, j].Y / Math.Abs(offsets[i, j].Y) * 24;
                        isAnim = true;
                    }
                }
            }
            if (!isAnim && state == Game.State.Animation)
            {
                int oldScore = gameField.Score;
                gameField.resolveClusters();

                if (oldScore == gameField.Score && wasMove)
                {
                    gameField.swap(chosenX, chosenY, swapX, swapY);
                    updateOffsets(chosenX, chosenY, swapX, swapY);
                    wasMove = false;
                }
                state = Game.State.Game;
            }
        }

        private void updateTime() // обновление таймера на экране
        {
            timeLeft -= timerFrequency;
            labelTime.Text = "Время: " + timeLeft / 1000;
        }

        private void updateScore() // обновление счёта на экране
        {
            labelScore.Text = "Счёт: " + gameField.Score;
        }

        private bool checkEnd() // проверка оставшегося времени
        {
            if (timeLeft <= 0) // если время вышло, закрытие окна с игрой и открытие окна "конец игры"
            {
                new GameOverWindow(gameField.Score);
                Destroy();
                return true;
            }
            return false;
        }

        private void drawField(Context cc) // отрисовка поля со всеми элементами
        {
            cc.LineWidth = 1;
            int x = margin, y = margin;

            for (int i = 0; i < gameField.FieldSize; i++)
            {
                for (int j = 0; j < gameField.FieldSize; j++)
                {
                    cc.Rectangle(new PointD(x + offsets[i, j].X, y + offsets[i, j].Y),
                                 elSize, elSize);
                    int id = gameField.Field[i, j] + 1;
                    cc.SetSourceRGB(colors[id, 0], colors[id, 1], colors[id, 2]);
                    cc.Fill();
                    x += elSize + margin;
                }
                y += elSize + margin;
                x = margin;
            }
        }

        void drawSelectionFrame(Context cc) // отрисовка рамки для выбранного элемента
        {
            if (chosenX == -1 || chosenY == -1 || !isChosen)
                return;
            cc.LineWidth = 2;
            cc.Rectangle(new PointD(chosenX * elSize + margin * (chosenX + 1),
                                    chosenY * elSize + margin * (chosenY + 1)),
                         elSize, elSize);
            cc.SetSourceRGB(128, 0, 128);
            cc.StrokePreserve();
        }

        void updateOffsets(int x1, int y1, int x2, int y2) // смещение элементов при анимации
        {
            state = Game.State.Animation;
            int offsetX = (elSize + margin) * (x2 - x1);
            int offsetY = (elSize + margin) * (y2 - y1);
            offsets[y1, x1].X = offsetX;
            offsets[y1, x1].Y = offsetY;
            offsets[y2, x2].X = -offsetX;
            offsets[y2, x2].Y = -offsetY;
        }

        protected void OnDeleteEvent(object o, DeleteEventArgs args) // если нажата кнопка закрыть
        {
            timeLeft = 0;
        }

        protected void OnDrawingAreaExposeEvent(object o, ExposeEventArgs args) // проявление drawingarea
        {
            DrawingArea area = (DrawingArea)o;
            Context cc = CairoHelper.Create(area.GdkWindow);
            drawField(cc);
            drawSelectionFrame(cc);
            ((IDisposable)cc.GetTarget()).Dispose();
            ((IDisposable)cc).Dispose();
        }

        private void OnFieldClick(object o, ButtonPressEventArgs args) // обработка события (нажатие мышкой на drawingarea)
        {
            if (!isChosen) // когда ни одного выбранного элемента
            {
                chosenX = (int)(args.Event.X / (elSize + margin));
                chosenY = (int)(args.Event.Y / (elSize + margin));
                isChosen = true;
            }
            else // когда уже есть выбранный элемент
            {
                swapX = (int)(args.Event.X / (elSize + margin));
                swapY = (int)(args.Event.Y / (elSize + margin));

                if (gameField.canSwap(chosenX, chosenY, swapX, swapY))
                {
                    gameField.swap(chosenX, chosenY, swapX, swapY);
                    updateOffsets(chosenX, chosenY, swapX, swapY);
                }
                isChosen = false;
                wasMove = true;
            }
        }
    }
}
