using System;
using System.IO;
using System.Linq;
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
            var compiler = new Compiler()
                .AddElementToScope("name", "Excel")
                .AddElementToScope("width", 100)
                .AddElementToScope("height", 500)
                .AddElementToScope("bounds", new[] {10, 0, 10, 0})
                .AddElementToScope("elements", new []
                {
                    new { name = "John", age= 10 },
                    new { name = "Maria", age= 57 },
                    new { name = "Mark", age= 23 },
                    new { name = "Edit", age= 82 },
                    new { name = "Susan", age= 37 }
                });


            var startedTime = DateTime.Now;
            //var compiled = compiler.Compile(new StringReader(SourceBox.Text));
            var onlyContet = compiler.Compile(new StringReader(SourceBox.Text),
                x => x.Children.First(y => y.Name == "content"));
            ResultBlock.Text = "Compilation total time " + (DateTime.Now - startedTime).TotalMilliseconds + "ms";

            CompiledBox.Text = onlyContet;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "testFile.xml"))
            { 
                SourceBox.Text = sr.ReadToEnd();
            }
        }
    }
}
