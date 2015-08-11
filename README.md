# SuperXml

SuperXml is a light weight and fast library to use angular-like markup in xml files.
Useful to create Xml templates.
It uses a fast Compiler class that evaluates your initial markup and returns a new xml file.
the compiler uses [NCalc](https://www.nuget.org/packages/ncalc/).

#NuGet
https://www.nuget.org/packages/SuperXML/
```
Install-Package SuperXML 
```
#Example

c#
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
                //you can add enums, classes, integers, arrays, arrays of classes with nested clases...
                //it does not matter! just add them.
                //if the path exist it will compile correctly (myclass.myproperty.myfield)

//Compile from a Xml File
string compiled = compiler.Compile(@"C:\...\myXml.xml");

//Or from a string
string compiled = compiler.Compile(new StringReader("<doc><.../></doc>"));
```

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
      <name>{{element.age}}</name>
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
      <name>57</name>
    </element>
    <element>
      <name>Edit</name>
      <name>82</name>
    </element>
    <element>
      <name>Susan</name>
      <name>37</name>
    </element>
  </content>
</document>
```
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
#If Command
Evaluates if an XmlNode should be included according to condition. condition can include everything supported by ncalc (most of common things). examples:
* `<MyElement If="10 > 6"/>` numeric.
* `<MyElement If="aValueFromScope == 'visible'"/>` string and from scope
* `<MyElement If="10 > h && aValueFromScope == 'visible'"/>` another example

#ForEach Command
Repeats an Xmlnode the same number of times as elements in the array. Example
* `<MyElement ForEach="number in numbers" />` where numbers is an array in the scope.

#TemplateBlock Command
this command is usefull when you need to group elements into a command, this tag is erased when compiled. Example:
```
<Document>
  <TemplateBlock ForEach="number in [1,2,3]">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </TemplateBlock>
  <TemplateBlock If="8 > 7">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </TemplateBlock>
</Document>
```
#Supported Types:
When you use `.AddElementToScope(Key, Value)`, Value is dynamic, that means that it will be evaluated at runtime, so 
it should support all kind of types, enums, classes, all elements and commands can be nested with no problem.
#Performance
from `<element ForEach="element in elements">{{element}}</element>` and elements equals to an array of 10,000 integers Core i5 @ 2.3 GHz took an average of 300 ms to compile in release.

Compile only what you need
```
var onlyContet = compiler.Compile(new StringReader(SourceBox.Text), 
                 x => x.Children.First(y => y.Name == "content")); 
```


#Debug
when a property is not found in the Compiler Scope, Compiler will let you know wich name could not be found. it uses Trace.WriteLine(), so in visual studio you will find it in the output window.
