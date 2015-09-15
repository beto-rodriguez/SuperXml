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
                .AddKey("m", new ViewModel
                {
                    Name = "Excel",
                    Date = new DateTime?(),
                    Width = 100,
                    Height = 300,
                    Bounds = new List<int>
                    {
                        1,
                        2,
                        3,
                        4,
                        5
                    },
                    Users = new List<User>
                    {
                        new User {Name = "John", Age = 13},
                        new User {Name = "Maria", Age = 57},
                        new User {Name = "Mark", Age = 23},
                        new User {Name = "Edit", Age = 82},
                        new User {Name = "Susan", Age = 37}
                    },
                    NullUser = new User
                    {
                        Name = null,
                        Age = 10,
                        Date = new DateTime?()
                    }
                });

            Compiler.OnNullOrNotFound = "";

            var startedTime = DateTime.Now;
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
            using (var sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "testfile.xml"))
            {
                SourceBox.Text = sr.ReadToEnd();
            }
        }

        public class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DateTime? Date { get; set; }
        } 

        public class ViewModel
        {
            public string Name { get; set; }
            public DateTime? Date { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public List<int> Bounds { get; set; }
            public List<User> Users { get; set; } 
            public User NullUser { get; set; }
        }
    }
}
