# SuperXml

SuperXml is a light weight and fast library to use angular-like markup in xml files.
It uses a fast Compiler class that evaluates your initial markup and returns a new xml file.
the compiler uses [NCalc](https://www.nuget.org/packages/ncalc/) and [Newtonsoft.Json](http://www.newtonsoft.com/json).

#Basic Example

Input XML
```
<Document>
  <Text>
    {{sayHello}} {{name}}
  </Text>
</Document>
```
c#
```
var compiled = new Compiler()
    .SetDocumentFromFile(@"C:\Users\...\myXml.xml")
    .AddElementToScope("sayHello", "Hello")
    .AddElementToScope("name", "World").Compile();
```
Compiled
```
<Document>
  <Text>
    Hello World
  </Text>
</Document>
```
#Math and Logical Operatos
math operations are evaluated by Ncalc, basically it works with the same syntax used in C#. for more info go to https://ncalc.codeplex.com/
Input XML
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
it has no limitation you can use a long path as necesary, you can use all kind of types, arrays, classes etc.
Input XML
```
<Document>
  <Text>
    {{user.name}} {{user.lastName}}, age: {{user.age}}
  </Text>
</Document>
```
c#
```
//user is a class with 3 properties, name(string), lastName(string) and age(int).
User user = new User {name = "Roger", lastName = "Martinez", age = 20};

var compiled = new Compiler()
    .SetDocumentFromFile(@"C:\Users\...\myXml.xml")
    .AddElementToScope("user", user).Compile();
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
commands do not need {{ }}, they are already expressions.
Input XML
```
<Document>
  <Text If="number > .5">
    number is more than .5
  </Text>
  <Text If="number <= .5">
    number is less than .5
  </Text>
</Document>
```
c#
```
var compiled = new Compiler()
    .SetDocumentFromFile(@"C:\Users\...\myXml.xml")
    .AddElementToScope("number", .2)
    .Compile();
```
Compiled
```
<Document>
  <Text>
    number is less than .5
  </Text>
</Document>
```
#ForEach Command
commands do not need {{ }}, they are already expressions.
Input XML
```
<Document>
  <!--Example from scope-->
  <Node ForEach="node in nodes">
    {{node}}
  </Node>
  <!--Example from markup, use json format it is deserialized by Newtonsoft.Json-->
  <Node ForEach="node in [1,2,3]"> 
    {{node}}
  </Node
  <!--more markup exmaples:
    objects: [{name: 'mark', age: 20}, {name: 'juliet', age: 50}, {name: 'unknown', age: 56}],
    strings: ['hello', 'world']
  -->
</Document>
```
c#
```
int[] arrayExample = {1, 2, 3};
var compiled = new Compiler()
    .SetDocumentFromFile(@"C:\Users\...\myXml.xml")
    .AddElementToScope("nodes", arrayExample)
    .Compile();
```
Compiled
```
<Document>
  <!--Example from scope-->
  <Node>
    1
  </Node>
  <Node>
    2
  </Node>
  <Node>
    3
  </Node>
  <!--Example from markup, use json format it is deserialized by Newtonsoft.Json-->
  <Node> 
    1
  </Node
  <Node> 
    2
  </Node
  <Node> 
    3
  </Node
  <!--more markup exmaples:
    objects: [{name: 'mark', age: 20}, {name: 'juliet', age: 50}, {name: 'unknown', age: 56}],
    strings: ['hello', 'world']
  -->
</Document>
```
#Expression Command
Expression command is usefull when you need to group elements into a command, this tag is erased when compiled
Input XML
```
<Document>
  <Expression ForEach="number in [1,2,3]">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </Expression>
  <Expression If="8 > 7">
    <text1></text1>
    <text2></text2>
    ...
    <text3></text3>
  </Expression>
</Document>
```
#Supported Types:
when you use `.AddElementToScope(Key, Value)`, value is dynamic, that means that it will be evaluated at runtime, so 
it should support all kind of types, enums, classes, all elements and commands can be nested with no problem.
#Performance
