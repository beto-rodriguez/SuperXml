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

            int? nullableInt = 10;
            int[] i = populate(150);

            var compiler = new Compiler()
                //.AddElementToScope("Ticket",
                //    new
                //    {
                //        LocalData = new { ImageSource = @"C:\Users\Alberto\Desktop\Test", Header = "un encabezado" },
                //        Type = "Sell",
                //        Items =
                //            new[]
                //            {
                //                new {Q = 1, Style = "#A", UnitValue = "10", Total = "10", Text = "Hola"},
                //                new {Q = 2, Style = "#B", UnitValue = "10", Total = "20", Text= "Hola"},
                //                new {Q = 1, Style = "#A", UnitValue = "10", Total = "10", Text = "Hola"},
                //                new {Q = 2, Style = "#B", UnitValue = "10", Total = "20", Text= "Hola"},
                //                new {Q = 1, Style = "#A", UnitValue = "10", Total = "10", Text = "Hola"}
                //            },
                //        TotalAsCurrency = "$100.00",
                //        WrittenTotal = "cien pesos 00/100",
                //        TicketConfig = new { SalingFooter = "Pie de página" }
                //    })
                //.AddElementToScope("width", nullableInt)
                //.AddElementToScope("smallWidth", 42);
                .AddElementToScope("numbers", new [] {1,2,3});
                //.AddElementToScope("number", 100);

            var startedTime = DateTime.Now;
            var compiled = compiler.Compile(new StringReader(SourceBox.Text));
            ResultBlock.Text = "Compilation total time " + (DateTime.Now - startedTime).TotalMilliseconds + "ms";

            CompiledBox.Text = compiled;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //SourceBox.Text = "<EscPos FixedWidth=\"{{width}}\" SmallWidth=\"{{smallWidth}}\">" +
            //                    "<Image Alt=\"X\" Source=\"{{Ticket.LocalData.ImageSource}}\" MaxHeigth=\"200\" MaxWidth=\"350\"/>" +
            //                    "<Row>" +
            //                    "<Column>" +
            //                    "<TextBlock>{{Ticket.LocalData.Header}}</TextBlock>" +
            //                    "</Column>" +
            //                    "</Row>" +
            //                    "<Row>" +
            //                    "<Column>" +
            //                    "<TextBlock>" +
            //                    "Nota de Venta {{Ticket.Type == 'Sell'}}" +
            //                    "</TextBlock>" +
            //                    "</Column>" +
            //                    "</Row>" +
            //                    "<Row Modifiers=\"Bold Underline\">" +
            //                    "<Column Width=\"8\"><TextBlock HorizontalAlingment=\"Center\">Q</TextBlock></Column>" +
            //                    "<Column Width=\"25\"><TextBlock HorizontalAlingment=\"Center\">Modelo</TextBlock></Column>" +
            //                    "<Column Width=\"32\"><TextBlock HorizontalAlingment=\"Center\">PU</TextBlock></Column>" +
            //                    "<Column Width=\"35\"><TextBlock HorizontalAlingment=\"Center\">Total</TextBlock></Column>" +
            //                    "</Row>" +
            //                    "<Expression ForEach=\"item in Ticket.Items\">" +
            //                    "<Row>" +
            //                    "<Column Width=\"8\"><TextBlock HorizontalAlingment=\"Center\">{{item.Q}}</TextBlock></Column>" +
            //                    "<Column Width=\"25\"><TextBlock HorizontalAlingment=\"Center\">{{item.Style}}</TextBlock></Column>" +
            //                    "<Column Width=\"32\"><TextBlock HorizontalAlingment=\"Right\">{{item.UnitValue}}</TextBlock></Column>" +
            //                    "<Column Width=\"35\"><TextBlock HorizontalAlingment=\"Right\">{{item.Total}}</TextBlock></Column>" +
            //                    "</Row>" +
            //                    "<Row Modifiers=\"Small Underline\">" +
            //                    "<Column>" +
            //                    "<TextBlock HorizontalAlingment=\"Center\" MaxWidth=\"{{smallWidth}}\">" +
            //                    "{{item.Text}}" +
            //                    "</TextBlock>" +
            //                    "</Column>" +
            //                    "</Row>" +
            //                    "</Expression>" +
            //                    "<Row>" +
            //                    "<Column><TextBlock HorizontalAlingment=\"Right\">{{Ticket.TotalAsCurrency}}</TextBlock></Column>" +
            //                    "</Row>" +
            //                    "<Row Modifiers=\"Small\">" +
            //                    "<Column><TextBlock>{{Ticket.WrittenTotal}}</TextBlock></Column>" +
            //                    "</Row>" +
            //                    "<Row Modifiers=\"Negative\">" +
            //                    "<Column><TextBlock>{{Ticket.TicketConfig.SalingFooter}}</TextBlock></Column>" +
            //                    "</Row>" +
            //                    "</EscPos>";

            //SourceBox.Text = "<Document><Element ForEach=\"number in numbers\">{{number}}</Element></Document>";

            Func<int, string, string> stringtify = (x,y) => {
                var z = "";
                for (int i = 0; i < x; i++)
                {
                    z += y;
                }
                return z;
            };

            //SourceBox.Text = "<doc>" + stringtify(1, "<a>{{number}}</a>") + "</doc>";

            //SourceBox.Text = "<doc>" +
            //                   "<a></a>" +
            //                     "<a1>" +
            //                       "<a11></a11>" +
            //                       "<a12></a12>" +
            //                     "</a1>" +
            //                     "<a2>" +
            //                     "</a2>" +
            //                     "<a3>" +
            //                     "</a3>" +
            //                   "</a>" +
            //                   "<b></b>" +
            //                   "<c></c>" +
            //                 "</doc>";
        }
    }
}
