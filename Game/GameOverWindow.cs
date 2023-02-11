using System;
namespace Game
{
    public partial class GameOverWindow : Gtk.Window
    {
        public GameOverWindow(int score) :
                base(Gtk.WindowType.Toplevel)
        {
            this.Build();
            labelScore.Text = "Ваш счёт: " + score;
        }

        protected void OnDeleteEvent(object o, Gtk.DeleteEventArgs args)
        {
            buttonBack.Click();
        }

        protected void OnButtonBackClicked(object sender, EventArgs e)
        {
            new MainWindow();
            this.Destroy();
        }
    }
}
