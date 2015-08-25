using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using SuperXML;

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
            Compiler.Filters.Add("helloFilter", input =>
            {
                return "Hello " + input;
            });
        }

        private int[] _generateInts(int times)
        {
            var l = new List<int>();
            for (var i = 0; i < times; i++)
            {
                l.Add(i);
            }
            return l.ToArray();
        }

        private void CompileClick(object sender, RoutedEventArgs e)
        {
            var compiler = new Compiler()
                .AddKey("name", "Excel")
                .AddKey("width", 100)
                .AddKey("height", 500)
                .AddKey("bounds", new[] {10, 0, 10, 0})
                .AddKey("elements", new []
                {
                    new User {Name = "John", Age=13},
                    new User {Name = "Maria", Age=57},
                    new User {Name = "Mark", Age=23},
                    new User {Name = "Edit", Age=82},
                    new User {Name = "Susan", Age=37}
                })
                .AddKey("numbers", _generateInts(10000));

            

            var startedTime = DateTime.Now;
            //var compiled = compiler.CompileXml(new StringReader(SourceBox.Text));
            //var onlyContet = compiler.Compile(new StringReader(SourceBox.Text),
            //    x => x.Children.First(y => y.Name == "content"));
            var compiledAsString = compiler.CompileString(SourceBox.Text);
            ResultBlock.Text = "Compilation total time " + (DateTime.Now - startedTime).TotalMilliseconds + "ms";

            CompiledBox.Text = compiledAsString;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Xml Example
            //using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "testFile.xml"))
            //{ 
            //    SourceBox.Text = sr.ReadToEnd();
            //}


            //String Exmaple
            using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "TextFile.txt"))
            {
                SourceBox.Text = sr.ReadToEnd();
            }
        }

        public class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
        } 
    }
}
