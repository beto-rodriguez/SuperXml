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
using Microsoft.Win32;
using SuperXml;

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CompileClick(object sender, RoutedEventArgs e)
        {
            Func<int, int[]> populate = x =>
            {
                var l = new List<int>();
                for (int j = 0; j < x; j++)
                {
                    l.Add(j);
                }
                return l.ToArray();
            };

            int[] i = populate(150);
            var compiler = new Compiler().SetDocumentFromString(SourceBox.Text).AddElementToScope("numbers", i);
            var startedTime = DateTime.Now;
            var compiled = await compiler.CompileAsync();
            ResultBlock.Text = "Compilation total time " + (DateTime.Now - startedTime).TotalMilliseconds + "ms";

            CompiledBox.Text = compiled.OuterXml;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SourceBox.Text = "<Document><Element ForEach=\"number in numbers\">{{number}}</Element></Document>";
        }
    }
}
