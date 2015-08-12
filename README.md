# Templator (Tor)

SuperXml is a light weight and fast library to build string, Xml and Html Templates.
It uses a fast Compiler class that evaluates your initial markup and returns a new xml file.
Its is a native alternative for http://www.stringtemplate.org/, with a intuitive markup.

#Install From Nuget 
https://www.nuget.org/packages/SuperXML/
```
Install-Package SuperXML 
```
#Example
The first step to compile a tmeplate is to set up a compiler class. You can add elements to your compiler Scope so they can be evaluated when compiled. You can add as many elements as you need they can be of any type. when you add an element that already exists in the scope it will override the last value.
```
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
```
After Scope is ready all you need to do is call the compile method according to your needs
```
compiler.Compile("Hello {{name}}") // a string
compiler.CompileXml(@"c:/.../file.xml"); //a xml file
compiler.CompileXml(new StringReader("<doc><.../></doc>"));//a xml string
```
#String Example
Template:
```
Hello {{name}}, you are a document with a size of {{width}}x{{height}} and an area of {{width*height}}

now here is a list with your bounds:
  <Tor.Run Tor.Repeat="b in bounds">-value {{$index}}: {{b}}
  </Tor.Run>

now here you can see a filtered list of clases
  <Tor.Run Tor.Repeat="e in elements" Tor.If="e.age > 25">-{{e.name}}, age {{e.age}}
  </Tor.Run>
```
Result:
```
Hello Excel, you are a document with a size of 100x500 and an area of 50000

now here is a list with your bounds:
  -value 0: 10
  -value 1: 0
  -value 2: 10
  -value 3: 0
  

now here you can see a filtered list of clases
  -Maria, age 57
  -Edit, age 82
  -Susan, age 37
```
this is how it looks in C#
```
var template = "...a string containing the template of above..."
var compiled = new Compiler()
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
                })
                .CompileString(template);
  //compiled now contains the string of the result above
```
#XLM File Example
Input XML
```
<document>
  <name>my name is {{name}}</name>
  <width>{{width}}</width>
  <height>{{height}}</height>
  <area>{{width*height}}</area>
  <padding>
    <bound ForEach="bound in bounds">{{bound}}</bound>
  </padding>
  <content>
    <element ForEach="element in elements" If="element.age > 25">
      <name>{{element.name}}</name>
      <age>{{element.age}}</age>
    </element>
  </content> 
</document>
```
Compiled
```
<document>
  <name>my name is Excel</name>
  <width>100</width>
  <height>500</height>
  <area>50000</area>
  <padding>
    <bound>10</bound>
    <bound>0</bound>
    <bound>10</bound>
    <bound>0</bound>
  </padding>
  <content>
    <element>
      <name>Maria</name>
      <age>57</age>
    </element>
    <element>
      <name>Edit</name>
      <age>82</age>
    </element>
    <element>
      <name>Susan</name>
      <age>37</age>
    </element>
  </content>
</document>
```
dont forget to use `compiler.CompileXml(@"C:\...\myXml.xml");` if source is a file or `compiler.CompileXml(new StringReader("<doc><.../></doc>"));` if source is a string.
#Math and Logical Operatos
math operations are evaluated by Ncalc, basically it works with the same syntax used in C#. for more info go to https://ncalc.codeplex.com/
```
<Document>
  <Math>
    2 + 2 = {{2+2}}, 2 x 2 = {{2*2}}, 2 / 2 = {{2/2}},
    (2+2)/(2+2/2)x2 = {{(2+2)/(2+2/2)*2}}
  </Math>
  <Logical>
    2 > 5 {{2>5}}, 4 = 9 {{ 4 == 9 }},
    for strings use '', for example (hola = hello {{'hello' == 'hello'}})
  </Logical>
</Document>
```
Compiled
```
<Document>
  <Document>
  <Math>
    2 + 2 = 4, 2 x 2 = 4, 2 / 2 = 1,
    (2+2)/(2+2/2)x2 = .66
  </Math>
  <Logical>
    2 > 5 False, 4 = 9 False,
    for strings use '', for example (hello = hello True)
  </Logical>
</Document>
</Document>
```
#Dot Notation
Dot notation is usefull when you add clases to compiler Scope, in the next example we added an User class with a string property `name`, a string property `lastName` and a integer property `age`, you can add any type and nest as many classes as necesary.
Input XML
```
<Document>
  <Text>
    {{user.name}} {{user.lastName}}, age: {{user.age}}
  </Text>
</Document>
```
Compiled
```
<Document>
  <Text>
    Roger Martinez, age: 20
  </Text>
</Document>
```
#Tor.If Command
Evaluates if an XmlNode should be included according to condition. condition can include everything supported by ncalc (most of common things). examples:
* `<MyElement Tor.If="10 > 6"/>` numeric.
* `<MyElement Tor.If="aValueFromScope == 'visible'"/>` string and from scope
* `<MyElement Tor.If="10 > h && aValueFromScope == 'visible'"/>` another example

#Tor.Repat Command
Repeats the element the same number of times as items in the array. Example
* `<MyElement Tor.Repeat="number in numbers" myAttribute="{{number}}" />` where numbers is an array in the scope.

Each repeated element has an cero based index that indicates its position in the repeater you can access it using `$index`
```
//Input
<element Tor.Repeat="element in elements">{{$index}}</element>
//Output
<element>0</element>
<element>1</element>
<element>2</element>
...
<element>n</element>
```
#Tor.Run Command
this command is usefull to create lists of strings or when you need to group elements into a command, this tag is erased when compiled. Example:
```
<Document>
  <Tor.Run Tor.Repeat="number in numbers">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </Tor.Run>
  <Tor.Run Tor.If="8 > 7">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </Tor.Run>
</Document>
```
or create lists
```
<Tor.Run Tor.Repeat="e in elements" Tor.If="e.age > 25">
  * {{e.name}}, age {{e.age}}
</Tor.Run>
```
#Supported Types:
When you use `.AddElementToScope(Key, Value)`, Value is dynamic, that means that it will be evaluated at runtime, so 
it should support all kind of types, enums, classes, all elements and commands can be nested with no problem.
#Performance
from `<element ForEach="element in elements">{{element}}</element>` and elements equals to an array of 10,000 integers Core i5 @ 2.3 GHz took an average of 300 ms to compile in release.

Sometime Xml files contains elements that you dont need to compile. to improve performance compile only what you need.
```
var onlyContet = compiler.CompileXml(new StringReader(SourceBox.Text), 
                 x => x.Children.First(y => y.Name == "content")); 
```


#Debug
when a property is not found in the Compiler Scope, Compiler will let you know wich name could not be found. it uses Trace.WriteLine(), so in visual studio you will find it in the output window.
Warning when a property is not found the impact in performance is huge!.
