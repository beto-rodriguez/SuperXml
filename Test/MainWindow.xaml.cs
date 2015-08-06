using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
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

        private void CompileClick(object sender, RoutedEventArgs e)
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

            var compiler = new Compiler()
                .AddElementToScope("numbers", populate(10000));

            var startedTime = DateTime.Now;
            var compiled = compiler.Compile(new StringReader(SourceBox.Text));
            ResultBlock.Text = "Compilation total time " + (DateTime.Now - startedTime).TotalMilliseconds + "ms";

            CompiledBox.Text = compiled.ToString();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
